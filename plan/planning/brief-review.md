# Brief Review: Issue #118 — Add CONTRIBUTING.md

## Verification Results

All factual claims in the brief were confirmed against the codebase:

| Claim | Result |
|-------|--------|
| `CONTRIBUTING.md` does not exist | Confirmed |
| `README.md` exists; Development section at line ~264 | Confirmed (exactly line 264) |
| `HdrHistogram.sln` exists | Confirmed |
| `HdrHistogram.UnitTests/HdrHistogram.UnitTests.csproj` exists | Confirmed |
| `.editorconfig` exists | Confirmed |
| `.gitattributes` exists; configures LF for text files, CRLF for `.cmd`/`.bat` | Confirmed |
| `.devcontainer/` exists; no `devcontainer.json`; contains agent-automation scripts only | Confirmed (`fleet.sh`, `agent-loop.sh`, `entrypoint.sh`, etc.) |
| `global.json` does not exist | Confirmed |
| Target frameworks: `net8.0;netstandard2.0` (library), `net8.0` (tests) | Confirmed |
| Branch naming conventions (`feat/`, `fix/`, `chore/`) in `CLAUDE.md` | Confirmed |

## Assessment

**Clarity**: Good.
Each acceptance criterion maps to a specific section of the file to create.
Commands are exact and verified.

**Scope**: Appropriate for one PR.
Two files touched, no source code changes.

**Feasibility**: All file references and commands are accurate.

**Test strategy**: Appropriate for a documentation-only change.

**Acceptance criteria**: Specific and measurable.

## One Issue Requiring Resolution

### README.md edit target does not account for the existing "How would I contribute?" section

The brief states:

> **Modify** — add a short reference to `CONTRIBUTING.md` in the Development section (line ~264)

However, the README already contains a contributing-specific subsection immediately before the Development section:

```
### How would I contribute to this project?    ← line 257

We welcome pull requests!
If you do choose to contribute, please first raise an issue…
…there would be a Unit Test proving the fix and a reference to the Issues in the PR comments.

## Development    ← line 264
```

The `## Development` section covers running tests and benchmarks — it is a technical reference, not a contributor guide.
The `### How would I contribute to this project?` section at lines 257–263 is semantically the correct home for a link to `CONTRIBUTING.md`.

Leaving the "How would I contribute?" section unchanged while adding a reference in the Development section would result in two places in README.md that address contributing, without either pointing the reader cleanly to `CONTRIBUTING.md` first.

### Actionable suggestion

Clarify the README.md edit in the brief as follows:

1. Update `### How would I contribute to this project?` (lines 257–263) to replace its current prose with a short sentence linking to `CONTRIBUTING.md`.
   The current three sentences (raise an issue first, describe the PR, include a unit test) should be preserved in `CONTRIBUTING.md` itself under a "Pull Request Guidelines" or equivalent heading — not lost.
2. Optionally, also add a one-line note in the `## Development` section pointing to `CONTRIBUTING.md` for setup instructions, since contributors reading that section may not have seen the earlier link.

The affected-files table should be updated to reflect that the edit target within `README.md` is the `### How would I contribute to this project?` section, not the `## Development` section.

## Summary

The brief is accurate and well-structured.
One targeted revision is needed: clarify which README.md section receives the `CONTRIBUTING.md` reference, and note that the existing "How would I contribute?" prose should migrate into `CONTRIBUTING.md` rather than remain as a duplicate.
No other changes are required.
