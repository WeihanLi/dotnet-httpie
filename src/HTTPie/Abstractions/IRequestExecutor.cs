// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using HTTPie.Models;

namespace HTTPie.Abstractions;

public interface IRequestExecutor
{
    ValueTask ExecuteAsync(HttpContext httpContext);

    Option[] SupportedOptions();
}
