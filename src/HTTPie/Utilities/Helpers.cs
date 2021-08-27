using HTTPie.Abstractions;
using HTTPie.Implement;
using HTTPie.Middleware;
using HTTPie.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WeihanLi.Common;
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
                   ))
                {
                    SupportedOptions.Add(option);
                }
            }
            _command = InitializeCommand();
        }

        private static Command _command = null!;
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
            command.Handler = CommandHandler.Create(async (ParseResult parseResult, IConsole console) =>
            {
                var context = DependencyResolver.ResolveService<HttpContext>();
                await DependencyResolver.ResolveService<IRequestExecutor>()
                  .ExecuteAsync(context);
                var output = DependencyResolver.ResolveService<IOutputFormatter>()
                  .GetOutput(context);
                console.Out.Write(output);
            });
            command.TreatUnmatchedTokensAsErrors = false;
            return command;
        }
        public static string GetHelpText()
        {
            var helpTextBuilder = new StringBuilder();
            helpTextBuilder.AppendLine("Supported parameters:");
            helpTextBuilder.AppendLine("\tParameter Name\t\tParameter Description");
            helpTextBuilder.AppendLine($"\t{new[] { "--debug", "debug mode, output debug log" }.StringJoin("\t\t\t")}");

            foreach (var option in SupportedOptions)
            {
                helpTextBuilder.AppendLine($"\t{option.Name}\t{option.Aliases.StringJoin(",")}\t{option.Description}");
            }

            helpTextBuilder.AppendLine("Usage examples:");
            foreach (var example in UsageExamples) helpTextBuilder.AppendLine($"\t{example}");
            return helpTextBuilder.ToString();
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
                ;
            // ResponseMiddleware
            serviceCollection.AddResponseMiddleware<DefaultResponseMiddleware>();

            return serviceCollection;
        }

        public static void InitRequestModel(HttpRequestModel requestModel, string commandLine)
            => InitRequestModel(requestModel, CommandLineStringSplitter.Instance.Split(commandLine).ToArray());

        public static void InitRequestModel(HttpRequestModel requestModel, string[] args)
        {
            requestModel.ParseResult = _command.Parse(args);

            var method = args.FirstOrDefault(x => HttpMethods.Contains(x));
            if (!string.IsNullOrEmpty(method))
            {
                requestModel.Method = new HttpMethod(method);
            }
            // Url
            requestModel.Url = args.FirstOrDefault(x =>
                  !x.StartsWith("-", StringComparison.Ordinal)
                  && !HttpMethods.Contains(x))
                ?? string.Empty;

            requestModel.Options = args
                .Where(x => x.StartsWith('-'))
                .ToArray();
            requestModel.Arguments = args
                .Except(new[] { method, requestModel.Url })
                .Except(requestModel.Options)
                .Select(x=> x.GetValueOrDefault(string.Empty))
                .ToArray();
        }

        public static async Task<int> Handle(this IServiceProvider services, string[] args)
        {
            InitRequestModel(services.GetRequiredService<HttpRequestModel>(), args);
            return await _command.InvokeAsync(args);
        }

        public static async Task<int> Handle(this IServiceProvider services, string commandLine)
        {
            InitRequestModel(services.GetRequiredService<HttpRequestModel>(), commandLine);
            return await _command.InvokeAsync(commandLine);
        }        
    }
}