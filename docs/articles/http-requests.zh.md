# HTTP 请求

> 📖 [View English Documentation](http-requests.md)

本指南介绍如何使用 dotnet-httpie 发送各种类型的 HTTP 请求。

## HTTP 方法

dotnet-httpie 支持所有标准 HTTP 方法。若未指定方法，默认使用 GET。

### GET 请求

```bash
# 简单 GET
dotnet-http httpbin.org/get

# 带查询参数的 GET
dotnet-http httpbin.org/get name==John age==30

# 带请求头的 GET
dotnet-http httpbin.org/get Authorization:"Bearer token"
```

### POST 请求

```bash
# JSON POST 请求
dotnet-http POST httpbin.org/post name=John email=john@example.com

# 表单数据 POST 请求  
dotnet-http POST httpbin.org/post --form name=John email=john@example.com

# 原始数据 POST 请求
dotnet-http POST httpbin.org/post --raw "Custom raw data"
```

### PUT 请求

```bash
# 更新资源
dotnet-http PUT api.example.com/users/123 name="John Smith" email="john.smith@example.com"

# 替换整个资源
dotnet-http PUT api.example.com/users/123 @user.json
```

### PATCH 请求

```bash
# 部分更新
dotnet-http PATCH api.example.com/users/123 email="newemail@example.com"
```

### DELETE 请求

```bash
# 删除资源
dotnet-http DELETE api.example.com/users/123

# 带确认头的删除
dotnet-http DELETE api.example.com/users/123 X-Confirm:"yes"
```

### HEAD 请求

```bash
# 仅获取响应头
dotnet-http HEAD httpbin.org/get
```

### OPTIONS 请求

```bash
# 查询允许的方法
dotnet-http OPTIONS api.example.com/users
```

## 请求 URL

### 完整 URL

```bash
dotnet-http https://api.example.com/users
dotnet-http http://localhost:3000/api/data
```

### 简短本地 URL

```bash
# 等同于 http://localhost:PORT
dotnet-http :3000/api/users
dotnet-http localhost:3000/api/users
```

### 带参数的 URL

```bash
# 查询参数会自动附加到 URL
dotnet-http api.example.com/search q==httpie type==tool
# 结果：api.example.com/search?q=httpie&type=tool
```

## 请求头

### 标准请求头

```bash
# 授权头
dotnet-http api.example.com/protected Authorization:"Bearer jwt-token"

# Content-Type
dotnet-http POST api.example.com/data Content-Type:"application/xml" @data.xml

# User-Agent
dotnet-http api.example.com/get User-Agent:"MyApp/1.0"

# Accept
dotnet-http api.example.com/data Accept:"application/json"
```

### 自定义请求头

```bash
# API 密钥
dotnet-http api.example.com/data X-API-Key:"your-api-key"

# 自定义头
dotnet-http api.example.com/data X-Custom-Header:"custom-value"
```

### 多个请求头

```bash
dotnet-http api.example.com/data \
  Authorization:"Bearer token" \
  X-API-Key:"api-key" \
  User-Agent:"MyApp/1.0" \
  Accept:"application/json"
```

## 请求体

### JSON 请求体（默认）

```bash
# 简单 JSON
dotnet-http POST api.example.com/users name=John age:=30 active:=true

# 嵌套 JSON
dotnet-http POST api.example.com/users name=John address[city]=Seattle address[country]=USA

# 数组值
dotnet-http POST api.example.com/users name=John tags:='["developer", "dotnet"]'

# 原始 JSON 对象
dotnet-http POST api.example.com/users profile:='{"name": "John", "age": 30}'
```

### 表单数据

```bash
# URL 编码的表单数据
dotnet-http POST httpbin.org/post --form name=John email=john@example.com
```

### 原始数据

```bash
# 发送原始字符串
dotnet-http POST api.example.com/webhook --raw "Raw webhook payload"

# 从标准输入读取
echo "data" | dotnet-http POST api.example.com/data
```

## 复杂 JSON 结构

### 嵌套对象

```bash
dotnet-http POST api.example.com/users \
  name=John \
  address[street]="123 Main St" \
  address[city]=Seattle \
  address[zipcode]:=98101
```

### 数组

```bash
# 字符串数组
dotnet-http POST api.example.com/users name=John skills:='["C#", "JavaScript", "Python"]'

# 数字数组
dotnet-http POST api.example.com/data values:='[1, 2, 3, 4, 5]'

# 对象数组
dotnet-http POST api.example.com/batch items:='[{"id": 1, "name": "Item 1"}, {"id": 2, "name": "Item 2"}]'
```

### 布尔值与 null 值

```bash
# 布尔值
dotnet-http POST api.example.com/users name=John active:=true verified:=false

# null 值
dotnet-http POST api.example.com/users name=John middle_name:=null

# 数字
dotnet-http POST api.example.com/users name=John age:=30 salary:=50000.50
```

## 响应处理

### 查看完整响应

```bash
# 默认：同时显示响应头和响应体
dotnet-http httpbin.org/get
```

### 仅显示响应体

```bash
# 仅显示响应体
dotnet-http httpbin.org/get --body
```

### 仅显示响应头

```bash
# 仅显示响应头
dotnet-http HEAD httpbin.org/get
```

### 保存响应

```bash
# 保存到文件
dotnet-http httpbin.org/get > response.json

# 下载文件
dotnet-http httpbin.org/image/png --download
```

## 错误处理

### HTTP 错误码

```bash
# dotnet-httpie 会清晰地显示 HTTP 错误
dotnet-http httpbin.org/status/404
dotnet-http httpbin.org/status/500
```

### 超时配置

```bash
# 设置请求超时（如中间件支持）
dotnet-http api.example.com/slow-endpoint --timeout 30
```

## 高级功能

### 跟随重定向

```bash
# 自动跟随重定向
dotnet-http httpbin.org/redirect/3 --follow
```

### 忽略 SSL 错误

```bash
# 仅限开发/测试环境使用
dotnet-http https://self-signed.badssl.com/ --verify=no
```

### 代理支持

```bash
# 使用代理
dotnet-http httpbin.org/get --proxy http://proxy.example.com:8080
```

## 真实 API 使用示例

### GitHub API

```bash
# 获取用户信息
dotnet-http api.github.com/users/octocat

# 获取仓库列表（需要认证）
dotnet-http api.github.com/user/repos Authorization:"token your-token"

# 创建 Issue
dotnet-http POST api.github.com/repos/owner/repo/issues \
  Authorization:"token your-token" \
  title="Bug report" \
  body="Description of the bug"
```

### REST API CRUD 操作

```bash
# 创建
dotnet-http POST api.example.com/articles \
  title="My Article" \
  content="Article content" \
  published:=false

# 读取
dotnet-http GET api.example.com/articles/123

# 更新
dotnet-http PUT api.example.com/articles/123 \
  title="Updated Article" \
  published:=true

# 删除
dotnet-http DELETE api.example.com/articles/123
```

## 最佳实践

1. **使用有意义的名称**命名请求文件和变量
2. 将 API 密钥等**敏感数据存储在环境变量中**
3. 发送前使用 **--offline 模式**预览请求
4. **与 jq 组合**处理 JSON：`dotnet-http api.example.com/data | jq .`
5. 对于复杂数据，**使用文件**而非内联 JSON，以提高可维护性

## 下一步

- 了解[身份认证方法](authentication.zh.md)
- 探索[文件执行](file-execution.zh.md)处理可重复请求
