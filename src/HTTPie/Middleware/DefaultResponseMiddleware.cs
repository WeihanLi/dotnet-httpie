// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Implement;
using HTTPie.Models;
using HTTPie.Utilities;

namespace HTTPie.Middleware;

public sealed class DefaultResponseMiddleware : IResponseMiddleware
{
    public Task Invoke(HttpContext context, Func<Task> next)
    {
        var outputFormat = OutputFormatter.GetOutputFormat(context);
        if ((outputFormat & OutputFormat.Timestamp) != 0)
        {
            context.Request.Headers.TryAdd(Constants.RequestTimestampHeaderName, context.Request.Timestamp.ToString());
            if (context.Response.Elapsed > TimeSpan.Zero)
            {
                context.Response.Headers.TryAdd(Constants.ResponseTimestampHeaderName, context.Response.Timestamp.ToString());
                context.Response.Headers.TryAdd(Constants.RequestDurationHeaderName, $"{context.Response.Elapsed.TotalMilliseconds}ms");
            }
        }
        return next();
    }
}
