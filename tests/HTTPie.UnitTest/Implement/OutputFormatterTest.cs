// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace HTTPie.UnitTest.Implement;

public class OutputFormatterTest
{
    private readonly OutputFormatter _outputFormatter;

    public OutputFormatterTest()
    {
        _outputFormatter = new OutputFormatter(
            new ServiceCollection().BuildServiceProvider(), 
            NullLogger<OutputFormatter>.Instance)
            ;
    }


    [Theory]
    [InlineData(":5000/api/values -q")]
    [InlineData(":5000/api/values --quiet")]
    public async Task QuietTest(string input)
    {
        var httpContext = new HttpContext(new HttpRequestModel());
        Helpers.InitRequestModel(httpContext, input);
        var output = await _outputFormatter.GetOutput(httpContext);
        Assert.Empty(output);
    }
}
