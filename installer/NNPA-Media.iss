#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

#ifndef SourceDir
  #define SourceDir "..\artifacts\release\NNPA-Media-" + AppVersion
#endif

#ifndef OutputDir
  #define OutputDir "..\artifacts\release"
#endif

#define AppName "NNPA Media"
#define AppPublisher "Phát An"
#define AppExeName "NNPA.exe"
#define DotNet48Release 528040

[Setup]
AppId={{8D8CB5E0-BD56-4B6B-B130-A7889A95FC5D}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\Programs\NNPA Media
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir={#OutputDir}
OutputBaseFilename=NNPA-Media-Setup-{#AppVersion}
SetupIconFile=..\NNPA\icon_1.ico
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x86compatible x64compatible
CloseApplications=yes
RestartApplications=no
VersionInfoVersion={#AppVersion}.0
VersionInfoCompany={#AppPublisher}
VersionInfoDescription=NNPA Media Installer
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}

[Languages]
Name: "vietnamese"; MessagesFile: "Languages\Vietnamese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Tạo biểu tượng ngoài màn hình"; GroupDescription: "Biểu tượng bổ sung:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Mở {#AppName}"; Flags: nowait postinstall skipifsilent

[Code]
function IsDotNet48Installed: Boolean;
var
  Release: Cardinal;
begin
  Result := RegQueryDWordValue(
    HKLM,
    'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full',
    'Release',
    Release) and (Release >= {#DotNet48Release});
end;

function InitializeSetup: Boolean;
begin
  Result := IsDotNet48Installed;
  if not Result then
    MsgBox(
      'NNPA Media yêu cầu Microsoft .NET Framework 4.8.' + #13#10 +
      'Hãy cài .NET Framework 4.8 rồi chạy lại bộ cài.',
      mbError,
      MB_OK);
end;
