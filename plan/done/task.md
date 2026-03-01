# Task List — Issue #116: Add Directory.Build.props with shared build settings

## Context

No `Directory.Build.props` exists at the repository root.
None of the four projects (`HdrHistogram`, `HdrHistogram.UnitTests`, `HdrHistogram.Examples`, `HdrHistogram.Benchmarking`) define `Nullable`, `ImplicitUsings`, `EnforceCodeStyleInBuild`, `AnalysisLevel`, or `TreatWarningsAsErrors`.
The only per-project `WarningsAsErrors` entry is `NU5125;NU5048` in `HdrHistogram.csproj` — this is NuGet-packaging-specific and must remain in that file.

No public API changes are made by this issue; XML doc comments do not need updating.
No new tests are required; verification is build-and-test.

---

## Tasks

### 1 — Create `/workspace/repo/Directory.Build.props`

- **File**: `/workspace/repo/Directory.Build.props` (new file)
- **Change**: Create with the four shared MSBuild properties from the brief.
- **Why**: Centralises build settings for all four projects.
  `TreatWarningsAsErrors` is intentionally omitted — start conservative; a follow-up issue will tighten this once the warning volume is known.
- **Content**:

  ```xml
  <Project>
    <PropertyGroup>
      <!-- Enable nullable reference types repo-wide -->
      <Nullable>enable</Nullable>

      <!-- Explicit using directives (consistent with existing code style) -->
      <ImplicitUsings>disable</ImplicitUsings>

      <!-- Enforce .editorconfig style rules during build -->
      <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>

      <!-- Enable built-in .NET analysers at recommended level -->
      <AnalysisLevel>latest-recommended</AnalysisLevel>
    </PropertyGroup>
  </Project>
  ```

- **Verify**: File exists at `/workspace/repo/Directory.Build.props` with all four properties set to the values above and `TreatWarningsAsErrors` absent.

---

### 2 — Review `HdrHistogram/HdrHistogram.csproj` for redundant properties

- **File**: `/workspace/repo/HdrHistogram/HdrHistogram.csproj`
- **Change**: Audit all `PropertyGroup` entries.
  Confirm none of `Nullable`, `ImplicitUsings`, `EnforceCodeStyleInBuild`, or `AnalysisLevel` are present (they are not, per exploration).
  Confirm `WarningsAsErrors>NU5125;NU5048</WarningsAsErrors>` is retained — it is NuGet-specific and must not move to `Directory.Build.props`.
- **Why**: Per acceptance criterion AC9, any properties made redundant by `Directory.Build.props` must be removed.
  The exploration confirmed no redundant properties exist in this file, but the audit must be on-record.
- **Verify**: File is unchanged (or has only redundant properties removed if any were found during audit); `NU5125;NU5048` entry is still present.

---

### 3 — Review `HdrHistogram.UnitTests/HdrHistogram.UnitTests.csproj` for redundant properties

- **File**: `/workspace/repo/HdrHistogram.UnitTests/HdrHistogram.UnitTests.csproj`
- **Change**: Audit all `PropertyGroup` entries.
  The exploration shows only `TargetFrameworks` is present — no overlap with `Directory.Build.props` properties.
- **Why**: AC9 compliance audit.
- **Verify**: File is unchanged (or has only redundant properties removed if any were found during audit).

---

### 4 — Review `HdrHistogram.Examples/HdrHistogram.Examples.csproj` for redundant properties

- **File**: `/workspace/repo/HdrHistogram.Examples/HdrHistogram.Examples.csproj`
- **Change**: Audit all `PropertyGroup` entries.
  The exploration shows only `OutputType` and `TargetFrameworks` — no overlap with `Directory.Build.props` properties.
- **Why**: AC9 compliance audit.
- **Verify**: File is unchanged (or has only redundant properties removed if any were found during audit).

---

### 5 — Review `HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj` for redundant properties

- **File**: `/workspace/repo/HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj`
- **Change**: Audit all `PropertyGroup` entries.
  The exploration shows only `OutputType` and `TargetFrameworks` — no overlap with `Directory.Build.props` properties.
- **Why**: AC9 compliance audit.
- **Verify**: File is unchanged (or has only redundant properties removed if any were found during audit).

---

### 6 — Run `dotnet build` and address any new diagnostics

- **Command**: `dotnet build -c Release` from `/workspace/repo`
- **Change**: If new build errors are introduced (unexpected), investigate and resolve.
  If the volume of new analyser warnings is large, add targeted `<NoWarn>` entries or downgrade specific rule categories in `.editorconfig` — document the reason with an inline comment.
  If `EnforceCodeStyleInBuild` or `AnalysisLevel` produce warnings that would break CI, suppress them with justification.
  `TreatWarningsAsErrors` is not set, so warnings alone will not fail the build.
- **Why**: AC7 — `dotnet build` must exit 0 with no new errors.
- **Verify**: `dotnet build -c Release` exits with code 0 and produces no new errors (warnings are acceptable).

---

### 7 — Run `dotnet test` and confirm no regressions

- **Command**: `dotnet test ./HdrHistogram.UnitTests/HdrHistogram.UnitTests.csproj -c Release` from `/workspace/repo`
- **Change**: None expected.
  If any test fails due to a build-configuration side-effect, investigate and fix.
- **Why**: AC8 — all existing tests must pass.
- **Verify**: `dotnet test` exits with code 0; all 32 source files worth of tests pass with no failures or skips beyond the pre-existing baseline.

---

## Acceptance Criteria Cross-Reference

| Criterion | Covered by |
|---|---|
| AC1 — `Directory.Build.props` added at repository root | Task 1 |
| AC2 — `EnforceCodeStyleInBuild` set to `true` | Task 1 |
| AC3 — `AnalysisLevel` set to `latest-recommended` | Task 1 |
| AC4 — `Nullable` set to `enable` | Task 1 |
| AC5 — `ImplicitUsings` set to `disable` | Task 1 |
| AC6 — `TreatWarningsAsErrors` assessed (omitted conservatively) | Task 1 |
| AC7 — `dotnet build` succeeds with no new errors | Task 6 |
| AC8 — `dotnet test` passes with no regressions | Task 7 |
| AC9 — Redundant per-project settings removed | Tasks 2–5 |
