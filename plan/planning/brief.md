# Brief: Issue #105 — Remove EOL Target Frameworks from Benchmarking Project

## Summary

The `HdrHistogram.Benchmarking` project currently targets seven frameworks, five of which are end-of-life.
This causes repeated `NETSDK1138` warnings during CI builds.
Since the benchmarking project is a developer tool for measuring current performance — not a shipped library — it has no reason to target EOL runtimes.
The fix is to reduce `TargetFrameworks` to only currently supported frameworks and update the build-system spec to match.

## Affected Files

| File | Change |
|------|--------|
| `HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj` | Replace `TargetFrameworks` with supported frameworks only |
| `spec/tech-standards/build-system.md` | Update Benchmarking Project section to reflect new targets |

No other projects are affected: `HdrHistogram.csproj` already targets `net8.0;netstandard2.0`, and both `HdrHistogram.UnitTests` and `HdrHistogram.Examples` already target `net8.0` only.

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
<TargetFrameworks>net8.0;net9.0</TargetFrameworks>
```

Rationale:

- `net8.0` is LTS, supported until November 2026; it is already the SDK version used by CI.
- `net9.0` is the current STS release (supported until May 2026), giving a two-version comparison which is the primary value of multi-targeting in a benchmarking project.
- If `net9.0` is judged too close to its own EOL date at the time of merge, `net8.0` alone is an acceptable minimum.

## Acceptance Criteria

- [ ] `HdrHistogram.Benchmarking.csproj` contains no EOL target frameworks.
- [ ] `dotnet build -c Release` completes with zero `NETSDK1138` warnings.
- [ ] `dotnet build -c Release` completes successfully for the whole solution.
- [ ] `spec/tech-standards/build-system.md` Benchmarking Project section reflects the new `TargetFrameworks` value.

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
| `net9.0` SDK availability in CI | The CI workflow (`ci.yml`) pins `dotnet-version: 8.0.x`. If `net9.0` is added as a target, CI must be able to build it. The .NET 8 SDK cannot build `net9.0` targets; CI would need to be updated to use .NET 9 SDK (or a multi-SDK setup). If this is out of scope, use `net8.0` only. |
| `BenchmarkDotNet.Diagnostics.Windows` on Linux CI | This package targets Windows; the CI runner is `ubuntu-latest`. Building for `net8.0` on Linux should still succeed as the package conditionally applies. Verify no new build errors arise after removing the TFM list. |
| Spec doc accuracy | `spec/tech-standards/build-system.md` explicitly documents the old multi-target list and the rationale "Multiple .NET versions for comparison". This rationale still applies but the list must be updated. |
| `net47` removal | `net47` is .NET Framework 4.7, not .NET Core. Removing it means the benchmarking project no longer builds a .NET Framework binary. This is intentional and consistent with the issue goal. |
