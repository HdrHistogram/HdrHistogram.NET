# Build System

> Build configuration, CI/CD, and packaging for HdrHistogram.NET.

## Project Format

- **SDK-style** `.csproj` format (Microsoft.NET.Sdk)
- Modern .NET project system with implicit dependencies

## Solution Structure

```
HdrHistogram.sln
├── HdrHistogram/                   # Main library
├── HdrHistogram.UnitTests/         # Unit tests
├── HdrHistogram.Examples/          # Example applications
└── HdrHistogram.Benchmarking/      # Performance benchmarks
```

## Target Frameworks

### Main Library (HdrHistogram.csproj)

```xml
<TargetFrameworks>net8.0;netstandard2.0</TargetFrameworks>
```

| Target | Description |
|--------|-------------|
| `net8.0` | Modern .NET (primary target) |
| `netstandard2.0` | Broad compatibility (.NET Framework 4.6.1+, .NET Core 2.0+) |

### Test Project

```xml
<TargetFramework>net8.0</TargetFramework>
```

### Benchmarking Project

Multi-targeted for performance comparison:

```xml
<TargetFrameworks>net8.0;net7.0;net6.0;net5.0;net47;netcoreapp3.1;netcoreapp2.1.29</TargetFrameworks>
```

## Dependencies

### Main Library

**Zero external dependencies** - The core library has no external package dependencies.

### Test Project

```xml
<PackageReference Include="FluentAssertions" Version="6.12.0" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.0.2" />
<PackageReference Include="xunit" Version="2.9.0" />
<PackageReference Include="Xunit.Combinatorial" Version="1.6.24" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.2.0" />
```

### Benchmarking Project

```xml
<PackageReference Include="BenchmarkDotNet" Version="0.13.12" />
<PackageReference Include="BenchmarkDotNet.Diagnostics.Windows" Version="0.13.12" />
```

## Package Metadata

```xml
<PropertyGroup>
  <Description>HdrHistogram supports low latency recording and analyzing of value distributions...</Description>
  <Authors>Gil Tene, Lee Campbell</Authors>
  <PackageTags>HdrHistogram HdrHistogram.NET Histogram Instrumentation</PackageTags>
  <PackageProjectUrl>https://github.com/HdrHistogram/HdrHistogram.NET</PackageProjectUrl>
  <PackageLicenseExpression>CC0-1.0</PackageLicenseExpression>
</PropertyGroup>
```

## Documentation Generation

XML documentation is generated for Release builds:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <DocumentationFile>bin\$(Configuration)\$(TargetFramework)\HdrHistogram.xml</DocumentationFile>
</PropertyGroup>
```

## Build Commands

### Local Development

```bash
# Restore dependencies
dotnet restore

# Build in Debug mode
dotnet build

# Build in Release mode
dotnet build -c Release

# Run tests
dotnet test

# Run tests in Release mode (recommended for accurate timing)
dotnet test -c Release

# Create NuGet package
dotnet pack -c Release
```

### Full Build Script (build.cmd)

```batch
@echo off
setlocal

set SemVer=2.0.0

dotnet restore
dotnet build -c=Release /p:Version=%SemVer%
dotnet test --no-build -c=Release
dotnet pack --no-build --include-symbols -c=Release

REM Run benchmarks
HdrHistogram.Benchmarking.exe -f *

endlocal
```

## Continuous Integration

### AppVeyor Configuration (appveyor.yml)

```yaml
image: Visual Studio 2022

version: 2.{build}.0

environment:
  semver: 2.5.{build}

for:
  - branches:
      only:
        - main
    environment:
      semver: 2.5.{build}

  - branches:
      except:
        - main
    environment:
      semver: 2.5.{build}-pr{appveyor_pull_request_number}

build_script:
  - dotnet restore
  - dotnet build -c=Release /p:Version=%semver%

test: off

after_build:
  - dotnet pack --no-build --include-symbols -c=Release /p:Version=%semver%

artifacts:
  - path: .\HdrHistogram\bin\Release\*.nupkg
    name: Packages

notifications:
  - provider: GitHubPullRequest
    on_build_success: true
    on_build_failure: true
```

### CI/CD Pipeline

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Restore   │ --> │    Build    │ --> │    Test     │ --> │    Pack     │
│   dotnet    │     │   Release   │     │   xUnit     │     │   NuGet     │
│   restore   │     │   /p:Ver    │     │             │     │  .nupkg     │
└─────────────┘     └─────────────┘     └─────────────┘     └─────────────┘
```

## Version Management

### Semantic Versioning

Format: `{major}.{minor}.{build}`

- **main branch**: `2.5.{build}` (release versions)
- **PR builds**: `2.5.{build}-pr{pr_number}` (pre-release versions)

### Setting Version

```bash
# Via command line
dotnet build /p:Version=2.5.123

# Via environment variable
set SemVer=2.5.123
dotnet build /p:Version=%SemVer%
```

## Benchmarking

### Running Benchmarks

```bash
# Navigate to benchmarking project
cd HdrHistogram.Benchmarking

# Run all benchmarks
dotnet run -c Release

# Run specific benchmark
dotnet run -c Release -- -f *Recording*
```

### Benchmark Configuration

BenchmarkDotNet is used with these targets:
- Multiple .NET versions for comparison
- Windows diagnostics support
- Memory allocation tracking

## Output Artifacts

### NuGet Package

```
HdrHistogram/bin/Release/
├── HdrHistogram.{version}.nupkg
└── HdrHistogram.{version}.symbols.nupkg
```

### Build Output

```
HdrHistogram/bin/{Configuration}/{TargetFramework}/
├── HdrHistogram.dll
├── HdrHistogram.xml (Release only)
└── HdrHistogram.deps.json
```

## Development Environment

### Prerequisites

- .NET 8.0 SDK (or later)
- Visual Studio 2022 (optional, for IDE development)
- Git

### IDE Support

The solution works with:
- Visual Studio 2022
- JetBrains Rider
- VS Code with C# extension

### Building from Command Line

```bash
# Clone repository
git clone https://github.com/HdrHistogram/HdrHistogram.NET.git
cd HdrHistogram.NET

# Build and test
dotnet build
dotnet test

# Create package
dotnet pack -c Release
```
