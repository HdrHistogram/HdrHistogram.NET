# Issue #114: Add .editorconfig with project coding conventions

## Summary

Add a `.editorconfig` file at the repository root to codify the coding conventions already present in the codebase.
This is a foundation step for subsequent code-style enforcement and does not reformat any existing code.

The file must reflect observed conventions — not impose new ones — and must not break the build.
Severity levels must be `suggestion` or `warning`, never `error`.

## What needs to change and why

Currently the repository has no `.editorconfig`.
Editors and tooling have no machine-readable source of truth for indentation, line endings, charset, or C# style rules.
Adding `.editorconfig` at the root:

- Gives editors (VS, Rider, VS Code) automatic style feedback without CI enforcement.
- Codifies what is already in the codebase so future contributors follow the same conventions.
- Is a prerequisite for subsequent automated style-enforcement issues.

No existing file is reformatted as part of this issue.

## Affected files (confirmed by exploration)

| File | Change |
|------|--------|
| `.editorconfig` | **New file** — created at repository root |

No existing source files are modified.
The new `.editorconfig` is picked up automatically by .NET SDK / Roslyn for any file it covers.

## Conventions observed in the codebase

These settings must be reflected in `.editorconfig`:

### General (all files)

- Charset: `utf-8`
- Line endings: `lf` (enforced by `.gitattributes`; `.editorconfig` should agree)
- Insert final newline: `true`
- Trim trailing whitespace: `true`

### C# files (`.cs`)

- Indent style: spaces
- Indent size: 4
- Namespace style: block-scoped (traditional `namespace Foo { }`)
- `using` directives: outside the namespace block
- `var` usage: consistent with existing code (used where type is apparent, explicit otherwise)
- Private fields: `_camelCase` (underscore prefix + camelCase)
- Public members: PascalCase
- Interfaces: `I` prefix + PascalCase
- Constants / static readonly: PascalCase
- Parameters / local variables: camelCase
- Brace style: opening brace on same line (K&R / Allman — confirm from files)

### XML / project files (`.csproj`, `.props`, `.targets`)

- Indent style: spaces
- Indent size: 2

### JSON files (`.json`)

- Indent style: spaces
- Indent size: 2

### YAML files (`.yml`, `.yaml`)

- Indent style: spaces
- Indent size: 2

### Markdown files (`.md`)

- Trim trailing whitespace: `true`
- Insert final newline: `true`

### Solution files (`.sln`)

- Indent style: tabs (Visual Studio default — leave as-is or omit)

## Acceptance criteria

- [ ] `.editorconfig` file added at repository root
- [ ] Settings reflect the conventions already used in the existing codebase (no new rules invented)
- [ ] All severity levels for C# style rules set to `suggestion` or `warning` — never `error`
- [ ] `dotnet build` succeeds without new errors after the file is added
- [ ] Settings are documented with inline comments where the convention might not be obvious
- [ ] `root = true` is present so the file applies from the repository root

## Test strategy

### Build verification

Run `dotnet build` from the repository root and confirm:

- Exit code is 0
- No new errors introduced
- New warnings (if any) are at `suggestion` / `warning` severity only

### Manual inspection

- Open a C# file in an editor that respects `.editorconfig` (VS Code / VS / Rider) and confirm style hints appear as expected.
- Verify that the file is valid `.editorconfig` syntax (no parse errors reported by editor tooling).

### No automated test changes

No unit tests need to be added or modified.
The `.editorconfig` is tooling configuration, not logic.

## Risks and open questions

| Risk / Question | Notes |
|----------------|-------|
| Brace style (K&R vs Allman) | C# conventionally uses Allman (opening brace on new line). The existing code should be confirmed before setting `csharp_new_line_before_open_brace`. |
| `var` preference | The codebase mixes explicit types and `var`; severity must stay at `suggestion` to avoid noisy warnings. |
| Solution file indentation | `.sln` files use tab indentation by Visual Studio convention; either omit `.sln` from `.editorconfig` rules or match tabs to avoid false positives. |
| Severity escalation | Future issues will raise severity to `warning` or `error`; this issue intentionally stays at `suggestion`. Do NOT pre-empt that work here. |
| `dotnet format` | Must NOT be run as part of this issue. Reformatting is a separate subsequent issue. |
