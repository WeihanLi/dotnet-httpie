// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using System.Text.Json;

namespace HTTPie.UnitTest.Models;

public class HttpTestCollectionTest
{
    [Fact]
    public void HttpTestCollection_DefaultValues()
    {
        var collection = new HttpTestCollection();
        Assert.Equal(string.Empty, collection.Name);
        Assert.Empty(collection.Variables);
        Assert.Null(collection.PreScript);
        Assert.Null(collection.PostScript);
        Assert.Empty(collection.Groups);
    }

    [Fact]
    public void HttpTestGroup_DefaultValues()
    {
        var group = new HttpTestGroup();
        Assert.Equal(string.Empty, group.Name);
        Assert.Empty(group.Variables);
        Assert.Null(group.PreScript);
        Assert.Null(group.PostScript);
        Assert.Empty(group.Requests);
    }

    [Fact]
    public void HttpTestRequest_DefaultValues()
    {
        var request = new HttpTestRequest();
        Assert.Equal(string.Empty, request.Name);
        Assert.Equal("GET", request.Method);
        Assert.Equal(string.Empty, request.Url);
        Assert.Empty(request.Headers);
        Assert.Null(request.Body);
        Assert.Empty(request.Variables);
        Assert.Null(request.PreScript);
        Assert.Null(request.PostScript);
    }

    [Fact]
    public void HttpTestEnvironment_DefaultValues()
    {
        var env = new HttpTestEnvironment();
        Assert.Equal(string.Empty, env.Name);
        Assert.Empty(env.Variables);
    }

    [Fact]
    public void HttpTestCollection_SerializeDeserialize_RoundTrip()
    {
        var collection = new HttpTestCollection
        {
            Name = "TestCollection",
            Variables = new Dictionary<string, string> { { "baseUrl", "https://example.com" } },
            PreScript = "$request.headers.add(\"apiKey\", \"test\")",
            Groups =
            [
                new HttpTestGroup
                {
                    Name = "Group1",
                    Requests =
                    [
                        new HttpTestRequest
                        {
                            Name = "Request1",
                            Method = "GET",
                            Url = "{{baseUrl}}/api/test",
                            PostScript = "$response.EnsureSuccessStatusCode()"
                        }
                    ]
                }
            ]
        };

        var json = JsonSerializer.Serialize(collection, AppSerializationContext.Default.HttpTestCollection);
        Assert.NotEmpty(json);

        var deserialized = JsonSerializer.Deserialize(json, AppSerializationContext.Default.HttpTestCollection);
        Assert.NotNull(deserialized);
        Assert.Equal(collection.Name, deserialized.Name);
        Assert.Equal(collection.Variables["baseUrl"], deserialized.Variables["baseUrl"]);
        Assert.Equal(collection.PreScript, deserialized.PreScript);
        Assert.Single(deserialized.Groups);
        Assert.Equal(collection.Groups[0].Name, deserialized.Groups[0].Name);
        Assert.Single(deserialized.Groups[0].Requests);
        Assert.Equal(collection.Groups[0].Requests[0].PostScript, deserialized.Groups[0].Requests[0].PostScript);
    }

    [Fact]
    public void HttpTestEnvironment_SerializeDeserialize_RoundTrip()
    {
        var environments = new List<HttpTestEnvironment>
        {
            new() { Name = "dev", Variables = new() { { "baseUrl", "https://dev.example.com" } } },
            new() { Name = "prod", Variables = new() { { "baseUrl", "https://prod.example.com" } } }
        };

        var json = JsonSerializer.Serialize(environments, AppSerializationContext.Default.ListHttpTestEnvironment);
        Assert.NotEmpty(json);

        var deserialized = JsonSerializer.Deserialize(json, AppSerializationContext.Default.ListHttpTestEnvironment);
        Assert.NotNull(deserialized);
        Assert.Equal(2, deserialized.Count);
        Assert.Equal("dev", deserialized[0].Name);
        Assert.Equal("prod", deserialized[1].Name);
    }
}
