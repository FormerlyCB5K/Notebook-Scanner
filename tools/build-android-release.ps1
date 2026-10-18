param([string]$SigningFolder = (Join-Path (Split-Path $PSScriptRoot -Parent) '.private-release'))
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$store = Join-Path $SigningFolder 'release.jks'
$passwordFile = Join-Path $SigningFolder 'release-password.txt'
if (!(Test-Path -LiteralPath $store) -or !(Test-Path -LiteralPath $passwordFile)) { throw 'Private signing files are missing. Restore your existing signing backup, or generate a key only if no release has ever been signed.' }
$previousStore = $env:NOTEBOOK_SIGNING_STORE
$previousPassword = $env:NOTEBOOK_SIGNING_PASSWORD
try {
    $env:NOTEBOOK_SIGNING_STORE = (Resolve-Path -LiteralPath $store).Path
    $env:NOTEBOOK_SIGNING_PASSWORD = [IO.File]::ReadAllText($passwordFile).Trim()
    & "$projectRoot/android/gradlew.bat" -p "$projectRoot/android" assembleRelease lintRelease --no-daemon
    if ($LASTEXITCODE -ne 0) { throw 'Android release build/lint failed.' }
    New-Item -ItemType Directory -Force "$projectRoot/dist/v0.5/android" | Out-Null
    Copy-Item "$projectRoot/android/app/build/outputs/apk/release/app-release.apk" "$projectRoot/dist/v0.5/android/NotebookScanner-0.5.apk" -Force
} finally {
    $env:NOTEBOOK_SIGNING_STORE = $previousStore
    $env:NOTEBOOK_SIGNING_PASSWORD = $previousPassword
}
