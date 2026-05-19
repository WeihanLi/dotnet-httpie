# 常见使用场景

> 📖 [View English Documentation](common-use-cases.md)

本指南提供 dotnet-httpie 最常见使用场景的实际示例。

## API 开发与测试

### REST API CRUD 操作

```bash
# 用户 API 示例
BASE_URL="https://api.example.com"
TOKEN="your-jwt-token"

# 创建用户
dotnet-http POST $BASE_URL/users \
  Authorization:"Bearer $TOKEN" \
  Content-Type:"application/json" \
  name="John Doe" \
  email="john@example.com" \
  role="user"

# 获取所有用户
dotnet-http GET $BASE_URL/users \
  Authorization:"Bearer $TOKEN"

# 获取指定用户
dotnet-http GET $BASE_URL/users/123 \
  Authorization:"Bearer $TOKEN"

# 更新用户
dotnet-http PUT $BASE_URL/users/123 \
  Authorization:"Bearer $TOKEN" \
  name="John Smith" \
  email="john.smith@example.com"

# 部分更新
dotnet-http PATCH $BASE_URL/users/123 \
  Authorization:"Bearer $TOKEN" \
  email="newemail@example.com"

# 删除用户
dotnet-http DELETE $BASE_URL/users/123 \
  Authorization:"Bearer $TOKEN"
```

### GraphQL API

```bash
# GraphQL 查询
dotnet-http POST https://api.github.com/graphql \
  Authorization:"Bearer $GITHUB_TOKEN" \
  query='query { viewer { login name } }'

# GraphQL 变更
dotnet-http POST https://api.github.com/graphql \
  Authorization:"Bearer $GITHUB_TOKEN" \
  query='mutation { createIssue(input: {repositoryId: "repo-id", title: "Bug report", body: "Description"}) { issue { id title } } }'
```

## 身份认证模式

### JWT 认证

```bash
# 登录获取令牌
LOGIN_RESPONSE=$(dotnet-http POST api.example.com/auth/login \
  username="admin" \
  password="password" \
  --body)

TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.token')

# 在受保护请求中使用令牌
dotnet-http GET api.example.com/protected \
  Authorization:"Bearer $TOKEN"
```

### API 密钥认证

```bash
# 请求头中的 API 密钥
dotnet-http GET api.example.com/data \
  X-API-Key:"your-api-key"

# 查询参数中的 API 密钥
dotnet-http GET api.example.com/data \
  api_key==your-api-key
```

### 基本认证

```bash
# 基本认证
dotnet-http GET api.example.com/secure \
  Authorization:"Basic $(echo -n 'username:password' | base64)"

# 或使用 HTTPie 风格认证
dotnet-http GET api.example.com/secure \
  --auth username:password
```

### OAuth 2.0

```bash
# 获取访问令牌
TOKEN_RESPONSE=$(dotnet-http POST oauth.example.com/token \
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

## 文件操作

### 文件上传

```bash
# 单文件上传
dotnet-http POST api.example.com/upload \
  Authorization:"Bearer $TOKEN" \
  --multipart \
  file@/path/to/document.pdf \
  description="Important document"

# 多文件上传
dotnet-http POST api.example.com/batch-upload \
  --multipart \
  doc1@/path/to/file1.pdf \
  doc2@/path/to/file2.pdf \
  metadata@/path/to/metadata.json

# 带元数据的图片上传
dotnet-http POST api.example.com/images \
  --multipart \
  image@/path/to/photo.jpg \
  title="Vacation Photo" \
  tags:='["vacation", "travel", "beach"]' \
  public:=true
```

### 文件下载

```bash
# 下载文件
dotnet-http GET api.example.com/files/document.pdf \
  Authorization:"Bearer $TOKEN" \
  --download

# 以自定义文件名下载
dotnet-http GET api.example.com/exports/data.csv \
  --download \
  --output "$(date +%Y%m%d)-export.csv"

# 下载大文件并显示进度
dotnet-http GET api.example.com/large-file.zip \
  --download \
  --progress
```

## 数据处理

### 与 jq 结合处理 JSON

```bash
# 提取特定字段
USER_ID=$(dotnet-http POST api.example.com/users name="John" --body | jq -r '.id')

# 过滤数组
dotnet-http GET api.example.com/users | jq '.users[] | select(.active == true)'

# 转换数据
dotnet-http GET api.example.com/users | jq '.users | map({id, name, email})'

# 统计结果数量
COUNT=$(dotnet-http GET api.example.com/users | jq '.users | length')
echo "总用户数：$COUNT"
```

### 分页处理

```bash
# 获取所有页面的数据
page=1
all_data="[]"

while true; do
  response=$(dotnet-http GET api.example.com/users page==$page limit==50 --body)
  data=$(echo $response | jq '.data')
  
  if [ "$(echo $data | jq 'length')" -eq 0 ]; then
    break
  fi
  
  all_data=$(echo $all_data $data | jq -s 'add')
  ((page++))
done

echo $all_data | jq .
```

## CI/CD 集成

### 健康检查

```bash
#!/bin/bash
# health-check.sh

check_service() {
  local service_url=$1
  local service_name=$2
  
  echo "检查 $service_name..."
  
  if dotnet-http GET $service_url/health --check-status; then
    echo "✓ $service_name 健康"
    return 0
  else
    echo "✗ $service_name 不健康"
    return 1
  fi
}

# 检查多个服务
check_service "https://api.example.com" "API 服务"
check_service "https://auth.example.com" "认证服务"
check_service "https://cache.example.com" "缓存服务"
```

### 部署验证

```bash
#!/bin/bash
# verify-deployment.sh

ENVIRONMENT=${1:-staging}
BASE_URL="https://$ENVIRONMENT.api.example.com"

echo "正在验证 $ENVIRONMENT 环境的部署..."

# 检查 API 版本
VERSION=$(dotnet-http GET $BASE_URL/version --body | jq -r '.version')
echo "API 版本：$VERSION"

# 运行冒烟测试
dotnet-http exec tests/smoke-tests.http --env $ENVIRONMENT

# 检查关键端点
dotnet-http GET $BASE_URL/health
dotnet-http GET $BASE_URL/metrics
dotnet-http GET $BASE_URL/ready

echo "部署验证完成！"
```

### 负载测试

```bash
#!/bin/bash
# load-test.sh

URL="https://api.example.com/endpoint"
CONCURRENT=10
REQUESTS=100

echo "正在进行负载测试：$REQUESTS 个请求，$CONCURRENT 个并发用户"

# 创建临时结果文件
RESULTS_FILE=$(mktemp)

# 运行并发请求
for i in $(seq 1 $CONCURRENT); do
  (
    for j in $(seq 1 $((REQUESTS / CONCURRENT))); do
      start_time=$(date +%s%N)
      
      if dotnet-http GET $URL > /dev/null 2>&1; then
        end_time=$(date +%s%N)
        duration=$(((end_time - start_time) / 1000000))
        echo "SUCCESS,$duration" >> $RESULTS_FILE
      else
        echo "FAILURE,0" >> $RESULTS_FILE
      fi
    done
  ) &
done

wait

# 分析结果
total=$(wc -l < $RESULTS_FILE)
success=$(grep "SUCCESS" $RESULTS_FILE | wc -l)
failures=$((total - success))
avg_time=$(grep "SUCCESS" $RESULTS_FILE | cut -d, -f2 | awk '{sum+=$1} END {print sum/NR}')

echo "测试结果："
echo "  总请求数：$total"
echo "  成功：$success"
echo "  失败：$failures"
echo "  成功率：$(( success * 100 / total ))%"
echo "  平均响应时间：${avg_time}ms"

rm $RESULTS_FILE
```

## API 测试工作流

### 端到端测试

```http
# tests/e2e-workflow.http
@baseUrl = https://api.example.com
@contentType = application/json

###

# @name login
POST {{baseUrl}}/auth/login
Content-Type: {{contentType}}

{
  "username": "testuser",
  "password": "testpass"
}

###

# @name createUser
POST {{baseUrl}}/users
Authorization: Bearer {{login.response.body.token}}
Content-Type: {{contentType}}

{
  "name": "Test User",
  "email": "test@example.com",
  "role": "user"
}

###

# @name getUser
GET {{baseUrl}}/users/{{createUser.response.body.id}}
Authorization: Bearer {{login.response.body.token}}

###

# @name updateUser
PUT {{baseUrl}}/users/{{createUser.response.body.id}}
Authorization: Bearer {{login.response.body.token}}
Content-Type: {{contentType}}

{
  "name": "Updated Test User",
  "email": "updated@example.com"
}

###

# @name deleteUser
DELETE {{baseUrl}}/users/{{createUser.response.body.id}}
Authorization: Bearer {{login.response.body.token}}
```

### 契约测试

```bash
#!/bin/bash
# contract-test.sh

echo "正在运行 API 契约测试..."

# 测试必填字段
response=$(dotnet-http POST api.example.com/users name="Test" email="test@example.com" --body)

# 验证响应结构
echo $response | jq -e '.id' > /dev/null || { echo "缺少 id 字段"; exit 1; }
echo $response | jq -e '.name' > /dev/null || { echo "缺少 name 字段"; exit 1; }
echo $response | jq -e '.email' > /dev/null || { echo "缺少 email 字段"; exit 1; }
echo $response | jq -e '.created_at' > /dev/null || { echo "缺少 created_at 字段"; exit 1; }

# 验证数据类型
[ "$(echo $response | jq -r '.id | type')" = "string" ] || { echo "ID 应为字符串类型"; exit 1; }
[ "$(echo $response | jq -r '.name | type')" = "string" ] || { echo "名称应为字符串类型"; exit 1; }

echo "✓ 所有契约测试通过"
```

## 微服务测试

### 服务发现

```bash
#!/bin/bash
# test-microservices.sh

SERVICES=("user-service" "order-service" "payment-service" "notification-service")
BASE_URL="https://api.example.com"

for service in "${SERVICES[@]}"; do
  echo "正在测试 $service..."
  
  # 健康检查
  dotnet-http GET $BASE_URL/$service/health
  
  # 版本检查
  VERSION=$(dotnet-http GET $BASE_URL/$service/version --body | jq -r '.version')
  echo "$service 版本：$VERSION"
  
  # 基本功能测试
  case $service in
    "user-service")
      dotnet-http GET $BASE_URL/users/1
      ;;
    "order-service")
      dotnet-http GET $BASE_URL/orders limit==5
      ;;
    "payment-service")
      dotnet-http GET $BASE_URL/payments/methods
      ;;
    "notification-service")
      dotnet-http GET $BASE_URL/notifications/templates
      ;;
  esac
  
  echo "✓ $service 测试完成"
  echo
done
```

### 跨服务集成

```http
# tests/cross-service.http
@baseUrl = https://api.example.com

###

# @name createUser
POST {{baseUrl}}/users
Content-Type: application/json

{
  "name": "Integration Test User",
  "email": "integration@example.com"
}

###

# @name createOrder
POST {{baseUrl}}/orders
Content-Type: application/json

{
  "user_id": "{{createUser.response.body.id}}",
  "items": [
    {"product_id": "prod-123", "quantity": 2},
    {"product_id": "prod-456", "quantity": 1}
  ]
}

###

# @name processPayment
POST {{baseUrl}}/payments
Content-Type: application/json

{
  "order_id": "{{createOrder.response.body.id}}",
  "amount": "{{createOrder.response.body.total}}",
  "method": "credit_card",
  "card_token": "test-token-123"
}

###

# @name sendNotification
POST {{baseUrl}}/notifications
Content-Type: application/json

{
  "user_id": "{{createUser.response.body.id}}",
  "type": "order_confirmation",
  "data": {
    "order_id": "{{createOrder.response.body.id}}",
    "payment_id": "{{processPayment.response.body.id}}"
  }
}
```

## 开发工作流

### 本地开发

```bash
#!/bin/bash
# dev-setup.sh

echo "正在设置本地开发环境..."

# 启动本地服务
docker-compose up -d

# 等待服务就绪
sleep 10

# 植入测试数据
dotnet-http POST localhost:3000/api/seed

# 运行初始测试
dotnet-http exec tests/local-smoke-tests.http --env development

echo "开发环境已就绪！"
```

### API 文档测试

```bash
#!/bin/bash
# test-api-docs.sh

# 从 OpenAPI 规范中提取 API 端点
ENDPOINTS=$(curl -s https://api.example.com/openapi.json | jq -r '.paths | keys[]')

echo "正在测试文档中的 API 端点..."

for endpoint in $ENDPOINTS; do
  # 将 OpenAPI 路径转换为实际 URL
  url="https://api.example.com${endpoint//\{[^}]*\}/123}"
  
  echo "测试：$url"
  
  if dotnet-http GET "$url" > /dev/null 2>&1; then
    echo "✓ $endpoint"
  else
    echo "✗ $endpoint"
  fi
done
```

## 监控与告警

### 可用性监控

```bash
#!/bin/bash
# uptime-monitor.sh

SERVICES=(
  "https://api.example.com/health"
  "https://auth.example.com/health"
  "https://cdn.example.com/status"
)

for service in "${SERVICES[@]}"; do
  if ! dotnet-http GET "$service" --check-status; then
    # 发送告警
    dotnet-http POST "https://hooks.slack.com/services/YOUR/SLACK/WEBHOOK" \
      text="🚨 服务不可用：$service"
  fi
done
```

### 性能监控

```bash
#!/bin/bash
# perf-monitor.sh

ENDPOINT="https://api.example.com/users"
THRESHOLD=1000  # 毫秒

start_time=$(date +%s%N)
dotnet-http GET "$ENDPOINT" > /dev/null
end_time=$(date +%s%N)

duration=$(((end_time - start_time) / 1000000))

if [ $duration -gt $THRESHOLD ]; then
  echo "⚠️  检测到响应缓慢：${duration}ms（阈值：${THRESHOLD}ms）"
  
  # 发送性能告警
  dotnet-http POST "https://hooks.slack.com/services/YOUR/SLACK/WEBHOOK" \
    text="⚠️ API 响应缓慢：$ENDPOINT 耗时 ${duration}ms"
fi
```

## 最佳实践总结

1. **使用环境变量**管理配置和密钥
2. **在脚本中实现正确的错误处理**
3. **使用 .http 文件创建可复用的测试套件**
4. **与 jq 等工具结合**处理数据
5. **为保存的请求使用有意义的名称**
6. **用注释为 API 测试添加文档说明**
7. **将测试文件与代码一起纳入版本控制**
8. **为不稳定端点实现重试逻辑**
9. **使用离线模式预览请求**
10. **监控并告警** API 健康状态和性能

## 下一步

- 参阅[调试指南](../debugging.zh.md)进行故障排查
