// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using HTTPie.Models;

namespace HTTPie.Abstractions;

public interface IOutputFormatter : IPlugin
{
    Task<string> GetOutput(HttpContext httpContext);
}
