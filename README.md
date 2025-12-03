# dotnet-HTTPie

[![dotnet-HTTPie](https://img.shields.io/nuget/v/dotnet-httpie)](https://www.nuget.org/packages/dotnet-httpie/)
[![dotnet-HTTPie Latest](https://img.shields.io/nuget/vpre/dotnet-httpie)](https://www.nuget.org/packages/dotnet-httpie/absoluteLatest)
[![GitHub Action Build Status](https://github.com/WeihanLi/dotnet-httpie/actions/workflows/dotnet.yml/badge.svg)](https://github.com/WeihanLi/dotnet-httpie/actions/workflows/dotnet.yml)
[![Docker Pulls](https://img.shields.io/docker/pulls/weihanli/dotnet-httpie)](https://hub.docker.com/r/weihanli/dotnet-httpie/tags)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/WeihanLi/dotnet-httpie)

> **Modern, user-friendly command-line HTTP client for the .NET ecosystem**

dotnet-httpie is a .NET tool that brings the power and simplicity of [HTTPie](https://github.com/httpie/httpie) to .NET developers. It's designed for testing, debugging, and interacting with APIs and HTTP servers with an intuitive command-line interface.

![httpie](https://raw.githubusercontent.com/httpie/httpie/master/docs/httpie-animation.gif)

## ✨ Key Features

- 🚀 **Simple & Intuitive**: Human-friendly syntax for HTTP requests
- 📁 **File Execution**: Run `.http` and `.rest` files for repeatable testing
- 🔄 **cURL Support**: Execute cURL commands directly
- 🐳 **Docker Ready**: Available as a Docker image for containerized environments
- 🔗 **Request Chaining**: Reference previous responses in subsequent requests
- 🌍 **Environment Support**: Multiple environment configurations
- 📊 **Load Testing**: Built-in load testing capabilities
- 🔐 **Authentication**: Support for various auth methods (JWT, API keys, Basic auth)
- ⬇️ **File Downloads**: Download files with progress indicators
- 🔍 **JSON Schema Validation**: Validate API responses against schemas
- 💾 **Request Caching**: Cache requests for improved performance
- 🎯 **Middleware System**: Extensible request/response pipeline

## 🚀 Quick Start

### Installation

Install the latest stable version:

```bash
dotnet tool update --global dotnet-httpie
```

Or install the latest preview:

```bash
dotnet tool update --global dotnet-httpie --prerelease
```

### Your First Request

```bash
# Simple GET request
dotnet-http httpbin.org/get

# POST with JSON data  
dotnet-http POST httpbin.org/post name=John age:=30

# With custom headers
dotnet-http GET httpbin.org/headers Authorization:"Bearer token"
```

## 📖 Documentation

| Topic | Description |
|-------|-------------|
| 📋 [Installation Guide](docs/articles/installation.md) | Detailed installation instructions for all platforms |
| ⚡ [Quick Start](docs/articles/quick-start.md) | Get up and running in minutes |
| 🌐 [HTTP Requests](docs/articles/http-requests.md) | Complete guide to making HTTP requests |
| 📄 [File Execution](docs/articles/file-execution.md) | Execute .http/.rest files |
| 🐳 [Docker Usage](docs/articles/docker-usage.md) | Using dotnet-httpie with Docker |
| 💡 [Common Use Cases](docs/articles/examples/common-use-cases.md) | Practical examples and patterns |
| 🔧 [Full Documentation](docs/articles/README.md) | Complete documentation index |

## 💫 Command Syntax

```bash
dotnet-http [flags] [METHOD] URL [ITEM [ITEM]]
```

### Request Items

| Type | Syntax | Example | Description |
|------|--------|---------|-------------|
| **Query Parameters** | `name==value` | `search==httpie` | URL query string parameters |
| **Headers** | `name:value` | `Authorization:Bearer token` | HTTP request headers |
| **JSON Data** | `name=value` | `name=John` | JSON request body fields |
| **Raw JSON** | `name:=value` | `age:=30`, `active:=true` | Raw JSON values (numbers, booleans, objects) |

## 🎯 Examples

### Basic Requests

```bash
# GET request with query parameters
dotnet-http GET httpbin.org/get search==httpie limit==10

# POST request with JSON data
dotnet-http POST httpbin.org/post name=John email=john@example.com age:=30

# PUT request with headers
dotnet-http PUT api.example.com/users/123 \
  Authorization:"Bearer token" \
  name="John Smith" \
  active:=true
```

### Advanced Usage

```bash
# Complex JSON with nested objects
dotnet-http POST api.example.com/users \
  name=John \
  address[city]=Seattle \
  address[zipcode]:=98101 \
  tags:='["developer", "api"]'



# Download files
dotnet-http GET api.example.com/files/report.pdf --download
```

### Real-World API Examples

```bash
# GitHub API
dotnet-http GET api.github.com/users/octocat

# Create GitHub issue (with authentication)
dotnet-http POST api.github.com/repos/owner/repo/issues \
  Authorization:"token your-token" \
  title="Bug report" \
  body="Description of the issue"

# JSON API with multiple data types
dotnet-http POST api.example.com/orders \
  Authorization:"Bearer jwt-token" \
  customer_id:=12345 \
  items:='[{"id": 1, "qty": 2}, {"id": 2, "qty": 1}]' \
  urgent:=true \
  notes="Please handle with care"
```

## 📁 File Execution

Execute HTTP requests from `.http` and `.rest` files:

```bash
# Execute HTTP file
dotnet-http exec requests.http

# Execute with specific environment
dotnet-http exec api-tests.http --env production

# Execute cURL commands
dotnet-http exec commands.curl --type curl
```

### Example .http file:

```http
@baseUrl = https://api.example.com
@token = your-jwt-token

###

# @name getUsers
GET {{baseUrl}}/users
Authorization: Bearer {{token}}

###

# @name createUser  
POST {{baseUrl}}/users
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "name": "John Doe",
  "email": "john@example.com"
}

###

# Reference previous response
GET {{baseUrl}}/users/{{createUser.response.body.id}}
Authorization: Bearer {{token}}
```

### Environment Support

Create `http-client.env.json`:

```json
{
  "development": {
    "baseUrl": "http://localhost:3000",
    "token": "dev-token"
  },
  "production": {
    "baseUrl": "https://api.example.com", 
    "token": "prod-token"
  }
}
```

## 🐳 Docker

Use dotnet-httpie without installing .NET:

```bash
# Basic usage
docker run --rm weihanli/dotnet-httpie:latest httpbin.org/get

# POST with data  
docker run --rm weihanli/dotnet-httpie:latest POST httpbin.org/post name=test

# Execute HTTP files
docker run --rm -v $(pwd):/workspace -w /workspace \
  weihanli/dotnet-httpie:latest exec requests.http

# With environment variables
docker run --rm -e API_TOKEN="your-token" \
  weihanli/dotnet-httpie:latest GET api.example.com/protected \
  Authorization:"Bearer $API_TOKEN"
```

### Create an alias for easier usage:

```bash
# Add to your shell profile (.bashrc, .zshrc, etc.)
alias http='docker run --rm weihanli/dotnet-httpie:latest'

# Now use it like the installed version
http GET httpbin.org/get
http POST httpbin.org/post name=John
```

## 🔧 Advanced Features

### Authentication
- **JWT Tokens**: `Authorization:"Bearer token"`
- **API Keys**: `X-API-Key:"key"` or `api_key==key`  
- **Basic Auth**: `--auth username:password` or `Authorization:"Basic base64"`

### File Operations
- **Form data**: `--form field=value`
- **Download**: `--download` flag
- **Send raw data**: `--raw "data"`

### Request Features
- **Query parameters**: `param==value`
- **Custom headers**: `Header-Name:"value"`
- **JSON data**: `field=value` or `field:=rawjson`
- **Form data**: `--form` flag
- **Raw data**: `--raw "data"`

### Execution Modes
- **Offline mode**: `--offline` (preview requests)
- **Debug mode**: `--debug` (detailed logging)
- **Environment**: `--env production`

### Response Handling
- **Body only**: `--body` flag
- **Follow redirects**: `--follow`
- **JSON processing**: Pipe to `jq` for advanced processing

## 🚀 Use Cases

- **API Development**: Test endpoints during development
- **API Documentation**: Executable examples in documentation
- **CI/CD Testing**: Automated API testing in pipelines
- **Load Testing**: Built-in load testing capabilities  
- **Integration Testing**: Test service-to-service communication
- **Debugging**: Inspect HTTP requests and responses
- **Scripting**: Automate API interactions in shell scripts

## 🤝 Contributing

We welcome contributions! Here's how you can help:

1. **Report Issues**: Found a bug? [Open an issue](https://github.com/WeihanLi/dotnet-httpie/issues)
2. **Feature Requests**: Have an idea? [Open an issue](https://github.com/WeihanLi/dotnet-httpie/issues)
3. **Documentation**: Help improve the docs
4. **Code**: Submit pull requests for bug fixes or features

### Development Setup

```bash
# Clone the repository
git clone https://github.com/WeihanLi/dotnet-httpie.git
cd dotnet-httpie

# Build the project
dotnet build

# Run tests
dotnet test

# Install locally for testing
dotnet pack
dotnet tool install --global --add-source ./artifacts dotnet-httpie
```

## 📚 Resources

- **📖 [Complete Documentation](docs/articles/README.md)** - Comprehensive guides and tutorials
- **🎯 [Examples](docs/articles/examples/common-use-cases.md)** - Real-world usage patterns
- **🐳 [Docker Guide](docs/articles/docker-usage.md)** - Containerized usage
- **📄 [Release Notes](docs/ReleaseNotes.md)** - What's new in each version
- **💬 [Issues](https://github.com/WeihanLi/dotnet-httpie/issues)** - Community Q&A, bug reports, and feature requests

## 🙏 Acknowledgments

- Inspired by the amazing [HTTPie](https://github.com/httpie/httpie) project
- Built with ❤️ for the .NET community
- Special thanks to all [contributors](https://github.com/WeihanLi/dotnet-httpie/contributors)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

<div align="center">

**⭐ Star this repository if you find it useful!**

[🏠 Homepage](https://github.com/WeihanLi/dotnet-httpie) • 
[📖 Documentation](docs/articles/README.md) • 
[🐳 Docker Hub](https://hub.docker.com/r/weihanli/dotnet-httpie) • 
[📦 NuGet](https://www.nuget.org/packages/dotnet-httpie/)

</div>
