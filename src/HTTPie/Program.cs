// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Utilities;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using WeihanLi.Common.Helpers.Hosting;

var debugEnabled = args.Contains("--debug", StringComparer.OrdinalIgnoreCase);
var appBuilder = AppHost.CreateBuilder();
appBuilder.Logging.AddDefaultDelegateLogger();
appBuilder.Logging.SetMinimumLevel(debugEnabled ? LogLevel.Debug : LogLevel.Warning);
appBuilder.Services.RegisterApplicationServices();
var app = appBuilder.Build();
// output helps when no argument or there's only "-h"/"/h"
args = args switch
{
    [] => new[] { "--help" },
    ["-h"] or ["/h"] => new[] { "--help" },
    _ => args
};
if (args.Contains("--version"))
{
    Console.WriteLine(Constants.DefaultUserAgent);
    return 0;
}

if (debugEnabled)
{
    app.Logger.PrintInputParameters(args.StringJoin(";"));
#if DEBUG
    if (!Debugger.IsAttached) Debugger.Launch();
#endif
}

return await app.Services.Handle(args);
