using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HTTPie.Middleware
{
    public class RequestDataMiddleware : IRequestMiddleware
    {
        private readonly HttpContext _httpContext;

        public RequestDataMiddleware(HttpContext httpContext)
        {
            _httpContext = httpContext;
        }

        public static readonly Option FormOption = new(new[] { "-f", "--form" }, $"The request is form data, and content type is '{Constants.FormContentType}'");
        public static readonly Option JsonOption = new(new[]{"-j","--json"},$"The request body is json by default, and content type is '{Constants.JsonContentType}'");

        public ICollection<Option> SupportedOptions() => new[]{ FormOption, JsonOption };

        public Task Invoke(HttpRequestModel requestModel, Func<Task> next)
        {
            var isFormData = requestModel.ParseResult.HasOption(FormOption);
            _httpContext.UpdateFlag(Constants.FeatureFlagNames.IsFormContentType, isFormData);
            requestModel.Headers[Constants.ContentTypeHeaderName] = isFormData
                ? new StringValues(Constants.FormContentType)
                : new StringValues(Constants.JsonContentType);
            var dataInput = requestModel.Arguments
                .Where(x => x.IndexOf('=') > 0
                            && x.IndexOf("==", StringComparison.Ordinal) < 0
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
                            var index = input.IndexOf(":=");
                            if (index > 0)
                            {
                                if (k > 0) jsonDataBuilder.Append(",");
                                jsonDataBuilder.Append($@"""{input[..index]}"":{input[(index + 2)..]}");
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