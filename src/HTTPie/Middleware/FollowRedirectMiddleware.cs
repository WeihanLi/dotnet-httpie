using HTTPie.Abstractions;
using HTTPie.Models;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace HTTPie.Middleware
{
    public class FollowRedirectMiddleware : IHttpHandlerMiddleware
    {
        private readonly HttpRequestModel _requestModel;
        
        public static readonly Option FollowOption = new(new[]{ "--follow","-F" }, "The HTTP request should follow redirects");
        public static readonly Option<int> MaxRedirectsOption = new("--max-redirects", "Allowed max HTTP request redirect times");

        public FollowRedirectMiddleware(HttpRequestModel requestModel)
        {
            _requestModel = requestModel;
        }

        public Task Invoke(HttpClientHandler httpClientHandler, Func<Task> next)
        {
            if (_requestModel.ParseResult.HasOption(FollowOption)) httpClientHandler.AllowAutoRedirect = true;
            var maxRedirects = _requestModel.ParseResult.ValueForOption(MaxRedirectsOption);
            if (maxRedirects > 0)
                httpClientHandler.MaxAutomaticRedirections = maxRedirects;
            return next();
        }
        public ICollection<Option> SupportedOptions() => new []
        {
            FollowOption,
            MaxRedirectsOption
        };
    }
}