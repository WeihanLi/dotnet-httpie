// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Models;

namespace HTTPie.Abstractions;

public interface IHttpParser
{
    string? Environment { get; set; }

    Task<HttpRequestMessage> ParseScriptAsync(string script, CancellationToken cancellationToken = default);

    IAsyncEnumerable<HttpRequestMessageWrapper> ParseFileAsync(string filePath,
        CancellationToken cancellationToken = default);
}
