# 文件执行

> 📖 [View English Documentation](file-execution.md)

dotnet-httpie 可以执行 `.http` 和 `.rest` 文件中定义的 HTTP 请求，非常适合 API 测试、文档编写和自动化场景。

## 概述

`exec` 命令允许您运行文件中定义的 HTTP 请求，支持以下特性：
- 标准 `.http` 和 `.rest` 文件格式
- 变量替换
- 环境特定配置
- 请求链与请求引用

## 基本用法

### 执行单个文件

```bash
dotnet-http exec requests.http
```

### 指定环境执行

```bash
dotnet-http exec requests.http --env production
```

### 指定请求类型执行

```bash
dotnet-http exec requests.http --type http
dotnet-http exec curl-commands.curl --type curl
```

## HTTP 文件格式

### 基本请求

```http
# 获取用户信息
GET https://api.example.com/users/123
Authorization: Bearer your-token
```

### 多个请求

```http
# 获取所有用户
GET https://api.example.com/users
Authorization: Bearer your-token

###

# 创建新用户
POST https://api.example.com/users
Content-Type: application/json
Authorization: Bearer your-token

{
  "name": "John Doe",
  "email": "john@example.com"
}

###

# 更新用户
PUT https://api.example.com/users/123
Content-Type: application/json
Authorization: Bearer your-token

{
  "name": "John Smith",
  "email": "john.smith@example.com"
}
```

### 带变量的请求

```http
@baseUrl = https://api.example.com
@token = your-bearer-token

# 获取用户
GET {{baseUrl}}/users/123
Authorization: Bearer {{token}}

###

# 使用动态数据创建用户
POST {{baseUrl}}/users
Content-Type: application/json
Authorization: Bearer {{token}}

{
  "name": "User {{$randomInt}}",
  "email": "user{{$randomInt}}@example.com",
  "timestamp": "{{$datetime iso8601}}"
}
```

### 命名请求

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

## 环境文件

### HTTP 客户端环境文件

创建 `http-client.env.json`：

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

### 使用环境变量

```http
# 将使用指定环境中的变量
GET {{baseUrl}}/users
X-API-Key: {{apiKey}}
```

指定环境执行：

```bash
dotnet-http exec api-requests.http --env production
```

## 变量类型

### 内置变量

```http
# 随机值
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

### 环境变量

```http
# 访问系统环境变量
GET {{baseUrl}}/secure
Authorization: Bearer {{$env API_TOKEN}}
```

### 自定义变量

```http
@userId = 123
@apiVersion = v2

GET {{baseUrl}}/{{apiVersion}}/users/{{userId}}
```

## 请求引用

引用前一个请求的响应：

```http
# @name login
POST {{baseUrl}}/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "password"
}

###

# 使用登录响应中的令牌
GET {{baseUrl}}/protected/data
Authorization: Bearer {{login.response.body.token}}

###

# 引用请求头
GET {{baseUrl}}/audit
X-Original-Request-Id: {{login.request.headers.X-Request-ID}}
```

## Curl 文件执行

dotnet-httpie 也可以执行文件中的 curl 命令：

### Curl 文件格式

```bash
# 文件：api-calls.curl

# 获取用户数据
curl -X GET "https://api.example.com/users/123" \
  -H "Authorization: Bearer token" \
  -H "Accept: application/json"

# 创建新用户
curl -X POST "https://api.example.com/users" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer token" \
  -d '{
    "name": "John Doe",
    "email": "john@example.com"
  }'
```

### 执行 Curl 文件

```bash
dotnet-http exec api-calls.curl --type curl
```

## 高级功能

### 请求链

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

## 测试与验证

### 响应断言

```http
GET {{baseUrl}}/users/123

# 测试响应
# @test status === 200
# @test response.body.name === "John Doe"
# @test response.headers["content-type"] includes "application/json"
```

### Schema 验证

```http
POST {{baseUrl}}/users
Content-Type: application/json

{
  "name": "John Doe",
  "email": "john@example.com"
}

# 根据 JSON schema 验证响应
# @schema user-schema.json
```

## 调试文件执行

### 调试模式

```bash
dotnet-http exec requests.http --debug
```

### 离线模式（预览）

```bash
dotnet-http exec requests.http --offline
```

此模式在不实际执行请求的情况下显示将要发送的内容。

### 详细输出

```bash
dotnet-http exec requests.http --verbose
```

## 文件组织策略

### 项目结构

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

### 文件命名规范

- 使用描述性名称：`create-user.http`、`get-order-details.http`
- 按功能分组：`auth/`、`users/`、`orders/`
- 使用环境前缀：`dev-setup.http`、`prod-health.http`

## CI/CD 集成

### GitHub Actions 示例

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

### Azure DevOps 示例

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

## 最佳实践

1. **使用环境文件**区分不同部署环境
2. **为请求命名**以便更好地组织和引用
3. **将相关请求放在同一文件**中
4. **使用变量**替代硬编码值
5. **添加注释**说明复杂请求
6. **在关键位置验证响应**
7. **先使用离线模式测试**，确认请求结构正确
8. **将 HTTP 文件与代码一起纳入版本控制**

## 示例

### 完整 API 测试套件

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

## 下一步

- 详细了解[变量替换](variables.md)
- 探索[请求引用](request-referencing.md)模式
- 配置 [CI/CD 集成](ci-cd-integration.zh.md)与 HTTP 文件结合
- 查看[常见使用场景](examples/common-use-cases.zh.md)获取更多示例
