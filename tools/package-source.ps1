$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$sourceFiles = @('README.md', 'THIRD_PARTY_NOTICES.md', 'global.json', '.gitignore') | ForEach-Object { Get-Item -LiteralPath (Join-Path $projectRoot $_) }
foreach ($folder in @('android', 'windows', 'tests', 'tools', 'docs', 'LICENSES', '.github')) {
    $sourceFiles += Get-ChildItem -LiteralPath (Join-Path $projectRoot $folder) -Recurse -Force -File | Where-Object {
        $_.FullName -notmatch '[\\/](bin|obj|build|\.gradle|\.idea)[\\/]' -and $_.Name -ne 'local.properties'
    }
}
New-Item -ItemType Directory -Force (Join-Path $projectRoot 'dist') | Out-Null
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archivePath = Join-Path $projectRoot 'dist/NotebookScanner-source.zip'
$archiveStream = [System.IO.File]::Open($archivePath, [System.IO.FileMode]::Create)
$archive = [System.IO.Compression.ZipArchive]::new($archiveStream, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in $sourceFiles) {
        $relativePath = $file.FullName.Substring($projectRoot.Length + 1).Replace('\', '/')
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $relativePath) | Out-Null
    }
} finally { $archive.Dispose(); $archiveStream.Dispose() }
Write-Host "Created dist/NotebookScanner-source.zip with $($sourceFiles.Count) source/reference files."
