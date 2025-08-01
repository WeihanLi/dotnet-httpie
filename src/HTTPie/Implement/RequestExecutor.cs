﻿// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
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

    public Option[] SupportedOptions()
    {
        return [TimeoutOption, IterationOption, DurationOption, VirtualUserOption];
    }

    public async ValueTask ExecuteAsync(HttpContext httpContext)
    {
        var requestModel = httpContext.Request;
        await requestPipeline(requestModel);
        LogRequestModel(requestModel);
        if (requestModel.ParseResult.HasOption(OutputFormatter.OfflineOption))
        {
            RequestShouldBeOffline();
            return;
        }

        using var httpClientHandler = new HttpClientHandler();
        httpClientHandler.AllowAutoRedirect = false;
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
            httpContext.Response = await InvokeRequest(client, httpContext, httpContext.RequestCancelled);
            await responsePipeline(httpContext);
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
