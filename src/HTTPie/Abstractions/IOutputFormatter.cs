using HTTPie.Models;

namespace HTTPie.Abstractions
{
    public interface IOutputFormatter : IPlugin
    {
        string GetOutput(HttpContext httpContext);
    }
}
