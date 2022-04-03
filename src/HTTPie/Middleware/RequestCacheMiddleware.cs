// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using Microsoft.Extensions.Primitives;

namespace HTTPie.Middleware;

public sealed class RequestCacheMiddleware: IRequestMiddleware
{
    private static readonly Option NoCacheOption = new("--no-cache", "Send 'Cache-Control: No-Cache' request header");

    public ICollection<Option> SupportedOptions()
    {
        return new[] { NoCacheOption };
    }

    public Task Invoke(HttpRequestModel requestModel, Func<Task> next)
    {
        if (requestModel.ParseResult.HasOption(NoCacheOption))
        {
            requestModel.Headers["Cache-Control"] = new StringValues("No-Cache");
        }
        return Task.CompletedTask;
    }
}
