# Task List — Issue #138: Reorganise .devcontainer/ into autonomous/, scripts/, and a real VS Code devcontainer

## Current State (Confirmed by Exploration)

- `.devcontainer/` contains: `Dockerfile`, `entrypoint.sh`, `agent-loop.sh`, `init-firewall.sh`, `.env.example`, `run.sh`, `fleet.sh`, `prompts/` (5 files) — **no `devcontainer.json`**
- `scripts/` contains: `plan.sh`, `execute-issue.sh`, `execute-milestone.sh` — **no `run.sh` or `fleet.sh`**
- `autonomous/` directory **does not exist**
- `.devcontainer/devcontainer.json` **does not exist**
- `CONTRIBUTING.md` line 54 says `.devcontainer/` is for agent automation only

---

## Tasks

### Phase 1 — Move agent infrastructure files to `autonomous/`

- [ ] **T1** `git mv .devcontainer/Dockerfile autonomous/Dockerfile`
  — Moves the agent Docker build file out of the misused devcontainer dir.
  Verify: `git status` shows `renamed: .devcontainer/Dockerfile -> autonomous/Dockerfile`; file absent from `.devcontainer/`.

- [ ] **T2** `git mv .devcontainer/entrypoint.sh autonomous/entrypoint.sh`
  — Moves the container entrypoint script.
  Verify: `git status` shows rename; file absent from `.devcontainer/`.

- [ ] **T3** `git mv .devcontainer/agent-loop.sh autonomous/agent-loop.sh`
  — Moves the agent state-machine loop.
  Verify: `git status` shows rename; file absent from `.devcontainer/`.

- [ ] **T4** `git mv .devcontainer/init-firewall.sh autonomous/init-firewall.sh`
  — Moves the firewall initialisation script.
  Verify: `git status` shows rename; file absent from `.devcontainer/`.

- [ ] **T5** `git mv .devcontainer/.env.example autonomous/.env.example`
  — Moves the environment variable template. `.gitignore` already covers `autonomous/.env` via the bare `.env` pattern (line 122).
  Verify: `autonomous/.env.example` exists; `.devcontainer/.env.example` absent.

- [ ] **T6** `git mv .devcontainer/prompts autonomous/prompts`
  — Moves the entire prompts directory (5 markdown files: `execute-tasks.md`, `pick-issue.md`, `apply-review.md`, `create-tasks.md`, `review-brief.md`).
  Verify: `autonomous/prompts/` contains all 5 files; `.devcontainer/prompts/` absent.

### Phase 2 — Move host-side entry points to `scripts/`

- [ ] **T7** `git mv .devcontainer/run.sh scripts/run.sh`
  — Moves the single-agent launch script alongside the other host-side orchestration scripts.
  Verify: `scripts/run.sh` exists; `.devcontainer/run.sh` absent.

- [ ] **T8** `git mv .devcontainer/fleet.sh scripts/fleet.sh`
  — Moves the multi-agent fleet orchestration script.
  Verify: `scripts/fleet.sh` exists; `.devcontainer/fleet.sh` absent.

### Phase 3 — Update path references in moved scripts

- [ ] **T9** Update `scripts/run.sh` — add `REPO_ROOT` derivation and fix all paths.
  After the existing `SCRIPT_DIR` line, add:
  `REPO_ROOT="$(realpath "$SCRIPT_DIR/..")"`
  Then replace:
  - `"$SCRIPT_DIR/.env"` (source) → `"$REPO_ROOT/autonomous/.env"`
  - `"$SCRIPT_DIR/Dockerfile"` → `"$REPO_ROOT/autonomous/Dockerfile"`
  - `"$SCRIPT_DIR/"` (build context) → `"$REPO_ROOT/autonomous/"`
  - `"$SCRIPT_DIR/.env"` (env-file) → `"$REPO_ROOT/autonomous/.env"`
  Verify: Read `scripts/run.sh`; confirm no remaining `$SCRIPT_DIR` references to `.env` or `Dockerfile`; all four path occurrences use `$REPO_ROOT/autonomous/`.

- [ ] **T10** Update `scripts/fleet.sh` — add `REPO_ROOT` derivation and fix all paths.
  After the existing `SCRIPT_DIR` line, add:
  `REPO_ROOT="$(realpath "$SCRIPT_DIR/..")"`
  Then replace:
  - `"$SCRIPT_DIR/.env"` (source) → `"$REPO_ROOT/autonomous/.env"`
  - `"$SCRIPT_DIR/Dockerfile"` → `"$REPO_ROOT/autonomous/Dockerfile"`
  - `"$SCRIPT_DIR/"` (build context) → `"$REPO_ROOT/autonomous/"`
  - `"$SCRIPT_DIR/.env"` (env-file) → `"$REPO_ROOT/autonomous/.env"`
  Verify: Read `scripts/fleet.sh`; confirm no remaining `$SCRIPT_DIR` references to `.env` or `Dockerfile`; all four path occurrences use `$REPO_ROOT/autonomous/`.

### Phase 4 — Create the real VS Code devcontainer

- [ ] **T11** Create `.devcontainer/devcontainer.json` with the following content:
  - `name`: `"HdrHistogram.NET"`
  - `image`: `"mcr.microsoft.com/devcontainers/dotnet:8.0"`
  - `features`: Node.js 20 (`ghcr.io/devcontainers/features/node:1` with `version: "20"`) and GitHub CLI (`ghcr.io/devcontainers/features/github-cli:1`)
  - `postCreateCommand`: `"dotnet restore && npm install -g @anthropic-ai/claude-code"`
  - `customizations.vscode.extensions`: `["ms-dotnettools.csdevkit", "editorconfig.editorconfig"]`
  Verify: `jq empty .devcontainer/devcontainer.json` exits 0 (valid JSON); `jq '.image' .devcontainer/devcontainer.json` outputs `"mcr.microsoft.com/devcontainers/dotnet:8.0"`.

### Phase 5 — Update CONTRIBUTING.md

- [ ] **T12** Update `CONTRIBUTING.md` around line 54 — replace the paragraph that references `.devcontainer/` as agent-only infrastructure.
  New text should:
  - State that `autonomous/` contains the agent Docker infrastructure
  - Name `scripts/run.sh` and `scripts/fleet.sh` as the host-side entry points
  - No longer describe `.devcontainer/` as agent-only (it is now a proper VS Code devcontainer)
  Verify: Read `CONTRIBUTING.md`; confirm the word "`.devcontainer/`" does not appear in the context of agent infrastructure; `autonomous/` is mentioned; `scripts/run.sh` and `scripts/fleet.sh` are named.

### Phase 6 — Verification

- [ ] **T13** Confirm `.devcontainer/` contains only `devcontainer.json` and nothing else.
  Run: `ls .devcontainer/` and confirm only `devcontainer.json` is listed.

- [ ] **T14** Confirm `autonomous/` contains all six expected items.
  Run: `ls autonomous/` and confirm `Dockerfile`, `entrypoint.sh`, `agent-loop.sh`, `init-firewall.sh`, `.env.example`, `prompts/` are present.

- [ ] **T15** Confirm `scripts/` contains all five expected scripts.
  Run: `ls scripts/` and confirm `run.sh`, `fleet.sh`, `plan.sh`, `execute-issue.sh`, `execute-milestone.sh` are present.

- [ ] **T16** Validate `devcontainer.json` is well-formed JSON.
  Run: `jq empty .devcontainer/devcontainer.json` — must exit 0 with no output.

- [ ] **T17** Verify `dotnet build` still passes (the .NET solution has no dependency on infrastructure files).
  Run: `dotnet build` from repo root — must exit 0 with no errors.

---

## Acceptance Criteria Cross-Reference

| Acceptance Criterion | Covered By |
|---|---|
| `.devcontainer/` contains only `devcontainer.json` | T1–T8 (moves), T13 (verify) |
| `./autonomous/` contains `Dockerfile`, `entrypoint.sh`, `agent-loop.sh`, `init-firewall.sh`, `.env.example`, `prompts/` | T1–T6, T14 (verify) |
| `./scripts/` contains `run.sh`, `fleet.sh`, `plan.sh`, `execute-issue.sh`, `execute-milestone.sh` | T7–T8, T15 (verify) |
| `scripts/run.sh` uses `$REPO_ROOT/autonomous/` for build context and `.env` | T9 |
| `scripts/fleet.sh` uses `$REPO_ROOT/autonomous/` for build context and `.env` | T10 |
| `.devcontainer/devcontainer.json` is valid JSON with correct base image, features, post-create command, extensions | T11, T16 (verify) |
| `CONTRIBUTING.md` references `autonomous/` and `scripts/run.sh` / `scripts/fleet.sh`; no `.devcontainer/` for agent infra | T12 |
| `autonomous/.env.example` exists | T5, T14 (verify) |
