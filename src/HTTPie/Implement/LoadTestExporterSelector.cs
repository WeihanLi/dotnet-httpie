// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;

namespace HTTPie.Implement;

public sealed class LoadTestExporterSelector : ILoadTestExporterSelector
{
    private readonly HttpContext _context;
    private readonly Dictionary<string, ILoadTestExporter> _exporters;
    private static readonly Option<string> ExporterTypeOption = new("--exporter-type")
    {
        Description = "Load test result exporter type"
    };

    public Option[] SupportedOptions()
    {
        return [ExporterTypeOption];
    }

    public LoadTestExporterSelector(HttpContext context, IEnumerable<ILoadTestExporter> exporters)
    {
        _context = context;
        _exporters = exporters.ToDictionary(x => x.Type, x => x, StringComparer.OrdinalIgnoreCase);
        ExporterTypeOption.CompletionSources.Add(_exporters.Keys.ToArray());
    }

    public ILoadTestExporter? Select()
    {
        var exporterType = _context.Request.ParseResult.GetValue(ExporterTypeOption) ?? string.Empty;
        _exporters.TryGetValue(exporterType, out var exporter);
        return exporter;
    }
}
