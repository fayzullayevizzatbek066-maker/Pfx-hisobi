; Inno Setup script for PFX Manager.
; Produces PFXManager-Setup.exe from a self-contained "dotnet publish" output directory.
;
; Build the publish output first:
;   dotnet publish src\PFXManager.App\PFXManager.App.csproj -c Release -r win-x64 --self-contained true -o publish\PFXManager
; Then compile this script (ISCC.exe ships with Inno Setup 6, pre-installed on GitHub's
; windows-latest runners at "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"):
;   ISCC.exe installer\PFXManager.iss

#define MyAppName "PFX Manager"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "PFX Manager"
#define MyAppExeName "PFXManager.exe"
#define MyPublishDir "..\publish\PFXManager"

[Setup]
AppId={{6B7E7A9C-6C2E-4F1E-9B7C-2A6F3E8D9A11}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; Standard per-machine install requires elevation only for the installer itself (rule 27:
; the *application* still runs as a normal user afterwards — this is just where its files live).
PrivilegesRequired=admin
OutputDir=..\dist
OutputBaseFilename=PFXManager-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Ish stolida yorliq yaratish (Create a desktop shortcut)"; GroupDescription: "Qo'shimcha yorliqlar:"; Flags: unchecked

[Files]
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Uninstall never touches the SQLite database, logs, or (crucially) the Quarantine folder under
; ProgramData: those are the user's data, and quarantined PFX files may be the only surviving
; copy of a certificate the user still needs (section 40). Only files this installer laid down
; under {app} are removed, which is the default Inno Setup behavior — no explicit deletion of
; %ProgramData%\PFXManager is declared here on purpose.
