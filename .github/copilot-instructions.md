# dotnet-httpie

A command-line HTTP client for .NET, providing a user-friendly alternative to curl for API testing and debugging. This tool can be installed as a global .NET tool or run as a Docker container.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Build and Test Process

- **NEVER CANCEL builds or tests** - Builds and tests typically complete quickly. Set timeout to 60+ seconds.
- **Recommended Build Method**: `./build.sh` (uses dotnet-execute, builds and runs tests automatically)
- **Alternative Build Methods**:
  ```bash
  dotnet build                    # Direct dotnet CLI
  dotnet build dotnet-httpie.slnx # Specify solution file explicitly
  ```
- **Individual Test Commands**:
  ```bash
  dotnet test tests/HTTPie.UnitTest/HTTPie.UnitTest.csproj      # Unit tests
  dotnet test tests/HTTPie.IntegrationTest/HTTPie.IntegrationTest.csproj # Integration tests
  ```
- **Package the tool**:
  ```bash
  dotnet pack src/HTTPie/HTTPie.csproj --configuration Release
  ```

### Running the Application

- Run in development:
  ```bash
  dotnet run --project src/HTTPie/HTTPie.csproj --framework net10.0 -- --help
  ```

- Install as global tool:
  ```bash
  dotnet tool install --global --add-source src/HTTPie/bin/Release dotnet-httpie
  dotnet-http --help
  ```

- Test basic functionality:
  ```bash
  # Test in offline mode (no network required)
  dotnet-http https://httpbin.org/get --offline
  
  # Execute .http files (test assets available)
  dotnet-http exec tests/HTTPie.IntegrationTest/TestAssets/HttpStartedSample.http --offline
  ```

## Validation

- **ALWAYS run complete build and test suite** before submitting changes
- Build validation commands that MUST pass:
  ```bash
  dotnet build # build
  dotnet test # run test cases  
  dotnet pack src/HTTPie/HTTPie.csproj # pack artifacts
  ```
- **MANUAL VALIDATION SCENARIOS**: After code changes, test these workflows:
  1. **CLI Help**: `dotnet-http --help` - verify all options display correctly
  2. **HTTP Request**: `dotnet-http https://httpbin.org/get --offline` - verify request formatting
  3. **HTTP File Execution**: `dotnet-http exec tests/HTTPie.IntegrationTest/TestAssets/HttpStartedSample.http --offline` - verify .http file parsing
  4. **Package Installation**: Install as global tool and verify `dotnet-http` command works
- Integration tests should pass when network connectivity is available

## Key Project Structure

### Repository Layout
```
/
├── .github/workflows/     # CI/CD pipelines (dotnet.yml is main build)
├── src/HTTPie/           # Main application project (multi-targets net8.0;net10.0)
├── tests/                # Test projects
│   ├── HTTPie.UnitTest/  # Unit tests (all should pass)
│   └── HTTPie.IntegrationTest/  # Integration tests
├── build/                # Build scripts and dotnet-execute configuration
├── docs/                 # Release notes and documentation
├── dotnet-httpie.slnx   # Solution file (requires .NET 10 SDK)
├── build.sh             # Build script using dotnet-execute
└── .husky/              # Git hooks (pre-commit runs dotnet build)
```

### Important Files
- `src/HTTPie/HTTPie.csproj` - Main project, packaged as global tool (`dotnet-http` command)
- `tests/HTTPie.IntegrationTest/TestAssets/` - Sample .http files for testing exec functionality
- `build/build.cs` - Build script executed by dotnet-execute tool
- `Directory.Build.props` - Common MSBuild properties (sets LangVersion to preview)

### Technical Details
- **Target Frameworks**: net8.0 and net10.0 (multi-targeting enabled)
- **Package Output**: `src/HTTPie/bin/Release/dotnet-httpie.{version}.nupkg`
- **AOT Compilation**: Enabled for Release builds on .NET 10 (PublishAot=true)

## Common Development Tasks

### Complete Development Workflow
```bash
# 1. Build and test (recommended - runs both build and tests)
./build.sh

# Alternative: Build and test separately
# dotnet build
# dotnet test

# 2. Test functionality
dotnet run --project src/HTTPie/HTTPie.csproj --framework net10.0 -- --help
dotnet run --project src/HTTPie/HTTPie.csproj --framework net10.0 -- https://httpbin.org/get --offline

# 3. Package and install
dotnet pack src/HTTPie/HTTPie.csproj --configuration Release
dotnet tool install --global --add-source src/HTTPie/bin/Release dotnet-httpie --version {version}
```

### Environment Setup Verification
```bash
# Verify .NET installation
dotnet --list-sdks  # Should show 10.0.x
dotnet --list-runtimes  # Should show Microsoft.NETCore.App 10.0.x

# Verify tools
dotnet-exec --info  # Should show dotnet-execute tool info
which dotnet-http   # Should show path after global tool install
```

## Troubleshooting

### Git Workflow
- Pre-commit hook automatically runs `dotnet build` 
- Always ensure builds pass before pushing
- CI/CD runs on macOS, Linux, and Windows with .NET 10 SDK

### Common Issues
- **Build fails with "unrecognized Solution element"**: Install .NET 10 SDK, .NET 8 doesn't support .slnx format
- **Tests fail with "Framework not found"**: Ensure .NET 10 runtime is installed and DOTNET_ROOT is set
- **dotnet-execute build script fails**: The tool has compatibility issues with .NET 10 preview, use direct `dotnet build` instead
- **Integration tests fail**: Ensure network connectivity is available for external service calls

This project is a modern .NET tool that showcases advanced features like multi-targeting, AOT compilation, and global tool packaging. Always test the complete user workflow after making changes.
