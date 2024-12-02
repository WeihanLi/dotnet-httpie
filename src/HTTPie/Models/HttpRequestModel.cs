// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Primitives;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace HTTPie.Models;

[DebuggerDisplay("{Method} {Url}")]
public sealed class HttpRequestModel
{
    [Required] public string Schema { get; set; } = "http";

    public HttpMethod Method { get; set; } = HttpMethod.Get;

    [Required] public string Url { get; set; } = string.Empty;

    public Version HttpVersion { get; set; } = new(1, 1);
    public IDictionary<string, StringValues> Headers { get; } = new Dictionary<string, StringValues>();
    public IDictionary<string, StringValues> Query { get; set; } = new Dictionary<string, StringValues>();
    public string? Body { get; set; }

    public List<string> RequestItems { get; set; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [JsonIgnore]
    public DateTimeOffset Timestamp { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    [JsonIgnore]
    public ParseResult ParseResult { get; set; } = null!;

    public override string ToString()
    {
        return this.ToJson();
    }
}
