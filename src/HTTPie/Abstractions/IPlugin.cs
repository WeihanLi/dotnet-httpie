using System;
using System.Collections.Generic;
using System.CommandLine;

namespace HTTPie.Abstractions
{
    public interface IPlugin
    {
        ICollection<Option> SupportedOptions() => Array.Empty<Option>();
    }
}