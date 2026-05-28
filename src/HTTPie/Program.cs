// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using HTTPie.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

var debugEnabled = args.Contains("--debug", StringComparer.OrdinalIgnoreCase);
var services = new ServiceCollection();
services.RegisterApplicationServices();
services.AddLogging(logging =>
{
    logging.SetMinimumLevel(debugEnabled ? LogLevel.Debug : LogLevel.Warning);
    logging.AddProvider(AppLoggerProvider.Default);
});
await using var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

// output helps when no argument or there's only "-h"/"/h"
args = args switch
{
    [] => ["--help"],
    ["-h"] or ["/h"] => ["--help"],
    _ => args
};
if (args is ["--version"])
{
    Console.WriteLine(Constants.DefaultUserAgent);
    return 0;
}

if (debugEnabled)
{
    logger.PrintInputParameters(args.StringJoin(";"));
#if DEBUG
    if (!Debugger.IsAttached) Debugger.Launch();
#endif
}

return await serviceProvider.Handle(args);
