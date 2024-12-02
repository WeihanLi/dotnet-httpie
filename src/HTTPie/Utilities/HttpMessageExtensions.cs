// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using System.Text;

namespace HTTPie.Utilities;

public static class HttpMessageExtensions
{
    public static async Task<string> ToRawMessageAsync(this HttpRequestMessage httpRequestMessage,
        CancellationToken cancellationToken = default)
    {
        var messageBuilder = new StringBuilder();

        var headLine =
            $"{httpRequestMessage.Method.Method} {httpRequestMessage.RequestUri?.AbsoluteUri} HTTP/{httpRequestMessage.Version:2}";
        messageBuilder.AppendLine(headLine);

        var headersDictionary = httpRequestMessage.Headers.ToDictionary(x => x.Key, x => x.Value.StringJoin(", "));
        if (httpRequestMessage.Content?.Headers != null)
        {
            foreach (var header in httpRequestMessage.Content.Headers)
            {
                headersDictionary.TryAdd(header.Key, header.Value.StringJoin(", "));
            }
        }

        foreach (var (name, value) in headersDictionary)
        {
            messageBuilder.AppendLine($"{name}: {value}");
        }

        if (httpRequestMessage.Content != null)
        {
            var body = await httpRequestMessage.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrEmpty(body))
            {
                messageBuilder.AppendLine();
                messageBuilder.AppendLine(body);
            }
        }

        return messageBuilder.ToString();
    }


    public static async Task<string> ToRawMessageAsync(this HttpResponseMessage httpResponseMessage,
        CancellationToken cancellationToken = default)
    {
        var messageBuilder = new StringBuilder();

        var headLine =
            $"HTTP/{httpResponseMessage.Version:2} {(int)httpResponseMessage.StatusCode} {httpResponseMessage.StatusCode}";
        messageBuilder.AppendLine(headLine);

        var headersDictionary = httpResponseMessage.Headers.ToDictionary(x => x.Key, x => x.Value.StringJoin(", "));
        if (httpResponseMessage is { Content.Headers: not null })
        {
            foreach (var header in httpResponseMessage.Content.Headers)
            {
                headersDictionary.TryAdd(header.Key, header.Value.StringJoin(", "));
            }
        }

        foreach (var (name, value) in headersDictionary.OrderBy(x => x.Key))
        {
            messageBuilder.AppendLine($"{name}: {value}");
        }

        var body = await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken);
        if (!string.IsNullOrEmpty(body))
        {
            messageBuilder.AppendLine();
            messageBuilder.AppendLine(body);
        }

        return messageBuilder.ToString();
    }
}
