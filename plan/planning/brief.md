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
| `.github/workflows/ci.yml` | `dotnet-version: 8.0.x` | `8.0.x`, `9.0.x`, `10.0.x` (multi-line) |
| `spec/tech-standards/build-system.md` | Documents `net8.0;netstandard2.0` | Update to reflect new targets |

## Conditional Compilation

Only one `#if` guard exists in the main library:

- `HdrHistogram/Utilities/Bitwise.cs`: `#if NET5_0_OR_GREATER` — uses `System.Numerics.BitOperations.LeadingZeroCount()`.

This guard already applies correctly to net8.0, net9.0, and net10.0.
No new `#if` directives are required.
No `#if NET9_0_OR_GREATER` or `#if NET10_0_OR_GREATER` optimisations have been identified as necessary for this changeset.

## Acceptance Criteria

- [ ] `HdrHistogram.csproj` targets `net10.0;net9.0;net8.0;netstandard2.0`
- [ ] `HdrHistogram.UnitTests.csproj` targets `net10.0;net9.0;net8.0`
- [ ] `HdrHistogram.Examples.csproj` targets `net10.0`
- [ ] `HdrHistogram.Benchmarking.csproj` targets `net10.0;net9.0;net8.0`
- [ ] CI installs .NET SDK 8.0.x, 9.0.x, and 10.0.x
- [ ] `dotnet build -c Release` succeeds for all target frameworks
- [ ] `dotnet test` passes on all three modern runtimes (net8.0, net9.0, net10.0)
- [ ] `dotnet pack` produces a NuGet package containing assemblies for all four targets
- [ ] No regressions — all existing tests pass on all targets
- [ ] `spec/tech-standards/build-system.md` updated to reflect the new target frameworks

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

- **net10.0 SDK availability**: The devcontainer currently uses a .NET 9.0 base image with an additional 8.0 runtime.
  A .NET 10.0 SDK must be available in CI (GitHub Actions `setup-dotnet@v4` supports `10.0.x`) and locally.
  If the devcontainer does not have .NET 10 installed, local builds targeting `net10.0` will fail.
  The CI step is the authoritative build environment; local failure is acceptable during transition.

- **BenchmarkDotNet compatibility**: `BenchmarkDotNet` version `0.13.12` (currently referenced) must support net10.0.
  If it does not, the version pin may need updating.
  This should be verified during implementation by attempting a Release build of the benchmarking project.

- **Examples project**: The issue specifies updating to `net10.0` only (single target, not multi-target).
  This is intentional — examples are a developer-facing runnable tool, not a shipped library.

- **Spec update**: `spec/tech-standards/build-system.md` still documents AppVeyor CI.
  That section was already outdated before this issue.
  Only the target framework tables and CI setup sections need updating here; AppVeyor cleanup is out of scope.
