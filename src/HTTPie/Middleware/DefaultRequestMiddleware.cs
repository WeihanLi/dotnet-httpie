using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using WeihanLi.Extensions;

namespace HTTPie.Middleware
{
    public class DefaultRequestMiddleware : IRequestMiddleware
    {
        private const string SchemaParameter = "--schema";

        private readonly Dictionary<string, string> _supportedParameters = new()
        {
            {"--json", "The request body is json, and content type is 'application/json'"},
            {SchemaParameter, "The request schema"}
        };

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

            requestModel.Headers.TryAdd("User-Agent", Constants.DefaultUserAgent);
            await next();
        }

        public Dictionary<string, string> SupportedParameters()
        {
            return _supportedParameters;
        }
    }
}