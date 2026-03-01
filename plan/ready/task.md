# Task Checklist — Issue #114: Add `.editorconfig`

## Exploration findings

From codebase inspection:

- **Brace style**: K&R (opening brace on same line — not Allman)
- **`using` placement**: outside namespace blocks
- **Namespace style**: block-scoped (`namespace Foo { }`)
- **`var` usage**: used where type is apparent from RHS; explicit otherwise
- **Private fields**: `_camelCase`
- **Public members**: PascalCase
- **Interfaces**: `I` prefix + PascalCase
- **Indent — C# files**: 4 spaces
- **Indent — XML/JSON/YAML**: 2 spaces
- **Line endings**: LF (CRLF only for `.cmd`/`.bat`)
- **Solution file**: tabs (Visual Studio default)
- **No `.editorconfig` currently exists**

---

## Tasks

### 1. Confirm brace style from source files

- [ ] Read 3–5 representative C# files (e.g. `HdrHistogram/HistogramBase.cs`, `HdrHistogram/Recorder.cs`, `HdrHistogram/HistogramLogWriter.cs`) to confirm K&R brace style throughout.
  - **File**: existing source files (read-only)
  - **Why**: The brief lists this as an open question; exploration says K&R — confirm before writing the rule.
  - **Verify**: The `csharp_new_line_before_open_brace` setting in the final `.editorconfig` matches what is seen.

### 2. Create `.editorconfig` at repository root

- [ ] Create `/workspace/repo/.editorconfig` with the following sections, each annotated with inline comments:

  **`[*]` — all files**
  - `root = true`
  - `charset = utf-8`
  - `end_of_line = lf`
  - `insert_final_newline = true`
  - `trim_trailing_whitespace = true`

  **`[*.cs]` — C# source files**
  - `indent_style = space`
  - `indent_size = 4`
  - Roslyn naming rules: `_camelCase` for private fields (suggestion)
  - `csharp_using_directive_placement = outside_namespace:suggestion`
  - `csharp_style_namespace_declarations = block_scoped:suggestion`
  - `csharp_new_line_before_open_brace = none` (K&R style, confirmed above):suggestion
  - `csharp_prefer_var_elsewhere = true:suggestion`
  - `csharp_prefer_var_when_type_is_apparent = true:suggestion`
  - Naming: PascalCase for public symbols, `I`-prefix for interfaces, `_camelCase` for private fields — all at `suggestion`

  **`[*.{csproj,props,targets,xml}]` — XML/project files**
  - `indent_style = space`
  - `indent_size = 2`

  **`[*.{json,yml,yaml}]` — JSON and YAML**
  - `indent_style = space`
  - `indent_size = 2`

  **`[*.md]` — Markdown**
  - `trim_trailing_whitespace = true`
  - `insert_final_newline = true`

  **`[*.sln]` — Solution files**
  - `indent_style = tab` (Visual Studio convention)

  **`[*.{cmd,bat}]` — Windows scripts**
  - `end_of_line = crlf`

  - **File**: `/workspace/repo/.editorconfig` (new file)
  - **Why**: Core deliverable of issue #114; codifies conventions without reformatting code.
  - **Verify**: File exists at repo root; `root = true` is present; all sections listed above are present; every C# severity is `suggestion` or `warning`.

### 3. Verify all C# severity levels are `suggestion` or `warning`

- [ ] Grep the new `.editorconfig` for any occurrence of `:error` to confirm none are present.
  - **File**: `/workspace/repo/.editorconfig`
  - **Why**: Acceptance criterion — severity must never be `error`.
  - **Verify**: Zero matches for `:error`.

### 4. Verify `root = true` is present

- [ ] Confirm the first non-comment line in `.editorconfig` is `root = true`.
  - **File**: `/workspace/repo/.editorconfig`
  - **Why**: Acceptance criterion — ensures the file is treated as the root configuration.
  - **Verify**: `root = true` appears before any section header.

### 5. Run `dotnet build` and confirm success

- [ ] Run `dotnet build` from `/workspace/repo` and check exit code is 0 with no new errors.
  - **Why**: Acceptance criterion — the new file must not break the build.
  - **Verify**: Build output shows 0 errors; any new diagnostics are at `suggestion` / `warning` level only.

### 6. Inspect inline comments for non-obvious rules

- [ ] Read the finished `.editorconfig` and confirm every non-obvious rule (e.g. `_camelCase` naming, `csharp_new_line_before_open_brace`, `csharp_using_directive_placement`) has an explanatory comment.
  - **File**: `/workspace/repo/.editorconfig`
  - **Why**: Acceptance criterion — "Settings are documented with inline comments where the convention might not be obvious."
  - **Verify**: Each Roslyn/C# style rule has at least a brief `#` comment.

---

## Acceptance criteria cross-reference

| Criterion (from brief) | Covered by task(s) |
|---|---|
| `.editorconfig` file added at repository root | Task 2 |
| Settings reflect conventions already used (no new rules invented) | Tasks 1 + 2 |
| All C# severity levels `suggestion` or `warning` — never `error` | Tasks 2 + 3 |
| `dotnet build` succeeds without new errors | Task 5 |
| Settings documented with inline comments where non-obvious | Tasks 2 + 6 |
| `root = true` is present | Tasks 2 + 4 |

---

## Out of scope (per brief)

- No existing source files are reformatted.
- No unit tests are added or modified.
- `dotnet format` must NOT be run.
- Severity must NOT be escalated beyond `suggestion`; that is a future issue.
