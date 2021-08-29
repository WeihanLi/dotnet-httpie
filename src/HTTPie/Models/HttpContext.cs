using System.Diagnostics.CodeAnalysis;

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

    public static class HttpContextExtensions
    {
        public static bool TryGetProperty<T>(this HttpContext httpContext, string propertyName, [NotNullWhen(true)] out T? propertyValue) where T : notnull
        {
            if (httpContext.Properties.TryGetValue(propertyName, out var value))
            {
                propertyValue = (T)value;
                return true;
            }
            propertyValue = default;
            return default;
        }

        public static void SetProperty<T>(this HttpContext httpContext, string propertyName, T propertyValue) where T : notnull
        {
            httpContext.Properties[propertyName] = propertyValue;
        }
    }
}