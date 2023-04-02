﻿// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using Microsoft.Extensions.Primitives;

namespace HTTPie.Middleware;

public sealed class DownloadMiddleware : IResponseMiddleware
{
    public static readonly Option<bool> DownloadOption = new(new[] { "-d", "--download" }, "Download file");

    private static readonly Option<bool> ContinueOption =
        new(new[] { "-c", "--continue" }, "Download file using append mode");

    private static readonly Option<string> OutputOption = new(new[] { "-o", "--output" }, "Output file path");
    private static readonly Option<string> CheckSumOption = new(new[] { "--checksum" }, "Checksum to validate");

    private static readonly Option<HashType> CheckSumAlgOption =
        new(new[] { "--checksum-alg" }, () => HashType.SHA1, "Checksum hash algorithm type");

    public Option[] SupportedOptions()
    {
        return new Option[] { DownloadOption, ContinueOption, OutputOption, CheckSumOption, CheckSumAlgOption };
    }

    public async Task Invoke(HttpContext context, Func<HttpContext, Task> next)
    {
        var download = context.Request.ParseResult.HasOption(DownloadOption);
        if (!download)
        {
            await next(context);
            return;
        }

        var output = context.Request.ParseResult.GetValueForOption(OutputOption);
        if (string.IsNullOrWhiteSpace(output))
        {
            if (context.Response.Headers.TryGetValue(Constants.ContentDispositionHeaderName,
                    out var dispositionHeaderValues))
            {
                output = GetFileNameFromContentDispositionHeader(dispositionHeaderValues);
            }

            if (output.IsNullOrWhiteSpace())
            {
                // guess a file name
                context.Response.Headers.TryGetValue(Constants.ContentTypeHeaderName, out var contentType);
                output = GetFileNameFromUrl(context.Request.Url, contentType.ToString());
            }
        }

        var fileName = output.GetValueOrDefault($"{DateTime.Now:yyyyMMdd-HHmmss}.tmp");
        if (context.Request.ParseResult.HasOption(ContinueOption))
        {
            await File.AppendAllTextAsync(fileName, context.Response.Body).ConfigureAwait(false);
        }
        else
        {
            await File.WriteAllBytesAsync(fileName, context.Response.Bytes).ConfigureAwait(false);
        }

        var checksum = context.Request.ParseResult.GetValueForOption(CheckSumOption);
        if (checksum.IsNotNullOrWhiteSpace())
        {
            var checksumAlgType = context.Request.ParseResult.GetValueForOption(CheckSumAlgOption);
            var calculatedValue = HashHelper.GetHashedString(checksumAlgType, context.Response.Bytes);
            var checksumMatched = calculatedValue.EqualsIgnoreCase(checksum);
            context.Response.Headers.TryAdd(Constants.ResponseCheckSumValueHeaderName, calculatedValue);
            context.Response.Headers.TryAdd(Constants.ResponseCheckSumValidHeaderName, checksumMatched.ToString());
        }

        await next(context);
    }


    internal static string? GetFileNameFromContentDispositionHeader(StringValues headerValues)
    {
        const string filenameSeparator = "filename=";

        var value = headerValues.ToString().Split(new[] { ';' },
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(x => x.StartsWith(filenameSeparator));
        if (value is null || value.Length == filenameSeparator.Length)
            return null;

        return value[filenameSeparator.Length..].Trim('.', '"');
    }

    private static string GetFileNameFromUrl(string url, string responseContentType)
    {
        var contentType = responseContentType.Split(';')[0].Trim();
        // https://www.nuget.org/profiles/weihanli/avatar?imageSize=512
        var uri = new Uri(url);
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(uri.AbsolutePath);
        var fileExtension = Path.GetExtension(uri.AbsolutePath);
        var extension = fileExtension.GetValueOrDefault(() => MimeTypeMap.GetExtension(contentType));
        return $"{fileNameWithoutExt}{extension}";
    }
}
