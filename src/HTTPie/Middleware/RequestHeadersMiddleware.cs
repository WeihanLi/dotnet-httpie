// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using Microsoft.Extensions.Primitives;

namespace HTTPie.Middleware;

public sealed class RequestHeadersMiddleware : IRequestMiddleware
{
    public Task Invoke(HttpRequestModel requestModel, Func<HttpRequestModel, Task> next)
    {
        foreach (var item in requestModel.RequestItems
            .Where(x => x.IndexOf(":=", StringComparison.OrdinalIgnoreCase) < 0))
        {
            var index = item.IndexOf(':');
            if (index > 0 && item[..index].IsMatch(@"[\w_\-]+"))
            {
                var queryKey = item[..index];
                var queryValue = item[(index + 1)..];
                if (requestModel.Headers.TryGetValue(queryKey, out var values))
                    requestModel.Headers[queryKey] =
                        new StringValues(values.ToArray().Append(queryValue).ToArray());
                else
                    requestModel.Headers[queryKey] = new StringValues(queryValue);
            }
        }
        return next(requestModel);
    }
}
