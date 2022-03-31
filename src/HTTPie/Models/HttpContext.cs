// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using WeihanLi.Common.Abstractions;

namespace HTTPie.Models;

public class HttpContext : IProperties
{
    private readonly Dictionary<string, bool> _featureFlags = new();

    public HttpContext() : this(new HttpRequestModel())
    {
    }

    public HttpContext(HttpRequestModel request, HttpResponseModel? response = null)
    {
        Request = request;
        Response = response ?? new HttpResponseModel();
    }

    public HttpRequestModel Request { get; }
    public HttpResponseModel Response { get; set; }
    public IDictionary<string, object?> Properties { get; } = new Dictionary<string, object?>();

    public void UpdateFlag(string flagName, bool value)
    {
        _featureFlags[flagName] = value;
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
    public static bool TryGetProperty<T>(this HttpContext httpContext, string propertyName, out T? propertyValue)
    {
        if (httpContext.Properties.TryGetValue(propertyName, out var value))
        {
            propertyValue = (T?)value;
            return true;
        }
        propertyValue = default;
        return false;
    }
}
