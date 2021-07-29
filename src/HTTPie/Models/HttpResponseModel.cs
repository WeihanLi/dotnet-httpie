using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Primitives;

namespace HTTPie.Models
{
    public class HttpResponseModel
    {
        public Version HttpVersion { get; set; } = new(1, 1);
        public HttpStatusCode StatusCode { get; set; }
        public Dictionary<string, StringValues> Headers { get; set; } = new();
        public string? Body { get; set; }
    }
}