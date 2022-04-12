// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using WeihanLi.Npoi;

namespace HTTPie.Implement;

public sealed class CsvLoadTestExporter: ILoadTestExporter
{
    private static readonly Option<string> OutputCsvPathOption = new("--export-csv-path", "Expected export csv file path");

    public ICollection<Option> SupportedOptions()
    {
        return new[] { OutputCsvPathOption };
    }

    public string Type => "csv";
    public async ValueTask Export(HttpContext context, HttpResponseModel[] responseList)
    {
        var csvPath = context.Request.ParseResult.GetValueForOption(OutputCsvPathOption);
        if (string.IsNullOrEmpty(csvPath))
        {
            return;
        }
        await responseList.ToCsvFileAsync(csvPath);
    }
}
