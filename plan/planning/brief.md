# Issue #115: Run dotnet format to fix existing code to match .editorconfig

## Summary

The `.editorconfig` file was introduced in issue #114 (commit `14b962b`).
Existing source code predates these conventions and does not yet conform to them.
This issue is a one-time bulk reformatting pass using `dotnet format` to bring all files into alignment.
The change must be isolated in its own commit so that `git blame --ignore-rev` can skip it.

## What Needs to Change and Why

Running `dotnet format --verify-no-changes` on the current solution reports **346 formatting violations across 101 files**:

- **78 FINALNEWLINE violations** — files that do not end with a newline character (`insert_final_newline = true` in `.editorconfig`)
- **8 WHITESPACE violations** — indentation issues in `HdrHistogram/Utilities/Bitwise.cs` (lines 43–55)

The changes are purely cosmetic (whitespace and newlines).
No logic, API surface, or behaviour is altered.

## Affected Files

All four projects are affected:

- `HdrHistogram/` — 51 `.cs` source files (main library)
- `HdrHistogram.UnitTests/` — 35 `.cs` test files
- `HdrHistogram.Examples/` — 4 `.cs` example files
- `HdrHistogram.Benchmarking/` — 13 `.cs` benchmark files

**Total: 101 files, 346 violations.**

The file with the most substantive changes is:

- `HdrHistogram/Utilities/Bitwise.cs` — 8 whitespace/indentation fixes

All other files require only a trailing newline added at end-of-file.

## Acceptance Criteria

- [ ] `dotnet format --verify-no-changes` exits with code 0 after the fix is applied
- [ ] `dotnet build` succeeds with no errors or warnings introduced by this change
- [ ] All unit tests pass (`dotnet test`)
- [ ] No functional code changes — diffs contain only whitespace and newline additions
- [ ] A `.git-blame-ignore-revs` file is created at the repo root containing the SHA of the formatting commit
- [ ] The commit message is `chore: apply dotnet format to match .editorconfig`

## Test Strategy

No new tests are required — this is a pure formatting change.
Existing tests must continue to pass without modification.

Verification steps:

1. Run `dotnet format --verify-no-changes` — must exit 0
2. Run `dotnet build HdrHistogram.sln` — must succeed
3. Run `dotnet test HdrHistogram.sln` — all tests must pass
4. Inspect the diff with `git diff` before committing — confirm only whitespace/newline changes

## Implementation Steps

1. Run `dotnet format HdrHistogram.sln` to apply all fixes automatically
2. Verify with `dotnet format --verify-no-changes HdrHistogram.sln`
3. Run `dotnet build HdrHistogram.sln` to confirm build is clean
4. Run `dotnet test HdrHistogram.sln` to confirm tests pass
5. Commit with message: `chore: apply dotnet format to match .editorconfig`
6. Record the commit SHA and create `.git-blame-ignore-revs` with that SHA
7. Commit `.git-blame-ignore-revs` (separate commit or same PR)
8. Open a PR against `main`

## Risks and Open Questions

- **Risk:** `dotnet format` may apply analyser-driven style changes (not just whitespace) if analyser rules are enabled in `.editorconfig` or project files.
  Mitigation: review `git diff` before committing to confirm the scope is whitespace-only.
- **Risk:** CRLF/LF normalisation could affect files on Windows dev machines.
  Mitigation: `.editorconfig` enforces `end_of_line = lf`; this is the intended target.
- **Open question:** Should `.git-blame-ignore-revs` be committed in the same PR or a follow-up?
  Preference from issue: same PR.
- **Note:** The `Bitwise.cs` indentation changes (8 violations) are the only non-trivial whitespace fixes; they should be spot-checked manually to confirm no logic change.
