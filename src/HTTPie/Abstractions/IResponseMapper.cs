using HTTPie.Models;
using System.Net.Http;
using System.Threading.Tasks;

namespace HTTPie.Abstractions
{
    public interface IResponseMapper
    {
        Task<HttpResponseModel> ToResponseModel(HttpResponseMessage responseMessage);
    }
}