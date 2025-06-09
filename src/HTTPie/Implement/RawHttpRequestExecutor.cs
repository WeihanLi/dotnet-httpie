// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;

namespace HTTPie.Implement;

public sealed class RawHttpRequestExecutor : IRawHttpRequestExecutor
{
    private readonly HttpClient _client = new(new HttpClientHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        UseDefaultCredentials = false
    });

    public async Task<HttpResponseMessage> ExecuteAsync(HttpRequestMessage httpRequestMessage,
        CancellationToken cancellationToken)
    {
        return await _client.SendAsync(httpRequestMessage, cancellationToken);
    }
}
