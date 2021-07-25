using System;
using System.Collections.Generic;

namespace HTTPie.Models
{
    public class HttpContext
    {
        public HttpContext(HttpRequestModel request, HttpResponseModel response, IServiceProvider services)
        {
            Request = request;
            Response = response;
            RequestServices = services;
        }

        public HttpRequestModel Request { get; }
        public HttpResponseModel Response { get; }
        public Dictionary<string, object> Properties { get; } = new();
        public IServiceProvider RequestServices { get; }
    }
}