# Quick Start Guide

Get up and running with dotnet-httpie in minutes! This guide shows you the essential commands to start making HTTP requests.

## Your First Request

Make a simple GET request:

```bash
dotnet-http httpbin.org/get
```

This will send a GET request to httpbin.org and display the response.

## Basic Command Structure

```
dotnet-http [flags] [METHOD] URL [ITEM [ITEM]]
```

Where:
- `flags`: Optional command flags (e.g., `--offline`, `--debug`)
- `METHOD`: HTTP method (GET, POST, PUT, DELETE, etc.) - defaults to GET
- `URL`: The target URL
- `ITEM`: Request items (query parameters, headers, data)

## Request Item Types

dotnet-httpie supports three types of request items:

| Type | Syntax | Example | Description |
|------|--------|---------|-------------|
| Query Parameters | `name==value` | `search==dotnet` | URL query parameters |
| Headers | `name:value` | `Authorization:Bearer token` | HTTP headers |
| JSON Data | `name=value` | `title=hello` | JSON request body fields |
| Raw JSON | `name:=value` | `age:=25` | Raw JSON values (numbers, booleans, objects) |

## Common Examples

### GET with Query Parameters

```bash
dotnet-http httpbin.org/get search==httpie limit==10
```

### POST with JSON Data

```bash
dotnet-http POST httpbin.org/post title=Hello body="World"
```

### Custom Headers

```bash
dotnet-http httpbin.org/headers Authorization:"Bearer your-token" User-Agent:"MyApp/1.0"
```

### Mixed Request Types

```bash
dotnet-http POST api.example.com/users \
  Authorization:"Bearer token" \
  name="John Doe" \
  age:=30 \
  active:=true \
  search==users
```

## Working with APIs

### RESTful API Example

```bash
# List users
dotnet-http GET api.example.com/users

# Get specific user
dotnet-http GET api.example.com/users/123

# Create new user
dotnet-http POST api.example.com/users name="John" email="john@example.com"

# Update user
dotnet-http PUT api.example.com/users/123 name="John Smith"

# Delete user
dotnet-http DELETE api.example.com/users/123
```

### JSON API with Authentication

```bash
dotnet-http POST api.example.com/posts \
  Authorization:"Bearer your-jwt-token" \
  Content-Type:"application/json" \
  title="My Post" \
  content="This is my post content" \
  published:=true
```

## Useful Flags

### Offline Mode (Preview Request)

```bash
dotnet-http POST api.example.com/data name=test --offline
```

This shows what request would be sent without actually sending it.

### Debug Mode

```bash
dotnet-http httpbin.org/get --debug
```

Enables detailed logging and debugging information.

### Response Body Only

```bash
dotnet-http httpbin.org/get --body
```

Shows only the response body, useful for piping to other tools.

## File Operations

### Execute HTTP Files

```bash
dotnet-http exec requests.http
```

### Download Files

```bash
dotnet-http httpbin.org/image/png --download
```

## Docker Usage

If you're using the Docker version:

```bash
# Basic request
docker run --rm weihanli/dotnet-httpie:latest httpbin.org/get

# With data
docker run --rm weihanli/dotnet-httpie:latest POST httpbin.org/post name=test
```

## Local Development

For local APIs, you can use shortened URLs:

```bash
# Instead of http://localhost:5000/api/users
dotnet-http :5000/api/users

# Or
dotnet-http localhost:5000/api/users
```

## Next Steps

Now that you're familiar with the basics:

1. Learn about [advanced request data types](request-data-types.md)
2. Explore [file execution capabilities](file-execution.md)
3. Set up [authentication](authentication.md) for your APIs
4. Check out [common use cases](examples/common-use-cases.md)

## Need Help?

- Use `dotnet-http --help` for command-line help
- See [debugging guide](debugging.md) for troubleshooting
- Check [examples](examples/common-use-cases.md) for more usage patterns