using System;
using System.Threading.Tasks;
using HTTPie.Models;

namespace HTTPie.Abstractions
{
    public interface IRequestMiddleware : IPlugin
    {
        Task Invoke(HttpRequestModel requestModel, Func<Task> next);
    }
}