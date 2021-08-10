using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using Microsoft.Extensions.Primitives;

namespace HTTPie.Middleware
{
    public class RequestDataMiddleware : IRequestMiddleware
    {
        private readonly HttpContext _httpContext;

        private readonly Dictionary<string, string> _supportedParameters = new()
        {
            { "--form, -f", $"The request is form data, and content type is '{Constants.FormContentType}'" },
            { "--json, -j", $"The request body is json by default, and content type is '{Constants.JsonContentType}'" }
        };

        public RequestDataMiddleware(HttpContext httpContext)
        {
            _httpContext = httpContext;
        }

        public Dictionary<string, string> SupportedParameters()
        {
            return _supportedParameters;
        }

        public Task Invoke(HttpRequestModel requestModel, Func<Task> next)
        {
            var isFormData = requestModel.RawInput.Contains("-f") || requestModel.RawInput.Contains("--form");
            _httpContext.UpdateFlag(Constants.FeatureFlagNames.IsFormContentType, isFormData);
            requestModel.Headers[Constants.ContentTypeHeaderName] = isFormData
                ? new StringValues(Constants.FormContentType)
                : new StringValues(Constants.JsonContentType);
            var dataInput = requestModel.RawInput
                .Where(x => x.IndexOf('=') > 0
                            && x.IndexOf("==", StringComparison.Ordinal) < 0
                            && x.IndexOf("://", StringComparison.Ordinal) < 0
                            && !x.StartsWith("-")
                            && x.Split('=')[0].IndexOf(':') < 0
                            )
                .ToArray();
            if (dataInput.Length > 0)
            {
                if (requestModel.Method == HttpMethod.Get) requestModel.Method = HttpMethod.Post;
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
                            var arr = input.Split(":=");
                            if (arr is { Length: 2 })
                            {
                                if (k > 0) jsonDataBuilder.Append(",");
                                jsonDataBuilder.Append($@"""{arr[0]}"":{arr[1]}");
                                k++;
                            }
                        }
                        else
                        {
                            var index = input.IndexOf('=');
                            if (index > 0)
                            {
                                if (k > 0) jsonDataBuilder.Append(",");
                                jsonDataBuilder.Append(
                                    $@"""{input[..index]}"":""{input[(index + 1)..].Replace("\"", "\\\"")}""");
                                k++;
                            }
                        }

                    jsonDataBuilder.Append("}");
                    requestModel.Body = jsonDataBuilder.ToString();
                }
            }

            return next();
        }
    }
}