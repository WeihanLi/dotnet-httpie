using System;
using System.Net.Http;
using System.Threading.Tasks;
using HTTPie.Abstractions;
using HTTPie.Models;
using Microsoft.Extensions.DependencyInjection;

namespace HTTPie.Implement
{
    public class RequestExecutor : IRequestExecutor
    {
        private readonly Func<HttpClientHandler, Task> _httpHandlerPipeline;
        private readonly IRequestMapper _requestMapper;
        private readonly Func<HttpRequestModel, Task> _requestPipeline;
        private readonly IResponseMapper _responseMapper;
        private readonly Func<HttpContext, Task> _responsePipeline;
        private readonly IServiceProvider _serviceProvider;

        public RequestExecutor(
            IRequestMapper requestMapper,
            IResponseMapper responseMapper,
            Func<HttpClientHandler, Task> httpHandlerPipeline,
            Func<HttpRequestModel, Task> requestPipeline,
            Func<HttpContext, Task> responsePipeline,
            IServiceProvider serviceProvider
        )
        {
            _requestMapper = requestMapper;
            _responseMapper = responseMapper;
            _httpHandlerPipeline = httpHandlerPipeline;
            _requestPipeline = requestPipeline;
            _responsePipeline = responsePipeline;
            _serviceProvider = serviceProvider;
        }

        public async Task<HttpResponseModel> ExecuteAsync(HttpRequestModel requestModel)
        {
            using var httpClientHandler = new HttpClientHandler();
            await _httpHandlerPipeline(httpClientHandler);
            using var httpClient = new HttpClient(httpClientHandler);
            await _requestPipeline(requestModel);
            using var requestMessage = await _requestMapper.ToRequestMessage(requestModel);
            using var response = await httpClient.SendAsync(requestMessage);
            var responseModel = await _responseMapper.ToResponseModel(response);
            using var scope = _serviceProvider.CreateScope();
            var context = new HttpContext(requestModel, responseModel, scope.ServiceProvider);
            await _responsePipeline(context);
            return responseModel;
        }
    }
}