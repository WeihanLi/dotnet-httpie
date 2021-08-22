using HTTPie.Abstractions;
using HTTPie.Models;
using HTTPie.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HTTPie.IntegrationTest
{
    [Collection("HttpTests")]
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
            return Services.GetRequiredService<HttpRequestModel>();
        }

        protected HttpResponseModel GetResponse()
        {
            return Services.GetRequiredService<HttpContext>().Response;
        }

        protected async Task<int> Handle(string input)
        {
            return await Helpers.Handle(Services, input);
        }

        protected async Task<string> GetOutput(string input)
        {
            await Handle(input);
            return Services.GetRequiredService<IOutputFormatter>()
                .GetOutput(GetHttpContext());
        }

        
    }
}