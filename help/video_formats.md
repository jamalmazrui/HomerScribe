# Video file formats

Written for HomerScribe, but it is general. It answers what `.mkv` is, how it
differs from `.mp4` and the rest, and where captions live.

## The one idea worth having first

**A video file name tells you almost nothing about what is inside it.**

`.mkv`, `.mp4`, `.mov`, `.avi` are **containers**. A container is a box. It
holds separate **tracks**, and each track is encoded with a **codec**.

- The **container** decides how the parts are packed, labelled and found. It is
  the box and the index card on the lid.
- The **codec** decides how the picture or the sound is squeezed down. It is
  what is actually in the box.
- A **track** is one stream inside: a picture, a language of sound, a set of
  subtitles, a chapter list.

So two `.mp4` files can hold completely different things, and an `.mkv` and an
`.mp4` can hold exactly the same picture and sound. Asking "is MP4 better than
MKV" is like asking whether a cardboard box is better than a plastic one
without saying what goes in it.

This is why HomerScribe asks ffmpeg what a file holds rather than looking at
its name.

## The containers

### Matroska, `.mkv`

Matroska is an open container, first released in 2002, and it is the most
permissive of the common ones. It will hold almost any codec, any number of
tracks, subtitles of every kind, chapters, attachments and tags.

- **Strength:** it refuses almost nothing. If two things can be put in a file
  together, Matroska will do it.
- **Weakness:** less widely supported by hardware players, phones and web
  browsers than MP4. A television may not open one.
- `.webm` is a deliberately narrowed Matroska: same structure, but only a short
  list of codecs allowed, so browsers can support it safely.

**HomerScribe merges downloads to `.mkv` for exactly this reason.** It has to
add a description track to whatever the film already carries, and Matroska is
the container least likely to object.

### MP4, `.mp4` and `.m4v` and `.m4a`

MP4 is the ISO standard container, descended from QuickTime. It is the one
everything can play: phones, browsers, televisions, editing software.

- **Strength:** universal support.
- **Weakness:** fussier about what it will hold. Some codecs and some subtitle
  kinds simply cannot go in an MP4, or go in awkwardly.
- `.m4v` is MP4 with an Apple flavour. `.m4a` is MP4 holding sound only.

### QuickTime, `.mov`

Apple's container, and the ancestor of MP4 — the two are so close that many
programs treat them as one. Common in video editing, less common for finished
files people share.

- **Strength:** what professional editing tools produce and expect.
- **Weakness:** files are often huge, because editors keep the picture at high
  quality rather than squeezing it.

### MPEG program and transport streams, `.mpg`, `.mpeg`, `.vob`, `.ts`, `.m2ts`

These are older and built for a different job: being broadcast or played from a
disc, where the file may be joined part way through or arrive damaged.

- `.mpg` and `.vob` are what DVDs use.
- `.ts` and `.m2ts` are what digital television broadcasts and Blu-ray use.
- **Strength:** they survive damage and can be joined mid-stream, which is what
  broadcasting needs.
- **Weakness:** wasteful of space, and limited in what they will hold. Subtitles
  in them are usually pictures rather than text — see below, because that
  matters.

### AVI, `.avi`

Microsoft's container from 1992. Still met in old collections.

- **Weakness:** it predates most of what a modern file needs. No proper support
  for modern codecs, several sound tracks, or text subtitles. Convert it and
  move on.

### Ogg, `.ogg` and `.ogv` and `.opus`

The Xiph Foundation's container, made to be entirely unencumbered by patents.
Mostly met carrying sound.

## What goes inside

### Picture codecs, oldest first

- **MPEG-2** — DVDs and older broadcast. Large files.
- **H.264**, also called AVC — the safe choice. Everything plays it.
- **H.265**, also called HEVC — about half the size of H.264 for the same
  quality, but patent-encumbered, so support is patchier.
- **VP9** — Google's answer to H.265, free to use. What YouTube serves a great
  deal of.
- **AV1** — the current free standard, smaller again, but slow to decode on
  older machines. YouTube serves this too.

### Sound codecs

- **MP3** — old, universal, wasteful.
- **AAC** — what MP4 usually carries. Better than MP3 at the same size.
- **Opus** — the current best for speech and music at small sizes. What YouTube
  pairs with VP9 and AV1.
- **FLAC** — squeezed but loses nothing.
- **PCM** — not squeezed at all. Enormous. What HomerScribe writes while it
  works, because nothing is lost.

### Subtitle tracks, and why the difference matters

This is the part that bites, and it is worth knowing.

**Text subtitles** hold actual words:

- **SubRip**, `.srt` — the simplest. A number, two times, the words.
- **WebVTT**, `.vtt` — the web's version. What YouTube serves.
- **SubStation Alpha**, `.ssa` and `.ass` — text plus positioning and styling.
- **Timed Text**, `.ttml` and `.dfxp` — an XML form, used in broadcast.
- **mov_text** — the cut-down text format MP4 accepts.

**Picture subtitles** hold images of words:

- **VobSub** on DVDs, **PGS** on Blu-ray.
- There are no words in them at all — only pictures of words, one per line.

**Why it matters to you:** a program can read a text subtitle track and use the
words. A picture subtitle track has to be put through optical character
recognition first, which is a separate job with its own errors. When HomerScribe
says a film's subtitles could not be read as text, this is usually why.

## Where captions actually live

Three different places, and they behave differently.

### Inside the file, as a track

An `.mkv` or `.mp4` can carry subtitle tracks alongside the picture and sound.
One file, everything in it, nothing to lose. This is what a DVD or Blu-ray rip
usually gives you, and what a film downloaded with subtitles embedded looks
like.

**HomerScribe reads these directly.** It asks ffmpeg what tracks the file has,
picks an English one, and pulls the words out. No web request is involved.

### Beside the file, as a separate document

A `.srt` or `.vtt` sitting in the same folder, named after the film. Players
find it by name. This is what yt-dlp writes by default, and what HomerScribe
looks for when the film itself carries nothing.

### Burned into the picture

The words are part of the image, painted on before the file was made. Nothing
can extract them, because they are no longer text — they are pixels of a face
and pixels of a letter, indistinguishable to the file.

This is why HomerScribe cuts the bottom of each frame before showing it to the
model: it is the only defence against a burned-in subtitle being read out as
though it were a description.

## What YouTube actually serves

Worth being clear about, because it is not what most people assume.

YouTube does **not** send you a video file. It sends:

- a picture stream, on its own,
- a sound stream, on its own,
- and captions from a **different address again**, in their own format.

Nothing is joined together until something on your machine joins it. That is
what yt-dlp uses ffmpeg for, and it is why a download produces one file from
several requests.

**So captions from YouTube are always a separate request.** Asking for them to
be embedded in the file does not save a request — it fetches the same thing and
then muxes it in. What embedding saves is later: a file that carries its own
captions never has to be asked about again.

## Finding out what a file holds

    ffmpeg -hide_banner -i thefilm.mkv

It will complain that you gave it no output, which is fine — it prints what it
found first. Look for the lines beginning `Stream`. Each one is a track, with
its kind, its codec, and its language where the file bothered to say.

## In short

- The extension names the **box**, not the contents.
- **MKV** holds anything and is the right choice while working.
- **MP4** plays everywhere and is the right choice for sharing.
- **MOV** is for editing, **MPG** and **TS** are for discs and broadcast,
  **AVI** is for the past.
- Subtitles are **text** or **pictures**, and only text can be read.
- YouTube's captions are always a separate fetch, whatever you do with them
  afterwards.
