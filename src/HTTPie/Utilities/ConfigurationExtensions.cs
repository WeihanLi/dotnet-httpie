// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Text.Json.Nodes;

namespace HTTPie.Utilities;

public static class ConfigurationExtensions
{
    public static IEnumerable<string> ToEnvironmentVariables(this IConfiguration configuration, string? prefix = null)
    {
        var envPrefix = prefix.IsNullOrEmpty() ? "" : $"{prefix}_";
        foreach (var (key, value) in configuration.AsEnumerable())
        {
            yield return $"{envPrefix}{key.Replace(":", "__")}={value}";
        }
    }

    public static JsonObject ToJsonNode(this IConfiguration configuration)
    {
        var root = new JsonObject();
        Dictionary<string, JsonNode> jsonPathNodes = new();
        foreach (var (key, value) in configuration.AsEnumerable())
        {
            var jsonValue = JsonValue.Create(value);
            var isArray = false;
            var keyName = key;
            var jsonPath = key;
            var separatorIndex = key.IndexOf(':');
            if (separatorIndex > 0)
            {
                keyName = key[..separatorIndex];
                var keySubPath = keyName[(separatorIndex + 1)..];
                var nextIndex = keySubPath.IndexOf(':');
                if (nextIndex > 0)
                {
                    jsonPath = key[..nextIndex];
                    var nextKeyName = keySubPath[(nextIndex + 1)..];
                    if (int.TryParse(nextKeyName, out _))
                    {
                        // should be array
                        isArray = true;
                    }
                }
            }

            var parentJsonPath = string.Empty;
            var idx = jsonPath.IndexOf(':');
            if (idx > 0)
            {
                parentJsonPath = jsonPath[..idx];
            }

            // add to root
            if (string.IsNullOrEmpty(parentJsonPath))
            {
                root[keyName] = jsonValue;
            }
            else
            {
                var jsonNode = jsonPathNodes.GetOrAdd(jsonPath, _ =>
                {
                    JsonNode node = isArray ? new JsonArray() : new JsonObject();

                    return node;
                });
                jsonNode[keyName] = jsonValue;
            }
        }

        return root;

        //
        JsonNode GetJsonNode(string path)
        {
            if (string.IsNullOrEmpty(path))
                return root;

            var idx = path.IndexOf(':');
            if (idx > 0)
            {
                var lastSubPath = path[(idx + 1)..];
                if (int.TryParse(lastSubPath, out var arrayIndex))
                {
                    var pathPrefix = path[..idx];
                    var innerIndex = pathPrefix.IndexOf(':');
                    Debug.Assert(innerIndex > 0);
                    var arrayPath = pathPrefix[(innerIndex + 1)..];
                    jsonPathNodes[arrayPath] = new JsonArray();
                }
                else
                {
                    root[path] = new JsonObject();
                }
            }

            return root[path];
        }
    }
}
