# 请求数据类型

> 📖 [View English Documentation](request-data-types.md)

本指南介绍使用 dotnet-httpie 构造和发送数据的不同方式。

## 概述

dotnet-httpie 支持多种请求数据格式，并为不同数据类型提供了直观的语法：

- **查询参数**：`name==value`
- **请求头**：`name:value`
- **JSON 字段**：`name=value`
- **原始 JSON 值**：`name:=value`

## 查询参数

查询参数追加到 URL 后，使用 `==` 语法。

### 基本查询参数

```bash
# 单个参数
dotnet-http httpbin.org/get search==httpie

# 多个参数
dotnet-http httpbin.org/get search==httpie lang==en page==1

# URL 编码自动处理
dotnet-http httpbin.org/get query=="hello world" special=="chars!@#"
```

### 查询参数中的数组

```bash
# 同一参数的多个值
dotnet-http httpbin.org/get tags==javascript tags==web tags==api

# 结果：?tags=javascript&tags=web&tags=api
```

### 空值

```bash
# 空参数
dotnet-http httpbin.org/get empty==

# null 参数（被省略）
dotnet-http httpbin.org/get param==null
```

## HTTP 请求头

请求头使用 `:` 语法，用于控制请求的处理方式。

### 常用请求头

```bash
# 授权
dotnet-http httpbin.org/headers Authorization:"Bearer jwt-token"

# Content-Type
dotnet-http POST httpbin.org/post Content-Type:"application/xml"

# User-Agent
dotnet-http httpbin.org/headers User-Agent:"MyApp/1.0"

# Accept
dotnet-http httpbin.org/headers Accept:"application/json, text/plain"

# API 密钥
dotnet-http api.example.com/data X-API-Key:"your-api-key"
```

### 自定义请求头

```bash
# 多个自定义请求头
dotnet-http api.example.com/webhook \
  X-Webhook-Source:"github" \
  X-Signature:"sha256=signature" \
  X-Event-Type:"push"
```

### 含空格的请求头值

```bash
# 对包含空格的值加引号
dotnet-http httpbin.org/headers User-Agent:"My Application v1.0"
```

## JSON 请求体

### 简单 JSON 字段

使用 `=` 可自动创建 JSON 字段：

```bash
dotnet-http POST httpbin.org/post name=John email=john@example.com

# 生成：
# {
#   "name": "John",
#   "email": "john@example.com"
# }
```

### 数据类型

#### 字符串（默认）

```bash
dotnet-http POST httpbin.org/post title="Hello World" description="A test post"
```

#### 数字

```bash
# 整数
dotnet-http POST httpbin.org/post age:=30 count:=100

# 浮点数
dotnet-http POST httpbin.org/post price:=19.99 rating:=4.5
```

#### 布尔值

```bash
dotnet-http POST httpbin.org/post active:=true verified:=false published:=true
```

#### null 值

```bash
dotnet-http POST httpbin.org/post middle_name:=null optional_field:=null
```

#### 数组

```bash
# 字符串数组
dotnet-http POST httpbin.org/post tags:='["javascript", "web", "api"]'

# 数字数组
dotnet-http POST httpbin.org/post scores:='[95, 87, 92, 78]'

# 对象数组
dotnet-http POST httpbin.org/post items:='[{"id": 1, "name": "Item 1"}, {"id": 2, "name": "Item 2"}]'
```

#### 对象

```bash
# 嵌套对象
dotnet-http POST httpbin.org/post profile:='{"name": "John", "age": 30, "skills": ["C#", "JavaScript"]}'

# 复杂嵌套结构
dotnet-http POST httpbin.org/post config:='{"database": {"host": "localhost", "port": 5432}, "features": {"auth": true, "cache": false}}'
```

## 嵌套 JSON 结构

### 方括号表示法

```bash
# 使用方括号表示法表示嵌套对象
dotnet-http POST httpbin.org/post \
  user[name]=John \
  user[email]=john@example.com \
  user[address][street]="123 Main St" \
  user[address][city]=Seattle \
  user[address][zipcode]:=98101

# 生成：
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

### 数组元素

```bash
# 带索引的数组元素
dotnet-http POST httpbin.org/post \
  items[0][name]=First \
  items[0][value]:=100 \
  items[1][name]=Second \
  items[1][value]:=200

# 生成：
# {
#   "items": [
#     {"name": "First", "value": 100},
#     {"name": "Second", "value": 200}
#   ]
# }
```

## 表单数据

### URL 编码表单

```bash
# 使用 --form 标志发送 application/x-www-form-urlencoded 数据
dotnet-http POST httpbin.org/post --form name=John email=john@example.com

# 与其他选项混合使用
dotnet-http POST httpbin.org/post --form \
  name=John \
  age=30 \
  Authorization:"Bearer token"
```

### 多部分表单

```bash
# 使用 --multipart 发送 multipart/form-data
dotnet-http POST httpbin.org/post --multipart \
  name=John \
  file@/path/to/document.pdf

# 多个文件
dotnet-http POST httpbin.org/post --multipart \
  name=John \
  avatar@/path/to/avatar.jpg \
  resume@/path/to/resume.pdf
```

## 文件上传

### 将文件作为请求体发送

```bash
# 将整个文件作为请求体发送
dotnet-http POST api.example.com/upload @/path/to/data.json

# 带 Content-Type
dotnet-http POST api.example.com/upload \
  Content-Type:"application/json" \
  @/path/to/data.json
```

### 多部分表单中的文件

```bash
# 文件作为表单字段
dotnet-http POST api.example.com/upload --multipart \
  description="My document" \
  file@/path/to/document.pdf
```

### 多个文件

```bash
dotnet-http POST api.example.com/batch-upload --multipart \
  batch_name="Document Batch" \
  doc1@/path/to/file1.pdf \
  doc2@/path/to/file2.pdf \
  metadata@/path/to/metadata.json
```

## 原始数据

### 原始字符串数据

```bash
# 发送原始文本
dotnet-http POST api.example.com/webhook \
  Content-Type:"text/plain" \
  --raw "This is raw text data"

# 原始 JSON（字段语法的替代方式）
dotnet-http POST api.example.com/data \
  Content-Type:"application/json" \
  --raw '{"name": "John", "age": 30}'

# 原始 XML
dotnet-http POST api.example.com/xml \
  Content-Type:"application/xml" \
  --raw '<user><name>John</name><age>30</age></user>'
```

### 从标准输入读取数据

```bash
# 从命令管道数据
echo '{"message": "Hello"}' | dotnet-http POST api.example.com/data

# 从文件通过标准输入传入
cat data.json | dotnet-http POST api.example.com/upload

# 从其他工具传入
curl -s api.example.com/export | dotnet-http POST api.example.com/import
```

## 数据类型示例

### 电商 API

```bash
# 创建商品
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

### 用户注册

```bash
# 复杂用户对象
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

### API 配置

```bash
# 配置更新
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

## Content-Type 处理

### 自动设置 Content-Type

```bash
# JSON（字段语法默认值）
dotnet-http POST api.example.com/data name=John
# Content-Type: application/json

# 表单数据
dotnet-http POST api.example.com/data --form name=John
# Content-Type: application/x-www-form-urlencoded

# 多部分
dotnet-http POST api.example.com/data --multipart name=John file@data.txt
# Content-Type: multipart/form-data
```

### 手动指定 Content-Type

```bash
# 覆盖 Content-Type
dotnet-http POST api.example.com/data \
  Content-Type:"application/vnd.api+json" \
  name=John age:=30

# XML 内容
dotnet-http POST api.example.com/data \
  Content-Type:"application/xml" \
  @data.xml
```

## 高级数据处理

### 条件字段

```bash
# 仅在有值时包含字段
dotnet-http POST api.example.com/users \
  name=John \
  email=john@example.com \
  $([ "$PHONE" ] && echo "phone=$PHONE") \
  $([ "$COMPANY" ] && echo "company=$COMPANY")
```

### 动态值

```bash
# 使用命令替换
dotnet-http POST api.example.com/events \
  timestamp:=$(date +%s) \
  uuid="$(uuidgen)" \
  hostname="$(hostname)"
```

### 环境变量

```bash
# 引用环境变量
dotnet-http POST api.example.com/deploy \
  Authorization:"Bearer $API_TOKEN" \
  version="$BUILD_VERSION" \
  environment="$DEPLOY_ENV"
```

## 验证与测试

### Schema 验证

```bash
# 根据 schema 验证响应
dotnet-http POST api.example.com/users \
  name=John \
  email=john@example.com \
  --schema user-schema.json
```

### 响应测试

```bash
# 测试响应中的特定字段
dotnet-http POST api.example.com/users name=John | jq '.id != null'

# 与 Shell 脚本结合使用
response=$(dotnet-http POST api.example.com/users name=John --body)
user_id=$(echo $response | jq -r '.id')
dotnet-http GET api.example.com/users/$user_id
```

## 最佳实践

1. **使用合适的数据类型**——数字用 `:=123`，布尔值用 `:=true`
2. **对复杂值加引号**——尤其是 JSON 对象和数组
3. **命名保持一致**——统一使用 snake_case 或 camelCase
4. **验证数据结构**——使用 `--offline` 预览请求
5. **大型数据使用文件**——避免命令行过长
6. **注意安全**——不要将敏感数据写入命令历史
7. **使用环境变量**——存储令牌和配置信息
8. **逐步测试**——从简单开始，逐渐增加复杂度

## 下一步

- 了解[身份认证方法](authentication.zh.md)
- 探索[文件执行](file-execution.zh.md)处理复杂数据场景
- 参阅[示例](examples/common-use-cases.zh.md)了解真实使用场景
