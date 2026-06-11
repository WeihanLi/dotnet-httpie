// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using System.Net.Http.Headers;
using System.Text;

namespace HTTPie.Implement;

public sealed class RequestMapper : IRequestMapper
{
    public async Task<HttpRequestMessage> ToRequestMessage(HttpContext httpContext)
    {
        var requestModel = httpContext.Request;
        var request = new HttpRequestMessage(requestModel.Method, requestModel.Url);
        if (requestModel.HttpVersion is not null)
        {
            request.Version = requestModel.HttpVersion;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
        }
        else
        {
            request.Version = new Version(2, 0);
        }

        var isMultipart = httpContext.GetFlag(Constants.FlagNames.IsMultipartContentType);
        if (isMultipart)
        {
            var multipartContent = new MultipartFormDataContent();

            foreach (var multipartTextPart in requestModel.MultipartTextParts)
            {
                multipartContent.Add(new StringContent(multipartTextPart.Value), multipartTextPart.FieldName);
            }

            // Add file parts
            foreach (var fileUpload in requestModel.FileUploads)
            {
                var fileContent = CreateMultipartFileContent(fileUpload);
                try
                {
                    multipartContent.Add(fileContent, fileUpload.FieldName, Path.GetFileName(fileUpload.FilePath));
                }
                catch
                {
                    fileContent.Dispose();
                    throw;
                }
            }

            request.Content = multipartContent;
        }
        else if (!string.IsNullOrEmpty(requestModel.Body))
        {
            request.Content = new StringContent(requestModel.Body, Encoding.UTF8,
                httpContext.GetFlag(Constants.FlagNames.IsFormContentType)
                    ? HttpHelper.TextPlainMediaType
                    : HttpHelper.ApplicationJsonMediaType);
        }

        if (requestModel.Headers is { Count: > 0 })
            foreach (var header in requestModel.Headers)
            {
                if (isMultipart && string.Equals(header.Key, Constants.ContentTypeHeaderName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                request.TryAddHeader(header.Key, header.Value.ToString());
            }
        return request;
    }

    private static StreamContent CreateMultipartFileContent(FileUploadPart fileUpload)
    {
        var fileStream = CreateMultipartFileStream(fileUpload.FilePath);

        try
        {
            var fileContent = new StreamContent(fileStream);
            var mimeType = MimeTypeMap.GetMimeType(Path.GetExtension(fileUpload.FilePath));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
            return fileContent;
        }
        catch
        {
            fileStream.Dispose();
            throw;
        }
    }

    private static FileStream CreateMultipartFileStream(string filePath)
    {
        try
        {
            return new FileStream(filePath, new FileStreamOptions
            {
                Access = FileAccess.Read,
                Mode = FileMode.Open,
                Share = FileShare.Read,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(
                $"Failed to open file for upload: {filePath}. Ensure the file exists and is accessible.", ex);
        }
    }
}
