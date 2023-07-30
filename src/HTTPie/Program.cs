// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;

var debugEnabled = args.Contains("--debug", StringComparer.OrdinalIgnoreCase);
var logAsJson = args.Contains("--logAsJson", StringComparer.OrdinalIgnoreCase);
var serviceCollection = new ServiceCollection();
serviceCollection.AddLogging(builder =>
{
    if (logAsJson)
    {
        builder.AddJsonConsole(options =>
        {
            options.JsonWriterOptions = new JsonWriterOptions()
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Indented = true
            };
        });
    }
    else
    {
        builder.AddConsole();
    }

    builder.SetMinimumLevel(debugEnabled ? LogLevel.Debug : LogLevel.Warning);
});
serviceCollection.RegisterApplicationServices();
await using var services = serviceCollection.BuildServiceProvider();

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

var logger = services.GetRequiredService<ILogger>();
logger.PrintInputParameters(args.StringJoin(";"));

#if DEBUG
if (debugEnabled && !Debugger.IsAttached) Debugger.Launch();
#endif

return await services.Handle(args);
