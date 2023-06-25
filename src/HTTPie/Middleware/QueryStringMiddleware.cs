// Copyright (c) Weihan Li.All rights reserved.
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
        for (var i = requestModel.RequestItems.Count - 1; i >= 0; i--)
        {
            var item = requestModel.RequestItems[i];
            var index = item.IndexOf("==", StringComparison.Ordinal);
            if (index <= 0) continue;
            var key = item[..index];
            if (!key.IsMatch(Constants.ParamNameRegex)) continue;

            var value = item[(index + 2)..];
            if (requestModel.Query.TryGetValue(key, out var values))
                requestModel.Query[key] =
                    new StringValues(values.ToArray().Prepend(value).ToArray());
            else
                requestModel.Query[key] = new StringValues(value);

            requestModel.RequestItems.RemoveAt(i);
        }

        return next(requestModel);
    }
}
