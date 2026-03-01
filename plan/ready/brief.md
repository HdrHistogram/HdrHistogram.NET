# Brief: Issue #105 — Remove EOL Target Frameworks from Benchmarking Project

## Summary

The `HdrHistogram.Benchmarking` project currently targets seven frameworks, five of which are end-of-life.
This causes repeated `NETSDK1138` warnings during CI builds.
Since the benchmarking project is a developer tool for measuring current performance — not a shipped library — it has no reason to target EOL runtimes.
The fix is to reduce `TargetFrameworks` to `net8.0` only and update the build-system spec to match.

## Affected Files

| File | Change |
|------|--------|
| `HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj` | Replace `TargetFrameworks` with `net8.0` only |
| `spec/tech-standards/build-system.md` | Update the Benchmarking Project TFM block (lines 39–45) and the Benchmark Configuration narrative bullet (lines 226–229) to reflect the new target |

No other projects are affected: `HdrHistogram.csproj` already targets `net8.0;netstandard2.0`, and both `HdrHistogram.UnitTests` and `HdrHistogram.Examples` already target `net8.0` only.
No CI workflow changes are required because the existing `dotnet-version: '8.0.x'` pin already covers `net8.0`.

## Current State

```xml
<!-- HdrHistogram.Benchmarking.csproj -->
<TargetFrameworks>net8.0;net7.0;net6.0;net5.0;net47;netcoreapp3.1;netcoreapp2.1.29</TargetFrameworks>
```

EOL frameworks in the list (all EOL as of 2026-03-01):

- `net7.0` — EOL May 2024
- `net6.0` — EOL November 2024
- `net5.0` — EOL May 2022
- `net47` — No mainstream SDK support under modern .NET SDK (framework-era)
- `netcoreapp3.1` — EOL December 2022
- `netcoreapp2.1.29` — EOL August 2021 (pinned patch version, non-standard)

## Target State

```xml
<TargetFrameworks>net8.0</TargetFrameworks>
```

Rationale:

- `net8.0` is LTS, supported until November 2026; it is already the SDK version used by CI.
- `net9.0` reaches end-of-life in May 2026 — approximately two months from the date of this brief — so it does not meet the bar for inclusion.
- A single supported LTS framework is the correct minimum for a developer-only benchmarking tool.

## Acceptance Criteria

- [ ] `HdrHistogram.Benchmarking.csproj` contains no EOL target frameworks.
- [ ] `dotnet build -c Release` completes with zero `NETSDK1138` warnings.
- [ ] `dotnet build -c Release` completes successfully for the whole solution.
- [ ] The `spec/tech-standards/build-system.md` Benchmarking Project TFM block reflects `net8.0` only.
- [ ] The `spec/tech-standards/build-system.md` Benchmark Configuration section accurately describes the final `TargetFrameworks` value (the "Multiple .NET versions for comparison" bullet is updated or removed to reflect a single target).

## Test Strategy

The benchmarking project contains no unit tests.
Verification is build-only:

1. Run `dotnet build -c Release` from the repo root and confirm no `NETSDK1138` warnings appear.
2. Confirm the build succeeds with exit code 0.
3. Run `dotnet build -c Release` for the full solution to confirm no regressions in other projects.

No new tests need to be added or modified.

## Risks and Open Questions

| Item | Detail |
|------|--------|
| `BenchmarkDotNet.Diagnostics.Windows` on Linux CI | This package targets Windows; the CI runner is `ubuntu-latest`. Building for `net8.0` on Linux should still succeed as the package conditionally applies. Verify no new build errors arise after removing the TFM list. |
| `net47` removal | `net47` is .NET Framework 4.7, not .NET Core. Removing it means the benchmarking project no longer builds a .NET Framework binary. This is intentional and consistent with the issue goal. |
