# Issue #138: Reorganise .devcontainer/ into autonomous/, scripts/, and a real VS Code devcontainer

## Summary

The `.devcontainer/` directory currently contains autonomous-agent Docker infrastructure (Dockerfile, agent-loop.sh, firewall scripts, prompt files) rather than a real VS Code / GitHub Codespaces devcontainer.
This misuses the well-known devcontainer convention and was already flagged as confusing in CONTRIBUTING.md (line 54).

The directory also mixes two concerns: host-side orchestration entry points (`run.sh`, `fleet.sh`) and Docker build internals (`Dockerfile`, `entrypoint.sh`, `init-firewall.sh`, `prompts/`).

The goal is to split these cleanly and provide a genuine VS Code devcontainer for human contributors.

## What Needs to Change and Why

### Why

- `devcontainer.json` is absent — VS Code and GitHub Codespaces users who open the repo get nothing useful.
- Agent infrastructure mixed with expected devcontainer convention causes contributor confusion (acknowledged in CONTRIBUTING.md).
- `run.sh` and `fleet.sh` are host-side scripts that belong with the other orchestration scripts in `scripts/`.

### What

1. **Create `./autonomous/`** — move all agent Docker build internals here:
   - `Dockerfile`
   - `entrypoint.sh`
   - `agent-loop.sh`
   - `init-firewall.sh`
   - `.env.example`
   - `prompts/` (all five markdown prompt files)

2. **Move `run.sh` and `fleet.sh` → `./scripts/`** — host-side entry points live alongside `plan.sh`, `execute-issue.sh`, `execute-milestone.sh`.

3. **Update `run.sh` and `fleet.sh`** — fix Docker build context and `.env` paths:
   - Build context: `"$REPO_ROOT/autonomous/"` (was `"$SCRIPT_DIR/"`)
   - Dockerfile flag: `-f "$REPO_ROOT/autonomous/Dockerfile"`
   - `.env` source/env-file: `"$REPO_ROOT/autonomous/.env"` (was `"$SCRIPT_DIR/.env"`)
   - Both scripts must derive `REPO_ROOT` from `SCRIPT_DIR` (e.g., `REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"` — but note `cd` is disallowed in agent contexts; use `$(dirname "$SCRIPT_DIR")` or `realpath "$SCRIPT_DIR/.."` instead).

4. **Create `.devcontainer/devcontainer.json`** — a proper interactive devcontainer for human contributors:
   - Base image: `mcr.microsoft.com/devcontainers/dotnet:8.0`
   - Features: Node.js 20, GitHub CLI
   - Post-create command: `dotnet restore && npm install -g @anthropic-ai/claude-code`
   - VS Code extensions: C# Dev Kit (`ms-dotnettools.csdevkit`), EditorConfig (`editorconfig.editorconfig`)
   - No firewall, no agent-loop, no fork cloning

5. **Update `CONTRIBUTING.md`** — line 54 references `.devcontainer/`; update to reference `autonomous/` and note `scripts/run.sh` / `scripts/fleet.sh` as the entry points.

## Files Affected (Confirmed by Exploration)

### Files to move (git mv)

| Source | Destination |
|---|---|
| `.devcontainer/Dockerfile` | `autonomous/Dockerfile` |
| `.devcontainer/entrypoint.sh` | `autonomous/entrypoint.sh` |
| `.devcontainer/agent-loop.sh` | `autonomous/agent-loop.sh` |
| `.devcontainer/init-firewall.sh` | `autonomous/init-firewall.sh` |
| `.devcontainer/.env.example` | `autonomous/.env.example` |
| `.devcontainer/prompts/` | `autonomous/prompts/` |
| `.devcontainer/run.sh` | `scripts/run.sh` |
| `.devcontainer/fleet.sh` | `scripts/fleet.sh` |

### Files to create

| File | Description |
|---|---|
| `.devcontainer/devcontainer.json` | VS Code devcontainer for human contributors |

### Files to edit

| File | Change |
|---|---|
| `scripts/run.sh` | Fix build context, Dockerfile path, and `.env` path to use `$REPO_ROOT/autonomous/` |
| `scripts/fleet.sh` | Fix build context, Dockerfile path, and `.env` path to use `$REPO_ROOT/autonomous/` |
| `CONTRIBUTING.md` | Update reference from `.devcontainer/` to `autonomous/`; mention `scripts/run.sh` and `scripts/fleet.sh` |

### Files confirmed unchanged

| File | Reason |
|---|---|
| `autonomous/Dockerfile` | `COPY` paths are build-context-relative — no changes needed |
| `.gitignore` | Already covers `autonomous/.env` via bare `.env` pattern on line 122 |
| `.claude/settings.json` | Already permits `Bash(./scripts/*)` |

## Acceptance Criteria

- [ ] `.devcontainer/` contains only `devcontainer.json` (no Dockerfile, no shell scripts, no prompts)
- [ ] `./autonomous/` contains: `Dockerfile`, `entrypoint.sh`, `agent-loop.sh`, `init-firewall.sh`, `.env.example`, `prompts/`
- [ ] `./scripts/` contains: `run.sh`, `fleet.sh`, `plan.sh`, `execute-issue.sh`, `execute-milestone.sh`
- [ ] `scripts/run.sh` builds Docker image with `$REPO_ROOT/autonomous/` as build context and reads `.env` from `$REPO_ROOT/autonomous/.env`
- [ ] `scripts/fleet.sh` builds Docker image with `$REPO_ROOT/autonomous/` as build context and reads `.env` from `$REPO_ROOT/autonomous/.env`
- [ ] `.devcontainer/devcontainer.json` is valid JSON with base image `mcr.microsoft.com/devcontainers/dotnet:8.0`, Node.js 20 feature, GitHub CLI feature, post-create `dotnet restore && npm install -g @anthropic-ai/claude-code`, C# Dev Kit and EditorConfig extensions
- [ ] `CONTRIBUTING.md` no longer references `.devcontainer/` for agent infrastructure; references `autonomous/` and `scripts/run.sh` / `scripts/fleet.sh`
- [ ] `autonomous/.env.example` exists (moved from `.devcontainer/.env.example`)

## Test Strategy

There are no automated tests for shell scripts or Docker infrastructure in this repository (it uses xUnit for .NET code only).
Verification is manual / structural:

1. **File presence checks** — confirm each file exists at its new path and is absent from its old path.
2. **Path correctness in scripts** — read `scripts/run.sh` and `scripts/fleet.sh` and confirm all references to the build context, Dockerfile, and `.env` resolve to `autonomous/` relative to the repo root.
3. **JSON validity** — validate `.devcontainer/devcontainer.json` with `jq empty .devcontainer/devcontainer.json`.
4. **CONTRIBUTING.md review** — confirm line 54 (and surrounding context) now references `autonomous/` and no longer says `.devcontainer/` for agent infrastructure.
5. **`dotnet build` still passes** — the .NET solution must not be broken by the reorganisation (it has no dependency on these files).

No new xUnit tests are required — the changed artefacts are infrastructure files, not library code.

## Risks and Open Questions

| Item | Detail |
|---|---|
| `REPO_ROOT` derivation in scripts | `run.sh` and `fleet.sh` currently set `SCRIPT_DIR="$(dirname "${BASH_SOURCE[0]}")"`. After moving to `scripts/`, `SCRIPT_DIR` will be `<repo>/scripts/`. Deriving `REPO_ROOT` via `"$(dirname "$SCRIPT_DIR")"` or `realpath "$SCRIPT_DIR/.."` is fine in bash outside the agent context; `cd` is forbidden inside agent tool calls but these scripts run as host commands, not via the agent's Bash tool. Use `REPO_ROOT="$(realpath "$SCRIPT_DIR/..")"` for clarity. |
| Issue body template artefact | The issue body contains `{{ISSUE_BODY}}{{ISSUE_BODY}}` in the post-create command — this is a template rendering artefact. The intended command is `dotnet restore && npm install -g @anthropic-ai/claude-code`. |
| `.env.example` move | The `.env.example` file needs to be moved (not just copied) so contributors know the canonical location is `autonomous/.env.example`. Update any inline comments or docs that reference its old path. |
| CONTRIBUTING.md scope | Only the agent-infrastructure paragraph (around line 54) needs updating. The build/test instructions for human contributors remain unchanged. |
| No devcontainer Dockerfile needed | The issue says "optionally a lightweight Dockerfile" — a plain `devcontainer.json` using the Microsoft base image is sufficient and simpler. |
