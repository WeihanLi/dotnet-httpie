// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Models;

namespace HTTPie.Implement;

public sealed class LoadTestExporterSelector : ILoadTestExporterSelector
{
    private readonly HttpContext _context;
    private readonly Dictionary<string, ILoadTestExporter> _exporters;
    private static readonly Option<string> ExporterTypeOption = new("--exporter-type", "Load test result exporter type");

    public Option[] SupportedOptions()
    {
        return new[] { ExporterTypeOption };
    }

    public LoadTestExporterSelector(HttpContext context, IEnumerable<ILoadTestExporter> exporters)
    {
        _context = context;
        _exporters = exporters.ToDictionary(x => x.Type, x => x, StringComparer.OrdinalIgnoreCase);
        ExporterTypeOption.AddCompletions(_exporters.Keys.ToArray());
    }

    public ILoadTestExporter? Select()
    {
        var exporterType = _context.Request.ParseResult.GetValueForOption(ExporterTypeOption) ?? string.Empty;
        _exporters.TryGetValue(exporterType, out var exporter);
        return exporter;
    }
}
