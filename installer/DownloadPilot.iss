#define MyAppName "DownloadPilot"
#define MyAppVersion "0.2.3"
#define MyAppExeName "DownloadPilot.App.exe"

; Inno Setup script for DownloadPilot x64
[Setup]
AppId={{9C2E693E-0C7C-4D84-8A2C-6B3F8A9B9142}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher=DownloadPilot
AppPublisherURL=https://github.com/kratje050/Downloadpilot
AppSupportURL=https://github.com/kratje050/Downloadpilot/issues
AppUpdatesURL=https://github.com/kratje050/Downloadpilot/releases/latest
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=output
OutputBaseFilename=DownloadPilot-Setup-{#MyAppVersion}-x64
ArchitecturesInstallIn64BitMode=x64
Compression=lzma
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\DownloadPilot.App\Assets\DownloadPilot.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest

[Languages]
Name: "dutch"; MessagesFile: "compiler:Languages\Dutch.isl"

[Files]
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Bureaubladpictogram aanmaken"; GroupDescription: "Extra opties:"; Flags: unchecked
Name: "startup"; Description: "DownloadPilot automatisch starten met Windows"; GroupDescription: "Extra opties:"; Flags: unchecked

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "DownloadPilot starten"; Flags: nowait postinstall skipifsilent

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startup
