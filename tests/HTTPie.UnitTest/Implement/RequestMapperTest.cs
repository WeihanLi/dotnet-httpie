// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

namespace HTTPie.UnitTest.Implement;

public class RequestMapperTest
{
    [Fact]
    public async Task MultipartRequest_WithTextAndFile_BuildsMultipartContent()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var fileBytes = "hello world"u8.ToArray();
            await File.WriteAllBytesAsync(tempFile, fileBytes, TestContext.Current.CancellationToken);

            var requestModel = new HttpRequestModel
            {
                Method = HttpMethod.Post,
                Url = "https://httpbin.org/post",
                MultipartTextParts = [new MultipartTextPart("description", "My document")],
                FileUploads = [new FileUploadPart("file", tempFile)]
            };
            var httpContext = new HttpContext(requestModel);
            httpContext.UpdateFlag(Constants.FlagNames.IsMultipartContentType, true);
            httpContext.RequestCancelled = TestContext.Current.CancellationToken;

            var mapper = new RequestMapper();
            using var requestMessage = await mapper.ToRequestMessage(httpContext);

            Assert.NotNull(requestMessage.Content);
            Assert.IsType<MultipartFormDataContent>(requestMessage.Content);

            var multipartContent = (MultipartFormDataContent)requestMessage.Content;
            var parts = multipartContent.ToList();
            Assert.Equal(2, parts.Count);

            // Verify text part
            var textPart = parts.FirstOrDefault(p =>
                p.Headers.ContentDisposition?.Name?.Trim('"') == "description");
            Assert.NotNull(textPart);
            var textValue = await textPart.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal("My document", textValue);

            // Verify file part
            var filePart = parts.FirstOrDefault(p =>
                p.Headers.ContentDisposition?.Name?.Trim('"') == "file");
            Assert.NotNull(filePart);
            Assert.IsType<StreamContent>(filePart);
            var fileContent = await filePart.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
            Assert.Equal(fileBytes, fileContent);
            Assert.Equal(Path.GetFileName(tempFile), filePart.Headers.ContentDisposition?.FileName?.Trim('"'));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task MultipartRequest_FileOnly_BuildsMultipartContent()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var fileBytes = "pdf content"u8.ToArray();
            await File.WriteAllBytesAsync(tempFile, fileBytes, TestContext.Current.CancellationToken);

            var requestModel = new HttpRequestModel
            {
                Method = HttpMethod.Post,
                Url = "https://httpbin.org/post",
                FileUploads = [new FileUploadPart("document", tempFile)]
            };
            var httpContext = new HttpContext(requestModel);
            httpContext.UpdateFlag(Constants.FlagNames.IsMultipartContentType, true);
            httpContext.RequestCancelled = TestContext.Current.CancellationToken;

            var mapper = new RequestMapper();
            using var requestMessage = await mapper.ToRequestMessage(httpContext);

            Assert.NotNull(requestMessage.Content);
            Assert.IsType<MultipartFormDataContent>(requestMessage.Content);

            var parts = ((MultipartFormDataContent)requestMessage.Content).ToList();
            Assert.Single(parts);
            Assert.Equal("document", parts[0].Headers.ContentDisposition?.Name?.Trim('"'));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task NonMultipartRequest_WithBody_BuildsStringContent()
    {
        var requestModel = new HttpRequestModel
        {
            Method = HttpMethod.Post,
            Url = "https://httpbin.org/post",
            Body = """{"name":"test"}"""
        };
        var httpContext = new HttpContext(requestModel);
        httpContext.UpdateFlag(Constants.FlagNames.IsMultipartContentType, false);
        httpContext.RequestCancelled = TestContext.Current.CancellationToken;

        var mapper = new RequestMapper();
        var requestMessage = await mapper.ToRequestMessage(httpContext);

        Assert.NotNull(requestMessage.Content);
        Assert.IsType<StringContent>(requestMessage.Content);
        Assert.Equal(HttpHelper.ApplicationJsonMediaType, requestMessage.Content.Headers.ContentType?.MediaType);
    }
}
