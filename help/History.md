# HomerScribe History



## 9 October 2026 -- from the build and release logs

- **Ten walks of about three minutes.** Walks 2 to 8 were lengthened on 8 October with what each feature writes and how it decides -- transcripts' sources, list files and playlists, the files a description writes, how advertisements are judged, scanned and text PDFs, reading Help -- but those files never reached the project; they come now. Each predicts three minutes or more.
- **The installer's per-user writes are acknowledged,** as for DbDo: the launch flag and logs folder are written for the person running setup, on purpose, and UsedUserAreasWarning=no now says so.

## Unreleased -- 9 October 2026

- **PdfRead.cs is HomerScribe's own again.** The file that reads a PDF with PdfPig, a partial class of HomerScribe, had been kept in the kit's exec\CSharp as though it were a shared class, though no other program could use it. It now sits beside HomerScribe.cs, is compiled from this folder and is named in RepoFiles; the build no longer looks for it in the kit, and its clean-up of old top-level kit classes no longer names it. The kit retires its copy from version 1.65.3. Nothing changes in how a PDF is read.

## 8 October 2026 -- an audit by another AI

ChatGPT audited HomerScribe and reported 41 findings. Checked against the code, these held and are fixed:

- **A film is finished only when it is written.** The description track and the final film each report success or failure, and both were ignored: the work was marked finished and the film announced even when either failed. Now a failure keeps the descriptions as unfinished, says so, and fails the job; running again only rebuilds the film.
- **No gap when a film is replaced.** The finished file was deleted before the new one was moved in, so a failure between the two left neither. The new one now replaces the old in one step.
- **Removing advertisements never drops programme.** A kept piece that could not be cut was skipped and the rest joined, quietly losing part of the programme; now the edit stops and the earlier copy stays. A copy that could not be written no longer says "no advertisement was certain enough"; it says it could not be written, and the job fails.
- **The Word version of a PDF.** Its page-break filter was looked for beside the program, in exec, while it belongs in scripts -- and the installer had not carried it at all. Both are fixed. A Word version Pandoc cannot make now fails that part of the job instead of passing quietly.
- **Two pictures with one name.** In an archive, a/photo.jpg and b/photo.jpg became one file, and both descriptions described the second. A later picture with an earlier one's name now takes its folders into its name.
- **What reaches the internet.** The ReadMe said nothing leaves the machine. It now says exactly what does, and when: a download from a web address you give, installing and updating parts, and, only with Web context on, a video's title to its page or Wikipedia.
- **The installer** uses CopyFile, the current name for FileCopy.
- **Kit tools** updated from HomerDev 1.63.3: a straight quote no longer silently drops a tutorial sentence, and walks 5 and 8, which lost one each, are spoken again at the next build; push stops when its whitelist cannot be rewritten, and reports a failed commit as a failure.

Left for later, as larger changes: a manifest that ties cached work and outputs to their exact source; building a forced rerun beside the earlier output rather than over it; locks for simultaneous runs; a check of every advertisement boundary; page provenance for tagged PDFs; and longer task walks, six of which run under three minutes.

## 8 October 2026 -- tutorials in the pattern of ten

The tutorials follow the Homer pattern of ten: 0 Overview, now teaching Insert plus Tab; 1 User Interface, new, the dialog, its Alt letters, the keys that read a field, progress keys and help; 2 Install and Launch, new, the installer's tool boxes; the five task walks as 3 to 7; 8 Stay Current, the help and newer-version walk; and 9 Conclusion, new. The tutorials' audio is no longer kept local: LocalFiles.txt no longer names help/tutorials, so the repository carries it as the installer does.

## 1.0.266, 4 October 2026 -- what 122 podcasts in one run taught

The run -- 54 hours of audio, 122 files, 14 hours -- finished with no
warning and no error, and the program's own overhead between files was
three seconds in all: the time was Whisper's, at a quarter of real time
on the four threads it uses unless told otherwise. So Whisper now runs on
half the machine's logical processors, never fewer than four (the new
`whisper-threads` setting overrides), and its own account of each file --
threads, detected language, encode and decode time -- goes to the log.
Each spoken stretch is logged once, not twice, which halves a long run's
log. The results box leads with the run in one line -- hours of audio,
spoken stretches, the time as a percentage of the audio -- before the list
of files, and the same text is written beside the log as a results file
so a long list can be read and kept after the box is closed.

## 1.0.258, 27 September 2026 -- setup wording

Numbered as version.txt (1.0.257) plus one.

- **Setup.** The Results box at the end of setup is titled "HomerScribe Setup Results", and the finish page uses the Homer wording: the verb first, no "recommended", and "Launch HomerScribe (desktop hotkey ...)".

## 1.0.257, 26 September 2026 -- built with HomerDev 1.43.19

Numbered as version.txt (1.0.256) plus one, the number this build stamps.

- **The kit's tools carry the day's fixes.** `scripts\tidy` keeps a file where the project says it lives and untracks only what RepoFiles.txt leaves out; `scripts\check` reads keys and access letters without false alarms, reads only the project's own files, and never waits for a key; and `scripts\release` publishes a draft and confirms the release is GitHub's latest before calling it published.

## 1.0.256, 26 September 2026 -- brought up to HomerDev 1.43.15

Numbered as version.txt (1.0.255) plus one, the number this build stamps.

- **The kit's scripts under their plain names.** The build refreshes check, push, release, tidy, unpushed, finish and installCommon into scripts and deletes the old checkHomerApp, gitPush, gitUnpushed, homerFinish, homerInstall, homerTidy and tagRelease copies. HomerScribe's own seven install scripts and the installer use installCommon.cmd, the shared half of every install script; under its old name it would soon have stopped being refreshed, and every install script would have stopped with "missing".
- **The installer is written to the top of the project**, where the kit's release script looks for it, and finds the kit's HomerComponents.iss in the kit folder the build passes it rather than at a fixed C:\HomerDev.
- **Tidier project.** The copies of the kit's classes, and the copies of the install scripts, getPdfPig.ps1 and pagebreak.lua left at the top from the layout before, are removed; a stray check report in scripts goes. What git carries follows RepoFiles.txt: version.txt, the notes and packages folders and the repository bootstrap stay on this disk, and `.gitattributes` keeps the Homer CRLF line endings as they are.

## 1.0.238, 24 September 2026

Everything below is in ONE build. The entries had been numbered 1.0.275 to
1.0.283, which were numbers I invented: `version.txt` says 1.0.237 and the
build script bumps it to 238.

That is not a tidy-up. **It made me misread his own logs.** Twice I saw
"HomerScribe 1.0.237 starting", compared it against a History claiming the
280s, and told him he was running a build that predated my work. He was
running my work. The version number is the one thing that ties a log to a
change, and I had made it fiction.









### 25 September, closing: everything built goes to exec

The program, its config file, the PdfPig assemblies and the installer are
written to `exec` now, never to the project root, and the build no longer
copies ffmpeg, ffprobe or yt-dlp into the project at all: they are shared,
machine-wide components found on the PATH. Every tidy had been carrying the
same five root files off again. The quick test after a build is
`exec\HomerScribe.exe`. `RepoFiles.txt` names no binary.

### 25 September, closing: one tool per job

The kit retired the scripts whose names sounded like another's: `cleanDir`
and `tidyRepo` are `homerTidy`; `gitRelease` is `tagRelease`, which runs the
checks itself now; `sayTutorial` is `buildTutorials`; `installTools`, which
put copies in `C:\bin` that went stale, is gone. The build removes any retired
copy HomerScribe still carries. Kit 1.38.0.

### 25 September, last: the context folder, and the reader's voice

**`context` is `templates`.** The worked example of a context file lives in
`templates` now: a folder named context shared its first letter with
configs, and the standard folders carry distinct first letters so a list of
them can be walked by initial letter. The build moves what was in `context`
into `templates` once and removes the empty folder.

**The walks, faster and even.** The reader speaks through piper while the
narrator keeps Kokoro, which halves the minutes; long lines are cut at
sentences and commas before Kokoro sees them; every piece is brought to one
loudness, so the reader is no longer quieter than the narrator; and the
build says "step N of M" every four steps while a walk is being spoken.
`installOllama.cmd` and `homerInstall.cmd` are now refreshed from the kit on
every build, like the other shared scripts.

### 25 September, late: the stopped push, and the release command

**The push you stopped.** The first tutorial build had left the voices in
HomerScribe's own scripts folder; the tidy called all 486 files strays, moved
them into notes, and, with no RepoFiles.txt to whitelist against, committed
them all. Three fixes in kit 1.35.0: only buildHomerDev fetches voices, and
into C:\HomerDev\exec; homerTidy deletes fetched things rather than archiving
them; and it stages nothing without RepoFiles.txt. HomerScribe now has
RepoFiles.txt and LocalFiles.txt, and `scripts\gitUnpushed` undoes that commit
while keeping every file, ready for a proper tidy.

**The release command is `scripts\tagRelease`.** The build refreshes the
kit's tools into scripts on every build: tagRelease, gitRelease (check, then
release), gitPush, gitUnpushed, homerTidy, checkHomerApp and the tutorial
tools. The copy of tagRelease in C:\bin is an older edition; this one logs to
logs and works for every Homer app.

### 25 September, night: the tutorials, and the version the zip clobbered

**Seven spoken walks.** Overview, transcribe a recording, describe a video, a
web address for sound only, remove the advertisements, read a PDF, and the
Help box with a newer version. Two voices: a narrator at work and a screen
reader answering, in the four beats a trainer uses -- say the key, press it,
hear the reader word for word, then say what it meant. The reader's lines
follow the grammar of a real reader at its middle verbosity, taken from two
training classes read back through HomerScribe; no reader is named. The
audio is one `.mp3` per walk in `help\tutorials`, made by the build whenever a
walk has none, with the tools refreshed from HomerDev on every build. The
voices are Kokoro through sherpa-onnx when they can be fetched, piper
otherwise; both licensed so the audio can be published under MIT. They are
fetched once into C:\\HomerDev\\exec and shared by every Homer app. The kit's tools
are now in its scripts folder, like every app's. Kit 1.34.0.

**"Already released" from tagRelease.** The delivery zip carried
`version.txt` at 1.0.237, so every unarchiving put the number back and the
build stepped to the 1.0.238 that was already out. The zip no longer carries
`version.txt`; the copy on the machine is the only one.

### 25 September, evening: two patterns for every Homer app

**Names Whisper should spell right.** The JAWS training recording came back
with "Joss" eight times. Whisper spells a name it has never read the way it
sounds, and its own remedy is an initial prompt: names in it are spelled as
given. The new `--vocabulary` setting is that prompt, starting with the
screen readers and the Homer programs; add your own.

**Both builds failed on the first try of the new kit** -- a Pascal
procedure declared as a function, and sample builds without Elevate.cs.
Kit 1.31.1 fixes both; this build requires it.

**"Took 19:00" read as nineteen hundred hours.** The run took 19 minutes.
Every length a person reads or hears -- how long a run took, how long a film
runs, how much speech was heard, how much advertising was cut -- is now said
in words: "19 minutes", "1 hour and 6 minutes", "45 seconds". A clock reading
stays only where it marks a place in the film, as in a caption's "[12:34]".
The words come from Util.spokenLength in HomerDev, so every Homer app can say
lengths the same way.

**The finish page is now in a fixed order.** Install boxes first (ticked;
screen reader scripts would lead, then components alphabetically), then
Update (ticked), then Reinstall (unticked), then Launch (ticked), then Open
the user guide (unticked). Each component has three entries, one per verb;
the installer shows the one that applies. HomerScribe's page: ExifTool,
Ollama, Pandoc, the two models, Tesseract, Whisper -- each under the verb its
state calls for.

**The Help box checks for a newer version.** Press Help (or F1) in the main
dialog and the box ends with what version this is and whether a newer one is
on the web. Its buttons are Yes and No: Yes is the default when a newer
version exists, No when this is the newest, and OK alone when the web could
not be reached. Yes fetches the setup program from the latest release and
starts it. This is the shared Elevate class in HomerDev 1.31.0, so EdSharp
and FileDir can carry the same box.

### 25 September, afternoon: three things from the first successful install

**Enter in the source paths field opened Help.** The dialog listed its
buttons as Help, Default settings, OK, Cancel -- and Lbc makes the first one
the default. OK is now first; Lbc adds Help itself, rightmost.

**The Results box told the old story.** A minute after Whisper was installed
and Ollama updated from 0.34.3 to 0.34.4, the box said Whisper was not
installed and Ollama was out of date, and it recited Tesseract, Pandoc and
ExifTool, which nobody had ticked. It was reading the probe made when the
wizard opened. It now reports one past-tense line per ticked box, from a probe
made after the scripts ran, and nothing about a box that was not ticked. Kit
1.30.0 carries the mechanism.

**"Checking for a newer version" hid a 1 GB download.** Every install script
now says "Downloading", with a rough size and time, before any long step.

### 25 September: a BOM where it cannot go, and the HomerDev logs

**The build refused a kit of exactly the version it asked for.** The log
said "needs HomerDev 1.28.0 or later, and the kit is 1.28.0" -- with three
invisible bytes in front of the second number. The evening before, every kit
file had been brought to UTF-8 with a byte order mark, version.txt included.
But version.txt is read by cmd's `set /p`, which cannot strip a mark, so the
mark went into the version compare and broke it. HomerDev's own build says
so: "version.txt: should not have a byte order mark". The mark is removed,
and the build now reads the kit version through PowerShell, which drops a
mark and trims, so this cannot recur however the file is saved.

The rule, stated once: a file that cmd.exe reads -- a .cmd, a .bat, or a
value file read with `set /p` -- takes no byte order mark. Everything else
does.

**The kit files were in the wrong folders.** HomerComponents.iss and
homerInstall.cmd had been delivered under Inno\\ and Scripts\\, and two documents
under Docs\\ -- folders HomerDev never had. Kit 1.29.0 puts them in Templates\\
and help\\, and the installer now includes HomerComponents.iss from Templates.
The build requires kit 1.29.0.

**HomerDev's build logs did not follow the convention.** buildHomerDev.py
wrote a fixed buildHomerDev.log at the kit root, and the four sample builds
wrote build<App>.log beside themselves -- each run overwriting the one
before. Tools\fixHomerDevLogs.cmd moves them: HomerDev-build-yyyyMMdd-HHmmss.log
in logs\, and <App>-build-yyyyMMdd-HHmmss.log in each sample's own logs\.

They are replacement files in HomerDev.zip, and the kit's own move list
removes the old logs and the old folders on the next build -- no patch script,
no hand deletions. Tested on a copy of the kit laid out as the machine was:
ten files and folders removed, the log in logs\\, 0 problems in shipped files.

### The regression, found in the first install log that ever got written

    No Windows build was listed in the latest release

whisper.cpp changed its release process on 20 August 2026. A version tag such
as v1.9.4 now carries only the two source archives; the compiled binaries are
published under nightly tags such as b5130, marked pre-release. GitHub's
`/releases/latest` returns the newest release that is NOT a pre-release -- so
it returned v1.9.4, whose two assets hold no Windows build.

The download now reads the release LIST, newest first, and takes the first
release carrying a Windows x64 CPU build under either naming scheme
(`whisper-bin-win-<variant>-x64.zip` or the older `whisper-bin-x64.zip`),
skipping CUDA, Vulkan and arm builds. If the API cannot be read at all, a
pinned release known to carry `whisper-bin-x64.zip` is fetched directly.

### Three more from the same logs

- **installOllama did nothing on "Update".** It found Ollama, said "already
  installed", and exited in a second -- which is the too-fast update he
  noticed. It now runs `winget upgrade` when Ollama is present.
- **Reinstall boxes were hidden, not unticked.** In Inno, Check: controls
  visibility. One entry per component gated on "wanted" made every present
  component vanish from the page, models included. Each component now has two
  entries: wanted and ticked, or current and unticked.
- **Kit files broke the encoding standard.** Docs, homerTidy.py and
  homerTidy.cmd lacked a BOM or CRLF endings; HomerDev's own build reported
  it. All brought to UTF-8 with BOM and CRLF, .cmd files CRLF without BOM.

### The regression that stopped transcription, found by tracing the whole path

`whisperModelPath()` looked for `ggml-small.bin` in two places: the old
per-user folder, and beside HomerScribe.exe. **Never in C:\Program Files\Whisper**,
where installWhisper.cmd has put it since the machine-wide move. So the program
was found there and the model was not, and every run ended with "ggml-small.bin
was not found" -- which his logs said all along.

The model is now looked for BESIDE whisper-cli.exe first, wherever findTool
found it, then in every shared-tool folder, then the old places.

### Three more, from reading every line rather than patching

- **The install logs went to the wrong folder.** homerInstall.cmd worked the app
  name out with `~nx` of a path ending in `..`, which can yield `..` itself, so
  the log landed in `%LOCALAPPDATA%\..\logs`. It now resolves the parent with
  `~f` first, and needs no delayed expansion -- five of the eight callers never
  enabled it.
- **The Whisper downloads did not log.** Both PowerShell blocks now append to
  the log, record which release asset they chose, and each failure path writes
  a line saying so.
- **I briefly added `setlocal` to homerInstall.cmd** while fixing the above,
  which would have discarded every variable it exists to pass back. Caught and
  removed in the same turn; recorded because it is the kind of thing that
  survives when only the console says "ok".

### The installer writes its own log now

He asked for one, and it is a HomerDev requirement. `SetupLogging=yes` makes
Inno keep a log; it lands in `%TEMP%` under a name nobody would guess, so the
`[Code]` section copies it at `ssDone` to

    %LOCALAPPDATA%\HomerScribe\logs\HomerScribe-setup-yyyymmdd-hhnnss.log

beside the runtime logs and the install scripts' logs. One folder to zip.

### And the version evidence

His runtime log says:

    HomerScribe 1.0.237 starting
    Program: C:\program files\homerscribe\exec\homerscribe.exe
    Whisper was not found. Run installWhisper.cmd in the program folder.

Two facts sit awkwardly together. The **exec** path is an installer change of
mine, so the installer he ran is recent. But "in the program folder" is the OLD
wording -- the source has said "in the HomerScribe scripts folder" since the
machine-wide work -- and the version is 237, where his own build log showed
`1.0.237 -> 1.0.238`.

**So the executable he is running was built before that build, and the source
changes are not in it.** The installer changes landed and the compiled changes
did not, which is exactly what a build that did not run, or did not replace the
exe, looks like.

### A stale binary should say so

The "not found" message now lists **every folder it actually searched**. On the
current source that is a dozen machine-wide locations; on the binary he is
running it is the old sentence. One line of a log will now distinguish them
without anybody having to reason about version numbers -- which is the trap I
fell into for six rounds.

### The regression, found at last: the uninstaller deleted Whisper

He asked for an earlier version that worked, and what was different. The answer
was in his own logs.

**21 August, working:**

    C:\Users\Jamal\AppData\Local\HomerScribe\whisper\whisper-cli.exe -m ...

**24 September:** that folder holds `work` and `HomerScribe.inix`. Nothing else.

The cause is one line in this installer:

    Type: filesandordirs; Name: "{localappdata}\HomerScribe"

`installWhisper.cmd` put Whisper in `{localappdata}\HomerScribe\whisper`, and
the uninstaller removed the whole folder. **An upgrade runs the uninstaller
first**, so every reinstall destroyed a 500 MB component he had installed
deliberately -- and then the installer offered to fetch it again, which is why
it looked like the installer was failing rather than succeeding and being
undone.

EdSharp, FileDir and DbDo never did this: their uninstallers name individual
files. HomerScribe was the outlier, as it has been in every other part of this
week's work.

It now removes the four named things -- `work`, the settings file, the old
`.ini`, and `logs` -- and nothing else. **A folder that may hold something a
user installed is never removed wholesale.**

### Why my own logs did not resolve it

The log said "whisper-cli.exe was not found", which was TRUE and was not the
fault. I treated the symptom as the bug for six rounds, and moving Whisper to
Program Files made it survive the next uninstall without ever explaining why it
had gone.

**What settled it was comparing a working log with a failing one** -- which is
what he asked for this turn, and what I should have done first. A regression has
a before and an after, and the before was sitting in the same folder as the
after the whole time.

###  -- the file that was never shipped

He got a Results box and no log folder. That combination is the whole
diagnosis: the installer ran, and the install scripts did not.

**`homerInstall.cmd` was never named in the installer.** I added it to the
project last build, pointed all eight scripts at it, and did not add a
`Source:` line. So every script called a file that was not there and died at
that line -- before it could create the log folder, before it could print
anything.

Auditing the rest of the manifest found more of the same: `checkConfig.ps1`,
`getPdfPig.ps1` and `pdfPages.cmd` were also unshipped, and **ten help files**.
Fourteen files in the project and not in the installer.

### The check that should have caught it

Comparing every file in `scripts\` and `help\` against the `Source:` lines
takes four lines of script. I had run it once, two builds ago, and not since --
and the folder has changed twice since then.

### And a missing file now says so

A script whose shared half is absent should not die silently. Each one now
tests for `homerInstall.cmd` first and, if it is missing, says which file and
where it should be, and stops. **A missing file must announce itself**, or the
only symptom is the one he saw: a Results box, and nothing else.

### Checkboxes grouped and sorted

As asked: the finish page lists **Install, then Reinstall, then Update**, and
sorts alphabetically within each group, always case-insensitively so
"exiftool" and "ExifTool" never sort apart.

    Install:    Whisper
    Reinstall:  ExifTool, Tesseract
    Update:     Ollama, Pandoc

`homerOrder` in the kit returns the indices in that order. The grouping is a
number rather than a sort of the verb strings, so changing the wording later
cannot quietly change the order.

###  -- audited rather than assumed

He is right to be frustrated. The MP3 still fails, and the log he sent is from
**1.0.237** -- a build that predates every fix of the last two days -- but that
is an explanation and not a defence. What follows is an audit of what I have
actually been shipping, not another guess.

### What the audit found in installWhisper.cmd

**It claimed success from an exit code.** After the downloads it printed
"Whisper is ready" because PowerShell had returned zero. A download returns zero
and leaves nothing usable in several ordinary ways: a redirect to an HTML error
page, an archive with a different layout, a copy refused for want of
administrator rights.

**Checking that a command succeeded is not the same as checking that the
artifact exists**, and this script was doing the second while claiming the
first. It is the precise fault I have been writing into this history for a
fortnight, in a file I wrote myself.

It now tests for `whisper-cli.exe` and the model file, and a new `:notReady`
path says plainly that the commands reported success and the files are not
there -- which is worth reporting, because it means something else is wrong.

**The download went somewhere else than the check looked.** The PowerShell
hardcoded `$env:ProgramFiles + '\Whisper'` while the script had just, possibly,
pointed `%whisperDir%` at a copy found on the PATH. Both now read one variable.

**A message claimed an install before anything was known:** "Whisper is
installed in ..." printed before any check ran. It says "Looking for Whisper
in ..." now.

**And a stray label** was left behind by my own edit of the previous build --
harmless to cmd, and proof that the edit was never read back.

### The same audit over the other seven scripts

Three more claimed success without checking: `installDocumentModel.cmd`,
`installTextModel.cmd` and `installModels.cmd` all ran `ollama list`, logged it,
and announced the model was installed without ever searching that list for it.
All three now search it.

`installPandoc.cmd` and `installTesseract.cmd` were already correct -- they test
for the file before claiming. Worth saying, because a crude first pass flagged
them too and reading them showed the flag was wrong.

### What I have not verified

I cannot run any of this on Windows. What I have done is read every jump against
every label, confirm the variable the PowerShell reads is set before it runs,
and confirm each success message now has a file or list check above it. That is
a stronger claim than I made last time and still weaker than a run.

###  -- what the finish page should say

His JAWS speech history settles the wording question. This is one checkbox, as
the screen reader read it:

    Install the vision model only, for describing video and the pictures in a
    PDF (about 5.5 GB; tick this if Ollama is already installed)

**148 characters**, and the history shows JAWS reading it four times over as he
arrowed onto it. A finish page is arrowed down, not studied, and a label that
long is an obstacle rather than an explanation.

### Three shapes, and the version in each

    Install Whisper (transcribes speech)                        36 characters
    Update Pandoc from 3.1.11 to 3.2 (writes the Word version)   58
    Reinstall Tesseract 5.3.3 (reads scanned pages)              47

The parenthetical says what the component DOES, in three or four words. The
size, the caveats and the advice have gone: the Results box and the guide are
where those belong.

### And the ticks make Enter mean something

    absent    Install    ticked
    outdated  Update     ticked
    current   Reinstall  unticked

**Pressing Enter installs everything missing and updates everything stale, and
reinstalls nothing that is already current.** That is the whole intent, and it
only works if a current component is offered rather than proposed.

The earlier build had this wrong in a way that looked reasonable: outdated was
unticked, on the grounds that an update can wait. It can, but then Enter no
longer means "make this machine right", and the page needs reading rather than
confirming.

`homerWanted` in the kit answers the question a `Check:` should ask -- missing
or stale -- rather than the narrower one I first wrote.

###  -- the install scripts move into the kit

Two things were being written once per app and drifting in every one: the
common half of an install script, and the code that decides whether a component
is already there.

### homerInstall.cmd

Forty lines that had been copied into a dozen scripts across four apps --
finding the app name, making the log folder, timestamping the file, writing the
environment header. EdSharp logged to its own folder, HomerScribe logged beside
itself inside Program Files, two logged nowhere at all.

One file in the kit now. A script says:

    set "sScript=%~n0"
    set "sCallerDir=%~dp0"
    call "%~dp0homerInstall.cmd" setup "%~f0"

and has `sApp`, `sLogDir`, `log` and `sQuiet` set, and its header written. All
eight of HomerScribe's use it.

It does NOT hide the console. That is written into the file's own comments, so
the next person to read it does not make the mistake I made yesterday.

### HomerComponents.iss

The part he asked for: **is it installed, and is what is installed current?**

DbDo had this for Ollama alone, as `ollamaState`, `ollamaNeedsInstall`,
`ollamaNeedsUpdate` and `ollamaIsCurrent` -- four functions for one component.
HomerScribe had none, and offered "Install Whisper" whether Whisper was there
or not.

Now an app registers what it cares about:

    iWhisper := homerAddComponent('Whisper', '',
                                  '{pf}\Whisper\whisper-cli.exe', 'whisper-cli');

and gets three states -- absent, outdated, current -- from a file check, a PATH
check and a winget version probe, in that order. **The file first, because it
cannot fail for a reason nobody can see**, where winget can be absent, slow or
refused.

### What that buys the finish page

The label says what will actually happen:

    Install Whisper, so that speech can be transcribed (about 500 MB)
    Update Pandoc from 3.1.11 to 3.2
    Reinstall Tesseract (version 5.3.3 is already here)

And the tick follows the state: **ticked only when the component is absent**. A
page that proposes what has already been done teaches the reader to ignore it.

The Results box is built from the same probe, so the box and the checkbox
cannot disagree. Two places telling different stories is worse than one telling
none.

###  -- console and log do different jobs

I had the pattern backwards. The last build hid every install script's console,
which treated the window as noise to be suppressed. **It is not noise. It is
where a person sees what is happening.**

The rule is the one already in his conventions: the CONSOLE says, briefly and
in plain words, what was found and what was done; the LOG keeps every command,
exit code and path, for diagnosing a failure from a file rather than from a
window that has already closed.

### The console is visible again

`runhidden` removed from all seven install-script entries. `waituntilterminated`
stays, so the steps run in order rather than racing each other.

What is still suppressed is only the WAITING: `HOMER_QUIET` stops a script
pausing for a key, because the installer is driving it and nobody is watching
for a prompt.

### And the logs go where he can find them

Three scripts logged **beside themselves**, inside Program Files -- a folder
that needs administrator rights to write and is not where anybody would look.
Two logged nowhere at all, and `installWhisper.cmd` called a `logLine` routine
that did not exist.

All eight now write to **`%LOCALAPPDATA%\HomerScribe\logs`**, named
`HomerScribe-<script>-yyyyMMdd-HHmmss.log` like every other Homer log, so the
whole folder can be zipped and sent.

One thing that would have spoilt it: two scripts opened their log with a single
`>`, which truncates. After the header had already written the environment
lines, the first message would have erased them. Every write appends now.

###  -- the installer, properly

Five faults, and one of them explains two of the others.

### The scripts moved and the Run lines did not

Last build moved every helper into the scripts folder and rewrote the `Source:`
lines to match -- **and left the `[Run]` lines pointing at the old flat path.**
Seven of them. Each would have failed silently, which is why nothing seemed to
install.

Same class of mistake as the document links, caught the same way and one build
too late: **a move is only done when everything that names the file has moved
with it.**

### The program belongs in exec

DbDo installs to `{app}\exec\DbDo.exe`; HomerScribe was installing to `{app}`.
Eleven references repointed, including the Start-menu shortcut and the
uninstall icon.

### Nothing waits for a key any more

`installWhisper.cmd` had a `noPause` guard the installer never set, and
`installOllama.cmd` had four bare pauses. Run hidden, a pause waits for a key
that can never arrive.

Every install script now runs with `runhidden waituntilterminated` and
`HOMER_QUIET=1`, and every pause honours it -- EdSharp's rule, stated in its own
installer: no console window and no message box; everything goes to the log.

### An existing component was invisible

`installWhisper.cmd` looked only in its own target folder, so a Whisper another
Homer app had installed, or one on the PATH, counted for nothing and 500 MB was
fetched again. **That defeats the entire point of a shared machine-wide
component.** It now takes the first `whisper-cli.exe` on the PATH.

### And there was no Results box because there was no code

HomerScribe had **no `[Code]` section at all**. The wizard closed and the only
record was a log nobody had been pointed at.

There is one now, shown at `ssDone` -- after every other step, not as a checkbox
-- reporting what is actually on the machine rather than what was attempted.
Whisper, Tesseract and Pandoc each get a line saying installed or not, and what
an absence costs.

### The finish page

The launch box stays ticked. **The user guide is unticked**, as DbDo's is:
finishing an install should open one window, not two.

###  — the Homer folder layout

The development folder now mirrors the installed one, as DbDo's does and as the
Homer layout says: **sources, build files, ReadMe and License at the top**, and
`configs`, `data`, `exec`, `help`, `logs`, `scripts` and `templates` beneath.

    top level   HomerScribe.cs, buildHomerScribe.cmd, HomerScribe_setup.iss,
                ReadMe, License, version.txt, the linked DLLs
    help        26 files -- every guide, in .md and .htm
    scripts     12 files -- the install scripts, getPdfPig.ps1,
                checkConfig.ps1, pdfPages.cmd, pagebreak.lua

It was 64 files in one heap before.

### Moving files is the easy half

Everything that named them had to follow, and three things did:

- **The build** runs `getPdfPig.ps1` and `checkConfig.ps1`, now from `scripts`.
- **The installer** rewrote 25 `Source:` lines to take each file from its new
  folder and install it into the matching folder under `{app}`, so the
  installed tree has the same shape. The DLL lines are wildcards and needed
  nothing.
- **The program itself** loads `pagebreak.lua`, which is a real path and not
  just a message. It looks in `scripts` first and falls back to the old place,
  so a copy installed by an earlier version still works.

### And the links between documents

Moving the guides into `help` broke every relative link in both directions:
`ReadMe.md` pointing down at them, and the guides pointing back up at `ReadMe`
and `License`. Seven links repointed into `help/`, two documents repointed with
`../`, and a walk of every Markdown file in the tree confirms **0 broken
document links**.

That is the part a move quietly gets wrong, because nothing fails until
somebody follows a link.

###  — built on the kit, like DbDo

DbDo no longer carries its own copies of the Homer classes, and HomerScribe
should not either. `buildDbDo.cmd` says why plainly: so a fix reaches every app
that uses them, and so the version an app compiles against is a fact rather
than a guess.

**HomerScribe's copies had drifted.** Compared against `C:\HomerDev\CSharp`:

    Inix.cs     different from the kit's
    Lbc.cs      different
    Say.cs      different
    Web.cs      different
    Util.cs     identical
    PdfRead.cs  identical

Four stale copies, and HomerScribe had none of `Log.cs`, `Ollama.cs`,
`KeyMap.cs`, `KeyName.cs`, `Mdi.cs` or `Paths.cs` at all.

`PdfRead.cs` is the pleasing one: written here for HomerScribe, promoted into
the kit, and byte for byte the same. The good case, and still one file too many.

### What changed

- **`buildHomerScribe.cmd` finds the kit** the way `buildDbDo.cmd` does — the
  `HomerDev` environment variable, then `C:\HomerDev`, then this folder — and
  **refuses to build against a kit older than 1.25.0**, saying so in a sentence
  rather than failing deep in the compiler with a message that names the
  symptom.
- It compiles nine kit modules: Inix, Lbc, Log, Ollama, Paths, PdfRead, Say,
  Util, Web.
- **The six local copies are deleted.** `HomerScribe.cs` is now the only C#
  source in the project, as `DbDo.cs` is for DbDo.

### One thing for the kit, not for HomerScribe

`PdfRead.cs` in the kit declares `public partial class HomerScribe`. It
compiles here, because HomerScribe declares the same class in the same
namespace — but it means **the shared kit holds a class named after one app**,
and any other app compiling `PdfRead.cs` acquires a `HomerScribe` class it
never asked for.

That wants a rename in HomerDev rather than a change here, so it is noted and
not touched.

### Still to do

`Paths.cs` offers `logs`, `temp`, `clearTemp` and the shipped-folder helpers
that HomerScribe hand-rolls as `appDataFolder`, `workFolderFor`, `tidyWorkFolder`
and `sweepOldWork`. Moving to them is the next step and a larger one: it is a
behaviour change in the working-folder handling, and it deserves its own build
rather than riding along with this one.

###  — shared components go where they are shared

His Whisper vanished. The reason was in the folder listing: `installWhisper.cmd`
was present and `whisper-cli.exe` was not.

**Whisper had been installing to `%LOCALAPPDATA%\HomerScribe\whisper`** — one
user's copy, invisible to every other Homer app, in a folder named after this
one. And four more components were worse off still: `ffmpeg.exe`,
`ffprobe.exe`, `yt-dlp.exe` and `exiftool.exe` sat in `C:\Program
Files\HomerScribe` as installer payload, **which the next install of
HomerScribe replaces**.

The other Homer apps already had this right. EdSharp, FileDir and DbDo ship
`installPandoc.cmd` and `installOllama.cmd` and no binaries at all.
HomerScribe was the outlier.

### What changed

- **Whisper** now installs to `%ProgramFiles%\Whisper`, **ExifTool** to
  `%ProgramFiles%\ExifTool`.
- **The build no longer copies ffmpeg, ffprobe and yt-dlp into the app folder.**
  winget had already installed them machine-wide; three `copy` lines were
  undoing that.
- **The installer ships no shared binaries** — only the scripts that fetch
  them.
- **`findTool` looks machine-wide first**, through a new `sharedToolFolders`,
  and searches the old app-data and app-folder locations last so an existing
  machine keeps working until its next install.

### The rule, plainly

A component many apps use goes to its own default machine-wide directory. A
library the program links against belongs beside the executable. The seven
PdfPig DLLs and seven `System.*` DLLs stay exactly where they are.

The difference is whether anything else could want it.

###  — the announcement

Retitled **"HomerScribe Now Batch Converts PDF to DOCX"**, since "What's New in
HomerScribe" has been used before, and rewritten at ninth-grade level rather
than sixth. His standard is ninth, and the earlier draft had been simplified
past the point of respecting the reader.

2,240 characters, comfortably inside LinkedIn's 3,000.

### The three-layer claim, checked before it was written

He asked for it to be mentioned **if true**. It is, with one distinction worth
keeping:

    reads the tag layer                      yes
    reads the text layer                     yes
    reads the image layer                    yes
    weighs tag words against page words      yes
    weighs tagged headings against inferred  yes
    caps inferred depth by tagged depth      yes
    abandons unusable text for the picture   yes
    MERGES layers -- lists from tags AND
      headings from layout                   no

So it **measures the layers against one another and converts from whichever
holds up best**. It does not blend them. "Reconciles" would have implied the
blending, which is still outstanding work, so the announcement says measures
and chooses — which is exactly what the code does.

The distinction is small and the difference in a reader's expectations is not.

###  — the release

All ten test documents pass on content, the two-column page reads down each
column, and `06-no-spaces` is correctly refused — the one test where succeeding
would have been the failure.

### The announcement, rewritten to fit

`Announce.md` was **4,220 characters**, against LinkedIn's limit of 3,000. It
is now **1,663**, at a sixth-grade reading level, and it leads with what is new
rather than with what HomerScribe already did.

The cuts were mostly explanation nobody asked for. What stayed: it reads
scanned PDFs, it uses the PDF's own tags, the page numbers match, it tells you
about faults in the original, and it runs in batch from the keyboard.

### What the announcement does not claim

**Tables come through as text. Nested lists in untagged PDFs are sometimes
flattened.** Both are in the announcement, under a heading that says so
plainly.

That is not modesty. A tool that oversells itself gets one try with a lawyer
before a deadline, and a gap they were warned about is a limitation while a gap
they discover is a betrayal.

### Checked, not assumed

Every claim in the announcement and the ReadMe was matched against the build:
the two install scripts exist and the installer offers them, the tag reader is
there, the page breaks are written, the unusable-text check runs, and the table
gap is stated rather than hidden.

`ReadMe.md` now describes the PDF work as it stands rather than as it was
fifteen builds ago. Reading levels after the rewrite: Announce 6.1, ReadMe 8.2,
HomerScribe.md 8.0, Hotkeys 3.0.

###  — lists, from both directions

**The pure scan reads.** `07-pure-scan.pdf` now reports *2 are scans, 2
pictures in all*, and Tesseract took both pages at 96 and 95 out of a hundred,
with every expected phrase present. The ASCII85 fix did what it was meant to.

That leaves the lists, and the two test files failed in different ways —
which is why there were two of them.

### Tagged: the words are in a paragraph inside the list

`10-tagged-lists.pdf` put each item on its own line and marked none of them:

    Erster Punkt
    Zweiter Punkt
    Innerer Punkt eins

**A tagged list puts its words in a `P`, inside an `LBody`, inside an `LI`.**
The piece that carries the text is a paragraph; only its ancestors say it is a
list item. Asking the piece alone gave nine plain paragraphs where nine list
items were tagged.

A list anywhere above now marks everything below it, carried down the walk.

### Untagged: the bullet is its own line

`03-lists.pdf` came out as one paragraph with the markers embedded in the
middle of it:

    Outer item one Outer item two Inner item one • Inner item two • Oute...

**In a PDF the marker and the text are separate runs at different positions**,
so the reading order delivers "•" and then "Inner item two" as two lines — and
matching "bullet, space, text" finds neither.

A line that is **only** a marker now turns the next line into an item. Checked
on the exact shape the test file produces: five items out of five, and the
prose after them untouched.

### What the pair was for

Neither fault would have been visible in the other file. The tagged one needed
ancestry; the untagged one needed to stop expecting the marker and the words to
arrive together. **Two files holding the same content, one tagged and one not,
found two different bugs** — which is the argument for building test documents
rather than collecting them.

###  — the test suite earned itself in one run

Ten purpose-built PDFs, answers written down beforehand. Eight passed on
content. **Two failures, and the first is the one that matters.**

### A document made entirely of pictures yielded nothing

`07-pure-scan.pdf` is two pages of drawn text with a verified **zero**
characters of text layer. Its log:

    0 carry their own text, 0 are scans, 0 pictures in all

And its output then said:

    Every page carried its own text, so the words are exact.

A document that produced nothing described itself as exact. **That sentence
fired whenever no page was a scan — including when no page was anything at
all**, and it is now specific about the difference.

### The cause: a filter I did not know about

    /Filter [ /ASCII85Decode /DCTDecode ]

**A PDF may wrap an image in more than one filter.** Those bytes are a JPEG
printed as ASCII85 text, and the decoder only knew about Flate — so they never
looked like a picture, every page was passed over, and the whole document came
back empty.

`unAscii85` is written now and runs before the inflater. Checked against the
real file: the stream decodes to 70,727 bytes beginning **FF D8 FF**.

This is the third format assumption to fail this way. Raw samples with no
header, Flate-wrapped JPEGs, and now ASCII85 — each time the fix was to look at
the bytes rather than to assume what they would be.

### And this is exactly the case the feature exists for

A PDF that is nothing but pictures of text is the ABBYY use case — the blind
lawyer with a scanned brief. **It was the one document in sixty-plus that
HomerScribe could not read at all**, and the only reason it surfaced is that
this file was built to be that and nothing else.

The sixty real documents never found it because accessibility publishers do not
ship pure scans.

###  — one document got no Word version at all

Ten more, and the results list gives it away by what is missing: **ADA
Checklist for Existing Facilities has a `described.md` and no `.docx`.** Its
log says why:

    Pandoc could not make the Word version: YAML parse exception at
    line 0, column 0

And the head of its Markdown says why that happened:

    lang: ^u@^^ ¸²aJn}Ö߸¼Æ...

**Its `/Lang` is binary rubbish** — an undecoded byte string, not a language —
and HomerScribe wrote it straight into the front matter. Pandoc choked on the
document, and an 89-page conversion lost its Word version entirely over one
malformed entry in the original.

This is the same lesson as the OCR text that became headings: **a value read
out of a file is data, not a fact.** I had escaped OCR output and not this.

### Two fixes, because one is not enough

**The value is checked.** A language tag looks like `en` or `en-US` or
`zh-Hans-CN` — letters, digits and hyphens, and short. Anything else is not one
and is dropped.

**And the language no longer travels as Markdown.** It goes to Pandoc as
`-M lang=...`, a command-line setting, so a bad value cannot break the parse
even if one gets past the check. The front-matter blocks are gone from both
paths.

Checking the value stops this case. Not putting configuration inside the
document stops the class.

## 1.0.269, 13 September 2026 — almost none is the same problem as none

The rerun was identical across all ten, which is right: 1.0.268 added a
message and changed no output. **But the message only fired once in three.**

    Accessible Meetings Toolkit     tags    1 heading  / 23 pages   warned
    Accessible Event Planning       tags    2 headings / 14 pages   NOT warned
    Accessible Procurement Toolkit  layout  1 heading  / 25 pages   NOT warned

Two gaps, and the second is the worse one:

- the warning fired only at **exactly zero**, so a 14-page guide with two
  headings passed;
- it lived on the **tagged path only**, so a document read from the layout was
  never asked the question at all.

### Moved to where both paths pass

The screen-reader review already reads the finished document, whichever way it
was built. That is the right place, and it needs no second copy: **a heading
every six pages is too far apart to navigate by**, whatever produced it.

    Event Planning            2 / 14    warned
    Meetings Toolkit          1 / 23    warned
    Procurement Toolkit       1 / 25    warned
    Signage Guidelines       25 / 27    fine
    Publishing Guidelines   180 / 51    fine
    Physics textbook        384 / 784   fine

The wording says which kind of fault it is, because it depends: where the
original is tagged, the headings are missing from the document; where it is
not, they were not distinct enough in the layout to find. A reader deserves to
know which.

### A note on where the bar sits

Eight pages let Event Planning through and six catches it. Neither number is
principled — what is principled is that the bar was set by looking at documents
rather than by choosing a round figure, and that the well-structured ones are
untouched by it.

## 1.0.268, 13 September 2026

A 784-page physics textbook read in part of twenty-eight seconds:
**218,548 words, 384 headings, 3,038 list items.** That is the largest document
this has been given by a factor of seven, and nothing about it was remarkable
in the log, which is the right kind of dull.

### But some documents come out with no headings at all

    Accessible Meetings Toolkit    23 pages, 1 heading, 235 list items
    Accessible Procurement Toolkit 25 pages, 1 heading,   0 list items
    Accessible Event Planning      14 pages, 2 headings, 104 list items

The Meetings Toolkit's log says why:

    own tags carry the document: 0 headings, 233 list items

**Its tags name no headings.** That is not a fault in the reading — it is
**PAC check 6**, word for word: *"if the document has no assigned headings"*
produces a warning, because a reader moving by heading has nothing to move
through.

So it is now said outright, as a fault in the original rather than passed on
quietly. HomerScribe cannot supply headings a document never had — the tagged
path has no sizes to infer from, only tags — and pretending otherwise would be
inventing structure nobody wrote.

### What that suggests next

Where the tags carry good lists and no headings, the two sources could be
combined: lists from the tags, headings inferred from the layout. That is a
real improvement and a larger change than tonight allows, and it wants its own
build.

## 1.0.267, 13 September 2026 — one document wearing another's tags

Ten documents, and the log reported identical tag counts in pairs:

    Accessibility Testing Criteria For iOS   96 headings, 313 list items
    Accessibility Toolkit                    96 headings, 313 list items

    Accessibility Training and Resources      6 headings, 0 list items
    Accessible Charts                         6 headings, 0 list items

A 63-page guide and a 76-page toolkit do not have the same structure. **The
second document of each pair was being described by the first document's
tags.**

### Ten static fields, none of them cleared

`bTaggedDocument`, `sTaggedSaid`, `sTagSummary`, `sDocumentLanguage`,
`sPiecesTrouble`, `iMarkPages`, `lTagged`, `lPieces`, `lTaggedFigureAlt`,
`dRoleMap`.

They are static because one document is read at a time — **which is true, and
says nothing about what happens when the next one starts.** Nothing cleared
them, so a document whose own tree could not be read silently inherited its
predecessor's, and every count in the log looked plausible.

Cleared now at the one place that cannot be forgotten: the moment a PDF is
opened. Checked by listing every static this file holds against what the reset
touches — nine of nine, plus the flag.

### The worst kind of bug

It produced no error, no empty output, and numbers that looked entirely
reasonable. **`Accessible Charts` is 46 pages and came out with 2,752 words and
6 headings** — thin, but not obviously wrong unless you notice that the
15-page document before it reported exactly the same structure.

Batch processing is where this kind of fault lives, and HomerScribe has been
batch-processing since long before it read a PDF. Worth a look at whether
anything else carries state across sources.

## 1.0.266, 13 September 2026 — a half was not cautious enough

The page-search fix worked: **0:14 for ten documents**, all ten taking the tag
path, and the structure is transformed —

    Accessibility Handbook   381 headings, 3,803 list items
    In E-Learning             49 headings,   102 list items
    Print Design              19 headings,    54 list items

against 456 headings and 523 list items for the handbook by inference, and
**zero** list items for several of the others a few runs ago.

### But three documents lost words

    Handbook for Teaching  69,842 -> 42,938   -39%
    In E-Learning          10,202 ->  7,684   -25%
    Marketing               6,503 ->  5,846   -10%

The handbook **passed my parity test and still lost 39 per cent**: 42,938 words
from its tags against 65,577 on its pages. Losing a third of a textbook is not
a trade worth making for better headings, and a half was never a cautious bar —
it permits exactly this.

**Four fifths now.** Some loss is right: a tagged document marks its running
headers and footers as artifacts and they are correctly left out. But that is a
few words a page, not a third of the document.

    document                tag words  page words  kept   taken from
    Handbook for Teaching      42,938      65,577   65%   layout
    In E-Learning               7,684       9,962   77%   layout
    Marketing                   5,846       6,503   90%   tags
    Skills Hiring Toolkit       8,590       8,776   98%   tags
    Interview Questions         1,574       1,574  100%   tags
    Print Design                2,578       2,194  118%   tags

Eight by tags, two by layout — and the two that fall back are precisely the two
that were quietly losing a quarter or more.

### Where the tags give MORE

Four documents come out longer from their tags than from their pages — Print
Design by 18 per cent, the HECVAT guide by twice. That is not invention: a
tagged table reads its cells as content where the layout reading ran them
together, and alt text on a figure is text the pages never had.

## 1.0.265, 13 September 2026 — the lookup was blind

Ten more documents, and a pattern I could not ignore:

    Accessibility Handbook   tags yield  5,228 words, pages hold 65,577
    Accessibility In E-Learning          1,784                 9,962
    Accessibility Skills Hiring Toolkit    973                 8,776

**About a tenth, three times over.** Added to the Public Libraries
disagreement, that is not three odd documents. That is a fault.

### The page search never got past page three

An identifier's words were looked for on pages 1 to `iPage + 2`, and `iPage`
only advanced when something matched. **On a document whose first elements did
not match, it never looked past page three** — so a 655-page handbook found
almost nothing, and the two tests added yesterday correctly refused the result.

The tags were fine. The lookup was blind.

    identifiers matched out of 200, on a 655-page document
      old loop, pages 1 to iPage+2:    1
      new search, every page:        200

Every page is searched now, **starting at the one we think we are on**, since
that is nearly always right and makes the ordinary case cost nothing.

### What yesterday's work did right

Those three documents came out correct anyway — 69,842 words, 10,202, 9,269 —
because the word-parity test caught the shortfall and sent them to the layout.
**A safety net that fires three times in ten is doing its job and is also
telling you something**, and I nearly read it as documents being awkward rather
than as my own lookup failing.

The gate saved the output. It did not save me from the bug, and I should have
asked why it kept firing.

## 1.0.264, 13 September 2026 — the tags must earn it

**Fourteen seconds for 373 pages**, against six minutes and seventeen. The tag
reader works and costs almost nothing, because a tagged document needs no model
and no OCR.

And on two of the ten it threw away most of the document.

### Able Gamers Guidelines: 25,995 words became 515

Its log says why:

    own tags carry the document: 0 headings, 0 list items, 1374 words

**Zero headings. Zero lists.** It is a SCANNED document that happens to be
tagged — its tags name 141 figures and almost no text — and the pages
themselves hold 25,995 words that Tesseract had read perfectly well the run
before.

My test was words alone, and 1,374 cleared the bar of twenty a page. **A count
of words says nothing about whether the tags describe the document.**

### Two tests now, and both measured

- **Shape.** A tree with no heading and no list is not describing a document,
  whatever else it contains.
- **Not plainly poorer than the pages.** The words the pages hold are counted
  and compared. Tags yielding less than half of what the pages carry are not
  carrying the document.

Against the real numbers:

    Able Gamers            1,374 tag words,  0 headings, 25,995 on the pages -> no shape
    10 Key Guidelines      4,216 tag words, 28 headings,  7,277 on the pages -> tags
    Public Libraries      12,951 tag words,118 headings, 12,925 on the pages -> tags
    Interactive Web Maps   2,644 tag words, 54 headings,  9,922 on the pages -> poorer

**Interactive Web Maps is the one I would have missed.** Its tags have plenty
of shape — 54 headings, 159 list items — and still carry barely a quarter of
the words. Shape alone would have passed it and lost 7,000 words.

### What this says about the feature

The tag reader is right when the tags are good, and the check for whether they
are good is now the more important half. A tagged PDF is a claim about a
document, not a guarantee — and this project has learnt that lesson in about
six different forms now.

## 1.0.263, 13 September 2026

    error CS0052: Inconsistent accessibility: field type
    'List<HomerScribe.TaggedPiece>' is less accessible than field
    'HomerScribe.lPieces'

**The same fault as `TaggedItem`, six builds ago, repeated verbatim.** I fixed
it once, wrote a near-identical class later, and made the identical mistake —
having also, at the time, run a check that found no others.

That check was too narrow. It looked at public members and asked what type they
held. The better question is the other way round: **every class, and whether
anything public exposes it.** Asked that way:

    TaggedItem    public    exposed by a public member: yes
    TaggedPiece   public    exposed by a public member: yes
    PageLine      internal  exposed by a public member: yes  <-- looked wrong
    PdfPage       internal  exposed by a public member: no
    AdBreak       internal  exposed by a public member: no

`PageLine` turned out to be a false alarm: the field holding it is `public`, but
it sits **inside** `PdfPage`, which is `internal`, so its effective
accessibility is internal and the compiler is content. Worth checking rather
than assuming — the pattern that flagged it could not see the containing class.

The rule, for next time there is a class like this: **if a class is named in
anything `public`, the class is `public` too.**

## 1.0.262, 13 September 2026 — the tag reader

Ten accessibility documents made the case with numbers. Nine are tagged, and
against what their tags say the inference is wrong in both directions:

    Interactive Web Maps      62 tagged headings, 10 inferred
    Public Libraries          29 tagged headings, 118 inferred
    9 Steps                   23 tagged lists, 0 list items produced
    Access Handbook           32 tagged lists, 0 list items produced

**Four documents with tagged lists produced no list items at all.**

So the tags are read properly now, not merely counted.

### How a tag finds its words

A structure element's `/K` holds **marked content identifiers** — numbers — and
the page's content stream marks each run of text with the matching identifier.
PdfPig hands those runs over through `GetMarkedContents()`, so the join is:

    element -> identifier -> run of letters -> words

Both spellings are handled: a bare number in `/K`, and an `MCR` dictionary
naming `/MCID`.

### What the document then becomes

Headings keep **the level the author gave them**. A list is a list because the
tag says `L`, not because a line begins with a bullet. A figure carries the
author's own description. Page breaks come from where the tags say the pages
change, so the Word version still paginates like the original.

**Nothing in it is guessed.**

### Judged by what came back, not by what it claims

A tree yielding a handful of words for a fifty-page document has not been read
properly, whatever it says about itself. So the tagged rendering is used only
where it produces at least twenty words a page, and otherwise the layout
inference runs as before, with the log saying which and why.

That matters: a tagged document this cannot read is no worse off than an
untagged one.

### And a patch I had to throw away

The first attempt wove the new path into the middle of `readPdf` with string
surgery and index arithmetic. It compiled. It also dropped the code that used
its own result and left a "too little to be the document" message firing every
time — visible only because I read the region afterwards rather than trusting
the edit. Rewritten whole.

## 1.0.261, 13 September 2026 — a text layer can be worthless

The PAC guide lists fourteen accessibility checks, and two of them describe
ways a PDF can carry a text layer that is **worthless**. HomerScribe trusted
every text layer absolutely.

> **Check 8, accessible font encodings.** If a translation table is missing for
> a certain font, non-interpretable characters are passed to the assistive
> technology.

> **Check 14, spaces existent.** Some PDF generators think it not necessary to
> include invisible spaces to be passed into the PDF document.

A missing translation table gives a page that looks perfect and extracts as
gibberish. A generator that skipped the spaces gives
`Thewordsrunttogetherlikethis`.

**Both produce a document that reads as nonsense while every count in the log
looks healthy** — the worst kind of failure, because nothing reports it. And
this is precisely the reliability he asked about before announcing.

Both have the same remedy: **the page is a picture too**, so where the text
fails, the pages are treated as scans and read instead. Slower, and it gives a
usable document.

Three signs, each cheap:

    no spaces at all, or a space only every twenty letters
    replacement characters, meaning a font has no translation table
    more than a tenth neither letter, digit nor punctuation

### The false positive I nearly shipped

The first version rejected a text layer where fewer than a quarter of
characters were letters — and **a page of counts and totals is legitimately
mostly digits.** His journal is full of them. Digits and ordinary punctuation
no longer count against a text layer; only characters that are neither.

    ordinary prose            sound
    a page of totals          sound
    mixed prose and figures   sound
    words run together        rejected
    broken font               rejected

### The rest of PAC's list

Already done: marked as tagged, language defined, consistent heading structure
(all three of its cases), alternative text, correct tag syntax through role
mapping, logical reading order.

Not done, and worth knowing: **document title**, **bookmarks**, and **security
settings** — the last of which can block assistive technology outright, and is
a fact about a document worth telling somebody before they try to read it.

## 1.0.260, 13 September 2026 — the tags are used, not merely noticed

He is willing to release without tables. He is **not** willing to announce that
tags are recognised when they are only detected, and he is right: recognising
something and doing nothing with it is not a feature.

Four uses, none of which needs the marked-content machinery that has been
holding this up.

### The author's own description, instead of the model's

A tagged PDF's `Figure` elements hold alt text somebody wrote. **It is better
than anything a model produces** — it is what the author meant the picture to
convey — and it costs nothing where the model costs about ninety seconds.

The structure tree **is** the reading order, so the nth Figure describes the nth
picture. That is exact for a well-made document and no worse than nothing for a
bad one. The log says when the PDF's own description was used and the model was
not asked.

### The document's language

`/Lang` from the catalogue, written into the Markdown's front matter, which
Pandoc turns into the docx's language — **what decides how a screen reader
pronounces it.** A document HomerScribe produced from a French PDF should not
be read aloud in English.

### The tags set the depth

A tagged PDF states how deep its headings go. Inferring six levels for a
document whose author used three invents a structure nobody wrote, so the
inference is now capped at what the document says.

### And the count still audits the inference

From 1.0.255, and now one of four uses rather than the only one.

### A stub I wrote and threw away

The first attempt at matching figures to pages read `/Pg`, the element's
reference to its page object — and PdfPig does not expose a page's object
number, so I wrote a helper that **always returned zero**. It would have
compiled, run, and quietly matched nothing.

Matching by order needs none of that and is what the tree already guarantees.

## 1.0.259, 13 September 2026 — reliability before an announcement

He wants to announce the OCR work, and asked whether it is reliable enough.
Reading all four documents, most of the answer is yes and one thing was not.

### What is sound

    document                    pages  headings  per page  words
    GAC-Evidence-Gap-Resource       6        11       1.8   3,419
    HMS Vol.15 No.1               112       303       2.7  91,227
    mlk                             9         1       0.1   6,962
    tickets                         1         3       3.0      83

- **No page came out empty**, in any of the four.
- **Two passages marked unclear** in 91,227 words of scanned journal.
- **Heading counts are plausible** — 2.7 a page on a journal, 1.8 on a policy
  paper.
- **mlk has one heading in nine pages, and that is correct.** The Letter from
  Birmingham Jail is continuous prose with no sections. A tool that invented
  headings there would be worse, not better.

Both kinds of document work: born-digital read exactly, scanned read by
Tesseract with the model for what it cannot manage.

### What was not sound

The screen-reader review — the check taken from Iris — earned its place:

    Heading level jumps from 1 to 6
    Heading level jumps from 2 to 5
    Heading level jumps from 2 to 6   (three times)

**Matterhorn checkpoint 14-003.** A level that jumps leaves a reader moving by
heading unsure whether something was missed, and it is precisely the reader
this feature exists for.

The cause is two sources meeting. Inferred pages squeeze their own levels; a
model-written page brings its own; a document made of both skips where they
join. So the levels are now walked once over **the whole document**: each
heading at most one deeper than the one before, and going back up always
allowed, since that is how a document returns to a higher section.

It runs before the review, so what is judged is what will be written.

### My view on announcing

**The words are reliable. The structure is now defensible.** What I would not
claim yet is tables, and what I would say plainly is that a document's own tags
are read but not yet used.

One honest caution: every measurement here is on four documents. They are four
good ones — scanned and born-digital, one page and a hundred and twelve, prose
and policy — but four is four.

## 1.0.258, 13 September 2026

Two things, and the first is embarrassing.

### The tree walker found nothing in a document that plainly has tags

    Reading its tags: its structure tree names no headings, lists, tables
    or figures.

`/K` is very often an **indirect reference to an array**, and the walker asked
"is this an array?" of the reference itself, which it is not. So the tree was
dropped at the first hop.

Everything else resolved references properly; this one place asked the question
before resolving. A `resolved()` helper now does it once, at the top of the
walk, and the dictionary-only helper that could not see arrays is gone.

### And the journal's headings tighten themselves now

1,769 headings in 112 pages — down from 4,336, and still ten times too many.
The deepest ones were a fair mix: *Myth or Fact: Sexing Osprey* is a real
heading, *Migration -* is a contents-page leader.

**No fixed setting can be right for every scan**, because the noise in a
measured height depends on the scan. So the document corrects the setting: if
too many of its lines would become headings, the bar goes up and it is tried
again, up to six times, and the log says each time.

The threshold matters more than it sounds. **A tenth sounded safe and is not**:
1,769 of about 20,000 lines is 8.8 per cent, which would have passed a tenth
untouched. One line in twenty-five is still generous — a well-made document
runs nearer a fiftieth — and it actually bites:

    bar 1.40 -> 1,769 headings (8.8%)
    bar 1.61 ->   972 headings (4.9%)
    bar 1.85 ->   534 headings (2.7%)   accepted

About five a page, which is what a journal looks like.

And this needs no tags — which matters, because three of his four documents
have none.

## 1.0.257, 13 September 2026

Two errors and four warnings, all in the new tag code.

### The role map is keyed by string

    error CS0030: Cannot convert KeyValuePair<string, IToken> to
    KeyValuePair<NameToken, IToken>

`DictionaryToken.Data` is keyed by `string`. I wrote `NameToken` because the
keys in a PDF dictionary *are* names, and the library has already turned them
into strings by the time they reach you.

### Two counters with one name

    error CS0136: A local named 'iFigures' cannot be declared in this scope
    because that name is used in an enclosing local scope

Two different counts of figures: those found in the picture layer, and those
the tags name. Different numbers about different things, and they wanted
different names. The tag one is `iTaggedFigures`.

### And `Letter.Font` is obsolete, again

Four warnings, and I had fixed this exact obsolescence a dozen builds ago —
then reintroduced it in new code by writing the same line from memory.

**`FontDetails` is better than a workaround**: it carries `IsBold` and
`IsItalic` outright, rather than leaving them to be read out of the typeface
name. Both are used now — the flags first, since they state what the name only
implies, and the name still read, because plenty of PDFs set neither flag and
call the font "Times-Bold".

So a warning I had been treating as tidying turned out to be an improvement I
had been declining.

### One caught before it shipped

`bSawBold` and `bSawItalic` were used and never declared — the next build's
error, found by checking the file for names used but not declared rather than
waiting for the compiler to say so.

## 1.0.256, 13 September 2026

    PdfRead.cs(446,40): error CS0052: Inconsistent accessibility: field type
    'List<HomerScribe.TaggedItem>' is less accessible than field
    'HomerScribe.lTagged'

`TaggedItem` was written before the field that exposes it, and its default
accessibility was never revisited. A `public` field cannot hold a type nobody
outside can name. The class is `public` now.

Then the same question was asked of everything else rather than only the line
the compiler pointed at: every `public static` member in both files, checked
against the accessibility of the type it holds. **None other is wrong** —
`PageLine`, `PdfPage` and `AdBreak` are internal and only ever used internally,
which is correct.

One error, one line, and the check that it was the only one of its kind.

## 1.0.255, 13 September 2026 — the tags audit the inference

**One of his four PDFs is tagged.**

    GAC-Evidence-Gap-Resource   it declares itself marked and carries a
                                structure tree
    HMS Vol.15 No.1             not tagged
    mlk                         not tagged
    tickets                     not tagged

A policy paper prepared for publication, and three that were printed to PDF.
That ratio is worth knowing: **most documents a person is handed will not be
tagged**, so the inference is not a fallback, it is the usual case.

### What the tags are used for now

Matching a tag to the words it covers needs marked-content identifiers and the
page's content stream — a larger piece of machinery, and the right next step.
**Counting is not**, and counting catches the failure that actually happened:

    The document's own tags name 47 headings, 3 levels deep.
    This reading made 4,336 headings, 6 levels deep.
      That is far more than the document says it has, so the sizes are being
      read too generously. Raise --heading-size to tighten it.

Three times as many is not a difference of judgement, it is a fault — and a
tenth as many is the same fault the other way. Where the two are close, it says
so, which is the only independent confirmation the inference has ever had.

**A tagged PDF is a marking scheme for the guesswork**, and that is worth having
before it is worth reading in full.

### And the threshold is a setting now

`--heading-size`, 1.4 by default, driving all five bands proportionally. The
audit above tells him which way to move it, so the advice and the control match.

## 1.0.254, 13 September 2026

The band change over-corrected badly. His journal went from 38 headings to
**4,336** — thirty-nine a page — and the levels say exactly where they came
from:

    h1 28   h2 394   h3 383   h4 502   h5 613   h6 2416

### 2,416 level-6 headings

**A measured line height is noisy in a way a point size is not.** A line of
"Migration" measures taller than a line of "was more" in the same font, because
capitals and ascenders reach higher than lowercase. Ordinary text wanders over
two or three pixels — and at 1.16 above a 9-pixel body, every line with a
capital in it qualified.

A real heading is set **noticeably** larger, not a sixth larger. The lowest band
is 1.4 now, where the wandering of ordinary text falls below the bar and a
heading still clears it comfortably.

### And the 28 level-1 headings were not what I said they were

I blamed OCR misreading a mark as a hash, and escaped lines beginning with one.
**Not one escape fired**, because that was not the cause.

**The model writes its own Markdown.** Twenty-two pages went to the picture
model, and it answers a page with a document — complete with its own `# Title`.
One per page, twenty-eight in all, in a document that should have exactly one.

A page is not a document. Whatever the model calls a level 1 is at best a level
2 here, so every heading in a model-read page is pushed down one and the levels
keep their shape among themselves.

That is worth recording as a mistake in method: I had a plausible explanation,
implemented it, and did not check that the fix fired. **The escape count was
zero in the very next run and I would not have noticed had the symptom not
survived.**

## 1.0.253, 13 September 2026 — reading the structure tree

No free .NET package reads PDF tags: PdfPig says accessibility tagging is out
of scope, iText does it and is AGPL — which would force HomerScribe off its MIT
licence — and VellumPdf is .NET 10 and writes rather than reads.

So it is written here, on PdfPig's tokens, which is all a structure tree
actually needs. **The tree is a dictionary tree**, and PdfPig hands over
dictionaries.

### Role mapping first

Matterhorn checkpoint 02: only standard PDF 1.7 tags may be used, and a custom
tag must carry a role map entry saying which standard tag it stands for. A
document using `Heading1` for `H1` is perfectly valid — and **without applying
the role map it would look untagged**. So `/RoleMap` is read before anything
else, and followed where one custom name points at another, with a limit
because a broken document can point in a circle.

Tested on a realistic tree: `Heading1` resolved to `H1` and came out as a level
1 heading.

### What it reports

    its structure tree names 2 headings, 1 lists, 1 tables, 2 figures,
    1 with alternative text

And where a figure carries no description, it says so — **Matterhorn 13-004**.
That is worth stating carefully, because it is a fault in the SOURCE document
rather than in this reading of it: somebody tagged that PDF and left a picture
undescribed. HomerScribe describes it itself where it can, which is a thing the
original author should have done and did not.

### Guards, because a PDF may be malformed

Depth capped at 40, items at 200,000, role-map hops at 8, every step in a
try/catch that returns rather than throws. A structure tree can be circular,
and a document that cannot be read should not take the program down with it.

### Still inferred rather than used

The tree is read and reported; the headings in the output still come from the
layout. Using the tags instead is the next step and a larger one — it means
matching structure elements to the text on the page through marked-content
identifiers, which is a different piece of machinery.

## 1.0.252, 13 September 2026 — a tagged PDF says what it is

He asked whether HomerScribe looks for a PDF's accessibility tags. **It did
not**, and they are the one source that beats everything it infers.

A tagged PDF carries real headings with real levels, lists that know they are
lists, tables that mark which cells are headers, alt text on every figure, and
a reading order the author set. Every inference built here over the last two
days — font size into heading level, length into heading or paragraph, word
height on a scan, Docstrum into reading order — **is a reconstruction of what a
tagged PDF simply states.**

Where the tags exist they win, because they are what the author meant rather
than what the layout suggests.

### What is built, and what is not

HomerScribe now **looks** for them: `/MarkInfo /Marked` in the catalogue, which
is the declaration, and `/StructTreeRoot`, which is the tree itself. A document
can carry one without the other, so both are checked and the answer is specific:

    it is tagged: it declares itself marked and carries a structure tree
    it carries a structure tree but does not declare itself marked
    it declares itself marked but carries no structure tree, so there is
      nothing to read
    it is not tagged, so its structure has to be inferred from the layout

It says so in the log, and **in the document itself**, because a reader deserves
to know how much of a document's shape was stated and how much was guessed.

**It does not read the tags yet.** That is the honest position and the log says
it outright rather than letting the mention imply more than it does.

### Why reporting is worth having on its own

A lawyer handed a filed brief can be told whether it was **made accessible or
merely printed to PDF** — which is a fact about the other side's document, not
about HomerScribe, and one nobody currently has an easy way to check.

## 1.0.251, 13 September 2026 — the picture layer is unproven

He asked how the two layers are doing, beyond headings. Reading all four
documents:

### The text layer is working

    GAC-Evidence-Gap-Resource   every page carried its own text
    mlk                         every page carried its own text
    tickets                     every page carried its own text
    HMS Vol.15 No.1             112 pages read from pictures

Three born-digital documents read exactly, one scanned journal read by
Tesseract. The routing is doing what it should, and the words in the three text
documents are the document's own rather than a reading of it.

### The picture layer has described nothing, in any document

**Zero pictures described across all four.** For the journal that is correct —
every page IS a picture, and a scanned page's picture is the page. For the
other three it means either they genuinely contain no figures, or the picture
layer is dropping them, **and the log gave no way to tell.**

I know which I suspect. A figure in a born-digital PDF is often stored as
Flate-compressed **raw samples with no header at all** — and the code skips
anything that does not begin like a JPEG, a PNG or a JPEG 2000 after
inflating. That test drops precisely the case those three documents would have
exercised.

So the first fix is to stop guessing: **pictures found but unreadable are now
counted and reported**, rather than passed over in silence. "0 pictures" will
mean a document with no pictures, and anything else will say so.

That is the honest state of it. The two-layer reconciliation has been proved on
the text side and **has never once been proved on the picture side**, and I had
not noticed because every document I had checked was either all text or all
scan.

**What would settle it**: a PDF with a chart or a photograph on a text page. If
the log then reports pictures found but unreadable, the header test is the
fault and a PNG header can be built from the width, height and colour space
PdfPig already provides.

## 1.0.250, 13 September 2026

The GAC document went from **130 headings to 46** — the length test works. The
journal went from 32 to 38, which does not, and reading its levels said why:

    h1 28   h2 3   h3 5   h4 1   h5 1   h6 1

### Five places, taken by outliers

`headingLevels` took the five largest DISTINCT sizes. That is right for a text
PDF, where sizes are a designer's small set. On a scan a measured pixel height
is noisy: the journal produced dozens of distinct heights, **the five largest
were rare outliers on a cover**, and the common subheadings a fifth above the
body got nothing at all.

Sizes are grouped into **bands by ratio to the body** now, so the same rule
serves a point size and a pixel height, and heights of 13 and 14 land together
instead of competing for one of five places:

    body 9px
    11px, 12px  ->  h6      18px, 22px  ->  h3
    13px, 14px  ->  h5      26px, 34px  ->  h2
    15px        ->  h4

A size is also weighed by how much of the document uses it, so one used once on
a cover cannot outrank one used on forty subheadings.

### And 28 of those headings were not headings at all

One read:

    # Haven. Three such sites produced about average counts

Body text from the middle of a sentence. **Tesseract had read some mark on the
page as a hash**, and because the line went into the Markdown untouched, it
became a level-1 heading.

OCR output is text, not markup, and it was being trusted as markup. A line
beginning with `#`, `>`, `|`, `=` or `+` is escaped now. Only the start of a
line matters, since that is where Markdown looks; escaping inside the line
would litter the prose with backslashes for nothing.

That one is worth remembering beyond this project: **anything a machine reads
off a page is data, and writing it into a structured format without escaping is
the same mistake as putting user input into a query.**

## 1.0.249, 13 September 2026

Three PDFs of quite different kinds in one run, and reading all three outputs
found two faults that no single document would have shown.

**The encoding fix holds**: zero mangled characters in any of the three, where
there were 98 before.

### The journal still had 32 headings in 112 pages

The height inference was built and did nothing, for a reason of ordering:
`bodySizeOf` runs **before** the page loop, and on an all-scanned document
there are no lines to measure until that loop has run. The sizes arrived after
the decision that needed them.

**A second pass** now renders the OCR pages once every page has been read and
the body size can actually be measured. Pages that carried their own text are
untouched — they had their sizes from the start.

### And the other document had 130 headings in six pages

Every paragraph of `GAC-Evidence-Gap-Resource.pdf` came out as an `h6`. The
size most CHARACTERS were set in was the small print, so ordinary prose sat a
sixth above it and qualified.

**Size alone cannot tell a heading from a paragraph.** Length can: a heading
names what follows, it does not run to two hundred characters, and it does not
end in a full stop. Under 100 characters and not ending in sentence punctuation
— a test that costs nothing and that no real heading fails.

    UNPROVEN AND UNSAFE:                     heading
    Claims abound regarding the benefits...  paragraph
    Myth or Fact: Sexing Osprey              heading
    the eighth edition of the World Prof...  paragraph

### What the three documents together showed

One scanned, one born-digital, one a single page. **Each exposed a different
fault, and each fault was invisible in the other two.** The journal's ordering
bug needed an all-scanned document; the heading flood needed a text document
with small print. A single test file would have passed.

## 1.0.248, 13 September 2026 — the second view

He asked whether I had incorporated the learnings from Equalify Iris. **Partly,
and I had named the best part twice without building it.**

Honest audit of Iris's three phases against HomerScribe:

    extract each page                  done
      ...and VERIFY it                 not done
      ...specialist per content type   done
    assemble in page order             done
      ...validate the result           not done
    review in TWO VIEWS                not done
      ...loop until nothing changes    not done

Four of seven missing, and the two-view review is the one worth having.

### What it does

Iris reads its work in two views — the marked-up text, and a flattened reading
of what a screen reader would actually announce — **because a fault invisible in
one is obvious in the other.** A heading at the wrong level looks perfectly fine
in Markdown and announces itself wrongly. A picture with no alt text is an
unremarkable line in the source and a silent gap in the reading.

    heading level 1, Journal
    heading level 2, Features
    heading level 4, Eagle Migration      <- the jump is audible here
    graphic, no description               <- and so is this

It reports heading levels that skip, a document with no level 1 or with several,
pictures with no description, and a document with no headings at all.

### Where it departs from Iris, and why

**Iris asks a model to do the reviewing. This does not.**

Iris is remediating arbitrary HTML it did not write, so it needs judgement.
HomerScribe **wrote** this document and knows what it meant, so the same faults
can be found deterministically: no model, no minutes of work, no hallucinated
objection, and the same answer every time.

That matters for the heading inference particularly. Levels here come from font
size or from OCR word height, and both can be fooled — by a dropped cap, by a
page of nothing but headings. This is the check that catches it, and it costs
nothing.

Still not done: verifying each page as it is extracted, and looping the review
until a round changes nothing. Both need a model, and both are worth doing once
there is something for them to fix.

## 1.0.247, 13 September 2026

He asked whether the OCR work has cost anything elsewhere. Traced rather than
asserted, and **it found one**.

### The one real risk, now fixed

`sweepOldWork` clears working folders older than a day. It judged age by the
FOLDER's own timestamp — and **a batch of video can run for more than a day**,
while a second HomerScribe started alongside the first would have swept the
first's folder out from under it. Windows does not reliably touch a parent
folder when files inside it change, so a folder in active use can look old.

**The newest file anywhere inside decides now**, and the log says when a folder
was spared. Checked against five cases:

    abandoned 3 days ago        swept
    a batch running 30 hours    left alone
    a second run started now    left alone
    finished yesterday          swept
    started an hour ago         left alone

That fault predates this question and would have shown up as a batch failing
overnight for no visible reason — the hardest kind to diagnose.

### What is genuinely untouched

- **The UTF-8 change is scoped.** `bWantUtf8Output` is false by default, set in
  exactly one place, and restored in a `finally`. ffmpeg, yt-dlp, Whisper,
  ExifTool and Ollama read exactly as before.
- **A `.zip` never reaches `readPdf`.** The routing is guarded by `bFromPdf` and
  returns immediately, so the picture-archive path — descriptions, renamed
  copies, ExifTool metadata — runs the same code it did a fortnight ago.
- **Removing ads touches nothing OCR-related**, checked function by function.
- **Transcribing and describing** never enter the PDF path at all.
- Both test harnesses pass.

The honest summary: no regression in those four features, and a real one found
in the housekeeping that serves all of them.

## 1.0.246, 13 September 2026

Reading his 511 KB of Markdown found two faults the logs could not show.

### The words were mangled on the way in

`ΓÇÖ` appears **98 times**. That is a right single quote, written by Tesseract
as UTF-8 and read back through the console's code page. Tesseract recognised
those characters correctly and HomerScribe corrupted them, then carried the
corruption into the Word file.

`runCommand` now sets `StandardOutputEncoding` to UTF-8 when asked, before the
process starts — after it starts, the setting is ignored.

### And the document had no shape

**32 headings across 112 pages, and one list item.** A page read by Tesseract
was handed through as plain lines: the words were right and the structure was
absent, on 91 of the 112 pages.

Tesseract's TSV carries each word's **height**, which does for a scan exactly
what point size does for a text page. The same inference now runs on both:

    body height 14px, from 775 characters
    MIGRATION                 34px  -> h2
    Features                  22px  -> h3
    The Newsletter of the...  18px  -> h4
    New Perspective on...     14px  -> body

So a scanned page gets headings, lists, and the page breaks — the same document
a text PDF gets.

### The redundant model is gone from the installer

He asked whether anything offered has been superseded, and one thing has.
**`granite3.2-vision`**, 2.4 GB, was offered "for reading scanned pages on a
computer short of memory". It was already measured as worse than the ordinary
vision model, and **Tesseract now reads scanned pages in 60 MB with no graphics
card at all**. Anyone short of memory should install Tesseract, not a weaker
2.4 GB model. The task is removed; the `--document-model` setting stays for
anyone who wants it.

The installer now offers, in order: **Ollama and the vision model**, the vision
model alone, **ExifTool** (which shipped but was never offered), **Pandoc**,
**Tesseract**, the **reading model**, and **Whisper**.

## 1.0.245, 13 September 2026

Two things, one a confirmation and one a lesson taken from `BuildEdSharp.ps1`.

### The documentation is already in both formats

Nine documents, nine `.htm` beside them, checked file by file: `ReadMe`,
`HomerScribe`, `Hotkeys`, `History`, `Developer`, `License`, `Review`,
`Announce`, `video_formats`. None missing either way.

And HomerScribe's **output** stays Markdown. He has not asked for `described.htm`
and I have not added one.

### PdfPig is pinned now, the way EdSharp pins everything

`BuildEdSharp.ps1` pins every package and says why beside each one:

> ReverseMarkdown is pinned at the last line that supports .NET Framework: the
> 4.x series ships netstandard2.0, which net48 consumes; 5.x and 6.x ship only
> net8.0 and later, which net48 cannot reference at all.

> HtmlAgilityPack 1.12.1 — ReverseMarkdown 4.7.1 is compiled against it, and a
> mismatched copy fails at runtime with a manifest-definition error the moment
> any HTML-to-Markdown path runs.

`getPdfPig.ps1` asked nuget.org for **"the latest PdfPig"** with no pin at all.
That is the same fragility: the day PdfPig ships a version targeting only
net8.0, or changes the assemblies it needs, a build that worked yesterday fails
with nothing changed here — and after the fortnight this project has just spent
on dependency faults, that is not a risk worth carrying for nothing.

`0.1.16` is what HomerScribe has been built and tested against. The pin says
so, says why, and says to change the comment when the version is raised
deliberately.

That is worth more than the pin itself. **Every version constant in EdSharp's
build carries the failure that taught it**, with a date — Scott's crash of 25
August, the build log of 19 August. A pin without a reason is a number somebody
will raise on a whim.

## 1.0.244, 13 September 2026

He asked how the Word file is built, wanting no dependency on Word's COM
interface or an Office installation. **There is none, and there never was.**

**Pandoc writes it.** Free, open source under the GPL, one program with no
runtime behind it, and the same converter the other Homer Tools already use. A
`.docx` is a zip of XML; anything can write one. The common shortcut is to
drive Word itself through COM, which needs Office installed, licensed and
running, and which fails outright on a machine that has none — exactly the
machine a person is most likely to be using if they are looking for a free
tool.

Checked rather than asserted: the only `InteropServices` in HomerScribe is
`DllImport` of `kernel32` and `user32`, the plain Windows API used for window
and screen-reader work. No `Word.Application`, no `Microsoft.Office`, no
`Interop` assembly anywhere.

### But nothing fetched Pandoc

That is the gap his question found. Every other component has a script —
Ollama, the models, Whisper, ExifTool, Tesseract — and Pandoc had none. A
person installing HomerScribe would get `described.md` and no Word version, and
a line in the log they might never read.

**`installPandoc.cmd`** now fetches it with winget, about 30 MB, and it is
ticked by default on the installer's last page. The message when Pandoc is
absent names that script instead of leaving him to work it out, and says
plainly that no Office is required.

## 1.0.243, 12 September 2026

He asked whether Tesseract was installed, since there was no speed gain. **It
was installed, found, and did no work at all:**

    Tesseract found at C:\Program Files\Tesseract-OCR\tesseract.exe
    ...
    Tesseract was only 0 sure, under the floor of 70.

Zero on every page, so every page fell under the floor and went to the picture
model — which is precisely why it ran at the old speed.

### Tesseract 5 writes confidence as a float

`96.5`, not `96`. And I read it with

    int.TryParse(asBit[10], out iConf);

which fails on a decimal point and leaves **zero**. Every word scored nothing,
every page averaged nothing, every page was handed to the model. The OCR was
running correctly the whole time and its answers were being thrown away by a
parse.

Read as a number now, and with the invariant culture — a machine set to a comma
decimal would have failed the same way on the same string, and that is the kind
of fault that only appears on somebody else's computer.

    conf field   int.TryParse   as a number
    96                     96            96
    96.5                    0            96
    41.83                   0            42
    -1                     -1            -1

A page of five words in the nineties averaged **0** before and **93** now: the
difference between going to the model and being taken from Tesseract.

### What he should see

Forty-six seconds a page should become one or two, with the model called only
where Tesseract is genuinely unsure. His 112-page journal should finish in a
few minutes rather than an hour and a half, and the tally line will say how the
pages divided.

## 1.0.242, 12 September 2026

The journal came out whole: **112 pages read**, both `described.md` and
`HMS Vol.15 No.1 (Compressed).docx` written, in 1:27:15.

### But the results box told him something untrue

    HMS Vol.15 No.1 (Compressed): 112 pages read, 112 pictures described

**None were described.** Every page of that journal is a scan, and a scanned
page's picture IS the page — so the describing branch never ran. The number
reported was `iFigures`, which counts pictures **found**.

Reporting found as described is the same class of untruth as trusting an exit
code, and this project has spent a fortnight on that lesson. It now reports
`iDescribed`, and says nothing at all where nothing was described.

### And 1:27 says Tesseract did not run

Eighty-seven minutes for 112 pages is forty-six seconds a page — the same rate
as the build before Tesseract existed. Either the build predates it or
Tesseract is not installed; the run log would say which, and it was not in the
upload. **`installTesseract.cmd` is the thing to try**, and the log's first
mention of Tesseract will say plainly which way it went.

### Settings move to .inix

His universal preference: a Homer Tool's configuration file is **`.inix`**, not
`.ini`. Inix is not a variant of the common format — it is a different one,
carrying multiline verbatim values and doubling as a table of records a screen
reader can move through. A format that does not follow .ini's rules should not
borrow its extension.

`HomerScribe.inix` is written from now on. An existing `HomerScribe.ini` is
still **read** where one is found, so nobody loses settings they have already
chosen; anything written goes to the new name.

## 1.0.241, 12 September 2026

**One error, and a precise one:**

    PdfRead.cs(53,46): error CS0266: Cannot implicitly convert type
    'IEnumerable<Word>' to 'IReadOnlyList<Word>'

`GetWords` hands back a sequence, not a list. Both are taken as `List<>` now,
which is the right shape anyway since each is walked more than once, and the
null checks went with the change because a list built that way cannot be null.

What is worth noting is what did **not** error. `NearestNeighbourWordExtractor`,
`DocstrumBoundingBoxes`, `UnsupervisedReadingOrderDetector`, `TextBlock`,
`TextLine` and `.Instance` on all three are real, spelled right, and used
correctly. Of three guessed APIs the compiler objected to one detail of one —
against a fortnight in which almost every guess was wrong.

The difference is that these came from PdfPig's documented layout-analysis
namespace rather than from my memory of what an image API might be called.

`lOrdered` stays an `IEnumerable` on purpose: it is walked once, so there is
nothing to gain by materialising it.

## 1.0.240, 12 September 2026 — reading order

He wants HomerScribe to be able to claim the best PDF-to-Word conversion built
on free, open source parts, run from the keyboard in batch on an ordinary PC.
Four things stood between the claim and the truth. **This is the first, and it
was the one that had to go before anything could be announced.**

A two-column page was being read **straight across the gutter**. Letters were
sorted top to bottom and grouped into lines by their vertical position, which
is right for a letter and nonsense for a brief — and a lawyer's brief is very
often two columns. Publishing the claim with that in place would have been
discredited by the first document anybody tried.

**PdfPig ships the fix and HomerScribe was not using it:**

- `NearestNeighbourWordExtractor` builds words from letters by spacing, rather
  than my grouping letters by how far apart their baselines sit;
- `DocstrumBoundingBoxes` groups those words into blocks;
- `UnsupervisedReadingOrderDetector` puts the blocks in the order a person
  reads them.

All three are better tested than anything I would write, which is the same
lesson PyMuPDF4LLM taught a few builds ago: **the library has thought about
this harder than I have.**

The old letter sorting is kept as `readByPosition` and used when the analysis
cannot handle a page. A page read badly beats a page not read at all, and a
fallback means a wrong guess about the API degrades rather than takes the
document down.

### What remains before the claim is fully true

- **the PDF's own tags** — a tagged PDF carries real headings, lists, tables
  and alt text, and using them beats inferring every time
- **tables**, from the drawn rules in `page.Paths`
- **horizontal rules**, from the same place

One at a time, and each with a build behind it. Four unverifiable API guesses
at once would give four tangled errors instead of one clear one.

## 1.0.239, 12 September 2026 — Tesseract reads the words

A scanned page can be read two ways, and until now HomerScribe used the slower
and riskier one for everything.

**The picture model** takes about twenty-five seconds a page and can invent a
word that was never there — fluent, plausible and wrong, which is the worst
kind of error in a legal document. **Tesseract** takes about a second, cannot
invent anything, and reports how sure it was of every word. On clean print its
accuracy is at least as good.

So Tesseract now reads the words, and the model keeps the two jobs it is
genuinely better at: **describing the pictures**, and **reading a page
Tesseract could not**.

- The confidence decides. Tesseract's own figure for a page is averaged across
  its words, and below **70** — `--ocr-floor` — the page goes to the model
  instead. A model that is slow is better than a reading that is wrong.
- The log says which read each page and why: *"Page 7 read by Tesseract, 92
  sure out of a hundred"*, or *"Page 9 read by qwen2.5vl:7b: Tesseract was only
  41 sure, under the floor of 70."* And a tally at the end.
- `installTesseract.cmd` fetches it with winget, about 60 MB, and is ticked by
  default on the installer's last page. Without it nothing breaks — scanned
  pages go to the model as before, and the log says so.

The TSV output is asked for rather than plain text, because it carries the
confidence and the line numbering in one pass. Checked against a real TSV: the
lines rebuild correctly across block boundaries, a word with confidence -1 is
skipped rather than counted, and the mean comes out right.

### What this changes for his 112-page journal

Roughly **forty-five minutes becomes a few**, with the model called only where
a page has a figure or Tesseract struggles. That is the difference between a
tool a lawyer uses before a hearing and one they start the night before.

## 1.0.238, 12 September 2026

He asked whether the folder should also hold a log. It should, and it does —
`HomerScribe.log`, written by `writeFileLog`, which the PDF path calls like
every other. I traced it rather than assume: `describeArchive` starts the
per-item log before it routes to `readPdf`, so the reading of the document is
in it from the first page.

So the folder holds three things:

    described.md
    HMS Vol.15 No.1 (Compressed).docx
    HomerScribe.log

and the omnibus log stays in the output directory above them, which is the
shape he described.

What was wrong was the wording. Two messages called it a **film**:

    A log of this film alone is in ...
    The per-film log could not be written: ...

True when a film was all there was. It is now also a podcast, an archive of
photographs, or a PDF, and a message that calls a scanned journal a film is the
kind of small untruth that makes a reader doubt the rest.

## 1.0.237, 12 September 2026

The Word version is named after the **source**, not after the Markdown:

    described.md
    HMS Vol.15 No.1 (Compressed).docx

`described.md` says what HomerScribe did, and that is right for it — it sits in
a folder named after the document and reports on the reading. But the Word file
**is the document**, and a document has a name. A lawyer with six converted
briefs open needs to tell them apart by their titles, and six files called
`described.docx` would not let him.

## 1.0.236, 12 September 2026 — a Word version that paginates like the PDF

He gave the reason before the requirement, and the reason is the design: **a
blind lawyer reading a converted brief has to be on the same page as the
colleague across the table.** If page 14 of the document is page 14 of the PDF,
"the second paragraph on page 14" means the same thing to both of them. Without
that, the conversion is readable and useless for the argument.

So `described.docx` is written beside `described.md`, **with a hard page break
between every page**, and it comes out the same number of pages, numbered the
same way.

### Pandoc still has no page break

I checked before building around a workaround, and the workaround is still the
answer: **jgm/pandoc issue #1934 is open**, and multi-format page breaks are
described as on the roadmap. The sanctioned solution is the official
`pandoc-ext/pagebreak` Lua filter, which HomerScribe now ships and uses.

That settles his worry about `.htm`. **With the filter, `\pagebreak` is safe in
every target**: a real break in Word, a styled div in a web page, `\newpage` in
LaTeX, and a form feed where pages do not exist. Without it, the same paragraph
reaches Word as the literal text, which is worse than nothing — so the code
says so in the log if the filter is missing rather than producing a document
with `\pagebreak` printed through it.

### The pictures are not embedded, and that was measured

He asked what embedding would cost and we worked it out: for his scanned
journal a docx holding the page images comes to about **17 MB against a quarter
of a megabyte of Markdown**, because a zip cannot compress a JPEG any further —
the Word file would weigh what the PDF weighs. The alt-text descriptions carry
the meaning at a sixtieth of the weight.

Fonts are not embedded either, for the same reason and one more: he asked for
font distinctions to be dropped where they are insignificant, and the
conversion already keeps only what structure needs — headings, lists, bold and
italic. There is no typeface left to embed.

## 1.0.235, 12 September 2026

He set out what the Markdown should carry, and it is a short list worth having
in one place: **headings, lists, horizontal rules, tables, bold and italic, and
alt text for any picture that is not decoration.** And what it should NOT
carry: font choices and sizes. No embedded HTML or CSS to reproduce styling.

That is the right line. A size decides a heading level and then has no further
business in the document; a typeface name has none at any point.

### Bold and italic are now kept

A PDF names its fonts `Times-BoldItalic` or `Arial,Bold`, so the name carries
the emphasis and nothing else is needed. A line set in bold that is not large
enough to be a heading is usually a run-in heading or a defined term, and
marking it keeps that.

    The Dante Club     bold           -> **The Dante Club**
    sotto voce         italic         -> *sotto voce*
    Important note     bold + italic  -> ***Important note***
    ordinary prose     neither        -> ordinary prose
    — — —              bold           -> — — —   (punctuation only, left alone)

Headings are not wrapped: a heading is emphatic by being one.

### Where this stands against his list

- **headings** — done, from font size
- **lists** — done, bullets and numbers
- **bold and italic** — done here
- **alt text** — done, and capped since 1.0.234
- **horizontal rules** — NOT done. A rule is a drawn line rather than text, so
  it needs the page's paths rather than its letters.
- **tables** — NOT done, and the hardest: ruled areas have to be found from
  line positions.

Two of six outstanding, and both need the same thing — the page's graphics
rather than its text. That is the next piece of work.

### And the logging convention

He reminded me of the shape: an omnibus log in the output directory and an
individual log in each subdirectory for the file processed. That is what
HomerScribe already does — `writeFileLog` writes the per-file log, and the PDF
path calls it — so nothing needed changing once the mistaken copy into
`HomerScribe\logs` was removed in 1.0.234.

## 1.0.234, 12 September 2026

Five PDFs in one run, and three things learnt.

### The two fixes landed

`c:\pdf2txt\pdf\*.pdf` **matches 5 files** now. And the inflating fix shows
plainly:

    HMS Vol.15 No.1: 0 carry their own text, 112 are scans, 112 pictures

112 scans where the run before found 11. Every page of that journal is readable
now.

A born-digital PDF works too — `Dante Club.pdf`, 644 pages carrying their own
text, body set at 11 point with three larger sizes becoming heading levels.

### But it asked about 1,286 pictures

`Dante Club.pdf` holds **1,307 pictures**, two on most pages — real ones, book
covers and street scenes, not artefacts. At roughly twenty-five seconds each
that is **nine hours**, for a document nobody would read to the end.

So there is a limit: **`--page-picture-limit`, sixty by default**. The largest
are described, since size is what separates a figure from a decoration, and the
rest are named in the Markdown without a description so nothing vanishes
silently. The log says what it is doing and how to change it.

### And the runtime log goes back where it belongs

He corrected me: this is the kind of Homer Tool that has an **output
directory**, so the run log belongs with the output. `HomerScribe\logs` is for
an installation log, not this. I read the convention too broadly and copied the
run log there as well; that copy is gone, along with the function that made it.

## 1.0.233, 12 September 2026

`c:\pdf2txt\pdf\*.pdf` found nothing, though the files are plainly there.

**The wildcard matched every one of them and then threw them all away.** The
expansion asks, of each file it finds, whether it is a video or a recording —
and nothing else. Every PDF was passed over as "not a video or a recording".

The list it checks against holds twenty-three sound and video extensions and
has not changed since before archives of pictures became a source in August,
or PDFs this month. **A source path typed out in full is accepted whatever its
kind; a wildcard was held to a narrower rule**, and nobody would guess that
from the outside.

A wildcard now takes anything HomerScribe can read: video, sound, `.pdf`, an
archive of pictures, or a list of sources. Checked:

    report.pdf     taken
    scan.PDF       taken
    notes.zip      taken
    films.m3u      taken
    clip.mp4       taken
    readme.docx    passed over

And the message for what it does skip now names the kinds it accepts, rather
than mentioning only video and recordings — which was the line that would have
explained this immediately had it been accurate.

## 1.0.232, 12 September 2026

**It read the PDF.** `described.md` written, 4 minutes 50 seconds, no crash and
no missing assembly. The whole chain works.

But the figures say the work was a tenth done:

    112 pages in HMS Vol.15 No.1: 0 carry their own text, 11 are scans,
    11 pictures in all

**101 of 112 pages produced nothing.** Only pages 1, 12, 21, 31 and seven
others were read at all.

### Why: a JPEG that does not look like one yet

That journal stores 101 of its 112 pages as

    /Filter [/FlateDecode /DCTDecode]

a JPEG, **deflated**. `RawBytes` hands back the raw stream, so those bytes do
not begin `FF D8 FF`, my format sniffing called them headerless bitmaps, and
every one was passed over. The eleven that worked are the JPEG 2000 pages,
which carry no Flate layer.

I knew this. In August, deciding not to write a PDF parser in C#, I wrote down
that this very file stores its pages as *"a JPEG, deflated, inside an object
whose length lives in a cross-reference table"* — and then built a reader that
did not inflate.

**Inflating is the whole of the fix**, and .NET has had `DeflateStream` all
along. Where the bytes are not already a picture they are inflated and looked
at again; a zlib stream's two header bytes are skipped, and raw deflate is
tried as well since a PDF may carry either.

Checked against his actual file, page by page:

    already an image       11
    recovered by inflating 101
    still not usable        0
    usable                112 of 112

## 1.0.231, 12 September 2026

**The diagnostic did its job.** His run log now says exactly what is there:

    13 assemblies are beside HomerScribe.exe: System.Buffers.dll,
    System.Memory.dll, ... UglyToad.PdfPig.Tokens.dll
    HomerScribe.exe.config: present, 1776 bytes

Thirteen present, `Microsoft.Bcl.HashCode.dll` absent. Not a binding problem, a
missing file — and that took one run to establish rather than a round trip for
the build log.

### What the research says

`Microsoft.Bcl.HashCode` **6.0.0** is on nuget.org — exactly the version PdfPig
asks for. It provides the `HashCode` type for .NET Standard 2.0, and is needed
for `net462` and similar targets and not for newer ones, which is why a library
built for seven frameworks carries it for some and not others.

So it is fetchable, and my walk simply is not finding it. Two builds of reading
nuspecs have not produced it.

### Seeded, and still walked

Rather than a sixth guess at why the reading misses it, **the dependency PdfPig
is known to need is named outright**, and the nuspec walk still runs on top to
catch anything else.

A list is a poor way to FIND dependencies — that is why the walk stays. It is a
perfectly good way to make sure of one already known by name.

### And every fetch now reports its outcome

The old code threw the answer away with `| Out-Null`. Now each says which of
four things happened: already present, arrived, downloaded but the dll was not
in it, or could not be fetched at all. If this fails again the build log will
say which, and that is a different question from the one I have been guessing
at.

## 1.0.230, 12 September 2026

Same failure at 1.0.205: `Microsoft.Bcl.HashCode` still absent. So the
nuspec walk did not bring it, and **the run log cannot say why** — it names the
assembly wanted and nothing about what is present.

That is the thing to fix first, before another guess.

### A load failure now lists what is actually there

When the error is "could not load file or assembly", HomerScribe writes out
every `.dll` beside its executable and whether `HomerScribe.exe.config` is
present and how big it is. Three runs have failed on this shape and each time
the build log had to be fetched to learn anything. A directory listing costs
nothing and answers it.

### And a sweep that does not depend on reading the nuspec correctly

A nuspec groups its dependencies **by target framework**, and a reader that
misses a group misses its contents. Rather than trust the walk, the script now
also looks at what the chosen `lib` folder actually holds: nuget puts every
assembly a build needs in the same folder, so anything there that is not
PdfPig's own is a dependency whether a nuspec mentioned it or not.

It also lists what it found, so the build log says plainly which dependencies
were named and which were picked up from the folder.

**I would rather see the build log than guess again.** The run log shows the
program, not the fetch; whether `getPdfPig.ps1` found `Microsoft.Bcl.HashCode`
and what it did about it is recorded there and nowhere else.

## 1.0.229, 12 September 2026

**It did not crash.** The window stayed, the failure was caught and reported,
and the run finished with a message rather than a vanishing. That part works:

    Nothing could be read out of HMS Vol.15 No.1 (Compressed).pdf: Could not
    load file or assembly 'Microsoft.Bcl.HashCode'

A **seventh** assembly, which was not in the list of six I had written by
hand.

### The list was the wrong idea

Guessing a dependency list gets one assembly closer each time and never
arrives. **The package says what it needs, in its own `.nuspec`** — so that is
read instead, and followed for the dependencies' dependencies as well.
`getPdfPig.ps1` now walks that graph, fetching whatever is named and whatever
those name in turn.

Three things that were lists are now readings of what is actually there:

- the **dependencies to fetch** come from the nuspecs;
- the **binding redirects** are written for every managed dll beside the
  executable that is not PdfPig's own;
- **`checkConfig.ps1`** checks that same set, so it cannot pass a config that
  omits something.

And the installer takes `System.*.dll` and `Microsoft.*.dll` rather than six
names, so it ships what the build copied instead of what somebody remembered.

### The early exit is gone entirely

It could not survive this change anyway — the dependency list is not known
until the nuspecs are read. Which is just as well: an early exit that skips
work added later has now cost three builds here, and copying a file that is
already correct is cheap.

## 1.0.228, 12 September 2026

    FATAL System.IO.FileLoadException: Could not load file or assembly
    'System.Memory, Version=4.0.2.0'

**The same exception, for a new reason.** `getPdfPig.ps1` finds the assembly
and every dependency already present, says so, and **exits before writing the
config** — so the wrong redirect left by the previous version stayed exactly
where it was.

That is the third fault of this shape here: **an early return that skips work
added since the early return was written.** The stale `pdfpig.name` marker was
the first, the dependency copying the second, this the third.

- The config is written by a function now, called on **every** path out,
  including the one that finds everything already in place.
- And the build no longer takes its existence as proof. **`checkConfig.ps1`**
  reads the real version of each assembly present and compares it against the
  redirect that names it. A mismatch fails the build, with the numbers:

      [config] System.Memory is 4.0.5.0 but the redirect points at 4.0.2.0

Checking that a file exists is not the same as checking that it is right, and
that difference has now cost two runs.

## 1.0.227, 12 September 2026

    HomerScribe.cs(821,13): error CS0103: The name 'sweepOldWork' does not
    exist in the current context

**Called once, defined nowhere.** The patch that was to add the method had two
edits in it; the second failed its anchor and the script exited before writing
either, so the method never arrived. A later patch added the call. Between them
they left a program that referred to something that did not exist.

That is the third time in this project a patch has half-applied, and the
remedy is the same each time: **check the result rather than the patch's own
report.** A count of definitions against calls would have caught it in a
second, so that is what I ran afterwards — every method added in this session,
defined against called. The other fifteen are all present.

## 1.0.226, 12 September 2026

### The redirect pointed at a version that was not there

    System.IO.FileLoadException: Could not load file or assembly
    'System.Memory, Version=4.0.2.0'
       at Homer.HomerScribe.pagesOfPdf

I wrote the config by hand with `newVersion="4.0.2.0"` — **the version PdfPig
asks for** — when the file on disk is 4.0.5.0. A redirect to something that
does not exist, which threw exactly the exception it was meant to prevent. And
because the window died with it, he heard "Processing one file" and then
nothing, and HomerScribe left the Alt+Tab list.

**The config is now generated from the versions actually on the disk.**
`getPdfPig.ps1` reads each assembly's real version and public key token with
`[Reflection.AssemblyName]::GetAssemblyName` and writes the redirects to match.
Whatever nuget hands over next year, the config will fit it. Guessing a version
was the fault; reading it is the fix.

### 260 working folders left behind

He found **1,226 files** under the work folder in 260 folders, one holding 344
page images from the PDF run that crashed.

A run that ends badly cannot tidy up after itself, so **the next run does it**:
anything under the work folder untouched for a day belongs to a run that is
over, however it ended. The log says how many went and how much space came
back. Only folders, only under `work`, nothing else touched.

### And the log was not where he looked

He looked in `HomerScribe\logs`, which is where the other Homer Tools keep
theirs, and there was no such folder. HomerScribe wrote its log beside the
output — right for a run that produces files somewhere he chose, but not the
convention.

The finished log is now **also** copied to `<app data>\HomerScribe\logs`, and
the newest thirty are kept so the folder does not become its own clutter
problem.

## 1.0.225, 12 September 2026

**Everything is in the installer now.** The compressed list holds all seven
PdfPig assemblies, all six System dependencies, and
`HomerScribe.exe.config` — so the installed copy has what the build folder has.
The `CS1702` warning is still printed, and is now answered by the config rather
than left to chance.

One cosmetic fault fixed: the log said both

    UglyToad.PdfPig.dll is beside the executable.
    PdfPig.dll is beside the executable.

The second was a leftover naming a file that does not exist. A log that reports
a file which was never there is a small lie, and small lies in logs are how
large ones get believed.

### What this does that EdSharp and FileDir do not

He asked, reasonably, since those already convert PDF to Markdown.

**They use PyMuPDF4LLM, and for a born-digital PDF it is better than what is
here.** It reads font sizes into headings, bullet runs into lists, and ruled
areas into tables. HomerScribe does the first two and not the third.

**But it can only read what is already text.** Two things it cannot do:

- **A scanned page has no text at all**, and PyMuPDF4LLM returns nothing useful
  from one. HomerScribe reads the page picture with a vision model.
- **A picture on a page stays a picture.** PyMuPDF4LLM can extract the image; it
  cannot say what is in it. HomerScribe describes it and writes the description
  as alt text.

So they are not the same job. EdSharp and FileDir convert a document that is
already readable into a form he can edit. HomerScribe takes a document that
**cannot be read at all** and makes one — which is what it does for video and
photographs too.

Neither should be ported wholesale into the other. What would help each:
HomerScribe wants table extraction, which it has not got; EdSharp and FileDir
want alt text and scanned-page reading, which need a vision model they do not
otherwise require. The natural arrangement is that a scanned PDF goes to
HomerScribe and a born-digital one to FileDir.

## 1.0.224, 12 September 2026

**The build succeeded** — thirteen files fetched, all six dependencies found,
`HomerScribe.exe` and `HomerScribe_setup.exe` written.

And it would have failed the moment he opened a PDF. Two things in that log say
so, and this is precisely the case he asked me to watch for: shipping something
that does not work as advertised.

### The compiler assumed; the runtime will not

    warning CS1702: Assuming assembly reference 'System.Memory,
    Version=4.0.2.0' used by 'UglyToad.PdfPig' matches identity
    'System.Memory, Version=4.0.5.0' ... you may need to supply runtime policy

PdfPig asks for **4.0.2.0** by name. What sits beside it is **4.0.5.0**. The
compiler shrugs; the runtime throws `FileLoadException` on the first PDF
opened. `HomerScribe.exe.config` now carries binding redirects for all six
assemblies, and the build warns if it is missing.

### The installer shipped none of the dependencies

Reading the list of what Inno Setup compressed: seven `UglyToad.PdfPig.*.dll`
and **not one `System.*`**. My pattern was `*PdfPig*.dll`, which matches
PdfPig's own names and nothing else — so an installed HomerScribe would have
had the library without what it needs to run. The six are named explicitly now,
along with the config.

### Worth noting about the checking

The config was written with a `--` inside an XML comment, which is illegal.
Parsing it caught that before it shipped. Two builds ago I would not have
parsed it.

## 1.0.223, 12 September 2026

The script works, and it names the problem in its own words:

    [pdfpig] taking ...\net462\UglyToad.PdfPig.dll
    [pdfpig] copied UglyToad.PdfPig.dll          (and six more)
    [pdfpig] dependency NOT FOUND: System.Memory.dll   (and five more)

The right assembly is chosen now. What is missing is the six that PdfPig's
.NET Framework build needs — and the reason they were never fetched is three
lines above:

    Package "PdfPig.0.1.16" is already installed.

**nuget stops there.** `-DependencyVersion Highest` never gets a chance to
resolve anything, because a package installed once is installed, whatever is
asked of it the second time. Adding that switch to a machine where the first
install had already happened changed nothing at all.

### So each dependency is fetched by name

The package is named after the assembly — `System.Memory.dll` lives in the
`System.Memory` package — so the script now downloads each missing one straight
from nuget.org and unpacks it under `packages`. That needs nothing installed
and **cannot be skipped by something already being there.**

`FetchPackage` is one function used for PdfPig and for every dependency, so
there is one download path rather than two.

Checked before delivery, since I still cannot run it: quotes balanced on every
line, braces, parentheses and brackets counted, `FetchPackage` defined before
first use, and all eight steps of the logic traced by hand.

## 1.0.222, 12 September 2026

He asked why this is taking so many iterations. The answer is in this build's
log, and it is not PdfPig.

    Join-Path : A positional parameter cannot be found that accepts
    argument 'downloading'
    At line:52 char:8
    + $pkg = Join-Path $PWD 'packages''

**A stray trailing quote, put there by my own edit.** The fetch never ran, so
the dependencies were never copied, so the same two errors appeared again.

### The real cause, which is a process fault

I had been carrying the PowerShell inside the batch file — first with caret
continuations, then written line by line with `echo`, then base64'd into
`-EncodedCommand` — and **editing it afterwards by string replacement, with no
way to check the result.** Three layers of quoting, no compiler, no parser, no
check of any kind.

That is exactly the trap I kept diagnosing in the batch file. I simply did it to
myself in another language, and it cost four of the last six builds.

### So the PowerShell is a file now

`getPdfPig.ps1` ships beside the build script and is run with `-File`. Nothing
is encoded and nothing is patched. It can be read, diffed, and checked before
it goes anywhere — and it was: every line balanced for quotes, and the braces,
parentheses and brackets counted.

The block in the batch file went from about 110 lines to 52, because a script
that is a script does not need escaping.

It also reports what it could not find:

    [pdfpig] dependency NOT FOUND: System.Memory.dll
    [pdfpig] still missing: System.Memory.dll

so the next failure, if there is one, names itself.

## 1.0.221, 12 September 2026

`TryGetBytes` and the obsolete `GlyphRectangle` are gone. Two errors left, both
the same one, and the log says why the fix for them never ran:

    References:  /reference:"...UglyToad.PdfPig.Core.dll" ... (seven, and
                 not one System.Memory among them)

**The dependency copying never happened**, because the block began with

    if exist "pdfpig.name" goto :pdfHaveName

and `pdfpig.name` was left by the previous build. **A marker written by the old
version made the new version skip its own new work.** The build was told
"already done" by a file that had no idea what this build had learnt to do
since.

That is a real trap and worth naming: **a file that records "done" cannot know
what "done" means in the next version.**

- The shortcut is gone.
- The check is now inside the PowerShell, where it can be specific: it skips
  only when the main assembly **and every dependency** are present, and says
  so. Anything missing and it does the work.

## 1.0.220, 12 September 2026

Real API errors at last, which is the sort I have been waiting for. Three
faults and a set of warnings.

### System.Memory was missing

    error CS0012: The type 'Span<>' is defined in an assembly that is not
    referenced. You must add a reference to assembly 'System.Memory'

PdfPig's .NET Framework build uses `Span` and `ReadOnlyMemory`, which live in
`System.Memory` and its companions. nuget pulls them in as dependencies; they
simply were not being copied.

The fetch now takes `System.Memory`, `System.Buffers`,
`System.Runtime.CompilerServices.Unsafe`, `System.Numerics.Vectors`,
`System.Threading.Tasks.Extensions` and `System.ValueTuple` as well, says which
it copied and which it could not find, and the build references each one that
is present. nuget is asked with `-DependencyVersion Highest` so they are there
to copy.

### TryGetBytes does not exist

    error CS1061: 'IPdfImage' does not contain a definition for 'TryGetBytes'

I guessed at that API, and at `TryGetPng` beside it, whose shape varies between
versions. Both guesses are gone. **`RawBytes` is always there**, and for a
scanned page it is the JPEG itself — so the bytes are taken and the file
extension is decided by looking at them: `FF D8 FF` is a JPEG, the PNG
signature is a PNG, `6A 50` at offset four is JPEG 2000.

Anything else is a raw bitmap with no header, which ffmpeg cannot open without
being told its shape. Those are passed over rather than written as a file
nothing can read.

### GlyphRectangle is obsolete

    warning CS0618: 'Letter.GlyphRectangle' is obsolete: 'Use BoundingBox instead.'

Six of those, all fixed. Warnings rather than errors, but a warning left alone
becomes an error at the next version of the library.

## 1.0.219, 12 September 2026

**The fetch works.** The log lists all 49 assemblies in the package across
seven target frameworks, picks `net462`, and copies seven files. That part is
finished.

And it reached the compiler, which is what I have been waiting for:

    PdfRead.cs(22,23): error CS0234: The type or namespace name 'Content'
    does not exist in the namespace 'UglyToad.PdfPig'

Two faults behind one message.

### It referenced the wrong assembly

    [pdfpig] taking ...\net462\UglyToad.PdfPig.Core.dll

`UglyToad.PdfPig.**Core**.dll` sorts before `UglyToad.PdfPig.dll`, and my
chooser took the first of the list. It now asks for the exact name and falls
back to first only if that is absent.

### And one reference was never going to be enough

**PdfPig is seven assemblies** — Core, Fonts, Tokens, Tokenization,
DocumentLayoutAnalysis, Package, and the main one — and its types live across
them. `Content` is in the main assembly; referencing only `Core` could not have
worked whichever one was chosen.

The build now references **every** `*PdfPig*.dll` it copied, and writes the list
to the log.

### A trap avoided rather than sprung, for once

The reference list was first written as

    set "pdfRefs=!pdfRefs! /reference:"%%~fF""

— a quoted `set` whose value contains quotes, which is the nested-quote fault
that has cost this script four builds in other disguises. Caught by reading it
rather than by running it, and the outer quotes are gone.

## 1.0.218, 12 September 2026

**The log spoke, and named the bug in four lines:**

    [pdfpig] found 0 copy or copies under packages
    [pdfpig] downloading from nuget.org
    [pdfpig] the package holds 0 copy or copies
    [pdfpig] no PdfPig.dll anywhere

The package does not contain `PdfPig.dll`. **The assembly is
`UglyToad.PdfPig.dll`** — and the namespace at the top of `PdfRead.cs`, `using
UglyToad.PdfPig;`, said so from the day I wrote it.

Four builds were spent hunting a filename that does not exist, and the answer
was in my own source.

- The search now matches **`*PdfPig*.dll`**.
- It copies **every dll in that folder**, because a package's companions are
  its dependencies and referencing one without them fails at run time.
- It writes the chosen name to `pdfpig.name`, which the build reads, so the
  compiler references whatever the file is actually called rather than what I
  assumed.
- The installer ships `*PdfPig*.dll` for the same reason.

### What this cost, and what actually fixed it

Five builds. The first four failed silently — a parse error, a swallowed pipe,
a loop that found nothing — and I answered each by guessing at the mechanism.
**The fifth reported what it saw, and the bug was obvious immediately.**

The lesson is not about batch files or filenames. It is that I spent four
attempts making the thing work and one making it *speak*, and only the latter
found anything. Logging is the cheaper fix and I reached for it last, on a
project whose whole method has been to measure rather than assume.

## 1.0.217, 12 September 2026

Fourth attempt at the same twenty lines. The log:

    Package "PdfPig.0.1.16" is already installed.
      downloading the package from nuget.org
    #< CLIXML ...
    Build FAILED

So the package was there, **both `for /r` loops found nothing**, the download
ran and produced nothing either.

The first loop had a pipe in its body — `echo %%F ^| findstr` — which inside
`( )` needs escaping I had not done. And I cannot tell from here why the second
found nothing, which is the real problem: **batch gives no way to see.**

### So the batch searching is gone entirely

One PowerShell now does all of it, as `-EncodedCommand`, so not one character
passes through cmd's parser. It:

- looks under `packages` first and **says how many copies it found**;
- downloads from nuget.org only if it must, and **lists what the package
  holds**;
- prefers a `net4` build, then `netstandard2`, then whatever there is;
- says which one it took, and whether the copy arrived;
- returns 0 or 1.

nuget still runs first when present, because it caches.

**The point is not that PowerShell is nicer.** It is that every one of the four
failures was invisible — a parse error, a silent loop, a swallowed pipe — and
the new version reports each step. If it fails again, the log will say where,
which none of the previous four did.

Checked before delivery: no line in that block carries a pipe, a caret, or an
unbalanced quote, and the encoded command decodes to the 40 lines intended.

## 1.0.216, 12 September 2026

nuget put the package in the right folder this time. Then **the script died
without a word**: the log ends at

    Successfully installed 'PdfPig 0.1.16' to packages
    Executing nuget actions took 323.82 ms

and nothing follows. No error, no "PdfPig ready", no compiler. A batch parse
error ends a script silently, which is the worst way for anything to fail.

The line that did it was mine:

    for /f "delims=" %%F in ('dir /b /s /o-d "packages\PdfPig*.dll" 2^>nul
      ^| findstr /i /v "\\netstandard1" ^| findstr /i "PdfPig.dll"') do (

A `for /f` wrapping a piped command, with carets escaping the pipes and doubled
backslashes inside a quoted findstr pattern. Four kinds of escaping in one
line, in a language with no way to test it short of running it.

### Both replacements avoid cmd's parser rather than satisfying it

- **The search is now two plain `for /r` loops.** `for /r` walks a tree
  natively: no pipes, no carets, nothing to misparse. The first pass prefers a
  `net4` build, the second takes whatever there is.
- **The download is one `-EncodedCommand`.** The PowerShell is base64'd into a
  single token, so **nothing in it passes through cmd's parser at all** — no
  quotes to balance, no carets, no parentheses to escape. Writing it out line
  by line with `echo` is what broke the attempt before, and writing it inline
  with carets is what broke the one before that.

That is three failures in the same place from the same cause. The rule I should
have started with: **if a line needs cmd escaping to work, find a way not to
need it.**

Every line of the block is now checked for unbalanced quotes and stray carets,
and there are none.

## 1.0.215, 12 September 2026

The build log is worth reading closely, because **nuget succeeded and the build
still failed.**

    Successfully installed 'PdfPig 0.1.16' to \packages
    ...
    PdfPig could not be fetched
    Build FAILED

Three faults, all mine.

1. **I used a variable the script does not set.** `%here%` is nothing in
   `buildHomerScribe.cmd`, so `"%here%\packages"` became `"\packages"` and the
   package landed in `C:\packages`. The script already does `cd /d "%~dp0"` at
   the top, so plain relative paths work and no variable was needed at all. I
   copied the idiom from the install scripts without checking it existed here.
2. **The search then looked in the wrong place**, which is why a successful
   download was reported as a failure.
3. **The PowerShell fallback broke on its own quoting** —
   `Get-ChildItem -Recurse -Filter PdfPig.dll $env:TEMP\pdfpig` needs `-Path`,
   and cmd's caret continuations made it hard to see. It is written to a `.ps1`
   file now rather than fighting cmd's parser.

### And no more instructions to go and fetch things

He asked for these to go, and he is right: an instruction to download something
by hand is a build script admitting defeat.

- **PdfPig** now has three routes: nuget if present; **winget to install nuget**
  and then nuget; and failing both, a direct download of the package from
  nuget.org unpacked by a written-out PowerShell script. Nothing installed on
  the machine is assumed.
- **ffmpeg** falls back to `winget install Gyan.FFmpeg` and copies what that
  puts on the path.
- **yt-dlp** falls back to `winget install yt-dlp.yt-dlp` the same way.

Where something still cannot be had, the message now says the log holds what
was tried, rather than handing him a URL and a copy instruction.

## 1.0.214, 12 September 2026

He asked whether the PDF work needs another local model. **It does not, and I
should have been clearer.**

**PyMuPDF4LLM is not an AI model.** It is a Python library doing layout
analysis — measuring font sizes, spotting bullet runs, finding ruled areas. No
model, no download beyond the package itself, and nothing that would go into
Ollama. It was mentioned as evidence that the method here is the right one, not
as something to install.

What reading a PDF actually asks of a model is already covered:

- **A scanned page** goes to `readPage`, which uses the document model where
  one is installed and the ordinary picture model otherwise. Both already have
  installer ticks.
- **A picture on a text page** goes to the picture model, for its alt text.
  Already installed by `installModels.cmd`.
- **A page with its own text** needs no model at all. The words are exact and
  the structure comes from font size.

So no new model, and nothing added to the build.

The two installer descriptions were misleading, though, and are fixed: the
vision model tick said "for describing video" when it now also reads PDFs and
describes the pictures on their pages. Somebody choosing what to install should
be told what it is for.

The only new thing the build fetches remains **PdfPig**, which is a DLL rather
than a model and was already handled.

## 1.0.213, 12 September 2026

**What EdSharp and FileDir actually use is PyMuPDF4LLM**, and the whole of it
is one call:

    sMarkdown = pymupdf4llm.to_markdown(pathSource)

`installPdfTools.cmd` names it plainly — the free PDF reader used in place of
Microsoft Word, about 25 MB, no Word and no account. `pdfRich.py` says why:

> Plain text from a PDF loses everything a screen reader user navigates by:
> headings, lists, tables, emphasis, and reading order.

### What that told me

**It validates the approach here and names exactly what was missing.**
PyMuPDF4LLM does three things to make a PDF rich:

1. font sizes become heading levels — **already built, and tested**
2. **bullet runs become lists** — missing
3. ruled areas become tables — still missing

So the font-size inference was not a guess at a method; it is the method,
arrived at independently. And the gap was named for me rather than found by
accident.

**Lists are now read.** A bullet — any of the several characters PDFs use for
one — or a number followed by a dot or a bracket starts a list item, which
closes any paragraph in progress. Checked that "In 1963 the city refused" and
"Mr. Boutwell is a gentle person" stay prose, since a sentence beginning with a
year or an abbreviation is the obvious way to get this wrong.

**Tables are the one thing still missing**, and they are the hardest: ruled
areas have to be found from line positions rather than text.

### What cannot be copied

PyMuPDF4LLM is Python, and HomerScribe may not depend on Python — his own
ruling of 24 August, and still right. EdSharp can install a helper on request
and fall back to Word; HomerScribe has no such fallback.

Two things do carry over, and both were already in place: **install on request
with graceful degradation**, as HomerScribe does for its document and reading
models, and **one rich conversion serving every target**, which is why
`described.md` is written well enough that an `.htm` could be made from it
without reading the PDF again.

## 1.0.212, 10 September 2026

**PdfPig is a plain DLL on disk now, not a resource inside the executable.**

I had carried the single-file rule over from 2htm, extCheck and urlCheck, where
it is right: those are small independent tools, and one file each is the whole
point. HomerScribe is not that. It leans on Ollama, Whisper, ffmpeg, yt-dlp and
ExifTool, and pretending the executable is self-contained was a fiction that
cost something.

It cost reliability. Loading an assembly from a byte array, which is what the
embedding required, hands the runtime a copy with no file identity: no path, no
version on disk, and a separate failure mode of its own for anything with
satellite or native parts. Referencing a DLL beside the executable is what the
runtime is built to do.

- The `/resource` embedding and the `AssemblyResolve` handler are gone.
- The build leaves `PdfPig.dll` beside `HomerScribe.exe`.
- The installer ships it.
- A `using` I had added for the resolver went with it — every use of
  `Assembly` in the program was already fully qualified, so it had been dead
  weight from the moment the resolver was removed.

## 1.0.211, 10 September 2026 — PDF reading, wired up properly

He would rather meet a build error than ship something that does not work as
advertised. So the quarantine is gone: **PdfRead.cs is always compiled, and the
build fetches PdfPig itself.**

- **The build fetches it.** `nuget install` where nuget is on the machine,
  otherwise a plain download of the package from nuget.org — which is a zip
  with the DLL inside — unpacked by PowerShell. If neither works the build
  **fails and says so**, rather than quietly producing a HomerScribe that
  cannot read a PDF while the guide says it can.
- **The executable stays one file.** `PdfPig.dll` is embedded as a resource and
  resolved at run time from inside the exe, so nothing sits beside it. That
  rule was worth keeping.
- **A `.pdf` source now goes to `readPdf`** and produces `described.md` in a
  folder named after the document.

### What it does with a page

- **A page with its own text**: the words are taken exactly, and the heading
  structure comes from font size with no model at all. Any figure on the page
  is described by the picture model and becomes alt text — which is the part no
  PDF reader does, and the reason this belongs in HomerScribe.
- **A scanned page**: read from the picture, the way a page out of a zip of
  scans already was.
- **Both**: the text is trusted for the words, because it is exact where a
  reading is a guess.

### And the furniture is dropped

A line repeating on more than half the pages is a running header or footer, not
content. Those are left out, and the log says how many.

### What is still not done

The two-view review Iris does — reading the finished document as marked-up text
and again as a flattened screen-reader view, and fixing what the second view
objects to. That is the next thing worth building, and it is the part that
would catch a heading at the wrong level.

**This has not been compiled.** There is no compiler or NuGet here. The PdfPig
calls are written from its documented API, and the first build is the test.

## 1.0.210, 10 September 2026 — PDF reading, a pilot

He asked for PDFs to be read again, this time using both layers and a C# PDF
package, with the result written as `<name>.md` in the output folder.

### What the research says

- **Equalify Iris**, whose documentation he sent, runs three phases: extract
  each page to an accessible fragment with one vision call and **verify** it,
  correcting when the verifier objects; assemble in page order; then **review
  the whole in two views** — the marked-up text and a flattened screen-reader
  view — fixing what the review objects to and looping until a round changes
  nothing. The two-view review is the cleverest part of it and costs almost
  nothing.
- **OpenDataLoader-pdf** (Apache 2.0, built with the PDF Association and the
  veraPDF people) does layout analysis for headings, tables, lists and reading
  order and emits Markdown with those preserved. It is close to the reference
  implementation of what he is asking for — in Java.
- **iTagPDF** (CHI 2026) found its errors clustered where two regions were
  semantically alike: a caption merged with a table, a header with the title.
  Worth knowing where to look when this goes wrong.
- **PdfPig** (Apache 2.0, .NET Standard, back to .NET 4.5) is what makes it
  possible in C#. It does not hand back a string; it hands back **letters with
  positions and font sizes**, which is what the structure inference needs.

### What is built

**The structure comes from font size and needs no model at all.** The size the
most characters are set in is the body; anything a sixth larger is a heading;
the distinct larger sizes rank into levels, leaving h1 for the document's own
title. Tested on realistic line data: an 18-point title becomes h2, 14-point
section headings become h3, 11-point body stays body, and an 8-point footer
stays body. That is, in essence, what OpenDataLoader's layout analysis does,
and it is free.

**Both layers are read.** `PdfRead.cs` groups PdfPig's letters into lines by
vertical position, takes the point size of each line from most of its letters
rather than its first, and writes out the page's pictures biggest first —
because on a scanned page the biggest IS the page, and on a text page it is a
figure wanting alt text. A page with almost no text and a big picture is
treated as a scan whatever else it claims.

### What is honest about this pilot

**I could not compile it.** PdfPig is a NuGet package and there is no compiler
or package here, so the half of this that calls PdfPig is written from its
documented API and has never been built.

So it is quarantined. `PdfRead.cs` is a separate file, `HomerScribe` is now a
`partial` class, and **the build includes that file only when `PdfPig.dll` is
beside it**. Without the package, HomerScribe compiles and behaves exactly as
it did before. A mistake in the untested half cannot stop the program building
at all — which matters more than the feature does.

To try it:

    nuget install PdfPig -OutputDirectory packages

then copy `PdfPig.dll` beside the build script and build. The log says which
way it went.

### Still to come

The pieces above are the foundation, not the whole. Not yet built: joining the
text and picture knowledge per page, alt text for the figures found, the
two-view review Iris does, and dropping running headers and footers — the
8-point "page 3" is correctly not a heading, but it should not be in a
paragraph either.

## 1.0.209, 10 September 2026

**Remove ads is part of HomerScribe now**, and the documentation says so.

It came in behind an unticked box with a rollback point marked in case it
proved unwise. Fifteen builds of testing later it has not: four shows, nine
episodes, ad loads from one and a half per cent to fifteen, no error lines in
the later runs, and the gate visibly refusing a real false positive — a host
asking for ratings, which the model called an advertisement at 95 and which
would have taken a piece of the programme with it.

- **`ReadMe.md`** gains a section on it, at ninth-grade reading level as the
  rest of that file is: what it does, the two files it writes, the reading
  model it wants, and the plain statement that it will not catch everything and
  is not meant to.
- **`HomerScribe.md`** gains the full account — how the four signals are
  weighed, how a span is widened and joined and moved to silence, every
  setting, and why the reading model matters.
- **`Announce.md`** gains a section, kept to the shape of the rest: short,
  plain, and honest about the limit. It says the number tracks the show rather
  than promising a figure.
- **`Hotkeys.md`** lists **Alt+R** in both its orderings.
- The rollback marker is retired, kept as a record rather than a plan.

Reading levels checked rather than assumed after the edits: `Announce.md` 6.6,
`ReadMe.md` 8.4, `Hotkeys.md` 2.9, `HomerScribe.md` 8.1. All within what he
asked for.

**The one thing the documentation does not claim** is the host-read case. The
code handles it and the scoring was tested on written passages, but it has not
fired on real material — every advertisement found across nine episodes carried
disclosure wording. So the guide describes it as how the weighing works, not as
a result, and the announcement does not mention it at all.

## 1.0.208, 10 September 2026

### The runs

Four episodes of a new show, and the best figures yet:

    7 cut of 7    5:35 removed from 44:39     twelve and a half per cent
    7 cut of 8    3:06 removed from 41:04
    5 cut of 5    6:03 removed from 40:36     fifteen per cent
    3 cut of 5    1:38 removed from 47:16

Zero errors across all of them. Two of the four had nothing rejected at all,
and the two that did rejected one and two — the gate working rather than
blocking.

### "Instead" was the wrong word

He is right that it implies the two are alternatives. They are not: both boxes
can be ticked and both do substantial work. Describing was simply not possible
on a recording with no picture, and transcribing had been asked for anyway.

    was:  ... so there is nothing to describe. It will be transcribed instead.
    now:  ... has no picture in it, so there is nothing to describe.
          Transcribing goes ahead.

### "Initializing" was labelling the whole run

This is the confusion he describes, and the log shows it plainly: of 112 spoken
messages in one run, **98 were labelled Initializing** — including every
"2 min, 4%". "Transcribing" appeared not once as a phase.

The phase word is the dialog's title, which a screen reader reads first, so it
should say which job is running. Progress during describing now says
**Describing**; progress during transcribing already said **Transcribing** and
will now be heard, since it is no longer drowned by a generic label. Setting up
a batch says **Preparing**. `Initializing` is left only for the handful of
moments that really are setting up.

No extra words: the same messages, under a truthful heading.

### "Starting." is gone

He heard it well after things had started, and every other message already
shows that something is under way.

## 1.0.207, 10 September 2026

**A host-read advertisement was being punished for being well made.** He spotted
it, and he was right.

The term is a **host-read ad**; where the host speaks in their own voice it is a
**personal endorsement**, and the FTC distinguishes the two — where the language
suggests the host is expressing their own views, they must actually hold them.
Industry guidance goes further: where a sponsor asks for no disclosure during
the read, the show is advised to put it at the beginning or end of the episode
instead.

So his reasoning was exactly right. Naming the sponsor undercuts the
recommendation, so the best reads avoid it — and HomerScribe's sponsor-phrase
list did not merely miss those. **It subtracted four points from them**, which
is backwards for the kind of advertisement that omits disclosure wording on
purpose.

### What a personal endorsement cannot omit

The call to action. The whole trade runs on conversions measured by a vanity
address or a code, and the advice to advertisers is to state it clearly and
repeat it. A host may decline to say "our sponsor"; they will not decline to
say where to go.

So a second and softer set of marks is now counted: first-person use of a
product — *I've been using*, *my go-to*, *we swear by*, *honestly, I* — and the
call to action a read cannot do without: *head over to*, *link in the show
notes*, *slash*, *that's something dot com*, *tell them I sent you*.

**Two or more of those cancel the no-disclosure penalty** and add two instead.
They do not on their own make an advertisement, which is why they are worth
less than a disclosure phrase and why the model still has to be sure first.

Worked through:

    a stealthy host read, no disclosure, six signs     95 -> 95   cut
    a classic disclosed read                           95 -> 98   cut
    a host asking for ratings and subscriptions        95 -> 91   kept
    a case detail mentioning a Honda Civic             88 -> 84   kept

The third is the one to watch: that is the real false positive the gate caught
on Black Box Down, and it stays caught, because asking for ratings has neither
a disclosure phrase nor a call to action for somebody else's product.

`stripped.md` now reports the count as **Signs of a personal recommendation**,
so the working of it is visible rather than buried.

## 1.0.206, 10 September 2026

He asked whether I had looked at the rest of the log. I had not — I fixed the
window problem and stopped. Three things in it, two good and one overdue.

### The advertisement memory is working

This is the change that came from his own idea, and the log shows it earning
its place on the second episode of the show:

    25:20 to 26:14    99 sure, raised because 7 sentences in it have been heard before
    51:41 to 51:57    95 sure, raised because 1 sentence in it has been heard before
    1:13:34 to 1:13:52  95 sure, raised because 2 sentences in it have been heard before

**All three**. The first episode taught it what this show's sponsors say, and
the second recognised them. That is the thing that gets better by being used,
doing exactly that.

### The playlist names the folder

`The Barefoot Witness`, `The After Show - The Barefoot Witness`. The change
made in 1.0.198 did not fire on the run after it and I noted it as outstanding;
it fires now. Folders are readable.

### And the false errors are gone at last

Every one of the eight `ERROR` lines was the same thing: `Skipping 626 bytes of
junk at 37856`, an ordinary ffmpeg notice, reported as a fault because
`ffmpeg -i file` with no output **always exits 1** — that is how ffmpeg says
"you gave me nothing to write". HomerScribe uses that form six times to read a
file's header.

I drafted this fix two turns ago, the patch failed on its anchor, and I said I
would finish it next build and did not. **It is applied now**: a `runProbe`
wrapper marks a command whose non-zero exit is the expected answer, and its
output is recorded rather than reported as a fault.

That makes the second time a log of his has been 100 per cent false alarms, and
the second different cause. A log where every error line is a false one is a
log nobody reads.

## 1.0.205, 10 September 2026

**He worked out the cause from the symptom, and he was right.**

After the finalizing began he could see HomerScribe in Alt+Tab, but letting go
of Alt left the focus nowhere. He guessed the window needed a message pump for
Windows to think it could be activated. That is exactly it, and a comment
already in the source says the first half of it:

    // The work runs on this thread, so the window only redraws when it is
    // given the chance.

`runCommand` was not giving it the chance. It called `ReadToEnd` twice and then
`WaitForExit`, which blocks this thread until the program finishes — and on an
85-minute podcast the silence scan takes minutes. For all that time nothing
pumped the message queue, so Windows had a window it could not activate.

- **`runCommand` now reads asynchronously and pumps while it waits**, every
  120 milliseconds. The reads had to become event-driven rather than simply
  moving `WaitForExit` first: read-then-wait exists to avoid a deadlock when a
  program fills its output buffer, and reordering would have brought that back.
  **Every external command in HomerScribe benefits**, not only this one.
- **The silence scan now uses `runScan`**, which was written for exactly this
  shape of job: it reads ffmpeg's progress as it arrives, pumps the dialog, and
  says how far along it is. It is the longest single thing HomerScribe does and
  it was the one long pass not using it. That is also why he heard the message
  once and then nothing — there was one announcement before a blocking call,
  and no way for anything to speak during it.

## 1.0.204, 10 September 2026

Three things from his 20/20 run, and all three are fixed.

### A web page was not recognised as a playlist

`ABC2020.htm` held sixty episode links and was ignored, while the `.md` beside
it worked. The link-mining was never the problem — **the file never reached
it**, because `.htm` was not in the list of kinds that count as a source list
at all. Only `.txt`, `.md`, `.lst`, `.list` and `.markdown` were.

`.htm`, `.html`, `.m3u` and `.m3u8` are now included. The anchor-reading code
was already written and finds all sixty of his links; it had simply never been
given the file.

### It was not hanging

The last line in his log is an ffmpeg command starting. That is the **silence
scan**, which reads the whole file looking for quiet places to cut at, and on
an 85-minute podcast it takes minutes — with nothing said, because nothing
announced it.

He asked for "Finalizing" messages and percentages, and he is right. Three
stages now say what they are doing:

    Listening for the quiet places to cut at
    Writing the copy without advertisements, in 4 pieces
    Piece 1, 25%
    Piece 2, 50%
    Joining 4 pieces together

The same shape as everything else that takes a while. **Anything that runs for
minutes has to say it is doing something**, and this was the last place in
HomerScribe that did not.

### The advertisements

Three found, three cut, **1:27 removed from 1:24:54** — and everything found
was confident enough to cut, so nothing was left behind by the gate:

    25:20 to 26:14    99 sure, two sponsor phrases   a sponsor read with a discount code
    51:41 to 51:57    95 sure                        Shane Company, a jeweller
    1:13:34 to 1:13:52  95 sure                      the Snap Judgment podcast on Spotify

The middle two are short, at sixteen and eighteen seconds. On a network podcast
those are plausibly whole spots. Whether more breaks were missed is a question
his `stripped.md` and the transcript can answer better than I can from the log
alone.

- New behaviours: `finalizing-says-so`, `web-pages-are-playlists`.

## 1.0.203, 10 September 2026

He asked two good questions and the answers were "no" and "yes".

### The installer did not offer the reading model

`qwen2.5:7b` was on his machine only because EdSharp put it there. Anybody
installing HomerScribe fresh would have had the picture model reading their
transcripts and would have got the poor result — **one advertisement and 23
seconds** rather than five and 2:19 — with nothing to tell them why.

**`installTextModel.cmd` is new**, offered on the installer's last page as an
optional tick and clearly labelled: about 4.7 GB, only needed for Remove ads,
and it finds five times as many. It checks whether the model is already there
before downloading, like every other component.

### Describing and transcribing cannot be affected

Traced every model call in the program rather than assuming:

    findAdBreaks        textModel()      <- the only one
    mostlyPrint         text("model")
    pictureOnPage       text("model")
    pictureOnPageSaid   text("model")
    describeImage       text("model")
    describeStill       text("model")
    readPage            document model, or text("model")

**`textModel()` is reached from exactly one place: finding advertisements.**
Everything that looks at a picture asks for the picture model by name, and
transcribing does not touch Ollama at all — that is whisper.cpp with its own
model files.

So the reading model can be changed, missing, or wrong, and describing and
transcribing carry on exactly as before. That was the intent when
`--text-model` was added, and it is now checked rather than believed.

The one place worth keeping an eye on is `readPage`, which uses the DOCUMENT
model setting and falls back to the picture model — never to the reading one. A
scanned page is a picture and must stay with the model that can see.

## 1.0.202, 10 September 2026

**The text model was the answer.** On a 58-minute episode, with
`qwen2.5:7b` doing the reading instead of the vision model:

|                     | vision model | text model |
| ------------------- | ------------ | ---------- |
| advertisements found | 1           | **5**      |
| cut                  | 1           | **3**      |
| removed              | 0:23        | **2:19**   |

Six times as much advertising gone, and 2:19 out of 58:27 is a believable load
for a podcast rather than a rounding error.

The spans are real breaks now, not slivers:

    0:25 to 1:25      sixty seconds
    50:08 to 51:23    seventy-five seconds
    57:06 to 57:11    five seconds

against the five- and twelve-second fragments of the run before. The three
changes compounded: the wording hints found them, the whole-break instruction
sized them, the widening joined them, and the text model actually read the
transcript.

Three prompt rewrites moved recall barely at all. Changing which model did the
reading moved it fivefold. **The model was the problem the whole time**, and I
spent three builds rewording instructions before testing that.

### Split breaks are rejoined

The same log showed one more pattern. Three spans in a row: **50:08-51:23 cut,
51:25-51:55 kept, 52:02-52:18 kept** — two seconds and seven seconds apart.
That is one break of about two minutes chopped into three, and the middle piece
was kept only because the sponsor phrases happened to fall in the first.

So a span the model was sure of **on its own**, sitting within twenty seconds
of one being cut, is now taken as more of the same break. The raw figure is
used rather than the adjusted one, deliberately: the no-phrase penalty exists
to catch a lone passage with nothing to corroborate it, and **a neighbour
already being cut is corroboration**.

On his three that joins the 95 at 51:25 and leaves the 85 at 52:02 — the right
answer both times.

- New behaviour: `split-breaks-rejoined`.

## 1.0.201, 10 September 2026

**The reading is now done by a text model, if one is installed.**

His Ollama holds five: `qwen2.5vl:7b`, `qwen2.5:7b`, `qwen2.5-coder:7b`,
`llama3.2:latest` and `llama3.2:3b`. The last four arrived with EdSharp. Until
now, finding advertisements in a transcript — reading and reasoning about
words, with no picture in it anywhere — was being done by the **vision** model,
because that is the only one HomerScribe knew about.

`--text-model` has existed since 1.0.194 but defaulted to empty, meaning "use
the vision model". Empty now means **take the best text-only model that is
already installed**: `qwen2.5:7b`, then `qwen2.5:14b`, then `llama3.1:8b`, then
`llama3.2`. Only models actually present are considered, which matters: naming
one that is not installed answers 404 and loses the work, as
`granite3.2-vision` did in August.

It says which it picked and why, and how to go back:

    Using qwen2.5:7b for the reading, since it is installed and is a text
    model. The picture model qwen2.5vl:7b can do this too, but reading an
    hour of transcript is not what it is best at. Pass --text-model to
    choose another, or --text-model qwen2.5vl:7b to go back to the old way.

Nothing is downloaded and nothing changes for anybody who has only the vision
model.

### Why this rather than more prompt wording

Three prompt changes have now been aimed at recall, and the last run said
plainly what they are worth: of three chunks where the advertising wording was
pointed out, **one came back with an advertisement**. The model looks away from
what it is shown, two times in three.

That is not a wording problem any more. Either a text model reads an hour of
transcript better than a vision model does — which is the experiment — or a 7B
model loses track across 1,290 passages in thirteen chunks, and the answer is
smaller chunks rather than better words. One setting settles which.

### And a caution about comparing runs

These podcast addresses carry `media_type=dynamic`. **The advertisements are
inserted when the file is fetched**, so two downloads of the same episode can
carry different ones at different times. That is why one run found an
advertisement at 53:05 and another at 51:41 in "the same" episode. Comparisons
across runs are not like for like, and neither of us should read too much into
a single number.

## 1.0.200, 10 September 2026

**Pointing at the wording worked.** Two episodes, against one advertisement
found in the whole of the previous run:

- first episode: **3 found, 3 cut**
- second: **5 found, 2 cut**, three kept for carrying no sponsor phrase at all

That is detection working and the gate working, in the same run. And the memory
took its first three sentences, so the next episode starts knowing something.

### But the spans are slivers

    1:23 to 1:28     five seconds
    58:44 to 58:49   five seconds
    51:41 to 51:53   twelve seconds

**21 seconds removed from an hour**, and 26 from sixty-eight minutes, where the
real advertising load in that show is minutes. The model is handing back the
LINE that gave the advertisement away rather than the break it belongs to.

Two changes, because either alone would be half a fix.

- **The prompt now asks for the whole break**, and says why a short answer is
  wrong: a sponsor read begins before the brand is first named, often with a
  change of subject or "we will be right back", and ends after the address has
  been repeated. A span of a few seconds is almost always wrong.
- **And a span is widened over the passages beside it that also carry
  advertising wording** — outward only, only while the wording continues, and
  only across a gap of forty-five seconds. So the Shopify read, which says its
  own address three times over, is joined into one break rather than clipped to
  whichever line the model happened to notice.

- New behaviour: `whole-break-not-the-giveaway-line`.

## 1.0.199, 10 September 2026

**It works, and it finds about a quarter of what is there.**

`stripped.md` from his 62-minute episode: one advertisement found at 95, cut,
32 seconds removed, `stripped.mp3` written. The whole chain runs. The earlier
"0 found" was him looking in the folder of the episode still being processed,
not a fault.

But the transcript of that same episode holds **at least four**:

- **Dupixent** — "I'm sponsored by Regeneron"
- **Shopify** — "head on over to Shopify.com/swordinscale and start your free
  trial today. That's right. Start your free trial at Shopify.com/swordinscale.
  That's Shopify.com/swordinscale."
- **Hotels.com**
- **Rustigo** — the one it found

Twelve of thirteen chunks answered `{"ads":[]}`. The Shopify read says "start
your free trial" twice and its own address three times, and the model walked
past it.

### The phrases were being counted at the wrong moment

They were used only AFTER an answer came back, to adjust confidence — which is
no help whatever when the model never mentions the passage. The words were in
the transcript it was reading and nothing drew its eye to them.

**They are now shown to it before it answers**, with their times, framed as
places to look rather than conclusions, since somebody can mention a product
without selling it. It costs nothing: they were already being counted.

Checked against his four: **Dupixent, Shopify and Rustigo would all now be
pointed at**, Shopify with five phrases beside it. Hotels.com would not — it is
a bare name-drop with no selling language near it, and catching that one needs
the repetition memory rather than the phrase list.

## 1.0.198, 10 September 2026

**The playlist names the folder.** His Sword and Scale run wrote everything
into folders called `0d174bea-e1da-5292-9768-04821720a3a1`, and he could not
find his own transcripts. Fair enough.

Those GUIDs are not HomerScribe's doing: a podcast link redirects to a content
network, so yt-dlp names the download after the file it lands on, and the
output folder was named after the download. But the playlist knew all along —
the link text says **"Episode 5"**. That name is now kept when the playlist is
read and used for the folder, so the results land in `Episode 5\` and are
findable.

### On the missing documents

He was right to ask, and the answer is that they were there.
`transcribed.md` **was** written, in the same folder as `stripped.md` — both in
`0d174bea…`. The four GUID folders I first took for scattering are two
episodes, each with an output folder and a work folder. Nothing was lost; it
was unfindable, which for a screen reader user amounts to the same thing.

`described.md` was correctly absent: the source is an mp3 with no video stream,
so describing turned itself off.

`scribed.md` was also correctly absent, and for the same reason — it exists to
give what was said and what was there to be seen **in one sequence**, and needs
descriptions to interleave. An audio podcast has none.

That said, there is a real gap behind his question. For a podcast the
equivalent whole would be the transcript **with the advertisements marked in
place** — what was said, and which of it was somebody selling. `stripped.md`
lists the breaks and `transcribed.md` holds the words, and nothing yet puts
them together. Worth building once the finder is reliable enough to be worth
reading that way.

## 1.0.197, 10 September 2026

**It ran.** 1,290 passages, thirteen asks, a one-hour episode. And two faults.

### An answer was found and silently dropped

Twelve chunks answered `{"ads":[]}`. One answered:

    {"ads":[{"from":"52:03","to":"52:21","sure":95,
             "why":"promotion for Rustigo, a treatment for GMG"}]}

Valid, complete, and at exactly the threshold. **The run still finished "0
advertisements cut of 0 found."** Something between that answer and the list
threw it away, and nothing in the log said what — which is the one thing a log
must never allow.

`secondsOfStamp` reads "52:03" correctly and no parse error was logged, so I
have not identified the cause with certainty. The likeliest is
`JavaScriptSerializer` handing the array back as an `ArrayList` rather than an
`object[]` in this path, which the code skipped without a word. **Both types are
now accepted**, and more importantly every step of taking an answer apart says
what it got: how many advertisements were in the answer, what each stamp read
as in seconds, and — if one is dropped — why. The next run names the culprit
whatever it is.

This is the third time in this project that a silent drop has cost a round of
testing. The lesson keeps arriving in a new place.

### Recall is a separate problem

The transcript says **"this episode is brought to you by Rustigo" at 30
seconds**, and "brought to you by" appears four times in the hour. The chunk
covering the opening answered `{"ads":[]}`. So the model is missing
advertisements it has been handed in plain words — which the sentence memory
added in 1.0.196 will help with over several episodes, but which is worth
attacking directly too.

### The output folder is a GUID

`C:\Users\Jamal\Videos\fa775617-e192-5924-b429-fb3d83cf4098\` — which is why
he found no `.md`: it is there, in a folder nobody would look in. A podcast mp3
address carries no title, so the folder is named from a hash of it. The episode
title is sitting in the pseudo-playlist as the link text — "Episode 232" — and
that is what the folder should be called. **Still to fix.**

## 1.0.196, 10 September 2026

### The checkbox never reached the settings

He ticked Remove ads twice and both logs said `Setting remove-ads = no`. Mine.
The tick was read out of the box into a local variable and **stopped there**:
it was never written back into the settings, and it was not in the list of
things remembered between runs. Both fixed. That is why the feature has never
executed once.

### Advertisements are remembered between episodes

His idea, and the research says it is *the* idea. The field is built on
repetition:

- A 2010 paper in the *Journal on Audio, Speech and Music Processing* describes
  podcast advertisements as **"inserted into and repeated, at different
  locations"**, five to thirty seconds each.
- A commercial detector fingerprints each segment and matches it against a
  database of known advertisements, scoring partial matches by similarity and
  clustering repeats.
- A patent for podcast repetitive-content detection combines **text matching,
  audio feature matching and fingerprint matching**, and treats agreement
  between any two of the three as confidence.
- Somebody built a working podcast ad blocker by fingerprinting, finding
  repeated segments, and cutting them with ffmpeg.

The audio half of that needs a fingerprinting library, which HomerScribe is not
going to carry. **The text half needs nothing** — Whisper has already written
the words down, and a sponsor read is the same words every time.

So the sentences of every advertisement actually cut are kept, per show, in
`ads-<show>.txt` under the application data. A passage repeating one of them is
corroborated by it, and so is a sentence said twice within one episode. A
programme does not repeat a sentence; a sponsor read repeats it every time.

Ten words minimum, so "thanks for listening" is not mistaken for a sponsor.
Capped at two thousand sentences, oldest dropped, since a sponsor unheard for
that long has stopped advertising.

**The second episode is better than the first and the tenth better than the
second**, which is the first thing in HomerScribe that improves by being used.

Worked through: a passage the model is only 93 sure of, carrying one sponsor
phrase and two sentences heard in an earlier episode, reaches 97 and is cut —
where before it would have been kept at 92. A passage at 96 with no phrase, no
repeat and nothing else to corroborate it still keeps, at 92, which is the
caution working as intended.

- New behaviour: `ads-remembered-between-episodes`.

## 1.0.195, 24 August 2026

**A document with media links in it is now a playlist.** Give HomerScribe his
Sword and Scale directory and it processes the 114 episodes in it, in the order
they appear.

The idea is FileDir's Play List command, Control+Shift+L, which gathers tagged
items into an `.m3u`. This is the same thought from the other end: rather than
building a list of what to play, find the list already inside something written
for people to read.

- `.md` was already accepted as a list of sources, but read as a plain list his
  directory would have been nonsense — every line of prose taken for a path. It
  is now **mined for links first**, and only falls back to the plain reading
  when there are none.
- Markdown links, HTML anchors and bare addresses are all read, and only those
  ending in something that plays are kept. On his directory that is **114 audio
  links found and 114 web-page links correctly ignored** — every episode has
  one of each, and only the title link is the audio.
- The link's TEXT is kept and logged beside it, so the log reads
  `3. Episode 232 — https://...` rather than a hundred characters of redirect.
- Order is document order, which for that directory is oldest first.

### Traced against his test

Ticking **Remove ads** and giving it `SwordAndScale.md`:

1. The file is read as a list, mined, and 114 sources come out.
2. Each is fetched by yt-dlp in turn.
3. Whisper listens to it — forced, because removing advertisements needs the
   words and their times, whether or not Transcribe audio is ticked.
4. The transcript is read in overlapping chunks; anything reaching 95 is cut,
   its edges moved into silence.
5. `stripped.mp3` and `stripped.md` are written for that episode, then the next
   one begins.

**Describing turns itself off**, since `hasPicture` finds no video stream in an
mp3, so a stray tick costs nothing.

At about 54 minutes an episode, expect roughly 20 minutes of listening and 3 of
advertisement-hunting per episode. Stopping after a few is the right way to try
it: every episode is finished and written before the next starts, so whatever
has completed is intact.

## 1.0.194, 24 August 2026

**Remove ads** — the first thing HomerScribe does that takes something away.
Alt+R, because S and A were both taken and "strip ads" had no letter of its
own.

### Four ways it could have been done

- **The transcript alone.** A model reads the words and says which passages are
  advertisements. Catches host reads, which are formulaic. But the flaw is not
  detection, it is EDGES: a break runs *content, sting, ad, sting, content*, and
  the sting has no words and therefore no times. Cut on the words and you leave
  half a jingle at each join, which is the artefact anybody notices.
- **The sound alone.** Loudness jumps and silences. Rejected outright: it cannot
  tell an advertisement from a loud passage, and the error it makes is the
  dangerous one.
- **Both, the sound used only for edges.** The transcript decides WHICH break,
  the silence decides WHERE to cut it. This is the first with its one real flaw
  repaired, and the repair is cheap.
- **Two models voting.** Sound, but it doubles the time on every file. The same
  confidence is had more cheaply by asking the model for a number and counting
  the sponsor phrases separately — agreement between those two is a second
  opinion that costs nothing.

**The third was built, with the fourth's caution expressed as a gate.**

### How it decides

The transcript is read in overlapping chunks — overlapping so that a break
straddling a boundary is not seen as two halves, each scoring badly and each
being kept. The model returns every advertisement with a confidence out of a
hundred. The passage is then scanned separately for the phrases a sponsor read
always carries: *brought to you by*, *promo code*, *dot com slash*, and thirty
more.

Two or more of those phrases raises the score by three. **None at all lowers it
by four**, because a sponsor read without one of them is unusual and a false
positive is not.

**Nothing is cut below 95.** His number, and the right one: missing an
advertisement costs half a minute of annoyance, and cutting the programme costs
a piece of it for good. Tested against six written-out passages — a classic host
read cuts at 99, a genuine product mention keeps at 84, and a passage the model
was 96 sure of but which carries no sponsor phrase at all is kept at 92, which
is the gate doing exactly its job.

Every surviving break is then moved outward to the nearest silence, so the join
closes on two quiet edges rather than mid-syllable.

### What it writes

`stripped.mp3` from an mp3, `stripped.mp4` from an mp4 — the same kind of file
that went in. There is no advantage to Matroska here: that container is in
HomerScribe because a described film needs an EXTRA audio track, and this adds
no track, it removes time.

Sound is cut without re-encoding, so nothing is lost and the cuts are exact.
Video cut without re-encoding lands on the nearest keyframe and can be a second
or two out; the log says so, and `--ad-reencode yes` makes it exact at the cost
of time and a generation of quality.

And `stripped.md`, which is how he can judge it: every break found, cut or kept,
its confidence, its sponsor-phrase count, the model's own reason, and the words
it begins with. **The ones left in are the interesting ones** — a break kept at
94 is where the caution is working, and the same real advertisement being kept
repeatedly means the number is too high for that material.

### Other notes

- The file is listened to whether or not Transcribe audio is ticked, since this
  needs the words and their times.
- `--text-model` is new, and empty by default: a question with no picture in it
  can go to a text-only model such as `qwen2.5:7b` where somebody has one. It
  changes nothing for anybody who does not.
- New behaviours: `ads-removed`, `ads-gated-at-confidence`,
  `ad-cuts-land-in-silence`.

## The rollback point, and why it is no longer needed

1.0.193 was marked as the last version before ad removal, in case the feature
proved unwise. **It did not.** It was kept as a marker through fifteen builds
of testing and is left here as a record rather than a plan.

What settled it: four shows, nine episodes, ad loads from one and a half per
cent to fifteen, zero error lines in the later runs, and the gate visibly
refusing a real false positive — a host asking for ratings, which the model
called an advertisement at 95 and which would have taken a piece of the
programme with it.

## 1.0.193, 24 August 2026

**The dependency on Python is gone.** HomerScribe was never meant to need it,
and it does not: everything else it uses ships beside it as an executable. No
process is launched for a PDF, and `pdfPages` is no longer installed.

I tried to replace it with C# and stopped, which is worth recording so nobody
starts again lightly. **Taking the pages out of a PDF is not one problem, it is
four**, and I hit all four in an afternoon:

1. **`/Length` is often an indirect reference**, so the end of a stream has to
   be found by scanning rather than read.
2. **Page dictionaries live inside `/ObjStm` object streams**, deflated, with
   their own offset table. On his journal, 562 of 903 objects were in there.
3. **Page order is the `/Kids` tree, not object number order.** Ordering by
   object number put the wrong page first — page one is a JPEG2000 and the
   lowest-numbered image is a JPEG.
4. **`/Resources` and `/XObject` are themselves usually indirect**, and
   reaching for the image without a proper resolver picks up `/Contents`
   instead. That is the bug I ended on: page 1 resolved to a 34-byte content
   stream rather than the scan.

Each is solvable. Together they are a tokeniser and an object resolver, which
is a library — and a half-right one does not fail loudly. It silently shuffles
the pages of somebody's archive, which is worse than not doing it at all.

**So HomerScribe reads a zip of page pictures, and says so plainly.** Given a
PDF it explains what to do rather than half-working: turn it into images with
any of the many free tools, name them `page001` upward so they sort, zip them,
and hand it that. Every page gets read and every picture on them described,
which is the part nothing else does.

Extracting a PDF's own text layer is likewise not a HomerScribe job. Plenty of
tools do it, and carrying a PDF library to redo it would cost the single-file
build for something already solved.

## 1.0.192, 24 August 2026

**A PDF that already carries its words now gives them to you, instead of being
refused.**

He gave it `MLK.pdf` — the Letter from Birmingham Jail — and got back
*"Nothing was done."* HomerScribe was **right about the file**: all nine pages
carry a text layer, forty thousand characters of it, and there is not one image
in the whole document. There were no pages to read because there are no page
pictures.

It was right and useless. The words were sitting there.

- **The text is now taken straight from the file.** No model, no reading off a
  picture, nothing guessed — the document's own words, which is not merely
  faster than OCR but *perfect*, in a way no reading of a scan can be.
- It comes out as `described.md` in a folder named after the PDF, the same
  shape a scanned document produces. His nine-page letter took **a second**
  rather than the ten minutes reading it would have cost.
- The lines are joined back into paragraphs, since a PDF breaks its lines where
  the page did rather than where the sentences do, and each page keeps a
  marker.
- **A scanned PDF is unaffected.** If there are page pictures, they are read as
  before; the text route is taken only when there is text and no pictures.
- Where a PDF has neither — no pictures and no text — it says that plainly
  rather than assuming it was a word-processor file.

The lesson is one this project keeps handing me: **a correct answer that leaves
somebody with nothing is still a failure.** The check for page images was
right. Stopping there was not.

## 1.0.191, 23 August 2026

**A document now leaves only the document.** The output folder was filling with
the page images taken out of the PDF — a hundred and twelve scans of somebody's
journal, sitting beside the thing that was actually wanted. They were the
input, not a result, and he still has the PDF.

- The pages are no longer copied out at all.
- **The reassembled document is now `described.md`**, which is what he said a
  PDF source should produce when he asked the question. It had been
  `<name>.md`, with a nearly empty `described.md` beside it listing metadata
  for files that no longer exist.
- No `described.zip`, and no field listing, since there are no files left to
  list.

So a PDF now produces exactly one thing: `<output>\<name>\described.md`.

Two faults found while making that change, both from the early return:

- **It skipped the tidy-up**, which would have left 112 page scans in the work
  folder under AppData after every run — growing quietly, since nothing ever
  looks there. There is a `tidyWorkFolder` now, called on that way out too.
- **It skipped the housekeeping**: the folder "View output" opens, and the
  per-source log. An early return that skips the housekeeping is how a feature
  works and still feels broken.

## 1.0.190, 23 August 2026

**Asking separately worked.** His 23:44 run, on the same 112-page journal:

    Across those pages: 25 pictures described and 497 passages marked unclear.

Twenty-five, from nought. And it cost nothing: the run took **1:25:25 against
1:27:05** for the pass that described no pictures at all — slightly faster,
because the reading prompt lost the paragraph of instruction it had been
ignoring.

The descriptions are the thing the whole feature was for. Not just "a drawing
of a bird":

- the osprey on page 21, found at last, wings extended and talons visible;
- a **map** with its lookouts named — Lehigh Furnace, Velox Rocks, Lehigh Gap,
  Little Gap, Wind Gap — and its distances noted;
- a **graph**, described by what it plots: the age distribution of Bald and
  Golden Eagles across August to November;
- an illustrator's signature read off the corner of a drawing, "Zemaitis-99".

No OCR program produces any of that.

**Passages marked unclear fell again**, 1,455 to 806 to 497 across the three
runs of the same document, as the reading prompt got shorter and more single
minded.

Two faults in the log itself, both fixed:

- **Every picture preview began mid-word** — "ob, Lehigh Furnace" for a map of
  Lehigh Furnace, "wing of a red-tailed hawk", "and alert expression". The
  preview was built with `tail`, which takes the LAST characters. It takes the
  opening now.
- **The advisory about nought pictures printed even when there were 25.** It
  only appears when the figure is actually nought.

## 1.0.189, 23 August 2026

**The picture on a page is now a separate question, because rearranging the
instruction twice did not work.**

His 18:32 run says it plainly, and says it because 1.0.187 added the line that
counts:

    Across those pages: 0 pictures described and 806 passages marked unclear.

Zero again. In 1.0.183 the illustration rule was one bullet among six. In
1.0.187 it was moved to the very front of the prompt, given a worked example,
and repeated as a closing check. Both produced nothing across 112 pages of a
journal with a full-page drawing of an osprey in it.

Twice is enough to stop blaming the prompt. **A model told to transcribe a page
transcribes the page**, and no amount of rearranging changes that.

- **The picture is now asked about on its own.** First a one-word question —
  does this page carry a photograph, drawing, diagram, chart or map? — which
  costs a second or two. Only where the answer is yes is a description asked
  for, and it is asked of the ordinary picture model, which is what that model
  is good at.
- The reading pass no longer pretends it can do both. Its instruction about
  illustrations is gone.
- The description goes at the top of the page as an `[Illustration: ...]` line,
  because a reader should be told what is on the page before being read the
  page, and where on the page it sits is not something a model can be relied on
  to place.
- **`--page-pictures no`** turns it off. The cost on his journal is about eight
  minutes if a tenth of the pages carry pictures, seventeen if a third do — on
  top of the eighty-seven the reading takes.

### What that run also confirmed

- **Not one false error.** Zero `ERROR` lines, against 26 of 68 across the
  previous six runs. The ffmpeg banner and the document/ExifTool message were
  the whole of it.
- **The percentage counting reads as intended** — `Page 2, 1%`, `Page 3, 2%`.
- **806 passages unclear**, down from 1,455 on the same document. Some of that
  is the prompt losing a paragraph of instruction it was ignoring anyway.

## 1.0.188, 23 August 2026

- **Reading a document now counts itself aloud the way the video side does.**
  It said "Picture 47 of 112: page047.png" every time, which through a hundred
  and twelve pages is a great deal of file name and very little information.
  Now the first one names what it is working on and the rest give the position:

      Processing page 1 of 112
      Page 2, 1%
      Page 3, 2%
      Page 56, 50%

  The same shape as `2 min, 53%` on a film, and for the same reason: after the
  first announcement the only thing worth hearing is how far along it is.
- **"Page" rather than "Picture" for a document.** His word and the right one —
  shorter, one syllable, and page 47 of a journal is not a picture in the sense
  the rest of HomerScribe means. An archive of photographs still says
  "Picture", because there it is true.
- The file name of each page still goes in the log, so nothing is lost; it is
  only taken out of what is spoken.
- **A document no longer reports its work twice.** The results box was adding
  "112 pictures described" under the line that already said "112 pages read" —
  the same work counted twice under the wrong noun.

## 1.0.187, 23 August 2026

Six runs of his, read one at a time and then together. The PDF work succeeded
and one thing about it failed silently.

### The scanned journal came out

112 pages of the Hawk Mountain journal, every one read, none blank, in an hour
and a half. **3,518 lines of Markdown table** — the dense count pages came
through as tables. **1,455 passages marked `[unclear]`**, about thirteen a
page, which at 144 dpi is the honest answer rather than a failure.

### But not one picture was described

**Zero `[Illustration: ...]` lines across all 112 pages** — in a journal with a
full-page drawing of an osprey in it. Describing the pictures on a page was the
whole reason for putting this in HomerScribe rather than using an OCR program,
and it never fired once.

The cause is plain in hindsight: the rule was **one bullet among six**, near
the end of a prompt whose first instruction was to transcribe the page. A model
told to transcribe a page transcribes the page and walks past the pictures.

- **The pictures are now asked for FIRST**, on their own, before any mention of
  the words, with a worked example of the line wanted — and asked for again at
  the end as a check.
- **And they are counted.** The log now reports how many pictures were
  described and how many passages were unclear, with a line saying that nought
  on a document that plainly has pictures means the model is transcribing and
  walking past them. That figure is what would have caught this on the first
  run instead of the second.

### Thirty-eight per cent of the error lines were not errors

Across the six runs, **26 of 68 `ERROR` lines were false alarms**, which is how
a log stops being read.

- **ffmpeg writes its banner to stderr on every successful run** — `Input #0,
  matroska,webm, from ...` — and that was being logged as an error. Five a run.
- **A document reported a missing ExifTool.** That one is mine, from
  yesterday: a PDF clears ExifTool deliberately, because nothing is written
  into page images, and then the "not found" message fired on it. It now says
  what is actually happening.

### A named model that is not installed no longer loses the work

One run asked for `granite3.2-vision`, which was not installed, got **404 from
Ollama twelve times, and left twelve pages blank**. The default stopped naming
it in 1.0.185, but the failure deserved handling rather than avoiding: the
first empty answer from a named model is now taken as "not installed", said
once with the `ollama pull` line to fix it, and the rest of the pages are read
with the ordinary model.

## 1.0.186, 23 August 2026

**A PDF is now a source path in its own right.** No unpacking it first.

He asked whether a PDF should be a source path of its own, or whether it should
be dropped into a zip alongside pictures. A source path of its own, and the
rest of HomerScribe had already settled it: **a film makes a folder named after
the film, an archive makes a folder named after the archive, so a document
should make a folder named after the document.**

- Give it `HMS_Vol_15_No_1.pdf` and it writes `HMS_Vol_15_No_1\described.md`
  under the output directory — exactly the shape everything else produces.
- Putting a PDF inside a zip would mean packaging a file before HomerScribe
  could read it, and leaves no sensible answer to "what if the zip holds three
  PDFs and twenty photographs".
- HomerScribe unpacks it by running `pdfPages.py` itself, in its own work
  folder, so nothing is left beside the original. If Python is missing it says
  so plainly and names the alternative; everything else in HomerScribe works
  without it.
- **The already-done check happens before the unpacking**, so a second run on a
  finished document costs nothing rather than re-extracting 112 pages first.
- **A document gets `described.md` and nothing else.** No renamed copies, no
  metadata written into the page images: "page 47 of a journal" renamed to a
  sentence about its contents is worse than `page047.jpg`, and a second copy of
  112 scans is a hundred megabytes nobody asked for. The log says so rather
  than leaving it to be noticed.
- New behaviour: `pdf-is-a-source-path`.

## 1.0.185, 23 August 2026

**I was wrong about the document model, and checking the benchmarks put it
right.** He asked whether IBM's model would noticeably outperform what
HomerScribe already installs. It would not.

- The picture model HomerScribe installs is **Qwen2.5-VL at 7B**, and it is
  already among the best document readers there is: **second of twenty-six
  models on the DocVQA leaderboard at 95.7 percent**, behind only its own 72B
  sibling, and **883 on OCRBench**.
- **`granite3.2-vision` is a 2B model.** IBM's own claim is that it is strong
  *for its size*, which is not the same as better. Swapping a 7B leader for a
  2B contender would have made every page worse while looking like an upgrade.
- I recommended it because it is *described* as being for documents, without
  checking whether it beats the model already in the box. That is the same
  mistake as trusting an exit code, in a different costume.
- **So `--document-model` now defaults to empty**, meaning the ordinary picture
  model reads pages too. Nothing extra to install, and better results.
- The setting stays, because a smaller model is genuinely useful on a computer
  short of memory — 2.4 GB against 5.5. The installer tick now says that
  instead of implying a quality gain, and `installDocumentModel.cmd` opens by
  saying most people should skip it.
- **And it now checks before it downloads.** It was calling `ollama pull`
  unconditionally; a pull on a model already present still contacts the server
  and checks every layer. It asks `ollama list` first, matches the bare name as
  well as a tagged one, and exits early when the model is there. Every other
  component was checked this way and this one was not.

## 1.0.184, 23 August 2026

Three of these come from reading the user guide of Kelly Ford's Image
Description Toolkit, which does an overlapping job well and had got some things
right that HomerScribe had not.

- **HEIC and HEIF are now read.** They are what an iPhone has produced since
  iOS 11, which makes them the formats most photographs now arrive in — and
  HomerScribe passed them over without a word. They are converted to PNG the
  way BMP and TIFF already were. Where a build of ffmpeg cannot decode one, the
  log says so and the picture is reported rather than silently lost.
- **A PNG now keeps its description in its own text chunks.** A PNG has no EXIF
  at all, so `XPTitle` and `XPComment` had nowhere to live in one and the
  Windows Comments column stayed empty. `PNG:Description`, `PNG:Title` and
  `PNG:Comment` are written as well, which is where other tools look.
- **What the camera recorded is now given to the model as context** — "This
  photograph was taken on 10 June 2024 with a Canon EOS R5." It was already in
  the file and already being ignored. A date in particular helps place a
  photograph in an era.
- The Toolkit goes further and turns the GPS into a place name by asking
  OpenStreetMap. **HomerScribe deliberately does not.** That would be the first
  thing it ever sent off the machine, and asking somebody else's server where a
  photograph was taken is worth more than the context would be. The date and
  the camera cost nothing and leak nothing.
- **The archive of renamed copies is now `described.zip`**, not
  `<archive>.zip`. It says what is in it, it matches `described.mkv` and
  `described.md`, and it cannot be mistaken for the archive that went in.
- One fault caught before delivery: the camera lookup was calling
  `exifToolProgram()` inside the loop, which would have re-run the whole
  candidate search — every path, every `-ver` — once per picture. It is found
  once per archive now.

## 1.0.183, 23 August 2026

**Recognising print is now part of describing**, which is where he decided it
belongs, and he was right: a page of a journal is a picture, and HomerScribe is
the thing that can look at it. There is no new checkbox.

- **Every picture is now asked one question first**: is this mostly print, or
  mostly a photograph? One word, at temperature zero, costing about a second.
  A photograph is described as before. A page of print is **read**, and set out
  as Markdown.
- Asked of the MODEL rather than guessed from the file name, because the file
  name of `page047.jpg` says nothing.
- **A page is read into Markdown**, with headings as headings and tables as
  tables, read DOWN each column in turn rather than across the page — which is
  where ordinary OCR scrambles a magazine. An illustration on the page is not
  skipped: it gets a short description in square brackets, in its place, so a
  reader who cannot see it still knows what was there. **Nothing else does
  that**, and it is the reason this belongs in HomerScribe rather than in an
  OCR tool.
- **The instruction against invention is repeated, and aimed at numbers.** A
  vision model that cannot quite read a figure supplies a plausible one, and
  unlike ordinary OCR the result looks right. Anything unreadable must come
  back as `[unclear]`. The rebuilt document carries a warning saying so and
  telling the reader to check numbers, names and dates first.
- **The pages are put back together in order** as `<name>.md` — the scanned
  document, readable at last.
- **`--document-model`** chooses the model for pages, defaulting to IBM's
  `granite3.2-vision`, which is built for documents, tables and charts and is
  about 2.4 GB against the picture model's 5.5. Empty uses the ordinary model
  for everything. **`--read-pages no`** turns the whole thing off.
- **`installDocumentModel.cmd`** fetches it, and the installer offers it as an
  optional tick on the last page.
- **`pdfPages.cmd` and `pdfPages.py`** turn a scanned PDF into a zip of page
  pictures, which HomerScribe already accepts. Tested on a 112-page scanned
  journal: all 112 pages out, in reading order, with a note in the archive
  telling the model what it is looking at.
- Why a separate script and not C#: that journal stores its pages as
  `/Filter [/FlateDecode /DCTDecode]` — a JPEG, deflated, inside an object
  whose length lives in a cross-reference table. Handling that in every case
  means object streams, several filters and encryption, which is a library's
  worth of work that pypdf already does correctly. Reusing what exists beats
  inventing it.
- `askAboutImage` factored out of `describeStill`, since three callers now need
  it and three copies of the same twenty lines is how they drift apart.
- New behaviours: `pages-of-print-are-read`, `scanned-document-rebuilt`.

## 1.0.182, 22 August 2026

- **`License.md` now names HomerScribe and its author**, which it did not. It
  opened with a bare "# License" and the MIT text, and the only place the
  program appeared was the copyright line.
- It now opens by saying that **HomerScribe was written by Jamal Mazrui, its
  author and developer**, and is released under the MIT License, followed by a
  plain-English summary: use it, change it, sell it, keep the notice, no
  warranty.
- **The MIT text itself is untouched, word for word**, and the file says so and
  says why: it is left exactly as it is written everywhere else so that a
  person, a legal team, or a program that scans for licenses recognises it at
  once. Rewriting a standard license to mention your own program is how a
  standard license stops being one. A line above it explains that where the
  text says "the Software" it means HomerScribe — the program, its source, and
  the documents that come with it.
- Checked before delivery by normalising the whitespace and comparing the body
  against the canonical MIT wording: identical.

## 1.0.181, 22 August 2026

- **`Announce.md` rewritten to announce the CHANGES rather than the program.**
  It had grown by having new sections added to an announcement of the whole
  thing, so the two new features sat second and third of nine headings. For a
  post telling people what is new, that is the wrong shape. It now opens with
  the pictures, then the captions, then the honest limits, and it is 1,018
  words rather than 1,700.
- **Reading level measured rather than assumed.** Flesch-Kincaid across the
  documentation set: `Announce.md` was already 7.6 and is now **6.0**;
  `ReadMe.md` 8.5, `HomerScribe.md` 8.0, `Hotkeys.md` 2.9, `History.md` 8.2,
  `Developer.md` 8.3. So nothing was above ninth grade to begin with; what
  needed fixing was the framing, not the words.
- The tone is deliberately humble: it says plainly that trying to improve
  descriptions with caption speaker names **has not worked yet**, that
  description is the weaker half, and that none of this replaces a described
  version made by people who do it for a living.
- Every claim in it was checked against the source before delivery — eleven of
  them, each traced to the function that does the thing.

## 1.0.180, 22 August 2026

The documentation is now laid out the way Jamal wants every Homer Tools app
documented, which he set down today as a standing preference rather than a
one-off request.

- **`ReadMe.md` is now the short way in**: what HomerScribe is, what you need,
  how to install it, a Quick Start, and a list of where everything else lives.
  It was 1,440 lines and is now under 200.
- **`HomerScribe.md` is new — the complete guide.** Every setting, every
  document it writes, what it does with captions and with pictures, and what to
  do when something goes wrong. That is the 31 sections that used to make
  `ReadMe.md` unreadable as an introduction.
- **`Hotkeys.md` is new**, listing every key three ways: by what you want to
  do, by where you are when you press it, and by the key itself. Built from the
  actual bindings in the source rather than from memory — the twelve `Alt`
  access keys in the dialog, and the editing, reading and navigation keys the
  accessible dialog provides.
- **Every `.md` now has a matching `.htm`**, made with Pandoc, with a table of
  contents and a real page title. Nine of each.
- The installer packages `HomerScribe.md`, `Hotkeys.md` and `video_formats.md`
  along with their `.htm` companions.

## 1.0.179, 22 August 2026

Two faults his four new runs exposed, one of them mine from yesterday.

- **The nearby-speaker hint fired for the first time — and carried NARRATOR.**
  His logs read *"GPS and FORTIER and NARRATOR speaking around 22:02"*. A
  narrator is never in shot, and GPS is not a person at all. `spokeAround` was
  taking any speaker label, while the cast list applied a filter that rejects
  exactly those. Nearly every hint carried a narrator, turning the useful
  single-name case into a three-name guess. **The same filter now applies to
  both.** On his eleven real hints that takes the ones naming exactly one
  person from one in eleven to three in eleven.
- **The cast list stopped being logged**, because when it moved out of `lNames`
  in 1.0.178 its log line was left reading `lNames`. Six runs later there was
  no way to tell from a log whether it had reached the model at all. Moving a
  thing and leaving its evidence behind is the same mistake as trusting an exit
  code. It now logs from the roster itself.
- `Announce.md` gains the **Windows installer** and **Project on GitHub** links
  at the top as well as at the foot, and says plainly that captions a person
  wrote are used while captions a machine made are not.
- `Review.md` gains a measured account of what the runs show: transcription is
  better for the caption work, descriptions are not yet, and the announcement
  says only the first.

## 1.0.178, 22 August 2026

He asked why speaker names from a person's captions were not adding value, and
he was right to. My previous answer — that names without appearances cannot be
attached to faces — was true as far as it went and beside the point. **The
names were never offered to the model as names.**

- The roster was poured into `lNames`, which the prompt renders as *"Names you
  have already used in this film"*, followed by *"if somebody here matches how
  one of those was DESCRIBED, use that name again"*.
- That paragraph is for CONTINUITY: names the model coined itself, whose
  appearance it knows from its own earlier sentences. For a name off a caption
  track none of it holds — the model has never used it and there is no earlier
  description to match against, so the instruction is unanswerable. Handed a
  list labelled as its own prior work when it had done no such work, the model
  did the only sensible thing: nothing.
- **The roster now has its own paragraph**, saying what it is: the film's cast
  list, spelled as its makers spell it, which does NOT say who is in this shot.
  A name is to be used only where the model can actually tell — because it was
  told what that person looks like, or because the picture itself says so, such
  as an on-screen caption naming them. Never because a name is the only one
  left.
- **Whoever spoke near the moment is now its own paragraph too**, and last,
  because a model weighs the end of a prompt most. It is the one piece of
  caption evidence tied to this moment rather than to the film as a whole:
  where exactly one person is named there and exactly one person is in shot,
  saying who it is will usually be right.
- **That was being looked for in the dialogue window**, twenty-five seconds,
  which is sized for quoting the line just spoken so a description does not
  repeat it. "Who is in this scene" is a scene-sized question, so it has its own
  setting, `--speaker-window`, at two minutes either way. More than three names
  in the window and nothing is said at all: a dozen is a cast list, and offering
  one invites the guessing the prompt forbids.
- Being straight about which of these is proven: **the mislabelling is a
  definite bug with a definite fix**. The window is a reasoned adjustment whose
  effect is not yet demonstrated — the five description moments in his NOVA
  film had caption cues nearby at any window size, so what was missing was a
  cue that named somebody, not a cue. The new log line — *the captions have X
  speaking around 3:16* — makes the next run answer it.
- New behaviours: `roster-named-as-a-cast-list`, `speaker-window-scene-sized`.

## 1.0.177, 22 August 2026

- **"Done" was spoken twice**, and the reason is in a comment I wrote myself in
  `flushAnnouncements`: a screen reader reads a dialog's title, then reads the
  dialog — title and all — when focus lands on it, so anything in the title is
  heard twice. The title is the CATEGORY. I passed "Done" as the category AND
  "Done" as the message, walking straight into the thing that comment exists to
  prevent. **The message is now what finished** — "Done. Introduction to Web
  Accessibility" — which is the useful half anyway.
- **Nothing said what the film was before the work began.** The opening added
  in 1.0.173 is spoken INSIDE `described.mkv`, so it is only heard when the
  finished film is played, and only when describing was asked for at all. He
  was listening for it during the run, where the last thing said was
  "Downloading" and then a long silence. **The title, the publisher and the
  running time are now announced once**, when the film is in hand and before
  either job starts.
- **Captions a person wrote are now trusted over Whisper; captions a machine
  made are not.** He asked whether the two can be told apart. They can, and
  HomerScribe always could — `looksAutomatic` decides from per-word timing tags
  and from cues that roll up the screen repeating the line before, and his logs
  have been saying "written by a person" or "made automatically" all along.
  What it did not do was act on the difference: any captions at all displaced
  Whisper.
- The two are not comparable. **A person's captions carry the words, who says
  them, and the sounds that are not speech** — a door, music starting,
  laughter. No machine produces that last part, and it is the one a deafblind
  reader cannot get any other way. **A machine's captions are the words and
  nothing else**, with punctuation only where the recogniser guessed, and
  Whisper is at least as good at the words and better at the sentences. So for
  a machine-made track the richer transcript is the one that listens.
- `--auto-captions` (default no) accepts them anyway. `--captions` is unchanged.
- New behaviours: `film-announced-before-work`, `person-captions-only`.

## 1.0.176, 22 August 2026

- **`descriptions.md` is now `described.md`**, matching `described.mkv`. It was
  the odd one out among the documents.
- **There is one in the output folder and one inside the archive**, and they
  are not copies of each other. The folder holds the pictures under their
  original names and the archive holds them under their new ones, so each
  document lists its own copies by the names they actually carry there.
- **The content is now what is IN each picture, not what HomerScribe put
  there.** A heading per file, then every field that has a value, sorted by
  field name without regard to case, with the group each field belongs to in
  brackets so a bare name like `Description` tells you where it lives.
- Anything that was already in the file is listed beside what HomerScribe
  wrote — the camera, the date, the lens, somebody else's caption — because the
  question a reader has is what this file says about itself, not what this
  program did to it.
- **Read back out of the finished files with ExifTool**, not assembled from
  what was sent to them. Same reason as the counts: what was sent is a hope,
  what reads back is the fact. This is now true of every figure and every
  field HomerScribe reports about a picture.
- The `System` and `ExifTool` groups are left out: file dates, permissions and
  the folder a file happens to sit in belong to the disk rather than the
  picture, and they change every time it is copied.
- The old report is gone, and with it `metadataStanding`, which existed only to
  phrase a line in it. The descriptions themselves are not lost — they are in
  the files, in `ImageDescription` and `Caption-Abstract` and the rest, which
  is where they were wanted and where the new document reads them from. What
  that document cannot show is the things that never became pictures, so the
  notes and the passed-over files are named in a short closing section.
- New behaviours: `described-md-in-both-places`, `fields-read-back-not-assumed`.

## 1.0.175, 22 August 2026

He asked whether captions improved the descriptions. The 08:20 run is the first
that can answer it: 39 speakers in the roster, and five descriptions placed.

- **The answer is no, and now it is measured rather than argued.** Not one of
  the real names — Susana Martinez-Conde, Thalia Wheatley, Anil Seth and the
  rest — appears in any of the five descriptions.
- **The only roster entries that did appear were MAN and WOMAN**, which the
  descriptions would have used anyway. That is a bug, not a result. Subtitles
  for the deaf label an unidentified speaker by what they are: MAN, WOMAN,
  MAN 2, SECOND WOMAN, REPORTER. Those were going into the roster as though
  they were people's names, which tells the model that "MAN" is a name this
  film uses and invites it to read a generic word as an identification. They
  are now kept out, along with roles like HOST, DOCTOR and COMPUTER. On his own
  NOVA roster that removes seven entries and keeps twelve.
- **Why the real names went unused is the thing I predicted and can now show.**
  A roster says who exists. It says nothing about what any of them looks like,
  so the model has no way to attach a name to a face — and it correctly
  declines to guess, because it is told not to. The captions supply the WHO;
  only a context file supplies the WHAT THEY LOOK LIKE; a name gets used where
  the two meet, and here only one of the two was present.
- **The nearby-speaker hint fired zero times**, which is also explicable rather
  than broken. NOVA is wall-to-wall narration: five descriptions in
  fifty-four minutes, each placed in one of the rare silences. A labelled
  caption cue was never close enough to one of those silences to fall inside
  the dialogue window.
- The new spoken opening worked: *"Your Brain: Who's in Control? | Full
  Documentary | NOVA | PBS. Published by NOVA PBS Official. 54 minutes long.
  Audio description is on."*

## 1.0.174, 22 August 2026

The format test ran properly this time, and the log answered two questions
without anybody having to look.

- **The accessibility fields work on his 2019 ExifTool.** The self test picked
  the TIFF this time rather than the BMP, and the log shows both fields NOT
  written when asked plainly, then both written with the definitions supplied.
  `IPTC:Caption-Abstract` also reads as written, which confirms the punctuation
  fix in 1.0.172.
- **ExifTool 11.79 cannot write WebP at all**: *Writing of WEBP files is not
  yet supported*. WebP writing came long after that version. So one of my
  format lists was wrong for this copy — and the honest fix is not to correct a
  list, because which formats can be written depends on the version in use.
  **HomerScribe now learns it from ExifTool during the run**: a picture refused
  that way is counted as a format with nowhere to put a description, exactly as
  a BMP is, rather than reported as a failure. The log says so and adds that a
  newer ExifTool may manage it.
- **Names were still being cut mid-phrase.** "A bright yellow sun rises behind
  two dark green triangular hills against a" stops in the middle of a thing.
  Cutting at the last space before the limit is not enough — wherever the cut
  lands, the last word has to be one that can end a phrase. The backing-off
  that `trimToPhrase` already did is now shared with the length cut in
  `friendlyName`, so no route to a name can leave a dangling article or
  preposition. That one becomes "...two dark green triangular hills", and the
  other two test names come out at 75 and 76 characters.
- The rest of the format test passed as predicted: 4 pictures, 2 notes and 1
  other file; the SVG named as a drawing; both notes found and used; the
  camera-junk GIF name correctly yielding no context.

## 1.0.173, 22 August 2026

Six things his two runs turned up. Three of the four predictions for
`test.zip` were right; the fourth was not, and the metadata result was a false
alarm caused by my own test.

- **The self test picked the wrong picture.** It used the first in the archive,
  and in `test.zip` that is a BMP — a format ExifTool refuses outright:
  *Writing of BMP files is not yet supported*. So every field came back NOT
  written and it announced that the accessibility definitions had failed, when
  the same definitions had worked perfectly on a JPEG an hour earlier. **It now
  picks the first picture whose format can hold the full set**, and says so
  where none can. Nothing was ever wrong with the writing.
- **The camera-junk name got through**: `IMG_20240115_WA0042.gif` was offered to
  the model as context. Same trap as the housekeeping words a day earlier, one
  step earlier in the same function — the camera pattern ran while the
  underscores were still there, and `_` is a word character, so
  `\bIMG[\s_-]?\d+\b` cannot match across `IMG_20240115_`. **Separators are now
  normalised before any pattern looks at the name**, which is where that line
  should have been from the start. Five camera forms added to the checks.
- **And twenty of the forty-nine names mostly repeated the file's existing
  name**, which is the real answer to why so little looked renamed.
  `Jamal_Mazrui_signature.jpg` became "Jamal Mazrui Signature" — tidier, and
  saying nothing new. The cause is the file-name context added in 1.0.163: the
  file name is handed to the model as authoritative, and the model hands it
  straight back as the name.
- Replacing such a name with the description would lose the names, and the
  names are the one part no model could have worked out. So a short name is
  now **kept and extended**: "Jamal Mazrui Signature, handwritten signature on
  a white sheet of paper". The prompt also says outright not to give back the
  name the file already has — use its people and places, but say what is
  visible.
- **The names were too short** — 21 to 25 characters against an allowance of 79,
  and he was right to say so. Two changes. The prompt now asks for **eight to
  fourteen words** rather than naming a character ceiling, because a ceiling is
  permission to stop rather than something to aim at. And where an answer comes
  back in fewer than five words, the name is taken from the description
  instead, cut at a phrase rather than a word count — `trimToPhrase` prefers a
  comma and backs off any trailing joining word, so a name no longer ends "in a
  pale blue". On his four test pictures that turns 21–25 characters into 50–68.
- **The spoken opening now names the film.** It has always said only "Audio
  description is on." He noticed the film's own closing credits named NOVA
  while nothing at the start did. The documents have opened with the title, the
  publisher and the running time since 1.0.150; the spoken opening never did.
  It now gives the title, who published it, and how long it runs — the running
  time in words, since a clock reading would be spelt out digit by digit.
- **"Done" between one source and the next.** After a long silent stretch the
  only signal was the next "Processing" line, which says a new thing has begun
  without ever saying the last one ended. Not said where a source failed or was
  skipped: those have spoken for themselves, and "Done" after "Error" would be
  a lie.
- **The first field's text is selected when the dialog opens.** Windows
  convention for a data entry field, and Microsoft's own guidance: highlighting
  the value lets you type or paste over it, or press Tab to leave it alone.
  WinForms does this when a box is reached by Tab but not when it is simply
  made the active control before the form opens, which is how this one starts —
  so it is done explicitly, on Shown, and only for an editable box with
  something in it.
- **Two counts of the same thing disagreed**: the results box said "2 of a kind
  that cannot hold one" while the log said "1 of a kind with nowhere to put
  one". A GIF is neither — it holds a plain comment — and only formats with
  nowhere at all belong in that figure. Counted once now.
- The rest of the format test passed: 4 pictures, 2 notes and 1 other file; the
  SVG named as a drawing rather than passed over as junk; both notes found.
- **And the caption speaker roster fired for the first time**, on the NOVA
  films: *The captions name 38 speakers* on one and 26 on the other.
- New behaviours: `opening-names-the-film`, `fuller-picture-names`,
  `done-between-sources`, `source-box-selected`.

## 1.0.172, 22 August 2026

- **It works, and on his 2019 copy.** The self test settled the question it was
  built for. ExifTool 11.79, asked plainly, refuses both accessibility fields:

      XMP-iptcCore:AltTextAccessibility: NOT written
      XMP-iptcCore:ExtDescrAccessibility: NOT written

  Given HomerScribe's own definitions with `-config`, the same copy writes them:

      XMP-iptcCore:AltTextAccessibility: written
      XMP-iptcCore:ExtDescrAccessibility: written

  And the run finished **49 pictures described, 49 carrying their descriptions,
  49 of them in the IPTC accessibility fields**, counted by reading the files
  back rather than by trusting an exit code. He was right that a current
  ExifTool was not needed, and the version requirement is gone for good.
- The single-file rule held too. The log shows the winget copy passed over by
  name — *it needs an exiftool_files folder beside it* — and his own
  single-file 11.79 chosen instead.
- **One line of that self test was still wrong, and it was my parsing.**
  `IPTC:Caption-Abstract` was reported "NOT written" on every pass while being
  written perfectly well. ExifTool's `-s` prints the tag's own name, and IPTC's
  is `Caption-Abstract`, hyphen and all; the check stripped the hyphen from what
  it looked for but not from what ExifTool printed, so it never matched. Tag
  names are now compared with the punctuation taken out of both sides.
- That the report was the thing at fault rather than the work is the same shape
  of error as counting exit codes, and it is worth noticing that the self test
  is what exposed it.

## 1.0.171, 21 August 2026

- **One file, and only one file.** `exiftool.exe` must be a self-contained
  binary with nothing beside it — no `exiftool_files` folder, no Perl DLLs.
  His requirement, and it now governs the build, the installer and the
  program.
- **This rules out everything currently published, and it is worth recording
  why so nobody quietly undoes it.** The exiftool.org download, the SourceForge
  download and the winget package `OliverBetz.ExifTool` are all the same thing:
  a small launcher plus an `exiftool_files` folder holding Perl. The
  `Image-ExifTool` tarball is Perl source and needs Perl installed. **Nobody
  publishes a current single-file build.** The one-file form is the older
  exiftool.org format, a packed archive, and the launcher replaced it
  deliberately — unpacking a packed archive on every invocation is slow, and
  HomerScribe runs ExifTool once per picture.
- A third-party single file does exist on GitHub. It is a personal archive with
  no following whose own README points at somebody else's releases, and it is
  not something to hang a build on.
- **None of this costs anything**, which is the point. Since 1.0.168
  HomerScribe carries the definitions of the two IPTC accessibility properties
  itself and hands them to ExifTool with `-config`, so an older single-file copy
  writes all nine fields. His existing 11.79 is a perfectly good answer.
- So the build no longer fetches anything. It looks for a single-file copy — in
  its own folder, `C:\HomerScribe`, and the installed program folders — says
  which it found and how old it is, and **deletes any `exiftool_files` folder a
  previous attempt left behind**. The installer packages the one file and not
  the folder.
- **The program passes over any ExifTool with an `exiftool_files` folder beside
  it**, naming it in the log, so the rule is visible rather than silent.
- `installExifTool.cmd` no longer pretends it can fetch one. It moves a
  single-file copy into place if there is one, and otherwise says plainly what
  is wanted and why nothing can be downloaded.
- New behaviour: `single-file-exiftool-only`.

## 1.0.170, 21 August 2026

- **The build could not fetch ExifTool, and his log says exactly why.** Both
  download attempts came back "Too small to be the archive". That message is
  the one part of the attempt that worked: the size check caught that what
  arrived was not a zip.
- **SourceForge's `/download` address serves a web page, not the file** — one
  reading "Your download will start shortly", with the real file behind a
  redirect. What landed was a few tens of kilobytes of HTML. And exiftool.org
  404s, because it now points its download links at SourceForge and says so on
  its own front page.
- **So the build asks winget first, and it should have all along.** `winget
  install -e --id OliverBetz.ExifTool` is the supported way to install ExifTool
  on Windows. winget knows the current version, knows where to get it, and
  handles the mirrors. Scraping a download page knows none of those things.
  After it runs, the build copies the result in from wherever winget put it.
- A direct fetch remains as a last resort, and is now aimed correctly: Oliver
  Betz's own installer first — which is what winget would have fetched anyway —
  then `downloads.sourceforge.net`, which is the real download host rather than
  the page that advertises it. The size check stays, since it is what caught
  this.
- `installExifTool.cmd` gets the same chain.
- One note for whoever edits these scripts next, mine included: **anchor on the
  line, not the substring.** Locating a block by `:exifToolDone` matches inside
  `goto :exifToolDone` too, and that is how a duplicated block got into
  `installExifTool.cmd` earlier today. Both edits here were line-anchored and
  both were checked for undefined and duplicated labels afterwards.

## 1.0.169, 21 August 2026

- **No Windows ExifTool needs Perl installed.** Worth saying plainly, because
  the worry would have kept a better copy out of use. Both Windows
  distributions carry Perl inside them: the exiftool.org package keeps it in
  the `exiftool_files` folder, and the self-contained build has the whole of
  Perl packed into one executable. What *does* need Perl installed is the
  `Image-ExifTool` source tarball, which is the Unix and Mac route, and that is
  the part of the install page that mentions it.
- **`winget install exiftool` resolves to `OliverBetz.ExifTool`** — an Inno
  Setup installer of a self-contained package, currently 13.59, by the same
  Oliver Betz whose launcher the official exiftool.org packages already use. It
  is not more cumbersome than the 2019 copy; it is the same shape of thing,
  four years newer, and it is the one to use.
- **HomerScribe now finds every ExifTool on the machine, runs each one, logs
  them all with their versions, and chooses the newest** — saying which and
  why. The places searched cover winget's two habits, a real install under
  Program Files or the user's Programs folder and a shim under `WinGet\Links`,
  as well as the package folder it unpacks into, HomerScribe's own folder, and
  the PATH. Guessing where winget puts things is how that question gets
  answered wrongly; the log now answers it.
- **A bug my own test caught before delivery.** Comparing versions as decimals
  makes 13.8 look newer than 13.11 — but ExifTool released 13.11 *after* 13.8,
  so the older copy would have been chosen. Each part is now compared as a
  whole number.
- `buildHomerScribe.cmd` and `installExifTool.cmd` look in the same places, and
  the failure advice now offers the winget line and says that no Perl is needed.
- New behaviour: `newest-exiftool-chosen`.

## 1.0.168, 21 August 2026

He asked three things, and was right about all three.

- **"Are you using correct command-line syntax?"** Yes, and it should have been
  written down. Writing is `exiftool -@ <argfile>` with `-overwrite_original`,
  `-m` and `-charset UTF8`, one `-TAG=value` line per field — an argument file
  rather than a command line, because a description holds quotation marks,
  ampersands and accented letters. Reading is `exiftool -j -q -m -charset UTF8
  -TAG -TAG ... <folder>`, parsed as JSON. **ExifTool does both halves**, and
  the count reported comes from the reading half.
- **"I am surprised the latest ExifTool is really needed."** He was right and I
  was wrong. The two properties genuinely are new — the IPTC added them in
  October 2021 and his copy is from October 2019 — but concluding that an older
  ExifTool therefore *cannot write them* was a mistake. **ExifTool has been able
  to write tags it does not know for far longer than these tags have existed**,
  given their definition. Its own documentation says any namespace may be
  written by giving a family 1 group name, "including namespaces which are not
  pre-defined by ExifTool".
- **So HomerScribe now carries the definition itself.** When a copy does not
  recognise the fields, a small configuration file is written naming the
  `Iptc4xmpCore` namespace, its URI as the IPTC publishes it, and the two
  properties as `lang-alt`, and the write is repeated with `-config`. The same
  bytes land in the same place a current ExifTool would have put them. **The
  version requirement is gone**; a newer copy is merely preferred.
- The definitions were checked before delivery: extracted from the compiled-in
  text, passed through `perl -c`, and evaluated to confirm the namespace URI,
  the group names and both properties as `lang-alt`.
- **"Do not ask me to check things manually."** Fair, and it had happened more
  than once. **HomerScribe now self-tests before the first picture**: it copies
  that picture aside, writes every field into the copy, reads them all back, and
  logs each one BY NAME as written or not written, with the version in use and
  the commands run. Then it chooses how to write the rest and says which way it
  chose and why. One log now answers every question I had been putting to him.
- New behaviours: `metadata-self-test`, `accessibility-tags-taught`.

## 1.0.167, 21 August 2026

- **The read-back worked, and it caught the thing it was built for.** The 19:03
  run reports 49 of 49 pictures carrying their descriptions, verified by
  reading the files, and says plainly: *none of them in the IPTC accessibility
  fields*. The version line explains why. ExifTool **11.79**, from 2019; the
  fields were added at 12.41.
- **And that is my fault from 1.0.165.** I made the build prefer a copy of
  ExifTool already on the machine over downloading one. Right in principle, and
  it is what the ffmpeg step does. But his copy was four years old, so
  preferring it is precisely what kept the accessibility fields missing — the
  optimisation defeated the fix it was sitting next to.
- **The build now asks how old a copy is before preferring it.** Anything below
  12.41 is passed over and a current one fetched. `installExifTool.cmd` does the
  same. Versions are compared as version numbers, not as decimals, because 11.9
  and 11.79 compare the wrong way round otherwise.
- If the copy in use is still too old, the build log says so in as many words
  rather than leaving it to be discovered in a run.
- `Announce.md` now describes the picture feature as it actually is, the
  paragraph saying metadata was not written yet having stopped being true.

## 1.0.166, 21 August 2026

- **It worked, and it reported more than it had done.** The 18:45 run wrote
  descriptions into 48 of 49 pictures and made `images.zip` — but the figure
  "48 carrying their descriptions inside them" was my code trusting an exit
  code, and the log shows what that missed:

      Warning: Tag 'XMP-iptcCore:AltTextAccessibility' is not defined
      Warning: Tag 'XMP-iptcCore:ExtDescrAccessibility' is not defined

- Those are WARNINGS. ExifTool still exits zero. So the two IPTC accessibility
  fields — the whole reason for choosing ExifTool — were quietly dropped from
  every one of the 48, and HomerScribe called each of them a success. It only
  came to light because one file ALSO had a real error, so its output was
  printed. The tag names are right; that copy of ExifTool simply predates them,
  and **which copy was in use had never been logged**.
- **The count is now read back out of the files.** After the writing, one
  ExifTool call over the whole folder asks what is actually in those pictures,
  and that is the number reported. Where it disagrees with the number of
  successful writes, both are logged and the read-back one is used. An exit
  code says a program finished; it does not say the work was done.
- **ExifTool's version is logged**, once, at the start. Nothing above could
  have been diagnosed without it.
- **`-m`, ignore minor errors.** The one refusal was a PNG with `IFD0 pointer
  references previous IFD0 directory` — damage already in the file and nothing
  to do with the description being added. That picture now gets its description.
- **Where the accessibility fields are not supported they are dropped, the
  write is retried without them, and the log says so plainly** — that ExifTool
  is too old, that the caption, title and comment fields are still written, and
  that `installExifTool.cmd` will fetch a current copy. Better a smaller true
  claim than a larger false one.
- New behaviours: `metadata-read-back`, `exiftool-version-logged`,
  `minor-errors-ignored`.

## 1.0.165, 21 August 2026

- **The ExifTool fetch was asking the wrong server**, and his build log caught
  it: every attempt came back `Not Found`.
- exiftool.org says so on its own front page, twice. The site is a *"temporary
  stop-gap"* because its host disabled it, and *"the download links now point
  to SourceForge"*. My script read the right FILE NAME off that page and then
  built the wrong BASE address. The archive lives at
  `sourceforge.net/projects/exiftool/files/`, and three places are now tried in
  turn. The version number is taken from `exiftool.org/ver.txt`, which is
  published for the purpose and steadier than a pattern over a page.
- **There are two kinds of `exiftool.exe`, and the test was wrong for one of
  them.** The packaged one is a small launcher needing an `exiftool_files`
  folder beside it. Oliver Betz also publishes a self-contained build of about
  eight megabytes that needs nothing beside it -- and that is the one Jamal
  already had. Testing for the folder would have made every build fetch again
  on a machine with a perfectly good copy. **The test is now that the program
  RUNS**, which is the only thing that actually matters.
- **A copy already on the machine is used before anything is downloaded**, the
  same courtesy the ffmpeg step has always paid: the build folder, then
  `C:\HomerScribe`, then the installed program folder. His copy would have been
  found on the first try.
- `installExifTool.cmd` carries all of the same corrections, and its message on
  failure now tells the truth about where to go and says that a self-contained
  build is equally welcome.

## 1.0.164, 21 August 2026

- **The picture feature is finished.** Phases two, three and four.
- **The description is written into the picture**, with ExifTool, which the
  build now fetches and the installer packages. The same words go into several
  places on purpose, because different software looks in different ones:
  `AltTextAccessibility` and `ExtDescrAccessibility`, the two fields the IPTC
  added in 2021 for exactly this; `dc:Description` and `Caption-Abstract`,
  which most photo software displays; and `XPTitle` and `XPComment`, which are
  what Windows Explorer shows and a screen reader reads out of a file's
  properties. That last pair is the one that matters at a Windows machine.
- Arguments go to ExifTool in a file rather than on a command line. A
  description holds quotation marks, ampersands and accented letters, and every
  one of those is a way for a command line to go wrong.
- **Two copies of every picture, both carrying the description.** The output
  folder holds them under their original names; `<archive>.zip` holds them
  under their new descriptive ones. Described once, metadata written once, then
  copied, so the two are identical apart from the name and there is no second
  pass to drift.
- **Every format accounted for.** PNG, JPEG, WebP and TIFF take the full set of
  fields. GIF takes a plain comment and nothing named. BMP has no metadata
  container at all. SVG is a drawing the model cannot be shown, and is now
  named as that in the log rather than passed over as though it were junk --
  its own title and desc elements would be the best home of any format here,
  and it is worth coming back for.
- **ExifTool is bundled at his instruction**, so `License.md` names it and
  points at its source. It is under the same terms as Perl -- the Artistic
  License or the GPL, the redistributor's choice. HomerScribe's own MIT terms
  are untouched: it runs ExifTool as a separate program and never links it.
  `installExifTool.cmd` remains for a machine where the bundled copy is
  missing.

### What Rentitle taught

He sent his own file-renaming program, which does this job from document
metadata. Six of its rules were missing here, and every one is the kind of
thing learnt from a file that broke something.

- **Substitute, do not delete.** A colon becomes " - ", an ampersand " and ", a
  bracket a parenthesis. Blanking them, as this was doing, turned "Jeannie &
  Jim" into "Jeannie Jim" and lost the word.
- **The illegal list is longer than Windows says.** Rentitle bans a long run of
  smart quotes, box-drawing characters, bullets and accented letters, with a
  comment recording that they stopped file-not-found errors in Python and in
  FileDir. Every one is LEGAL in a Windows file name. They break other things
  anyway.
- **Some titles are not titles.** Rentitle refuses "untitled", "none", "Title",
  "Presentation1". A vision model hands back "Image" and "Photo" just as
  readily, and a folder where every picture is called Image is worse than one
  full of IMG_4471. Such an answer is now refused and the name taken from the
  description instead.
- **A name of digits is not a name.**
- **A leading dot hides the file**; it becomes "Dot".
- **There is a limit to trying.** Rentitle stops after a thousand collisions.
- One difference worth recording: Rentitle pads its suffix to three digits
  always. He asked here for the fewest zeros that will do, so that is what this
  does. The newer instruction wins, but the two are not the same rule.

- New behaviours: `metadata-written`, `renamed-copies-archived`,
  `placeholder-names-refused`, `wider-illegal-letters`.

## 1.0.163, 21 August 2026

- **The names came right.** Twenty-four of twenty-four now read as phrases with
  spaces, none run together, none with underscores, the longest 76 characters
  and the middle one 27. "Elderly woman in a blue floral dress by a stone wall"
  where it used to say "OutdoorElderlyLady".
- **As few leading zeros as will do**, as he asked. The width is taken from the
  LARGEST clashing group in the archive: a pair gets `-1` and `-2` however many
  pictures there are, and a group of twelve gets `-01` to `-12`. Not from the
  number of pictures, since only names that clash ever sit together and only a
  clashing group has to sort. Not from each group separately either, so every
  numbered name in one folder has the same shape rather than `-1` sitting
  beside `-01`.
- **The original file name is now offered as context.** "Jeannie & Jim - Lake
  Tahoe.jpg" was named by somebody who was there, and that is better evidence
  than a model can take from the picture. What the camera wrote is stripped
  first -- `IMG-20230113-WA0000` says nothing and offering it only invites the
  model to invent a meaning for it.
- This does not loosen the rule against guessing at identities, and the prompt
  says why: a name written by a person is a statement, a face is only a
  resemblance. Use the name the file gives IF what you see fits; never a name a
  face suggests.
- **A truncated answer became a name.** One picture was reported as
  `{ name Winter Couple at a Snowy Forest , description A man and a woman
  stand` -- the model's JSON ran past its token budget, stopped mid-word, would
  not parse, and the fallback took the wreckage. The two fields are now pulled
  out of partial JSON by pattern, a name still carrying JSON is refused, and
  the budget is raised so an answer has room to close its own braces.
- **Twenty-five of forty-nine pictures were refused by Ollama** with
  `(400) Bad Request`, each within half a second -- too fast to be inference,
  so rejected on sight. Same archive, same prompt, same extension: `image.jpg`
  went through and `Phil2.jpg` did not, which points at something in the
  picture the decoder would not take. **Every picture now goes through ffmpeg
  into a plain PNG** of at most 1024 pixels on its longest side, in a settled
  colour space, before the model is shown it. That removes the whole class of
  problem and cuts what has to be encoded and sent. `--picture-width` changes
  the size, and each original's size is logged so a pattern would be visible.
- Two faults were found while testing rather than by reading. The file-name
  strip removed housekeeping words BEFORE turning underscores into spaces, and
  `_` is a word character, so `signature_proper_orientation` never matched
  `\bproper\b`. And `captionTest.py` was asserting that a mixed list of names
  sorts into the order it was made in, which is true only within one clashing
  group.
- `captionTest.py` is at nineteen checks.
- New behaviours: `fewest-leading-zeros`, `file-name-as-context`,
  `pictures-normalised`, `partial-answer-rescued`.

## 1.0.162, 21 August 2026

- **Picture names may now run to 79 characters**, after PEP 8's line length,
  and `--name-length` sets it.
- **But the length was never what was binding.** Measured on his own run of
  twenty-four pictures against a cap of sixty: the longest name was
  **twenty-four characters**, and **not one of the twenty-four held a space**.
  The model returned identifiers -- `OutdoorElderlyLady`, `Kenyan_ID_2024`,
  `Child_Bike_Ride_Sea_View`. Raising the cap alone would have changed nothing.
- The cause was my prompt. It asked for a phrase "fit to be a file name", and
  "file name" is what did it: the model heard "identifier" and answered in
  code. **The prompt now shows the shape rather than describing it** -- spaces,
  sentence case, no CamelCase, no underscores, no extension, the budget stated
  in characters, and three worked examples, two good and two bad.
- **And whatever comes back is repaired anyway.** `spacedOut` turns
  `WomanInWhiteTee` into "Woman in White Tee" and `Kenyan_ID_2024` into
  "Kenyan ID 2024". A word is lowered only when it is a small joining word and
  is not already in capitals, so `JamalMazruiAmazonPoster` keeps both names and
  `NIRA_Birth_Certificate` keeps its initials. The error it can make is leaving
  a word capitalised; it cannot destroy a name.
- **An extension the model invents is taken off.** One answer was
  `BananaSmile.png`, which would have been written out as
  `BananaSmile.png.jpg`.
- **It is now told never to guess at who anyone is.** One picture came back as
  `BezosWithFriends`. There was no note in that archive, so the model had
  nothing to go on and named a living person from a face. The rule was in the
  note branch only; it belongs in the prompt whether or not a note exists.
- All twenty-four of his real names are now a permanent check in
  `captionTest.py`, which is at sixteen checks.
- New behaviours: `names-as-phrases`, `names-repaired`,
  `no-guessed-identities`.

## 1.0.161, 21 August 2026

- **The license question settled and written down.** HomerScribe stays MIT.
  It links nothing: every other program is run separately and spoken to through
  a command line, a file, or a local web request, which the Free Software
  Foundation's own guidance treats as how separate programs talk. Putting them
  side by side in an installer is mere aggregation, which leaves each one's
  license to itself.
- `License.md` now says which program is packaged and which is fetched, what
  each is under, and what would change the answer -- linking a GPL or LGPL
  library into HomerScribe.exe, which it must never do.
- The ffmpeg choice is now stated as the deliberate act it is: the build takes
  BtbN's **LGPL** build, never a GPL one, because a GPL build would oblige
  anyone redistributing HomerScribe to supply ffmpeg's source too. The three
  LGPL duties -- name the license, say where the source is, allow the copy to
  be replaced -- are each met, and `License.md` says how.
- `ReadMe.md` gains a short License section near the end, with links rather
  than bare addresses.

## 1.0.160, 21 August 2026

- **A missing semicolon failed his build**, at the end of the run of
  concatenated strings added in 1.0.159:
  `HomerScribe.cs(7502,111): error CS1002: ; expected`. Fixed.
- **And `buildHomerScribe.cmd` had a byte order mark**, which it must not:
  cmd.exe reads the first line literally, so three stray bytes in front of
  `@echo off` are an error on line one. My own delivery put it there, by
  writing every file with the same encoding as the C# source. Batch files are
  now written without one.
- **`csCheck.py` is new**, and exists because neither fault could be seen by
  what I was checking. Braces, parentheses and brackets all balanced -- a
  missing statement terminator changes none of them, so the check was measuring
  the wrong thing. It takes no arguments, writes a log beside itself, and looks
  for: a statement that never ends; unbalanced braces; a byte order mark on a
  batch file; and a `rem` stranded inside a `^` continuation, which hands the
  word "rem" to the compiler as a file name.
- Writing it turned up the reason the fault slipped through. Blanking string
  literals to check the braces left the offending line reading as a bare `+`,
  which every check took for an unfinished continuation and skipped. A literal
  now becomes a single `S`, so the line reads `+ S` and the missing terminator
  is visible. **Tested both ways**: the check is silent on the corrected source
  and names line 7502 on the broken one.

## 1.0.159, 21 August 2026

- **Who the captions say was speaking around a moment is now given to the
  model**, with its limits stated in the same breath. He asked the obvious
  question and he was right to: captions carry times, so a name at 12:03 must
  say something about who is on screen at 12:03.
- It partly does. In an interview, a talk, a piece to camera or much television
  drama, the speaker is in shot a good deal of the time. My earlier answer, that
  captions do not tell you who is on screen, was too absolute.
- Three things weaken it, and the third belongs to this program alone.
  Narration is never in shot. Dialogue cuts to the listener's face as often as
  the speaker's, most of all on the line that matters. And **HomerScribe
  describes in the gaps BETWEEN speech, by design** -- so at the moment a
  description is made nobody is speaking at all, and the nearest speaker is on
  one side of the silence or the other. The correlation is weakest exactly
  where descriptions happen.
- What survives all three is still worth having: whoever spoke either side of
  the silence is almost certainly in the SCENE, if not in the frame. So it is
  offered as exactly that, and the model is told to use the name only if
  somebody in the picture matches how that person has been described to it.
- New behaviour: `speaker-near-the-moment`.

## 1.0.158, 21 August 2026

- **An archive can carry notes about its own pictures.** A file named after the
  archive -- `holidays.md` inside `holidays.zip` -- is sent with EVERY picture
  in it. A file named after one picture -- `image07.md` beside `image07.jpg` --
  is sent with that one only. Both, where both exist, and anything given by
  `--context-file` before them: most general first, most particular last, so
  the nearest note has the last word. A `.md` in the archive is counted as a
  note, not as something passed over.
- **The note is framed as a way of RECOGNISING people, not as a claim that they
  are present.** This matters more than it looks. A note saying who tends to
  appear and how to tell them apart is an invitation to write those names into
  every picture, because a model uses a name it has been given. The instruction
  is therefore explicit: use a name only where what is actually visible matches
  what the note describes; where you cannot tell, say what you can see and do
  not guess. That is the rule that took naming in film from a scatter of
  half-named strangers to something a listener could follow.
- **The captions' speakers are now gathered as a roster** and offered to the
  describing model as names this film uses. What this is NOT is a claim about
  who is on screen: a film cuts to the listener as often as the speaker, and a
  narrator is never in shot. Handing the model "Penelope is speaking" would
  produce a confident Penelope in frames she is not in.
- What a roster IS good for is knowing which names the film uses and how its
  makers spell them, so that a name reached for is a real one. It cannot attach
  a name to a face by itself -- the context file is where appearances live. The
  captions say who exists; the context file says what they look like; a name is
  used where the two meet. Roles that name nobody visible -- NARRATOR,
  AUDIENCE, ALL, CROWD, VOICEOVER -- are left out.
- **Measured on his own four runs, this yields nothing**: nought, nought,
  nought, and one, which was "Audience" and is filtered. Documentary and
  conference captions rarely name speakers. A drama with proper subtitles for
  the deaf and hard of hearing names them constantly, and that is where it will
  earn its place.
- New behaviours: `notes-inside-archives`, `caption-speaker-roster`.

## 1.0.157, 21 August 2026

- **A zip of pictures can now be a source**, on its own or by wildcard. Every
  image in the archive is described, and `descriptions.md` is written to a
  folder named after the archive. Nothing is renamed and no metadata is written
  yet: this is the first of four steps, and it exists so the QUALITY of the
  names can be judged before anything is built on them.
- Read from the archive: `.png`, `.jpg`, `.jpeg`, `.webp` and `.gif` go to the
  model as they are; `.bmp`, `.tif` and `.tiff` are turned into a PNG by ffmpeg
  first. Anything else in the archive is passed over, counted and named in the
  log, so nothing looks silently lost.
- **Every picture is reported on for metadata standing**, as asked: whether the
  format can carry the description, can carry only a plain comment, or has
  nowhere to put one at all. GIF is the middle case and BMP the last -- that is
  a fact about those formats, not something to be worked around.
- **The clash rule, and why every member of a group is numbered.** His
  convention is `-001`, so that alpha order is also the order the pictures came
  in. Numbering only the second and later members breaks that, because "-" is
  character 45 and "." is 46: `Sunset-002.jpg` sorts BEFORE `Sunset.jpg`, so the
  plain one lands last. Tested, not assumed. So every member of a clashing group
  is numbered from `-001`, and a name that occurs once is left plain. Three
  digits as asked, widened past 999 so that being right outlasts sorting.
- Alternatives weighed and not taken: a distinguishing detail instead of a
  number, which needs a second question per picture and can still clash; and the
  original file name in brackets, which traces back but is long where he asked
  for succinct. `descriptions.md` lists everything in archive order, so the
  mapping is kept without lengthening any name.
- Names are cut at a word and never mid-word, stripped of anything Windows,
  macOS or a zip objects to, and kept clear of the names Windows reserves.
- Transcribe audio does not apply to an archive, and says so rather than
  quietly doing nothing. A run over archives alone does not need Whisper.
- `captionTest.py` is at fifteen checks, four of them on the naming rules.
- New behaviours: `pictures-from-archives`, `clash-numbering-whole-group`,
  `metadata-standing-reported`.

## 1.0.156, 21 August 2026

- **The chain of alternatives stopped after one, and that was my fault.** In the
  14:46 run the usual way was refused with a 403, the web player was tried, and
  yt-dlp answered "Requested format is not available". My own guard read that as
  "the video is absent, asking differently will not help" and abandoned the
  remaining four ways. It is not that at all: it means that player was handed an
  empty menu, so nothing matched "best video plus best audio". It is a refusal
  wearing a different coat, and it is now treated as one.
- **The reason reported was the wrong one.** The results box said "Requested
  format is not available" for all four videos when the actual trouble was the
  403 that came first. The first failure is now what gets reported; a later
  attempt's complaint describes that attempt, not the problem.
- **The order is better.** The single combined stream is now tried second, being
  the cheapest thing that often works. Then the television, iOS and mobile web
  players, which ask for less and are commonly served; the web player last,
  since it is the one most often given an empty menu. A seventh try asks for any
  format at all, for the case where a player's menu holds something but not the
  usual pairing.
- **A browser's session is borrowed before giving up.** When every way is
  refused and no cookies were offered, HomerScribe now tries again with Edge's
  cookies, then Chrome's, then Firefox's. A signed-in session is often served
  what an anonymous one is not. Whichever works is named, so it can be set with
  `--browser-cookies` and the tries paid for once. `--browser-session no` turns
  it off.
- New behaviours: `browser-session-last-resort`, `first-reason-reported`.

## 1.0.155, 21 August 2026

- **The build keeps yt-dlp current, and the program stops mentioning it.**
- `buildHomerScribe.cmd` now runs `yt-dlp --update-to nightly` on every build.
  It is one quick call that says "up to date" and stops when there is nothing
  to do, and it never fails the build: no network, or a copy that cannot write
  to itself, and the build carries on with what is there. The version before
  and after goes in the build log. The yt-dlp that ships in the installer is
  therefore never older than the build.
- **Nightly, not stable.** yt-dlp's own README calls nightly "the recommended
  channel for regular users", and asks that anyone hitting a problem on stable
  move to nightly before reporting it. YouTube changes what it serves, at times
  deliberately to break downloaders, and the fix for something that broke this
  week is on nightly this week. `--update-channel stable` or `master` if wanted.
- **No warning at run time.** The version and its age are recorded in the log as
  a plain fact and nothing is said about them. Keeping the tool current is the
  build's job, and telling somebody at run time about a thing they cannot act on
  there and then is noise.
- **yt-dlp's own nagging is silenced too.** It prints a complaint on the console
  when it is more than ninety days old, which is fair of it and the wrong place
  here, where the console is carrying progress a listener is following. Every
  call now passes `--no-update`, which is what that flag is documented for.
- The update on refusal, added in 1.0.154, now goes to the same channel.
- New behaviours: `build-updates-yt-dlp`, `no-update-nagging`.

## 1.0.154, 21 August 2026

- **The extra information call was not the cause, and I was wrong to suspect
  it.** The 13:59, 14:02 and 14:05 runs each show exactly one yt-dlp command
  before the download, as in 1.0.146, and the refusal is unchanged. The
  reordering in 1.0.153 was right on its own merits and fixed nothing. At the
  point of download HomerScribe now does what 1.0.146 did, character for
  character, and gets a different answer, so the difference is not in the
  program.
- **A refused film is now asked for a different way, and then another.** The
  usual request first, exactly as before; then the web player, the Safari web
  player, the television player, the iOS player, and finally a single combined
  stream instead of the best picture and best sound taken apart. It stops at
  the first that is served and writes down which one that was.
- **Only a refusal is retried.** A video that is private, deleted or
  geo-blocked is just as absent from every player, so six tries would learn
  nothing; the reason is read and the attempt abandoned.
- **yt-dlp is updated when nothing else works**, once per session, and the
  usual request tried again. Its age is now worked out from its version at
  startup and said in the log, with a word of warning past thirty days. At
  2026.07.04 the copy in use is seven weeks old against a service that changes
  most weeks.
- **`--player-client`** pins one once it is known -- web, web_safari, tv, ios or
  mweb -- so the tries are paid for once rather than on every video.
  **`--update-tools`** turns the automatic update off.
- New behaviours: `other-ways-to-fetch`, `update-tools-on-refusal`,
  `tool-age-reported`.

## 1.0.153, 21 August 2026

- **A third request was being made of YouTube before every download, and it was
  not there before the caption work.** 1.0.147 added a `--dump-single-json`
  call to learn the title and the caption tracks, and put it in front of
  everything. So each video was extracted three times where 1.0.146 extracted
  it twice, and only then downloaded.
- The last version known to fetch a film successfully is 1.0.146, and it is
  also the last version that did not make that call. That is a correlation, not
  a demonstration, and I had been asserting the opposite: that the download
  command was character for character unchanged and therefore nothing here
  could be the cause. The command was unchanged. The COMMAND was not the only
  thing that changed, and I should have looked at the sequence before saying so.
- **The question is now asked only when the answer is needed.** Wanting a
  transcript alone, it decides whether the film is needed at all, so it comes
  first. Wanting the film, it comes after the film is safely down. On that path
  the download is once again preceded by exactly one request, as in 1.0.146.
- `ytCheck.py` now settles it rather than guessing. Its first three trials run
  the SAME download after nothing, after one information call, and after two,
  and it says in as many words which of those three it was.

## 1.0.152, 21 August 2026

- **Half is better than nothing.** When the film cannot be fetched but a
  transcript was also asked for, the captions are now taken anyway and
  `transcribed.md` is written. The results box says plainly that nothing was
  described and why.
- This matters because the two things come down different roads. Every
  information call and every caption file has succeeded on this machine
  throughout; only the signed media stream is being refused. A run that wanted
  both used to report that nothing at all could be done, when half of it was
  sitting there for the asking.
- **The 13:44 run was not a regression.** Describe video was ticked as well as
  Transcribe audio, so the film really was needed, and the fetch met the same
  403 that has stood since 12:27. The log shows `Setting describe = yes` and
  the caption tracks being read correctly for all four sources before the
  download was even attempted.

## 1.0.151, 21 August 2026

- **"e.g." is no longer the end of a sentence.** The first run of 1.0.150 put
  this at the top of the TED transcript: *"For more information on using TED
  for commercial purposes (e.g."* The sentence splitter broke at the full stop
  inside the abbreviation, and the fragment that was left passed every test
  because it ended with a full stop. A piece broken that way is now joined back
  on to the one before it, and a run of common abbreviations is recognised.
- The trim-back that guarantees a blurb ends on a full stop **would have hung**
  on exactly that input: the offending stop was the last character, so
  searching from the end found it again and set the same length for ever. It
  now searches from before the last character, so every pass is strictly
  shorter. Found by porting the function to Python and running it against TED's
  real description, which is also now a check in `captionTest.py`.
- A blurb sentence with a bracket opened and never closed is dropped too.
- **Each film's own log now begins at the fetch.** It began inside `runOne`,
  which left out how the film was obtained -- and on the captions-only route
  left out which caption track was chosen and why, which was most of what there
  was to say. The per-film logs from the 13:14 run were four lines long.
- `captionTest.py` is at eleven checks, two of them on the publisher's blurb.

## 1.0.150, 21 August 2026

- **The three documents are rewritten to be read.** A log is for a machine.
  `described.md`, `transcribed.md` and `scribed.md` are for a person, and a
  person listening rather than skimming, which is a harder audience than
  either. The rules below are the W3C Web Accessibility Initiative's, from its
  guidance on transcripts.
- **No times in the body.** The Initiative puts it plainly: timestamps are
  usually unnecessary clutter, need not be as granular as the captions, and
  need no end times. So a time now appears only in a section heading, where a
  screen reader can move between them, and only on a film over twenty minutes.
  Below that there is no time anywhere. A section heading reads "From 10
  minutes" rather than "0:10:00 to 0:20:00", because that is a place in the
  film rather than four numbers.
- **Paragraphs, not bullets.** A hundred and thirty list items is a hundred and
  thirty announcements of the word "bullet" before anything is said. And a
  passage now ends where a SENTENCE ends: it may run on half as long again
  while it waits for a full stop, because a paragraph broken mid-clause reads
  as though the speaker was cut off. In a timed list that was invisible.
- **`scribed.md` is a descriptive transcript**, and says so. That is the term
  of art, and the Initiative names this document as the one a reader who is
  both Deaf and blind needs -- adding that where you have one, you do not need
  a separate basic transcript. It was being written as though it were an extra.
- **Only what was added is labelled.** A description carries a bold
  "Description." at the front. Nothing else does, so an unlabelled paragraph is
  the film's own words. The Initiative asks only that added material be
  distinguishable, and labelling both sides would double the reading to say the
  same thing.
- **The heading is four or five lines** and no more, since every word of it is
  read before the reader reaches a single word of the film. On the one-minute
  video the old heading was 61 percent of the whole document. Measured on the
  four-minute W3C introduction: 1,788 bytes of heading became 678, and the
  document as a whole 4,913 bytes became 3,737.
- **A note only where there is something to explain.** Telling a reader what a
  speaker's name looks like, in a film where nobody is named, is a sentence
  spent on nothing.
- **The publisher's blurb is tidied or dropped.** Sentences carrying a web
  address go, and so do appeals to subscribe and permission notices; a screen
  reader reads an address one character at a time. What is left never ends on a
  fragment. Too little left and the whole section is dropped.
- **A speaker named in brackets is now recognised.** TED writes "(Audience)
  Good." with no colon, and it was being run into the sentence before. What
  keeps this apart from a sound is what follows: a speaker is followed by a
  sentence starting with a capital, where "(Music) plays softly" carries on in
  lower case.
- New behaviours: `documents-as-prose`, `times-only-in-headings`,
  `descriptions-labelled`, `publisher-blurb-tidied`.

## 1.0.149, 21 August 2026

- **A transcript no longer downloads the film.** When only Transcribe audio is
  ticked and the video carries its own English captions, the captions are
  fetched with `--skip-download` and the film is never touched. There is
  nothing in a transcript that needs it: the captions say what is said, and
  nothing is being described.
- This is the right thing to do on its own merits -- about thirty kilobytes
  instead of a hundred megabytes, and seconds instead of minutes -- and it also
  steps straight past what has been failing. In the 12:17 run every caption
  file came down whole while every media request was refused with a 403.
  Captions are reachable on that machine; the media stream is not.
- Taken from Jamal's own youtube-dl scripts, which have been doing exactly this
  for years: `--write-sub --write-auto-sub --skip-download`. Two more of their
  settings came across as well.
  - **`--convert-subs vtt`.** YouTube also serves srv3 and ttml, which the
    caption reader cannot parse, and asking for "vtt/srt/best" would fall back
    to one of those quite happily. Now whatever arrives is turned into VTT.
  - **`--sleep-requests 1`**, a pause between requests, which is what a service
    that counts them is asking for.
- With Describe video ticked the film is still fetched, because there is
  something to look at. And a video with no English captions is still fetched
  and listened to. The fallback is automatic and the log says which happened.
- The running time in the heading now comes from the page rather than from the
  file, since on this route there is no file to ask.
- New behaviours: `captions-without-the-film`, `captions-converted-to-vtt`,
  `sleep-between-requests`.

## 1.0.148, 21 August 2026

- **Fixes the caption fetch, which failed every source in the first run of
  1.0.147.** Nothing was downloaded and all four videos were refused.
- The cause was one flag. `--sub-langs "en.*"` is a REGULAR EXPRESSION in
  yt-dlp, not a language, and it matched every machine translation OUT of
  English. On the Ken Robinson talk that was sixty-six tracks -- Albanian,
  Arabic, Armenian, Azerbaijani and on through Vietnamese -- each a separate
  request. YouTube answered 429 Too Many Requests part way through the second
  video, and the two after it were refused with 403 before their video data
  could be fetched.
- **The language rule is now an exact list.** Anything beginning "en-" used to
  count as English, which is precisely how a translation is named. "en-ca" and
  "en-in" are deliberately left out although they read like Canadian and Indian
  English: YouTube uses them for Catalan and Indonesian. "-hi" is no longer
  trimmed as a hearing-impaired marker either, because "en-hi" is Hindi.
- **The track is now chosen, not guessed.** The `--dump-single-json` call
  already made for the title also returns "subtitles", the tracks a person
  uploaded, and "automatic_captions", the machine ones and every translation.
  The English tracks are picked out of that by name and **exactly one** is
  asked for, preferring a person's over a machine's on YouTube's own say-so.
  Replayed against the four real track lists from the failed run: 23, 3, 66 and
  3 tracks listed, 2, 3, 3 and 3 of them English, one request each.
- **A caption failure can no longer kill the film.** The subtitle flags used to
  ride on the download command, so yt-dlp giving up on a subtitle abandoned the
  video too. Captions are now fetched by a second command after the film is
  safely down. If it fails, that is logged and Whisper writes the transcript,
  which is what happened before captions existed and is a perfectly good answer.
- **A video's details no longer leak into the next one's documents.** The
  title, publisher and description were never cleared between sources, so a
  local file following a downloaded one would head its documents with the
  downloaded one's title. `Developer.md` said this was handled. It said so
  wrongly, and was only found by going to check.
- New behaviours in the fingerprint: `captions-one-track`,
  `captions-track-from-page`, `captions-fetched-apart`.

## 1.0.147, 21 August 2026

- **A film's own captions are now the transcript.** Somebody wrote them, so the
  words are right. They say who is speaking. And they write down what can be
  heard but not spoken -- a door slamming, music starting -- which no transcript
  of speech has ever held. Whisper cannot do any of those three things.
- They are taken from a subtitle track inside the film, or from a caption file
  sitting beside it, whichever is there. English only: every track is asked its
  language, and anything else is left to Whisper. A film with one track that does
  not say what language it is in is taken as English, and the document says so.
- **A downloaded video now brings its captions with it.** yt-dlp is asked for
  them whenever a transcript is wanted.
- **One transcript, not two.** `transcribed.md` is still the only transcript,
  with a line at the top saying where the words came from. Two files of nearly
  the same words would only make the reader choose between them.
- **Captions decide the words, never where a description goes.** This is the part
  that matters most and is easiest to get wrong. A caption is put on screen early
  and taken away late, so that a reader can finish it. That is not when the words
  are said. Using those timings to place descriptions would shrink and shift the
  quiet the placement rule measures, and the damage would not show in any count
  -- only in a listener losing dialogue. So a run that describes still listens to
  the film with Whisper, and uses what it hears for placement alone.
- `scribed.md`, the document for a reader who can neither see nor hear, gains
  what it was always missing: a **Sound** entry for something heard but not
  spoken, and a speaker's name against the words. That was the whole reason for
  wanting captions.
- **The rolling repeat, got wrong and then got right.** YouTube's automatic
  captions scroll up the screen, and each cue holds the one before it with more
  added. The first version compared each cue with the one before and asked
  whether the OLD held the NEW. A rolling caption grows the other way, so nothing
  matched and every line was written down twice. Tested before it was believed,
  found within the minute, and replaced: the words already written down are
  remembered, and only what has not been seen yet is added.
- **A sound stays its own entry.** Found the same way, before any film was run:
  a talk writes "(Laughter)" and "(Applause)" as cues of their own, a second or
  two after the line before, and both merge rules swallowed them into the
  surrounding prose. `scribed.md` would then have labelled the whole passage
  Spoken. A cue that is nothing but a sound is now never merged, in either
  direction.
- **Whisper is no longer required in order to transcribe**, so long as captions
  are left on. A film that turns out to have none is refused by name, with the
  reason.
- Every document -- `described.md`, `transcribed.md` and `scribed.md` -- now
  opens with what the video says about itself: its title, who published it,
  where it came from, and what the publisher wrote about it. yt-dlp was already
  being asked for all of that; it was being used for the model's context and then
  thrown away. The web address is written as a link with the title as its text,
  never bare, because a screen reader reads a bare address one character at a
  time.
- New setting `--captions`, on by default. Turn it off to make Whisper do the
  work in every case, which is how to compare the two.
- New behaviours in the build fingerprint: `captions-preferred`,
  `captions-english-only`, `captions-keep-sound`, `captions-never-place`,
  `video-heading`. **Check the `This build does:` line before analysing any log
  about captions.**

## 1.0.146, 17 August 2026

- Built without a History entry. Recorded here so that a version in a log can be
  looked up.

## 1.0.145, 17 August 2026

- The build now asks the repository which versions are already released, and
  steps over any number it finds. A working copy whose `version.txt` has fallen
  behind can no longer mint a number that is already spent.
- That is what had happened. `version.txt` held 1.0.143 while 1.0.144 was
  released and committed, so the build stamped a second 1.0.144 and tagRelease
  refused it, publishing nothing. The refusal was right; the number should never
  have been offered, and the wasted work was the whole build.
- One `git ls-remote --tags` is the only network call the build makes. If it
  fails the plain increment is used, so a machine with no network still builds,
  and tagRelease stays the check of last resort it has always been.
- `Developer.md` and `ReadMe.md` say so too, since a versioning rule that is
  only in the script is a rule nobody reads.

## 1.0.144, 13 August 2026

- A recording is no longer described. Asked to describe twenty-nine mp3 files,
  HomerScribe produced one description for each: the model inventing a sentence
  from a blank montage, written out as though it meant something. There is no
  picture in a recording and nothing to describe.
  It now asks ffmpeg whether there is a picture at all. Cover art does not count
  -- it is carried as a video stream of one still frame and is not something to
  describe. With Transcribe audio also ticked it transcribes and says so; with
  only Describe ticked it refuses the file and says to tick Transcribe audio.
- Tested against ffmpeg's own output for three cases: an mp3, an mp3 carrying
  cover art, and a film.

## 1.0.143, 13 August 2026

- The repository is now what it should be: thirty files, the program and its
  documents and nothing else. Every note, script, film and build product is off
  GitHub and still on the disk.
- tidyRepo now names the ten largest objects in the repository and the path each
  came from, saying plainly when one belongs to no commit at all and is merely
  waiting to be collected. I had said I could not tell what was holding 516 MB;
  that was a failure of effort rather than of possibility, since git can simply
  be asked. Several rounds of guessing were the wrong response to a question with
  a one-command answer.

## 1.0.143, 13 August 2026

- Added finishRepo.py, which settles the size question from evidence rather than
  guesswork and needs no arguments. It reads the pack index for the largest
  objects actually stored, names the file each belongs to from the object list,
  and says how much is reachable and how much is not. Anything large that does
  not belong is then removed and the pack rebuilt.
- The reason nothing had worked: repack without -A keeps unreachable objects
  inside the pack, so the pack never shrinks. With -A they are written out loose
  and the prune that follows deletes them. Measured on a test repository: 38.16
  MiB to 2.35 KiB, and on another with a file deleted in a later commit, 57.24
  MiB to 1.69 KiB.
- It also removes every leftover folder from the earlier runs.

## 1.0.142, 13 August 2026

- Files purged by one run came back in the next. The commit purgeRepo takes
  before rewriting used "git add -A", which stages UNTRACKED files as well --
  and files purged earlier were sitting on disk untracked, exactly as intended.
  It hoovered them straight back in: committed, rewritten past, and pushed.
  Three returned that way. It uses "git add -u" now, which stages changes to
  files git already tracks and nothing else, which is all that was ever wanted.
- .gitignore covers context/video* and video.htm, which the replacement I sent
  had dropped without noticing: purgeRepo had added them itself in an earlier
  run and my file overwrote that.
- Tested by running the whole thing twice over. What is purged in the first run
  is still purged after the second, and every file is still on disk.

## 1.0.141, 13 August 2026

- The Python scripts and the Odyssey context files are taken out of the
  repository. The build script uses no Python at all -- checked rather than
  assumed -- so none of it belongs there, and one film's context is not part of
  the program. The installer no longer claims to ship measure.py and
  placement_test.py. All of them stay in the working folder.
- Two faults in purgeRepo, both about files in a subfolder: the copy set aside
  used only the base name, so two files of the same name in different folders
  would have become one; and the restore never recreated the folder, which git
  removes once it holds nothing tracked, so the move failed and the exception
  was swallowed. Found by testing rather than reading: context/The_Odyssey.md
  and prototype/describeMovie.py disappeared from the disk.
- When the collection cannot free the space, a fresh copy of the repository is
  now fetched and its .git swapped in rather than the user being told to do it.
  Only the hidden folder is replaced; every file stays where it is, and the old
  one is kept aside until the next run.

## 1.0.140, 13 August 2026

Two things tidyRepo did not finish, both found from its own log.

- The space was not reclaimed: the pack was 516 MB before the collection and
  516 MB after. git gc leaves an unreachable object where it is when it is
  already inside a pack, so nothing moved. It now clears everything that can
  still be holding them -- the backups filter-branch keeps, the reflog including
  unreachable entries, the stash, stale remote-tracking refs -- and then repacks
  from scratch rather than collecting. Measured on a test repository: 7.9 MB of
  git folder down to 192 KB.
- The backup folders could not be deleted: git marks its object files read-only
  and Windows refuses. The flag is now cleared and the deletion retried, which
  is the standard remedy.
- If the size still does not move, it says so and gives the one certain cure, a
  fresh clone, rather than reporting success.

## 1.0.139, 13 August 2026

- Added tidyRepo.py, which finishes the repository clean-up in one run and takes
  no arguments: it checks the set-aside files came back, takes the testing
  material and build products out of the history, updates the remote to the
  address GitHub asks for, reclaims the space, and removes the backup folders
  once it can see nothing is missing.
- Two faults in purgeRepo.py found by testing it, both of which had let files
  come back after being removed. filter-branch rewrites the commits and leaves
  the INDEX alone, so the purged files were still staged and the next commit put
  them straight back; the index is now brought into line, mixed rather than
  hard, so nothing leaves the disk. And purgeRepo never accepted paths on the
  command line at all, though I had said it did -- it silently used its own list
  instead, which is why a first attempt removed the wrong files.
- Verified end to end: source alone in the repository and on the remote, every
  other file still on disk.

## 1.0.138, 13 August 2026

- purgeRepo.py failed with "You have unstaged changes", and the cause was itself:
  its own log is written as it runs, and the repository was tracking it, so the
  tree was dirtied by the act of recording what was being done. The check passed,
  the log was written, and the rewrite then refused.
  The log is now untracked before anything looks at the tree, along with
  fixRepo.log and buildHomerScribe.log, and *.log is added to .gitignore.
- Genuine uncommitted changes are now found before the folder is copied rather
  than after, and committed rather than discarded -- they are the user's changes
  and a commit is the one operation here that cannot lose anything. Line endings
  are the usual reason: git calls a file changed when it would write it back
  differently.
- Tested on two repositories, one with the log tracked and one with a genuinely
  modified file: both rewritten, the executables gone from local history and from
  the remote, the source and the modified file intact, and everything still on
  disk.

## 1.0.137, 13 August 2026

- Added purgeRepo.py, which removes a file from every commit rather than merely
  from the future. It uses git filter-repo when installed and git filter-branch
  otherwise, the latter being part of git and needing nothing fetched.
- Two things it got wrong and now does not, both found by testing rather than
  reasoning. Rewriting the history checks the working tree out again, and a file
  that is in no commit any more is deleted from disk along with it -- which for a
  5.7 GB video would be unforgivable. And setting the files aside by MOVING them
  leaves the tree dirty, which makes the rewrite refuse to run; they are copied
  instead and the originals put back afterwards.
- Verified against a repository built to match: the installer stayed on disk
  byte for byte, it and the other executables left every commit and the remote,
  and the source history and tags were untouched.

## 1.0.137, 13 August 2026

- purgeRepo.py now also offers to remove the three stray *_HomerScribe.exe files
  and the describer's guide, which are in the history and were not on its list.
  A file that turns out not to be in the history is reported and skipped, so
  naming one harmlessly costs nothing.
- Tested end to end against a repository with two executables committed and
  pushed: both gone from the local history and from the remote, the source
  changes intact, both files still on disk, and a copy of the folder taken
  first. It used git filter-branch, filter-repo not being installed, which is
  the fallback working as intended.

## 1.0.136, 13 August 2026

- fixRepo.py used 50 MB as the size that blocks a push. That is GitHub's warning;
  the limit that blocks is 100. So an 87 MB installer looked like the cause, and
  since it was already in the remote's history the script stopped over a file
  that was never the problem -- the 5.7 GB video was.
  It now asks two separate questions: what is over GitHub's limit and blocked
  the push, and what should not be tracked at all whatever its size. Only the
  first, when already in the remote's history, is a reason to stop; a merely
  unwanted file is untracked from here on and its old copies stay where they are,
  costing space and blocking nothing.
- Where it must stop, it now gives the git filter-repo command needed and says
  to copy the folder first.
- A new .gitignore covering build products, fetched tools, media, run output and
  the maintainer's own drafts and notes. Checked against every file in the
  repository as it stands: nothing that belongs is excluded and nothing that
  does not is kept.

## 1.0.135, 12 August 2026

- Added fixRepo.py, for a push rejected because a file is too large. It rewinds
  to the remote's last commit keeping every file on disk, drops the large files
  from the index, adds patterns to .gitignore, commits and pushes. Nothing is
  deleted from disk.
  Tested against four repositories built to match: one with a large file in an
  unpushed commit, which it repaired; one where the large file was already on the
  remote, which it correctly refused; one with nothing to push; and a folder that
  is not a repository.

## 1.0.134, 12 August 2026

From counting how often a film's protagonist was named.

- On a two and three quarter hour film he was named in 117 descriptions of 341,
  and an unnamed "bearded man" appeared in 37 more, spread evenly throughout --
  not absent, INCONSISTENT. The model was following an instruction to name only
  where the picture made it plain, and its confidence varied from one frame to
  the next.
  That is worse for a listener than either extreme. "A bearded man strides across
  the deck" at 26 minutes and "Odysseus looks out to sea" at 27 give no way of
  knowing they are the same man; consistently wrong would at least be followable.
- The prompt used to say "names you have already used, so keep using them",
  which is a list with nothing to attach to. It now says to use a name again
  whenever somebody matches how that person was described, and explains why: a
  listener cannot tell that "a bearded man" and a name are the same person.
- Context.md and the Odyssey context file both changed to tie a name to
  something visible and require its use every time, rather than leaving the model
  to judge when it is sure. That turns recognising a person, which it cannot do,
  into matching an attribute, which it can.

## 1.0.133, 12 August 2026

- Context.md was written entirely from one narrative film and gave advice that
  is wrong elsewhere. Two corrections.
  The naming rule said to name the two or three people the picture makes plain.
  That holds for a documentary, where one person addresses the camera, and fails
  for drama: a model cannot tell a bearded man of forty from a bearded man of
  twenty-five, and half a film will carry the wrong name. A name is now advised
  only where you can say what makes that person distinguishable in a still frame
  -- the test being whether a stranger could pick them out from what you wrote.
  Added what different material needs: documentary, science and technical, drama,
  silent film, lecture, and the case of knowing nothing about it. The science
  entry is the one most likely to be missed -- saying that labels and captions
  appear and should be read out, since on such material the words on screen are
  the content and the narration says "as you can see here".
- The template no longer assumes a cast, and says which parts to leave out.

## 1.0.132, 12 August 2026

Three symptoms, one cause: the window was never given the chance to redraw.

- A model call blocks this thread for ten seconds or more, and nothing pumped the
  message queue meanwhile. So Windows decided the program had stopped, which is
  the "Not responding" a screen reader reports; the window never repainted; and
  a screen reader coming back to it read the title it had last been told about,
  which is why Alt+Tabbing back two hours into a run said "Starting".
  The model call now goes to a thread and this one pumps until it returns.
- The queue is also pumped immediately after every status update, as 2htm does.
  Setting the text is not enough on its own: until the queue is pumped, Windows
  has not repainted the window or told anybody the title changed.
- Measured from that run, and NOT explained by these changes: 32.5 seconds a
  description against 10.3 the run before, with the same model, picture size,
  frames and detail. That is still to be accounted for.

## 1.0.131, 12 August 2026

- When there is a dialog, the console now carries the same messages the dialog
  gave, one to a line, in the order they were spoken. It carried the log stream
  instead: command lines, exit codes and paths, which are useful when there is
  no dialog and unreadable when there is. Those are in the log, where somebody
  looking for them will go.
- Lines are written whether or not the message was spoken aloud, since the
  console is a record to be read back rather than an interruption. Errors still
  appear in both, and --verbose puts everything back.
- Running from the command line with no dialog is unchanged.

## 1.0.130, 12 August 2026

- Fixed a build failure I introduced with the timed context sections. The code
  that builds a prompt asked which section covers this moment, but had never been
  told which moment it was: nothing in a prompt had depended on the time before.
  The time is now passed to it, through describeImage and its five callers.
  It failed to compile rather than doing something wrong, which is the good
  outcome, but it should not have reached you.

## 1.0.129, 12 August 2026

- The context sections now carry setting and who is present, and nothing else.
  They had carried the "key visual information" bullets from the guide they came
  from, which are descriptions: given one, a model produces it whether or not it
  is in the frame, and the output becomes a paraphrase of the file rather than an
  account of the picture. 44 sections, about twenty words each.
- The prompt now says outright what a section is for: where the film is, not
  what is in this picture; use it for the right words and names, describe only
  what can be seen.
- The log measures whether that held. Each move into a new section is logged, and
  a MEASURE line at the end of each film reports how many descriptions were given
  a section and how many repeat half or more of their words from it -- which
  would mean reciting rather than describing.
- Context.md leads with the rule this comes down to: a context file supplies
  nouns and names, never observations. The test given is whether a sentence could
  be false of the picture and still be in the file.

## 1.0.128, 12 August 2026

- context/video-sequences.md: all 50 sequences of a describer's guide for The
  Odyssey turned into timed sections, each about fifty words of setting, who is
  present, and what is seen. Three carry times taken from the film's own
  transcript; the rest are marked TIME and are ignored until filled in, so the
  file works at any stage of being completed.
- Context.md gained a section on doing this: what transfers from a guide written
  for a person and what does not. Reasoning, reliability ratings and production
  facts all go -- the last would be read aloud as though they were in the
  picture -- and anything phrased as advice rather than fact goes with them.

## 1.0.127, 12 August 2026

- A context file may now be divided by time. A heading beginning with a time --
  "## 41:00 The Cyclops's island" -- marks a section sent only while the film is
  inside it; everything before the first such heading is sent with every
  description.
  I had said a scene-by-scene guide was unusable because a moment could not be
  matched to a scene. That was wrong: nothing had been built to match them. The
  length objection goes with it, since a forty-section guide now costs no more
  per description than a four-section one.

## 1.0.126, 12 August 2026

- Added Context.md: how to write a context file for this arrangement, which is
  not how one would brief a person. The governing fact is that the whole file
  goes into EVERY prompt, so it must be short -- under about 250 words -- and
  vocabulary helps far more than plot, since a vision model shown a bronze age
  warship says "a boat" until it is told what world it is looking at.
- Added context/video.md for Nolan's The Odyssey, 246 words, distilled from a
  1,043 line guide written for a human describer. What survived: the period and
  its vocabulary, the three names the picture makes plain and an instruction to
  describe everyone else by sight, the rule about disguises, the rule about the
  supernatural. The reasoning was dropped -- a person needs to know why, the
  model needs only the instruction.

## 1.0.125, 12 August 2026

From a run where the film was moved or deleted while it was being described.

- A source that disappears mid-run now stops that film at once, says so, and is
  named in the results as an error. It used to fail on every moment in turn --
  639 times in 47 seconds, silently -- and then report "1 descriptions" as
  though that were a result.
- Frames that cannot be read twelve times in a row also stop the film, since
  that is the file or the disk rather than any particular moment. One failure
  can happen for ordinary reasons; a dozen cannot.
- Nothing is written from a film that fell over. An empty described film left in
  a results folder looks finished, and the next run would skip it as already
  done.
- The new MEASURE lines earned their place immediately: "room available, middle
  0s" is what identified this as a missing file rather than a fault in the
  placement, which was working correctly throughout.

## 1.0.124, 12 August 2026

- Dropped two opening messages that told the user nothing they could act on:
  looking for the programs, and asking Ollama for its models. Both were under
  the hood.
- The first thing said about the work is now how many files there are, which one
  this is, and where its results go: "Processing 1 file, video.mkv" or
  "Processing 3 of 9 files, The Africans - Episode 4.mp4". The results folder is
  named only when it is not simply the file name without its extension, since
  saying that would be repetition.

## 1.0.123, 12 August 2026

- The first eight messages of a run are spoken at once rather than collected.
  Messages are grouped so that descriptions arriving every few seconds do not
  interrupt constantly, and a message waits up to twenty seconds for company.
  That is right in the middle of a film and wrong at the beginning, where they
  are rare, each is informative, and somebody is waiting to hear the program is
  alive: "Starting" was spoken and everything after it waited twenty seconds.
  After the first few, grouping resumes exactly as before.
- A message that was WITHHELD no longer counts as having been said. It reset the
  clock that decides whether the next one waits, so a message nobody heard could
  silence the one after it for twenty seconds.
- The rule that keeps HomerScribe quiet when it is not the window in front is
  unchanged, as is everything that decides it.

## 1.0.122, 12 August 2026

- A description was allowed its gap PLUS up to 3.5 seconds at --detail rich.
  That allowance was written when a moment sat in whatever silence happened to
  exist and a little overrun was the price of a whole sentence. Under the current
  design it is wrong: a moment is placed in measured room and given the whole of
  it, with a margin already kept at each end, so a licensed overrun is licensed
  talking over the dialogue. It is nought now, whatever the detail.
  This is also why the rule meant to drop an overrunning description had never
  fired in two versions: descriptions were not exceeding their allowance, they
  were using it. Eleven overlapped speech on the last run and not one exceeded
  what it was allowed.
- Two MEASURE lines are written at the end of each film: words and seconds per
  description against the room available, how many still judge rather than
  observe, how many name somebody, and how often the film's memory was
  rewritten. One block that answers the questions that have needed a whole log
  read to answer.

## 1.0.121, 12 August 2026

- Naming the session log for its session left the old un-timestamped
  HomerScribe.log sitting beside it, stale. It is the obvious name to reach for,
  so it was reached for, and a fresh run was compared against a two day old one.
  An older log of that name is now moved aside as HomerScribe-superseded.log the
  first time a timestamped one is written, so the only HomerScribe.log left in a
  results folder is the per-film one, which is current by construction.
- The results box now ends with the path of this run's log, so the right file is
  named rather than guessed at.

## 1.0.120, 11 August 2026

- Each finished film now gets its own log, in its own results folder, holding
  only the entries that belong to it. Somebody looking at one film's results
  should not have to search a whole session for the part about it.
- The session log is named for when the session began --
  HomerScribe-20260811-134216.log -- so a later run never erases an earlier one,
  and it is opened for appending rather than rewriting. It was already flushed
  to disk every second, so it stays current while a run is going on.

## 1.0.119, 11 August 2026

- Removed the batch look-ahead added in the previous version. Films are worked
  on one at a time again.
- Within one film, the ffmpeg work for the next moment is now done while the
  model works on this one: cutting and tiling the frames, and reducing them to a
  thumbprint. That work depends only on the film and a timestamp, so doing it
  early alters nothing the model is shown -- the same frames, the same picture,
  the same prompt. It is the only saving inside a single film that costs
  nothing.
  The ready file is renamed into place rather than juggling two path variables
  through a loop with many exits, and the run waits for the thread before
  finishing so nothing is left writing.

## 1.0.118, 11 August 2026

- On a batch, the next film is transcribed while the current one is described.
  The two use different hardware, so neither waits for the other: about 1.6 times
  quicker per film, 120 minutes becoming roughly 75.
  It was chosen over the alternatives for what it does NOT touch. The background
  work writes one file -- the next film's transcript, in that film's own folder
  -- and shares nothing else. When that film's turn comes the ordinary path finds
  it, exactly as it finds the transcript of an interrupted run. Not one line of
  the describing path changed. If it fails or is unfinished, that film is
  transcribed as usual.
- It is started only once the current film's descriptions begin, by which time
  that film's own transcript is made, so two transcriptions never contend for the
  processor.

## 1.0.117, 11 August 2026

- The build no longer crawls when it fetches ffmpeg. PowerShell's
  Invoke-WebRequest draws a progress meter unless told not to, and for a hundred
  megabyte file that meter costs far more than the transfer: tens of times
  slower, and it is what prints about writing a request stream. Every download
  now sets $ProgressPreference to SilentlyContinue first.
- And it usually will not download at all. Building in a second folder is the
  ordinary case rather than a rare one, so the build looks for ffmpeg, ffprobe
  and yt-dlp in an existing HomerScribe folder and copies them. They are large
  and change rarely; fetching them again to sit beside a second copy of the same
  source is waste.

## 1.0.116, 11 August 2026

- Two HomerScribes may now be run at once without any care being taken. The
  second notices the first, writes its own log rather than overwriting it, and
  does not save the shared settings while the other may be reading them. Working
  folders were already separate. This is worth doing because transcribing uses
  the processor and describing the graphics card, so two runs on halves of a list
  overlap: about 1.4 times quicker on four films, 1.5 on eight, with a ceiling of
  1.6.
- Fixed a check added two versions ago that would have blocked exactly this. It
  asked whether a process named HomerScribe was running, which matches one in ANY
  folder -- including the case where building is entirely safe because the file
  being written is a different file. The only question that matters is whether
  this file can be opened for writing, so that test is now the authoritative one
  and the process check merely explains the answer.

## 1.0.115, 11 August 2026

- The opening no longer goes quiet after "Starting". Between that word and the
  first message about the actual work, HomerScribe finds its programs, asks
  yt-dlp its version, looks for Whisper, asks Ollama for its model list, reads a
  list file and expands a playlist -- any of which can take seconds, and all of
  which was silent. Each step now says what it is about to do before doing it,
  so the longest silence is one step rather than all of them.
- The moment the sources are known it says how many there are and names the
  first, which is the confirmation that the program understood what it was
  given.
- Names are said as a person would say them: no folders, no extension, and
  underscores read as the spaces they stand for. A file called
  The_Africans_-_Episode_1.mkv is announced as "The Africans - Episode 1".

## 1.0.114, 11 August 2026

Written after a review of the whole project, which is in Review.md.

- The montage no longer samples the gap. It sampled frames from the start of the
  gap to its end -- but a gap is a pause in the speech, frequently the least
  eventful part of a film, and what the listener needs described usually happened
  while somebody was talking just before it. The published systems separate the
  INTERVAL being described from the PLACEMENT PERIOD it is spoken in; this
  program was conflating them and describing the pause. The window now runs back
  from the end of the gap far enough to cover the run-up, never shorter than the
  gap itself. This is the change most likely to improve the descriptions
  themselves rather than their placement.
- Every run now logs "This build does:" and the names of the behaviours compiled
  into it. Five rounds of analysis in this project were spent on logs from builds
  that did not contain the change being analysed, because nothing in a log said
  what was in the build and a version number says only when it was made.
- Added Review.md: why the same kinds of mistake recurred, and what now prevents
  each. Five kinds, each with its safeguard.

## 1.0.113, 10 August 2026

Two of my own decisions reversed by measurement.

- The frames go back to 512 pixels from 768. I raised them arguing that quality
  comes before time; the measurement says 2.25 times the pixels cost 3.9 times
  the time -- attention is quadratic, so image tokens are worse than linear --
  and bought nothing detectable. The same film took 6 hours 6 rather than 2
  hours, with overlap and interpretive language unchanged. The setting now
  records that cost so the next person to reach for it knows.
- A moment placed on the timer started at the MIDDLE of the quiet that had been
  measured for it, so a description had half the room the program thought it
  had, and the second half ran into the speech. That is where the residual 6.6
  percent came from, and why the rule added in the previous version to drop an
  overrunning description never once fired: the overrun was not detected,
  because the room was recorded as larger than what remained. The moment now
  starts where the quiet starts, keeping the same margin a natural gap keeps,
  and is given the whole of the quiet that was measured.
- placement_test.py shared the same fault and has been corrected. It now reports
  nought overlap from 4 percent speech to 93.

## 1.0.112, 10 August 2026

Measured on the same film across three runs as the floor was corrected:

    min-gap   descriptions   over speech
      2s          315          38  (12.1%)
      4s          315          38  (12.1%)
      5s          279          17  ( 6.1%)

Speech occupies 11.1 percent of that film, so at the old floor a description
landed on speech as often as speech occurred -- chance. It now lands on it about
half as often as chance would give, for 36 fewer descriptions out of 315, all of
which would have been spoken over the dialogue.

- The remaining overlap had one cause: the ladder that shortens a description to
  fit its gap said it anyway when it could not. Those seventeen ran past their
  gap into the speech. A description that will not fit AND would be spoken over
  the dialogue is now dropped. Running past the end into more silence is
  harmless and still allowed, and the two cases are told apart rather than
  treated alike.

## 1.0.111, 10 August 2026

- Added placement_test.py, which runs the real placement rules against generated
  speech patterns from 4 percent speech to 93. Judging the algorithm from runs
  on particular films cannot tell a general improvement from one that happens to
  suit those films; this can.
- It immediately found what no run had. With the floor at four seconds, a
  talkative film put 21 percent of its descriptions over speech -- because a
  description cannot be cut below about twelve words, and four seconds does not
  hold twelve words, so the words overran the room they were given.
  --min-gap is now five seconds, above the shortest description the program is
  willing to say. Across the whole range the figure is nought.

## 1.0.110, 10 August 2026

The room rule ran for the first time, and its own figures showed its floor was
wrong.

- A timer-placed moment needed two seconds of quiet, while a description takes
  four and a half at its shortest and eight at its usual length. So a moment was
  accepted with room for a quarter of what would be said in it and the rest ran
  into the speech. On a film that is 11.1 percent speech, 12.1 percent of the
  descriptions landed on speech: the placement was doing no better than chance
  at the one thing it exists to do.
- The floor is now --min-gap, the same requirement a natural pause has to meet.
  There was never a reason for a timer-placed description to need less room than
  any other; it is the same description.
- One fewer number to be wrong about: the separate two-second constant is gone.

## 1.0.109, 10 August 2026

- Force overwrite cleared the record from disk and left it in memory. The
  already-done check reads that record BEFORE the clearing happens, so the
  moments it held survived the deletion of the file they came from, and the run
  reused moments whose record no longer existed -- announcing "(reused)" while
  doing it.
  This is why the room rule has never once run: every run since it was written
  reused moments worked out before it existed. The memory is now discarded with
  the files, and reset between films.
- Measured from that run, on a two hour forty-five minute film at 11 percent
  speech: 304 descriptions, 36 of them over speech (11.8 percent), the film
  memory refreshed twelve times, and 12.4 seconds a description against 9.7
  before -- the cost of the wider frames.

## 1.0.108, 10 August 2026

- Force overwrite did not apply to the transcript. It was reused whenever the
  file existed, so the one part of the work that takes twenty minutes was the
  one part force did not redo. It now redoes it, and clears the described film,
  the script, the transcript, the interleaved account and the records before
  starting -- everything HomerScribe itself wrote, and nothing else, since a
  folder may hold a context file or notes that are the user's.
- The model now carries a memory of the film it is describing. It cannot learn:
  the model is fixed and forgets everything between calls. But every 25
  descriptions the same model is asked, with no picture, to write 70 words on
  what the film has established -- who keeps appearing, where it takes place,
  what is going on -- and that note goes into every later description with an
  instruction to take it as known. Before this, the hundred and fiftieth
  description knew the two before it and nothing of the other 148.

## 1.0.107, 10 August 2026

- When several sources ask you to sign in, the results now say so once and name
  the setting that answers it. One run had 34 refusals of 81 quoting yt-dlp's own
  advice to use cookies from a browser, which HomerScribe has a setting for; the
  advice was repeated in yt-dlp's terms eighty times and in the program's terms
  never.
- Recorded from that run, which the new status words made legible in nine
  minutes: of 106 videos in that playlist, 25 were already described and 81 could
  not be fetched -- 40 blocked by country, 29 private, 5 needing a sign-in, 7
  withdrawn. The playlist holds 25 usable films and all 25 are done.

## 1.0.106, 10 August 2026

- Added the Resuming status. A film described in part by an earlier run has
  those descriptions read back and only the rest asked for -- the record is
  written after every single description, so an interrupted run loses at most
  one -- but it said so only in the log and on a console that is hidden. It now
  announces "Resuming", with how many descriptions it is carrying forward.
- A transcript made in an earlier run announces the same thing, since not
  listening to an hour of film again is the largest single saving there is and
  the silence while it did not happen was indistinguishable from the silence
  while it did.

## 1.0.105, 10 August 2026

- A source that is skipped, refused or fails now says one word: Skipped, Error
  or Rejected, followed by the reason. Every one of those endings was silent, so
  a run said "Processing 7 of 98 files" and then nothing until the next -- which
  on a playlist where two videos in five have been withdrawn sounds like a
  program racing through work it is not doing. Seven separate paths ended that
  way and all seven now speak.
- The word is the kind of the announcement, so a screen reader hears it before
  the name, and a run of refusals groups into one message rather than
  interrupting once each.

## 1.0.104, 10 August 2026

One rule where there were three thresholds.

- A talkative rule at 70 percent, a crowded rule at 85, and a fixed interval
  underneath both were all proxies for a single question that can be asked
  directly: is there room here for anything to be heard? The code already
  measured the quiet at each point it considered and then placed a description
  regardless. It now declines when there is less than two seconds of it.
- The thresholds become unnecessary. A film with room gets many descriptions, a
  film without gets few, and nothing has to be classified in advance: about 240
  an hour at 10 percent speech, 210 at 35, 145 at 80, and 27 at 95.
- The time spent is now proportional to what there is to gain, which is the
  answer to "80 percent of the quality in the least time": the moments that were
  costing ten seconds each to produce a description nobody could hear are simply
  not attempted.

## 1.0.103, 10 August 2026

Quality before time, as asked.

- Each frame is now 768 pixels wide rather than 512. Everything the program does
  afterwards -- the two passes, the checks, the context -- works on what the
  model could see, and it was reading faces, signs and gestures out of a 512
  wide picture. This costs time in the model and nothing in correctness, which
  is the trade that was asked for.
- Added measure.py, which reads a results folder and prints a short report:
  descriptions, how many landed on speech, their length, spacing, how many judge
  rather than observe, and a sample. Two kilobytes instead of a twenty megabyte
  log, so a question about quality can be answered without uploading a run.

## 1.0.102, 10 August 2026

The entries between 1.0.39 and 1.0.101 were lost. Each was written as a
replacement against the entry above it, and when that replacement found nothing
to match it did nothing and said nothing -- the same silent no-op that has
caused two build failures in this project. Twenty-six entries survived out of
about ninety. Rather than invent the missing ones, the work of that period is
recorded here in one place, and the numbering now matches what a build actually
produces.

What changed between 1.0.39 and 1.0.102:

- The program became HomerScribe, describing video and transcribing speech,
  either or both, with scribed.md interleaving them in the order they happen.
- Descriptions are placed by listening for SPEECH rather than for silence, using
  Whisper. Above 70 percent speech the spacing widens to 45 seconds; above 85
  percent nothing is placed on the timer at all, because a description spoken
  over narration costs the listener the narration too.
- A gap must be at least 4 seconds to count as room. Two seconds holds five
  words, which is a fragment that runs into the speech that follows.
- Whisper's repetition loop is filtered out, and a transcript that comes back
  almost empty is recognised rather than believed.
- Subtitles are refused three ways: the frame is cropped, the rules forbid it,
  and what comes back is checked for a language the film is not spoken in or for
  text that repeats the dialogue.
- Descriptions that judge rather than observe have the judging word, clause or
  sentence removed, provided what remains is still a sentence.
- The presenter is named, since a vision model cannot recognise a face: whoever
  addresses the viewer is the presenter, and saying so took one documentary from
  naming nobody to naming him in 53 of 94 descriptions.
- Progress is spoken through a UIA live region on the dialog's status line
  rather than by timed message boxes, so the keyboard is never taken; only while
  HomerScribe is the window in front, though the title and status line stay
  current regardless.
- Sources may be files, wildcards, web addresses, playlists, or a text file
  listing any of those. Everything that could not be used is named in the
  results with its reason.
- The log opens before anything can fail and grows visibly on disk.

## 1.0.38, 4 August 2026

- Fixed the failure to write the described film, which had never worked since
  the temporary file was introduced. The name was "described.mp4.part", and
  ffmpeg cannot tell what container to write from a ".part" extension: it failed
  instantly with Invalid argument. The temporary name now keeps the real
  extension last, "described.part.mp4".
- A failed write now says what ffmpeg said. The reason had been captured and
  thrown away, so the message was "could not be written" and nothing more.
- Extra descriptions are now placed at the quietest instant the transcript can
  find, rather than on a fixed clock. On a film that is mostly narration this
  helps only somewhat -- there is no room to find -- so HomerScribe now says so
  outright when someone is talking for more than 70 percent of the film, and
  recommends interrupting less often, which helps far more.
- The spoken progress during the long passes now reads like a description line:
  "Listening  0:12:34". The sentences explaining what was about to happen are
  gone.
- The version is no longer announced on launch. It is on the help screen and in
  the read-me, which is where it belongs.

## 1.0.37, 4 August 2026

From a tester's second run, on a machine without a graphics card: two hours,
26 descriptions, and two progress lines in the whole log.

- One results summary at the end of a run, and no message box between videos. A
  box after each video stops a batch until somebody presses a key, which defeats
  giving HomerScribe a folder full of them. The summary names each video, its
  descriptions and where they went, with the totals and the time taken, and
  offers to open the results.
- The slow-machine warning now says how to make it quicker in the way that
  actually matters on such a machine: --summarise no halves the work, since each
  description takes two model calls and each rejected one takes another. It also
  says that nothing is lost, and that --rebuild will make the film from the
  descriptions already written.
- The installer no longer asks for the destination when a previous installation
  is found, as the other Homer Tools do.

## 1.0.36, 4 August 2026

Descriptions are now placed by listening for speech rather than for silence.

- With Whisper installed, HomerScribe transcribes the film once and puts
  descriptions in the quiet between the talking. Silence detection could not tell
  music from speech, so on a scored film it found 113 usable gaps and had to
  invent 588 on a timer. The transcript is kept, so a resumed run never pays for
  it twice, and the program falls back to silence detection and says so when
  Whisper is absent.
- The dialogue spoken in the 25 seconds before each moment is shown to the model,
  so a description does not repeat what the listener has just heard.
  --dialogue-window changes that, or 0 turns it off.
- Two lines of measurement so the contribution can be judged rather than assumed.
  PLACEMENT reports how many gaps were real against how many were placed on the
  timer. RESULT reports how many descriptions overlapped speech, which is the
  fault this change exists to remove, along with how many were written knowing
  the dialogue.
- --speech no restores the old behaviour for a controlled comparison on the same
  film.

## 1.0.35, 4 August 2026

- The installer offers Whisper, ticked, after the vision model: about 500 MB of
  speech recognition, open source and entirely local, installed into
  %LOCALAPPDATA%\HomerScribe\whisper. This version does not use it yet. It is
  offered now so the next version, which will place descriptions by detecting
  speech rather than silence, needs no second download, and so a future
  HomerTranscribe has what it needs.
- HomerScribe now looks for programs in that folder as well as beside itself
  and on the PATH.

## 1.0.34, 4 August 2026

- Cleared the one compiler warning: the flag recording that the console had been
  hidden was set but never read, because the line that used it was lost in the
  rebuild. With the console hidden there is nobody to write to, so those writes
  are now skipped.

## 1.0.33, 4 August 2026

- Fixed a build failure: rebuilding the lost work added a second copy of
  defaultVideoFolder and initialBrowseFolder, which the recovered source already
  had. The duplicates are gone, and every rebuilt helper is now checked to be
  defined exactly once.

## 1.0.32, 4 August 2026

- The console window is hidden when HomerScribe is started from a shortcut or
  the Start menu, following urlFido, extCheck and 2htm: if exactly one process is
  attached to the console, Windows made it for HomerScribe alone. A tester
  Alt-Tabbed onto that window instead of the dialog, and closing it with
  Control C killed the run.
- Resuming no longer reads the whole sound track again. The moment list and the
  settings that produced it are kept in the record, and reused when the settings
  are unchanged. A resumed run now starts describing at once.
- A long wait no longer looks like a stopped program: while the model is
  thinking, HomerScribe says every twenty five seconds that it is still working
  and for how long. On a machine without a graphics card a single description
  takes two or three minutes.
- "Nearly there" removed from the scan progress; the percentage says it.

## 1.0.22 to 1.0.31, 4 August 2026

Recorded together, because the individual entries were lost when the working
copy was rolled back and the code was rebuilt from the running program.

- The film is written once at the end rather than every half hour, and
  --rebuild builds it from descriptions already made without asking the model
  anything.
- Descriptions are made in two passes, after AutoAD-Zero: the vision model looks
  thoroughly, then the same model turns what it saw into one spoken description.
  Measured over a film, this cut interpretive language from 44 percent of
  descriptions to 12 and shortened them by 30 percent.
- A description is never cut in a way that stops it being a sentence. Whole
  sentences go first, then trailing comma clauses, then the model is asked to
  say it again in fewer words.
- Audio only writes a single mp3 of the film's audio with the descriptions mixed
  in, a quarter the size of the film. View output opens the results when a run
  finishes. Every checkbox in the dialog starts unticked.
- Source paths accept wildcard patterns as well as files and web addresses.
- The settings, the working files and usually the log moved to
  %LOCALAPPDATA%\HomerScribe, because an installed program cannot write beside
  its own executable. Each video's folder holds only the described film and the
  script to read.
- A run where every video had already been described says so and offers the
  earlier results, rather than ending in silence.
- Progress is reported from the second description onward, and a run heading past
  half a minute a description says so, says why, and says what to do.
- The installer clears its settings on uninstall, ships developer notes, and its
  Ollama box installs the model too. Both install scripts find Ollama where it is
  put rather than asking the PATH.

## 1.0.21, 4 August 2026

- Fixed a fault that would have made resuming impossible: a video was skipped if
  its output FOLDER existed, and the folder exists from the moment work starts.
  An interrupted run would have been skipped rather than continued. What counts
  as finished is now the described film being there.
- The film so far is written on a second thread, so describing carries on
  meanwhile. Writing a three hour film is about five minutes of ffmpeg work that
  has nothing to do with the model, and it was five minutes of doing nothing else.
- With that no longer costing anything, the interval is back to 30 minutes.
- The log is now written under a lock, since two threads may report at once.

## 1.0.20, 4 August 2026

- Writing the described film no longer looks like a hang. It reports progress as
  it goes, says at the start roughly how long it will take, and warns that
  nothing else happens meanwhile. A five minute silence on a three hour film had
  been read as a crash, which is a fair reading.
- The film is written to a temporary name and moved into place only when it is
  complete, so a run stopped part way cannot leave a half-written film where a
  whole one used to be.
- It is written every 45 minutes rather than every 20. At five minutes a time
  the old cadence was spending a quarter of the run on it.
- A stray SKIP at the END of a real description is removed. The model was
  answering with a description and the escape word together, and 21 of 126
  descriptions in one run ended "... as they move quickly. Skip."

## 1.0.19, 4 August 2026

- The opening phase now explains itself on screen. It says that the whole film is
  being read once to find the pauses, that nothing is described until that
  finishes, and roughly how long it takes; then it reports a percentage and a
  time remaining. The decibel figure has gone from the screen, where it meant
  nothing, and stays in the log where it is a tuning detail.
- When the scan finishes it says how many moments were found and that
  descriptions follow one at a time.

## 1.0.18, 4 August 2026

- Fixed a build failure: an empty context folder aborted the installer, because
  a wildcard matching nothing is a fatal error in Inno Setup unless the line is
  marked skippable. Every source below the program itself is now optional --
  only HomerScribe.exe is genuinely required. Since 1.0.15 the context that
  matters is video.md beside the video, so the shipped example is exactly that,
  an example.

## 1.0.17, 4 August 2026

- The dialog now uses the folders Windows nominates, following urlFido. Browse
  source and Choose output open in your Videos folder, or wherever the field
  already points, rather than at whatever the program's working directory
  happened to be. Documents, then the current directory, are the fallbacks.
- The Output directory box opens filled with your Videos folder instead of
  empty. Clearing it still means each video's results go beside the video.

## 1.0.16, 4 August 2026

The prompt reworked as prompt engineering rather than as a list of rules.

- The craft rules now travel as the request's system message; the prompt carries
  only what changes from moment to moment.
- Six rewritten examples added, each wrong then right. A rule that is shown holds
  far better than one that is only stated, and the interpretive language was
  surviving the stated rule in more than half of all descriptions.
- Negative instructions reduced. Naming a phrase to forbid puts that phrase in
  front of the model, and the forbidden openings had duly appeared in the output.
- A repetition penalty is now sent with each request, so repeats are largely
  prevented at generation time instead of being caught and thrown away after.
- The model is held in memory for thirty minutes between moments, so a long run
  cannot lose it to an idle timeout and reload it from disk.
- Temperature raised from 0.2 to 0.35: at the lower setting the model reached for
  the same sentence shapes repeatedly.

## 1.0.15, 4 August 2026

- Context is now found by convention: a file named after the video and sitting
  beside it -- video.md for video.mkv -- is read without being asked for. That is
  how a general purpose describer learns one film's character names.
  --context-file still overrides it, and a run with no context found says so
  rather than quietly producing nameless descriptions.
- Every video gets a folder of its own, named after the video, holding
  described.mkv (or whatever the source extension was), described.md,
  described.json, described.vtt, described.wav, and a HomerScribe.log of that
  video alone. The running log beside the program still holds the whole session.
- A folder that already exists means that video is skipped, so a run over many
  videos can be stopped and resumed. Force overwrite describes them again.
- The new-scene instruction no longer invites a guess at the location. It had
  produced "In the palace hall" over a studio logo.

## 1.0.14, 4 August 2026

Measured against a completed 2 hour 45 minute film, 701 moments, and corrected
where the numbers showed a fault.

- Descriptions no longer ask for more words than can be spoken. The detail level
  had been multiplying the word budget, so an eight second slot was asking for 48
  words: over four words a second, against a comfortable 2.67. 229 of 251
  descriptions overran their slot. The budget is now the time available times the
  speaking rate, and nothing else.
- Repetition is judged against the union of two descriptions rather than the
  shorter of them, which had punished every short description sharing nouns with a
  longer one. 352 of 701 moments had been thrown away, leaving two silences of
  over seven minutes.
- A description is now kept despite echoing an earlier one when nothing has been
  said for --max-silence seconds, 45 by default. Minutes of silence is a worse
  failure than an echo.
- A trim that removed "the camera" could leave a sentence hanging on its
  preposition: "facing away from the camera" became "facing away from". The
  phrase now goes with it.
- Guesses at identity -- "likely Telemachus or another suitor" -- are asked
  again. Naming the wrong character is worse than naming none.
- The interpretive word list gained the offenders a whole film actually produced:
  intently, serious, stern, relaxed, suggests, and the rest. 132 of 251
  descriptions had contained at least one.

## 1.0.13, 4 August 2026

A second reading of the audio description standards, for what the first pass
missed.

- Descriptions no longer narrate what can be heard. The listener has the
  dialogue, the music and the sound effects; description exists to supply what
  sound cannot.
- When the pause is short, who and what come before where, and detail is dropped
  first.
- Names already used are carried forward into later prompts, so a character stays
  named once the film has named them.
- A description placed where no pause existed is now told it will fall across the
  music, and to say nothing unless the moment genuinely matters. The standards
  ask that a score not be talked over lightly.
- The default word budget moves to 2.67 words a second, the 160 words a minute
  the standards call a comfortable pace.

## 1.0.12, 4 August 2026

- The Ollama and vision model checkboxes appear on the final page every time,
  in that order, before the launch and documentation items. The previous build
  hid them when it believed both were already installed, and its test for Ollama
  was wrong in a way that could hide the box on a machine that needed it: when
  ollama is absent, the shell answers "'ollama' is not recognized", and the check
  looked for the word ollama in that answer.
- Those options are now worded like the other Homer Tools installers.

## 1.0.11, 4 August 2026

- The documentation now travels in both forms: Markdown to read in an editor or
  on a braille display, and HTML for a browser. The shortcuts open the HTML.
- buildHomerScribe.cmd regenerates the .htm files with 2htm when it is
  available, and packages whatever is already there when it is not.

## 1.0.10, 4 August 2026

- No license page. The license travels with the program and sits on the Start
  menu, but it is not a gate on the way in.
- The desktop shortcut is created without asking. Its hotkey is named on the
  launch checkbox at the end, which is where the user is looking when it matters.
- The final page now offers what HomerScribe needs first -- Ollama, then the
  vision model -- and only then offers to launch it or open the documentation.
- Each of those two is shown only when it is actually missing. The wizard asks
  ollama what it holds as it opens, so a machine that already has the model is
  not offered a five gigabyte download it does not need.
- installModels.cmd checks before pulling, accepts more than one model name, and
  reports each one. installOllama.cmd notices when Ollama is already installed.
- Fixed the ffmpeg download: an escaped pipe reached PowerShell as a literal
  caret, so the archive was fetched and then not unpacked.

## 1.0.9, 4 August 2026

- The prompt now follows the published audio description standards rather than
  improvised rules: report what is visible and never what it means, present tense
  and active voice, the exact verb, no filmmaking vocabulary.
- Descriptions that judge rather than observe -- tense, ominous, weary, an
  atmosphere -- are detected and asked again, once. `--objective no` turns this off.
- A picture that has changed completely is treated as a new scene, and the model
  is asked to establish where we are before anything else, general to specific.
- The model is now forbidden to run ahead of the film. This closes a real risk:
  the context file hands it the whole plot, so without the rule it could name a
  disguise or an outcome the film has not yet revealed.
- On-screen words that carry meaning are read out, introduced as "Words appear:",
  while translation subtitles are still ignored. A logo is named once.

## 1.0.8, 4 August 2026

- The build script now downloads ffmpeg, ffprobe and yt-dlp when they are not
  already in the folder, so a fresh clone builds a complete installer.
- ffmpeg is taken from an LGPL build rather than a GPL one, which is lighter to
  redistribute and works identically.
- The installer follows the Homer Tools pattern properly: no "install for me or
  for everyone" question, the destination page always shown, modern wizard,
  desktop shortcut with a hotkey, and a launch checkbox at the end.

## 1.0.7, 4 August 2026

- The installer now packages ffmpeg.exe, ffprobe.exe and yt-dlp.exe when they
  are present at build time, so an installed HomerScribe works with nothing
  else set up. A build without them still succeeds.
- The build script warns when a companion program is missing from the folder.
- License notes added for the packaged programs.

## 1.0.6, 4 August 2026

- Downloading from a web address now actually downloads. `--print` implies
  `--simulate` in yt-dlp unless `--no-simulate` is given, so the previous build
  would have reported a file name and fetched nothing.
- yt-dlp is now told where ffmpeg is, so the separate best-video and best-audio
  streams merge even when ffmpeg is only beside HomerScribe and not on the PATH.
- Filenames are restricted to plain ASCII, so punctuation in a video title cannot
  trip the steps that follow.
- Download progress is reported as it happens instead of the screen going silent.

## 1.0.5, 4 August 2026

- Starting the program with nothing on the command line now opens the dialog,
  as a Windows program should. `--gui` still forces it.
- In dialog mode an existing HomerScribe.ini is loaded automatically, so the
  dialog opens showing last time's answers.
- "Log session" now does something: a copy of the log is written into the output
  folder when the run finishes.
- A run started from the dialog reports its result in a message box, since there
  may be no console to print to.

## 1.0.0, 4 August 2026

First release.

- Finds where a description can be spoken, from silences in the sound track plus
  a guaranteed interval so that a continuously scored film still gets described.
- Listens to the centre channel alone when the sound track is 5.1 or better,
  which turns "silence" into "nobody is speaking".
- Tiles several frames from each moment into one picture in time order, so a
  still-image model can see what changes.
- Asks a vision model served locally by Ollama, with an optional context file
  naming the film's characters and setting.
- Refuses to repeat itself: a moment whose picture has barely changed is passed
  over, and a description too close to a recent one is asked again and then
  dropped rather than spoken twice.
- Speaks with a built-in Windows voice straight into memory at the final sample
  rate, so no temporary wave files are made.
- Writes the described film with the description as the first and default audio
  track, the original as the second.
- Writes the script as Markdown for reading on a braille display, as WebVTT for
  players, and as JSON for resuming.
- Saves after every moment and rebuilds the film periodically, so an interrupted
  run loses nothing and resumes without redoing work.
- Detailed log beside the program; the console shows only what went into the film.

Grown from a Python program written across 3 and 4 August 2026, kept in
`prototype\describeMovie.py` as the reference implementation.
