// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace HTTPie.UnitTest;

public class Startup
{
    public void ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddLogging();
        serviceCollection.RegisterHTTPieServices();
    }

    public void Configure(IServiceProvider services)
    {
        DependencyResolver.SetDependencyResolver(services);
        Helpers.InitializeSupportOptions(services);
    }
}
