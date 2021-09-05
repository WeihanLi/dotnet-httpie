namespace HTTPie.UnitTest.Utilities
{
    public class HelpersTest
    {
        [Fact]
        public void RequestInit_BasicAuthTest()
        {
            var input = "GET -a uid:pwd reservation.weihanli.xyz/health";
            var httpContext = new HttpContext(new HttpRequestModel());
            Helpers.InitRequestModel(httpContext, input);
            httpContext.Request.Url.Should().Be("reservation.weihanli.xyz/health");
            httpContext.Request.RequestItems.Should().BeEmpty();
        }

        [Fact]
        public void RequestInit_RequestItemsTest_WithoutMethod()
        {
            var input = "reservation.weihanli.xyz/health hello=world Api-Version:1.2 name=test age:=10";
            var httpContext = new HttpContext();
            Helpers.InitRequestModel(httpContext, input);
            httpContext.Request.Url.Should().Be("reservation.weihanli.xyz/health");
            httpContext.Request.RequestItems.Should().NotBeEmpty();
            httpContext.Request.RequestItems.Length.Should().Be(4);
        }
    }
}