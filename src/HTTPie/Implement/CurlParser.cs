// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Utilities;
using System.Text;

namespace HTTPie.Implement;

public sealed class CurlParser : ICurlParser
{
    public HttpRequestMessage Parse(string curlScript)
    {
        Guard.NotNullOrEmpty(curlScript);
        var normalizedScript = curlScript
            .Replace("\\\n", " ")
            .Replace("\\\r\n", " ")
            .Replace("\r\n", " ")
            .Replace("\n ", " ")
            .Trim();
        if (!normalizedScript.StartsWith("curl "))
        {
            throw new ArgumentException($"Invalid curl script: {curlScript}", nameof(curlScript));
        }

        var splits = CommandLineParer.ParseLine(normalizedScript).ToArray();
        string requestMethod = string.Empty, requestBody = string.Empty;
        Uri? uri = null;
        var headers = new List<KeyValuePair<string, string>>();
        for (var i = 1; i < splits.Length; i++)
        {
            var item = splits[i].Trim('\'', '"');

            // url
            if (uri is null && Uri.TryCreate(item, UriKind.Absolute, out uri))
            {
                continue;
            }

            // Get Request method
            if (item is "-X")
            {
                i++;

                if (i < splits.Length)
                {
                    item = splits[i].Trim('\'', '"');
                    if (Helpers.HttpMethods.Contains(item))
                        requestMethod = item;
                }

                continue;
            }

            // request body
            if (item is "-d")
            {
                i++;
                if (i < splits.Length)
                {
                    item = splits[i].Trim('\'', '"');
                    requestBody = item;
                }

                continue;
            }

            // headers
            if (item is "-H")
            {
                i++;
                if (i < splits.Length)
                {
                    var header = splits[i].Trim('\'', '"');
                    var headerSplits = header.Split(':', 2, StringSplitOptions.TrimEntries);
                    headers.Add(new KeyValuePair<string, string>(headerSplits[0],
                        headerSplits.Length > 1 ? headerSplits[1] : string.Empty));
                }

                continue;
            }
        }

        if (string.IsNullOrEmpty(requestMethod)) requestMethod = "GET";

        if (uri is null) throw new ArgumentException("Url info not found");

        var request = new HttpRequestMessage(new HttpMethod(requestMethod), uri);
        // request body
        if (!string.IsNullOrEmpty(requestBody))
        {
            request.Content = new StringContent(requestBody);
        }

        // headers
        foreach (var headerGroup in headers.GroupBy(x => x.Key))
        {
            request.TryAddHeader(
                headerGroup.Key,
                headerGroup.Select(x => x.Value).StringJoin(",")
            );
        }

        return request;
    }
}

internal static class CommandLineParer
{
    public static IEnumerable<string> SplitCommandLine(string commandLine)
    {
        var memory = commandLine.AsMemory();

        var startTokenIndex = 0;

        var pos = 0;

        var seeking = Boundary.TokenStart;
        var seekingQuote = Boundary.QuoteStart;
        var isInQuote = false;

        while (pos < memory.Length)
        {
            var c = memory.Span[pos];

            if (char.IsWhiteSpace(c))
            {
                if (seekingQuote == Boundary.QuoteStart)
                {
                    switch (seeking)
                    {
                        case Boundary.WordEnd:
                            yield return CurrentToken();
                            startTokenIndex = pos;
                            seeking = Boundary.TokenStart;
                            break;

                        case Boundary.TokenStart:
                            startTokenIndex = pos;
                            break;
                    }
                }
            }
            else if (c == '\"')
            {
                if (seeking == Boundary.TokenStart)
                {
                    switch (seekingQuote)
                    {
                        case Boundary.QuoteEnd:
                            yield return CurrentToken();
                            startTokenIndex = pos;
                            seekingQuote = Boundary.QuoteStart;
                            break;

                        case Boundary.QuoteStart:
                            startTokenIndex = pos + 1;
                            seekingQuote = Boundary.QuoteEnd;
                            break;
                    }
                }
                else
                {
                    switch (seekingQuote)
                    {
                        case Boundary.QuoteEnd:
                            seekingQuote = Boundary.QuoteStart;
                            break;

                        case Boundary.QuoteStart:
                            seekingQuote = Boundary.QuoteEnd;
                            break;
                    }
                }
            }
            else if (seeking == Boundary.TokenStart && seekingQuote == Boundary.QuoteStart)
            {
                seeking = Boundary.WordEnd;
                startTokenIndex = pos;
            }

            pos++;

            if (IsAtEndOfInput())
            {
                switch (seeking)
                {
                    case Boundary.TokenStart:
                        break;
                    default:
                        yield return CurrentToken();
                        break;
                }
            }
        }

        string CurrentToken()
        {
            return memory.Slice(startTokenIndex, IndexOfEndOfToken()).ToString().Trim('"');
        }

        int IndexOfEndOfToken() => pos - startTokenIndex;

        bool IsAtEndOfInput() => pos == memory.Length;
    }

    public static IEnumerable<string> ParseLine(string line)
    {
        if (string.IsNullOrEmpty(line))
        {
            yield break;
        }

        var columnBuilder = new StringBuilder();
        var fields = new List<string>();

        var inColumn = false;
        var inQuotes = false;

        // Iterate through every character in the line
        for (var i = 0; i < line.Length; i++)
        {
            var character = line[i];

            // If we are not currently inside a column
            if (!inColumn)
            {
                // If the current character is a double quote then the column value is contained within
                // double quotes, otherwise append the next character
                inColumn = true;
                if (character == '\'')
                {
                    inQuotes = true;
                    continue;
                }
            }

            // If we are in between double quotes
            if (inQuotes)
            {
                if (i + 1 == line.Length)
                {
                    break;
                }

                if (character == '\'' && line[i + 1] == ' ') // quotes end
                {
                    inQuotes = false;
                    inColumn = false;
                    i++; //skip next
                }
                else if (character == '\'' && line[i + 1] == '\'') // quotes
                {
                    i++; //skip next
                }
                else if (character == '\'')
                {
                    throw new ArgumentException($"unable to escape {line}");
                }
            }
            else if (character == ' ')
            {
                inColumn = false;
            }

            // If we are no longer in the column clear the builder and add the columns to the list
            if (!inColumn)
            {
                if (columnBuilder.Length > 0)
                {
                    yield return columnBuilder.ToString();
                    columnBuilder.Clear();
                }
            }
            else // append the current column
            {
                columnBuilder.Append(character);
            }
        }

        if (columnBuilder.Length > 0)
        {
            yield return columnBuilder.ToString();
            columnBuilder.Clear();
        }
    }

    private enum Boundary
    {
        TokenStart,
        WordEnd,
        QuoteStart,
        QuoteEnd
    }
}
