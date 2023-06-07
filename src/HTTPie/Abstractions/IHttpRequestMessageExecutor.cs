// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

namespace HTTPie.Abstractions;

public interface IRawHttpRequestExecutor
{
    Task<HttpResponseMessage> Execute(HttpRequestMessage httpRequestMessage, CancellationToken cancellationToken);
}
