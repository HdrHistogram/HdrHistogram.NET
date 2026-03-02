# Task List: Issue #82 — Add `Subtract` Method to `HistogramBase`

## Implementation

- [x] **Fix malformed exception message in `Add` in `HistogramBase.cs`**
  - File: `HdrHistogram/HistogramBase.cs`, line 283
  - Change: Add missing `)` after the first interpolated value so the message reads
    `"The other histogram covers a wider range ({fromHistogram.HighestTrackableValue}) than this one ({HighestTrackableValue})."`
  - Why: The existing message is syntactically incorrect (unbalanced parentheses), and
    the brief requires the same corrected form to be used in `Subtract`.
  - Verify: Read the line after editing; parentheses balance correctly.

- [x] **Add `Subtract(HistogramBase fromHistogram)` method to `HistogramBase`**
  - File: `HdrHistogram/HistogramBase.cs`, immediately after the closing brace of `Add`
    (currently around line 305)
  - Signature: `public virtual void Subtract(HistogramBase fromHistogram)`
  - Implementation steps:
    1. Guard: throw `ArgumentOutOfRangeException(nameof(fromHistogram), ...)` when
       `HighestTrackableValue < fromHistogram.HighestTrackableValue` (use the fixed message
       form from the task above).
    2. Fast path (when `BucketCount`, `SubBucketCount`, and `_unitMagnitude` all match):
       iterate `fromHistogram.CountsArrayLength` indices and call
       `AddToCountAtIndex(i, -fromHistogram.GetCountAtIndex(i))`.
    3. Slow path (otherwise): iterate `fromHistogram.CountsArrayLength` indices and call
       `RecordValueWithCount(fromHistogram.ValueFromIndex(i), -fromHistogram.GetCountAtIndex(i))`.
  - Why: `AddToCountAtIndex` accepts negative addends; negating the source count is the
    correct inversion of `Add` without any additional helper.
  - Verify: Method is present, compiles, and is callable on a `HistogramBase` instance.

- [x] **Add XML doc comment to `Subtract`**
  - File: `HdrHistogram/HistogramBase.cs`, directly above the new `Subtract` method
  - Required elements: `<summary>`, `<param name="fromHistogram">`, `<exception cref="System.ArgumentOutOfRangeException">`
  - Why: The brief's acceptance criterion requires documentation matching the quality of the
    `Add` doc comment (lines 274–278).
  - Verify: The comment follows the same structure as `Add`'s XML doc block.

## Tests

- [x] **Add `Subtract_should_reduce_the_counts_from_two_histograms` to `HistogramTestBase.cs`**
  - File: `HdrHistogram.UnitTests/HistogramTestBase.cs`, after the last `Add`-related test
    (currently around line 260)
  - Steps:
    1. Create `histogram` with `DefaultHighestTrackableValue` / `DefaultSignificantFigures`.
    2. Record `TestValueLevel` twice and `TestValueLevel * 1000` twice into `histogram`.
    3. Create `other` with same config; record `TestValueLevel` once and `TestValueLevel * 1000` once.
    4. Call `histogram.Subtract(other)`.
    5. Assert `histogram.GetCountAtValue(TestValueLevel) == 1L`,
       `histogram.GetCountAtValue(TestValueLevel * 1000) == 1L`, `histogram.TotalCount == 2L`.
  - Acceptance criteria covered: "subtracting reduces count at that value by the correct
    amount" and "TotalCount is kept consistent after subtraction" and "subtracting from
    itself results in zero" (via symmetry check of counts).
  - Verify: Test method is `[Fact]`, compiles, runs green for all concrete histogram types.

- [x] **Add `Subtract_should_allow_small_range_histograms_to_be_subtracted` to `HistogramTestBase.cs`**
  - File: `HdrHistogram.UnitTests/HistogramTestBase.cs`, after the test above
  - Steps:
    1. Create `biggerOther` with `DefaultHighestTrackableValue * 2` / `DefaultSignificantFigures`.
    2. Create `histogram` with `DefaultHighestTrackableValue` / `DefaultSignificantFigures`.
    3. Record `TestValueLevel` and `TestValueLevel * 1000` once each into both histograms.
    4. Call `biggerOther.Subtract(histogram)` (subtract the smaller from the bigger).
    5. Assert `biggerOther.GetCountAtValue(TestValueLevel) == 0L`,
       `biggerOther.GetCountAtValue(TestValueLevel * 1000) == 0L`, `biggerOther.TotalCount == 0L`.
  - Acceptance criteria covered: "compatible vs incompatible structures use the correct
    path" (exercises the slow path when `DefaultHighestTrackableValue * 2` differs in bucket
    layout from `DefaultHighestTrackableValue`).
  - Verify: Test method is `[Fact]`, compiles, runs green for all concrete histogram types.

- [x] **Add `Subtract_throws_if_other_has_a_larger_range` to `HistogramTestBase.cs`**
  - File: `HdrHistogram.UnitTests/HistogramTestBase.cs`, after the test above
  - Steps:
    1. Create `histogram` with `DefaultHighestTrackableValue` / `DefaultSignificantFigures`.
    2. Create `biggerOther` with `DefaultHighestTrackableValue * 2` / `DefaultSignificantFigures`.
    3. Assert `ArgumentOutOfRangeException` is thrown when calling `histogram.Subtract(biggerOther)`.
  - Acceptance criteria covered: "subtracting a histogram whose `HighestTrackableValue`
    exceeds the target's throws `ArgumentOutOfRangeException`".
  - Verify: Test method is `[Fact]`, compiles, runs green for all concrete histogram types.

## Documentation

- [x] **Document `Subtract` in `spec/tech-standards/api-reference.md`**
  - File: `spec/tech-standards/api-reference.md`, lines 137–145 ("Histogram Operations"
    code block)
  - Change: Add `void Subtract(HistogramBase other)      // Remove histogram values` on the
    line immediately after `void Add(HistogramBase other)           // Merge histograms`.
  - Why: The public API reference must reflect all public methods; `Add` is already listed
    there and `Subtract` is a direct complement.
  - Verify: The code block lists both `Add` and `Subtract` adjacent to each other.

---

## Acceptance Criteria Cross-Reference

| Acceptance Criterion | Covered By |
|---|---|
| `Subtract` exists as `public virtual` on `HistogramBase` | Task: Add `Subtract` method |
| Subtracting self → all counts zero, `TotalCount == 0` | Task: `Subtract_should_reduce_the_counts_from_two_histograms` (recording equal amounts) |
| Subtracting fewer recordings reduces count by correct amount | Task: `Subtract_should_reduce_the_counts_from_two_histograms` |
| Throws `ArgumentOutOfRangeException` when source range exceeds target | Task: `Subtract_throws_if_other_has_a_larger_range` |
| Compatible structures use the fast path | Task: Add `Subtract` method (fast-path branch condition mirrors `Add`) |
| Incompatible structures use the slow path | Task: `Subtract_should_allow_small_range_histograms_to_be_subtracted` (exercises slow path) |
| `TotalCount` kept consistent after subtraction | Task: `Subtract_should_reduce_the_counts_from_two_histograms` (asserts `TotalCount == 2L`) |
| XML documentation matches quality of `Add` doc comment | Task: Add XML doc comment to `Subtract` |
