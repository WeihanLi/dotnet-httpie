// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using System.Text.Json;

namespace HTTPie.Implement;

public class JsonLoadTestExporter : ILoadTestExporter
{
    private static readonly Option<string> OutputJsonPathOption = new("--output-json-path", "output json file path");

    public ICollection<Option> SupportedOptions()
    {
        return new[] { OutputJsonPathOption };
    }

    public string Type => "Json";

    public async ValueTask Export(HttpContext context, HttpResponseModel[] responseList)
    {
        var jsonPath = context.Request.ParseResult.GetValueForOption(OutputJsonPathOption);
        if (string.IsNullOrEmpty(jsonPath))
        {
            return;
        }
        var result = new { context.Response.Elapsed, context.Request, responseList };
        await using var fs = File.Create(jsonPath);
        await JsonSerializer.SerializeAsync(fs, result);
        await fs.FlushAsync();
    }
}
