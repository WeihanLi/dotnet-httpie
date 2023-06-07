// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using Xunit.Abstractions;

namespace HTTPie.UnitTest.Implement;

public class HttpParserTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public HttpParserTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("")]
    public void VariableReplacementTest_NoReplacement(string text)
    {
        var replacedText = HttpParser.EnsureVariableReplaced(text, new(), null);
        Assert.Equal(text, replacedText);
    }

    [Theory]
    [InlineData("GET {{host}}/api/account/login")]
    [InlineData("GET {{baseUrl}}/api/notice")]
    public void VariableReplacementTest_VariableReplacement(string text)
    {
        var replacedText = HttpParser.EnsureVariableReplaced(text,
            new() { { "host", "https://sparktodo.weihanli.xyz" } },
            new()
            {
                { "baseUrl", "https://reservation.weihanli.xyz" }, { "host", "https://reservation.weihanli.xyz" }
            });
        Assert.NotEqual(text, replacedText);
        Assert.DoesNotContain("{{", replacedText);
        Assert.DoesNotContain("}}", replacedText);
        _testOutputHelper.WriteLine(replacedText);
    }

    [Theory]
    [InlineData("GET {{$env reservation_host}}/api/notice")]
    [InlineData("GET {{$processEnv reservation_host}}/api/notice")]
    public void VariableReplacementTest_EnvReplacement(string text)
    {
        Environment.SetEnvironmentVariable("reservation_host", "https://reservation.weihanli.xyz");

        var replacedText = HttpParser.EnsureVariableReplaced(text,
            new(), new());
        Assert.NotEqual(text, replacedText);
        Assert.DoesNotContain("{{", replacedText);
        Assert.DoesNotContain("}}", replacedText);
        _testOutputHelper.WriteLine(replacedText);
    }
}
