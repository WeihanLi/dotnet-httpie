// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Implement;
using HTTPie.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace HTTPie.IntegrationTest.Implement;

public class HttpParserTest(ITestOutputHelper outputHelper)
{
    [Theory]
    [InlineData("HttpStartedSample.http")]
    [InlineData("HttpVariableSample.http")]
    [InlineData("HttpRequestReferenceSample.http")]
    [InlineData("HttpEnvFileVariableSample.http")]
    [InlineData("HttpEnvFileVariableSample.http", "test")]
    [InlineData("HttpEnvFileVariableSample.http", "dev")]
    [InlineData("HttpEnvFileVariableSample.http", "staging")]
    public async Task CommonParseTest(string fileName, string? env = null)
    {
        Environment.SetEnvironmentVariable("timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString());
        var path = Path.Combine(Directory.GetCurrentDirectory(), "TestAssets", fileName);
        var parser = new HttpParser(NullLogger.Instance)
        {
            Environment = env
        };
        var count = 0;

        outputHelper.WriteLine($"http-file: {fileName}...");
        await foreach (var request in parser.ParseFileAsync(path, CancellationToken.None))
        {
            Assert.NotNull(request);
            count++;

            outputHelper.WriteLine(request.Name);
            outputHelper.WriteLine(await request.RequestMessage.ToRawMessageAsync());
        }

        Assert.NotEqual(0, count);
    }
}
