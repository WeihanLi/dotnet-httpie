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
                    if (dataInput.Any(x => x.IndexOf('[') > 0))
                    {
                        // nested json exists
                        var rootJsonObject = new JsonObject();
                        
                        foreach (var item in dataInput)
                        {
                            ParseNestedJsonItem(item, rootJsonObject);
                        }

                        requestModel.Body = rootJsonObject.ToJsonString(Helpers.JsonSerializerOptions);
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

    private static void ParseNestedJsonItem(string item, JsonObject rootObject)
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

        // Navigate/create the nested structure
        JsonNode currentNode = rootObject;
        
        for (int i = 0; i < keys.Count - 1; i++)
        {
            var key = keys[i];
            
            if (key.IsArrayIndex)
            {
                // Handle array navigation
                var arrayNode = currentNode.AsObject();
                if (!arrayNode.ContainsKey(key.Name))
                {
                    arrayNode[key.Name] = new JsonArray();
                }
                currentNode = arrayNode[key.Name]!;
            }
            else
            {
                // Handle object navigation
                var objectNode = currentNode.AsObject();
                if (!objectNode.ContainsKey(key.Name))
                {
                    objectNode[key.Name] = new JsonObject();
                }
                currentNode = objectNode[key.Name]!;
            }
        }

        // Set the final value
        var finalKey = keys[^1];
        if (finalKey.IsArrayIndex)
        {
            // Add to array
            var arrayNode = currentNode.AsObject();
            if (!arrayNode.ContainsKey(finalKey.Name))
            {
                arrayNode[finalKey.Name] = new JsonArray();
            }
            var array = arrayNode[finalKey.Name]!.AsArray();
            array.Add(CreateJsonValue(value, isRawValue));
        }
        else
        {
            // Set object property
            var objectNode = currentNode.AsObject();
            objectNode[finalKey.Name] = CreateJsonValue(value, isRawValue);
        }
    }

    private static List<PropertyKey> ParsePropertyPath(string path)
    {
        var keys = new List<PropertyKey>();
        var current = 0;
        
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
                keys.RemoveAt(keys.Count - 2); // Remove the duplicate key
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
