# 基本用法

> 📖 [View English Documentation](basic-usage.md)

本指南介绍 dotnet-httpie 的基本概念和常用使用模式。

## 命令结构

dotnet-httpie 命令的基本语法：

```
dotnet-http [flags] [METHOD] URL [ITEM [ITEM]]
```

### 各组成部分

- **flags**：可选命令标志（例如 `--offline`、`--debug`、`--body`）
- **METHOD**：HTTP 方法（GET、POST、PUT、DELETE、PATCH、HEAD、OPTIONS）
- **URL**：目标 URL（可以是完整 URL 或简短格式）
- **ITEM**：请求项（查询参数、请求头、数据）

## HTTP 方法

### GET（默认）

```bash
# 简单 GET 请求
dotnet-http httpbin.org/get

# 带查询参数的 GET 请求
dotnet-http httpbin.org/get name==John age==30

# 显式指定 GET 方法
dotnet-http GET httpbin.org/get search==query
```

### POST

```bash
# 带 JSON 数据的 POST 请求
dotnet-http POST httpbin.org/post name=John email=john@example.com

# 带表单数据的 POST 请求
dotnet-http POST httpbin.org/post --form name=John email=john@example.com
```

### PUT

```bash
# PUT 请求（通常用于更新）
dotnet-http PUT httpbin.org/put id:=123 name=John
```

### DELETE

```bash
# DELETE 请求
dotnet-http DELETE httpbin.org/delete

# 带参数的 DELETE 请求
dotnet-http DELETE api.example.com/users/123
```

### 其他方法

```bash
# PATCH 用于部分更新
dotnet-http PATCH httpbin.org/patch status=active

# HEAD 仅获取响应头
dotnet-http HEAD httpbin.org/get

# OPTIONS 查询允许的方法
dotnet-http OPTIONS httpbin.org
```

## URL 格式

### 完整 URL

```bash
# HTTPS URL
dotnet-http https://api.example.com/users

# HTTP URL
dotnet-http http://localhost:3000/api/data

# 带端口的 URL
dotnet-http https://api.example.com:8443/secure
```

### 简短 URL

```bash
# localhost 快捷方式
dotnet-http :3000/api/users          # → http://localhost:3000/api/users
dotnet-http localhost:5000/health    # → http://localhost:5000/health

# 域名默认使用 HTTPS
dotnet-http api.example.com/data     # → https://api.example.com/data
```

### 带路径的 URL

```bash
# 简单路径
dotnet-http api.example.com/v1/users

# 带参数的复杂路径
dotnet-http api.example.com/users/123/posts/456

# 含特殊字符的路径
dotnet-http "api.example.com/search?q=hello world"
```

## 请求项

### 查询参数（`==`）

查询参数被追加到 URL 后：

```bash
# 单个参数
dotnet-http httpbin.org/get name==John

# 多个参数
dotnet-http httpbin.org/get name==John age==30 city=="New York"

# 数组/多个值
dotnet-http httpbin.org/get tag==javascript tag==web tag==api
```

### 请求头（`:`）

请求头用于控制请求行为：

```bash
# 认证头
dotnet-http httpbin.org/headers Authorization:"Bearer token123"

# Content type
dotnet-http POST httpbin.org/post Content-Type:"application/xml"

# 多个请求头
dotnet-http httpbin.org/headers \
  Authorization:"Bearer token" \
  User-Agent:"MyApp/1.0" \
  Accept:"application/json"
```

### JSON 数据（`=`）

创建 JSON 请求体：

```bash
# 简单字段
dotnet-http POST httpbin.org/post name=John age=30

# 生成：{"name": "John", "age": "30"}
```

### 原始 JSON 数据（`:=`）

用于带类型的 JSON 值：

```bash
# 数字
dotnet-http POST httpbin.org/post age:=30 price:=19.99

# 布尔值
dotnet-http POST httpbin.org/post active:=true published:=false

# 数组
dotnet-http POST httpbin.org/post tags:='["web", "api", "tool"]'

# 对象
dotnet-http POST httpbin.org/post profile:='{"name": "John", "level": 5}'

# 生成：{"age": 30, "price": 19.99, "active": true, "published": false, "tags": ["web", "api", "tool"], "profile": {"name": "John", "level": 5}}
```

## 常用模式

### API 测试

```bash
# 健康检查
dotnet-http GET api.example.com/health

# 获取资源列表
dotnet-http GET api.example.com/users

# 获取指定资源
dotnet-http GET api.example.com/users/123

# 创建新资源
dotnet-http POST api.example.com/users name=John email=john@example.com

# 更新资源
dotnet-http PUT api.example.com/users/123 name="John Smith"

# 删除资源
dotnet-http DELETE api.example.com/users/123
```

### 认证模式

```bash
# Bearer 令牌
dotnet-http GET api.example.com/protected \
  Authorization:"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

# 请求头中的 API 密钥
dotnet-http GET api.example.com/data \
  X-API-Key:"your-api-key"

# 查询参数中的 API 密钥
dotnet-http GET api.example.com/data \
  api_key==your-api-key

# 基本认证
dotnet-http GET api.example.com/secure \
  Authorization:"Basic $(echo -n 'user:pass' | base64)"
```

### 数据提交模式

```bash
# 简单表单数据
dotnet-http POST api.example.com/contact \
  name=John \
  email=john@example.com \
  message="Hello from dotnet-httpie"

# 复杂嵌套数据
dotnet-http POST api.example.com/orders \
  customer[name]=John \
  customer[email]=john@example.com \
  items[0][id]:=1 \
  items[0][quantity]:=2 \
  items[1][id]:=2 \
  items[1][quantity]:=1 \
  total:=99.99

# 文件上传
dotnet-http POST api.example.com/upload \
  --multipart \
  description="My document" \
  file@/path/to/document.pdf
```

## 响应处理

### 默认响应

同时显示响应头和响应体：

```bash
dotnet-http GET httpbin.org/get
```

### 仅显示响应体

```bash
# 仅响应体
dotnet-http GET httpbin.org/get --body

# 便于管道传输给其他工具
dotnet-http GET api.example.com/users --body | jq '.users[0]'
```

### 仅显示响应头

```bash
# 仅响应头
dotnet-http HEAD httpbin.org/get
```

### 保存响应

```bash
# 保存到文件
dotnet-http GET api.example.com/report --body > report.json

# 下载文件
dotnet-http GET api.example.com/files/document.pdf --download
```

## 常用标志

### 调试模式

获取请求的详细信息：

```bash
dotnet-http GET api.example.com/data --debug
```

### 离线模式

在不发送请求的情况下预览请求内容：

```bash
dotnet-http POST api.example.com/users name=John --offline
```

### 检查状态

HTTP 错误时返回非零退出码：

```bash
if dotnet-http GET api.example.com/health --check-status; then
  echo "API is healthy"
else
  echo "API is down"
fi
```

## 处理 JSON

### 简单 JSON

```bash
# 字符串值（默认）
dotnet-http POST httpbin.org/post name=John title="Software Engineer"

# 数字值
dotnet-http POST httpbin.org/post age:=30 salary:=75000

# 布尔值
dotnet-http POST httpbin.org/post active:=true verified:=false

# null 值
dotnet-http POST httpbin.org/post middle_name:=null
```

### 复杂 JSON

```bash
# 数组
dotnet-http POST httpbin.org/post \
  skills:='["C#", "JavaScript", "Python"]' \
  scores:='[95, 87, 92]'

# 嵌套对象
dotnet-http POST httpbin.org/post \
  address:='{"street": "123 Main St", "city": "Seattle", "zip": "98101"}' \
  contact:='{"email": "john@example.com", "phone": "+1-555-0123"}'

# 混合复杂数据
dotnet-http POST httpbin.org/post \
  name=John \
  age:=30 \
  active:=true \
  skills:='["programming", "testing"]' \
  address:='{"city": "Seattle", "state": "WA"}' \
  metadata:=null
```

## 错误处理

### HTTP 状态码

```bash
# dotnet-httpie 会清晰地显示 HTTP 错误
dotnet-http GET httpbin.org/status/404  # 显示 404 Not Found
dotnet-http GET httpbin.org/status/500  # 显示 500 Internal Server Error
```

### 调试错误

```bash
# 使用调试模式查看详细错误信息
dotnet-http GET api.example.com/broken --debug

# 先检查请求格式
dotnet-http POST api.example.com/users invalid-data --offline
```

## 使用技巧与最佳实践

### 1. 使用环境变量

```bash
# 将 API 令牌存储在环境变量中
export API_TOKEN="your-secret-token"
dotnet-http GET api.example.com/protected Authorization:"Bearer $API_TOKEN"

# 存储基础 URL
export API_BASE="https://api.example.com"
dotnet-http GET "$API_BASE/users"
```

### 2. 对特殊字符加引号

```bash
# 对包含空格或特殊字符的值加引号
dotnet-http POST httpbin.org/post message="Hello, world!" tags:='["tag with spaces", "special!chars"]'
```

### 3. 大型数据使用文件

```bash
# 使用文件代替冗长的命令行
cat > user.json << EOF
{
  "name": "John Doe",
  "email": "john@example.com",
  "address": {
    "street": "123 Main St",
    "city": "Seattle",
    "state": "WA",
    "zip": "98101"
  }
}
EOF

dotnet-http POST api.example.com/users @user.json
```

### 4. 与其他工具组合使用

```bash
# 使用 jq 提取特定数据
USER_ID=$(dotnet-http POST api.example.com/users name=John --body | jq -r '.id')
dotnet-http GET "api.example.com/users/$USER_ID"

# 格式化 JSON 输出
dotnet-http GET api.example.com/users | jq .

# 保存并处理响应
dotnet-http GET api.example.com/users --body > users.json
jq '.users[] | select(.active == true)' users.json
```

### 5. 逐步测试

```bash
# 从简单请求开始
dotnet-http GET api.example.com/health

# 添加认证
dotnet-http GET api.example.com/protected Authorization:"Bearer $TOKEN"

# 逐步添加数据
dotnet-http POST api.example.com/users name=John
dotnet-http POST api.example.com/users name=John email=john@example.com
dotnet-http POST api.example.com/users name=John email=john@example.com age:=30
```

## 常见使用场景

### 开发工作流

```bash
# 1. 检查 API 是否运行
dotnet-http GET localhost:3000/health

# 2. 测试认证
dotnet-http POST localhost:3000/auth/login username=admin password=password

# 3. 测试 CRUD 操作
dotnet-http GET localhost:3000/api/users
dotnet-http POST localhost:3000/api/users name=Test email=test@example.com
dotnet-http PUT localhost:3000/api/users/1 name="Updated Name"
dotnet-http DELETE localhost:3000/api/users/1
```

### API 探索

```bash
# 发现 API 端点
dotnet-http OPTIONS api.example.com

# 检查 API 文档端点
dotnet-http GET api.example.com/docs

# 测试不同响应格式
dotnet-http GET api.example.com/users Accept:"application/json"
dotnet-http GET api.example.com/users Accept:"application/xml"
```

### 集成测试

```bash
# 测试服务依赖
dotnet-http GET auth-service.internal/health
dotnet-http GET user-service.internal/health
dotnet-http GET order-service.internal/health

# 测试跨服务通信
TOKEN=$(dotnet-http POST auth-service.internal/token client_id=test --body | jq -r '.access_token')
dotnet-http GET user-service.internal/profile Authorization:"Bearer $TOKEN"
```

## 下一步

- 了解[高级请求数据类型](request-data-types.zh.md)
- 探索[身份认证方法](authentication.zh.md)
- 尝试[文件执行](file-execution.zh.md)处理复杂工作流
- 查看[常见使用场景](examples/common-use-cases.zh.md)中的实际案例
- 遇到问题时使用[调试技术](debugging.zh.md)
