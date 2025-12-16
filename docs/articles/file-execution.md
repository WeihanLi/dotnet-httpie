# File Execution

dotnet-httpie can execute HTTP requests from `.http` and `.rest` files, making it perfect for API testing, documentation, and automation.

## Overview

The `exec` command allows you to run HTTP requests defined in files, supporting:
- Standard `.http` and `.rest` file formats
- Variable substitution
- Environment-specific configurations
- Request chaining and referencing

## Basic Usage

### Execute Single File

```bash
dotnet-http exec requests.http
```

### Execute with Environment

```bash
dotnet-http exec requests.http --env production
```

### Execute Specific Request Type

```bash
dotnet-http exec requests.http --type http
dotnet-http exec curl-commands.curl --type curl
```

## HTTP File Format

### Basic Request

```http
# Get user information
GET https://api.example.com/users/123
Authorization: Bearer your-token
```

### Multiple Requests

```http
# Get all users
GET https://api.example.com/users
Authorization: Bearer your-token

###

# Create new user
POST https://api.example.com/users
Content-Type: application/json
Authorization: Bearer your-token

{
  "name": "John Doe",
  "email": "john@example.com"
}

###

# Update user
PUT https://api.example.com/users/123
Content-Type: application/json
Authorization: Bearer your-token

{
  "name": "John Smith",
  "email": "john.smith@example.com"
}
```

### Request with Variables

```http
@baseUrl = https://api.example.com
@token = your-bearer-token

# Get user
GET {{baseUrl}}/users/123
Authorization: Bearer {{token}}

###

# Create user with dynamic data
POST {{baseUrl}}/users
Content-Type: application/json
Authorization: Bearer {{token}}

{
  "name": "User {{$randomInt}}",
  "email": "user{{$randomInt}}@example.com",
  "timestamp": "{{$datetime iso8601}}"
}
```

### Named Requests

```http
@baseUrl = https://api.example.com

###

# @name getUser
GET {{baseUrl}}/users/123

###

# @name createUser
POST {{baseUrl}}/users
Content-Type: application/json

{
  "name": "John Doe",
  "email": "john@example.com"
}
```

## Environment Files

### HTTP Client Environment File

Create `http-client.env.json`:

```json
{
  "development": {
    "baseUrl": "http://localhost:3000",
    "apiKey": "dev-api-key"
  },
  "staging": {
    "baseUrl": "https://staging-api.example.com",
    "apiKey": "staging-api-key"
  },
  "production": {
    "baseUrl": "https://api.example.com",
    "apiKey": "prod-api-key"
  }
}
```

### Using Environment Variables

```http
# This will use variables from the specified environment
GET {{baseUrl}}/users
X-API-Key: {{apiKey}}
```

Execute with specific environment:

```bash
dotnet-http exec api-requests.http --env production
```

## Variable Types

### Built-in Variables

```http
# Random values
POST {{baseUrl}}/users
Content-Type: application/json

{
  "id": "{{$uuid}}",
  "name": "User {{$randomInt}}",
  "email": "user{{$randomInt}}@example.com",
  "timestamp": "{{$datetime iso8601}}",
  "created": "{{$timestamp}}"
}
```

### Environment Variables

```http
# Access system environment variables
GET {{baseUrl}}/secure
Authorization: Bearer {{$env API_TOKEN}}
```

### Custom Variables

```http
@userId = 123
@apiVersion = v2

GET {{baseUrl}}/{{apiVersion}}/users/{{userId}}
```

## Request Referencing

Reference responses from previous requests:

```http
# @name login
POST {{baseUrl}}/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "password"
}

###

# Use token from login response
GET {{baseUrl}}/protected/data
Authorization: Bearer {{login.response.body.token}}

###

# Reference request headers
GET {{baseUrl}}/audit
X-Original-Request-Id: {{login.request.headers.X-Request-ID}}
```

## Curl File Execution

dotnet-httpie can also execute curl commands from files:

### Curl File Format

```bash
# file: api-calls.curl

# Get user data
curl -X GET "https://api.example.com/users/123" \
  -H "Authorization: Bearer token" \
  -H "Accept: application/json"

# Create new user
curl -X POST "https://api.example.com/users" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer token" \
  -d '{
    "name": "John Doe",
    "email": "john@example.com"
  }'
```

### Execute Curl File

```bash
dotnet-http exec api-calls.curl --type curl
```

## Advanced Features

### Request Chaining

```http
# @name createUser
POST {{baseUrl}}/users
Content-Type: application/json

{
  "name": "John Doe",
  "email": "john@example.com"
}

###

# @name getUserProfile
# @depends createUser
GET {{baseUrl}}/users/{{createUser.response.body.id}}/profile

###

# @name updateProfile
# @depends getUserProfile
PUT {{baseUrl}}/users/{{createUser.response.body.id}}/profile
Content-Type: application/json

{
  "bio": "Updated bio",
  "avatar": "{{getUserProfile.response.body.avatar}}"
}
```

## Testing and Validation

### Response Assertions

```http
GET {{baseUrl}}/users/123

# Test response
# @test status === 200
# @test response.body.name === "John Doe"
# @test response.headers["content-type"] includes "application/json"
```

### Schema Validation

```http
POST {{baseUrl}}/users
Content-Type: application/json

{
  "name": "John Doe",
  "email": "john@example.com"
}

# Validate response against JSON schema
# @schema user-schema.json
```

## Debugging File Execution

### Debug Mode

```bash
dotnet-http exec requests.http --debug
```

### Offline Mode (Preview)

```bash
dotnet-http exec requests.http --offline
```

This shows what requests would be sent without actually executing them.

### Verbose Output

```bash
dotnet-http exec requests.http --verbose
```

## Organization Strategies

### Project Structure

```
project/
├── api-tests/
│   ├── auth/
│   │   ├── login.http
│   │   └── logout.http
│   ├── users/
│   │   ├── create-user.http
│   │   ├── get-user.http
│   │   └── update-user.http
│   └── http-client.env.json
├── environments/
│   ├── development.env.json
│   ├── staging.env.json
│   └── production.env.json
└── scripts/
    ├── setup.http
    ├── cleanup.http
    └── health-check.http
```

### File Naming Conventions

- Use descriptive names: `create-user.http`, `get-order-details.http`
- Group by feature: `auth/`, `users/`, `orders/`
- Use environment prefixes: `dev-setup.http`, `prod-health.http`

## CI/CD Integration

### GitHub Actions Example

```yaml
name: API Tests
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v6
      - name: Setup .NET
        uses: actions/setup-dotnet@v5
        with:
          dotnet-version: '10.0.x'
      - name: Install dotnet-httpie
        run: dotnet tool install --global dotnet-httpie
      - name: Run API tests
        run: dotnet-http exec tests/api-tests.http --env testing
```

### Azure DevOps Example

```yaml
steps:
- task: DotNetCoreCLI@2
  displayName: 'Install dotnet-httpie'
  inputs:
    command: 'custom'
    custom: 'tool'
    arguments: 'install --global dotnet-httpie'

- script: dotnet-http exec api-tests/health-check.http --env $(Environment)
  displayName: 'Run Health Check'
```

## Best Practices

1. **Use environment files** for different deployment environments
2. **Name your requests** for better organization and referencing
3. **Group related requests** in the same file
4. **Use variables** instead of hardcoding values
5. **Add comments** to explain complex requests
6. **Validate responses** where critical
7. **Test offline first** to verify request structure
8. **Version control** your HTTP files alongside your code

## Examples

### Complete API Test Suite

```http
@baseUrl = https://api.example.com
@contentType = application/json

###

# @name healthCheck
GET {{baseUrl}}/health

###

# @name authenticate
POST {{baseUrl}}/auth/login
Content-Type: {{contentType}}

{
  "username": "testuser",
  "password": "testpass"
}

###

# @name createUser
POST {{baseUrl}}/users
Authorization: Bearer {{authenticate.response.body.token}}
Content-Type: {{contentType}}

{
  "name": "Test User",
  "email": "test@example.com"
}

###

# @name getUser
GET {{baseUrl}}/users/{{createUser.response.body.id}}
Authorization: Bearer {{authenticate.response.body.token}}

###

# @name updateUser
PUT {{baseUrl}}/users/{{createUser.response.body.id}}
Authorization: Bearer {{authenticate.response.body.token}}
Content-Type: {{contentType}}

{
  "name": "Updated Test User"
}

###

# @name deleteUser
DELETE {{baseUrl}}/users/{{createUser.response.body.id}}
Authorization: Bearer {{authenticate.response.body.token}}
```

## Next Steps

- Learn about [variable substitution](variables.md) in detail
- Explore [request referencing](request-referencing.md) patterns
- Set up [CI/CD integration](ci-cd-integration.md) with HTTP files
- Check [common use cases](examples/common-use-cases.md) for more examples