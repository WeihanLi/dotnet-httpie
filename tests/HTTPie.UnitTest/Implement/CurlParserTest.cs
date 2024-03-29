﻿// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

namespace HTTPie.UnitTest.Implement;

public sealed class CurlParserTest
{
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("Hello World")]
    public async Task InvalidParseTest(string? script)
    {
        var parser = new CurlParser();
        await Assert.ThrowsAnyAsync<ArgumentException>(() => parser.ParseScriptAsync(script ?? ""));
    }


    [Theory]
    [MemberData(nameof(CurlScriptTestData))]
    public async Task ParseTest(string script)
    {
        var parser = new CurlParser();
        var request = await parser.ParseScriptAsync(script);
        Assert.NotNull(request);
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
