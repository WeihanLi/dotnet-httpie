// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using Newtonsoft.Json;
using System.CommandLine.Invocation;
using WeihanLi.Common.Abstractions;

namespace HTTPie.Models;

public sealed class HttpContext(HttpRequestModel request, HttpResponseModel? response = null) : IProperties
{
    private readonly Dictionary<string, bool> _featureFlags = new();

    public HttpContext() : this(new HttpRequestModel())
    {
    }

    public HttpRequestModel Request { get; } = request;
    public HttpResponseModel Response { get; set; } = response ?? new HttpResponseModel();

    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    public CancellationToken RequestCancelled => InvocationContext.GetCancellationToken();

    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    public InvocationContext InvocationContext { get; set; } = null!;

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
