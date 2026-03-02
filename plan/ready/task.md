# Task Checklist: Issue #79 — `GetPercentileAtOrBelowValue`

Cross-reference: every acceptance criterion in `brief.md` is covered by at least one task below (see criterion labels in parentheses).

---

## Implementation

- [ ] **`HdrHistogram/HistogramBase.cs`** — Add `GetPercentileAtOrBelowValue(long value)` as a public instance method returning `double`, placed directly after `GetValueAtPercentile` (around line 388).
  - Algorithm (mirrors the bucket-traversal loop of `GetValueAtPercentile` in the forward direction):
    1. Guard: if `TotalCount == 0`, return `0.0`. *(criterion 5)*
    2. Compute `bucketIndex = GetBucketIndex(value)` and `subBucketIndex = GetSubBucketIndex(value, bucketIndex)`.
    3. If `bucketIndex >= BucketCount`, return `100.0`. *(criterion 4)*
    4. Traverse from bucket 0 / sub-bucket 0 through `(bucketIndex, subBucketIndex)` inclusive, accumulating counts via `GetCountAt(i, j)`. Use the same loop structure as `GetValueAtPercentile`: first bucket starts at `j = 0`; subsequent buckets start at `j = SubBucketHalfCount`. *(criterion 3)*
    5. Return `(100.0 * runningCount) / TotalCount`, clamped to `[0.0, 100.0]`. *(criteria 1, 2)*
  - Verifiable: method appears in `HistogramBase`, compiles without error, and all tests below pass.

- [ ] **`HdrHistogram/HistogramBase.cs`** — Add XML doc comment for `GetPercentileAtOrBelowValue` following the existing style (summary, `<param>`, `<returns>`).
  - Must describe: the method is the inverse of `GetValueAtPercentile`; the return range `[0.0, 100.0]`; the equivalent-value-range semantics.
  - Verifiable: `dotnet build` produces no XML doc warnings for the new method.

---

## Unit Tests

All tests go in **`HdrHistogram.UnitTests/HistogramTestBase.cs`** as `[Fact]` methods on the abstract base class, so they run for `ShortHistogram`, `IntHistogram`, and `LongHistogram` automatically.
Use `FluentAssertions` and the existing `Create(highestTrackableValue, significantFigures)` / `Create(lowestTrackableValue, highestTrackableValue, significantFigures)` factory helpers.
Floating-point comparisons must use `.BeApproximately(..., precision: 0.1)` unless an exact value is expected.

- [ ] **Empty histogram returns 0.0** *(criterion 5)*
  - Create a histogram with no recorded values.
  - Assert `histogram.GetPercentileAtOrBelowValue(TestValueLevel).Should().Be(0.0)`.
  - Verifiable: test is green for all three histogram word sizes.

- [ ] **Value at or above `HighestTrackableValue` returns 100.0** *(criterion 4)*
  - Create a histogram, record at least one value.
  - Assert `histogram.GetPercentileAtOrBelowValue(DefaultHighestTrackableValue).Should().Be(100.0)`.
  - Also assert for a value beyond range: `histogram.GetPercentileAtOrBelowValue(long.MaxValue / 2).Should().Be(100.0)`.
  - Verifiable: test is green; no exception is thrown.

- [ ] **Basic percentile — known value set** *(criterion 3)*
  - Record values `1` through `100` (one each), so `TotalCount == 100`.
  - Assert `GetPercentileAtOrBelowValue(50)` is approximately `50.0` (±0.1).
  - Assert `GetPercentileAtOrBelowValue(100)` is approximately `100.0` (±0.1).
  - Assert `GetPercentileAtOrBelowValue(1)` is approximately `1.0` (±0.1).
  - Verifiable: test is green for all three histogram word sizes.

- [ ] **Return value is always in range `[0.0, 100.0]`** *(criterion 2)*
  - Record a known set of values.
  - For a range of query values (including 0, a recorded value, and `HighestTrackableValue`), assert each result `.Should().BeInRange(0.0, 100.0)`.
  - Verifiable: test is green for all three histogram word sizes.

- [ ] **Round-trip consistency with `GetValueAtPercentile`** *(criterion 6)*
  - Record values `1`, `10`, `100`, `1000`.
  - For each recorded value `v`, compute `p = histogram.GetPercentileAtOrBelowValue(v)`.
  - Assert `histogram.GetValueAtPercentile(p)` equals `histogram.HighestEquivalentValue(histogram.LowestEquivalentValue(v))`.
  - Verifiable: test is green; demonstrates the inverse relationship holds within histogram resolution.

- [ ] **Monotonicity — non-decreasing percentiles for increasing values** *(criterion 7)*
  - Record values `1`, `50`, `100`, `500`, `1000`.
  - Query percentiles for `1, 10, 50, 100, 500, 1000, 5000` in order.
  - Assert each successive result is `>=` the previous one.
  - Verifiable: test is green for all three histogram word sizes.

- [ ] **Single recorded value — queries below return 0.0, queries at or above return 100.0** *(criteria 3, 4, 5)*
  - Create a histogram, record value `TestValueLevel` (= 4) once.
  - Assert `GetPercentileAtOrBelowValue(TestValueLevel - 1).Should().Be(0.0)` (value below the recorded value).
  - Assert `GetPercentileAtOrBelowValue(TestValueLevel).Should().Be(100.0)` (value equals the recorded value).
  - Assert `GetPercentileAtOrBelowValue(TestValueLevel + 1000).Should().Be(100.0)` (value above the recorded value).
  - Verifiable: test is green for all three histogram word sizes.

- [ ] **Boundary — query at `LowestTrackableValue`** *(criterion 3)*
  - Create a histogram with `lowestTrackableValue = 1`.
  - Record value `1` once and value `1000` once (`TotalCount == 2`).
  - Assert `GetPercentileAtOrBelowValue(1)` is approximately `50.0` (±0.1) — one of two values is `<= 1`.
  - Verifiable: test is green for all three histogram word sizes.

---

## Acceptance Criterion Coverage Matrix

| Criterion | Covered by task(s) |
|-----------|-------------------|
| 1. Public method `double GetPercentileAtOrBelowValue(long value)` on `HistogramBase` | Implementation task (add method) |
| 2. Return value in `[0.0, 100.0]` | Implementation (clamp); test "Return value is always in range" |
| 3. Returns % of values `<= v` using equivalent-value semantics | Implementation (algorithm step 4); tests: basic percentile, single value, boundary |
| 4. Returns `100.0` when `value >= HighestTrackableValue` or beyond range | Implementation (step 3); tests: at-maximum, single value (above) |
| 5. Returns `0.0` when `TotalCount == 0` | Implementation (step 1); test: empty histogram |
| 6. Round-trip: `GetValueAtPercentile(GetPercentileAtOrBelowValue(v))` equals highest equivalent value of bucket | Test: round-trip consistency |
| 7. Monotonic for increasing input values | Test: monotonicity |
