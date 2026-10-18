#ifndef AppSource
  #error AppSource must point to the self-contained Windows publish folder
#endif
#ifndef OutputFolder
  #error OutputFolder must point to the installer output folder
#endif

[Setup]
AppId={{B68369F0-5725-4ED0-89DE-291B9C046B11}
AppName=Notebook Scanner
AppVersion=0.5.0
AppVerName=Notebook Scanner 0.5 beta
AppPublisher=Sebastian Lashmet
AppPublisherURL=https://github.com/FormerlyCB5K/Notebook-Scanner
DefaultDirName={localappdata}\Programs\Notebook Scanner
DefaultGroupName=Notebook Scanner
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
OutputDir={#OutputFolder}
OutputBaseFilename=NotebookScanner-0.5-Windows-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
LicenseFile={#AppSource}\LICENSE
UninstallDisplayIcon={app}\NotebookScanner.exe
CloseApplications=yes
RestartApplications=no
SetupLogging=yes

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Files]
Source: "{#AppSource}\NotebookScanner.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppSource}\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppSource}\THIRD_PARTY_NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppSource}\LICENSES\*"; DestDir: "{app}\LICENSES"; Flags: ignoreversion

[Icons]
Name: "{userprograms}\Notebook Scanner"; Filename: "{app}\NotebookScanner.exe"; WorkingDir: "{app}"; Check: ShouldCreateShortcuts
Name: "{userdesktop}\Notebook Scanner"; Filename: "{app}\NotebookScanner.exe"; WorkingDir: "{app}"; Tasks: desktopicon; Check: ShouldCreateShortcuts

[Run]
Filename: "{app}\NotebookScanner.exe"; Description: "Open Notebook Scanner"; Flags: nowait postinstall skipifsilent

[Code]
function ShouldCreateShortcuts: Boolean;
begin
  { Used only by the isolated install/uninstall check. Normal installs create shortcuts. }
  Result := ExpandConstant('{param:SkipShortcuts|0}') <> '1';
end;
