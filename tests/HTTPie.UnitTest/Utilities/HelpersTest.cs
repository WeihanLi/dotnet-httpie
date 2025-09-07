// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using CommandLineParser = System.CommandLine.Parsing.CommandLineParser;

namespace HTTPie.UnitTest.Utilities;

public class HelpersTest
{
    [Fact]
    public async Task RequestInit_BasicAuthTest()
    {
        var input = "GET -a uid:pwd reservation.weihanli.xyz/health";
        var services = new ServiceCollection()
            .AddLogging()
            .RegisterApplicationServices()
            .BuildServiceProvider();
        await services.Handle(input, (_, _) => Task.CompletedTask);
        var httpContext = services.GetRequiredService<HttpContext>();
        Assert.Equal("reservation.weihanli.xyz/health", httpContext.Request.Url);
        Assert.Empty(httpContext.Request.RequestItems);
    }

    [Fact]
    public async Task RequestInit_RequestItemsTest_WithoutMethod()
    {
        var input = "reservation.weihanli.xyz/health hello=world Api-Version:1.2 name=test age:=10";
        var services = new ServiceCollection()
            .AddLogging()
            .RegisterApplicationServices()
            .BuildServiceProvider();
        await services.Handle(input, (_, _) => Task.CompletedTask);
        var httpContext = services.GetRequiredService<HttpContext>();

        Assert.Equal("reservation.weihanli.xyz/health", httpContext.Request.Url);
        Assert.NotEmpty(httpContext.Request.RequestItems);
        Assert.Equal(4, httpContext.Request.RequestItems.Count);
    }

    [Theory(Skip = "https://github.com/dotnet/command-line-api/issues/1755")]
    [InlineData(@"--raw {""Id"":1,""Name"":""Test""}")]
    [InlineData(@"--raw {""""Id"""":1,""""Name"""":""""Test""""}")]
    [InlineData(@"--raw '{""Id"":1,""Name"":""Test""}'")]
    public void SplitTest(string commandLine)
    {
        var args = CommandLineParser.SplitCommandLine(commandLine).ToArray();
        Assert.Equal(2, args.Length);
        Assert.Equal("--raw", args[0]);
        Assert.Equal(@"{""Id"":1,""Name"":""Test""}", args[1]);
    }
}
