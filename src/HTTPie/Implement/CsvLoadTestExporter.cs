// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using System.Collections.Frozen;

namespace HTTPie.Implement;

public sealed class CsvLoadTestExporter : ILoadTestExporter
{

    private static readonly Option<string> OutputCsvPathOption =
        new("--export-csv-path", "Expected export csv file path");

    private static readonly FrozenSet<string> ExcludeExportPropertyNames = new[]
        {
            nameof(HttpResponseModel.Bytes), nameof(HttpResponseModel.Body), nameof(HttpResponseModel.Headers)
        }
        .ToFrozenSet();

    public Option[] SupportedOptions()
    {
        return [OutputCsvPathOption];
    }

    public string Type => "csv";

    public async ValueTask Export(HttpContext context, HttpResponseModel[] responseList)
    {
        var csvPath = context.Request.ParseResult.GetValueForOption(OutputCsvPathOption);
        if (string.IsNullOrEmpty(csvPath))
        {
            return;
        }

        var properties = AppSerializationContext.Default.HttpResponseModel.Properties
            .Where(p => p.Get is not null && ExcludeExportPropertyNames.Contains(p.Name))
            .OrderBy(p => p.Order)
            .ToArray();
        await using var fileStream = File.OpenWrite(Path.GetFullPath(csvPath));
        
        {
            await using var csvWriter = new StreamWriter(fileStream);
            var headerLine = properties.Select(x => x.Name).StringJoin(",");
            await csvWriter.WriteLineAsync(headerLine);
            foreach (var response in responseList)
            {
                var dataLine = properties.Select(p => p.Get!.Invoke(response)?.ToString())
                    .StringJoin(",");
                await csvWriter.WriteLineAsync(dataLine);
            }
        }

        await fileStream.FlushAsync();
    }
}
