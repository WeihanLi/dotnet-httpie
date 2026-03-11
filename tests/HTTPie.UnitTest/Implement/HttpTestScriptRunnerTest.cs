// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

namespace HTTPie.UnitTest.Implement;

public class HttpTestScriptRunnerTest
{
    // -----------------------------------------------------------------------
    // preScript – simple fast-path (regex)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ExecutePreScript_Null_DoesNothing()
    {
        var headers = new Dictionary<string, string>();
        await HttpTestScriptRunner.ExecutePreScriptAsync(null, headers);
        Assert.Empty(headers);
    }

    [Fact]
    public async Task ExecutePreScript_Empty_DoesNothing()
    {
        var headers = new Dictionary<string, string>();
        await HttpTestScriptRunner.ExecutePreScriptAsync(string.Empty, headers);
        Assert.Empty(headers);
    }

    [Fact]
    public async Task ExecutePreScript_AddHeader()
    {
        var headers = new Dictionary<string, string>();
        await HttpTestScriptRunner.ExecutePreScriptAsync("$request.headers.add(\"apiKey\", \"test123\")", headers);
        Assert.Single(headers);
        Assert.Equal("test123", headers["apiKey"]);
    }

    [Fact]
    public async Task ExecutePreScript_SetHeader()
    {
        var headers = new Dictionary<string, string> { { "apiKey", "oldValue" } };
        await HttpTestScriptRunner.ExecutePreScriptAsync("$request.headers.set(\"apiKey\", \"newValue\")", headers);
        Assert.Single(headers);
        Assert.Equal("newValue", headers["apiKey"]);
    }

    [Fact]
    public async Task ExecutePreScript_MultipleLines()
    {
        var headers = new Dictionary<string, string>();
        var script = """
            $request.headers.add("X-Api-Key", "key1")
            $request.headers.add("X-Tenant", "tenant1")
            """;
        await HttpTestScriptRunner.ExecutePreScriptAsync(script, headers);
        Assert.Equal(2, headers.Count);
        Assert.Equal("key1", headers["X-Api-Key"]);
        Assert.Equal("tenant1", headers["X-Tenant"]);
    }

    [Fact]
    public async Task ExecutePreScript_IgnoresCommentLines()
    {
        var headers = new Dictionary<string, string>();
        var script = """
            // This is a comment
            # This is also a comment
            $request.headers.add("X-Key", "value")
            """;
        await HttpTestScriptRunner.ExecutePreScriptAsync(script, headers);
        Assert.Single(headers);
        Assert.Equal("value", headers["X-Key"]);
    }

    // -----------------------------------------------------------------------
    // preScript – complex C# (Roslyn)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ExecutePreScript_ComplexCSharp_SetHeaderViaVariable()
    {
        var headers = new Dictionary<string, string>();
        var script = """
            var key = "dynamic-value";
            request.headers.set("X-Dynamic", key);
            """;
        await HttpTestScriptRunner.ExecutePreScriptAsync(script, headers);
        Assert.Equal("dynamic-value", headers["X-Dynamic"]);
    }

    [Fact]
    public async Task ExecutePreScript_ComplexCSharp_DollarSyntaxStillWorks()
    {
        // $request is preprocessed to request, so shorthand syntax works in Roslyn scripts too
        var headers = new Dictionary<string, string>();
        var script = """
            var tenant = "acme";
            $request.headers.set("X-Tenant", tenant);
            """;
        await HttpTestScriptRunner.ExecutePreScriptAsync(script, headers);
        Assert.Equal("acme", headers["X-Tenant"]);
    }

    [Fact]
    public async Task ExecutePreScript_ComplexCSharp_ConditionalHeader()
    {
        var headers = new Dictionary<string, string> { { "X-Mode", "debug" } };
        var script = """
            if (request.headers.get("X-Mode") == "debug")
            {
                request.headers.set("X-Debug-Token", "abc123");
            }
            """;
        await HttpTestScriptRunner.ExecutePreScriptAsync(script, headers);
        Assert.Equal("abc123", headers["X-Debug-Token"]);
    }

    [Fact]
    public async Task ExecutePreScript_ComplexCSharp_CompilationError_Throws()
    {
        var headers = new Dictionary<string, string>();
        var ex = await Assert.ThrowsAsync<HttpTestAssertionException>(() =>
            HttpTestScriptRunner.ExecutePreScriptAsync("this is not valid C#!!!", headers));
        Assert.Contains("compilation error", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // postScript – simple fast-path (regex)
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

    // -----------------------------------------------------------------------
    // postScript – complex C# (Roslyn)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ExecutePostScript_ComplexCSharp_EnsureSuccessStatusCode()
    {
        var script = "response.EnsureSuccessStatusCode();";
        await HttpTestScriptRunner.ExecutePostScriptAsync(script, 200, () => Task.FromResult(string.Empty));
    }

    [Fact]
    public async Task ExecutePostScript_ComplexCSharp_EnsureSuccessStatusCode_Fails()
    {
        var script = "response.EnsureSuccessStatusCode();";
        await Assert.ThrowsAsync<HttpTestAssertionException>(() =>
            HttpTestScriptRunner.ExecutePostScriptAsync(script, 500, () => Task.FromResult(string.Empty)));
    }

    [Fact]
    public async Task ExecutePostScript_ComplexCSharp_AssertHelper()
    {
        var script = "response.Assert(response.StatusCode == 200, \"Expected 200\");";
        await HttpTestScriptRunner.ExecutePostScriptAsync(script, 200, () => Task.FromResult(string.Empty));
    }

    [Fact]
    public async Task ExecutePostScript_ComplexCSharp_AssertHelper_Fails()
    {
        var script = "response.Assert(response.StatusCode == 201, \"Expected 201\");";
        var ex = await Assert.ThrowsAsync<HttpTestAssertionException>(() =>
            HttpTestScriptRunner.ExecutePostScriptAsync(script, 200, () => Task.FromResult(string.Empty)));
        Assert.Contains("Expected 201", ex.Message);
    }

    [Fact]
    public async Task ExecutePostScript_ComplexCSharp_BodyAssertion()
    {
        var script = """
            var body = await response.GetBodyAsync();
            response.Assert(body.Contains("hello"), "Body should contain hello");
            """;
        await HttpTestScriptRunner.ExecutePostScriptAsync(
            script, 200, () => Task.FromResult("{\"message\": \"hello world\"}"));
    }

    [Fact]
    public async Task ExecutePostScript_ComplexCSharp_BodyAssertion_Fails()
    {
        var script = """
            var body = await response.GetBodyAsync();
            response.Assert(body.Contains("missing"), "Body should contain missing");
            """;
        var ex = await Assert.ThrowsAsync<HttpTestAssertionException>(() =>
            HttpTestScriptRunner.ExecutePostScriptAsync(
                script, 200, () => Task.FromResult("{\"message\": \"hello world\"}")));
        Assert.Contains("missing", ex.Message);
    }

    [Fact]
    public async Task ExecutePostScript_ComplexCSharp_DollarSyntaxStillWorks()
    {
        // $response is preprocessed to response, so shorthand syntax works in Roslyn scripts too
        var script = """
            $response.EnsureSuccessStatusCode();
            var body = await response.GetBodyAsync();
            response.Assert(body.Contains("ok"), "Body should contain ok");
            """;
        await HttpTestScriptRunner.ExecutePostScriptAsync(
            script, 200, () => Task.FromResult("{\"status\": \"ok\"}"));
    }

    [Fact]
    public async Task ExecutePostScript_ComplexCSharp_CompilationError_Throws()
    {
        var ex = await Assert.ThrowsAsync<HttpTestAssertionException>(() =>
            HttpTestScriptRunner.ExecutePostScriptAsync(
                "this is not valid C#!!!",
                200,
                () => Task.FromResult(string.Empty)));
        Assert.Contains("compilation error", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}

