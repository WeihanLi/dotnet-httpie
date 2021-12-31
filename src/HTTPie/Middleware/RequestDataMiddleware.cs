using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using Microsoft.Extensions.Primitives;
using System.Text;

namespace HTTPie.Middleware;

public class RequestDataMiddleware : IRequestMiddleware
{
    private readonly HttpContext _httpContext;

    public RequestDataMiddleware(HttpContext httpContext)
    {
        _httpContext = httpContext;
    }
    public static readonly Option FormOption = new(new[] { "-f", "--form" }, $"The request is form data, and content type is '{Constants.FormContentType}'");
    public static readonly Option JsonOption = new(new[] { "-j", "--json" }, $"The request body is json by default, and content type is '{Constants.JsonContentType}'");
    public static readonly Option<string> RawDataOption = new("--raw", $"The raw request body");

    public ICollection<Option> SupportedOptions() => new[]
    {
            FormOption, JsonOption, RawDataOption
        };

    public Task Invoke(HttpRequestModel requestModel, Func<Task> next)
    {
        var isFormData = requestModel.ParseResult.HasOption(FormOption);
        _httpContext.UpdateFlag(Constants.FeatureFlagNames.IsFormContentType, isFormData);

        if (requestModel.ParseResult.HasOption(RawDataOption))
        {
            var rawData = requestModel.ParseResult.GetValueForOption(RawDataOption);
            requestModel.Body = rawData;
        }
        else
        {
            var dataInput = requestModel.RequestItems
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
                 ? new StringValues(Constants.FormContentType)
                 : new StringValues(Constants.JsonContentType);
            if (requestModel.Method == HttpMethod.Get)
            {
                requestModel.Method = HttpMethod.Post;
            }
        }
        return next();
    }
}
