#ifndef AppVersion
  #define AppVersion "0.15.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\dist"
#endif
#ifndef OutputDir
  #define OutputDir "..\artifacts"
#endif
[Setup]
AppId={{E03C0A99-33D7-4403-93D6-A671031C98B1}
AppName=Capture Studio
AppVersion={#AppVersion}
AppPublisher=nferon
AppPublisherURL=https://github.com/nferon/capture-studio
AppSupportURL=https://github.com/nferon/capture-studio/issues
AppUpdatesURL=https://github.com/nferon/capture-studio/releases
DefaultDirName={localappdata}\Programs\CaptureStudio
DefaultGroupName=Capture Studio
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
MinVersion=10.0.17763
OutputDir={#OutputDir}
OutputBaseFilename=CaptureStudio-Setup-{#AppVersion}
SetupIconFile={#SourceDir}\CaptureStudio.ico
UninstallDisplayIcon={app}\CaptureStudio.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=no
RestartApplications=no
SignedUninstaller=no

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Files]
Source: "{#SourceDir}\CaptureStudio.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\CaptureStudio.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\CaptureStudio.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\ocr.ps1"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\README.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\Capture Studio"; Filename: "{app}\CaptureStudio.exe"; WorkingDir: "{app}"; AppUserModelID: "Botaneo.CaptureStudio.Desktop"

[Registry]
Root: HKCU; Subkey: "Software\Classes\CaptureStudio.Project"; ValueType: string; ValueData: "Projet Capture Studio"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\CaptureStudio.Project\DefaultIcon"; ValueType: string; ValueData: """{app}\CaptureStudio.exe"",0"
Root: HKCU; Subkey: "Software\Classes\CaptureStudio.Project\shell\open\command"; ValueType: string; ValueData: """{app}\CaptureStudio.exe"" ""%1"""
Root: HKCU; Subkey: "Software\Classes\.capture\OpenWithProgids"; ValueType: string; ValueName: "CaptureStudio.Project"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\CaptureStudio\Capabilities"; ValueType: string; ValueName: "ApplicationName"; ValueData: "Capture Studio"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\CaptureStudio\Capabilities\FileAssociations"; ValueType: string; ValueName: ".capture"; ValueData: "CaptureStudio.Project"
Root: HKCU; Subkey: "Software\RegisteredApplications"; ValueType: string; ValueName: "CaptureStudio"; ValueData: "Software\CaptureStudio\Capabilities"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "CaptureStudio"; ValueData: """{app}\CaptureStudio.exe"" --background"; Check: StartupAlreadyEnabled

[Run]
Filename: "{app}\CaptureStudio.exe"; Description: "Ouvrir Capture Studio"; Flags: nowait postinstall skipifsilent
Filename: "{app}\CaptureStudio.exe"; Parameters: "--resume-update"; Flags: nowait; Check: RestartRequested

[Code]
function OpenProcess(Access: LongWord; Inherit: Boolean; ProcessId: LongWord): THandle;
  external 'OpenProcess@kernel32.dll stdcall';
function WaitForSingleObject(Handle: THandle; Milliseconds: LongWord): LongWord;
  external 'WaitForSingleObject@kernel32.dll stdcall';
function CloseHandle(Handle: THandle): Boolean;
  external 'CloseHandle@kernel32.dll stdcall';
function GetCurrentProcessId: LongWord;
  external 'GetCurrentProcessId@kernel32.dll stdcall';
function ProcessIdToSessionId(ProcessId: LongWord; var SessionId: LongWord): Boolean;
  external 'ProcessIdToSessionId@kernel32.dll stdcall';

function StartupAlreadyEnabled: Boolean;
begin
  Result := RegValueExists(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'CaptureStudio');
end;

function RestartRequested: Boolean;
begin
  Result := ExpandConstant('{param:RESTARTAPP|0}') = '1';
end;

function InitializeSetup: Boolean;
var
  ProcessId: Integer;
  Handle: THandle;
  SessionId: LongWord;
  Locator, Services, Processes: Variant;
  Runtime: Cardinal;
begin
  Result := False;
  if (not RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Runtime)) or (Runtime < 528040) then begin
    MsgBox('Capture Studio nécessite .NET Framework 4.8. Installez-le depuis Microsoft puis relancez cet installateur.', mbError, MB_OK);
    Exit;
  end;
  ProcessId := StrToIntDef(ExpandConstant('{param:WAITPID|0}'), 0);
  if ProcessId > 0 then begin
    Handle := OpenProcess($00100000, False, ProcessId);
    if Handle <> 0 then begin
      WaitForSingleObject(Handle, 30000);
      CloseHandle(Handle);
    end;
  end;
  try
    if not ProcessIdToSessionId(GetCurrentProcessId, SessionId) then begin
      MsgBox('Impossible de vérifier la session Windows. Réessayez après avoir quitté Capture Studio.', mbError, MB_OK);
      Exit;
    end;
    Locator := CreateOleObject('WbemScripting.SWbemLocator');
    Services := Locator.ConnectServer('.', 'root\CIMV2');
    Processes := Services.ExecQuery('SELECT ProcessId FROM Win32_Process WHERE Name = ''CaptureStudio.exe'' AND SessionId = ' + IntToStr(SessionId));
    if Processes.Count > 0 then begin
      MsgBox('Capture Studio est encore actif. Enregistrez vos projets puis choisissez Quitter depuis son icône près de l''horloge, et relancez l''installation.', mbInformation, MB_OK);
      Exit;
    end;
  except
    MsgBox('Impossible de vérifier si Capture Studio est fermé. Réessayez après avoir quitté l''application.', mbError, MB_OK);
    Exit;
  end;
  Result := True;
end;

function InitializeUninstall: Boolean;
var
  SessionId: LongWord;
  Locator, Services, Processes: Variant;
begin
  Result := False;
  try
    if not ProcessIdToSessionId(GetCurrentProcessId, SessionId) then Exit;
    Locator := CreateOleObject('WbemScripting.SWbemLocator');
    Services := Locator.ConnectServer('.', 'root\CIMV2');
    Processes := Services.ExecQuery('SELECT ProcessId FROM Win32_Process WHERE Name = ''CaptureStudio.exe'' AND SessionId = ' + IntToStr(SessionId));
    if Processes.Count > 0 then begin
      MsgBox('Enregistrez vos projets puis quittez Capture Studio depuis son icône près de l''horloge avant de désinstaller.', mbInformation, MB_OK);
      Exit;
    end;
    Result := True;
  except
    MsgBox('Impossible de vérifier si Capture Studio est fermé. Réessayez après avoir quitté l''application.', mbError, MB_OK);
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var Value: String;
begin
  if CurUninstallStep = usUninstall then begin
    if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'CaptureStudio', Value) then
      if CompareText(Value, ExpandConstant('"{app}\CaptureStudio.exe" --background')) = 0 then
        RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'CaptureStudio');
  end;
end;
