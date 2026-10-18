param([ValidateSet('win-x64','win-arm64')][string]$Runtime = 'win-x64')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
dotnet publish "$projectRoot/windows/NotebookScanner/NotebookScanner.csproj" -c Release -r $Runtime --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false -o "$projectRoot/dist/$Runtime"
if ($LASTEXITCODE -ne 0) { throw 'Windows publish failed.' }
Copy-Item -LiteralPath "$projectRoot/LICENSES" -Destination "$projectRoot/dist/$Runtime/LICENSES" -Recurse -Force
Copy-Item -LiteralPath "$projectRoot/THIRD_PARTY_NOTICES.md" -Destination "$projectRoot/dist/$Runtime/THIRD_PARTY_NOTICES.md" -Force
Copy-Item -LiteralPath "$projectRoot/LICENSE" -Destination "$projectRoot/dist/$Runtime/LICENSE" -Force
Write-Host "Built executable in dist/$Runtime. No .NET installation is required on the receiving PC. This build is unsigned."
