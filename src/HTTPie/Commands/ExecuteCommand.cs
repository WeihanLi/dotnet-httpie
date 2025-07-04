// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Implement;
using HTTPie.Utilities;
using Json.Path;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using WeihanLi.Common.Http;

namespace HTTPie.Commands;

public sealed class ExecuteCommand : Command
{
    private static readonly Argument<string> FilePathArgument = new("scriptPath")
    {
        Description = "The script to execute",
        Arity = ArgumentArity.ZeroOrOne
    };

    private static readonly Option<string> EnvironmentTypeOption =
        new("--env")
        {
            Description = "The environment to execute script"
        };

    private static readonly Option<ExecuteScriptType> ExecuteScriptTypeOption =
        new("-t", "--type")
        {
            Description = "The script type to execute"
        };

    public ExecuteCommand() : base("exec", "execute http request")
    {
        Options.Add(ExecuteScriptTypeOption);
        Options.Add(EnvironmentTypeOption);
        Arguments.Add(FilePathArgument);
    }

    public async Task InvokeAsync(
        ParseResult parseResult, CancellationToken cancellationToken, IServiceProvider serviceProvider
        )
    {
        var scriptText = string.Empty;
        var filePath = parseResult.GetValue(FilePathArgument);
        if (string.IsNullOrEmpty(filePath))
        {
            // try to read script content from stdin
            if (Console.IsInputRedirected && Console.In.Peek() != -1)
            {
                scriptText = (await Console.In.ReadToEndAsync(cancellationToken)).Trim();
            }

            if (string.IsNullOrEmpty(scriptText))
            {
                throw new InvalidOperationException("Invalid script to execute");
            }
        }
        else
        {
            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException("Invalid filePath");
            }
        }

        var logger = serviceProvider.GetRequiredService<ILogger>();
        var requestExecutor = serviceProvider.GetRequiredService<IRawHttpRequestExecutor>();
        var type = parseResult.GetValue(ExecuteScriptTypeOption);
        var environment = parseResult.GetValue(EnvironmentTypeOption);
        var parser = type switch
        {
            ExecuteScriptType.Http => serviceProvider.GetRequiredService<IHttpParser>(),
            ExecuteScriptType.Curl => serviceProvider.GetRequiredService<ICurlParser>(),
            _ => throw new InvalidOperationException($"Not supported request type: {type}")
        };
        parser.Environment = environment;
        logger.LogDebug("Executing {ScriptType} http request {ScriptPath} with {ScriptExecutor} with environment {Environment}",
            type, filePath, parser.GetType().Name, environment);
        
        var offline = parseResult.GetValue(OutputFormatter.OfflineOption);
        await InvokeRequest(parser, requestExecutor, scriptText, filePath, offline, cancellationToken);
    }

    private static async Task InvokeRequest(
        IHttpParser httpParser, IRawHttpRequestExecutor requestExecutor, string scriptText,
        string? filePath, bool offline, CancellationToken cancellationToken)
    {
        var responseList = new Dictionary<string, HttpResponseMessage>();
        
        try
        {
            var getRequests = string.IsNullOrEmpty(filePath) 
                ? httpParser.ParseScriptAsync(scriptText, cancellationToken)
                : httpParser.ParseFileAsync(filePath, cancellationToken)
                ;
            await foreach (var request in getRequests.WithCancellation(cancellationToken))
            {
                await EnsureRequestVariableReferenceReplaced(request, responseList);
                var response = await ExecuteRequest(
                    requestExecutor, request.RequestMessage, offline, cancellationToken, request.Name
                    );
                if (response is null)
                    continue;

                responseList[request.Name] = response;
            }
        }
        finally
        {
            foreach (var responseMessage in responseList.Values)
            {
                try
                {
                    responseMessage.Dispose();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                }
            }

            responseList.Clear();
        }
    }

    private static async Task<HttpResponseMessage?> ExecuteRequest(
        IRawHttpRequestExecutor requestExecutor,
        HttpRequestMessage requestMessage,
        bool offline,
        CancellationToken cancellationToken,
        string? requestName = null)
    {
        requestMessage.TryAddHeaderIfNotExists(HttpHeaderNames.UserAgent, Constants.DefaultUserAgent);
        ConsoleHelper.WriteLineIf(requestName!, !string.IsNullOrEmpty(requestName));

        Console.WriteLine("Request message:");
        Console.WriteLine(await requestMessage.ToRawMessageAsync(cancellationToken));
        if (offline)
            return null;
        
        var startTimestamp = Stopwatch.GetTimestamp();
        var response = await requestExecutor.ExecuteAsync(requestMessage, cancellationToken);
        var requestDuration = ProfilerHelper.GetElapsedTime(startTimestamp);
        Console.WriteLine($"Response message({requestDuration.TotalMilliseconds}ms):");
        Console.WriteLine(await response.ToRawMessageAsync(cancellationToken));
        Console.WriteLine();

        return response;
    }

    // use source generated regex when removing net8.0
    private static readonly Regex RequestVariableNameReferenceRegex =
        new(@"\{\{(?<requestName>\s?[a-zA-Z_]\w*)\.(request|response)\.(headers|body).*\s?\}\}",
            RegexOptions.Compiled);

    private static async Task EnsureRequestVariableReferenceReplaced(HttpRequestMessage requestMessage,
        Dictionary<string, HttpResponseMessage> requests)
    {
        var requestHeaders = requestMessage.Headers.ToArray();
        foreach (var (headerName, headerValue) in requestHeaders)
        {
            var headerValueString = headerValue.StringJoin(",");
            var headerValueChanged = false;
            var match = RequestVariableNameReferenceRegex.Match(headerValueString);
            while (match.Success)
            {
                var requestVariableValue = await GetRequestVariableValue(match, requests);
                headerValueString = headerValueString.Replace(match.Value, requestVariableValue);
                headerValueChanged = true;
                match = RequestVariableNameReferenceRegex.Match(headerValueString);
            }

            if (headerValueChanged)
            {
                requestMessage.Headers.Remove(headerName);
                requestMessage.Headers.TryAddWithoutValidation(headerName, headerValueString);
            }
        }

        if (requestMessage.Content != null)
        {
            // request content headers
            {
                requestHeaders = requestMessage.Content.Headers.ToArray();
                foreach (var (headerName, headerValue) in requestHeaders)
                {
                    var headerValueString = headerValue.StringJoin(",");
                    var headerValueChanged = false;
                    var match = RequestVariableNameReferenceRegex.Match(headerValueString);
                    while (match.Success)
                    {
                        var requestVariableValue = await GetRequestVariableValue(match, requests);
                        headerValueString = headerValueString.Replace(match.Value, requestVariableValue);

                        headerValueChanged = true;
                        match = RequestVariableNameReferenceRegex.Match(headerValueString);
                    }

                    if (headerValueChanged)
                    {
                        requestMessage.Content.Headers.Remove(headerName);
                        requestMessage.Content.Headers.TryAddWithoutValidation(headerName, headerValueString);
                    }
                }
            }

            // request body
            {
                if (requestMessage.Content is StringContent stringContent)
                {
                    var requestBody = await requestMessage.Content.ReadAsStringAsync();
                    var normalizedRequestBody = requestBody;
                    var requestBodyChanged = false;

                    if (!string.IsNullOrEmpty(requestBody))
                    {
                        var match = RequestVariableNameReferenceRegex.Match(normalizedRequestBody);
                        while (match.Success)
                        {
                            var requestVariableValue = await GetRequestVariableValue(match, requests);
                            normalizedRequestBody = normalizedRequestBody.Replace(match.Value, requestVariableValue);
                            requestBodyChanged = true;
                            match = RequestVariableNameReferenceRegex.Match(normalizedRequestBody);
                        }

                        if (requestBodyChanged)
                        {
                            requestMessage.Content = new StringContent(normalizedRequestBody, Encoding.UTF8,
                                stringContent.Headers.ContentType?.MediaType ?? "application/json");
                        }
                    }
                }
            }
        }
    }

    private static async Task<string> GetRequestVariableValue(Match match,
        Dictionary<string, HttpResponseMessage> responseMessages)
    {
        var matchedText = match.Value;
        var requestName = match.Groups["requestName"].Value;
        if (responseMessages.TryGetValue(requestName, out var responseMessage))
        {
            // {{requestName.(response|request).(body|headers).(*|JSONPath|XPath|Header Name)}}
            var splits = matchedText.Split('.', 4);
            Debug.Assert(splits.Length is 4 or 3);
            if (splits.Length != 3 && splits.Length != 4) return string.Empty;
            if (splits.Length == 4 && splits[3].EndsWith("}}"))
            {
                splits[3] = splits[3][..^2];
            }

            switch (splits[2])
            {
                case "headers":
                    return splits[1] switch
                    {
                        "request" => responseMessage.RequestMessage?.Headers.TryGetValues(splits[3],
                            out var requestHeaderValue) == true
                            ? requestHeaderValue.StringJoin(",")
                            : string.Empty,
                        "response" => responseMessage.Headers.TryGetValues(splits[3], out var responseHeaderValue)
                            ? responseHeaderValue.StringJoin(",")
                            : string.Empty,
                        _ => string.Empty
                    };
                case "body":
                    // TODO: consider cache the body in case of reading the body multi times
                    var getBodyTask = splits[1] switch
                    {
                        "request" => responseMessage.RequestMessage?.Content?.ReadAsStringAsync() ??
                                     Task.FromResult(string.Empty),
                        "response" => responseMessage.Content.ReadAsStringAsync(),
                        _ => Task.FromResult(string.Empty)
                    };
                    var body = await getBodyTask;
                    if (splits.Length == 3 || string.IsNullOrEmpty(splits[3]))
                    {
                        return body;
                    }

                    if (JsonPath.TryParse(splits[3], out var jsonPath))
                    {
                        try
                        {
                            var jsonNode = JsonNode.Parse(body);
                            var pathResult = jsonPath.Evaluate(jsonNode);
                            return pathResult.Matches.FirstOrDefault()?.Value?.ToString() ?? string.Empty;
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e);
                        }
                    }

                    break;
            }
        }

        return string.Empty;
    }
}

public enum ExecuteScriptType
{
    Http = 0,
    Curl = 1,
    // Har = 2,
}
