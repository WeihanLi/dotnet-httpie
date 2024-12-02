// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

namespace HTTPie.Abstractions;

public interface IPlugin
{
    Option[] SupportedOptions() => [];
}
