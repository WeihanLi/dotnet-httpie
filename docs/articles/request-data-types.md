# Request Data Types

This guide explains the different ways to structure and send data with your HTTP requests using dotnet-httpie.

## Overview

dotnet-httpie supports multiple request data formats and provides intuitive syntax for different data types:

- **Query Parameters**: `name==value`
- **Headers**: `name:value`
- **JSON Fields**: `name=value`
- **Raw JSON Values**: `name:=value`

## Query Parameters

Query parameters are appended to the URL and use the `==` syntax.

### Basic Query Parameters

```bash
# Single parameter
dotnet-http httpbin.org/get search==httpie

# Multiple parameters
dotnet-http httpbin.org/get search==httpie lang==en page==1

# URL encoding is automatic
dotnet-http httpbin.org/get query=="hello world" special=="chars!@#"
```

### Arrays in Query Parameters

```bash
# Multiple values for same parameter
dotnet-http httpbin.org/get tags==javascript tags==web tags==api

# Results in: ?tags=javascript&tags=web&tags=api
```

### Empty Values

```bash
# Empty parameter
dotnet-http httpbin.org/get empty==

# Null parameter (omitted)
dotnet-http httpbin.org/get param==null
```

## HTTP Headers

Headers use the `:` syntax and control how requests are processed.

### Common Headers

```bash
# Authorization
dotnet-http httpbin.org/headers Authorization:"Bearer jwt-token"

# Content-Type
dotnet-http POST httpbin.org/post Content-Type:"application/xml"

# User-Agent
dotnet-http httpbin.org/headers User-Agent:"MyApp/1.0"

# Accept
dotnet-http httpbin.org/headers Accept:"application/json, text/plain"

# API Keys
dotnet-http api.example.com/data X-API-Key:"your-api-key"
```

### Custom Headers

```bash
# Multiple custom headers
dotnet-http api.example.com/webhook \
  X-Webhook-Source:"github" \
  X-Signature:"sha256=signature" \
  X-Event-Type:"push"
```

### Header Values with Spaces

```bash
# Quote values containing spaces
dotnet-http httpbin.org/headers User-Agent:"My Application v1.0"
```

## JSON Request Body

### Simple JSON Fields

Using `=` creates JSON fields automatically:

```bash
dotnet-http POST httpbin.org/post name=John email=john@example.com

# Generates:
# {
#   "name": "John",
#   "email": "john@example.com"
# }
```

### Data Types

#### Strings (Default)

```bash
dotnet-http POST httpbin.org/post title="Hello World" description="A test post"
```

#### Numbers

```bash
# Integers
dotnet-http POST httpbin.org/post age:=30 count:=100

# Floats
dotnet-http POST httpbin.org/post price:=19.99 rating:=4.5
```

#### Booleans

```bash
dotnet-http POST httpbin.org/post active:=true verified:=false published:=true
```

#### Null Values

```bash
dotnet-http POST httpbin.org/post middle_name:=null optional_field:=null
```

#### Arrays

```bash
# Array of strings
dotnet-http POST httpbin.org/post tags:='["javascript", "web", "api"]'

# Array of numbers
dotnet-http POST httpbin.org/post scores:='[95, 87, 92, 78]'

# Array of objects
dotnet-http POST httpbin.org/post items:='[{"id": 1, "name": "Item 1"}, {"id": 2, "name": "Item 2"}]'
```

#### Objects

```bash
# Nested objects
dotnet-http POST httpbin.org/post profile:='{"name": "John", "age": 30, "skills": ["C#", "JavaScript"]}'

# Complex nested structure
dotnet-http POST httpbin.org/post config:='{"database": {"host": "localhost", "port": 5432}, "features": {"auth": true, "cache": false}}'
```

## Nested JSON Structures

### Bracket Notation

```bash
# Nested objects using bracket notation
dotnet-http POST httpbin.org/post \
  user[name]=John \
  user[email]=john@example.com \
  user[address][street]="123 Main St" \
  user[address][city]=Seattle \
  user[address][zipcode]:=98101

# Generates:
# {
#   "user": {
#     "name": "John",
#     "email": "john@example.com",
#     "address": {
#       "street": "123 Main St",
#       "city": "Seattle",
#       "zipcode": 98101
#     }
#   }
# }
```

### Array Elements

```bash
# Array with indexed elements
dotnet-http POST httpbin.org/post \
  items[0][name]=First \
  items[0][value]:=100 \
  items[1][name]=Second \
  items[1][value]:=200

# Generates:
# {
#   "items": [
#     {"name": "First", "value": 100},
#     {"name": "Second", "value": 200}
#   ]
# }
```

## Form Data

### URL-Encoded Forms

```bash
# Use --form flag for application/x-www-form-urlencoded
dotnet-http POST httpbin.org/post --form name=John email=john@example.com

# Mixed with other options
dotnet-http POST httpbin.org/post --form \
  name=John \
  age=30 \
  Authorization:"Bearer token"
```

### Multipart Forms

```bash
# Use --multipart for multipart/form-data
dotnet-http POST httpbin.org/post --multipart \
  name=John \
  file@/path/to/document.pdf

# Multiple files
dotnet-http POST httpbin.org/post --multipart \
  name=John \
  avatar@/path/to/avatar.jpg \
  resume@/path/to/resume.pdf
```

## File Uploads

### Send File as Body

```bash
# Send entire file as request body
dotnet-http POST api.example.com/upload @/path/to/data.json

# With content type
dotnet-http POST api.example.com/upload \
  Content-Type:"application/json" \
  @/path/to/data.json
```

### File in Multipart Form

```bash
# File as form field
dotnet-http POST api.example.com/upload --multipart \
  description="My document" \
  file@/path/to/document.pdf
```

### Multiple Files

```bash
dotnet-http POST api.example.com/batch-upload --multipart \
  batch_name="Document Batch" \
  doc1@/path/to/file1.pdf \
  doc2@/path/to/file2.pdf \
  metadata@/path/to/metadata.json
```

## Raw Data

### Raw String Data

```bash
# Send raw text
dotnet-http POST api.example.com/webhook \
  Content-Type:"text/plain" \
  --raw "This is raw text data"

# Raw JSON (alternative to field syntax)
dotnet-http POST api.example.com/data \
  Content-Type:"application/json" \
  --raw '{"name": "John", "age": 30}'

# Raw XML
dotnet-http POST api.example.com/xml \
  Content-Type:"application/xml" \
  --raw '<user><name>John</name><age>30</age></user>'
```

### Data from Stdin

```bash
# Pipe data from command
echo '{"message": "Hello"}' | dotnet-http POST api.example.com/data

# From file via stdin
cat data.json | dotnet-http POST api.example.com/upload

# From other tools
curl -s api.example.com/export | dotnet-http POST api.example.com/import
```

## Data Type Examples

### E-commerce API

```bash
# Create product
dotnet-http POST api.shop.com/products \
  Authorization:"Bearer token" \
  name="Laptop Computer" \
  description="High-performance laptop" \
  price:=999.99 \
  in_stock:=true \
  categories:='["electronics", "computers"]' \
  specifications:='{"cpu": "Intel i7", "ram": "16GB", "storage": "512GB SSD"}' \
  tags==electronics tags==computers
```

### User Registration

```bash
# Complex user object
dotnet-http POST api.example.com/users \
  personal[first_name]=John \
  personal[last_name]=Doe \
  personal[email]=john.doe@example.com \
  personal[phone]="+1-555-0123" \
  address[street]="123 Main Street" \
  address[city]=Seattle \
  address[state]=WA \
  address[zipcode]:=98101 \
  address[country]=USA \
  preferences[newsletter]:=true \
  preferences[notifications]:=false \
  role=user \
  active:=true
```

### API Configuration

```bash
# Configuration update
dotnet-http PUT api.example.com/config \
  Authorization:"Bearer admin-token" \
  database[host]=localhost \
  database[port]:=5432 \
  database[ssl]:=true \
  cache[enabled]:=true \
  cache[ttl]:=3600 \
  features:='["auth", "logging", "monitoring"]' \
  limits[requests_per_minute]:=1000 \
  limits[max_file_size]:=10485760
```

## Content-Type Handling

### Automatic Content-Type

```bash
# JSON (default for field syntax)
dotnet-http POST api.example.com/data name=John
# Content-Type: application/json

# Form data
dotnet-http POST api.example.com/data --form name=John
# Content-Type: application/x-www-form-urlencoded

# Multipart
dotnet-http POST api.example.com/data --multipart name=John file@data.txt
# Content-Type: multipart/form-data
```

### Manual Content-Type

```bash
# Override content type
dotnet-http POST api.example.com/data \
  Content-Type:"application/vnd.api+json" \
  name=John age:=30

# XML content
dotnet-http POST api.example.com/data \
  Content-Type:"application/xml" \
  @data.xml
```

## Advanced Data Handling

### Conditional Fields

```bash
# Only include fields if they have values
dotnet-http POST api.example.com/users \
  name=John \
  email=john@example.com \
  $([ "$PHONE" ] && echo "phone=$PHONE") \
  $([ "$COMPANY" ] && echo "company=$COMPANY")
```

### Dynamic Values

```bash
# Use command substitution
dotnet-http POST api.example.com/events \
  timestamp:=$(date +%s) \
  uuid="$(uuidgen)" \
  hostname="$(hostname)"
```

### Environment Variables

```bash
# Reference environment variables
dotnet-http POST api.example.com/deploy \
  Authorization:"Bearer $API_TOKEN" \
  version="$BUILD_VERSION" \
  environment="$DEPLOY_ENV"
```

## Validation and Testing

### Schema Validation

```bash
# Validate response against schema
dotnet-http POST api.example.com/users \
  name=John \
  email=john@example.com \
  --schema user-schema.json
```

### Response Testing

```bash
# Test specific fields in response
dotnet-http POST api.example.com/users name=John | jq '.id != null'

# Combine with shell scripting
response=$(dotnet-http POST api.example.com/users name=John --body)
user_id=$(echo $response | jq -r '.id')
dotnet-http GET api.example.com/users/$user_id
```

## Best Practices

1. **Use appropriate data types** - Numbers as `:=123`, booleans as `:=true`
2. **Quote complex values** - Especially JSON objects and arrays
3. **Be consistent with naming** - Use snake_case or camelCase consistently
4. **Validate data structure** - Use `--offline` to preview requests
5. **Use files for large data** - Avoid very long command lines
6. **Consider security** - Don't put sensitive data in command history
7. **Use environment variables** - For tokens and configuration
8. **Test incrementally** - Start simple and add complexity

## Next Steps

- Learn about [authentication methods](authentication.md)
- Explore [file execution](file-execution.md) for complex data scenarios
- Check out [variable substitution](variables.md) for dynamic values
- See [examples](examples/common-use-cases.md) for real-world usage patterns