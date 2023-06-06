// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Utilities;
using Json.Path;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace HTTPie.Commands;

public sealed class ExecuteCommand : Command
{
    private static readonly Argument<string> FilePathArgument = new("path", "The script path to execute");

    private static readonly Option<ExecuteScriptType> ExecuteScriptTypeOption =
        new("--type", "The script type to execute");

    public ExecuteCommand() : base("execute", "execute http request related scripts")
    {
        AddArgument(FilePathArgument);
    }

    public async Task InvokeAsync(InvocationContext invocationContext, IServiceProvider serviceProvider)
    {
        var filePath = invocationContext.ParseResult.GetValueForArgument(FilePathArgument);
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            throw new InvalidOperationException("Invalid filePath");
        }

        var httpParser = serviceProvider.GetRequiredService<IHttpParser>();
        var httpExecutor = serviceProvider.GetRequiredService<IRawHttpRequestExecutor>();
        var requests = new Dictionary<string, HttpResponseMessage>();
        try
        {
            await foreach (var request in httpParser.ParseAsync(filePath))
            {
                await EnsureRequestVariableReferenceReplaced(request, requests);
                if (!request.RequestMessage.Headers.TryGetValues("User-Agent", out _))
                {
                    request.RequestMessage.Headers.TryAddWithoutValidation("User-Agent", Constants.DefaultUserAgent);
                }

                Console.WriteLine(request.Name);
                Console.WriteLine("Request message:");
                Console.WriteLine(await request.RequestMessage.ToRawMessageAsync());
                var response = await httpExecutor.Execute(request, invocationContext.GetCancellationToken());
                Console.WriteLine("Response message:");
                Console.WriteLine(await response.ToRawMessageAsync());
                Console.WriteLine();
                requests[request.Name] = response;
            }
        }
        finally
        {
            foreach (var responseMessage in requests.Values)
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

            requests.Clear();
        }
    }


    private static readonly Regex RequestVariableNameReferenceRegex =
        new(@"\{\{(?<requestName>\s?[a-zA-Z_]\w*)\.(request|response)\.(headers|body).*\s?\}\}",
            RegexOptions.Compiled);

    private async Task EnsureRequestVariableReferenceReplaced(HttpRequestMessage requestMessage,
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


    private async Task<string> GetRequestVariableValue(Match match,
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
                            return pathResult.Matches?.FirstOrDefault()?.Value?.ToString() ?? string.Empty;
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
    Http,
    // Curl,
    // Har,
}
