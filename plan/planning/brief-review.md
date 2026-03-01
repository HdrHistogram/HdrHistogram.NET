# Brief Review: Issue #119 — Add support for net9.0 and net10.0

## Verdict: Revisions required before moving to ready/

The brief is largely solid — all stated file paths exist, all "current values" match the actual codebase, and the scope fits comfortably in one PR.
However, four issues must be addressed before implementation begins.

---

## Issue 1 — SIGNIFICANT: Examples project will not build in the devcontainer after the change

**Location in brief:** Affected Files table (Examples row), Risks section

**Problem:**
The devcontainer (`/.devcontainer/Dockerfile`) is built on `mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim` and additionally installs the .NET 8.0 *runtime* only.
Targets buildable in the devcontainer today: `net8.0`, `net9.0`, `netstandard2.0`.

The brief changes `HdrHistogram.Examples.csproj` from `net8.0` to `net10.0` (single target).
After this change, the Examples project cannot be built or run inside the devcontainer — the .NET 10 SDK is simply not present.

The brief's "local failure is acceptable during transition" applies to the main library and test project; those are CI-authoritative artefacts.
The Examples project is described in the brief itself as "a developer-facing runnable tool", so breaking its local buildability is a direct regression.

**Required resolution (choose one and state it explicitly in the brief):**

- **Option A (preferred):** Update `.devcontainer/Dockerfile` to use `mcr.microsoft.com/dotnet/sdk:10.0-bookworm-slim` as the base image and add an explicit install of the 8.0 and 9.0 runtimes.
  Add `.devcontainer/Dockerfile` to the Affected Files table.
- **Option B:** Target Examples at `net9.0` (matching the current devcontainer SDK) and note this as an interim state pending a devcontainer upgrade in a follow-up issue.
- **Option C:** Make Examples multi-target (`net10.0;net9.0`) so it is runnable wherever any of those SDKs is present.

---

## Issue 2 — MINOR: CI YAML format for multiple SDK versions is ambiguous

**Location in brief:** Affected Files table (ci.yml row)

**Problem:**
The brief states the required value is `8.0.x`, `9.0.x`, `10.0.x` (multi-line) but does not show the exact YAML syntax.
The `actions/setup-dotnet@v4` action supports a multi-line scalar for `dotnet-version`, but an implementer unfamiliar with the action could also try multiple separate steps, which would not work with `cache: true` correctly.

The current `ci.yml` also uses `cache: true` with `cache-dependency-path: '**/*.csproj'`.
With multiple SDK versions, NuGet restore and caching behaviour should be confirmed.

**Required fix:**
Add an exact YAML snippet to the brief, for example:

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

---

## Issue 3 — MINOR: BenchmarkDotNet risk has no resolution path

**Location in brief:** Risks and Open Questions section

**Problem:**
The brief correctly flags that `BenchmarkDotNet 0.13.12` may not support `net10.0` and says "verify during implementation."
That leaves the implementer without any guidance on what to do if verification fails.

**Required fix:**
Add a concrete fallback action, for example:

> If `BenchmarkDotNet 0.13.12` fails to build against `net10.0`, upgrade both `BenchmarkDotNet` and `BenchmarkDotNet.Diagnostics.Windows` to the latest stable version as part of this PR and update the version in `spec/tech-standards/build-system.md` accordingly.

---

## Issue 4 — MINOR: Spec update not itemised

**Location in brief:** Affected Files table (build-system.md row), Acceptance Criteria

**Problem:**
The brief says to update `spec/tech-standards/build-system.md` to "reflect the new targets" but does not enumerate which sections require changes.
The file currently has at least five distinct locations that reference the old framework targets:

| Line | Section | Content to update |
|------|---------|-------------------|
| 24–26 | Main Library TargetFrameworks | `net8.0;netstandard2.0` → `net10.0;net9.0;net8.0;netstandard2.0` |
| 35–37 | Test Project TargetFramework | `net8.0` → `net10.0;net9.0;net8.0` |
| 42–45 | Benchmarking Project TargetFrameworks | `net8.0` → `net10.0;net9.0;net8.0` |
| 226–228 | Benchmark Configuration list | "net8.0 (current LTS runtime)" → list all three runtimes |
| 254 | Prerequisites | ".NET 8.0 SDK (or later)" → ".NET 10.0 SDK (or later)" |

The AppVeyor section (lines 137–177) is already acknowledged as out of scope, which is correct.

**Required fix:**
Replace the single "update to reflect new targets" note with the table above.
The Examples project section is absent from the spec (the spec does not currently document the Examples target framework), so no change is needed for Examples.

---

## Summary of required actions

| # | Severity | Action |
|---|----------|--------|
| 1 | Significant | Choose and document an explicit resolution for the devcontainer/Examples incompatibility |
| 2 | Minor | Add exact YAML snippet for the multi-version `setup-dotnet` step |
| 3 | Minor | Specify the fallback action if BenchmarkDotNet 0.13.12 is incompatible with net10.0 |
| 4 | Minor | Replace vague spec-update note with the itemised list of sections to change |
