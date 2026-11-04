; Inno Setup script for BlahBlah3000. Build it with build-installer.cmd (it publishes the app to installer\staging first).

#define AppName "BlahBlah3000"
#define AppExe "BlahBlah3000.exe"
#define StagingExe "staging\" + AppExe
#define FullVersion GetFileVersion(StagingExe)
#define AppVersion Copy(FullVersion, 1, RPos(".", FullVersion) - 1)

[Setup]
AppId={{6F0C2B4E-3D7A-4B58-9E1F-5A8C7D2E4B91}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Bohuslav Franc
DefaultDirName={autopf}\Bogan\BlahBlah3000
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#AppExe}
SetupIconFile=..\assets\BlahBlah3000.ico
LicenseFile=..\LICENSE
OutputDir=output
OutputBaseFilename=BlahBlah3000-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Installs for the current user without administrator rights; the dialog lets the user choose an all-users install.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
; Setup (and the uninstaller) asks the user to close a running BlahBlah3000, which holds this mutex.
AppMutex=Local\Bogan.BlahBlah3000.SingleInstance

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "czech"; MessagesFile: "compiler:Languages\Czech.isl"

[CustomMessages]
english.DesktopIcon=Create a &desktop shortcut
english.RuntimeMissing=BlahBlah3000 needs the .NET 10 Desktop Runtime (x64), which was not found on this computer.%n%nOpen the download page now? You can install BlahBlah3000 anyway and install the runtime afterwards.
english.RemoveData=Do you also want to delete BlahBlah3000's settings, log and the downloaded Whisper model (about 834 MB)?
english.LaunchApp=Start BlahBlah3000
czech.DesktopIcon=Vytvořit zástupce na &ploše
czech.RuntimeMissing=BlahBlah3000 potřebuje .NET 10 Desktop Runtime (x64), který na tomto počítači nebyl nalezen.%n%nOtevřít stránku ke stažení? BlahBlah3000 můžete nainstalovat i tak a runtime doinstalovat později.
czech.RemoveData=Chcete smazat také nastavení BlahBlah3000, log a stažený Whisper model (asi 834 MB)?
czech.LaunchApp=Spustit BlahBlah3000

[Tasks]
Name: "desktopicon"; Description: "{cm:DesktopIcon}"; Flags: unchecked

[Files]
; The runtimes folder with the native Whisper libraries must stay next to BlahBlah3000.exe.
Source: "staging\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"; Parameters: "--tray"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Parameters: "--tray"; Tasks: desktopicon

[Registry]
; The application writes this value itself when autostart is enabled in its settings; remove it on uninstall.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: none; ValueName: "BlahBlah3000"; Flags: uninsdeletevalue dontcreatekey

[Run]
Filename: "{app}\{#AppExe}"; Parameters: "--tray"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent

[Code]
function IsDesktopRuntimeInstalled: Boolean;
var
  Found: TFindRec;
begin
  Result := False;
  if FindFirst(ExpandConstant('{commonpf64}\dotnet\shared\Microsoft.WindowsDesktop.App\10.*'), Found) then
  begin
    Result := True;
    FindClose(Found);
  end;
end;

function InitializeSetup: Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;
  if not IsDesktopRuntimeInstalled then
  begin
    if MsgBox(CustomMessage('RuntimeMissing'), mbConfirmation, MB_YESNO) = IDYES then
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/10.0', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    if MsgBox(CustomMessage('RemoveData'), mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
    begin
      DelTree(ExpandConstant('{localappdata}\Bogan\BlahBlah3000'), True, True, True);
      DelTree(ExpandConstant('{userappdata}\Bogan\BlahBlah3000'), True, True, True);
    end;
  end;
end;
