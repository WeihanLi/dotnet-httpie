# Command Line Options Reference

This comprehensive reference covers all command line options and flags available in dotnet-httpie.

## Command Syntax

```
dotnet-http [global-flags] [METHOD] URL [request-items...] [request-flags]
```

## Global Flags

### Help and Version

| Flag | Description | Example |
|------|-------------|---------|
| `--help`, `-h` | Show help information | `dotnet-http --help` |
| `--version` | Show version information | `dotnet-http --version` |

### Debug and Logging

| Flag | Description | Example |
|------|-------------|---------|
| `--debug` | Enable debug mode with detailed logging | `dotnet-http GET api.example.com --debug` |
| `--verbose`, `-v` | Enable verbose output | `dotnet-http GET api.example.com --verbose` |
| `--quiet`, `-q` | Suppress non-error output | `dotnet-http GET api.example.com --quiet` |

### Request Preview

| Flag | Description | Example |
|------|-------------|---------|
| `--offline` | Preview request without sending | `dotnet-http POST api.example.com --offline` |
| `--print` | Specify what to print (request/response) | `dotnet-http GET api.example.com --print=HhBb` |

## HTTP Methods

All standard HTTP methods are supported:

```bash
# GET (default)
dotnet-http GET api.example.com/users
dotnet-http api.example.com/users  # GET is default when there's no body data parameter

# POST
dotnet-http POST api.example.com/users

# PUT
dotnet-http PUT api.example.com/users/123

# PATCH
dotnet-http PATCH api.example.com/users/123

# DELETE
dotnet-http DELETE api.example.com/users/123

# HEAD
dotnet-http HEAD api.example.com/users

# OPTIONS
dotnet-http OPTIONS api.example.com/users
```

## Request Flags

### Authentication

| Flag | Description | Example |
|------|-------------|---------|
| `--auth`, `-a` | Basic authentication | `dotnet-http GET api.example.com --auth user:pass` |
| `--auth-type` | Authentication type | `dotnet-http GET api.example.com --auth-type bearer` |

### Request Body

| Flag | Description | Example |
|------|-------------|---------|
| `--json`, `-j` | Force JSON content type | `dotnet-http POST api.example.com --json` |
| `--form`, `-f` | Send as form data | `dotnet-http POST api.example.com --form` |
| `--multipart` | Send as multipart form | `dotnet-http POST api.example.com --multipart` |
| `--raw` | Send raw data | `dotnet-http POST api.example.com --raw "text data"` |

### File Operations

| Flag | Description | Example |
|------|-------------|---------|
| `--download`, `-d` | Download response to file | `dotnet-http GET api.example.com/file.pdf --download` |
| `--output`, `-o` | Save response to specific file | `dotnet-http GET api.example.com/data --output data.json` |
| `--continue`, `-C` | Resume interrupted download | `dotnet-http GET api.example.com/large.zip --download --continue` |

### Response Options

| Flag | Description | Example |
|------|-------------|---------|
| `--body`, `-b` | Show only response body | `dotnet-http GET api.example.com --body` |
| `--headers`, `-h` | Show only response headers | `dotnet-http GET api.example.com --headers` |
| `--meta`, `-m` | Show response metadata | `dotnet-http GET api.example.com --meta` |

### Network Options

| Flag | Description | Example |
|------|-------------|---------|
| `--timeout` | Request timeout in seconds | `dotnet-http GET api.example.com --timeout 30` |
| `--proxy` | Proxy server URL | `dotnet-http GET api.example.com --proxy http://proxy:8080` |
| `--verify` | SSL certificate verification | `dotnet-http GET https://api.example.com --verify=no` |
| `--cert` | Client certificate file | `dotnet-http GET api.example.com --cert client.pem` |
| `--cert-key` | Client certificate key | `dotnet-http GET api.example.com --cert-key client.key` |

### Redirect Handling

| Flag | Description | Example |
|------|-------------|---------|
| `--follow`, `-F` | Follow redirects | `dotnet-http GET api.example.com/redirect --follow` |
| `--max-redirects` | Maximum number of redirects | `dotnet-http GET api.example.com --follow --max-redirects 5` |

## Execute Command Options

The `exec` command has its own set of options:

```bash
dotnet-http exec [options] [file-path]
```

### Execute Flags

| Flag | Description | Example |
|------|-------------|---------|
| `--env` | Environment to use | `dotnet-http exec requests.http --env production` |
| `--type`, `-t` | Script type (http/curl) | `dotnet-http exec script.curl --type curl` |
| `--debug` | Debug mode for execution | `dotnet-http exec requests.http --debug` |
| `--offline` | Preview execution without sending | `dotnet-http exec requests.http --offline` |

### Supported Script Types

| Type | Description | File Extensions |
|------|-------------|-----------------|
| `http` | HTTP request files | `.http`, `.rest` |
| `curl` | cURL command files | `.curl`, `.sh` |

## Request Item Types

### Query Parameters

```bash
# Syntax: name==value
dotnet-http GET api.example.com/search query==httpie limit==10
```

### Headers

```bash
# Syntax: name:value
dotnet-http GET api.example.com/data Authorization:"Bearer token"
```

### JSON Fields

```bash
# Syntax: name=value (creates JSON)
dotnet-http POST api.example.com/users name=John email=john@example.com
```

### Raw JSON Values

```bash
# Syntax: name:=value (typed JSON)
dotnet-http POST api.example.com/users age:=30 active:=true
```

### File Uploads

```bash
# Syntax: name@/path/to/file
dotnet-http POST api.example.com/upload file@document.pdf
```

## Environment Variables

dotnet-httpie respects several environment variables:

| Variable | Description | Example |
|----------|-------------|---------|
| `HTTP_PROXY` | HTTP proxy server | `export HTTP_PROXY=http://proxy:8080` |
| `HTTPS_PROXY` | HTTPS proxy server | `export HTTPS_PROXY=https://proxy:8443` |
| `NO_PROXY` | Hosts to bypass proxy | `export NO_PROXY=localhost,127.0.0.1` |
| `DOTNET_HTTP_TIMEOUT` | Default timeout | `export DOTNET_HTTP_TIMEOUT=60` |

## Configuration Files

### HTTP Client Environment Files

```json
{
  "development": {
    "baseUrl": "http://localhost:3000",
    "apiKey": "dev-key"
  },
  "production": {
    "baseUrl": "https://api.example.com",
    "apiKey": "prod-key"
  }
}
```

File locations:
- `http-client.env.json` (same directory)
- `~/.httpie/env.json` (global)
- Custom path via `--env-file`

## Output Formats

### Response Output Control

```bash
# Default: headers + body
dotnet-http GET api.example.com/users

# Headers only
dotnet-http HEAD api.example.com/users

# Body only
dotnet-http GET api.example.com/users --body

# Specific components with --print
dotnet-http GET api.example.com/users --print=HhBb
```

### Print Option Values

| Code | Component |
|------|-----------|
| `H` | Request headers |
| `B` | Request body |
| `h` | Response headers |
| `b` | Response body |
| `m` | Response metadata |

## Exit Codes

| Code | Meaning |
|------|---------|
| `0` | Success |
| `1` | Generic error |
| `2` | Request timeout |
| `3` | Too many redirects |
| `4` | HTTP 4xx error |
| `5` | HTTP 5xx error |
| `6` | Network error |

## Examples by Category

### Basic Requests

```bash
# Simple GET
dotnet-http GET httpbin.org/get

# POST with data
dotnet-http POST httpbin.org/post name=John age:=30

# Custom headers
dotnet-http GET httpbin.org/headers User-Agent:"MyApp/1.0"
```

### Authentication Examples

```bash
# Bearer token
dotnet-http GET api.example.com/protected Authorization:"Bearer token"

# Basic auth
dotnet-http GET api.example.com/secure --auth user:pass

# API key
dotnet-http GET api.example.com/data X-API-Key:"key123"
```

### File Operations

```bash
# Upload file
dotnet-http POST api.example.com/upload --multipart file@document.pdf

# Download file
dotnet-http GET api.example.com/report.pdf --download

# Send file as body
dotnet-http POST api.example.com/data @data.json
```

### Form Data

```bash
# URL-encoded form
dotnet-http POST httpbin.org/post --form name=John email=john@example.com

# Multipart form
dotnet-http POST httpbin.org/post --multipart name=John file@avatar.jpg
```

### Response Handling

```bash
# Save response
dotnet-http GET api.example.com/data --output response.json

# Body only to stdout
dotnet-http GET api.example.com/data --body

# Headers only
dotnet-http HEAD api.example.com/data
```

### Network Options

```bash
# With proxy
dotnet-http GET api.example.com/data --proxy http://proxy:8080

# Custom timeout
dotnet-http GET api.example.com/slow --timeout 60

# Skip SSL verification
dotnet-http GET https://self-signed.local --verify=no
```

### Debug and Development

```bash
# Debug mode
dotnet-http GET api.example.com/data --debug

# Preview request
dotnet-http POST api.example.com/users name=John --offline

# Verbose output
dotnet-http GET api.example.com/data --verbose
```

## Advanced Usage Patterns

### Environment-Specific Requests

```bash
# Development
dotnet-http GET localhost:3000/api/users --debug

# Staging
dotnet-http GET staging-api.example.com/users \
  Authorization:"Bearer $STAGING_TOKEN"

# Production
dotnet-http GET api.example.com/users \
  Authorization:"Bearer $PROD_TOKEN" \
  --timeout 30
```

### Conditional Requests

```bash
# Check status code
if dotnet-http GET api.example.com/health --check-status; then
  echo "API is healthy"
fi

# With error handling
dotnet-http GET api.example.com/data || echo "Request failed"
```

### Scripting Integration

```bash
# Extract data for further processing
USER_ID=$(dotnet-http POST api.example.com/users name=John --body | jq -r '.id')
dotnet-http GET "api.example.com/users/$USER_ID/profile"

# Batch operations
for user in alice bob charlie; do
  dotnet-http POST api.example.com/users name="$user"
done
```

## Migration from Other Tools

### From cURL

```bash
# cURL
curl -X POST https://api.example.com/users \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer token" \
  -d '{"name": "John", "age": 30}'

# dotnet-httpie equivalent
dotnet-http POST api.example.com/users \
  Authorization:"Bearer token" \
  name=John \
  age:=30
```

### From HTTPie

```bash
# HTTPie
http POST api.example.com/users Authorization:"Bearer token" name=John age:=30

# dotnet-httpie (very similar)
dotnet-http POST api.example.com/users Authorization:"Bearer token" name=John age:=30
```

## Platform-Specific Notes

### Windows

```powershell
# PowerShell escaping
dotnet-http POST api.example.com/users name=John tags:='[\"web\", \"api\"]'

# Command Prompt escaping
dotnet-http POST api.example.com/users name=John tags:="[\"web\", \"api\"]"
```

### macOS/Linux

```bash
# Bash/Zsh (standard escaping)
dotnet-http POST api.example.com/users name=John tags:='["web", "api"]'

# Fish shell
dotnet-http POST api.example.com/users name=John tags:=\'["web", "api"]\'
```

## Performance Tips

1. **Use `--body` for piping**: Avoids header processing overhead
2. **Reuse connections**: Use session management where available
3. **Minimize debug output**: Only use `--debug` when troubleshooting
4. **Optimize JSON**: Use `:=` for numbers/booleans instead of strings
5. **Batch requests**: Use file execution for multiple related requests

## Next Steps

- Review [common use cases](../examples/common-use-cases.md) for practical applications
- Learn about [file execution](../file-execution.md) for advanced workflows
- Check [debugging guide](../debugging.md) for troubleshooting
- Explore [Docker usage](../docker-usage.md) for containerized environments