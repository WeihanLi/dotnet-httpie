using System;
using System.Linq;
using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WeihanLi.Extensions;

var debugEnabled = args.Contains("--debug", StringComparer.OrdinalIgnoreCase);
var serviceCollection = new ServiceCollection()
    .RegisterHTTPieServices(debugEnabled);
await using var services = serviceCollection.BuildServiceProvider();
if (args is not {Length: > 0} || args.Contains("--help"))
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
    var requestModel = services.GetRequiredService<HttpRequestModel>();
    Helpers.InitRequestModel(requestModel, args);
    var responseModel = await services.GetRequiredService<IRequestExecutor>()
        .ExecuteAsync(requestModel);
    var output = services.GetRequiredService<IOutputFormatter>()
        .GetOutput(requestModel, responseModel);
    Console.WriteLine(output);
    return 0;
}
catch (Exception e)
{
    logger.LogError(e, "Invoke Request Exception");
    return -1;
}