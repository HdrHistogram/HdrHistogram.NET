# Issue #72: Consistent Index Error on Windows

## Summary

When a caller passes a negative value to `RecordValue` (e.g. `actual - expected` where the delta is negative), the library crashes with an `IndexOutOfRangeException` deep inside the bitwise utilities rather than surfacing a clear, actionable error.

The root cause is that `HistogramBase.GetBucketIndex` passes its `value` argument directly to `Bitwise.NumberOfLeadingZeros` without first checking whether it is negative.
`NumberOfLeadingZeros` in turn calls `Bitwise.Imperative.Log2(int i)`, which indexes into a 256-element lookup table using bit-shifted values of `i`.
For negative `i`, the C# right-shift of a signed integer preserves the sign bit, producing a negative (and therefore invalid) array index, which throws `IndexOutOfRangeException`.

HdrHistogram is defined over non-negative integers only.
Zero is a valid measurement.
The fix is to add a guard in `GetBucketIndex` (the single chokepoint for all recording and query paths) that throws `ArgumentOutOfRangeException` with an informative message when `value < 0`.

## Affected Files

Confirmed by codebase exploration:

- `HdrHistogram/HistogramBase.cs` — `GetBucketIndex(long value, long subBucketMask, int bucketIndexOffset)` (line 684): add negative-value guard.
- `HdrHistogram/Utilities/Bitwise.cs` — `Imperative.Log2(int i)` (line 103): add `Debug.Assert(i >= 0, …)` documenting the contract.
- `HdrHistogram.UnitTests/HistogramTestBase.cs` — add three parameterised tests covering all histogram types.
- `HdrHistogram.UnitTests/LongHistogramTests.cs` — add one concrete reproducer test matching the exact scenario from the issue.

## Acceptance Criteria

1. `RecordValue(value)` where `value < 0` throws `ArgumentOutOfRangeException` (not `IndexOutOfRangeException`).
2. The exception message contains both the word `"non-negative"` and the offending value.
3. `RecordValue(0)` succeeds and increments `TotalCount` by 1.
4. `RecordValueWithCount(value, count)` where `value < 0` also throws `ArgumentOutOfRangeException` with the same message contract.
5. `RecordValueWithExpectedInterval(value, interval)` where `value < 0` also throws `ArgumentOutOfRangeException` with the same message contract.
6. The exact scenario from the issue report — `new LongHistogram((long)TimeSpan.FromMinutes(15).TotalMilliseconds, 3)` followed by `RecordValue(-1)` — throws `ArgumentOutOfRangeException`.
7. All existing tests continue to pass.

## Test Strategy

### New tests in `HistogramTestBase` (covers Short, Int, Long histogram types via inheritance)

| Test name | What it verifies |
|---|---|
| `RecordValue_WhenValueIsNegative_ThrowsArgumentOutOfRangeException` | `RecordValue(-1)` throws, message contains `"non-negative"` and `"-1"` |
| `RecordValue_WhenValueIsZero_Succeeds` | `RecordValue(0)` succeeds, `TotalCount == 1` |
| `RecordValueWithCount_WhenValueIsNegative_ThrowsArgumentOutOfRangeException` | `RecordValueWithCount(-1, 1)` throws with same message contract |
| `RecordValueWithExpectedInterval_WhenValueIsNegative_ThrowsArgumentOutOfRangeException` | `RecordValueWithExpectedInterval(-1, 1000)` throws with same message contract |

### New test in `LongHistogramTests` (concrete issue reproducer)

| Test name | What it verifies |
|---|---|
| `RecordValue_NegativeDelta_ThrowsArgumentOutOfRangeException` | Exact issue scenario: 15-minute range histogram, `RecordValue(-1)` |

### Tests to verify are unaffected

Run the full test suite (`dotnet test`) to confirm no regressions.

## Risks and Open Questions

- **Zero as a valid value**: The original issue suggests the caller was unsure whether zero is valid.
  Zero is accepted — it maps to bucket 0, sub-bucket 0 — and the fix does not reject it.
  This is consistent with the Java reference implementation.

- **Query methods with negative values**: `GetBucketIndex` is also called by `SizeOfEquivalentValueRange`, `LowestEquivalentValue`, and `GetCountAtValue`.
  These are query methods that should equally reject negative values.
  Placing the guard in the static `GetBucketIndex` overload protects all six call sites with a single change.

- **`Bitwise.Imperative.Log2` negative-int path**: On the legacy code path (`#else` branch, i.e. pre-.NET 5), `NumberOfLeadingZeros` casts `value` to `int` before calling `Log2`.
  A negative `long` that is also within `[int.MinValue, 0)` produces a negative `int`.
  The guard in `GetBucketIndex` prevents this from ever reaching `Log2`, making the `Debug.Assert` in `Log2` a belt-and-braces contract document rather than a primary defence.

- **Windows-specific framing**: The issue title mentions Windows, but the bug is platform-independent.
  It may have been observed first on Windows because of specific workload patterns producing negative deltas, not because of OS-level differences.
