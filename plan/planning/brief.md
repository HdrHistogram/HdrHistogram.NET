# Brief: Issue #118 — Add CONTRIBUTING.md with Development Setup and Guidelines

## Summary

There is no `CONTRIBUTING.md` file in the repository root.
New contributors (human and agentic) have no single reference for how to set up their development environment, build the project, run tests, apply code formatting, or follow the project's git workflow.
The issue asks us to create `CONTRIBUTING.md` covering prerequisites, build, test, code style, git workflow, line endings, and cross-platform notes.
A secondary acceptance criterion asks that `README.md` reference the new file if appropriate.

## Affected Files

Confirmed by exploration:

| File | Change |
|------|--------|
| `CONTRIBUTING.md` | **New file** — create at repository root |
| `README.md` | **Modify** — add a short reference to `CONTRIBUTING.md` in the Development section (line ~264) |

No source code (`.cs`) or project (`.csproj`) files are affected.

## Acceptance Criteria

Derived from the issue:

- [ ] `CONTRIBUTING.md` exists at the repository root.
- [ ] Covers **prerequisites**: .NET SDK version (net8.0 / netstandard2.0 targets; no `global.json` pins a version, so state .NET 8 SDK minimum), supported platforms (Windows, macOS, Linux).
- [ ] Covers **building**: `dotnet build HdrHistogram.sln`.
- [ ] Covers **testing**: `dotnet test ./HdrHistogram.UnitTests/HdrHistogram.UnitTests.csproj -v q -c Release`.
- [ ] Covers **code style**: reference to `.editorconfig`; instruction to run `dotnet format` before submitting a PR.
- [ ] Covers **git workflow**: branch naming (`feat/`, `fix/`, `chore/`), one issue per PR, PRs target `main`, no direct commits to `main`.
- [ ] Covers **line endings**: LF convention, `.gitattributes` normalises line endings automatically; Windows batch files use CRLF by exception.
- [ ] Covers **cross-platform notes**: shell scripts require LF; the devcontainer is available for a consistent Linux environment.
- [ ] Uses **British English**, **one sentence per line** (per `CLAUDE.md` markdown standards).
- [ ] Headings have a blank line beneath them; ordered/unordered lists have a blank line before and after.
- [ ] `README.md` Development section links to `CONTRIBUTING.md`.

## Test Strategy

`CONTRIBUTING.md` is documentation — there are no automated tests to add or modify.
Verification is manual:

1. Confirm the file renders correctly in GitHub Markdown preview (headings, code blocks, lists).
2. Confirm all commands listed in `CONTRIBUTING.md` execute successfully in a clean checkout:
   - `dotnet build HdrHistogram.sln`
   - `dotnet test ./HdrHistogram.UnitTests/HdrHistogram.UnitTests.csproj -v q -c Release`
   - `dotnet format` (requires .NET SDK 6+, available in net8.0 environment)
3. Confirm the `README.md` link resolves to the new file.
4. Confirm British English spelling and one-sentence-per-line formatting throughout.

## Risks and Open Questions

| # | Risk / Question | Mitigation |
|---|-----------------|------------|
| 1 | No `global.json` pins the .NET SDK version — the stated minimum (8.0) may drift as new SDK releases land. | Document the minimum as ".NET 8 SDK" and note that newer SDKs are supported. Revisit when the project upgrades targets. |
| 2 | `dotnet format` behaviour varies by SDK version; older SDKs may produce different results. | State "use the same SDK version as the CI environment (.NET 8)" in the file. |
| 3 | Cross-platform notes for Windows CRLF vs LF — `.gitattributes` handles normalisation automatically, but contributors using Git for Windows with `core.autocrlf=true` may see unexpected diffs. | Advise contributors to keep `core.autocrlf=false` or rely on `.gitattributes`. |
| 4 | The README Development section is at line ~264 — a small edit is needed to add the reference without disrupting surrounding content. | Read the exact lines before editing; use a targeted `Edit` call. |
