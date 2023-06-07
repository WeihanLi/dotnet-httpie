// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

namespace HTTPie.Models;

public sealed class HttpRequestMessageWrapper
{
    public string Name { get; }
    public HttpRequestMessage RequestMessage { get; }

    public HttpRequestMessageWrapper(string name, HttpRequestMessage httpRequestMessage)
    {
        Name = name;
        RequestMessage = httpRequestMessage;
    }

    public static implicit operator HttpRequestMessage(HttpRequestMessageWrapper httpRequestMessageWrapper)
        => httpRequestMessageWrapper.RequestMessage;
}
