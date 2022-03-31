// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;

namespace HTTPie.Middleware;

public class FollowRedirectMiddleware : IHttpHandlerMiddleware
{
    private readonly HttpRequestModel _requestModel;

    private static readonly Option FollowOption = new(new[] { "--follow", "-F" }, "The HTTP request should follow redirects");
    private static readonly Option<int> MaxRedirectsOption = new("--max-redirects", "Allowed max HTTP request redirect times");

    public FollowRedirectMiddleware(HttpRequestModel requestModel)
    {
        _requestModel = requestModel;
    }

    public Task Invoke(HttpClientHandler httpClientHandler, Func<Task> next)
    {
        if (_requestModel.ParseResult.HasOption(FollowOption)
            || _requestModel.ParseResult.HasOption(DownloadMiddleware.DownloadOption))
        {
            httpClientHandler.AllowAutoRedirect = true;
        }
        var maxRedirects = _requestModel.ParseResult.GetValueForOption(MaxRedirectsOption);
        if (maxRedirects > 0)
            httpClientHandler.MaxAutomaticRedirections = maxRedirects;
        return next();
    }
    public ICollection<Option> SupportedOptions() => new[]
    {
            FollowOption,
            MaxRedirectsOption
        };
}
