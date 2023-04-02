// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using Microsoft.Extensions.Primitives;

namespace HTTPie.Middleware;

public sealed class RequestHeadersMiddleware : IRequestMiddleware
{
    public Task Invoke(HttpRequestModel requestModel, Func<HttpRequestModel, Task> next)
    {
        for (var i = requestModel.RequestItems.Count - 1; i >= 0; i--)
        {
            var item = requestModel.RequestItems[i];
            var index = item.IndexOf(':');
            if (index > 0 && item.Length > (index + 1)
                          && item[(index + 1)] != '='
                          && item[..index].IsMatch(Constants.ParamNameRegex))
            {
                var key = item[..index];
                var value = item[(index + 1)..].Trim();
                if (requestModel.Headers.TryGetValue(key, out var values))
                    requestModel.Headers[key] =
                        new StringValues(values.ToArray().Append(value).ToArray());
                else
                    requestModel.Headers[key] = new StringValues(value);

                requestModel.RequestItems.RemoveAt(i);
            }
        }

        return next(requestModel);
    }
}
