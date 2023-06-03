// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Utilities;

namespace HTTPie.IntegrationTest.Utilities;

public class HttpParserTest
{
    [Fact]
    public async Task CommonParseTest()
    {
        var fileName = "HttpSample.http";
        var path = Path.Combine(Directory.GetCurrentDirectory(), "TestAssets", fileName);
        var parser = new HttpParser();
        await foreach (var request in parser.ParseAsync(path))
        {
            Assert.NotNull(request);
            Assert.NotNull(request.RequestUri);
        }
    }
}
