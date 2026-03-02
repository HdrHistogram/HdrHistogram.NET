# Issue #132: Upgrade BenchmarkDotNet from 0.13.12 to 0.15.8

## Summary

The benchmarking project (`HdrHistogram.Benchmarking`) currently references BenchmarkDotNet **0.13.12**.
The target version is **0.15.8**.
Both `BenchmarkDotNet` and `BenchmarkDotNet.Diagnostics.Windows` must be updated together, as they are companion packages that must always share the same version.

The upgrade spans two minor versions (0.13 → 0.14 → 0.15) and brings .NET 10 support, new Roslyn analysers for compile-time correctness checking, a WakeLock feature to prevent system sleep during benchmarks, improved engine internals, and numerous bug fixes.

## Affected Files

The following files are confirmed by exploration:

### Primary change

- `HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj` — contains the two `<PackageReference>` elements to update from `0.13.12` to `0.15.8`.

### Source files that import BenchmarkDotNet APIs (may need fixes if breaking changes exist)

- `HdrHistogram.Benchmarking/Program.cs` — runner entry point; uses `BenchmarkSwitcher` / `BenchmarkRunner` configuration.
- `HdrHistogram.Benchmarking/LeadingZeroCount/LeadingZeroCountBenchmarkBase.cs` — base class with `[Benchmark]` attributes and benchmark methods.
- `HdrHistogram.Benchmarking/Recording/Recording32BitBenchmark.cs` — benchmark class with `[Benchmark]` attributes.

### Documentation that records the version number

- `spec/tech-standards/build-system.md` — lists `BenchmarkDotNet` version `0.13.12` in the Dependencies section; must be updated to `0.15.8` after the code change is verified.

## What Needs to Change

1. In `HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj`, change both package versions from `0.13.12` to `0.15.8`.
2. Resolve any breaking-change compilation errors or new Roslyn analyser warnings that are emitted as errors under the existing `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>` and `<AnalysisLevel>latest-recommended</AnalysisLevel>` settings inherited from `Directory.Build.props`.
3. Update `spec/tech-standards/build-system.md` to record the new version.

## Acceptance Criteria

- [ ] `BenchmarkDotNet` version in `HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj` is `0.15.8`.
- [ ] `BenchmarkDotNet.Diagnostics.Windows` version in the same file is `0.15.8`.
- [ ] `dotnet build HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj -c Release` exits with code 0 and no errors or warnings treated as errors.
- [ ] `dotnet build HdrHistogram.sln -c Release` exits with code 0 (full solution build is clean).
- [ ] `spec/tech-standards/build-system.md` no longer references `0.13.12`; it references `0.15.8` instead.
- [ ] No existing benchmark class logic is changed unless forced by a BenchmarkDotNet API breaking change.

## Test Strategy

BenchmarkDotNet is a developer tool, not shipped library code, so there are no unit tests to add or modify.
Verification is build-based:

1. Run `dotnet restore HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj` to pull the new packages.
2. Run `dotnet build HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj -c Release` and confirm zero errors and zero warnings-as-errors.
3. Run `dotnet build HdrHistogram.sln -c Release` to confirm the full solution still compiles cleanly.
4. Optionally run `dotnet run --project HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj -c Release -- --list flat` (dry-run discovery) to confirm BenchmarkDotNet can enumerate all benchmarks without crashing at start-up.

No new test files are required.

## Risks and Open Questions

### Breaking changes (two-minor-version jump)

BenchmarkDotNet follows semantic versioning loosely.
The 0.14.x series and 0.15.x series may contain breaking changes to public configuration APIs used in `Program.cs`.
The `BenchmarkRunner.Run` / `BenchmarkSwitcher.FromAssembly` surface is historically stable, but job/column/exporter configuration APIs have changed in the past.
Risk: **low to medium** — inspect compiler output after the version bump to surface any issues.

### New Roslyn analysers

BenchmarkDotNet 0.15.x ships new compile-time analysers.
Under `<AnalysisLevel>latest-recommended</AnalysisLevel>` and `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>` (set in `Directory.Build.props`), new analyser warnings may be promoted to errors.
If this occurs, the benchmark source files must be corrected to satisfy the analysers.
Risk: **low** — the existing benchmark code is simple and unlikely to trigger new rules.

### `BenchmarkDotNet.Diagnostics.Windows` on non-Windows

`BenchmarkDotNet.Diagnostics.Windows` is a conditional dependency.
On Linux (WSL2, CI), the package must still restore and build without error even though its ETW-based diagnostics cannot run.
BenchmarkDotNet has historically handled this gracefully; no change is expected.
Risk: **very low**.

### `spec/tech-standards/build-system.md` drift

The spec file hard-codes the package version.
It must be updated as part of this issue to prevent stale documentation.
This is a documentation-only risk; no code impact.
