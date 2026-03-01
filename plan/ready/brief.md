# Brief: Issue #119 — Add support for all currently supported .NET frameworks (net9.0, net10.0)

## Summary

The library currently targets `net8.0;netstandard2.0`.
As of March 2026, .NET 9 (STS) and .NET 10 (LTS) are both in active support alongside .NET 8 (LTS).
This issue adds `net9.0` and `net10.0` as target frameworks across the library, test, examples, benchmarking, and CI projects.
No code logic changes are expected — the existing `#if NET5_0_OR_GREATER` guard in `Bitwise.cs` already covers all three modern targets.
The spec file `spec/tech-standards/build-system.md` must also be updated to reflect the new targets.

## Affected Files (confirmed by exploration)

| File | Current value | Required value |
|------|--------------|----------------|
| `HdrHistogram/HdrHistogram.csproj` | `net8.0;netstandard2.0` | `net10.0;net9.0;net8.0;netstandard2.0` |
| `HdrHistogram.UnitTests/HdrHistogram.UnitTests.csproj` | `net8.0` | `net10.0;net9.0;net8.0` |
| `HdrHistogram.Examples/HdrHistogram.Examples.csproj` | `net8.0` | `net10.0` |
| `HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj` | `net8.0` | `net10.0;net9.0;net8.0` |
| `.github/workflows/ci.yml` | `dotnet-version: 8.0.x` | `8.0.x`, `9.0.x`, `10.0.x` (multi-line — see CI section below) |
| `.devcontainer/Dockerfile` | `sdk:9.0-bookworm-slim` + 8.0 runtime | `sdk:10.0-bookworm-slim` + explicit 8.0 and 9.0 runtimes |
| `spec/tech-standards/build-system.md` | Documents `net8.0;netstandard2.0` | Update five specific sections (see Spec Update section below) |

## Conditional Compilation

Only one `#if` guard exists in the main library:

- `HdrHistogram/Utilities/Bitwise.cs`: `#if NET5_0_OR_GREATER` — uses `System.Numerics.BitOperations.LeadingZeroCount()`.

This guard already applies correctly to net8.0, net9.0, and net10.0.
No new `#if` directives are required.
No `#if NET9_0_OR_GREATER` or `#if NET10_0_OR_GREATER` optimisations have been identified as necessary for this changeset.

## CI YAML Change

The `actions/setup-dotnet@v4` action supports a multi-line scalar for `dotnet-version`.
Use the following exact syntax in `.github/workflows/ci.yml`:

```yaml
- uses: actions/setup-dotnet@v4
  with:
    dotnet-version: |
      8.0.x
      9.0.x
      10.0.x
    cache: true
    cache-dependency-path: '**/*.csproj'
```

Do not use separate `setup-dotnet` steps for each version — this would break `cache: true` behaviour.

## Devcontainer Change

The current `.devcontainer/Dockerfile` is based on `mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim` and installs only the .NET 8.0 *runtime*.
The Examples project is a developer-facing runnable tool; after changing its target to `net10.0`, it must remain buildable inside the devcontainer.

**Resolution (Option A):** Change the base image to `mcr.microsoft.com/dotnet/sdk:10.0-bookworm-slim` and add explicit installation of the 8.0 and 9.0 runtimes using the `dotnet-install.sh` script, so all three runtime versions remain available locally.

## Spec Update: Sections to Change in `spec/tech-standards/build-system.md`

The following five locations must be updated; all other sections (including the AppVeyor section, lines 137–177, which is already acknowledged as outdated) are out of scope:

| Approx. Line | Section | Change |
|---|---|---|
| 24–26 | Main Library TargetFrameworks | `net8.0;netstandard2.0` → `net10.0;net9.0;net8.0;netstandard2.0` |
| 35–37 | Test Project TargetFramework | `net8.0` → `net10.0;net9.0;net8.0` |
| 42–45 | Benchmarking Project TargetFrameworks | `net8.0` → `net10.0;net9.0;net8.0` |
| 226–228 | Benchmark Configuration list | "net8.0 (current LTS runtime)" → list all three runtimes |
| 254 | Prerequisites | ".NET 8.0 SDK (or later)" → ".NET 10.0 SDK (or later)" |

The Examples project section is absent from the spec, so no spec change is needed for that project.

## Acceptance Criteria

- [ ] `HdrHistogram.csproj` targets `net10.0;net9.0;net8.0;netstandard2.0`
- [ ] `HdrHistogram.UnitTests.csproj` targets `net10.0;net9.0;net8.0`
- [ ] `HdrHistogram.Examples.csproj` targets `net10.0`
- [ ] `HdrHistogram.Benchmarking.csproj` targets `net10.0;net9.0;net8.0`
- [ ] CI installs .NET SDK 8.0.x, 9.0.x, and 10.0.x using a single `setup-dotnet` step with a multi-line `dotnet-version` scalar
- [ ] `.devcontainer/Dockerfile` uses `sdk:10.0-bookworm-slim` base image with 8.0 and 9.0 runtimes installed
- [ ] `dotnet build -c Release` succeeds for all target frameworks
- [ ] `dotnet test` passes on all three modern runtimes (net8.0, net9.0, net10.0)
- [ ] `dotnet pack` produces a NuGet package containing assemblies for all four targets
- [ ] No regressions — all existing tests pass on all targets
- [ ] `spec/tech-standards/build-system.md` updated at the five specific locations listed above

## Test Strategy

No new tests need to be written.
The existing test suite in `HdrHistogram.UnitTests/` provides full coverage.
Multi-targeting the test project is sufficient: `dotnet test` automatically runs all tests against each `<TargetFrameworks>` entry.
The CI pipeline, once updated to install all three SDKs, will exercise net8.0, net9.0, and net10.0 in a single `dotnet test` invocation.

Verification locally:

```bash
dotnet build HdrHistogram/HdrHistogram.csproj -c Release
dotnet test HdrHistogram.UnitTests/HdrHistogram.UnitTests.csproj -c Release
dotnet pack HdrHistogram/HdrHistogram.csproj -c Release --no-build
```

After `dotnet pack`, confirm the `.nupkg` contains `lib/net8.0/`, `lib/net9.0/`, `lib/net10.0/`, and `lib/netstandard2.0/` folders.

## Risks and Open Questions

- **BenchmarkDotNet compatibility**: `BenchmarkDotNet` version `0.13.12` (currently referenced) must support net10.0.
  Verify during implementation by attempting a Release build of the benchmarking project.
  If `BenchmarkDotNet 0.13.12` fails to build against `net10.0`, upgrade both `BenchmarkDotNet` and `BenchmarkDotNet.Diagnostics.Windows` to the latest stable version as part of this PR and update the version recorded in `spec/tech-standards/build-system.md` accordingly.

- **Examples project**: The issue specifies updating to `net10.0` only (single target, not multi-target).
  This is intentional — examples are a developer-facing runnable tool, not a shipped library.
  The devcontainer change (see above) ensures local buildability is preserved.

- **Spec update**: `spec/tech-standards/build-system.md` still documents AppVeyor CI.
  That section was already outdated before this issue.
  Only the five target framework and prerequisite locations listed above need updating here; AppVeyor cleanup is out of scope.
