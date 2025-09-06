// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

namespace HTTPie.UnitTest.Implement;

public sealed class CurlParserTest
{
    [Theory]
    [InlineData("")]
    [InlineData("Hello World")]
    public async Task InvalidParseTest(string script)
    {
        var parser = new CurlParser();

        await Assert.ThrowsAnyAsync<ArgumentException>(async () =>
        {
            await foreach (var request in parser.ParseScriptAsync(script, TestContext.Current.CancellationToken))
            {
                Assert.NotNull(request);
            }
        });
    }

    [Theory]
    [MemberData(nameof(CurlScriptTestData))]
    public async Task ParseTest(string script)
    {
        var parser = new CurlParser();

        var requests = await parser.ParseScriptAsync(script, TestContext.Current.CancellationToken).ToArrayAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(requests);
        Assert.Single(requests);
        Assert.NotNull(requests[0].RequestMessage);
        Assert.NotEmpty(requests[0].RequestMessage.Method.Method);
    }

    public static IEnumerable<object[]> CurlScriptTestData()
    {
        // common get request
        yield return new object[]
        {
            """
            curl -X 'GET' \
            'https://reservation.weihanli.xyz/api/Reservations?pageNumber=1&pageSize=10' \
            -H 'accept: */*'
            """
        };
        // post with body
        yield return new object[]
        {
            """
            curl -X 'POST' \
            'https://reservation.weihanli.xyz/api/Reservations' \
            -H 'accept: */*' \
            -H 'Content-Type: application/json' \
            -d '{
            "ReservationForDate": "2023-07-30T01:27:12.457Z",
            "ReservationPlaceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
            }'
            """
        };
    }
}
