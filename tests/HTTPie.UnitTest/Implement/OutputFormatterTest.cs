using HTTPie.Implement;
using HTTPie.Models;
using HTTPie.Utilities;
using Xunit;

namespace HTTPie.UnitTest.Implement
{
    public class OutputFormatterTest
    {
        private readonly OutputFormatter _outputFormatter;

        public OutputFormatterTest(IServiceProvider services)
        {
            _outputFormatter = new OutputFormatter();
            Services = services;
        }

        public IServiceProvider Services { get; }

        [Theory]
        [InlineData(":5000/api/values -q")]
        [InlineData(":5000/api/values --quiet")]
        public void QuietTest(string input)
        {
            var httpContext = new HttpContext(new HttpRequestModel());
            Helpers.InitRequestModel(httpContext.Request, input);
            var output = _outputFormatter.GetOutput(httpContext);
            Assert.Empty(output);
        }
    }
}