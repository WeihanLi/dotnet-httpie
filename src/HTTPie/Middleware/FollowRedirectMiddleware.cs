using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HTTPie.Abstractions;
using HTTPie.Models;

namespace HTTPie.Middleware
{
    public class FollowRedirectMiddleware : IHttpHandlerMiddleware
    {
        private readonly HttpRequestModel _requestModel;
        private const string FollowOption = "--follow", MaxRedirectsOption= "--max-redirects";

        public FollowRedirectMiddleware(HttpRequestModel requestModel)
        {
            _requestModel = requestModel;
        }

        public Task Invoke(HttpClientHandler httpClientHandler, Func<Task> next)
        {
            if (_requestModel.Options.Contains(FollowOption)) httpClientHandler.AllowAutoRedirect = true;
            var maxRedirectOptionPrefix = $"{MaxRedirectsOption}="; 
            var maxRedirects = _requestModel.Options.FirstOrDefault(x => x.StartsWith(maxRedirectOptionPrefix));
            if (!string.IsNullOrEmpty(maxRedirects)
                && int.TryParse(maxRedirects[maxRedirectOptionPrefix.Length..], out var maxRedirect)
                && maxRedirect > 0)
                httpClientHandler.MaxAutomaticRedirections = maxRedirect;
            return next();
        }

        public Dictionary<string, string> SupportedParameters() => new()
        {
            { FollowOption, "follow redirect" },
            { MaxRedirectsOption, "allowed max redirect times" }
        };
    }
}