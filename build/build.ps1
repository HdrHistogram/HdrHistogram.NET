# The bones of this build script taken from the James Newton-King's JSON.NET project (https://github.com/JamesNK/Newtonsoft.Json/blob/81103079e241fb055af2e81c51cdd56c52410fbf/Build/build.ps1)
# They have then been heavily modified again based on Allan Hardy's (https://github.com/alhardy) work.
#  Requires 
#	dotnet cli tools
#    either NuGet v3 or higher on the path -AND- local caches of the NuGet package dependencies
#    -OR- a connection to the internet (to download NuGet and to restore packages)
#
#  To run, from a PowerShell console
#    PS .\HdrHistogram.NET> Import-Module .\build\psake.psm1
#    PS .\HdrHistogram.NET> Invoke-psake %~dp0.\build.ps1
#
#  Modifications to the origin script taken from JSON.NET project are:
#    -No dependency on 7Zip. So the binary is not checked into source control
#    -NuGet is not in source control. We look for an installed version, else download it
#    -Sandcastle documentation generation has been removed.
properties { 
  $semver = "1.0.0"
  $buildEnv = "local" #Or TeamCity, AppVeyor
  $baseDir  = resolve-path ..
  $sourceDir = "$baseDir\src"
  $testsDir = "$baseDir\test"
  $workingDir = "$baseDir\Working"
  $workingSourceDir = "$workingDir\src"
  $workingTestsDir = "$workingDir\test"
  $packableProjectDirectories = @("$workingSourceDir\HdrHistogram")
  $packageOutputDir = "$workingDir\Nuget"
  $testOutputPath = "$workingDir\NunitTestResults.xml"
  $localNugetPath = "$workingDir\nuget.exe"
  $nugetDependecies = "$workingDir\packages"
  $jsonlib= "$nugetDependecies\Newtonsoft.Json\lib\net45\Newtonsoft.Json.dll"
}

task default -depends Package

task Clean {
  Write-Host "Setting location to $baseDir"
  Set-Location $baseDir
  
  if (Test-Path -path $workingDir) {
    Write-Host "Deleting existing working directory $workingDir"
    
    Execute-Command -command { del $workingDir -Recurse -Force }
  }
  
  Write-Host "Creating working directory $workingDir"
  New-Item -Path $workingDir -ItemType Directory
}

task CreateWorkingDir -depends Clean {
  Write-Host "Copying source to working source directory $workingSourceDir"
  robocopy $sourceDir $workingSourceDir /MIR /NP /XD /NJH /NJS /NFL /NDL bin obj TestResults AppPackages $packageDirs .vs artifacts /XF *.suo *.user *.lock.json | Out-Default
  
  Write-Host "Copying tests to working test directory $workingTestsDir"
  robocopy $testsDir $workingTestsDir /MIR /NP /XD /NJH /NJS /NFL /NDL bin obj TestResults AppPackages $packageDirs .vs artifacts /XF *.suo *.user *.lock.json | Out-Default
  
  Write-Host "Copying HdrHistogram.snk to working directory"
  Copy-Item "$baseDir\HdrHistogram.snk" $workingDir
  
  Write-Host "Copying global.json to working directory"
  Copy-Item "$baseDir\global.json" $workingDir
}

task Patch -depends CreateWorkingDir { 
  Get-Nuget
  Get-JsonNet
  Write-Host -ForegroundColor Green "Patching semantic version number --> $semver"
  Write-Host
  Patch-Versions $workingSourceDir $semver
}

task Build -depends Patch { 
  $srcProjects = Get-ChildItem "$workingDir\src\**\project.json" | foreach { $_.FullName }
  $testProjects= Get-ChildItem "$workingDir\test\**\project.json" | foreach { $_.FullName }
  
  Set-Location $workingDir
  exec { dotnet restore }
  Set-Location $baseDir
  
  $srcProjects + $testProjects | foreach {
	exec { dotnet build "$_" --configuration Release }
  }
}

task Test -depends Build {
	Get-ChildItem "$workingDir\test\**\" | 
	foreach { $_.FullName }	| 
	foreach {
		Write-Output "Running tests for '$_'"
		exec { dotnet test "$_" "-result:$testOutputPath" }
	}
}

task Package -depends Build {
	$packableProjectDirectories | foreach {
		exec { dotnet pack "$_" --configuration Release -o "$packageOutputDir" }
	}
}

function Get-Nuget () {
  #Check for existence of Nuget.exe on path, if not there, download and install from nuget.org
  $currentNuget = (Get-Command "nuget.exe" -ErrorAction SilentlyContinue)
  if (($currentNuget -eq $null) -Or ($currentNuget.Version.Major -lt 3)) { 
    Write-Host "Unable to find suitable nuget.exe in your PATH" -ForegroundColor Green  
    if ((Get-Command $localNugetPath -ErrorAction SilentlyContinue) -eq $null) {
      Write-Host "Downloading nuget.exe locally" -ForegroundColor Green
        $nugetRemoteUrl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
        $webclient = New-Object System.Net.WebClient
        $webclient.DownloadFile($nugetRemoteUrl,$localNugetPath)
    } else {
      Write-Host "A copy already found at $localNugetPath" -ForegroundColor Green
    }
  } else {
    Write-Host "Using copy of nuget.exe found on PATH, but making local copy." -ForegroundColor Green
    Copy-Item -Path $currentNuget.Source -Destination $localNugetPath
  }
}

function Get-JsonNet() {
	& "$localNugetPath" install newtonsoft.json -Version 9.0.1 -ExcludeVersion -o $nugetDependecies
}

#HACK While we wait for `dotnet pack` to support setting the version, not just "bizzarely" the version-suffix. -LC
# https://github.com/dotnet/cli/issues/5568
function Patch-Versions ([string] $workingSourceDir, [string]$semver) {
	[Reflection.Assembly]::LoadFile($jsonlib) | out-null
	
	$packableProjectDirectories | foreach {
		Write-Host "Patching project.json"
		
		$json = (Get-Content "$_\project.json" | Out-String)
		$config = [Newtonsoft.Json.Linq.JObject]::Parse($json)
		$version = $config.Item("version").ToString()
		$config.Item("version") = New-Object -TypeName Newtonsoft.Json.Linq.JValue -ArgumentList "$semver"

		$config.ToString() | Out-File "$_\project.json"
		
		$after = (Get-Content "$_\project.json" | Out-String)
		Write-Host $after
	}
}

function Execute-Command ($command) {
  $currentRetry = 0
  $success = $false
  do {
    try {
      & $command
      $success = $true
    }
    catch [System.Exception] {
      if ($currentRetry -gt 5) {
          throw $_.Exception.ToString()
      } else {
          write-host "Retry $currentRetry"
          Start-Sleep -s 1
      }
      $currentRetry = $currentRetry + 1
    }
  } while (!$success)
}