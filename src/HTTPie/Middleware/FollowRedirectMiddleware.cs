// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;

namespace HTTPie.Middleware;

public sealed class FollowRedirectMiddleware(HttpRequestModel requestModel) : IHttpHandlerMiddleware
{
    private static readonly Option<bool> FollowOption = new("--follow", "-F")
    {
        Description = "The HTTP request should follow redirects"
    };
    private static readonly Option<int> MaxRedirectsOption = new("--max-redirects")
    {
        Description = "Allowed max HTTP request redirect times"
    };

    public Task InvokeAsync(HttpClientHandler httpClientHandler, Func<HttpClientHandler, Task> next)
    {
        if (requestModel.ParseResult.HasOption(FollowOption)
            || requestModel.ParseResult.HasOption(DownloadMiddleware.DownloadOption))
        {
            httpClientHandler.AllowAutoRedirect = true;
        }
        var maxRedirects = requestModel.ParseResult.GetValue(MaxRedirectsOption);
        if (maxRedirects > 0)
            httpClientHandler.MaxAutomaticRedirections = maxRedirects;

        return next(httpClientHandler);
    }
    public Option[] SupportedOptions() =>
    [
        FollowOption,
        MaxRedirectsOption
    ];
}
