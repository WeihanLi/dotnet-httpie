using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using Microsoft.Extensions.Logging;
using WeihanLi.Extensions;

namespace HTTPie.Middleware
{
    public class DefaultRequestMiddleware : IRequestMiddleware
    {
        private readonly ILogger _logger;

        private readonly Dictionary<string, string> _supportedParameters = new()
        {
            {"--schema", "The request schema"},
            {"--httpVersion", "The request http version"},
            {"--timeout", "timeout for the request, in seconds"}
        };

        public DefaultRequestMiddleware(ILogger logger)
        {
            _logger = logger;
        }

        public async Task Invoke(HttpRequestModel requestModel, Func<Task> next)
        {
            var url = requestModel.Url;
            if (requestModel.Query.Count > 0)
            {
                url += url.LastIndexOf('?') > 0 ? "&" : "?";
                url += requestModel.Query.Select(x => x.Value.Select(v => $"{x.Key}={v}").StringJoin("&"))
                    .StringJoin("&");
            }
            requestModel.Url = url;

            var httpVersionOption =
                requestModel.RawInput.FirstOrDefault(x => x.StartsWith("--httpVersion="))?["--httpVersion=".Length..];
            if (!string.IsNullOrEmpty(httpVersionOption))
            {
                _logger.LogDebug($"httpVersion: {httpVersionOption}");
                if (httpVersionOption.IndexOf('.') < 0)
                {
                    httpVersionOption = $"{httpVersionOption}.0";
                }
                if(Version.TryParse(httpVersionOption, out var version))
                  requestModel.HttpVersion = version;
            }

            requestModel.Headers.TryAdd("User-Agent", Constants.DefaultUserAgent);
            await next();
        }

        public Dictionary<string, string> SupportedParameters() => _supportedParameters;
    }
}