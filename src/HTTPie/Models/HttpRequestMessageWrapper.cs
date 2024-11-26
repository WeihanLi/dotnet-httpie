// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

namespace HTTPie.Models;

public sealed class HttpRequestMessageWrapper(string name, HttpRequestMessage httpRequestMessage)
{
    public string Name { get; } = name;
    public HttpRequestMessage RequestMessage { get; } = httpRequestMessage;

    public static implicit operator HttpRequestMessage(HttpRequestMessageWrapper httpRequestMessageWrapper)
        => httpRequestMessageWrapper.RequestMessage;
}
