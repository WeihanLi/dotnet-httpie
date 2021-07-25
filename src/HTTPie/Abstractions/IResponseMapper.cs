using System.Net.Http;
using System.Threading.Tasks;
using HTTPie.Models;

namespace HTTPie.Abstractions
{
    public interface IResponseMapper
    {
        Task<HttpResponseModel> ToResponseModel(HttpResponseMessage responseMessage);
    }
}