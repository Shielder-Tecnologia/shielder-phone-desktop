; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "Shielder Phone"
#define MyAppVersion "1.0"
#define MyAppPublisher "Shielder Tecnologia"
#define MyAppExeName "ShielderPhone.exe"
#define MyAppIcoName "so_logo.ico"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{FE11F1CE-6D96-4DB9-B5A3-AC4040F2DBF8}
AppName={#MyAppName}
AppVersion={#MyAppVersion}

;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Shielder Tecnologia\{#MyAppName}
DisableDirPage=yes
DisableProgramGroupPage=yes
; Uncomment the following line to run in non administrative install mode (install for current user only.)
;PrivilegesRequired=lowest
OutputBaseFilename=ShielderPhone
OutputDir=C:\Users\gabriel\Desktop\Shielder\Shielder-Phone\App
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "C:\Users\gabriel\Desktop\Shielder\Shielder-Phone\SIP_Desktop\Binaries\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\gabriel\Desktop\Shielder\Shielder-Phone\SIP_Desktop\Binaries\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "C:\Users\gabriel\Desktop\Shielder\Shielder-Phone\SIP_Desktop\Binaries\sch_create.bat"; DestDir: "{app}\bin"; Flags: ignoreversion
Source: "C:\Users\gabriel\Desktop\Shielder\Shielder-Phone\CriarRegistro.bat"; DestDir: "{app}\bin"; Flags: ignoreversion
Source: "C:\Users\gabriel\Desktop\Shielder\Shielder-Phone\register.reg"; DestDir: "{app}\bin"; Flags: ignoreversion
Source: "C:\Users\gabriel\Desktop\Shielder\Shielder-Phone\SIP_Desktop\Binaries\Shielder Phone.xml"; DestDir: "{app}\bin"; Flags: ignoreversion
Source: "C:\Users\gabriel\Desktop\Shielder\Shielder-Phone\SIP_Desktop\Binaries\so_logo.ico"; DestDir: "{app}\bin"; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\bin\{#MyAppIcoName}"; 

[Run]
Filename: "{app}\bin\sch_create.bat"; Flags: runhidden runascurrentuser
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: runascurrentuser nowait postinstall skipifsilent
Filename: "{app}\bin\CriarRegistro.bat"; Flags: runhidden runascurrentuser