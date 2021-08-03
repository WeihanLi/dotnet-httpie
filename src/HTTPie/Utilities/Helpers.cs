using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using HTTPie.Abstractions;
using HTTPie.Implement;
using HTTPie.Middleware;
using HTTPie.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using WeihanLi.Common.Helpers;
using WeihanLi.Extensions;

namespace HTTPie.Utilities
{
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

        public static string GetHelpText(IServiceProvider serviceProvider)
        {
            var helpTextBuilder = new StringBuilder();
            helpTextBuilder.AppendLine("Supported parameters:");
            helpTextBuilder.AppendLine("\tParameter Name\t\tParameter Description");
            helpTextBuilder.AppendLine($"\t{new[] {"--debug", "debug mode, output debug log"}.StringJoin("\t\t\t")}");
            foreach (var parameter in serviceProvider.GetServices<IHttpHandlerMiddleware>()
                .SelectMany(x => x.SupportedParameters())
            )
                helpTextBuilder.AppendLine($"\t{parameter.Key}\t\t{parameter.Value}");
            foreach (var parameter in serviceProvider.GetServices<IRequestMiddleware>()
                .SelectMany(x => x.SupportedParameters())
            )
                helpTextBuilder.AppendLine($"\t{parameter.Key}\t\t{parameter.Value}");
            foreach (var parameter in serviceProvider.GetServices<IResponseMiddleware>()
                .SelectMany(x => x.SupportedParameters())
            )
                helpTextBuilder.AppendLine($"\t{parameter.Key}\t\t{parameter.Value}");
            foreach (var parameter in serviceProvider.GetRequiredService<IOutputFormatter>().SupportedParameters())
                helpTextBuilder.AppendLine($"\t{parameter.Key}\t\t{parameter.Value}");

            helpTextBuilder.AppendLine("Usage examples:");
            foreach (var example in UsageExamples) helpTextBuilder.AppendLine($"\t{example}");

            return helpTextBuilder.ToString();
        }

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

        // ReSharper disable once InconsistentNaming
        public static IServiceCollection RegisterHTTPieServices(this IServiceCollection serviceCollection,
            bool debugEnabled = false)
        {
            serviceCollection.AddLogging(builder =>
                    builder.AddConsole().SetMinimumLevel(debugEnabled ? LogLevel.Debug : LogLevel.Warning))
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
                .AddSingleton<ILogger>(sp => sp.GetRequiredService<ILoggerFactory>().CreateLogger(Constants.ApplicationName));

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

            return serviceCollection;
        }

        public static void InitRequestModel(HttpRequestModel requestModel, string[] args)
        {
            if (args[0].EndsWith("HTTPie.dll", StringComparison.OrdinalIgnoreCase)) args = args[1..];
            requestModel.RawInput = args;
            var method = args.FirstOrDefault(x => HttpMethods.Contains(x));
            if (!string.IsNullOrEmpty(method)) requestModel.Method = new HttpMethod(method);
            // Url
            requestModel.Url =
                args.FirstOrDefault(x => !x.StartsWith("-", StringComparison.Ordinal) && !HttpMethods.Contains(x)) ??
                string.Empty;
            var schema = requestModel.RawInput.FirstOrDefault(x => x.StartsWith("--schema="))?["--schema=".Length..];
            if (!string.IsNullOrEmpty(schema)) requestModel.Schema = schema;

            if (requestModel.Url == ":")
            {
                requestModel.Url = "localhost";
            }
            else
            {
                if (requestModel.Url.StartsWith(":/")) requestModel.Url = $"localhost{requestModel.Url[1..]}";
                if (requestModel.Url.StartsWith(':')) requestModel.Url = $"localhost{requestModel.Url}";
            }

            if (requestModel.Url.IndexOf("://", StringComparison.Ordinal) < 0)
                requestModel.Url = $"{requestModel.Schema}://{requestModel.Url}";
            if (requestModel.Url.StartsWith("://"))
                requestModel.Url = $"{requestModel.Schema}{requestModel.Url}";
        }
    }
}