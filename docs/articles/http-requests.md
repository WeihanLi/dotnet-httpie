# HTTP Requests

This guide covers how to make various types of HTTP requests using dotnet-httpie.

## HTTP Methods

dotnet-httpie supports all standard HTTP methods. If no method is specified, GET is used by default.

### GET Requests

```bash
# Simple GET
dotnet-http httpbin.org/get

# GET with query parameters
dotnet-http httpbin.org/get name==John age==30

# GET with headers
dotnet-http httpbin.org/get Authorization:"Bearer token"
```

### POST Requests

```bash
# JSON POST request
dotnet-http POST httpbin.org/post name=John email=john@example.com

# Form data POST request  
dotnet-http POST httpbin.org/post --form name=John email=john@example.com

# Raw data POST request
dotnet-http POST httpbin.org/post --raw "Custom raw data"
```

### PUT Requests

```bash
# Update resource
dotnet-http PUT api.example.com/users/123 name="John Smith" email="john.smith@example.com"

# Replace entire resource
dotnet-http PUT api.example.com/users/123 @user.json
```

### PATCH Requests

```bash
# Partial update
dotnet-http PATCH api.example.com/users/123 email="newemail@example.com"
```

### DELETE Requests

```bash
# Delete resource
dotnet-http DELETE api.example.com/users/123

# Delete with confirmation header
dotnet-http DELETE api.example.com/users/123 X-Confirm:"yes"
```

### HEAD Requests

```bash
# Get headers only
dotnet-http HEAD httpbin.org/get
```

### OPTIONS Requests

```bash
# Check allowed methods
dotnet-http OPTIONS api.example.com/users
```

## Request URLs

### Full URLs

```bash
dotnet-http https://api.example.com/users
dotnet-http http://localhost:3000/api/data
```

### Shortened Local URLs

```bash
# These are equivalent to http://localhost:PORT
dotnet-http :3000/api/users
dotnet-http localhost:3000/api/users
```

### URL with Parameters

```bash
# Query parameters are added automatically
dotnet-http api.example.com/search q==httpie type==tool
# Results in: api.example.com/search?q=httpie&type=tool
```

## Request Headers

### Standard Headers

```bash
# Authorization
dotnet-http api.example.com/protected Authorization:"Bearer jwt-token"

# Content-Type
dotnet-http POST api.example.com/data Content-Type:"application/xml" @data.xml

# User-Agent
dotnet-http api.example.com/get User-Agent:"MyApp/1.0"

# Accept
dotnet-http api.example.com/data Accept:"application/json"
```

### Custom Headers

```bash
# API keys
dotnet-http api.example.com/data X-API-Key:"your-api-key"

# Custom headers
dotnet-http api.example.com/data X-Custom-Header:"custom-value"
```

### Multiple Headers

```bash
dotnet-http api.example.com/data \
  Authorization:"Bearer token" \
  X-API-Key:"api-key" \
  User-Agent:"MyApp/1.0" \
  Accept:"application/json"
```

## Request Body

### JSON Body (Default)

```bash
# Simple JSON
dotnet-http POST api.example.com/users name=John age:=30 active:=true

# Nested JSON
dotnet-http POST api.example.com/users name=John address[city]=Seattle address[country]=USA

# Array values
dotnet-http POST api.example.com/users name=John tags:='["developer", "dotnet"]'

# Raw JSON objects
dotnet-http POST api.example.com/users profile:='{"name": "John", "age": 30}'
```

### Form Data

```bash
# URL-encoded form data
dotnet-http POST httpbin.org/post --form name=John email=john@example.com
```

### Raw Data

```bash
# Send raw string
dotnet-http POST api.example.com/webhook --raw "Raw webhook payload"

# Send from stdin
echo "data" | dotnet-http POST api.example.com/data
```

## Complex JSON Structures

### Nested Objects

```bash
dotnet-http POST api.example.com/users \
  name=John \
  address[street]="123 Main St" \
  address[city]=Seattle \
  address[zipcode]:=98101
```

### Arrays

```bash
# Array of strings
dotnet-http POST api.example.com/users name=John skills:='["C#", "JavaScript", "Python"]'

# Array of numbers
dotnet-http POST api.example.com/data values:='[1, 2, 3, 4, 5]'

# Array of objects
dotnet-http POST api.example.com/batch items:='[{"id": 1, "name": "Item 1"}, {"id": 2, "name": "Item 2"}]'
```

### Boolean and Null Values

```bash
# Boolean values
dotnet-http POST api.example.com/users name=John active:=true verified:=false

# Null values
dotnet-http POST api.example.com/users name=John middle_name:=null

# Numbers
dotnet-http POST api.example.com/users name=John age:=30 salary:=50000.50
```

## Response Handling

### View Full Response

```bash
# Default: shows headers and body
dotnet-http httpbin.org/get
```

### Body Only

```bash
# Show only response body
dotnet-http httpbin.org/get --body
```

### Headers Only

```bash
# Show only response headers
dotnet-http HEAD httpbin.org/get
```

### Save Response

```bash
# Save to file
dotnet-http httpbin.org/get > response.json

# Download files
dotnet-http httpbin.org/image/png --download
```

## Error Handling

### HTTP Error Codes

```bash
# dotnet-httpie shows HTTP errors clearly
dotnet-http httpbin.org/status/404
dotnet-http httpbin.org/status/500
```

### Timeout Configuration

```bash
# Set request timeout (if supported by middleware)
dotnet-http api.example.com/slow-endpoint --timeout 30
```

## Advanced Features

### Follow Redirects

```bash
# Automatically follow redirects
dotnet-http httpbin.org/redirect/3 --follow
```

### Ignore SSL Errors

```bash
# For development/testing only
dotnet-http https://self-signed.badssl.com/ --verify=no
```

### Proxy Support

```bash
# Use proxy
dotnet-http httpbin.org/get --proxy http://proxy.example.com:8080
```

## Examples with Real APIs

### GitHub API

```bash
# Get user info
dotnet-http api.github.com/users/octocat

# Get repositories (with authentication)
dotnet-http api.github.com/user/repos Authorization:"token your-token"

# Create issue
dotnet-http POST api.github.com/repos/owner/repo/issues \
  Authorization:"token your-token" \
  title="Bug report" \
  body="Description of the bug"
```

### REST API CRUD Operations

```bash
# Create
dotnet-http POST api.example.com/articles \
  title="My Article" \
  content="Article content" \
  published:=false

# Read
dotnet-http GET api.example.com/articles/123

# Update
dotnet-http PUT api.example.com/articles/123 \
  title="Updated Article" \
  published:=true

# Delete
dotnet-http DELETE api.example.com/articles/123
```

## Best Practices

1. **Use meaningful names** for your request files and variables
2. **Store sensitive data** like API keys in environment variables
3. **Use --offline mode** to preview requests before sending
4. **Combine with jq** for JSON processing: `dotnet-http api.example.com/data | jq .`
5. **Use files for complex data** instead of inline JSON for better maintainability

## Next Steps

- Learn about [authentication methods](authentication.md)
- Explore [file execution](file-execution.md) for repeatable requests
- Check out [variable substitution](variables.md) for dynamic requests
