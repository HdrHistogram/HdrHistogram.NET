# The bones of this build script taken from the James Newton-King's JSON.NET project (https://github.com/JamesNK/Newtonsoft.Json/blob/81103079e241fb055af2e81c51cdd56c52410fbf/Build/build.ps1)
#  Requires PowerShell 5 (https://www.microsoft.com/en-us/download/details.aspx?id=50395)
#  Requires 
#    either NuGet v3 or higher on the path -AND- local caches of the NuGet package dependencies
#    -OR- a connection to the internet (to download NuGet and to restore packages)
#
#  To run, from a PowerShell console
#    PS .\HdrHistogram.NET> Import-Module .\build\psake.psm
#    PS .\HdrHistogram.NET> Invoke-psake %~dp0.\build.ps1
#
#  Modifications to the origin script taken from JSON.NET project are:
#    -No dependency on 7Zip. So the binary is not checked into source control
#    -NuGet is not in source control. We look for an installed version, else download it
#    -Currently no support for dotNetCore so no dependency on KVM (which I think is deprecated now anyway, in favor of dotnet cli)
#    -Only a semver* version property is set. Other Versions (assembly, file, NuGet) are inferred from this. *semver==Semantic Version see http://semver.org/
#    -Requires PS version 5 to extract and compress archives.
#    -Sandcastle documentation generation has been removed.
properties { 
  $semver = "1.0.0-beta"
  $zipFileName = "HdrHistogram.NET$semver.zip"
  $packageId = "HdrHistogram"
  $signAssemblies = $false
  $signKeyPath = "C:\Development\Releases\HdrHistogram.snk.pfx"
  $buildNuGet = $true
  $treatWarningsAsErrors = $false
  $baseDir  = resolve-path ..
  $buildDir = "$baseDir\Build"
  $sourceDir = "$baseDir\Src"
  $docDir = "$baseDir\Doc"
  $releaseDir = "$baseDir\Release"
  $workingDir = "$baseDir\Working"
  $workingSourceDir = "$workingDir\Src"
  $builds = @(
     @{Name = "HdrHistogram"; TestsName = "HdrHistogram.UnitTests"; BuildFunction = "MSBuildBuild"; TestsFunction = "NUnitTests"; Constants=""; FinalDir="Net45"; NuGetDir = "net45"; Framework="net-4.0"}
  )
}

framework '4.6x86'

task default -depends Test

task VerifyDependencies {
  if($PSVersionTable.PSVersion.Major -lt 5) {
    #Compress-Archive and Expand-Archive are PS5 feature. Means we don't need a binary dependency on 7Zip or have to hand code .NET or Shell compression.
    Write-Error "This build script requires PowerShell 5 or greater"
  }
}

# Ensure a clean working directory
task Clean -depends VerifyDependencies {
  Write-Host "Setting location to $baseDir"
  Set-Location $baseDir
  
  if (Test-Path -path $workingDir) {
    Write-Host "Deleting existing working directory $workingDir"
    
    Execute-Command -command { del $workingDir -Recurse -Force }
  }
  
  Write-Host "Creating working directory $workingDir"
  New-Item -Path $workingDir -ItemType Directory
  
  GetNuget
}

# Build each solution, optionally signed
task Build -depends Clean { 
  Write-Host "Copying source to working source directory $workingSourceDir"
  robocopy $sourceDir $workingSourceDir /MIR /NP /XD bin obj TestResults AppPackages $packageDirs .vs artifacts /XF *.suo *.user *.lock.json | Out-Default

  Write-Host -ForegroundColor Green "Updating assembly version"
  Write-Host
  Update-AssemblyInfoFiles $workingSourceDir $semver
  
  foreach ($build in $builds) {
    $name = $build.Name
    if ($name -ne $null) {
      Write-Host -ForegroundColor Green "Building " $name
      Write-Host -ForegroundColor Green "Signed " $signAssemblies
      Write-Host -ForegroundColor Green "Key " $signKeyPath

      & $build.BuildFunction $build
    }
  }
}

# Optional build documentation, add files to final zip
task Package -depends Build {
  foreach ($build in $builds) {
    $name = $build.TestsName
    $finalDir = $build.FinalDir
    
    robocopy "$workingSourceDir\HdrHistogram\bin\Release\$finalDir" $workingDir\Package\Bin\$finalDir *.dll *.pdb *.xml /NFL /NDL /NJS /NC /NS /NP /XO /XF *.CodeAnalysisLog.xml | Out-Default
  }
  
  if ($buildNuGet) {
    New-Item -Path $workingDir\NuGet -ItemType Directory

    $nuspecPath = "$workingDir\NuGet\HdrHistogram.nuspec"
    Copy-Item -Path "$buildDir\HdrHistogram.nuspec" -Destination $nuspecPath -recurse

    Write-Host "Updating nuspec file at $nuspecPath" -ForegroundColor Green
    Write-Host

    $xml = [xml](Get-Content $nuspecPath)
    Edit-XmlNodes -doc $xml -xpath "//*[local-name() = 'id']" -value $packageId
    Edit-XmlNodes -doc $xml -xpath "//*[local-name() = 'version']" -value $semver

    Write-Host $xml.OuterXml

    $xml.save($nuspecPath)
    
    Write-Host "Copying build artefacts to NuGet target structure" -ForegroundColor Green
    foreach ($build in $builds) {
      if ($build.NuGetDir) {
        $name = $build.TestsName
        $finalDir = $build.FinalDir
        $frameworkDirs = $build.NuGetDir.Split(",")
        
        foreach ($frameworkDir in $frameworkDirs) {
          $artefactSource = "$workingSourceDir\HdrHistogram\bin\Release\$finalDir"
          $artefactTarget = "$workingDir\NuGet\lib\$frameworkDir"
      
          Write-Host "Copying build artefacts from '$artefactSource' to '$artefactTarget'" -ForegroundColor Green
      
          robocopy $artefactSource $artefactTarget *.dll *.pdb *.xml /NFL /NDL /NJS /NC /NS /NP /XO /XF *.CodeAnalysisLog.xml | Out-Default
        }
      }
    }
  
    robocopy $workingSourceDir $workingDir\NuGet\src *.cs /S /NFL /NDL /NJS /NC /NS /NP /XD obj .vs artifacts | Out-Default

    Write-Host "Building NuGet package with ID $packageId and version $semver from '$nuspecPath'" -ForegroundColor Green
    Write-Host
    Write-Host "Using NuGet from $nugetPath"

    exec { .\working\nuget.exe pack $nuspecPath -Symbols } "Error packing $nuspecPath"
    move -Path .\*.nupkg -Destination $workingDir\NuGet
  }
  
  Copy-Item -Path $baseDir\readme.md -Destination $workingDir\Package\
  Copy-Item -Path $baseDir\license.txt -Destination $workingDir\Package\

  robocopy $workingSourceDir $workingDir\Package\Source\Src /MIR /NFL /NDL /NJS /NC /NS /NP /XD bin obj TestResults AppPackages .vs artifacts /XF *.suo *.user *.lock.json | Out-Default
  robocopy $buildDir $workingDir\Package\Source\Build /MIR /NFL /NDL /NJS /NC /NS /NP /XF runbuild.txt | Out-Default
  robocopy $docDir $workingDir\Package\Source\Doc /MIR /NFL /NDL /NJS /NC /NS /NP | Out-Default
  
  Compress-Archive -Path "$workingDir\Package\*" -DestinationPath "$workingDir\$zipFileName"
}

# Unzip package to a location
task Deploy -depends Package {
  Expand-Archive -Path "$workingDir\$zipFileName" -DestinationPath "$workingDir\Deployed"
}

# Run tests on deployed files
task Test -depends Deploy {
  foreach ($build in $builds) {
    if ($build.TestsFunction -ne $null) {
      & $build.TestsFunction $build
    }
  }
}

function MSBuildBuild ($build) {
  $name = $build.Name
  $finalDir = $build.FinalDir
  
  Write-Host
  Write-Host "Restoring $workingSourceDir\$name.sln" -ForegroundColor Green
  [Environment]::SetEnvironmentVariable("EnableNuGetPackageRestore", "true", "Process")
  exec { .\working\nuget.exe restore "$workingSourceDir\$name.sln" `
    -verbosity detailed `
    -configfile $workingSourceDir\nuget.config `
    | Out-Default 
  } "Error restoring $name"

  $constants = GetConstants $build.Constants $signAssemblies

  Write-Host
  Write-Host "Building $workingSourceDir\$name.sln" -ForegroundColor Green
  exec { msbuild "/t:Clean;Rebuild" `
    /p:Configuration=Release `
    "/p:CopyNuGetImplementations=true" `
    "/p:Platform=Any CPU" `
    "/p:PlatformTarget=AnyCPU" `
    /p:OutputPath=bin\Release\$finalDir\ `
    /p:AssemblyOriginatorKeyFile=$signKeyPath `
    "/p:SignAssembly=$signAssemblies" `
    "/p:TreatWarningsAsErrors=$treatWarningsAsErrors" `
    "/p:VisualStudioVersion=14.0" `
    /p:DefineConstants=`"$constants`" `
    "$workingSourceDir\$name.sln" `
    | Out-Default 
  } "Error building $name"
}

function GetNuget () {
  $localNugetPath = "$workingDir\nuget.exe"
  #Check for existence of Nuget.exe on path, if not there, download and install from nuget.org
  $currentNuget = (Get-Command "nuget.exe" -ErrorAction SilentlyContinue)
  if (($currentNuget -eq $null) -Or ($currentNuget.Version.Major -le 3)) { 
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

function NUnitTests ($build) {
  $name = $build.TestsName
  $finalDir = $build.FinalDir
  $framework = $build.Framework

  exec { .\working\nuget.exe install NUnit.ConsoleRunner -version 3.2.0 -OutputDirectory $workingSourceDir\packages }
  
  Write-Host -ForegroundColor Green "Copying test assembly $name to deployed directory"
  Write-Host
  robocopy "$workingSourceDir\HdrHistogram.UnitTests\bin\Release\$finalDir" $workingDir\Deployed\Bin\$finalDir /MIR /NFL /NDL /NJS /NC /NS /NP /XO | Out-Default

  Copy-Item -Path "$workingSourceDir\HdrHistogram.UnitTests\bin\Release\$finalDir\HdrHistogram.UnitTests.dll" -Destination $workingDir\Deployed\Bin\$finalDir\

  Write-Host -ForegroundColor Green "Running NUnit tests " $name
  Write-Host
  $nUnitPath = "$workingSourceDir\packages\NUnit.ConsoleRunner.3.2.0\tools\nunit3-console.exe"  
  exec { .\working\src\packages\NUnit.ConsoleRunner.3.2.0\tools\nunit3-console.exe `
    $workingDir\Deployed\Bin\$finalDir\HdrHistogram.UnitTests.dll `
    --framework=$framework `
    --teamcity `
    | Out-Default 
  } "Error running $name tests"
}

function GetConstants ($constants, $includeSigned) {
  $signed = switch($includeSigned) { $true { ";SIGNED" } default { "" } }

  return "CODE_ANALYSIS;TRACE;$constants$signed"
}

function Update-AssemblyInfoFiles ([string] $workingSourceDir, [string]$semver) {
  $majorMinor = GetMajorMinor($semver)
  $majorMinorPatch = GetMajorMinorPatch($semver)
  $assemblyVersionNumber = "$majorMinor.0.0"
  $fileVersionNumber = "$majorMinorPatch.0"
  $assemblyVersionPattern = 'AssemblyVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)'
  $fileVersionPattern = 'AssemblyFileVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)'
  $assemblyVersion = 'AssemblyVersion("' + $assemblyVersionNumber + '")';
  $fileVersion = 'AssemblyFileVersion("' + $fileVersionNumber + '")';
    
  Get-ChildItem -Path $workingSourceDir -r -filter AssemblyInfo.cs | ForEach-Object {
    $filename = $_.Directory.ToString() + '\' + $_.Name
    Write-Host $filename + ' -> ' + $fileVersionNumber
    
    (Get-Content $filename) | ForEach-Object {
        % {$_ -replace $assemblyVersionPattern, $assemblyVersion } |
        % {$_ -replace $fileVersionPattern, $fileVersion }
    } | Set-Content $filename
  }
}

function Edit-XmlNodes {
  param (
      [xml] $doc,
      [string] $xpath = $(throw "xpath is a required parameter"),
      [string] $value = $(throw "value is a required parameter")
  )
    
  $nodes = $doc.SelectNodes($xpath)
  $count = $nodes.Count

  Write-Host "Found $count nodes with path '$xpath'"
    
  foreach ($node in $nodes) {
    if ($node -ne $null) {
      if ($node.NodeType -eq "Element") {
          $node.InnerXml = $value
      } else {
          $node.Value = $value
      }
    }
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

function GetMajorMinorPatch ([string] $semver) {
  $semverPattern = '(?<major>[0-9]+)\.(?<minor>[0-9]+)\.(?<patch>[0-9]+)(-(?<label>.*))?'
  $matches = [regex]::matches($semver, $semverPattern)
  $major = $matches.captures.groups['major'].Value;
  $minor = $matches.captures.groups['minor'].Value;
  $patch = $matches.captures.groups['patch'].Value;
  return "$major.$minor.$patch";
}

function GetMajorMinor ([string] $semver) {
  $semverPattern = '(?<major>[0-9]+)\.(?<minor>[0-9]+)\.(?<patch>[0-9]+)(-(?<label>.*))?'
  $matches = [regex]::matches($semver, $semverPattern)
  $major = $matches.captures.groups['major'].Value;
  $minor = $matches.captures.groups['minor'].Value;
  return "$major.$minor";
}