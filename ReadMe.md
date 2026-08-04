# HomerDescribe

Audio description for local video files, generated on your own machine.

HomerDescribe reads a video file, finds the moments where a description can be
spoken without covering the dialogue, asks a vision model what is on screen,
speaks the answer in a built-in Windows voice, and writes a described copy of the
film with the description as the first and default audio track. It also writes
the whole description script as a Markdown document, which can be read on a
braille display by someone for whom the spoken track is no use.

Nothing is uploaded. The model runs locally through Ollama, the speech comes from
Windows, and the only other program involved is ffmpeg.

HomerDescribe is the latest of the Homer Tools, alongside 2htm, extCheck,
urlCheck, urlFido, bookFido, HomerView, EdSharp, FileDir and DbDo.

## What you need

- Windows 10 or 11, 64 bit. Nothing to install for HomerDescribe itself; it is a
  single executable built against the .NET Framework 4.8 that ships with Windows.
- ffmpeg, found beside HomerDescribe.exe, in a folder given with `--ffmpeg-dir`,
  or on the PATH. Install with `winget install Gyan.FFmpeg`, then open a new
  console so the changed PATH is picked up.
- Ollama, running, with a vision model installed:

      ollama pull qwen2.5vl:7b

`buildHomerDescribe.cmd` downloads `ffmpeg.exe`, `ffprobe.exe`, and `yt-dlp.exe`
when they are not already in the folder, so a fresh clone builds a complete
installer without anything being fetched by hand. Files already present are left
alone, so the download happens once.

The installer then packages them into the program folder, which is the first
place HomerDescribe looks. Installed that way, nothing else has to be set up and
nothing has to be on the PATH. If a download fails, the build still succeeds and
says what to do by hand; HomerDescribe falls back to the PATH at run time.

None of them are committed to the repository — `ffmpeg.exe` alone is far past
what a repository should carry.

ffmpeg is taken from BtbN's LGPL build rather than a GPL one. Both work
identically here, and the LGPL build carries lighter obligations when the
installer is redistributed. See `License.md`.

On the two downloaders: use **yt-dlp**, and delete `youtube-dl.exe`. youtube-dl
is the original and is now barely maintained, with many broken extractors;
Debian replaced it with an empty package that simply depends on yt-dlp. yt-dlp is
the actively developed fork, releasing most weeks. HomerDescribe looks only for
yt-dlp.

## Getting started

The installer offers to do both of those on its final page, and offers each only
when it is actually missing: as the wizard opens it asks ollama what it holds, so
a machine that already has the model is not offered the download again. Both
boxes are checked when they do appear, because without them there is nothing to
write the descriptions. `installOllama.cmd` and `installModels.cmd` stay in the
program folder, so either can be run again later.

Only one model is needed, and it must be a **vision** model — one that can be
shown a picture. `qwen2.5vl:7b` is the default. A text-only model such as
`llama3.2` cannot see the frame at all, so having one installed does not help.
`installModels.cmd` accepts other names if you want to compare:
`installModels.cmd qwen2.5vl:3b gemma3:4b`.

Check that everything is in place:

    HomerDescribe "video.mkv" --check

Hear which voices are available, then choose one:

    HomerDescribe --list-voices
    HomerDescribe "video.mkv" --voice "Microsoft Zira Desktop"

Describe five minutes from a stretch well into the film, which tests the result
better than opening credits:

    HomerDescribe "video.mkv" --begin 00:22:30 --minutes 5

Describe the whole film:

    HomerDescribe "video.mkv"

Describe several things at once, mixing files and web addresses:

    HomerDescribe "first.mkv" "second film.mp4" https://www.youtube.com/watch?v=...

Each source is handled in turn. A web address is downloaded first, into the
output directory, or into a `downloads` folder beside the program when no output
directory is given, and then described like any other file.

The download is done by yt-dlp rather than by HomerDescribe itself, and that is a
deliberate choice rather than a shortcut. Getting a video out of YouTube is not a
matter of reading a page: the addresses are signed by obfuscated JavaScript that
has to be executed, the signing changes without notice, and formats are
negotiated per video. yt-dlp tracks all of that and is updated most weeks. Code
inside HomerDescribe doing the same job would be a maintenance burden with
nothing to do with audio description, and it would fail silently one Tuesday when
YouTube changed something. Calling the program that already solves the problem is
the smaller dependency.

HomerDescribe finds yt-dlp beside itself or on the PATH, tells it where ffmpeg
is so the separate video and audio streams merge, asks for plain ASCII filenames
so later steps are not tripped by punctuation in a title, and reports download
progress as it goes.

Or open the dialog, which is what happens when the program is started with
nothing on the command line at all -- from the Start menu, a desktop shortcut, or
just its name:

    HomerDescribe

`--gui` forces the dialog even when arguments were given. Any argument without
`--gui` means a command line run.

Stop it at any time. Run the same command again and it carries on from where it
left off, reusing both the descriptions and the speech already made.

## What it writes

Results go to a folder named after the video with `_described` appended:

- `described.mkv` — the film with the description as the first, default audio
  track and the original audio as the second. Any player selects the described
  track without you doing anything.
- `description.md` — the whole script as a readable document, grouped into
  ten-minute sections with each entry timed from the start of the film.
- `description.vtt` — the same text as timed captions.
- `description.wav` — the description track on its own, to play alongside the
  original in a player such as mpv.
- `description.json` — the machine-readable record used to resume a run.

`HomerDescribe.log` is written beside the program, not beside the video. It holds
the full detail: environment, every effective setting, every command with its
exit code, and any error. The console shows only what was actually put into the
film, each description prefixed by its position, as `2:14` or `1:37:52`.

## Telling it about the film

A description is far better when the model knows what it is watching. Put the
film's characters, setting and story in a text file and point at it:

    HomerDescribe "video.mkv" --context-file context\The_Odyssey.md

`context\The_Odyssey.md` is supplied as a worked example. Without it the model
writes "a man in a boat"; with it, "Odysseus". Keep such a file to a few hundred
words, because it is sent with every single request.

## Settings

Every setting has a long form and a short form. The long form is the command line
parameter and, in the dialog, the label. The short form is the command line
letter and, in the dialog, the trigger letter. Run `HomerDescribe --help` for the
full list with current values.

The ones that matter most:

- `--detail brief|normal|rich` — how much is said at each moment.
- `--every` — guarantees a description at least this often, in seconds, even over
  music. A scored film has almost no true silence, so this does most of the work.
- `--noise-floor` — the level below which sound counts as a gap. Raise it toward
  -16 if too few natural gaps are found, lower it toward -40 if descriptions land
  on quiet dialogue.
- `--crop-bottom` — percentage cut off the bottom of each frame before the model
  sees it, which is how burnt-in subtitles are kept out of the description.
- `--ad-volume` — loudness of the description against the film. Below 1 sits it
  just under normal dialogue level, with the film ducking beneath it.
- `--similarity` and `--same-shot` — how hard it works to avoid saying the same
  thing twice. A vision model asked about a static shot will repeat itself
  endlessly if left alone.

Four short forms are exceptions to the rule that the letter is the first
character of a word, because the natural letters were already taken:
`--every` is `-y`, `--forced-length` is `-z`, `--dialogue-channel` is `-D`, and
`--same-shot` is `-h`.

## Building

Everything is in one folder. `buildHomerDescribe.cmd` compiles every `.cs` file
present into a single 64-bit executable, then builds the installer if Inno Setup
is found:

    buildHomerDescribe.cmd

It writes `buildHomerDescribe.log` beside itself, recording the version, the
compiler used, and the full compiler output.

The shared Homer modules — `Lbc.cs`, `Say.cs`, `Inix.cs`, `Util.cs`, `Web.cs` —
are already here, copied unmodified from urlFido so that improvements to them
keep porting between tools. They compile into the same assembly, so the result is
still a single self-contained executable.

No JSON package is downloaded, because none is needed: HomerDescribe reads and
writes JSON with `JavaScriptSerializer` from `System.Web.Extensions`, part of the
.NET Framework itself. DbDo fetches Newtonsoft.Json because it needs what
Newtonsoft does that the built-in serializer cannot; nothing here does. Staying
with the built-in one is also what keeps `HomerDescribe.exe` a single file with
no DLL beside it.

Three assemblies are not on the compiler's default reference path and are found
by full path: `System.Speech.dll` for the voices, and `UIAutomationProvider.dll`
and `UIAutomationTypes.dll` for the Narrator notification events raised by
`Say.cs`. If any is missing, install the .NET Framework 4.8 Developer Pack.

## Documentation in two forms

Every document ships as both `.md` and `.htm`. The Markdown is the source, and
is the better form for reading in an editor or on a braille display; the HTML is
what the Start menu shortcut and the installer's last checkbox open.
`buildHomerDescribe.cmd` regenerates the `.htm` files with 2htm when it can find
it, and otherwise leaves the ones already present alone.

## Versions and releases

The version lives in exactly one place: `version.txt`, one line and nothing
else. This is the pattern DbDo and bookFido use, and it is the best of the four
approaches across the Homer Tools, because no version literal appears in any
other file and so a stale copy of a file cannot rewind the number.

From there it flows outward:

1. `buildHomerDescribe.cmd` increments it, then generates `Version.cs` holding
   `BuildVersion.Version`, so the program reports it through `--help`.
2. `HomerDescribe_setup.iss` reads `version.txt` at compile time through
   `FileOpen` and `FileRead`, and writes it into the version resource of
   `HomerDescribe_setup.exe` through `VersionInfoVersion` and
   `VersionInfoTextVersion`. The text form is set explicitly because tagRelease
   reads the FileVersion *string*, and a tag of `v1.0.0` is wanted rather than
   `v1.0.0.0`.
3. `tagRelease` reads that FileVersion, forms the tag, and posts
   `HomerDescribe_setup.exe`, whose name it takes from `OutputBaseFilename`.

So a release is: run `buildHomerDescribe.cmd`, commit, run `tagRelease`. The
program, the installer, and the tag can never disagree.

`Version.cs` is generated output. It is in `.gitignore` and should not be edited
or committed.

The first build increments 1.0.0 to 1.0.1. To release 1.0.0 itself, build once
with `buildHomerDescribe.cmd nobump`.

## The dialog

`--gui` opens a dialog built with `Lbc.cs`, the shared layout-by-code module used
by DbDo, EdSharp, FileDir and urlFido. It carries the standard controls:

- **&Source paths** with **&Browse source...**
- **&Output directory** with **&Choose output...**
- **&Force overwrite**, **&Log session**, **&Use configuration**
- OK and Cancel, with Help supplied by LbcDialog itself

OK and Cancel carry no mnemonic, as Windows convention requires: Escape cancels,
Enter accepts, and Control plus Enter accepts from any control.

The remaining settings stay on the command line for now. Say which of them you
want as controls and where, and they go in.

## Spoken status while it runs

When the dialog was used, each description is also shown in a timed message box,
the technique bookFido uses: a screen reader speaks a window that is genuinely
activated, without being asked, and the box closes itself so nothing has to be
dismissed. The caption carries the position in the film, and the body carries the
description that was just embedded. Each time the whole percentage through the
film changes, the caption says so too.

`--boxes` and `--boxes=no` force this on or off regardless of how the program was
started. From the command line it is off by default, since every description is
already printed there.

## Configuration

`--use-configuration` loads settings from `HomerDescribe.ini` beside the program
at startup, and saves them when the dialog is accepted. In dialog mode the file
is loaded automatically when it exists, so the dialog opens showing last time's
answers. Anything given on the
command line wins over the file. The file is written by `Inix.cs`, the shared
Homer ini codec, so hand edits and comments survive a round trip.

## The prototype

`prototype\describeMovie.py` is the Python program this was grown from, and it
still works. It is the reference implementation: quicker to change when trying a
new prompt, and useful for checking that a change in behaviour is deliberate. It
is not needed to run HomerDescribe.

## Where the rules come from

The prompt is not a set of preferences I invented. It follows the published
guidance for audio description: the American Council of the Blind's Audio
Description Project guidelines and standards, and the Audio Description
Coalition's standards for describers. The rules that a machine describer can
actually be held to are these.

- **Report, do not interpret.** A describer says what is visible and lets the
  listener draw the conclusion. Not "he is furious" but "he clenches his fist";
  not "the atmosphere is tense" but whatever on screen made you think so. This is
  the rule earlier versions broke most often, and HomerDescribe now checks its own
  output for judging words and asks again when it finds one. `--objective no`
  turns the check off.
- **Establish the place first when the scene changes.** General to specific: "In
  the palace hall, Penelope sits at her loom." HomerDescribe already compares each
  moment's picture with the last one; when the picture has changed enough to be a
  new scene, the model is told so and asked to begin with where we are.
- **Present tense, active voice, third person**, and the exact verb rather than a
  vague one with an adverb bolted on.
- **No filmmaking vocabulary.** No camera, no shots, no cuts. The whale lunges
  forward; it does not swim toward the camera.
- **Do not run ahead of the film.** Relationships, disguises and outcomes belong
  to the filmmaker to reveal. This one matters here more than it would for a human
  describer, because the context file hands the model the whole plot: without the
  rule it could name a character the film has not identified yet, or give away an
  ending. The prompt now says plainly that the background is for names and
  vocabulary, not for anticipating the story.
- **Read words that carry meaning** -- signs, letters, titles -- introduced as
  "Words appear:", while ignoring translation subtitles.
- **Name a logo once**, plainly, and never again.

Two points from the standards are deliberately left to you rather than built in.

The standards say a describer must not censor: nudity, violence and sexual
content are described as objectively as anything else, because the listener has
the same right to that information as a sighted viewer. A local model may refuse
or soften such material of its own accord. HomerDescribe does not fight it, and
you should know that is a gap rather than a policy.

The standards also say that when race, ethnicity or nationality is something a
sighted viewer would perceive, the listener should be given it too, since
withholding it leaves them with less than everyone else in the room. Whether and
how to ask a model for that is a judgement for the publisher, so it is not in the
prompt. If you want it, the natural place is the context file, which is read
verbatim and sits in front of every request.

## Honest limits

- The model sees still frames, not motion. Four frames tiled in time order give
  it some sense of change, but fast action is missed.
- Memory across moments is short, so recurring characters are described afresh
  unless the context file names them.
- Descriptions are written without knowing what was just said, so they sometimes
  restate the dialogue. A transcript pass would fix this and is not built yet.
- The standards ask for consistent vocabulary across a whole production. Each
  moment is written knowing only the last two descriptions and the context file,
  so consistency rests on the context file naming things once and clearly.
- A seven-billion-parameter local model gives useful orientation rather than
  literary description. It is a real improvement over nothing, and a real step
  below professional description.
