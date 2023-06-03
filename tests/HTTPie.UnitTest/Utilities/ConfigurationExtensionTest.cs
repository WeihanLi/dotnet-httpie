// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Configuration;

namespace HTTPie.UnitTest.Utilities;

public class ConfigurationExtensionTest
{
    [Fact(Skip = "InCompleted")]
    public void ToJsonNodeTest()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>()
            {
                { "Name", "Alice" }, { "Age", "10" }, { "Job:Title", "Middle" }, { "Job:Name", "Developer" }
            })
            .Build();
        var jsonNode = configuration.ToJsonNode();
        Assert.NotNull(jsonNode);
        Assert.Equal("Alice", jsonNode["Name"]?.GetValue<string>());
        Assert.Equal("10", jsonNode["Age"]?.GetValue<string>());
        Assert.Equal("Developer", jsonNode["Job"]?["Name"]?.GetValue<string>());
    }
}
