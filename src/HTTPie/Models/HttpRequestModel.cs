using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace HTTPie.Models
{
    public class HttpRequestModel
    {
        [Required] public string Schema { get; set; } = "http";

        public HttpMethod Method { get; set; } = HttpMethod.Get;

        [Required] public string Url { get; set; } = string.Empty;

        public Version HttpVersion { get; set; } = new(1, 1);
        public int? Timeout { get; set; }
        public IDictionary<string, StringValues> Headers { get; } = new Dictionary<string, StringValues>();
        public IDictionary<string, StringValues> Query { get; set; } = new Dictionary<string, StringValues>();
        public string? Body { get; set; }

        public string[] RawInput { get; set; } = Array.Empty<string>();
        public IConfiguration RawConfiguration { get; set; } = null!;
    }
}