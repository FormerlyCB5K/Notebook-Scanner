param([string]$SigningFolder = (Join-Path (Split-Path $PSScriptRoot -Parent) '.private-release'))
$ErrorActionPreference = 'Stop'
if (!$IsWindows) { throw 'This helper protects the signing folder using Windows access controls; run it on Windows.' }
if (!$env:JAVA_HOME) { throw 'Set JAVA_HOME to a JDK 17 installation first.' }
if (Test-Path -LiteralPath $SigningFolder) { throw 'Signing folder already exists. Reuse the existing key; this helper never overwrites one.' }
New-Item -ItemType Directory -Path $SigningFolder | Out-Null
$SigningFolder = (Resolve-Path -LiteralPath $SigningFolder).Path
$identity = [System.Security.Principal.WindowsIdentity]::GetCurrent().User
$acl = Get-Acl -LiteralPath $SigningFolder
$acl.SetAccessRuleProtection($true, $false)
$acl.AddAccessRule([System.Security.AccessControl.FileSystemAccessRule]::new($identity,'FullControl','ContainerInherit,ObjectInherit','None','Allow'))
Set-Acl -LiteralPath $SigningFolder -AclObject $acl
$password = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
[IO.File]::WriteAllText((Join-Path $SigningFolder 'release-password.txt'), $password)
$previousPassword = $env:NOTEBOOK_SIGNING_PASSWORD
try {
    $env:NOTEBOOK_SIGNING_PASSWORD = $password
    & "$env:JAVA_HOME/bin/keytool.exe" -genkeypair -noprompt -keystore (Join-Path $SigningFolder 'release.jks') -storetype PKCS12 -storepass:env NOTEBOOK_SIGNING_PASSWORD -keypass:env NOTEBOOK_SIGNING_PASSWORD -alias notebook-scanner -keyalg RSA -keysize 3072 -validity 10000 -dname 'CN=Notebook Scanner, O=Sebastian Lashmet'
    if ($LASTEXITCODE -ne 0) { throw 'Key generation failed. Preserve this folder and investigate; do not replace a distributed key.' }
    & "$env:JAVA_HOME/bin/keytool.exe" -exportcert -rfc -keystore (Join-Path $SigningFolder 'release.jks') -storepass:env NOTEBOOK_SIGNING_PASSWORD -alias notebook-scanner -file (Join-Path $SigningFolder 'release-certificate.pem')
    if ($LASTEXITCODE -ne 0) { throw 'Certificate export failed.' }
} finally { $env:NOTEBOOK_SIGNING_PASSWORD = $previousPassword }
@'
PRIVATE SIGNING FILES - DO NOT SEND WITH THE APP OR UPLOAD TO GITHUB

release.jks and release-password.txt are needed to sign future Android updates.
Keep both in a password manager with file attachments or another encrypted backup
outside this computer. The password file is plaintext inside this access-controlled
folder; copying the entire folder into an ordinary shared/cloud folder is unsafe.
The certificate PEM is public identity information, not the private key.
Do not delete or replace these files after sharing a signed build.
The project excludes this folder from Git and from the source/share packages.
'@ | Set-Content (Join-Path $SigningFolder 'BACKUP-INSTRUCTIONS.txt') -Encoding UTF8
Write-Host 'Release signing key created in the private folder. Arrange an independent encrypted backup before broad distribution.'
