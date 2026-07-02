#ifndef AppVersion
#define AppVersion "local-test"
#endif

#define AppName "Nexus"
#define AppPublisher "Nexus Project"
#define AppExeName "Nexus App.exe"

[Setup]
AppId={{cbf2e120-032c-42bc-9658-6fe3ac1186d4} 
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppMutex=Global\NexusClientMutex-{cbf2e120-032c-42bc-9658-6fe3ac1186d4}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
OutputDir=..\Output
OutputBaseFilename=Nexus_Setup_v{#AppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
SetupIconFile=..\src\Nexus.Client\wwwroot\icons\favicon.ico
UninstallDisplayIcon={app}\Client\{#AppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\publish\Service\*"; DestDir: "{app}\Service"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\publish\Client\*"; DestDir: "{app}\Client"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\Client\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\Client\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{sys}\sc.exe"; Parameters: "create ""Nexus Background Service"" binPath= ""{app}\Service\Nexus Background Service.exe"" start= auto"; Flags: runhidden
Filename: "{sys}\sc.exe"; Parameters: "start ""Nexus Background Service"""; Flags: runhidden
Filename: "{app}\Client\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{sys}\sc.exe"; Parameters: "stop ""Nexus Background Service"""; Flags: runhidden; RunOnceId: "StopService"
Filename: "{sys}\sc.exe"; Parameters: "delete ""Nexus Background Service"""; Flags: runhidden; RunOnceId: "DeleteService"