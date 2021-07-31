using System;
using System.Linq;
using System.Net.Http;
using HTTPie.Abstractions;
using HTTPie.Implement;
using HTTPie.Middleware;
using HTTPie.Models;
using HTTPie.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WeihanLi.Common.Helpers;
using WeihanLi.Extensions;

var debugEnabled = args.Contains("--debug", StringComparer.OrdinalIgnoreCase);
var serviceCollection = new ServiceCollection()
    .AddLogging(builder => builder.AddConsole().SetMinimumLevel(debugEnabled ? LogLevel.Debug : LogLevel.Warning))
    .AddSingleton<IRequestExecutor, RequestExecutor>()
    .AddSingleton<IRequestMapper, RequestMapper>()
    .AddSingleton<IResponseMapper, ResponseMapper>()
    .AddSingleton<IOutputFormatter, OutputFormatter>()
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
    .AddSingleton<HttpRequestModel>()
    .AddSingleton<ILogger>(sp => sp.GetRequiredService<ILoggerFactory>().CreateLogger("dotnet-HTTPie"));

// HttpHandlerMiddleware
serviceCollection
    .AddHttpHandlerMiddleware<FollowRedirectMiddleware>()
    .AddHttpHandlerMiddleware<HttpSslMiddleware>()
    ;
// RequestMiddleware
serviceCollection
    .AddRequestMiddleware<QueryStringMiddleware>()
    .AddRequestMiddleware<RequestHeadersMiddleware>()
    .AddRequestMiddleware<RequestDataMiddleware>()
    .AddRequestMiddleware<DefaultRequestMiddleware>()
    ;
// ResponseMiddleware
serviceCollection.AddResponseMiddleware<DefaultResponseMiddleware>();
await using var services = serviceCollection.BuildServiceProvider();
if (args is not {Length: > 0} || args.Contains("-h") || args.Contains("--help"))
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