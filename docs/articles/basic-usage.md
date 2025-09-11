# Basic Usage

This guide covers the fundamental concepts and basic usage patterns of dotnet-httpie.

## Command Structure

The basic syntax for dotnet-httpie commands:

```
dotnet-http [flags] [METHOD] URL [ITEM [ITEM]]
```

### Components

- **flags**: Optional command flags (e.g., `--offline`, `--debug`, `--body`)
- **METHOD**: HTTP method (GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS)
- **URL**: Target URL (can be full URL or shortened format)
- **ITEM**: Request items (query parameters, headers, data)

## HTTP Methods

### GET (Default)

```bash
# Simple GET request
dotnet-http httpbin.org/get

# GET with query parameters
dotnet-http httpbin.org/get name==John age==30

# Explicit GET method
dotnet-http GET httpbin.org/get search==query
```

### POST

```bash
# POST with JSON data
dotnet-http POST httpbin.org/post name=John email=john@example.com

# POST with form data
dotnet-http POST httpbin.org/post --form name=John email=john@example.com
```

### PUT

```bash
# PUT request (typically for updates)
dotnet-http PUT httpbin.org/put id:=123 name=John
```

### DELETE

```bash
# DELETE request
dotnet-http DELETE httpbin.org/delete

# DELETE with parameters
dotnet-http DELETE api.example.com/users/123
```

### Other Methods

```bash
# PATCH for partial updates
dotnet-http PATCH httpbin.org/patch status=active

# HEAD for headers only
dotnet-http HEAD httpbin.org/get

# OPTIONS for allowed methods
dotnet-http OPTIONS httpbin.org
```

## URL Formats

### Full URLs

```bash
# HTTPS URLs
dotnet-http https://api.example.com/users

# HTTP URLs
dotnet-http http://localhost:3000/api/data

# URLs with ports
dotnet-http https://api.example.com:8443/secure
```

### Shortened URLs

```bash
# Localhost shortcuts
dotnet-http :3000/api/users          # → http://localhost:3000/api/users
dotnet-http localhost:5000/health    # → http://localhost:5000/health

# HTTPS by default for domains
dotnet-http api.example.com/data     # → https://api.example.com/data
```

### URL with Paths

```bash
# Simple paths
dotnet-http api.example.com/v1/users

# Complex paths with parameters
dotnet-http api.example.com/users/123/posts/456

# Paths with special characters
dotnet-http "api.example.com/search?q=hello world"
```

## Request Items

### Query Parameters (`==`)

Query parameters are added to the URL:

```bash
# Single parameter
dotnet-http httpbin.org/get name==John

# Multiple parameters
dotnet-http httpbin.org/get name==John age==30 city=="New York"

# Arrays/multiple values
dotnet-http httpbin.org/get tag==javascript tag==web tag==api
```

### Headers (`:`)

Headers control request behavior:

```bash
# Authentication header
dotnet-http httpbin.org/headers Authorization:"Bearer token123"

# Content type
dotnet-http POST httpbin.org/post Content-Type:"application/xml"

# Multiple headers
dotnet-http httpbin.org/headers \
  Authorization:"Bearer token" \
  User-Agent:"MyApp/1.0" \
  Accept:"application/json"
```

### JSON Data (`=`)

Creates JSON request body:

```bash
# Simple fields
dotnet-http POST httpbin.org/post name=John age=30

# Creates: {"name": "John", "age": "30"}
```

### Raw JSON Data (`:=`)

For typed JSON values:

```bash
# Numbers
dotnet-http POST httpbin.org/post age:=30 price:=19.99

# Booleans
dotnet-http POST httpbin.org/post active:=true published:=false

# Arrays
dotnet-http POST httpbin.org/post tags:='["web", "api", "tool"]'

# Objects
dotnet-http POST httpbin.org/post profile:='{"name": "John", "level": 5}'

# Creates: {"age": 30, "price": 19.99, "active": true, "published": false, "tags": ["web", "api", "tool"], "profile": {"name": "John", "level": 5}}
```

## Common Patterns

### API Testing

```bash
# Health check
dotnet-http GET api.example.com/health

# Get list of resources
dotnet-http GET api.example.com/users

# Get specific resource
dotnet-http GET api.example.com/users/123

# Create new resource
dotnet-http POST api.example.com/users name=John email=john@example.com

# Update resource
dotnet-http PUT api.example.com/users/123 name="John Smith"

# Delete resource
dotnet-http DELETE api.example.com/users/123
```

### Authentication Patterns

```bash
# Bearer token
dotnet-http GET api.example.com/protected \
  Authorization:"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

# API key in header
dotnet-http GET api.example.com/data \
  X-API-Key:"your-api-key"

# API key in query
dotnet-http GET api.example.com/data \
  api_key==your-api-key

# Basic authentication
dotnet-http GET api.example.com/secure \
  Authorization:"Basic $(echo -n 'user:pass' | base64)"
```

### Data Submission Patterns

```bash
# Simple form data
dotnet-http POST api.example.com/contact \
  name=John \
  email=john@example.com \
  message="Hello from dotnet-httpie"

# Complex nested data
dotnet-http POST api.example.com/orders \
  customer[name]=John \
  customer[email]=john@example.com \
  items[0][id]:=1 \
  items[0][quantity]:=2 \
  items[1][id]:=2 \
  items[1][quantity]:=1 \
  total:=99.99

# File upload
dotnet-http POST api.example.com/upload \
  --multipart \
  description="My document" \
  file@/path/to/document.pdf
```

## Response Handling

### Default Response

Shows headers and body:

```bash
dotnet-http GET httpbin.org/get
```

### Body Only

```bash
# Only response body
dotnet-http GET httpbin.org/get --body

# Useful for piping to other tools
dotnet-http GET api.example.com/users --body | jq '.users[0]'
```

### Headers Only

```bash
# Only response headers
dotnet-http HEAD httpbin.org/get
```

### Save Response

```bash
# Save to file
dotnet-http GET api.example.com/report --body > report.json

# Download files
dotnet-http GET api.example.com/files/document.pdf --download
```

## Useful Flags

### Debug Mode

Get detailed information about the request:

```bash
dotnet-http GET api.example.com/data --debug
```

### Offline Mode

Preview the request without sending it:

```bash
dotnet-http POST api.example.com/users name=John --offline
```

### Check Status

Exit with non-zero code for HTTP errors:

```bash
if dotnet-http GET api.example.com/health --check-status; then
  echo "API is healthy"
else
  echo "API is down"
fi
```

## Working with JSON

### Simple JSON

```bash
# String values (default)
dotnet-http POST httpbin.org/post name=John title="Software Engineer"

# Number values
dotnet-http POST httpbin.org/post age:=30 salary:=75000

# Boolean values
dotnet-http POST httpbin.org/post active:=true verified:=false

# Null values
dotnet-http POST httpbin.org/post middle_name:=null
```

### Complex JSON

```bash
# Arrays
dotnet-http POST httpbin.org/post \
  skills:='["C#", "JavaScript", "Python"]' \
  scores:='[95, 87, 92]'

# Nested objects
dotnet-http POST httpbin.org/post \
  address:='{"street": "123 Main St", "city": "Seattle", "zip": "98101"}' \
  contact:='{"email": "john@example.com", "phone": "+1-555-0123"}'

# Mixed complex data
dotnet-http POST httpbin.org/post \
  name=John \
  age:=30 \
  active:=true \
  skills:='["programming", "testing"]' \
  address:='{"city": "Seattle", "state": "WA"}' \
  metadata:=null
```

## Error Handling

### HTTP Status Codes

```bash
# dotnet-httpie shows HTTP errors clearly
dotnet-http GET httpbin.org/status/404  # Shows 404 Not Found
dotnet-http GET httpbin.org/status/500  # Shows 500 Internal Server Error
```

### Debugging Errors

```bash
# Use debug mode to see detailed error information
dotnet-http GET api.example.com/broken --debug

# Check request format first
dotnet-http POST api.example.com/users invalid-data --offline
```

## Tips and Best Practices

### 1. Use Environment Variables

```bash
# Store API tokens in environment variables
export API_TOKEN="your-secret-token"
dotnet-http GET api.example.com/protected Authorization:"Bearer $API_TOKEN"

# Store base URLs
export API_BASE="https://api.example.com"
dotnet-http GET "$API_BASE/users"
```

### 2. Quote Special Characters

```bash
# Quote values with spaces or special characters
dotnet-http POST httpbin.org/post message="Hello, world!" tags:='["tag with spaces", "special!chars"]'
```

### 3. Use Files for Large Data

```bash
# Instead of long command lines, use files
cat > user.json << EOF
{
  "name": "John Doe",
  "email": "john@example.com",
  "address": {
    "street": "123 Main St",
    "city": "Seattle",
    "state": "WA",
    "zip": "98101"
  }
}
EOF

dotnet-http POST api.example.com/users @user.json
```

### 4. Combine with Other Tools

```bash
# Extract specific data with jq
USER_ID=$(dotnet-http POST api.example.com/users name=John --body | jq -r '.id')
dotnet-http GET "api.example.com/users/$USER_ID"

# Format JSON output
dotnet-http GET api.example.com/users | jq .

# Save and process responses
dotnet-http GET api.example.com/users --body > users.json
jq '.users[] | select(.active == true)' users.json
```

### 5. Test Incrementally

```bash
# Start with simple requests
dotnet-http GET api.example.com/health

# Add authentication
dotnet-http GET api.example.com/protected Authorization:"Bearer $TOKEN"

# Add data gradually
dotnet-http POST api.example.com/users name=John
dotnet-http POST api.example.com/users name=John email=john@example.com
dotnet-http POST api.example.com/users name=John email=john@example.com age:=30
```

## Common Use Cases

### Development Workflow

```bash
# 1. Check if API is running
dotnet-http GET localhost:3000/health

# 2. Test authentication
dotnet-http POST localhost:3000/auth/login username=admin password=password

# 3. Test CRUD operations
dotnet-http GET localhost:3000/api/users
dotnet-http POST localhost:3000/api/users name=Test email=test@example.com
dotnet-http PUT localhost:3000/api/users/1 name="Updated Name"
dotnet-http DELETE localhost:3000/api/users/1
```

### API Exploration

```bash
# Discover API endpoints
dotnet-http OPTIONS api.example.com

# Check API documentation endpoint
dotnet-http GET api.example.com/docs

# Test different response formats
dotnet-http GET api.example.com/users Accept:"application/json"
dotnet-http GET api.example.com/users Accept:"application/xml"
```

### Integration Testing

```bash
# Test service dependencies
dotnet-http GET auth-service.internal/health
dotnet-http GET user-service.internal/health
dotnet-http GET order-service.internal/health

# Test cross-service communication
TOKEN=$(dotnet-http POST auth-service.internal/token client_id=test --body | jq -r '.access_token')
dotnet-http GET user-service.internal/profile Authorization:"Bearer $TOKEN"
```

## Next Steps

- Learn about [advanced request data types](request-data-types.md)
- Explore [authentication methods](authentication.md)
- Try [file execution](file-execution.md) for complex workflows
- Check out [common use cases](examples/common-use-cases.md) for real-world examples
- Use [debugging techniques](debugging.md) when things go wrong