# CI/CD Integration

This guide shows how to integrate dotnet-httpie into various CI/CD pipelines for automated API testing, health checks, and deployment verification.

## Overview

dotnet-httpie is perfect for CI/CD scenarios because it:
- Provides deterministic exit codes
- Supports scriptable automation
- Works in containerized environments
- Handles authentication securely
- Offers offline mode for validation

## GitHub Actions

### Basic API Testing

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
          dotnet-version: '8.0'
      
      - name: Install dotnet-httpie
        run: dotnet tool install --global dotnet-httpie
      
      - name: API Health Check
        run: dotnet-http GET ${{ vars.API_BASE_URL }}/health
        
      - name: Run API Test Suite
        run: dotnet-http exec tests/api-integration.http --env testing
        env:
          API_TOKEN: ${{ secrets.API_TOKEN }}
```

### Multi-Environment Testing

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
          dotnet-version: '8.0'
          
      - name: Install dotnet-httpie
        run: dotnet tool install --global dotnet-httpie
        
      - name: Test ${{ matrix.environment }}
        run: dotnet-http exec tests/smoke-tests.http --env ${{ matrix.environment }}
        env:
          API_TOKEN: ${{ secrets[format('API_TOKEN_{0}', matrix.environment)] }}
          API_BASE_URL: ${{ vars[format('API_BASE_URL_{0}', matrix.environment)] }}
```

### Docker-based Testing

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

### Deployment Verification

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
      
      # Deploy steps here...
      
      - name: Install dotnet-httpie
        run: dotnet tool install --global dotnet-httpie
      
      - name: Verify Deployment
        run: |
          # Wait for deployment to be ready
          sleep 30
          
          # Health check
          dotnet-http GET ${{ vars.PRODUCTION_API_URL }}/health
          
          # Smoke tests
          dotnet-http exec tests/post-deployment.http --env production
        env:
          PRODUCTION_API_TOKEN: ${{ secrets.PRODUCTION_API_TOKEN }}
          
      - name: Rollback on Failure
        if: failure()
        run: |
          echo "Deployment verification failed, initiating rollback..."
          # Rollback logic here
```

## Azure DevOps

### Basic Pipeline

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
    version: '8.0.x'

- script: dotnet tool install --global dotnet-httpie
  displayName: 'Install dotnet-httpie'

- script: dotnet-http GET $(apiBaseUrl)/health
  displayName: 'API Health Check'

- script: dotnet-http exec tests/api-tests.http --env $(Environment)
  displayName: 'Run API Tests'
  env:
    API_TOKEN: $(ApiToken)
```

### Multi-Stage Pipeline

```yaml
trigger:
- main

stages:
- stage: Test
  displayName: 'Test Stage'
  jobs:
  - job: ApiTests
    displayName: 'API Tests'
    pool:
      vmImage: 'ubuntu-latest'
    steps:
    - task: UseDotNet@2
      inputs:
        packageType: 'sdk'
        version: '8.0.x'
    
    - script: dotnet tool install --global dotnet-httpie
      displayName: 'Install dotnet-httpie'
    
    - script: dotnet-http exec tests/unit-api-tests.http --env testing
      displayName: 'Unit API Tests'
      env:
        API_TOKEN: $(TestApiToken)

- stage: Deploy
  displayName: 'Deploy Stage'
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
          # Deployment steps...
          
          - script: dotnet tool install --global dotnet-httpie
            displayName: 'Install dotnet-httpie'
          
          - script: |
              # Wait for deployment
              sleep 60
              
              # Verify deployment
              dotnet-http GET $(ProductionApiUrl)/health
              dotnet-http exec tests/production-smoke.http --env production
            displayName: 'Verify Deployment'
            env:
              PRODUCTION_API_TOKEN: $(ProductionApiToken)
```

## GitLab CI

### Basic Configuration

```yaml
stages:
  - test
  - deploy
  - verify

variables:
  DOTNET_VERSION: "8.0"

before_script:
  - apt-get update -qy
  - apt-get install -y dotnet-sdk-8.0
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
    - echo "Deploying to production..."
    # Deployment logic here
  only:
    - main

verify-production:
  stage: verify
  script:
    - sleep 30  # Wait for deployment
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

### Docker-based GitLab CI

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

### Declarative Pipeline

```groovy
pipeline {
    agent any
    
    environment {
        DOTNET_VERSION = '8.0'
        API_BASE_URL = 'https://api.example.com'
    }
    
    stages {
        stage('Setup') {
            steps {
                sh '''
                    # Install .NET SDK if not available
                    if ! command -v dotnet &> /dev/null; then
                        wget https://dot.net/v1/dotnet-install.sh
                        chmod +x dotnet-install.sh
                        ./dotnet-install.sh --version ${DOTNET_VERSION}
                        export PATH="$PATH:$HOME/.dotnet"
                    fi
                    
                    # Install dotnet-httpie
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
                sh 'echo "Deploying to production..."'
                // Deployment steps
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
                subject: "Pipeline Failed: ${env.JOB_NAME} - ${env.BUILD_NUMBER}",
                body: "API tests failed. Check the build logs for details.",
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
          command: echo "Deploying..."
      
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

## Docker Compose for Testing

### Local Integration Testing

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

### API Testing Job

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

### CronJob for Health Monitoring

```yaml
apiVersion: batch/v1
kind: CronJob
metadata:
  name: api-health-monitor
spec:
  schedule: "*/5 * * * *"  # Every 5 minutes
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
                  echo "Health check failed"
                  exit 1
                fi
            env:
            - name: API_BASE_URL
              value: "http://api-service:8080"
          restartPolicy: OnFailure
```

## Test Patterns

### Health Check Scripts

```bash
#!/bin/bash
# health-check.sh

set -e

API_BASE_URL="${API_BASE_URL:-http://localhost:3000}"
MAX_RETRIES="${MAX_RETRIES:-30}"
RETRY_DELAY="${RETRY_DELAY:-2}"

echo "Waiting for API to be ready at $API_BASE_URL..."

for i in $(seq 1 $MAX_RETRIES); do
  if dotnet-http GET "$API_BASE_URL/health" --check-status >/dev/null 2>&1; then
    echo "API is ready after $i attempts"
    exit 0
  fi
  
  echo "Attempt $i/$MAX_RETRIES failed, retrying in ${RETRY_DELAY}s..."
  sleep $RETRY_DELAY
done

echo "API failed to become ready after $MAX_RETRIES attempts"
exit 1
```

### Smoke Test Suite

```bash
#!/bin/bash
# smoke-tests.sh

set -e

API_BASE_URL="${API_BASE_URL:-http://localhost:3000}"
ENVIRONMENT="${ENVIRONMENT:-development}"

echo "Running smoke tests for $ENVIRONMENT environment..."

# Critical endpoints
ENDPOINTS=(
  "/health"
  "/api/v1/status"
  "/api/v1/version"
)

for endpoint in "${ENDPOINTS[@]}"; do
  echo "Testing $endpoint..."
  if dotnet-http GET "$API_BASE_URL$endpoint" --check-status; then
    echo "✓ $endpoint OK"
  else
    echo "✗ $endpoint FAILED"
    exit 1
  fi
done

echo "All smoke tests passed!"
```

### Load Testing

```bash
#!/bin/bash
# load-test.sh

ENDPOINT="${1:-http://localhost:3000/api/test}"
CONCURRENT="${2:-10}"
REQUESTS="${3:-100}"

echo "Load testing $ENDPOINT with $CONCURRENT concurrent users, $REQUESTS total requests"

# Create temporary results file
RESULTS_FILE=$(mktemp)

# Function to run requests
run_requests() {
  local requests_per_worker=$1
  for i in $(seq 1 $requests_per_worker); do
    start_time=$(date +%s%N)
    if dotnet-http GET "$ENDPOINT" >/dev/null 2>&1; then
      end_time=$(date +%s%N)
      duration=$(((end_time - start_time) / 1000000))
      echo "SUCCESS,$duration" >> "$RESULTS_FILE"
    else
      echo "FAILURE,0" >> "$RESULTS_FILE"
    fi
  done
}

# Start concurrent workers
REQUESTS_PER_WORKER=$((REQUESTS / CONCURRENT))
for i in $(seq 1 $CONCURRENT); do
  run_requests $REQUESTS_PER_WORKER &
done

# Wait for all workers to complete
wait

# Analyze results
total=$(wc -l < "$RESULTS_FILE")
successful=$(grep "SUCCESS" "$RESULTS_FILE" | wc -l)
failed=$((total - successful))

if [ $successful -gt 0 ]; then
  avg_response_time=$(grep "SUCCESS" "$RESULTS_FILE" | cut -d, -f2 | awk '{sum+=$1} END {print sum/NR}')
else
  avg_response_time=0
fi

echo "Load test results:"
echo "  Total requests: $total"
echo "  Successful: $successful"
echo "  Failed: $failed"
echo "  Success rate: $(( successful * 100 / total ))%"
echo "  Average response time: ${avg_response_time}ms"

# Cleanup
rm "$RESULTS_FILE"

# Exit with error if failure rate is too high
if [ $((failed * 100 / total)) -gt 5 ]; then
  echo "Failure rate too high!"
  exit 1
fi
```

## Best Practices

### 1. Environment Management

```bash
# Use environment-specific configurations
dotnet-http exec tests/api-tests.http --env $CI_ENVIRONMENT_NAME

# Store secrets securely in CI/CD variables
export API_TOKEN="$CI_API_TOKEN"
dotnet-http GET api.example.com/protected Authorization:"Bearer $API_TOKEN"
```

### 2. Error Handling

```bash
# Proper error handling in scripts
if ! dotnet-http GET api.example.com/health --check-status; then
  echo "Health check failed, aborting deployment"
  exit 1
fi

# Retry logic for flaky endpoints
for i in {1..3}; do
  if dotnet-http GET api.example.com/flaky-endpoint; then
    break
  elif [ $i -eq 3 ]; then
    echo "Endpoint failed after 3 attempts"
    exit 1
  else
    echo "Attempt $i failed, retrying..."
    sleep 5
  fi
done
```

### 3. Parallel Testing

```bash
# Run tests in parallel for faster execution
{
  dotnet-http exec tests/user-api.http --env $ENV &
  dotnet-http exec tests/order-api.http --env $ENV &
  dotnet-http exec tests/payment-api.http --env $ENV &
  wait
} && echo "All API tests completed successfully"
```

### 4. Reporting

```bash
# Generate test reports
{
  echo "# API Test Report"
  echo "Generated: $(date)"
  echo ""
  
  if dotnet-http GET api.example.com/health; then
    echo "✅ Health Check: PASSED"
  else
    echo "❌ Health Check: FAILED"
  fi
  
  # More test results...
} > test-report.md
```

## Troubleshooting CI/CD Issues

### Common Problems

1. **Tool not found**: Ensure dotnet-httpie is installed and in PATH
2. **Network issues**: Check firewall rules and DNS resolution
3. **Authentication failures**: Verify secrets and environment variables
4. **Timeout issues**: Increase timeouts for slow networks
5. **SSL certificate problems**: Use `--verify=no` for development (not production)

### Debug CI/CD Issues

```bash
# Enable debug mode in CI
dotnet-http GET api.example.com/data --debug

# Check environment variables
env | grep -i api

# Test network connectivity
dotnet-http GET httpbin.org/get  # External connectivity
dotnet-http GET localhost:3000/health  # Local connectivity
```

## Next Steps

- Set up [monitoring and alerting](../examples/common-use-cases.md) with dotnet-httpie
- Explore [Docker usage](../docker-usage.md) for containerized CI/CD
- Learn about [debugging techniques](../debugging.md) for troubleshooting
- Review [authentication methods](../authentication.md) for secure CI/CD