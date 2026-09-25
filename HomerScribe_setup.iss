; HomerScribe_setup.iss -- installer for HomerScribe.
;
; Follows the pattern of 2htm, extCheck, urlFido, bookFido and helpFido:
; per-machine install under Program Files, no "who is this for" question, the
; destination page always shown, a desktop shortcut with a hotkey, and the
; optional extras as checkboxes on the final page.
;
; ---- Version -----------------------------------------------------------------
; The version number is NOT stored in this script. It lives in version.txt, one
; line, which buildHomerScribe.cmd increments on every build. Inno reads it
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
Source: "{#AppExeName}"; DestDir: "{app}\exec"; Flags: ignoreversion
; Every line below the program itself carries skipifsourcedoesntexist. Only
; HomerScribe.exe is genuinely required; a missing document or an empty
; context folder must not abort a build, and a wildcard matching nothing is a
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

; The companion programs, packaged when they are present in the build folder.
; buildHomerScribe.cmd downloads them when they are missing, so normally they
; are here. skipifsourcedoesntexist means a build without them still succeeds;
; HomerScribe then looks on the PATH instead. Installing them beside
; HomerScribe.exe is what makes the program work with nothing else set up,
; since its own folder is the first place it looks.
; ExifTool writes the descriptions into the pictures. A SINGLE FILE only --
; no "exiftool_files" folder is packaged, deliberately. See License.md and
; the ExifTool section of buildHomerScribe.cmd for why.
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
Source: "*PdfPig*.dll"; DestDir: "{app}\exec"; Flags: ignoreversion skipifsourcedoesntexist
; AND WHATEVER THEY DEPEND ON. Naming six by hand missed a seventh
; (Microsoft.Bcl.HashCode) and HomerScribe threw on the first PDF. The build
; copies whatever the packages declare, so the installer takes what is there
; rather than what somebody remembered.
Source: "System.*.dll"; DestDir: "{app}\exec"; Flags: ignoreversion skipifsourcedoesntexist
Source: "Microsoft.*.dll"; DestDir: "{app}\exec"; Flags: ignoreversion skipifsourcedoesntexist
Source: "HomerScribe.exe.config"; DestDir: "{app}\exec"; Flags: ignoreversion
; No Python is shipped and none is needed. HomerScribe reads a zip of page
; pictures; turning a PDF into those is a solved problem with many free tools,
; and carrying a PDF library to redo it would cost the single-file build.
; homerInstall.cmd is the half every other script CALLS. Not shipping it
; meant each one died at its first line, before it could even make a log
; folder -- which is exactly the symptom: a Results box and no logs.
Source: "scripts\checkConfig.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "scripts\getPdfPig.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "scripts\homerInstall.cmd"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "scripts\pdfPages.cmd"; DestDir: "{app}\scripts"; Flags: ignoreversion
; A standalone Whisper installer that depends on nothing else and checks
; the files rather than the exit codes. Run it by hand when the ordinary
; route has not worked.
Source: "scripts\getWhisper.cmd"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "scripts\installWhisper.cmd"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "context\*.md"; DestDir: "{app}\context"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist
Source: "context\*.htm"; DestDir: "{app}\context"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist

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
; TWO ENTRIES PER COMPONENT, and why.
;
; In Inno, Check: decides whether an entry is SHOWN, not whether it is ticked.
; With one entry per component gated on "wanted", every component already on
; the machine simply vanished from the finish page -- which is why no Reinstall
; boxes and no model boxes appeared on his run. DbDo had three entries per
; component for exactly this reason, and I collapsed them to one.
;
; So each component has two entries with the same label function: one shown
; when it is wanted (missing or stale), ticked; one shown when it is current,
; unticked. The label function says Install, Update or Reinstall as the case
; is, so the reader sees one box per component with the right verb and the
; right default.
; cmd NEEDS /s WITH THE DOUBLED QUOTES. Without it, cmd took
; ""C:\Program Files\...\installWhisper.cmd"" apart at the spaces and gave up:
; every one of these exited with code 1 in a tenth of a second, having run
; nothing, while the finish page reported them as done. With /s cmd strips the
; outermost pair and runs what is left verbatim, which is the whole point of
; writing the path that way. DbDo learned this in its 1.0.168.
; THE CONSOLE STAYS VISIBLE. An earlier attempt hid these windows, which was
; the wrong reading of the pattern: the console is not noise to be suppressed,
; it is where a person sees what is happening. It says, briefly and in plain
; words, what was found and what was done. The DETAIL -- every command, exit
; code and path -- goes to %LOCALAPPDATA%\HomerScribe\logs, where it can be
; zipped and sent when something needs diagnosing.
;
; What is suppressed is only the waiting: HOMER_QUIET stops a script pausing
; for a key, since the installer is driving it and nobody is watching for a
; prompt.
; Post-install checkboxes shown on the final wizard page. What HomerScribe
; needs comes first, then what to do next. All four default to checked; any can
; be unchecked to skip.
;
; helpFido leaves its Ollama box unchecked because helpFido works without it.
; HomerScribe does not: with no local model there is nothing to write the
; descriptions. Both are shown every time rather than being hidden when already
; present -- the scripts themselves notice what is installed and say so in a
; second, and a checkbox that sometimes vanishes is worse than one that
; occasionally has nothing to do.
;
; The installs happen here rather than by sending the user to a download page:
; winget ships with Windows 10 and 11 and can fetch Ollama unattended.
; runascurrentuser matters -- winget and ollama install per-user, into the
; profile of whoever is signed in, and this installer is running elevated.

FileName: "{app}\scripts\installOllama.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelOllama}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated; Check: wantOllama
FileName: "{app}\scripts\installOllama.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelOllama}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated unchecked; Check: not wantOllama

FileName: "{app}\scripts\installModels.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelVisionModel}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated; Check: wantVisionModel
FileName: "{app}\scripts\installModels.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelVisionModel}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated unchecked; Check: not wantVisionModel

FileName: "{app}\scripts\installTextModel.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelTextModel}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated; Check: wantTextModel
FileName: "{app}\scripts\installTextModel.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelTextModel}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated unchecked; Check: not wantTextModel

FileName: "{app}\scripts\installExifTool.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelExifTool}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated; Check: wantExifTool
FileName: "{app}\scripts\installExifTool.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelExifTool}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated unchecked; Check: not wantExifTool

FileName: "{app}\scripts\installPandoc.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelPandoc}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated; Check: wantPandoc
FileName: "{app}\scripts\installPandoc.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelPandoc}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated unchecked; Check: not wantPandoc

FileName: "{app}\scripts\installTesseract.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelTesseract}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated; Check: wantTesseract
FileName: "{app}\scripts\installTesseract.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelTesseract}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated unchecked; Check: not wantTesseract

FileName: "{app}\scripts\installWhisper.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelWhisper}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated; Check: wantWhisper
FileName: "{app}\scripts\installWhisper.cmd"; \
  Parameters: "noPause"; \
  WorkingDir: "{app}\scripts"; \
  Description: "{code:labelWhisper}"; \
  Flags: postinstall skipifsilent runascurrentuser waituntilterminated unchecked; Check: not wantWhisper

; THE LAUNCH IS RECORDED HERE AND HAPPENS AFTER THE RESULTS BOX.
;
; Starting the program from this entry puts its window on top of a box the user
; has not read yet -- and for a screen reader user that is worse than untidy:
; the new window takes focus, the reader begins announcing it, and the summary
; of what the installer just did is buried behind it.
;
; Inno runs postinstall entries BEFORE CurStepChanged(ssDone), so no ordering of
; the entries can fix this. The entry therefore leaves a marker, and the Results
; box starts the program once it has been closed. The checkbox reads the same
; either way. DbDo has worked this way since its 1.0.145.
FileName: "{cmd}"; \
  Parameters: "/c echo launch > ""{localappdata}\{#AppName}\logs\{#AppName}_launch.flag"""; \
  WorkingDir: "{app}\exec"; \
  Description: "Launch {#AppName} now (desktop hotkey: {#HotKeyDisplay})"; \
  Flags: postinstall skipifsilent runhidden runasoriginaluser

FileName: "{app}\help\ReadMe.htm"; \
  Description: "Open the user guide (F1 opens it inside {#AppName})"; \
  Flags: postinstall shellexec nowait skipifsilent skipifdoesntexist runasoriginaluser unchecked

[UninstallDelete]
Type: filesandordirs; Name: "{app}\context"

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
#include "C:\HomerDev\Templates\HomerComponents.iss"


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

function wantWhisper(): Boolean;   begin Result := homerWanted(iWhisper); end;
function wantTesseract(): Boolean; begin Result := homerWanted(iTesseract); end;
function wantPandoc(): Boolean;    begin Result := homerWanted(iPandoc); end;
function wantExifTool(): Boolean;  begin Result := homerWanted(iExifTool); end;
function wantOllama(): Boolean;    begin Result := homerWanted(iOllama); end;

// The two models are not components in the table: they live inside Ollama.
function labelVisionModel(sParam: String): String;
begin Result := homerModelLabel('qwen2.5vl:7b', 'describes video and pictures', 'about 5.5 GB'); end;
function labelTextModel(sParam: String): String;
begin Result := homerModelLabel('qwen2.5:7b', 'removes advertisements', 'about 4.7 GB'); end;
function wantVisionModel(): Boolean; begin Result := homerModelWanted('qwen2.5vl:7b'); end;
function wantTextModel(): Boolean;   begin Result := homerModelWanted('qwen2.5:7b'); end;

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
(* Built from the shared component state, so the box says what IS true rather
   than what was attempted -- and says it in the same words the checkbox used,
   from the same probe. Two places telling different stories is worse than one
   telling none. *)
var
  sBody: String;
begin
  addAction(homerSummaryLine(iWhisper,
    'Speech cannot be transcribed until scripts\installWhisper.cmd is run.'));
  addAction(homerSummaryLine(iTesseract,
    'Scanned PDFs will be read by the picture model, which is much slower.'));
  addAction(homerSummaryLine(iPandoc,
    'A PDF will produce Markdown only, with no Word version.'));
  addAction(homerSummaryLine(iExifTool,
    'Descriptions cannot be written into photographs.'));
  addAction(homerSummaryLine(iOllama,
    'Video and pictures cannot be described.'));

  sBody := 'HomerScribe {#AppVersion} is installed.' + #13#10 + #13#10
         + sActions + #13#10 + #13#10
         + 'Logs are kept in ' + ExpandConstant('{localappdata}\{#AppName}\logs') + '.';
  MsgBox(sBody, mbInformation, MB_OK);

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

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssDone then
  begin
    keepTheSetupLog();
    reportWhatHappened();
  end;
end;
