using HTTPie.Abstractions;
using HTTPie.Models;
using System;
using System.Threading.Tasks;

namespace HTTPie.Middleware
{
    public class DefaultResponseMiddleware : IResponseMiddleware
    {
        public Task Invoke(HttpContext context, Func<Task> next)
        {
            return next();
        }
    }
}