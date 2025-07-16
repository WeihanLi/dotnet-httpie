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
                        JsonNode jsonNode = dataInput[0].StartsWith("[]") ? new JsonObject() : new JsonArray();
                        foreach (var item in dataInput)
                        {
                            var idx = item.IndexOf('[');
                            if (idx > -1)
                            {

                            }
                        }
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
}
