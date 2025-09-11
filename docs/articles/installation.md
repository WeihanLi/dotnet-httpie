# Installation

This guide covers how to install dotnet-httpie on different platforms and environments.

## Prerequisites

- .NET SDK 8.0 or later
- Internet connection for package installation

## Global Tool Installation (Recommended)

### Install Latest Stable Version

```bash
dotnet tool update --global dotnet-httpie
```

### Install Latest Preview Version

```bash
dotnet tool update --global dotnet-httpie --prerelease
```

### Install Specific Version

```bash
dotnet tool install --global dotnet-httpie --version 0.12.0
```

## Verification

After installation, verify that dotnet-httpie is working correctly:

```bash
dotnet-http --version
```

You should see output similar to:
```
dotnet-httpie/0.12.0 (.NET; HTTPie-like)
```

## Docker Installation

If you prefer using Docker instead of installing globally:

### Pull Latest Image

```bash
docker pull weihanli/dotnet-httpie:latest
```

### Use Without Installation

```bash
docker run --rm weihanli/dotnet-httpie:latest --help
```

## Updating

### Update Global Tool

```bash
dotnet tool update --global dotnet-httpie
```

### Update to Preview Version

```bash
dotnet tool update --global dotnet-httpie --prerelease
```

## Uninstallation

### Remove Global Tool

```bash
dotnet tool uninstall --global dotnet-httpie
```

### Remove Docker Image

```bash
docker rmi weihanli/dotnet-httpie:latest
```

## Troubleshooting Installation

### Common Issues

1. **Permission Denied**: Make sure you have proper permissions to install global tools
2. **PATH Issues**: Ensure the .NET tools directory is in your PATH
3. **Old .NET Version**: Verify you have .NET 8.0 or later installed

### Check .NET Version

```bash
dotnet --version
```

### Check Installed Tools

```bash
dotnet tool list --global
```

### Reinstall if Corrupted

```bash
dotnet tool uninstall --global dotnet-httpie
dotnet tool install --global dotnet-httpie
```

## Platform-Specific Notes

### Windows
- Tools are installed to `%USERPROFILE%\.dotnet\tools`
- May require restart of command prompt/PowerShell

### macOS/Linux
- Tools are installed to `~/.dotnet/tools`
- May need to restart terminal session

### CI/CD Environments
See [CI/CD Integration](ci-cd-integration.md) for specific setup instructions for continuous integration environments.

## Next Steps

Once installed, continue with the [Quick Start Guide](quick-start.md) to begin using dotnet-httpie.