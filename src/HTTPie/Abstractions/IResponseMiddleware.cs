// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using HTTPie.Models;

namespace HTTPie.Abstractions;

public interface IResponseMiddleware : IPlugin
{
    Task Invoke(HttpContext context, Func<HttpContext, Task> next);
}
