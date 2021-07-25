using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WeihanLi.Common.Helpers;
using WeihanLi.Extensions;

var debugEnabled = args.Contains("--debug", StringComparer.OrdinalIgnoreCase);
await using var services = new ServiceCollection()
    .AddLogging(builder => builder.AddConsole().SetMinimumLevel(debugEnabled ? LogLevel.Debug : LogLevel.Warning))
    .RegisterAssemblyTypesAsImplementedInterfaces(Assembly.GetExecutingAssembly())
    .AddSingleton(sp =>
    {
        var pipelineBuilder = PipelineBuilder.CreateAsync<HttpRequestModel>();
        foreach (var middleware in
            sp.GetServices<IRequestMiddleware>())
            pipelineBuilder.Use(middleware.Invoke);
        return pipelineBuilder.Build();
    })
    .AddSingleton(sp =>
    {
        var pipelineBuilder = PipelineBuilder.CreateAsync<HttpContext>();
        foreach (var middleware in
            sp.GetServices<IResponseMiddleware>())
            pipelineBuilder.Use(middleware.Invoke);
        return pipelineBuilder.Build();
    })
    .AddSingleton(sp =>
    {
        var pipelineBuilder = PipelineBuilder.CreateAsync<HttpClientHandler>();
        foreach (var middleware in
            sp.GetServices<IHttpHandlerMiddleware>())
            pipelineBuilder.Use(middleware.Invoke);
        return pipelineBuilder.Build();
    })
    .BuildServiceProvider();
if (args is not { Length: > 0 } || args.Contains("-h") || args.Contains("--help"))
{
    // Print Help
    var helpText = ParseHelper.GetHelpText(services);
    Console.WriteLine(helpText);
    return 0;
}

var logger = services.GetRequiredService<ILoggerFactory>()
    .CreateLogger("dotnet-HTTPie");
logger.LogDebug($"Input parameters: {args.StringJoin(";")}");
try
{
    var requestModel = ParseHelper.GetRequestModel(args);
    logger.LogDebug("requestModel:{requestModel}", requestModel.ToJson());
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