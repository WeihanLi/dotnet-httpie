using System;
using System.Linq;
using HTTPie.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WeihanLi.Extensions;

var debugEnabled = args.Contains("--debug", StringComparer.OrdinalIgnoreCase);
var serviceCollection = new ServiceCollection()
    .RegisterHTTPieServices(debugEnabled);
await using var services = serviceCollection.BuildServiceProvider();
if (args is not { Length: > 0 } || args.Contains("--help"))
{
    // Print Help
    var helpText = Helpers.GetHelpText(services);
    Console.WriteLine(helpText);
    return 0;
}

if (args.Contains("--version"))
{
    Console.WriteLine(Constants.DefaultUserAgent);
    return 0;
}

var logger = services.GetRequiredService<ILogger>();
logger.LogDebug($"Input parameters: {args.StringJoin(";")}");
try
{
    var output = await Helpers.GetOutput(services, args);
    Console.WriteLine(output);
    return 0;
}
catch (Exception e)
{
    logger.LogError(e, "Invoke Request Exception");
    return -1;
}