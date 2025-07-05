// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using System.Runtime.CompilerServices;
using CommandLineParser = WeihanLi.Common.Helpers.CommandLineParser;

namespace HTTPie.Implement;

public sealed class CurlParser : AbstractHttpRequestParser, ICurlParser
{
    public override async IAsyncEnumerable<HttpRequestMessageWrapper> ParseScriptAsync
        (string script, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Guard.NotNullOrEmpty(script);
        await foreach (var request in ParseHttpRequestsAsync(
                           script.Split("\n###\n").ToAsyncEnumerable(), null,
                           cancellationToken))
        {
            yield return request;
        }
    }

    public override async IAsyncEnumerable<HttpRequestMessageWrapper> ParseFileAsync(
        string filePath, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Guard.NotNullOrEmpty(filePath);
        var script = await File.ReadAllTextAsync(filePath, cancellationToken);
        await foreach (var request in ParseHttpRequestsAsync(
                           script.Split("\n###\n").ToAsyncEnumerable(), filePath,
                           cancellationToken))
        {
            yield return request;
        }
    }

    protected override async IAsyncEnumerable<HttpRequestMessageWrapper> ParseHttpRequestsAsync
        (
            IAsyncEnumerable<string> chunks,
            string? filePath, 
            [EnumeratorCancellation]CancellationToken cancellationToken
        )
    {
        var index = 0;
        await foreach (var chunk in chunks.WithCancellation(cancellationToken))
        {
            var request = ParseCurlScript(chunk, cancellationToken);
            var requestName = $"request#{index}";
            index++;
            yield return new HttpRequestMessageWrapper(requestName, request);
        }
    }
    
    private static HttpRequestMessage ParseCurlScript(
        string curlScript, CancellationToken cancellationToken = default
        )
    {
        Guard.NotNullOrEmpty(curlScript);
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedScript = curlScript
            .Replace("\\\n", " ")
            .Replace("\\\r\n", " ")
            .Replace("\r\n", " ")
            .Replace("\n ", " ")
            .Trim();
        if (!normalizedScript.StartsWith("curl "))
        {
            throw new ArgumentException($"Invalid curl script: {curlScript}", nameof(curlScript));
        }

        var splits = CommandLineParser.ParseLine(normalizedScript)
            .Where(s=> !string.IsNullOrEmpty(s))
            .ToArray();
        string requestMethod = string.Empty, requestBody = string.Empty;
        Uri? uri = null;
        var headers = new List<KeyValuePair<string, string>>();
        for (var i = 1; i < splits.Length; i++)
        {
            var item = splits[i].Trim('\'', '"');

            // url
            if (uri is null && Uri.TryCreate(item, UriKind.Absolute, out uri))
            {
                continue;
            }

            // Get Request method
            if (item is "-X")
            {
                i++;

                if (i < splits.Length)
                {
                    item = splits[i].Trim('\'', '"');
                    if (Helpers.HttpMethods.Contains(item))
                        requestMethod = item;
                }

                continue;
            }

            // request body
            if (item is "-d")
            {
                i++;
                if (i < splits.Length)
                {
                    item = splits[i].Trim('\'', '"');
                    requestBody = item;
                }

                continue;
            }

            // headers
            if (item is "-H")
            {
                i++;
                if (i < splits.Length)
                {
                    var header = splits[i].Trim('\'', '"');
                    var headerSplits = header.Split(':', 2, StringSplitOptions.TrimEntries);
                    if (headerSplits.Length == 2)
                    {
                        headers.Add(new KeyValuePair<string, string>(headerSplits[0], headerSplits[1].Trim()));   
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(requestMethod))
        {
            requestMethod =  requestBody.IsNullOrEmpty() ? HttpMethod.Get.Method : HttpMethod.Post.Method;
        }

        if (uri is null) throw new ArgumentException("Url info not found");

        var request = new HttpRequestMessage(new HttpMethod(requestMethod), uri);
        // request body
        if (!string.IsNullOrEmpty(requestBody))
        {
            request.Content = new StringContent(requestBody);
        }

        // headers
        foreach (var headerGroup in headers.GroupBy(x => x.Key))
        {
            request.TryAddHeader(
                headerGroup.Key,
                headerGroup.Select(x => x.Value).StringJoin(",")
            );
        }

        return request;
    }
}
