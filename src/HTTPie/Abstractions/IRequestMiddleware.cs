// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using HTTPie.Models;

namespace HTTPie.Abstractions;

public interface IRequestMiddleware : IPlugin
{
    Task Invoke(HttpRequestModel requestModel, Func<HttpRequestModel, Task> next);
}
