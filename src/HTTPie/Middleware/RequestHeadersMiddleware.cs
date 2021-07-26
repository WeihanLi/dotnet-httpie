using System;
using System.Linq;
using System.Threading.Tasks;
using HTTPie.Abstractions;
using HTTPie.Models;
using Microsoft.Extensions.Primitives;

namespace HTTPie.Middleware
{
    public class RequestHeadersMiddleware : IRequestMiddleware
    {
        public Task Invoke(HttpRequestModel model, Func<Task> next)
        {
            foreach (var input in model.RawInput
                .Where(x => x.IndexOf(':') > 0 
                            && !x.StartsWith("http://", StringComparison.Ordinal) 
                            && !x.StartsWith("https://", StringComparison.Ordinal) 
                            && x.IndexOf(":=", StringComparison.OrdinalIgnoreCase)<0))
            {
                var arr = input.Split(':');
                if (arr.Length == 2)
                {
                    if (model.Headers.TryGetValue(arr[0], out var values))
                        model.Headers[arr[0]] = new StringValues(new[] {arr[1]}.Union(values.ToArray()).ToArray());
                    else
                        model.Headers[arr[0]] = arr[1];
                }
            }
            return next();
        }
    }
}