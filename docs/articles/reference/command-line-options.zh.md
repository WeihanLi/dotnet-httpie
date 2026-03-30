# 命令行选项参考

> 📖 [View English Documentation](command-line-options.md)

本文档提供 dotnet-httpie 所有命令行选项和标志的完整参考。

## 命令语法

```
dotnet-http [global-flags] [METHOD] URL [request-items...] [request-flags]
```

## 全局标志

### 帮助与版本

| 标志 | 描述 | 示例 |
|------|-------------|---------|
| `--help` | 显示帮助信息（当 `-h` 是唯一参数时，同样会显示此帮助） | `dotnet-http --help` |
| `--version` | 显示版本信息 | `dotnet-http --version` |

> 注意：在实际 CLI 中，`-h` 主要是 `--headers`（仅输出响应头部）的短标志；只有当 `-h` 作为唯一参数使用时（例如 `dotnet-http -h`）才会被解析为帮助。`--headers`/`-h` 选项详见下文"请求标志"章节。

### 调试与日志

| 标志 | 描述 | 示例 |
|------|-------------|---------|
| `--debug` | 启用调试模式及详细日志 | `dotnet-http GET api.example.com --debug` |
| `--verbose`, `-v` | 启用详细输出 | `dotnet-http GET api.example.com --verbose` |
| `--quiet`, `-q` | 隐藏非错误输出 | `dotnet-http GET api.example.com --quiet` |

### 请求预览

| 标志 | 描述 | 示例 |
|------|-------------|---------|
| `--offline` | 预览请求但不实际发送 | `dotnet-http POST api.example.com --offline` |
| `--print` | 指定打印内容（请求/响应） | `dotnet-http GET api.example.com --print=HhBb` |

## HTTP 方法

支持所有标准 HTTP 方法：

```bash
# GET（默认）
dotnet-http GET api.example.com/users
dotnet-http api.example.com/users  # 没有请求体数据参数时默认使用 GET

# POST
dotnet-http POST api.example.com/users

# PUT
dotnet-http PUT api.example.com/users/123

# PATCH
dotnet-http PATCH api.example.com/users/123

# DELETE
dotnet-http DELETE api.example.com/users/123

# HEAD
dotnet-http HEAD api.example.com/users

# OPTIONS
dotnet-http OPTIONS api.example.com/users
```

## 请求标志

### 身份认证

| 标志 | 描述 | 示例 |
|------|-------------|---------|
| `--auth`, `-a` | 基本认证 | `dotnet-http GET api.example.com --auth user:pass` |
| `--auth-type` | 认证类型 | `dotnet-http GET api.example.com --auth-type bearer` |

### 请求体

| 标志 | 描述 | 示例 |
|------|-------------|---------|
| `--json`, `-j` | 强制使用 JSON Content-Type | `dotnet-http POST api.example.com --json` |
| `--form`, `-f` | 以表单数据发送 | `dotnet-http POST api.example.com --form` |
| `--raw` | 发送原始数据 | `dotnet-http POST api.example.com --raw "text data"` |

### 文件操作

| 标志 | 描述 | 示例 |
|------|-------------|---------|
| `--download`, `-d` | 将响应下载到文件 | `dotnet-http GET api.example.com/file.pdf --download` |
| `--output`, `-o` | 将响应保存到指定文件 | `dotnet-http GET api.example.com/data --output data.json` |
| `--continue`, `-C` | 恢复中断的下载 | `dotnet-http GET api.example.com/large.zip --download --continue` |

## exec 命令选项

`exec` 命令有其专属的选项集：

```bash
dotnet-http exec [options] [file-path]
```

### exec 标志

| 标志 | 描述 | 示例 |
|------|-------------|---------|
| `--env` | 使用的环境 | `dotnet-http exec requests.http --env production` |
| `--type`, `-t` | 脚本类型（http/curl） | `dotnet-http exec script.curl --type curl` |
| `--debug` | 执行时的调试模式 | `dotnet-http exec requests.http --debug` |
| `--offline` | 预览执行但不实际发送 | `dotnet-http exec requests.http --offline` |

### 支持的脚本类型

| 类型 | 描述 | 文件扩展名 |
|------|-------------|-----------------|
| `http` | HTTP 请求文件 | `.http`、`.rest` |
| `curl` | cURL 命令文件 | `.curl`、`.sh` |

## 请求项类型

### 查询参数

```bash
# 语法：name==value
dotnet-http GET api.example.com/search query==httpie limit==10
```

### 请求头

```bash
# 语法：name:value
dotnet-http GET api.example.com/data Authorization:"Bearer token"
```

### JSON 字段

```bash
# 语法：name=value（创建 JSON）
dotnet-http POST api.example.com/users name=John email=john@example.com
```

### 原始 JSON 值

```bash
# 语法：name:=value（带类型的 JSON）
dotnet-http POST api.example.com/users age:=30 active:=true
```

## 输出格式

### 响应输出控制

```bash
# 默认：显示响应头和响应体
dotnet-http GET api.example.com/users

# 仅显示响应头
dotnet-http HEAD api.example.com/users

# 仅显示响应体
dotnet-http GET api.example.com/users --body

# 使用 --print 指定显示内容
dotnet-http GET api.example.com/users --print=HhBb
```

### --print 选项值

| 代码 | 内容 |
|------|-----------|
| `H` | 请求头 |
| `B` | 请求体 |
| `h` | 响应头 |
| `b` | 响应体 |
| `p` | 请求/响应属性 |

## 退出码

| 代码 | 含义 |
|------|---------|
| `0` | 成功 |
| `1` | 通用错误 |
| `2` | 请求超时 |
| `3` | 重定向次数过多 |
| `4` | HTTP 4xx 错误 |
| `5` | HTTP 5xx 错误 |
| `6` | 网络错误 |

## 按类别分类的示例

### 基本请求

```bash
# 简单 GET
dotnet-http GET httpbin.org/get

# 带数据的 POST
dotnet-http POST httpbin.org/post name=John age:=30

# 自定义请求头
dotnet-http GET httpbin.org/headers User-Agent:"MyApp/1.0"
```

### 认证示例

```bash
# Bearer 令牌
dotnet-http GET api.example.com/protected Authorization:"Bearer token"

# 基本认证
dotnet-http GET api.example.com/secure --auth user:pass

# API 密钥
dotnet-http GET api.example.com/data X-API-Key:"key123"
```

### 文件操作

```bash
# 下载文件
dotnet-http GET api.example.com/report.pdf --download

# 将文件作为请求体发送
dotnet-http POST api.example.com/data @data.json
```

### 表单数据

```bash
# URL 编码表单
dotnet-http POST httpbin.org/post --form name=John email=john@example.com
```

### 响应处理

```bash
# 保存响应
dotnet-http GET api.example.com/data --output response.json

# 仅输出响应体到标准输出
dotnet-http GET api.example.com/data --body

# 仅显示响应头
dotnet-http HEAD api.example.com/data
```

### 网络选项

```bash
# 使用代理
dotnet-http GET api.example.com/data --proxy http://proxy:8080

# 自定义超时
dotnet-http GET api.example.com/slow --timeout 60

# 跳过 SSL 验证
dotnet-http GET https://self-signed.local --verify=no
```

### 调试与开发

```bash
# 调试模式
dotnet-http GET api.example.com/data --debug

# 预览请求
dotnet-http POST api.example.com/users name=John --offline

# 详细输出
dotnet-http GET api.example.com/data --verbose
```

## 高级使用模式

### 特定环境请求

```bash
# 开发环境
dotnet-http GET localhost:3000/api/users --debug

# 预发布环境
dotnet-http GET staging-api.example.com/users \
  Authorization:"Bearer $STAGING_TOKEN"

# 生产环境
dotnet-http GET api.example.com/users \
  Authorization:"Bearer $PROD_TOKEN" \
  --timeout 30
```

### 条件请求

```bash
# 检查状态码
if dotnet-http GET api.example.com/health --check-status; then
  echo "API 健康"
fi

# 带错误处理
dotnet-http GET api.example.com/data || echo "请求失败"
```

### 脚本集成

```bash
# 提取数据用于后续处理
USER_ID=$(dotnet-http POST api.example.com/users name=John --body | jq -r '.id')
dotnet-http GET "api.example.com/users/$USER_ID/profile"

# 批量操作
for user in alice bob charlie; do
  dotnet-http POST api.example.com/users name="$user"
done
```

## 从其他工具迁移

### 从 cURL 迁移

```bash
# cURL 命令
curl -X POST https://api.example.com/users \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer token" \
  -d '{"name": "John", "age": 30}'

# dotnet-httpie 等效命令
dotnet-http POST api.example.com/users \
  Authorization:"Bearer token" \
  name=John \
  age:=30
```

### 从 HTTPie 迁移

```bash
# HTTPie 命令
http POST api.example.com/users Authorization:"Bearer token" name=John age:=30

# dotnet-httpie（非常相似）
dotnet-http POST api.example.com/users Authorization:"Bearer token" name=John age:=30
```

## 平台特定说明

### Windows

```powershell
# PowerShell 转义
dotnet-http POST api.example.com/users name=John tags:='[\"web\", \"api\"]'

# 命令提示符转义
dotnet-http POST api.example.com/users name=John tags:="[\"web\", \"api\"]"
```

### macOS/Linux

```bash
# Bash/Zsh（标准转义）
dotnet-http POST api.example.com/users name=John tags:='["web", "api"]'

# Fish shell
dotnet-http POST api.example.com/users name=John tags:=\'["web", "api"]\'
```

## 性能优化建议

1. **使用 `--body` 进行管道传输**：避免请求头处理开销
2. **复用连接**：在支持的地方使用会话管理
3. **减少调试输出**：仅在排查问题时使用 `--debug`
4. **优化 JSON**：数字/布尔值使用 `:=` 而非字符串
5. **批量请求**：对多个相关请求使用文件执行

## 下一步

- 参阅[常见使用场景](../examples/common-use-cases.zh.md)了解实际应用
- 了解[文件执行](../file-execution.zh.md)处理高级工作流
- 查看[调试指南](../debugging.zh.md)进行故障排查
- 探索 [Docker 使用](../docker-usage.zh.md)在容器化环境中使用
