using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Primitives;

namespace HTTPie.Models
{
    public class HttpResponseModel
    {
        public Version HttpVersion { get; set; } = null!;
        public HttpStatusCode StatusCode { get; set; }
        public Dictionary<string, StringValues> Headers { get; set; } = null!;
        public string? Body { get; set; }
    }
}