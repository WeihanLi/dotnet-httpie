﻿// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using Microsoft.Extensions.Primitives;

namespace HTTPie.Middleware;

public sealed class RequestCacheMiddleware : IRequestMiddleware
{
    private static readonly Option<bool> NoCacheOption = new("--no-cache")
    {
        Description = "Send 'Cache-Control: No-Cache' request header"
    };

    public Option[] SupportedOptions()
    {
        return [NoCacheOption];
    }

    public Task InvokeAsync(HttpRequestModel requestModel, Func<HttpRequestModel, Task> next)
    {
        if (requestModel.ParseResult.HasOption(NoCacheOption))
        {
            requestModel.Headers["Cache-Control"] = new StringValues("No-Cache");
        }
        return next(requestModel);
    }
}
