# Task List: Issue #118 — Add CONTRIBUTING.md with Development Setup and Guidelines

## Context

- `CONTRIBUTING.md` does not yet exist at the repository root.
- `README.md` lines 257–263 contain the contribution section whose prose must migrate to `CONTRIBUTING.md`.
- `.gitattributes` sets `* text=auto eol=lf`; `.cmd`/`.bat` use CRLF by exception.
- `.editorconfig` governs code style; `dotnet format` is the formatting tool.
- No `global.json` exists; minimum SDK is .NET 8.
- `.devcontainer/` contains agent-automation scripts only — no `devcontainer.json`.
- This task touches only documentation files; no `.cs` or `.csproj` files change.

---

## Phase 1 — Create `CONTRIBUTING.md`

- [x] **Task 1** — Create `CONTRIBUTING.md` at the repository root with a `# Contributing` heading and a one-to-two sentence introduction welcoming contributions.
  - **File**: `CONTRIBUTING.md` (new file, `/workspace/repo/CONTRIBUTING.md`)
  - **Why**: The file must exist at the root; without it no other section tasks can be placed.
  - **Verify**: File exists; first line is `# Contributing`; blank line follows the heading.

- [x] **Task 2** — Add `## Prerequisites` section to `CONTRIBUTING.md`.
  - **File**: `CONTRIBUTING.md`
  - **Change**: State .NET 8 SDK as the minimum (note newer SDKs are also supported); list supported platforms: Windows, macOS, Linux.
  - **Why**: Acceptance criterion — prerequisites (.NET SDK version and platforms).
  - **Verify**: Section contains ".NET 8 SDK" and names all three platforms; heading has a blank line beneath it.

- [x] **Task 3** — Add `## Building` section to `CONTRIBUTING.md`.
  - **File**: `CONTRIBUTING.md`
  - **Change**: Add a `sh` code block containing `dotnet build HdrHistogram.sln`.
  - **Why**: Acceptance criterion — building the solution.
  - **Verify**: Section contains the exact command `dotnet build HdrHistogram.sln` in a fenced code block.

- [x] **Task 4** — Add `## Testing` section to `CONTRIBUTING.md`.
  - **File**: `CONTRIBUTING.md`
  - **Change**: Add a `sh` code block containing `dotnet test ./HdrHistogram.UnitTests/HdrHistogram.UnitTests.csproj -v q -c Release`.
  - **Why**: Acceptance criterion — running the test suite.
  - **Verify**: Section contains the exact test command in a fenced code block.

- [x] **Task 5** — Add `## Code Style` section to `CONTRIBUTING.md`.
  - **File**: `CONTRIBUTING.md`
  - **Change**: State that code style is governed by `.editorconfig`; instruct contributors to run `dotnet format` before submitting a PR; note that the .NET 8 SDK should be used to match the CI environment.
  - **Why**: Acceptance criterion — code style (`.editorconfig` reference and `dotnet format` instruction).
  - **Verify**: Section names `.editorconfig`; section mentions `dotnet format`; no American English spellings.

- [x] **Task 6** — Add `## Git Workflow` section to `CONTRIBUTING.md`.
  - **File**: `CONTRIBUTING.md`
  - **Change**: Cover the four workflow rules: (1) branch naming prefixes `feat/`, `fix/`, `chore/`; (2) one issue per PR; (3) PRs target `main`; (4) no direct commits to `main`.
  - **Why**: Acceptance criterion — git workflow.
  - **Verify**: All four rules are present; branch prefix examples are shown; heading followed by blank line; list surrounded by blank lines.

- [x] **Task 7** — Add `## Pull Request Guidelines` section to `CONTRIBUTING.md`, migrating the prose from `README.md` lines 259–262.
  - **File**: `CONTRIBUTING.md`
  - **Change**: Add a section with three rules drawn verbatim (adjusted for sentence-per-line format) from the README: (1) raise an issue before contributing; (2) describe what the PR achieves and which issues it closes; (3) include a unit test proving any fix and reference the issue in the PR comments.
  - **Why**: Acceptance criterion — pull request guidelines; the brief requires these sentences to migrate rather than be lost.
  - **Verify**: All three original README sentences are present in this section (adjusted for markdown style, not discarded); section follows blank-line rules.

- [x] **Task 8** — Add `## Line Endings` section to `CONTRIBUTING.md`.
  - **File**: `CONTRIBUTING.md`
  - **Change**: State LF is the project convention; explain that `.gitattributes` normalises line endings automatically on checkout; note that Windows batch files (`.cmd`, `.bat`) use CRLF by exception; advise contributors to keep `core.autocrlf=false` or rely on `.gitattributes`.
  - **Why**: Acceptance criterion — line endings (LF convention, `.gitattributes`, CRLF exception for batch files).
  - **Verify**: Section mentions LF, `.gitattributes`, CRLF for `.cmd`/`.bat`, and `core.autocrlf`.

- [x] **Task 9** — Add `## Cross-Platform Notes` section to `CONTRIBUTING.md`.
  - **File**: `CONTRIBUTING.md`
  - **Change**: Cover four points: (1) shell scripts require LF line endings; (2) `.gitattributes` normalises line endings on checkout so no manual intervention is needed; (3) all platforms (Windows, macOS, Linux) with the .NET 8 SDK can build and test natively — no container is required; (4) `.devcontainer/` exists in the repository but contains agent-automation infrastructure only (`fleet.sh`, `agent-loop.sh`, etc.) and is not intended for human contributors.
  - **Why**: Acceptance criterion — cross-platform notes including the devcontainer clarification.
  - **Verify**: Section addresses all four points; devcontainer note names at least one contained script as evidence it is automation-only.

---

## Phase 2 — Update `README.md`

- [x] **Task 10** — Replace the body of `README.md` `### How would I contribute to this project?` (lines 259–262) with a single sentence linking to `CONTRIBUTING.md`.
  - **File**: `README.md`
  - **Change**: Remove the three prose sentences ("We welcome pull requests!", raise issue first, describe PR, include unit test) and replace them with one sentence, e.g. "See [CONTRIBUTING.md](CONTRIBUTING.md) for full contribution guidelines."
    The `### How would I contribute to this project?` heading is kept unchanged.
  - **Why**: Acceptance criterion — README links to `CONTRIBUTING.md`; original prose must not be duplicated in README (it now lives in `CONTRIBUTING.md`).
  - **Verify**: `git diff README.md` shows the three original sentences removed and replaced with a link; heading line is unchanged; the word "CONTRIBUTING.md" appears in the section body as a markdown link.

- [x] **Task 11** — Add a one-line pointer to `CONTRIBUTING.md` in `README.md` `## Development` section (line 264 area).
  - **File**: `README.md`
  - **Change**: Insert a single sentence immediately after the `## Development` heading (before `### Running the Tests`), e.g. "For prerequisites and development setup, see [CONTRIBUTING.md](CONTRIBUTING.md)."
  - **Why**: Acceptance criterion — `## Development` carries an optional one-line pointer to `CONTRIBUTING.md`.
  - **Verify**: `## Development` section contains a link to `CONTRIBUTING.md`; no other content in the `## Development` section is altered.

---

## Phase 3 — Formatting Verification

- [x] **Task 12** — Verify markdown formatting throughout `CONTRIBUTING.md` against `CLAUDE.md` standards.
  - **File**: `CONTRIBUTING.md`
  - **Change**: Review and correct any issues: British English spelling (e.g. "normalise" not "normalize", "licence" not "license"); one sentence per line with no mid-sentence line breaks; every heading followed by a blank line; every ordered or unordered list preceded and followed by a blank line.
  - **Why**: Acceptance criteria — British English, one-sentence-per-line, heading spacing, list spacing.
  - **Verify**: No American English spellings; each sentence ends with `.` or `:` before a newline; no heading is immediately followed by content without a blank line; no list starts or ends without surrounding blank lines.

- [x] **Task 13** — Verify `README.md` diff is minimal and does not introduce formatting regressions.
  - **File**: `README.md`
  - **Change**: Inspect `git diff README.md`; confirm only the two intended hunks are changed (lines 259–262 contribution prose, and the `## Development` one-liner).
  - **Why**: Risk mitigation — no unintended changes to surrounding README content.
  - **Verify**: `git diff README.md` shows exactly two change hunks and no other modifications.

---

## Acceptance Criteria Cross-Reference

| Acceptance Criterion (brief) | Covered by Task(s) |
|---|---|
| `CONTRIBUTING.md` exists at repo root | Task 1 |
| Prerequisites: .NET 8 SDK, Windows/macOS/Linux | Task 2 |
| Building: `dotnet build HdrHistogram.sln` | Task 3 |
| Testing: full `dotnet test` command | Task 4 |
| Code style: `.editorconfig` + `dotnet format` | Task 5 |
| Git workflow: branch naming, one issue/PR, target main, no direct commits | Task 6 |
| Pull request guidelines: raise issue, describe PR, include unit test | Task 7 |
| Line endings: LF convention, `.gitattributes`, CRLF for batch | Task 8 |
| Cross-platform notes: shell LF, `.gitattributes`, no container, devcontainer note | Task 9 |
| British English, one sentence per line | Task 12 |
| Headings blank line beneath; lists blank lines around | Task 12 |
| README contribution section links to `CONTRIBUTING.md`; no duplication | Task 10 |
| README `## Development` has optional pointer to `CONTRIBUTING.md` | Task 11 |
