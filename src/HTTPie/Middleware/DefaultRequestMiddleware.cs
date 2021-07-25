using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;

namespace HTTPie.Middleware
{
    public class DefaultRequestMiddleware : IRequestMiddleware
    {
        private readonly Dictionary<string, string> _supportedParameters = new()
        {
            {"--json", "The request body is json, and content type is 'application/json'"}
        };

        public async Task Invoke(HttpRequestModel model, Func<Task> next)
        {
            if (model.Url.StartsWith(':')) model.Url = $"http://localhost{model.Url}";
            if (!model.Url.StartsWith("http") && !model.Url.StartsWith("https"))
                model.Url = $"{model.Schema}://{model.Url}";
            model.Headers.TryAdd("User-Agent", Constants.DefaultUserAgent);
            await next();
        }

        public Dictionary<string, string> SupportedParameters()
        {
            return _supportedParameters;
        }
    }
}