// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using Microsoft.Extensions.Primitives;

namespace HTTPie.Middleware;

public sealed class RequestHeadersMiddleware : IRequestMiddleware
{
    public Task Invoke(HttpRequestModel model, Func<HttpRequestModel, Task> next)
    {
        foreach (var input in model.RequestItems
            .Where(x => x.IndexOf(':') > 0
              && x.IndexOf(":=", StringComparison.OrdinalIgnoreCase) < 0))
        {
            var arr = input.Split(':');
            if (arr.Length == 2)
            {
                var (headerName, headerValue) = (arr[0], arr[1]);
                if (model.Headers.TryGetValue(headerName, out var values))
                {
                    var originalValues = values.ToArray();
                    var newValues = new string[values.Count + 1];
                    Array.Copy(originalValues, newValues, originalValues.Length);
                    newValues[^1] = headerValue;
                    model.Headers[headerName] = new StringValues(newValues);
                }
                else
                    model.Headers[headerName] = headerValue;
            }
        }

        return next(model);
    }
}
