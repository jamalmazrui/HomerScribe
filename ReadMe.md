# HomerDescribe

Audio description for local video files, generated on your own machine.

Blind and low vision viewers miss what happens on screen when a film has no
audio description, and most films have none. HomerDescribe makes one. It finds
the pauses where a description can be spoken, asks a vision model what is
happening, speaks it in a Windows voice, and writes a copy of the film with the
description as its first audio track. It also writes the whole script as a
document, which can be read on a braille display by someone for whom the spoken
track is no use at all.

Nothing is uploaded. The model runs locally through Ollama, the speech comes from
Windows, and the only other programs involved are ffmpeg and, for web addresses,
yt-dlp.

HomerDescribe is one of the Homer Tools, alongside 2htm, extCheck, urlCheck,
urlFido, bookFido, HomerView, EdSharp, FileDir and DbDo.

`Developer.md` describes what the program is built from, how the parts interact,
and how to rebuild it.

## Quick start

1. Install HomerDescribe. On the last page of the installer, leave the Ollama and
   vision model boxes ticked; together they are about six and a half gigabytes and
   take a while.
2. Run HomerDescribe with nothing on the command line. The dialog opens.
3. Choose a video with **Browse source**.
4. Press OK.

You will hear each description as it is made. When it finishes you have a folder
holding `described.mkv` and `described.md`.

Before committing to a feature film, describe five minutes of it:

    HomerDescribe "film.mkv" --begin 00:22:30 --minutes 5

Pick a stretch well into the film rather than the opening credits, and listen to
whether the voice is bearable over five minutes. If it tires you there, it will
be unbearable over two hours.

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

The installer offers to do both of those on its final page, ticked, because
without them there is nothing to write the descriptions. (Those are the
installer's own boxes; the checkboxes in HomerDescribe's dialog all start
unticked.) Each script
notices in a second when its work is already done and says so, so ticking them on
a machine that is already set up costs nothing. `installOllama.cmd` and
`installModels.cmd` stay in the program folder, so either can be run again later.

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

Describe everything matching a pattern:

    HomerDescribe "C:\video\*Africans*.mp4"

Describe several things at once, mixing files, patterns and web addresses:

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
progress as it goes. The version in use is written to the log at the start of
every run.

**When a download fails, yt-dlp being out of date is nearly always the reason.**
YouTube changes how it serves video every few weeks, and an older yt-dlp stops
working — the symptoms are "Precondition check failed", an HTTP 400, and then
"Requested format is not available" because no video format was found at all.
HomerDescribe now runs `yt-dlp -U` and tries once more before giving up, and the
build script replaces any copy more than thirty days old rather than packaging
it.

Some videos cannot be downloaded at all — private, age restricted, or members
only. If the address plays only when you are signed in, HomerDescribe cannot
reach it either.

Or open the dialog, which is what happens when the program is started with
nothing on the command line at all -- from the Start menu, a desktop shortcut, or
just its name:

    HomerDescribe

`--gui` forces the dialog even when arguments were given. Any argument without
`--gui` means a command line run.

Stop it at any time. Run the same command again and it carries on from where it
left off, reusing both the descriptions and the speech already made.

## What it writes

Every video gets a folder of its own, named after the video. `video.mkv` gives a
folder called `video`, beside the video itself, or inside the output directory
when one was given. That folder holds **two files, and only two**:

- `described.mkv` — the film with the description as the first, default audio
  track and the original audio as the second. With **Audio only** ticked this is
  `described.mp3` instead: the mixed sound alone, no video. Any player selects the described
  track without you doing anything. The extension follows the source, so an mp4
  gives `described.mp4`.
- `described.md` — the whole script as a readable document, grouped into
  ten-minute sections with each entry timed from the start of the film. This is
  the one to open on a braille display.

Everything else is working material and lives out of the way, under
`%LOCALAPPDATA%\HomerDescribe\work`, in a folder named after the video: the
record used to resume a run, the caption file, the description track, and the
montages sent to the model. When a run finishes, the bulky ones are deleted and
only the record is kept, because `--rebuild` works from it and it costs a few
kilobytes.

**Audio only** produces one mp3 instead of a film: the original audio with the
descriptions mixed into it, and no video at all. It is a quarter of the size on
a measured sample, and far quicker to make, since nothing has to be copied. When
the picture is of no use to the listener, this is the sensible output — and it
plays on anything, including a phone or a DAISY player. `--audio-only` on the
command line.

If nothing matches what was typed in **Source paths**, the dialog comes back with
the text still in it, so a mistyped path can be corrected rather than retyped.

**View output** opens the results folder when a run finishes, with the described
film selected, so it does not have to be hunted for. `--view-output no` turns it
off for an unattended run.

`HomerDescribe.log` covers the **whole run**, across every video, and is written
to the output directory — or beside the first video when no output directory was
given. It is deliberately not written beside the program: installed, that is
`C:\Program Files\HomerDescribe`, which an ordinary user cannot write to.
Unticking **Log session** keeps it under your application data instead, so a run
that goes wrong still leaves a record without cluttering your results.

**A video whose described film already exists is left alone.** Point
HomerDescribe at a dozen videos, stop it halfway, and run it again: the ones that
finished are skipped, and one that was interrupted **carries on where it
stopped** rather than starting over. Tick **Force overwrite**, or pass `--force`,
to describe everything again from the beginning.

Resuming costs very little. Every description is written to `described.json` as
it is made, so nothing has to be asked of the model twice; only the speech is
made again, and that is a fraction of a second each. A run stopped two hours in
picks up in about a minute.

If you only want the film built from descriptions already made — after a run that
was interrupted after most of the work, say — `--rebuild` speaks and assembles
what is in `described.json` and asks the model nothing at all. On a three hour
film that is about a minute of speech and five minutes of writing, against the
two and a half hours the descriptions took.

`HomerDescribe.log` is written beside the program, not beside the video. It holds
the full detail: environment, every effective setting, every command with its
exit code, and any error. The console shows only what was actually put into the
film, each description prefixed by its position, as `2:14` or `1:37:52`.

## Where it looks and where it writes

HomerDescribe uses the folders Windows nominates rather than whatever directory
it happened to be started from. Starting in the program's own folder is how a
tool ends up dropping results among its own source files.

- **Browse source** opens in your **Videos** folder, unless the Source paths box
  already holds a path, in which case it opens there.
- **Choose output** does the same, and the Output directory box opens already
  filled with your Videos folder rather than empty.
- If Videos cannot be found, Documents is used, and only then the current
  directory.

On the command line the rule is different and deliberate: leaving `--output-dir`
unset puts each video's folder of results **beside the video itself**, which is
almost always what is wanted when a path was typed out in full. Clearing the
Output directory box in the dialog does the same thing.

## Telling it about the film

A description is far better when the model knows what it is watching. Without
context it writes "a man in a boat"; with it, "Odysseus".

**Name the file after the video and put it beside it.** For `video.mkv`, write
`video.md` in the same folder. HomerDescribe finds it without being told, which
is what lets one general purpose program describe any film properly. Nothing has
to be passed on the command line and nothing has to be set in the dialog.

`--context-file` overrides that when you want one file used for several videos.
`context\The_Odyssey.md` is supplied as a worked example of what to write: who
the characters are, what they look like, where the story is set, and an
instruction to describe by appearance rather than guess at a name.

Keep such a file to a few hundred words. It is sent with every single request, so
length costs time on every description.

If no context file is found, HomerDescribe says so plainly at the start of the
run rather than quietly producing nameless descriptions.

## The dialog

`--gui` opens a dialog built with `Lbc.cs`, the shared layout-by-code module used
by DbDo, EdSharp, FileDir and urlFido. It carries the standard controls:

- **&Source paths** with **&Browse source...**
- **&Output directory** with **&Choose output...**
- **&Force overwrite**, **&Log session**, **&Use configuration**, **&Audio only**,
  **&View output** — all unticked when the dialog opens, as in the other Homer
  Tools
- OK and Cancel, with Help supplied by LbcDialog itself

OK and Cancel carry no mnemonic, as Windows convention requires: Escape cancels,
Enter accepts, and Control plus Enter accepts from any control.

The remaining settings stay on the command line for now. Say which of them you
want as controls and where, and they go in.

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

Uninstalling removes the settings, the resume records and the working files
along with the program. Described films and scripts are never touched: they live
wherever you chose to put them.

`--use-configuration` loads settings from `HomerDescribe.ini` at startup and
saves them when the dialog is accepted. The file lives in
`%LOCALAPPDATA%\HomerDescribe`, not beside the program: installed, the program
sits in `C:\Program Files\HomerDescribe`, where an ordinary user cannot write,
and every save failed there. A file left beside the program by an earlier build
is still read, but never written. In dialog mode the file
is loaded automatically when it exists, so the dialog opens showing last time's
answers. Anything given on the
command line wins over the file. The file is written by `Inix.cs`, the shared
Homer ini codec, so hand edits and comments survive a round trip.

## On subtitles

Yes, and by two routes, because one is not enough.

The prompt tells the model that subtitles belong to people who cannot hear, that
the words in them are already spoken aloud in the film, and that it should
neither read them nor mention their presence. It distinguishes them from words
that do carry meaning -- a sign, a letter, a name on a door -- which are worth
reading and are introduced as "Words appear:".

But a model told to ignore text on screen will often read it anyway, because the
text is right there in the picture. So `--crop-bottom` removes the lower part of
every frame before the model ever sees it, twelve percent by default, which is
where burnt-in subtitles almost always sit. Instruction handles the ones that
appear elsewhere; cropping handles the ordinary case, and it cannot be talked
out of.

## Two stages, not one

A description is made in two passes, following AutoAD-Zero from Oxford's Visual
Geometry Group. The vision model is first asked to look thoroughly and say
everything it sees, with three times the word budget. The same model is then
asked again, with no picture attached, to turn those notes into one spoken
description within the real budget.

The reason it helps is that perceiving and being concise are different jobs.
Asked to do both at once, a vision model spends its attention on the picture and
its words on whatever comes first, which is how descriptions ended up verbose and
full of judgements. Separating the two lets the first pass look hard and the
second pass write well. The published result is that this training-free approach
is competitive with models fine-tuned on real audio description.

No second model is installed. It is the same model with no image attached, so
nothing is loaded or unloaded between the calls — which matters, because swapping
between two models on every moment would cost more than everything else put
together.

It costs roughly 20% more time per moment. Some of that should come back: a
measured run spent 38 full vision calls retrying descriptions that judged or
guessed, and those are exactly what a separate writing pass should prevent.
`--summarise no` turns it off, so the same film can be run both ways and compared.

Both passes are recorded in the log, as `Saw:` and `Said:`, so the difference
between what the model noticed and what it chose to say is visible.

## How the model is asked

The prompt is engineered, not improvised, and it is where most of the remaining
quality lives. Four things about its shape are deliberate.

**The rules travel separately from the material.** Everything a describer must
know goes in the request's system message; only what changes moment to moment --
the context file, the last two descriptions, the names already used, the word
budget -- goes in the prompt. The model sees instruction and material as
different kinds of thing, and the rules do not have to be reprocessed as part of
the picture every time.

**Rules are shown, not only stated.** The system message carries six rewritten
examples, each wrong then right: "He looks furious" becomes "He clenches his
fist"; "The camera pans across the shore" becomes "The shore stretches away,
empty to the headland". This matters more than any amount of prose. Measured over
a whole film, 132 of 251 descriptions carried an interpretive word while the rule
against them was stated plainly and no example was given.

**Negatives are few and concrete.** Telling a model never to write "the frames
show" puts that phrase in front of it, and it duly appeared in the output. Where
a positive form exists, it is used instead.

**Repetition is prevented rather than detected.** The request now carries a
repetition penalty, so the model is less inclined to reach for the phrasing it
used a moment ago. Rejecting a repeat afterwards costs a second call and can
leave silence; not producing one costs nothing. The model is also held in memory
between moments, which saves reloading several gigabytes from disk mid-run.

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
- **Do not narrate what can be heard.** The listener hears the dialogue, the
  music and every sound effect. A door slamming or a horse galloping does not need
  describing; the standards are firm that description exists to supply what sound
  cannot.
- **Who and what before where, and detail last**, when the pause is short.
- **Keep the same names and words throughout.** HomerDescribe now carries the
  names it has already used forward into later prompts, so a character does not
  become "a man in a grey cloak" three minutes after being called Odysseus.
- **Let the music play.** The standards ask that a score not be talked over
  except for something that genuinely matters. HomerDescribe's guaranteed interval
  works against that by design, since a continuously scored film would otherwise
  get almost nothing. The compromise: a description placed where no pause existed
  is told that it will fall across the sound, and to answer SKIP unless the moment
  holds something a blind viewer would truly miss.
- **160 words a minute** is the pace the standards call comfortable, which is
  where the default word budget now sits, at 2.67 words a second.

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
- Consistency across a whole production is only partly solved. The names already
  used are carried forward, but the vocabulary and register are not, so the
  context file naming things once and clearly still does most of the work.
- The voice matters more than it looks. The standards ask that the describer's
  voice be clearly distinguishable from the voices in the production without being
  distracting in itself. `--list-voices` shows what is installed; pick one that
  will not be mistaken for a character, and listen to a five minute sample before
  committing to a feature.
- A seven-billion-parameter local model gives useful orientation rather than
  literary description. It is a real improvement over nothing, and a real step
  below professional description.

## Documentation in two forms

Every document ships as both `.md` and `.htm`. The Markdown is the source, and
is the better form for reading in an editor or on a braille display; the HTML is
what the Start menu shortcut and the installer's last checkbox open.

The `.htm` files in this folder are ready to use as they are. Each carries
exactly one level-one heading, a table of contents where the document is long
enough to want one, and `lang="en"`, so the outline is navigable by heading or by
link. Nothing needs to be run to produce them.

`buildHomerDescribe.cmd` will regenerate them with 2htm if it finds it, which
keeps them current when the Markdown changes. When 2htm is absent the build
leaves the existing files alone, so the documentation cannot fail a build.

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

## The prototype

`prototype\describeMovie.py` is the Python program this was grown from, and it
still works. It is the reference implementation: quicker to change when trying a
new prompt, and useful for checking that a change in behaviour is deliberate. It
is not needed to run HomerDescribe.
