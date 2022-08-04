// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace HTTPie.UnitTest.Implement;

public class OutputFormatterTest
{
    private readonly OutputFormatter _outputFormatter;

    public OutputFormatterTest(IServiceProvider serviceProvider)
    {
        _outputFormatter = new OutputFormatter(
            serviceProvider,
            NullLogger<OutputFormatter>.Instance)
            ;
    }


    [Theory]
    [InlineData(":5000/api/values -q")]
    [InlineData(":5000/api/values --quiet")]
    public async Task QuietTest(string input)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .RegisterApplicationServices()
            .BuildServiceProvider();
        await services.Handle(input, _ => Task.CompletedTask);
        var httpContext = services.GetRequiredService<HttpContext>();
        var output = await _outputFormatter.GetOutput(httpContext);
        Assert.Empty(output);
    }
}
