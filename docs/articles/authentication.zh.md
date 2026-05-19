# 身份认证

> 📖 [View English Documentation](authentication.md)

本指南介绍 dotnet-httpie 支持的各种身份认证方法，用于保护您的 API 请求。

## 概述

dotnet-httpie 支持现代 API 中常用的所有认证方式：

- Bearer 令牌（JWT）
- API 密钥认证
- 基本认证（Basic Auth）
- 自定义请求头认证
- OAuth 2.0 流程
- 基于 Cookie 的认证

## Bearer 令牌认证

最常用于现代 API 中的 JWT 令牌认证。

### 基于请求头的 Bearer 令牌

```bash
# 标准 Bearer 令牌
dotnet-http GET api.example.com/protected \
  Authorization:"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

# 从环境变量读取令牌
dotnet-http GET api.example.com/protected \
  Authorization:"Bearer $JWT_TOKEN"
```

### 获取 JWT 令牌

```bash
# 登录获取 JWT 令牌
LOGIN_RESPONSE=$(dotnet-http POST api.example.com/auth/login \
  username="admin" \
  password="password" \
  --body)

# 使用 jq 提取令牌
TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.access_token')

# 在受保护请求中使用令牌
dotnet-http GET api.example.com/users \
  Authorization:"Bearer $TOKEN"
```

### 刷新令牌流程

```bash
# 使用刷新令牌获取新访问令牌
REFRESH_RESPONSE=$(dotnet-http POST api.example.com/auth/refresh \
  refresh_token="$REFRESH_TOKEN" \
  --body)

NEW_TOKEN=$(echo $REFRESH_RESPONSE | jq -r '.access_token')

# 使用新令牌
dotnet-http GET api.example.com/protected \
  Authorization:"Bearer $NEW_TOKEN"
```

## API 密钥认证

服务间通信中常见的 REST API 认证方式。

### 基于请求头的 API 密钥

```bash
# 标准 API 密钥请求头
dotnet-http GET api.example.com/data \
  X-API-Key:"your-api-key-here"

# 自定义请求头名称
dotnet-http GET api.example.com/data \
  X-RapidAPI-Key:"your-rapidapi-key" \
  X-RapidAPI-Host:"api.example.com"

# 多个 API 密钥
dotnet-http GET api.example.com/data \
  X-API-Key:"primary-key" \
  X-Secondary-Key:"secondary-key"
```

### 查询参数中的 API 密钥

```bash
# API 密钥作为查询参数
dotnet-http GET api.example.com/data \
  api_key==your-api-key

# 多个参数
dotnet-http GET api.example.com/data \
  key==your-api-key \
  format==json \
  version==v2
```

### 基于环境变量的 API 密钥

```bash
# 将 API 密钥存储在环境变量中
export API_KEY="your-secret-api-key"

# 在请求中使用
dotnet-http GET api.example.com/data \
  X-API-Key:"$API_KEY"
```

## 基本认证

传统的用户名/密码认证方式。

### 手动基本认证

```bash
# 手动编码凭据
CREDENTIALS=$(echo -n 'username:password' | base64)
dotnet-http GET api.example.com/secure \
  Authorization:"Basic $CREDENTIALS"

# 直接编码
dotnet-http GET api.example.com/secure \
  Authorization:"Basic $(echo -n 'admin:secret123' | base64)"
```

### HTTPie 风格基本认证

```bash
# 使用 --auth 标志（username:password 格式）
dotnet-http GET api.example.com/secure \
  --auth username:password

# 显式指定认证类型
dotnet-http GET api.example.com/secure \
  --auth-type Basic --auth username:password
```

## OAuth 2.0 流程

### 客户端凭据流程

```bash
# 获取访问令牌
TOKEN_RESPONSE=$(dotnet-http POST oauth.provider.com/token \
  grant_type="client_credentials" \
  client_id="your-client-id" \
  client_secret="your-client-secret" \
  scope="read write" \
  --body)

ACCESS_TOKEN=$(echo $TOKEN_RESPONSE | jq -r '.access_token')

# 使用访问令牌
dotnet-http GET api.example.com/protected \
  Authorization:"Bearer $ACCESS_TOKEN"
```

### 授权码流程（手动）

```bash
# 步骤 1：获取授权码（需手动在浏览器中操作）
echo "访问：https://oauth.provider.com/authorize?client_id=your-client-id&response_type=code&redirect_uri=http://localhost:8080/callback&scope=read"

# 步骤 2：用授权码换取令牌
TOKEN_RESPONSE=$(dotnet-http POST oauth.provider.com/token \
  grant_type="authorization_code" \
  client_id="your-client-id" \
  client_secret="your-client-secret" \
  code="authorization-code-from-callback" \
  redirect_uri="http://localhost:8080/callback" \
  --body)

ACCESS_TOKEN=$(echo $TOKEN_RESPONSE | jq -r '.access_token')
```

### 资源所有者密码凭据

```bash
# 直接用户名/密码换取令牌（安全性较低）
TOKEN_RESPONSE=$(dotnet-http POST oauth.provider.com/token \
  grant_type="password" \
  username="user@example.com" \
  password="user-password" \
  client_id="your-client-id" \
  client_secret="your-client-secret" \
  --body)

ACCESS_TOKEN=$(echo $TOKEN_RESPONSE | jq -r '.access_token')
```

## 自定义认证方案

### 基于签名的认证

```bash
# AWS 风格签名
SIGNATURE=$(echo -n "GET\n/api/data\n$(date -u)" | openssl dgst -sha256 -hmac "$SECRET_KEY" -binary | base64)

dotnet-http GET api.example.com/data \
  Authorization:"AWS4-HMAC-SHA256 Credential=$ACCESS_KEY/$(date -u +%Y%m%d)/us-east-1/service/aws4_request, SignedHeaders=host;x-amz-date, Signature=$SIGNATURE" \
  X-Amz-Date:"$(date -u +%Y%m%dT%H%M%SZ)"
```

### HMAC 认证

```bash
# 生成 HMAC 签名
TIMESTAMP=$(date +%s)
PAYLOAD="GET/api/data$TIMESTAMP"
SIGNATURE=$(echo -n "$PAYLOAD" | openssl dgst -sha256 -hmac "$SECRET_KEY" -binary | base64)

dotnet-http GET api.example.com/data \
  X-API-Key:"$API_KEY" \
  X-Timestamp:"$TIMESTAMP" \
  X-Signature:"$SIGNATURE"
```

### 摘要认证

```bash
# 简单摘要实现（仅供演示）
NONCE=$(openssl rand -hex 16)
HASH=$(echo -n "username:realm:password" | md5sum | cut -d' ' -f1)
RESPONSE=$(echo -n "$HASH:$NONCE:GET:/api/data" | md5sum | cut -d' ' -f1)

dotnet-http GET api.example.com/data \
  Authorization:"Digest username=\"username\", realm=\"realm\", nonce=\"$NONCE\", uri=\"/api/data\", response=\"$RESPONSE\""
```

## 基于 Cookie 的认证

### 会话 Cookie

```bash
# 登录并保存 Cookie
dotnet-http POST api.example.com/login \
  username="admin" \
  password="password" \
  --session=api-session

# 在后续请求中使用已保存的会话
dotnet-http GET api.example.com/protected \
  --session=api-session
```

### 手动处理 Cookie

```bash
# 手动设置 Cookie
dotnet-http GET api.example.com/protected \
  Cookie:"sessionid=abc123; csrftoken=xyz789"

# 多个 Cookie
dotnet-http GET api.example.com/protected \
  Cookie:"auth_token=token123; user_pref=dark_mode; lang=en"
```

## 多因素认证

### TOTP（基于时间的一次性密码）

```bash
# 使用外部工具生成 TOTP 码
TOTP_CODE=$(oathtool --totp --base32 "$TOTP_SECRET")

# 在请求中包含 TOTP 码
dotnet-http POST api.example.com/sensitive-action \
  Authorization:"Bearer $TOKEN" \
  X-TOTP-Code:"$TOTP_CODE" \
  action="transfer" \
  amount:=1000
```

### 短信/邮件验证

```bash
# 请求验证码
dotnet-http POST api.example.com/request-verification \
  Authorization:"Bearer $TOKEN" \
  phone="+1-555-0123"

# 提交验证码
dotnet-http POST api.example.com/verify \
  Authorization:"Bearer $TOKEN" \
  verification_code="123456" \
  action="sensitive-operation"
```

## HTTP 文件中的认证

### 环境变量

```http
# auth-example.http
@baseUrl = https://api.example.com
@token = {{$env JWT_TOKEN}}
@apiKey = {{$env API_KEY}}

###

# 从环境变量读取 Bearer 令牌
GET {{baseUrl}}/protected
Authorization: Bearer {{token}}

###

# 从环境变量读取 API 密钥
GET {{baseUrl}}/data
X-API-Key: {{apiKey}}
```

### HTTP 文件中的登录流程

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

## 安全最佳实践

### 环境变量

```bash
# 切勿在命令或文件中硬编码密钥
# 请使用环境变量代替

# 不推荐
dotnet-http GET api.example.com/data X-API-Key:"secret-key-123"

# 推荐
export API_KEY="secret-key-123"
dotnet-http GET api.example.com/data X-API-Key:"$API_KEY"
```

### 安全存储

```bash
# 使用安全凭据存储
# 以 macOS Keychain 为例
security add-internet-password -s "api.example.com" -a "myapp" -w "secret-api-key"
API_KEY=$(security find-internet-password -s "api.example.com" -a "myapp" -w)

dotnet-http GET api.example.com/data X-API-Key:"$API_KEY"
```

### 令牌轮换

```bash
#!/bin/bash
# token-rotation.sh

# 检查令牌是否过期
if ! dotnet-http GET api.example.com/verify Authorization:"Bearer $ACCESS_TOKEN" >/dev/null 2>&1; then
  echo "令牌已过期，正在刷新..."
  
  # 刷新令牌
  NEW_TOKEN_RESPONSE=$(dotnet-http POST api.example.com/auth/refresh \
    refresh_token="$REFRESH_TOKEN" --body)
  
  ACCESS_TOKEN=$(echo $NEW_TOKEN_RESPONSE | jq -r '.access_token')
  export ACCESS_TOKEN
  
  echo "令牌刷新成功"
fi

# 使用当前令牌
dotnet-http GET api.example.com/protected Authorization:"Bearer $ACCESS_TOKEN"
```

## 各平台示例

### GitHub API

```bash
# 个人访问令牌
dotnet-http GET api.github.com/user \
  Authorization:"token $GITHUB_TOKEN"

# 创建仓库
dotnet-http POST api.github.com/user/repos \
  Authorization:"token $GITHUB_TOKEN" \
  name="new-repo" \
  description="Created via dotnet-httpie"
```

### AWS API

```bash
# AWS 签名版本 4（简化）
AWS_ACCESS_KEY="your-access-key"
AWS_SECRET_KEY="your-secret-key"
AWS_REGION="us-east-1"
SERVICE="s3"

# 注意：完整的 AWS sig v4 实现会更复杂
dotnet-http GET s3.amazonaws.com/bucket-name \
  Authorization:"AWS4-HMAC-SHA256 ..." \
  X-Amz-Date:"$(date -u +%Y%m%dT%H%M%SZ)"
```

### Google APIs

```bash
# 通过浏览器流程获取 OAuth 令牌
# 获取访问令牌后使用

dotnet-http GET www.googleapis.com/oauth2/v1/userinfo \
  Authorization:"Bearer $GOOGLE_ACCESS_TOKEN"

# 服务账号认证（使用 JWT）
dotnet-http GET www.googleapis.com/storage/v1/b \
  Authorization:"Bearer $SERVICE_ACCOUNT_JWT"
```

## 认证测试

### 验证令牌有效性

```bash
# 测试令牌是否有效
if dotnet-http GET api.example.com/verify Authorization:"Bearer $TOKEN" --check-status; then
  echo "令牌有效"
else
  echo "令牌无效或已过期"
fi
```

### 认证调试

```bash
# 调试认证问题
dotnet-http GET api.example.com/protected \
  Authorization:"Bearer $TOKEN" \
  --debug \
  --offline  # 先预览请求
```

### 自动化认证测试

```bash
#!/bin/bash
# auth-test.sh

# 测试各种认证方式
echo "测试认证方式..."

# 测试 API 密钥
if dotnet-http GET api.example.com/test X-API-Key:"$API_KEY" --check-status; then
  echo "✓ API 密钥认证成功"
else
  echo "✗ API 密钥认证失败"
fi

# 测试 JWT 令牌
if dotnet-http GET api.example.com/test Authorization:"Bearer $JWT_TOKEN" --check-status; then
  echo "✓ JWT 认证成功"
else
  echo "✗ JWT 认证失败"
fi

# 测试基本认证
if dotnet-http GET api.example.com/test Authorization:"Basic $BASIC_AUTH" --check-status; then
  echo "✓ 基本认证成功"
else
  echo "✗ 基本认证失败"
fi
```

## 下一步

- 了解[请求数据类型](request-data-types.zh.md)以发送认证请求
- 探索[文件执行](file-execution.zh.md)管理 HTTP 文件中的认证
- 参阅[示例](examples/common-use-cases.zh.md)了解真实认证模式
