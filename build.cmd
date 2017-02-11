@echo off
cd %~dp0

:: Get Psake to Return Non-Zero Return Code on Build Failure (https://github.com/psake/psake/issues/58)
@powershell -NoProfile -ExecutionPolicy unrestricted -command "&{ Import-Module .\build\psake.psm1;	Invoke-Psake .\build\build.ps1 -taskList Test,Package -properties @{'semver'='1.0.0-local'} ; exit !($psake.build_success) }"