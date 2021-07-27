using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using HTTPie.Abstractions;
using HTTPie.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            helpTextBuilder.AppendLine("Parameter Name\t\tParameter Description");
            foreach (var parameter in serviceProvider.GetServices<IHttpHandlerMiddleware>()
                .SelectMany(x => x.SupportedParameters())
            )
                helpTextBuilder.AppendLine($"{parameter.Key}\t\t{parameter.Value}");
            foreach (var parameter in serviceProvider.GetServices<IRequestMiddleware>()
                .SelectMany(x => x.SupportedParameters())
            )
                helpTextBuilder.AppendLine($"{parameter.Key}\t\t{parameter.Value}");
            foreach (var parameter in serviceProvider.GetServices<IResponseMiddleware>()
                .SelectMany(x => x.SupportedParameters())
            )
                helpTextBuilder.AppendLine($"{parameter.Key}\t\t{parameter.Value}");
            foreach (var parameter in serviceProvider.GetRequiredService<IOutputFormatter>().SupportedParameters())
                helpTextBuilder.AppendLine($"{parameter.Key}\t\t{parameter.Value}");
            return helpTextBuilder.ToString();
        }

        public static void InitRequestModel(HttpRequestModel requestModel, string[] args)
        {
            requestModel.RawInput = args;
            try
            {
                requestModel.RawConfiguration = new ConfigurationBuilder()
                    .AddCommandLine(args)
                    .Build();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                requestModel.RawConfiguration = new ConfigurationBuilder().Build();
            }

            var method = args.FirstOrDefault(x => HttpMethods.Contains(x));
            if (!string.IsNullOrEmpty(method)) requestModel.Method = new HttpMethod(method);
            // Url
            requestModel.Url =
                args.FirstOrDefault(x => !x.StartsWith("-", StringComparison.Ordinal) && !HttpMethods.Contains(x)) ??
                string.Empty;
#if DEBUG
            if (string.IsNullOrEmpty(requestModel.Url)) requestModel.Url = "https://reservation.weihanli.xyz/health";
#endif
        }
    }
}