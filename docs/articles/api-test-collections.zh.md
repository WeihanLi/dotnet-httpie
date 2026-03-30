# HTTP API 测试集合

> 📖 [View English Documentation](api-test-collections.md)

dotnet-httpie 通过 `test` 子命令支持运行结构化的 HTTP API 测试集合。集合以 JSON 文件定义，支持层级组织，并具备变量继承、请求前后脚本和丰富的断言助手。

## 概述

`test` 命令允许您：

- 将 API 测试组织为**集合 → 组 → 请求**的层级结构
- 在集合、组或请求级别定义**变量**（内层变量会覆盖外层变量）
- 在请求前运行 **preScripts**（例如动态注入认证头）
- 在请求后运行 **postScripts**（例如断言状态码和响应体）
- 使用**环境**在开发、预发布和生产配置之间切换
- 使用 **Roslyn C# 脚本**进行复杂断言和数据转换

## 快速开始

### 1. 创建测试集合文件

```json
{
  "name": "MyApi",
  "variables": {
    "baseUrl": "https://api.example.com"
  },
  "postScript": "$response.EnsureSuccessStatusCode()",
  "groups": [
    {
      "name": "Users",
      "requests": [
        {
          "name": "GetUsers",
          "method": "GET",
          "url": "{{baseUrl}}/users"
        },
        {
          "name": "CreateUser",
          "method": "POST",
          "url": "{{baseUrl}}/users",
          "headers": {
            "Content-Type": "application/json"
          },
          "body": "{\"name\": \"John Doe\", \"email\": \"john@example.com\"}",
          "postScript": "response.StatusCode.ShouldBe(201);"
        }
      ]
    }
  ]
}
```

### 2. 运行集合

```bash
dotnet-http test my-api.httptest.json
```

### 3. 指定环境运行

```bash
dotnet-http test my-api.httptest.json --env staging
```

## 集合文件格式

测试集合以 JSON 文件存储（惯例命名为 `*.httptest.json`）。

### 顶层结构

| 字段 | 类型 | 描述 |
|-------|------|-------------|
| `name` | string | 集合的显示名称 |
| `variables` | object | 对所有组和请求可用的键值对 |
| `preScript` | string | 集合中**每个**请求执行前运行的脚本 |
| `postScript` | string | 集合中**每个**请求执行后运行的脚本 |
| `groups` | array | 请求组列表 |

### 组结构

| 字段 | 类型 | 描述 |
|-------|------|-------------|
| `name` | string | 组的显示名称 |
| `variables` | object | 为该组内所有请求覆盖集合变量的变量 |
| `preScript` | string | 该组每个请求执行前运行的脚本（覆盖集合 preScript） |
| `postScript` | string | 该组每个请求执行后运行的脚本（覆盖集合 postScript） |
| `requests` | array | HTTP 请求列表 |

### 请求结构

| 字段 | 类型 | 描述 |
|-------|------|-------------|
| `name` | string | 请求的显示名称 |
| `method` | string | HTTP 方法（`GET`、`POST`、`PUT`、`PATCH`、`DELETE` 等） |
| `url` | string | 请求 URL，支持 `{{variableName}}` 替换 |
| `headers` | object | 请求头，值支持 `{{variableName}}` 替换 |
| `body` | string | 请求体，支持 `{{variableName}}` 替换 |
| `variables` | object | 为此请求覆盖组/集合变量的变量 |
| `preScript` | string | 此请求执行前运行的脚本（覆盖组/集合 preScript） |
| `postScript` | string | 此请求执行后运行的脚本（覆盖组/集合 postScript） |

## 变量

变量使用 `{{variableName}}` 语法，在 URL、请求头值和请求体中进行替换。

### 变量继承

变量按从低到高的优先级合并，高优先级值会覆盖低优先级值：

1. 集合 `variables` —— 最低优先级（提供集合级默认值）
2. 环境文件变量 —— 覆盖集合默认值（使用 `--env` 时应用；未指定 `--env` 时，如果存在名为 `default` 的环境则加载）
3. 组 `variables` —— 覆盖集合和环境变量
4. 请求 `variables` —— 最高优先级，覆盖所有外层作用域

```json
{
  "name": "Orders API",
  "variables": {
    "baseUrl": "https://api.example.com",
    "apiVersion": "v2"
  },
  "groups": [
    {
      "name": "Legacy",
      "variables": {
        "apiVersion": "v1"
      },
      "requests": [
        {
          "name": "GetOrders",
          "method": "GET",
          "url": "{{baseUrl}}/{{apiVersion}}/orders"
        }
      ]
    }
  ]
}
```

### 在脚本中更新变量

脚本可以更新变量，在请求间传递值：

```json
{
  "name": "CreateUser",
  "method": "POST",
  "url": "{{baseUrl}}/users",
  "headers": { "Content-Type": "application/json" },
  "body": "{\"name\": \"Jane\"}",
  "postScript": "variables[\"userId\"] = (string)response.body.json.id;"
},
{
  "name": "GetUser",
  "method": "GET",
  "url": "{{baseUrl}}/users/{{userId}}"
}
```

## 环境

环境允许在不修改集合文件的情况下切换变量集。

### 环境文件格式

创建文件（惯例命名为 `*.httptest.env.json`）：

```json
[
  {
    "name": "dev",
    "variables": {
      "baseUrl": "http://localhost:5000",
      "apiKey": "dev-key-123"
    }
  },
  {
    "name": "staging",
    "variables": {
      "baseUrl": "https://staging.example.com",
      "apiKey": "staging-key-abc"
    }
  },
  {
    "name": "prod",
    "variables": {
      "baseUrl": "https://api.example.com",
      "apiKey": "prod-key-xyz"
    }
  }
]
```

### 指定环境运行

```bash
# 使用默认环境文件中的命名环境
dotnet-http test my-api.httptest.json --env staging

# 使用显式环境文件
dotnet-http test my-api.httptest.json --env-file envs/staging.httptest.env.json

# 两者结合
dotnet-http test my-api.httptest.json --env staging --env-file envs/staging.httptest.env.json
```

## 脚本

脚本是在每个请求前/后运行的 C# 表达式或语句。

### preScript

在 HTTP 请求发送**前**执行。用于：
- 注入认证头
- 动态修改请求头
- 在请求前设置变量

可用的全局变量：
- `request` —— 原始 `HttpRequestMessage`
- `variables` —— 合并后的变量字典（可读写）

```json
"preScript": "$request.headers.add(\"Authorization\", \"Bearer {{apiKey}}\")"
```

### postScript

在收到 HTTP 响应**后**执行。用于：
- 断言响应状态码
- 验证响应体内容
- 将响应中的值提取到变量中供后续请求使用

可用的全局变量：
- `request` —— 原始 `HttpRequestMessage`
- `response` —— 带状态码和响应体访问的 `HttpTestResponseContext`
- `variables` —— 合并后的变量字典（可读写）

```json
"postScript": "$response.EnsureSuccessStatusCode()"
```

### 简写语法

| 模式 | 描述 |
|---------|-------------|
| `$request.headers.add("name", "value")` | 添加请求头 |
| `$request.headers.set("name", "value")` | 设置/替换请求头 |
| `$response.EnsureSuccessStatusCode()` | 状态码非 2xx 时失败 |
| `$response.StatusCode == 201` | 断言精确状态码 |
| `$response.StatusCode != 404` | 断言状态码不同 |
| `$response.Body.Contains("text")` | 断言响应体包含文本 |

### 复杂 C# 脚本

对于高级逻辑，可使用完整 C# 表达式，`$request`/`$response` 前缀为可选：

```csharp
// preScript —— 从环境变量注入动态 API 密钥
var key = System.Environment.GetEnvironmentVariable("API_KEY");
request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {key}");
```

```csharp
// postScript —— 提取令牌并保存供后续请求使用
response.EnsureSuccessStatusCode();
var token = (string)response.body.json.token;
token.ShouldNotBeNullOrEmpty("响应中必须包含令牌");
variables["token"] = token;
```

## 断言助手

所有断言失败都会抛出 `HttpTestAssertionException`，立即停止脚本并将请求标记为失败。

### 链式扩展方法

链式断言作为常见类型的扩展方法提供：

```csharp
// 状态码
response.StatusCode.ShouldBe(200);
response.StatusCode.ShouldNotBe(500);
response.StatusCode.ShouldBeGreaterThanOrEqualTo(200);
response.StatusCode.ShouldBeLessThan(300);

// 布尔条件
(response.StatusCode < 500).ShouldBeTrue("服务器错误");
false.ShouldBeFalse();

// JSON 响应体访问
var id = (long)response.body.json.id;
id.ShouldBeGreaterThan(0L);

// 响应体文本
response.body.text.ShouldContain("\"status\":\"ok\"");
response.body.text.ShouldNotContain("error");
response.body.text.ShouldStartWith("{");

// 字符串值
var name = (string)response.body.json.name;
name.ShouldBe("John Doe");
name.ShouldNotBeNullOrEmpty("名称字段必须存在");

// null 检查
((object)response.body.json.id).ShouldNotBeNull("ID 必须存在");
```

**完整扩展方法列表：**

| 方法 | 适用类型 | 描述 |
|--------|-----------|-------------|
| `ShouldBeTrue(msg?)` | `bool` | 断言值为 `true` |
| `ShouldBeFalse(msg?)` | `bool` | 断言值为 `false` |
| `ShouldBeNull(msg?)` | `object?` | 断言值为 null |
| `ShouldNotBeNull(msg?)` | `object?` | 断言值不为 null |
| `ShouldBe(expected, msg?)` | `int`, `long`, `string?` | 断言相等 |
| `ShouldNotBe(unexpected, msg?)` | `int` | 断言不相等 |
| `ShouldBeGreaterThan(n, msg?)` | `int`, `long` | 断言 `> n` |
| `ShouldBeGreaterThanOrEqualTo(n, msg?)` | `int` | 断言 `>= n` |
| `ShouldBeLessThan(n, msg?)` | `int` | 断言 `< n` |
| `ShouldContain(s, msg?)` | `string?` | 断言字符串包含子串 |
| `ShouldNotContain(s, msg?)` | `string?` | 断言字符串不包含子串 |
| `ShouldStartWith(s, msg?)` | `string?` | 断言字符串以指定前缀开头 |
| `ShouldEndWith(s, msg?)` | `string?` | 断言字符串以指定后缀结尾 |
| `ShouldNotBeNullOrEmpty(msg?)` | `string?` | 断言不为 null 或空字符串 |
| `ShouldNotBeNullOrWhiteSpace(msg?)` | `string?` | 断言不为 null、空字符串或空白字符 |

### HttpAssert 静态类

用于 xunit / `Debug.Assert` 风格的断言：

```csharp
HttpAssert.Equal(200, response.StatusCode);
HttpAssert.NotEqual(500, response.StatusCode);
HttpAssert.True(response.StatusCode < 500, "服务器错误");
HttpAssert.False(response.StatusCode >= 500);
HttpAssert.NotNull(response.body.json.id, "ID 必须存在");
HttpAssert.Null(response.body.json.error);
HttpAssert.Contains("token", response.body.text);
HttpAssert.DoesNotContain("error", response.body.text);
HttpAssert.NotNullOrEmpty((string)response.body.json.name);
HttpAssert.GreaterThan(0, (int)response.StatusCode);
HttpAssert.LessThan(300, response.StatusCode);
HttpAssert.Fail("显式失败消息");
```

### 内置响应助手

`HttpTestResponseContext` 直接暴露以下助手方法：

```csharp
// 非 2xx 时抛出异常
response.EnsureSuccessStatusCode();

// 通用条件断言
response.Assert(response.StatusCode == 200, "期望 200 OK");

// 状态码
int code = response.StatusCode;

// 响应体
string text = response.body.text;
dynamic json = response.body.json;

// 深层 JSON 访问
var userId = (long)response.body.json.user.id;
var email  = (string)response.body.json.user.email;
```

## 完整示例

以下集合演示了环境使用、变量继承、跨请求传递值以及各种断言风格。

### 集合文件：`users-api.httptest.json`

```json
{
  "name": "Users API",
  "variables": {
    "baseUrl": "https://api.example.com",
    "contentType": "application/json"
  },
  "postScript": "$response.EnsureSuccessStatusCode()",
  "groups": [
    {
      "name": "Auth",
      "requests": [
        {
          "name": "Login",
          "method": "POST",
          "url": "{{baseUrl}}/auth/login",
          "headers": {
            "Content-Type": "{{contentType}}"
          },
          "body": "{\"username\": \"{{username}}\", \"password\": \"{{password}}\"}",
          "postScript": "response.StatusCode.ShouldBe(200);\nvar token = (string)response.body.json.token;\ntoken.ShouldNotBeNullOrEmpty();\nvariables[\"token\"] = token;"
        }
      ]
    },
    {
      "name": "Users",
      "preScript": "$request.headers.add(\"Authorization\", \"Bearer {{token}}\")",
      "requests": [
        {
          "name": "ListUsers",
          "method": "GET",
          "url": "{{baseUrl}}/users",
          "postScript": "response.StatusCode.ShouldBe(200);\nHttpAssert.Contains(\"users\", response.body.text);"
        },
        {
          "name": "CreateUser",
          "method": "POST",
          "url": "{{baseUrl}}/users",
          "headers": {
            "Content-Type": "{{contentType}}"
          },
          "body": "{\"name\": \"Test User\", \"email\": \"test@example.com\"}",
          "postScript": "response.StatusCode.ShouldBe(201);\nvar id = (long)response.body.json.id;\nid.ShouldBeGreaterThan(0L);\nvariables[\"newUserId\"] = id.ToString();"
        },
        {
          "name": "GetUser",
          "method": "GET",
          "url": "{{baseUrl}}/users/{{newUserId}}",
          "postScript": "response.StatusCode.ShouldBe(200);\n((string)response.body.json.email).ShouldBe(\"test@example.com\");"
        },
        {
          "name": "DeleteUser",
          "method": "DELETE",
          "url": "{{baseUrl}}/users/{{newUserId}}",
          "postScript": "response.StatusCode.ShouldBe(204);"
        }
      ]
    }
  ]
}
```

### 环境文件：`users-api.httptest.env.json`

```json
[
  {
    "name": "dev",
    "variables": {
      "baseUrl": "http://localhost:5000",
      "username": "dev_user",
      "password": "dev_pass"
    }
  },
  {
    "name": "staging",
    "variables": {
      "baseUrl": "https://staging.example.com",
      "username": "staging_user",
      "password": "staging_pass"
    }
  }
]
```

### 运行命令

```bash
# 针对 dev 环境运行
dotnet-http test users-api.httptest.json --env dev --env-file users-api.httptest.env.json

# 离线模式预览请求（不实际发送）
dotnet-http test users-api.httptest.json --env dev --env-file users-api.httptest.env.json --offline
```

### 示例输出

```
=== Collection: Users API ===
--- Group: Auth ---
  [Login]
  Request:
  POST https://api.example.com/auth/login
  Content-Type: application/json
  ...
  Response (42ms):
  HTTP/1.1 200 OK
  ...
  ✓ Login PASSED (42ms)
--- Group: Users ---
  [ListUsers]
  ...
  ✓ ListUsers PASSED (18ms)
  [CreateUser]
  ...
  ✓ CreateUser PASSED (23ms)
  [GetUser]
  ...
  ✓ GetUser PASSED (11ms)
  [DeleteUser]
  ...
  ✓ DeleteUser PASSED (9ms)

=== Test Summary: Users API ===
  Total:  5
  Passed: 5
  Failed: 0
```

## CLI 参考

```
dotnet-http test [collectionPath] [options]

参数：
  collectionPath    测试集合文件路径（.httptest.json）

选项：
  --env <name>          要使用的环境名称
  --env-file <path>     环境文件路径（.httptest.env.json）
  --offline             仅打印请求，不实际发送
```

## CI/CD 集成

### GitHub Actions

```yaml
name: API Tests
on: [push, pull_request]

jobs:
  api-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Install dotnet-httpie
        run: dotnet tool install --global dotnet-httpie
      - name: Run API tests
        run: dotnet-http test tests/users-api.httptest.json --env ci --env-file tests/users-api.httptest.env.json
        env:
          API_KEY: ${{ secrets.API_KEY }}
```

### Azure DevOps

```yaml
steps:
- task: DotNetCoreCLI@2
  displayName: Install dotnet-httpie
  inputs:
    command: custom
    custom: tool
    arguments: install --global dotnet-httpie

- script: dotnet-http test tests/api.httptest.json --env $(Environment) --env-file tests/api.httptest.env.json
  displayName: Run API Test Collection
```

## 使用技巧与最佳实践

1. **使用环境** —— 将特定环境的值（基础 URL、凭据）放在 `.httptest.env.json` 文件中，不要写入集合文件。
2. **通过变量链接请求** —— 从响应中提取值并存储到 `variables` 中，供后续请求使用。
3. **设置集合级 postScript** —— 全局的 `$response.EnsureSuccessStatusCode()` 可在不逐一重复的情况下捕获意外错误。
4. **按请求覆盖** —— 对于预期返回非 2xx 响应的请求，在请求级别覆盖集合/组脚本。
5. **先使用离线模式** —— 运行 `--offline` 确认 URL 和请求头，再发送真实请求。
6. **将集合文件纳入版本控制** —— 将 `*.httptest.json` 文件与源代码一起提交，使测试随 API 演进。
7. **将环境文件排除在版本控制之外** —— 如果包含密钥，将 `*.httptest.env.json` 添加到 `.gitignore`，在 CI 中使用密钥管理。

## 下一步

- 了解[文件执行](file-execution.zh.md)以运行 `.http` 文件
- 探索 [CI/CD 集成](ci-cd-integration.zh.md)构建自动化测试流水线
- 查看[身份认证](authentication.zh.md)模式保护请求安全
