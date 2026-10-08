; HomerScribe_setup.iss -- installer for HomerScribe.
;
; Follows the pattern of 2htm, extCheck, urlFido, bookFido and helpFido:
; per-machine install under Program Files, no "who is this for" question, the
; destination page always shown, a desktop shortcut with a hotkey, and the
; optional extras as checkboxes on the final page.
;
; ---- Version -----------------------------------------------------------------
; The version number is NOT stored in this script. It lives in version.txt, one
; line, which build.cmd increments on every build. Inno reads it
; here, and the build script also generates Version.cs from it, so the program,
; the installer, and the release tag always report the same number. Because no
; version literal appears in this file, a stale copy of it cannot rewind the
; version.

#define AppName       "HomerScribe"

#define VerFile FileOpen(AddBackslash(SourcePath) + "version.txt")
#define AppVersion Trim(FileRead(VerFile))
#expr FileClose(VerFile)
#undef VerFile

#define AppPublisher  "Jamal Mazrui"
#define AppUrl        "https://github.com/JamalMazrui/HomerScribe"
#define AppExeName    "HomerScribe.exe"
#define AppCopyright  "Copyright (c) 2026 Jamal Mazrui. MIT License."

; HotKey is the Inno Setup HotKey: directive value, which requires Ctrl syntax.
; HotKeyDisplay is the same key in the notation used everywhere a person reads
; it: Control rather than Ctrl, modifiers in alphabetical order. helpFido has
; taken Alt+Ctrl+Shift+H, so this is the plain form. Change both if you would
; rather it were something else.
#define HotKey        "Alt+Ctrl+H"
#define HotKeyDisplay "Alt+Control+H"



[Setup]
; THE INSTALLER'S OWN LOG, which is a HomerDev requirement and was missing.
; Inno writes it to %TEMP%; the [Code] section below copies it into
; %LOCALAPPDATA%\HomerScribe\logs at the end, where every other Homer log
; lives, so the folder can be zipped and sent.
SetupLogging=yes
AppId={{B4E27A19-6C08-4F3D-8A52-D9137E60C4BB}

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

; Hide the destination page when a previous install of the same AppId is found,
; which is what the other Homer Tools do: a reinstall then asks nothing at all
; and goes where the last one went. A first install still chooses the folder.
DisableDirPage=auto
UsePreviousGroup=yes

; THE INSTALLER IS WRITTEN TO THE TOP OF THE PROJECT, as in every Homer app:
; scripts\release looks for it there, and LocalFiles.txt names it, so tidy
; leaves it in place and git never takes it (exec until 26 September 2026).
OutputDir=.
OutputBaseFilename={#AppName}_setup
SolidCompression=yes
; Empty on purpose: no license page. The license travels with the program
; and is on the Start menu, but it is not a gate on the way in.
LicenseFile=
WizardStyle=modern
Compression=lzma2/max
MinVersion=10.0
AppComments=Describes video and transcribes speech, on this machine.

#if FileExists(AddBackslash(SourcePath) + "HomerScribe.ico")
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
UninstallDisplayIcon={app}\exec\{#AppExeName}
UninstallDisplayName={#AppName} {#AppVersion}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
; SHARED COMPONENTS ARE NOT SHIPPED HERE.
;
; ffmpeg, ffprobe, yt-dlp and exiftool used to be installer payload, which put
; them inside this app's folder. That is the wrong home twice over: no other
; Homer app can find them, and the next upgrade of THIS app replaces the folder
; and destroys them. That is how Whisper went missing.
;
; Each is installed to its own default machine-wide directory by the scripts
; below, and HomerScribe looks there. The DLLs further down are different: a
; library the program links against belongs beside the executable.
Source: "exec\{#AppExeName}"; DestDir: "{app}\exec"; Flags: ignoreversion
; Every line below the program itself carries skipifsourcedoesntexist. Only
; HomerScribe.exe is genuinely required; a missing document or an empty
; templates folder must not abort a build, and a wildcard matching nothing is a
; fatal error in Inno unless the line says otherwise.
;
; Both forms of the documentation travel: Markdown for reading in an editor or
; on a braille display, and HTML for opening in a browser, which is what the
; shortcuts point at.
Source: "ReadMe.md"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
; The complete guide and the hotkey summary. ReadMe.md is the short way in and
; points at both.
Source: "help\Camel_Type_C#.htm"; DestDir: "{app}\help"; Flags: ignoreversion
Source: "help\Camel_Type_C#.md"; DestDir: "{app}\help"; Flags: ignoreversion
Source: "help\captioned_videos.htm"; DestDir: "{app}\help"; Flags: ignoreversion
Source: "help\captioned_videos.md"; DestDir: "{app}\help"; Flags: ignoreversion
Source: "help\handover.htm"; DestDir: "{app}\help"; Flags: ignoreversion
Source: "help\handover.md"; DestDir: "{app}\help"; Flags: ignoreversion
Source: "help\science_playlist.htm"; DestDir: "{app}\help"; Flags: ignoreversion
Source: "help\silent_films.htm"; DestDir: "{app}\help"; Flags: ignoreversion
Source: "help\tv_shows.htm"; DestDir: "{app}\help"; Flags: ignoreversion
Source: "help\w3c-perspectives.htm"; DestDir: "{app}\help"; Flags: ignoreversion
Source: "help\HomerScribe.md"; DestDir: "{app}\help"; Flags: ignoreversion skipifsourcedoesntexist
Source: "help\Hotkeys.md"; DestDir: "{app}\help"; Flags: ignoreversion skipifsourcedoesntexist
Source: "help\video_formats.md"; DestDir: "{app}\help"; Flags: ignoreversion skipifsourcedoesntexist
Source: "help\History.md"; DestDir: "{app}\help"; Flags: ignoreversion skipifsourcedoesntexist
Source: "License.md"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "help\Developer.md"; DestDir: "{app}\help"; Flags: ignoreversion skipifsourcedoesntexist
Source: "help\Review.md"; DestDir: "{app}\help"; Flags: ignoreversion skipifsourcedoesntexist
Source: "help\Context.md"; DestDir: "{app}\help"; Flags: ignoreversion skipifsourcedoesntexist
Source: "help\Announce.md"; DestDir: "{app}\help"; Flags: ignoreversion skipifsourcedoesntexist
Source: "ReadMe.htm"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "help\HomerScribe.htm"; DestDir: "{app}\help"; Flags: ignoreversion skipifsourcedoesntexist
Source: "help\Hotkeys.htm"; DestDir: "{app}\help"; Flags: ignoreversion skipifsourcedoesntexist
Source: "help\video_formats.htm"; DestDir: "{app}\help"; Flags: ignoreversion skipifsourcedoesntexist
Source: "help\History.htm"; DestDir: "{app}\help"; Flags: ignoreversion skipifsourcedoesntexist
Source: "License.htm"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "help\Developer.htm"; DestDir: "{app}\help"; Flags: ignoreversion skipifsourcedoesntexist
Source: "help\Review.htm"; DestDir: "{app}\help"; Flags: ignoreversion skipifsourcedoesntexist
Source: "help\Context.htm"; DestDir: "{app}\help"; Flags: ignoreversion skipifsourcedoesntexist
Source: "help\Announce.htm"; DestDir: "{app}\help"; Flags: ignoreversion skipifsourcedoesntexist
; The tutorials: the written walks, the podcast feed, and one .mp3 per walk
; in help\tutorials with a playlist beside them. Spoken by the build when
; missing, so they are here whenever the build ran to the end.
Source: "help\Tutorials.md"; DestDir: "{app}\help"; Flags: ignoreversion skipifsourcedoesntexist
Source: "help\Tutorials.htm"; DestDir: "{app}\help"; Flags: ignoreversion skipifsourcedoesntexist
Source: "help\TutorialFeed.xml"; DestDir: "{app}\help"; Flags: ignoreversion skipifsourcedoesntexist
Source: "help\tutorials\*.mp3"; DestDir: "{app}\help\tutorials"; Flags: ignoreversion skipifsourcedoesntexist
Source: "help\tutorials\Tutorials.m3u"; DestDir: "{app}\help\tutorials"; Flags: ignoreversion skipifsourcedoesntexist

; The companion programs, packaged when they are present in the build folder.
; build.cmd downloads them when they are missing, so normally they
; are here. skipifsourcedoesntexist means a build without them still succeeds;
; HomerScribe then looks on the PATH instead. Installing them beside
; HomerScribe.exe is what makes the program work with nothing else set up,
; since its own folder is the first place it looks.
; ExifTool writes the descriptions into the pictures. A SINGLE FILE only --
; no "exiftool_files" folder is packaged, deliberately. See License.md and
; the ExifTool section of build.cmd for why.
Source: "scripts\installExifTool.cmd"; DestDir: "{app}\scripts"; Flags: ignoreversion skipifsourcedoesntexist

; AND THE BINARY ITSELF, from exec\, which he put there by hand.
;
; ExifTool's current packages are Perl-based and need an exiftool_files folder
; of hundreds of files beside the executable. The SINGLE-FILE build does
; everything HomerScribe asks of it -- reading and writing the description and
; keyword tags -- so that is the one carried, and it is carried rather than
; fetched because no current download offers it on its own.
;
; skipifsourcedoesntexist: a developer without the file can still build; the
; program then falls back to looking for ExifTool on the machine.
Source: "exec\exiftool.exe"; DestDir: "{app}\exec"; Flags: ignoreversion skipifsourcedoesntexist

Source: "scripts\installOllama.cmd"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "scripts\installModels.cmd"; DestDir: "{app}\scripts"; Flags: ignoreversion
; Reads a page of print rather than describing a photograph. Optional.
Source: "scripts\installDocumentModel.cmd"; DestDir: "{app}\scripts"; Flags: ignoreversion
; Reads a transcript to find advertisements. Only needed for Remove ads.
Source: "scripts\installTextModel.cmd"; DestDir: "{app}\scripts"; Flags: ignoreversion
; Tesseract reads the words on a scanned page, fast and without inventing any.
Source: "scripts\installTesseract.cmd"; DestDir: "{app}\scripts"; Flags: ignoreversion
; Pandoc writes the Word version. No Microsoft Office is involved.
Source: "scripts\installPandoc.cmd"; DestDir: "{app}\scripts"; Flags: ignoreversion
; The page-break filter, without which a Word version shows "\pagebreak" as
; text instead of breaking the page. Pandoc has none of its own.
Source: "scripts\pagebreak.lua"; DestDir: "{app}\scripts"; Flags: ignoreversion
; PdfPig, which reads PDF files. Referenced rather than embedded, so it has to
; be installed beside the executable.
; PdfPig's seven assemblies.
Source: "exec\*PdfPig*.dll"; DestDir: "{app}\exec"; Flags: ignoreversion skipifsourcedoesntexist
; AND WHATEVER THEY DEPEND ON. Naming six by hand missed a seventh
; (Microsoft.Bcl.HashCode) and HomerScribe threw on the first PDF. The build
; copies whatever the packages declare, so the installer takes what is there
; rather than what somebody remembered.
Source: "exec\System.*.dll"; DestDir: "{app}\exec"; Flags: ignoreversion skipifsourcedoesntexist
Source: "exec\Microsoft.*.dll"; DestDir: "{app}\exec"; Flags: ignoreversion skipifsourcedoesntexist
Source: "exec\HomerScribe.exe.config"; DestDir: "{app}\exec"; Flags: ignoreversion
; No Python is shipped and none is needed. HomerScribe reads a zip of page
; pictures; turning a PDF into those is a solved problem with many free tools,
; and carrying a PDF library to redo it would cost the single-file build.
; installCommon.cmd (homerInstall.cmd before kit 1.42) is the half every other script CALLS. Not shipping it
; meant each one died at its first line, before it could even make a log
; folder -- which is exactly the symptom: a Results box and no logs.
; checkConfig.ps1 and getPdfPig.ps1 are build-time scripts and are not
; shipped: HomerDev is a development-time dependency only, and the installed
; program must run with no kit on the machine (25 Sep 2026).
Source: "scripts\installCommon.cmd"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "scripts\pdfPages.cmd"; DestDir: "{app}\scripts"; Flags: ignoreversion
; A standalone Whisper installer that depends on nothing else and checks
; the files rather than the exit codes. Run it by hand when the ordinary
; route has not worked.
Source: "scripts\getWhisper.cmd"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "scripts\installWhisper.cmd"; DestDir: "{app}\scripts"; Flags: ignoreversion
; THE WORKED EXAMPLES OF A CONTEXT FILE LIVE IN templates (25 Sep 2026). They
; were in a folder named context, which shared its first letter with configs;
; the standard folders carry distinct first letters so a list of them can be
; walked by initial letter. A context file is something a person copies and
; edits for their own film, which is what a template is.
Source: "templates\*.md"; DestDir: "{app}\templates"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist; Excludes: ".git,.venv,__pycache__,*.pyc,venv"
Source: "templates\*.htm"; DestDir: "{app}\templates"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist; Excludes: ".git,.venv,__pycache__,*.pyc,venv"

[Icons]
; WorkingDir is the user's Documents folder, so a run started from a shortcut
; writes its results somewhere writable by default.
Name: "{group}\{#AppName}"; Filename: "{app}\exec\{#AppExeName}"; WorkingDir: "{userdocs}"
Name: "{group}\{#AppName} documentation"; Filename: "{app}\help\ReadMe.htm"; Flags: createonlyiffileexists
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
; Created without asking. The hotkey is mentioned on the launch checkbox at the
; end, which is where the user is looking when it matters.
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\exec\{#AppExeName}"; WorkingDir: "{userdocs}"; HotKey: "{#HotKey}"

[Run]
; FINISH-PAGE ORDER, a HomerDev rule (25 September 2026):
;   1. Install entries, ticked -- screen reader scripts first, then components
;      in alphabetical order (HomerScribe has no screen reader scripts).
;   2. Update entries, ticked, alphabetical.
;   3. Reinstall entries, UNTICKED, alphabetical.
;   4. Launch, ticked.
;   5. Open the user guide, unticked.
; Inno shows [Run] entries in script order and Check: hides the ones that do not
; apply, so three entries per component -- one per verb, each with its own
; Check: -- group the page by themselves. The label function words each one.
; A model has Install and Reinstall only: ollama pull always fetches the
; current model.

; ---- 1. Install --------------------------------------------------------------
FileName: "{app}\scripts\installExifTool.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelExifTool}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated; Check: isInstallExifTool

FileName: "{app}\scripts\installOllama.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelOllama}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated; Check: isInstallOllama

FileName: "{app}\scripts\installPandoc.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelPandoc}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated; Check: isInstallPandoc

FileName: "{app}\scripts\installTextModel.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelTextModel}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated; Check: isModelInstallText

FileName: "{app}\scripts\installModels.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelVisionModel}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated; Check: isModelInstallVision

FileName: "{app}\scripts\installTesseract.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelTesseract}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated; Check: isInstallTesseract

FileName: "{app}\scripts\installWhisper.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelWhisper}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated; Check: isInstallWhisper

; ---- 2. Update ---------------------------------------------------------------
FileName: "{app}\scripts\installExifTool.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelExifTool}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated; Check: isUpdateExifTool

FileName: "{app}\scripts\installOllama.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelOllama}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated; Check: isUpdateOllama

FileName: "{app}\scripts\installPandoc.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelPandoc}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated; Check: isUpdatePandoc

FileName: "{app}\scripts\installTesseract.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelTesseract}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated; Check: isUpdateTesseract

FileName: "{app}\scripts\installWhisper.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelWhisper}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated; Check: isUpdateWhisper

; ---- 3. Reinstall, unticked --------------------------------------------------
FileName: "{app}\scripts\installExifTool.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelExifTool}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated unchecked; Check: isReinstallExifTool

FileName: "{app}\scripts\installOllama.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelOllama}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated unchecked; Check: isReinstallOllama

FileName: "{app}\scripts\installPandoc.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelPandoc}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated unchecked; Check: isReinstallPandoc

FileName: "{app}\scripts\installTextModel.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelTextModel}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated unchecked; Check: isModelReinstallText

FileName: "{app}\scripts\installModels.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelVisionModel}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated unchecked; Check: isModelReinstallVision

FileName: "{app}\scripts\installTesseract.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelTesseract}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated unchecked; Check: isReinstallTesseract

FileName: "{app}\scripts\installWhisper.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelWhisper}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated unchecked; Check: isReinstallWhisper

; ---- 4. Launch, ticked -------------------------------------------------------
FileName: "{cmd}"; \
  Parameters: "/c echo launch > ""{localappdata}\{#AppName}\logs\{#AppName}_launch.flag"""; \
  WorkingDir: "{app}\exec"; \
  Description: "Launch {#AppName} (desktop hotkey {#HotKeyDisplay})"; \
  Flags: postinstall skipifsilent runhidden runasoriginaluser

; ---- 5. Open the user guide, unticked ----------------------------------------
FileName: "{app}\help\ReadMe.htm"; \
  Description: "Open the user guide (F1 in {#AppName})"; \
  Flags: postinstall shellexec nowait skipifsilent skipifdoesntexist runasoriginaluser unchecked

[UninstallDelete]
Type: filesandordirs; Name: "{app}\context"
Type: filesandordirs; Name: "{app}\templates"

; THIS LINE DESTROYED HIS WHISPER, and every reinstall did it again.
;
; installWhisper.cmd used to put Whisper in {localappdata}\HomerScribe\whisper.
; Deleting the whole folder therefore deleted a 500 MB component the user had
; installed deliberately -- on an UPGRADE as much as a removal, because an
; upgrade runs the uninstaller first.
;
; The evidence was in his own logs all along: on 21 August Whisper ran from
; C:\Users\Jamal\AppData\Local\HomerScribe\whisper\whisper-cli.exe, and by
; September that folder held only the working files and the settings.
;
; So the named things go, and nothing else. A folder that may hold something a
; user installed is never removed wholesale.
Type: filesandordirs; Name: "{localappdata}\HomerScribe\work"
Type: files; Name: "{localappdata}\HomerScribe\HomerScribe.inix"
Type: files; Name: "{localappdata}\HomerScribe\HomerScribe.ini"
Type: filesandordirs; Name: "{localappdata}\HomerScribe\logs"

[Code]
// ---- shared component detection -------------------------------------------
// INCLUDED FROM INSIDE [Code], not before it. The kit file carries no section
// header of its own, so these definitions join this section rather than
// starting a competing one.
// The kit comes from the build as /DHomerDev=; compiled by hand, C:\HomerDev.
// (Inside [Code] the language is Pascal: a comment starts with //, never ;.)
#ifndef HomerDev
  #define HomerDev "C:\HomerDev"
#endif
#include HomerDev + "\Templates\HomerComponents.iss"


var
  iOllama, iWhisper, iTesseract, iPandoc, iExifTool: Integer;

procedure InitializeWizard();
begin
  (* name, winget ids (semicolon separated), an exe that answers --version,
     a file that proves it, and three or four words on what it does.

     Whisper and ExifTool have no winget package, so their exe and file do the
     work. Ollama carries two ids because it registers differently depending on
     how it was installed -- the very case that made HomerScribe offer to
     install things he already had. *)
  iOllama    := homerAdd('Ollama', 'Ollama.Ollama',
                         'ollama', '{localappdata}\Programs\Ollama\ollama.exe',
                         'describes video and pictures', 'Ollama');
  iWhisper   := homerAdd('Whisper', '',
                         '"{pf}\Whisper\whisper-cli.exe"', '{pf}\Whisper\whisper-cli.exe',
                         'transcribes speech', '');
  iTesseract := homerAdd('Tesseract', 'UB-Mannheim.TesseractOCR',
                         'tesseract', '{pf}\Tesseract-OCR\tesseract.exe',
                         'reads scanned pages', 'Tesseract-OCR');
  iPandoc    := homerAdd('Pandoc', 'JohnMacFarlane.Pandoc',
                         'pandoc', '{pf}\Pandoc\pandoc.exe',
                         'writes the Word version', 'Pandoc');
  iExifTool  := homerAdd('ExifTool', 'OliverBetz.ExifTool;PhilHarvey.ExifTool',
                         'exiftool', '{pf}\ExifTool\exiftool.exe',
                         'writes descriptions into photographs', 'ExifTool');
end;

function labelWhisper(sParam: String): String;   begin Result := homerLabel(iWhisper); end;
function labelTesseract(sParam: String): String; begin Result := homerLabel(iTesseract); end;
function labelPandoc(sParam: String): String;    begin Result := homerLabel(iPandoc); end;
function labelExifTool(sParam: String): String;  begin Result := homerLabel(iExifTool); end;
function labelOllama(sParam: String): String;    begin Result := homerLabel(iOllama); end;

function isInstallExifTool(): Boolean;   begin Result := homerIs(iExifTool, 0); end;
function isUpdateExifTool(): Boolean;    begin Result := homerIs(iExifTool, 1); end;
function isReinstallExifTool(): Boolean; begin Result := homerIs(iExifTool, 2); end;
function isInstallOllama(): Boolean;   begin Result := homerIs(iOllama, 0); end;
function isUpdateOllama(): Boolean;    begin Result := homerIs(iOllama, 1); end;
function isReinstallOllama(): Boolean; begin Result := homerIs(iOllama, 2); end;
function isInstallPandoc(): Boolean;   begin Result := homerIs(iPandoc, 0); end;
function isUpdatePandoc(): Boolean;    begin Result := homerIs(iPandoc, 1); end;
function isReinstallPandoc(): Boolean; begin Result := homerIs(iPandoc, 2); end;
function isInstallTesseract(): Boolean;   begin Result := homerIs(iTesseract, 0); end;
function isUpdateTesseract(): Boolean;    begin Result := homerIs(iTesseract, 1); end;
function isReinstallTesseract(): Boolean; begin Result := homerIs(iTesseract, 2); end;
function isInstallWhisper(): Boolean;   begin Result := homerIs(iWhisper, 0); end;
function isUpdateWhisper(): Boolean;    begin Result := homerIs(iWhisper, 1); end;
function isReinstallWhisper(): Boolean; begin Result := homerIs(iWhisper, 2); end;

// The two models are not components in the table: they live inside Ollama.
function labelVisionModel(sParam: String): String;
begin Result := homerModelLabel('qwen2.5vl:7b', 'describes video and pictures', 'about 5.5 GB'); end;
function labelTextModel(sParam: String): String;
begin Result := homerModelLabel('qwen2.5:7b', 'removes advertisements', 'about 4.7 GB'); end;
function isModelInstallVision(): Boolean;   begin Result := homerModelIs('qwen2.5vl:7b', False); end;
function isModelReinstallVision(): Boolean; begin Result := homerModelIs('qwen2.5vl:7b', True); end;
function isModelInstallText(): Boolean;     begin Result := homerModelIs('qwen2.5:7b', False); end;
function isModelReinstallText(): Boolean;   begin Result := homerModelIs('qwen2.5:7b', True); end;

(* THE RESULTS BOX.
   HomerScribe had no [Code] section at all, which is why an installation ended
   with no summary: the wizard closed and the only record of what had happened
   was in a log nobody had been told about.

   DbDo's rule is the one followed here: the box is built from what the
   installation actually DID, so a component that was already present and needed
   nothing is never mentioned. A box listing seven things every time teaches the
   reader to ignore it.

   It is not a checkbox on the finish page. It is not optional and it must run
   last of all, so it is shown from code after every other step has finished. *)

var
  sActions: String;

procedure addAction(sText: String);
begin
  if sText = '' then exit;
  if sText = '' then exit;
  if sActions <> '' then sActions := sActions + #13#10;
  sActions := sActions + '  ' + sText;
end;

procedure startIfAsked();
(* Starts the program if the finish page asked for it, and removes the marker
   so a later run does not start it again.

   runasoriginaluser on the marker entry matters: the installer is elevated, and
   a program started from it would run as administrator and write its settings
   into the wrong profile. *)
var
  sFlag: String;
  iResult: Integer;
begin
  sFlag := ExpandConstant('{localappdata}\{#AppName}\logs\{#AppName}_launch.flag');
  if not FileExists(sFlag) then exit;
  DeleteFile(sFlag);
  (* TWO pairs of quotes, not one. cmd /s strips the outermost pair and runs
     what is left verbatim, so with one pair a path under Program Files arrives
     unquoted and cmd tries to run "C:\Program". *)
  Exec(ExpandConstant('{cmd}'),
       '/s /c ""' + ExpandConstant('{app}\exec\{#AppExeName}') + '""',
       ExpandConstant('{userdocs}'), SW_SHOW, ewNoWait, iResult);
end;

procedure reportWhatHappened();
(* ONE LINE PER BOX THAT WAS TICKED, AND NOTHING ELSE. On 25 September 2026
   this box listed every component from the probe made when the wizard opened:
   it said Whisper was not installed and Ollama was out of date, a minute after
   both scripts had succeeded, and it recited three components nobody had
   asked about. A result box reports the actions taken this session, from a
   probe made after they ran. The kit records the ticked boxes when Finish is
   pressed (homerNoteTicked, called from NextButtonClick) and probes again
   only those. *)
var
  sBody: String;
begin
  addAction(homerOutcomeLine(iOllama));
  addAction(homerModelOutcomeLine('qwen2.5vl:7b', 'describes video and pictures', 'about 5.5 GB'));
  addAction(homerModelOutcomeLine('qwen2.5:7b', 'removes advertisements', 'about 4.7 GB'));
  addAction(homerOutcomeLine(iExifTool));
  addAction(homerOutcomeLine(iPandoc));
  addAction(homerOutcomeLine(iTesseract));
  addAction(homerOutcomeLine(iWhisper));

  sBody := 'HomerScribe {#AppVersion} is installed.';
  if sActions <> '' then sBody := sBody + #13#10 + #13#10 + sActions;
  sBody := sBody + #13#10 + #13#10
         + 'Logs are kept in ' + ExpandConstant('{localappdata}\{#AppName}\logs') + '.';
  homerResultsBox(sBody);

  (* THE BOX HAS BEEN READ AND CLOSED. Only now does the program start. *)
  startIfAsked();
end;

procedure keepTheSetupLog();
(* Inno's own log lands in %TEMP% under a name nobody would guess. It is copied
   to the Homer logs folder so it sits beside the runtime logs and the install
   scripts' logs -- one folder to zip when something needs diagnosing. *)
var
  sLogs, sTo: String;
begin
  sLogs := ExpandConstant('{localappdata}\{#AppName}\logs');
  if not DirExists(sLogs) then ForceDirectories(sLogs);
  sTo := sLogs + '\{#AppName}-setup-' + GetDateTimeString('yyyymmdd-hhnnss', #0, #0) + '.log';
  if not FileCopy(ExpandConstant('{log}'), sTo, False) then
    MsgBox('The setup log could not be copied to ' + sLogs + '.', mbInformation, MB_OK);
end;

function NextButtonClick(CurPageID: Integer): Boolean;
(* Finish pressed: the boxes are settled, the scripts have not yet run. *)
begin
  Result := True;
  if CurPageID = wpFinished then homerNoteTicked();
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssDone then
  begin
    keepTheSetupLog();
    reportWhatHappened();
  end;
end;
