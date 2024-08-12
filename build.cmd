@ECHO OFF

if [%1]==[] (
 SET SemVer="1.0.0.0-local"
) ELSE (
 SET SemVer=%1
)

dotnet restore -v=q
IF %ERRORLEVEL% NEQ 0 GOTO EOF

dotnet build -v=q -c=Release /p:Version=%SemVer%
IF %ERRORLEVEL% NEQ 0 GOTO EOF

dotnet test .\HdrHistogram.UnitTests\HdrHistogram.UnitTests.csproj -v=q --no-build -c=Release
IF %ERRORLEVEL% NEQ 0 GOTO EOF

dotnet pack .\HdrHistogram\HdrHistogram.csproj --no-build --include-symbols -c=Release /p:Version=%SemVer%
IF %ERRORLEVEL% NEQ 0 GOTO EOF

.\HdrHistogram.Benchmarking\bin\Release\net8.0\HdrHistogram.Benchmarking.exe *