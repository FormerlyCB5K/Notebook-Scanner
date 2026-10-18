$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$folder = Join-Path $projectRoot 'dist/v0.5/friends-and-family'
New-Item -ItemType Directory -Force $folder | Out-Null
$assets = @(
    @{Source='dist/v0.5/windows/NotebookScanner-0.5-Windows-Setup.exe'; Name='NotebookScanner-0.5-Windows-Setup.exe'},
    @{Source='dist/v0.5/android/NotebookScanner-0.5.apk'; Name='NotebookScanner-0.5.apk'},
    @{Source='docs/FRIENDS-AND-FAMILY.txt'; Name='START-HERE.txt'},
    @{Source='LICENSE'; Name='LICENSE'},
    @{Source='THIRD_PARTY_NOTICES.md'; Name='THIRD_PARTY_NOTICES.md'}
)
# Explicit allowlist: do not recursively collect the workspace or signing folder.
foreach ($asset in $assets) { Copy-Item -LiteralPath (Join-Path $projectRoot $asset.Source) -Destination (Join-Path $folder $asset.Name) -Force }
$noticeFolder = Join-Path $folder 'LICENSES'
New-Item -ItemType Directory -Force $noticeFolder | Out-Null
Get-ChildItem -LiteralPath (Join-Path $projectRoot 'LICENSES') -File | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $noticeFolder $_.Name) -Force }
$hashes = foreach ($name in @('NotebookScanner-0.5-Windows-Setup.exe','NotebookScanner-0.5.apk','START-HERE.txt')) {
    '{0}  {1}' -f (Get-FileHash -LiteralPath (Join-Path $folder $name) -Algorithm SHA256).Hash.ToLowerInvariant(),$name
}
Set-Content -LiteralPath (Join-Path $folder 'SHA256SUMS.txt') -Value $hashes -Encoding ASCII
# Compress only the allowlisted files, notices, and generated checksums.
$zipFiles = @($assets | ForEach-Object { Join-Path $folder $_.Name }) + @($noticeFolder, (Join-Path $folder 'SHA256SUMS.txt'))
Compress-Archive -LiteralPath $zipFiles -DestinationPath (Join-Path $projectRoot 'dist/v0.5/NotebookScanner-0.5-Friends-and-Family.zip') -Force
Write-Host 'Created local friends-and-family beta ZIP. Nothing has been published.'
