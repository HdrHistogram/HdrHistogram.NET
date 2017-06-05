@ECHO OFF

if [%1]==[] (
 SET SemVer="1.0.0.0-local"
) ELSE (
 SET SemVer=%1
)

dotnet restore -v=q

dotnet build -v=q -c=Release /p:Version=%SemVer%

dotnet test .\HdrHistogram.UnitTests\HdrHistogram.UnitTests.csproj -v=q --no-build -c=Release

dotnet pack .\HdrHistogram\HdrHistogram.csproj --no-build --include-symbols -c=Release /p:Version=%SemVer%