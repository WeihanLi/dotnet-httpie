# CI/CD 集成

> 📖 [View English Documentation](ci-cd-integration.md)

本指南介绍如何将 dotnet-httpie 集成到各种 CI/CD 流水线中，用于自动化 API 测试、健康检查和部署验证。

## 概述

dotnet-httpie 非常适合 CI/CD 场景，因为它：
- 提供确定性的退出码
- 支持脚本化自动化
- 在容器化环境中工作良好
- 安全处理身份认证
- 提供离线模式进行验证

## GitHub Actions

### 基本 API 测试

```yaml
name: API Tests
on: [push, pull_request]

jobs:
  api-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0'
      
      - name: Install dotnet-httpie
        run: dotnet tool install --global dotnet-httpie
      
      - name: API Health Check
        run: dotnet-http GET ${{ vars.API_BASE_URL }}/health
        
      - name: Run API Test Suite
        run: dotnet-http exec tests/api-integration.http --env testing
        env:
          API_TOKEN: ${{ secrets.API_TOKEN }}
```

### 多环境测试

```yaml
name: Multi-Environment Tests
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    strategy:
      matrix:
        environment: [development, staging, production]
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0'
          
      - name: Install dotnet-httpie
        run: dotnet tool install --global dotnet-httpie
        
      - name: Test ${{ matrix.environment }}
        run: dotnet-http exec tests/smoke-tests.http --env ${{ matrix.environment }}
        env:
          API_TOKEN: ${{ secrets[format('API_TOKEN_{0}', matrix.environment)] }}
          API_BASE_URL: ${{ vars[format('API_BASE_URL_{0}', matrix.environment)] }}
```

### 基于 Docker 的测试

```yaml
name: Docker API Tests
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    services:
      api:
        image: my-api:latest
        ports:
          - 3000:3000
        env:
          DATABASE_URL: postgresql://test:test@postgres:5432/testdb
      postgres:
        image: postgres:13
        env:
          POSTGRES_DB: testdb
          POSTGRES_USER: test
          POSTGRES_PASSWORD: test
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
      - uses: actions/checkout@v3
      
      - name: Wait for API to be ready
        run: |
          timeout 60 bash -c 'until docker run --rm --network host weihanli/dotnet-httpie:latest GET localhost:3000/health; do sleep 2; done'
      
      - name: Run Integration Tests
        run: |
          docker run --rm --network host \
            -v ${{ github.workspace }}:/workspace -w /workspace \
            weihanli/dotnet-httpie:latest exec tests/integration.http --env ci
```

### 部署验证

```yaml
name: Deploy and Verify
on:
  push:
    branches: [main]

jobs:
  deploy-and-verify:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      # 此处为部署步骤...
      
      - name: Install dotnet-httpie
        run: dotnet tool install --global dotnet-httpie
      
      - name: Verify Deployment
        run: |
          # 等待部署就绪
          sleep 30
          
          # 健康检查
          dotnet-http GET ${{ vars.PRODUCTION_API_URL }}/health
          
          # 冒烟测试
          dotnet-http exec tests/post-deployment.http --env production
        env:
          PRODUCTION_API_TOKEN: ${{ secrets.PRODUCTION_API_TOKEN }}
          
      - name: Rollback on Failure
        if: failure()
        run: |
          echo "部署验证失败，正在启动回滚..."
          # 此处为回滚逻辑
```

## Azure DevOps

### 基本流水线

```yaml
trigger:
- main

pool:
  vmImage: 'ubuntu-latest'

variables:
  apiBaseUrl: 'https://api.example.com'

steps:
- task: UseDotNet@2
  displayName: 'Setup .NET SDK'
  inputs:
    packageType: 'sdk'
    version: '10.0.x'

- script: dotnet tool install --global dotnet-httpie
  displayName: 'Install dotnet-httpie'

- script: dotnet-http GET $(apiBaseUrl)/health
  displayName: 'API Health Check'

- script: dotnet-http exec tests/api-tests.http --env $(Environment)
  displayName: 'Run API Tests'
  env:
    API_TOKEN: $(ApiToken)
```

### 多阶段流水线

```yaml
trigger:
- main

stages:
- stage: Test
  displayName: '测试阶段'
  jobs:
  - job: ApiTests
    displayName: 'API 测试'
    pool:
      vmImage: 'ubuntu-latest'
    steps:
    - task: UseDotNet@2
      inputs:
        packageType: 'sdk'
        version: '10.0.x'
    
    - script: dotnet tool install --global dotnet-httpie
      displayName: 'Install dotnet-httpie'
    
    - script: dotnet-http exec tests/unit-api-tests.http --env testing
      displayName: 'Unit API Tests'
      env:
        API_TOKEN: $(TestApiToken)

- stage: Deploy
  displayName: '部署阶段'
  dependsOn: Test
  condition: succeeded()
  jobs:
  - deployment: DeployAPI
    displayName: 'Deploy API'
    environment: 'production'
    strategy:
      runOnce:
        deploy:
          steps:
          # 部署步骤...
          
          - script: dotnet tool install --global dotnet-httpie
            displayName: 'Install dotnet-httpie'
          
          - script: |
              # 等待部署完成
              sleep 60
              
              # 验证部署
              dotnet-http GET $(ProductionApiUrl)/health
              dotnet-http exec tests/production-smoke.http --env production
            displayName: 'Verify Deployment'
            env:
              PRODUCTION_API_TOKEN: $(ProductionApiToken)
```

## GitLab CI

### 基本配置

```yaml
stages:
  - test
  - deploy
  - verify

variables:
  DOTNET_VERSION: "10.0"

before_script:
  - apt-get update -qy
  - apt-get install -y dotnet-sdk-10.0
  - dotnet tool install --global dotnet-httpie
  - export PATH="$PATH:/root/.dotnet/tools"

api-tests:
  stage: test
  script:
    - dotnet-http GET $API_BASE_URL/health
    - dotnet-http exec tests/api-suite.http --env $CI_ENVIRONMENT_NAME
  variables:
    API_BASE_URL: "https://api-test.example.com"
  environment:
    name: testing

deploy-production:
  stage: deploy
  script:
    - echo "正在部署到生产环境..."
    # 此处为部署逻辑
  only:
    - main

verify-production:
  stage: verify
  script:
    - sleep 30  # 等待部署完成
    - dotnet-http GET $PRODUCTION_API_URL/health
    - dotnet-http exec tests/production-verification.http --env production
  variables:
    PRODUCTION_API_URL: "https://api.example.com"
  environment:
    name: production
  dependencies:
    - deploy-production
  only:
    - main
```

### 基于 Docker 的 GitLab CI

```yaml
image: mcr.microsoft.com/dotnet/sdk:8.0

stages:
  - test
  - deploy

api-tests:
  stage: test
  services:
    - name: postgres:13
      alias: postgres
    - name: redis:6
      alias: redis
  before_script:
    - dotnet tool install --global dotnet-httpie
    - export PATH="$PATH:/root/.dotnet/tools"
  script:
    - dotnet-http GET http://api-container:3000/health
    - dotnet-http exec tests/integration.http --env gitlab-ci
  variables:
    POSTGRES_DB: testdb
    POSTGRES_USER: test
    POSTGRES_PASSWORD: test
```

## Jenkins

### 声明式流水线

```groovy
pipeline {
    agent any
    
    environment {
        DOTNET_VERSION = '10.0'
        API_BASE_URL = 'https://api.example.com'
    }
    
    stages {
        stage('Setup') {
            steps {
                sh '''
                    # 如果没有安装 .NET SDK，则安装它
                    if ! command -v dotnet &> /dev/null; then
                        wget https://dot.net/v1/dotnet-install.sh
                        chmod +x dotnet-install.sh
                        ./dotnet-install.sh --version ${DOTNET_VERSION}
                        export PATH="$PATH:$HOME/.dotnet"
                    fi
                    
                    # 安装 dotnet-httpie
                    dotnet tool install --global dotnet-httpie
                '''
            }
        }
        
        stage('API Health Check') {
            steps {
                sh 'dotnet-http GET ${API_BASE_URL}/health'
            }
        }
        
        stage('API Tests') {
            steps {
                withCredentials([string(credentialsId: 'api-token', variable: 'API_TOKEN')]) {
                    sh '''
                        export API_TOKEN=${API_TOKEN}
                        dotnet-http exec tests/api-tests.http --env ${BRANCH_NAME}
                    '''
                }
            }
        }
        
        stage('Deploy') {
            when {
                branch 'main'
            }
            steps {
                sh 'echo "正在部署到生产环境..."'
                // 部署步骤
            }
        }
        
        stage('Verify Deployment') {
            when {
                branch 'main'
            }
            steps {
                sh '''
                    sleep 30
                    dotnet-http GET ${API_BASE_URL}/health
                    dotnet-http exec tests/production-smoke.http --env production
                '''
            }
        }
    }
    
    post {
        failure {
            emailext (
                subject: "流水线失败：${env.JOB_NAME} - ${env.BUILD_NUMBER}",
                body: "API 测试失败，请查看构建日志了解详情。",
                to: "${env.CHANGE_AUTHOR_EMAIL}"
            )
        }
    }
}
```

## CircleCI

```yaml
version: 2.1

orbs:
  dotnet: circleci/dotnet@2.0.0

jobs:
  api-tests:
    docker:
      - image: mcr.microsoft.com/dotnet/sdk:8.0
    steps:
      - checkout
      - run:
          name: Install dotnet-httpie
          command: dotnet tool install --global dotnet-httpie
      - run:
          name: Add tools to PATH
          command: echo 'export PATH="$PATH:/root/.dotnet/tools"' >> $BASH_ENV
      - run:
          name: API Health Check
          command: dotnet-http GET $API_BASE_URL/health
      - run:
          name: Run API Tests
          command: dotnet-http exec tests/api-tests.http --env testing

  deploy:
    docker:
      - image: cimg/base:stable
    steps:
      - checkout
      - run:
          name: Deploy to production
          command: echo "正在部署..."
      
  verify-deployment:
    docker:
      - image: mcr.microsoft.com/dotnet/sdk:8.0
    steps:
      - checkout
      - run:
          name: Install dotnet-httpie
          command: dotnet tool install --global dotnet-httpie
      - run:
          name: Add tools to PATH
          command: echo 'export PATH="$PATH:/root/.dotnet/tools"' >> $BASH_ENV
      - run:
          name: Verify Production Deployment
          command: |
            sleep 30
            dotnet-http GET $PRODUCTION_API_URL/health
            dotnet-http exec tests/production-verification.http --env production

workflows:
  test-deploy-verify:
    jobs:
      - api-tests
      - deploy:
          requires:
            - api-tests
          filters:
            branches:
              only: main
      - verify-deployment:
          requires:
            - deploy
```

## Docker Compose 测试

### 本地集成测试

```yaml
# docker-compose.test.yml
version: '3.8'

services:
  api:
    build: .
    ports:
      - "3000:3000"
    environment:
      - NODE_ENV=test
      - DATABASE_URL=postgresql://test:test@postgres:5432/testdb
    depends_on:
      postgres:
        condition: service_healthy

  postgres:
    image: postgres:13
    environment:
      POSTGRES_DB: testdb
      POSTGRES_USER: test
      POSTGRES_PASSWORD: test
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U test"]
      interval: 5s
      timeout: 5s
      retries: 5

  api-tests:
    image: weihanli/dotnet-httpie:latest
    depends_on:
      - api
    volumes:
      - ./tests:/tests
    command: >
      sh -c "
        sleep 10 &&
        dotnet-http GET http://api:3000/health &&
        dotnet-http exec /tests/integration-tests.http --env docker
      "
    environment:
      - API_BASE_URL=http://api:3000
```

## Kubernetes Jobs

### API 测试 Job

```yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: api-tests
spec:
  template:
    spec:
      containers:
      - name: api-tests
        image: weihanli/dotnet-httpie:latest
        command: ["/bin/sh"]
        args:
          - -c
          - |
            dotnet-http GET $API_BASE_URL/health
            dotnet-http exec /tests/k8s-tests.http --env kubernetes
        env:
        - name: API_BASE_URL
          value: "http://api-service:8080"
        - name: API_TOKEN
          valueFrom:
            secretKeyRef:
              name: api-secrets
              key: token
        volumeMounts:
        - name: test-files
          mountPath: /tests
      volumes:
      - name: test-files
        configMap:
          name: api-test-files
      restartPolicy: Never
  backoffLimit: 3
```

### 健康监控 CronJob

```yaml
apiVersion: batch/v1
kind: CronJob
metadata:
  name: api-health-monitor
spec:
  schedule: "*/5 * * * *"  # 每 5 分钟执行一次
  jobTemplate:
    spec:
      template:
        spec:
          containers:
          - name: health-check
            image: weihanli/dotnet-httpie:latest
            command: ["/bin/sh"]
            args:
              - -c
              - |
                if ! dotnet-http GET $API_BASE_URL/health --check-status; then
                  echo "健康检查失败"
                  exit 1
                fi
            env:
            - name: API_BASE_URL
              value: "http://api-service:8080"
          restartPolicy: OnFailure
```

## 测试模式

### 健康检查脚本

```bash
#!/bin/bash
# health-check.sh

set -e

API_BASE_URL="${API_BASE_URL:-http://localhost:3000}"
MAX_RETRIES="${MAX_RETRIES:-30}"
RETRY_DELAY="${RETRY_DELAY:-2}"

echo "正在等待 $API_BASE_URL 上的 API 就绪..."

for i in $(seq 1 $MAX_RETRIES); do
  if dotnet-http GET "$API_BASE_URL/health" --check-status >/dev/null 2>&1; then
    echo "API 在第 $i 次尝试后就绪"
    exit 0
  fi
  
  echo "第 $i/$MAX_RETRIES 次尝试失败，${RETRY_DELAY}s 后重试..."
  sleep $RETRY_DELAY
done

echo "API 在 $MAX_RETRIES 次尝试后仍未就绪"
exit 1
```

### 冒烟测试套件

```bash
#!/bin/bash
# smoke-tests.sh

set -e

API_BASE_URL="${API_BASE_URL:-http://localhost:3000}"
ENVIRONMENT="${ENVIRONMENT:-development}"

echo "正在 $ENVIRONMENT 环境中运行冒烟测试..."

# 关键端点
ENDPOINTS=(
  "/health"
  "/api/v1/status"
  "/api/v1/version"
)

for endpoint in "${ENDPOINTS[@]}"; do
  echo "测试 $endpoint..."
  if dotnet-http GET "$API_BASE_URL$endpoint" --check-status; then
    echo "✓ $endpoint 正常"
  else
    echo "✗ $endpoint 失败"
    exit 1
  fi
done

echo "所有冒烟测试通过！"
```

## 最佳实践

### 1. 环境管理

```bash
# 使用特定环境配置
dotnet-http exec tests/api-tests.http --env $CI_ENVIRONMENT_NAME

# 在 CI/CD 变量中安全存储密钥
export API_TOKEN="$CI_API_TOKEN"
dotnet-http GET api.example.com/protected Authorization:"Bearer $API_TOKEN"
```

### 2. 错误处理

```bash
# 脚本中的正确错误处理
if ! dotnet-http GET api.example.com/health --check-status; then
  echo "健康检查失败，正在中止部署"
  exit 1
fi

# 不稳定端点的重试逻辑
for i in {1..3}; do
  if dotnet-http GET api.example.com/flaky-endpoint; then
    break
  elif [ $i -eq 3 ]; then
    echo "端点在 3 次尝试后仍然失败"
    exit 1
  else
    echo "第 $i 次尝试失败，正在重试..."
    sleep 5
  fi
done
```

### 3. 并行测试

```bash
# 并行运行测试以加快执行速度
{
  dotnet-http exec tests/user-api.http --env $ENV &
  dotnet-http exec tests/order-api.http --env $ENV &
  dotnet-http exec tests/payment-api.http --env $ENV &
  wait
} && echo "所有 API 测试成功完成"
```

### 4. 生成报告

```bash
# 生成测试报告
{
  echo "# API 测试报告"
  echo "生成时间：$(date)"
  echo ""
  
  if dotnet-http GET api.example.com/health; then
    echo "✅ 健康检查：通过"
  else
    echo "❌ 健康检查：失败"
  fi
  
  # 更多测试结果...
} > test-report.md
```

## CI/CD 问题排查

### 常见问题

1. **找不到工具**：确保 dotnet-httpie 已安装并在 PATH 中
2. **网络问题**：检查防火墙规则和 DNS 解析
3. **认证失败**：验证密钥和环境变量
4. **超时问题**：为慢速网络增加超时时间
5. **SSL 证书问题**：在开发环境中使用 `--verify=no`（不适用于生产环境）

### 调试 CI/CD 问题

```bash
# 在 CI 中启用调试模式
dotnet-http GET api.example.com/data --debug

# 检查环境变量
env | grep -i api

# 测试网络连接
dotnet-http GET httpbin.org/get  # 外部连通性
dotnet-http GET localhost:3000/health  # 本地连通性
```

## 下一步

- 配置 [dotnet-httpie 的监控和告警](../examples/common-use-cases.zh.md)
- 探索[Docker 使用](../docker-usage.zh.md)实现容器化 CI/CD
- 了解[调试技术](../debugging.zh.md)进行故障排查
- 查看[身份认证方法](../authentication.zh.md)保护 CI/CD 安全
