// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using Microsoft.Extensions.Primitives;
using System.Text;
using WeihanLi.Common.Extensions;

namespace HTTPie.Middleware;

public sealed class RequestDataMiddleware : IRequestMiddleware
{
    private readonly HttpContext _httpContext;

    public RequestDataMiddleware(HttpContext httpContext)
    {
        _httpContext = httpContext;
    }

    private static readonly Option<bool> FormOption = new(new[] { "-f", "--form" },
        $"The request is form data, and content type is '{HttpHelper.FormDataContentType}'");

    private static readonly Option<bool> JsonOption = new(new[] { "-j", "--json" },
        $"The request body is json by default, and content type is '{HttpHelper.JsonContentType}'");

    private static readonly Option<string> RawDataOption = new("--raw", $"The raw request body");

    public Option[] SupportedOptions() => new Option[] { FormOption, JsonOption, RawDataOption };

    public Task InvokeAsync(HttpRequestModel requestModel, Func<HttpRequestModel, Task> next)
    {
        var isFormData = requestModel.ParseResult.HasOption(FormOption);
        _httpContext.UpdateFlag(Constants.FlagNames.IsFormContentType, isFormData);

        if (requestModel.ParseResult.HasOption(RawDataOption))
        {
            var rawData = requestModel.ParseResult.GetValueForOption(RawDataOption);
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
                        return x[..(index - 1)].IsMatch(Constants.ParamNameRegex);

                    if (x[..index].IsMatch(Constants.ParamNameRegex))
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

        if (requestModel.Body.IsNotNullOrEmpty())
        {
            requestModel.Headers[Constants.ContentTypeHeaderName] = isFormData
                ? new StringValues(HttpHelper.FormDataContentType)
                : new StringValues(HttpHelper.JsonContentType);

            var requestMethodExists = _httpContext.GetProperty<bool>(Constants.RequestMethodExistsPropertyName);
            if (!requestMethodExists)
            {
                requestModel.Method = HttpMethod.Post;
            }
        }

        return next(requestModel);
    }
}
