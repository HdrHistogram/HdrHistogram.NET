# Brief: Issue #72 — Consistent Index Error on Windows

## Issue

**Number:** #72
**Title:** Consistent index error on Windows
**Branch:** `agent/72-consistent-index-error-on-windows`

## Summary

When `RecordValue` is called with a negative value (e.g. `actual - expected` where actual < expected), an `IndexOutOfRangeException` is thrown deep in the bitwise utility code rather than a clear, descriptive error.
The root cause is the absence of input validation in `RecordValue` (and `RecordSingleValue`).
Negative values should be rejected with an `ArgumentOutOfRangeException` before they reach the bucket-index calculation.

### Why This Happens

The call chain is:

1. `RecordValue(long value)` — no validation, passes any value through
2. `RecordSingleValue(long value)` — no validation
3. `GetBucketIndex(long value)` — computes `value | subBucketMask`; when `value < 0` this produces a negative `long`
4. `Bitwise.NumberOfLeadingZeros(long value)` — the imperative path checks `if (value < int.MaxValue)`, which is satisfied by any negative value, so it casts to `int` and calls `Log2`
5. `Bitwise.Imperative.Log2(int i)` — accesses `Lookup[i]`, `Lookup[i >> 8]`, etc.; arithmetic right-shift of a negative `int` preserves the sign bit, yielding indices outside the 256-element `Lookup` array → `IndexOutOfRangeException`

Zero (`0`) is not affected: `0 | subBucketMask = subBucketMask` (positive), so the lookup proceeds normally.

## Affected Files

Confirmed by exploration:

| File | Role |
|------|------|
| `HdrHistogram/HistogramBase.cs` | `RecordValue`, `RecordSingleValue`, `GetBucketIndex` — where validation must be added |
| `HdrHistogram/Utilities/Bitwise.cs` | `Log2`, `NumberOfLeadingZeros` — where the crash manifests; a defensive guard may be added here as belt-and-braces |
| `HdrHistogram.UnitTests/HistogramTestBase.cs` | Shared test base; new negative-value tests belong here |
| `HdrHistogram.UnitTests/LongHistogramTests.cs` | Concrete histogram tests; may need a concrete test for the exact reproducer from the issue |

## Acceptance Criteria

- Calling `RecordValue` with a negative value throws `ArgumentOutOfRangeException` with a message that names the value and states it must be non-negative.
- Calling `RecordValue(0)` succeeds without error (zero is a valid measurement).
- Calling `RecordValue` with a value exceeding `highestTrackableValue` continues to throw `IndexOutOfRangeException` (existing behaviour is preserved).
- The existing test suite continues to pass.
- New tests cover:
  - Recording a negative value → `ArgumentOutOfRangeException`
  - Recording zero → succeeds
  - The exact reproducer from the issue (`LongHistogram` with 15-minute range, recording a negative delta)

## Test Strategy

### Tests to Add

In `HdrHistogram.UnitTests/HistogramTestBase.cs` (shared across all histogram types):

- `RecordValue_WhenValueIsNegative_ThrowsArgumentOutOfRangeException` — asserts `ArgumentOutOfRangeException` is thrown and the message is informative.
- `RecordValue_WhenValueIsZero_Succeeds` — asserts the call completes and the histogram count increases.

In `HdrHistogram.UnitTests/LongHistogramTests.cs`:

- `RecordValue_NegativeDelta_ThrowsArgumentOutOfRangeException` — reproduces the exact scenario from the issue: `LongHistogram((long)TimeSpan.FromMinutes(15).TotalMilliseconds, 3)` then `RecordValue(-1)`.

### Tests to Verify Remain Passing

All existing `RecordValue` tests in `HistogramTestBase.cs` and `LongHistogramTests.cs`.

## Risks and Open Questions

1. **Behaviour parity with Java HdrHistogram** — the Java library also throws on negative values (an `ArrayIndexOutOfBoundsException`).
   The .NET port should throw `ArgumentOutOfRangeException`, which is the idiomatic .NET equivalent.
   This is a deliberate, documented divergence in exception type.

2. **Other recording methods** — `RecordValueWithCount`, `RecordValueWithExpectedInterval`, and `RecordSingleValueWithExpectedInterval` in `HistogramBase.cs` may have the same gap.
   These should be audited and, if they delegate to `RecordSingleValue`, they are protected by the guard added there.
   If they call the bucket-index path directly, they need their own guard.

3. **`Bitwise.Imperative.NumberOfLeadingZeros` defensive fix** — even after validation is added in `RecordSingleValue`, the `Log2` method remains subtly broken for negative inputs.
   A defensive `ArgumentOutOfRangeException` or `Debug.Assert` inside `Log2`/`NumberOfLeadingZeros` is recommended as belt-and-braces, but is not strictly required once the caller validates.

4. **`Bitwise.IntrinsicNumberOfLeadingZeros` (NET5+)** — `BitOperations.LeadingZeroCount(ulong)` casts the input to `ulong`, so a negative `long` would be interpreted as a very large unsigned integer and return `0` rather than throwing.
   This means the crash on NET5+ would be a silent wrong-answer rather than an exception.
   The guard in `RecordSingleValue` also fixes this path.
