param([string]$Compiler = (Join-Path (Split-Path $PSScriptRoot -Parent) '.tools/inno/ISCC.exe'), [switch]$SkipPublish)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
if (!(Test-Path -LiteralPath $Compiler)) { throw 'Install Inno Setup 6.7.3+ on the build machine and pass -Compiler with its ISCC.exe path. End users do not need it.' }
if (!$SkipPublish) { & "$PSScriptRoot/publish-windows.ps1" }
$appSource = Join-Path $projectRoot 'dist/win-x64'
if (!(Test-Path "$appSource/NotebookScanner.exe")) { throw 'Publish the Windows app first.' }
$outputFolder = Join-Path $projectRoot 'dist/v0.5/windows'
& $Compiler "/DAppSource=$appSource" "/DOutputFolder=$outputFolder" "$PSScriptRoot/NotebookScanner.iss"
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
