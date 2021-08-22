using HTTPie.Abstractions;
using HTTPie.Models;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Net.Http;
using System.Threading.Tasks;
using WeihanLi.Common.Http;
using WeihanLi.Extensions;

namespace HTTPie.Implement
{
    public class RequestExecutor : IRequestExecutor
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
            _logger.LogDebug("RequestModel info: {requestModel}", requestModel.ToJson());
            if (requestModel.ParseResult.HasOption(OutputFormatter.OfflineOption))
            {
                _logger.LogDebug("Request should be offline, wont send request");
                return;
            }

            using var requestMessage = await _requestMapper.ToRequestMessage(httpContext);
            using var httpClientHandler = new NoProxyHttpClientHandler
            {
                AllowAutoRedirect = false
            };
            await _httpHandlerPipeline(httpClientHandler);
            using var httpClient = new HttpClient(httpClientHandler);
            var timeout = requestModel.ParseResult.ValueForOption(TimeoutOption);
            if (timeout > 0)
                httpClient.Timeout = TimeSpan.FromSeconds(timeout);
            _logger.LogDebug($@"Request message: {requestMessage}");
            using var responseMessage = await httpClient.SendAsync(requestMessage);
            _logger.LogDebug($"Response message: {responseMessage}");
            _httpContext.Response = await _responseMapper.ToResponseModel(responseMessage);
            await _responsePipeline(_httpContext);
        }
    }
}