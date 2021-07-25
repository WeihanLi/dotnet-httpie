using System.Threading.Tasks;
using HTTPie.Models;

namespace HTTPie.Abstractions
{
    public interface IRequestExecutor
    {
        Task<HttpResponseModel> ExecuteAsync(HttpRequestModel request);
    }
}