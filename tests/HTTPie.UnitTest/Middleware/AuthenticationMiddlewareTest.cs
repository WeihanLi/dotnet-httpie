// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace HTTPie.UnitTest.Middleware;

public class AuthorizationMiddlewareTest
{
    [Theory]
    [InlineData("GET :5000/api/values")]
    [InlineData(":5000/api/values")]
    public async Task NoAuthTest(string input)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .RegisterApplicationServices()
            .BuildServiceProvider();
        await services.Handle(input, (_, _) => Task.CompletedTask);
        var httpContext = services.GetRequiredService<HttpContext>();
        var middleware = new AuthorizationMiddleware();
        await middleware.InvokeAsync(httpContext.Request, _ => Task.CompletedTask);
        Assert.Empty(httpContext.Request.Headers);
    }

    [Theory]
    [InlineData("GET -a=uid:pwd :5000/api/values")]
    [InlineData("-A=Basic -a=uid:pwd :5000/api/values")]
    public async Task BasicAuthTest(string input)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .RegisterApplicationServices()
            .BuildServiceProvider();
        await services.Handle(input, (_, _) => Task.CompletedTask);
        var httpContext = services.GetRequiredService<HttpContext>();
        var middleware = new AuthorizationMiddleware();
        await middleware.InvokeAsync(httpContext.Request, _ => Task.CompletedTask);
        Assert.NotEmpty(httpContext.Request.Headers);
        Assert.True(httpContext.Request.Headers.ContainsKey(Constants.AuthorizationHeaderName));
        var value = httpContext.Request.Headers[Constants.AuthorizationHeaderName].ToString();
        Assert.NotNull(value);
        Assert.StartsWith("Basic ", value);
        Assert.Equal(Convert.ToBase64String(Encoding.UTF8.GetBytes("uid:pwd")), value["Basic ".Length..]);
    }

    [Theory]
    [InlineData("GET -A=Bearer -a=TestToken :5000/api/values")]
    [InlineData("-A=jwt -a=TestToken :5000/api/values")]
    public async Task JwtAuthTest(string input)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .RegisterApplicationServices()
            .BuildServiceProvider();
        await services.Handle(input, (_, _) => Task.CompletedTask);
        var httpContext = services.GetRequiredService<HttpContext>();
        var middleware = new AuthorizationMiddleware();
        await middleware.InvokeAsync(httpContext.Request, _ => Task.CompletedTask);
        Assert.NotEmpty(httpContext.Request.Headers);
        Assert.True(httpContext.Request.Headers.ContainsKey(Constants.AuthorizationHeaderName));
        var value = httpContext.Request.Headers[Constants.AuthorizationHeaderName].ToString();
        Assert.NotNull(value);
        Assert.StartsWith("Bearer ", value);
        Assert.Equal("TestToken", value["Bearer ".Length..]);
    }
}
