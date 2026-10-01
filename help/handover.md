# HomerScribe — handover

For picking this up in a new conversation. Upload this together with the source
from `C:\HomerScribe`: `HomerScribe.cs`, `Lbc.cs`, `Say.cs`, `Inix.cs`,
`Util.cs`, `Web.cs`, `build.cmd`, `HomerScribe_setup.iss`,
`version.txt`, and the documents. Also `captions.py`, which is written and
tested and waiting to be folded in.

Written at version.txt 1.0.143, so the next build stamps **1.0.144**.

---

## Who and how

Jamal Mazrui, blind developer, JAWS on Windows. These hold throughout:

- Begin every reply with a line reading exactly `Claude:` as a JAWS landmark.
- End every reply with a `## Summary` H2 digest of findings and actions.
- Deliver files at the ROOT of a single zip, never nested. **Whenever a file is
  revised or added, put it in HomerScribe.zip** so it can be unarchived straight
  into `C:\HomerScribe`.
- **Camel Type** style: Hungarian prefixes (`s` string, `i` int, `n` real, `b`
  bool, `l` list, `d` dictionary, `o` object, `f` file), lower camel case, no
  subprocedures, constants prefixed `c_`.
- Every CLI script writes a DETAILED log for debugging: environment, every
  setting, every command with its exit code, every error. The log sits beside
  the SCRIPT, except for an installed program, where it goes with the output.
- Never a `.ps1` without a `.cmd` wrapper.
- Prefer lists to tables; a table only for sighted readers, such as
  accessibility test findings.
- Never a search-results URL in a deliverable, and never an unverified one.

He is direct and exacting, and catches errors quickly. When he says something
seems wrong, it usually is: several of the worst bugs here were found because he
questioned a figure that looked fine.

---

## THE WORK TO DO

Three things, specified by him and not yet built. **The source was never
uploaded before this conversation ran out**, so none of it is started.

### 1. Captions in preference to automatic transcription

When **Transcribe audio** is ticked and the film carries embedded captions, use
those instead of Whisper. They are written by a person, so they are more
accurate than any model; they name who is speaking; and they mark what can be
heard but not spoken — a door slamming, music starting — which no transcript of
speech ever contains.

**Only English.** Check the track's language tag; anything else, fall back to
Whisper. He supports only English for now.

Where they come from:

- **A downloaded video** — pass `--write-subs --write-auto-subs --sub-format
  vtt` to yt-dlp, which HomerScribe does not currently do. yt-dlp leaves the
  `.vtt` beside the video, named for the language, and prefers a hand-written
  track over an automatic one.
- **A local film** — `ffprobe -select_streams s` finds a subtitle stream and its
  language tag; `ffmpeg -map 0:s:0 -c:s srt` extracts it. Works for `.mp4`,
  `.mkv` and the rest.

**`captions.py` already does all the parsing and is tested** on four routes: an
`.srt`, a `.vtt`, a subtitle track inside an `.mp4`, and a `.vtt` sidecar as
yt-dlp leaves it. It handles YouTube's rolling repeats (automatic captions
repeat each line as they scroll, so the same words arrive two or three times
over), merges close fragments into passages, and strips the display markup.
Fold its logic into `HomerScribe.cs` rather than shelling out to Python — the
build uses no Python and none is shipped.

### 2. One file, not two

He asked whether `captions.md` should exist alongside `transcribed.md`.
**Recommendation given and awaiting his agreement: one file, `transcribed.md`,
with a line at the top saying where it came from** — captions or Whisper. They
serve the same purpose, and two files of nearly identical content burden the
reader. Where they disagree is a diagnostic interest, and the log can carry it.

### 3. The video's own metadata

yt-dlp already returns the title, uploader and description; HomerScribe fetches
these for its web context but does not put them in the output. They should head
`described.md`, `transcribed.md` and `scribed.md` as appropriate.

---

## What HomerScribe is

Windows, C# on .NET Framework 4.8, one self-contained executable, namespace
Homer. It **describes video** (a vision model through Ollama, spoken by a
Windows voice, mixed into a copy of the film) and **transcribes speech**
(whisper.cpp), either or both. With both, it also writes the two interleaved in
time order — which is the document a deafblind reader needs, and the reason
captions matter: they would carry the sound effects and speaker names into it.

- Folder `C:\HomerScribe`, repository `github.com/jamalmazrui/HomerScribe`
  (lowercase — GitHub redirects the capitalised form).
- Orchestrates four programs and links to none: **ffmpeg**, **whisper.cpp**,
  **Ollama** (qwen2.5vl:7b over HTTP on localhost), **yt-dlp**. Windows speech
  voices speak; UI Automation carries the progress messages.
- `build.cmd` bumps `version.txt`, compiles, and runs ISCC. **The
  build increments version.txt BEFORE compiling**, so a History entry must be
  numbered `version.txt` plus one, or a version in a log cannot be looked up.

---

## The state of the algorithm

Measured on a 2h45 film at 11% speech:

- **1.47% of descriptions overlap speech** (305 descriptions), the best result;
  it was 12.1% before the placement work.
- 10.4 seconds a description, 1h38 for the film.
- Median 24 words spoken into 13.6 seconds of room.

The governing rule, reached by removing three thresholds rather than tuning
them: **a moment is placed only where there is enough measured quiet for the
description to be heard.** `--min-gap` is 5 seconds, above the 4.5 a twelve-word
description needs. A description that would run over the dialogue is dropped,
because it costs the listener the dialogue as well as itself. So the film
decides its own density — about 240 descriptions an hour on a silent film, 27 on
continuous narration — with nothing classified in advance.

`placement_test.py` runs the real placement rules against generated speech
patterns from 4% speech to 93% and reports nought overlap across the range.

**Ceilings, not defects.** Interpretive language runs 13% on documentaries
against 2.7% on silent film. Naming the protagonist reached 44% of descriptions
after tying the name to a visible attribute — up from 34%, well short of the
200-of-341 predicted. Both are the 7-billion-parameter model's limit.

---

## How to measure

Do NOT ask for whole logs. Two things suffice:

- `measure.py <results folder>` — descriptions, overlap, length, spacing,
  interpretive count, a sample. Two kilobytes.
- The **`MEASURE:`** lines at the end of each film's log.

Every run logs **`This build does:`** and the behaviours compiled in. **Check
that line before analysing anything.** Five rounds of analysis here were spent
on logs from builds that did not contain the change under discussion.

---

## The recurring mistakes

`Review.md` in the repository has the full account.

1. **Analysing logs from builds lacking the change.** The fingerprint prevents
   it.
2. **Edits that silently did nothing.** A replacement finding no match does
   nothing and says nothing; it destroyed about sixty History entries.
   **Assert every anchor.**
3. **Testing against my own belief.** `placement_test.py` once carried the same
   bug as the code, because both came from the same wrong picture.
4. **Optimising a proxy** — "percent in real gaps" rather than descriptions a
   listener can hear.
5. **Confusing what the program knows with what he was told.** Three faults
   where the cause was logged and withheld.

Two more: **verify before claiming** — twice I described behaviour I had not
checked, including telling him a script accepted arguments it ignored. And
**state predictions out loud**; three were wrong, two badly overstated in size
though right in direction.

---

## Context files

`Context.md` is the guide. The governing facts:

- The whole file goes into EVERY prompt, so the general part stays under about
  250 words. Timed sections do not count against that.
- **A context file supplies nouns and names, never observations.** "Setting: a
  cave on a rocky island" is context; "a vast cave entrance, his single eye" is
  a description, and the model will produce it whether or not it is in frame.
  The test: could this sentence be false of the picture and still be in the file?
- A heading beginning with a time — `## 41:00 The Cyclops's island` — marks a
  section sent only while the film is inside it. An untimed heading is ignored,
  so a file can be filled in gradually.
- Name somebody only if a stranger could pick them out of a still frame from
  what you wrote, and say to use the name **every time**. Leaving the model to
  judge gave 34% naming and 37 unnamed "bearded man" descriptions spread evenly
  — worse for a listener than either extreme.

---

## Done and working

The placement rule; the two-stage description; the subtitle filter; the Whisper
loop filter; the empty-transcript check; presenter naming; the film memory
(rewritten every 25 descriptions); the UIA live region speaking without taking
focus and staying silent when the window is not in front; status words (Skipped,
Error, Rejected, Resuming); per-film and timestamped session logs; the montage
prefetch; message pumping during model calls; and refusing to describe a file
with no picture.

That last one was confirmed on 17 Aug: **102 mp3 files, all transcribed only,
none described, no spurious `described.mp3` written** — 22,938 spoken stretches,
median 66 per file, 3h20 for the set. Before the fix, 29 mp3 files had each
produced one invented description from a blank montage.

---

## The repository

Cleaned up over many rounds and now 29 files: the source, the documents, the
build and installer scripts, the three install helpers, `version.txt` and
`.gitignore`. No executables, video, Python, Odyssey files or drafts — all still
on disk, none in git. The pack went from 516 MB to 706 KB once the cause was
found by reading the pack index rather than guessing: six 87 MB copies of
**`HomerDescribe_setup.exe`**, the installer from before the rename, which
nothing had searched for.

Four repository scripts, each taking no arguments and writing a log:
`fixRepo.py` (a push rejected for size), `purgeRepo.py` (remove files from
history), `tidyRepo.py` (the whole clean-up), `finishRepo.py` (find what is
taking the space). **They are not in the repository** — the build uses no
Python — and live in the working folder.

**tagRelease, 17 Aug:** reported "ALREADY RELEASED" for v1.0.144 and published
nothing. Not a fault in tagRelease: the local `version.txt` had rewound to
1.0.143 while GitHub already carried 1.0.144 and a release for it, so the build
re-minted a spent number. Two lines of tagRelease's own recovery text are stale
— it claims the build skips already-released numbers (it does not) and tells him
to compile the `.iss` separately in Inno Setup (the build already does it).
Worth correcting when convenient.

---

## Other things outstanding

- **Chunked transcription.** Whisper writes its transcript only on completion,
  so an interrupted run loses the whole 45 minutes. Ten-minute pieces would cap
  the loss. Proposed, not built.
- **Concurrent descriptions.** Two HomerScribe processes describing at once cost
  nothing measurable — 12.9 seconds a description alone against 12.7 while
  sharing — so the card has headroom. Doing it inside one process would nearly
  halve the time, at the cost of each description not seeing its immediate
  predecessor. He has asked to leave this until he says otherwise.
- **A results box for a large batch** runs to 204 lines for 102 files. It could
  give the count, the total, the folder, and list only what was unusual. General,
  not fitted to one case.
- **Flagging a thin transcript by ratio** — stretches per minute rather than the
  present 2%-of-duration floor. Would have named a suspiciously thin file in the
  102-file run instead of leaving it to be spotted.
- **Persisting the film memory between sessions**, and **using his own preferred
  descriptions as prompt examples**. The second is the more promising.
- **Image description** was discussed and is feasible as a separate program:
  JPEG EXIF took 60,000 characters in testing, PNG `iTXt` has no practical
  limit, and `XPTitle`/`XPComment` are what Windows Explorer shows and JAWS
  reads. IPTC's `AltTextAccessibility` and `ExtDescrAccessibility` are the 2021
  accessibility fields. Not started, and should not be part of HomerScribe.
