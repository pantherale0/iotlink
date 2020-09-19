; Script generated by the Inno Script Studio Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!
#define APP_NAME         "IOT Link"
#define APP_DIR_NAME     "IOTLink"
#define APP_AGENT_NAME   "IOTLinkAgent.exe"
#define APP_SERVICE_NAME "IOTLinkService.exe"
#define APP_VERSION      "2.2.1"
#define APP_BETA_VERSION ""

#define APP_AUTHOR_NAME "Alexandre Leites"
#define APP_AUTHOR_URL  "https://www.alexandreleites.com.br"

#include "donate.iss"

[Setup]
; Basic Information
AppId={{CD785E2E-5102-4053-A1E1-208CA1D8DC98}
AppName={#APP_NAME}
AppVersion={#APP_VERSION}
VersionInfoVersion={#APP_VERSION}
; Publisher Information
AppPublisher={#APP_AUTHOR_NAME}
AppPublisherURL={#APP_AUTHOR_URL}
AppSupportURL={#APP_AUTHOR_URL}
AppUpdatesURL={#APP_AUTHOR_URL}
; Installation Directort Settings
DefaultDirName={commonpf}\{#APP_DIR_NAME}
DefaultGroupName={#APP_NAME}
UsePreviousAppDir=yes
; Setup Settings
OutputDir=Setup
#if APP_BETA_VERSION == ""
OutputBaseFilename={#APP_DIR_NAME}_Installer_v{#APP_VERSION}
#else
OutputBaseFilename={#APP_DIR_NAME}_Installer_v{#APP_VERSION}-{#APP_BETA_VERSION}
#endif
WizardStyle=modern
AllowNoIcons=yes
LicenseFile=LICENSE.md
UninstallDisplayIcon={app}\IOTLinkAgent.exe    
; Compression
Compression=lzma2
SolidCompression=no
; Privileges Settings
PrivilegesRequired=admin
SetupIconFile=Assets\images\icons\application.ico

[Files]
; Main Files
Source: "Builds\Release\IOTLinkAgent.exe";                                     DestDir: "{app}"; DestName: "{#APP_AGENT_NAME}";          Flags: ignoreversion
Source: "Builds\Release\IOTLinkAgent.exe.config";                              DestDir: "{app}"; DestName: "{#APP_AGENT_NAME}.config";   Flags: ignoreversion
Source: "Builds\Release\IOTLinkService.exe";                                   DestDir: "{app}"; DestName: "{#APP_SERVICE_NAME}";        Flags: ignoreversion
Source: "Builds\Release\IOTLinkService.exe.config";                            DestDir: "{app}"; DestName: "{#APP_SERVICE_NAME}.config"; Flags: ignoreversion
Source: "Builds\Release\*.dll";                                                DestDir: "{app}"; Flags: ignoreversion
; Configuration Sample
Source: "Assets\config.yaml-sample";                                           DestDir: "{commonappdata}\{#APP_DIR_NAME}\Configs"; DestName: "configuration.yaml"; Flags: confirmoverwrite; Permissions: everyone-full
; Application Icon
Source: "Assets\images\icons\application.ico";                                 DestDir: "{app}\Icons"; Flags: ignoreversion
; Folder Icons
Source: "Assets\images\icons\addons.ico";                                      DestDir: "{app}\Icons"; Flags: ignoreversion
Source: "Assets\images\icons\configs.ico";                                     DestDir: "{app}\Icons"; Flags: ignoreversion
Source: "Assets\images\icons\logs.ico";                                        DestDir: "{app}\Icons"; Flags: ignoreversion
; Service Install/Uninstall
Source: "Assets\images\icons\install_service.ico";                             DestDir: "{app}\Icons"; Flags: ignoreversion
Source: "Assets\images\icons\uninstall_service.ico";                           DestDir: "{app}\Icons"; Flags: ignoreversion
; Service Start/Stop
Source: "Assets\images\icons\start_service.ico";                               DestDir: "{app}\Icons"; Flags: ignoreversion
Source: "Assets\images\icons\stop_service.ico";                                DestDir: "{app}\Icons"; Flags: ignoreversion

; Addon - Commands
Source: "Addons\Commands\addon.yaml";                                          DestDir: "{commonappdata}\{#APP_DIR_NAME}\Addons\Commands";       Flags: ignoreversion;                  Permissions: everyone-full;  Tasks: Addons\Commands
Source: "Addons\Commands\bin\Release\Commands.dll";                            DestDir: "{commonappdata}\{#APP_DIR_NAME}\Addons\Commands";       Flags: ignoreversion;                  Permissions: everyone-full;  Tasks: Addons\Commands
Source: "Addons\Commands\Assets\*";                                            DestDir: "{commonappdata}\{#APP_DIR_NAME}\Addons\Commands";       Flags: ignoreversion recursesubdirs;   Permissions: everyone-full;  Tasks: Addons\Commands

; Addon - Windows Monitor
Source: "Addons\WindowsMonitor\addon.yaml";                                    DestDir: "{commonappdata}\{#APP_DIR_NAME}\Addons\WindowsMonitor"; Flags: ignoreversion;                  Permissions: everyone-full;  Tasks: Addons\WindowsMonitor
Source: "Addons\WindowsMonitor\config.yaml";                                   DestDir: "{commonappdata}\{#APP_DIR_NAME}\Addons\WindowsMonitor"; Flags: confirmoverwrite;               Permissions: everyone-full;  Tasks: Addons\WindowsMonitor
Source: "Addons\WindowsMonitor\bin\Release\WindowsMonitor.dll";                DestDir: "{commonappdata}\{#APP_DIR_NAME}\Addons\WindowsMonitor"; Flags: ignoreversion;                  Permissions: everyone-full;  Tasks: Addons\WindowsMonitor
Source: "Addons\WindowsMonitor\Assets\*";                                      DestDir: "{commonappdata}\{#APP_DIR_NAME}\Addons\WindowsMonitor"; Flags: ignoreversion recursesubdirs;   Permissions: everyone-full;  Tasks: Addons\WindowsMonitor

; Addon - Process Monitor
Source: "Addons\ProcessMonitor\addon.yaml";                                    DestDir: "{commonappdata}\{#APP_DIR_NAME}\Addons\ProcessMonitor"; Flags: ignoreversion;                  Permissions: everyone-full;  Tasks: Addons\ProcessMonitor
Source: "Addons\ProcessMonitor\config.yaml";                                   DestDir: "{commonappdata}\{#APP_DIR_NAME}\Addons\ProcessMonitor"; Flags: confirmoverwrite;               Permissions: everyone-full;  Tasks: Addons\ProcessMonitor
Source: "Addons\ProcessMonitor\bin\Release\ProcessMonitor.dll";                DestDir: "{commonappdata}\{#APP_DIR_NAME}\Addons\ProcessMonitor"; Flags: ignoreversion;                  Permissions: everyone-full;  Tasks: Addons\ProcessMonitor
Source: "Addons\ProcessMonitor\Assets\*";                                      DestDir: "{commonappdata}\{#APP_DIR_NAME}\Addons\ProcessMonitor"; Flags: ignoreversion recursesubdirs;   Permissions: everyone-full;  Tasks: Addons\ProcessMonitor

[Icons]
; Service Install/Uninstall
Name: "{group}\Install Windows Service";      Filename: "{app}\{#APP_SERVICE_NAME}";                                     IconFilename: "{app}\Icons\install_service.ico";    WorkingDir: "{app}"; Parameters: "--install";     AfterInstall: SetElevationBit('{group}\Install Windows Service.lnk')
Name: "{group}\Uninstall Windows Service";    Filename: "{app}\{#APP_SERVICE_NAME}";                                     IconFilename: "{app}\Icons\uninstall_service.ico";  WorkingDir: "{app}"; Parameters: "--uninstall";   AfterInstall: SetElevationBit('{group}\Uninstall Windows Service.lnk')
; Service Start/Stop
Name: "{group}\Start Windows Service";        Filename: "net.exe";                                                       IconFilename: "{app}\Icons\start_service.ico";      WorkingDir: "{sys}"; Parameters: "start {#APP_DIR_NAME}";  AfterInstall: SetElevationBit('{group}\Start Windows Service.lnk')
Name: "{group}\Stop Windows Service";         Filename: "net.exe";                                                       IconFilename: "{app}\Icons\stop_service.ico";       WorkingDir: "{sys}"; Parameters: "stop {#APP_DIR_NAME}";   AfterInstall: SetElevationBit('{group}\Stop Windows Service.lnk')
; Open Folders
Name: "{group}\Open Configuration File";      Filename: "{commonappdata}\{#APP_DIR_NAME}\Configs\configuration.yaml";    IconFilename: "{app}\Icons\configs.ico";            WorkingDir: "{commonappdata}\{#APP_DIR_NAME}\Configs";
Name: "{group}\Open Addons Folder";           Filename: "{commonappdata}\{#APP_DIR_NAME}\Addons";                        IconFilename: "{app}\Icons\addons.ico";             WorkingDir: "{commonappdata}\{#APP_DIR_NAME}\Addons";    Flags: foldershortcut
Name: "{group}\Open Logs Folder";             Filename: "{commonappdata}\{#APP_DIR_NAME}\Logs";                          IconFilename: "{app}\Icons\logs.ico";               WorkingDir: "{commonappdata}\{#APP_DIR_NAME}\Logs";      Flags: foldershortcut

[Run]
Filename: "net.exe";                          Parameters: "stop {#APP_DIR_NAME}";  WorkingDir: "{app}"; Flags: runascurrentuser runhidden;             Description: "Stop {#APP_NAME} Service";       StatusMsg: "Stopping Windows Service"
Filename: "{app}\{#APP_SERVICE_NAME}";        Parameters: "--install";             WorkingDir: "{app}"; Flags: runascurrentuser postinstall runhidden; Description: "Install {#APP_NAME} as Service"; StatusMsg: "Installing Windows Service"
Filename: "net.exe";                          Parameters: "start {#APP_DIR_NAME}"; WorkingDir: "{app}"; Flags: runascurrentuser postinstall runhidden; Description: "Start {#APP_NAME} Service";      StatusMsg: "Starting Windows Service"

[UninstallRun]
Filename: "net.exe";                          Parameters: "stop {#APP_DIR_NAME}";       WorkingDir: "{app}"; Flags: runhidden;
Filename: "{app}\{#APP_SERVICE_NAME}";        Parameters: "--uninstall";                WorkingDir: "{app}"; Flags: runhidden;

[Dirs]
Name: "{commonappdata}\{#APP_DIR_NAME}";            Permissions: everyone-full
Name: "{commonappdata}\{#APP_DIR_NAME}\Configs";    Permissions: everyone-full
Name: "{commonappdata}\{#APP_DIR_NAME}\Logs";       Permissions: everyone-full
Name: "{commonappdata}\{#APP_DIR_NAME}\Addons";     Permissions: everyone-full

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Tasks]
Name: "Addons";                 Description: "Install Addons"
Name: "Addons\Commands";        Description: "Addon: Commands"
Name: "Addons\WindowsMonitor";  Description: "Addon: Windows Monitor"
Name: "Addons\ProcessMonitor";  Description: "Addon: Process Monitor"

[Code]
procedure SetElevationBit(Filename: string);
var
  Buffer: string;
  Stream: TStream;
begin
  Filename := ExpandConstant(Filename);
  Log('Setting elevation bit for ' + Filename);

  Stream := TFileStream.Create(FileName, fmOpenReadWrite);
  try
    Stream.Seek(21, soFromBeginning);
    SetLength(Buffer, 1);
    Stream.ReadBuffer(Buffer, 1);
    Buffer[1] := Chr(Ord(Buffer[1]) or $20);
    Stream.Seek(-1, soFromCurrent);
    Stream.WriteBuffer(Buffer, 1);
  finally
    Stream.Free;
  end;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpFinished then
    WizardForm.RunList.Visible := False;
end;
