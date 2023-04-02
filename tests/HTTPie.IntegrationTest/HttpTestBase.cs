// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HTTPie.IntegrationTest;

[Collection("HttpTests")]
public abstract class HttpTestBase : IDisposable
{
    private bool _disposed;

    protected HttpTestBase()
    {
        Services = new ServiceCollection()
            .AddLogging()
            .RegisterApplicationServices()
            .BuildServiceProvider();
    }

    protected IServiceProvider Services { get; }

    protected HttpContext GetHttpContext()
    {
        return Services.GetRequiredService<HttpContext>();
    }

    protected HttpRequestModel GetRequest()
    {
        return Services.GetRequiredService<HttpRequestModel>();
    }

    protected HttpResponseModel GetResponse()
    {
        return Services.GetRequiredService<HttpContext>().Response;
    }

    protected async Task<int> Handle(string input)
    {
        return await Services.Handle(input);
    }

    protected async Task<string> GetOutput(string input)
    {
        await Handle(input);
        return await Services.GetRequiredService<IOutputFormatter>()
            .GetOutput(GetHttpContext());
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                (Services as IDisposable)?.Dispose();
                // TODO: dispose managed state (managed objects)
            }

            // free unmanaged resources (unmanaged objects) and override finalizer
            // set large fields to null
            _disposed = true;
        }
    }

    // // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~HttpTestBase()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
