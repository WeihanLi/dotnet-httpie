// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace HTTPie.UnitTest.Middleware;

public class MultipartUploadMiddlewareTest
{
    [Fact]
    public async Task MultipartFlag_WithTextFields_ParsesTextParts()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .RegisterApplicationServices()
            .BuildServiceProvider();
        await services.Handle(
            "POST httpbin.org/post --multipart description=\"My document\"",
            (_, _) => Task.CompletedTask);

        var httpContext = services.GetRequiredService<HttpContext>();
        var middleware = new RequestDataMiddleware(httpContext);
        await middleware.InvokeAsync(httpContext.Request, _ => Task.CompletedTask);

        Assert.True(httpContext.GetFlag(Constants.FlagNames.IsMultipartContentType));
        var textPart = Assert.Single(httpContext.Request.MultipartTextParts);
        Assert.Equal("description", textPart.FieldName);
        Assert.Equal("My document", textPart.Value);
        Assert.Null(httpContext.Request.Body);
    }

    [Fact]
    public async Task MultipartFlag_WithFileUploadItem_ParsesFileUpload()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "file content", TestContext.Current.CancellationToken);

            var services = new ServiceCollection()
                .AddLogging()
                .RegisterApplicationServices()
                .BuildServiceProvider();
            await services.Handle(
                $"POST httpbin.org/post --multipart file@{tempFile}",
                (_, _) => Task.CompletedTask);

            var httpContext = services.GetRequiredService<HttpContext>();
            var middleware = new RequestDataMiddleware(httpContext);
            await middleware.InvokeAsync(httpContext.Request, _ => Task.CompletedTask);

            Assert.True(httpContext.GetFlag(Constants.FlagNames.IsMultipartContentType));
            Assert.Single(httpContext.Request.FileUploads);
            Assert.Equal("file", httpContext.Request.FileUploads[0].FieldName);
            Assert.Equal(tempFile, httpContext.Request.FileUploads[0].FilePath);
            Assert.Empty(httpContext.Request.MultipartTextParts);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task MultipartFlag_WithTextAndFileFields_ParsesBoth()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "file content", TestContext.Current.CancellationToken);

            var services = new ServiceCollection()
                .AddLogging()
                .RegisterApplicationServices()
                .BuildServiceProvider();
            await services.Handle(
                $"POST httpbin.org/post --multipart description=\"My document\" file@{tempFile}",
                (_, _) => Task.CompletedTask);

            var httpContext = services.GetRequiredService<HttpContext>();
            var middleware = new RequestDataMiddleware(httpContext);
            await middleware.InvokeAsync(httpContext.Request, _ => Task.CompletedTask);

            Assert.True(httpContext.GetFlag(Constants.FlagNames.IsMultipartContentType));
            Assert.Single(httpContext.Request.FileUploads);
            Assert.Equal("file", httpContext.Request.FileUploads[0].FieldName);
            var textPart = Assert.Single(httpContext.Request.MultipartTextParts);
            Assert.Equal("description", textPart.FieldName);
            Assert.Equal("My document", textPart.Value);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task MultipartFlag_SetsMethodToPost_WhenNoMethodSpecified()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "file content", TestContext.Current.CancellationToken);

            var services = new ServiceCollection()
                .AddLogging()
                .RegisterApplicationServices()
                .BuildServiceProvider();
            await services.Handle(
                $"httpbin.org/post --multipart file@{tempFile}",
                (_, _) => Task.CompletedTask);

            var httpContext = services.GetRequiredService<HttpContext>();
            var middleware = new RequestDataMiddleware(httpContext);
            await middleware.InvokeAsync(httpContext.Request, _ => Task.CompletedTask);

            Assert.Equal(HttpMethod.Post, httpContext.Request.Method);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task MultipartFlag_WithRawTextField_NormalizesFieldName()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .RegisterApplicationServices()
            .BuildServiceProvider();
        await services.Handle(
            "POST httpbin.org/post --multipart age:=10",
            (_, _) => Task.CompletedTask);

        var httpContext = services.GetRequiredService<HttpContext>();
        var middleware = new RequestDataMiddleware(httpContext);
        await middleware.InvokeAsync(httpContext.Request, _ => Task.CompletedTask);

        var textPart = Assert.Single(httpContext.Request.MultipartTextParts);
        Assert.Equal("age", textPart.FieldName);
        Assert.Equal("10", textPart.Value);
    }

    [Fact]
    public async Task MultipartFlag_RemovesContentTypeHeader()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .RegisterApplicationServices()
            .BuildServiceProvider();
        await services.Handle(
            "POST httpbin.org/post Content-Type:application/json --multipart description=test",
            (_, _) => Task.CompletedTask);

        var httpContext = services.GetRequiredService<HttpContext>();
        var headersMiddleware = new RequestHeadersMiddleware();
        await headersMiddleware.InvokeAsync(httpContext.Request, _ => Task.CompletedTask);

        var middleware = new RequestDataMiddleware(httpContext);
        await middleware.InvokeAsync(httpContext.Request, _ => Task.CompletedTask);

        Assert.DoesNotContain(httpContext.Request.Headers.Keys,
            headerName => string.Equals(headerName, Constants.ContentTypeHeaderName, StringComparison.OrdinalIgnoreCase));
    }
}
