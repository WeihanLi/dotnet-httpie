namespace HTTPie.Abstractions;

public interface IHttpHandlerMiddleware : IPlugin
{
    Task Invoke(HttpClientHandler httpClientHandler, Func<Task> next);
}
