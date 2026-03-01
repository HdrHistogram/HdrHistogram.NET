# Issue #107: Modernize NuGet Package Metadata (licenseUrl, iconUrl, readme)

## Summary

The `dotnet pack` step produced two deprecation warnings and an informational message:

- **NU5125**: `<PackageLicenseUrl>` is deprecated — replace with `<PackageLicenseExpression>` or `<PackageLicenseFile>`
- **NU5048**: `<PackageIconUrl>` is deprecated — replace with `<PackageIcon>` and embed the icon file
- **Missing readme**: Add `<PackageReadmeFile>` and include README.md in the package

All three issues are metadata-only changes in the `.csproj` file; no library source code is affected.

## Affected Files

| File | Change required |
|------|----------------|
| `HdrHistogram/HdrHistogram.csproj` | Replace deprecated NuGet properties; add `<None>` items to embed icon and readme |
| `HdrHistogram-icon-64x64.png` (repo root) | Already exists; must be declared in `<ItemGroup>` with `Pack="true"` |
| `README.md` (repo root) | Already exists; must be declared in `<ItemGroup>` with `Pack="true"` |

No other source, test, or CI files need to change.

## Current State (post-fix on this branch)

`HdrHistogram/HdrHistogram.csproj` already contains the modernized metadata as of commit `425f022`:

```xml
<PackageLicenseExpression>CC0-1.0 OR BSD-2-Clause</PackageLicenseExpression>
<PackageIcon>HdrHistogram-icon-64x64.png</PackageIcon>
<PackageReadmeFile>README.md</PackageReadmeFile>
<WarningsAsErrors>NU5125;NU5048</WarningsAsErrors>
```

And the embedding `<ItemGroup>`:

```xml
<None Include="../HdrHistogram-icon-64x64.png" Pack="true" PackagePath="" />
<None Include="../README.md" Pack="true" PackagePath="" />
```

The deprecated `<PackageLicenseUrl>` and `<PackageIconUrl>` elements have been removed.

## Acceptance Criteria

1. `dotnet pack` completes with **no NU5125 or NU5048 warnings**.
2. `dotnet pack` produces **no "missing readme" informational message**.
3. The generated `.nupkg` contains `HdrHistogram-icon-64x64.png` at the package root.
4. The generated `.nupkg` contains `README.md` at the package root.
5. `<PackageLicenseUrl>` is absent from the `.csproj`; `<PackageLicenseExpression>CC0-1.0 OR BSD-2-Clause</PackageLicenseExpression>` is present.
6. `<PackageIconUrl>` is absent from the `.csproj`; `<PackageIcon>HdrHistogram-icon-64x64.png</PackageIcon>` is present.
7. `<WarningsAsErrors>NU5125;NU5048</WarningsAsErrors>` prevents future regression (build fails if either warning reappears).
8. All existing unit tests continue to pass (`dotnet test`).

## Test Strategy

### No new tests needed

This change is purely metadata for the NuGet packaging step. There is no runtime behaviour to test and the xUnit test suite covers library logic, not packaging artefacts.

### Verification steps (manual / CI)

1. **Build check**: `dotnet build -c Release` — must succeed with zero NU5125/NU5048 warnings (enforced as errors by `<WarningsAsErrors>`).
2. **Pack check**: `dotnet pack ./HdrHistogram/HdrHistogram.csproj -c Release --no-build` — must produce no warnings or "missing readme" messages.
3. **Package content inspection** (optional but recommended):
   ```
   unzip -l bin/Release/HdrHistogram.*.nupkg | grep -E 'README|icon'
   ```
   Confirm `README.md` and `HdrHistogram-icon-64x64.png` appear in the archive.
4. **Unit tests**: `dotnet test ./HdrHistogram.UnitTests/HdrHistogram.UnitTests.csproj -c Release --no-build` — must pass without regressions.
5. **CI**: The GitHub Actions workflow (`ci.yml`) runs all of the above steps on every push and PR; a green run is sufficient automated evidence.

## Risks and Open Questions

| # | Risk / Question | Likelihood | Mitigation |
|---|----------------|-----------|------------|
| 1 | SPDX expression `CC0-1.0 OR BSD-2-Clause` must be valid per NuGet validation | Low | Both identifiers are in the SPDX license list; NuGet accepts compound SPDX expressions |
| 2 | Icon file must be accessible at the relative path `../HdrHistogram-icon-64x64.png` from the project directory | Low | File exists at repo root (`/workspace/repo/HdrHistogram-icon-64x64.png`); relative path is correct |
| 3 | `PackagePath=""` vs `PackagePath="\"` syntax for embedding files | Low | Empty string `""` is equivalent to root; both work with the NuGet SDK |
| 4 | `<WarningsAsErrors>` scope — does it affect only `dotnet pack` or also `dotnet build`? | Low | This property is evaluated at pack time; it will not affect library build warnings |
| 5 | README.md content — NuGet.org renders Markdown; any broken relative links (e.g., to local images) will not resolve | Informational | Acceptable; this is an existing upstream README |
