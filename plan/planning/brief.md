# Issue #82: Subtract — additional method rather than issue

## Summary

The request is to add a `Subtract` method to `HistogramBase` that mirrors the existing `Add` method.
The `Add` method merges counts from one histogram into another by summing bucket counts.
`Subtract` would do the inverse: remove counts from one histogram by subtracting the source histogram's bucket counts from the target's.

This is a natural complement to `Add` and follows the same structural pattern (fast path for compatible structures, slow path for incompatible ones).

## Files Affected

| File | Change |
|---|---|
| `HdrHistogram/HistogramBase.cs` | Add `Subtract(HistogramBase)` virtual method (primary change) |
| `HdrHistogram.UnitTests/HistogramTestBase.cs` | Add three mirrored test methods for `Subtract` |
| `spec/tech-standards/api-reference.md` | Document `Subtract` in the public API section alongside `Add` |

Concrete implementations (`LongHistogram`, `IntHistogram`, `ShortHistogram`, `LongConcurrentHistogram`, `IntConcurrentHistogram`) do **not** require changes because they inherit the base implementation and already implement `AddToCountAtIndex` which accepts negative values for subtraction.

## Acceptance Criteria

- A `Subtract(HistogramBase fromHistogram)` method exists on `HistogramBase` with the same visibility as `Add` (`public virtual`).
- Subtracting a histogram from itself results in all bucket counts being zero and `TotalCount == 0`.
- Subtracting a histogram that has fewer recordings at a value reduces the count at that value by the correct amount.
- Subtracting a histogram whose `HighestTrackableValue` exceeds the target's throws `ArgumentOutOfRangeException` (same guard as `Add`).
- Subtracting when the structures are compatible (same `BucketCount`, `SubBucketCount`, `_unitMagnitude`) uses the fast path (direct index iteration).
- Subtracting when structures differ uses the slow path (re-record via `ValueFromIndex`).
- `TotalCount` is kept consistent after subtraction.
- XML documentation matches the quality of the `Add` doc comment.

## Test Strategy

Add the following test methods to `HdrHistogram.UnitTests/HistogramTestBase.cs`, directly below the existing `Add` tests:

1. **`Subtract_should_reduce_the_counts_from_two_histograms`**
   - Record values into `histogram` and `other` (same config).
   - Record one extra value into `histogram` at the same slots.
   - Subtract `other` from `histogram`.
   - Assert that counts equal the surplus, and `TotalCount` is correct.

2. **`Subtract_should_allow_small_range_histograms_to_be_subtracted`**
   - Create `biggerOther` with double the range; record same values into both.
   - Subtract smaller histogram from `biggerOther`.
   - Assert counts and `TotalCount` decrease correctly.

3. **`Subtract_throws_if_other_has_a_larger_range`**
   - Create a smaller target and a larger source.
   - Assert `ArgumentOutOfRangeException` is thrown (mirrors `Add_throws_if_other_has_a_larger_range`).

These three tests run against all concrete histogram types via the existing abstract base-test pattern (xUnit inheritance through `LongHistogramTests`, `IntHistogramTests`, `ShortHistogramTests`, etc.).

## Implementation Notes

### Fast path

```csharp
for (var i = 0; i < fromHistogram.CountsArrayLength; i++)
{
    AddToCountAtIndex(i, -fromHistogram.GetCountAtIndex(i));
}
```

`AddToCountAtIndex` already accepts negative `addend` values; passing the negated count is sufficient.

### Slow path

```csharp
for (var i = 0; i < fromHistogram.CountsArrayLength; i++)
{
    var count = fromHistogram.GetCountAtIndex(i);
    if (count != 0)
    {
        SubtractFromCountAtValue(fromHistogram.ValueFromIndex(i), count);
    }
}
```

The slow path requires a helper `SubtractFromCountAtValue` (analogous to `RecordValueWithCount` but decrements).
Alternatively — and more simply — reuse `RecordValueWithCount` with a **negated** count if the internal implementation tolerates it (it calls `AddToCountAtIndex` which does).
Confirm this before coding; if `RecordValueWithCount` has a guard rejecting negative counts, a dedicated helper is needed.

### Validation

Identical guard to `Add`:

```csharp
if (HighestTrackableValue < fromHistogram.HighestTrackableValue)
{
    throw new ArgumentOutOfRangeException(
        nameof(fromHistogram),
        $"The other histogram covers a wider range ({fromHistogram.HighestTrackableValue}) " +
        $"than this one ({HighestTrackableValue}).");
}
```

## Risks and Open Questions

1. **Negative counts**: The current design does not prevent bucket counts from going negative if more is subtracted than was recorded.
   The Java reference implementation does not guard against this either; the initial implementation should follow the same lenient approach.
   A future issue can address validation if needed.

2. **`RecordValueWithCount` with negative count**: Check whether `RecordValueWithCount` internally rejects negative values.
   If it does, the slow path must call `AddToCountAtIndex` directly (after computing the index) rather than going via `RecordValueWithCount`.

3. **Concurrent implementations**: `LongConcurrentHistogram` and `IntConcurrentHistogram` override `AddToCountAtIndex` with atomic operations.
   Because `Subtract` in the base class uses `AddToCountAtIndex` (passing negated counts), concurrent safety should be inherited automatically — but this must be verified.

4. **`TotalCount` consistency**: `AddToCountAtIndex` increments `TotalCount` by the addend.
   Passing a negative addend should decrement `TotalCount` correctly, keeping it consistent.
   Verify with `LongHistogram` before relying on this for all types.
