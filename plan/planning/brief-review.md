# Brief Review — Issue #131: Fix build warnings from dotnet build Release configuration

## Verdict: Changes required before promoting to ready

All 13 files referenced in the brief exist and line numbers are accurate within ±5 lines.
The scope is appropriate for one PR.
The test strategy and acceptance criteria are clear and measurable.
Two issues must be resolved before the brief is promoted.

---

## Issue 1 — Critical: `StartsWith('#')` char overload is not available on netstandard2.0

**Location:** Affected Files table, nullable reference type warnings section, `HistogramLogReader.cs` line 242.

**Problem:**
The brief prescribes:

> Use char overload: `line.StartsWith('#')`

`string.StartsWith(char)` was introduced in .NET Standard 2.1.
The project targets both `net8.0` and `netstandard2.0` (confirmed in `HdrHistogram/HdrHistogram.csproj` line 4).
Using the char overload would cause a compilation failure on the `netstandard2.0` target.

**Required fix:**
Replace the prescribed fix with one that compiles on both targets:

| File | Line | Warning | Current code | Required fix |
|------|------|---------|--------------|--------------|
| `HdrHistogram/HistogramLogReader.cs` | 242 | CA1310/CA1866 | `line.StartsWith("#")` | `line.StartsWith("#", StringComparison.Ordinal)` |

The ordinal string comparison satisfies CA1310, is compatible with netstandard2.0, and avoids the CA1866 trigger entirely (the analyser only recommends the char overload when the framework supports it; switching to ordinal comparison is the accepted netstandard2.0-safe alternative).

Also verify whether the CA1866 warning actually fires on the netstandard2.0 build or only on net8.0.
If it fires only on net8.0, note in the brief that the char overload is still not appropriate because the source must compile on both targets, and ordinal is the correct cross-target fix.

---

## Issue 2 — Minor clarity: `Tag` property getter/setter return-type change belongs in the Affected Files table

**Location:** "Risks and Open Questions" section, `HistogramBase._tag` nullability subsection.

**Problem:**
Changing `private string _tag;` to `private string? _tag;` causes the compiler to reject any property getter that returns `string` (non-nullable) from a nullable backing field.
The necessary change — updating the `Tag` property getter return type to `string?` — is described in the Risks section but is absent from the Affected Files table.
A developer working from the table alone would encounter a secondary compiler error they were not briefed to expect.

**Required fix:**
Add an explicit row to the Affected Files table under the nullable reference type warnings:

| File | Line | Warning | Current code | Required fix |
|------|------|---------|--------------|--------------|
| `HdrHistogram/HistogramBase.cs` | (Tag property) | CS8618 (secondary) | `public string Tag { get; set; }` | `public string? Tag { get; set; }` |

Move the Risks note to a cross-reference ("See Affected Files table") rather than the primary description.

---

## What is already good

- Every file and line reference was verified against the actual source; all match.
- The nullable/null-forgiving annotations for `Recorder.cs` are correctly reasoned: `PerformIntervalSample()` guarantees `_inactiveHistogram` is non-null before `CopyInto` is reached, making `!` safe at line 175.
- The CA1711 suppression recommendation (rather than rename) is the right call for a warnings-only PR.
- The CA1510 pragma suppression for `ByteBuffer.cs` correctly identifies the netstandard2.0 compatibility constraint.
- The test command and identified test areas are complete and accurate.
- Scope is one PR's worth of work: all changes are mechanical, non-behavioural, and localised.
