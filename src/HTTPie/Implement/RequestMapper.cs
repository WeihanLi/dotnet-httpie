using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using WeihanLi.Common.Helpers;

namespace HTTPie.Implement;

public class RequestMapper : IRequestMapper
{
    private readonly ILogger _logger;

    public RequestMapper(ILogger logger)
    {
        _logger = logger;
    }

    public Task<HttpRequestMessage> ToRequestMessage(HttpContext httpContext)
    {
        var requestModel = httpContext.Request;
        var request = new HttpRequestMessage(requestModel.Method, requestModel.Url)
        {
            Version = requestModel.HttpVersion
        };
        if (!string.IsNullOrEmpty(requestModel.Body))
            request.Content = new StringContent(requestModel.Body, Encoding.UTF8,
                httpContext.GetFlag(Constants.FeatureFlagNames.IsFormContentType)
                    ? Constants.PlainTextMediaType
                    : Constants.JsonMediaType);
        if (requestModel.Headers is { Count: > 0 })
            foreach (var header in requestModel.Headers)
            {
                if (Constants.ContentTypeHeaderName.EqualsIgnoreCase(header.Key))
                {
                    if (request.Content != null)
                        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(header.Value);
                    continue;
                }

                if (HttpHelper.IsWellKnownContentHeader(header.Key) && request.Content != null)
                    request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                else
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        return Task.FromResult(request);
    }
}
