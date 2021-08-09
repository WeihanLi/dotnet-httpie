using System.Collections.Generic;

namespace HTTPie.Models
{
    public class HttpContext
    {
        private readonly Dictionary<string, bool> _featureFlags = new();

        public HttpContext(HttpRequestModel request, HttpResponseModel? response = null)
        {
            Request = request;
            Response = response ?? new HttpResponseModel();
        }

        public HttpRequestModel Request { get; }
        public HttpResponseModel Response { get; set; }
        public Dictionary<string, object> Properties { get; } = new();

        public void UpdateFlag(string flagName, bool enabled)
        {
            _featureFlags[flagName] = enabled;
        }

        public bool GetFlag(string flagName, bool defaultValue = default)
        {
            return _featureFlags.TryGetValue(flagName, out var enabled)
                ? enabled
                : defaultValue;
        }
    }
}