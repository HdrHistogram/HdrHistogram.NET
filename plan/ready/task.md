# Task List: Issue #117 — Add dotnet format check to CI pipeline

## Context

The only file that changes is `.github/workflows/ci.yml`.
Direct edits to `.github/` are blocked by repository permissions in this environment.
All tasks related to that file are handled by preparing an attachment and manual-intervention instructions — no direct file edit is attempted.

---

## Tasks

### Preparation

- [ ] **T1 — Dry-run format check locally**
  - **File:** repo root (runs against `HdrHistogram.sln`)
  - **Change:** Execute `dotnet format --verify-no-changes --verbosity diagnostic` from `/workspace/repo` and confirm exit code is `0`.
  - **Why:** Acceptance criterion 4 — CI must pass on the current codebase.
    If this exits non-zero, formatting issues remain (likely from #114/#115) and must be resolved before the CI step can be added.
  - **Verification:** Command exits `0` with no files listed as requiring changes.

### CI Workflow Change (manual intervention required)

> **Note:** The `.github/` directory cannot be edited directly in this environment.
> Tasks T2–T4 produce an attachment file and instructions that a maintainer must apply manually.

- [ ] **T2 — Create patch file for `ci.yml`**
  - **File:** Create `plan/ready/ci-format-step.patch` (or `plan/ready/ci.yml.proposed`) containing the full proposed `ci.yml` content with the new step inserted.
  - **Change:** Insert the following step between `dotnet restore` and `dotnet build` in the `jobs.build.steps` array:
    ```yaml
          - name: Check code formatting
            run: dotnet format --verify-no-changes --verbosity diagnostic
    ```
  - **Why:** Provides the maintainer with a ready-to-apply artefact rather than relying on prose instructions alone.
  - **Verification:** File exists at `plan/ready/ci.yml.proposed`; diff against current `ci.yml` shows exactly one new step inserted in the correct position.

- [ ] **T3 — Create manual-intervention instructions file**
  - **File:** Create `plan/ready/manual-intervention.md`
  - **Change:** Document the exact steps a maintainer must follow:
    1. Copy `plan/ready/ci.yml.proposed` to `.github/workflows/ci.yml` (or apply the diff).
    2. Verify the step order in the file: `checkout → setup-dotnet → restore → **format check** → build → test → pack → upload`.
    3. Commit and push on the feature branch.
    4. Confirm the "Check code formatting" step appears in the GitHub Actions run log.
  - **Why:** Satisfies the project requirement that `.github/` changes be accompanied by explicit manual-intervention directions.
  - **Verification:** File exists at `plan/ready/manual-intervention.md` and covers all four points above.

- [ ] **T4 — Validate step content satisfies all acceptance criteria**
  - **File:** `plan/ready/ci.yml.proposed` (review only)
  - **Change:** Cross-check the proposed file against every acceptance criterion:
    - AC1: `dotnet format --verify-no-changes` is present. ✓
    - AC2: Step appears after `dotnet restore` and before `dotnet build`. ✓
    - AC3: Workflow trigger includes `pull_request` (unchanged from current file). ✓
    - AC4: Dry-run (T1) confirmed exit 0. ✓
    - AC5: `--verbosity diagnostic` flag is present. ✓
  - **Why:** Ensures no acceptance criterion is missed before handing off.
  - **Verification:** All five criteria are ticked off in this review.

---

## Acceptance Criterion Coverage

| Acceptance Criterion | Covered By |
|---|---|
| `dotnet format --verify-no-changes` step is present in `ci.yml` | T2, T4 |
| Step positioned after `dotnet restore` and before `dotnet build` | T2, T4 |
| Step runs on every PR build (inherits `pull_request` trigger) | T4 |
| CI passes on current codebase (depends on #114 and #115) | T1 |
| Failure output clearly indicates which files need formatting (`--verbosity diagnostic`) | T2, T4 |

---

## Dependency Order

```
T1 (dry-run) → T2 (create proposed file) → T3 (write intervention instructions) → T4 (cross-check)
```

T1 must complete successfully before T2 is authored, because if `dotnet format` exits non-zero the proposed CI step would immediately break CI and #114/#115 must be resolved first.
