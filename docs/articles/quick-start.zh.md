# 快速上手指南

> 📖 [View English Documentation](quick-start.md)

几分钟内即可上手 dotnet-httpie！本指南展示发起 HTTP 请求所需的核心命令。

## 第一个请求

发送一个简单的 GET 请求：

```bash
dotnet-http httpbin.org/get
```

这将向 httpbin.org 发送 GET 请求并显示响应结果。

## 基本命令结构

```
dotnet-http [flags] [METHOD] URL [ITEM [ITEM]]
```

其中：
- `flags`：可选命令标志（例如 `--offline`、`--debug`）
- `METHOD`：HTTP 方法（GET、POST、PUT、DELETE 等），默认为 GET
- `URL`：目标 URL
- `ITEM`：请求项（查询参数、请求头、数据）

## 请求项类型

dotnet-httpie 支持四种类型的请求项：

| 类型 | 语法 | 示例 | 描述 |
|------|--------|---------|-------------|
| 查询参数 | `name==value` | `search==dotnet` | URL 查询参数 |
| 请求头 | `name:value` | `Authorization:"Bearer token"` | HTTP 请求头 |
| JSON 数据 | `name=value` | `title=hello` | JSON 请求体字段 |
| 原始 JSON | `name:=value` | `age:=25` | 原始 JSON 值（数字、布尔值、对象） |

## 常用示例

### 带查询参数的 GET 请求

```bash
dotnet-http httpbin.org/get search==httpie limit==10
```

### 带 JSON 数据的 POST 请求

```bash
dotnet-http POST httpbin.org/post title=Hello body="World"
```

### 自定义请求头

```bash
dotnet-http httpbin.org/headers Authorization:"Bearer your-token" User-Agent:"MyApp/1.0"
```

### 混合请求类型

```bash
dotnet-http POST api.example.com/users \
  Authorization:"Bearer token" \
  name="John Doe" \
  age:=30 \
  active:=true \
  search==users
```

## 与 API 交互

### RESTful API 示例

```bash
# 获取用户列表
dotnet-http GET api.example.com/users

# 获取指定用户
dotnet-http GET api.example.com/users/123

# 创建新用户
dotnet-http POST api.example.com/users name="John" email="john@example.com"

# 更新用户
dotnet-http PUT api.example.com/users/123 name="John Smith"

# 删除用户
dotnet-http DELETE api.example.com/users/123
```

### 带认证的 JSON API

```bash
dotnet-http POST api.example.com/posts \
  Authorization:"Bearer your-jwt-token" \
  Content-Type:"application/json" \
  title="My Post" \
  content="This is my post content" \
  published:=true
```

## 常用标志

### 离线模式（预览请求）

```bash
dotnet-http POST api.example.com/data name=test --offline
```

此模式在不实际发送请求的情况下，显示将要发送的请求内容。

### 调试模式

```bash
dotnet-http httpbin.org/get --debug
```

启用详细日志和调试信息。

### 仅显示响应体

```bash
dotnet-http httpbin.org/get --body
```

仅显示响应体，便于与其他工具组合使用。

## 文件操作

### 执行 HTTP 文件

```bash
dotnet-http exec requests.http
```

### 下载文件

```bash
dotnet-http httpbin.org/image/png --download
```

## Docker 使用

如果使用 Docker 版本：

```bash
# 基本请求
docker run --rm weihanli/dotnet-httpie:latest httpbin.org/get

# 带数据的请求
docker run --rm weihanli/dotnet-httpie:latest POST httpbin.org/post name=test
```

## 本地开发

访问本地 API 时可以使用简短 URL：

```bash
# 等同于 http://localhost:5000/api/users
dotnet-http :5000/api/users

# 或
dotnet-http localhost:5000/api/users
```

## 下一步

掌握基础之后：

1. 了解[高级请求数据类型](request-data-types.zh.md)
2. 探索[文件执行功能](file-execution.zh.md)
3. 为您的 API 配置[身份认证](authentication.zh.md)
4. 查看[常见使用场景](examples/common-use-cases.zh.md)

## 需要帮助？

- 使用 `dotnet-http --help` 查看命令行帮助
- 参阅[调试指南](debugging.zh.md)进行故障排查
- 查看[示例](examples/common-use-cases.zh.md)了解更多使用模式
