// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using HTTPie.Models;
using HTTPie.Utilities;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace HTTPie.IntegrationTest.Implement;

public class HttpTestCollectionTest(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task ParseSampleCollection_Succeeds()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "TestAssets", "SampleCollection.httptest.json");
        Assert.True(File.Exists(path), $"Test asset not found: {path}");

        await using var stream = File.OpenRead(path);
        var collection = await JsonSerializer.DeserializeAsync(
            stream, AppSerializationContext.Default.HttpTestCollection, TestContext.Current.CancellationToken);

        Assert.NotNull(collection);
        Assert.Equal("SampleCollection", collection.Name);
        Assert.NotEmpty(collection.Variables);
        Assert.Equal("https://httpbin.org", collection.Variables["baseUrl"]);
        Assert.NotEmpty(collection.Groups);
        Assert.Equal(2, collection.Groups.Count);

        var firstGroup = collection.Groups[0];
        Assert.Equal("Get Requests", firstGroup.Name);
        Assert.Equal(2, firstGroup.Requests.Count);

        var firstRequest = firstGroup.Requests[0];
        Assert.Equal("get anything", firstRequest.Name);
        Assert.Equal("GET", firstRequest.Method);
        Assert.Equal("{{baseUrl}}/get", firstRequest.Url);
        Assert.Equal("$response.EnsureSuccessStatusCode()", firstRequest.PostScript);

        outputHelper.WriteLine(JsonSerializer.Serialize(collection, AppSerializationContext.Default.HttpTestCollection));
    }

    [Fact]
    public async Task ParseSampleEnvironmentFile_Succeeds()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "TestAssets", "SampleCollection.httptest.env.json");
        Assert.True(File.Exists(path), $"Test asset not found: {path}");

        await using var stream = File.OpenRead(path);
        var environments = await JsonSerializer.DeserializeAsync(
            stream, AppSerializationContext.Default.ListHttpTestEnvironment, TestContext.Current.CancellationToken);

        Assert.NotNull(environments);
        Assert.Equal(2, environments.Count);
        Assert.Equal("dev", environments[0].Name);
        Assert.Equal("staging", environments[1].Name);
        Assert.Equal("https://httpbin.org", environments[0].Variables["baseUrl"]);
    }

    [Fact]
    public async Task TestCommand_OfflineMode_DoesNotSendRequests()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "TestAssets", "SampleCollection.httptest.json");
        Assert.True(File.Exists(path), $"Test asset not found: {path}");

        var services = new ServiceCollection()
            .AddLogging()
            .RegisterApplicationServices()
            .BuildServiceProvider();

        // Offline mode should succeed (print requests without sending)
        var exitCode = await services.Handle($"test {path} --offline");
        Assert.Equal(0, exitCode);
    }
}
