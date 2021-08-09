using System.Collections.Generic;

namespace HTTPie.Abstractions
{
    public interface IPlugin
    {
        Dictionary<string, string> SupportedParameters()
        {
            return new Dictionary<string, string>();
        }
    }
}