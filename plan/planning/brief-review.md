# Brief Review — Issue #116: Add Directory.Build.props

## Overall assessment

The brief is clear, well-scoped, and technically accurate in almost every respect.
It can move to ready once the one factual correction below is applied.

---

## Verification results

All file-existence claims were checked against the repository.

| Claim in brief | Verified |
|---|---|
| `HdrHistogram.csproj` exists with `TargetFrameworks: net8.0;netstandard2.0` | ✓ |
| `HdrHistogram.UnitTests.csproj` exists with minimal PropertyGroup | ✓ |
| `HdrHistogram.Examples.csproj` exists with `OutputType: Exe` | ✓ |
| `HdrHistogram.Benchmarking.csproj` exists with `OutputType: Exe` | ✓ |
| None of the four projects set `Nullable`, `AnalysisLevel`, `EnforceCodeStyleInBuild`, `ImplicitUsings` | ✓ |
| `HdrHistogram.csproj` has `<WarningsAsErrors>NU5125;NU5048</WarningsAsErrors>` | ✓ |
| `.editorconfig` present; all style rules set to `suggestion` severity | ✓ |
| `Directory.Build.props` does not exist | ✓ |
| `Directory.Build.targets`, `global.json`, `nuget.config` do not exist | ✓ |
| `appveyor.yml` does not exist at repo root | ✓ |

---

## Issues requiring correction

### 1. CI is active — it is not unknown (required fix)

**Location:** Risk table row for `AnalysisLevel=latest-recommended`, and Open question #2.

**Current text (risk table):**
> Note that AppVeyor CI does not appear to be configured currently (appveyor.yml absent).

**Current text (open question #2):**
> Observation: no appveyor.yml found at repo root; CI status is unknown.

**Actual state:**
CI is active via GitHub Actions at `.github/workflows/ci.yml`.
The pipeline runs on every PR and on pushes to `main`.
It executes:

```
dotnet restore
dotnet build -c Release --no-restore /p:Version=...
dotnet test ./HdrHistogram.UnitTests/HdrHistogram.UnitTests.csproj --no-build -c Release
dotnet pack ./HdrHistogram/HdrHistogram.csproj -c Release ...
```

`Directory.Build.props` is picked up automatically by MSBuild during the `dotnet build` step, so `EnforceCodeStyleInBuild=true` and `AnalysisLevel=latest-recommended` will be exercised on every PR.

**Why it matters:**
The implementer needs to know that CI will exercise the new settings automatically.
The mitigation advice ("verify with the team before tightening rules further") should reference the GitHub Actions workflow, not imply the absence of CI.

**Suggested replacement for the risk table note:**
> CI is active via GitHub Actions (`.github/workflows/ci.yml`).
> The pipeline runs `dotnet build -c Release` and `dotnet test` on every PR.
> New analyser warnings will surface in CI output.
> Because `TreatWarningsAsErrors` is not set, they will not fail the build.

**Suggested replacement for open question #2:**
> CI runs via GitHub Actions (`.github/workflows/ci.yml`) with `dotnet build -c Release` and `dotnet test`.
> No pipeline changes are needed for this PR; the new settings are exercised automatically.
> If warning volume is high after merging, downgrade specific rule categories via `.editorconfig` in a follow-up.

---

## Minor notes (no action required)

- `spec/tech-standards/build-system.md` documents an AppVeyor configuration as if it were current.
  This is a pre-existing documentation inaccuracy in the spec, not in the brief.
  The brief correctly observed that `appveyor.yml` is absent.
  Updating the spec doc is out of scope for this issue.

- The proposed `Directory.Build.props` content is minimal, well-justified, and matches the acceptance criteria exactly.
  No changes to the proposed XML are needed.

- The test strategy (build + test verification, no new test code) is appropriate for a build-system change.

- Scope is correctly sized for a single PR.

---

## Recommended action

Apply the CI correction (issue #1 above), then move the brief to `plan/ready/brief.md`.
