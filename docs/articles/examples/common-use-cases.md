# Common Use Cases

This guide provides practical examples for the most common scenarios when using dotnet-httpie.

## API Development & Testing

### REST API CRUD Operations

```bash
# Users API example
BASE_URL="https://api.example.com"
TOKEN="your-jwt-token"

# Create user
dotnet-http POST $BASE_URL/users \
  Authorization:"Bearer $TOKEN" \
  Content-Type:"application/json" \
  name="John Doe" \
  email="john@example.com" \
  role="user"

# Get all users
dotnet-http GET $BASE_URL/users \
  Authorization:"Bearer $TOKEN"

# Get specific user
dotnet-http GET $BASE_URL/users/123 \
  Authorization:"Bearer $TOKEN"

# Update user
dotnet-http PUT $BASE_URL/users/123 \
  Authorization:"Bearer $TOKEN" \
  name="John Smith" \
  email="john.smith@example.com"

# Partial update
dotnet-http PATCH $BASE_URL/users/123 \
  Authorization:"Bearer $TOKEN" \
  email="newemail@example.com"

# Delete user
dotnet-http DELETE $BASE_URL/users/123 \
  Authorization:"Bearer $TOKEN"
```

### GraphQL API

```bash
# GraphQL query
dotnet-http POST https://api.github.com/graphql \
  Authorization:"Bearer $GITHUB_TOKEN" \
  query='query { viewer { login name } }'

# GraphQL mutation
dotnet-http POST https://api.github.com/graphql \
  Authorization:"Bearer $GITHUB_TOKEN" \
  query='mutation { createIssue(input: {repositoryId: "repo-id", title: "Bug report", body: "Description"}) { issue { id title } } }'
```

## Authentication Patterns

### JWT Authentication

```bash
# Login and get token
LOGIN_RESPONSE=$(dotnet-http POST api.example.com/auth/login \
  username="admin" \
  password="password" \
  --body)

TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.token')

# Use token for protected requests
dotnet-http GET api.example.com/protected \
  Authorization:"Bearer $TOKEN"
```

### API Key Authentication

```bash
# Header-based API key
dotnet-http GET api.example.com/data \
  X-API-Key:"your-api-key"

# Query parameter API key
dotnet-http GET api.example.com/data \
  api_key==your-api-key
```

### Basic Authentication

```bash
# Basic auth
dotnet-http GET api.example.com/secure \
  Authorization:"Basic $(echo -n 'username:password' | base64)"

# Or with HTTPie-style auth
dotnet-http GET api.example.com/secure \
  --auth username:password
```

### OAuth 2.0

```bash
# Get access token
TOKEN_RESPONSE=$(dotnet-http POST oauth.example.com/token \
  grant_type="client_credentials" \
  client_id="your-client-id" \
  client_secret="your-client-secret" \
  scope="read write" \
  --body)

ACCESS_TOKEN=$(echo $TOKEN_RESPONSE | jq -r '.access_token')

# Use access token
dotnet-http GET api.example.com/protected \
  Authorization:"Bearer $ACCESS_TOKEN"
```

## File Operations

### File Uploads

```bash
# Single file upload
dotnet-http POST api.example.com/upload \
  Authorization:"Bearer $TOKEN" \
  --multipart \
  file@/path/to/document.pdf \
  description="Important document"

# Multiple file upload
dotnet-http POST api.example.com/batch-upload \
  --multipart \
  doc1@/path/to/file1.pdf \
  doc2@/path/to/file2.pdf \
  metadata@/path/to/metadata.json

# Image upload with metadata
dotnet-http POST api.example.com/images \
  --multipart \
  image@/path/to/photo.jpg \
  title="Vacation Photo" \
  tags:='["vacation", "travel", "beach"]' \
  public:=true
```

### File Downloads

```bash
# Download file
dotnet-http GET api.example.com/files/document.pdf \
  Authorization:"Bearer $TOKEN" \
  --download

# Download with custom filename
dotnet-http GET api.example.com/exports/data.csv \
  --download \
  --output "$(date +%Y%m%d)-export.csv"

# Download large files with progress
dotnet-http GET api.example.com/large-file.zip \
  --download \
  --progress
```

## Data Processing

### JSON Processing with jq

```bash
# Extract specific fields
USER_ID=$(dotnet-http POST api.example.com/users name="John" --body | jq -r '.id')

# Filter arrays
dotnet-http GET api.example.com/users | jq '.users[] | select(.active == true)'

# Transform data
dotnet-http GET api.example.com/users | jq '.users | map({id, name, email})'

# Count results
COUNT=$(dotnet-http GET api.example.com/users | jq '.users | length')
echo "Total users: $COUNT"
```

### Pagination

```bash
# Fetch all pages
page=1
all_data="[]"

while true; do
  response=$(dotnet-http GET api.example.com/users page==$page limit==50 --body)
  data=$(echo $response | jq '.data')
  
  if [ "$(echo $data | jq 'length')" -eq 0 ]; then
    break
  fi
  
  all_data=$(echo $all_data $data | jq -s 'add')
  ((page++))
done

echo $all_data | jq .
```

## CI/CD Integration

### Health Checks

```bash
#!/bin/bash
# health-check.sh

check_service() {
  local service_url=$1
  local service_name=$2
  
  echo "Checking $service_name..."
  
  if dotnet-http GET $service_url/health --check-status; then
    echo "✓ $service_name is healthy"
    return 0
  else
    echo "✗ $service_name is unhealthy"
    return 1
  fi
}

# Check multiple services
check_service "https://api.example.com" "API Service"
check_service "https://auth.example.com" "Auth Service"
check_service "https://cache.example.com" "Cache Service"
```

### Deployment Verification

```bash
#!/bin/bash
# verify-deployment.sh

ENVIRONMENT=${1:-staging}
BASE_URL="https://$ENVIRONMENT.api.example.com"

echo "Verifying deployment in $ENVIRONMENT..."

# Check API version
VERSION=$(dotnet-http GET $BASE_URL/version --body | jq -r '.version')
echo "API Version: $VERSION"

# Run smoke tests
dotnet-http exec tests/smoke-tests.http --env $ENVIRONMENT

# Check critical endpoints
dotnet-http GET $BASE_URL/health
dotnet-http GET $BASE_URL/metrics
dotnet-http GET $BASE_URL/ready

echo "Deployment verification complete!"
```

### Load Testing

```bash
#!/bin/bash
# load-test.sh

URL="https://api.example.com/endpoint"
CONCURRENT=10
REQUESTS=100

echo "Running load test: $REQUESTS requests with $CONCURRENT concurrent users"

# Create temporary file for results
RESULTS_FILE=$(mktemp)

# Run concurrent requests
for i in $(seq 1 $CONCURRENT); do
  (
    for j in $(seq 1 $((REQUESTS / CONCURRENT))); do
      start_time=$(date +%s%N)
      
      if dotnet-http GET $URL > /dev/null 2>&1; then
        end_time=$(date +%s%N)
        duration=$(((end_time - start_time) / 1000000))
        echo "SUCCESS,$duration" >> $RESULTS_FILE
      else
        echo "FAILURE,0" >> $RESULTS_FILE
      fi
    done
  ) &
done

wait

# Analyze results
total=$(wc -l < $RESULTS_FILE)
success=$(grep "SUCCESS" $RESULTS_FILE | wc -l)
failures=$((total - success))
avg_time=$(grep "SUCCESS" $RESULTS_FILE | cut -d, -f2 | awk '{sum+=$1} END {print sum/NR}')

echo "Results:"
echo "  Total requests: $total"
echo "  Successful: $success"
echo "  Failed: $failures"
echo "  Success rate: $(( success * 100 / total ))%"
echo "  Average response time: ${avg_time}ms"

rm $RESULTS_FILE
```

## API Testing Workflows

### End-to-End Testing

```http
# tests/e2e-workflow.http
@baseUrl = https://api.example.com
@contentType = application/json

###

# @name login
POST {{baseUrl}}/auth/login
Content-Type: {{contentType}}

{
  "username": "testuser",
  "password": "testpass"
}

###

# @name createUser
POST {{baseUrl}}/users
Authorization: Bearer {{login.response.body.token}}
Content-Type: {{contentType}}

{
  "name": "Test User",
  "email": "test@example.com",
  "role": "user"
}

###

# @name getUser
GET {{baseUrl}}/users/{{createUser.response.body.id}}
Authorization: Bearer {{login.response.body.token}}

###

# @name updateUser
PUT {{baseUrl}}/users/{{createUser.response.body.id}}
Authorization: Bearer {{login.response.body.token}}
Content-Type: {{contentType}}

{
  "name": "Updated Test User",
  "email": "updated@example.com"
}

###

# @name deleteUser
DELETE {{baseUrl}}/users/{{createUser.response.body.id}}
Authorization: Bearer {{login.response.body.token}}
```

### Contract Testing

```bash
#!/bin/bash
# contract-test.sh

echo "Running API contract tests..."

# Test required fields
response=$(dotnet-http POST api.example.com/users name="Test" email="test@example.com" --body)

# Validate response structure
echo $response | jq -e '.id' > /dev/null || { echo "Missing id field"; exit 1; }
echo $response | jq -e '.name' > /dev/null || { echo "Missing name field"; exit 1; }
echo $response | jq -e '.email' > /dev/null || { echo "Missing email field"; exit 1; }
echo $response | jq -e '.created_at' > /dev/null || { echo "Missing created_at field"; exit 1; }

# Validate data types
[ "$(echo $response | jq -r '.id | type')" = "string" ] || { echo "ID should be string"; exit 1; }
[ "$(echo $response | jq -r '.name | type')" = "string" ] || { echo "Name should be string"; exit 1; }

echo "✓ All contract tests passed"
```

## Microservices Testing

### Service Discovery

```bash
#!/bin/bash
# test-microservices.sh

SERVICES=("user-service" "order-service" "payment-service" "notification-service")
BASE_URL="https://api.example.com"

for service in "${SERVICES[@]}"; do
  echo "Testing $service..."
  
  # Health check
  dotnet-http GET $BASE_URL/$service/health
  
  # Version check
  VERSION=$(dotnet-http GET $BASE_URL/$service/version --body | jq -r '.version')
  echo "$service version: $VERSION"
  
  # Basic functionality test
  case $service in
    "user-service")
      dotnet-http GET $BASE_URL/users/1
      ;;
    "order-service")
      dotnet-http GET $BASE_URL/orders limit==5
      ;;
    "payment-service")
      dotnet-http GET $BASE_URL/payments/methods
      ;;
    "notification-service")
      dotnet-http GET $BASE_URL/notifications/templates
      ;;
  esac
  
  echo "✓ $service test completed"
  echo
done
```

### Cross-Service Integration

```http
# tests/cross-service.http
@baseUrl = https://api.example.com

###

# @name createUser
POST {{baseUrl}}/users
Content-Type: application/json

{
  "name": "Integration Test User",
  "email": "integration@example.com"
}

###

# @name createOrder
POST {{baseUrl}}/orders
Content-Type: application/json

{
  "user_id": "{{createUser.response.body.id}}",
  "items": [
    {"product_id": "prod-123", "quantity": 2},
    {"product_id": "prod-456", "quantity": 1}
  ]
}

###

# @name processPayment
POST {{baseUrl}}/payments
Content-Type: application/json

{
  "order_id": "{{createOrder.response.body.id}}",
  "amount": "{{createOrder.response.body.total}}",
  "method": "credit_card",
  "card_token": "test-token-123"
}

###

# @name sendNotification
POST {{baseUrl}}/notifications
Content-Type: application/json

{
  "user_id": "{{createUser.response.body.id}}",
  "type": "order_confirmation",
  "data": {
    "order_id": "{{createOrder.response.body.id}}",
    "payment_id": "{{processPayment.response.body.id}}"
  }
}
```

## Development Workflows

### Local Development

```bash
#!/bin/bash
# dev-setup.sh

echo "Setting up local development environment..."

# Start local services
docker-compose up -d

# Wait for services to be ready
sleep 10

# Seed test data
dotnet-http POST localhost:3000/api/seed

# Run initial tests
dotnet-http exec tests/local-smoke-tests.http --env development

echo "Development environment ready!"
```

### API Documentation Testing

```bash
#!/bin/bash
# test-api-docs.sh

# Extract API endpoints from OpenAPI spec
ENDPOINTS=$(curl -s https://api.example.com/openapi.json | jq -r '.paths | keys[]')

echo "Testing API endpoints from documentation..."

for endpoint in $ENDPOINTS; do
  # Convert OpenAPI path to actual URL
  url="https://api.example.com${endpoint//\{[^}]*\}/123}"
  
  echo "Testing: $url"
  
  if dotnet-http GET "$url" > /dev/null 2>&1; then
    echo "✓ $endpoint"
  else
    echo "✗ $endpoint"
  fi
done
```

## Monitoring & Alerting

### Uptime Monitoring

```bash
#!/bin/bash
# uptime-monitor.sh

SERVICES=(
  "https://api.example.com/health"
  "https://auth.example.com/health"
  "https://cdn.example.com/status"
)

for service in "${SERVICES[@]}"; do
  if ! dotnet-http GET "$service" --check-status; then
    # Send alert
    dotnet-http POST "https://hooks.slack.com/services/YOUR/SLACK/WEBHOOK" \
      text="🚨 Service down: $service"
  fi
done
```

### Performance Monitoring

```bash
#!/bin/bash
# perf-monitor.sh

ENDPOINT="https://api.example.com/users"
THRESHOLD=1000  # milliseconds

start_time=$(date +%s%N)
dotnet-http GET "$ENDPOINT" > /dev/null
end_time=$(date +%s%N)

duration=$(((end_time - start_time) / 1000000))

if [ $duration -gt $THRESHOLD ]; then
  echo "⚠️  Slow response detected: ${duration}ms (threshold: ${THRESHOLD}ms)"
  
  # Send performance alert
  dotnet-http POST "https://hooks.slack.com/services/YOUR/SLACK/WEBHOOK" \
    text="⚠️ Slow API response: $ENDPOINT took ${duration}ms"
fi
```

## Best Practices Summary

1. **Use environment variables** for configuration and secrets
2. **Implement proper error handling** in scripts
3. **Create reusable test suites** with .http files
4. **Combine with other tools** like jq for data processing
5. **Use meaningful names** for saved requests
6. **Document your API tests** with comments
7. **Version control your test files** alongside your code
8. **Implement retry logic** for flaky endpoints
9. **Use offline mode** to preview requests
10. **Monitor and alert** on API health and performance

## Next Steps

- Explore [API testing scenarios](api-testing.md) for more advanced patterns
- Learn about [integration examples](integrations.md) with other tools
- Check out [performance tips](../performance-tips.md) for optimization
- Review [debugging guide](../debugging.md) for troubleshooting