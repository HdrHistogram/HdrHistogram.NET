# Brief Review: Issue #72 — Consistent Index Error on Windows

## Verdict: Needs Revision

The brief is clear and well-reasoned, but contains one critical factual error that would cause the implementation to ship with incomplete protection.
There are also minor gaps in acceptance criteria and test coverage.
The brief is otherwise well-scoped and feasible.

---

## Critical Issue: RecordValueWithCount Bypasses RecordSingleValue

**Location in brief:** Risks and Open Questions, item 2.

**The problem:**
The brief states that `RecordValueWithCount`, `RecordValueWithExpectedInterval`, and `RecordSingleValueWithExpectedInterval` "are protected by the guard added there [in RecordSingleValue] if they delegate to RecordSingleValue."

This assumption is **incorrect**.
Codebase exploration confirms:

- `RecordValueWithCount` (line 237) calls `GetBucketIndex()` directly — it does **not** call `RecordSingleValue`.
- `RecordValueWithExpectedInterval` delegates to `RecordValueWithCountAndExpectedInterval`, which calls `RecordValueWithCount`, which calls `GetBucketIndex()` directly.

The call chains are:

```
RecordValue → RecordSingleValue → GetBucketIndex            ← protected by a guard in RecordSingleValue
RecordValueWithCount → GetBucketIndex                       ← NOT protected
RecordValueWithExpectedInterval → RecordValueWithCountAndExpectedInterval → RecordValueWithCount → GetBucketIndex  ← NOT protected
```

**Required action:**
Choose one of the following fix strategies and state it explicitly in the brief:

- **Option A (recommended):** Add the guard in `GetBucketIndex` itself, which protects all callers in one place.
- **Option B:** Add the guard at each public entry point: `RecordValue`, `RecordValueWithCount`, and `RecordValueWithExpectedInterval`.

---

## Acceptance Criteria: Incomplete Coverage

**Affected acceptance criteria items:**

The three acceptance criteria lines refer only to `RecordValue`.
They must be extended to cover `RecordValueWithCount` and `RecordValueWithExpectedInterval`:

- Calling `RecordValueWithCount` with a negative value throws `ArgumentOutOfRangeException`.
- Calling `RecordValueWithExpectedInterval` with a negative value throws `ArgumentOutOfRangeException`.

---

## Test Strategy: Missing Tests for Sibling Methods

**Location in brief:** Tests to Add section.

The two shared tests in `HistogramTestBase` only exercise `RecordValue`.
Add the following shared tests:

- `RecordValueWithCount_WhenValueIsNegative_ThrowsArgumentOutOfRangeException`
- `RecordValueWithExpectedInterval_WhenValueIsNegative_ThrowsArgumentOutOfRangeException`

---

## Minor: Exception Message Verifiability

**Location in brief:** Acceptance criteria, first bullet.

The criterion "the message names the value and states it must be non-negative" is good intent but needs to be tied to a specific assertion pattern so the implementer and reviewer know what "informative" means.

**Suggested wording:**
> The exception message contains the string `"non-negative"` and the string representation of the value (e.g. `"-1"`).

---

## Feasibility Confirmation

The following was verified against the actual code:

| Claim in brief | Status |
|---|---|
| `RecordValue` delegates to `RecordSingleValue` | Confirmed (lines 226-229) |
| `RecordSingleValue` has no validation | Confirmed (lines 621-628) |
| `GetBucketIndex` uses `value \| subBucketMask` before calling `NumberOfLeadingZeros` | Confirmed (line 685) |
| `Imperative.Log2` uses a 256-element `Lookup` array | Confirmed (lines 57-66, 102-108) |
| `value < int.MaxValue` is always true for negative values → cast to `int` | Confirmed (line 73) |
| NET5+ casts `long` to `ulong`, silent wrong-answer for negatives | Confirmed (lines 38-43) |
| `RecordValueWithCount` calls `GetBucketIndex` directly, not via `RecordSingleValue` | Confirmed (lines 237-244) |

All four files mentioned in the Affected Files table exist at the paths stated.

---

## Scope Assessment

Once the critical gap is resolved, the scope is appropriate for a single PR.
The fix is small: one or three guard clauses plus tests.

---

## Summary of Required Changes to Brief

1. **Fix the factual error in Risk #2:** `RecordValueWithCount` does not call `RecordSingleValue`; state which fix strategy will be used (guard in `GetBucketIndex` or guards at each public entry point).
2. **Extend acceptance criteria** to cover `RecordValueWithCount` and `RecordValueWithExpectedInterval`.
3. **Extend test strategy** to include negative-value tests for `RecordValueWithCount` and `RecordValueWithExpectedInterval` in `HistogramTestBase.cs`.
4. *(Optional)* Tighten the exception message criterion to a verifiable string check.
