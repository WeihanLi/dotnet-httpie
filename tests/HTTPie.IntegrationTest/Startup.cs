using HTTPie.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

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