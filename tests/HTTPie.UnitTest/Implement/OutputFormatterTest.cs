using HTTPie.Implement;
using HTTPie.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HTTPie.UnitTest.Implement
{
    public class OutputFormatterTest
    {
        private readonly Mock<ILogger> _loggerMock = new();
        private readonly OutputFormatter _outputFormatter;

        public OutputFormatterTest()
        {
            _outputFormatter = new OutputFormatter(_loggerMock.Object);
        }

        [Theory]
        [InlineData("-q")]
        [InlineData("--quiet")]
        public void QuietTest(string printOption)
        {
            var output = _outputFormatter.GetOutput(new HttpContext(new HttpRequestModel
            {
                RawInput = new[] { printOption }
            }));
            Assert.Empty(output);
        }
    }
}