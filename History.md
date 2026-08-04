# HomerDescribe History

## 1.0.30, 4 August 2026

- The log records the version again. Deferring when the log opens had dropped
  the line naming the build, and a support log that does not say which build
  produced it is nearly useless.
- A description can no longer run past what its gap allows, and is never cut in
  a way that stops it being a sentence. Whole sentences are dropped first; then
  trailing comma clauses, which leaves a sentence rather than a fragment; then,
  if it is still too long, the model is asked to say it again in fewer words. A
  sentence opening with a place or a time is never cut back to that opening,
  since a phrase is not a sentence. If nothing can be done grammatically, the
  description runs a second long and the log says so.
- The second pass is told its word limit more plainly, and what to drop first.
  It had been answering with 65 words against a budget of 30.

## 1.0.29, 4 August 2026

- A run where every video had already been described used to end in silence,
  having done nothing and said nothing, which is indistinguishable from a program
  that failed to start. It now says how many were skipped, says how to describe
  them again, offers to open the earlier results, and returns to the dialog.
- The advice given when a video is skipped names the right thing for how the
  program was started: the checkbox from the dialog, --force from the command
  line.

## 1.0.28, 4 August 2026

First public release.

**What it does.** Reads a video file, finds the moments where a description can
be spoken without covering the dialogue, asks a vision model running on your own
machine what is on screen, speaks the answer in a built-in Windows voice, and
writes a copy of the film with the description as its first and default audio
track. Nothing is uploaded.

**Written to standards.** The descriptions follow the published guidance of the
American Council of the Blind's Audio Description Project and the Audio
Description Coalition: report what is visible and never what it means, present
tense and active voice, establish the place when the scene changes, say nothing
the soundtrack already conveys, and never run ahead of what the film has
revealed. HomerDescribe checks its own output against several of these and asks
the model again when it falls short.

**Two passes, not one.** Following AutoAD-Zero from Oxford's Visual Geometry
Group, the vision model is first asked to look thoroughly, and the same model is
then asked, with no picture, to turn what it saw into one spoken description.
Perceiving and being concise are different jobs.

**Knows the film.** A text file named after the video and sitting beside it,
video.md for video.mkv, is read without being asked for. Given the characters and
setting, descriptions use real names instead of "a man in a boat".

**Does not repeat itself.** A moment whose picture has barely changed is passed
over in silence. A description too close to a recent one is asked again and then
dropped rather than spoken twice, unless the film would otherwise go too long
with nothing said.

**Outputs.** Each video gets a folder of its own holding the described film and
described.md, the whole script as a document to read on a braille display.
Audio only produces an mp3 instead, a quarter the size, for when the picture is
of no use.

**Handles a batch.** Several videos, wildcard patterns and web addresses can be
given at once. A video already described is skipped, so a long run can be stopped
and resumed. Every description is saved as it is made, so an interrupted run
loses at most one.

**Speaks as it works.** Each description is shown in a timed message box that a
screen reader reads aloud, and the console prints the film position and the
description as it is embedded.

Grown from a Python program written on 3 and 4 August 2026, kept in
prototype\describeMovie.py as the reference implementation.

## 1.0.1 to 1.0.27, 3 and 4 August 2026

Development builds, never published. The version number advances on every build,
so the first public release carries the number the build machine had reached
rather than starting again at zero.

The work of those builds, by theme:

- **Getting the pipeline right.** Gap detection from the sound track, frames
  tiled into a montage so a still-image model can see movement, speech straight
  into memory at the final sample rate, and the described track muxed as the
  first and default audio stream.
- **Learning what audio description actually is.** The published standards of the
  American Council of the Blind and the Audio Description Coalition were read
  twice, and much of what the program had been doing was wrong by them: it
  interpreted rather than reported, used filmmaking vocabulary, narrated sounds
  the listener could hear, and would have run ahead of the story.
- **Stopping it repeating itself.** A vision model asked about a static shot will
  say the same thing indefinitely. Fixed at three levels: skip a moment whose
  picture has not changed, reject a description too close to a recent one, and
  ask the model again rather than accept an echo.
- **Two passes instead of one**, following AutoAD-Zero: look thoroughly, then
  write briefly.
- **Behaving like a Windows program.** The dialog opens when the program is
  started with nothing; the folders Windows nominates are used; the log, the
  settings and the working files live where an installed program may write them
  rather than beside the executable.
- **Surviving interruption.** Every description is saved as it is made, an
  unfinished run resumes rather than starting over, and a finished one is skipped.
