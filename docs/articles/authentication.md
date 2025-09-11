# Authentication

This guide covers various authentication methods supported by dotnet-httpie for securing your API requests.

## Overview

dotnet-httpie supports all common authentication methods used in modern APIs:

- Bearer Token (JWT)
- API Key Authentication
- Basic Authentication  
- Custom Header Authentication
- OAuth 2.0 flows
- Cookie-based Authentication

## Bearer Token Authentication

Most commonly used for JWT tokens in modern APIs.

### Header-based Bearer Token

```bash
# Standard Bearer token
dotnet-http GET api.example.com/protected \
  Authorization:"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

# Environment variable token
dotnet-http GET api.example.com/protected \
  Authorization:"Bearer $JWT_TOKEN"
```

### Getting JWT Tokens

```bash
# Login to get JWT token
LOGIN_RESPONSE=$(dotnet-http POST api.example.com/auth/login \
  username="admin" \
  password="password" \
  --body)

# Extract token using jq
TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.access_token')

# Use token for protected requests
dotnet-http GET api.example.com/users \
  Authorization:"Bearer $TOKEN"
```

### Refresh Token Flow

```bash
# Use refresh token to get new access token
REFRESH_RESPONSE=$(dotnet-http POST api.example.com/auth/refresh \
  refresh_token="$REFRESH_TOKEN" \
  --body)

NEW_TOKEN=$(echo $REFRESH_RESPONSE | jq -r '.access_token')

# Use new token
dotnet-http GET api.example.com/protected \
  Authorization:"Bearer $NEW_TOKEN"
```

## API Key Authentication

Common in REST APIs for service-to-service communication.

### Header-based API Keys

```bash
# Standard API key header
dotnet-http GET api.example.com/data \
  X-API-Key:"your-api-key-here"

# Custom header names
dotnet-http GET api.example.com/data \
  X-RapidAPI-Key:"your-rapidapi-key" \
  X-RapidAPI-Host:"api.example.com"

# Multiple API keys
dotnet-http GET api.example.com/data \
  X-API-Key:"primary-key" \
  X-Secondary-Key:"secondary-key"
```

### Query Parameter API Keys

```bash
# API key as query parameter
dotnet-http GET api.example.com/data \
  api_key==your-api-key

# Multiple parameters
dotnet-http GET api.example.com/data \
  key==your-api-key \
  format==json \
  version==v2
```

### Environment-based API Keys

```bash
# Store API key in environment variable
export API_KEY="your-secret-api-key"

# Use in requests
dotnet-http GET api.example.com/data \
  X-API-Key:"$API_KEY"
```

## Basic Authentication

Traditional username/password authentication.

### Manual Basic Auth

```bash
# Encode credentials manually
CREDENTIALS=$(echo -n 'username:password' | base64)
dotnet-http GET api.example.com/secure \
  Authorization:"Basic $CREDENTIALS"

# Direct encoding
dotnet-http GET api.example.com/secure \
  Authorization:"Basic $(echo -n 'admin:secret123' | base64)"
```

### HTTPie-style Basic Auth

```bash
# If supported by middleware
dotnet-http GET api.example.com/secure \
  --auth username:password
```

## OAuth 2.0 Flows

### Client Credentials Flow

```bash
# Get access token
TOKEN_RESPONSE=$(dotnet-http POST oauth.provider.com/token \
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

### Authorization Code Flow (Manual)

```bash
# Step 1: Get authorization code (manual browser step)
echo "Visit: https://oauth.provider.com/authorize?client_id=your-client-id&response_type=code&redirect_uri=http://localhost:8080/callback&scope=read"

# Step 2: Exchange code for token
TOKEN_RESPONSE=$(dotnet-http POST oauth.provider.com/token \
  grant_type="authorization_code" \
  client_id="your-client-id" \
  client_secret="your-client-secret" \
  code="authorization-code-from-callback" \
  redirect_uri="http://localhost:8080/callback" \
  --body)

ACCESS_TOKEN=$(echo $TOKEN_RESPONSE | jq -r '.access_token')
```

### Resource Owner Password Credentials

```bash
# Direct username/password exchange (less secure)
TOKEN_RESPONSE=$(dotnet-http POST oauth.provider.com/token \
  grant_type="password" \
  username="user@example.com" \
  password="user-password" \
  client_id="your-client-id" \
  client_secret="your-client-secret" \
  --body)

ACCESS_TOKEN=$(echo $TOKEN_RESPONSE | jq -r '.access_token')
```

## Custom Authentication Schemes

### Signature-based Authentication

```bash
# AWS-style signature
SIGNATURE=$(echo -n "GET\n/api/data\n$(date -u)" | openssl dgst -sha256 -hmac "$SECRET_KEY" -binary | base64)

dotnet-http GET api.example.com/data \
  Authorization:"AWS4-HMAC-SHA256 Credential=$ACCESS_KEY/$(date -u +%Y%m%d)/us-east-1/service/aws4_request, SignedHeaders=host;x-amz-date, Signature=$SIGNATURE" \
  X-Amz-Date:"$(date -u +%Y%m%dT%H%M%SZ)"
```

### HMAC Authentication

```bash
# Generate HMAC signature
TIMESTAMP=$(date +%s)
PAYLOAD="GET/api/data$TIMESTAMP"
SIGNATURE=$(echo -n "$PAYLOAD" | openssl dgst -sha256 -hmac "$SECRET_KEY" -binary | base64)

dotnet-http GET api.example.com/data \
  X-API-Key:"$API_KEY" \
  X-Timestamp:"$TIMESTAMP" \
  X-Signature:"$SIGNATURE"
```

### Digest Authentication

```bash
# Simple digest implementation (for demonstration)
NONCE=$(openssl rand -hex 16)
HASH=$(echo -n "username:realm:password" | md5sum | cut -d' ' -f1)
RESPONSE=$(echo -n "$HASH:$NONCE:GET:/api/data" | md5sum | cut -d' ' -f1)

dotnet-http GET api.example.com/data \
  Authorization:"Digest username=\"username\", realm=\"realm\", nonce=\"$NONCE\", uri=\"/api/data\", response=\"$RESPONSE\""
```

## Cookie-based Authentication

### Session Cookies

```bash
# Login and save cookies
dotnet-http POST api.example.com/login \
  username="admin" \
  password="password" \
  --session=api-session

# Use saved session for subsequent requests
dotnet-http GET api.example.com/protected \
  --session=api-session
```

### Manual Cookie Handling

```bash
# Set cookies manually
dotnet-http GET api.example.com/protected \
  Cookie:"sessionid=abc123; csrftoken=xyz789"

# Multiple cookies
dotnet-http GET api.example.com/protected \
  Cookie:"auth_token=token123; user_pref=dark_mode; lang=en"
```

## Multi-factor Authentication

### TOTP (Time-based One-time Password)

```bash
# Generate TOTP code (using external tool)
TOTP_CODE=$(oathtool --totp --base32 "$TOTP_SECRET")

# Include in request
dotnet-http POST api.example.com/sensitive-action \
  Authorization:"Bearer $TOKEN" \
  X-TOTP-Code:"$TOTP_CODE" \
  action="transfer" \
  amount:=1000
```

### SMS/Email Verification

```bash
# Request verification code
dotnet-http POST api.example.com/request-verification \
  Authorization:"Bearer $TOKEN" \
  phone="+1-555-0123"

# Submit verification code
dotnet-http POST api.example.com/verify \
  Authorization:"Bearer $TOKEN" \
  verification_code="123456" \
  action="sensitive-operation"
```

## Authentication in HTTP Files

### Environment Variables

```http
# auth-example.http
@baseUrl = https://api.example.com
@token = {{$env JWT_TOKEN}}
@apiKey = {{$env API_KEY}}

###

# Bearer token from environment
GET {{baseUrl}}/protected
Authorization: Bearer {{token}}

###

# API key from environment
GET {{baseUrl}}/data
X-API-Key: {{apiKey}}
```

### Login Flow in HTTP Files

```http
# complete-auth-flow.http
@baseUrl = https://api.example.com

###

# @name login
POST {{baseUrl}}/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "password"
}

###

# @name getProfile
GET {{baseUrl}}/profile
Authorization: Bearer {{login.response.body.access_token}}

###

# @name updateProfile
PUT {{baseUrl}}/profile
Authorization: Bearer {{login.response.body.access_token}}
Content-Type: application/json

{
  "name": "Updated Name",
  "email": "new@example.com"
}
```

## Security Best Practices

### Environment Variables

```bash
# Never hardcode secrets in commands or files
# Use environment variables instead

# Bad
dotnet-http GET api.example.com/data X-API-Key:"secret-key-123"

# Good
export API_KEY="secret-key-123"
dotnet-http GET api.example.com/data X-API-Key:"$API_KEY"
```

### Secure Storage

```bash
# Use secure credential storage
# Example with macOS Keychain
security add-internet-password -s "api.example.com" -a "myapp" -w "secret-api-key"
API_KEY=$(security find-internet-password -s "api.example.com" -a "myapp" -w)

dotnet-http GET api.example.com/data X-API-Key:"$API_KEY"
```

### Token Rotation

```bash
#!/bin/bash
# token-rotation.sh

# Check if token is expired
if ! dotnet-http GET api.example.com/verify Authorization:"Bearer $ACCESS_TOKEN" >/dev/null 2>&1; then
  echo "Token expired, refreshing..."
  
  # Refresh token
  NEW_TOKEN_RESPONSE=$(dotnet-http POST api.example.com/auth/refresh \
    refresh_token="$REFRESH_TOKEN" --body)
  
  ACCESS_TOKEN=$(echo $NEW_TOKEN_RESPONSE | jq -r '.access_token')
  export ACCESS_TOKEN
  
  echo "Token refreshed successfully"
fi

# Use current token
dotnet-http GET api.example.com/protected Authorization:"Bearer $ACCESS_TOKEN"
```

## Platform-Specific Examples

### GitHub API

```bash
# Personal access token
dotnet-http GET api.github.com/user \
  Authorization:"token $GITHUB_TOKEN"

# Create repository
dotnet-http POST api.github.com/user/repos \
  Authorization:"token $GITHUB_TOKEN" \
  name="new-repo" \
  description="Created via dotnet-httpie"
```

### AWS API

```bash
# AWS Signature Version 4 (simplified)
AWS_ACCESS_KEY="your-access-key"
AWS_SECRET_KEY="your-secret-key"
AWS_REGION="us-east-1"
SERVICE="s3"

# Note: Full AWS sig v4 implementation would be more complex
dotnet-http GET s3.amazonaws.com/bucket-name \
  Authorization:"AWS4-HMAC-SHA256 ..." \
  X-Amz-Date:"$(date -u +%Y%m%dT%H%M%SZ)"
```

### Google APIs

```bash
# OAuth 2.0 with Google
# First, get OAuth token through browser flow
# Then use the access token

dotnet-http GET www.googleapis.com/oauth2/v1/userinfo \
  Authorization:"Bearer $GOOGLE_ACCESS_TOKEN"

# Service account authentication (with JWT)
dotnet-http GET www.googleapis.com/storage/v1/b \
  Authorization:"Bearer $SERVICE_ACCOUNT_JWT"
```

## Testing Authentication

### Verify Token Validity

```bash
# Test if token is valid
if dotnet-http GET api.example.com/verify Authorization:"Bearer $TOKEN" --check-status; then
  echo "Token is valid"
else
  echo "Token is invalid or expired"
fi
```

### Authentication Debugging

```bash
# Debug authentication issues
dotnet-http GET api.example.com/protected \
  Authorization:"Bearer $TOKEN" \
  --debug \
  --offline  # Preview the request first
```

### Automated Authentication Testing

```bash
#!/bin/bash
# auth-test.sh

# Test various authentication methods
echo "Testing authentication methods..."

# Test API key
if dotnet-http GET api.example.com/test X-API-Key:"$API_KEY" --check-status; then
  echo "✓ API Key authentication works"
else
  echo "✗ API Key authentication failed"
fi

# Test JWT token
if dotnet-http GET api.example.com/test Authorization:"Bearer $JWT_TOKEN" --check-status; then
  echo "✓ JWT authentication works"
else
  echo "✗ JWT authentication failed"
fi

# Test basic auth
if dotnet-http GET api.example.com/test Authorization:"Basic $BASIC_AUTH" --check-status; then
  echo "✓ Basic authentication works"
else
  echo "✗ Basic authentication failed"
fi
```

## Next Steps

- Learn about [request data types](request-data-types.md) for sending authenticated requests
- Explore [file execution](file-execution.md) for managing authentication in HTTP files
- Check out [environment variables](environment-variables.md) for secure credential management
- Review [examples](examples/common-use-cases.md) for real-world authentication patterns