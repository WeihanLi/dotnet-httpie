// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using System.Net;

namespace HTTPie.Middleware;

public sealed class ProxyMiddleware(HttpRequestModel requestModel) : IHttpHandlerMiddleware
{
    private static readonly Option<string> ProxyOption = new("--proxy", "Send request with proxy");
    private static readonly Option<bool> NoProxyOption = new("--no-proxy", "Disable proxy");

    public Option[] SupportedOptions()
    {
        return [ProxyOption, NoProxyOption];
    }

    public Task InvokeAsync(HttpClientHandler httpClientHandler, Func<HttpClientHandler, Task> next)
    {
        if (requestModel.ParseResult.HasOption(NoProxyOption))
        {
            httpClientHandler.Proxy = null;
            httpClientHandler.UseProxy = false;
        }
        else
        {
            var proxyValue = requestModel.ParseResult.GetValueForOption(ProxyOption);
            if (Uri.TryCreate(proxyValue, UriKind.Absolute, out var proxyUri))
            {
                httpClientHandler.Proxy = new WebProxy(proxyUri);
                httpClientHandler.UseProxy = true;
            }
        }

        return next(httpClientHandler);
    }
}
