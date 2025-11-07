#define MyAppName "HavenCNCServer"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Haven CNC"
#define MyAppURL "https://havencnc.com"
#define MyAppExeName "HavenCNCServer.exe"
#define MyServiceName "HavenCNCService"
#define MyServiceDisplayName "Haven CNC Server"

[Setup]
AppId={{B8F5E8A1-2C3D-4E5F-6789-ABCDEF123456}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=
InfoBeforeFile=
InfoAfterFile=
OutputDir=installer
OutputBaseFilename=HavenCNCServer-Setup-{#MyAppVersion}
SetupIconFile=
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1
Name: "installservice"; Description: "Install as Windows Service"; GroupDescription: "Service Options:"; Flags: unchecked
Name: "startservice"; Description: "Start service after installation"; GroupDescription: "Service Options:"; Flags: unchecked; Check: IsTaskSelected('installservice')

[Files]
; Application executable and core files
Source: "bin\Release\net8.0-windows\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net8.0-windows\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net8.0-windows\*.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net8.0-windows\*.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net8.0-windows\*.pdb"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

; Configuration files
Source: "appsettings.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "settings.json"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

; Web content and static files
Source: "wwwroot\*"; DestDir: "{app}\wwwroot"; Flags: ignoreversion recursesubdirs createallsubdirs

; Centroid API and related files
Source: "CentroidAPI.dll"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "Centriod\*"; DestDir: "{app}\Centriod"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist

; Documentation
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "Documentation\*"; DestDir: "{app}\Documentation"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist

; Runtime files (if self-contained)
Source: "bin\Release\net8.0-windows\runtimes\*"; DestDir: "{app}\runtimes"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
; Install and start service if selected
Filename: "sc"; Parameters: "create ""{#MyServiceName}"" binpath= ""{app}\{#MyAppExeName} --service"" displayname= ""{#MyServiceDisplayName}"" start= auto"; WorkingDir: "{app}"; Flags: runhidden waituntilterminated; Tasks: installservice; StatusMsg: "Installing Windows Service..."
Filename: "sc"; Parameters: "start ""{#MyServiceName}"""; Flags: runhidden waituntilterminated; Tasks: startservice; StatusMsg: "Starting service..."

; Configure firewall (optional)
Filename: "netsh"; Parameters: "advfirewall firewall add rule name=""Haven CNC Server HTTP"" dir=in action=allow protocol=TCP localport=5000"; Flags: runhidden waituntilterminated; StatusMsg: "Configuring firewall for HTTP access..."
Filename: "netsh"; Parameters: "advfirewall firewall add rule name=""Haven CNC Server HTTPS"" dir=in action=allow protocol=TCP localport=5001"; Flags: runhidden waituntilterminated; StatusMsg: "Configuring firewall for HTTPS access..."

; Launch application if not installing as service
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent; Check: not IsTaskSelected('installservice')

[UninstallRun]
; Stop and remove service if it was installed
Filename: "sc"; Parameters: "stop ""{#MyServiceName}"""; Flags: runhidden waituntilterminated; RunOnceId: "StopService"
Filename: "sc"; Parameters: "delete ""{#MyServiceName}"""; Flags: runhidden waituntilterminated; RunOnceId: "DeleteService"

; Remove firewall rules
Filename: "netsh"; Parameters: "advfirewall firewall delete rule name=""Haven CNC Server HTTP"""; Flags: runhidden waituntilterminated; RunOnceId: "RemoveHTTPFirewall"
Filename: "netsh"; Parameters: "advfirewall firewall delete rule name=""Haven CNC Server HTTPS"""; Flags: runhidden waituntilterminated; RunOnceId: "RemoveHTTPSFirewall"

[Code]
function GetUninstallString(): String;
var
  sUnInstPath: String;
  sUnInstallString: String;
begin
  sUnInstPath := ExpandConstant('Software\Microsoft\Windows\CurrentVersion\Uninstall\{#emit SetupSetting("AppId")}_is1');
  sUnInstallString := '';
  if not RegQueryStringValue(HKLM, sUnInstPath, 'UninstallString', sUnInstallString) then
    RegQueryStringValue(HKCU, sUnInstPath, 'UninstallString', sUnInstallString);
  Result := sUnInstallString;
end;

function IsUpgrade(): Boolean;
begin
  Result := (GetUninstallString() <> '');
end;

function UnInstallOldVersion(): Integer;
var
  sUnInstallString: String;
  iResultCode: Integer;
begin
  Result := 0;
  sUnInstallString := GetUninstallString();
  if sUnInstallString <> '' then begin
    sUnInstallString := RemoveQuotes(sUnInstallString);
    if Exec(sUnInstallString, '/SILENT /NORESTART /SUPPRESSMSGBOXES','', SW_HIDE, ewWaitUntilTerminated, iResultCode) then
      Result := 3
    else
      Result := 2;
  end else
    Result := 1;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep=ssInstall) then
  begin
    if (IsUpgrade()) then
    begin
      UnInstallOldVersion();
    end;
  end;
end;

function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin
  // Check if .NET 8 Runtime is installed
  if not RegKeyExists(HKEY_LOCAL_MACHINE, 'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\Microsoft.WindowsDesktop.App') and
     not RegKeyExists(HKEY_LOCAL_MACHINE, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\Microsoft.WindowsDesktop.App') then
  begin
    if MsgBox('.NET 8 Desktop Runtime is required but not found. Would you like to download it now?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/8.0', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
    end;
    Result := False;
  end else
    Result := True;
end;