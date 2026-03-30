# 调试与故障排查

> 📖 [View English Documentation](debugging.md)

本指南帮助您调试 dotnet-httpie 的问题并排查常见故障。

## 调试模式

启用调试模式以获取请求处理的详细信息：

```bash
dotnet-http GET api.example.com/data --debug
```

调试模式提供：
- 详细的请求/响应日志
- 中间件执行信息
- 错误堆栈跟踪
- 性能计时
- 配置详情

## 离线模式（请求预览）

在不实际发送请求的情况下预览请求内容：

```bash
# 预览单个请求
dotnet-http POST api.example.com/users name=John --offline

# 预览 HTTP 文件执行
dotnet-http exec requests.http --offline

# 结合调试信息预览
dotnet-http POST api.example.com/users name=John --debug --offline
```

离线模式适用于：
- 验证请求结构
- 检查 JSON 格式
- 确认请求头和参数
- 测试变量替换

## 常见问题

### 1. 安装问题

#### 安装后找不到工具

**问题**：`dotnet-http: command not found`

**解决方案**：
```bash
# 检查工具是否已安装
dotnet tool list --global

# 确认 .NET 工具路径是否在 PATH 中
echo $PATH | grep -q "$HOME/.dotnet/tools" || echo "工具路径不在 PATH 中"

# 添加到 Shell 配置文件（bash/zsh）
echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.bashrc
source ~/.bashrc

# 损坏后重新安装
dotnet tool uninstall --global dotnet-httpie
dotnet tool install --global dotnet-httpie
```

#### 权限被拒绝

**问题**：安装时出现权限问题

**解决方案**：
```bash
# 检查工具目录的权限
ls -la ~/.dotnet/tools/

# 必要时修复权限
chmod +x ~/.dotnet/tools/dotnet-http

# 仅为当前用户安装
dotnet tool install --global dotnet-httpie --tool-path ~/.local/bin
```

### 2. 请求问题

#### SSL/TLS 证书错误

**问题**：SSL 证书验证失败

**解决方案**：
```bash
# 跳过 SSL 验证（仅限开发环境）
dotnet-http GET https://self-signed-site.com --verify=no

# 使用自定义 CA 证书
dotnet-http GET https://internal-api.company.com \
  --ca-cert /path/to/ca-certificate.pem

# 检查 SSL 证书详情
openssl s_client -connect api.example.com:443 -servername api.example.com
```

#### 连接超时

**问题**：请求超时

**解决方案**：
```bash
# 增加超时时间（如支持）
dotnet-http GET api.example.com/slow-endpoint --timeout 60

# 检查网络连接
ping api.example.com
curl -I api.example.com

# 先用简单请求测试
dotnet-http GET api.example.com/health
```

#### 认证失败

**问题**：收到 401 Unauthorized 响应

**调试步骤**：
```bash
# 检查令牌有效性
echo $JWT_TOKEN | base64 -d

# 验证令牌格式
dotnet-http GET api.example.com/verify \
  Authorization:"Bearer $JWT_TOKEN" \
  --debug

# 用最简单的请求测试
dotnet-http GET api.example.com/public

# 检查请求头格式
dotnet-http GET httpbin.org/headers \
  Authorization:"Bearer $JWT_TOKEN"
```

### 3. JSON 和数据问题

#### JSON 解析错误

**问题**：请求体中的 JSON 无效

**调试**：
```bash
# 预览 JSON 结构
dotnet-http POST api.example.com/users \
  name=John \
  age:=30 \
  tags:='["dev", "api"]' \
  --offline

# 手动验证 JSON
echo '{"name": "John", "age": 30}' | jq .

# 对于复杂 JSON 使用文件
cat > user.json << EOF
{
  "name": "John",
  "age": 30,
  "skills": ["C#", "JavaScript"]
}
EOF

dotnet-http POST api.example.com/users @user.json
```

#### 字符编码问题

**问题**：特殊字符处理不正确

**解决方案**：
```bash
# 指定字符集
dotnet-http POST api.example.com/data \
  Content-Type:"application/json; charset=utf-8" \
  message="Hello 世界"

# 使用正确编码的文件
echo '{"message": "Hello 世界"}' > data.json
dotnet-http POST api.example.com/data @data.json
```

### 4. 文件执行问题

#### 找不到文件

**问题**：无法加载 HTTP 文件

**调试**：
```bash
# 检查文件是否存在及权限
ls -la requests.http

# 使用绝对路径
dotnet-http exec /full/path/to/requests.http

# 检查当前目录
pwd
dotnet-http exec ./requests.http
```

#### 变量替换失败

**问题**：变量未被替换

**调试**：
```bash
# 检查环境文件
cat http-client.env.json

# 验证环境选择
dotnet-http exec requests.http --env development --debug

# 测试变量语法
# 正确：{{baseUrl}}
# 错误：{baseUrl} 或 $baseUrl
```

#### 请求引用错误

**问题**：无法引用前一个响应

**调试**：
```bash
# 确保已定义请求名称
# @name createUser
POST {{baseUrl}}/users

# 使用正确语法引用
GET {{baseUrl}}/users/{{createUser.response.body.id}}

# 检查响应结构
dotnet-http exec requests.http --debug --offline
```

## 调试工具与技术

### 1. 网络分析

#### 使用 tcpdump/Wireshark

```bash
# 抓取网络流量（Linux/macOS）
sudo tcpdump -i any -w capture.pcap host api.example.com

# 运行请求
dotnet-http GET api.example.com/data

# 用 Wireshark 或 tcpdump 分析抓包
tcpdump -r capture.pcap -A
```

#### 与 curl 对比

```bash
# 与 curl 行为对比
curl -v https://api.example.com/data \
  -H "Authorization: Bearer $TOKEN"

# 转换为 dotnet-httpie 等效命令
dotnet-http GET api.example.com/data \
  Authorization:"Bearer $TOKEN" \
  --debug
```

### 2. 响应分析

#### JSON 处理

```bash
# 格式化打印 JSON 响应
dotnet-http GET api.example.com/users | jq .

# 提取特定字段
dotnet-http GET api.example.com/users | jq '.users[0].id'

# 验证 JSON 类型
dotnet-http GET api.example.com/users | jq 'type'
```

#### 请求头分析

```bash
# 仅显示响应头
dotnet-http HEAD api.example.com/data

# 检查特定请求头
dotnet-http GET httpbin.org/headers | jq '.headers'

# 跟踪请求头传播
dotnet-http GET api.example.com/data \
  X-Trace-ID:"$(uuidgen)" \
  --debug
```

### 3. 性能调试

#### 响应时间分析

```bash
# 测量响应时间
time dotnet-http GET api.example.com/data

# 多次请求取平均值
for i in {1..10}; do
  time dotnet-http GET api.example.com/data >/dev/null
done
```

#### 内存与资源使用

```bash
# 监控资源使用
top -p $(pgrep dotnet-http)

# 内存使用
ps aux | grep dotnet-http
```

## 错误分析

### HTTP 状态码

#### 4xx 客户端错误

```bash
# 400 Bad Request
dotnet-http POST api.example.com/users \
  invalid-json \
  --debug  # 检查请求格式

# 401 Unauthorized
dotnet-http GET api.example.com/protected \
  --debug  # 检查认证

# 403 Forbidden
dotnet-http GET api.example.com/admin \
  Authorization:"Bearer $USER_TOKEN" \
  --debug  # 检查权限

# 404 Not Found
dotnet-http GET api.example.com/nonexistent \
  --debug  # 检查 URL

# 429 Too Many Requests
dotnet-http GET api.example.com/data \
  --debug  # 检查速率限制
```

#### 5xx 服务器错误

```bash
# 500 Internal Server Error
dotnet-http POST api.example.com/users \
  name=John \
  --debug  # 检查服务器日志

# 502 Bad Gateway
dotnet-http GET api.example.com/data \
  --debug  # 检查代理/负载均衡器

# 503 Service Unavailable
dotnet-http GET api.example.com/health \
  --debug  # 检查服务状态
```

### 错误响应分析

```bash
# 捕获错误详情
ERROR_RESPONSE=$(dotnet-http GET api.example.com/error --body 2>&1)
echo "$ERROR_RESPONSE" | jq .

# 检查错误结构
dotnet-http GET api.example.com/error | jq '{error, message, code}'
```

## 日志记录与监控

### 请求日志

```bash
# 将所有请求记录到文件
dotnet-http GET api.example.com/data --debug > request.log 2>&1

# 结构化日志
dotnet-http GET api.example.com/data 2>&1 | \
  grep -E "(Request|Response|Error)" > structured.log
```

### 自动化健康检查

```bash
#!/bin/bash
# health-monitor.sh

LOG_FILE="/var/log/api-health.log"

check_endpoint() {
  local endpoint=$1
  local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
  
  if dotnet-http GET "$endpoint" --check-status >/dev/null 2>&1; then
    echo "[$timestamp] OK: $endpoint" >> "$LOG_FILE"
    return 0
  else
    echo "[$timestamp] FAIL: $endpoint" >> "$LOG_FILE"
    return 1
  fi
}

# 监控多个端点
check_endpoint "https://api.example.com/health"
check_endpoint "https://api.example.com/status"
check_endpoint "https://auth.example.com/health"
```

## Docker 调试

### 容器问题

```bash
# 以调试输出运行
docker run --rm weihanli/dotnet-httpie:latest \
  GET api.example.com/data --debug

# 检查容器日志
docker run --name httpie-debug weihanli/dotnet-httpie:latest \
  GET api.example.com/data
docker logs httpie-debug
docker rm httpie-debug

# 交互式调试
docker run -it --rm weihanli/dotnet-httpie:latest /bin/sh
```

### Docker 网络问题

```bash
# 测试网络连通性
docker run --rm weihanli/dotnet-httpie:latest \
  GET httpbin.org/get

# 使用主机网络
docker run --rm --network host \
  weihanli/dotnet-httpie:latest GET localhost:3000/api

# 检查 DNS 解析
docker run --rm weihanli/dotnet-httpie:latest \
  nslookup api.example.com
```

## 环境调试

### 环境变量

```bash
# 检查所有环境变量
env | grep -i http
env | grep -i api

# 验证特定变量
echo "API_TOKEN: $API_TOKEN"
echo "BASE_URL: $BASE_URL"

# 调试变量展开
dotnet-http GET "$BASE_URL/data" \
  Authorization:"Bearer $API_TOKEN" \
  --debug
```

### 配置文件

```bash
# 检查 HTTP 客户端环境
cat http-client.env.json | jq .

# 验证 JSON 语法
jq empty http-client.env.json && echo "JSON 格式正确" || echo "JSON 格式错误"

# 检查文件权限
ls -la http-client.env.json
```

## 性能故障排查

### 请求缓慢

```bash
# 添加计时信息
time dotnet-http GET api.example.com/slow-endpoint

# 用 curl 对比分析
time curl -s https://api.example.com/slow-endpoint >/dev/null

# 检查 DNS 解析时间
time nslookup api.example.com

# 测试不同端点
time dotnet-http GET httpbin.org/delay/5
```

### 内存问题

```bash
# 在大型请求期间监控内存使用
dotnet-http GET api.example.com/large-dataset &
PID=$!
while kill -0 $PID 2>/dev/null; do
  ps -p $PID -o pid,vsz,rss,comm
  sleep 1
done
```

## 高级调试

### 自定义中间件调试

如果您正在开发自定义中间件：

```bash
# 启用详细中间件日志
dotnet-http GET api.example.com/data \
  --debug \
  -v  # 详细模式（如可用）
```

### 源代码调试

```bash
# 克隆仓库进行本地调试
git clone https://github.com/WeihanLi/dotnet-httpie.git
cd dotnet-httpie

# 以调试模式构建
dotnet build -c Debug

# 附加调试器运行
dotnet run --project src/HTTPie -- GET api.example.com/data --debug
```

## 获取帮助

### 提交 Bug 报告时需包含的信息

报告问题时，请包含：

1. **版本信息**：
   ```bash
   dotnet-http --version
   dotnet --version
   ```

2. **失败的命令**：
   ```bash
   dotnet-http GET api.example.com/data --debug
   ```

3. **预期行为与实际行为**

4. **错误信息**（使用 `--debug` 的完整输出）

5. **环境详情**：
   - 操作系统
   - .NET 版本
   - Docker 版本（如使用 Docker）

### 社区资源

- [GitHub Issues](https://github.com/WeihanLi/dotnet-httpie/issues)
- [GitHub Discussions](https://github.com/WeihanLi/dotnet-httpie/discussions)
- [Stack Overflow](https://stackoverflow.com/questions/tagged/dotnet-httpie)

### 自助排查清单

寻求帮助之前，请先确认：

- [ ] 已使用 `--debug` 标志
- [ ] 已使用 `--offline` 检查请求结构
- [ ] 已验证认证令牌/密钥
- [ ] 已检查网络连接
- [ ] 已先用简单请求测试
- [ ] 已查阅相关文档
- [ ] 已搜索现有 Issues

## 下一步

- 查看[常见使用场景](examples/common-use-cases.zh.md)中的可用示例
- 参阅 [CI/CD 集成](ci-cd-integration.zh.md)了解自动化测试
