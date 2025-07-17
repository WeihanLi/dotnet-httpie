// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Nodes;

namespace HTTPie.UnitTest.Middleware;

public class RequestDataMiddlewareTest(IServiceProvider serviceProvider)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    [Theory]
    [InlineData("http://localhost:5000/api/values?name=test&hello=world")]
    [InlineData("https://reservation.weihanli.xyz/health?name=test&hello=world")]
    public async Task UrlQueryStringShouldNotBeTreatAsRequestData_Issue1(string url)
    {
        await _serviceProvider.Handle(url, (_, _) => Task.CompletedTask);
        var httpContext = _serviceProvider.GetRequiredService<HttpContext>();
        var middleware = new RequestDataMiddleware(httpContext);
        await middleware.InvokeAsync(httpContext.Request, _ => Task.CompletedTask);
        Assert.Null(httpContext.Request.Body);
    }

    [Theory]
    [InlineData("GET :5000/api/values name==test hello==world")]
    [InlineData("https://reservation.weihanli.xyz/health name==test hello==world")]
    public async Task QueryShouldNotBeTreatAsRequestData(string input)
    {
        await _serviceProvider.Handle(input, (_, _) => Task.CompletedTask);
        var httpContext = _serviceProvider.GetRequiredService<HttpContext>();
        var middleware = new RequestDataMiddleware(httpContext);
        await middleware.InvokeAsync(httpContext.Request, _ => Task.CompletedTask);
        Assert.Null(httpContext.Request.Body);
    }

    [Theory]
    //[InlineData("GET :5000/api/values Api-Version:E=2")]
    //[InlineData("https://reservation.weihanli.xyz/health Authorization: 'Bearer dede'")]
    [InlineData("https://reservation.weihanli.xyz/health Authorization:'Bearer dede'")]
    public async Task HeadersShouldNotBeTreatAsRequestData(string input)
    {
        await _serviceProvider.Handle(input, (_, _) => Task.CompletedTask);
        var httpContext = _serviceProvider.GetRequiredService<HttpContext>();
        var middleware = new RequestDataMiddleware(httpContext);
        await middleware.InvokeAsync(httpContext.Request, _ => Task.CompletedTask);
        Assert.Null(httpContext.Request.Body);
    }

    [Theory]
    [InlineData("GET :5000/api/values --print=Hh")]
    [InlineData("GET :5000/api/values -p=HB")]
    [InlineData("reservation.weihanli.xyz/health --verbose --schema=https")]
    public async Task FlagsShouldNotBeTreatAsRequestData(string input)
    {
        await _serviceProvider.Handle(input, (_, _) => Task.CompletedTask);
        var httpContext = _serviceProvider.GetRequiredService<HttpContext>();
        var middleware = new RequestDataMiddleware(httpContext);
        await middleware.InvokeAsync(httpContext.Request, _ => Task.CompletedTask);
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
        await services.Handle(input, (_, _) => Task.CompletedTask);
        var httpContext = services.GetRequiredService<HttpContext>();
        var middleware = new RequestDataMiddleware(httpContext);
        await middleware.InvokeAsync(httpContext.Request, _ => Task.CompletedTask);
        Assert.NotNull(httpContext.Request.Body);
        Assert.Equal(@"{""Id"":1,""Name"":""Alice""}", httpContext.Request.Body);
    }

    [Theory]
    [InlineData("httpbin.org/post platform[name]=HTTPie", """{"platform":{"name":"HTTPie"}}""")]
    [InlineData("httpbin.org/post platform[about][mission]=MakeAPIsSimple platform[about][stars]:=54000", 
                """{"platform":{"about":{"mission":"MakeAPIsSimple","stars":54000}}}""")]
    [InlineData("httpbin.org/post platform[apps][]=Terminal platform[apps][]=Desktop", 
                """{"platform":{"apps":["Terminal","Desktop"]}}""")]
    [InlineData("httpbin.org/post obj[key]=value nested[deep][prop]:=true", 
                """{"obj":{"key":"value"},"nested":{"deep":{"prop":true}}}""")]
    [InlineData("httpbin.org/post [][name]=test", """[{"name":"test"}]""")]
    [InlineData("httpbin.org/post [][name]=test [][age]:=25", """[{"name":"test"},{"age":25}]""")]
    [InlineData("httpbin.org/post [][name]=first [][name]=second", """[{"name":"first"},{"name":"second"}]""")]
    public async Task NestedJsonTest(string input, string expectedJsonPattern)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .RegisterApplicationServices()
            .BuildServiceProvider();
        await services.Handle(input, (_, _) => Task.CompletedTask);
        var httpContext = services.GetRequiredService<HttpContext>();
        var middleware = new RequestDataMiddleware(httpContext);
        await middleware.InvokeAsync(httpContext.Request, _ => Task.CompletedTask);
        
        Assert.NotNull(httpContext.Request.Body);
        
        // Parse both actual and expected JSON to compare structure
        var actualJson = JsonNode.Parse(httpContext.Request.Body);
        var expectedJson = JsonNode.Parse(expectedJsonPattern);
        
        Assert.Equal(expectedJson?.ToJsonString(), actualJson?.ToJsonString());
    }

    [Theory]
    [Theory]
    [InlineData("httpbin.org/post [0]=first [1]=second", """["first","second"]""")]
    [InlineData("httpbin.org/post []=first []=second", """["first","second"]""")]
    [InlineData("httpbin.org/post [0]=first [2]=third", """["first",null,"third"]""")]
    [InlineData("httpbin.org/post [0]:=123 [1]:=true", """[123,true]""")]
    public async Task RootArrayJsonTest(string input, string expectedJsonPattern)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .RegisterApplicationServices()
            .BuildServiceProvider();
        await services.Handle(input, (_, _) => Task.CompletedTask);
        var httpContext = services.GetRequiredService<HttpContext>();
        var middleware = new RequestDataMiddleware(httpContext);
        await middleware.InvokeAsync(httpContext.Request, _ => Task.CompletedTask);
        
        Assert.NotNull(httpContext.Request.Body);
        
        // Parse both actual and expected JSON to compare structure
        var actualJson = JsonNode.Parse(httpContext.Request.Body);
        var expectedJson = JsonNode.Parse(expectedJsonPattern);
        
        Assert.Equal(expectedJson?.ToJsonString(), actualJson?.ToJsonString());
    }
}
