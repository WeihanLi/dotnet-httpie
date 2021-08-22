using HTTPie.Models;
using System.Net.Http;
using System.Threading.Tasks;

namespace HTTPie.Abstractions
{
    public interface IRequestMapper
    {
        Task<HttpRequestMessage> ToRequestMessage(HttpContext httpContext);
    }
}