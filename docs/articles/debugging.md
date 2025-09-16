# Debugging & Troubleshooting

This guide helps you debug issues with dotnet-httpie and troubleshoot common problems.

## Debug Mode

Enable debug mode to get detailed information about request processing:

```bash
dotnet-http GET api.example.com/data --debug
```

Debug mode provides:
- Detailed request/response logging
- Middleware execution information
- Error stack traces
- Performance timing
- Configuration details

## Offline Mode (Request Preview)

Preview requests without sending them:

```bash
# Preview a single request
dotnet-http POST api.example.com/users name=John --offline

# Preview HTTP file execution
dotnet-http exec requests.http --offline

# Preview with debug information
dotnet-http POST api.example.com/users name=John --debug --offline
```

Offline mode is useful for:
- Validating request structure
- Checking JSON formatting
- Verifying headers and parameters
- Testing variable substitution

## Common Issues

### 1. Installation Problems

#### Tool Not Found After Installation

**Problem**: `dotnet-http: command not found`

**Solutions**:
```bash
# Check if tool is installed
dotnet tool list --global

# Verify .NET tools path is in PATH
echo $PATH | grep -q "$HOME/.dotnet/tools" || echo "Tools path not in PATH"

# Add to shell profile (bash/zsh)
echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.bashrc
source ~/.bashrc

# Reinstall if corrupted
dotnet tool uninstall --global dotnet-httpie
dotnet tool install --global dotnet-httpie
```

#### Permission Denied

**Problem**: Permission issues during installation

**Solutions**:
```bash
# Check permissions on tools directory
ls -la ~/.dotnet/tools/

# Fix permissions if needed
chmod +x ~/.dotnet/tools/dotnet-http

# Install for current user only
dotnet tool install --global dotnet-httpie --tool-path ~/.local/bin
```

### 2. Request Issues

#### SSL/TLS Certificate Errors

**Problem**: SSL certificate validation failures

**Solutions**:
```bash
# Skip SSL verification (development only)
dotnet-http GET https://self-signed-site.com --verify=no

# Use custom CA certificate
dotnet-http GET https://internal-api.company.com \
  --ca-cert /path/to/ca-certificate.pem

# Check SSL certificate details
openssl s_client -connect api.example.com:443 -servername api.example.com
```

#### Connection Timeouts

**Problem**: Requests timing out

**Solutions**:
```bash
# Increase timeout (if supported)
dotnet-http GET api.example.com/slow-endpoint --timeout 60

# Check network connectivity
ping api.example.com
curl -I api.example.com

# Test with simpler request first
dotnet-http GET api.example.com/health
```

#### Authentication Failures

**Problem**: 401 Unauthorized responses

**Debug Steps**:
```bash
# Check token validity
echo $JWT_TOKEN | base64 -d

# Verify token format
dotnet-http GET api.example.com/verify \
  Authorization:"Bearer $JWT_TOKEN" \
  --debug

# Test with minimal request
dotnet-http GET api.example.com/public

# Check header format
dotnet-http GET httpbin.org/headers \
  Authorization:"Bearer $JWT_TOKEN"
```

### 3. JSON and Data Issues

#### JSON Parsing Errors

**Problem**: Invalid JSON in request body

**Debug**:
```bash
# Preview JSON structure
dotnet-http POST api.example.com/users \
  name=John \
  age:=30 \
  tags:='["dev", "api"]' \
  --offline

# Validate JSON manually
echo '{"name": "John", "age": 30}' | jq .

# Use file for complex JSON
cat > user.json << EOF
{
  "name": "John",
  "age": 30,
  "skills": ["C#", "JavaScript"]
}
EOF

dotnet-http POST api.example.com/users @user.json
```

#### Character Encoding Issues

**Problem**: Special characters not handled correctly

**Solutions**:
```bash
# Specify charset
dotnet-http POST api.example.com/data \
  Content-Type:"application/json; charset=utf-8" \
  message="Hello 世界"

# Use file with proper encoding
echo '{"message": "Hello 世界"}' > data.json
dotnet-http POST api.example.com/data @data.json
```

### 4. File Execution Issues

#### File Not Found

**Problem**: HTTP files not loading

**Debug**:
```bash
# Check file exists and permissions
ls -la requests.http

# Use absolute path
dotnet-http exec /full/path/to/requests.http

# Check current directory
pwd
dotnet-http exec ./requests.http
```

#### Variable Substitution Failures

**Problem**: Variables not being replaced

**Debug**:
```bash
# Check environment file
cat http-client.env.json

# Verify environment selection
dotnet-http exec requests.http --env development --debug

# Test variable syntax
# Good: {{baseUrl}}
# Bad: {baseUrl} or $baseUrl
```

#### Request Reference Errors

**Problem**: Cannot reference previous responses

**Debug**:
```bash
# Ensure request names are defined
# @name createUser
POST {{baseUrl}}/users

# Reference with correct syntax
GET {{baseUrl}}/users/{{createUser.response.body.id}}

# Check response structure
dotnet-http exec requests.http --debug --offline
```

## Debugging Tools and Techniques

### 1. Network Analysis

#### Using tcpdump/Wireshark

```bash
# Capture network traffic (Linux/macOS)
sudo tcpdump -i any -w capture.pcap host api.example.com

# Run your request
dotnet-http GET api.example.com/data

# Analyze capture with Wireshark or tcpdump
tcpdump -r capture.pcap -A
```

#### Using curl for comparison

```bash
# Compare with curl behavior
curl -v https://api.example.com/data \
  -H "Authorization: Bearer $TOKEN"

# Convert to dotnet-httpie equivalent
dotnet-http GET api.example.com/data \
  Authorization:"Bearer $TOKEN" \
  --debug
```

### 2. Response Analysis

#### JSON Processing

```bash
# Pretty print JSON response
dotnet-http GET api.example.com/users | jq .

# Extract specific fields
dotnet-http GET api.example.com/users | jq '.users[0].id'

# Validate JSON schema
dotnet-http GET api.example.com/users | jq 'type'
```

#### Header Analysis

```bash
# Show response headers only
dotnet-http HEAD api.example.com/data

# Check specific headers
dotnet-http GET httpbin.org/headers | jq '.headers'

# Trace header propagation
dotnet-http GET api.example.com/data \
  X-Trace-ID:"$(uuidgen)" \
  --debug
```

### 3. Performance Debugging

#### Response Time Analysis

```bash
# Measure response time
time dotnet-http GET api.example.com/data

# Multiple requests for average
for i in {1..10}; do
  time dotnet-http GET api.example.com/data >/dev/null
done
```

#### Memory and Resource Usage

```bash
# Monitor resource usage
top -p $(pgrep dotnet-http)

# Memory usage
ps aux | grep dotnet-http
```

## Error Analysis

### HTTP Status Codes

#### 4xx Client Errors

```bash
# 400 Bad Request
dotnet-http POST api.example.com/users \
  invalid-json \
  --debug  # Check request format

# 401 Unauthorized
dotnet-http GET api.example.com/protected \
  --debug  # Check authentication

# 403 Forbidden
dotnet-http GET api.example.com/admin \
  Authorization:"Bearer $USER_TOKEN" \
  --debug  # Check permissions

# 404 Not Found
dotnet-http GET api.example.com/nonexistent \
  --debug  # Check URL

# 429 Too Many Requests
dotnet-http GET api.example.com/data \
  --debug  # Check rate limiting
```

#### 5xx Server Errors

```bash
# 500 Internal Server Error
dotnet-http POST api.example.com/users \
  name=John \
  --debug  # Check server logs

# 502 Bad Gateway
dotnet-http GET api.example.com/data \
  --debug  # Check proxy/load balancer

# 503 Service Unavailable
dotnet-http GET api.example.com/health \
  --debug  # Check service status
```

### Error Response Analysis

```bash
# Capture error details
ERROR_RESPONSE=$(dotnet-http GET api.example.com/error --body 2>&1)
echo "$ERROR_RESPONSE" | jq .

# Check error structure
dotnet-http GET api.example.com/error | jq '{error, message, code}'
```

## Logging and Monitoring

### Request Logging

```bash
# Log all requests to file
dotnet-http GET api.example.com/data --debug > request.log 2>&1

# Structured logging
dotnet-http GET api.example.com/data 2>&1 | \
  grep -E "(Request|Response|Error)" > structured.log
```

### Automated Health Checks

```bash
#!/bin/bash
# health-monitor.sh

LOG_FILE="/var/log/api-health.log"

check_endpoint() {
  local endpoint=$1
  local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
  
  if dotnet-http GET "$endpoint" --check-status >/dev/null 2>&1; then
    echo "[$timestamp] OK: $endpoint" >> "$LOG_FILE"
    return 0
  else
    echo "[$timestamp] FAIL: $endpoint" >> "$LOG_FILE"
    return 1
  fi
}

# Monitor multiple endpoints
check_endpoint "https://api.example.com/health"
check_endpoint "https://api.example.com/status"
check_endpoint "https://auth.example.com/health"
```

## Docker Debugging

### Container Issues

```bash
# Run with debug output
docker run --rm weihanli/dotnet-httpie:latest \
  GET api.example.com/data --debug

# Check container logs
docker run --name httpie-debug weihanli/dotnet-httpie:latest \
  GET api.example.com/data
docker logs httpie-debug
docker rm httpie-debug

# Interactive debugging
docker run -it --rm weihanli/dotnet-httpie:latest /bin/sh
```

### Network Issues in Docker

```bash
# Test network connectivity
docker run --rm weihanli/dotnet-httpie:latest \
  GET httpbin.org/get

# Use host network
docker run --rm --network host \
  weihanli/dotnet-httpie:latest GET localhost:3000/api

# Check DNS resolution
docker run --rm weihanli/dotnet-httpie:latest \
  nslookup api.example.com
```

## Environment Debugging

### Environment Variables

```bash
# Check all environment variables
env | grep -i http
env | grep -i api

# Verify specific variables
echo "API_TOKEN: $API_TOKEN"
echo "BASE_URL: $BASE_URL"

# Debug variable expansion
dotnet-http GET "$BASE_URL/data" \
  Authorization:"Bearer $API_TOKEN" \
  --debug
```

### Configuration Files

```bash
# Check HTTP client environment
cat http-client.env.json | jq .

# Validate JSON syntax
jq empty http-client.env.json && echo "Valid JSON" || echo "Invalid JSON"

# Check file permissions
ls -la http-client.env.json
```

## Performance Troubleshooting

### Slow Requests

```bash
# Add timing information
time dotnet-http GET api.example.com/slow-endpoint

# Profile with curl for comparison
time curl -s https://api.example.com/slow-endpoint >/dev/null

# Check DNS resolution time
time nslookup api.example.com

# Test with different endpoints
time dotnet-http GET httpbin.org/delay/5
```

### Memory Issues

```bash
# Monitor memory usage during large requests
dotnet-http GET api.example.com/large-dataset &
PID=$!
while kill -0 $PID 2>/dev/null; do
  ps -p $PID -o pid,vsz,rss,comm
  sleep 1
done
```

## Advanced Debugging

### Custom Middleware Debugging

If you're developing custom middleware:

```bash
# Enable detailed middleware logging
dotnet-http GET api.example.com/data \
  --debug \
  -v  # Verbose mode if available
```

### Source Code Debugging

```bash
# Clone repository for local debugging
git clone https://github.com/WeihanLi/dotnet-httpie.git
cd dotnet-httpie

# Build in debug mode
dotnet build -c Debug

# Run with debugger
dotnet run --project src/HTTPie -- GET api.example.com/data --debug
```

## Getting Help

### Information to Include in Bug Reports

When reporting issues, include:

1. **Version information**:
   ```bash
   dotnet-http --version
   dotnet --version
   ```

2. **Command that failed**:
   ```bash
   dotnet-http GET api.example.com/data --debug
   ```

3. **Expected vs actual behavior**

4. **Error messages** (full output with `--debug`)

5. **Environment details**:
   - Operating system
   - .NET version
   - Docker version (if using Docker)

### Community Resources

- [GitHub Issues](https://github.com/WeihanLi/dotnet-httpie/issues)
- [GitHub Discussions](https://github.com/WeihanLi/dotnet-httpie/discussions)
- [Stack Overflow](https://stackoverflow.com/questions/tagged/dotnet-httpie)

### Self-Help Checklist

Before asking for help:

- [ ] Tried with `--debug` flag
- [ ] Tested with `--offline` to check request structure
- [ ] Verified authentication tokens/keys
- [ ] Checked network connectivity
- [ ] Tested with a simple request first
- [ ] Reviewed relevant documentation
- [ ] Searched existing issues

## Next Steps

- Review [performance tips](performance-tips.md) for optimization
- Check [common use cases](examples/common-use-cases.md) for working examples
- Explore [advanced features](middleware-system.md) for complex scenarios