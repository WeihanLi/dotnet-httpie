﻿// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using WeihanLi.Common.Http;

namespace HTTPie.Implement;

public partial class RequestExecutor : IRequestExecutor
{
    private readonly Func<HttpClientHandler, Task> _httpHandlerPipeline;
    private readonly ILogger _logger;
    private readonly IRequestMapper _requestMapper;
    private readonly Func<HttpRequestModel, Task> _requestPipeline;
    private readonly IResponseMapper _responseMapper;
    private readonly Func<HttpContext, Task> _responsePipeline;

    public static readonly Option<double> TimeoutOption = new("--timeout", "Request timeout in seconds");
    public static readonly Option<int> IterationOption = new(new[] { "-n", "--iteration" }, () => 1, "Request iteration");
    public static readonly Option<int> VirtualUserOption = new(new[] { "--vu", "--vus", "--virtual-users" }, () => 1, "Virtual users");
    public static readonly Option<string> DurationOption = new(new[] { "-d", "--duration" }, "Duration");

    public Option[] SupportedOptions()
    {
        return new Option[]
        {
            TimeoutOption, IterationOption, DurationOption, VirtualUserOption
        };
    }

    public RequestExecutor(
        IRequestMapper requestMapper,
        IResponseMapper responseMapper,
        Func<HttpClientHandler, Task> httpHandlerPipeline,
        Func<HttpRequestModel, Task> requestPipeline,
        Func<HttpContext, Task> responsePipeline,
        ILogger logger
    )
    {
        _requestMapper = requestMapper;
        _responseMapper = responseMapper;
        _httpHandlerPipeline = httpHandlerPipeline;
        _requestPipeline = requestPipeline;
        _responsePipeline = responsePipeline;
        _logger = logger;
    }

    public async ValueTask ExecuteAsync(HttpContext httpContext)
    {
        var requestModel = httpContext.Request;
        await _requestPipeline(requestModel);
        LogRequestModel(requestModel);
        if (requestModel.ParseResult.HasOption(OutputFormatter.OfflineOption))
        {
            RequestShouldBeOffline();
            return;
        }

        using var httpClientHandler = new NoProxyHttpClientHandler
        {
            AllowAutoRedirect = false
        };
        await _httpHandlerPipeline(httpClientHandler);
        using var client = new HttpClient(httpClientHandler);
        var timeout = requestModel.ParseResult.GetValueForOption(TimeoutOption);
        if (timeout > 0)
            client.Timeout = TimeSpan.FromSeconds(timeout);
        var iteration = requestModel.ParseResult.GetValueForOption(IterationOption);
        var virtualUsers = Math.Max(requestModel.ParseResult.GetValueForOption(VirtualUserOption), 1);
        var durationValue = requestModel.ParseResult.GetValueForOption(DurationOption);
        var duration = TimeSpan.Zero;
        if (!string.IsNullOrEmpty(durationValue))
        {
            if (durationValue[^1].ToLower() is 's' && double.TryParse(durationValue[..^1], out var seconds))
            {
                duration = TimeSpan.FromSeconds(seconds);
            }
            else
            if (durationValue[^1].ToLower() is 'm' && double.TryParse(durationValue[..^1], out var minutes))
            {
                duration = TimeSpan.FromMinutes(minutes);
            }
            else
            if (durationValue[^1].ToLower() is 'h' && double.TryParse(durationValue[..^1], out var hours))
            {
                duration = TimeSpan.FromHours(hours);
            }
            else
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
            httpContext.Response = await InvokeRequest(client, httpContext, false);
            await _responsePipeline(httpContext);
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
                    while (!cts.IsCancellationRequested)
                    {
                        responseList.Add(
                            await InvokeRequest(httpClient, httpContext, true)
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
                        responseList.Add(await InvokeRequest(httpClient, httpContext, true));
                    } while (--iteration > 0);
                };
            }

            var startTimestamp = Stopwatch.GetTimestamp();
            await Parallel.ForEachAsync(
                Enumerable.Range(1, virtualUsers),
                new ParallelOptions { MaxDegreeOfParallelism = virtualUsers },
                action);

            httpContext.Response.Elapsed = ProfilerHelper.GetElapsedTime(startTimestamp);
            httpContext.SetProperty(Constants.ResponseListPropertyName, responseList.ToArray());
        }
    }

    private async Task<HttpResponseModel> InvokeRequest(HttpClient httpClient, HttpContext httpContext, bool isLoadTest)
    {
        var responseModel = new HttpResponseModel();
        try
        {
            using var requestMessage = await _requestMapper.ToRequestMessage(httpContext);
            LogRequestMessage(requestMessage);
            httpContext.Request.Timestamp = DateTimeOffset.Now;
            var startTime = Stopwatch.GetTimestamp();
            using var responseMessage = await httpClient.SendAsync(requestMessage);
            var elapsed = ProfilerHelper.GetElapsedTime(startTime);
            LogResponseMessage(responseMessage);
            responseModel = await _responseMapper.ToResponseModel(responseMessage);
            responseModel.Elapsed = elapsed;
            responseModel.Timestamp = httpContext.Request.Timestamp.Add(elapsed);
            LogRequestDuration(httpContext.Request.Url, httpContext.Request.Method, responseModel.StatusCode, elapsed);
        }
        catch (Exception exception)
        {
            LogException(exception);
        }
        return responseModel;
    }

    [LoggerMessage(Level = LogLevel.Debug, EventId = 0, Message = "Request should be offline, wont send request")]
    private partial void RequestShouldBeOffline();

    [LoggerMessage(Level = LogLevel.Debug, EventId = 2, EventName = "RequestModel", Message = "RequestModel info: {requestModel}")]
    private partial void LogRequestModel(HttpRequestModel requestModel);

    [LoggerMessage(Level = LogLevel.Debug, EventId = 3, EventName = "RequestMessage", Message = "Request message: {requestMessage}")]
    private partial void LogRequestMessage(HttpRequestMessage requestMessage);

    [LoggerMessage(Level = LogLevel.Debug, EventId = 4, EventName = "ResponseMessage", Message = "Response message: {responseMessage}")]
    private partial void LogResponseMessage(HttpResponseMessage responseMessage);

    [LoggerMessage(Level = LogLevel.Information, EventId = 1000, Message = "Request {url}({method}), {responseStatusCode} duration: {duration}")]
    private partial void LogRequestDuration(string url, HttpMethod method, HttpStatusCode responseStatusCode, TimeSpan duration);

    [LoggerMessage(Level = LogLevel.Error, EventId = 1001, Message = "Send httpRequest exception")]
    private partial void LogException(Exception exception);
}
