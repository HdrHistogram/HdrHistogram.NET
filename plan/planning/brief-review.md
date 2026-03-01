# Brief Review: Issue #115 — Run dotnet format

## Overall Assessment

The brief is clear and well-structured.
The scope is appropriate for a single PR.
Three concrete issues need fixing before implementation begins.

---

## Issues Found

### 1. Violation count is internally inconsistent (Critical)

The brief states **346 formatting violations across 101 files**, but the breakdown only accounts for 86:

- 78 FINALNEWLINE violations
- 8 WHITESPACE violations
- **Total: 86**, not 346

Either the 346 figure is from a different tool invocation or reporting mode, or there are additional violation categories not listed.
An implementer checking their work against "346" will be confused if `dotnet format --verify-no-changes` reports a different number.

**Action**: Run `dotnet format --verify-no-changes HdrHistogram.sln` on the current branch and replace the stale counts with actual output.
If there are additional violation types (e.g. ENCODING, CHARSET), list them explicitly.

---

### 2. Per-project file counts are inaccurate (Significant)

The brief's per-directory `.cs` file counts do not match the actual filesystem:

| Project | Brief claimed | Actual on disk |
|---------|--------------|----------------|
| `HdrHistogram/` | 51 | 56 |
| `HdrHistogram.UnitTests/` | 35 | 34 |
| `HdrHistogram.Examples/` | 4 | 6 |
| `HdrHistogram.Benchmarking/` | 13 | 27 |
| **Total** | **103** | **123** |

The benchmarking project discrepancy (13 vs 27) is large enough to cast doubt on the "101 affected files" claim.
Additionally, the per-directory totals in the brief sum to 103, not the stated 101 — a secondary arithmetic error.

**Action**: Replace the static file counts with the actual output of `dotnet format --verify-no-changes --verbosity diagnostic`, which lists every affected file.
Alternatively, note that these are approximate counts and that the authoritative list comes from the tool.

---

### 3. `dotnet format` sub-command scope is unspecified (Minor)

The implementation step says:

> Run `dotnet format HdrHistogram.sln`

Running `dotnet format` without a sub-command invokes all three fixers: `whitespace`, `style`, and `analyzers`.
The `.editorconfig` contains Roslyn naming-convention rules (`_camelCase` private fields, `PascalCase` public members) at `suggestion` severity.
By default `dotnet format style` only applies rules at `warning` severity or above, so naming rules will not be auto-applied — but this is not stated in the brief, leaving an implementer uncertain.

**Action**: Either:

- Narrow the command to `dotnet format whitespace HdrHistogram.sln` to make the whitespace-only scope explicit and remove ambiguity, or
- Keep `dotnet format HdrHistogram.sln` but add a note explaining that `suggestion`-severity naming rules are excluded from the default run, so the diff will still be whitespace-only.

The brief already identifies this as a risk; it just needs a resolution, not just a mitigation note.

---

## What Is Good

- **Clarity**: Step-by-step implementation instructions are unambiguous.
- **Scope**: A one-time bulk formatting pass is exactly one PR's worth of work.
- **Feasibility**: All referenced files and directories exist.
  `.editorconfig` is confirmed to contain `insert_final_newline = true` and `end_of_line = lf`.
  `HdrHistogram/Utilities/Bitwise.cs` exists and has visible indentation/trailing-whitespace issues.
- **Test strategy**: Correct — no new tests needed; existing suite verifies no regressions.
- **Acceptance criteria**: All six criteria are measurable and verifiable by command exit code or `git diff` inspection.
- **`.git-blame-ignore-revs`**: File does not yet exist; creating it in the same PR is the right call.
- **Analyser risk**: No custom analyser NuGet packages are present in any `.csproj` file, so the risk of non-whitespace changes from analyser rules is low.

---

## Summary of Required Changes to Brief

1. Replace stale violation counts (346 / 101) with figures from a fresh `dotnet format --verify-no-changes` run.
2. Correct or remove the per-project `.cs` file counts (actual totals are 56 / 34 / 6 / 27 = 123 files).
3. Resolve the sub-command ambiguity: specify `dotnet format whitespace` or document why `dotnet format` (full) is safe.
