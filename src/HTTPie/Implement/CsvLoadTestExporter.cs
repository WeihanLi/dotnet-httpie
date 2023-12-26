// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;
using WeihanLi.Npoi;
using WeihanLi.Npoi.Configurations;

namespace HTTPie.Implement;

public sealed class CsvLoadTestExporter : ILoadTestExporter
{
    static CsvLoadTestExporter()
    {
        FluentSettings.LoadMappingProfile<ResponseMappingProfile>();
    }

    private static readonly Option<string> OutputCsvPathOption =
        new("--export-csv-path", "Expected export csv file path");

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

        await responseList.ToCsvFileAsync(csvPath);
    }

    private sealed class ResponseMappingProfile : IMappingProfile<HttpResponseModel>
    {
        public void Configure(IExcelConfiguration<HttpResponseModel> configuration)
        {
            configuration.Property(x => x.Bytes).Ignored();
            configuration.Property(x => x.Headers).Ignored();
        }
    }
}
