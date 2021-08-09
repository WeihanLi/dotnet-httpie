using System;
using System.Linq;
using System.Threading.Tasks;
using HTTPie.Abstractions;
using HTTPie.Models;
using Microsoft.Extensions.Primitives;

namespace HTTPie.Middleware
{
    public class QueryStringMiddleware : IRequestMiddleware
    {
        public Task Invoke(HttpRequestModel requestModel, Func<Task> next)
        {
            foreach (var query in
                requestModel.RawInput.Where(x => x.IndexOf("==", StringComparison.Ordinal) > 0))
            {
                var arr = query.Split("==");
                if (arr.Length == 2)
                {
                    if (requestModel.Query.TryGetValue(arr[0], out var values))
                        requestModel.Query[arr[0]] =
                            new StringValues(new[] { arr[1] }.Union(values.ToArray()).ToArray());
                    else
                        requestModel.Query[arr[0]] = arr[1];
                }
            }

            return next();
        }
    }
}