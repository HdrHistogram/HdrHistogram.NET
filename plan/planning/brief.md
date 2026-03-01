# Issue #116 — Add Directory.Build.props with shared build settings

## Summary

The repository currently has no centralised MSBuild property file.
Each of the four projects (`HdrHistogram`, `HdrHistogram.UnitTests`, `HdrHistogram.Examples`, `HdrHistogram.Benchmarking`) carries only minimal PropertyGroup settings (`TargetFramework(s)`, `OutputType`).
None of the projects configure `Nullable`, `ImplicitUsings`, `EnforceCodeStyleInBuild`, `AnalysisLevel`, or `TreatWarningsAsErrors`.

Adding a `Directory.Build.props` file at the repository root will:

- Centralise these settings in one place.
- Enforce `.editorconfig` style rules during build.
- Enable the built-in .NET analysers at the `latest-recommended` level.
- Establish a consistent nullable-reference-types policy across all projects.
- Avoid duplication as new projects are added.

## Current state (confirmed by exploration)

### Projects

| Project | TargetFramework(s) | OutputType | Nullable | AnalysisLevel | EnforceCodeStyleInBuild | TreatWarningsAsErrors |
|---|---|---|---|---|---|---|
| HdrHistogram | net8.0;netstandard2.0 | (library) | — | — | — | NU5125;NU5048 only |
| HdrHistogram.UnitTests | net8.0 | — | — | — | — | — |
| HdrHistogram.Examples | net8.0 | Exe | — | — | — | — |
| HdrHistogram.Benchmarking | net8.0 | Exe | — | — | — | — |

### Root configuration

- `.editorconfig` — present, comprehensive; all code-style rules are set to `suggestion` severity.
- `Directory.Build.props` — **does not exist** (to be created by this issue).
- `Directory.Build.targets` — does not exist.
- `global.json` — does not exist.
- `nuget.config` — does not exist.
- `appveyor.yml` — does not exist.

### Existing per-project WarningsAsErrors

`HdrHistogram.csproj` treats two NuGet-specific warnings as errors (`NU5125`, `NU5048`).
These are packaging concerns and must be preserved in the per-project file (they should not be moved to `Directory.Build.props`).

## Files affected

### Created

- `/workspace/repo/Directory.Build.props` — new file; shared MSBuild properties.

### Modified

- `/workspace/repo/HdrHistogram/HdrHistogram.csproj` — remove any properties that become redundant once lifted to `Directory.Build.props`.
- `/workspace/repo/HdrHistogram.UnitTests/HdrHistogram.UnitTests.csproj` — same.
- `/workspace/repo/HdrHistogram.Examples/HdrHistogram.Examples.csproj` — same.
- `/workspace/repo/HdrHistogram.Benchmarking/HdrHistogram.Benchmarking.csproj` — same.

In practice, because no current per-project settings duplicate what will go in `Directory.Build.props`, the per-project files will only need review (not necessarily editing) unless duplicate entries appear after introducing the new file.

## Acceptance criteria

- [ ] `Directory.Build.props` added at repository root.
- [ ] `EnforceCodeStyleInBuild` set to `true` in `Directory.Build.props`.
- [ ] `AnalysisLevel` set to `latest-recommended` in `Directory.Build.props`.
- [ ] `Nullable` set to `enable` in `Directory.Build.props`.
- [ ] `ImplicitUsings` explicitly configured (set to `disable` to match the explicit-using style visible in existing code).
- [ ] `TreatWarningsAsErrors` assessed: start conservative — omit or set per-category at `suggestion` level to avoid breaking the build; a follow-up issue can tighten this.
- [ ] `dotnet build` succeeds with no new errors.
- [ ] `dotnet test` passes with no regressions.
- [ ] Any per-project settings made redundant by `Directory.Build.props` are removed from the individual `.csproj` files.

## Proposed `Directory.Build.props` content

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

`TreatWarningsAsErrors` is intentionally omitted from the initial implementation.
The `.editorconfig` already uses `suggestion` severity for all style rules, so enabling `EnforceCodeStyleInBuild` alone will not produce errors.
Analyser diagnostics at `latest-recommended` default to warnings; if the build is clean these can be promoted to errors in a follow-up.

## Test strategy

No new test code is required.
Verification is build-and-test:

1. Run `dotnet build` — must exit 0 with no new errors.
2. Run `dotnet test` — all existing tests must pass.
3. Review build output for new warnings introduced by `EnforceCodeStyleInBuild` or `AnalysisLevel`; address or suppress with justified comments if they would block the build.

If the number of analyser warnings is large, severity for specific rule categories can be downgraded to `suggestion` in `.editorconfig` or via a `<NoWarn>` entry in `Directory.Build.props`.

## Risks and open questions

| Risk | Likelihood | Mitigation |
|---|---|---|
| Enabling `Nullable` causes widespread nullable warnings/errors on existing code that was written without nullability annotations | High | Nullable warnings default to `warning`, not `error`; the build will not break. Warnings can be addressed incrementally. If the volume is very high, add `<Nullable>annotations</Nullable>` initially (emits fewer warnings) and upgrade in a follow-up. |
| `EnforceCodeStyleInBuild` promotes `.editorconfig` `suggestion`-level rules to build diagnostics | Medium | The `.editorconfig` already sets all rules to `suggestion`; `EnforceCodeStyleInBuild` reports them but at `suggestion` (info) level, which does not fail the build. Verify output after first build. |
| `AnalysisLevel=latest-recommended` introduces new warning rules that fail CI | Medium | Run `dotnet build` locally first; inspect warnings. Use `<NoWarn>` or downgrade individual rules via `.editorconfig` if needed. Note that AppVeyor CI does not appear to be configured currently (`appveyor.yml` absent). |
| `ImplicitUsings=disable` conflicts with any project that implicitly relies on them | Low | Existing code has explicit `using` statements; disabling implicit usings is consistent with current practice. |
| `netstandard2.0` target in the main library may not support all analyser rules | Low | Analyser rules apply at the MSBuild/Roslyn layer regardless of target framework; no special handling needed. |

## Open questions

1. Should `TreatWarningsAsErrors` be scoped to analyser-only categories (e.g., `<WarningsAsErrors>$(WarningsAsErrors);CA*</WarningsAsErrors>`) now, or deferred to a follow-up issue after the warning volume is known?
   Recommendation: defer — start conservative per the issue notes.
2. Is there a CI pipeline that needs updating once `EnforceCodeStyleInBuild` is active?
   Observation: no `appveyor.yml` found at repo root; CI status is unknown.
   Action: verify with the team before tightening rules further.
