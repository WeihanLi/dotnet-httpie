// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace HTTPie.Utilities;

public interface IHttpParser
{
    IAsyncEnumerable<HttpRequestMessage> ParseAsync(string filePath);
}

public sealed class HttpParser : IHttpParser
{
    public async IAsyncEnumerable<HttpRequestMessage> ParseAsync(string filePath)
    {
        using var reader = File.OpenText(filePath);
        HttpRequestMessage? requestMessage = null;
        StringBuilder? requestBodyBuilder = null;
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (line.IsNullOrWhiteSpace()) continue;
            if ("###" == line || line.StartsWith("### "))
            {
                if (requestMessage != null)
                {
                    yield return requestMessage;
                    requestMessage = null;
                    requestBodyBuilder = null;
                }
            }

            if (line.StartsWith("#") || line.StartsWith("//")) continue;
            //
            if (requestMessage is null)
            {
                var splits = line.Split(' ');
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
                var headerSplits = line.Split(':', 2);
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
                        requestBodyBuilder.AppendLine(line);
                    }
                }
                else
                {
                    requestBodyBuilder.AppendLine(line);
                }
            }
        }

        if (requestMessage == null) yield break;

        if (requestBodyBuilder is { Length: > 0 })
        {
            var contentHeaders = requestMessage.Content?.Headers;
            if (contentHeaders is { ContentType: null })
            {
                contentHeaders.ContentType = MediaTypeHeaderValue.Parse(HttpHelper.JsonContentType);
            }

            requestMessage.Content = new StringContent(requestBodyBuilder.ToString(), Encoding.UTF8,
                contentHeaders?.ContentType?.MediaType ?? "application/json");
            if (contentHeaders != null)
            {
                foreach (var header in contentHeaders)
                {
                    requestMessage.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
        }

        yield return requestMessage;
    }
}
