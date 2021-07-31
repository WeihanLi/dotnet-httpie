using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HTTPie.Abstractions;
using HTTPie.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using WeihanLi.Extensions;

namespace HTTPie.Implement
{
    public class OutputFormatter : IOutputFormatter
    {
        private readonly ILogger _logger;

        private readonly Dictionary<string, string> _supportedFormat = new()
        {
            {"--headers, -h", "output response headers only"},
            {"--body, -b", "output response headers and response body only"},
            {"--verbose, -v", "output request/response, response headers and response body"},
            {"--quiet, -q", "quiet mode, output nothing"},
            {
                "--print, -p",
                "print mode, output specific info,H:request headers,B:request body,h:response headers,b:response body"
            },
            {"--offline", "offline mode, would not send the request, print request info"}
        };

        public OutputFormatter(ILogger logger)
        {
            _logger = logger;
        }

        public Dictionary<string, string> SupportedParameters()
        {
            return _supportedFormat;
        }

        public string GetOutput(HttpRequestModel requestModel, HttpResponseModel responseModel)
        {
            if (requestModel.RawInput.Contains("--quiet") || requestModel.RawInput.Contains("-q")) return string.Empty;
            var outputFormat = OutputFormat.ResponseInfo;
            if (requestModel.RawInput.Contains("--offline"))
            {
                outputFormat = OutputFormat.RequestInfo;
            }
            else if (requestModel.RawInput.Contains("--verbose") || requestModel.RawInput.Contains("-v"))
            {
                outputFormat = OutputFormat.All;
            }
            else if (requestModel.RawInput.Contains("--body") || requestModel.RawInput.Contains("-b"))
            {
                outputFormat = OutputFormat.ResponseBody;
            }
            else if (requestModel.RawInput.Contains("--headers") || requestModel.RawInput.Contains("-h"))
            {
                outputFormat = OutputFormat.ResponseHeaders;
            }
            else if (requestModel.RawInput.Any(x => x.StartsWith("--print=") || x.StartsWith("-p=")))
            {
                var mode = requestModel.RawInput.FirstOrDefault(x => x.StartsWith("--print="))?["--print=".Length..]
                           ?? requestModel.RawInput.FirstOrDefault(x =>
                               x.StartsWith("-p="))?["-p=".Length..];
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
            _logger.LogDebug($"RequestUrl: {requestModel.Url}");
            var uri = new Uri(requestModel.Url);
            return
                $@"{requestModel.Method.Method.ToUpper()} {uri.PathAndQuery} HTTP/{requestModel.HttpVersion.ToString(2)}
Host: {uri.Host}
Schema: {uri.Scheme}";
        }

        private static string GetResponseVersionAndStatus(HttpResponseModel responseModel)
        {
            return
                $"HTTP/{responseModel.HttpVersion.ToString(2)} {(int) responseModel.StatusCode} {responseModel.StatusCode.ToString()}";
        }

        private static string GetHeadersString(IDictionary<string, StringValues> headers)
        {
            return
                $"{headers.Select(h => $"{h.Key}: {h.Value.ToString()}").OrderBy(h => h).StringJoin(Environment.NewLine)}";
        }
    }
}