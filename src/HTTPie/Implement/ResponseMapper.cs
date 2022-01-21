// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using Microsoft.Extensions.Primitives;

namespace HTTPie.Implement;

public class ResponseMapper : IResponseMapper
{
    private readonly HttpContext _httpContext;

    public ResponseMapper(HttpContext httpContext)
    {
        _httpContext = httpContext;
    }
    public async Task<HttpResponseModel> ToResponseModel(HttpResponseMessage responseMessage)
    {
        var responseModel = new HttpResponseModel
        {
            HttpVersion = responseMessage.Version,
            StatusCode = responseMessage.StatusCode,
        };
        var outputFormat = OutputFormatter.GetOutputFormat(_httpContext);
        if (outputFormat.HasFlag(OutputFormat.ResponseHeaders))
        {
            responseModel.Headers = responseMessage.Headers
              .Union(responseMessage.Content.Headers)
              .ToDictionary(x => x.Key, x => new StringValues(x.Value.ToArray()));
        }
        if (outputFormat.HasFlag(OutputFormat.ResponseBody))
        {
            responseModel.Body = await responseMessage.Content.ReadAsStringAsync();
        }
        return responseModel;
    }
}
