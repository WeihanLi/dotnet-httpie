using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HTTPie.Abstractions;
using HTTPie.Models;
using WeihanLi.Common.Helpers;

namespace HTTPie.Implement
{
    public class RequestMapper : IRequestMapper
    {
        public Task<HttpRequestMessage> ToRequestMessage(HttpRequestModel requestModel)
        {
            var request = new HttpRequestMessage(requestModel.Method, requestModel.Url);
            if (requestModel.Headers is {Count: > 0})
                foreach (var header in requestModel.Headers)
                    if (HttpHelper.IsWellKnownContentHeader(header.Key))
                        request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                    else
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            request.Version = requestModel.HttpVersion;
            if (!string.IsNullOrEmpty(requestModel.Body))
                request.Content = new StringContent(requestModel.Body, Encoding.UTF8);
            return Task.FromResult(request);
        }
    }
}