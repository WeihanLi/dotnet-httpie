using HTTPie.Models;

namespace HTTPie.Abstractions;

public interface IRequestMiddleware : IPlugin
{
    Task Invoke(HttpRequestModel requestModel, Func<Task> next);
}
