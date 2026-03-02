# Task List: Fix Build Warnings from dotnet build Release Configuration

Issue: #131
Branch: `agent/131-fix-build-warnings-from-dotnet-build-rel`

## Acceptance Criteria Coverage

| Criterion | Covered by tasks |
|-----------|-----------------|
| `dotnet build -v=q -c=Release` produces zero warnings | All fix tasks + T30 (build verification) |
| All existing tests continue to pass | T31 (test run) |

---

## Nullable Reference Type Warnings

### CS8625 — Non-nullable parameter/field assigned null

- [x] **T01** — `HdrHistogram/HistogramEncoding.cs:65`
  Change `DeflateStream decompressor = null` to `DeflateStream? decompressor = null` in the `DecodeFromByteBuffer` method signature.
  **Verify:** Warning CS8625 no longer emitted for this line; method still compiles.

- [x] **T02** — `HdrHistogram/Recorder.cs` — field `_inactiveHistogram`
  Change `private HistogramBase _inactiveHistogram;` to `private HistogramBase? _inactiveHistogram;`.
  **Verify:** Field declaration uses nullable type.

- [x] **T03** — `HdrHistogram/Recorder.cs` — `GetIntervalHistogram(HistogramBase histogramToRecycle)` parameter
  Change `HistogramBase histogramToRecycle` parameter to `HistogramBase? histogramToRecycle` and update `_inactiveHistogram = histogramToRecycle;` assignment (already compatible after T02).
  Also change `_inactiveHistogram = null;` at line ~159 (compatible after T02).
  **Verify:** Warning CS8625 no longer emitted; callers that pass `null` remain valid.

- [x] **T04** — `HdrHistogram/Recorder.cs:175` — null-forgiving operator on `sampledHistogram` return
  Change `return sampledHistogram;` to `return sampledHistogram!;` and change `_inactiveHistogram.CopyInto(targetHistogram)` to `_inactiveHistogram!.CopyInto(targetHistogram)` where the value is guaranteed non-null post-`PerformIntervalSample()`.
  **Verify:** Warning CS8602 no longer emitted; logic is unchanged (lock guarantees non-null).

### CS8618 — Non-nullable field/property uninitialized

- [x] **T05** — `HdrHistogram/HistogramBase.cs:50` — backing field `_tag`
  Change `private string _tag;` to `private string? _tag;`.
  **Verify:** Field declaration uses nullable type.

- [x] **T06** — `HdrHistogram/HistogramBase.cs` — `Tag` property return type
  Change `public string Tag { get; set; }` (or the manual getter/setter) to `public string? Tag { get; set; }`.
  Both getter and setter use the nullable backing field from T05.
  **Verify:** Warning CS8618 no longer emitted; all callers already handle null at runtime (log writer checks `histogram.Tag == null` before use).

- [x] **T07** — `HdrHistogram/Iteration/AbstractHistogramEnumerator.cs:38`
  Initialise the `Current` property inside the constructor body: add `Current = new HistogramIterationValue();` at the end of the `AbstractHistogramEnumerator(HistogramBase histogram)` constructor.
  **Verify:** Warning CS8618 no longer emitted for this property; object is always initialised before use.

### CS8603 — Possible null reference return

- [x] **T08** — `HdrHistogram/HistogramLogReader.cs:266-274` — `ParseTag` return type
  Change `private static string ParseTag(string value)` to `private static string? ParseTag(string value)`.
  **Verify:** Warning CS8603 no longer emitted; caller at line ~84 (`var tag = ParseTag(...)`) already assigns to an implicitly-typed variable so propagation is automatic.

- [x] **T09** — `HdrHistogram/Utilities/TypeHelper.cs:22-23` — `GetConstructor` return type
  Change the return type of the method containing `FirstOrDefault(...)` from `ConstructorInfo` to `ConstructorInfo?`.
  **Verify:** Warning CS8603 no longer emitted; confirm all callers handle null (check call sites in the file and any usages elsewhere in the project).

---

## String Comparison Locale Warnings (CA1305, CA1309, CA1310)

All changes are in `HdrHistogram/HistogramLogReader.cs`.

- [x] **T10** — Line 242 (`IsComment` method)
  Change `line.StartsWith("#")` to `line.StartsWith("#", StringComparison.Ordinal)`.
  **Note:** Do not use the `char` overload — `string.StartsWith(char)` requires .NET Standard 2.1+, unavailable on the `netstandard2.0` target.
  **Verify:** Warning CA1310/CA1866 no longer emitted.

- [x] **T11** — Line 247 (`IsStartTime` method)
  Change `line.StartsWith("#[StartTime: ")` to `line.StartsWith("#[StartTime: ", StringComparison.Ordinal)`.
  **Verify:** Warning CA1310 no longer emitted.

- [x] **T12** — Line 252 (`IsBaseTime` method)
  Change `line.StartsWith("#[BaseTime: ")` to `line.StartsWith("#[BaseTime: ", StringComparison.Ordinal)`.
  **Verify:** Warning CA1310 no longer emitted.

- [x] **T13** — Line 258 (`IsLegend` method)
  Change `line.Equals(legend)` to `string.Equals(line, legend, StringComparison.Ordinal)`.
  **Verify:** Warning CA1309 no longer emitted.

- [x] **T14** — Line 263 (`IsV1Legend` method)
  Change `line.Equals(legend)` to `string.Equals(line, legend, StringComparison.Ordinal)`.
  **Verify:** Warning CA1309 no longer emitted.

- [x] **T15** — Line 291 (`ParseDouble` or inline `double.Parse`)
  Change `double.Parse(value)` to `double.Parse(value, CultureInfo.InvariantCulture)`.
  Add `using System.Globalization;` if not already present.
  **Verify:** Warning CA1305 no longer emitted.

---

## Default Value Initialisation Warnings (CA1805)

Remove explicit assignments of the default value of a type (CLR already zeroes fields).

- [x] **T16** — `HdrHistogram/HistogramLogWriter.cs:17-18`
  Remove `= false` from `private bool _hasHeaderWritten = false;`.
  Remove `= 0` from `private int _isDisposed = 0;` (or whichever integer field has `= 0`).
  **Verify:** Warning CA1805 no longer emitted for these lines; compile succeeds.

- [x] **T17** — `HdrHistogram/IntConcurrentHistogram.cs:24`
  Remove `= 0L` from `private long _totalCount = 0L;`.
  **Verify:** Warning CA1805 no longer emitted.

- [x] **T18** — `HdrHistogram/LongConcurrentHistogram.cs:24`
  Remove `= 0L` from `private long _totalCount = 0L;`.
  **Verify:** Warning CA1805 no longer emitted.

- [x] **T19** — `HdrHistogram/Utilities/WriterReaderPhaser.cs:45-46`
  Remove `= 0` from `private long _startEpoch = 0;` and `private long _evenEndEpoch = 0;`.
  **Verify:** Warning CA1805 no longer emitted for both fields.

---

## Performance Recommendations (CA1859)

Use the concrete type instead of an interface for private/static members to allow devirtualisation.

- [x] **T20** — `HdrHistogram/HistogramBase.cs:707` — `GetData` return type
  Change `private IRecordedData GetData()` to `private RecordedData GetData()`.
  (Private method; no public API impact.)
  **Verify:** Warning CA1859 no longer emitted; callers compile against the concrete type.

- [x] **T21** — `HdrHistogram/Persistence/CountsDecoder.cs:11` — `Decoders` field type
  Change `private static readonly IDictionary<int, ICountsDecoder> Decoders` to `private static readonly Dictionary<int, ICountsDecoder> Decoders`.
  **Verify:** Warning CA1859 no longer emitted; static constructor assignment still type-compatible.

---

## Other Code Analysis Warnings

### CA1711 — Identifier ends with reserved suffix

- [x] **T22** — `HdrHistogram/HistogramFactoryDelegate.cs`
  Suppress the CA1711 warning with a `#pragma warning disable CA1711` / `#pragma warning restore CA1711` pair around the delegate declaration.
  **Rationale:** Renaming to `HistogramFactory` is impossible — that name is already taken by `public abstract class HistogramFactory` in `HdrHistogram/Histogram.cs`. API redesign is out of scope for this issue.
  **Verify:** Warning CA1711 no longer emitted; public API is unchanged.

### CA2201 — Exception type is reserved by the runtime

- [x] **T23** — `HdrHistogram/HistogramEncoding.cs:242` — `GetBestTypeForWordSize` default case
  Replace `throw new IndexOutOfRangeException();` with `throw new InvalidOperationException($"Unexpected word size: {wordSizeInBytes}");` (or similar descriptive message).
  **Verify:** Warning CA2201 no longer emitted; the `default:` branch now throws a non-reserved exception type.

### CA1510 — Use ArgumentNullException.ThrowIfNull

- [x] **T24** — `HdrHistogram/Utilities/ByteBuffer.cs:198`
  Suppress the CA1510 warning at the call site using `#pragma warning disable CA1510` / `#pragma warning restore CA1510` around the manual `if (value == null) throw new ArgumentNullException(...)` guard.
  **Rationale:** `ArgumentNullException.ThrowIfNull` requires .NET 6+; the project targets `netstandard2.0` where this API is not available. Prefer suppression over `#if NET6_0_OR_GREATER` to avoid complexity.
  **Verify:** Warning CA1510 no longer emitted; null guard remains intact and functional.

---

## Verification Tasks

- [x] **T30** — Run `dotnet build -v=q -c=Release` from the repository root.
  **Verify:** Output contains zero warnings. ✓ Confirmed: 0 Warning(s), 0 Error(s).

- [x] **T31** — Run `dotnet test HdrHistogram.UnitTests/HdrHistogram.UnitTests.csproj -c Release`.
  **Verify:** All tests pass with no failures. ✓ Confirmed: 800 passed, 0 failed, 0 skipped.
  Key test areas:
  - Encoding/decoding round-trip tests (`HistogramEncodingTestBase.cs`, `ShortHistogramEncodingTests.cs`, `LongHistogramEncodingTests.cs`)
  - Log reader/writer tests (`Persistence/` folder — `HistogramLogReaderWriterTestBase.cs` and per-type derivatives)
  - Recorder interval-histogram tests (`Recording/RecorderTestsBase.cs` and per-type derivatives)
  - Iteration tests (histogram test classes that exercise `AbstractHistogramEnumerator`)
