// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using WeihanLi.Common.Http;

namespace HTTPie.Implement;

public sealed class HttpParser : IHttpParser
{
    private const string DotEnvFileName = ".env";
    private const string HttpEnvFileName = "httpenv.json";
    private const string UserHttpEnvFileName = "httpenv.json.user";

    public Task<HttpRequestMessage> ParseScriptAsync(string script, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async IAsyncEnumerable<HttpRequestMessageWrapper> ParseFileAsync(string filePath,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var fileScopedVariables = new Dictionary<string, string>();
        var fileScopedVariablesEnded = false;

        using var reader = File.OpenText(filePath);
        HttpRequestMessage? requestMessage = null;
        string? requestName = null;
        var requestNumber = 0;
        StringBuilder? requestBodyBuilder = null;
        Dictionary<string, string>? requestVariables = null;

        while (!reader.EndOfStream)
        {
#if NET7_0_OR_GREATER
            var line = await reader.ReadLineAsync(cancellationToken);
#else
            var line = await reader.ReadLineAsync();
#endif
            if (line.IsNullOrWhiteSpace()) continue;
            // variable definition handling
            if (line.StartsWith("@"))
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
                    requestVariables ??= new();
                    requestVariables[variableName] = variableValue;
                }
                else
                {
                    fileScopedVariables[variableName] = variableValue;
                }

                continue;
            }

            // request end
            if ("###" == line || line.StartsWith("### "))
            {
                fileScopedVariablesEnded = true;
                if (requestMessage != null)
                {
                    PreReturnRequest();
                    yield return new HttpRequestMessageWrapper(requestName!, requestMessage);
                    requestMessage = null;
                    requestBodyBuilder = null;
                    requestVariables = null;
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
                    requestName = line["# @name ".Length..].TrimStart(new[] { '=' }).Trim();
                    fileScopedVariablesEnded = true;
                }

                continue;
            }

            //
            var normalizedLine = EnsureVariableReplaced(line, requestVariables, fileScopedVariables);
            if (requestMessage is null)
            {
                var splits = normalizedLine.Split(' ');
                Debug.Assert(splits.Length > 1, "splits.Length > 1");
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
                            requestMessage.Content ??= new ByteArrayContent(Array.Empty<byte>());
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

    internal static string EnsureVariableReplaced(
        string rawText,
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

            match = VariableNameReferenceRegex.Match(textReplaced);
        }

        // env name replacement
        match = EnvNameReferenceRegex.Match(textReplaced);
        while (match.Success)
        {
            var variableName = match.Groups["variableName"].Value;
            var variableValue = Environment.GetEnvironmentVariable(variableName);
            textReplaced = textReplaced.Replace(match.Value, variableValue ?? string.Empty);
            match = EnvNameReferenceRegex.Match(textReplaced);
        }

        return textReplaced;
    }
}
