// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Middleware;
using HTTPie.Models;
using HTTPie.Utilities;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text;
using WeihanLi.Common.Extensions;

namespace HTTPie.Implement;

public sealed partial class RequestExecutor(
    IRequestMapper requestMapper,
    IResponseMapper responseMapper,
    Func<HttpClientHandler, Task> httpHandlerPipeline,
    Func<HttpRequestModel, Task> requestPipeline,
    Func<HttpContext, Task> responsePipeline,
    ILogger logger
    ) : IRequestExecutor
{
#if NET8_0
    // needed for net8.0 only
    private readonly ILogger _logger = logger;
#endif

    private static readonly Option<double> TimeoutOption = new("--timeout")
    {
        Description = "Request timeout in seconds"
    };

    private static readonly Option<int> IterationOption =
        new("-n", "--iteration")
        {
            Description = "Request iteration",
            DefaultValueFactory = _ => 1
        };

    private static readonly Option<int> VirtualUserOption =
        new("--vu", "--vus", "--virtual-users")
        {
            Description = "Virtual users",
            DefaultValueFactory = _ => 1
        };

    private static readonly Option<string> DurationOption = new("--duration")
    {
        Description = "Request duration, 10s/1m or 00:01:00 ..."
    };

    private static readonly Option<bool> StreamOption = new("--stream", "-S")
    {
        Description = "Stream response body output as it arrives (text responses only)"
    };

    public Option[] SupportedOptions()
    {
        return [TimeoutOption, IterationOption, DurationOption, VirtualUserOption, StreamOption];
    }

    public async ValueTask ExecuteAsync(HttpContext httpContext)
    {
        var requestModel = httpContext.Request;
        await requestPipeline(requestModel);
        LogRequestModel(requestModel);

        // Set streaming mode flag early, before any early returns
        var streamMode = requestModel.ParseResult.HasOption(StreamOption);
        httpContext.UpdateFlag(Constants.FlagNames.IsStreamingMode, streamMode);

        if (requestModel.ParseResult.HasOption(OutputFormatter.OfflineOption))
        {
            RequestShouldBeOffline();
            return;
        }

        using var httpClientHandler = Helpers.GetHttpClientHandler();
        await httpHandlerPipeline(httpClientHandler);
        using var client = new HttpClient(httpClientHandler);
        var timeout = requestModel.ParseResult.GetValue(TimeoutOption);
        if (timeout > 0)
            client.Timeout = TimeSpan.FromSeconds(timeout);
        var iteration = requestModel.ParseResult.GetValue(IterationOption);
        var virtualUsers = requestModel.ParseResult.GetValue(VirtualUserOption);
        var durationValue = requestModel.ParseResult.GetValue(DurationOption);
        var duration = TimeSpan.Zero;
        if (!string.IsNullOrEmpty(durationValue))
        {
            if (!char.IsNumber(durationValue[^1]) && double.TryParse(durationValue[..^1], out var value))
            {
                duration = durationValue[^1].ToLower() switch
                {
                    's' => TimeSpan.FromSeconds(value),
                    'm' => TimeSpan.FromMinutes(value),
                    'h' => TimeSpan.FromHours(value),
                    _ => TimeSpan.Zero
                };
            }

            if (duration == TimeSpan.Zero)
            {
                TimeSpan.TryParse(durationValue, out duration);
            }
        }

        var isLoadTest = duration > TimeSpan.Zero || iteration > 1 || virtualUsers > 1;
        httpContext.UpdateFlag(Constants.FlagNames.IsLoadTest, isLoadTest);

        if (isLoadTest)
        {
            await InvokeLoadTest(client);
        }
        else
        {
            if (streamMode)
            {
                await InvokeStreamingRequest(client, httpContext, httpContext.RequestCancelled);
                // Mark that streaming actually completed
                httpContext.UpdateFlag(Constants.FlagNames.StreamingCompleted, true);
            }
            else
            {
                httpContext.Response = await InvokeRequest(client, httpContext, httpContext.RequestCancelled);
                await responsePipeline(httpContext);
            }
        }

        async Task InvokeLoadTest(HttpClient httpClient)
        {
            var responseList = new ConcurrentBag<HttpResponseModel>();
            Func<int, CancellationToken, ValueTask> action;
            if (duration > TimeSpan.Zero)
            {
                action = async (_, _) =>
                {
                    using var cts = new CancellationTokenSource(duration);
                    using var linkedCts =
                        CancellationTokenSource.CreateLinkedTokenSource(cts.Token, httpContext.RequestCancelled);
                    while (!linkedCts.IsCancellationRequested)
                    {
                        responseList.Add(
                            await InvokeRequest(httpClient, httpContext, linkedCts.Token)
                        );
                    }
                };
            }
            else
            {
                action = async (_, _) =>
                {
                    do
                    {
                        responseList.Add(await InvokeRequest(httpClient, httpContext, httpContext.RequestCancelled));
                    } while (--iteration > 0);
                };
            }

            var startTimestamp = Stopwatch.GetTimestamp();
            if (virtualUsers > 1)
            {
                await Parallel.ForEachAsync(
                    Enumerable.Range(1, virtualUsers),
                    new ParallelOptions { MaxDegreeOfParallelism = virtualUsers },
                    action
                );
            }
            else
            {
                await action(default, default);
            }

            httpContext.Response.Elapsed = ProfilerHelper.GetElapsedTime(startTimestamp);
            httpContext.SetProperty(Constants.ResponseListPropertyName, responseList.ToArray());
        }
    }

    private async Task<HttpResponseModel> InvokeRequest(HttpClient httpClient, HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var responseModel = new HttpResponseModel();
        try
        {
            using var requestMessage = await requestMapper.ToRequestMessage(httpContext);
            LogRequestMessage(requestMessage);
            httpContext.Request.Timestamp = DateTimeOffset.Now;
            var startTime = Stopwatch.GetTimestamp();
            using var responseMessage = await httpClient.SendAsync(requestMessage, cancellationToken);
            var elapsed = ProfilerHelper.GetElapsedTime(startTime);
            LogResponseMessage(responseMessage);
            responseModel = await responseMapper.ToResponseModel(responseMessage);
            responseModel.Elapsed = elapsed;
            responseModel.Timestamp = httpContext.Request.Timestamp.Add(elapsed);
            LogRequestDuration(httpContext.Request.Url, httpContext.Request.Method, responseModel.StatusCode, elapsed);
        }
        catch (OperationCanceledException operationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            LogRequestCancelled(operationCanceledException);
        }
        catch (Exception exception)
        {
            LogException(exception);
        }

        return responseModel;
    }

    private async Task InvokeStreamingRequest(HttpClient httpClient, HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            using var requestMessage = await requestMapper.ToRequestMessage(httpContext);
            LogRequestMessage(requestMessage);
            httpContext.Request.Timestamp = DateTimeOffset.Now;
            var startTime = Stopwatch.GetTimestamp();

            // Send request with HttpCompletionOption.ResponseHeadersRead to start streaming
            using var responseMessage = await httpClient.SendAsync(requestMessage,
                HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            var elapsed = ProfilerHelper.GetElapsedTime(startTime);
            LogResponseMessage(responseMessage);

            // Build response model with headers only
            var responseModel = new HttpResponseModel
            {
                RequestHttpVersion = responseMessage.RequestMessage?.Version,
                HttpVersion = responseMessage.Version,
                StatusCode = responseMessage.StatusCode,
                Headers = responseMessage.Headers
                    .Union(responseMessage.Content.Headers)
                    .ToDictionary(x => x.Key, x => new Microsoft.Extensions.Primitives.StringValues(x.Value.ToArray())),
                Timestamp = httpContext.Request.Timestamp.Add(elapsed),
                Elapsed = elapsed
            };

            httpContext.Response = responseModel;

            // Run response pipeline for headers (e.g., to set properties)
            await responsePipeline(httpContext);

            // Check if we should stream based on content type
            var isTextResponse = IsTextResponse(responseMessage);
            var downloadMode = httpContext.Request.ParseResult.HasOption(DownloadMiddleware.DownloadOption);

            if (!isTextResponse || downloadMode)
            {
                // Fall back to buffered mode for binary content or downloads
                responseModel.Bytes = await responseMessage.Content.ReadAsByteArrayAsync(cancellationToken);
                if (isTextResponse)
                {
                    try
                    {
                        responseModel.Body = responseModel.Bytes.GetString();
                    }
                    catch (Exception ex)
                    {
                        // Unable to decode response as text, likely encoding issue
                        LogException(ex);
                    }
                }
                return;
            }

            // Output headers immediately
            var outputFormat = OutputFormatter.GetOutputFormat(httpContext);

            if (outputFormat.HasFlag(OutputFormat.ResponseHeaders) || outputFormat == OutputFormat.ResponseInfo)
            {
                var headerOutput = GetStreamingHeaderOutput(httpContext);
                await Console.Out.WriteLineAsync(headerOutput);
            }

            // Stream the body
            if (outputFormat.HasFlag(OutputFormat.ResponseBody) || outputFormat == OutputFormat.ResponseInfo)
            {
                await using var stream = await responseMessage.Content.ReadAsStreamAsync(cancellationToken);

                // Use UTF-8 encoding for AOT compatibility
                // Most modern APIs use UTF-8, and this matches the behavior of ResponseMapper
                using var reader = new StreamReader(stream, Encoding.UTF8);

                var bodyBuilder = new StringBuilder();
                string? line;
                while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
                {
                    await Console.Out.WriteLineAsync(line);
                    bodyBuilder.AppendLine(line);
                }

                // Store the body for potential later use
                responseModel.Body = bodyBuilder.ToString();
                responseModel.Bytes = Encoding.UTF8.GetBytes(responseModel.Body);
            }
            else
            {
                // Even if not outputting body, we need to consume it
                responseModel.Bytes = await responseMessage.Content.ReadAsByteArrayAsync(cancellationToken);
                responseModel.Body = responseModel.Bytes.GetString();
            }

            LogRequestDuration(httpContext.Request.Url, httpContext.Request.Method, responseModel.StatusCode, elapsed);
        }
        catch (OperationCanceledException operationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            LogRequestCancelled(operationCanceledException);
        }
        catch (Exception exception)
        {
            LogException(exception);
        }
    }

    private static bool IsTextResponse(HttpResponseMessage response)
    {
        // When ContentType is null, assume text response for compatibility
        // This matches the behavior of ResponseMapper.IsTextResponse
        if (response.Content.Headers.ContentType?.MediaType is null)
        {
            return true;
        }
        var contentType = response.Content.Headers.ContentType;
        var mediaType = contentType.MediaType;
        var isTextContent = mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
            || mediaType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase)
            || mediaType.StartsWith("application/xml", StringComparison.OrdinalIgnoreCase)
            || mediaType.StartsWith("application/javascript", StringComparison.OrdinalIgnoreCase)
            ;
        return isTextContent;
    }

    private string GetStreamingHeaderOutput(HttpContext httpContext)
    {
        var responseModel = httpContext.Response;
        var requestModel = httpContext.Request;
        var outputFormat = OutputFormatter.GetOutputFormat(httpContext);
        var output = new StringBuilder();

        // Request headers if needed
        if (outputFormat.HasFlag(OutputFormat.RequestHeaders))
        {
            var requestVersion = responseModel.HttpVersion;
            var uri = new Uri(requestModel.Url);
            output.AppendLine($"{requestModel.Method.Method.ToUpper()} {uri.PathAndQuery} {requestVersion.NormalizeHttpVersion()}");
            output.AppendLine($"Host: {uri.Host}{(uri.IsDefaultPort ? "" : $":{uri.Port}")}");
            output.AppendLine($"Schema: {uri.Scheme}");
            output.AppendLine($"[Url]: {requestModel.Url}");
            output.AppendLine(string.Join(Environment.NewLine,
                requestModel.Headers.Select(h => $"{h.Key}: {h.Value}").OrderBy(h => h)));

            if (outputFormat.HasFlag(OutputFormat.Properties) && requestModel.Properties.Count > 0)
            {
                output.AppendLine(string.Join(Environment.NewLine,
                    requestModel.Properties.Select(h => $"[{h.Key}]: {h.Value}").OrderBy(h => h)));
            }
            output.AppendLine();
        }

        // Response headers
        output.AppendLine($"{responseModel.HttpVersion.NormalizeHttpVersion()} {(int)responseModel.StatusCode} {responseModel.StatusCode}");
        output.AppendLine(string.Join(Environment.NewLine,
            responseModel.Headers.Select(h => $"{h.Key}: {h.Value}").OrderBy(h => h)));

        if (outputFormat.HasFlag(OutputFormat.Properties) && responseModel.Properties.Count > 0)
        {
            output.AppendLine(string.Join(Environment.NewLine,
                responseModel.Properties.Select(h => $"[{h.Key}]: {h.Value}").OrderBy(h => h)));
        }

        output.AppendLine();
        return output.ToString();
    }

    [LoggerMessage(Level = LogLevel.Debug, EventId = 0, Message = "Request should be offline, wont send request")]
    private partial void RequestShouldBeOffline();

    [LoggerMessage(Level = LogLevel.Debug, EventId = 2, EventName = "RequestModel",
        Message = "RequestModel info: {requestModel}")]
    private partial void LogRequestModel(HttpRequestModel requestModel);

    [LoggerMessage(Level = LogLevel.Debug, EventId = 3, EventName = "RequestMessage",
        Message = "Request message: {requestMessage}")]
    private partial void LogRequestMessage(HttpRequestMessage requestMessage);

    [LoggerMessage(Level = LogLevel.Debug, EventId = 4, EventName = "ResponseMessage",
        Message = "Response message: {responseMessage}")]
    private partial void LogResponseMessage(HttpResponseMessage responseMessage);

    [LoggerMessage(Level = LogLevel.Information, EventId = 1000,
        Message = "Request {url}({method}), {responseStatusCode} duration: {duration}")]
    private partial void LogRequestDuration(string url, HttpMethod method, HttpStatusCode responseStatusCode,
        TimeSpan duration);

    [LoggerMessage(Level = LogLevel.Error, EventId = 1001, Message = "Send httpRequest exception")]
    private partial void LogException(Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, EventId = 1002, Message = "Request cancelled")]
    private partial void LogRequestCancelled(Exception exception);
}
