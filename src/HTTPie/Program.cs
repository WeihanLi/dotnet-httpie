using HTTPie.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using WeihanLi.Common;
using WeihanLi.Extensions;

var debugEnabled = args.Contains("--debug", StringComparer.OrdinalIgnoreCase);
var serviceCollection = new ServiceCollection()
    .RegisterHTTPieServices(debugEnabled);
await using var services = serviceCollection.BuildServiceProvider();
DependencyResolver.SetDependencyResolver(services);
Helpers.InitializeSupportOptions(services);
if (args is not { Length: > 0 })
{
    args = new[] { "--help" };
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
    return await Helpers.Handle(services, args);
}
catch (Exception e)
{
    logger.LogError(e, "Invoke Request Exception");
    return -1;
}