; Inno Setup Script for ScreenTranslator (小光翻译)

#define MyAppName "小光翻译 (ScreenTranslator)"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "yangguangjin"
#define MyAppURL "https://github.com/yangguangjin/xiaoguang_ScreenTranslator"
#define MyAppExeName "ScreenTranslator.exe"
#define PublishDir "ScreenTranslator\bin\Release\net8.0-windows\win-x64\publish"

[Setup]
AppId={{B8F3A2E1-5C7D-4A9B-8E6F-1D2C3B4A5E6F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
DefaultDirName={localappdata}\Programs\ScreenTranslator
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=output
OutputBaseFilename=ScreenTranslator-Setup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=lowest
SetupIconFile=ScreenTranslator\icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallFilesDir={app}
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加选项:"
Name: "autostart"; Description: "开机自动启动"; GroupDescription: "附加选项:"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\卸载 {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "ScreenTranslator"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "启动 小光翻译"; Flags: nowait postinstall skipifsilent
