# Issue #117: Add dotnet format check to CI pipeline

## Summary

The CI pipeline at `.github/workflows/ci.yml` does not currently enforce code formatting.
Adding a `dotnet format --verify-no-changes` step will cause the build to fail fast whenever a PR introduces code that does not comply with the `.editorconfig` rules.
The step must run after `dotnet restore` (formatting requires packages to be present) but before `dotnet build`, so contributors get feedback quickly without waiting for a full compilation.

## Affected Files

- `.github/workflows/ci.yml` — the only file that changes.

## Current State

The workflow currently has these ordered steps:

1. `actions/checkout@v4`
2. `actions/setup-dotnet@v4` (dotnet 8.0, with caching)
3. `dotnet restore`
4. `dotnet build -c Release --no-restore /p:Version=…`
5. `dotnet test …`
6. `dotnet pack …`
7. `actions/upload-artifact@v4`

There is no formatting check.
`.editorconfig` is present and comprehensive (UTF-8, LF endings, Allman braces, `_camelCase` private fields, etc.).

## Proposed Change

Insert a new step between `dotnet restore` and `dotnet build`:

```yaml
- name: Check code formatting
  run: dotnet format --verify-no-changes --verbosity diagnostic
```

The `--verify-no-changes` flag exits with a non-zero code if any file would be changed, failing the CI job.
The `--verbosity diagnostic` flag prints the names of files that need fixing, satisfying the acceptance criterion for clear failure output.

## Acceptance Criteria

- [ ] `dotnet format --verify-no-changes` step is present in `ci.yml`.
- [ ] Step is positioned after `dotnet restore` and before `dotnet build`.
- [ ] Step runs on every PR build (inherits the existing `pull_request` trigger).
- [ ] CI passes on the current codebase (depends on issues #114 and #115 being complete).
- [ ] Failure output clearly indicates which files need formatting (`--verbosity diagnostic`).

## Test Strategy

This change affects the CI workflow file only — there are no C# source files to unit-test.
Validation steps:

1. **Dry-run locally**: run `dotnet format --verify-no-changes --verbosity diagnostic` from the repo root and confirm it exits 0.
2. **PR CI run**: push the branch and confirm the new step appears and passes in the GitHub Actions log.
3. **Negative test** (optional, manual): introduce a deliberate formatting violation on a throwaway branch and confirm the step fails with a diagnostic message naming the offending file.

## Risks and Open Questions

- **Dependency on #114 and #115**: if the codebase is not yet fully formatted, the new step will fail CI immediately.
  Confirm those issues are merged before merging this PR.
- **`dotnet format` availability**: the workflow uses `dotnet 8.0.x`, which ships `dotnet format` as a built-in tool — no additional installation is required.
- **Solution vs project scope**: `dotnet format` without a path argument defaults to the solution file in the current directory (`HdrHistogram.sln`), which covers all projects.
  This is the desired behaviour.
- **Cache invalidation**: the existing `cache-dependency-path: '**/*.csproj'` cache covers restored packages, so the format step will benefit from the already-warmed cache.
