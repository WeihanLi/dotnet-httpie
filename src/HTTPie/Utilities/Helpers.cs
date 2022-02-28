// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Implement;
using HTTPie.Middleware;
using HTTPie.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.CommandLine.Builder;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace HTTPie.Utilities;

public static class Helpers
{
    private static readonly HashSet<string> HttpMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethod.Head.Method,
        HttpMethod.Get.Method,
        HttpMethod.Post.Method,
        HttpMethod.Put.Method,
        HttpMethod.Patch.Method,
        HttpMethod.Delete.Method,
        HttpMethod.Options.Method
    };

    private static readonly string[] UsageExamples =
    {
        "http :5000/api/values",
        "http localhost:5000/api/values",
        "http https://reservation.weihanli.xyz/api/notice",
        "http post /api/notice title=test body=test-body"
    };

    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static readonly HashSet<Option> SupportedOptions = new();

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

    public static void InitializeSupportOptions(IServiceProvider serviceProvider)
    {
        if (SupportedOptions.Count == 0)
        {
            foreach (var option in
                serviceProvider.GetServices<IHttpHandlerMiddleware>()
                   .SelectMany(x => x.SupportedOptions())
                   .Union(serviceProvider.GetServices<IRequestMiddleware>()
                    .SelectMany(x => x.SupportedOptions())
                    .Union(serviceProvider.GetServices<IResponseMiddleware>()
                   .SelectMany(x => x.SupportedOptions()))
                    .Union(serviceProvider.GetRequiredService<IOutputFormatter>().SupportedOptions())
                    .Union(serviceProvider.GetRequiredService<IRequestExecutor>().SupportedOptions()))
                )
            {
                SupportedOptions.Add(option);
            }
        }
        var command = InitializeCommand();
        var builder = new CommandLineBuilder(command);
        builder.UseDefaults();
        _commandParser = builder.Build();
    }

    private static Parser _commandParser = null!;

    private static Command InitializeCommand()
    {
        var command = new RootCommand()
        {
            Name = "http",
        };
        //var methodArgument = new Argument<HttpMethod>("method")
        //{
        //    Description = "Request method",
        //    Arity = ArgumentArity.ZeroOrOne,
        //};
        //methodArgument.SetDefaultValue(HttpMethod.Get.Method);
        //var allowedMethods = HttpMethods.ToArray();
        //methodArgument.AddSuggestions(allowedMethods);

        //command.AddArgument(methodArgument);
        //var urlArgument = new Argument<string>("url")
        //{
        //    Description = "Request url",
        //    Arity = ArgumentArity.ExactlyOne
        //};
        //command.AddArgument(urlArgument);

        foreach (var option in SupportedOptions)
        {
            command.AddOption(option);
        }
        command.SetHandler(async (ParseResult parseResult, IConsole console) =>
        {
            try
            {
                var context = DependencyResolver.ResolveRequiredService<HttpContext>();
                await DependencyResolver.ResolveRequiredService<IRequestExecutor>()
                    .ExecuteAsync(context);
                var output = DependencyResolver.ResolveRequiredService<IOutputFormatter>()
                    .GetOutput(context);
                console.Out.Write(output);
            }
            catch (Exception e)
            {
                console.Error.Write(e.ToString());
            }
        });
        command.TreatUnmatchedTokensAsErrors = false;
        return command;
    }

    // ReSharper disable once InconsistentNaming
    public static IServiceCollection RegisterHTTPieServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IRequestExecutor, RequestExecutor>()
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
        .AddSingleton(sp => new HttpContext(sp.GetRequiredService<HttpRequestModel>()))
        .AddSingleton<ILogger>(sp =>
            sp.GetRequiredService<ILoggerFactory>().CreateLogger(Constants.ApplicationName));

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
            .AddRequestMiddleware<AuthenticationMiddleware>()
            ;
        // ResponseMiddleware
        serviceCollection.AddResponseMiddleware<DefaultResponseMiddleware>();

        return serviceCollection;
    }

    public static void InitRequestModel(HttpContext httpContext, string commandLine)
        => InitRequestModel(httpContext, CommandLineStringSplitter.Instance.Split(commandLine).ToArray());

    public static void InitRequestModel(HttpContext httpContext, string[] args)
    {
        if (args.Contains("--help"))
        {
            return;
        }
        var requestModel = httpContext.Request;
        requestModel.ParseResult = _commandParser.Parse(args);

        var method = requestModel.ParseResult.UnmatchedTokens.FirstOrDefault(x => HttpMethods.Contains(x));
        if (!string.IsNullOrEmpty(method))
        {
            requestModel.Method = new HttpMethod(method);
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
        var urlIndex = Array.IndexOf(args, requestModel.Url);

        requestModel.Options = args
            .Where(x => x.StartsWith('-'))
            .ToArray();
#nullable disable
        requestModel.RequestItems = requestModel.ParseResult.UnmatchedTokens
            .Except(new[] { method, requestModel.Url })
            .Where(x => !x.StartsWith('-'))
            .ToArray();
#nullable restore
    }

    public static async Task<int> Handle(this IServiceProvider services, string[] args)
    {
        InitRequestModel(services.GetRequiredService<HttpContext>(), args);
        return await _commandParser.InvokeAsync(args);
    }

    public static async Task<int> Handle(this IServiceProvider services, string commandLine)
    {
        InitRequestModel(services.GetRequiredService<HttpContext>(), commandLine);
        return await _commandParser.InvokeAsync(commandLine);
    }
}
