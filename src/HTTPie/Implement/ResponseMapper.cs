using HTTPie.Abstractions;
using HTTPie.Models;
using Microsoft.Extensions.Primitives;

namespace HTTPie.Implement
{
    public class ResponseMapper : IResponseMapper
    {
        public async Task<HttpResponseModel> ToResponseModel(HttpResponseMessage responseMessage)
        {
            var responseModel = new HttpResponseModel
            {
                HttpVersion = responseMessage.Version,
                StatusCode = responseMessage.StatusCode,
                Headers =
                    responseMessage.Headers
                        .Union(responseMessage.Content.Headers)
                        .ToDictionary(x => x.Key, x => new StringValues(x.Value.ToArray())),
                Body = await responseMessage.Content.ReadAsStringAsync()
            };
            return responseModel;
        }
    }
}