# Task List: Issue #72 — Consistent Index Error on Windows

## Implementation

- [x] **HistogramBase.cs — Add guard in `GetBucketIndex` (static overload, line 684)**
  - File: `HdrHistogram/HistogramBase.cs`
  - Change: At the start of the static `GetBucketIndex(long value, long subBucketMask, int bucketIndexOffset)` method, add:
    ```csharp
    if (value < 0)
        throw new ArgumentOutOfRangeException(nameof(value), value,
            $"Histogram recorded values must be non-negative. Got: {value}");
    ```
  - Why: This is the single choke-point for all callers — `RecordSingleValue`, `RecordValueWithCount`, and every query method that calls `GetBucketIndex`. One guard protects all paths.
  - Verify: The three `RecordValue*` methods and the three query methods (`SizeOfEquivalentValueRange`, `LowestEquivalentValue`, `GetCountAtValue`) all reach this method before touching `Bitwise`; the new guard will be hit first.

- [x] **Bitwise.cs — Add defensive `Debug.Assert` in `Imperative.Log2` (line ~102)**
  - File: `HdrHistogram/Utilities/Bitwise.cs`
  - Change: At the top of `Imperative.Log2(int i)`, add:
    ```csharp
    Debug.Assert(i >= 0, "Log2 called with a negative value; caller must validate.");
    ```
  - Why: Belt-and-braces protection. The array `Lookup` has 256 elements (indices 0–255); a negative `int` argument causes arithmetic right-shifts to yield out-of-bounds indices. The assert makes the contract explicit without changing production behaviour on NET5+ (where the `IntrinsicNumberOfLeadingZeros` path is taken instead).
  - Verify: The assert is present and the method signature/behaviour is otherwise unchanged.

## Tests — Shared Base (covers all histogram types)

- [x] **HistogramTestBase.cs — Add `RecordValue_WhenValueIsNegative_ThrowsArgumentOutOfRangeException`**
  - File: `HdrHistogram.UnitTests/HistogramTestBase.cs`
  - Change: Add a `[Fact]` test that calls `RecordValue(-1)` on the default histogram and asserts an `ArgumentOutOfRangeException` is thrown whose message contains both `"non-negative"` and `"-1"`.
  - Why: Covers acceptance criterion — `RecordValue` with a negative value must throw `ArgumentOutOfRangeException` with a message containing `"non-negative"` and the value string.
  - Verify: Test appears in the test runner output for `ShortHistogramTests`, `IntHistogramTests`, and `LongHistogramTests` (all inherit from the base).

- [x] **HistogramTestBase.cs — Add `RecordValue_WhenValueIsZero_Succeeds`**
  - File: `HdrHistogram.UnitTests/HistogramTestBase.cs`
  - Change: Add a `[Fact]` test that calls `RecordValue(0)` and asserts no exception is thrown and `TotalCount` equals `1`.
  - Why: Covers acceptance criterion — zero is a valid measurement and must not be rejected by the guard.
  - Verify: Test passes; `TotalCount` is `1` after a single `RecordValue(0)` call.

- [x] **HistogramTestBase.cs — Add `RecordValueWithCount_WhenValueIsNegative_ThrowsArgumentOutOfRangeException`**
  - File: `HdrHistogram.UnitTests/HistogramTestBase.cs`
  - Change: Add a `[Fact]` test that calls `RecordValueWithCount(-1, 1)` and asserts an `ArgumentOutOfRangeException` is thrown whose message contains `"non-negative"` and `"-1"`.
  - Why: Covers acceptance criterion — `RecordValueWithCount` calls `GetBucketIndex` directly (not via `RecordSingleValue`); the guard must fire on this path too.
  - Verify: Test appears for all concrete histogram types and the exception message matches.

- [x] **HistogramTestBase.cs — Add `RecordValueWithExpectedInterval_WhenValueIsNegative_ThrowsArgumentOutOfRangeException`**
  - File: `HdrHistogram.UnitTests/HistogramTestBase.cs`
  - Change: Add a `[Fact]` test that calls `RecordValueWithExpectedInterval(-1, 1000)` and asserts an `ArgumentOutOfRangeException` is thrown whose message contains `"non-negative"` and `"-1"`.
  - Why: Covers acceptance criterion — `RecordValueWithExpectedInterval` delegates through `RecordValueWithCountAndExpectedInterval` → `RecordValueWithCount` → `GetBucketIndex`; the guard must fire on this path.
  - Verify: Test appears for all concrete histogram types and the exception message matches.

## Tests — Concrete Reproducer (LongHistogram-specific)

- [x] **LongHistogramTests.cs — Add `RecordValue_NegativeDelta_ThrowsArgumentOutOfRangeException`**
  - File: `HdrHistogram.UnitTests/LongHistogramTests.cs`
  - Change: Add a `[Fact]` test that:
    1. Creates a `LongHistogram` via `HistogramFactory.With64BitBucketSize().WithValuesUpTo((long)TimeSpan.FromMinutes(15).TotalMilliseconds).WithPrecisionOf(3).Create()`.
    2. Calls `histogram.RecordValue(-1)`.
    3. Asserts `ArgumentOutOfRangeException` is thrown and the message contains `"non-negative"` and `"-1"`.
  - Why: Reproduces the exact scenario from GitHub issue #72 and provides a regression guard specific to `LongHistogram`.
  - Verify: Test is named correctly, runs independently, and fails before the fix is applied (confirming it is a genuine regression test).

## Verification: Acceptance Criteria Cross-Reference

| Acceptance Criterion | Covered By |
|---|---|
| `RecordValue(negative)` → `ArgumentOutOfRangeException` with `"non-negative"` and value | `GetBucketIndex` guard + `RecordValue_WhenValueIsNegative_ThrowsArgumentOutOfRangeException` |
| `RecordValueWithCount(negative)` → `ArgumentOutOfRangeException` with `"non-negative"` and value | `GetBucketIndex` guard + `RecordValueWithCount_WhenValueIsNegative_ThrowsArgumentOutOfRangeException` |
| `RecordValueWithExpectedInterval(negative)` → `ArgumentOutOfRangeException` with `"non-negative"` and value | `GetBucketIndex` guard + `RecordValueWithExpectedInterval_WhenValueIsNegative_ThrowsArgumentOutOfRangeException` |
| `RecordValue(0)` succeeds | Guard condition is `< 0` (excludes zero) + `RecordValue_WhenValueIsZero_Succeeds` |
| `RecordValue(value > highestTrackableValue)` → `IndexOutOfRangeException` preserved | Guard only throws for negatives; existing overflow path is unmodified. Verified by existing `RecordValue_Overflow_ShouldThrowException`. |
| Existing test suite continues to pass | No existing behaviour changed; guard is additive only |
| New test: `RecordValue(negative)` → exception | `RecordValue_WhenValueIsNegative_ThrowsArgumentOutOfRangeException` in `HistogramTestBase.cs` |
| New test: `RecordValueWithCount(negative)` → exception | `RecordValueWithCount_WhenValueIsNegative_ThrowsArgumentOutOfRangeException` in `HistogramTestBase.cs` |
| New test: `RecordValueWithExpectedInterval(negative)` → exception | `RecordValueWithExpectedInterval_WhenValueIsNegative_ThrowsArgumentOutOfRangeException` in `HistogramTestBase.cs` |
| New test: zero → succeeds | `RecordValue_WhenValueIsZero_Succeeds` in `HistogramTestBase.cs` |
| New test: exact issue reproducer (`LongHistogram`, 15-minute range, negative delta) | `RecordValue_NegativeDelta_ThrowsArgumentOutOfRangeException` in `LongHistogramTests.cs` |
