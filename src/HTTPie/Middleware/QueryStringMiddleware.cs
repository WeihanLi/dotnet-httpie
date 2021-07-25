using System;
using System.Linq;
using System.Threading.Tasks;
using HTTPie.Abstractions;
using HTTPie.Models;
using WeihanLi.Extensions;

namespace HTTPie.Middleware
{
    public class QueryStringMiddleware: IRequestMiddleware
    {
        public Task Invoke(HttpRequestModel model, Func<Task> next)
        {
            foreach (var query in 
                model.RawInput.Where(x=>x.IndexOf("==", StringComparison.Ordinal)>0))
            {
                var arr = query.Split("==");
                model.Query[arr[0]] = arr[1];
            }
            return next();
        }
    }
}