# Load Testing

dotnet-httpie has built-in load testing capabilities that allow you to send multiple requests, configure virtual users, and measure performance metrics — all from the same familiar CLI syntax.

## Overview

Load testing mode is triggered automatically when you configure any of the following options beyond their single-run defaults (i.e., iterations > 1, virtual users > 1, or a positive duration):

- `-n` / `--iteration` — repeat the request N times
- `--vu` / `--vus` / `--virtual-users` — run concurrent virtual users
- `--duration` — run the request repeatedly for a fixed time window

When load testing mode is active, dotnet-httpie collects response statistics and prints a performance summary after all requests complete.

## Options

| Option | Description | Default |
|--------|-------------|---------|
| `-n`, `--iteration` | Number of times to repeat the request | `1` |
| `--vu`, `--vus`, `--virtual-users` | Number of concurrent virtual users | `1` |
| `--duration` | How long to run the test (e.g. `10s`, `1m`, `00:01:00`) | — |
| `--timeout` | Per-request timeout in seconds | — |
| `--exporter-type` | Exporter type for results (e.g. `json`) | — |
| `--export-json-path` | Path to export raw response data as JSON (requires `--exporter-type=json`) | — |

## Basic Usage

### Repeat a Request N Times

Send the same request 10 times sequentially:

```bash
dotnet-http GET https://httpbin.org/get -n 10
```

### Concurrent Virtual Users

Simulate 5 concurrent users each sending the request once:

```bash
dotnet-http GET https://httpbin.org/get --vu 5
```

Combine virtual users with a duration — 5 concurrent users running for 30 seconds:

```bash
dotnet-http GET https://httpbin.org/get --vu 5 --duration 30s
```

### Duration-Based Testing

Run the request as many times as possible for 30 seconds:

```bash
dotnet-http GET https://httpbin.org/get --duration 30s
```

Run for 2 minutes with 10 virtual users:

```bash
dotnet-http GET https://httpbin.org/get --duration 2m --vu 10
```

Use a `TimeSpan` format for longer durations:

```bash
dotnet-http GET https://httpbin.org/get --duration 00:05:00
```

### POST Requests

Load test a POST endpoint:

```bash
dotnet-http POST https://httpbin.org/post \
  name=John \
  email=john@example.com \
  -n 100 --vu 10
```

### With Authentication

```bash
dotnet-http GET https://api.example.com/protected \
  Authorization:"Bearer $TOKEN" \
  --duration 60s --vu 20
```

## Output

After the load test completes, dotnet-httpie prints a summary report:

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

## Exporting Results

Export the raw response data to a JSON file for further analysis:

```bash
dotnet-http GET https://httpbin.org/get \
  -n 100 --vu 5 \
  --exporter-type=json --export-json-path results.json
```

The exported JSON file contains the full request context, total elapsed time, and the list of individual response models.

## Duration Format

The `--duration` option accepts the following formats:

| Format | Example | Description |
|--------|---------|-------------|
| Seconds | `30s` | Run for 30 seconds |
| Minutes | `5m` | Run for 5 minutes |
| Hours | `1h` | Run for 1 hour |
| TimeSpan | `00:01:30` | Run for 1 minute 30 seconds |

## Examples

### Quick Smoke Test

Send 5 requests and verify the endpoint is healthy:

```bash
dotnet-http GET https://api.example.com/health -n 5
```

### Baseline Performance Test

Measure response time with 10 concurrent users over 1 minute:

```bash
dotnet-http GET https://api.example.com/users \
  Authorization:"Bearer $TOKEN" \
  --vu 10 --duration 1m
```

### Stress Test

Push the endpoint with 50 virtual users for 5 minutes:

```bash
dotnet-http GET https://api.example.com/search \
  q==dotnet \
  --vu 50 --duration 5m \
  --exporter-type=json --export-json-path stress-results.json
```

### POST Load Test

Test a write endpoint with concurrent users:

```bash
dotnet-http POST https://api.example.com/orders \
  product_id:=42 \
  quantity:=1 \
  --vu 20 -n 50
```

## Tips

- Use `--timeout` to prevent slow requests from blocking virtual users indefinitely.
- Combine `--exporter-type=json` and `--export-json-path` with external tools (e.g., jq, Python, Excel) for detailed analysis.
- Start with low concurrency (`--vu 1`) to establish a baseline before scaling up.

## Next Steps

- Learn about [CI/CD integration](ci-cd-integration.md) to run load tests in pipelines
- Explore [file execution](file-execution.md) for complex multi-request scenarios
- Check [debugging techniques](debugging.md) when investigating failures
