using System.Collections.Generic;

namespace HTTPie.Models
{
    public class HttpContext
    {
        public HttpContext(HttpRequestModel request, HttpResponseModel response)
        {
            Request = request;
            Response = response;
        }

        public HttpRequestModel Request { get; }
        public HttpResponseModel Response { get; }
        public Dictionary<string, object> Properties { get; } = new();
    }
}