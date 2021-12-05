using HTTPie.Models;

namespace HTTPie.Abstractions
{
    public interface IRequestMapper
    {
        Task<HttpRequestMessage> ToRequestMessage(HttpContext httpContext);
    }
}