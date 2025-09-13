# Docker Usage

dotnet-httpie is available as a Docker image, making it easy to use in containerized environments, CI/CD pipelines, and systems without .NET installed.

## Docker Image

The official Docker image is available at: `weihanli/dotnet-httpie`

### Available Tags

- `latest` - Latest stable release
- `preview` - Latest preview/pre-release version
- `0.12.0` - Specific version tags

## Quick Start

### Pull the Image

```bash
docker pull weihanli/dotnet-httpie:latest
```

### Basic Usage

```bash
# Simple GET request
docker run --rm weihanli/dotnet-httpie:latest httpbin.org/get

# POST with data
docker run --rm weihanli/dotnet-httpie:latest POST httpbin.org/post name=John age:=30

# With headers
docker run --rm weihanli/dotnet-httpie:latest GET httpbin.org/headers Authorization:"Bearer token"
```

## Common Usage Patterns

### Interactive Usage

```bash
# Create an alias for easier usage
alias http='docker run --rm -i weihanli/dotnet-httpie:latest'

# Now use it like the installed version
http GET httpbin.org/get
http POST httpbin.org/post name=John
```

### With Local Files

Mount local directories to access files:

```bash
# Mount current directory
docker run --rm -v $(pwd):/workspace -w /workspace \
  weihanli/dotnet-httpie:latest exec requests.http

# Mount specific directory
docker run --rm -v /path/to/files:/files \
  weihanli/dotnet-httpie:latest exec /files/api-tests.http
```

### Environment Variables

Pass environment variables to the container:

```bash
# Single environment variable
docker run --rm -e API_TOKEN="your-token" \
  weihanli/dotnet-httpie:latest GET api.example.com/protected \
  Authorization:"Bearer $API_TOKEN"

# Multiple environment variables
docker run --rm \
  -e API_BASE_URL="https://api.example.com" \
  -e API_TOKEN="your-token" \
  weihanli/dotnet-httpie:latest GET "$API_BASE_URL/users" \
  Authorization:"Bearer $API_TOKEN"

# Environment file
docker run --rm --env-file .env \
  weihanli/dotnet-httpie:latest GET api.example.com/data
```

## File Operations

### Executing HTTP Files

```bash
# Mount and execute HTTP file
docker run --rm -v $(pwd):/workspace -w /workspace \
  weihanli/dotnet-httpie:latest exec tests/api.http

# With environment
docker run --rm -v $(pwd):/workspace -w /workspace \
  weihanli/dotnet-httpie:latest exec tests/api.http --env production
```

### File Downloads

```bash
# Download to mounted volume
docker run --rm -v $(pwd)/downloads:/downloads \
  weihanli/dotnet-httpie:latest GET httpbin.org/image/png \
  --download --output /downloads/image.png
```

### Upload Files

```bash
# Upload file from mounted volume
docker run --rm -v $(pwd):/workspace -w /workspace \
  weihanli/dotnet-httpie:latest POST api.example.com/upload \
  @data.json
```

## Networking

### Host Network

Access services running on the host:

```bash
# Access localhost services (Linux)
docker run --rm --network host \
  weihanli/dotnet-httpie:latest GET localhost:3000/api/health

# Access host services (macOS/Windows)
docker run --rm \
  weihanli/dotnet-httpie:latest GET host.docker.internal:3000/api/health
```

### Custom Networks

```bash
# Create network
docker network create api-test-network

# Run API server in network
docker run -d --name api-server --network api-test-network my-api-image

# Test API using dotnet-httpie in same network
docker run --rm --network api-test-network \
  weihanli/dotnet-httpie:latest GET api-server:3000/health
```

## CI/CD Integration

### GitHub Actions

```yaml
name: API Tests
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Test API Health
        run: |
          docker run --rm --network host \
            weihanli/dotnet-httpie:latest GET localhost:3000/health
      
      - name: Run API Test Suite
        run: |
          docker run --rm -v ${{ github.workspace }}:/workspace -w /workspace \
            weihanli/dotnet-httpie:latest exec tests/api-suite.http --env testing
      
      - name: Test with Authentication
        env:
          API_TOKEN: ${{ secrets.API_TOKEN }}
        run: |
          docker run --rm -e API_TOKEN \
            weihanli/dotnet-httpie:latest GET api.example.com/protected \
            Authorization:"Bearer $API_TOKEN"
```

### Azure DevOps

```yaml
stages:
- stage: ApiTests
  jobs:
  - job: RunTests
    pool:
      vmImage: 'ubuntu-latest'
    steps:
    - script: |
        docker run --rm -v $(System.DefaultWorkingDirectory):/workspace -w /workspace \
          weihanli/dotnet-httpie:latest exec tests/integration.http --env $(Environment)
      displayName: 'Run Integration Tests'
      env:
        API_TOKEN: $(ApiToken)
```

### GitLab CI

```yaml
test-api:
  image: docker:latest
  services:
    - docker:dind
  script:
    - docker run --rm -v $PWD:/workspace -w /workspace 
        weihanli/dotnet-httpie:latest exec tests/api.http --env $CI_ENVIRONMENT_NAME
  variables:
    API_TOKEN: $API_TOKEN
```

## Docker Compose Integration

### Basic Setup

```yaml
# docker-compose.yml

services:
  api:
    image: my-api:latest
    ports:
      - "3000:3000"
    environment:
      - NODE_ENV=development
  
  api-tests:
    image: weihanli/dotnet-httpie:latest
    depends_on:
      - api
    volumes:
      - ./tests:/tests
    command: exec /tests/api-suite.http
    environment:
      - API_BASE_URL=http://api:3000
```

### Health Checks

```yaml
version: '3.8'

services:
  api:
    image: my-api:latest
    healthcheck:
      test: ["CMD", "docker", "run", "--rm", "--network", "container:my-api", 
             "weihanli/dotnet-httpie:latest", "GET", "localhost:3000/health"]
      interval: 30s
      timeout: 10s
      retries: 3
```

## Advanced Scenarios

### Multi-Stage Testing

```bash
# Test development environment
docker run --rm -v $(pwd):/workspace -w /workspace \
  weihanli/dotnet-httpie:latest exec tests/smoke.http --env development

# Test staging environment  
docker run --rm -v $(pwd):/workspace -w /workspace \
  -e API_BASE_URL="https://staging.api.example.com" \
  weihanli/dotnet-httpie:latest exec tests/full-suite.http --env staging

# Test production environment
docker run --rm -v $(pwd):/workspace -w /workspace \
  -e API_BASE_URL="https://api.example.com" \
  weihanli/dotnet-httpie:latest exec tests/health-check.http --env production
```

### Proxy Testing

```bash
# Test through proxy
docker run --rm \
  -e HTTP_PROXY="http://proxy.company.com:8080" \
  -e HTTPS_PROXY="http://proxy.company.com:8080" \
  weihanli/dotnet-httpie:latest GET api.example.com/data
```

## Shell Scripts and Automation

### Bash Scripts

```bash
#!/bin/bash
# api-test.sh

set -e

API_BASE_URL="${API_BASE_URL:-http://localhost:3000}"
DOCKER_IMAGE="weihanli/dotnet-httpie:latest"

echo "Testing API at $API_BASE_URL..."

# Health check
echo "Checking API health..."
docker run --rm $DOCKER_IMAGE GET "$API_BASE_URL/health"

# Login to get token (JSON data, not Basic Auth)
echo "Testing login..."
TOKEN=$(docker run --rm $DOCKER_IMAGE POST "$API_BASE_URL/auth/login" \
  username=testuser password=testpass --body | jq -r '.token')

# Protected endpoint test with Bearer token
echo "Testing protected endpoint..."
docker run --rm $DOCKER_IMAGE GET "$API_BASE_URL/protected" \
  Authorization:"Bearer $TOKEN"

# Basic Auth test (HTTP Basic Authentication)
echo "Testing Basic Auth..."
docker run --rm $DOCKER_IMAGE GET "$API_BASE_URL/basic-protected" \
  --auth testuser:testpass

echo "All tests passed!"
```

### PowerShell Scripts

```powershell
# api-test.ps1

param(
    [string]$ApiBaseUrl = "http://localhost:3000",
    [string]$Environment = "development"
)

$dockerImage = "weihanli/dotnet-httpie:latest"

Write-Host "Testing API at $ApiBaseUrl..."

# Run test suite
docker run --rm -v "${PWD}:/workspace" -w /workspace `
  -e API_BASE_URL=$ApiBaseUrl `
  $dockerImage exec tests/api-suite.http --env $Environment

Write-Host "Tests completed!"
```

## Configuration

### Custom Configuration

```bash
# Mount custom configuration
docker run --rm -v $(pwd)/config:/config \
  -e DOTNET_HTTPIE_CONFIG="/config/httpie.json" \
  weihanli/dotnet-httpie:latest GET api.example.com/data
```

### SSL Certificates

```bash
# Mount custom CA certificates
docker run --rm -v $(pwd)/certs:/certs \
  -e SSL_CERT_DIR="/certs" \
  weihanli/dotnet-httpie:latest GET https://internal-api.company.com/data
```

## Troubleshooting

### Debug Mode

```bash
# Enable debug output
docker run --rm \
  weihanli/dotnet-httpie:latest GET httpbin.org/get --debug
```

### Offline Mode

```bash
# Preview requests without sending
docker run --rm -v $(pwd):/workspace -w /workspace \
  weihanli/dotnet-httpie:latest exec tests/api.http --offline
```

### Container Logs

```bash
# Run with verbose output
docker run --rm \
  weihanli/dotnet-httpie:latest GET httpbin.org/get --verbose

# Save logs
docker run --rm \
  weihanli/dotnet-httpie:latest GET httpbin.org/get > request.log 2>&1
```

## Performance Considerations

### Image Size

The dotnet-httpie Docker image is optimized for size using:
- Multi-stage builds
- Alpine Linux base (where applicable)
- AOT compilation for reduced runtime dependencies

### Caching

```bash
# Pre-pull image for faster execution
docker pull weihanli/dotnet-httpie:latest

# Use specific version for consistency
docker run --rm weihanli/dotnet-httpie:0.12.0 GET httpbin.org/get
```

## Best Practices

1. **Use specific image tags** in production environments
2. **Mount volumes efficiently** - only mount what you need
3. **Use environment variables** for configuration
4. **Leverage Docker networks** for service-to-service communication
5. **Clean up containers** with `--rm` flag
6. **Pre-pull images** in CI/CD for faster execution
7. **Use multi-stage testing** for different environments
8. **Secure sensitive data** using Docker secrets or external secret management

## Next Steps

- Set up [CI/CD integration](ci-cd-integration.md) with Docker
- Learn about [environment configuration](environment-variables.md)
- Explore [advanced examples](examples/integrations.md) with Docker
- Review [troubleshooting guide](debugging.md) for common issues