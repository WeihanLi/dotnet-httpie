// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using Microsoft.Extensions.Primitives;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using WeihanLi.Common.Extensions;

namespace HTTPie.Middleware;

public sealed partial class RequestDataMiddleware(HttpContext httpContext) : IRequestMiddleware
{
    private static readonly Option<bool> FormOption = new("-f", "--form")
    {
        Description = $"The request is form data, and content type is '{HttpHelper.FormDataContentType}'"
    };

    private static readonly Option<bool> JsonOption = new("-j", "--json")
    {
        Description = $"The request body is json by default, and content type is '{HttpHelper.ApplicationJsonContentType}'"
    };

    private static readonly Option<string> RawDataOption = new("--raw")
    {
        Description = "The raw request body"
    };

    [GeneratedRegex(@"^[a-zA-Z_\[][\w_\-\[\]]*$")]
    private static partial Regex PropertyNameRegex();

    public Option[] SupportedOptions() => [FormOption, JsonOption, RawDataOption];

    public Task InvokeAsync(HttpRequestModel requestModel, Func<HttpRequestModel, Task> next)
    {
        var isFormData = requestModel.ParseResult.HasOption(FormOption);
        httpContext.UpdateFlag(Constants.FlagNames.IsFormContentType, isFormData);

        if (requestModel.ParseResult.HasOption(RawDataOption))
        {
            var rawData = requestModel.ParseResult.GetValue(RawDataOption);
            requestModel.Body = rawData;
        }
        else
        {
            var dataInput = requestModel.RequestItems
                .Where(x =>
                {
                    var index = x.IndexOf('=');
                    if (index <= 0) return false;

                    if (x[index - 1] == ':')
                        return PropertyNameRegex().IsMatch(x[..(index - 1)]);

                    if (PropertyNameRegex().IsMatch(x[..index]))
                        return index == x.Length - 1 || x[index + 1] != '=';

                    return false;
                })
                .ToArray();
            if (dataInput.Length > 0)
            {
                if (isFormData)
                {
                    requestModel.Body = string.Join("&", dataInput);
                }
                else
                {
                    if (dataInput.Any(x => x.IndexOf('[') > 0) || dataInput.Any(x => x.StartsWith('[')))
                    {
                        // nested json exists or root array
                        JsonNode rootNode;
                        
                        // Check if all inputs start with '[' indicating root array
                        if (dataInput.All(x => x.StartsWith('[')))
                        {
                            rootNode = new JsonArray();
                        }
                        else
                        {
                            rootNode = new JsonObject();
                        }
                        
                        foreach (var item in dataInput)
                        {
                            ParseNestedJsonItem(item, rootNode);
                        }

                        requestModel.Body = rootNode.ToJsonString(Helpers.JsonSerializerOptions);
                    }
                    else
                    {
                        var jsonDataBuilder = new StringBuilder("{");
                        var k = 0;
                        foreach (var input in dataInput)
                            if (input.IndexOf(":=", StringComparison.Ordinal) > 0)
                            {
                                var index = input.IndexOf(":=", StringComparison.Ordinal);
                                if (index > 0)
                                {
                                    if (k > 0) jsonDataBuilder.Append(',');
                                    jsonDataBuilder.Append($@"""{input[..index]}"":{input[(index + 2)..]}");
                                    k++;
                                }
                            }
                            else
                            {
                                var index = input.IndexOf('=');
                                if (index > 0)
                                {
                                    if (k > 0) jsonDataBuilder.Append(',');
                                    jsonDataBuilder.Append(
                                        $@"""{input[..index]}"":""{input[(index + 1)..].Replace("\"", "\\\"")}""");
                                    k++;
                                }
                            }

                        jsonDataBuilder.Append('}');
                        requestModel.Body = jsonDataBuilder.ToString();
                    }
                }
            }
        }

        if (requestModel.Body.IsNotNullOrEmpty())
        {
            requestModel.Headers[Constants.ContentTypeHeaderName] = isFormData
                ? new StringValues(HttpHelper.FormDataContentType)
                : new StringValues(HttpHelper.ApplicationJsonContentType);

            var requestMethodExists = httpContext.GetProperty<bool>(Constants.RequestMethodExistsPropertyName);
            if (!requestMethodExists)
            {
                requestModel.Method = HttpMethod.Post;
            }
        }

        return next(requestModel);
    }

    private static void ParseNestedJsonItem(string item, JsonNode rootNode)
    {
        // Determine if this is a raw value (:=) or string value (=)
        var isRawValue = item.Contains(":=");
        var separator = isRawValue ? ":=" : "=";
        var separatorIndex = item.IndexOf(separator, StringComparison.Ordinal);
        
        if (separatorIndex <= 0) return;

        var path = item[..separatorIndex];
        var value = item[(separatorIndex + separator.Length)..];

        // Parse the path to extract keys
        var keys = ParsePropertyPath(path);
        if (keys.Count == 0) return;

        // Handle root array case
        if (rootNode is JsonArray rootArray && keys.Count == 1 && keys[0].IsArrayIndex)
        {
            // Direct array access like [0]= or []=
            var rootArrayKey = keys[0];
            if (rootArrayKey.Name == "root" || string.IsNullOrEmpty(rootArrayKey.Name))
            {
                // Array append operation []= 
                rootArray.Add(CreateJsonValue(value, isRawValue));
            }
            else
            {
                // Indexed array access [0]= 
                if (int.TryParse(rootArrayKey.Name, out int index))
                {
                    // Extend array if necessary
                    while (rootArray.Count <= index)
                    {
                        rootArray.Add(JsonValue.Create((string?)null));
                    }
                    rootArray[index] = CreateJsonValue(value, isRawValue);
                }
            }
            return;
        }

        // Navigate/create the nested structure
        JsonNode currentNode = rootNode;
        
        for (int i = 0; i < keys.Count - 1; i++)
        {
            var key = keys[i];
            
            if (key.IsArrayIndex)
            {
                // Handle array navigation
                if (currentNode is JsonArray currentArray)
                {
                    // We're working with an array, need to access by index
                    if (int.TryParse(key.Name, out int index))
                    {
                        while (currentArray.Count <= index)
                        {
                            currentArray.Add(new JsonObject());
                        }
                        currentNode = currentArray[index]!;
                    }
                }
                else
                {
                    var objectNode = currentNode.AsObject();
                    if (!objectNode.ContainsKey(key.Name))
                    {
                        objectNode[key.Name] = new JsonArray();
                    }
                    currentNode = objectNode[key.Name]!;
                }
            }
            else
            {
                // Handle object navigation
                if (currentNode is JsonArray currentArray)
                {
                    // Convert array to object if needed (shouldn't happen in normal usage)
                    throw new InvalidOperationException("Cannot access object property on array");
                }
                else
                {
                    var objectNode = currentNode.AsObject();
                    if (!objectNode.ContainsKey(key.Name))
                    {
                        objectNode[key.Name] = new JsonObject();
                    }
                    currentNode = objectNode[key.Name]!;
                }
            }
        }

        // Set the final value
        var finalKey = keys[^1];
        if (finalKey.IsArrayIndex)
        {
            // Add to array
            if (currentNode is JsonArray currentArray)
            {
                // Direct array access
                currentArray.Add(CreateJsonValue(value, isRawValue));
            }
            else
            {
                var objectNode = currentNode.AsObject();
                if (!objectNode.ContainsKey(finalKey.Name))
                {
                    objectNode[finalKey.Name] = new JsonArray();
                }
                var array = objectNode[finalKey.Name]!.AsArray();
                array.Add(CreateJsonValue(value, isRawValue));
            }
        }
        else
        {
            // Set object property
            if (currentNode is JsonArray)
            {
                throw new InvalidOperationException("Cannot set object property on array");
            }
            else
            {
                var objectNode = currentNode.AsObject();
                objectNode[finalKey.Name] = CreateJsonValue(value, isRawValue);
            }
        }
    }

    private static List<PropertyKey> ParsePropertyPath(string path)
    {
        var keys = new List<PropertyKey>();
        var current = 0;
        
        // Handle root array case where path starts with [
        if (path.StartsWith('['))
        {
            var closingBracket = path.IndexOf(']', 1);
            if (closingBracket == -1) return keys; // Malformed
            
            var bracketContent = path[1..closingBracket];
            
            if (string.IsNullOrEmpty(bracketContent))
            {
                // Empty brackets indicate array append operation []
                keys.Add(new PropertyKey("root", true));
            }
            else
            {
                // Indexed array access [0], [1], etc.
                keys.Add(new PropertyKey(bracketContent, true));
            }
            
            current = closingBracket + 1;
        }
        
        while (current < path.Length)
        {
            var bracketIndex = path.IndexOf('[', current);
            
            if (bracketIndex == -1)
            {
                // No more brackets, take the rest as a simple property
                if (current < path.Length)
                {
                    keys.Add(new PropertyKey(path[current..], false));
                }
                break;
            }
            
            // Add the property name before the bracket
            if (bracketIndex > current)
            {
                keys.Add(new PropertyKey(path[current..bracketIndex], false));
            }
            
            // Find the closing bracket
            var closingBracket = path.IndexOf(']', bracketIndex);
            if (closingBracket == -1) break; // Malformed path
            
            var bracketContent = path[(bracketIndex + 1)..closingBracket];
            
            if (string.IsNullOrEmpty(bracketContent))
            {
                // Empty brackets indicate array append operation
                keys.Add(new PropertyKey(keys.Count > 0 ? keys[^1].Name : "root", true));
                if (keys.Count > 1) keys.RemoveAt(keys.Count - 2); // Remove the duplicate key
            }
            else
            {
                // Named property in brackets
                keys.Add(new PropertyKey(bracketContent, false));
            }
            
            current = closingBracket + 1;
        }
        
        return keys;
    }

    private static JsonNode CreateJsonValue(string value, bool isRawValue)
    {
        if (!isRawValue)
        {
            return JsonValue.Create(value);
        }

        // Try to parse as JSON for raw values
        try
        {
            return JsonNode.Parse(value) ?? JsonValue.Create(value);
        }
        catch
        {
            // If parsing fails, treat as string
            return JsonValue.Create(value);
        }
    }

    private record PropertyKey(string Name, bool IsArrayIndex);
}
