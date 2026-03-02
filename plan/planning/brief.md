# Issue #131: Fix build warnings from dotnet build Release configuration

## Summary

Running `dotnet build -v=q -c=Release` produces numerous warnings across the HdrHistogram project.
The warnings fall into five categories: nullable reference types (CS8618, CS8625, CS8603), string comparison locale (CA1305, CA1309, CA1310, CA1866), default value initialisation (CA1805), performance recommendations (CA1859), and other code-analysis rules (CA1711, CA2201, CA1510).
None of these require behaviour changes — they are annotation, style, and API-usage fixes that align the code with the compiler's analysis expectations.

## Affected Files (confirmed by exploration)

### Nullable reference type warnings

| File | Line | Warning | Current code | Required fix |
|------|------|---------|--------------|--------------|
| `HdrHistogram/HistogramEncoding.cs` | 65 | CS8625 | `DeflateStream decompressor = null` | `DeflateStream? decompressor = null` |
| `HdrHistogram/HistogramBase.cs` | 50 | CS8618 | `private string _tag;` (uninitialized, emitted at ctor line 181) | `private string? _tag;` |
| `HdrHistogram/HistogramLogReader.cs` | 269, 272 | CS8603 | `return null;` in `string ParseTag(string value)` | Change return type to `string?` |
| `HdrHistogram/Iteration/AbstractHistogramEnumerator.cs` | 38 | CS8618 | `public HistogramIterationValue Current { get; private set; }` not set in ctor | Initialise `Current = new HistogramIterationValue();` in constructor |
| `HdrHistogram/Recorder.cs` | 134 | CS8625 | `return GetIntervalHistogram(null)` passes null to non-nullable param | Change `histogramToRecycle` param and `_inactiveHistogram` field to nullable (`HistogramBase?`) |
| `HdrHistogram/Recorder.cs` | 159 | CS8625 | `_inactiveHistogram = null` on non-nullable field | Same fix as above; use `sampledHistogram!` null-forgiving on return since post-`PerformIntervalSample()` the value is guaranteed non-null |
| `HdrHistogram/Utilities/TypeHelper.cs` | 22–23 | CS8603 | `FirstOrDefault(...)` returns nullable but method signature is `ConstructorInfo` | Change return type to `ConstructorInfo?` |

### String comparison locale warnings

| File | Lines | Warning | Current code | Required fix |
|------|-------|---------|--------------|--------------|
| `HdrHistogram/HistogramLogReader.cs` | 242 | CA1310/CA1866 | `line.StartsWith("#")` | Use char overload: `line.StartsWith('#')` |
| `HdrHistogram/HistogramLogReader.cs` | 247 | CA1310 | `line.StartsWith("#[StartTime: ")` | Add `StringComparison.Ordinal` |
| `HdrHistogram/HistogramLogReader.cs` | 252 | CA1310 | `line.StartsWith("#[BaseTime: ")` | Add `StringComparison.Ordinal` |
| `HdrHistogram/HistogramLogReader.cs` | 258 | CA1309 | `line.Equals(legend)` | `string.Equals(line, legend, StringComparison.Ordinal)` |
| `HdrHistogram/HistogramLogReader.cs` | 263 | CA1309 | `line.Equals(legend)` | `string.Equals(line, legend, StringComparison.Ordinal)` |
| `HdrHistogram/HistogramLogReader.cs` | 291 | CA1305 | `double.Parse(value)` | `double.Parse(value, CultureInfo.InvariantCulture)` |

### Default value initialisation warnings (CA1805)

| File | Lines | Current code | Required fix |
|------|-------|--------------|--------------|
| `HdrHistogram/HistogramLogWriter.cs` | 17–18 | `_hasHeaderWritten = false`, `_isDisposed = 0` | Remove explicit default initialisation |
| `HdrHistogram/IntConcurrentHistogram.cs` | 24 | `_totalCount = 0L` | Remove explicit default initialisation |
| `HdrHistogram/LongConcurrentHistogram.cs` | 24 | `_totalCount = 0L` | Remove explicit default initialisation |
| `HdrHistogram/Utilities/WriterReaderPhaser.cs` | 45–46 | `_startEpoch = 0`, `_evenEndEpoch = 0` | Remove explicit default initialisation |

### Performance recommendations (CA1859)

| File | Line | Warning | Current code | Required fix |
|------|------|---------|--------------|--------------|
| `HdrHistogram/HistogramBase.cs` | 707 | CA1859 | `private IRecordedData GetData()` | Change return type to `RecordedData` (private method, no API impact) |
| `HdrHistogram/Persistence/CountsDecoder.cs` | 11 | CA1859 | `IDictionary<int, ICountsDecoder> Decoders` | Change field type to `Dictionary<int, ICountsDecoder>` |

### Other code analysis warnings

| File | Line | Warning | Current code | Required fix |
|------|------|---------|--------------|--------------|
| `HdrHistogram/HistogramFactoryDelegate.cs` | 18 | CA1711 | `public delegate … HistogramFactoryDelegate` | Rename to `HistogramFactory` — **breaking public API change; see Risks** |
| `HdrHistogram/HistogramEncoding.cs` | 242 | CA2201 | `throw new IndexOutOfRangeException()` | Replace with `throw new InvalidOperationException(…)` (reserved by runtime) |
| `HdrHistogram/Utilities/ByteBuffer.cs` | 198 | CA1510 | Manual `if (value == null) throw new ArgumentNullException(…)` | `ArgumentNullException.ThrowIfNull(value)` |

## Acceptance Criteria

- `dotnet build -v=q -c=Release` produces zero warnings.
- All existing tests continue to pass (`dotnet test`).

## Test Strategy

These are purely annotation/style/type-system fixes — no business logic changes.
No new tests need to be written.
The existing test suite validates correctness after each fix.

The following test areas touch the affected code and should be confirmed green:

- Histogram encoding/decoding round-trip tests (cover `HistogramEncoding.cs`, `CountsDecoder.cs`).
- Log reader/writer tests (cover `HistogramLogReader.cs`, `HistogramLogWriter.cs`).
- `Recorder` interval-histogram tests (cover `Recorder.cs`).
- Iteration tests (cover `AbstractHistogramEnumerator.cs`).

Run command: `dotnet test HdrHistogram.UnitTests/HdrHistogram.UnitTests.csproj -c Release`

## Risks and Open Questions

### CA1711 — `HistogramFactoryDelegate` rename (breaking public API)

Renaming `HistogramFactoryDelegate` to `HistogramFactory` is a **breaking change** for any consumer that references the type by name.
Options:

1. **Rename** and bump the major version — aligns with .NET naming conventions but breaks callers.
2. **Suppress** the warning with `#pragma warning disable CA1711` or `[SuppressMessage]` — avoids the break but leaves the name violation.
3. **Obsolete and alias** — add a `[Obsolete]` `HistogramFactory` type alias pointing to the old name; remove the old name in a future major version.

Recommended approach: **suppress** for this issue (scope is warnings only, not API redesign); track as a separate breaking-change issue.

### `ArgumentNullException.ThrowIfNull` target framework

`ArgumentNullException.ThrowIfNull` was introduced in .NET 6.
The project targets `netstandard2.0` and `net6.0` (check `.csproj`/`Directory.Build.props`).
If `netstandard2.0` is a target, the call must be guarded with `#if NET6_0_OR_GREATER` or a polyfill, or the warning should be suppressed for the netstandard build.

### `string?` propagation in `HistogramLogReader.ParseTag`

`ParseTag` returns `string?` after the fix.
All call sites that assign its result must be updated to handle nullable (either check for null or propagate `?`).
Verify that the callers already handle null correctly at runtime.

### `HistogramBase._tag` nullability and the `Tag` property setter

The `Tag` property setter currently accepts any string (including empty).
After marking `_tag` as `string?`, the getter return type should also become `string?` to stay consistent.
Verify that all callers of `Tag` handle a possible null return.
