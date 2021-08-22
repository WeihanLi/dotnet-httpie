using HTTPie.Models;
using System;
using System.Threading.Tasks;

namespace HTTPie.Abstractions
{
    public interface IResponseMiddleware : IPlugin
    {
        Task Invoke(HttpContext context, Func<Task> next);
    }
}