# Brief Review — Issue #131: Fix build warnings from dotnet build Release configuration

## Verdict

The brief is well-structured and nearly ready.
One factual gap must be addressed before it moves to ready.
All other sections (clarity, scope, test strategy, acceptance criteria) are strong.

---

## Findings

### Files and line numbers

All 13 files listed in the brief were confirmed to exist.
Every code excerpt was verified against the actual source — line numbers and quoted code are accurate.

### Clarity

The tables (file / line / current code / required fix) are unambiguous.
A developer could implement every change mechanically from the brief alone.

### Scope

All changes are annotation, style, and type-system fixes with no behaviour impact.
The number of files is large (13) but every change is small and mechanical.
This is one PR's worth of work.

### Test strategy

The four test areas are correctly identified and the exact run command is provided.
No new tests are required.

### Acceptance criteria

Zero warnings from `dotnet build -v=q -c=Release` and all tests passing are measurable and verifiable.

---

## Issue requiring correction

### CA1711 risks section — naming conflict not mentioned

The risks section lists three options for the `HistogramFactoryDelegate` / CA1711 warning:

> 1. **Rename** and bump the major version
> 2. **Suppress** the warning
> 3. **Obsolete and alias** — add a `[Obsolete]` `HistogramFactory` type alias

Both option 1 (rename) and option 3 (alias pointing to the new name) assume `HistogramFactory` is available as a type name.
It is not.
`HdrHistogram/Histogram.cs` already defines `public abstract class HistogramFactory` — a public factory class with static builder methods.

Presenting options 1 and 3 without this caveat is misleading.
An implementer who reads the options without knowing the codebase may waste time attempting an infeasible rename.

**Required correction:** Add a note under the CA1711 section stating that `HistogramFactory` is already taken by an existing public class, making option 1 and option 3 impossible.
The recommended action (option 2 — suppress) is unchanged; the rationale just needs this additional supporting fact.

**Suggested wording addition (after the options list):**

> Note: renaming to `HistogramFactory` (options 1 and 3) is not possible — that name is already taken by the existing `public abstract class HistogramFactory` in `HdrHistogram/Histogram.cs`.
> Suppression is therefore the only viable path for this issue.

---

## No other changes needed

All remaining sections are accurate and actionable.
Once the CA1711 naming-conflict note is added, the brief is ready to move to `plan/ready/`.
