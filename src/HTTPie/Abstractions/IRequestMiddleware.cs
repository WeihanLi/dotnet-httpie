using HTTPie.Models;
using System;
using System.Threading.Tasks;

namespace HTTPie.Abstractions
{
    public interface IRequestMiddleware : IPlugin
    {
        Task Invoke(HttpRequestModel requestModel, Func<Task> next);
    }
}