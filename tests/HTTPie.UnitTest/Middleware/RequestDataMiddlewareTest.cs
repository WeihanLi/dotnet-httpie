using HTTPie.Middleware;
using HTTPie.Models;
using HTTPie.Utilities;
using Xunit;

namespace HTTPie.UnitTest.Middleware
{
    public class RequestDataMiddlewareTest
    {
        public IServiceProvider Services { get; }

        public RequestDataMiddlewareTest(IServiceProvider services)
        {
            Services = services;
        }

        [Theory]
        [InlineData("http://localhost:5000/api/values?name=test&hello=world")]
        [InlineData("https://reservation.weihanli.xyz/health?name=test&hello=world")]
        public void UrlQueryStringShouldNotBeTreatAsRequestData_Issue1(string url)
        {
            var httpContext = new HttpContext(new HttpRequestModel()
            {
                Url = url
            });
            Helpers.InitRequestModel(httpContext.Request, url);
            var middleware = new RequestDataMiddleware(httpContext);
            middleware.Invoke(httpContext.Request, () => Task.CompletedTask);
            Assert.Null(httpContext.Request.Body);
        }

        [Theory]
        [InlineData("GET :5000/api/values name==test hello==world")]
        [InlineData("https://reservation.weihanli.xyz/health name==test hello==world")]
        public void QueryShouldNotBeTreatAsRequestData(string input)
        {
            var httpContext = new HttpContext(new HttpRequestModel());
            Helpers.InitRequestModel(httpContext.Request, input);
            var middleware = new RequestDataMiddleware(httpContext);
            middleware.Invoke(httpContext.Request, () => Task.CompletedTask);
            Assert.Null(httpContext.Request.Body);
        }

        [Theory]
        //[InlineData("GET :5000/api/values Api-Version:E=2")]
        //[InlineData("https://reservation.weihanli.xyz/health Authorization: 'Bearer dede'")]
        [InlineData("https://reservation.weihanli.xyz/health Authorization:'Bearer dede'")]
        public void HeadersShouldNotBeTreatAsRequestData(string input)
        {
            var httpContext = new HttpContext(new HttpRequestModel());
            Helpers.InitRequestModel(httpContext.Request, input);
            var middleware = new RequestDataMiddleware(httpContext);
            middleware.Invoke(httpContext.Request, () => Task.CompletedTask);
            Assert.Null(httpContext.Request.Body);
        }

        [Theory]
        [InlineData("GET :5000/api/values --print=Hh")]
        [InlineData("GET :5000/api/values --p=HB")]
        [InlineData("reservation.weihanli.xyz/health --verbose --schema=https")]
        public void FlagsShouldNotBeTreatAsRequestData(string input)
        {
            var httpContext = new HttpContext(new HttpRequestModel());
            Helpers.InitRequestModel(httpContext.Request, input);
            var middleware = new RequestDataMiddleware(httpContext);
            middleware.Invoke(httpContext.Request, () => Task.CompletedTask);
            Assert.Null(httpContext.Request.Body);
        }
    }
}