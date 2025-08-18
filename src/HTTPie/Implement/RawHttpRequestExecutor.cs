// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Utilities;

namespace HTTPie.Implement;

public sealed class RawHttpRequestExecutor : IRawHttpRequestExecutor
{
    private readonly HttpClient _client = new(Helpers.GetHttpClientHandler());

    public async Task<HttpResponseMessage> ExecuteAsync(HttpRequestMessage httpRequestMessage,
        CancellationToken cancellationToken)
    {
        return await _client.SendAsync(httpRequestMessage, cancellationToken);
    }
}
