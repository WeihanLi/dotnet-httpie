// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace HTTPie.UnitTest.Middleware;

public class MultipartUploadMiddlewareTest(IServiceProvider serviceProvider)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

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
        Assert.NotNull(httpContext.Request.Body);
        Assert.Contains("description=", httpContext.Request.Body);
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
        }
        finally
        {
            File.Delete(tempFile);
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
            Assert.NotNull(httpContext.Request.Body);
            Assert.Contains("description=", httpContext.Request.Body);
        }
        finally
        {
            File.Delete(tempFile);
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
            File.Delete(tempFile);
        }
    }
}
