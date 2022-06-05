// Copyright (c) Weihan Li. All rights reserved.
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
        await services.Handle(input, _ => Task.CompletedTask);
        var httpContext = services.GetRequiredService<HttpContext>();
        var middleware = new AuthorizationMiddleware();
        await middleware.Invoke(httpContext.Request, _ => Task.CompletedTask);
        httpContext.Request.Headers.Should().BeEmpty();
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
        await services.Handle(input, _ => Task.CompletedTask);
        var httpContext = services.GetRequiredService<HttpContext>();
        var middleware = new AuthorizationMiddleware();
        await middleware.Invoke(httpContext.Request, _ => Task.CompletedTask);
        httpContext.Request.Headers.Should().NotBeEmpty();
        httpContext.Request.Headers.Should().ContainKey(Constants.AuthorizationHeaderName);
        var value = httpContext.Request.Headers[Constants.AuthorizationHeaderName].ToString();
        value.Should().NotBeNullOrEmpty();
        value.Should().StartWith("Basic ");
        value["Basic ".Length..].Should().Be(Convert.ToBase64String(Encoding.UTF8.GetBytes("uid:pwd")));
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
        await services.Handle(input, _ => Task.CompletedTask);
        var httpContext = services.GetRequiredService<HttpContext>();
        var middleware = new AuthorizationMiddleware();
        await middleware.Invoke(httpContext.Request, _ => Task.CompletedTask);
        httpContext.Request.Headers.Should().NotBeEmpty();
        httpContext.Request.Headers.Should().ContainKey(Constants.AuthorizationHeaderName);
        var value = httpContext.Request.Headers[Constants.AuthorizationHeaderName].ToString();
        value.Should().NotBeNullOrEmpty();
        value.Should().StartWith("Bearer ");
        value["Bearer ".Length..].Should().Be("TestToken");
    }
}
