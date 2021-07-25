using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HTTPie.Abstractions;
using HTTPie.Models;
using Microsoft.Extensions.Primitives;
using WeihanLi.Extensions;

namespace HTTPie.Implement
{
    public class OutputFormatter : IOutputFormatter
    {
        private readonly Dictionary<string, string> _supportedFormat = new()
        {
            {"--header", "output response headers only"},
            {"--body", "output response headers and response body"},
            {"--bodyOnly", "output response body only"},
            {"--full", "output request/response, response headers and response body"}
        };

        public Dictionary<string, string> SupportedParameters()
        {
            return _supportedFormat;
        }

        public string GetOutput(HttpRequestModel requestModel, HttpResponseModel responseModel)
        {
            var outputFormat = OutputFormat.ResponseStatus | OutputFormat.ResponseHeaders;
            if (requestModel.RawInput.Contains("--full"))
            {
                outputFormat |= OutputFormat.RequestStatus;
                outputFormat |= OutputFormat.RequestHeaders;
                outputFormat |= OutputFormat.RequestBody;
                outputFormat |= OutputFormat.ResponseBody;
            }
            if (requestModel.RawInput.Contains("--bodyOnly")) outputFormat = OutputFormat.ResponseBody;

            var output = new StringBuilder();
            output.AppendLineIf(GetRequestVersionAndStatus(requestModel),
                outputFormat.HasFlag(OutputFormat.RequestStatus));
            output.AppendLineIf(GetHeadersString(requestModel.Headers),
                outputFormat.HasFlag(OutputFormat.RequestHeaders));
            if (outputFormat.HasFlag(OutputFormat.RequestBody) && !string.IsNullOrEmpty(requestModel.Body))
            {
                output.AppendLineIf(string.Empty, output.Length > 0);
                output.AppendLine(requestModel.Body);
            }
            output.AppendLineIf(string.Empty, output.Length > 0);

            var requestLength = output.Length;
            output.AppendLineIf(GetResponseVersionAndStatus(responseModel),
                outputFormat.HasFlag(OutputFormat.ResponseStatus));
            output.AppendLineIf(GetHeadersString(responseModel.Headers),
                outputFormat.HasFlag(OutputFormat.ResponseHeaders));
            if (outputFormat.HasFlag(OutputFormat.ResponseBody) && !string.IsNullOrEmpty(responseModel.Body))
            {
                output.AppendLineIf(string.Empty, output.Length > requestLength);
                output.AppendLine(responseModel.Body);
            }

            return output.ToString();
        }

        private static string GetRequestVersionAndStatus(HttpRequestModel requestModel)
        {
            var uri = new Uri(requestModel.Url);
            return
                $@"{requestModel.Method.Method.ToUpper()} {requestModel.Url} HTTP/{requestModel.HttpVersion.ToString(2)}
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