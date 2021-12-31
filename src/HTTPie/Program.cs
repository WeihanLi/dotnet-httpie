using HTTPie.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;
using System.Text.Json;
using WeihanLi.Common;

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
serviceCollection.RegisterHTTPieServices();
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
    return await services.Handle(args);
}
catch (Exception e)
{
    logger.LogError(e, "Invoke Request Exception");
    return -1;
}
