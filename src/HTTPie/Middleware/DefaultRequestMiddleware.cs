// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using Microsoft.Extensions.Logging;

namespace HTTPie.Middleware;

public sealed class DefaultRequestMiddleware : IRequestMiddleware
{
    private readonly ILogger _logger;

    public DefaultRequestMiddleware(ILogger logger)
    {
        _logger = logger;
    }

    private static readonly Option DebugOption = new("--debug", "Enable debug mode, output debug log");
    private static readonly Option<string> SchemaOption = new("--schema", "The HTTP request schema");
    private static readonly Option<Version> HttpVersionOption = new("--httpVersion", "The HTTP request HTTP version");

    public ICollection<Option> SupportedOptions() => new HashSet<Option>()
        {
            DebugOption,
            SchemaOption,
            HttpVersionOption,
        };

    public Task Invoke(HttpRequestModel requestModel, Func<HttpRequestModel, Task> next)
    {
        var schema = requestModel.ParseResult.GetValueForOption(SchemaOption);
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

        var httpVersionOption =
            requestModel.ParseResult.GetValueForOption(HttpVersionOption);
        if (httpVersionOption != default)
        {
            _logger.LogDebug($"httpVersion: {httpVersionOption}");
            requestModel.HttpVersion = httpVersionOption;
        }

        requestModel.Headers.TryAdd("User-Agent", Constants.DefaultUserAgent);
        
        return next(requestModel);
    }
}
