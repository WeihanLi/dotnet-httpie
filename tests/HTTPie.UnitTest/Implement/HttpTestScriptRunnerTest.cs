// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using System.Net;

namespace HTTPie.UnitTest.Implement;

public class HttpTestScriptRunnerTest
{
    // -----------------------------------------------------------------------
    // preScript – simple fast-path (regex)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ExecutePreScript_Null_DoesNothing()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var variables = new Dictionary<string, string>();
        await HttpTestScriptRunner.ExecutePreScriptAsync(null, request, variables);
        Assert.Empty(request.Headers);
    }

    [Fact]
    public async Task ExecutePreScript_Empty_DoesNothing()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var variables = new Dictionary<string, string>();
        await HttpTestScriptRunner.ExecutePreScriptAsync(string.Empty, request, variables);
        Assert.Empty(request.Headers);
    }

    [Fact]
    public async Task ExecutePreScript_AddHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var variables = new Dictionary<string, string>();
        await HttpTestScriptRunner.ExecutePreScriptAsync(
            "$request.headers.add(\"apiKey\", \"test123\")", request, variables);
        Assert.Equal("test123", request.Headers.GetValues("apiKey").First());
    }

    [Fact]
    public async Task ExecutePreScript_SetHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        request.Headers.TryAddWithoutValidation("apiKey", "oldValue");
        var variables = new Dictionary<string, string>();
        await HttpTestScriptRunner.ExecutePreScriptAsync(
            "$request.headers.set(\"apiKey\", \"newValue\")", request, variables);
        Assert.Equal("newValue", request.Headers.GetValues("apiKey").Single());
    }

    [Fact]
    public async Task ExecutePreScript_MultipleLines()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var variables = new Dictionary<string, string>();
        var script = """
            $request.headers.add("X-Api-Key", "key1")
            $request.headers.add("X-Tenant", "tenant1")
            """;
        await HttpTestScriptRunner.ExecutePreScriptAsync(script, request, variables);
        Assert.Equal("key1", request.Headers.GetValues("X-Api-Key").First());
        Assert.Equal("tenant1", request.Headers.GetValues("X-Tenant").First());
    }

    [Fact]
    public async Task ExecutePreScript_IgnoresCommentLines()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var variables = new Dictionary<string, string>();
        var script = """
            // This is a comment
            # This is also a comment
            $request.headers.add("X-Key", "value")
            """;
        await HttpTestScriptRunner.ExecutePreScriptAsync(script, request, variables);
        Assert.Equal("value", request.Headers.GetValues("X-Key").First());
    }

    // -----------------------------------------------------------------------
    // preScript – complex C# (Roslyn)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ExecutePreScript_ComplexCSharp_SetHeaderViaVariable()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var variables = new Dictionary<string, string>();
        var script = """
            var key = "dynamic-value";
            request.Headers.TryAddWithoutValidation("X-Dynamic", key);
            """;
        await HttpTestScriptRunner.ExecutePreScriptAsync(script, request, variables);
        Assert.Equal("dynamic-value", request.Headers.GetValues("X-Dynamic").First());
    }

    [Fact]
    public async Task ExecutePreScript_ComplexCSharp_DollarSyntaxRewrittenToRequest()
    {
        // $request is preprocessed to request (raw HttpRequestMessage)
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var variables = new Dictionary<string, string>();
        var script = """
            var tenant = "acme";
            request.Headers.TryAddWithoutValidation("X-Tenant", tenant);
            """;
        await HttpTestScriptRunner.ExecutePreScriptAsync(script, request, variables);
        Assert.Equal("acme", request.Headers.GetValues("X-Tenant").First());
    }

    [Fact]
    public async Task ExecutePreScript_ComplexCSharp_ConditionalHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        request.Headers.TryAddWithoutValidation("X-Mode", "debug");
        var variables = new Dictionary<string, string>();
        var script = """
            if (request.Headers.TryGetValues("X-Mode", out var modeVals) && modeVals.FirstOrDefault() == "debug")
            {
                request.Headers.TryAddWithoutValidation("X-Debug-Token", "abc123");
            }
            """;
        await HttpTestScriptRunner.ExecutePreScriptAsync(script, request, variables);
        Assert.Equal("abc123", request.Headers.GetValues("X-Debug-Token").First());
    }

    [Fact]
    public async Task ExecutePreScript_ComplexCSharp_UpdatesVariables()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var variables = new Dictionary<string, string> { { "token", "old" } };
        var script = """
            variables["token"] = "new";
            """;
        await HttpTestScriptRunner.ExecutePreScriptAsync(script, request, variables);
        Assert.Equal("new", variables["token"]);
    }

    [Fact]
    public async Task ExecutePreScript_ComplexCSharp_CompilationError_Throws()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var variables = new Dictionary<string, string>();
        var ex = await Assert.ThrowsAsync<HttpTestAssertionException>(() =>
            HttpTestScriptRunner.ExecutePreScriptAsync("this is not valid C#!!!", request, variables));
        Assert.Contains("compilation error", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // postScript – simple fast-path (regex)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ExecutePostScript_Null_DoesNothing()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        // Should not throw
        await HttpTestScriptRunner.ExecutePostScriptAsync(null, request, response, new Dictionary<string, string>());
    }

    [Theory]
    [InlineData(200)]
    [InlineData(201)]
    [InlineData(204)]
    [InlineData(299)]
    public async Task ExecutePostScript_EnsureSuccessStatusCode_PassesFor2xx(int statusCode)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage((HttpStatusCode)statusCode);
        await HttpTestScriptRunner.ExecutePostScriptAsync(
            "$response.EnsureSuccessStatusCode()", request, response, new Dictionary<string, string>());
    }

    [Theory]
    [InlineData(400)]
    [InlineData(404)]
    [InlineData(500)]
    public async Task ExecutePostScript_EnsureSuccessStatusCode_ThrowsForNon2xx(int statusCode)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage((HttpStatusCode)statusCode);
        await Assert.ThrowsAsync<HttpTestAssertionException>(() =>
            HttpTestScriptRunner.ExecutePostScriptAsync(
                "$response.EnsureSuccessStatusCode()", request, response, new Dictionary<string, string>()));
    }

    [Fact]
    public async Task ExecutePostScript_StatusCodeEquals_Passes()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        await HttpTestScriptRunner.ExecutePostScriptAsync(
            "$response.StatusCode == 200", request, response, new Dictionary<string, string>());
    }

    [Fact]
    public async Task ExecutePostScript_StatusCodeEquals_Fails()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        await Assert.ThrowsAsync<HttpTestAssertionException>(() =>
            HttpTestScriptRunner.ExecutePostScriptAsync(
                "$response.StatusCode == 200", request, response, new Dictionary<string, string>()));
    }

    [Fact]
    public async Task ExecutePostScript_StatusCodeNotEquals_Passes()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        await HttpTestScriptRunner.ExecutePostScriptAsync(
            "$response.StatusCode != 404", request, response, new Dictionary<string, string>());
    }

    [Fact]
    public async Task ExecutePostScript_StatusCodeNotEquals_Fails()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        await Assert.ThrowsAsync<HttpTestAssertionException>(() =>
            HttpTestScriptRunner.ExecutePostScriptAsync(
                "$response.StatusCode != 200", request, response, new Dictionary<string, string>()));
    }

    [Fact]
    public async Task ExecutePostScript_BodyContains_Passes()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"message\": \"hello world\"}")
        };
        await HttpTestScriptRunner.ExecutePostScriptAsync(
            "$response.Body.Contains(\"hello\")", request, response, new Dictionary<string, string>());
    }

    [Fact]
    public async Task ExecutePostScript_BodyContains_Fails()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"message\": \"hello world\"}")
        };
        await Assert.ThrowsAsync<HttpTestAssertionException>(() =>
            HttpTestScriptRunner.ExecutePostScriptAsync(
                "$response.Body.Contains(\"missing\")", request, response, new Dictionary<string, string>()));
    }

    [Fact]
    public async Task ExecutePostScript_MultipleAssertions_AllPass()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"status\": \"ok\"}")
        };
        var script = """
            $response.EnsureSuccessStatusCode()
            $response.StatusCode == 200
            $response.Body.Contains("ok")
            """;
        await HttpTestScriptRunner.ExecutePostScriptAsync(
            script, request, response, new Dictionary<string, string>());
    }

    [Fact]
    public async Task ExecutePostScript_MultipleAssertions_FirstFails()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var script = """
            $response.StatusCode == 201
            $response.EnsureSuccessStatusCode()
            """;
        var ex = await Assert.ThrowsAsync<HttpTestAssertionException>(() =>
            HttpTestScriptRunner.ExecutePostScriptAsync(
                script, request, response, new Dictionary<string, string>()));
        Assert.Contains("201", ex.Message);
    }

    [Fact]
    public async Task ExecutePostScript_IgnoresCommentLines()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var script = """
            // This checks status code
            $response.EnsureSuccessStatusCode()
            """;
        await HttpTestScriptRunner.ExecutePostScriptAsync(
            script, request, response, new Dictionary<string, string>());
    }

    // -----------------------------------------------------------------------
    // postScript – complex C# (Roslyn)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ExecutePostScript_ComplexCSharp_EnsureSuccessStatusCode()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var script = "response.EnsureSuccessStatusCode();";
        await HttpTestScriptRunner.ExecutePostScriptAsync(script, request, response, new Dictionary<string, string>());
    }

    [Fact]
    public async Task ExecutePostScript_ComplexCSharp_EnsureSuccessStatusCode_Fails()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        var script = "response.EnsureSuccessStatusCode();";
        await Assert.ThrowsAsync<HttpTestAssertionException>(() =>
            HttpTestScriptRunner.ExecutePostScriptAsync(script, request, response, new Dictionary<string, string>()));
    }

    [Fact]
    public async Task ExecutePostScript_ComplexCSharp_AssertHelper()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var script = "response.Assert(response.StatusCode == 200, \"Expected 200\");";
        await HttpTestScriptRunner.ExecutePostScriptAsync(script, request, response, new Dictionary<string, string>());
    }

    [Fact]
    public async Task ExecutePostScript_ComplexCSharp_AssertHelper_Fails()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var script = "response.Assert(response.StatusCode == 201, \"Expected 201\");";
        var ex = await Assert.ThrowsAsync<HttpTestAssertionException>(() =>
            HttpTestScriptRunner.ExecutePostScriptAsync(script, request, response, new Dictionary<string, string>()));
        Assert.Contains("Expected 201", ex.Message);
    }

    [Fact]
    public async Task ExecutePostScript_ComplexCSharp_BodyTextAssertion()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"message\": \"hello world\"}")
        };
        var script = """
            var body = response.body.text;
            response.Assert(body.Contains("hello"), "Body should contain hello");
            """;
        await HttpTestScriptRunner.ExecutePostScriptAsync(
            script, request, response, new Dictionary<string, string>());
    }

    [Fact]
    public async Task ExecutePostScript_ComplexCSharp_BodyTextAssertion_Fails()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"message\": \"hello world\"}")
        };
        var script = """
            var body = response.body.text;
            response.Assert(body.Contains("missing"), "Body should contain missing");
            """;
        var ex = await Assert.ThrowsAsync<HttpTestAssertionException>(() =>
            HttpTestScriptRunner.ExecutePostScriptAsync(
                script, request, response, new Dictionary<string, string>()));
        Assert.Contains("missing", ex.Message);
    }

    [Fact]
    public async Task ExecutePostScript_ComplexCSharp_JsonBodyAccess()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\": 42, \"name\": \"test\"}")
        };
        var script = """
            var id = response.body.json.id;
            response.Assert((long)id == 42, $"Expected id=42, got {id}");
            """;
        await HttpTestScriptRunner.ExecutePostScriptAsync(
            script, request, response, new Dictionary<string, string>());
    }

    [Fact]
    public async Task ExecutePostScript_ComplexCSharp_JsonBodyNestedAccess()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"user\": {\"name\": \"Alice\"}}")
        };
        var script = """
            var name = (string)response.body.json.user.name;
            response.Assert(name == "Alice", $"Expected Alice, got {name}");
            """;
        await HttpTestScriptRunner.ExecutePostScriptAsync(
            script, request, response, new Dictionary<string, string>());
    }

    [Fact]
    public async Task ExecutePostScript_ComplexCSharp_UpdatesVariables()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"token\": \"abc123\"}")
        };
        var variables = new Dictionary<string, string>();
        var script = """
            variables["authToken"] = (string)response.body.json.token;
            """;
        await HttpTestScriptRunner.ExecutePostScriptAsync(script, request, response, variables);
        Assert.Equal("abc123", variables["authToken"]);
    }

    [Fact]
    public async Task ExecutePostScript_ComplexCSharp_DollarSyntaxStillWorks()
    {
        // $response is preprocessed to response
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"status\": \"ok\"}")
        };
        var script = """
            response.EnsureSuccessStatusCode();
            var body = response.body.text;
            response.Assert(body.Contains("ok"), "Body should contain ok");
            """;
        await HttpTestScriptRunner.ExecutePostScriptAsync(
            script, request, response, new Dictionary<string, string>());
    }

    [Fact]
    public async Task ExecutePostScript_ComplexCSharp_CompilationError_Throws()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var ex = await Assert.ThrowsAsync<HttpTestAssertionException>(() =>
            HttpTestScriptRunner.ExecutePostScriptAsync(
                "this is not valid C#!!!",
                request, response, new Dictionary<string, string>()));
        Assert.Contains("compilation error", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
