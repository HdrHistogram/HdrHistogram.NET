# Brief: Issue #118 — Add CONTRIBUTING.md with Development Setup and Guidelines

## Summary

There is no `CONTRIBUTING.md` file in the repository root.
New contributors (human and agentic) have no single reference for how to set up their development environment, build the project, run tests, apply code formatting, or follow the project's git workflow.
The issue asks us to create `CONTRIBUTING.md` covering prerequisites, build, test, code style, git workflow, line endings, and cross-platform notes.
A secondary acceptance criterion asks that `README.md` reference the new file.

## Affected Files

Confirmed by exploration:

| File | Change |
|------|--------|
| `CONTRIBUTING.md` | **New file** — create at repository root |
| `README.md` | **Modify** — replace the body of `### How would I contribute to this project?` (lines 257–263) with a short sentence linking to `CONTRIBUTING.md`; optionally add a one-line note in `## Development` (line 264) pointing to `CONTRIBUTING.md` for setup instructions |

No source code (`.cs`) or project (`.csproj`) files are affected.

The existing three sentences in `### How would I contribute to this project?` (raise an issue first, describe the PR, include a unit test) must be preserved in `CONTRIBUTING.md` under a "Pull Request Guidelines" heading — not discarded.

## Acceptance Criteria

Derived from the issue:

- [ ] `CONTRIBUTING.md` exists at the repository root.
- [ ] Covers **prerequisites**: .NET SDK version (net8.0 / netstandard2.0 targets; no `global.json` pins a version, so state .NET 8 SDK minimum), supported platforms (Windows, macOS, Linux).
- [ ] Covers **building**: `dotnet build HdrHistogram.sln`.
- [ ] Covers **testing**: `dotnet test ./HdrHistogram.UnitTests/HdrHistogram.UnitTests.csproj -v q -c Release`.
- [ ] Covers **code style**: reference to `.editorconfig`; instruction to run `dotnet format` before submitting a PR.
- [ ] Covers **git workflow**: branch naming (`feat/`, `fix/`, `chore/`), one issue per PR, PRs target `main`, no direct commits to `main`.
- [ ] Covers **pull request guidelines**: raise an issue first before contributing; describe the change in the PR; include a unit test proving any fix and reference the issue in the PR comments.
- [ ] Covers **line endings**: LF convention, `.gitattributes` normalises line endings automatically; Windows batch files use CRLF by exception.
- [ ] Covers **cross-platform notes**: shell scripts require LF; `.gitattributes` normalises line endings on checkout; all platforms with the .NET 8 SDK can build and test natively — no container is required.
  Note: `.devcontainer/` exists in the repository but contains agent-automation infrastructure only (`fleet.sh`, `agent-loop.sh`, etc.); there is no `devcontainer.json` and it is not intended for human contributors.
- [ ] Uses **British English**, **one sentence per line** (per `CLAUDE.md` markdown standards).
- [ ] Headings have a blank line beneath them; ordered/unordered lists have a blank line before and after.
- [ ] `README.md` `### How would I contribute to this project?` section (lines 257–263) is updated to link to `CONTRIBUTING.md`; the original prose is not duplicated in README.md.
- [ ] `README.md` `## Development` section optionally carries a one-line pointer to `CONTRIBUTING.md` for setup instructions.

## Test Strategy

`CONTRIBUTING.md` is documentation — there are no automated tests to add or modify.
Verification is manual:

1. Confirm the file renders correctly in GitHub Markdown preview (headings, code blocks, lists).
2. Confirm all commands listed in `CONTRIBUTING.md` execute successfully in a clean checkout:
   - `dotnet build HdrHistogram.sln`
   - `dotnet test ./HdrHistogram.UnitTests/HdrHistogram.UnitTests.csproj -v q -c Release`
   - `dotnet format` (requires .NET SDK 6+, available in net8.0 environment)
3. Confirm the `README.md` `### How would I contribute to this project?` link resolves to the new file.
4. Confirm the pull request guidelines (raise an issue, describe the PR, include a unit test) appear in `CONTRIBUTING.md` and are not duplicated or lost from the README edit.
5. Confirm British English spelling and one-sentence-per-line formatting throughout.

## Risks and Open Questions

| # | Risk / Question | Mitigation |
|---|-----------------|------------|
| 1 | No `global.json` pins the .NET SDK version — the stated minimum (8.0) may drift as new SDK releases land. | Document the minimum as ".NET 8 SDK" and note that newer SDKs are supported. Revisit when the project upgrades targets. |
| 2 | `dotnet format` behaviour varies by SDK version; older SDKs may produce different results. | State "use the same SDK version as the CI environment (.NET 8)" in the file. |
| 3 | Cross-platform notes for Windows CRLF vs LF — `.gitattributes` handles normalisation automatically, but contributors using Git for Windows with `core.autocrlf=true` may see unexpected diffs. | Advise contributors to keep `core.autocrlf=false` or rely on `.gitattributes`. |
| 4 | The README `### How would I contribute to this project?` section is at lines 257–263 — its existing prose must migrate to `CONTRIBUTING.md`, not be lost. | Read the exact lines before editing; confirm the migrated prose appears under a "Pull Request Guidelines" heading in `CONTRIBUTING.md`. |
