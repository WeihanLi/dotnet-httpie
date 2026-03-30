# Docker 使用指南

> 📖 [View English Documentation](docker-usage.md)

dotnet-httpie 提供 Docker 镜像，便于在容器化环境、CI/CD 流水线以及未安装 .NET 的系统中使用。

## Docker 镜像

官方 Docker 镜像地址：`weihanli/dotnet-httpie`

### 可用标签

- `latest` —— 最新稳定版本
- `preview` —— 最新预览/预发布版本
- `0.12.0` —— 特定版本标签

## 快速开始

### 拉取镜像

```bash
docker pull weihanli/dotnet-httpie:latest
```

### 基本用法

```bash
# 简单 GET 请求
docker run --rm weihanli/dotnet-httpie:latest httpbin.org/get

# 带数据的 POST 请求
docker run --rm weihanli/dotnet-httpie:latest POST httpbin.org/post name=John age:=30

# 带请求头
docker run --rm weihanli/dotnet-httpie:latest GET httpbin.org/headers Authorization:"Bearer token"
```

## 常用使用模式

### 交互式使用

```bash
# 创建别名以简化使用
alias http='docker run --rm -i weihanli/dotnet-httpie:latest'

# 像安装版一样使用
http GET httpbin.org/get
http POST httpbin.org/post name=John
```

### 使用本地文件

挂载本地目录以访问文件：

```bash
# 挂载当前目录
docker run --rm -v $(pwd):/workspace -w /workspace \
  weihanli/dotnet-httpie:latest exec requests.http

# 挂载指定目录
docker run --rm -v /path/to/files:/files \
  weihanli/dotnet-httpie:latest exec /files/api-tests.http
```

### 环境变量

向容器传递环境变量：

```bash
# 单个环境变量
docker run --rm -e API_TOKEN="your-token" \
  weihanli/dotnet-httpie:latest GET api.example.com/protected \
  Authorization:"Bearer $API_TOKEN"

# 多个环境变量
docker run --rm \
  -e API_BASE_URL="https://api.example.com" \
  -e API_TOKEN="your-token" \
  weihanli/dotnet-httpie:latest GET "$API_BASE_URL/users" \
  Authorization:"Bearer $API_TOKEN"

# 环境文件
docker run --rm --env-file .env \
  weihanli/dotnet-httpie:latest GET api.example.com/data
```

## 文件操作

### 执行 HTTP 文件

```bash
# 挂载并执行 HTTP 文件
docker run --rm -v $(pwd):/workspace -w /workspace \
  weihanli/dotnet-httpie:latest exec tests/api.http

# 指定环境
docker run --rm -v $(pwd):/workspace -w /workspace \
  weihanli/dotnet-httpie:latest exec tests/api.http --env production
```

### 文件下载

```bash
# 下载到挂载的卷
docker run --rm -v $(pwd)/downloads:/downloads \
  weihanli/dotnet-httpie:latest GET httpbin.org/image/png \
  --download --output /downloads/image.png
```

### 上传文件

```bash
# 从挂载的卷上传文件
docker run --rm -v $(pwd):/workspace -w /workspace \
  weihanli/dotnet-httpie:latest POST api.example.com/upload \
  @data.json
```

## 网络配置

### 主机网络

访问宿主机上运行的服务：

```bash
# 访问 localhost 服务（Linux）
docker run --rm --network host \
  weihanli/dotnet-httpie:latest GET localhost:3000/api/health

# 访问宿主机服务（macOS/Windows）
docker run --rm \
  weihanli/dotnet-httpie:latest GET host.docker.internal:3000/api/health
```

### 自定义网络

```bash
# 创建网络
docker network create api-test-network

# 在网络中运行 API 服务器
docker run -d --name api-server --network api-test-network my-api-image

# 在同一网络中使用 dotnet-httpie 测试 API
docker run --rm --network api-test-network \
  weihanli/dotnet-httpie:latest GET api-server:3000/health
```

## CI/CD 集成

### GitHub Actions

```yaml
name: API Tests
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Test API Health
        run: |
          docker run --rm --network host \
            weihanli/dotnet-httpie:latest GET localhost:3000/health
      
      - name: Run API Test Suite
        run: |
          docker run --rm -v ${{ github.workspace }}:/workspace -w /workspace \
            weihanli/dotnet-httpie:latest exec tests/api-suite.http --env testing
      
      - name: Test with Authentication
        env:
          API_TOKEN: ${{ secrets.API_TOKEN }}
        run: |
          docker run --rm -e API_TOKEN \
            weihanli/dotnet-httpie:latest GET api.example.com/protected \
            Authorization:"Bearer $API_TOKEN"
```

### Azure DevOps

```yaml
stages:
- stage: ApiTests
  jobs:
  - job: RunTests
    pool:
      vmImage: 'ubuntu-latest'
    steps:
    - script: |
        docker run --rm -v $(System.DefaultWorkingDirectory):/workspace -w /workspace \
          weihanli/dotnet-httpie:latest exec tests/integration.http --env $(Environment)
      displayName: 'Run Integration Tests'
      env:
        API_TOKEN: $(ApiToken)
```

### GitLab CI

```yaml
test-api:
  image: docker:latest
  services:
    - docker:dind
  script:
    - docker run --rm -v $PWD:/workspace -w /workspace 
        weihanli/dotnet-httpie:latest exec tests/api.http --env $CI_ENVIRONMENT_NAME
  variables:
    API_TOKEN: $API_TOKEN
```

## Docker Compose 集成

### 基本配置

```yaml
# docker-compose.yml

services:
  api:
    image: my-api:latest
    ports:
      - "3000:3000"
    environment:
      - NODE_ENV=development
  
  api-tests:
    image: weihanli/dotnet-httpie:latest
    depends_on:
      - api
    volumes:
      - ./tests:/tests
    command: exec /tests/api-suite.http
    environment:
      - API_BASE_URL=http://api:3000
```

### 健康检查

```yaml
version: '3.8'

services:
  api:
    image: my-api:latest
    healthcheck:
      test: ["CMD", "docker", "run", "--rm", "--network", "container:my-api", 
             "weihanli/dotnet-httpie:latest", "GET", "localhost:3000/health"]
      interval: 30s
      timeout: 10s
      retries: 3
```

## 高级场景

### 多阶段测试

```bash
# 测试开发环境
docker run --rm -v $(pwd):/workspace -w /workspace \
  weihanli/dotnet-httpie:latest exec tests/smoke.http --env development

# 测试预发布环境  
docker run --rm -v $(pwd):/workspace -w /workspace \
  -e API_BASE_URL="https://staging.api.example.com" \
  weihanli/dotnet-httpie:latest exec tests/full-suite.http --env staging

# 测试生产环境
docker run --rm -v $(pwd):/workspace -w /workspace \
  -e API_BASE_URL="https://api.example.com" \
  weihanli/dotnet-httpie:latest exec tests/health-check.http --env production
```

### 代理测试

```bash
# 通过代理测试
docker run --rm \
  -e HTTP_PROXY="http://proxy.company.com:8080" \
  -e HTTPS_PROXY="http://proxy.company.com:8080" \
  weihanli/dotnet-httpie:latest GET api.example.com/data
```

## Shell 脚本与自动化

### Bash 脚本

```bash
#!/bin/bash
# api-test.sh

set -e

API_BASE_URL="${API_BASE_URL:-http://localhost:3000}"
DOCKER_IMAGE="weihanli/dotnet-httpie:latest"

echo "正在测试 $API_BASE_URL 上的 API..."

# 健康检查
echo "检查 API 健康状态..."
docker run --rm $DOCKER_IMAGE GET "$API_BASE_URL/health"

# 登录获取令牌（JSON 数据，非基本认证）
echo "测试登录..."
TOKEN=$(docker run --rm $DOCKER_IMAGE POST "$API_BASE_URL/auth/login" \
  username=testuser password=testpass --body | jq -r '.token')

# 用 Bearer 令牌测试受保护端点
echo "测试受保护端点..."
docker run --rm $DOCKER_IMAGE GET "$API_BASE_URL/protected" \
  Authorization:"Bearer $TOKEN"

# 基本认证测试（HTTP 基本认证）
echo "测试基本认证..."
docker run --rm $DOCKER_IMAGE GET "$API_BASE_URL/basic-protected" \
  --auth testuser:testpass

echo "所有测试通过！"
```

### PowerShell 脚本

```powershell
# api-test.ps1

param(
    [string]$ApiBaseUrl = "http://localhost:3000",
    [string]$Environment = "development"
)

$dockerImage = "weihanli/dotnet-httpie:latest"

Write-Host "正在测试 $ApiBaseUrl 上的 API..."

# 运行测试套件
docker run --rm -v "${PWD}:/workspace" -w /workspace `
  -e API_BASE_URL=$ApiBaseUrl `
  $dockerImage exec tests/api-suite.http --env $Environment

Write-Host "测试完成！"
```

## 配置

### 自定义配置

```bash
# 挂载自定义配置
docker run --rm -v $(pwd)/config:/config \
  -e DOTNET_HTTPIE_CONFIG="/config/httpie.json" \
  weihanli/dotnet-httpie:latest GET api.example.com/data
```

### SSL 证书

```bash
# 挂载自定义 CA 证书
docker run --rm -v $(pwd)/certs:/certs \
  -e SSL_CERT_DIR="/certs" \
  weihanli/dotnet-httpie:latest GET https://internal-api.company.com/data
```

## 故障排查

### 调试模式

```bash
# 启用调试输出
docker run --rm \
  weihanli/dotnet-httpie:latest GET httpbin.org/get --debug
```

### 离线模式

```bash
# 预览请求而不实际发送
docker run --rm -v $(pwd):/workspace -w /workspace \
  weihanli/dotnet-httpie:latest exec tests/api.http --offline
```

### 容器日志

```bash
# 使用详细输出运行
docker run --rm \
  weihanli/dotnet-httpie:latest GET httpbin.org/get --verbose

# 保存日志
docker run --rm \
  weihanli/dotnet-httpie:latest GET httpbin.org/get > request.log 2>&1
```

## 性能注意事项

### 镜像大小

dotnet-httpie Docker 镜像通过以下方式优化大小：
- 多阶段构建
- Alpine Linux 基础镜像（适用时）
- AOT 编译以减少运行时依赖

### 缓存

```bash
# 预先拉取镜像以加快执行速度
docker pull weihanli/dotnet-httpie:latest

# 使用特定版本确保一致性
docker run --rm weihanli/dotnet-httpie:0.12.0 GET httpbin.org/get
```

## 最佳实践

1. **在生产环境中使用特定镜像标签**
2. **高效挂载卷** —— 只挂载必要的内容
3. **使用环境变量**进行配置
4. **利用 Docker 网络**实现服务间通信
5. **使用 `--rm` 标志**自动清理容器
6. **在 CI/CD 中预先拉取镜像**以加快执行速度
7. **针对不同环境进行多阶段测试**
8. **使用 Docker secrets 或外部密钥管理**保护敏感数据

## 下一步

- 配置 [CI/CD 集成](ci-cd-integration.zh.md)与 Docker 结合
- 了解[环境配置](environment-variables.md)
- 探索使用 Docker 的[高级示例](examples/integrations.md)
- 查看[故障排查指南](debugging.zh.md)了解常见问题
