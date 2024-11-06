; Ocean-Browser installer script

; Dep checker
#define public Dependency_Path_NetCoreCheck "deps\"
#include "inc\CodeDependencies.iss"

; Variables
#define MyAppName "Ocean-Browser"
#define MyAppVersion "0.0.7"
#define MyAppVerName "{#MyAppName} {#MyAppVersion}"
#define MyAppPublisher "BracketProto"
#define MyAppURL "https://bracketproto.com"
#define CanonicalName 'OceanBrowser.exe'
#define MainEXE "OceanBrowser.exe"
#define RegistryName 'OceanBrowser'
#define ProgId 'OceanBrowserHTML'
#define ProgHash '{{BROWSEREXEHASH}}'
#define SYSARCH '{{SYSARCH}}'

[Setup]
AppId={{623D1E9E-B542-4AA3-BAFB-EE6A535BBFF1}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={localappdata}\{#MyAppPublisher}\{#MyAppName}
DisableDirPage=yes
DefaultGroupName={#MyAppPublisher}\{#MyAppName}
OutputDir=..\
OutputBaseFilename="{#MyAppName}_v{#MyAppVersion}_installer"
UninstallDisplayName={#MyAppName}
SetupIconFile=inc\img\WindowsPackageManager.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
DisableWelcomePage=false
WizardImageFile=inc\img\wizardImage-1.bmp
ArchitecturesInstallIn64BitMode=x64compatible or arm64
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Registry]
; Register capabilities section, basic info
Root: HKLM; Subkey: "Software\{#RegistryName}\Capabilities"; ValueType: string; ValueName: "ApplicationDescription"; ValueData: "{#RegistryName}"; Flags: uninsdeletekey createvalueifdoesntexist
Root: HKLM; Subkey: "Software\{#RegistryName}\Capabilities"; ValueType: string; ValueName: "ApplicationIcon"; ValueData: "{app}\{#MainEXE},0"; Flags: uninsdeletekey createvalueifdoesntexist
Root: HKLM; Subkey: "Software\{#RegistryName}\Capabilities"; ValueType: string; ValueName: "ApplicationName"; ValueData: "{#RegistryName}"; Flags: uninsdeletekey createvalueifdoesntexist

; Register app file associations
Root: HKLM; Subkey: "Software\{#RegistryName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".htm"; ValueData: "{#ProgId}"; Flags: uninsdeletevalue createvalueifdoesntexist
Root: HKLM; Subkey: "Software\{#RegistryName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".html"; ValueData: "{#ProgId}"; Flags: uninsdeletevalue createvalueifdoesntexist
Root: HKLM; Subkey: "Software\{#RegistryName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".shtml"; ValueData: "{#ProgId}"; Flags: uninsdeletevalue createvalueifdoesntexist
Root: HKLM; Subkey: "Software\{#RegistryName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".xht"; ValueData: "{#ProgId}"; Flags: uninsdeletevalue createvalueifdoesntexist
Root: HKLM; Subkey: "Software\{#RegistryName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".xhtml"; ValueData: "{#ProgId}"; Flags: uninsdeletevalue createvalueifdoesntexist

; Register app url associations
Root: HKLM; Subkey: "Software\{#RegistryName}\Capabilities\URLAssociations"; ValueType: string; ValueName: "http"; ValueData: "{#ProgId}"; Flags: uninsdeletevalue createvalueifdoesntexist
Root: HKLM; Subkey: "Software\{#RegistryName}\Capabilities\URLAssociations"; ValueType: string; ValueName: "https"; ValueData: "{#ProgId}"; Flags: uninsdeletevalue createvalueifdoesntexist

; Register to Default Programs
Root: HKLM; Subkey: "Software\RegisteredApplications"; ValueType: string; ValueName: "{#RegistryName}"; ValueData: "Software\{#RegistryName}\Capabilities"; Flags: uninsdeletekey createvalueifdoesntexist

; Register app url handler
Root: HKLM; Subkey: "Software\Classes\{#ProgId}"; ValueType: string; ValueName: ""; ValueData: "{#RegistryName}"; Flags: uninsdeletekey createvalueifdoesntexist
Root: HKLM; Subkey: "Software\Classes\{#ProgId}\FriendlyTypeName"; ValueType: string; ValueName: ""; ValueData: "{#RegistryName} Document"; Flags: uninsdeletekey createvalueifdoesntexist
Root: HKLM; Subkey: "Software\Classes\{#ProgId}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MainEXE}"" ""%1"""; Flags: uninsdeletevalue createvalueifdoesntexist

; Register application under the client type
Root: HKLM; Subkey: "Software\Clients\StartMenuInternet\{#CanonicalName}"; ValueType: string; ValueName: ""; ValueData: "{#CanonicalName}"; Flags: uninsdeletekey createvalueifdoesntexist

; Setting Default Command for Open With Dialog
Root: HKCR; Subkey: "Applications\{#ProgId}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MainEXE}"" ""%1"""; Flags: uninsdeletevalue createvalueifdoesntexist

; Register handler for .html files
Root: HKCR; Subkey: ".html"; ValueType: string; ValueName: ""; ValueData: "{#ProgId}"; Flags: uninsdeletevalue createvalueifdoesntexist
Root: HKCR; Subkey: "{#ProgId}"; ValueType: string; ValueName: ""; ValueData: "HTML Document"; Flags: uninsdeletekey createvalueifdoesntexist
Root: HKCR; Subkey: "{#ProgId}\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MainEXE},0"; Flags: uninsdeletevalue createvalueifdoesntexist
Root: HKCR; Subkey: "{#ProgId}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MainEXE}"" ""%1"""; Flags: uninsdeletevalue createvalueifdoesntexist

; Register handler for .htm files
Root: HKCR; Subkey: ".htm"; ValueType: string; ValueName: ""; ValueData: "{#ProgId}"; Flags: uninsdeletevalue createvalueifdoesntexist
Root: HKCR; Subkey: "{#ProgId}\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MainEXE},0"; Flags: uninsdeletevalue createvalueifdoesntexist
Root: HKCR; Subkey: "{#ProgId}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MainEXE}"" ""%1"""; Flags: uninsdeletevalue createvalueifdoesntexist

; Register handler for http URLs and be set as the default browser
Root: HKCU; Subkey: "Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice"; ValueType: string; ValueName: "Progid"; ValueData: "{#ProgId}"; Flags: uninsdeletekey createvalueifdoesntexist
Root: HKCU; Subkey: "Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice"; ValueType: string; ValueName: "Hash"; ValueData: "{#ProgHash}"; Flags: uninsdeletekey createvalueifdoesntexist

[Files]
; Setup Images
Source: "inc\img\splash.bmp"; DestDir: "{tmp}"; Flags: dontcopy
Source: "inc\img\logo.bmp"; DestDir: "{tmp}"; Flags: dontcopy
Source: "inc\img\BracketProto_small.bmp"; DestDir: "{tmp}"; Flags: dontcopy

; Stuff to include
Source: "..\Browser\Build\OceanBrowser\net472\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "inc\img\WindowsPackageManager.ico"; DestDir: "{app}"; DestName: "WinGet-Logo.ico"; Flags: ignoreversion

[Icons]
Name: "{userprograms}\{#MyAppName}"; Filename: "{app}\{#MainEXE}"; IconFilename: "{app}\WinGet-Logo.ico"; WorkingDir: "{app}"
Name: "{userstartmenu}\{#MyAppName}"; Filename: "{app}\{#MainEXE}"; IconFilename: "{app}\WinGet-Logo.ico"; WorkingDir: "{app}"

[Code]
function PreferArm64Files: Boolean;
begin
  Result := IsArm64;
end;

function PreferX64Files: Boolean;
begin
  Result := not PreferArm64Files and IsX64Compatible;
end;

function PreferX86Files: Boolean;
begin
  Result := not PreferArm64Files and not PreferX64Files;
end;

function CheckInternetConnection: Boolean;
var
  WinHttpReq: Variant;
begin
  Result := False;
  try
    WinHttpReq := CreateOleObject('WinHttp.WinHttpRequest.5.1');
    WinHttpReq.Open('GET', 'https://www.google.com/', False);
    WinHttpReq.Send('');
    if WinHttpReq.Status = 200 then
      Result := True;
  except
    Result := False;
  end;
end;

function SwitchHasValue(Name: string; Value: string): Boolean;
begin
  Result := CompareText(ExpandConstant('{param:' + Name + '}'), Value) = 0;
end;

function InitializeSetup: Boolean;
begin
  #ifdef Dependency_Path_NetCoreCheck
    Dependency_AddDotNet47;
  #endif
  Result := True;
end;

procedure AboutButtonOnClick(Sender: TObject);
begin
  MsgBox('Installer made for BracketProto by oxmc', mbInformation, mb_Ok);
end;

procedure WebsiteBtnImageOnClick(Sender: TObject);
var
  ErrorCode: Integer;
begin
  ShellExec('open', 'https://bracketproto.com', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
end;

procedure InitializeWizard;
var
  SplashImage : TBitmapImage;
  WebsiteBtnImage : TBitmapImage;
  Splash  : TSetupForm;
  AboutButton: TNewButton;

begin
  // Splash screen
  Splash := CreateCustomForm;
  Splash.BorderStyle := bsNone;

  SplashImage := TBitmapImage.Create(Splash);
  SplashImage.AutoSize := True;
  SplashImage.Align := alClient;
  SplashImage.Left := 0;
  SplashImage.Top := 0;
  SplashImage.stretch := True;
  SplashImage.Parent := Splash;

  ExtractTemporaryFile('splash.bmp');
  SplashImage.Bitmap.LoadFromFile(ExpandConstant('{tmp}\splash.bmp'));

  Splash.Width := SplashImage.Width;
  Splash.Height := SplashImage.Height;

  Splash.Position := poScreenCenter;
  
  Splash.Show;

  SplashImage.Refresh;

  // About Button
  AboutButton := TNewButton.Create(WizardForm);
  AboutButton.Parent := WizardForm;
  AboutButton.Caption := '&About';
  AboutButton.OnClick := @AboutButtonOnClick;

  // Positioning the AboutButton relative to the CancelButton
  AboutButton.Left := WizardForm.ClientWidth - AboutButton.Width - ScaleX(370);
  AboutButton.Top := WizardForm.CancelButton.Top + ScaleY(70);
  
  // ProjectWebsite "Button" image
  WebsiteBtnImage := TBitmapImage.Create(WizardForm);
  WebsiteBtnImage.Parent := WizardForm;
  WebsiteBtnImage.AutoSize := True;
  try
    ExtractTemporaryFile('BracketProto_small.bmp');
    WebsiteBtnImage.Bitmap.LoadFromFile(ExpandConstant('{tmp}\BracketProto_small.bmp'));
  except
    MsgBox('Failed to load BracketProto_small image.', mbError, MB_OK);
  end;

  // Positioning the ProjectWebsite "Button" image relative to the CancelButton
  WebsiteBtnImage.Left := WizardForm.ClientWidth - WebsiteBtnImage.Width - ScaleX(460);
  WebsiteBtnImage.Top := WizardForm.CancelButton.Top + ScaleY(65);
  
  WebsiteBtnImage.OnClick := @WebsiteBtnImageOnClick;

  // Wait for a few seconds (e.g., 3 seconds) before closing the splash screen
  Sleep(3000);

  Splash.Close;
  Splash.Free;
end;