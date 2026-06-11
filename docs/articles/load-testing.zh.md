# 负载测试

> 📖 [View English Documentation](load-testing.md)

dotnet-httpie 内置了负载测试功能，让您可以使用熟悉的 CLI 语法发送多个请求、配置虚拟用户并衡量性能指标。

## 概述

当您将以下任一选项配置为超出单次运行的默认值时（即迭代次数大于 1、虚拟用户数大于 1，或设置了正数的持续时间），将自动触发负载测试模式：

- `-n` / `--iteration` —— 重复请求 N 次
- `--vu` / `--vus` / `--virtual-users` —— 运行并发虚拟用户
- `--duration` —— 在固定时间窗口内持续运行请求

负载测试模式激活后，dotnet-httpie 会收集响应统计信息，并在所有请求完成后打印性能摘要。

## 选项

| 选项 | 描述 | 默认值 |
|--------|-------------|---------|
| `-n`, `--iteration` | 重复请求的次数 | `1` |
| `--vu`, `--vus`, `--virtual-users` | 并发虚拟用户数 | `1` |
| `--duration` | 测试运行时长（例如 `10s`、`1m`、`00:01:00`） | — |
| `--timeout` | 每个请求的超时时间（秒） | — |
| `--exporter-type` | 结果导出类型（例如 `json`） | — |
| `--export-json-path` | 将原始响应数据导出为 JSON 的路径（需要 `--exporter-type=json`） | — |

## 基本用法

### 重复请求 N 次

顺序发送同一请求 10 次：

```bash
dotnet-http GET https://httpbin.org/get -n 10
```

### 并发虚拟用户

模拟 5 个并发用户各发送一次请求：

```bash
dotnet-http GET https://httpbin.org/get --vu 5
```

组合虚拟用户与持续时间 —— 5 个并发用户运行 30 秒：

```bash
dotnet-http GET https://httpbin.org/get --vu 5 --duration 30s
```

### 基于时长的测试

在 30 秒内尽可能多地运行请求：

```bash
dotnet-http GET https://httpbin.org/get --duration 30s
```

使用 10 个虚拟用户运行 2 分钟：

```bash
dotnet-http GET https://httpbin.org/get --duration 2m --vu 10
```

使用 `TimeSpan` 格式表示更长的时长：

```bash
dotnet-http GET https://httpbin.org/get --duration 00:05:00
```

### POST 请求

对 POST 端点进行负载测试：

```bash
dotnet-http POST https://httpbin.org/post \
  name=John \
  email=john@example.com \
  -n 100 --vu 10
```

### 带认证的请求

```bash
dotnet-http GET https://api.example.com/protected \
  Authorization:"Bearer $TOKEN" \
  --duration 60s --vu 20
```

## 输出结果

负载测试完成后，dotnet-httpie 将打印摘要报告：

```
Total Requests:    100
Success Requests:  99  (99.00%)
Failed Requests:   1

Total Elapsed:     5432.00 ms
Requests/sec:      18.41

Response Time (ms):
  Average:  52.34
  Min:      12.10
  Max:      312.45
  Median:   48.20
  P50:      48.20
  P75:      65.30
  P90:      95.10
  P95:      120.80
  P99:      285.60
```

## 导出结果

将原始响应数据导出为 JSON 文件以便进一步分析：

```bash
dotnet-http GET https://httpbin.org/get \
  -n 100 --vu 5 \
  --exporter-type=json --export-json-path results.json
```

导出的 JSON 文件包含完整的请求上下文、总耗时以及各响应模型的列表。

## 时长格式

`--duration` 选项支持以下格式：

| 格式 | 示例 | 描述 |
|--------|---------|-------------|
| 秒 | `30s` | 运行 30 秒 |
| 分钟 | `5m` | 运行 5 分钟 |
| 小时 | `1h` | 运行 1 小时 |
| TimeSpan | `00:01:30` | 运行 1 分 30 秒 |

## 示例

### 快速冒烟测试

发送 5 个请求验证端点是否健康：

```bash
dotnet-http GET https://api.example.com/health -n 5
```

### 基准性能测试

用 10 个并发用户测试 1 分钟的响应时间：

```bash
dotnet-http GET https://api.example.com/users \
  Authorization:"Bearer $TOKEN" \
  --vu 10 --duration 1m
```

### 压力测试

用 50 个虚拟用户压测端点 5 分钟：

```bash
dotnet-http GET https://api.example.com/search \
  q==dotnet \
  --vu 50 --duration 5m \
  --exporter-type=json --export-json-path stress-results.json
```

### POST 负载测试

并发测试写入端点：

```bash
dotnet-http POST https://api.example.com/orders \
  product_id:=42 \
  quantity:=1 \
  --vu 20 -n 50
```

## 使用技巧

- 使用 `--timeout` 防止慢请求无限期阻塞虚拟用户。
- 将 `--exporter-type=json` 和 `--export-json-path` 与外部工具（如 jq、Python、Excel）结合使用，进行详细分析。
- 先以低并发（`--vu 1`）建立基准，再逐步提高并发数。

## 下一步

- 了解 [CI/CD 集成](ci-cd-integration.zh.md)以在流水线中运行负载测试
- 探索[文件执行](file-execution.zh.md)处理复杂的多请求场景
- 查看[调试技术](debugging.zh.md)排查失败原因
