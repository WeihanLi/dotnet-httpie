// Copyright (c) Weihan Li.All rights reserved.
// Licensed under the MIT license.

using System.Text.Json.Serialization;

namespace HTTPie.Models;

/// <summary>
/// Represents a test collection that groups related HTTP requests for API testing.
/// </summary>
public sealed class HttpTestCollection
{
    /// <summary>The name of the test collection.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Collection-level variables available to all groups and requests.</summary>
    public Dictionary<string, string> Variables { get; set; } = new();

    /// <summary>Script executed before each request in the collection (e.g. $request.headers.add("key","value")).</summary>
    public string? PreScript { get; set; }

    /// <summary>Script executed after each request in the collection (e.g. $response.EnsureSuccessStatusCode()).</summary>
    public string? PostScript { get; set; }

    /// <summary>The groups of requests in this collection.</summary>
    public List<HttpTestGroup> Groups { get; set; } = [];
}

/// <summary>
/// Represents a group of related requests within a test collection.
/// </summary>
public sealed class HttpTestGroup
{
    /// <summary>The name of the group.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Group-level variables that override collection variables for all requests in this group.</summary>
    public Dictionary<string, string> Variables { get; set; } = new();

    /// <summary>Script executed before each request in the group.</summary>
    public string? PreScript { get; set; }

    /// <summary>Script executed after each request in the group.</summary>
    public string? PostScript { get; set; }

    /// <summary>The requests in this group.</summary>
    public List<HttpTestRequest> Requests { get; set; } = [];
}

/// <summary>
/// Represents an individual HTTP test request within a group.
/// </summary>
public sealed class HttpTestRequest
{
    /// <summary>The name of the request.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The HTTP method (GET, POST, PUT, PATCH, DELETE, etc.).</summary>
    public string Method { get; set; } = "GET";

    /// <summary>The request URL. Supports variable substitution e.g. {{baseUrl}}/path.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Request headers. Values support variable substitution.</summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>Request body content. Supports variable substitution.</summary>
    public string? Body { get; set; }

    /// <summary>Request-level variables that override group and collection variables.</summary>
    public Dictionary<string, string> Variables { get; set; } = new();

    /// <summary>Script executed before this request (overrides group and collection preScript).</summary>
    public string? PreScript { get; set; }

    /// <summary>Script executed after this request to validate the response (overrides group and collection postScript).</summary>
    public string? PostScript { get; set; }
}

/// <summary>
/// Represents a named environment with variables for use in test collections.
/// </summary>
public sealed class HttpTestEnvironment
{
    /// <summary>The name of the environment (e.g. "dev", "staging", "prod").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Environment-specific variables.</summary>
    public Dictionary<string, string> Variables { get; set; } = new();
}

/// <summary>
/// Result of executing a single test request.
/// </summary>
public sealed class HttpTestResult
{
    /// <summary>The request name.</summary>
    public string RequestName { get; set; } = string.Empty;

    /// <summary>Whether the test passed.</summary>
    [JsonIgnore]
    public bool Passed { get; set; }

    /// <summary>The HTTP status code returned.</summary>
    public int? StatusCode { get; set; }

    /// <summary>Error or assertion failure message, if any.</summary>
    public string? Error { get; set; }

    /// <summary>Time elapsed for the request.</summary>
    public TimeSpan Elapsed { get; set; }
}
