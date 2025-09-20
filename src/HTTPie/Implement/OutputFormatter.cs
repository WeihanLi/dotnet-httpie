// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using MathNet.Numerics.Statistics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Text;
using System.Text.Json.Nodes;
using WeihanLi.Common.Extensions;

namespace HTTPie.Implement;

[Flags]
public enum PrettyOptions
{
    None = 0,
    Format = 1,
    Style = 2,
    All = 3
}

public sealed class OutputFormatter(IServiceProvider serviceProvider, ILogger<OutputFormatter> logger)
    : IOutputFormatter
{
    private static readonly Option<PrettyOptions> PrettyOption = new("--pretty")
    {
        Description = "pretty output",
        DefaultValueFactory = _ => PrettyOptions.All
    };

    private static readonly Option<bool> QuietOption = new("--quiet", "-q")
    {
        Description = "quiet mode, output nothing"
    };

    public static readonly Option<bool> OfflineOption =
        new("--offline", "--dry-run")
        {
            Description = "offline mode, would not send the request, just print request info"
        };

    private static readonly Option<bool> OutputHeadersOption =
        new("-h", "--headers") { Description = "output response headers only" };

    private static readonly Option<bool> OutputBodyOption =
        new("-b", "--body") { Description = "output response headers and response body only" };

    private static readonly Option<bool> OutputVerboseOption = new("-v", "--verbose")
    {
        Description = "output all request/response info, including request/response headers,properties,body"
    };

    private static readonly Option<string> OutputPrintModeOption = new("-p", "--print")
    {
        Description =
            "print mode, output specific info,H:request headers,B:request body,h:response headers,b:response body"
    };

    public Option[] SupportedOptions() =>
    [
        OfflineOption, QuietOption, OutputHeadersOption, OutputBodyOption, OutputVerboseOption,
        OutputPrintModeOption, PrettyOption
    ];

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
            var mode = requestModel.ParseResult.GetValue(OutputPrintModeOption);
            if (!string.IsNullOrEmpty(mode))
                outputFormat = mode.Select(m => m switch
                    {
                        'H' => OutputFormat.RequestHeaders,
                        'B' => OutputFormat.RequestBody,
                        'h' => OutputFormat.ResponseHeaders,
                        'b' => OutputFormat.ResponseBody,
                        'p' => OutputFormat.Properties,
                        _ => OutputFormat.None
                    })
                    .Aggregate(OutputFormat.None, (current, format) => current | format);
        }

        httpContext.SetProperty(Constants.ResponseOutputFormatPropertyName, outputFormat);
        return outputFormat;
    }

    public async Task<string> GetOutput(HttpContext httpContext)
    {
        var isLoadTest = httpContext.GetFlag(Constants.FlagNames.IsLoadTest);
        var outputFormat = GetOutputFormat(httpContext);
        return isLoadTest
            ? await GetLoadTestOutput(httpContext, outputFormat).ConfigureAwait(false)
            : GetCommonOutput(httpContext, outputFormat);
    }

    private static string GetCommonOutput(HttpContext httpContext, OutputFormat outputFormat)
    {
        var requestModel = httpContext.Request;
        var responseModel = httpContext.Response;

        var hasValidResponse = (int)responseModel.StatusCode > 0;
        // The HttpVersion in ResponseMessage is the version used for the request after negotiation
        var requestVersion = hasValidResponse
            ? httpContext.Response.HttpVersion
            : httpContext.Request.HttpVersion ?? new Version(2, 0)
            ;
        var prettyOption = requestModel.ParseResult.GetValue(PrettyOption);
        var output = new StringBuilder();
        if (outputFormat.HasFlag(OutputFormat.RequestHeaders))
        {
            output.AppendLine(GetRequestVersionAndStatus(httpContext, requestVersion));
            output.AppendLine(GetHeadersString(requestModel.Headers));
            if (outputFormat.HasFlag(OutputFormat.Properties) && requestModel.Properties.Count > 0)
            {
                output.AppendLine(GetPropertiesString(requestModel.Properties));
            }
        }

        if (outputFormat.HasFlag(OutputFormat.RequestBody) && !string.IsNullOrEmpty(requestModel.Body))
        {
            output.AppendLineIf(string.Empty, output.Length > 0);
            output.AppendLine(Prettify(requestModel.Body, prettyOption));
        }

        var requestLength = output.Length;
        if ((int)responseModel.StatusCode <= 0) return output.ToString();

        output.AppendLineIf(string.Empty, output.Length > 0 && (outputFormat & OutputFormat.ResponseInfo) != 0);

        if (outputFormat.HasFlag(OutputFormat.ResponseHeaders))
        {
            output.AppendLine(GetResponseVersionAndStatus(responseModel));
            output.AppendLine(GetHeadersString(responseModel.Headers));
            if (outputFormat.HasFlag(OutputFormat.Properties) && responseModel.Properties.Count > 0)
            {
                output.AppendLine(GetPropertiesString(responseModel.Properties));
            }
        }

        if (outputFormat.HasFlag(OutputFormat.ResponseBody) && !string.IsNullOrEmpty(responseModel.Body))
        {
            output.AppendLineIf(string.Empty, output.Length > requestLength);

            output.AppendLine(Prettify(responseModel.Body, prettyOption));
        }

        return output.ToString();
    }

    private async Task<string> GetLoadTestOutput(HttpContext httpContext, OutputFormat outputFormat)
    {
        httpContext.TryGetProperty(Constants.ResponseListPropertyName,
            out HttpResponseModel[]? responseList);
        if (responseList is not { Length: > 0 })
            return GetCommonOutput(httpContext, outputFormat);

        var durationInMs = responseList
            .Where(x => x.Elapsed > TimeSpan.Zero)
            .Select(r => r.Elapsed.TotalMilliseconds)
            .OrderBy(x => x)
            .ToArray();
        var totalElapsed = httpContext.Response.Elapsed.TotalMilliseconds;
        var reportModel = new LoadTestReportModel()
        {
            TotalRequestCount = responseList.Length,
            SuccessRequestCount = responseList.Count(x => x.IsSuccessStatusCode),
            Average = durationInMs.Average(),
            TotalElapsed = totalElapsed,
            Min = SortedArrayStatistics.Minimum(durationInMs),
            Max = SortedArrayStatistics.Maximum(durationInMs),
            Median = SortedArrayStatistics.Median(durationInMs),
            P99 = SortedArrayStatistics.Quantile(durationInMs, 0.99),
            P95 = SortedArrayStatistics.Quantile(durationInMs, 0.95),
            P90 = SortedArrayStatistics.Quantile(durationInMs, 0.90),
            P75 = SortedArrayStatistics.Quantile(durationInMs, 0.75),
            P50 = SortedArrayStatistics.Quantile(durationInMs, 0.50),
        };

        try
        {
            var exporterSelector = serviceProvider.GetRequiredService<ILoadTestExporterSelector>();
            var exporter = exporterSelector.Select();
            if (exporter is not null)
                await exporter.Export(httpContext, responseList);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Export load test result failed");
        }

        return $@"{GetCommonOutput(httpContext, outputFormat & OutputFormat.RequestInfo)}
Total requests: {reportModel.TotalRequestCount}({reportModel.TotalElapsed} ms), successCount: {reportModel.SuccessRequestCount}({reportModel.SuccessRequestRate}%), failedCount: {reportModel.FailRequestCount}

Request duration:
Requests per second: {reportModel.RequestsPerSecond}
{nameof(reportModel.Min)}: {reportModel.Min} ms
{nameof(reportModel.Max)}: {reportModel.Max} ms
{nameof(reportModel.Median)}: {reportModel.Median} ms
{nameof(reportModel.Average)}: {reportModel.Average} ms
{nameof(reportModel.P99)}: {reportModel.P99} ms
{nameof(reportModel.P95)}: {reportModel.P95} ms
{nameof(reportModel.P90)}: {reportModel.P90} ms
{nameof(reportModel.P75)}: {reportModel.P75} ms
{nameof(reportModel.P50)}: {reportModel.P50} ms
";
    }

    private static string Prettify(string body, PrettyOptions prettyOption)
    {
        if (prettyOption == PrettyOptions.None || string.IsNullOrWhiteSpace(body))
            return body;
        try
        {
            if (body.Length > 2 &&
                (body[0] == '[' && body[^1] == ']'
                 || body[0] == '{' && body[^1] == '}')
               )
            {
                var formattedJson = JsonNode.Parse(body)?.ToJsonString(Helpers.JsonSerializerOptions)
                                    ?? body;
                return formattedJson;
            }
        }
        catch
        {
            // ignore
        }

        return body;
    }

    private static string GetRequestVersionAndStatus(HttpContext httpContext, Version requestVersion)
    {
        var requestModel = httpContext.Request;
        var uri = new Uri(requestModel.Url);
        return
            $"""
             {requestModel.Method.Method.ToUpper()} {uri.PathAndQuery} {requestVersion.NormalizeHttpVersion()}
             Host: {uri.Host}{(uri.IsDefaultPort ? "" : $":{uri.Port}")}
             Schema: {uri.Scheme}
             Url: {requestModel.Url}
             """;
    }

    private static string GetResponseVersionAndStatus(HttpResponseModel responseModel)
    {
        return
            $"{responseModel.HttpVersion.NormalizeHttpVersion()} {(int)responseModel.StatusCode} {responseModel.StatusCode}";
    }

    private static string GetHeadersString(IDictionary<string, StringValues> headers)
    {
        return
            $"{headers.Select(h => $"{h.Key}: {h.Value}").OrderBy(h => h).StringJoin(Environment.NewLine)}";
    }

    private static string GetPropertiesString(Dictionary<string, string> headers)
    {
        return
            $"{headers.Select(h => $"[{h.Key}]: {h.Value}")
                .OrderBy(h => h)
                .StringJoin(Environment.NewLine)}";
    }
}
