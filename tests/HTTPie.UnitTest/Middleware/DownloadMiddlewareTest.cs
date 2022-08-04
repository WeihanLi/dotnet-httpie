// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

namespace HTTPie.UnitTest.Middleware;

public class DownloadMiddlewareTest
{
    [Theory]
    [InlineData(@"form-data; name=""fieldName""; filename=""filename.jpg""", "filename.jpg")]
    [InlineData(@"form-data; name=fieldName; filename=filename.jpg", "filename.jpg")]
    [InlineData("attachment; filename=\"hello-0.0.1.tgz\"; filename*=UTF-8''hello-0.0.1.tgz", "hello-0.0.1.tgz")]
    [InlineData("attachment; filename=\"cool.html\"", "cool.html")]
    public void GetFileNameFromContentDispositionHeader(string responseHeader, string expectedFileName)
    {
        var fileName = DownloadMiddleware.GetFileNameFromContentDispositionHeader(responseHeader);
        Assert.Equal(expectedFileName, fileName);
    }
}
