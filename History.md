# HomerDescribe History

## 1.0.11, 4 August 2026

- The documentation now travels in both forms: Markdown to read in an editor or
  on a braille display, and HTML for a browser. The shortcuts open the HTML.
- buildHomerDescribe.cmd regenerates the .htm files with 2htm when it is
  available, and packages whatever is already there when it is not.

## 1.0.10, 4 August 2026

- No license page. The license travels with the program and sits on the Start
  menu, but it is not a gate on the way in.
- The desktop shortcut is created without asking. Its hotkey is named on the
  launch checkbox at the end, which is where the user is looking when it matters.
- The final page now offers what HomerDescribe needs first -- Ollama, then the
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
  are present at build time, so an installed HomerDescribe works with nothing
  else set up. A build without them still succeeds.
- The build script warns when a companion program is missing from the folder.
- License notes added for the packaged programs.

## 1.0.6, 4 August 2026

- Downloading from a web address now actually downloads. `--print` implies
  `--simulate` in yt-dlp unless `--no-simulate` is given, so the previous build
  would have reported a file name and fetched nothing.
- yt-dlp is now told where ffmpeg is, so the separate best-video and best-audio
  streams merge even when ffmpeg is only beside HomerDescribe and not on the PATH.
- Filenames are restricted to plain ASCII, so punctuation in a video title cannot
  trip the steps that follow.
- Download progress is reported as it happens instead of the screen going silent.

## 1.0.5, 4 August 2026

- Starting the program with nothing on the command line now opens the dialog,
  as a Windows program should. `--gui` still forces it.
- In dialog mode an existing HomerDescribe.ini is loaded automatically, so the
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
