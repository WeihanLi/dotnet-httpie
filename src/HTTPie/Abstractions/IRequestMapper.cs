using System.Net.Http;
using System.Threading.Tasks;
using HTTPie.Models;

namespace HTTPie.Abstractions
{
    public interface IRequestMapper
    {
        Task<HttpRequestMessage> ToRequestMessage(HttpContext httpContext);
    }
}