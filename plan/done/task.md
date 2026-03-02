# Task Breakdown — Issue #72: Consistent Index Error on Windows

Cross-referenced against every acceptance criterion in `brief.md`.
Ordered by dependency: implementation → XML docs → tests → verification.

---

## Implementation

- [x] **`HdrHistogram/HistogramBase.cs` — add negative-value guard to `GetBucketIndex`**
  - File: `HdrHistogram/HistogramBase.cs`, static overload `GetBucketIndex(long value, long subBucketMask, int bucketIndexOffset)` (~line 684).
  - Change: before the `Bitwise.NumberOfLeadingZeros` call, add `if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, $"Histogram recorded values must be non-negative. Got: {value}");`.
  - Why: single chokepoint for all six call sites (`RecordValue`, `RecordValueWithCount`, `RecordValueWithExpectedInterval`, `SizeOfEquivalentValueRange`, `LowestEquivalentValue`, `GetCountAtValue`); one guard protects all paths.
  - Verify: calling `RecordValue(-1)` throws `ArgumentOutOfRangeException`; calling `RecordValue(0)` succeeds.
  - Covers AC 1, 2, 4, 5, 6.

- [x] **`HdrHistogram/Utilities/Bitwise.cs` — add `Debug.Assert` to `Imperative.Log2`**
  - File: `HdrHistogram/Utilities/Bitwise.cs`, method `Imperative.Log2(int i)` (~line 103).
  - Change: insert `Debug.Assert(i >= 0, "Log2 called with a negative value; caller must validate.");` as the first statement in the method body.
  - Why: belt-and-braces contract documentation for the pre-.NET 5 code path; the guard in `GetBucketIndex` is the primary defence; this assert makes the implicit precondition explicit.
  - Verify: assert is present; no behaviour change in release builds.

---

## XML Documentation

- [x] **`HdrHistogram/HistogramBase.cs` — update XML doc on `RecordValue`**
  - File: `HdrHistogram/HistogramBase.cs`, XML summary block above `RecordValue(long value)` (~line 221).
  - Change: add `<exception cref="System.ArgumentOutOfRangeException">if <paramref name="value"/> is negative.</exception>` alongside the existing `IndexOutOfRangeException` entry.
  - Why: public API now throws a second exception type for negative values; callers consulting IntelliSense or generated docs must be informed.
  - Verify: XML doc block contains both exception elements.
  - Covers AC 1 (documentation completeness).

- [x] **`HdrHistogram/HistogramBase.cs` — update XML doc on `RecordValueWithCount`**
  - File: `HdrHistogram/HistogramBase.cs`, XML summary block above `RecordValueWithCount(long value, long count)` (~line 231).
  - Change: add `<exception cref="System.ArgumentOutOfRangeException">if <paramref name="value"/> is negative.</exception>`.
  - Why: same reasoning as `RecordValue`.
  - Verify: XML doc block contains both exception elements.
  - Covers AC 4 (documentation completeness).

- [x] **`HdrHistogram/HistogramBase.cs` — update XML doc on `RecordValueWithExpectedInterval`**
  - File: `HdrHistogram/HistogramBase.cs`, XML summary block above `RecordValueWithExpectedInterval(long value, long expectedIntervalBetweenValueSamples)` (~line 246).
  - Change: add `<exception cref="System.ArgumentOutOfRangeException">if <paramref name="value"/> is negative.</exception>`.
  - Why: same reasoning as `RecordValue`.
  - Verify: XML doc block contains both exception elements.
  - Covers AC 5 (documentation completeness).

---

## Unit Tests

- [x] **`HdrHistogram.UnitTests/HistogramTestBase.cs` — add `RecordValue_WhenValueIsNegative_ThrowsArgumentOutOfRangeException`**
  - File: `HdrHistogram.UnitTests/HistogramTestBase.cs`.
  - Change: add a `[Fact]` test that calls `Create(...).RecordValue(-1)` inside a lambda, asserts it throws `ArgumentOutOfRangeException`, and verifies `ex.Message` contains both `"non-negative"` and `"-1"` using FluentAssertions `.Should().Contain(...)`.
  - Why: exercises the guard via all three histogram word-size subclasses (Short, Int, Long) through base-class inheritance.
  - Verify: test appears and passes for ShortHistogramTests, IntHistogramTests, LongHistogramTests.
  - Covers AC 1, 2.

- [x] **`HdrHistogram.UnitTests/HistogramTestBase.cs` — add `RecordValue_WhenValueIsZero_Succeeds`**
  - File: `HdrHistogram.UnitTests/HistogramTestBase.cs`.
  - Change: add a `[Fact]` test that calls `Create(...).RecordValue(0)` and asserts `histogram.TotalCount.Should().Be(1)`.
  - Why: zero is a valid measurement (maps to bucket 0, sub-bucket 0); the fix must not regress this.
  - Verify: test passes for all three histogram types.
  - Covers AC 3.

- [x] **`HdrHistogram.UnitTests/HistogramTestBase.cs` — add `RecordValueWithCount_WhenValueIsNegative_ThrowsArgumentOutOfRangeException`**
  - File: `HdrHistogram.UnitTests/HistogramTestBase.cs`.
  - Change: add a `[Fact]` test that calls `RecordValueWithCount(-1, 1)`, asserts `ArgumentOutOfRangeException`, and checks message contains `"non-negative"` and `"-1"`.
  - Why: `RecordValueWithCount` is a separate public entry point that also routes through `GetBucketIndex`; explicit coverage ensures the guard is not bypassed via this path.
  - Verify: test passes for all three histogram types.
  - Covers AC 4.

- [x] **`HdrHistogram.UnitTests/HistogramTestBase.cs` — add `RecordValueWithExpectedInterval_WhenValueIsNegative_ThrowsArgumentOutOfRangeException`**
  - File: `HdrHistogram.UnitTests/HistogramTestBase.cs`.
  - Change: add a `[Fact]` test that calls `RecordValueWithExpectedInterval(-1, 1000)`, asserts `ArgumentOutOfRangeException`, and checks message contains `"non-negative"` and `"-1"`.
  - Why: `RecordValueWithExpectedInterval` is the third public recording entry point; it routes through `RecordValueWithCountAndExpectedInterval` → `GetBucketIndex`.
  - Verify: test passes for all three histogram types.
  - Covers AC 5.

- [x] **`HdrHistogram.UnitTests/LongHistogramTests.cs` — add `RecordValue_NegativeDelta_ThrowsArgumentOutOfRangeException`**
  - File: `HdrHistogram.UnitTests/LongHistogramTests.cs`.
  - Change: add a `[Fact]` test that constructs the exact histogram from the issue report — `HistogramFactory.With64BitBucketSize().WithValuesUpTo((long)TimeSpan.FromMinutes(15).TotalMilliseconds).WithPrecisionOf(3).Create()` — then calls `RecordValue(-1)`, asserting `ArgumentOutOfRangeException` with message containing `"non-negative"` and `"-1"`.
  - Why: concrete reproducer pinning the exact scenario described in the issue, ensuring the fix holds for the 64-bit, 15-minute-range histogram.
  - Verify: test passes.
  - Covers AC 6.

---

## Verification

- [x] **Run full test suite to confirm no regressions**
  - Command: `dotnet test` from the repository root.
  - Why: AC 7 requires all existing tests to continue to pass.
  - Verify: all tests pass; no new failures in ShortHistogramTests, IntHistogramTests, LongHistogramTests, or any other test class.
  - Covers AC 7.

---

## Regression Fixes (identified by code review)

- [x] **`HdrHistogram/HistogramEncoding.cs` — restore `InvalidOperationException` in `GetBestTypeForWordSize`**
  - File: `HdrHistogram/HistogramEncoding.cs`, `default` case of `GetBestTypeForWordSize` switch (~line 242).
  - Change: revert `throw new IndexOutOfRangeException();` back to `throw new InvalidOperationException($"Unexpected word size: {wordSizeInBytes}");`.
  - Why: a previous agent introduced the wrong exception type — `IndexOutOfRangeException` with no message is the exact opposite of the fix being applied in this PR; `InvalidOperationException` with an informative message is the correct exception for an unexpected enum/switch value.

- [x] **`HdrHistogram/HistogramLogReader.cs` — restore `CultureInfo.InvariantCulture` in `ParseDouble`**
  - File: `HdrHistogram/HistogramLogReader.cs`, `ParseDouble` method (~line 291).
  - Change: revert `double.Parse(value)` back to `double.Parse(value, CultureInfo.InvariantCulture)` and restore the `using System.Globalization;` directive at the top of the file.
  - Why: histogram log files use `.` as the decimal separator (machine-generated invariant format); without `CultureInfo.InvariantCulture`, `double.Parse` will fail or produce wrong results on systems whose current culture uses `,` as the decimal separator (e.g. most European locales).

- [x] **`HdrHistogram/HistogramLogReader.cs` — restore `StringComparison.Ordinal` in string comparisons**
  - File: `HdrHistogram/HistogramLogReader.cs`, methods `IsComment`, `IsStartTime`, `IsBaseTime`, `IsLegend`, `IsV1Legend` (~lines 242-270).
  - Change: revert `line.StartsWith("#")` to `line.StartsWith("#", StringComparison.Ordinal)`, restore `StringComparison.Ordinal` arguments to all `StartsWith` and `string.Equals` calls, and restore the `#if NETSTANDARD2_0` conditional for `IsComment`.
  - Why: log format strings are machine-generated fixed-format; ordinal comparison is correct and avoids culture-sensitive behaviour that could silently fail on non-English systems.

---

## Acceptance Criteria Coverage Matrix

| AC | Description | Covered by |
|----|-------------|-----------|
| 1 | `RecordValue(value < 0)` throws `ArgumentOutOfRangeException` | Guard task; `RecordValue_WhenValueIsNegative` test; XML doc task |
| 2 | Exception message contains `"non-negative"` and the offending value | Guard task (message format); `_WhenValueIsNegative` tests |
| 3 | `RecordValue(0)` succeeds, `TotalCount == 1` | Guard task (zero not rejected); `RecordValue_WhenValueIsZero_Succeeds` test |
| 4 | `RecordValueWithCount(value < 0, ...)` throws `ArgumentOutOfRangeException` | Guard task; `RecordValueWithCount_WhenValueIsNegative` test; XML doc task |
| 5 | `RecordValueWithExpectedInterval(value < 0, ...)` throws `ArgumentOutOfRangeException` | Guard task; `RecordValueWithExpectedInterval_WhenValueIsNegative` test; XML doc task |
| 6 | Exact issue scenario: 15-minute `LongHistogram`, `RecordValue(-1)` throws | `RecordValue_NegativeDelta_ThrowsArgumentOutOfRangeException` in `LongHistogramTests` |
| 7 | All existing tests continue to pass | Run full test suite task |
