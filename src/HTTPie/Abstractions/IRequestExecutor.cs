using System.Threading.Tasks;
using HTTPie.Models;

namespace HTTPie.Abstractions
{
    public interface IRequestExecutor
    {
        ValueTask ExecuteAsync(HttpContext httpContext);
    }
}