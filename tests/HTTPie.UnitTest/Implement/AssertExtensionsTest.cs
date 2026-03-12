// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using System.Net;

namespace HTTPie.UnitTest.Implement;

public class AssertExtensionsTest
{
    // -----------------------------------------------------------------------
    // AssertExtensions – extension on bool
    // -----------------------------------------------------------------------

    [Fact]
    public void ShouldBeTrue_Passes_ForTrue()
    {
        true.ShouldBeTrue();
    }

    [Fact]
    public void ShouldBeTrue_Throws_ForFalse()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => false.ShouldBeTrue());
        Assert.Contains("ShouldBeTrue", ex.Message);
    }

    [Fact]
    public void ShouldBeTrue_Throws_WithCustomMessage()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => false.ShouldBeTrue("custom msg"));
        Assert.Equal("custom msg", ex.Message);
    }

    [Fact]
    public void ShouldBeFalse_Passes_ForFalse()
    {
        false.ShouldBeFalse();
    }

    [Fact]
    public void ShouldBeFalse_Throws_ForTrue()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => true.ShouldBeFalse());
        Assert.Contains("ShouldBeFalse", ex.Message);
    }

    // -----------------------------------------------------------------------
    // AssertExtensions – extension on object?
    // -----------------------------------------------------------------------

    [Fact]
    public void ShouldBeNull_Passes_ForNull()
    {
        string? s = null;
        s.ShouldBeNull();
    }

    [Fact]
    public void ShouldBeNull_Throws_ForNonNull()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => ((object)"hello").ShouldBeNull());
        Assert.Contains("ShouldBeNull", ex.Message);
    }

    [Fact]
    public void ShouldNotBeNull_Passes_ForNonNull()
    {
        ((object)"hello").ShouldNotBeNull();
    }

    [Fact]
    public void ShouldNotBeNull_Throws_ForNull()
    {
        string? s = null;
        var ex = Assert.Throws<HttpTestAssertionException>(() => s.ShouldNotBeNull());
        Assert.Contains("ShouldNotBeNull", ex.Message);
    }

    // -----------------------------------------------------------------------
    // AssertExtensions – extension on int
    // -----------------------------------------------------------------------

    [Fact]
    public void ShouldBe_Int_Passes_WhenEqual()
    {
        200.ShouldBe(200);
    }

    [Fact]
    public void ShouldBe_Int_Throws_WhenNotEqual()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => 404.ShouldBe(200));
        Assert.Contains("200", ex.Message);
        Assert.Contains("404", ex.Message);
    }

    [Fact]
    public void ShouldNotBe_Int_Passes_WhenDifferent()
    {
        404.ShouldNotBe(200);
    }

    [Fact]
    public void ShouldNotBe_Int_Throws_WhenEqual()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => 200.ShouldNotBe(200));
        Assert.Contains("ShouldNotBe", ex.Message);
    }

    [Fact]
    public void ShouldBeGreaterThan_Int_Passes()
    {
        5.ShouldBeGreaterThan(3);
    }

    [Fact]
    public void ShouldBeGreaterThan_Int_Throws_WhenEqual()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => 3.ShouldBeGreaterThan(3));
        Assert.Contains("ShouldBeGreaterThan", ex.Message);
    }

    [Fact]
    public void ShouldBeGreaterThan_Int_Throws_WhenLess()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => 2.ShouldBeGreaterThan(3));
        Assert.Contains("ShouldBeGreaterThan", ex.Message);
    }

    [Fact]
    public void ShouldBeLessThan_Int_Passes()
    {
        2.ShouldBeLessThan(5);
    }

    [Fact]
    public void ShouldBeLessThan_Int_Throws_WhenEqual()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => 5.ShouldBeLessThan(5));
        Assert.Contains("ShouldBeLessThan", ex.Message);
    }

    [Fact]
    public void ShouldBeGreaterThanOrEqualTo_Int_Passes_WhenEqual()
    {
        5.ShouldBeGreaterThanOrEqualTo(5);
    }

    [Fact]
    public void ShouldBeGreaterThanOrEqualTo_Int_Passes_WhenGreater()
    {
        6.ShouldBeGreaterThanOrEqualTo(5);
    }

    [Fact]
    public void ShouldBeGreaterThanOrEqualTo_Int_Throws_WhenLess()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => 4.ShouldBeGreaterThanOrEqualTo(5));
        Assert.Contains("ShouldBeGreaterThanOrEqualTo", ex.Message);
    }

    // -----------------------------------------------------------------------
    // AssertExtensions – extension on long
    // -----------------------------------------------------------------------

    [Fact]
    public void ShouldBe_Long_Passes_WhenEqual()
    {
        42L.ShouldBe(42L);
    }

    [Fact]
    public void ShouldBe_Long_Throws_WhenNotEqual()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => 1L.ShouldBe(42L));
        Assert.Contains("42", ex.Message);
    }

    [Fact]
    public void ShouldBeGreaterThan_Long_Passes()
    {
        100L.ShouldBeGreaterThan(0L);
    }

    // -----------------------------------------------------------------------
    // AssertExtensions – extension on string?
    // -----------------------------------------------------------------------

    [Fact]
    public void ShouldBe_String_Passes_WhenEqual()
    {
        "hello".ShouldBe("hello");
    }

    [Fact]
    public void ShouldBe_String_Throws_WhenNotEqual()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => "hello".ShouldBe("world"));
        Assert.Contains("world", ex.Message);
        Assert.Contains("hello", ex.Message);
    }

    [Fact]
    public void ShouldContain_Passes_WhenPresent()
    {
        "hello world".ShouldContain("world");
    }

    [Fact]
    public void ShouldContain_Throws_WhenAbsent()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => "hello world".ShouldContain("missing"));
        Assert.Contains("missing", ex.Message);
    }

    [Fact]
    public void ShouldNotContain_Passes_WhenAbsent()
    {
        "hello world".ShouldNotContain("missing");
    }

    [Fact]
    public void ShouldNotContain_Throws_WhenPresent()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => "hello world".ShouldNotContain("world"));
        Assert.Contains("ShouldNotContain", ex.Message);
    }

    [Fact]
    public void ShouldStartWith_Passes()
    {
        "hello world".ShouldStartWith("hello");
    }

    [Fact]
    public void ShouldStartWith_Throws()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => "hello world".ShouldStartWith("world"));
        Assert.Contains("ShouldStartWith", ex.Message);
    }

    [Fact]
    public void ShouldEndWith_Passes()
    {
        "hello world".ShouldEndWith("world");
    }

    [Fact]
    public void ShouldEndWith_Throws()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => "hello world".ShouldEndWith("hello"));
        Assert.Contains("ShouldEndWith", ex.Message);
    }

    [Fact]
    public void ShouldNotBeNullOrEmpty_Passes_ForNonEmpty()
    {
        "hello".ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void ShouldNotBeNullOrEmpty_Throws_ForEmpty()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => "".ShouldNotBeNullOrEmpty());
        Assert.Contains("ShouldNotBeNullOrEmpty", ex.Message);
    }

    [Fact]
    public void ShouldNotBeNullOrEmpty_Throws_ForNull()
    {
        string? s = null;
        var ex = Assert.Throws<HttpTestAssertionException>(() => s.ShouldNotBeNullOrEmpty());
        Assert.Contains("ShouldNotBeNullOrEmpty", ex.Message);
    }

    [Fact]
    public void ShouldNotBeNullOrWhiteSpace_Passes_ForNonWhiteSpace()
    {
        "hello".ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ShouldNotBeNullOrWhiteSpace_Throws_ForWhiteSpace()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => "   ".ShouldNotBeNullOrWhiteSpace());
        Assert.Contains("ShouldNotBeNullOrWhiteSpace", ex.Message);
    }

    // -----------------------------------------------------------------------
    // HttpAssert – static methods
    // -----------------------------------------------------------------------

    [Fact]
    public void HttpAssert_True_Passes()
    {
        HttpAssert.True(true);
    }

    [Fact]
    public void HttpAssert_True_Throws_ForFalse()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => HttpAssert.True(false, "must be true"));
        Assert.Equal("must be true", ex.Message);
    }

    [Fact]
    public void HttpAssert_False_Passes()
    {
        HttpAssert.False(false);
    }

    [Fact]
    public void HttpAssert_False_Throws_ForTrue()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => HttpAssert.False(true));
        Assert.Contains("HttpAssert.False", ex.Message);
    }

    [Fact]
    public void HttpAssert_Null_Passes()
    {
        HttpAssert.Null(null);
    }

    [Fact]
    public void HttpAssert_Null_Throws_ForNonNull()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => HttpAssert.Null("value"));
        Assert.Contains("HttpAssert.Null", ex.Message);
    }

    [Fact]
    public void HttpAssert_NotNull_Passes()
    {
        HttpAssert.NotNull("value");
    }

    [Fact]
    public void HttpAssert_NotNull_Throws_ForNull()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => HttpAssert.NotNull(null));
        Assert.Contains("HttpAssert.NotNull", ex.Message);
    }

    [Fact]
    public void HttpAssert_Equal_Passes()
    {
        HttpAssert.Equal(200, 200);
        HttpAssert.Equal("hello", "hello");
    }

    [Fact]
    public void HttpAssert_Equal_Throws_WhenNotEqual()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => HttpAssert.Equal(200, 404));
        Assert.Contains("200", ex.Message);
        Assert.Contains("404", ex.Message);
    }

    [Fact]
    public void HttpAssert_NotEqual_Passes()
    {
        HttpAssert.NotEqual(200, 404);
    }

    [Fact]
    public void HttpAssert_NotEqual_Throws_WhenEqual()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => HttpAssert.NotEqual(200, 200));
        Assert.Contains("HttpAssert.NotEqual", ex.Message);
    }

    [Fact]
    public void HttpAssert_Contains_Passes()
    {
        HttpAssert.Contains("world", "hello world");
    }

    [Fact]
    public void HttpAssert_Contains_Throws_WhenAbsent()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => HttpAssert.Contains("missing", "hello world"));
        Assert.Contains("missing", ex.Message);
    }

    [Fact]
    public void HttpAssert_DoesNotContain_Passes()
    {
        HttpAssert.DoesNotContain("missing", "hello world");
    }

    [Fact]
    public void HttpAssert_DoesNotContain_Throws_WhenPresent()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => HttpAssert.DoesNotContain("world", "hello world"));
        Assert.Contains("HttpAssert.DoesNotContain", ex.Message);
    }

    [Fact]
    public void HttpAssert_NotNullOrEmpty_Passes()
    {
        HttpAssert.NotNullOrEmpty("value");
    }

    [Fact]
    public void HttpAssert_NotNullOrEmpty_Throws_ForNull()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => HttpAssert.NotNullOrEmpty(null));
        Assert.Contains("HttpAssert.NotNullOrEmpty", ex.Message);
    }

    [Fact]
    public void HttpAssert_GreaterThan_Passes()
    {
        HttpAssert.GreaterThan(0, 5);
    }

    [Fact]
    public void HttpAssert_GreaterThan_Throws_WhenEqual()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => HttpAssert.GreaterThan(5, 5));
        Assert.Contains("HttpAssert.GreaterThan", ex.Message);
    }

    [Fact]
    public void HttpAssert_LessThan_Passes()
    {
        HttpAssert.LessThan(10, 3);
    }

    [Fact]
    public void HttpAssert_LessThan_Throws_WhenEqual()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => HttpAssert.LessThan(5, 5));
        Assert.Contains("HttpAssert.LessThan", ex.Message);
    }

    [Fact]
    public void HttpAssert_Fail_AlwaysThrows()
    {
        var ex = Assert.Throws<HttpTestAssertionException>(() => HttpAssert.Fail("deliberate failure"));
        Assert.Equal("deliberate failure", ex.Message);
    }

    // -----------------------------------------------------------------------
    // Roslyn script integration — AssertExtensions used inside scripts
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Script_ShouldBe_Passes()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        await HttpTestScriptRunner.ExecutePostScriptAsync(
            "response.StatusCode.ShouldBe(200);",
            request, response, new Dictionary<string, string>());
    }

    [Fact]
    public async Task Script_ShouldBe_Fails()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        var ex = await Assert.ThrowsAsync<HttpTestAssertionException>(() =>
            HttpTestScriptRunner.ExecutePostScriptAsync(
                "response.StatusCode.ShouldBe(200);",
                request, response, new Dictionary<string, string>()));
        Assert.Contains("200", ex.Message);
    }

    [Fact]
    public async Task Script_ShouldContain_OnBodyText()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"message\": \"hello world\"}")
        };
        await HttpTestScriptRunner.ExecutePostScriptAsync(
            "response.body.text.ShouldContain(\"hello\");",
            request, response, new Dictionary<string, string>());
    }

    [Fact]
    public async Task Script_HttpAssert_Equal_Passes()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        await HttpTestScriptRunner.ExecutePostScriptAsync(
            "HttpAssert.Equal(200, response.StatusCode);",
            request, response, new Dictionary<string, string>());
    }

    [Fact]
    public async Task Script_HttpAssert_Contains_OnBodyText()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"token\": \"abc123\"}")
        };
        await HttpTestScriptRunner.ExecutePostScriptAsync(
            "HttpAssert.Contains(\"token\", response.body.text);",
            request, response, new Dictionary<string, string>());
    }

    [Fact]
    public async Task Script_HttpAssert_Fail_StopsExecution()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        // Fail is called first; the second line should never execute
        var ex = await Assert.ThrowsAsync<HttpTestAssertionException>(() =>
            HttpTestScriptRunner.ExecutePostScriptAsync("""
                HttpAssert.Fail("deliberate");
                HttpAssert.True(false, "should not reach here");
                """,
                request, response, new Dictionary<string, string>()));
        Assert.Equal("deliberate", ex.Message);
    }

    [Fact]
    public async Task Script_JsonBodyId_WithShouldBeGreaterThan()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\": 42}")
        };
        await HttpTestScriptRunner.ExecutePostScriptAsync(
            "((long)response.body.json.id).ShouldBeGreaterThan(0L);",
            request, response, new Dictionary<string, string>());
    }
}
