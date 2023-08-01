// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using System.Net.Http.Headers;
using System.Text;

namespace HTTPie.Implement;

public sealed class RequestMapper : IRequestMapper
{
    public Task<HttpRequestMessage> ToRequestMessage(HttpContext httpContext)
    {
        var requestModel = httpContext.Request;
        var request = new HttpRequestMessage(requestModel.Method, requestModel.Url)
        {
            Version = requestModel.HttpVersion
        };
        if (!string.IsNullOrEmpty(requestModel.Body))
            request.Content = new StringContent(requestModel.Body, Encoding.UTF8,
                httpContext.GetFlag(Constants.FlagNames.IsFormContentType)
                    ? HttpHelper.TextPlainMediaType
                    : HttpHelper.ApplicationJsonMediaType);
        if (requestModel.Headers is { Count: > 0 })
            foreach (var header in requestModel.Headers)
            {
                request.TryAddHeader(header.Key, header.Value.ToString());
            }
        return Task.FromResult(request);
    }
}
