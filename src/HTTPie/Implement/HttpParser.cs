// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using WeihanLi.Common.Http;

namespace HTTPie.Implement;

public abstract class AbstractHttpRequestParser : IHttpParser
{
    public string? Environment { get; set; }

    public virtual IAsyncEnumerable<HttpRequestMessageWrapper> ParseScriptAsync(string script, CancellationToken cancellationToken = default)
    {
        var lines = script.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return ParseHttpRequestsAsync(lines.ToAsyncEnumerable(), null, cancellationToken);
    }

    public virtual IAsyncEnumerable<HttpRequestMessageWrapper> ParseFileAsync(
        string filePath, CancellationToken cancellationToken = default
    ) =>
        ParseHttpRequestsAsync(
            File.ReadLinesAsync(filePath, cancellationToken),
            filePath,
            cancellationToken
        );

    protected abstract IAsyncEnumerable<HttpRequestMessageWrapper> ParseHttpRequestsAsync(
        IAsyncEnumerable<string> chunks,
        string? filePath,
        CancellationToken cancellationToken
    );
}

public sealed partial class HttpParser(ILogger logger) : AbstractHttpRequestParser
{
#if NET8_0
    private readonly ILogger _logger = logger;
#endif

    private const string DotEnvFileName = ".env";
    private const string HttpEnvFileName = "httpenv.json";
    private const string UserHttpEnvFileName = "httpenv.json.user";
    private const string HttpClientPublicEnvFileName = "http-client.env.json";
    private const string HttpClientPrivateEnvFileName = "http-client.private.env.json";
    private const int MaxRecursionDepth = 32;

    private static readonly Dictionary<string, Func<string[], string>> BuiltInFunctions;

    static HttpParser()
    {
        BuiltInFunctions = new Dictionary<string, Func<string[], string>>
        {
            { "guid", static _ => Guid.NewGuid().ToString() },
            {
                "randomInt", static input =>
                {
                    if (input.Length == 2 && int.TryParse(input[0], out var min) && int.TryParse(input[1], out var max))
                    {
                        return Random.Shared.Next(min, max).ToString();
                    }

                    if (input.Length == 1 && int.TryParse(input[0], out max))
                    {
                        return Random.Shared.Next(max).ToString();
                    }

                    return Random.Shared.Next(10_000).ToString(CultureInfo.InvariantCulture);
                }
            },
            {
                "datetime", static input =>
                {
                    if (input.Length is 1)
                    {
                        return DateTimeOffset.Now.ToString(input[0], CultureInfo.InvariantCulture);
                    }

                    return DateTimeOffset.Now.ToString(CultureInfo.InvariantCulture);
                }
            },
            {
                "timestamp", static input =>
                {
                    if (input.Length is 1)
                    {
                        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(input[0], CultureInfo.InvariantCulture);
                    }

                    return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
                }
            }
        };
    }

    protected override async IAsyncEnumerable<HttpRequestMessageWrapper> ParseHttpRequestsAsync(
        IAsyncEnumerable<string> fileLines,
        string? filePath,
        [EnumeratorCancellation] CancellationToken cancellationToken
        )
    {
        var dotEnvVariables = new Dictionary<string, string>();
        var fileScopedVariables = new Dictionary<string, string>();

        var dir = filePath is null ? Directory.GetCurrentDirectory() : Path.GetDirectoryName(Path.GetFullPath(filePath));
        // Load environment variables from .env file
        await LoadEnvVariables(DotEnvFileName, dir, dotEnvVariables);
        if (!string.IsNullOrEmpty(Environment))
        {
            // Load environment variables from http-client.env.json file
            await LoadJsonEnvVariables(HttpClientPublicEnvFileName, dir, Environment, fileScopedVariables);
            // Load environment variables from http-client.private.env.json file
            await LoadJsonEnvVariables(HttpClientPrivateEnvFileName, dir, Environment, fileScopedVariables);

            // Load environment variables from httpenv.json file
            await LoadJsonEnvVariables(HttpEnvFileName, dir, Environment, fileScopedVariables);
            // Load environment variables from httpenv.json.user file
            await LoadJsonEnvVariables(UserHttpEnvFileName, dir, Environment, fileScopedVariables);
        }

        var fileScopedVariablesEnded = false;

        HttpRequestMessage? requestMessage = null;
        string? requestName = null;
        var requestNumber = 0;
        StringBuilder? requestBodyBuilder = null;
        Dictionary<string, string> requestVariables = new();

        // CA2024: Do not use StreamReader.EndOfStream in async methods
        // https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2024
        await foreach (var line in fileLines.WithCancellation(cancellationToken))
        {
            if (line.IsNullOrWhiteSpace()) continue;

            // variable definition handling
            if (line.StartsWith('@'))
            {
                var splits = line[1..].Split('=', 2, StringSplitOptions.TrimEntries);
                Debug.Assert(splits.Length == 2, "Invalid variable");
                if (splits.Length != 2)
                {
                    continue;
                }

                var (variableName, variableValue) = (splits[0], splits[1]);
                if (variableValue.Length >= 1 && ((variableValue[0] == '"' && variableValue[^1] == '"') || (variableValue[0] == '\'' && variableValue[^1] == '\'')))
                {
                    variableValue = variableValue.Length > 2 ? variableValue[1..^2] : string.Empty;
                }
                if (fileScopedVariablesEnded)
                {
                    requestVariables.Clear();
                    requestVariables[variableName] = variableValue;
                }
                else
                {
                    fileScopedVariables[variableName] = variableValue;
                }

                continue;
            }

            // request end
            if ("###" == line || line.StartsWith("### ", StringComparison.Ordinal))
            {
                fileScopedVariablesEnded = true;
                if (requestMessage != null)
                {
                    PreReturnRequest();
                    yield return new HttpRequestMessageWrapper(requestName!, requestMessage);
                    requestMessage = null;
                    requestBodyBuilder = null;
                    requestVariables.Clear();
                    requestName = null;
                }

                continue;
            }

            if (line.StartsWith("#") || line.StartsWith("//"))
            {
                if (line.StartsWith("# @name ")
                    || line.StartsWith("# @name=")
                    || line.StartsWith("// @name ")
                    || line.StartsWith("// @name=")
                   )
                {
                    requestName = line["# @name ".Length..].TrimStart(['=']).Trim();
                    fileScopedVariablesEnded = true;
                }

                continue;
            }

            //
            var normalizedLine = EnsureVariableReplaced(line, dotEnvVariables, requestVariables, fileScopedVariables);
            if (requestMessage is null)
            {
                var splits = normalizedLine.Split(' ');
                Debug.Assert(splits.Length > 1, "The normalized line must contain at least two parts separated by spaces: the HTTP method and the URL.");
                if (Helpers.HttpMethods.Contains(splits[0]))
                {
                    requestMessage = new HttpRequestMessage(new HttpMethod(splits[0].ToUpper()), splits[1]);
                    if (splits.Length == 3)
                    {
                        var httpVersion = splits[2].TrimStart("HTTP/");
                        if (Version.TryParse(httpVersion, out var version))
                        {
                            requestMessage.Version = version;
                        }
                    }
                }
            }
            else
            {
                var headerSplits = normalizedLine.Split(':', 2);
                if (requestBodyBuilder is null)
                {
                    if (headerSplits.Length == 2 && Regex.IsMatch(headerSplits[0], Constants.ParamNameRegex))
                    {
                        var (headerName, headerValue) = (headerSplits[0], headerSplits[1]);
                        if (HttpHelper.IsWellKnownContentHeader(headerName))
                        {
                            requestMessage.Content ??= new ByteArrayContent([]);
                            requestMessage.Content.Headers.TryAddWithoutValidation(headerName, headerValue);
                        }
                        else
                        {
                            requestMessage.Headers.TryAddWithoutValidation(headerName, headerValue);
                        }
                    }
                    else
                    {
                        requestBodyBuilder = new StringBuilder();
                        requestBodyBuilder.AppendLine(normalizedLine);
                    }
                }
                else
                {
                    requestBodyBuilder.AppendLine(normalizedLine);
                }
            }
        }

        if (requestMessage == null) yield break;

        PreReturnRequest();
        yield return new HttpRequestMessageWrapper(requestName!, requestMessage);

        void PreReturnRequest()
        {
            // attach request body and copy request headers
            if (requestBodyBuilder is { Length: > 0 })
            {
                var contentHeaders = requestMessage.Content?.Headers;
                requestMessage.Content = new StringContent(requestBodyBuilder.ToString(), Encoding.UTF8,
                        requestMessage.Content?.Headers.ContentType?.MediaType ?? HttpHelper.ApplicationJsonMediaType
                    );
                if (contentHeaders != null)
                {
                    foreach (var header in contentHeaders)
                    {
                        if (header.Key.EqualsIgnoreCase(HttpHeaderNames.ContentType))
                        {
                            requestMessage.Content.Headers.Remove(HttpHeaderNames.ContentType);
                        }

                        requestMessage.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
            }
            else
            {
                requestMessage.Content = null;
            }

            requestNumber++;
            requestName ??= $"request#{requestNumber}";
        }
    }

    private static readonly Regex VariableNameReferenceRegex =
        new(@"\{\{(?<variableName>\s?[a-zA-Z_][\w\.:]*\s?)\}\}", RegexOptions.Compiled);
    private static readonly Regex EnvNameReferenceRegex =
        new(@"\{\{(\$processEnv|\$env)\s+(?<variableName>\s?[a-zA-Z_][\w\.:]*\s?)\}\}", RegexOptions.Compiled);
    private static readonly Regex DotEnvNameReferenceRegex =
        new(@"\{\{(\$dotenv)\s+(?<variableName>\s?[a-zA-Z_][\w\.:]*\s?)\}\}", RegexOptions.Compiled);
    private static readonly Regex CustomFunctionReferenceRegex =
        new(@"\{\{\$(?<variableName>\s?[a-zA-Z_][\w\.:\s]*\s?)\}\}", RegexOptions.Compiled);

    private async Task LoadEnvVariables(string fileName, string? dir, Dictionary<string, string> variables)
    {
        var filePath = GetFilePath(fileName, dir);
        if (filePath is null) return;

        LogDebugLoadEnvVariablesFromFile(filePath);

        await foreach (var line in File.ReadLinesAsync(filePath))
        {
            if (line.IsNullOrWhiteSpace()
                || line.StartsWith('#')
                )
            {
                continue;
            }

            var splits = line.Split('=', 2, StringSplitOptions.TrimEntries);
            if (splits.Length != 2) continue;

            var (variableName, variableValue) = (splits[0], splits[1]);
            variables[variableName] = variableValue;
        }
    }

    private async Task LoadJsonEnvVariables(string fileName, string? dir, string environmentName, Dictionary<string, string> variables)
    {
        var filePath = GetFilePath(fileName, dir);
        if (filePath is null) return;

        await using var jsonContentStream = File.OpenRead(filePath);
        var jsonNode = await JsonNode.ParseAsync(jsonContentStream);
        if (jsonNode is null) return;

        // load environment shared variables
        LogDebugLoadVariablesFromEnvFile(filePath);

        var sharedVariables = jsonNode["$shared"]?.AsObject();
        if (sharedVariables is not null)
        {
            foreach (var variable in sharedVariables)
            {
                if (variable.Value?.GetValueKind() == JsonValueKind.String)
                {
                    variables[variable.Key] = variable.Value.GetValue<string>();
                }
            }
        }

        // load environment specific variables
        var environmentSpecificVariables = jsonNode[environmentName]?.AsObject();
        if (environmentSpecificVariables is not null)
        {
            foreach (var variable in environmentSpecificVariables)
            {
                if (variable.Value?.GetValueKind() == JsonValueKind.String)
                {
                    variables[variable.Key] = variable.Value.GetValue<string>();
                }
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Load variables from env file {FileName}")]
    private partial void LogDebugLoadVariablesFromEnvFile(string fileName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Load environment variables from {FileName}")]
    private partial void LogDebugLoadEnvVariablesFromFile(string fileName);

    private static string? GetFilePath(string fileName, string? dir = null, int depth = 0)
    {
        dir ??= Directory.GetCurrentDirectory();

        var path = Path.Combine(dir, fileName);
        if (File.Exists(path))
        {
            return path;
        }

        var parentDir = Directory.GetParent(dir);
        if (parentDir is not null && depth <= MaxRecursionDepth)
        {
            return GetFilePath(fileName, parentDir.FullName, depth + 1);
        }

        return null;
    }

    internal static string EnsureVariableReplaced(
        string rawText,
        Dictionary<string, string> dotEnvVariables,
        params Dictionary<string, string>?[] variables
    )
    {
        if (string.IsNullOrEmpty(rawText)) return rawText;

        var textReplaced = rawText;

        // variable name replacement
        var match = VariableNameReferenceRegex.Match(textReplaced);
        while (match.Success)
        {
            var variableName = match.Groups["variableName"].Value;
            foreach (var variable in variables)
            {
                if (variable?.TryGetValue(variableName, out var variableValue) == true)
                {
                    textReplaced = textReplaced.Replace(match.Value, variableValue);
                    break;
                }
            }

            textReplaced = textReplaced.Replace(match.Value, string.Empty);
            match = VariableNameReferenceRegex.Match(textReplaced);
        }

        // dotenv name replacement
        match = DotEnvNameReferenceRegex.Match(textReplaced);
        while (match.Success)
        {
            var variableName = match.Groups["variableName"].Value;
            dotEnvVariables.TryGetValue(variableName, out var variableValue);
            textReplaced = textReplaced.Replace(match.Value, variableValue ?? string.Empty);
            match = EnvNameReferenceRegex.Match(textReplaced);
        }

        // env name replacement
        match = EnvNameReferenceRegex.Match(textReplaced);
        while (match.Success)
        {
            var variableName = match.Groups["variableName"].Value;
            var variableValue = System.Environment.GetEnvironmentVariable(variableName);
            textReplaced = textReplaced.Replace(match.Value, variableValue ?? string.Empty);
            match = EnvNameReferenceRegex.Match(textReplaced);
        }

        // custom functions
        match = CustomFunctionReferenceRegex.Match(textReplaced);
        while (match.Success)
        {
            var functionName = match.Groups["variableName"].Value;
            var split = functionName.Split(' ');
            string? value = null;
            if (BuiltInFunctions.TryGetValue(split[0], out var function))
            {
                value = function.Invoke(split[1..]);
            }
            else
            {
                ConsoleHelper.WriteLineWithColor($"{match.Value} is not supported, will be ignored", ConsoleColor.DarkYellow);
            }
            textReplaced = textReplaced.Replace(match.Value, value ?? string.Empty);
            match = EnvNameReferenceRegex.Match(textReplaced);
        }

        return textReplaced;
    }
}
