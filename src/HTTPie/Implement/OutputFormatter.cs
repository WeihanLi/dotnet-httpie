using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using Microsoft.Extensions.Primitives;
using System.Text;

namespace HTTPie.Implement
{
    public class OutputFormatter : IOutputFormatter
    {
        public static readonly Option QuietOption = new(new[] { "--quiet", "-q" }, "quiet mode, output nothing");
        public static readonly Option OfflineOption = new("--offline", "offline mode, would not send the request, just print request info");
        public static readonly Option OutputHeadersOption = new(new[] { "-h", "--headers" }, "output response headers only");
        public static readonly Option OutputBodyOption = new(new[] { "-b", "--body" }, "output response headers and response body only");
        public static readonly Option OutputVerboseOption = new(new[] { "-v", "--verbose" }, "output request/response, response headers and response body");
        public static readonly Option<string> OutputPrintModeOption = new(new[] { "-p", "--print" }, "print mode, output specific info,H:request headers,B:request body,h:response headers,b:response body");

        public ICollection<Option> SupportedOptions() => new HashSet<Option>()
        {
            OfflineOption,
            QuietOption,
            OutputHeadersOption,
            OutputBodyOption,
            OutputVerboseOption,
            OutputPrintModeOption,
        };

        public static OutputFormat GetOutputFormat(HttpContext httpContext)
        {
            if (httpContext.TryGetProperty<OutputFormat>(Constants.ResponseOutputFormatPropertyName, out var outputFormat))
            {
                return outputFormat;
            }
            outputFormat = OutputFormat.ResponseInfo;

            var requestModel = httpContext.Request;
            if (requestModel.ParseResult.HasOption(QuietOption))
            {
                outputFormat = OutputFormat.None;
            }
            else if (requestModel.ParseResult.HasOption(OfflineOption))
            {
                outputFormat = OutputFormat.RequestInfo;
            }
            else if (requestModel.ParseResult.HasOption(OutputVerboseOption))
            {
                outputFormat = OutputFormat.All;
            }
            else if (requestModel.ParseResult.HasOption(OutputBodyOption))
            {
                outputFormat = OutputFormat.ResponseBody;
            }
            else if (requestModel.ParseResult.HasOption(OutputHeadersOption))
            {
                outputFormat = OutputFormat.ResponseHeaders;
            }
            else if (requestModel.ParseResult.HasOption(OutputPrintModeOption))
            {
                var mode = requestModel.ParseResult.ValueForOption(OutputPrintModeOption);
                if (!string.IsNullOrEmpty(mode))
                    outputFormat = mode.Select(m => m switch
                    {
                        'H' => OutputFormat.RequestHeaders,
                        'B' => OutputFormat.RequestBody,
                        'h' => OutputFormat.ResponseHeaders,
                        'b' => OutputFormat.ResponseBody,
                        _ => OutputFormat.None
                    })
                        .Aggregate(OutputFormat.None, (current, format) => current | format);
            }
            httpContext.SetProperty(Constants.ResponseOutputFormatPropertyName, outputFormat);
            return outputFormat;
        }


        public string GetOutput(HttpContext httpContext)
        {
            var outputFormat = GetOutputFormat(httpContext);
            var requestModel = httpContext.Request;
            var output = new StringBuilder();
            if (outputFormat.HasFlag(OutputFormat.RequestHeaders))
            {
                output.AppendLine(GetRequestVersionAndStatus(requestModel));
                output.AppendLine(GetHeadersString(requestModel.Headers));
            }
            if (outputFormat.HasFlag(OutputFormat.RequestBody) && !string.IsNullOrEmpty(requestModel.Body))
            {
                output.AppendLineIf(string.Empty, output.Length > 0);
                output.AppendLine(requestModel.Body);
            }

            output.AppendLineIf(string.Empty, output.Length > 0 && (outputFormat & OutputFormat.ResponseInfo) != 0);

            var requestLength = output.Length;
            var responseModel = httpContext.Response;
            if (outputFormat.HasFlag(OutputFormat.ResponseHeaders))
            {
                output.AppendLine(GetResponseVersionAndStatus(responseModel));
                output.AppendLine(GetHeadersString(responseModel.Headers));
            }
            if (outputFormat.HasFlag(OutputFormat.ResponseBody) && !string.IsNullOrEmpty(responseModel.Body))
            {
                output.AppendLineIf(string.Empty, output.Length > requestLength);
                output.AppendLine(responseModel.Body);
            }

            return output.ToString();
        }

        private string GetRequestVersionAndStatus(HttpRequestModel requestModel)
        {
            var uri = new Uri(requestModel.Url);
            return
                $@"{requestModel.Method.Method.ToUpper()} {uri.PathAndQuery} HTTP/{requestModel.HttpVersion.ToString(2)}
Host: {uri.Host}{(uri.IsDefaultPort ? "" : $":{uri.Port}")}
Schema: {uri.Scheme}";
        }

        private static string GetResponseVersionAndStatus(HttpResponseModel responseModel)
        {
            return
                $"HTTP/{responseModel.HttpVersion.ToString(2)} {(int)responseModel.StatusCode} {responseModel.StatusCode}";
        }

        private static string GetHeadersString(IDictionary<string, StringValues> headers)
        {
            return
                $"{headers.Select(h => $"{h.Key}: {h.Value}").OrderBy(h => h).StringJoin(Environment.NewLine)}";
        }
    }
}