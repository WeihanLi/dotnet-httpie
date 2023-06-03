// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using System.Collections.Concurrent;

namespace HTTPie.Implement;

public sealed class HttpRequestMessageExecutor : IHttpRequestMessageExecutor
{
    private readonly Func<HttpClientHandler, Task> _httpHandlerPipeline;
    private readonly ConcurrentDictionary<byte, HttpClient> _httpClients = new();

    public HttpRequestMessageExecutor(
        Func<HttpClientHandler, Task> httpHandlerPipeline
    )
    {
        _httpHandlerPipeline = httpHandlerPipeline;
    }

    public async Task<HttpResponseMessage> Execute(HttpRequestMessage httpRequestMessage,
        CancellationToken cancellationToken)
    {
        var httpClientHandler = new HttpClientHandler { AllowAutoRedirect = false };
        await _httpHandlerPipeline(httpClientHandler);
        var client = _httpClients.GetOrAdd(0, _ => new HttpClient(httpClientHandler));
        return await client.SendAsync(httpRequestMessage, cancellationToken);
    }
}
