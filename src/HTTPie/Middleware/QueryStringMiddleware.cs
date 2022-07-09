// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using Microsoft.Extensions.Primitives;

namespace HTTPie.Middleware;

public sealed class QueryStringMiddleware : IRequestMiddleware
{
    public Task Invoke(HttpRequestModel requestModel, Func<HttpRequestModel, Task> next)
    {
        foreach (var item in requestModel.RequestItems)
        {
            var index = item.IndexOf("==", StringComparison.Ordinal);
            if (index > 0 && item[..index].IsMatch(Constants.ParamNameRegex))
            {
                var key = item[..index];
                var value = item[(index + 2)..];
                if (requestModel.Query.TryGetValue(key, out var values))
                    requestModel.Query[key] =
                        new StringValues(values.ToArray().Append(value).ToArray());
                else
                    requestModel.Query[key] = new StringValues(value);

            }
        }

        return next(requestModel);
    }
}
