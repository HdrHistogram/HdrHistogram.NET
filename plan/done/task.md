# Task List: Issue #115 — Run dotnet format to fix existing code

## Context

A one-time bulk reformatting pass using `dotnet format whitespace` to bring all 123 `.cs`
files into alignment with the `.editorconfig` introduced in issue #114.
346 violations exist across 101 files: 213 ENDOFLINE, 78 FINALNEWLINE, 47 CHARSET, 8 WHITESPACE.
No logic, API surface, or behaviour is altered.

---

## Tasks

### 1. Apply formatting

- [x] **Run `dotnet format whitespace HdrHistogram.sln`** to automatically fix all whitespace,
  line-ending, charset, and final-newline violations across all four projects.
  _Why:_ The brief mandates using the `whitespace` sub-command explicitly to avoid invoking
  `style` or `analyzers` fixers that could make non-cosmetic changes.
  _Verify:_ Command exits with code 0 and reports files modified.

### 2. Spot-check Bitwise.cs

- [x] **Inspect the diff for `HdrHistogram/Utilities/Bitwise.cs`** (lines 40–60) using `git diff`
  to confirm the 8 WHITESPACE fixes are indentation-only and contain no logic change.
  _Why:_ This is the only file with substantive (non-trivial) formatting changes; the brief
  calls it out explicitly as requiring manual verification.
  _Verify:_ Diff shows only whitespace/indentation changes; no executable tokens added or removed.

### 3. Verify formatting is fully resolved

- [x] **Run `dotnet format --verify-no-changes HdrHistogram.sln`** and confirm it exits with
  code 0 and reports zero violations.
  _Why:_ Acceptance criterion 1 — the formatter must report a clean state after the fix.
  _Verify:_ Exit code is 0; output contains no violation lines.

### 4. Confirm build is clean

- [x] **Run `dotnet build HdrHistogram.sln`** and confirm it succeeds with no errors and no
  warnings introduced by this change.
  _Why:_ Acceptance criterion 2 — reformatting must not break the build.
  _Verify:_ Build output ends with `Build succeeded` and warning count is unchanged.

### 5. Confirm all unit tests pass

- [x] **Run `dotnet test HdrHistogram.sln`** and confirm every test passes.
  _Why:_ Acceptance criterion 3 — existing tests must continue to pass without modification.
  _Verify:_ Test output shows 0 failed, 0 skipped (or same counts as pre-change baseline).

### 6. Commit the formatting change

- [x] **Stage all modified files** (`git add -u`) and **create a commit** with the exact message:
  `chore: apply dotnet format to match .editorconfig`
  _Why:_ Acceptance criterion 6 — the commit message must match this string exactly so that
  tooling (e.g. `git log --grep`) and the blame-ignore-revs entry can reference it reliably.
  _Verify:_ `git log -1 --format=%s` outputs the required message verbatim.

### 7. Create `.git-blame-ignore-revs`

- [x] **Record the SHA** of the formatting commit (`git rev-parse HEAD`) and **create
  `.git-blame-ignore-revs`** at the repository root containing that SHA, prefixed with a
  comment line: `# chore: apply dotnet format to match .editorconfig`
  _Why:_ Acceptance criterion 5 — the bulk formatting commit must be skippable via
  `git blame --ignore-revs-file .git-blame-ignore-revs` so that `git blame` output remains
  meaningful for future authors.
  _Verify:_ File exists at repo root; contains the correct 40-character SHA; running
  `git blame --ignore-revs-file .git-blame-ignore-revs HdrHistogram/Utilities/Bitwise.cs`
  does not attribute lines to the formatting commit.

### 8. Commit `.git-blame-ignore-revs`

- [x] **Stage and commit `.git-blame-ignore-revs`** with message:
  `chore: add .git-blame-ignore-revs for formatting commit`
  _Why:_ The brief says to include this file in the same PR; it must be its own commit so it
  is not mixed with the formatting diff.
  _Verify:_ `git log --oneline -2` shows both the formatting commit and this follow-up commit.

### 9. Open a pull request

- [x] **Push the branch** and **open a PR against `main`** using `gh pr create`.
  PR title: `chore: apply dotnet format to match .editorconfig (#115)`
  PR body must reference issue #115 and summarise: what was run, violation counts fixed,
  and note that `.git-blame-ignore-revs` is included.
  _Why:_ Required by the git workflow in CLAUDE.md; the brief asks for the PR to be opened.
  _Verify:_ PR is open, CI passes, PR description references issue #115.

---

## Acceptance Criterion Cross-Reference

| Acceptance Criterion (brief) | Covered By |
|---|---|
| `dotnet format --verify-no-changes HdrHistogram.sln` exits code 0 | Task 3 |
| `dotnet build HdrHistogram.sln` succeeds with no errors or warnings | Task 4 |
| All unit tests pass (`dotnet test HdrHistogram.sln`) | Task 5 |
| No functional code changes — diffs contain only whitespace/newline additions | Tasks 1, 2 |
| `.git-blame-ignore-revs` created at repo root with formatting commit SHA | Tasks 7, 8 |
| Commit message is `chore: apply dotnet format to match .editorconfig` | Task 6 |
