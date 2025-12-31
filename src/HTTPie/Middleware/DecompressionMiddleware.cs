// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using System.Net;

namespace HTTPie.Middleware;

public class DecompressionMiddleware(HttpRequestModel requestModel) : IHttpHandlerMiddleware
{  
    private static readonly Option<bool> DeCompressOption = new("--decompress")
    {
        Description = "The HTTP request allows auto-decompress"
    };
    public Option[] SupportedOptions() =>
    [
        DeCompressOption
    ];
    
    public Task InvokeAsync(HttpClientHandler httpClientHandler, Func<HttpClientHandler, Task> next)
    {
        var decompress = requestModel.ParseResult.HasOption(DeCompressOption);
        if (decompress)
            httpClientHandler.AutomaticDecompression = DecompressionMethods.All;

        return next(httpClientHandler);
    }
}
