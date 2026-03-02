# Issue #79: GetPercentileOfValue

## Summary

The issue requests a method to answer: "Given a measurement value, what percentile does it fall under?"
This is the **inverse of `GetValueAtPercentile`**.
The API reference spec already names it `GetPercentileAtOrBelowValue(long value)` — that name is more precise and matches the Java HdrHistogram library's `percentileAtOrBelowValue`.
The method must be implemented in `HistogramBase` and exposed as a public instance method.

## What Needs to Change and Why

`HistogramBase` currently has `GetValueAtPercentile(double percentile) → long` but no inverse.
Users recording response times need to ask: "300ms is at what percentile?" without iterating the distribution themselves.
The spec (`spec/tech-standards/api-reference.md` line 132) already lists `GetPercentileAtOrBelowValue` as part of the public API surface, so this is a missing implementation.

## Affected Files (confirmed by exploration)

- `HdrHistogram/HistogramBase.cs` — add `GetPercentileAtOrBelowValue(long value)` method (near `GetValueAtPercentile`, around line 388)
- `HdrHistogram.UnitTests/HistogramTestBase.cs` — add shared tests covering all histogram types
- `spec/tech-standards/api-reference.md` — no change needed; method is already listed

## Acceptance Criteria

1. A public method `double GetPercentileAtOrBelowValue(long value)` exists on `HistogramBase`.
2. Returns a value in the range `[0.0, 100.0]`.
3. For a recorded value `v`, `GetPercentileAtOrBelowValue(v)` returns the percentage of all recorded values that are `<= v` (using the histogram's equivalent-value range semantics).
4. Returns `100.0` when `value >= HighestTrackableValue` or when `value` is beyond the tracked range.
5. Returns `0.0` when `TotalCount == 0` (empty histogram).
6. Consistent with `GetValueAtPercentile`: `GetValueAtPercentile(GetPercentileAtOrBelowValue(v))` should equal the highest equivalent value of the bucket containing `v` (within histogram resolution).
7. Monotonic: for `v1 < v2`, `GetPercentileAtOrBelowValue(v1) <= GetPercentileAtOrBelowValue(v2)`.

## Algorithm

Mirrors the bucket-traversal loop in `GetValueAtPercentile` but in the forward direction:

1. Compute `targetBucketIndex = GetBucketIndex(value)` and `targetSubBucketIndex = GetSubBucketIndex(value, targetBucketIndex)`.
2. If `targetBucketIndex >= BucketCount`, return `100.0`.
3. Accumulate counts for all sub-buckets up to and including `(targetBucketIndex, targetSubBucketIndex)`.
4. Return `(100.0 * countAtValue) / TotalCount`.
5. Guard: if `TotalCount == 0`, return `0.0`.

## Test Strategy

Add tests to `HistogramTestBase` (runs across all histogram types via the abstract factory):

- **Basic percentile**: record a known set of values; assert `GetPercentileAtOrBelowValue` returns the expected percentage.
- **Round-trip consistency**: for a set of recorded values, assert `GetValueAtPercentile(GetPercentileAtOrBelowValue(v))` equals `HighestEquivalentValue(LowestEquivalentValue(v))`.
- **Empty histogram**: `TotalCount == 0` returns `0.0`.
- **At maximum**: value at or above `HighestTrackableValue` returns `100.0`.
- **Monotonicity**: verify that queried percentiles are non-decreasing for increasing input values.
- **Single value**: histogram with one recorded value; any query `>= that value` returns `100.0`, any query `< that value` returns `0.0`.
- **Boundary**: value exactly at `LowestTrackableValue` returns `(count_at_lowest / TotalCount) * 100`.

Tests should use `FluentAssertions` with appropriate tolerance for floating-point comparisons (e.g., `BeApproximately(..., precision: 0.1)`).

## Risks and Open Questions

- **Naming**: The issue title says `GetPercentileOfValue`; the spec says `GetPercentileAtOrBelowValue`.
  Use `GetPercentileAtOrBelowValue` to match the spec and Java library.
- **Values outside tracked range**: The method should not throw; return `100.0` for values above `HighestTrackableValue` (consistent with Java implementation).
- **Equivalent-value semantics**: The query value is mapped to a bucket via `GetBucketIndex`/`GetSubBucketIndex`, matching the same resolution logic used throughout `HistogramBase`.
  All values within the same equivalent range map to the same percentile.
- **Thread safety**: The method only reads counts; concurrent histograms (`LongConcurrentHistogram`, `IntConcurrentHistogram`) inherit the same implementation and their atomic reads ensure a consistent (if momentarily stale) snapshot.
