# Brief Review: Issue #72 — Consistent Index Error on Windows

## Verdict: Needs Revision

The brief is technically solid in its root cause analysis and fix strategy, but two issues need correcting before it is ready.

---

## Issues Found

### 1. Non-existent method cited in Risks section (factual error)

**Location:** Risks and Open Questions, item 2.

The brief states:

> `RecordValueWithCount`, `RecordValueWithExpectedInterval`, and `RecordSingleValueWithExpectedInterval` share the same vulnerability.

`RecordSingleValueWithExpectedInterval` does **not exist** in the codebase.
A search of `HdrHistogram/` returns no matches for this name.
The actual call chain is:

```
RecordValueWithExpectedInterval → RecordValueWithCountAndExpectedInterval → RecordValueWithCount → GetBucketIndex
```

**Action:** Remove `RecordSingleValueWithExpectedInterval` from the risks section.
If the intent was `RecordValueWithCountAndExpectedInterval`, note that this is an internal helper and is already covered by the chain above.

---

### 2. Four additional public callers of `GetBucketIndex` are not acknowledged

**Location:** Affected Files table and Acceptance Criteria.

The fix puts a validation guard inside `GetBucketIndex` itself.
The brief correctly notes this protects all recording callers in one place.
However, four other public (and one private) methods also call `GetBucketIndex` and will be silently affected by the fix:

| Method | File | Line |
|--------|------|------|
| `SizeOfEquivalentValueRange(long value)` | `HistogramBase.cs` | 315 |
| `LowestEquivalentValue(long value)` | `HistogramBase.cs` | 335 |
| `GetCountAtValue(long value)` | `HistogramBase.cs` | 398 |
| `GetRelevantCounts()` (private) | `HistogramBase.cs` | 724 |

After the fix these public methods will throw `ArgumentOutOfRangeException` for negative inputs, which is a behaviour change not covered by any acceptance criterion or test.

**Action:** The brief must explicitly address this.
Two acceptable options:

- **Option A — Acknowledge and test**: Add acceptance criteria and test cases for `SizeOfEquivalentValueRange`, `LowestEquivalentValue`, and `GetCountAtValue` with negative inputs.
  The private `GetRelevantCounts` receives the result of `GetMaxValue()`, which is always non-negative, so it requires no test.

- **Option B — Acknowledge and document as intentional**: State that these methods should also reject negative inputs (histogram values are always non-negative), that the behaviour change is intentional, and that no additional tests are required because the input domain is the same.

Either option is valid.
The brief must make the choice explicit so the implementer knows what to do.

---

## What the Brief Gets Right

The following has been verified against the codebase and is accurate:

- Root cause analysis (call chain, bitwise crash path, NET5+ silent wrong-answer path) — all correct.
- Fix strategy (validate in `GetBucketIndex`) — sound; protects all callers in one place.
- Call chains for `RecordValue`, `RecordValueWithCount`, and `RecordValueWithExpectedInterval` — all verified.
- File paths for all four affected files — all confirmed to exist.
- Bitwise details: `Lookup` array has 256 elements; `if (value < int.MaxValue)` is the exact condition at line 77; arithmetic right-shift of a negative `int` causes the out-of-bounds access.
- Test file structure: `HistogramTestBase.cs` is an abstract base class using xUnit and FluentAssertions; `LongHistogramTests.cs` extends it.
- Test names follow the codebase's existing `Method_Condition_ExpectedBehaviour` convention.
- Acceptance criteria are measurable (specific exception type, specific message substrings, zero-value success case).
- Scope is appropriate for a single PR.
