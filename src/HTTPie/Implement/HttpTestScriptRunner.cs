// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Text.RegularExpressions;

namespace HTTPie.Implement;

/// <summary>
/// Exposes request mutation capabilities inside a preScript.
/// </summary>
public sealed class HttpTestRequestScriptContext
{
    public HttpTestRequestScriptContext(Dictionary<string, string> headers)
    {
        this.headers = new HeadersContext(headers);
    }

    /// <summary>The request headers that can be manipulated by the script.</summary>
    public HeadersContext headers { get; }

    public sealed class HeadersContext(Dictionary<string, string> headers)
    {
        /// <summary>Adds the header if it does not already exist.</summary>
        public void add(string name, string value) => headers.TryAdd(name, value);

        /// <summary>Sets (replaces) the header value.</summary>
        public void set(string name, string value) => headers[name] = value;

        /// <summary>Removes a header by name.</summary>
        public void remove(string name) => headers.Remove(name);

        /// <summary>Returns the header value, or <c>null</c> if absent.</summary>
        public string? get(string name) => headers.TryGetValue(name, out var v) ? v : null;

        /// <summary>Returns whether a header with the given name is present.</summary>
        public bool contains(string name) => headers.ContainsKey(name);
    }
}

/// <summary>
/// Exposes response data and assertion helpers inside a postScript.
/// </summary>
public sealed class HttpTestResponseScriptContext
{
    private readonly Func<Task<string>> _getBody;
    private string? _cachedBody;

    public HttpTestResponseScriptContext(int statusCode, Func<Task<string>> getBody)
    {
        StatusCode = statusCode;
        _getBody = getBody;
    }

    /// <summary>The HTTP response status code.</summary>
    public int StatusCode { get; }

    /// <summary>Reads the response body text (result is cached after first call).</summary>
    public async Task<string> GetBodyAsync()
    {
        _cachedBody ??= await _getBody();
        return _cachedBody;
    }

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

/// <summary>Globals passed to a preScript when executing via Roslyn.</summary>
public sealed class HttpTestPreScriptGlobals(Dictionary<string, string> headers)
{
    /// <summary>The request context available in the script as <c>request</c>.</summary>
    public HttpTestRequestScriptContext request { get; } = new(headers);
}

/// <summary>Globals passed to a postScript when executing via Roslyn.</summary>
public sealed class HttpTestPostScriptGlobals(int statusCode, Func<Task<string>> getBody)
{
    /// <summary>The response context available in the script as <c>response</c>.</summary>
    public HttpTestResponseScriptContext response { get; } = new(statusCode, getBody);
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
///   executed as full C# scripts. <c>$request</c> and <c>$response</c> are
///   automatically rewritten to the <c>request</c> and <c>response</c> globals so
///   that the familiar shorthand syntax still works inside complex scripts.
///
///   preScript example:
///     var key = System.Environment.GetEnvironmentVariable("API_KEY");
///     request.headers.set("Authorization", $"Bearer {key}");
///
///   postScript example:
///     response.EnsureSuccessStatusCode();
///     var body = await response.GetBodyAsync();
///     response.Assert(body.Contains("token"), "Response must contain a token");
/// </remarks>
public static partial class HttpTestScriptRunner
{
    // -----------------------------------------------------------------------
    // Simple fast-path regex patterns
    // -----------------------------------------------------------------------

    [GeneratedRegex(
        @"^\$request\.headers\.(add|set)\(\s*""(?<name>[^""]+)""\s*,\s*""(?<value>[^""]*)""\s*\)\s*$",
        RegexOptions.IgnoreCase)]
    private static partial Regex RequestHeadersPattern { get; }

    [GeneratedRegex(@"^\$response\.EnsureSuccessStatusCode\(\)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex EnsureSuccessPattern { get; }

    [GeneratedRegex(@"^\$response\.StatusCode\s*(?<op>==|!=)\s*(?<code>\d{3})\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex StatusCodePattern { get; }

    [GeneratedRegex(@"^\$response\.Body\.Contains\(\s*""(?<text>[^""]*)""\s*\)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex BodyContainsPattern { get; }

    // -----------------------------------------------------------------------
    // -----------------------------------------------------------------------
    // Roslyn script options (shared / lazy-initialized)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Shared Roslyn <see cref="ScriptOptions"/> configured with common namespaces and a reference
    /// to the HTTPie assembly so that context types (e.g. <see cref="HttpTestAssertionException"/>)
    /// are available inside scripts. Lazy initialization defers the construction cost until the
    /// first complex script is actually executed.
    /// </summary>
    private static readonly Lazy<ScriptOptions> RoslynScriptOptions = new(() =>
        ScriptOptions.Default
            .AddImports(
                "System",
                "System.Collections.Generic",
                "System.Linq",
                "System.Text",
                "System.Text.Json",
                "System.Net.Http",
                "HTTPie.Implement")
            .AddReferences(typeof(HttpTestScriptRunner).Assembly));

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <summary>
    /// Executes a preScript, mutating the supplied <paramref name="headers"/> dictionary.
    /// Simple patterns are evaluated via fast-path regex; any unrecognized statement
    /// causes the entire script to be compiled and run via Roslyn CSharp scripting.
    /// </summary>
    public static async Task ExecutePreScriptAsync(string? script, Dictionary<string, string> headers)
    {
        if (string.IsNullOrWhiteSpace(script)) return;

        if (!HasComplexStatements(script, isPreScript: true))
        {
            ExecutePreScriptSimple(script, headers);
            return;
        }

        var processedScript = PreprocessScript(script);
        var globals = new HttpTestPreScriptGlobals(headers);
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
        int statusCode,
        Func<Task<string>> getResponseBody)
    {
        if (string.IsNullOrWhiteSpace(script)) return;

        if (!HasComplexStatements(script, isPreScript: false))
        {
            await ExecutePostScriptSimpleAsync(script, statusCode, getResponseBody);
            return;
        }

        var processedScript = PreprocessScript(script);
        var globals = new HttpTestPostScriptGlobals(statusCode, getResponseBody);
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

    private static void ExecutePreScriptSimple(string script, Dictionary<string, string> headers)
    {
        foreach (var rawLine in script.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("//") || line.StartsWith("#")) continue;

            var match = RequestHeadersPattern.Match(line);
            if (match.Success)
                headers[match.Groups["name"].Value] = match.Groups["value"].Value;
        }
    }

    private static async Task ExecutePostScriptSimpleAsync(
        string script, int statusCode, Func<Task<string>> getResponseBody)
    {
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
                cachedBody ??= await getResponseBody();
                var text = bodyMatch.Groups["text"].Value;
                if (!cachedBody.Contains(text, StringComparison.Ordinal))
                    throw new HttpTestAssertionException(
                        $"Body.Contains assertion failed: body does not contain \"{text}\".");
                continue;
            }
        }
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
    /// so that the shorthand syntax works in Roslyn-executed scripts too.
    /// </summary>
    private static string PreprocessScript(string script) =>
        script.Replace("$request", "request").Replace("$response", "response");
}

/// <summary>
/// Thrown when an HTTP test assertion (postScript) fails.
/// </summary>
public sealed class HttpTestAssertionException(string message) : Exception(message);

