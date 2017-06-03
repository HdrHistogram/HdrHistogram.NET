SET SemVer="1.0.0.0-local"

pushd %~dp0%..\
	dotnet restore .\src\
	dotnet build .\src\ -c=Release /p:Version=%SemVer%
	dotnet test .\src\HdrHistogram.UnitTests\HdrHistogram.UnitTests.csproj /p:Configuration=Release
	dotnet pack .\src\HdrHistogram\HdrHistogram.csproj -c=Release --include-symbols /p:Version=%SemVer%
popd