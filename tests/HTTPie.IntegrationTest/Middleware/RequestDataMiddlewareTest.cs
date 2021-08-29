namespace HTTPie.IntegrationTest.Middleware
{
    public class RequestDataMiddlewareTest : HttpTestBase
    {

        [Theory]
        [InlineData("https://reservation.weihanli.xyz/health Authorization:'Bearer dede'")]
        public async void HeadersShouldNotBeTreatAsRequestData(string input)
        {
            await Handle(input);

            var request = GetRequest();
            request.Body.Should().BeNull();
        }
    }
}
