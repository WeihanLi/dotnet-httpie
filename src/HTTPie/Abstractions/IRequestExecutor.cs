using HTTPie.Models;
using System.Threading.Tasks;

namespace HTTPie.Abstractions
{
    public interface IRequestExecutor
    {
        ValueTask ExecuteAsync(HttpContext httpContext);
    }
}