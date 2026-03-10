// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

namespace HTTPie.UnitTest.Implement;

public class HttpTestScriptRunnerTest
{
    // -----------------------------------------------------------------------
    // preScript tests
    // -----------------------------------------------------------------------

    [Fact]
    public void ExecutePreScript_Null_DoesNothing()
    {
        var headers = new Dictionary<string, string>();
        HttpTestScriptRunner.ExecutePreScript(null, headers);
        Assert.Empty(headers);
    }

    [Fact]
    public void ExecutePreScript_Empty_DoesNothing()
    {
        var headers = new Dictionary<string, string>();
        HttpTestScriptRunner.ExecutePreScript(string.Empty, headers);
        Assert.Empty(headers);
    }

    [Fact]
    public void ExecutePreScript_AddHeader()
    {
        var headers = new Dictionary<string, string>();
        HttpTestScriptRunner.ExecutePreScript("$request.headers.add(\"apiKey\", \"test123\")", headers);
        Assert.Single(headers);
        Assert.Equal("test123", headers["apiKey"]);
    }

    [Fact]
    public void ExecutePreScript_SetHeader()
    {
        var headers = new Dictionary<string, string> { { "apiKey", "oldValue" } };
        HttpTestScriptRunner.ExecutePreScript("$request.headers.set(\"apiKey\", \"newValue\")", headers);
        Assert.Single(headers);
        Assert.Equal("newValue", headers["apiKey"]);
    }

    [Fact]
    public void ExecutePreScript_MultipleLines()
    {
        var headers = new Dictionary<string, string>();
        var script = """
            $request.headers.add("X-Api-Key", "key1")
            $request.headers.add("X-Tenant", "tenant1")
            """;
        HttpTestScriptRunner.ExecutePreScript(script, headers);
        Assert.Equal(2, headers.Count);
        Assert.Equal("key1", headers["X-Api-Key"]);
        Assert.Equal("tenant1", headers["X-Tenant"]);
    }

    [Fact]
    public void ExecutePreScript_IgnoresCommentLines()
    {
        var headers = new Dictionary<string, string>();
        var script = """
            // This is a comment
            # This is also a comment
            $request.headers.add("X-Key", "value")
            """;
        HttpTestScriptRunner.ExecutePreScript(script, headers);
        Assert.Single(headers);
        Assert.Equal("value", headers["X-Key"]);
    }

    // -----------------------------------------------------------------------
    // postScript tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ExecutePostScript_Null_DoesNothing()
    {
        // Should not throw
        await HttpTestScriptRunner.ExecutePostScriptAsync(null, 200, () => Task.FromResult(string.Empty));
    }

    [Theory]
    [InlineData(200)]
    [InlineData(201)]
    [InlineData(204)]
    [InlineData(299)]
    public async Task ExecutePostScript_EnsureSuccessStatusCode_PassesFor2xx(int statusCode)
    {
        await HttpTestScriptRunner.ExecutePostScriptAsync(
            "$response.EnsureSuccessStatusCode()",
            statusCode,
            () => Task.FromResult(string.Empty));
    }

    [Theory]
    [InlineData(400)]
    [InlineData(404)]
    [InlineData(500)]
    public async Task ExecutePostScript_EnsureSuccessStatusCode_ThrowsForNon2xx(int statusCode)
    {
        await Assert.ThrowsAsync<HttpTestAssertionException>(() =>
            HttpTestScriptRunner.ExecutePostScriptAsync(
                "$response.EnsureSuccessStatusCode()",
                statusCode,
                () => Task.FromResult(string.Empty)));
    }

    [Fact]
    public async Task ExecutePostScript_StatusCodeEquals_Passes()
    {
        await HttpTestScriptRunner.ExecutePostScriptAsync(
            "$response.StatusCode == 200",
            200,
            () => Task.FromResult(string.Empty));
    }

    [Fact]
    public async Task ExecutePostScript_StatusCodeEquals_Fails()
    {
        await Assert.ThrowsAsync<HttpTestAssertionException>(() =>
            HttpTestScriptRunner.ExecutePostScriptAsync(
                "$response.StatusCode == 200",
                404,
                () => Task.FromResult(string.Empty)));
    }

    [Fact]
    public async Task ExecutePostScript_StatusCodeNotEquals_Passes()
    {
        await HttpTestScriptRunner.ExecutePostScriptAsync(
            "$response.StatusCode != 404",
            200,
            () => Task.FromResult(string.Empty));
    }

    [Fact]
    public async Task ExecutePostScript_StatusCodeNotEquals_Fails()
    {
        await Assert.ThrowsAsync<HttpTestAssertionException>(() =>
            HttpTestScriptRunner.ExecutePostScriptAsync(
                "$response.StatusCode != 200",
                200,
                () => Task.FromResult(string.Empty)));
    }

    [Fact]
    public async Task ExecutePostScript_BodyContains_Passes()
    {
        await HttpTestScriptRunner.ExecutePostScriptAsync(
            "$response.Body.Contains(\"hello\")",
            200,
            () => Task.FromResult("{\"message\": \"hello world\"}"));
    }

    [Fact]
    public async Task ExecutePostScript_BodyContains_Fails()
    {
        await Assert.ThrowsAsync<HttpTestAssertionException>(() =>
            HttpTestScriptRunner.ExecutePostScriptAsync(
                "$response.Body.Contains(\"missing\")",
                200,
                () => Task.FromResult("{\"message\": \"hello world\"}")));
    }

    [Fact]
    public async Task ExecutePostScript_MultipleAssertions_AllPass()
    {
        var script = """
            $response.EnsureSuccessStatusCode()
            $response.StatusCode == 200
            $response.Body.Contains("ok")
            """;
        await HttpTestScriptRunner.ExecutePostScriptAsync(
            script,
            200,
            () => Task.FromResult("{\"status\": \"ok\"}"));
    }

    [Fact]
    public async Task ExecutePostScript_MultipleAssertions_FirstFails()
    {
        var script = """
            $response.StatusCode == 201
            $response.EnsureSuccessStatusCode()
            """;
        var ex = await Assert.ThrowsAsync<HttpTestAssertionException>(() =>
            HttpTestScriptRunner.ExecutePostScriptAsync(
                script,
                200,
                () => Task.FromResult(string.Empty)));
        Assert.Contains("201", ex.Message);
    }

    [Fact]
    public async Task ExecutePostScript_IgnoresCommentLines()
    {
        var script = """
            // This checks status code
            $response.EnsureSuccessStatusCode()
            """;
        await HttpTestScriptRunner.ExecutePostScriptAsync(
            script,
            200,
            () => Task.FromResult(string.Empty));
    }
}
