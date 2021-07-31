using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HTTPie.Abstractions;
using HTTPie.Models;
using Microsoft.Extensions.Logging;
using WeihanLi.Common.Http;
using WeihanLi.Extensions;

namespace HTTPie.Implement
{
    public class RequestExecutor : IRequestExecutor
    {
        private readonly Func<HttpClientHandler, Task> _httpHandlerPipeline;
        private readonly ILogger _logger;
        private readonly IRequestMapper _requestMapper;
        private readonly Func<HttpRequestModel, Task> _requestPipeline;
        private readonly IResponseMapper _responseMapper;
        private readonly Func<HttpContext, Task> _responsePipeline;

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

        public async Task<HttpResponseModel> ExecuteAsync(HttpRequestModel requestModel)
        {
            await _requestPipeline(requestModel);
            _logger.LogDebug("RequestModel info: {requestModel}", requestModel.ToJson());
            if (requestModel.RawInput.Contains("--offline"))
            {
                _logger.LogDebug("Request should be offline, wont send request");
                return new HttpResponseModel();
            }

            using var requestMessage = await _requestMapper.ToRequestMessage(requestModel);
            using var httpClientHandler = new NoProxyHttpClientHandler
            {
                AllowAutoRedirect = false
            };
            await _httpHandlerPipeline(httpClientHandler);
            using var httpClient = new HttpClient(httpClientHandler);
            var timeoutConfig =
                requestModel.RawInput.FirstOrDefault(x => x.StartsWith("--timeout="))?["--timeout=".Length..];
            if (int.TryParse(timeoutConfig, out var timeout) && timeout > 0)
                httpClient.Timeout = TimeSpan.FromSeconds(timeout);
            _logger.LogDebug(
                $"Request message: {requestMessage.Method.Method.ToUpper()} {requestMessage.RequestUri.AbsoluteUri} HTTP/{requestMessage.Version.ToString(2)}");
            using var responseMessage = await httpClient.SendAsync(requestMessage);
            _logger.LogDebug(
                $"Response message: HTTP/{responseMessage.Version.ToString(2)} {(int) responseMessage.StatusCode} {responseMessage.StatusCode}");
            var responseModel = await _responseMapper.ToResponseModel(responseMessage);
            var context = new HttpContext(requestModel, responseModel);
            await _responsePipeline(context);
            return responseModel;
        }
    }
}