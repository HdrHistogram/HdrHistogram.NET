# Brief Review: Issue #82 — Subtract Method

## Verdict: Revise before implementation

The brief is clear, well-scoped, and architecturally sound.
All three files exist and the proposed changes are feasible.
However, five specific issues need resolving before handoff to avoid implementer confusion.

---

## Issue 1 — Open Question #2 is already answered: resolve it

The brief lists as an open question whether `RecordValueWithCount` guards against negative counts.
Codebase inspection confirms it does **not**:

```csharp
// HistogramBase.cs ~line 237
public void RecordValueWithCount(long value, long count)
{
    var bucketIndex = GetBucketIndex(value);
    var subBucketIndex = GetSubBucketIndex(value, bucketIndex);
    var countsIndex = CountsArrayIndex(bucketIndex, subBucketIndex);
    AddToCountAtIndex(countsIndex, count);  // no validation
}
```

**Action:** Remove Open Question #2.
In the slow-path Implementation Note, commit to the simpler approach:
pass `-count` directly to `RecordValueWithCount`.
Delete the mention of a `SubtractFromCountAtValue` helper — it is not needed.

---

## Issue 2 — Slow-path code snippet is ambiguous (two options, no decision)

The slow-path note presents both a `SubtractFromCountAtValue` helper and `RecordValueWithCount(-count)` as alternatives without choosing one.
Issue 1 above resolves the blocker; the simpler option is now unambiguously correct.

**Action:** Replace both slow-path snippets with a single definitive version:

```csharp
for (var i = 0; i < fromHistogram.CountsArrayLength; i++)
{
    var count = fromHistogram.GetCountAtIndex(i);
    RecordValueWithCount(fromHistogram.ValueFromIndex(i), -count);
}
```

This exactly mirrors the `Add` slow path (lines 299–303) with the sign flipped,
and requires no new helper.

---

## Issue 3 — Slow-path has a `if (count != 0)` guard that `Add` does not

The brief's proposed slow-path wraps the operation in `if (count != 0)`.
The actual `Add` slow path (lines 299–303) does **not** filter zero counts.

Passing `count = 0` to `RecordValueWithCount` is harmless (adds 0, increments `TotalCount` by 0),
so the guard is unnecessary.
Keeping it creates an unexplained divergence from `Add`.

**Action:** Drop the `if (count != 0)` guard from the slow-path snippet to match `Add` exactly.
If there is a performance reason to keep it, document it explicitly.

---

## Issue 4 — Test 1 assertions are under-specified

The brief says "Assert that counts equal the surplus, and `TotalCount` is correct" without
giving exact values.
All three `Add` tests use concrete `Assert.Equal(…)` calls.
The test author needs the same level of specificity to write a correct test without guessing.

**Action:** Specify the setup and assertions concretely, matching the `Add` test style.
Suggested wording:

> Record `TestValueLevel` and `TestValueLevel * 1000` into `histogram` twice each.
> Record `TestValueLevel` and `TestValueLevel * 1000` into `other` once each.
> Call `histogram.Subtract(other)`.
> Assert `histogram.GetCountAtValue(TestValueLevel) == 1L`,
> `histogram.GetCountAtValue(TestValueLevel * 1000) == 1L`,
> and `histogram.TotalCount == 2L`.

---

## Issue 5 — Exception message in the brief differs from the one in `Add`

The existing `Add` validation message (line 283) has a minor formatting issue —
the first interpolated value is not closed with `)`:

```csharp
// existing Add — note missing closing ')' after HighestTrackableValue
$"The other histogram covers a wider range ({fromHistogram.HighestTrackableValue} than this one ({HighestTrackableValue})."
```

The brief's proposed message adds the missing `)`, which is technically correct but
inconsistent with `Add`.

**Action:** Decide explicitly: either copy the existing message verbatim (warts and all, for
consistency with `Add`) or fix both `Add` and `Subtract` in the same PR and add a note to
the Files Affected table.
Do not silently diverge.

---

## Summary of required changes to the brief

| # | Change |
|---|--------|
| 1 | Remove Open Question #2; state definitively that `RecordValueWithCount` accepts negative counts |
| 2 | Replace two slow-path options with one: `RecordValueWithCount(fromHistogram.ValueFromIndex(i), -count)` |
| 3 | Drop `if (count != 0)` guard from slow-path to match `Add` |
| 4 | Specify Test 1 setup and assertions with exact values and `Assert.Equal` calls |
| 5 | Decide whether to copy the existing `Add` exception message or fix it in both methods; document the choice |

None of these change the scope or architecture.
After addressing them the brief is ready to move to `plan/ready/`.
