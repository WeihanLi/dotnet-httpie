using System;
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

        public FollowRedirectMiddleware(HttpRequestModel requestModel)
        {
            _requestModel = requestModel;
        }

        public Task Invoke(HttpClientHandler httpClientHandler, Func<Task> next)
        {
            if (_requestModel.RawInput.Contains("--follow")) httpClientHandler.AllowAutoRedirect = true;
            var followLimit = _requestModel.RawInput.FirstOrDefault(x => x.StartsWith("max-redirects="));
            if (!string.IsNullOrEmpty(followLimit)
                && int.TryParse(followLimit["max-redirects=".Length..], out var maxRedirect)
                && maxRedirect > 0)
                httpClientHandler.MaxAutomaticRedirections = maxRedirect;
            return next();
        }
    }
}