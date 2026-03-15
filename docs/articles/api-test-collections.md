# HTTP API Test Collections

dotnet-httpie supports running structured HTTP API test collections via the `test` subcommand. Collections are defined as JSON files and support hierarchical organization with variable inheritance, pre/post scripts, and rich assertion helpers.

## Overview

The `test` command allows you to:

- Organize API tests into **collections → groups → requests**
- Define **variables** at the collection, group, or request level (inner levels override outer levels)
- Run **preScripts** before requests (e.g. inject auth headers dynamically)
- Run **postScripts** after requests (e.g. assert status codes and response bodies)
- Use **environments** to switch between development, staging, and production settings
- Use **Roslyn C# scripting** for complex assertions and transformations

## Quick Start

### 1. Create a test collection file

```json
{
  "name": "MyApi",
  "variables": {
    "baseUrl": "https://api.example.com"
  },
  "postScript": "$response.EnsureSuccessStatusCode()",
  "groups": [
    {
      "name": "Users",
      "requests": [
        {
          "name": "GetUsers",
          "method": "GET",
          "url": "{{baseUrl}}/users"
        },
        {
          "name": "CreateUser",
          "method": "POST",
          "url": "{{baseUrl}}/users",
          "headers": {
            "Content-Type": "application/json"
          },
          "body": "{\"name\": \"John Doe\", \"email\": \"john@example.com\"}",
          "postScript": "response.StatusCode.ShouldBe(201);"
        }
      ]
    }
  ]
}
```

### 2. Run the collection

```bash
dotnet-http test my-api.httptest.json
```

### 3. Run with an environment

```bash
dotnet-http test my-api.httptest.json --env staging
```

## Collection File Format

Test collections are stored as JSON files (conventionally named `*.httptest.json`).

### Top-level Structure

| Field | Type | Description |
|-------|------|-------------|
| `name` | string | Display name for the collection |
| `variables` | object | Key-value pairs available to all groups and requests |
| `preScript` | string | Script run before **every** request in the collection |
| `postScript` | string | Script run after **every** request in the collection |
| `groups` | array | List of request groups |

### Group Structure

| Field | Type | Description |
|-------|------|-------------|
| `name` | string | Display name for the group |
| `variables` | object | Variables that override collection variables for all requests in this group |
| `preScript` | string | Script run before every request in this group (overrides collection preScript) |
| `postScript` | string | Script run after every request in this group (overrides collection postScript) |
| `requests` | array | List of HTTP requests |

### Request Structure

| Field | Type | Description |
|-------|------|-------------|
| `name` | string | Display name for the request |
| `method` | string | HTTP method (`GET`, `POST`, `PUT`, `PATCH`, `DELETE`, etc.) |
| `url` | string | Request URL; supports `{{variableName}}` substitution |
| `headers` | object | Request headers; values support `{{variableName}}` substitution |
| `body` | string | Request body; supports `{{variableName}}` substitution |
| `variables` | object | Variables that override group/collection variables for this request |
| `preScript` | string | Script run before this request (overrides group/collection preScript) |
| `postScript` | string | Script run after this request (overrides group/collection postScript) |

## Variables

Variables use the `{{variableName}}` syntax and are substituted in URLs, header values, and request bodies.

### Variable Inheritance

Variables are merged from lowest to highest priority. Higher-priority values override lower-priority ones:

1. Collection `variables` — lowest priority (provide collection-level defaults)
2. Environment file variables — override collection defaults (applied when `--env` is specified; when `--env` is omitted, the environment named `default` is loaded if present)
3. Group `variables` — override collection and environment variables
4. Request `variables` — highest priority, override all outer scopes

```json
{
  "name": "Orders API",
  "variables": {
    "baseUrl": "https://api.example.com",
    "apiVersion": "v2"
  },
  "groups": [
    {
      "name": "Legacy",
      "variables": {
        "apiVersion": "v1"
      },
      "requests": [
        {
          "name": "GetOrders",
          "method": "GET",
          "url": "{{baseUrl}}/{{apiVersion}}/orders"
        }
      ]
    }
  ]
}
```

### Updating Variables in Scripts

Scripts can update variables and pass values between requests:

```json
{
  "name": "CreateUser",
  "method": "POST",
  "url": "{{baseUrl}}/users",
  "headers": { "Content-Type": "application/json" },
  "body": "{\"name\": \"Jane\"}",
  "postScript": "variables[\"userId\"] = (string)response.body.json.id;"
},
{
  "name": "GetUser",
  "method": "GET",
  "url": "{{baseUrl}}/users/{{userId}}"
}
```

## Environments

Environments allow you to switch variable sets without changing collection files.

### Environment File Format

Create a file (conventionally named `*.httptest.env.json`):

```json
[
  {
    "name": "dev",
    "variables": {
      "baseUrl": "http://localhost:5000",
      "apiKey": "dev-key-123"
    }
  },
  {
    "name": "staging",
    "variables": {
      "baseUrl": "https://staging.example.com",
      "apiKey": "staging-key-abc"
    }
  },
  {
    "name": "prod",
    "variables": {
      "baseUrl": "https://api.example.com",
      "apiKey": "prod-key-xyz"
    }
  }
]
```

### Running with an Environment

```bash
# Use a named environment from the default env file
dotnet-http test my-api.httptest.json --env staging

# Use an explicit environment file
dotnet-http test my-api.httptest.json --env-file envs/staging.httptest.env.json

# Combine both
dotnet-http test my-api.httptest.json --env staging --env-file envs/staging.httptest.env.json
```

## Scripts

Scripts are C# expressions or statements that run before/after each request.

### preScript

Executed **before** the HTTP request is sent. Use it to:
- Inject authentication headers
- Modify request headers dynamically
- Set variables before the request

Available globals:
- `request` — the raw `HttpRequestMessage`
- `variables` — the merged variable dictionary (readable and writable)

```json
"preScript": "$request.headers.add(\"Authorization\", \"Bearer {{apiKey}}\")"
```

### postScript

Executed **after** the HTTP response is received. Use it to:
- Assert the response status code
- Validate response body content
- Extract values from the response into variables for subsequent requests

Available globals:
- `request` — the raw `HttpRequestMessage`
- `response` — an `HttpTestResponseContext` with status code and body access
- `variables` — the merged variable dictionary (readable and writable)

```json
"postScript": "$response.EnsureSuccessStatusCode()"
```

### Simple Shorthand Syntax

| Pattern | Description |
|---------|-------------|
| `$request.headers.add("name", "value")` | Add a request header |
| `$request.headers.set("name", "value")` | Set/replace a request header |
| `$response.EnsureSuccessStatusCode()` | Fail if status is not 2xx |
| `$response.StatusCode == 201` | Assert exact status code |
| `$response.StatusCode != 404` | Assert status code differs |
| `$response.Body.Contains("text")` | Assert body contains text |

### Complex C# Scripts

For advanced logic, use full C# expressions. The `$request`/`$response` prefix is optional:

```csharp
// preScript – inject a dynamic API key from an environment variable
var key = System.Environment.GetEnvironmentVariable("API_KEY");
request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {key}");
```

```csharp
// postScript – extract a token and store it for later requests
response.EnsureSuccessStatusCode();
var token = (string)response.body.json.token;
token.ShouldNotBeNullOrEmpty("Token must be present in the response");
variables["token"] = token;
```

## Assertion Helpers

All assertion failures throw `HttpTestAssertionException`, which immediately stops the script and marks the request as failed.

### Fluent Extension Methods

Fluent assertions are available as extension methods on common types:

```csharp
// Status code
response.StatusCode.ShouldBe(200);
response.StatusCode.ShouldNotBe(500);
response.StatusCode.ShouldBeGreaterThanOrEqualTo(200);
response.StatusCode.ShouldBeLessThan(300);

// Boolean conditions
(response.StatusCode < 500).ShouldBeTrue("Server error");
false.ShouldBeFalse();

// JSON body access
var id = (long)response.body.json.id;
id.ShouldBeGreaterThan(0L);

// Body text
response.body.text.ShouldContain("\"status\":\"ok\"");
response.body.text.ShouldNotContain("error");
response.body.text.ShouldStartWith("{");

// String values
var name = (string)response.body.json.name;
name.ShouldBe("John Doe");
name.ShouldNotBeNullOrEmpty("Name field must be present");

// Null checks
((object)response.body.json.id).ShouldNotBeNull("ID must be present");
```

**Full list of extension methods:**

| Method | Applies to | Description |
|--------|-----------|-------------|
| `ShouldBeTrue(msg?)` | `bool` | Asserts value is `true` |
| `ShouldBeFalse(msg?)` | `bool` | Asserts value is `false` |
| `ShouldBeNull(msg?)` | `object?` | Asserts value is null |
| `ShouldNotBeNull(msg?)` | `object?` | Asserts value is not null |
| `ShouldBe(expected, msg?)` | `int`, `long`, `string?` | Asserts equality |
| `ShouldNotBe(unexpected, msg?)` | `int` | Asserts inequality |
| `ShouldBeGreaterThan(n, msg?)` | `int`, `long` | Asserts `> n` |
| `ShouldBeGreaterThanOrEqualTo(n, msg?)` | `int` | Asserts `>= n` |
| `ShouldBeLessThan(n, msg?)` | `int` | Asserts `< n` |
| `ShouldContain(s, msg?)` | `string?` | Asserts string contains substring |
| `ShouldNotContain(s, msg?)` | `string?` | Asserts string does not contain substring |
| `ShouldStartWith(s, msg?)` | `string?` | Asserts string starts with prefix |
| `ShouldEndWith(s, msg?)` | `string?` | Asserts string ends with suffix |
| `ShouldNotBeNullOrEmpty(msg?)` | `string?` | Asserts not null or empty |
| `ShouldNotBeNullOrWhiteSpace(msg?)` | `string?` | Asserts not null, empty, or whitespace |

### HttpAssert Static Class

For xunit / `Debug.Assert`-style assertions:

```csharp
HttpAssert.Equal(200, response.StatusCode);
HttpAssert.NotEqual(500, response.StatusCode);
HttpAssert.True(response.StatusCode < 500, "Server error");
HttpAssert.False(response.StatusCode >= 500);
HttpAssert.NotNull(response.body.json.id, "ID must be present");
HttpAssert.Null(response.body.json.error);
HttpAssert.Contains("token", response.body.text);
HttpAssert.DoesNotContain("error", response.body.text);
HttpAssert.NotNullOrEmpty((string)response.body.json.name);
HttpAssert.GreaterThan(0, (int)response.StatusCode);
HttpAssert.LessThan(300, response.StatusCode);
HttpAssert.Fail("Explicit failure message");
```

### Built-in Response Helpers

`HttpTestResponseContext` exposes these helpers directly:

```csharp
// Throw if not 2xx
response.EnsureSuccessStatusCode();

// General condition assert
response.Assert(response.StatusCode == 200, "Expected 200 OK");

// Status code
int code = response.StatusCode;

// Response body
string text = response.body.text;
dynamic json = response.body.json;

// Deep JSON access
var userId = (long)response.body.json.user.id;
var email  = (string)response.body.json.user.email;
```

## Complete Example

The following collection demonstrates environments, variable inheritance, chaining values across requests, and a variety of assertion styles.

### Collection file: `users-api.httptest.json`

```json
{
  "name": "Users API",
  "variables": {
    "baseUrl": "https://api.example.com",
    "contentType": "application/json"
  },
  "postScript": "$response.EnsureSuccessStatusCode()",
  "groups": [
    {
      "name": "Auth",
      "requests": [
        {
          "name": "Login",
          "method": "POST",
          "url": "{{baseUrl}}/auth/login",
          "headers": {
            "Content-Type": "{{contentType}}"
          },
          "body": "{\"username\": \"{{username}}\", \"password\": \"{{password}}\"}",
          "postScript": "response.StatusCode.ShouldBe(200);\nvar token = (string)response.body.json.token;\ntoken.ShouldNotBeNullOrEmpty();\nvariables[\"token\"] = token;"
        }
      ]
    },
    {
      "name": "Users",
      "preScript": "$request.headers.add(\"Authorization\", \"Bearer {{token}}\")",
      "requests": [
        {
          "name": "ListUsers",
          "method": "GET",
          "url": "{{baseUrl}}/users",
          "postScript": "response.StatusCode.ShouldBe(200);\nHttpAssert.Contains(\"users\", response.body.text);"
        },
        {
          "name": "CreateUser",
          "method": "POST",
          "url": "{{baseUrl}}/users",
          "headers": {
            "Content-Type": "{{contentType}}"
          },
          "body": "{\"name\": \"Test User\", \"email\": \"test@example.com\"}",
          "postScript": "response.StatusCode.ShouldBe(201);\nvar id = (long)response.body.json.id;\nid.ShouldBeGreaterThan(0L);\nvariables[\"newUserId\"] = id.ToString();"
        },
        {
          "name": "GetUser",
          "method": "GET",
          "url": "{{baseUrl}}/users/{{newUserId}}",
          "postScript": "response.StatusCode.ShouldBe(200);\n((string)response.body.json.email).ShouldBe(\"test@example.com\");"
        },
        {
          "name": "DeleteUser",
          "method": "DELETE",
          "url": "{{baseUrl}}/users/{{newUserId}}",
          "postScript": "response.StatusCode.ShouldBe(204);"
        }
      ]
    }
  ]
}
```

### Environment file: `users-api.httptest.env.json`

```json
[
  {
    "name": "dev",
    "variables": {
      "baseUrl": "http://localhost:5000",
      "username": "dev_user",
      "password": "dev_pass"
    }
  },
  {
    "name": "staging",
    "variables": {
      "baseUrl": "https://staging.example.com",
      "username": "staging_user",
      "password": "staging_pass"
    }
  }
]
```

### Run it

```bash
# Run against dev
dotnet-http test users-api.httptest.json --env dev --env-file users-api.httptest.env.json

# Preview requests without sending (offline mode)
dotnet-http test users-api.httptest.json --env dev --env-file users-api.httptest.env.json --offline
```

### Example output

```
=== Collection: Users API ===
--- Group: Auth ---
  [Login]
  Request:
  POST https://api.example.com/auth/login
  Content-Type: application/json
  ...
  Response (42ms):
  HTTP/1.1 200 OK
  ...
  ✓ Login PASSED (42ms)
--- Group: Users ---
  [ListUsers]
  ...
  ✓ ListUsers PASSED (18ms)
  [CreateUser]
  ...
  ✓ CreateUser PASSED (23ms)
  [GetUser]
  ...
  ✓ GetUser PASSED (11ms)
  [DeleteUser]
  ...
  ✓ DeleteUser PASSED (9ms)

=== Test Summary: Users API ===
  Total:  5
  Passed: 5
  Failed: 0
```

## CLI Reference

```
dotnet-http test [collectionPath] [options]

Arguments:
  collectionPath    Path to the test collection file (.httptest.json)

Options:
  --env <name>          The environment name to use
  --env-file <path>     Path to an environment file (.httptest.env.json)
  --offline             Print requests without sending them
```

## CI/CD Integration

### GitHub Actions

```yaml
name: API Tests
on: [push, pull_request]

jobs:
  api-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Install dotnet-httpie
        run: dotnet tool install --global dotnet-httpie
      - name: Run API tests
        run: dotnet-http test tests/users-api.httptest.json --env ci --env-file tests/users-api.httptest.env.json
        env:
          API_KEY: ${{ secrets.API_KEY }}
```

### Azure DevOps

```yaml
steps:
- task: DotNetCoreCLI@2
  displayName: Install dotnet-httpie
  inputs:
    command: custom
    custom: tool
    arguments: install --global dotnet-httpie

- script: dotnet-http test tests/api.httptest.json --env $(Environment) --env-file tests/api.httptest.env.json
  displayName: Run API Test Collection
```

## Tips & Best Practices

1. **Use environments** — keep environment-specific values (base URLs, credentials) in `.httptest.env.json` files and out of the collection.
2. **Chain with variables** — extract values from responses and store them in `variables` for use in subsequent requests.
3. **Set a collection-level postScript** — a global `$response.EnsureSuccessStatusCode()` catches unexpected errors without repeating it in every request.
4. **Override per request** — override collection/group scripts at the request level for requests that have intentionally non-2xx responses.
5. **Use offline mode first** — run with `--offline` to verify URLs and headers before sending real requests.
6. **Version control your collections** — commit `*.httptest.json` files alongside your source code so tests evolve with the API.
7. **Keep env files out of source control** — add `*.httptest.env.json` to `.gitignore` if they contain secrets, and use CI secrets instead.

## Next Steps

- Learn about [file execution](file-execution.md) for running `.http` files
- Explore [CI/CD integration](ci-cd-integration.md) for automated testing pipelines
- Check [authentication](authentication.md) patterns for securing requests
