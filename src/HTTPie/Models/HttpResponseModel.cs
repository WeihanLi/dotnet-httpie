// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Primitives;
using System.Net;

namespace HTTPie.Models;

public record HttpResponseModel
{
    public Version HttpVersion { get; set; } = new(1, 1);
    public HttpStatusCode StatusCode { get; set; }
    public Dictionary<string, StringValues> Headers { get; set; } = new();
    public string Body { get; set; } = string.Empty;
}
