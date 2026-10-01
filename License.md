---
title: HomerScribe License
subtitle: MIT, by Jamal Mazrui
author: Jamal Mazrui
---

# HomerScribe License

**HomerScribe** was written by **Jamal Mazrui**, who is its author and
developer, and is released under the MIT License.

The license below is the standard MIT text, word for word. It is left exactly
as it is written everywhere else so that anybody — a person, a company's legal
team, or a program that scans for licenses — recognises it at once. Where it
says "the Software", it means HomerScribe: the program, its source code, and
the documents that come with it.

In short: use it, change it, sell it, build something else out of it. Keep the
notice below with it. There is no warranty.

## MIT License

Copyright (c) 2026 Jamal Mazrui

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

## What is covered, and what is not

The MIT license above covers **HomerScribe itself** — the program, the source,
and these documents. Nothing below changes that.

HomerScribe does not link any of the programs it uses. It runs each as a
separate process and talks to it through a command line, a file, or a local
web request. The Free Software Foundation's own guidance on the GPL says that
[pipes, sockets and command-line arguments are how separate programs
talk](https://www.gnu.org/licenses/old-licenses/gpl-2.0-faq.html#MereAggregation),
and that putting separate programs side by side is
[mere aggregation, which leaves each one's license
alone](https://www.gnu.org/licenses/old-licenses/gpl-2.0-faq.html#MereAggregation).
So each program below carries its own terms, for itself, and HomerScribe stays
MIT.

I am not a lawyer, and this is a plain account rather than legal advice.

### Packaged in the installer

- **[ffmpeg](https://ffmpeg.org)** does all the video and audio work.
  `build.cmd` fetches the **LGPL** build from
  [BtbN's FFmpeg-Builds](https://github.com/BtbN/FFmpeg-Builds), never a GPL
  one, and this is deliberate: the widely used GPL builds would oblige anyone
  redistributing HomerScribe to supply ffmpeg's source as well. Under the
  [LGPL version 2.1](https://www.gnu.org/licenses/old-licenses/lgpl-2.1.html)
  the duties are to say which license applies, to say where the source can be
  had, and not to stop anyone replacing the copy with their own. All three are
  met here: this file says so, the build is unmodified and its source is at the
  link above, and `ffmpeg.exe` is a plain file in the program folder that
  anybody may overwrite.
- **[yt-dlp](https://github.com/yt-dlp/yt-dlp)** downloads video from web
  addresses. It is released into the public domain under
  [the Unlicense](https://unlicense.org), so packaging it carries no duty at
  all.
- **[ExifTool](https://exiftool.org)** writes a picture's description into the
  picture itself. It is released under
  [the same terms as Perl](https://dev.perl.org/licenses/) — the Artistic
  License or the GPL, whichever the redistributor prefers. The copy packaged
  here is unmodified and its source is at the link above, which is what either
  license asks of somebody passing it on. `exiftool.exe` and its
  `exiftool_files` folder sit in the program folder and may be replaced.

### Fetched, not packaged

These are not in the installer. HomerScribe offers to fetch them, and each is
then the user's own copy under its own terms.

- **[whisper.cpp](https://github.com/ggml-org/whisper.cpp)** hears the speech.
  MIT, like HomerScribe.
- **[Ollama](https://ollama.com)** serves the vision model. MIT.

The models Ollama runs carry their own licenses, set by whoever published them.
Those govern what may be done with what the model produces, and they are worth
reading before publishing a description made with one.

### What would change this

If HomerScribe ever **linked** a library covered by the GPL or LGPL into its
own executable — rather than running a separate program — the answer above
would no longer hold, and the LGPL in particular would bring duties that a
single self-contained `.exe` cannot easily meet. It does not do this, and
should not start.
