// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using Microsoft.Extensions.Primitives;

namespace HTTPie.Middleware;

public sealed class RequestHeadersMiddleware : IRequestMiddleware
{
    public Task Invoke(HttpRequestModel model, Func<Task> next)
    {
        foreach (var input in model.RequestItems
            .Where(x => x.IndexOf(':') > 0
              && x.IndexOf(":=", StringComparison.OrdinalIgnoreCase) < 0))
        {
            var arr = input.Split(':');
            if (arr.Length == 2)
            {
                if (model.Headers.TryGetValue(arr[0], out var values))
                    model.Headers[arr[0]] = new StringValues(values.ToArray().Union(new[] { arr[1] }).ToArray());
                else
                    model.Headers[arr[0]] = arr[1];
            }
        }

        return next();
    }
}
