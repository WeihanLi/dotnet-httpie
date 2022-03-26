// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using HTTPie.Models;
using WeihanLi.Common.Models;

namespace HTTPie.Abstractions;

public interface ILoadTestExporter: IPlugin
{
    string Type { get; }
    
    ValueTask Export(HttpContext context, HttpResponseModel[] responseList);
}

public interface ILoadTestExporterSelector: IPlugin
{
    ILoadTestExporter? Select();
}
