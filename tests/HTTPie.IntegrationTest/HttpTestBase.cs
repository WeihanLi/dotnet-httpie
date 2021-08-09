using System;
using System.Threading.Tasks;
using HTTPie.Models;
using HTTPie.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HTTPie.IntegrationTest
{
    public abstract class HttpTestBase
    {
        protected HttpTestBase(IServiceProvider serviceProvider)
        {
            Services = serviceProvider;
        }

        protected IServiceProvider Services { get; }

        protected HttpContext GetHttpContext()
        {
            return Services.GetRequiredService<HttpContext>();
        }

        protected HttpRequestModel GetRequest()
        {
            return GetHttpContext().Request;
        }

        protected HttpResponseModel GetResponse()
        {
            return GetHttpContext().Response;
        }

        protected Task<string> GetOutput(string input)
        {
            return Helpers.GetOutput(Services, input.Split(';', ' '));
        }
    }
}