// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Primitives;
using System.Net;

namespace HTTPie.Models;

public sealed class HttpResponseModel
{
    public Version HttpVersion { get; init; } = new(1, 1);
    public HttpStatusCode StatusCode { get; init; }
    public Dictionary<string, StringValues> Headers { get; init; } = new();

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public byte[] Bytes { get; init; } = Array.Empty<byte>();
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public TimeSpan Elapsed { get; set; }
    public bool IsSuccessStatusCode => (int)StatusCode >= 200 && (int)StatusCode <= 299;
}
