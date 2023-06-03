// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Commands;
using HTTPie.Implement;
using HTTPie.Middleware;
using HTTPie.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using WeihanLi.Common.Extensions;

namespace HTTPie.Utilities;

public static class Helpers
{
    internal static readonly HashSet<string> HttpMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethod.Head.Method,
        HttpMethod.Get.Method,
        HttpMethod.Post.Method,
        HttpMethod.Put.Method,
        HttpMethod.Patch.Method,
        HttpMethod.Delete.Method,
        HttpMethod.Options.Method
    };

    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static IServiceCollection AddHttpHandlerMiddleware<THttpHandlerMiddleware>(
        this IServiceCollection serviceCollection)
        where THttpHandlerMiddleware : IHttpHandlerMiddleware
    {
        serviceCollection.TryAddEnumerable(new ServiceDescriptor(typeof(IHttpHandlerMiddleware),
            typeof(THttpHandlerMiddleware), ServiceLifetime.Singleton));
        return serviceCollection;
    }

    private static IServiceCollection AddRequestMiddleware<TRequestMiddleware>(
        this IServiceCollection serviceCollection)
        where TRequestMiddleware : IRequestMiddleware
    {
        serviceCollection.TryAddEnumerable(new ServiceDescriptor(typeof(IRequestMiddleware),
            typeof(TRequestMiddleware), ServiceLifetime.Singleton));
        return serviceCollection;
    }

    private static IServiceCollection AddResponseMiddleware<TResponseMiddleware>(
        this IServiceCollection serviceCollection)
        where TResponseMiddleware : IResponseMiddleware
    {
        serviceCollection.TryAddEnumerable(new ServiceDescriptor(typeof(IResponseMiddleware),
            typeof(TResponseMiddleware), ServiceLifetime.Singleton));
        return serviceCollection;
    }

    private static Parser ConstructCommand(this IServiceProvider serviceProvider,
        Func<InvocationContext, Task>? handler = null)
    {
        var command = InitializeCommandInternal(serviceProvider, handler);
        var builder = new CommandLineBuilder(command);
        // builder.UseDefaults();
        builder
            .UseHelp("--help", "-?", "/?")
            .UseEnvironmentVariableDirective()
            .UseParseDirective()
            .UseSuggestDirective()
            .RegisterWithDotnetSuggest()
            .UseTypoCorrections()
            .UseParseErrorReporting()
            .UseExceptionHandler()
            .CancelOnProcessTermination()
            ;
        return builder.Build();
    }

    private static Command InitializeCommandInternal(IServiceProvider serviceProvider,
        Func<InvocationContext, Task>? handler = null)
    {
        var command = new RootCommand() { Name = "http", };
        var executeCommand = new ExecuteCommand();
        executeCommand.SetHandler(invocationContext =>
            executeCommand.InvokeAsync(invocationContext, serviceProvider));
        command.AddCommand(executeCommand);
        command.SetHandler(async invocationContext =>
        {
            var context = serviceProvider.GetRequiredService<HttpContext>();
            context.InvocationContext = invocationContext;

            var requestModel = context.Request;
            requestModel.ParseResult = invocationContext.ParseResult;

            var method = requestModel.ParseResult.UnmatchedTokens
                .FirstOrDefault(x => HttpMethods.Contains(x), string.Empty);
            if (!string.IsNullOrEmpty(method))
            {
                requestModel.Method = new HttpMethod(method);
                context.SetProperty(Constants.RequestMethodExistsPropertyName, true);
            }
            else
            {
                context.SetProperty(Constants.RequestMethodExistsPropertyName, false);
            }

            // Url
            requestModel.Url = requestModel.ParseResult.UnmatchedTokens.FirstOrDefault(x =>
                                   !x.StartsWith("-", StringComparison.Ordinal)
                                   && !HttpMethods.Contains(x))
                               ?? string.Empty;
            if (string.IsNullOrEmpty(requestModel.Url))
            {
                throw new InvalidOperationException("The request url can not be null");
            }

            await serviceProvider.GetRequiredService<IRequestItemParser>()
                .ParseAsync(requestModel);
        });

        // var methodArgument = new Argument<HttpMethod>("method")
        // {
        //     Description = "Request method",
        //     Arity = ArgumentArity.ZeroOrOne,
        // };
        // methodArgument.SetDefaultValue(HttpMethod.Get.Method);
        // var allowedMethods = HttpMethods.ToArray();
        // methodArgument.AddCompletions(allowedMethods);
        // command.AddArgument(methodArgument);

        // var urlArgument = new Argument<string>("url")
        // {
        //     Description = "Request url",
        //     Arity = ArgumentArity.ExactlyOne
        // };
        // command.AddArgument(urlArgument);

        // options
        foreach (var option in
                 serviceProvider
                     .GetServices<IHttpHandlerMiddleware>().SelectMany(x => x.SupportedOptions())
                     .Union(serviceProvider.GetServices<IRequestMiddleware>().SelectMany(x => x.SupportedOptions())
                         .Union(serviceProvider.GetServices<IResponseMiddleware>()
                             .SelectMany(x => x.SupportedOptions()))
                         .Union(serviceProvider.GetRequiredService<IOutputFormatter>().SupportedOptions())
                         .Union(serviceProvider.GetRequiredService<IRequestExecutor>().SupportedOptions()))
                     .Union(serviceProvider.GetRequiredService<ILoadTestExporterSelector>().SupportedOptions())
                     .Union(serviceProvider.GetServices<ILoadTestExporter>().SelectMany(x => x.SupportedOptions()))
                )
        {
            command.AddOption(option);
        }

        command.TreatUnmatchedTokensAsErrors = false;

        handler ??= async invocationContext =>
        {
            var context = serviceProvider.ResolveRequiredService<HttpContext>();
            await serviceProvider.ResolveRequiredService<IRequestExecutor>()
                .ExecuteAsync(context);
            var output = await serviceProvider.ResolveRequiredService<IOutputFormatter>()
                .GetOutput(context);
            invocationContext.Console.Out.WriteLine(output.Trim());
        };
        command.SetHandler(handler);
        return command;
    }

    public static IServiceCollection RegisterApplicationServices(this IServiceCollection serviceCollection)
    {
        serviceCollection
            .AddSingleton<IRequestItemParser, RequestItemParser>()
            .AddSingleton<IRequestExecutor, RequestExecutor>()
            .AddSingleton<IHttpParser, HttpParser>()
            .AddSingleton<IRawHttpRequestExecutor, RawHttpRequestExecutor>()
            .AddSingleton<IRequestMapper, RequestMapper>()
            .AddSingleton<IResponseMapper, ResponseMapper>()
            .AddSingleton<IOutputFormatter, OutputFormatter>()
            .AddSingleton<ILoadTestExporterSelector, LoadTestExporterSelector>()
            .AddSingleton<ILoadTestExporter, JsonLoadTestExporter>()
            .AddSingleton<ILoadTestExporter, CsvLoadTestExporter>()
            // request pipeline
            .AddSingleton(sp =>
            {
                var pipelineBuilder = PipelineBuilder.CreateAsync<HttpRequestModel>();
                foreach (var middleware in
                         sp.GetServices<IRequestMiddleware>())
                    pipelineBuilder.Use(middleware.Invoke);
                return pipelineBuilder.Build();
            })
            // response pipeline
            .AddSingleton(sp =>
            {
                var pipelineBuilder = PipelineBuilder.CreateAsync<HttpContext>();
                foreach (var middleware in
                         sp.GetServices<IResponseMiddleware>())
                    pipelineBuilder.Use(middleware.Invoke);
                return pipelineBuilder.Build();
            })
            // httpHandler pipeline
            .AddSingleton(sp =>
            {
                var pipelineBuilder = PipelineBuilder.CreateAsync<HttpClientHandler>();
                foreach (var middleware in
                         sp.GetServices<IHttpHandlerMiddleware>())
                    pipelineBuilder.Use(middleware.Invoke);
                return pipelineBuilder.Build();
            })
            .AddSingleton<HttpRequestModel>()
            .AddSingleton(sp => new HttpContext(sp.GetRequiredService<HttpRequestModel>()))
            .AddSingleton(sp => sp.GetRequiredService<ILoggerFactory>()
                .CreateLogger(Constants.ApplicationName))
            ;

        // HttpHandlerMiddleware
        serviceCollection
            .AddHttpHandlerMiddleware<FollowRedirectMiddleware>()
            .AddHttpHandlerMiddleware<HttpSslMiddleware>()
            .AddHttpHandlerMiddleware<ProxyMiddleware>()
            ;
        // RequestMiddleware
        serviceCollection
            .AddRequestMiddleware<QueryStringMiddleware>()
            .AddRequestMiddleware<RequestHeadersMiddleware>()
            .AddRequestMiddleware<RequestDataMiddleware>()
            .AddRequestMiddleware<DefaultRequestMiddleware>()
            .AddRequestMiddleware<AuthorizationMiddleware>()
            .AddRequestMiddleware<RequestCacheMiddleware>()
            ;
        // ResponseMiddleware
        return serviceCollection
                .AddResponseMiddleware<DefaultResponseMiddleware>()
                .AddResponseMiddleware<DownloadMiddleware>()
                .AddResponseMiddleware<JsonSchemaValidationMiddleware>()
            ;
    }

#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Using Microsoft.Extensions.DependencyInjection requires generating code dynamically at runtime. For example, when using enumerable and generic ValueType services.")]
# endif
    public static async Task<int> Handle(this IServiceProvider services, string[] args)
    {
        var commandParser = services.ConstructCommand();
        return await commandParser.InvokeAsync(args);
    }
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Using Microsoft.Extensions.DependencyInjection requires generating code dynamically at runtime. For example, when using enumerable and generic ValueType services.")]
# endif
    public static async Task<int> Handle(this IServiceProvider services, string commandLine,
        Func<InvocationContext, Task>? handler = null)
    {
        var commandParser = services.ConstructCommand(handler);
        return await commandParser.InvokeAsync(commandLine);
    }
}
