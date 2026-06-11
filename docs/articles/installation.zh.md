# 安装

> 📖 [View English Documentation](installation.md)

本指南介绍如何在不同平台和环境中安装 dotnet-httpie。

## 前提条件

- .NET SDK 10.0 或更高版本
- 安装时需要网络连接

## 全局工具安装（推荐）

### 安装最新稳定版

```bash
dotnet tool update --global dotnet-httpie
```

### 安装最新预览版

```bash
dotnet tool update --global dotnet-httpie --prerelease
```

### 安装指定版本

```bash
dotnet tool install --global dotnet-httpie --version 0.12.0
```

## 其他安装方式

### 方式二：预构建二进制文件

从 [GitHub Releases](https://github.com/WeihanLi/dotnet-httpie/releases) 下载特定平台的可执行文件：

- **Windows**: `dotnet-httpie-win-x64.exe`
- **Linux**: `dotnet-httpie-linux-x64`  
- **macOS**: `dotnet-httpie-osx-x64`

解压后将其添加到系统 PATH 以便全局访问。

### 方式三：Docker

容器化使用方式请参阅 [Docker 使用指南](docker-usage.zh.md)。

## 验证安装

安装完成后，验证 dotnet-httpie 是否正常工作：

```bash
dotnet-http --version
```

应看到类似如下的输出：
```
dotnet-httpie/0.12.0 (.NET; HTTPie-like)
```

## Docker 安装

如果您希望使用 Docker 而非全局安装：

### 拉取最新镜像

```bash
docker pull weihanli/dotnet-httpie:latest
```

### 无需安装直接使用

```bash
docker run --rm weihanli/dotnet-httpie:latest --help
```

## 更新

### 更新全局工具

```bash
dotnet tool update --global dotnet-httpie
```

### 更新至预览版

```bash
dotnet tool update --global dotnet-httpie --prerelease
```

## 卸载

### 移除全局工具

```bash
dotnet tool uninstall --global dotnet-httpie
```

### 移除 Docker 镜像

```bash
docker rmi weihanli/dotnet-httpie:latest
```

## 安装故障排查

### 常见问题

1. **权限被拒绝**：请确保您有安装全局工具的相应权限
2. **PATH 问题**：确保 .NET 工具目录已添加到您的 PATH 中
3. **.NET 版本过旧**：请确认已安装 .NET 10.0 或更高版本

### 检查 .NET 版本

```bash
dotnet --version
```

### 查看已安装的工具

```bash
dotnet tool list --global
```

### 损坏后重新安装

```bash
dotnet tool uninstall --global dotnet-httpie
dotnet tool install --global dotnet-httpie
```

## 各平台注意事项

### Windows
- 工具安装至 `%USERPROFILE%\.dotnet\tools`
- 可能需要重启命令提示符或 PowerShell

### macOS/Linux
- 工具安装至 `~/.dotnet/tools`
- 可能需要重启终端会话

### CI/CD 环境
持续集成环境的具体配置请参阅 [CI/CD 集成](ci-cd-integration.zh.md)。

## 下一步

安装完成后，继续阅读[快速上手指南](quick-start.zh.md)开始使用 dotnet-httpie。
