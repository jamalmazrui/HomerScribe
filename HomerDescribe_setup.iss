; HomerDescribe_setup.iss -- installer for HomerDescribe.
;
; Follows the pattern of 2htm, extCheck, urlFido, bookFido and helpFido:
; per-machine install under Program Files, no "who is this for" question, the
; destination page always shown, a desktop shortcut with a hotkey, and the
; optional extras as checkboxes on the final page.
;
; ---- Version -----------------------------------------------------------------
; The version number is NOT stored in this script. It lives in version.txt, one
; line, which buildHomerDescribe.cmd increments on every build. Inno reads it
; here, and the build script also generates Version.cs from it, so the program,
; the installer, and the release tag always report the same number. Because no
; version literal appears in this file, a stale copy of it cannot rewind the
; version.

#define AppName       "HomerDescribe"

#define VerFile FileOpen(AddBackslash(SourcePath) + "version.txt")
#define AppVersion Trim(FileRead(VerFile))
#expr FileClose(VerFile)
#undef VerFile

#define AppPublisher  "Jamal Mazrui"
#define AppUrl        "https://github.com/JamalMazrui/HomerDescribe"
#define AppExeName    "HomerDescribe.exe"
#define AppCopyright  "Copyright (c) 2026 Jamal Mazrui. MIT License."

; HotKey is the Inno Setup HotKey: directive value, which requires Ctrl syntax.
; HotKeyDisplay is the same key in the notation used everywhere a person reads
; it: Control rather than Ctrl, modifiers in alphabetical order. helpFido has
; taken Alt+Ctrl+Shift+H, so this is the plain form. Change both if you would
; rather it were something else.
#define HotKey        "Alt+Ctrl+H"
#define HotKeyDisplay "Alt+Control+H"

[Setup]
AppId={{7C1D5A64-3E9B-4D2A-9F17-1B6C0A8E4D53}

AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}
AppUpdatesURL={#AppUrl}/releases
AppCopyright={#AppCopyright}

; The version resource of the built setup. tagRelease reads the FileVersion
; STRING from it and tags v<that>, so the text form is set explicitly: the tag
; wanted is v1.0.0, not v1.0.0.0.
VersionInfoVersion={#AppVersion}
VersionInfoTextVersion={#AppVersion}
VersionInfoProductVersion={#AppVersion}
VersionInfoProductTextVersion={#AppVersion}
VersionInfoCompany={#AppPublisher}
VersionInfoCopyright={#AppCopyright}
VersionInfoDescription={#AppName} Setup

; Install under Program Files. {autopf} resolves to "Program Files" on 64-bit
; Windows when the installer runs in 64-bit mode, per ArchitecturesInstallIn64BitMode.
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
UsePreviousAppDir=yes

; Always show the destination page, even on reinstall. Left at its default of
; "auto", Inno hides it whenever a prior install of the same AppId is found.
; UsePreviousAppDir fills in the previous directory, so a reinstall is one press
; of Next, but the path is visible and editable.
DisableDirPage=no
UsePreviousGroup=yes

OutputDir=.
OutputBaseFilename={#AppName}_setup
SolidCompression=yes
; Empty on purpose: no license page. The license travels with the program
; and is on the Start menu, but it is not a gate on the way in.
LicenseFile=
WizardStyle=modern
Compression=lzma2/max
MinVersion=10.0
AppComments=Generates audio description for local video files, on this machine.

#if FileExists(AddBackslash(SourcePath) + "HomerDescribe.ico")
SetupIconFile={#AppName}.ico
#endif

; Admin, to write to Program Files. PrivilegesRequiredOverridesAllowed is left
; EMPTY on purpose: that is what removes the "install for me only or for all
; users" page that Inno shows first when the choice is offered.
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=

; 64-bit Windows only.
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

Uninstallable=yes
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName} {#AppVersion}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
; Every line below the program itself carries skipifsourcedoesntexist. Only
; HomerDescribe.exe is genuinely required; a missing document or an empty
; context folder must not abort a build, and a wildcard matching nothing is a
; fatal error in Inno unless the line says otherwise.
;
; Both forms of the documentation travel: Markdown for reading in an editor or
; on a braille display, and HTML for opening in a browser, which is what the
; shortcuts point at.
Source: "ReadMe.md"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "History.md"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "License.md"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "ReadMe.htm"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "History.htm"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "License.htm"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

; The companion programs, packaged when they are present in the build folder.
; buildHomerDescribe.cmd downloads them when they are missing, so normally they
; are here. skipifsourcedoesntexist means a build without them still succeeds;
; HomerDescribe then looks on the PATH instead. Installing them beside
; HomerDescribe.exe is what makes the program work with nothing else set up,
; since its own folder is the first place it looks.
Source: "ffmpeg.exe"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "ffprobe.exe"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "yt-dlp.exe"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

Source: "installOllama.cmd"; DestDir: "{app}"; Flags: ignoreversion
Source: "installModels.cmd"; DestDir: "{app}"; Flags: ignoreversion
Source: "context\*.md"; DestDir: "{app}\context"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist
Source: "context\*.htm"; DestDir: "{app}\context"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist

[Icons]
; WorkingDir is the user's Documents folder, so a run started from a shortcut
; writes its results somewhere writable by default.
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{userdocs}"
Name: "{group}\{#AppName} documentation"; Filename: "{app}\ReadMe.htm"; Flags: createonlyiffileexists
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
; Created without asking. The hotkey is mentioned on the launch checkbox at the
; end, which is where the user is looking when it matters.
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{userdocs}"; HotKey: "{#HotKey}"

[Run]
; Post-install checkboxes shown on the final wizard page. What HomerDescribe
; needs comes first, then what to do next. All four default to checked; any can
; be unchecked to skip.
;
; helpFido leaves its Ollama box unchecked because helpFido works without it.
; HomerDescribe does not: with no local model there is nothing to write the
; descriptions. Both are shown every time rather than being hidden when already
; present -- the scripts themselves notice what is installed and say so in a
; second, and a checkbox that sometimes vanishes is worse than one that
; occasionally has nothing to do.
;
; The installs happen here rather than by sending the user to a download page:
; winget ships with Windows 10 and 11 and can fetch Ollama unattended.
; runascurrentuser matters -- winget and ollama install per-user, into the
; profile of whoever is signed in, and this installer is running elevated.

FileName: "{cmd}"; \
  Parameters: "/c """"{app}\installOllama.cmd"""""; \
  WorkingDir: "{app}"; \
  Description: "Install Ollama and the vision model (about 6.5 GB in all; this is what {#AppName} describes with)"; \
  Flags: postinstall skipifsilent runascurrentuser

FileName: "{cmd}"; \
  Parameters: "/c """"{app}\installModels.cmd"""""; \
  WorkingDir: "{app}"; \
  Description: "Install the vision model only (about 5.5 GB; tick this if Ollama is already installed)"; \
  Flags: postinstall skipifsilent runascurrentuser

FileName: "{app}\{#AppExeName}"; \
  WorkingDir: "{userdocs}"; \
  Description: "Launch {#AppName} now (desktop hotkey: {#HotKeyDisplay})"; \
  Flags: nowait postinstall skipifsilent

FileName: "{app}\ReadMe.htm"; \
  Description: "Read documentation for {#AppName}"; \
  Flags: postinstall shellexec skipifsilent skipifdoesntexist

[UninstallDelete]
Type: filesandordirs; Name: "{app}\context"

