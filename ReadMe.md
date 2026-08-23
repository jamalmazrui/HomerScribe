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

Once installed, nothing leaves your machine. No part of the video is uploaded,
no request reaches anyone's server, and there is no account, no subscription and
no key. That also means no tokens: describing a two hour film asks an AI model
several hundred questions and shows it several hundred pictures, and the count
here is zero. You can describe material you would not send anywhere at all.

The vision model runs locally through Ollama, the speech recognition through
Whisper, the voice comes from Windows, and the only other programs involved are
ffmpeg and, for web addresses, yt-dlp. **Transcribing needs Whisper alone** —
about half a gigabyte. Describing needs about six and a half, because it needs a
model that can look at pictures as well as a service to run it in. The installer
offers each separately, and they are downloaded once.

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

## The rest of the documentation

This file is the short way in. Everything else lives in its own document, and
each one has a matching `.htm` beside it if you would rather read it in a
browser.

- **[HomerScribe.md](HomerScribe.md)** — the complete guide. Every setting,
  every document it writes, what it does with captions and with pictures, and
  what to do when something goes wrong. Read this one when you want to know how
  something works.
- **[Hotkeys.md](Hotkeys.md)** — every key you can press, listed three ways: by
  what it does, by the key itself, and by where you are when you press it.
- **[History.md](History.md)** — what changed and when, in plain English.
- **[Developer.md](Developer.md)** — how to rebuild HomerScribe or change it.
- **[License.md](License.md)** — the MIT license, and the terms of the other
  programs HomerScribe uses.
- **[Review.md](Review.md)** — an honest account of what went wrong during
  development and what stops it happening again.
- **[Announce.md](Announce.md)** — the announcement, if you want the short
  version of what this is for.
- **[video_formats.md](video_formats.md)** — a plain explanation of video file
  formats: what `.mkv` is, how it differs from `.mp4`, and where captions live.
