﻿// Copyright (c) Weihan Li.All rights reserved.
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

public sealed class OutputFormatter : IOutputFormatter
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutputFormatter> _logger;

    private static readonly Option<PrettyOptions> PrettyOption = new("--pretty", () => PrettyOptions.All,
        "pretty output");

    private static readonly Option<bool> QuietOption = new(new[] { "--quiet", "-q" }, "quiet mode, output nothing");

    public static readonly Option<bool> OfflineOption =
        new("--offline", "offline mode, would not send the request, just print request info");

    private static readonly Option<bool> OutputHeadersOption =
        new(new[] { "-h", "--headers" }, "output response headers only");

    private static readonly Option<bool> OutputBodyOption =
        new(new[] { "-b", "--body" }, "output response headers and response body only");

    private static readonly Option<bool> OutputVerboseOption = new(new[] { "-v", "--verbose" },
        "output request/response, response headers and response body");

    private static readonly Option<string> OutputPrintModeOption = new(new[] { "-p", "--print" },
        "print mode, output specific info,H:request headers,B:request body,h:response headers,b:response body");


    public OutputFormatter(IServiceProvider serviceProvider, ILogger<OutputFormatter> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Option[] SupportedOptions() => new Option[]
    {
        OfflineOption, QuietOption, OutputHeadersOption, OutputBodyOption, OutputVerboseOption,
        OutputPrintModeOption, PrettyOption,
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
            var mode = requestModel.ParseResult.GetValueForOption(OutputPrintModeOption);
            if (!string.IsNullOrEmpty(mode))
                outputFormat = mode.Select(m => m switch
                    {
                        'H' => OutputFormat.RequestHeaders,
                        'B' => OutputFormat.RequestBody,
                        'h' => OutputFormat.ResponseHeaders,
                        'b' => OutputFormat.ResponseBody,
                        't' => OutputFormat.Timestamp,
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
        var prettyOption = requestModel.ParseResult.GetValueForOption(PrettyOption);
        var output = new StringBuilder();
        if (outputFormat.HasFlag(OutputFormat.RequestHeaders))
        {
            output.AppendLine(GetRequestVersionAndStatus(requestModel));
            output.AppendLine(GetHeadersString(requestModel.Headers));
        }

        if (outputFormat.HasFlag(OutputFormat.RequestBody) && !string.IsNullOrEmpty(requestModel.Body))
        {
            output.AppendLineIf(string.Empty, output.Length > 0);
            output.AppendLine(Prettify(requestModel.Body, prettyOption));
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
            var exporterSelector = _serviceProvider.GetRequiredService<ILoadTestExporterSelector>();
            var exporter = exporterSelector.Select();
            if (exporter is not null)
                await exporter.Export(httpContext, responseList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export load test result failed");
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
            var formattedJson = JsonNode.Parse(body)?.ToJsonString(Helpers.JsonSerializerOptions)
                                ?? body;
            return formattedJson;
        }
        catch (Exception)
        {
            return body;
        }
    }

    private static string GetRequestVersionAndStatus(HttpRequestModel requestModel)
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
