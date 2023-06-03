// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

namespace HTTPie.Utilities;

public sealed class JsonDataBuilder
{
    private readonly ICollection<string> _items;

    public JsonDataBuilder(ICollection<string> items)
    {
        _items = items;
    }

    private void Append(string item)
    {
        var equalsOperatorIndex = item.IndexOf('=');
        if (equalsOperatorIndex > 0)
        {
            var name = item[..equalsOperatorIndex];
            var value = equalsOperatorIndex == item.Length - 1
                ? string.Empty
                : item[(equalsOperatorIndex + 1)..];
            Append(name, value);
        }
        else
        {
            Append(item, string.Empty);
        }
    }

    private void Append(string name, string value)
    {
        var rawValue = name.EndsWith(':');
        if (rawValue)
        {
            var key = name[..^1];
        }
        else
        {
        }
    }
}
