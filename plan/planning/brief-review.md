# Brief Review: Issue #118 — Add CONTRIBUTING.md

## Overall Assessment

The brief is well-scoped, clear, and largely accurate.
All referenced files have been verified to exist (or to be absent, in the case of `CONTRIBUTING.md` and `global.json`).
The single PR is a sensible unit of work.
One factual inaccuracy must be corrected before the brief is ready to implement.

---

## Verification Results

| Claim in Brief | Verified? | Notes |
|---|---|---|
| `CONTRIBUTING.md` does not exist | Yes | Absent from repository root |
| `README.md` Development section at ~line 264 | Yes | `## Development` heading is at exactly line 264 |
| `.editorconfig` exists | Yes | 126-line file with comprehensive C# and Markdown rules |
| `.gitattributes` normalises line endings | Yes | LF for most files; CRLF for `*.cmd`/`*.bat` |
| `HdrHistogram.sln` exists | Yes | Four-project solution at repo root |
| `HdrHistogram.UnitTests/HdrHistogram.UnitTests.csproj` exists | Yes | TFM: `net8.0` |
| Main library targets `net8.0;netstandard2.0` | Yes | Confirmed in `HdrHistogram/HdrHistogram.csproj` |
| No `global.json` pins SDK version | Yes | File is absent |

---

## Issues Found

### Issue 1 — Devcontainer reference is inaccurate (must fix)

**Acceptance criterion:** "Covers cross-platform notes: shell scripts require LF; the devcontainer is available for a consistent Linux environment."

**Finding:** The `.devcontainer/` directory exists but contains only agent-automation tooling (`fleet.sh`, `agent-loop.sh`, `entrypoint.sh`, `prompts/`, etc.).
There is **no `devcontainer.json`**, so VS Code's "Reopen in Container" feature does not work.
This is not a standard development container for human contributors.

**Impact:** If CONTRIBUTING.md tells contributors "use the devcontainer for a consistent environment," they will find no working devcontainer configuration and will be confused.

**Action required:** Remove the devcontainer sentence from the cross-platform acceptance criterion.
The cross-platform notes should instead focus only on what is accurate and verifiable:

- Shell scripts require LF line endings.
- The `.gitattributes` file normalises line endings automatically on checkout.
- Contributors on Windows should keep `core.autocrlf=false` (or let `.gitattributes` handle it).
- All platforms with the .NET 8 SDK installed can build and test without any container.

---

## What Is Good (No Changes Needed)

- **Scope** — Creating one new file and making a small targeted edit to `README.md` is the right size for a single PR.
- **Build and test commands** — Both commands are correct and already used in `README.md`.
- **`.editorconfig` reference** — File exists and is comprehensive.
- **Git workflow rules** — Match `CLAUDE.md` exactly.
- **Line endings section** — Accurate; `.gitattributes` content confirmed.
- **Markdown standards** — British English, one sentence per line, blank lines under headings and around lists are all correctly stated.
- **Test strategy** — Manual verification steps are clear and complete for a documentation-only change.
- **Risks table** — Risk items 1–4 are all valid and well-mitigated.

---

## Required Change Summary

1. In **Acceptance Criteria**, remove the clause "the devcontainer is available for a consistent Linux environment" from the cross-platform notes bullet.
   Replace with: "All platforms with the .NET 8 SDK can build and test natively; no container is required."
2. Optionally, add a note that `.devcontainer/` is agent-automation infrastructure and is not intended for human contributors.

Once this change is made the brief is ready to move to `plan/ready/`.
