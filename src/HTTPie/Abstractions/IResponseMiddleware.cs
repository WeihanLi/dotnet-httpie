using HTTPie.Models;

namespace HTTPie.Abstractions;

public interface IResponseMiddleware : IPlugin
{
    Task Invoke(HttpContext context, Func<Task> next);
}
