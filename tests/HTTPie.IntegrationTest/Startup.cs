using HTTPie.Utilities;
using Microsoft.Extensions.DependencyInjection;
using WeihanLi.Common;

namespace HTTPie.IntegrationTest
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.RegisterHTTPieServices();
        }

        public void Configure(IServiceProvider services)
        {
            DependencyResolver.SetDependencyResolver(services);
            Helpers.InitializeSupportOptions(services);
        }
    }
}