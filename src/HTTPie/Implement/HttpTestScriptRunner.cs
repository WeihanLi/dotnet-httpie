// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Dynamic;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HTTPie.Implement;

/// <summary>
/// Provides access to the response body in different formats inside test scripts.
/// </summary>
public sealed class ResponseBodyContext
{
    private readonly HttpResponseMessage _response;
    private string? _text;
    private dynamic? _json;

    internal ResponseBodyContext(HttpResponseMessage response) => _response = response;

    /// <summary>The response body as a string (cached after first access).</summary>
    public string text
    {
        get
        {
            _text ??= _response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return _text;
        }
    }

    /// <summary>
    /// The response body parsed as JSON, allowing property-path access such as
    /// <c>response.body.json.id</c> or <c>response.body.json.user.name</c>.
    /// </summary>
    public dynamic json
    {
        get
        {
            _json ??= HttpTestScriptRunner.ParseJsonToDynamic(text);
            return _json;
        }
    }
}

/// <summary>
/// Wraps an <see cref="HttpResponseMessage"/> and exposes convenience members
/// (status code, body access, assertion helpers) for use in postScript expressions.
/// </summary>
public sealed class HttpTestResponseContext
{
    internal HttpTestResponseContext(HttpResponseMessage response)
    {
        Response = response;
        body = new ResponseBodyContext(response);
    }

    /// <summary>The underlying raw <see cref="HttpResponseMessage"/>.</summary>
    public HttpResponseMessage Response { get; }

    /// <summary>The HTTP status code as an integer.</summary>
    public int StatusCode => (int)Response.StatusCode;

    /// <summary>Provides access to the response body as text or parsed JSON.</summary>
    public ResponseBodyContext body { get; }

    /// <summary>
    /// Throws <see cref="HttpTestAssertionException"/> if the response status code is not a 2xx success code.
    /// </summary>
    public void EnsureSuccessStatusCode()
    {
        if (StatusCode < 200 || StatusCode >= 300)
            throw new HttpTestAssertionException(
                $"EnsureSuccessStatusCode failed: response status code {StatusCode} is not a success status.");
    }

    /// <summary>
    /// Throws <see cref="HttpTestAssertionException"/> when <paramref name="condition"/> is false.
    /// </summary>
    public void Assert(bool condition, string? message = null)
    {
        if (!condition)
            throw new HttpTestAssertionException(message ?? "Assertion failed.");
    }
}

/// <summary>
/// Globals object passed to both preScript and postScript when executing via Roslyn.
/// Exposes the raw <see cref="HttpRequestMessage"/>, an <see cref="HttpTestResponseContext"/>
/// wrapping the raw <see cref="HttpResponseMessage"/> (postScript only), and a mutable
/// variables dictionary that scripts can read and update.
/// </summary>
public sealed class HttpTestScriptGlobals
{
    /// <summary>Constructs globals for a preScript (no response available).</summary>
    internal HttpTestScriptGlobals(HttpRequestMessage request, Dictionary<string, string> variables)
    {
        this.request = request;
        this.variables = variables;
    }

    /// <summary>Constructs globals for a postScript.</summary>
    internal HttpTestScriptGlobals(
        HttpRequestMessage request,
        HttpResponseMessage response,
        Dictionary<string, string> variables)
    {
        this.request = request;
        this.response = new HttpTestResponseContext(response);
        this.variables = variables;
    }

    /// <summary>The raw <see cref="HttpRequestMessage"/>; scripts can inspect or mutate headers.</summary>
    public HttpRequestMessage request { get; }

    /// <summary>The response context (populated in postScript; <c>null</c> in preScript).</summary>
    public HttpTestResponseContext? response { get; }

    /// <summary>Merged variables dictionary; scripts can read and write values.</summary>
    public Dictionary<string, string> variables { get; }
}

/// <summary>
/// Evaluates preScript and postScript expressions for HTTP API tests.
/// </summary>
/// <remarks>
/// Simple shorthand patterns (handled via fast-path regex, no Roslyn overhead):
///
///   preScript:
///     $request.headers.add("name", "value")
///     $request.headers.set("name", "value")
///
///   postScript:
///     $response.EnsureSuccessStatusCode()
///     $response.StatusCode == NNN
///     $response.StatusCode != NNN
///     $response.Body.Contains("text")
///
/// Complex C# scripts (compiled and executed via Roslyn CSharp scripting):
///   Scripts that contain any statement not matching the simple patterns above are
///   executed as full C# scripts. The globals object exposes:
///     <c>request</c>   – the raw <see cref="HttpRequestMessage"/>
///     <c>response</c>  – an <see cref="HttpTestResponseContext"/> with <c>.body.json.id</c> support
///     <c>variables</c> – a <see cref="Dictionary{String,String}"/> that can be read/written
///   <c>$request</c> and <c>$response</c> prefixes are rewritten to the global names so that
///   the shorthand syntax still works inside complex scripts.
///
///   preScript example:
///     var key = System.Environment.GetEnvironmentVariable("API_KEY");
///     request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {key}");
///
///   postScript example:
///     response.EnsureSuccessStatusCode();
///     var id = response.body.json.id;
///     response.Assert(id != null, "Response must contain an id field");
///     response.StatusCode.ShouldBe(200);
///     HttpAssert.Contains("token", response.body.text);
/// </remarks>
public static partial class HttpTestScriptRunner
{
    // -----------------------------------------------------------------------
    // Simple fast-path regex patterns
    // -----------------------------------------------------------------------

    [GeneratedRegex(
        @"^\$request\.headers\.(?<op>add|set)\(\s*""(?<name>[^""]+)""\s*,\s*""(?<value>[^""]*)""\s*\)\s*$",
        RegexOptions.IgnoreCase)]
    private static partial Regex RequestHeadersPattern { get; }

    [GeneratedRegex(@"^\$response\.EnsureSuccessStatusCode\(\)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex EnsureSuccessPattern { get; }

    [GeneratedRegex(@"^\$response\.StatusCode\s*(?<op>==|!=)\s*(?<code>\d{3})\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex StatusCodePattern { get; }

    [GeneratedRegex(@"^\$response\.Body\.Contains\(\s*""(?<text>[^""]*)""\s*\)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex BodyContainsPattern { get; }

    // -----------------------------------------------------------------------
    // Roslyn script options (shared / lazy-initialized)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Shared Roslyn <see cref="ScriptOptions"/> configured with common namespaces and a reference
    /// to the HTTPie assembly so that context types are available inside scripts.
    /// Lazy initialization defers the construction cost until the first complex script is executed.
    /// </summary>
    private static readonly Lazy<ScriptOptions> RoslynScriptOptions = new(() =>
        ScriptOptions.Default
            .AddImports(
                "System",
                "System.Collections.Generic",
                "System.Linq",
                "System.Net",
                "System.Net.Http",
                "System.Net.Http.Headers",
                "System.Text",
                "System.Text.Json",
                "HTTPie.Implement")
            .AddReferences(
                typeof(object).Assembly,
                typeof(DynamicObject).Assembly,
                typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo).Assembly,
                typeof(HttpClient).Assembly,
                typeof(HttpTestScriptRunner).Assembly
            ));

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <summary>
    /// Executes a preScript, potentially mutating the supplied <paramref name="request"/> headers
    /// and <paramref name="variables"/> dictionary.
    /// Simple patterns are evaluated via fast-path regex; any unrecognized statement
    /// causes the entire script to be compiled and run via Roslyn CSharp scripting.
    /// </summary>
    public static async Task ExecutePreScriptAsync(
        string? script,
        HttpRequestMessage request,
        Dictionary<string, string> variables)
    {
        if (string.IsNullOrWhiteSpace(script)) return;

        if (!HasComplexStatements(script, isPreScript: true))
        {
            ExecutePreScriptSimple(script, request);
            return;
        }

        var processedScript = PreprocessScript(script);
        var globals = new HttpTestScriptGlobals(request, variables);
        try
        {
            await CSharpScript.RunAsync(processedScript, RoslynScriptOptions.Value, globals);
        }
        catch (HttpTestAssertionException)
        {
            throw;
        }
        catch (CompilationErrorException ex)
        {
            var diagnostics = string.Join("; ", ex.Diagnostics.Select(d => d.ToString()));
            throw new HttpTestAssertionException($"PreScript compilation error: {diagnostics}");
        }
        catch (Exception ex) when (ex is not HttpTestAssertionException)
        {
            throw new HttpTestAssertionException($"PreScript execution error: {ex.Message}");
        }
    }

    /// <summary>
    /// Evaluates a postScript against the HTTP response.
    /// Simple patterns are evaluated via fast-path regex; any unrecognized statement
    /// causes the entire script to be compiled and run via Roslyn CSharp scripting.
    /// Throws <see cref="HttpTestAssertionException"/> when an assertion fails.
    /// </summary>
    public static async Task ExecutePostScriptAsync(
        string? script,
        HttpRequestMessage request,
        HttpResponseMessage response,
        Dictionary<string, string> variables)
    {
        if (string.IsNullOrWhiteSpace(script)) return;

        if (!HasComplexStatements(script, isPreScript: false))
        {
            await ExecutePostScriptSimpleAsync(script, response);
            return;
        }

        var processedScript = PreprocessScript(script);
        var globals = new HttpTestScriptGlobals(request, response, variables);
        try
        {
            await CSharpScript.RunAsync(processedScript, RoslynScriptOptions.Value, globals);
        }
        catch (HttpTestAssertionException)
        {
            throw;
        }
        catch (CompilationErrorException ex)
        {
            var diagnostics = string.Join("; ", ex.Diagnostics.Select(d => d.ToString()));
            throw new HttpTestAssertionException($"PostScript compilation error: {diagnostics}");
        }
        catch (Exception ex) when (ex is not HttpTestAssertionException)
        {
            throw new HttpTestAssertionException($"PostScript execution error: {ex.Message}");
        }
    }

    // -----------------------------------------------------------------------
    // Simple fast-path implementations (regex-based)
    // -----------------------------------------------------------------------

    private static void ExecutePreScriptSimple(string script, HttpRequestMessage request)
    {
        foreach (var rawLine in script.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("//") || line.StartsWith("#")) continue;

            var match = RequestHeadersPattern.Match(line);
            if (!match.Success) continue;

            var op = match.Groups["op"].Value;
            var name = match.Groups["name"].Value;
            var value = match.Groups["value"].Value;

            if (op.Equals("set", StringComparison.OrdinalIgnoreCase))
                request.Headers.Remove(name);

            request.Headers.TryAddWithoutValidation(name, value);
        }
    }

    private static async Task ExecutePostScriptSimpleAsync(string script, HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;
        string? cachedBody = null;

        foreach (var rawLine in script.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("//") || line.StartsWith("#")) continue;

            if (EnsureSuccessPattern.IsMatch(line))
            {
                if (statusCode < 200 || statusCode >= 300)
                    throw new HttpTestAssertionException(
                        $"EnsureSuccessStatusCode failed: response status code {statusCode} is not a success status.");
                continue;
            }

            var statusMatch = StatusCodePattern.Match(line);
            if (statusMatch.Success)
            {
                var op = statusMatch.Groups["op"].Value;
                var expected = int.Parse(statusMatch.Groups["code"].Value);
                var passed = op == "==" ? statusCode == expected : statusCode != expected;
                if (!passed)
                    throw new HttpTestAssertionException(
                        $"StatusCode assertion failed: expected {op} {expected}, actual {statusCode}.");
                continue;
            }

            var bodyMatch = BodyContainsPattern.Match(line);
            if (bodyMatch.Success)
            {
                cachedBody ??= await response.Content.ReadAsStringAsync();
                var text = bodyMatch.Groups["text"].Value;
                if (!cachedBody.Contains(text, StringComparison.Ordinal))
                    throw new HttpTestAssertionException(
                        $"Body.Contains assertion failed: body does not contain \"{text}\".");
                continue;
            }
        }
    }

    // -----------------------------------------------------------------------
    // JSON helpers (used by ResponseBodyContext)
    // -----------------------------------------------------------------------

    /// <summary>Parses a JSON string into a dynamic object hierarchy.</summary>
    // IL2026/IL3050: JsonSerializer.Deserialize<JsonElement> is called only from the Roslyn
    // scripting path, which is itself not AOT-compatible. Suppressing these intentionally.
#pragma warning disable IL2026, IL3050
    internal static dynamic ParseJsonToDynamic(string json)
    {
        // JsonSerializer.Deserialize<JsonElement> clones the parsed data into a self-owned JsonElement
        // (no JsonDocument handle to dispose), so no lifetime tracking is needed.
        var element = JsonSerializer.Deserialize<JsonElement>(json);
        return ConvertJsonElement(element);
    }
#pragma warning restore IL2026, IL3050

    internal static dynamic ConvertJsonElement(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => new JsonBodyAccessor(element),
        JsonValueKind.Array => ConvertJsonArray(element),
        JsonValueKind.String => element.GetString()!,
        // Int64 is tried first so that whole-number JSON values surface as long rather than double.
        // Values with a fractional part (e.g. 1.5) fall through to double automatically.
        JsonValueKind.Number => element.TryGetInt64(out var l) ? (object)l : element.GetDouble(),
        JsonValueKind.True => (object)true,
        JsonValueKind.False => (object)false,
        _ => (object?)null!
    };

    private static dynamic[] ConvertJsonArray(JsonElement array)
    {
        var items = new dynamic[array.GetArrayLength()];
        var i = 0;
        foreach (var item in array.EnumerateArray())
            items[i++] = ConvertJsonElement(item);
        return items;
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns <c>true</c> when the script contains at least one non-comment line that
    /// does not match any known simple shorthand pattern. Such scripts must be executed
    /// via Roslyn rather than the fast-path regex evaluator.
    /// </summary>
    private static bool HasComplexStatements(string script, bool isPreScript)
    {
        foreach (var rawLine in script.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("//") || line.StartsWith("#")) continue;

            var recognized = isPreScript
                ? RequestHeadersPattern.IsMatch(line)
                : EnsureSuccessPattern.IsMatch(line)
                  || StatusCodePattern.IsMatch(line)
                  || BodyContainsPattern.IsMatch(line);

            if (!recognized) return true;
        }

        return false;
    }

    /// <summary>
    /// Replaces <c>$request</c> and <c>$response</c> prefixes with the C# global names
    /// so that scripts can use either the shorthand or full identifier.
    /// </summary>
    private static string PreprocessScript(string script) =>
        script.Replace("$request", "request").Replace("$response", "response");
}

/// <summary>
/// Dynamic accessor for a JSON object, enabling property-path access
/// (e.g. <c>response.body.json.id</c>) inside test scripts.
/// </summary>
/// <remarks>
/// This type extends <see cref="DynamicObject"/> and is only created within the Roslyn
/// scripting path, which is itself not AOT-compatible. The IL3050 warning is suppressed
/// intentionally because this code is never reached in an AOT-compiled binary.
/// </remarks>
#pragma warning disable IL3050
internal sealed class JsonBodyAccessor : DynamicObject
{
    private readonly JsonElement _element;

    internal JsonBodyAccessor(JsonElement element) => _element = element;

    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        if (_element.TryGetProperty(binder.Name, out var prop))
        {
            result = HttpTestScriptRunner.ConvertJsonElement(prop);
            return true;
        }

        result = null;
        return false;
    }

    public override string ToString() => _element.ToString();
}
#pragma warning restore IL3050

/// <summary>
/// Thrown when an HTTP test assertion (postScript) fails.
/// </summary>
public sealed class HttpTestAssertionException(string message) : Exception(message);

