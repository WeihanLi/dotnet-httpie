// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using System.Net;

namespace HTTPie.Middleware;

public sealed class ProxyMiddleware: IHttpHandlerMiddleware
{
    private readonly HttpRequestModel _requestModel;
    private static readonly Option<string> ProxyOption = new("--proxy", "Send request with proxy");
    private static readonly Option NoProxyOption = new("--no-proxy", "Disable proxy");

    public ProxyMiddleware(HttpRequestModel requestModel)
    {
        _requestModel = requestModel;
    }

    public ICollection<Option> SupportedOptions()
    {
        return new[] { ProxyOption, NoProxyOption };
    }

    public Task Invoke(HttpClientHandler httpClientHandler, Func<Task> next)
    {
        if (_requestModel.ParseResult.HasOption(NoProxyOption))
        {
            httpClientHandler.Proxy = null;
            httpClientHandler.UseProxy = false;
        }
        else
        {
            var proxyValue = _requestModel.ParseResult.GetValueForOption(ProxyOption);
            if (Uri.TryCreate(proxyValue, UriKind.Absolute, out var proxyUri))
            {
                httpClientHandler.Proxy = new WebProxy(proxyUri);
                httpClientHandler.UseProxy = true;
            }
        }
        return next();
    }
}
