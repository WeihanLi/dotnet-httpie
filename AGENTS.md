# AGENTS.md

## Project Overview

**dotnet-httpie** is a command-line HTTP client for the .NET ecosystem — a modern, user-friendly alternative to `curl`. It is distributed as a .NET global tool (`dotnet-http` command) and as a Docker image.

Key capabilities:
- Human-friendly syntax for HTTP requests (GET, POST, PUT, DELETE, etc.)
- Execute `.http` / `.rest` files for repeatable API testing
- Run cURL commands directly
- Environment variable support via `http-client.env.json`
- Request chaining (reference previous response values)
- Load testing, authentication helpers, file download, JSON Schema validation
- **AOT compilation** enabled for Release builds on .NET 10 for minimal binary size

**Technology stack**: C# / .NET 10, `System.CommandLine`, `xunit.v3`, NuGet central package management (`Directory.Packages.props`).

## Environment Setup

### Prerequisites

- **.NET 10 SDK** — required (the solution file `.slnx` format is not supported by .NET 8 or older)

```bash
dotnet --list-sdks      # should show 10.0.x
dotnet --list-runtimes  # should show Microsoft.NETCore.App 10.0.x
```

### Repository Layout

```
/
├── .github/workflows/          # CI/CD pipelines (dotnet.yml is the main build)
├── src/HTTPie/                 # Main application project (targets net10.0)
│   ├── Abstractions/           # Interfaces and abstractions
│   ├── Commands/               # CLI command definitions (System.CommandLine)
│   ├── Implement/              # Feature implementations
│   ├── Middleware/             # Request/response pipeline middleware
│   ├── Models/                 # Domain models
│   ├── Utilities/              # Helper utilities
│   └── HTTPie.csproj           # Main project file (PackAsTool=true)
├── tests/
│   ├── HTTPie.UnitTest/        # Unit tests (xunit.v3)
│   └── HTTPie.IntegrationTest/ # Integration tests (require network)
│       └── TestAssets/         # Sample .http/.rest/.curl files
├── build/
│   ├── build.cs                # dotnet-execute build script
│   └── version.props           # Version properties
├── Directory.Build.props       # Shared MSBuild properties (LangVersion=preview)
├── Directory.Build.targets     # Shared MSBuild targets
├── Directory.Packages.props    # Central NuGet package version management
├── dotnet-httpie.slnx          # Solution file (requires .NET 10 SDK)
├── build.sh                    # Linux/macOS build script (uses dotnet-execute)
├── build.ps1                   # Windows build script
└── Dockerfile                  # Docker image definition
```

## Build Commands

```bash
# Build the solution
dotnet build

# Build using the dotnet-execute build script (recommended — also runs tests)
bash build.sh

# Build with explicit solution file
dotnet build dotnet-httpie.slnx

# Package the tool as a NuGet package
dotnet pack src/HTTPie/HTTPie.csproj --configuration Release
# Output: src/HTTPie/bin/Release/dotnet-httpie.{version}.nupkg

# Publish AOT binary (Release only)
dotnet publish src/HTTPie/HTTPie.csproj -f net10.0 --use-current-runtime -o dist
```

## Testing Instructions

### Run All Tests

```bash
dotnet test
```

### Run Individual Test Projects

```bash
# Unit tests only (no network required)
dotnet test tests/HTTPie.UnitTest/HTTPie.UnitTest.csproj

# Integration tests (requires network connectivity)
dotnet test tests/HTTPie.IntegrationTest/HTTPie.IntegrationTest.csproj
```

### Test Conventions

- Test framework: **xunit.v3** with `Xunit.DependencyInjection`
- Unit tests are in `tests/HTTPie.UnitTest/`, mirroring the `src/HTTPie/` structure
- Integration tests are in `tests/HTTPie.IntegrationTest/`
- Sample `.http`, `.rest`, and `.curl` test assets live in `tests/HTTPie.IntegrationTest/TestAssets/`
- `xunit.runner.json` configures test runner behaviour per project
- Integration tests call external services — they may fail without network access

## Development Workflow

### Run the Application Locally

```bash
# Show help
dotnet run --project src/HTTPie/HTTPie.csproj --framework net10.0 -- --help

# Make an HTTP request (offline mode — no network needed)
dotnet run --project src/HTTPie/HTTPie.csproj --framework net10.0 -- https://httpbin.org/get --offline

# Execute a .http file in offline mode
dotnet run --project src/HTTPie/HTTPie.csproj --framework net10.0 -- exec tests/HTTPie.IntegrationTest/TestAssets/HttpStartedSample.http --offline
```

### Install and Test as a Global Tool

```bash
dotnet pack src/HTTPie/HTTPie.csproj --configuration Release
dotnet tool install --global --add-source src/HTTPie/bin/Release dotnet-httpie
dotnet-http --help
dotnet-http https://httpbin.org/get --offline
```

### Manual Validation Checklist

After making changes, verify these scenarios:

1. **CLI Help**: `dotnet-http --help` — all options display correctly
2. **Offline request**: `dotnet-http https://httpbin.org/get --offline` — request is formatted correctly
3. **HTTP file execution**: `dotnet-http exec tests/HTTPie.IntegrationTest/TestAssets/HttpStartedSample.http --offline`
4. **Package installation**: global tool installs and `dotnet-http` command responds

## Code Style Guidelines

- **Language**: C# with `LangVersion=preview` (set in `Directory.Build.props`)
- **Nullable reference types**: enabled everywhere
- **Implicit usings**: enabled; common WeihanLi.Common namespaces are globally imported
- **File headers**: every `.cs` file must begin with:
  ```csharp
  // Copyright (c) Weihan Li. All rights reserved.
  // Licensed under the MIT license.
  ```
- **Namespaces**: file-scoped namespace declarations (`namespace Foo;`)
- **Primary constructors**: preferred where applicable
- **`var`**: preferred for all local variable declarations
- **Indentation**: 4 spaces for C# files, 2 spaces for XML/JSON project files
- **Newline**: open braces on new lines (`csharp_new_line_before_open_brace = all`)
- **Sorting**: system `using` directives are **not** sorted first
- Run `dotnet format` to automatically fix formatting issues

## CI/CD

### Workflows

| Workflow | Trigger | Description |
|----------|---------|-------------|
| `dotnet.yml` | Push to `main`/`preview`/`dev`, PRs to `dev` | Build on macOS, Linux, Windows |
| `dotnet-format.yml` | Push to `main`/`dev` | Auto-formats code and commits changes |
| `release.yml` | Push to `main` | Builds AOT binaries for all platforms and creates a GitHub Release |
| `build-aot-tool.yml` | Manual / scheduled | Builds AOT-compiled tool packages |
| `dotnet-outdated.yml` | Scheduled | Checks for outdated NuGet packages |

### Branch Strategy

- `main` — production / releases
- `dev` — active development, CI builds and PRs target this branch
- `preview` — preview/pre-release builds

## Pull Request Guidelines

- Target the **`dev`** branch for feature and bug-fix PRs
- Ensure `dotnet build` and `dotnet test` pass locally before opening a PR
- The pre-commit hook (`.husky/`) automatically runs `dotnet build`
- Code formatting is enforced via `dotnet format` — run it before committing

### Commit Message Convention

Follow the [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) specification:

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

Common types:

| Type | When to use |
|------|-------------|
| `feat` | A new feature |
| `fix` | A bug fix |
| `docs` | Documentation changes only |
| `style` | Formatting changes (no logic change) |
| `refactor` | Code restructuring (no feature or fix) |
| `test` | Adding or updating tests |
| `chore` | Build process, dependency updates, tooling |
| `perf` | Performance improvements |
| `ci` | CI/CD workflow changes |

Examples:

```
feat(exec): support .rest file extension in exec command
fix(middleware): handle null response body in logging middleware
docs: update installation instructions in README
chore: bump WeihanLi.Common to 1.0.87
```

- Use the **imperative mood** in the description ("add" not "added")
- Keep the first line at 72 characters or fewer
- Reference issues in the footer: `Closes #123`

## Debugging and Troubleshooting

### Common Issues

| Symptom | Cause | Fix |
|---------|-------|-----|
| `error MSB4236: The SDK 'Microsoft.NET.Sdk' was not found` or unrecognized `.slnx` element | .NET 8 or older SDK | Install .NET 10 SDK |
| `Framework 'Microsoft.NETCore.App', version '10.0.x' was not found` | .NET 10 runtime missing | Install .NET 10 SDK/runtime, set `DOTNET_ROOT` |
| `dotnet-execute build script fails` | Compatibility issue with dotnet-execute and .NET 10 | Use `dotnet build` directly instead of `./build.sh` |
| Integration tests fail | No network connectivity | Run only unit tests: `dotnet test tests/HTTPie.UnitTest/HTTPie.UnitTest.csproj` |
| AOT publish errors (trim warnings) | New reflection-based code paths | Use source generators or annotate with `[DynamicallyAccessedMembers]` |

### Useful Flags

```bash
dotnet-http --debug    # verbose request/response logging
dotnet-http --offline  # preview the request without sending it (no network needed)
dotnet-http --env dev  # load variables from the "dev" environment in http-client.env.json
```
