# Brief Review: Issue #105 — Remove EOL Target Frameworks from Benchmarking Project

## Overall Assessment

The brief is well-structured and accurate.
All file paths, current `TargetFrameworks` values, and affected sections were verified against the actual codebase.
Three issues require resolution before the brief is ready for implementation.

---

## Issues

### 1. Ambiguous target state — decision must be made in the brief

**Section:** Target State / Risks and Open Questions

The brief proposes `net8.0;net9.0` as the primary target state, then hedges:

> If `net9.0` is judged too close to its own EOL date at the time of merge, `net8.0` alone is an acceptable minimum.

As of 2026-03-01, `net9.0` reaches end-of-life in May 2026 — approximately two months away.
The brief's own fallback condition is already satisfied.
Leaving this choice to the implementer introduces unnecessary ambiguity.

**Action required:** Commit to a single target state in the brief.
Given `net9.0`'s imminent EOL and the CI constraint described in issue 2 below, the recommended resolution is `net8.0` only.
Update the Target State section accordingly and remove the hedge.

---

### 2. Missing affected file: `.github/workflows/ci.yml`

**Section:** Affected Files

The CI workflow at `.github/workflows/ci.yml` pins the .NET SDK to `'8.0.x'`:

```yaml
- uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '8.0.x'
```

The .NET 8 SDK cannot build `net9.0` targets.
If `net9.0` is kept in the target state, CI will fail and `.github/workflows/ci.yml` must be updated to install the .NET 9 SDK (or use a multi-SDK setup).
This file is absent from the affected files table.

**Action required (two options — pick one consistently with issue 1):**

- **Option A (recommended):** Resolve issue 1 as `net8.0` only.
  No CI change is needed; remove this item.
- **Option B:** Keep `net9.0` in the target state.
  Add `.github/workflows/ci.yml` to the affected files table with the change description "Update `dotnet-version` to `'9.0.x'` (or add a second SDK install step)".
  Also add an acceptance criterion: "CI build completes without SDK-version errors for all listed target frameworks."

---

### 3. Incomplete spec update: `build-system.md` narrative section

**Section:** Affected Files / Acceptance Criteria

The brief specifies updating the "Benchmarking Project" TFM block in `spec/tech-standards/build-system.md` (lines 39–45).
However, the same file contains a separate narrative section at lines 226–229:

```markdown
### Benchmark Configuration

BenchmarkDotNet is used with these targets:
- Multiple .NET versions for comparison
- Windows diagnostics support
- Memory allocation tracking
```

If the final target is `net8.0` only, "Multiple .NET versions for comparison" becomes inaccurate.
If the target is `net8.0;net9.0`, the statement remains true but the list of versions should be explicit.

**Action required:** Add an explicit note to the affected files entry for `build-system.md` to also update the "Benchmark Configuration" narrative bullet to reflect the chosen target(s).
Add a corresponding acceptance criterion, for example:
"The `build-system.md` Benchmark Configuration section accurately describes the final `TargetFrameworks` value."

---

## Minor Observations (no action required)

- The claim that `HdrHistogram.csproj` targets `net8.0;netstandard2.0` and that both `HdrHistogram.UnitTests` and `HdrHistogram.Examples` target `net8.0` only is confirmed correct.
- `BenchmarkDotNet.Diagnostics.Windows` is confirmed present in the benchmarking `.csproj`.
  The risk note about Linux CI is valid and no action beyond the existing note is needed.
- No other projects reference the benchmarking project; no downstream side effects exist.
- The test strategy (build-only verification) is appropriate given the absence of unit tests in the benchmarking project.
- Acceptance criteria are otherwise measurable and verifiable.
