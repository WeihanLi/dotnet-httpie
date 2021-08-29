using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace HTTPie.Models
{
    public record HttpRequestModel
    {
        [Required] public string Schema { get; set; } = "http";

        public HttpMethod Method { get; set; } = HttpMethod.Get;

        [Required] public string Url { get; set; } = string.Empty;

        public Version HttpVersion { get; set; } = new(1, 1);
        public IDictionary<string, StringValues> Headers { get; } = new Dictionary<string, StringValues>();
        public IDictionary<string, StringValues> Query { get; set; } = new Dictionary<string, StringValues>();
        public string? Body { get; set; }

        public string[] Options { get; set; } = Array.Empty<string>();
        public string[] RequestItems { get; set; } = Array.Empty<string>();

        [JsonIgnore]
        public ParseResult ParseResult { get; set; } = null!;
    }
}