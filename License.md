# License

MIT License

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

## Programs packaged alongside

HomerDescribe calls three other programs. The MIT license above covers
HomerDescribe itself; each of the others carries its own terms, which apply to
that program and not to this one.

- **ffmpeg** (https://ffmpeg.org) does all the video and audio work. The
  installer packages `ffmpeg.exe` when it is present at build time. Which license
  applies depends on the build: the widely used Gyan "essentials" builds are
  GPL, so distributing one carries the GPL's obligations for that binary,
  including making its corresponding source available. An LGPL build, such as
  those from BtbN, carries lighter obligations and is worth preferring if
  HomerDescribe is to be distributed widely. I am not a lawyer; this is a
  pointer, not advice.
- **yt-dlp** (https://github.com/yt-dlp/yt-dlp) downloads video from web
  addresses. It is released into the public domain under the Unlicense, so
  packaging it carries no obligation.
- **Ollama** (https://ollama.com) serves the vision model. It is not packaged;
  the installer offers to fetch it, and it is MIT licensed.

The models Ollama runs carry their own licenses, set by whoever published them.
