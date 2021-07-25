using HTTPie.Models;

namespace HTTPie.Abstractions
{
    public interface IOutputFormatter : IPlugin
    {
        string GetOutput(HttpRequestModel requestModel, HttpResponseModel responseModel);
    }
}