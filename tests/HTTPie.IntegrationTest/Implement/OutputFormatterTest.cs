using Xunit;

namespace HTTPie.IntegrationTest.Implement
{
    public class OutputFormatterTest : HttpTestBase
    {
        [Theory]
        [InlineData("https://reservation.weihanli.xyz/health --offline")]
        [InlineData("get https://reservation.weihanli.xyz/health --offline")]
        public async Task OfflineTest(string input)
        {
            var output = await GetOutput(input);
            Assert.NotEmpty(output);

            var response = GetResponse();
            Assert.Empty(response.Headers);
        }

        [Theory]
        [InlineData("https://reservation.weihanli.xyz/health --quiet")]
        [InlineData("https://reservation.weihanli.xyz/health -q")]
        public async Task QuietTest(string input)
        {
            var output = await GetOutput(input);
            Assert.Empty(output);

            var response = GetResponse();
            Assert.NotEmpty(response.Headers);
        }

        [Theory]
        [InlineData("GET https://reservation.weihanli.xyz/health --print=Hh")]
        public async Task PrintTest(string input)
        {
            var output = await GetOutput(input);
            Assert.NotEmpty(output);

            var response = GetResponse();
            Assert.NotEmpty(response.Headers);
            Assert.NotEmpty(response.Body);

            Assert.DoesNotContain(response.Body, output);
        }

        [Theory]
        [InlineData("https://httpbin.org/get -p=b")]
        [InlineData("GET https://httpbin.org/get --print=b")]
        public async Task PrintResponseBodyTest(string input)
        {
            var output = await GetOutput(input);
            Assert.NotEmpty(output);

            var response = GetResponse();
            Assert.NotEmpty(response.Headers);
            Assert.NotNull(response.Body);
            Assert.NotEmpty(response.Body);

            Assert.Equal(response.Body!.Trim(), output.Trim());
        }
    }
}