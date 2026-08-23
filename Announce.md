---
title: What's New in HomerScribe
subtitle: It now describes photographs, and it reads a film's own captions
author: Jamal Mazrui
---

# What's New in HomerScribe

### It now describes photographs, and it reads a film's own captions

[Windows installer](https://github.com/JamalMazrui/HomerScribe/releases/latest/download/HomerScribe_setup.exe)
 · [Project on GitHub](https://github.com/JamalMazrui/HomerScribe)

HomerScribe describes video and transcribes speech on your own computer,
without sending anything anywhere. I wrote about it here a while ago. Two
things have been added since, and both are worth a few minutes of your time.

It is free, and it is open source. There is no account to make and nothing to
pay for later.

## It can now describe a folder of photographs

This is the bigger of the two changes.

Point HomerScribe at a zip file of pictures instead of a video, and it looks at
each one and writes down what it sees.

You get three things back:

- **Each picture, with the description written inside the file.** Not in a note
  beside it, and not in some program's database. Inside the picture, where it
  travels with the file wherever you send it.
- **The same pictures again under new names**, in a second zip. `IMG_4471.JPG`
  becomes `Elderly woman in a blue floral dress beside a dry stone wall.jpg`.
- **A document listing every picture** and everything now stored in it.

The description goes into several places at once, because different programs
look in different places. Two of them are the fields that **Windows Explorer
shows and a screen reader reads out of a file's Properties**. Select a picture,
open Properties, and the description is simply there.

Two others are fields the picture industry added in 2021 for exactly this
purpose. They are what a web page should use as alt text. Not much software
fills them in yet. HomerScribe does.

Forty-nine photographs took under three minutes on my machine.

### You can tell it what it cannot know

A model can see that a picture holds three men and an older woman at a table.
It cannot know who they are.

So HomerScribe reads the file name. If a picture is called
`Jeannie & Jim - Lake Tahoe.jpg`, it takes those names as right, because
somebody who was there typed them. You can also drop a short note into the zip
saying who tends to appear and how to tell them apart.

It will not name anyone it cannot actually match to what is in front of it. A
guessed name is worse than no name, because you cannot tell the two apart.

### What it cannot do

Some picture formats have nowhere to store a description. A GIF has only a
plain comment field. A BMP has nothing at all. Those pictures are still
described and still renamed, and the document says which they were.

## It now reads a film's own captions

Many films already carry their words, either inside the file or in a caption
file beside it. Where a **person** wrote those captions, HomerScribe now uses
them instead of listening to the film.

That is better than anything a machine can produce, and not only at the words:

- A person writes down **who is speaking**.
- A person writes down **what can be heard but not spoken** — a door slamming,
  music starting, laughter in the audience.

That second one matters most. For someone who can neither see nor hear a film,
the transcript is the whole of it, and until now it held nothing that was not a
word.

There is a practical gain too. If you only want the words, HomerScribe no
longer downloads the film at all. Four videos, transcribed, in under a minute.

**Captions a machine made are a different matter.** YouTube's automatic ones
carry the words and nothing else — no speaker, no sounds, and punctuation only
where the software guessed. HomerScribe can tell the two apart, and for a
machine-made track it listens to the film instead, which produces better
sentences. The transcript always says which it was.

## Being honest about it

Transcription is the stronger half. On clear speech it is close to what a
person would write down, and where a person wrote the captions it is not a
machine's work at all.

Description is harder, and it is worth being plain about that. The model
describes what a frame holds. It does not always know what matters in it. It
can tell you that a man is standing in a doorway; it cannot always tell you
that this is the moment everything turns on.

I also tried to make captions improve the descriptions, by handing the model
the list of people the captions name. So far that has not worked, and I am not
going to claim it has. Knowing that someone called Anil Seth speaks in a film
does not help a model pick his face out of three. That work continues.

None of this replaces a described version made by people who do it for a
living. It is for the far larger number of films and photographs where nobody
is going to do that, and the choice is this or nothing.

## It stays on your computer

Nothing is uploaded. No account, no key, no subscription, no company seeing
your family photographs. It needs a reasonably modern machine and some disk
space for the models, and after that it costs nothing to run.

It stands on other people's work — ffmpeg, Whisper, Ollama, ExifTool, yt-dlp —
all free and open source, and it would not exist without them.

## Try it

- [Windows installer](https://github.com/JamalMazrui/HomerScribe/releases/latest/download/HomerScribe_setup.exe)
- [Project on GitHub](https://github.com/JamalMazrui/HomerScribe)

Accept the defaults. On the last page of the installer, leave ticked whatever
matches what you want to do, and let it fetch the models. That is the long
part, and it happens once.

Then run it. There is a short sample video in the box, so you can press OK and
watch it work before you go looking for anything of your own.

I would like to hear how it goes, and particularly where it goes wrong.

Jamal Mazrui
