// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using System.Text.Json;

namespace HTTPie.Implement;

public sealed class JsonLoadTestExporter : ILoadTestExporter
{
    private static readonly Option<string> OutputJsonPathOption = new("--export-json-path")
    {
        Description = "Expected export json file path"
    };

    public Option[] SupportedOptions() => [OutputJsonPathOption];

    public string Type => "json";

    public async ValueTask Export(HttpContext context, HttpResponseModel[] responseList)
    {
        var jsonPath = context.Request.ParseResult.GetValue(OutputJsonPathOption);
        if (string.IsNullOrEmpty(jsonPath))
        {
            return;
        }
        var result = new
        {
            context.Response.Elapsed,
            context.Request,
            ResponseList = responseList
        };
        await using var fs = File.Create(jsonPath);
        await JsonSerializer.SerializeAsync(fs, result, Utilities.AppSerializationContext.Default.Options.GetTypeInfo(result.GetType()));
        await fs.FlushAsync();
    }
}
