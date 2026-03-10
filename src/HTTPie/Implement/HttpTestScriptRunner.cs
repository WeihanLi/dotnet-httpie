// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using System.Text.RegularExpressions;

namespace HTTPie.Implement;

/// <summary>
/// Evaluates simple preScript and postScript expressions for HTTP API tests.
/// </summary>
/// <remarks>
/// Supported preScript expressions:
///   $request.headers.add("name", "value")  – add a header to the request
///   $request.headers.set("name", "value")  – set (replace) a header on the request
///
/// Supported postScript expressions:
///   $response.EnsureSuccessStatusCode()         – assert 2xx status code
///   $response.StatusCode == NNN                 – assert exact status code
///   $response.StatusCode != NNN                 – assert status code is not NNN
///   $response.Body.Contains("text")             – assert body contains text
/// </remarks>
public static partial class HttpTestScriptRunner
{
    // preScript patterns
    [GeneratedRegex(
        @"^\$request\.headers\.(add|set)\(\s*""(?<name>[^""]+)""\s*,\s*""(?<value>[^""]*)""\s*\)\s*$",
        RegexOptions.IgnoreCase)]
    private static partial Regex RequestHeadersPattern { get; }

    // postScript patterns
    [GeneratedRegex(@"^\$response\.EnsureSuccessStatusCode\(\)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex EnsureSuccessPattern { get; }

    [GeneratedRegex(@"^\$response\.StatusCode\s*(?<op>==|!=)\s*(?<code>\d{3})\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex StatusCodePattern { get; }

    [GeneratedRegex(@"^\$response\.Body\.Contains\(\s*""(?<text>[^""]*)""\s*\)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex BodyContainsPattern { get; }

    /// <summary>
    /// Executes a preScript expression, mutating the supplied headers dictionary.
    /// </summary>
    /// <param name="script">The script text (one expression per line).</param>
    /// <param name="headers">The request headers to modify.</param>
    public static void ExecutePreScript(string? script, Dictionary<string, string> headers)
    {
        if (string.IsNullOrWhiteSpace(script)) return;

        foreach (var rawLine in script.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("//") || line.StartsWith("#")) continue;

            var match = RequestHeadersPattern.Match(line);
            if (match.Success)
            {
                var name = match.Groups["name"].Value;
                var value = match.Groups["value"].Value;
                // "add" keeps existing; "set" replaces – both are set in a dictionary so behavior is equivalent
                headers[name] = value;
            }
        }
    }

    /// <summary>
    /// Evaluates a postScript expression against the HTTP response.
    /// Throws an <see cref="HttpTestAssertionException"/> when an assertion fails.
    /// </summary>
    /// <param name="script">The script text (one expression per line).</param>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    /// <param name="responseBody">The response body text.</param>
    public static async Task ExecutePostScriptAsync(
        string? script,
        int statusCode,
        Func<Task<string>> getResponseBody)
    {
        if (string.IsNullOrWhiteSpace(script)) return;

        string? cachedBody = null;

        foreach (var rawLine in script.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("//") || line.StartsWith("#")) continue;

            if (EnsureSuccessPattern.IsMatch(line))
            {
                if (statusCode < 200 || statusCode >= 300)
                    throw new HttpTestAssertionException(
                        $"EnsureSuccessStatusCode failed: response status code {statusCode} is not a success status.");
                continue;
            }

            var statusMatch = StatusCodePattern.Match(line);
            if (statusMatch.Success)
            {
                var op = statusMatch.Groups["op"].Value;
                var expected = int.Parse(statusMatch.Groups["code"].Value);
                var passed = op == "==" ? statusCode == expected : statusCode != expected;
                if (!passed)
                    throw new HttpTestAssertionException(
                        $"StatusCode assertion failed: expected {op} {expected}, actual {statusCode}.");
                continue;
            }

            var bodyMatch = BodyContainsPattern.Match(line);
            if (bodyMatch.Success)
            {
                cachedBody ??= await getResponseBody();
                var text = bodyMatch.Groups["text"].Value;
                if (!cachedBody.Contains(text, StringComparison.Ordinal))
                    throw new HttpTestAssertionException(
                        $"Body.Contains assertion failed: body does not contain \"{text}\".");
                continue;
            }
        }
    }
}

/// <summary>
/// Thrown when an HTTP test assertion (postScript) fails.
/// </summary>
public sealed class HttpTestAssertionException(string message) : Exception(message);
