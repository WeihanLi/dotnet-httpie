// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Abstractions;
using HTTPie.Commands;
using HTTPie.Implement;
using HTTPie.Middleware;
using HTTPie.Models;
using Json.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using WeihanLi.Common.Extensions;

namespace HTTPie.Utilities;

[JsonSerializable(typeof(HttpRequestModel))]
[JsonSerializable(typeof(HttpResponseModel))]
[JsonSerializable(typeof(JsonSchema), GenerationMode = JsonSourceGenerationMode.Serialization)]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true)]
public sealed partial class AppSerializationContext : JsonSerializerContext;

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
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static IServiceCollection AddHttpHandlerMiddleware
        <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THttpHandlerMiddleware>
        (this IServiceCollection serviceCollection)
        where THttpHandlerMiddleware : class, IHttpHandlerMiddleware
    {
        var serviceDescriptor = ServiceDescriptor.Singleton<IHttpHandlerMiddleware, THttpHandlerMiddleware>();
        serviceCollection.TryAddEnumerable(serviceDescriptor);
        return serviceCollection;
    }

    private static IServiceCollection AddRequestMiddleware
        <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TRequestMiddleware>
        (this IServiceCollection serviceCollection)
        where TRequestMiddleware : class, IRequestMiddleware
    {
        var serviceDescriptor = ServiceDescriptor.Singleton<IRequestMiddleware, TRequestMiddleware>();
        serviceCollection.TryAddEnumerable(serviceDescriptor);
        return serviceCollection;
    }

    private static IServiceCollection AddResponseMiddleware
        <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TResponseMiddleware>
        (this IServiceCollection serviceCollection)
        where TResponseMiddleware : class, IResponseMiddleware
    {
        var serviceDescriptor = ServiceDescriptor.Singleton<IResponseMiddleware, TResponseMiddleware>();
        serviceCollection.TryAddEnumerable(serviceDescriptor);
        return serviceCollection;
    }

    private static Command ConstructCommand(this IServiceProvider serviceProvider,
        Func<ParseResult, CancellationToken, Task>? handler = null)
    {
        var command = InitializeCommandInternal(serviceProvider, handler);
        return command;
    }

    private static Command InitializeCommandInternal(IServiceProvider serviceProvider,
        Func<ParseResult, CancellationToken, Task>? handler = null)
    {
        var command = new RootCommand();
        var executeCommand = new ExecuteCommand();
        executeCommand.SetAction((parseResult, cancellationToken) =>
            executeCommand.InvokeAsync(parseResult, cancellationToken, serviceProvider));
        command.Add(executeCommand);

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
            command.Add(option);
        }

        command.TreatUnmatchedTokensAsErrors = false;
        command.SetAction((parseResult, cancellationToken) => HttpCommandHandler(parseResult, cancellationToken, serviceProvider, handler));
        return command;
    }

    public static IServiceCollection RegisterApplicationServices(this IServiceCollection serviceCollection)
    {
        serviceCollection
            .AddSingleton<IRequestItemParser, RequestItemParser>()
            .AddSingleton<IRequestExecutor, RequestExecutor>()
            .AddSingleton<IHttpParser, HttpParser>()
            .AddSingleton<ICurlParser, CurlParser>()
            .AddSingleton<IRawHttpRequestExecutor, RawHttpRequestExecutor>()
            .AddSingleton<IRequestMapper, RequestMapper>()
            .AddSingleton<IResponseMapper, ResponseMapper>()
            .AddSingleton<IOutputFormatter, OutputFormatter>()
            .AddSingleton<ILoadTestExporterSelector, LoadTestExporterSelector>()
            .AddSingleton<ILoadTestExporter, JsonLoadTestExporter>()
            // request pipeline
            .AddSingleton(sp =>
            {
                var pipelineBuilder = PipelineBuilder.CreateAsync<HttpRequestModel>();
                foreach (var middleware in sp.GetServices<IRequestMiddleware>())
                    pipelineBuilder.UseMiddleware(middleware);
                return pipelineBuilder.Build();
            })
            // response pipeline
            .AddSingleton(sp =>
            {
                var pipelineBuilder = PipelineBuilder.CreateAsync<HttpContext>();
                foreach (var middleware in sp.GetServices<IResponseMiddleware>())
                    pipelineBuilder.UseMiddleware(middleware);
                return pipelineBuilder.Build();
            })
            // httpHandler pipeline
            .AddSingleton(sp =>
            {
                var pipelineBuilder = PipelineBuilder.CreateAsync<HttpClientHandler>();
                foreach (var middleware in sp.GetServices<IHttpHandlerMiddleware>())
                    pipelineBuilder.UseMiddleware(middleware);
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

    public static async Task<int> Handle(this IServiceProvider services, string[] args)
    {
        var commandParser = services.ConstructCommand();
        return await commandParser.Parse(args).InvokeAsync();
    }

    public static async Task<int> Handle(this IServiceProvider services, string commandLine,
        Func<ParseResult, CancellationToken, Task>? handler = null)
    {
        var commandParser = services.ConstructCommand(handler);
        return await commandParser.Parse(commandLine).InvokeAsync();
    }

    private static async Task HttpCommandHandler(ParseResult parseResult, CancellationToken cancellationToken, IServiceProvider serviceProvider,
        Func<ParseResult, CancellationToken, Task>? internalHandler = null)
    {
        var context = serviceProvider.GetRequiredService<HttpContext>();
        context.ParseResult = parseResult;
        context.RequestCancelled = cancellationToken;

        var requestModel = context.Request;
        requestModel.ParseResult = parseResult;

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

        // Check for streaming mode option early by checking tokens
        // This is needed for tests that use an internal handler
        var hasStreamOption = parseResult.Tokens.Any(t =>
            t.Value.Equals("--stream", StringComparison.OrdinalIgnoreCase) ||
            t.Value.Equals("-S", StringComparison.OrdinalIgnoreCase));
        context.UpdateFlag(Constants.FlagNames.IsStreamingMode, hasStreamOption);

        if (internalHandler is null)
        {
            await serviceProvider.ResolveRequiredService<IRequestExecutor>()
                .ExecuteAsync(context);

            // Skip output formatting if streaming actually completed (output already written)
            var streamingCompleted = context.GetFlag(Constants.FlagNames.StreamingCompleted);
            if (!streamingCompleted)
            {
                var output = await serviceProvider.ResolveRequiredService<IOutputFormatter>()
                    .GetOutput(context);
                await Console.Out.WriteLineAsync(output.Trim());
            }
        }
        else
        {
            await internalHandler(parseResult, cancellationToken);
        }
    }

    public static bool HasOption(this ParseResult parseResult, Option option)
    {
        var result = parseResult.GetResult(option);
        return result is { Implicit: false };
    }

    public static string NormalizeHttpVersion(this Version version)
    {
        if (version.Major < 2)
        {
            // http/1.1
            return $"http/{version.Major}.{version.Minor}";
        }

        // h2, h3
        return $"h{version.Major}";
    }

    public static HttpClientHandler GetHttpClientHandler()
    {
        return new HttpClientHandler
        {
            AllowAutoRedirect = false,
            CheckCertificateRevocationList = false,
            UseCookies = false,
            UseDefaultCredentials = false
        };
    }
}
