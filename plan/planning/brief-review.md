# Brief Review: Issue #161 — Drop netstandard2.0

## Verdict: Changes required before moving to ready

The file list, acceptance criteria, and test strategy are solid.
Three issues need resolving.

---

## Issue 1 — Category `performance` is inconsistent with the brief's own analysis (blocking)

The brief states:

> "A micro-benchmark improvement in LeadingZeroCount that does not translate to observable improvement in RecordValue throughput is expected (the intrinsic is already used on net8.0+)."

This concedes the point: **no existing net8.0+ consumer will see any performance improvement**.
The hardware intrinsic path is already taken at compile time via `#if NET5_0_OR_GREATER`.
Removing the fallback does not change the compiled output for any net8.0 or net9.0 or net10.0 build.

The performance gain only applied to consumers targeting the `netstandard2.0` package on a modern runtime — consumers who are now being dropped, not improved.

The accurate category is `chore` (or `refactor`).

**Action required — choose one:**

- **Option A**: Change category to `chore`.
  Remove the entire "Benchmark Strategy" section (benchmarks are not required for `chore`).
  Retain the note that `LeadingZeroCountBenchmarkBase.cs` must be updated to remove the `ImperativeImplementation` method so it still compiles.

- **Option B**: Keep category as `performance` and rewrite the performance rationale.
  The argument must be made without contradiction: e.g., "consumers who pinned to the `netstandard2.0` target on .NET 5+ runtimes received the slow fallback path; this change forces them onto the net8.0 target, where they receive the intrinsic." Acknowledge this is a narrow population.
  With `performance` retained, the benchmark plan must also be strengthened (see Issue 2).

---

## Issue 2 — End-to-end benchmark is conditional when it should be mandatory (blocking if `performance` is kept)

The brief says:

> "If an end-to-end benchmark for `RecordValue` throughput exists (check `Recording/`), run it too."

`HdrHistogram.Benchmarking/Recording/Recording32BitBenchmark.cs` **was confirmed to exist**.
The spec (`testing-standards.md`) states: "Both levels of benchmark are required."
Conditional language is not acceptable for a `performance` issue.

**Action required (if `performance` category is retained):**

Replace the conditional phrasing with an explicit instruction:

> "Run `Recording32BitBenchmark` as the end-to-end benchmark.
> Include before/after results in the PR description alongside the `LeadingZeroCount` micro-benchmark results."

---

## Issue 3 — PropertyGroup consolidation scope is unresolved (minor, non-blocking)

Risk item 4 notes that the three per-framework `Release` `PropertyGroup` conditions could be collapsed into one unconditional block, but says "should be confirmed with the implementer."

A brief should not leave scope ambiguous.
The implementer cannot resolve this themselves — it must be decided before implementation begins.

**Action required:**

Make an explicit decision and state it clearly, e.g.:

> "Collapsing the three per-framework Release `PropertyGroup` conditions into a single `Condition="'$(Configuration)' == 'Release'"` block is **in scope** for this PR."

or:

> "Collapsing the Release `PropertyGroup` conditions is **out of scope**; a follow-up chore can address it."

---

## What is correct and does not need changing

- All four affected files exist at the stated paths.
- Line-level descriptions of the required changes are accurate.
- The claim that only two source files contain the relevant `#if` blocks is confirmed.
- Unit test and benchmarking projects already target `net10.0;net9.0;net8.0` — no changes needed there.
- Acceptance criteria are measurable and complete.
- The note that `ImperativeImplementation` in `LeadingZeroCountBenchmarkBase.cs` must be removed is correct and necessary.
- The NuGet breaking-change risk is correctly identified.
