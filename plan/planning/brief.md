# Issue #115: Run dotnet format to fix existing code to match .editorconfig

## Summary

The `.editorconfig` file was introduced in issue #114 (commit `14b962b`).
Existing source code predates these conventions and does not yet conform to them.
This issue is a one-time bulk reformatting pass using `dotnet format whitespace` to bring all files into alignment.
The change must be isolated in its own commit so that `git blame --ignore-rev` can skip it.

## What Needs to Change and Why

Running `dotnet format --verify-no-changes HdrHistogram.sln` on the current branch reports **346 formatting violations across 101 files**:

- **213 ENDOFLINE violations** — files using CRLF line endings instead of LF (`end_of_line = lf` in `.editorconfig`)
- **78 FINALNEWLINE violations** — files that do not end with a newline character (`insert_final_newline = true` in `.editorconfig`)
- **47 CHARSET violations** — files with character-encoding issues (e.g. byte-order marks)
- **8 WHITESPACE violations** — indentation issues in `HdrHistogram/Utilities/Bitwise.cs` (lines 43–55)

The changes are purely cosmetic (whitespace, line endings, and newlines).
No logic, API surface, or behaviour is altered.

## Affected Files

The repository contains 123 `.cs` files across four projects, of which 101 have at least one violation:

- `HdrHistogram/` — 56 `.cs` source files (main library)
- `HdrHistogram.UnitTests/` — 34 `.cs` test files
- `HdrHistogram.Examples/` — 6 `.cs` example files
- `HdrHistogram.Benchmarking/` — 27 `.cs` benchmark files

The authoritative list of affected files comes from `dotnet format --verify-no-changes HdrHistogram.sln` output.

The file with the most substantive changes is:

- `HdrHistogram/Utilities/Bitwise.cs` — 8 whitespace/indentation fixes

All other files require only line-ending normalisation and/or a trailing newline added at end-of-file.

## Acceptance Criteria

- [ ] `dotnet format --verify-no-changes HdrHistogram.sln` exits with code 0 after the fix is applied
- [ ] `dotnet build HdrHistogram.sln` succeeds with no errors or warnings introduced by this change
- [ ] All unit tests pass (`dotnet test HdrHistogram.sln`)
- [ ] No functional code changes — diffs contain only whitespace, line-ending, and newline additions
- [ ] A `.git-blame-ignore-revs` file is created at the repo root containing the SHA of the formatting commit
- [ ] The commit message is `chore: apply dotnet format to match .editorconfig`

## Test Strategy

No new tests are required — this is a pure formatting change.
Existing tests must continue to pass without modification.

Verification steps:

1. Run `dotnet format --verify-no-changes HdrHistogram.sln` — must exit 0
2. Run `dotnet build HdrHistogram.sln` — must succeed
3. Run `dotnet test HdrHistogram.sln` — all tests must pass
4. Inspect the diff with `git diff` before committing — confirm only whitespace/newline changes

## Implementation Steps

1. Run `dotnet format whitespace HdrHistogram.sln` to apply all whitespace fixes automatically
2. Verify with `dotnet format --verify-no-changes HdrHistogram.sln`
3. Run `dotnet build HdrHistogram.sln` to confirm build is clean
4. Run `dotnet test HdrHistogram.sln` to confirm tests pass
5. Commit with message: `chore: apply dotnet format to match .editorconfig`
6. Record the commit SHA and create `.git-blame-ignore-revs` with that SHA
7. Commit `.git-blame-ignore-revs` in the same PR
8. Open a PR against `main`

## Risks and Open Questions

- **Risk:** The `dotnet format` command without a sub-command invokes all three fixers: `whitespace`, `style`, and `analyzers`.
  The `.editorconfig` contains Roslyn naming-convention rules at `suggestion` severity; by default `dotnet format style` only applies rules at `warning` severity or above, so naming rules would not be auto-applied.
  Mitigation: use `dotnet format whitespace HdrHistogram.sln` explicitly to limit the scope to whitespace-only fixes and remove all ambiguity.
- **Risk:** CRLF/LF normalisation could affect files on Windows dev machines.
  Mitigation: `.editorconfig` enforces `end_of_line = lf`; this is the intended target, and ENDOFLINE violations confirm CRLF files exist in the repo.
- **Note:** The `Bitwise.cs` indentation changes (8 WHITESPACE violations) are the only non-trivial fixes; they should be spot-checked manually to confirm no logic change.
- **Note:** No custom analyser NuGet packages are present in any `.csproj` file, so the risk of non-whitespace changes from analyser rules is negligible.
