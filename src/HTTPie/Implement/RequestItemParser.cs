// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using Microsoft.Extensions.Logging;

namespace HTTPie.Implement;

public sealed class RequestItemParser(ILogger logger) : IRequestItemParser
{
    public Task ParseAsync(HttpRequestModel request)
    {
        logger.LogDebug("Unmatched tokens: {UnmatchedTokens}",
            string.Join(",", request.ParseResult.UnmatchedTokens));

        request.RequestItems = request.ParseResult.UnmatchedTokens
            .Except([request.Url])
            .Where(x => !x.StartsWith('-') && !Helpers.HttpMethods.Contains(x))
            .ToList();

        return Task.CompletedTask;
    }
}
