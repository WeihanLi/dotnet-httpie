// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using System.Security.Authentication;

namespace HTTPie.Middleware;

public sealed class HttpSslMiddleware(HttpRequestModel requestModel) : IHttpHandlerMiddleware
{
    private static readonly Option<bool> DisableSslVerifyOption =
        new(["--no-verify", "--verify=no"], "disable ssl cert check");

    private static readonly Option<SslProtocols?> SslProtocalsOption =
        new("--ssl", "specific the ssl protocols, ssl3, tls, tls1.1, tls1.2, tls1.3");

    public Option[] SupportedOptions() => [DisableSslVerifyOption, SslProtocalsOption];

    public Task InvokeAsync(HttpClientHandler httpClientHandler, Func<HttpClientHandler, Task> next)
    {
        if (requestModel.ParseResult.HasOption(DisableSslVerifyOption))
        {
            // ignore server cert
            httpClientHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        }

        // sslProtocols
        var sslOption = requestModel.ParseResult.GetValueForOption(SslProtocalsOption);
        if (sslOption.HasValue)
        {
            httpClientHandler.SslProtocols = sslOption.Value;
        }

        return next(httpClientHandler);
    }
}
