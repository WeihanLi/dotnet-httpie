// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using Microsoft.Extensions.Logging;

namespace HTTPie.Middleware;

public sealed class DefaultRequestMiddleware(ILogger logger) : IRequestMiddleware
{
    public static readonly Option<bool> DebugOption = new("--debug")
    {
        Description = "Enable debug mode, output debug log"
    };
    private static readonly Option<string> SchemaOption = new("--schema")
    {
        Description = "The HTTP request schema"
    };
    private static readonly Option<string> HttpVersionOption = new("--httpVersion")
    {
        Description = "The HTTP request HTTP version"
    };

    public Option[] SupportedOptions() => [DebugOption, SchemaOption, HttpVersionOption];

    public Task InvokeAsync(HttpRequestModel requestModel, Func<HttpRequestModel, Task> next)
    {
        var schema = requestModel.ParseResult.GetValue(SchemaOption);
        if (!string.IsNullOrEmpty(schema)) requestModel.Schema = schema;

        if (requestModel.Url is ":" or "/")
        {
            requestModel.Url = "localhost";
        }
        else
        {
            if (requestModel.Url.StartsWith(":/")) requestModel.Url = $"localhost{requestModel.Url[1..]}";
            if (requestModel.Url.StartsWith(':')) requestModel.Url = $"localhost{requestModel.Url}";
        }

        if (requestModel.Url.IndexOf("://", StringComparison.Ordinal) < 0)
            requestModel.Url = $"{requestModel.Schema}://{requestModel.Url}";
        if (requestModel.Url.StartsWith("://", StringComparison.Ordinal))
            requestModel.Url = $"{requestModel.Schema}{requestModel.Url}";

        var url = requestModel.Url;
        if (requestModel.Query.Count > 0)
        {
            url += url.LastIndexOf('?') > 0 ? "&" : "?";
            url += requestModel.Query.Select(x => x.Value.Select(v => $"{x.Key}={v}").StringJoin("&"))
                .StringJoin("&");
        }

        requestModel.Url = url;

        var httpVersionValue =
            requestModel.ParseResult.GetValue(HttpVersionOption);
        if (httpVersionValue.IsNotNullOrEmpty() && Version.TryParse(httpVersionValue, out var httpVersion))
        {
            logger.LogDebug("httpVersion specified: {HttpVersion}", httpVersionValue);
            requestModel.HttpVersion = httpVersion;
        }

        requestModel.Headers.TryAdd("User-Agent", Constants.DefaultUserAgent);

        return next(requestModel);
    }
}
