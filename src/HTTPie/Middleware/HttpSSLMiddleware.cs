using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading.Tasks;
using HTTPie.Abstractions;
using HTTPie.Models;

namespace HTTPie.Middleware
{
    public class HttpSslMiddleware : IHttpHandlerMiddleware
    {
        private readonly HttpRequestModel _requestModel;

        public HttpSslMiddleware(HttpRequestModel requestModel)
        {
            _requestModel = requestModel;
        }

        public Dictionary<string, string> SupportedParameters()
        {
            return new Dictionary<string, string>
            {
                { "--verify=no", "disable ssl cert check" },
                { "--ssl", "specific the ssl protocols, ssl3, tls, tls1.1, tls1.2, tls1.3" }
            };
        }

        public Task Invoke(HttpClientHandler httpClientHandler, Func<Task> next)
        {
            if (_requestModel.RawInput.Contains("--verify=no"))
                // ignore server cert
                httpClientHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
            // sslProtocols
            var sslOption = _requestModel.RawInput.FirstOrDefault(x => x.StartsWith("--ssl="))?["--ssl=".Length..];
            if (!string.IsNullOrEmpty(sslOption))
            {
                sslOption = sslOption.Replace(".", string.Empty);
                if (Enum.TryParse(sslOption, out SslProtocols sslProtocols))
                    httpClientHandler.SslProtocols = sslProtocols;
            }

            return next();
        }
    }
}