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
            Bytes = await responseMessage.Content.ReadAsByteArrayAsync(),
        };
        if (IsTextResponse(responseMessage))
        {
            try
            {
                responseModel.Body = responseModel.Bytes.GetString();
            }
            catch
            {
                // ignored
            }            
        }
        return responseModel;
    }
    
    private static bool IsTextResponse(HttpResponseMessage response)
    {
        if (response.Content.Headers.ContentType?.MediaType is null)
        {
            return false;
        }
        var contentType = response.Content.Headers.ContentType;
        var mediaType = contentType.MediaType;
        var isTextContent = mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
            || mediaType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase)
            || mediaType.StartsWith("application/xml", StringComparison.OrdinalIgnoreCase)
            || mediaType.StartsWith("application/javascript", StringComparison.OrdinalIgnoreCase)
            ;
        return isTextContent;
    }
     
}
