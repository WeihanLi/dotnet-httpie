using HTTPie.Abstractions;
using HTTPie.Models;
using Microsoft.Extensions.Logging;
using WeihanLi.Common.Http;

namespace HTTPie.Implement;

public partial class RequestExecutor : IRequestExecutor
{
    private readonly HttpContext _httpContext;
    private readonly Func<HttpClientHandler, Task> _httpHandlerPipeline;
    private readonly ILogger _logger;
    private readonly IRequestMapper _requestMapper;
    private readonly Func<HttpRequestModel, Task> _requestPipeline;
    private readonly IResponseMapper _responseMapper;
    private readonly Func<HttpContext, Task> _responsePipeline;

    public static readonly Option<double> TimeoutOption = new("--timeout", "Request timeout in seconds");

    public RequestExecutor(
        IRequestMapper requestMapper,
        IResponseMapper responseMapper,
        HttpContext httpContext,
        Func<HttpClientHandler, Task> httpHandlerPipeline,
        Func<HttpRequestModel, Task> requestPipeline,
        Func<HttpContext, Task> responsePipeline,
        ILogger logger
    )
    {
        _requestMapper = requestMapper;
        _responseMapper = responseMapper;
        _httpContext = httpContext;
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

        using var requestMessage = await _requestMapper.ToRequestMessage(httpContext);
        using var httpClientHandler = new NoProxyHttpClientHandler
        {
            AllowAutoRedirect = false
        };
        await _httpHandlerPipeline(httpClientHandler);
        using var httpClient = new HttpClient(httpClientHandler);
        var timeout = requestModel.ParseResult.GetValueForOption(TimeoutOption);
        if (timeout > 0)
            httpClient.Timeout = TimeSpan.FromSeconds(timeout);
        LogRequestMessage(requestMessage);
        using var responseMessage = await httpClient.SendAsync(requestMessage);
        LogResponseMessage(responseMessage);
        _httpContext.Response = await _responseMapper.ToResponseModel(responseMessage);
        await _responsePipeline(_httpContext);
    }

    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = "Request should be offline, wont send request")]
    private partial void RequestShouldBeOffline();

    [LoggerMessage(Level = LogLevel.Debug, EventName = "RequestModel", Message = "RequestModel info: {requestModel}")]
    private partial void LogRequestModel(HttpRequestModel requestModel);


    [LoggerMessage(Level = LogLevel.Debug, EventName = "RequestMessage", Message = "Request message: {requestMessage}")]
    private partial void LogRequestMessage(HttpRequestMessage requestMessage);

    [LoggerMessage(Level = LogLevel.Debug, EventName = "ResponseMessage", Message = "Response message: {responseMessage}")]
    private partial void LogResponseMessage(HttpResponseMessage responseMessage);
}
