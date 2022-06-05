// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace HTTPie.UnitTest.Middleware;

public class RequestDataMiddlewareTest
{
    private readonly IServiceProvider _serviceProvider;

    public RequestDataMiddlewareTest(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [Theory]
    [InlineData("http://localhost:5000/api/values?name=test&hello=world")]
    [InlineData("https://reservation.weihanli.xyz/health?name=test&hello=world")]
    public async Task UrlQueryStringShouldNotBeTreatAsRequestData_Issue1(string url)
    {
        await _serviceProvider.Handle(url, _ => Task.CompletedTask);
        var httpContext = _serviceProvider.GetRequiredService<HttpContext>();
        var middleware = new RequestDataMiddleware(httpContext);
        await middleware.Invoke(httpContext.Request, _ => Task.CompletedTask);
        Assert.Null(httpContext.Request.Body);
    }

    [Theory]
    [InlineData("GET :5000/api/values name==test hello==world")]
    [InlineData("https://reservation.weihanli.xyz/health name==test hello==world")]
    public async Task QueryShouldNotBeTreatAsRequestData(string input)
    {
        await _serviceProvider.Handle(input, _ => Task.CompletedTask);
        var httpContext = _serviceProvider.GetRequiredService<HttpContext>();
        var middleware = new RequestDataMiddleware(httpContext);
        await middleware.Invoke(httpContext.Request, _ => Task.CompletedTask);
        Assert.Null(httpContext.Request.Body);
    }

    [Theory]
    //[InlineData("GET :5000/api/values Api-Version:E=2")]
    //[InlineData("https://reservation.weihanli.xyz/health Authorization: 'Bearer dede'")]
    [InlineData("https://reservation.weihanli.xyz/health Authorization:'Bearer dede'")]
    public async Task HeadersShouldNotBeTreatAsRequestData(string input)
    {
        await _serviceProvider.Handle(input, _ => Task.CompletedTask);
        var httpContext = _serviceProvider.GetRequiredService<HttpContext>();
        var middleware = new RequestDataMiddleware(httpContext);
        await middleware.Invoke(httpContext.Request, _ => Task.CompletedTask);
        Assert.Null(httpContext.Request.Body);
    }

    [Theory]
    [InlineData("GET :5000/api/values --print=Hh")]
    [InlineData("GET :5000/api/values -p=HB")]
    [InlineData("reservation.weihanli.xyz/health --verbose --schema=https")]
    public async Task FlagsShouldNotBeTreatAsRequestData(string input)
    {
        await _serviceProvider.Handle(input, _ => Task.CompletedTask);
        var httpContext = _serviceProvider.GetRequiredService<HttpContext>();
        var middleware = new RequestDataMiddleware(httpContext);
        await middleware.Invoke(httpContext.Request, _ => Task.CompletedTask);
        Assert.Null(httpContext.Request.Body);
    }

    [Theory(Skip = "Split")]
    [InlineData(@"/ --raw='{""Id"":1,""Name"":""Alice""}'")]
    [InlineData(@"/ --raw=""{""Id"":1,""Name"":""Alice""}""")]
    [InlineData(@"/ --raw={""Id"":1,""Name"":""Alice""}")]
    public async Task RawDataJsonTest(string input)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .RegisterApplicationServices()
            .BuildServiceProvider();
        await services.Handle(input, _ => Task.CompletedTask);
        var httpContext = services.GetRequiredService<HttpContext>();
        var middleware = new RequestDataMiddleware(httpContext);
        await middleware.Invoke(httpContext.Request, _ => Task.CompletedTask);
        Assert.NotNull(httpContext.Request.Body);
        Assert.Equal(@"{""Id"":1,""Name"":""Alice""}", httpContext.Request.Body);
    }
}
