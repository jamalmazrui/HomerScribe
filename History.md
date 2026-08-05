# HomerScribe History

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
