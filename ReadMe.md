# HomerScribe

Describes video and transcribes speech, on your own machine.

Blind and low vision viewers miss what happens on screen when a film has no
audio description, and most films have none. Deaf and hard of hearing viewers
miss what is said when there are no captions. HomerScribe makes both, from a file
on your disk or from a web address, and writes them as documents that can be read
on a braille display.

Two checkboxes decide what it does:

- **Describe video** watches the picture and says what happens, producing
  `described.mkv` (or `described.mp3`) with the description as its first audio
  track, and `described.md`, the script to read.
- **Transcribe audio** writes down what is said, as `transcribed.md`.
- **Both together** also write `scribed.md`: the words and the descriptions
  interleaved in the order they happen. For someone who can neither see nor hear
  the film, that one document is the whole of it.

Nothing is uploaded and no account is needed. The vision model runs locally
through Ollama, the speech recognition through Whisper, the voice comes from
Windows, and the only other programs involved are ffmpeg and, for web addresses,
yt-dlp. **Transcribing needs Whisper alone** — the 5.5 GB vision model is
required only for describing.

HomerScribe is one of the Homer Tools, alongside 2htm, extCheck, urlCheck,
urlFido, bookFido, HomerView, EdSharp, FileDir and DbDo.

`Developer.md` describes what the program is built from, how the parts interact,
and how to rebuild it.

## Quick start

1. Install HomerScribe. On the last page of the installer, leave ticked whatever
   matches what you want to do: Ollama and the vision model for describing,
   Whisper for transcribing.
2. Run HomerScribe with nothing on the command line. The dialog opens.
3. Choose a file with **Browse source**.
4. Tick **Describe video**, **Transcribe audio**, or both.
5. Press OK.

Before committing to a feature film, describe five minutes of it:

    HomerScribe --describe "film.mkv" --begin 00:22:30 --minutes 5

Pick a stretch well into the film rather than the opening credits, and listen to
whether the voice is bearable over five minutes. If it tires you there, it will
be unbearable over two hours.

Transcribing is quicker and needs no such trial:

    HomerScribe --transcribe "talk.mp3"

## What you need

- Windows 10 or 11, 64 bit. Nothing to install for HomerScribe itself; it is a
  single executable built against the .NET Framework 4.8 that ships with Windows.
- ffmpeg, found beside HomerScribe.exe, in a folder given with `--ffmpeg-dir`,
  or on the PATH. Install with `winget install Gyan.FFmpeg`, then open a new
  console so the changed PATH is picked up.
- Ollama, running, with a vision model installed:

      ollama pull qwen2.5vl:7b

`buildHomerScribe.cmd` downloads `ffmpeg.exe`, `ffprobe.exe`, and `yt-dlp.exe`
when they are not already in the folder, so a fresh clone builds a complete
installer without anything being fetched by hand. Files already present are left
alone, so the download happens once.

The installer then packages them into the program folder, which is the first
place HomerScribe looks. Installed that way, nothing else has to be set up and
nothing has to be on the PATH. If a download fails, the build still succeeds and
says what to do by hand; HomerScribe falls back to the PATH at run time.

None of them are committed to the repository — `ffmpeg.exe` alone is far past
what a repository should carry.

ffmpeg is taken from BtbN's LGPL build rather than a GPL one. Both work
identically here, and the LGPL build carries lighter obligations when the
installer is redistributed. See `License.md`.

On the two downloaders: use **yt-dlp**, and delete `youtube-dl.exe`. youtube-dl
is the original and is now barely maintained, with many broken extractors;
Debian replaced it with an empty package that simply depends on yt-dlp. yt-dlp is
the actively developed fork, releasing most weeks. HomerScribe looks only for
yt-dlp.

## Getting started

The installer offers to do both of those on its final page, checked by default,
because without them there is nothing to write the descriptions. Each script
notices in a second when its work is already done and says so, so ticking them on
a machine that is already set up costs nothing. `installOllama.cmd` and
`installModels.cmd` stay in the program folder, so either can be run again later.

The first box installs Ollama and then goes straight on to the model, so one
tick does the whole job. The second installs the model alone, for a machine that
already has Ollama.

Both scripts find Ollama by looking where it is installed rather than by asking
the PATH, because a console opened before Ollama was installed keeps its old PATH
and would report Ollama missing minutes after installing it.

A third box installs **Whisper**, about 500 MB, into
`%LOCALAPPDATA%\HomerScribe\whisper`. Whisper is OpenAI's speech recognition
model: open source, MIT licensed, entirely local, no account and no login.

**HomerScribe now uses it, and it changes where every description goes.** See
"Hearing the film" below. Without it the program still works, falling back on
listening for silence, and says so at the start of the run.

Only one vision model is needed, and it must be a **vision** model — one that can be
shown a picture. `qwen2.5vl:7b` is the default. A text-only model such as
`llama3.2` cannot see the frame at all, so having one installed does not help.
`installModels.cmd` accepts other names if you want to compare:
`installModels.cmd qwen2.5vl:3b gemma3:4b`.

Check that everything is in place:

    HomerScribe "video.mkv" --check

Hear which voices are available, then choose one:

    HomerScribe --list-voices
    HomerScribe "video.mkv" --voice "Microsoft Zira Desktop"

Describe five minutes from a stretch well into the film, which tests the result
better than opening credits:

    HomerScribe "video.mkv" --begin 00:22:30 --minutes 5

Describe the whole film:

    HomerScribe "video.mkv"

Describe several things at once, mixing files and web addresses:

    HomerScribe "first.mkv" "second film.mp4" https://www.youtube.com/watch?v=...

Each source is handled in turn. A web address is downloaded first, into the
output directory, or into a `downloads` folder beside the program when no output
directory is given, and then described like any other file.

The download is done by yt-dlp rather than by HomerScribe itself, and that is a
deliberate choice rather than a shortcut. Getting a video out of YouTube is not a
matter of reading a page: the addresses are signed by obfuscated JavaScript that
has to be executed, the signing changes without notice, and formats are
negotiated per video. yt-dlp tracks all of that and is updated most weeks. Code
inside HomerScribe doing the same job would be a maintenance burden with
nothing to do with audio description, and it would fail silently one Tuesday when
YouTube changed something. Calling the program that already solves the problem is
the smaller dependency.

HomerScribe finds yt-dlp beside itself or on the PATH, tells it where ffmpeg
is so the separate video and audio streams merge, asks for plain ASCII filenames
so later steps are not tripped by punctuation in a title, and reports download
progress as it goes.

Or open the dialog, which is what happens when the program is started with
nothing on the command line at all -- from the Start menu, a desktop shortcut, or
just its name:

    HomerScribe

`--gui` forces the dialog even when arguments were given. Any argument without
`--gui` means a command line run.

Stop it at any time. Run the same command again and it carries on from where it
left off, reusing both the descriptions and the speech already made.

## Naming what to work on

**Quotes are not required.** A path with spaces, typed or pasted whole, is
recognised as one thing: the file system is asked rather than guessed at. Quoting
still works, and several items on one line still work, but nobody should have to
quote a filename they picked out of a folder.

The Source paths box, and the command line, accept:

- one path, with or without spaces, quoted or not
- several, separated by spaces, or one to a line
- wildcard patterns such as `C:\video\*.mp4`
- web addresses
- **a text file listing one source per line**, mixing files and addresses freely.
  Lines beginning with `#` or `;` are ignored, so a list can carry notes.

A `.txt` file is always taken as a list, never as something to describe.

## What it writes

Every video gets a folder of its own, named after the video. `video.mkv` gives a
folder called `video`, beside the video itself, or inside the output directory
when one was given. A run over several videos keeps their results apart without
any further arrangement.

In that folder:

- `described.mkv` — the film with the description as the first, default audio
  track and the original audio as the second. Any player selects the described
  track without you doing anything. The extension follows the source, so an mp4
  gives `described.mp4`.
- `described.md` — the whole script as a readable document, grouped into
  ten-minute sections with each entry timed from the start of the film.
- `described.vtt` — the same text as timed captions.
- `described.wav` — the description track on its own, to play alongside the
  original in a player such as mpv.
- `described.json` — the machine-readable record used to resume a run.
- `HomerScribe.log` — everything said while describing this video. The running
  log beside the program still holds the whole session, across every video.

**A video whose described film already exists is left alone.** Point
HomerScribe at a dozen videos, stop it halfway, and run it again: the ones that
finished are skipped, and one that was interrupted **carries on where it
stopped** rather than starting over. Tick **Force overwrite**, or pass `--force`,
to describe everything again from the beginning.

Resuming costs very little. Every description is written to `described.json` as
it is made, so nothing has to be asked of the model twice; only the speech is
made again, and that is a fraction of a second each. A run stopped two hours in
picks up in about a minute.

`HomerScribe.log` is written beside the program, not beside the video. It holds
the full detail: environment, every effective setting, every command with its
exit code, and any error. The console shows only what was actually put into the
film, each description prefixed by its position, as `2:14` or `1:37:52`.

## Where it looks and where it writes

HomerScribe uses the folders Windows nominates rather than whatever directory
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
`video.md` in the same folder. HomerScribe finds it without being told, which
is what lets one general purpose program describe any film properly. Nothing has
to be passed on the command line and nothing has to be set in the dialog.

`--context-file` overrides that when you want one file used for several videos.
`context\The_Odyssey.md` is supplied as a worked example of what to write: who
the characters are, what they look like, where the story is set, and an
instruction to describe by appearance rather than guess at a name.

Keep such a file to a few hundred words. It is sent with every single request, so
length costs time on every description.

If no context file is found, HomerScribe says so plainly at the start of the
run rather than quietly producing nameless descriptions.

## Telling it about the film automatically

**Web context** finds out what it is watching, so descriptions can use real names
without you writing anything.

For a web address, it uses the page's own account of itself: the title, who
published it, and the description. yt-dlp already has all of that, so no search
is involved and nothing is guessed.

For a file, it reads the title from the container -- not the file name, the title
the file carries inside it -- and asks Wikipedia. The answer is used **only** when
the title clearly agrees and the article clearly describes a film or programme.
Both tests must pass, because a confident wrong article is worse than none: it
would have the model naming actors who are not in the film. Every candidate and
its score goes in the log, so you can see what was considered and why it was
taken or refused.

Whatever is gathered is added to the context file if you have one, not instead
of it.

**And it identifies the presenter.** A vision model cannot recognise a face, so
knowing that a film was "written and narrated by Ali Mazrui" is not enough on its
own — it will still write "a man". What connects the name to the person is the
one inference a documentary makes safe: whoever addresses the viewer is the
presenter. When the background names one, HomerScribe says so explicitly, and the
name is carried forward from the first description so it stays consistent.

The same applies to a context file you write yourself. A line like "Presented by
Ali Mazrui" or "Narrated by Carl Sagan" is picked up and used the same way.

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

## The window stays

The dialog does not disappear when you press OK. It stays on screen for the
whole run with its controls disabled — the answers are given — and its title
says what is happening: "HomerScribe, describing 42 of 247". Alt+Tab always finds
it, and a screen reader reads the progress from the title.

Every message HomerScribe shows belongs to that window, so nothing can open
behind something else, and the timed announcements are its children too.

One honest caveat: the work runs on the same thread as the window, so during a
single long model call — two or three minutes on a machine without a graphics
card — Windows may mark the window as not responding. It is still there, still
named, still in Alt+Tab, and it recovers at the next description.

## What you hear while it runs

The dialog carries a **status line**, and that line is a UIA live region.
Messages are written to it and spoken by whichever screen reader is running —
JAWS, NVDA or Narrator — **without taking the keyboard focus**, so you can work
in other windows while a film is being described.

Messages of one kind are collected and spoken together. The kind is said, then
where the film had reached when the group started, then the messages:

    Describing. 1 hour 4 min, 51%. A train crosses a bridge above a dry
    riverbed. Ali Mazrui walks along a harbour wall, speaking to the viewer.
    Cranes stand against a pale sky.

There are four kinds — **Initializing**, **Transcribing**, **Describing**,
**Finalizing**. The position is a time and a percentage: minutes below the hour,
hours and minutes above it, and nothing at all rather than a zero. A group ends
when the kind changes, after twenty seconds, or once it is long enough to be
worth hearing.

`--boxes` returns to the old timed message boxes. They announce reliably in any
reader, but they take the focus for as long as they are up, and a two hour film
raises well over three hundred of them. `--announce-progress no` turns
announcements off altogether.

The account is chronological: the words of the film and the descriptions come in
the order they happen. Whisper reads the whole film before a single description
is made, so the words are held and played out in step with the descriptions
during the pass that follows.

## Configuration

`--use-configuration` loads settings from `HomerScribe.ini` beside the program
at startup, and saves them when the dialog is accepted. In dialog mode the file
is loaded automatically when it exists, so the dialog opens showing last time's
answers. Anything given on the
command line wins over the file. The file is written by `Inix.cs`, the shared
Homer ini codec, so hand edits and comments survive a round trip.

## Settings

Every setting has a long form and a short form. The long form is the command line
parameter and, in the dialog, the label. The short form is the command line
letter and, in the dialog, the trigger letter. Run `HomerScribe --help` for the
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

## Hearing the film

Silence detection asks whether there is sound. The question that decides where a
description belongs is whether **anyone is talking** — and on a film with a
score those are completely different questions. Music is not speech, but it is
not silence either, so a scored film looks to a silence detector like one long
uninterrupted sound.

The cost of that was measurable. One run over a feature film found **113 usable
gaps by silence and had to invent 588 more on a timer** — 84% of descriptions
placed by guesswork, many of them landing on top of dialogue.

With Whisper installed, HomerScribe now listens to the film once, learns where
the speech is, and puts descriptions in the quiet between the talking. The
transcript it produces is used twice more: the dialogue spoken in the 25 seconds
before each moment is shown to the model, so a description does not repeat what
the listener has just heard; and the model can tell who is present from what was
said.

Transcribing happens once and is kept, so a resumed run never pays for it twice.
Reckon on roughly one minute per six minutes of film on a processor, much less
with a graphics card.

`--speech no` turns it off. `--whisper-model` chooses a different size —
`small` is the default and is the right size for this: the question is only where
speech is, not what every word was. `--dialogue-window` sets how much preceding
dialogue the model sees, or `0` for none.

### When a film is nearly all talking

Hearing the film tells the truth, and sometimes the truth is that there is no
room. One measured documentary was **85.8 percent speech**: only 41 real gaps
existed in 57 minutes, so most descriptions had to interrupt somebody, and 60
percent of them landed on the narration.

HomerScribe now says so when speech takes more than 70 percent of a film, and
places the unavoidable interruptions at the quietest instant the transcript can
find rather than on a clock. That helps, but not enormously — on such a film,
what helps far more is interrupting less often. Measured against a model of that
documentary:

- `--every 14` (the default): a description falls on speech about 90 percent of
  the time
- `--every 45`: about half as often
- `--every 90`: less than a third as often
- `--detail brief`, which shortens each description, helps again on top

For a heavily narrated documentary, `--every 45 --detail brief` is a far better
starting point than the defaults, which are tuned for drama.

### When the model keeps declining

On a film that is nearly all narration, a description placed where there is no
pause is told it may answer SKIP unless the moment genuinely matters — and it
often does. One 57 minute programme produced **fifteen** descriptions, with one
stretch of nearly ten minutes in silence.

`--max-silence`, 45 seconds by default, now governs every way a moment can be
passed over, not just a description too like a recent one. When nothing has been
said for that long, the moment is asked again with no leave to skip: say
something, however ordinary. A film should never go minutes without a word.

### Judging whether it helped

The log carries two lines written for exactly this. After the moments are chosen:

    PLACEMENT: 214 real gaps (found by listening for speech), 11 placed on the
    timer, 95 percent real.

And at the end of the film:

    RESULT: 198 descriptions; 3 overlap speech (1.5 percent); 11 were placed on
    the timer rather than in a real gap; 176 were written knowing what had just
    been said; 42 moments left silent.

The number to watch is **how many descriptions overlap speech**. That is the
fault silence detection could not avoid, and it should now be close to zero. The
proportion of real gaps against timer-placed ones is the other: it was 16 percent
before, and should now be most of them. Run the same film with `--speech no` to
see the difference on your own material.

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
  the rule earlier versions broke most often, and HomerScribe now checks its own
  output for judging words and asks again when it finds one. `--objective no`
  turns the check off.
- **Establish the place first when the scene changes.** General to specific: "In
  the palace hall, Penelope sits at her loom." HomerScribe already compares each
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
- **Keep the same names and words throughout.** HomerScribe now carries the
  names it has already used forward into later prompts, so a character does not
  become "a man in a grey cloak" three minutes after being called Odysseus.
- **Let the music play.** The standards ask that a score not be talked over
  except for something that genuinely matters. HomerScribe's guaranteed interval
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
or soften such material of its own accord. HomerScribe does not fight it, and
you should know that is a gap rather than a policy.

The standards also say that when race, ethnicity or nationality is something a
sighted viewer would perceive, the listener should be given it too, since
withholding it leaves them with less than everyone else in the room. Whether and
how to ask a model for that is a judgement for the publisher, so it is not in the
prompt. If you want it, the natural place is the context file, which is read
verbatim and sits in front of every request.

## If it seems to have stopped

It has almost certainly not. Each description takes a few seconds on a machine
with a capable graphics card, and **two to three minutes on one without** — the
model runs on the processor instead, which is perhaps forty times slower. A
twenty minute video that takes three minutes here can take four hours there.

HomerScribe now reports its pace from the second description onward, and says
plainly when a run is going to take hours and why. If you see that, the choices
are:

- Use a smaller model: `ollama pull qwen2.5vl:3b`, then `--model qwen2.5vl:3b`.
  Roughly half the size and noticeably quicker, at some cost in detail.
- Send the model less to look at: `--frames 1 --width 384`.
- Describe a five minute stretch first, with `--begin` and `--minutes`, to find
  out what a whole film would cost before starting it.
- Ask the model to do less. Each description takes two calls, and each rejected
  one takes another: `--summarise no` roughly halves the work, `--objective no`
  removes the second attempt when a description judges rather than observes.

Nothing is ever lost by stopping. Every description is saved as it is made, so
running the same command again carries on where it stopped — and `--rebuild`
makes the film from the descriptions already written, without asking the model
anything.

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

The build does not regenerate them: they are written with the Markdown and ship
beside it, so there is nothing to install and nothing that can fail.

## Building

Everything is in one folder. `buildHomerScribe.cmd` compiles every `.cs` file
present into a single 64-bit executable, then builds the installer if Inno Setup
is found:

    buildHomerScribe.cmd

It writes `buildHomerScribe.log` beside itself, recording the version, the
compiler used, and the full compiler output.

The shared Homer modules — `Lbc.cs`, `Say.cs`, `Inix.cs`, `Util.cs`, `Web.cs` —
are already here, copied unmodified from urlFido so that improvements to them
keep porting between tools. They compile into the same assembly, so the result is
still a single self-contained executable.

No JSON package is downloaded, because none is needed: HomerScribe reads and
writes JSON with `JavaScriptSerializer` from `System.Web.Extensions`, part of the
.NET Framework itself. DbDo fetches Newtonsoft.Json because it needs what
Newtonsoft does that the built-in serializer cannot; nothing here does. Staying
with the built-in one is also what keeps `HomerScribe.exe` a single file with
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

1. `buildHomerScribe.cmd` increments it, then generates `Version.cs` holding
   `BuildVersion.Version`, so the program reports it through `--help`.
2. `HomerScribe_setup.iss` reads `version.txt` at compile time through
   `FileOpen` and `FileRead`, and writes it into the version resource of
   `HomerScribe_setup.exe` through `VersionInfoVersion` and
   `VersionInfoTextVersion`. The text form is set explicitly because tagRelease
   reads the FileVersion *string*, and a tag of `v1.0.0` is wanted rather than
   `v1.0.0.0`.
3. `tagRelease` reads that FileVersion, forms the tag, and posts
   `HomerScribe_setup.exe`, whose name it takes from `OutputBaseFilename`.

So a release is: run `buildHomerScribe.cmd`, commit, run `tagRelease`. The
program, the installer, and the tag can never disagree.

`Version.cs` is generated output. It is in `.gitignore` and should not be edited
or committed.

The first build increments 1.0.0 to 1.0.1. To release 1.0.0 itself, build once
with `buildHomerScribe.cmd nobump`.

## The prototype

`prototype\describeMovie.py` is the Python program this was grown from, and it
still works. It is the reference implementation: quicker to change when trying a
new prompt, and useful for checking that a change in behaviour is deliberate. It
is not needed to run HomerScribe.
