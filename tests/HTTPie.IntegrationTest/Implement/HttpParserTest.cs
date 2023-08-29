// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Implement;
using HTTPie.Utilities;
using Xunit.Abstractions;

namespace HTTPie.IntegrationTest.Implement;

public class HttpParserTest(ITestOutputHelper outputHelper)
{
    private readonly ITestOutputHelper _outputHelper = outputHelper;

    [Theory]
    [InlineData("HttpStartedSample.http")]
    [InlineData("HttpVariableSample.http")]
    public async Task CommonParseTest(string fileName)
    {
        Environment.SetEnvironmentVariable("timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString());
        var path = Path.Combine(Directory.GetCurrentDirectory(), "TestAssets", fileName);
        var parser = new HttpParser();
        var count = 0;

        await foreach (var request in parser.ParseAsync(path, new CancellationToken()))
        {
            Assert.NotNull(request);
            count++;

            _outputHelper.WriteLine(request.Name);
            _outputHelper.WriteLine(await request.RequestMessage.ToRawMessageAsync());
        }

        Assert.NotEqual(0, count);
    }
}
