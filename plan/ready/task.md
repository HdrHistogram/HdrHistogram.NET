# Task List: Upgrade BenchmarkDotNet from 0.13.12 to 0.15.8 (Issue #132)

## Context

The `HdrHistogram.Benchmarking` project must have both `BenchmarkDotNet` and `BenchmarkDotNet.Diagnostics.Windows` upgraded from `0.13.12` to `0.15.8`.
The upgrade spans two minor versions and may introduce breaking API changes or new Roslyn analyser diagnostics.
Verification is build-based — no unit test changes are required.

---

## Tasks

### 1. Update package references in the project file

- [ ] **Task 1** — In `HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj` line 9, change `Version="0.13.12"` to `Version="0.15.8"` for the `BenchmarkDotNet` `<PackageReference>`.
  - File: `HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj`
  - Why: This is the primary package that provides the benchmarking framework.
  - Verify: The element reads `<PackageReference Include="BenchmarkDotNet" Version="0.15.8" />`.

- [ ] **Task 2** — In `HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj` lines 10–12, change `<Version>0.13.12</Version>` to `<Version>0.15.8</Version>` for the `BenchmarkDotNet.Diagnostics.Windows` `<PackageReference>`.
  - File: `HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj`
  - Why: Companion package; must always match the main `BenchmarkDotNet` version exactly.
  - Verify: The element reads `<PackageReference Include="BenchmarkDotNet.Diagnostics.Windows"><Version>0.15.8</Version></PackageReference>`.

---

### 2. Restore packages

- [ ] **Task 3** — Run `dotnet restore HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj` and confirm it exits with code 0.
  - File: `HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj`
  - Why: Pulls the `0.15.8` NuGet packages from the feed before attempting compilation.
  - Verify: Command output shows no errors; `BenchmarkDotNet 0.15.8` appears in restored packages.

---

### 3. Attempt first build and capture diagnostics

- [ ] **Task 4** — Run `dotnet build HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj -c Release` and capture the full compiler output.
  - File: `HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj` (and referenced source files)
  - Why: Surfaces any compilation errors or new analyser warnings-as-errors introduced by the version bump before attempting fixes.
  - Verify: Output is captured and reviewed for error codes and messages.

---

### 4. Investigate and fix breaking changes in `Program.cs`

`Program.cs` configures jobs targeting several runtimes that may have been removed or renamed in BenchmarkDotNet 0.14/0.15.

- [ ] **Task 5** — Check whether `ClrRuntime.Net481` still exists in `BenchmarkDotNet.Environments` in 0.15.8; if it has been removed or renamed, update `Program.cs` accordingly.
  - File: `HdrHistogram.Benchmarking/Program.cs` line 19–20
  - Why: `ClrRuntime.Net481` and `Jit.LegacyJit` were historically only supported on Windows; BenchmarkDotNet 0.15.x may have removed or deprecated support for these.
  - Verify: No `CS0117` / `CS0103` compiler error referencing `ClrRuntime` or `Jit.LegacyJit`.

- [ ] **Task 6** — Check whether `CoreRuntime.Core21`, `CoreRuntime.Core31`, `CoreRuntime.Core50` still exist in 0.15.8; if any have been removed as EOL runtimes, remove or replace those `.AddJob(...)` calls in `Program.cs`.
  - File: `HdrHistogram.Benchmarking/Program.cs` lines 21–23
  - Why: BenchmarkDotNet may drop constants for end-of-life .NET versions; compilation will fail if the symbols no longer exist.
  - Verify: No `CS0117` compiler error referencing `CoreRuntime.Core21`, `CoreRuntime.Core31`, or `CoreRuntime.Core50`.

- [ ] **Task 7** — Check whether `StatisticColumn.P0`, `StatisticColumn.Q1`, `StatisticColumn.P67`, `StatisticColumn.Q3`, `StatisticColumn.P80`, `StatisticColumn.P90`, `StatisticColumn.P95`, `StatisticColumn.P100` still exist in `BenchmarkDotNet.Columns` in 0.15.8; if any have been renamed or removed, update `Program.cs`.
  - File: `HdrHistogram.Benchmarking/Program.cs` line 18
  - Why: Column name constants occasionally change between minor versions.
  - Verify: No `CS0117` compiler error referencing any `StatisticColumn.*` member.

- [ ] **Task 8** — Check whether `ManualConfig.Create`, `DefaultConfig.Instance`, `BenchmarkSwitcher`, and `switcher.Run` APIs remain present and have the same signatures in 0.15.8; fix any signature mismatches if they do not.
  - File: `HdrHistogram.Benchmarking/Program.cs` lines 13, 29, 34
  - Why: Core configuration and runner APIs are historically stable but could have changed in two minor versions.
  - Verify: No `CS1061`, `CS0246`, or overload-resolution errors on these call sites.

---

### 5. Investigate and fix breaking changes in benchmark source files

- [ ] **Task 9** — Check whether `[BenchmarkDotNet.Attributes.GlobalSetup]` and `[Benchmark]` attributes remain unchanged in the BenchmarkDotNet 0.15.8 `Attributes` namespace; fix any namespace or attribute-name changes if present.
  - File: `HdrHistogram.Benchmarking/LeadingZeroCount/LeadingZeroCountBenchmarkBase.cs` line 49 and benchmark methods
  - Why: Attributes are the primary extension point used in this file; a rename would cause compilation failure.
  - Verify: No `CS0246` errors referencing `GlobalSetup` or `Benchmark` attributes.

- [ ] **Task 10** — Confirm `[Benchmark(Baseline = true, OperationsPerInvoke = ...)]` constructor parameters are still valid in 0.15.8; fix any parameter changes if present.
  - File: `HdrHistogram.Benchmarking/Recording/Recording32BitBenchmark.cs` (all `[Benchmark]` attribute usages)
  - Why: Named constructor parameters can be removed or renamed across minor versions.
  - Verify: No `CS0246`, `CS1739`, or `CS1503` errors on `[Benchmark(...)]` usages.

---

### 6. Fix new Roslyn analyser diagnostics

- [ ] **Task 11** — After the first successful compilation pass, review all warnings and determine whether any are promoted to errors under `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>` and `<AnalysisLevel>latest-recommended</AnalysisLevel>` (set in `Directory.Build.props`); fix any new violations in the benchmarking source files.
  - Files: `HdrHistogram.Benchmarking/Program.cs`, `HdrHistogram.Benchmarking/LeadingZeroCount/LeadingZeroCountBenchmarkBase.cs`, `HdrHistogram.Benchmarking/Recording/Recording32BitBenchmark.cs`
  - Why: BenchmarkDotNet 0.15.x ships new Roslyn analysers; existing code may trigger rules that are elevated to errors by the project's global settings.
  - Verify: Build output contains zero `error` lines.

---

### 7. Verify benchmarking project build succeeds

- [ ] **Task 12** — Run `dotnet build HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj -c Release` and confirm exit code 0 with no errors and no warnings treated as errors.
  - File: `HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj`
  - Why: Directly satisfies acceptance criterion 3.
  - Verify: Terminal output ends with `Build succeeded` and `0 Error(s)`.

---

### 8. Verify full solution build succeeds

- [ ] **Task 13** — Run `dotnet build HdrHistogram.sln -c Release` and confirm exit code 0 with no errors across all four projects (`HdrHistogram`, `HdrHistogram.Examples`, `HdrHistogram.UnitTests`, `HdrHistogram.Benchmarking`).
  - File: `HdrHistogram.sln`
  - Why: Directly satisfies acceptance criterion 4; catches regressions in sibling projects caused by shared `Directory.Build.props` changes, if any.
  - Verify: Terminal output ends with `Build succeeded` and `0 Error(s)`.

---

### 9. Dry-run benchmark discovery

- [ ] **Task 14** — Run `dotnet run --project HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj -c Release -- --list flat` and confirm BenchmarkDotNet enumerates all benchmarks without crashing.
  - File: `HdrHistogram.Benchmarking/Program.cs`
  - Why: Validates runtime initialisation — a build success does not guarantee that BenchmarkDotNet can discover and enumerate benchmarks at start-up.
  - Verify: Output lists at least the `LeadingZeroCount` and `Recording` benchmark classes; process exits with code 0.

---

### 10. Update documentation

- [ ] **Task 15** — In `spec/tech-standards/build-system.md`, replace both occurrences of `0.13.12` in the BenchmarkDotNet `<PackageReference>` XML block with `0.15.8`.
  - File: `spec/tech-standards/build-system.md` lines 66–67
  - Why: Directly satisfies acceptance criterion 5; prevents stale documentation drift.
  - Verify: `grep "0.13.12" spec/tech-standards/build-system.md` returns no matches; `grep "0.15.8" spec/tech-standards/build-system.md` returns the updated lines.

---

## Acceptance Criteria Cross-Reference

| Acceptance criterion (from brief) | Covered by task(s) |
|---|---|
| `BenchmarkDotNet` version in `.csproj` is `0.15.8` | Task 1 |
| `BenchmarkDotNet.Diagnostics.Windows` version in `.csproj` is `0.15.8` | Task 2 |
| `dotnet build HdrHistogram.Benchmarking/...csproj -c Release` exits 0, no errors | Tasks 3–12 |
| `dotnet build HdrHistogram.sln -c Release` exits 0 | Task 13 |
| `spec/tech-standards/build-system.md` references `0.15.8`, not `0.13.12` | Task 15 |
| No existing benchmark class logic changed unless forced by breaking change | Tasks 5–10 (scope-limited fixes only) |
