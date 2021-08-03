using HTTPie.Implement;
using Microsoft.Extensions.Logging;
using Moq;

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
    }
}