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

            // Add text fields stored as url-encoded pairs in Body
            if (!string.IsNullOrEmpty(requestModel.Body))
            {
                foreach (var pair in requestModel.Body.Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var eqIdx = pair.IndexOf('=');
                    if (eqIdx > 0)
                    {
                        var fieldName = pair[..eqIdx];
                        var fieldValue = pair[(eqIdx + 1)..];
                        multipartContent.Add(new StringContent(fieldValue), fieldName);
                    }
                }
            }

            // Add file parts
            foreach (var (fieldName, filePath) in requestModel.FileUploads)
            {
                var fileBytes = await File.ReadAllBytesAsync(filePath, httpContext.RequestCancelled);
                var fileContent = new ByteArrayContent(fileBytes);
                var mimeType = MimeTypeMap.GetMimeType(Path.GetExtension(filePath));
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
                multipartContent.Add(fileContent, fieldName, Path.GetFileName(filePath));
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
                request.TryAddHeader(header.Key, header.Value.ToString());
            }
        return request;
    }
}
