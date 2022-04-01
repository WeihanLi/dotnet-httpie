// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using Microsoft.Extensions.Primitives;

namespace HTTPie.Implement;

public sealed class ResponseMapper : IResponseMapper
{
    public async Task<HttpResponseModel> ToResponseModel(HttpResponseMessage responseMessage)
    {
        var responseModel = new HttpResponseModel
        {
            HttpVersion = responseMessage.Version,
            StatusCode = responseMessage.StatusCode,
            Headers = responseMessage.Headers
                .Union(responseMessage.Content.Headers)
                .ToDictionary(x => x.Key, x => new StringValues(x.Value.ToArray())),
            Bytes = await responseMessage.Content.ReadAsByteArrayAsync()
        };
        try
        {
            responseModel.Body = responseModel.Bytes.GetString();
        }
        catch
        {
            // ignored
            responseModel.Body = string.Empty;
        }
        return responseModel;
    }
}
