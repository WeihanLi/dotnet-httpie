// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using System.Security.Authentication;

namespace HTTPie.Middleware;

public sealed class HttpSslMiddleware : IHttpHandlerMiddleware
{
    private readonly HttpRequestModel _requestModel;

    public HttpSslMiddleware(HttpRequestModel requestModel)
    {
        _requestModel = requestModel;
    }

    private static readonly Option DisableSslVerifyOption = new(new[] { "--no-verify", "--verify=no" }, "disable ssl cert check");
    private static readonly Option<SslProtocols> SslProtocalOption = new("--ssl", "specific the ssl protocols, ssl3, tls, tls1.1, tls1.2, tls1.3");

    public ICollection<Option> SupportedOptions() => new HashSet<Option>()
        {
            DisableSslVerifyOption,
            SslProtocalOption,
        };

    public Task Invoke(HttpClientHandler httpClientHandler, Func<HttpClientHandler, Task> next)
    {
        if (_requestModel.Options.Contains("--verify=no")
            || _requestModel.ParseResult.HasOption(DisableSslVerifyOption))
        {
            // ignore server cert
            httpClientHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        }
        // sslProtocols
        var sslOption = _requestModel.Options.FirstOrDefault(x => x.StartsWith("--ssl="))?["--ssl=".Length..];
        if (!string.IsNullOrEmpty(sslOption))
        {
            sslOption = sslOption.Replace(".", string.Empty);
            if (Enum.TryParse(sslOption, out SslProtocols sslProtocols))
                httpClientHandler.SslProtocols = sslProtocols;
        }

        return next(httpClientHandler);
    }
}
