# dotnet-httpie 发行说明

> 📖 [View English Documentation](ReleaseNotes.md)

## [0.12.0](https://nuget.org/packages/dotnet-httpie/0.12.0)

- 嵌套 JSON 构建器，修复 <https://github.com/WeihanLi/dotnet-httpie/issues/55>

## [0.11.0](https://nuget.org/packages/dotnet-httpie/0.11.0)

- 升级 `System.CommandLine`
- 执行功能增强，支持 debug/offline 模式及从 stdin 读取

## [0.10.0](https://nuget.org/packages/dotnet-httpie/0.10.0)

- 为 HTTP 文件执行添加 http env 文件支持
- 添加 `net10.0` 支持

## [0.9.0](https://nuget.org/packages/dotnet-httpie/0.9.0)

- 添加 `net9.0` 支持，移除 `net6.0`/`net7.0` 支持
- AOT 支持
- 发布基于 AOT 产物的容器镜像，支持多平台

## [0.8.2](https://nuget.org/packages/dotnet-httpie/0.8.2)

- 添加 `net8.0` 支持
- `HttpParser` 功能增强
- 添加 `CurlParser`

## [0.7.2](https://nuget.org/packages/dotnet-httpie/0.7.2)

- 添加用于执行 HTTP 请求的 `exec` 命令

## [0.6.3](https://nuget.org/packages/dotnet-httpie/0.6.3)

- 添加 `net7.0` 支持
- 默认不显示时间戳
- 允许 GET 请求携带请求体
- 修复 `Authorization` 请求头 Bug

## [0.5.3](https://nuget.org/packages/dotnet-httpie/0.5.3)

- 修复 `-h` Bug
- 修复下载 Bug
- 重构中间件

## [0.4.3](https://nuget.org/packages/dotnet-httpie/0.4.3)

- 添加文件下载支持
- 添加响应 JSON Schema 验证支持
- 添加 `RequestCacheMiddleware`
- 添加负载测试导出器

## [0.3.4](https://nuget.org/packages/dotnet-httpie/0.3.4)

- 添加负载测试支持

## [0.2.0](https://nuget.org/packages/dotnet-httpie/0.2.0)

- 使用 `System.CommandLine` 重构

## [0.1.0](https://nuget.org/packages/dotnet-httpie/0.1.0)

- 初始发布
