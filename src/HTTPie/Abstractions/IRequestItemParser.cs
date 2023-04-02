// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Models;

namespace HTTPie.Abstractions;

public interface IRequestItemParser
{
    Task ParseAsync(HttpRequestModel request);
}
