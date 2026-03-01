# Task List: Issue #70 — Fix `.gitignore` IDE artefact exclusions

## Context

Single file change: `/workspace/repo/.gitignore`.
No compiled code, tests, or XML doc comments are affected.

Current state (line numbers from the file as read):

- Line 16: `# VS Code` comment
- Line 17: `.vs/ ` — wrong artefact, wrong section, trailing space
- Line 62: `.vs/` — correct entry, correct section

---

## Tasks

- [x] **Replace `.vs/ ` with `.vscode/` on line 17 of `.gitignore`**
  - File: `/workspace/repo/.gitignore`
  - Change: replace the line `.vs/ ` (note trailing space) with `.vscode/`
  - Why: removes the misattributed, duplicated, and malformed `.vs/` entry; simultaneously adds the missing VS Code exclusion under the already-correct `# VS Code` comment
  - Verify: `grep -n '\.vs' .gitignore` shows `.vs/` exactly once (at what was line 62); `grep -n '\.vscode' .gitignore` shows `.vscode/` exactly once under `# VS Code`

- [x] **Confirm the `# Visual Studio cache/options directory` section and its `.vs/` entry are unchanged**
  - File: `/workspace/repo/.gitignore`
  - Change: none — this is a read-only verification step
  - Why: the brief requires `.vs/` to remain under the correct comment; the edit above must not touch this section
  - Verify: `grep -n -A1 'Visual Studio cache' .gitignore` shows `.vs/` immediately below the comment with no trailing space

- [x] **Verify no trailing spaces on any modified or adjacent lines**
  - File: `/workspace/repo/.gitignore`
  - Change: none — verification only
  - Why: acceptance criterion 3 explicitly forbids trailing spaces on changed/added lines
  - Verify: changed lines 17 has no trailing space; pre-existing trailing space on line 75 is unrelated

- [x] **Verify no duplicate or conflicting patterns remain**
  - File: `/workspace/repo/.gitignore`
  - Change: none — verification only
  - Why: acceptance criterion 4
  - Verify: `grep -c '\.vs/' .gitignore` prints `1`; `grep -c '\.vscode/' .gitignore` prints `1`

- [x] **Run manual git check-ignore verification**
  - File: n/a (git command)
  - Change: none
  - Why: acceptance criteria 1 and 2 require that both patterns are active in Git's ignore logic, not just present in the file
  - Verify:
    - `git check-ignore -v .vs/foo` reports the `.vs/` pattern from `.gitignore` ✓
    - `git check-ignore -v .vscode/settings.json` reports the `.vscode/` pattern from `.gitignore` ✓

---

## Acceptance Criteria Cross-Reference

| Criterion | Covered by |
|-----------|-----------|
| `.vs/` appears exactly once, under `# Visual Studio cache/options directory` | Task 1 (removes duplicate), Task 2 (confirms retained entry) |
| `.vscode/` is present under a `# VS Code` comment | Task 1 |
| No trailing spaces on changed or added lines | Task 3 |
| No duplicate or conflicting entries | Task 4 |
| All previously excluded patterns still excluded (no regressions) | Task 2 (spot-check `.vs/`), Task 5 (git check-ignore confirms live behaviour) |
