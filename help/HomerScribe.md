---
title: HomerScribe
subtitle: The complete guide
author: Jamal Mazrui
---

# HomerScribe

The complete guide to HomerScribe: every setting, every document it writes,
what it does with captions and with pictures, and what to do when something
goes wrong.

For installing it and a first run, see **[ReadMe.md](../ReadMe.md)**. For the keys
you can press, see **[Hotkeys.md](Hotkeys.md)**.

## Naming what to work on

**Quotes are not required.** A path with spaces, typed or pasted whole, is
recognised as one thing: the file system is asked rather than guessed at. Quoting
still works, and several items on one line still work, but nobody should have to
quote a filename they picked out of a folder.

The Source paths box, and the command line, accept:

- one path, with or without spaces, quoted or not
- several, separated by spaces, or one to a line
- wildcard patterns such as `C:\video\*.mp4`, which yield only videos and
  recordings: a playlist, a subtitle file or a stray text file that happens to
  match is passed over and the count is reported
- web addresses, including **playlist addresses**, which are expanded into the
  videos they hold. A playlist of more than a dozen prompts a warning, since
  describing them all may take days
- **a text file listing one source per line**, mixing files and addresses freely.
  Lines beginning with `#` or `;` are ignored, so a list can carry notes.

A text file is always taken as a list, never as something to describe: `.txt`,
`.md`, `.markdown`, `.lst` or `.list`. A file that is neither a recording nor a
list is reported as unusable by name, rather than counted among the things that
produced nothing.

A source under a minute long is flagged in the results as possibly damaged or
incomplete. A truncated download is easy to miss among a dozen good files, and
looks from the outside like a very short film.

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
- `described.md` — what can be seen, as a document to read.
- `described.vtt` — the same text as timed captions.
- `described.wav` — the description track on its own, to play alongside the
  original in a player such as mpv.
- `described.json` — the machine-readable record used to resume a run.
- `transcribed.md` — what is said, when **Transcribe audio** is ticked. A line at
  the top says where the words came from: the film's own captions, or Whisper
  listening to it.
- `scribed.md` — both of the above woven together in time order, when both boxes
  are ticked.
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

## Taking the advertisements out

**Remove ads** (Alt+R) writes a second copy of the file with the sponsor breaks
cut out. The original is never touched.

### How it decides

The file is transcribed first — whether or not Transcribe audio is ticked,
because this needs the words and the time each was said. The transcript is then
read in overlapping chunks, overlapping so that a break falling across a
boundary is not seen as two halves.

Each advertisement the model finds comes back with a confidence out of a
hundred, and that figure is then weighed against three other things, each
counted separately:

- **The phrases sponsors use** — *brought to you by*, *promo code*, *dot com
  slash*, and about thirty more. Two or more raise it.
- **What this show's advertisements have said before.** Every sentence of every
  advertisement removed is remembered for that show, so the second episode
  recognises what the first taught it. A sentence heard before, or said twice
  in the same episode, raises it.
- **Whether it reads as a personal recommendation.** A host reading for a
  sponsor in their own voice avoids saying "brought to you by", because naming
  the sponsor undercuts the recommendation. What such a read cannot leave out
  is where to go — *head over to*, *link in the show notes*, *that's something
  dot com*. Two or more of those, with no sponsor phrase, are taken as a
  host-read advertisement rather than counted against it.

**Nothing is cut below 95 out of a hundred.** Missing an advertisement costs
half a minute of annoyance; cutting part of the programme cannot be undone.

### How the cut is made

A span the model returns is usually the line that gave the advertisement away
rather than the whole break, so it is widened over the passages beside it that
also carry advertising wording. Where two breaks sit within twenty seconds of
each other and the model was sure of both, they are treated as one.

Each edge is then moved to the nearest silence, within three seconds, so the
join closes on quiet rather than mid-word.

Sound is cut without re-encoding: exact, and nothing is lost. Video cut without
re-encoding lands on the nearest keyframe and can be a second or two out;
`--ad-reencode yes` makes it exact at the cost of time and a generation of
quality.

### What you get

- **`stripped.mp3`** — or whatever kind went in. The same format.
- **`stripped.md`** — every advertisement found, cut or kept, with its
  confidence, its sponsor-phrase count, how many of its sentences had been
  heard before, the signs of a personal recommendation, the model's own reason,
  and the words it begins with.

**Read the kept ones first.** A break kept at 94 is where the caution is doing
its work. If the same real advertisement is kept again and again, 95 is too
high for that show and `--ad-confidence` can be lowered knowingly.

### The reading model

This is the one part of HomerScribe that wants a model of its own. Finding
advertisements is reading and reasoning about words, with no picture in it
anywhere, and the picture model is poor at it: on one episode it found one
advertisement where a text model found five.

`installTextModel.cmd` fetches `qwen2.5:7b`, or tick it on the last page of the
installer. If a text model is installed, HomerScribe uses it for this and
nothing else; describing and transcribing are unaffected either way.

### Settings

- `--remove-ads` — off unless ticked.
- `--ad-confidence` — 95 by default. Lower it and you will lose parts of
  programmes.
- `--ad-pad` — seconds of quiet left either side of a cut. 0.35 by default.
- `--ad-reencode` — exact video cuts, slower.
- `--text-model` — empty means use the best text model installed.

### What it will not do

It will not catch every advertisement, and it is not meant to. Across four
shows it has removed between one and a half and fifteen per cent of an episode,
tracking what each actually carried. Where a host mentions a product in passing,
or asks you to rate the show, it leaves it alone — those are the programme, and
a copy with the programme cut out would be worse than no copy at all.

## Where it looks and where it writes

Two logs are kept. The **session log** holds everything from one run of
HomerScribe and is named for when that run started —
`HomerScribe-20260811-134216.log` — so a later run never erases an earlier one.
It is appended to rather than rewritten, and flushed every second, so it is
current even while a run is going on.

Each finished film also gets **its own log**, `HomerScribe.log`, in its results
folder, holding only the entries that belong to it. Somebody looking at one
film's results should not have to search a session log for the part about it.


A video fetched from a web address is kept, **in the same folder as its own
results**. HomerScribe asks for the title before downloading, so it knows where
the results will go and puts the video there: the downloaded film, the described
copy and the script all sit together. It is not deleted afterwards, because
fetching it again to redescribe it would be slow for you and discourteous to the
server.

Asking for the title first has a second use: it settles whether the video has
already been described **before** it is downloaded. With Force overwrite off, a
video whose results are already in place is skipped without fetching it at all,
which on an hour-long programme saves the download rather than discovering
afterwards that it was not needed.

Asking for the title first also means the download announces itself by name
rather than by address, which matters because downloading a large video is
otherwise a long silence.

A video that cannot be fetched — withdrawn, blocked in your country, or asking
you to sign in — is reported by name in the results, with the reason the service
gave, rather than being quietly left out of the count. For a video that asks you
to sign in, `--browser-cookies chrome` (or `edge`, `firefox`, `brave`, `opera`)
lets yt-dlp use the cookies of a browser where you are already signed in.


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

## What the model remembers

It does not learn. Ollama serves a fixed model, and nothing said to it today is
remembered tomorrow. What it can do is work from what it is told each time, and
HomerScribe tells it a good deal: the last two descriptions, every name used so
far, the dialogue from the previous 25 seconds, and any context file.

What was missing was the middle distance. At the hundred and fiftieth
description the model knew the two before it and nothing of the other 148, so a
film's setting, its recurring people and its shape had to be worked out afresh
from four frames every time.

So every 25 descriptions HomerScribe asks the same model, with no picture
attached, to write 70 words on what the film has established — who keeps
appearing and how they are dressed, where it takes place, what is going on. That
note goes into every later description with an instruction to take it as known
and describe what is new.

It costs one text call per 25 descriptions. It is reset for each film, because it
belongs to that film.

A context file of your own, passed with `--context-file`, is the strongest
single lever on description quality. **Context.md** explains what belongs in one
and what does not — the short version being that the whole file goes into every
prompt, so it must be under about 250 words, and that vocabulary helps far more
than plot.

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
- Help, **Default settings**, OK and Cancel, in that order left to right,
  following Microsoft's guidance for a secondary window and matching 2htm and
  extCheck. **Enter** is OK, **Escape** is Cancel and **F1** is Help, so those
  two buttons carry no access key and Alt+O and Alt+C stay with Output directory
  and Choose output.

OK and Cancel carry no mnemonic, as Windows convention requires: Escape cancels,
Enter accepts, and Control plus Enter accepts from any control.

The remaining settings stay on the command line for now. Say which of them you
want as controls and where, and they go in.

## The console window

HomerScribe is a console program that also has a dialog, which is how all the
Homer Tools are built: everything available at the command line stays available.
Started from a shortcut, it hides its console at the first instant, because that
window is not the program and is only confusing.

Started from a command prompt you already had open, the console is left alone,
because it is yours. **In that case it holds the same messages the dialog gave**,
one to a line, in the order they were spoken:

    Initializing: Processing 1 file, video.mkv
    Transcribing: 12 min, 7%
    Describing: 1 hour 4 min, 51% A train crosses a bridge above a dry riverbed.
    Finalizing: 2 hours 45 min, 100%

So the window can be read back at leisure with a screen reader's cursor. Lines
appear whether or not they were spoken aloud, since this is a record rather than
an interruption. The command lines, exit codes and paths that used to fill it
are in the log, which is where somebody looking for them will go; errors still
appear in both. `--verbose` puts everything back.

Hiding the console does not take it out of the Alt+Tab list, which is a
limitation of Windows rather than a choice, so during a run you may find two
HomerScribe entries there. The dialog is the one whose title says what the
program is doing.

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

Each source announces itself as it begins. Given a list — a playlist, a wildcard
pattern, or a text file naming several — it says the position too:

    Processing 3 of 9 files, The_Africans_Episode_3.mkv

A single named file or address has no position worth stating, so it simply says
which one it is.

### The announcements themselves

**Audio only** produces sound alone: a single `described.mp3` holding the film's
own audio with the descriptions mixed into it, and no video. It is a quarter the
size of the film, much quicker to make since no video is copied, and enough when
the picture is of no use to the listener. **View output** opens the results when
the run finishes. **Force overwrite** does the work again, ignoring anything an
earlier run wrote.

Only one access key is shared: Alt+D belongs to both **Describe video** and
**Default settings**. Windows cycles between them; press it twice to reach the
second.

**Default settings** puts everything back as it arrives — the source returns to
the sample video, the output directory is cleared, every checkbox is unticked,
and any remembered settings are forgotten — then shows the dialog again so the
result can be seen before anything runs.

The **Source paths** box starts with a video already in it: the W3C Web
Accessibility Initiative's ten Perspectives films, seven and a half minutes in
one. They are freely licensed and published with professionally written
descriptions of every shot, so a first run can be compared against how it should
have been done. Press OK and something happens.

The dialog carries a **status line**, and that line is a UIA live region.
Messages are written to it and spoken by whichever screen reader is running —
JAWS, NVDA or Narrator — **without taking the keyboard focus**.

They are spoken **only while HomerScribe is the window in front**. Work in
another program and it falls silent.

The window itself stays current regardless. Its **title** and its **status line**
are updated as the work goes on whether or not anyone is looking, so you can
Alt+Tab across at any moment and read either with your screen reader's own
commands — the title with JAWS Insert+T, the status line by reading the window —
without waiting to be told anything. Coming back to the window also says where
things stand at once, then carries on.

Messages of one kind are collected and spoken together. The kind is said, then
where the film had reached when the group started, then the messages:

    Describing. 1 hour 4 min, 51%. A train crosses a bridge above a dry
    riverbed. Ali Mazrui walks along a harbour wall, speaking to the viewer.
    Cranes stand against a pale sky.

The opening says what it is doing at each step rather than going quiet while it
finds its programs, asks Ollama for its models, and reads a list or a playlist.
Nothing before the first description takes more than a few seconds without
saying so:

    Initializing. Starting.
    Looking for the programs it needs.
    Asking Ollama which models it has.
    8 files to work on. The first is The Africans Episode 1.
    Processing 1 of 8 files: The Africans Episode 1
    Listening to the film to find where the speech is.

Names are said as a person would say them: no folders, no extension, and
underscores read as the spaces they stand for.

Four more kinds appear when a source is not worked on from the beginning, so a run never
passes over something in silence:

- **Resuming** — some of this film was described in an earlier run, so those
  descriptions are read back and only the rest are asked for
- **Skipped** — already described, and Force overwrite is off
- **Error** — could not be fetched or could not be finished, with the reason
- **Rejected** — not a video, a recording, or a list; or not found

On a playlist where two videos in five have been withdrawn, this is the
difference between a program that sounds like it is racing through work it is
not doing, and one that tells you it is being refused.

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
- `--captions` — on by default. Use the film's own English captions as the
  transcript when it has them, instead of listening to it. Turn it off to make
  Whisper do the work every time.

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

### Names Whisper should spell right

Whisper spells a name it has never read the way it sounds. A recording about
JAWS came back saying "Joss". The `--vocabulary` setting is a list of names
and terms, separated by commas, that Whisper spells as written. It starts
with the screen readers and Homer programs -- JAWS, NVDA, VoiceOver, Narrator,
Windows, Notepad, WordPad, Insert, Caps Lock, HomerScribe, EdSharp, FileDir,
Ollama, Whisper. Add the people and products in your own recordings. Leave it
empty for none.

### When the transcript comes back empty

Occasionally Whisper returns almost nothing from a film that plainly has speech
in it — a sound track in a form it could not read, or a file carrying more than
one audio track where the wrong one was taken. One episode of a series spent 42%
of its own length being listened to and returned six seconds of speech from
fifty-seven minutes.

If less than 2% of a film over five minutes long is speech, HomerScribe says so,
notes how many audio tracks the file carries, and places descriptions by
listening for silence instead.

It does not assume a fault, because there is an innocent explanation: the film
may genuinely have no speech in it. A silent film is exactly this case — Buster
Keaton's *The General* yielded five seconds of imagined speech from seventy-nine
minutes — and falling back to silence detection is the right response either way.

Silent films are, as it happens, where HomerScribe does its best work. That run
placed 262 descriptions with **none** overlapping speech, and 21 of them read out
intertitles: the printed cards carrying the dialogue, which are the one part of
such a film a blind viewer could never reach.

### What counts as a gap

`--min-gap`, four seconds by default, is the shortest silence HomerScribe will
treat as room for a description. It used to be two, and two seconds is not room:
at the standard pace it holds five words, which is a fragment, and anything
longer runs into the speech that follows.

That is how a film could report most of its moments as falling in real gaps and
still have ninety percent of its descriptions landing on the narration. The gaps
were real and too short. The log now states how long the accepted gaps actually
are, alongside how long a twelve-word description needs, so the two can be
compared.

### How many descriptions a film gets

HomerScribe does not decide this by the clock. It asks one question at each
point it considers: **is there enough quiet here for anything to be heard?**

If a moment has less clear space than `--min-gap` — four seconds, about ten
words — no description is placed. That is the same requirement a natural pause
has to meet, because it is the same description and the listener hears it the
same way.
A description spoken over the narration is not merely a poor description — it
costs the listener the sentence it covered as well as itself, so it is worse
than saying nothing.

That single rule handles every kind of film without being told which it is:

| speech in the film | descriptions an hour |
| --- | --- |
| 10% — a silent film with a score | about 240 |
| 35% — drama | about 210 |
| 60% — a documentary with scenes | about 190 |
| 80% — a lecture with pictures | about 145 |
| 95% — continuous narration | about 27 |

The last row will look like a failure and is not. A programme narrated end to
end has perhaps twenty places where something can be said without talking over
it. Saying two hundred things instead does not describe it better; it makes it
harder to follow.

It also means the time spent is proportional to what there is to gain. A film
with room takes longer because there is more worth describing.

### When the model keeps declining

On a film that is nearly all narration, a description placed where there is no
pause is told it may answer SKIP unless the moment genuinely matters — and it
often does. One 57 minute programme produced **fifteen** descriptions, with one
stretch of nearly ten minutes in silence.

`--max-silence`, 45 seconds by default, now governs every way a moment can be
passed over, not just a description too like a recent one. When nothing has been
said for that long, the moment is asked again with no leave to skip: say
something, however ordinary. A film should never go minutes without a word.

### When a description will not fit

A description is shortened to fit the room it has: the voice speeds up, then
whole sentences go, then trailing clauses, then the model is asked to say it
again in fewer words. Words are never cut off the end.

If after all that it still runs past its gap, what happens depends on what
follows. Running into more silence is harmless and allowed. Running into speech
is not, and the description is dropped instead — for the same reason as
everywhere else: spoken over the dialogue it costs the listener the dialogue
too, so it is worth less than nothing.

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

## The film's own captions

Many films already carry the words. A subtitle track inside the file, or a
caption file sitting beside it, holds what is said and when. When **Transcribe
audio** is ticked, HomerScribe uses those in preference to listening to the film.

Three reasons, and each of them is something a machine cannot do:

- **A person wrote them**, so the words are right. No model matches that.
- **They say who is speaking.** A transcript made by listening cannot tell one
  voice from another.
- **They write down what can be heard but not spoken** — a door slamming, music
  starting, a phone ringing. A transcript of speech has no way to hold that.

That third one is why this matters most for `scribed.md`, the document for
somebody who can neither see nor hear the film. It now carries a **Sound** entry
for each of those, and a name against each line of speech.

A sound always stays an entry of its own. Nearby lines of speech are gathered
into a passage so the document reads as prose, but a sound is never gathered in
with them, and never has speech gathered into it.

### Where they come from

- **A subtitle track inside the film.** Every track is asked what language it is
  in, so an English one is used even when it is not the first.
- **A caption file beside the film**, named `film.en.vtt` or `film.srt`. This is
  where yt-dlp leaves them.
- **A video on the web.** HomerScribe asks the page which English tracks it
  has and fetches one of them. Asking for more is how a video service decides
  you are asking too often.

If you tick only **Transcribe audio** and the video has its own captions, **the
film is not downloaded at all.** Nothing in a transcript needs it, and skipping
it turns a hundred-megabyte download into about thirty kilobytes. Tick
**Describe video** as well and the film is fetched, because then there is
something to look at. A video with no English captions is fetched and listened
to, as before.

### English only, for now

A track in any other language is left alone and Whisper does the work. A film
with one subtitle track that does not say what language it is in is taken as
English, and `transcribed.md` says the language was never stated.

A track made of pictures rather than words — the kind a DVD or a Blu-ray carries
— cannot be read as text, so Whisper does that one too. The log says which
happened and why.

### Automatic captions

YouTube's automatic captions roll up the screen, showing each line two or three
times as they go. HomerScribe recognises a rolling track by what is inside it
rather than by its name, and takes the repeats out. Where both a hand-written
track and an automatic one are on disk, the hand-written one is used.

An automatic track is still a machine listening, so it is no more accurate than
Whisper. It is used because it usually costs nothing to have and takes no time,
and because a film that has one often has a hand-written one too.

### What captions do not decide

**Captions never decide where a description goes.** A caption is put on screen
early and taken away late, so that a reader has time to finish it. That is not
the same as when the words are said. Placing descriptions from those timings
would shift the quiet that HomerScribe measures, and the cost would show up
nowhere in any count — only in a listener losing dialogue.

So a run that describes still listens to the film with Whisper, and uses what it
hears for placement alone. The words you read still come from the captions. Both
boxes ticked means both things happen, and the film takes no longer to describe
than before.

### Turning it off

`--captions no`, or clear the box, and Whisper does the work in every case. That
is how to compare the two on the same film.

## The documents, and how they are meant to be read

A log is written for a machine. These three are written for a person, and for a
person listening rather than skimming. The rules come from the W3C Web
Accessibility Initiative's guidance on transcripts.

**Times are almost gone.** The Initiative says timestamps in a transcript are
usually unnecessary clutter, and that where they are kept they need not be as
fine as the captions and need no end times. So no line carries a time. A time
appears only in a section heading, and only when a film runs over twenty
minutes — below that there is no time anywhere in the document. A heading reads
"From 10 minutes", because that is a place in the film rather than four numbers
to listen to.

**Everything is in paragraphs.** A hundred and thirty list items means hearing
"bullet" a hundred and thirty times. A paragraph also ends where a sentence
ends, so nothing reads as though the speaker was cut off part way.

**The heading is short.** What it is, who published it, how long it runs, where
the words came from, and a link. Every word of it is read before the reader
reaches a single word of the film, so there is nothing else in it. Notes
explaining speaker names or bracketed sounds appear only in a document that
actually has them.

**What the publisher wrote about the video** is included when it says something
about the video. Sentences carrying a web address are taken out, along with
appeals to subscribe and permission notices. If little is left, the section is
dropped.

### The three documents

- `transcribed.md` — what is said.
- `described.md` — what can be seen.
- `scribed.md` — both, in the order they happen. This is what is called a
  **descriptive transcript**, and it is the whole of the film for somebody who
  can neither watch nor listen to it. The Initiative treats it as the document
  to provide, not as an extra, and HomerScribe now says so at the top of it.

In `scribed.md`, **only the descriptions are labelled**, with a bold
"Description." at the front. Everything unlabelled is the film's own — its
words, and the sounds its captions recorded. Labelling both sides would double
the reading to say the same thing.

## Describing the pictures in a zip

Give HomerScribe a `.zip` file, or a wildcard like `C:\Photos\*.zip`, and it
describes the pictures inside it. A folder is made beside the archive, named
after it, holding `descriptions.md`: every picture, in the order it appears in
the archive, with what can be seen in it.

Pictures read straight from the archive: `.png`, `.jpg`, `.jpeg`, `.webp` and
`.gif`. Also read, after ffmpeg turns them into a PNG: `.bmp`, `.tif` and
`.tiff`. Anything else in the archive is passed over, counted, and named in the
log so that nothing looks lost.

Each entry also gives **the name the picture would be given** — a short
description instead of `IMG_4471.JPG` — and **whether its format can hold the
description inside the file**. Some cannot: a GIF has only a plain comment
block, and a BMP has nowhere at all. That is a fact about those formats.

A new name may run to 79 characters before its extension, and is cut at a word,
never in the middle of one. `--name-length` changes that.

Where two pictures would be given the same name, **every one of them is
numbered** — `Sunset over water-1`, `-2` — and a name used only once is left
plain. Numbering them all is deliberate: leaving the first one plain would sort
it last, because a hyphen comes before a full stop.

The number carries as few leading zeros as will do. A pair gets `-1` and `-2`;
a group of a dozen gets `-01` to `-12`. The width comes from the largest
clashing group in the archive, so every numbered name in one folder matches.

**The picture's own file name is used as context.** `Jeannie & Jim - Lake
Tahoe.jpg` was named by somebody who was there, so the people and places in it
are taken as right and used where what is in the picture fits. What the camera
wrote — `IMG-20230113-WA0000` — is thrown away first, since it says nothing.
This is not a licence to guess: a name someone typed is a statement, a face is
only a resemblance, and HomerScribe still never names anyone from a face alone.

### Telling it who is in the pictures

A model looking at a family photograph can honestly say "three men and an older
woman at a table" and no more. If you put a note in the archive, it can do
better.

- A note named after the archive — `thanksgiving.md` inside `thanksgiving.zip`
  — is used for **every** picture in it.
- A note named after one picture — `image07.md` beside `image07.jpg` — is used
  for **that one only**.

Both are used where both exist, and anything you give with `--context-file`
comes before them. The nearest note has the last word.

Write the note as a way of **recognising** people, not as a list of who is
there:

> In these family pictures there will sometimes be a set of three brothers,
> in oldest to youngest order. The eldest has the most baldness, the youngest
> some. If you recognise a woman in her eighties, that is their mother.

HomerScribe tells the model to use a name **only** where what is actually
visible matches what the note describes, and to say what it can see without a
name where it cannot tell. Without that instruction a model will happily write
in every name it has been given, whether or not the person is in the frame —
which is worse than no names at all, because you cannot tell a real
identification from a guess.

### What you get

In a folder named after the archive:

- **`described.md`** — every picture, with every field that now has a value in
  it, sorted by field name. Read back out of the files themselves, so what is
  listed is what is there, including anything that was in the picture before
  HomerScribe saw it. There is a second copy inside the archive, listing the
  same pictures under their new names.
- **Every picture, under its original name**, with the description written
  inside the file.
- **`described.zip`** — the same pictures again, under their new descriptive
  names, carrying the same descriptions.

The two copies are identical apart from the name. The description is written
once and then copied, so they cannot drift apart.

The description goes into several fields, because different programs look in
different places: the two accessibility fields the IPTC added in 2021 for
exactly this purpose, the caption fields most photo software displays, and the
title and comment fields **Windows Explorer shows and a screen reader reads out
of a file's properties**. That last pair is the one you will meet day to day.

Some formats cannot hold a description. A GIF has only a plain comment block,
and a BMP has nowhere at all. Those pictures are still described and still
named; `descriptions.md` says which they were.

### What HomerScribe writes into a picture

HomerScribe asks the model for two things about each picture: a short **name**
of eight to fourteen words, and a fuller **description**. Those two pieces of
text are then written into several different fields, because different programs
look in different places, and a description nobody finds is no use.

Here is a real example. For a photograph called `IMG_4471.JPG`, HomerScribe
might write:

- **AltTextAccessibility** — Elderly woman in a blue floral dress beside a dry
  stone wall
- **ExtDescrAccessibility** — An elderly woman with white hair stands beside a
  dry stone wall in bright sun, wearing a blue floral dress and holding a
  walking stick in her right hand. Fields rise behind her.
- **Description** — An elderly woman with white hair stands beside a dry stone
  wall in bright sun, wearing a blue floral dress and holding a walking stick
  in her right hand. Fields rise behind her.
- **Title** — Elderly woman in a blue floral dress beside a dry stone wall
- **Caption-Abstract** — An elderly woman with white hair stands beside a dry
  stone wall in bright sun, wearing a blue floral dress and holding a walking
  stick in her right hand. Fields rise behind her.
- **ObjectName** — Elderly woman in a blue floral dress beside a dry stone wall
- **ImageDescription** — Elderly woman in a blue floral dress beside a dry
  stone wall
- **XPTitle** — Elderly woman in a blue floral dress beside a dry stone wall
- **XPComment** — An elderly woman with white hair stands beside a dry stone
  wall in bright sun, wearing a blue floral dress and holding a walking stick
  in her right hand. Fields rise behind her.
- **Software** — HomerScribe

So the short name goes into five of them and the fuller description into four.
The two are the same words a person would use for alt text and for a longer
account of the same picture.

What each one is for:

- **AltTextAccessibility** and **ExtDescrAccessibility** are the two fields the
  IPTC added in 2021 for exactly this job. The first is what a web page should
  use as alt text and is capped at 250 characters by the standard; the second
  is its companion for when alt text is not enough, and has no limit.
- **Description** and **Caption-Abstract** are what most photo software
  displays. **Title** and **ObjectName** are the short forms it shows in lists.
- **XPTitle** and **XPComment** are what **Windows Explorer** shows and what a
  screen reader reads out of a file's Properties. For working at a Windows
  machine, these are the two you will meet.
- **ImageDescription** is the oldest of them, understood by almost everything.
- **Software** simply records that HomerScribe wrote the rest.

Nothing already in the picture is removed. The camera, the date, the lens and
anybody else's caption stay where they are.

### What each kind of file can take

Not every format can hold a description, and the ones that can do not all hold
the same set.

**PNG, JPEG and TIFF** take **all ten fields** above. A PNG also gets its own
text chunks, because a PNG has no EXIF and would otherwise show nothing in the
Windows Comments column.

**HEIC and HEIF** — the formats an iPhone has produced since iOS 11 — are
converted to PNG before the model sees them, and take whatever fields your copy
of ExifTool can write to them.

**WebP** takes all ten as well, **but only with a recent ExifTool**. Older
copies answer "Writing of WEBP files is not yet supported", and HomerScribe
then treats WebP as a format with nowhere to put a description and says so in
the log. The picture is still described and still renamed.

**GIF** takes **one field only** — a plain **Comment**, holding the
description. A GIF has no named fields at all, so there is nowhere to put the
short name or anything else.

**BMP** takes **nothing**. The format has no metadata container. The picture is
still described and still renamed, and the description is in `described.md`.

**SVG** is not described at all. It is a drawing rather than a picture, and
nothing here can turn it into something the model could look at. It is named in
the log so you know it was seen and passed over.

Whatever the format, **the copy in `described.zip` is renamed** to the short
name — `Elderly woman in a blue floral dress beside a dry stone wall.jpg` — and
the copy in the folder keeps its original name. A file name is the one piece of
description that every program on earth can read, including those that ignore
metadata entirely.

### How it checks its own work

Before the first picture is touched, HomerScribe takes a copy of it, writes
every field into the copy, reads them all back, and records each one by name as
written or not written. The log holds that list, the version of ExifTool in
use, and the exact commands run. Then it writes the rest the way that worked.

At the end it reads the finished pictures again and counts the descriptions it
can actually find. That count, not the number of commands that ended without
complaint, is what the results box reports. A program finishing is not the same
as the work being done.

Two of the fields — the accessibility pair the IPTC added in 2021 — are not
known to versions of ExifTool from before then. HomerScribe carries their
definitions and supplies them when needed, so they are written either way.

**HomerScribe uses only a single-file `exiftool.exe`** — one binary, with no
`exiftool_files` folder beside it. Anything that needs that folder is passed
over, and the log says so. If no single-file copy is found, pictures are still
described and still renamed; only the writing into the files is skipped.

**Transcribe audio** does not apply to a zip of pictures, and HomerScribe says
so rather than quietly doing nothing.

## When a video will not download

YouTube does not always serve a video the first way it is asked. When that
happens, HomerScribe asks again a different way rather than giving up: the web
player, then the Safari web player, the television player, the iOS player, and
finally a single combined stream. It stops at the first that works and says in
the log which one it was.

If you see the same one working every time, put it in **`--player-client`** —
`web`, `web_safari`, `tv`, `ios` or `mweb` — and the other tries are skipped.

If none of them works, **yt-dlp is updated and the video tried once more**.

yt-dlp is the part of HomerScribe most likely to go out of date, because
YouTube changes what it serves — sometimes on purpose, to stop downloaders —
and yt-dlp follows within days. A copy a few weeks old is a copy from before
several of those changes, and the symptom is a video that used to download
being refused.

So it is kept current in two places, and you are never asked to do it:

- **Every build updates it**, to the nightly release. yt-dlp's own advice is
  that nightly is the channel regular users should be on, because that is where
  a fix for something that broke this week appears first.
- **A refusal updates it**, once per run, before giving up.

`--update-channel stable` if you would rather have the slower, more tested
releases. `--update-tools no` turns the update on refusal off.

A video that is private, deleted, or not shown in your country is not refused —
it is absent, and no player will find it. HomerScribe reads the reason and stops
rather than trying six times.

**A refused film does not cost you the words.** If you asked for a transcript as
well, the captions are fetched anyway and `transcribed.md` is written, because
captions do not come down the same road as the film. The results box says what
was not described and why.

## Naming people in a film from its captions

Where captions name their speakers, HomerScribe collects those names and offers
them to the describing model as names this film uses.

It does **not** tell the model who is on screen, and it would be wrong to. A
film cuts to the listener as often as to the speaker, and a narrator is never
in shot at all. A name given that way would appear in frames that person is not
in, and a reader could not tell a real identification from a guess.

What the list is good for is knowing which names the film uses and how its
makers spell them, so a name reached for is a real one. To attach a name to a
face you also need the **context file**, which is where appearances belong. The
captions say who exists; the context file says what they look like; a name gets
used where the two meet.

Roles that name nobody you could see — Narrator, Audience, All, Crowd,
Voiceover — are left out.

Most documentary and conference captions name nobody, so on that kind of film
this does nothing. A drama with proper subtitles for the deaf and hard of
hearing names people constantly, and that is where it helps.

### Whose captions are trusted

HomerScribe can tell captions a person wrote from captions a machine made, and
treats them differently on purpose.

**A person's captions are used as the transcript.** They are better than
anything a machine can produce, and not only at the words: they say who is
speaking, and they write down what can be heard but not spoken — a door
slamming, music starting, laughter in the audience. That last part is the one a
deafblind reader cannot get any other way.

**A machine's captions are not used.** YouTube's automatic ones carry the words
and nothing else: no speaker, no sounds, and punctuation only where the
recogniser guessed at it. Listening to the film gives at least as good a set of
words and better sentences, so that is what HomerScribe does instead. Pass
`--auto-captions yes` if you would rather have them, for instance to avoid the
download.

The transcript always says which it was.

## On subtitles

A subtitle must never be read out. It is not a description of anything: at best
it repeats what is being said, and at worst it is a translation for somebody
else. Three defences, because two were not enough.

**The frame is cut.** The bottom 18% of every frame is removed before the model
sees it, which physically removes most burnt-in subtitles. `--crop-bottom`
changes it; raise it if a print puts its subtitles higher.

**The rules forbid it**, in capitals, in any language.

**And what comes back is checked**, which is the new one, because a rule that is
stated can still be forgotten. Text the model reports is dropped when either of
two things is true, and both can be recognised without seeing the picture:

- It is in a language the film is not spoken in. Function words give a language
  away in a sentence or two — Spanish, French, German, Portuguese and Italian are
  recognised. Every word in that list was checked against English first, since a
  false match would silence a caption that deserved to be read.
- It repeats what is being said at that moment, which the transcript already
  holds. Text echoing the dialogue is a caption by definition.

An intertitle in a silent film is neither, and survives both tests — which
matters, since reading those is the best thing HomerScribe does.

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

## License

**HomerScribe is [MIT licensed](../License.md)** — use it, change it, sell it, no
permission needed. That covers the program, its source, and these documents.

It does not link any of the programs it uses. It runs each one separately and
talks to it through a command line, a file, or a local web request, which is
how separate programs talk. So each keeps its own terms, and none of them
reaches into HomerScribe's.

Two are packaged with the installer:

- **[ffmpeg](https://ffmpeg.org)**, under the LGPL. The build deliberately uses
  [BtbN's LGPL build](https://github.com/BtbN/FFmpeg-Builds) rather than a GPL
  one, so redistributing HomerScribe does not oblige you to ship ffmpeg's
  source. `ffmpeg.exe` sits in the program folder and you may replace it.
- **[yt-dlp](https://github.com/yt-dlp/yt-dlp)**, in the public domain under
  [the Unlicense](https://unlicense.org), so it carries no obligation.

Two more are fetched rather than packaged, and both are MIT like HomerScribe:
**[whisper.cpp](https://github.com/ggml-org/whisper.cpp)** and
**[Ollama](https://ollama.com)**. The models Ollama runs have their own
licenses, worth reading before you publish a description made with one.

[License.md](../License.md) gives the full text and the reasoning. I am not a
lawyer; that is a plain account, not legal advice.

## What it is, and what it is not

AI small enough to run on a home computer is not as capable as the AI reached
through a commercial service, and the difference shows.

More to the point: **audio description written and performed by professionals is
better than this, by a wide margin.** Description is a craft — knowing what
matters in a shot and what does not, when to stay silent, how to reveal
something at the moment the film reveals it, and how to say a great deal in four
seconds. HomerScribe is not that, and nobody should install it expecting it.

What it is for is everything with no description at all, which is most things. A
lecture recording. A family video. A documentary that was never described. For
those the question is not whether this matches a professional describer, but
whether it beats silence, and it beats silence easily.

Transcription is the stronger of the two halves. On clear speech it is close to
what a person would write down.

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

## Time spent on one film

Each moment needs ffmpeg work before the model can be asked anything: four
frames cut from the film and tiled, then reduced to a thumbprint for the
shot comparison. That work depends only on the film and a timestamp — nothing
from the previous description — so it is done for the **next** moment while the
model is working on this one.

Nothing the model sees changes: the same frames, the same picture, the same
prompt. Only the waiting is removed.

Beyond that there is little left inside a single film that is free. The time is
the two model calls, and the ways to shorten those all cost something: a smaller
picture, fewer frames, one pass instead of two, or a smaller model. Each is
available as a setting and each is a trade rather than a saving.

## Running two at once

Transcribing uses the processor and describing uses the graphics card, so a
single run leaves each idle in turn. Splitting a list in two and running
HomerScribe on each half lets one describe while the other transcribes.

Measured from one film's timings — 45 minutes transcribing, 75 describing — the
expected gain is about 1.2 times on two films, 1.4 on four, and 1.5 on eight,
with a ceiling of 1.6 since the card cannot overlap with itself. Setting
`OLLAMA_NUM_PARALLEL=2` before starting Ollama may raise that to about 2, because
one vision request rarely occupies a card fully.

Nothing needs to be adjusted. A second HomerScribe notices the first, writes its
own log rather than overwriting it, and leaves the shared settings alone. Working
folders are already separate, being named after each source.

**Building while a run is in progress:** unpack the source into another folder
and build there. The file being written is then a different file, and the running
program is untouched.

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

1. `buildHomerScribe.cmd` increments it, stepping over any number already
   released — which it learns from the repository's own tags, so a working copy
   that has fallen behind cannot reuse a number — then generates `Version.cs`
   holding `BuildVersion.Version`, so the program reports it through `--help`.
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
