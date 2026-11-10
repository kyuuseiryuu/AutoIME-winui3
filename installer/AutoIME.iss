; Script generated for AutoIME WinUI 3 Installer
#define MyAppName "AutoIME"
#define MyAppVersion "1.0.4"
#define MyAppPublisher "邱成龍"
#define MyAppExeName "AutoIME.exe"

[Setup]
AppId={{5F8A2D74-9C1B-4E38-A785-2E19D6F34C89}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=..\dist-installer
OutputBaseFilename=AutoIME_Setup_v{#MyAppVersion}
SetupIconFile=..\src\AutoIME\Assets\AppIcon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=commandline dialog

[Languages]
Name: "chinesesimplified"; MessagesFile: "Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\dist\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// Close existing AutoIME instance before installing/uninstalling
function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;
  ShellExec('open', 'taskkill.exe', '/f /im AutoIME.exe', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode);
end;

function InitializeUninstall(): Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;
  ShellExec('open', 'taskkill.exe', '/f /im AutoIME.exe', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode);
end;
