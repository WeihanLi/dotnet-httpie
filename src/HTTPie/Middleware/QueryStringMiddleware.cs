// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using Microsoft.Extensions.Primitives;

namespace HTTPie.Middleware;

public sealed class QueryStringMiddleware : IRequestMiddleware
{
    public Task Invoke(HttpRequestModel requestModel, Func<HttpRequestModel, Task> next)
    {
        foreach (var item in requestModel.RequestItems)
        {
            var index = item.IndexOf("==", StringComparison.Ordinal);
            if (index > 0 && item[..index].IsMatch(@"[\w_\-]+"))
            {
                var queryKey = item[..index];
                var queryValue = item[(index + 2)..];
                if (requestModel.Query.TryGetValue(queryKey, out var values))
                    requestModel.Query[queryKey] =
                        new StringValues(values.ToArray().Append(queryValue).ToArray());
                else
                    requestModel.Query[queryKey] = new StringValues(queryValue);

            }
        }

        return next(requestModel);
    }
}
