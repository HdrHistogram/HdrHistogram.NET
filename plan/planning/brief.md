# Issue #106: Fix Unresolved XML cref in Bitwise.cs for netstandard2.0

## Summary

The build produces a `CS1574` warning when compiling `HdrHistogram.csproj` for the `netstandard2.0` target.
The XML doc comment on the `Imperative` nested class inside `HdrHistogram/Utilities/Bitwise.cs` (line 53) contains a `<see cref="System.Numerics.BitOperations.LeadingZeroCount(ulong)"/>` reference.
`System.Numerics.BitOperations` was introduced in .NET Core 3.0 and is not part of the `netstandard2.0` API surface, so the compiler cannot resolve the `cref` and emits a warning.

The fix is to change the XML doc comment so the reference is expressed as plain formatted code (using `<c>`) rather than a resolvable `cref`, eliminating the warning for all target frameworks without altering runtime behaviour.

## Affected Files

- `HdrHistogram/Utilities/Bitwise.cs` — line 53, XML `<summary>` doc comment on the `Imperative` class (the only change required)

## What Needs to Change and Why

| Location | Current | Problem |
|----------|---------|---------|
| `Bitwise.cs:53` | `<see cref="System.Numerics.BitOperations.LeadingZeroCount(ulong)"/>` | `System.Numerics.BitOperations` does not exist in `netstandard2.0`; compiler emits `CS1574` |

Replace the `<see cref="..."/>` with `<c>System.Numerics.BitOperations.LeadingZeroCount(ulong)</c>`.
This preserves the intent (showing the fully-qualified method name in a monospace code style) without requiring the compiler to resolve a type that is absent from the `netstandard2.0` reference assembly set.

No conditional compilation (`#if`) is needed; plain `<c>` is the minimal, least-intrusive change.

## Acceptance Criteria

- Building `HdrHistogram.csproj` with `dotnet build -f netstandard2.0` produces **no** `CS1574` warning.
- Building `HdrHistogram.csproj` with `dotnet build -f net8.0` continues to succeed without new warnings.
- The generated XML documentation for `Imperative` still describes its purpose clearly.
- No runtime behaviour changes (the fix is doc-comment only).

## Test Strategy

The issue is confined to an XML doc comment; no logic changes, so no new unit tests are required.
Verification is via the build itself:

1. Run `dotnet build HdrHistogram/HdrHistogram.csproj -f netstandard2.0 /warnaserror:CS1574` — must exit 0.
2. Run `dotnet build HdrHistogram/HdrHistogram.csproj -f net8.0` — must exit 0.
3. Run the existing test suite (`dotnet test`) to confirm no regressions.

## Risks and Open Questions

- **Minimal risk**: The change is one line inside a doc comment; it cannot affect compiled IL or runtime behaviour.
- **Open question**: Should `CS1574` be promoted to an error in the project file (`<TreatWarningsAsErrors>` or `<WarningsAsErrors>CS1574</WarningsAsErrors>`) to prevent recurrence?
  This would be a separate, optional hardening step and is out of scope for this issue.
- **Alternative considered**: Using `#if NET5_0_OR_GREATER` ... `#else` conditional compilation around the entire `<summary>` block.
  This works but is significantly more verbose for no additional benefit over a simple `<c>` tag.
