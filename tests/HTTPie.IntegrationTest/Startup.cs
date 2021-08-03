using HTTPie.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HTTPie.IntegrationTest
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.RegisterHTTPieServices();
        }

        public void Configure()
        {
        }
    }
}