$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$registration = 'HKCU:/Software/Microsoft/Windows/CurrentVersion/Uninstall/{B68369F0-5725-4ED0-89DE-291B9C046B11}_is1'
if (Test-Path $registration) { throw 'An installed copy exists; refusing to replace its registration.' }
$installFolder = [IO.Path]::GetFullPath((Join-Path $projectRoot ('.tools/install-check-' + [Guid]::NewGuid().ToString('N'))))
if (!$installFolder.StartsWith($projectRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Test install path escaped the workspace.' }
$shortcutPath = Join-Path ([Environment]::GetFolderPath('Programs')) 'Notebook Scanner.lnk'
$shortcutBackup = if (Test-Path -LiteralPath $shortcutPath) { [IO.File]::ReadAllBytes($shortcutPath) } else { $null }
$oldHash = if ($null -ne $shortcutBackup) { (Get-FileHash -LiteralPath $shortcutPath).Hash } else { '' }
$installer = Join-Path $projectRoot 'dist/v0.5/windows/NotebookScanner-0.5-Windows-Setup.exe'
$arguments = @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/NOICONS','/SKIPSHORTCUTS=1',('/DIR="' + $installFolder + '"'),('/LOG="' + $projectRoot + '/.tools/installer-check.log"'))
try {
    for ($attempt = 0; $attempt -lt 2; $attempt++) {
        $process = Start-Process -FilePath $installer -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
        if ($process.ExitCode -ne 0) { throw "Install/reinstall failed: $($process.ExitCode)" }
        if ((Get-FileHash "$installFolder/NotebookScanner.exe").Hash -ne (Get-FileHash "$projectRoot/dist/win-x64/NotebookScanner.exe").Hash) { throw 'Installed binary differs.' }
        if (!(Test-Path "$installFolder/LICENSE") -or !(Test-Path "$installFolder/unins000.exe")) { throw 'License or uninstaller missing.' }
        $newHash = if (Test-Path -LiteralPath $shortcutPath) { (Get-FileHash -LiteralPath $shortcutPath).Hash } else { '' }
        if ($oldHash -ne $newHash) { throw 'Existing shortcut changed.' }
    }
} finally {
    if (Test-Path "$installFolder/unins000.exe") {
        $process = Start-Process -FilePath "$installFolder/unins000.exe" -ArgumentList '/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART' -WindowStyle Hidden -PassThru -Wait
        if ($process.ExitCode -ne 0) { throw "Test uninstall failed: $($process.ExitCode)" }
    }
    if ($null -ne $shortcutBackup) {
        # Restore only if changed; leave a working hotkey untouched in the passing case.
        if (!(Test-Path $shortcutPath) -or (Get-FileHash $shortcutPath).Hash -ne $oldHash) { [IO.File]::WriteAllBytes($shortcutPath, $shortcutBackup) }
    }
}
if ((Test-Path "$installFolder/NotebookScanner.exe") -or (Test-Path $registration)) { throw 'Test installation was not fully removed.' }
$newHash = if (Test-Path $shortcutPath) { (Get-FileHash $shortcutPath).Hash } else { '' }
if ($oldHash -ne $newHash) { throw 'Shortcut differs after uninstall.' }
Write-Host 'PASS: per-user install/reinstall/uninstall, exact application binary, license/uninstaller, unchanged existing shortcut. No app launched.'
