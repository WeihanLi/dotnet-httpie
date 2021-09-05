namespace HTTPie.UnitTest.Utilities
{
    public class HelpersTest
    {
        [Fact]
        public void RequestInit_RequestItemsTest()
        {
            var input = "GET -a uid:pwd reservation.weihanli.xyz/health";
            var httpContext = new HttpContext(new HttpRequestModel());
            Helpers.InitRequestModel(httpContext, input);
            httpContext.Request.Url.Should().Be("reservation.weihanli.xyz/health");
            httpContext.Request.RequestItems.Should().BeEmpty();
            httpContext.Request.Headers.Should().NotContainKey("uid");
        }
    }
}