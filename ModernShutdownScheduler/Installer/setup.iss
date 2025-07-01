; -- MyApp Setup Script for Inno Setup --

#define AppName "ModernShutdownScheduler"
#define AppVersion "1.0.0"
#define AppPublisher "JasonZ"
#define AppURL "https://github.com/JasonZ-Star/ModernShutdownScheduler"
#define OutputName "MSS-Setup"
#define NetVersion "9.0.0" 
#define NetInstaller "runtime-9.0.6-win-x64.exe"



[Setup]
AppId={{AUTO-GENERATED-GUID}}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
DefaultDirName=D:\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputBaseFilename={#OutputName}-{#AppVersion}
OutputDir=.\output
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}";

[Files]
; 1. 打包你的应用文件 (Action 会将它们发布到 'publish' 文件夹)

Source: "..\..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; 2. 打包 .NET 运行库安装包
Source: "redist\{#NetInstaller}"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\ModernShutdownScheduler.exe"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\ModernShutdownScheduler.exe"; Tasks: desktopicon

[Run]
; 3. 如果需要，则运行 .NET 安装包
Filename: "{tmp}\{#NetInstaller}"; Parameters: "/quiet /norestart"; StatusMsg: "正在安装 .NET 桌面运行库..."; Check: not IsNetDesktopRuntimeInstalled('{#NetVersion}');
; 4. 安装完成后运行主程序
Filename: "{app}\ModernShutdownScheduler.exe"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// 检查 .NET 桌面运行库是否已安装的函数
function IsNetDesktopRuntimeInstalled(MinVersion: string): Boolean;
var
  Installed: Boolean;
  version: string;
  major, minor, patch: Cardinal;
  min_major, min_minor, min_patch: Cardinal;
  key: string;
begin
  key := 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App';

  // 尝试直接查询注册表，看是否存在对应版本或更高版本的键
  if RegQueryStringValue(HKLM, key, MinVersion, version) then
  begin
    Result := True;
    exit;
  end;

  // 此处可以添加更复杂的逻辑来检查所有已安装的版本，判断是否大于等于MinVersion
  // 为简化，我们假设精确匹配或更高版本键的存在即可
  Result := False; 
end;