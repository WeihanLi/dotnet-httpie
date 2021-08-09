using System;
using System.Threading.Tasks;
using Xunit;

namespace HTTPie.IntegrationTest
{
    public class OfflineModeTest : HttpTestBase
    {
        public OfflineModeTest(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        [Theory]
        [InlineData("https://reservation.weihanli.xyz/health --quiet")]
        public async Task OfflineTestAsync(string input)
        {
            var output = await GetOutput(input);
            Assert.Empty(output);
        }
    }
}