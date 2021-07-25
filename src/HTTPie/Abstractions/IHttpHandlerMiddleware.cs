using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace HTTPie.Abstractions
{
    public interface IHttpHandlerMiddleware : IPlugin
    {
        Task Invoke(HttpClientHandler httpClientHandler, Func<Task> next);
    }
}