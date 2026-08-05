---
title: Announcing HomerScribe
subtitle: Audio Description and Transcription on Your Own Computer
author: Jamal Mazrui
---

# Announcing HomerScribe

### Audio Description and Transcription on Your Own Computer

[Download the HomerScribe installer for Windows](https://github.com/JamalMazrui/HomerScribe/releases/latest/download/HomerScribe_setup.exe)

HomerScribe is free and open source. It costs nothing to try and nothing to
keep, and there is no account to make, no subscription, and nothing to pay for
later.

## The two things it does

Point HomerScribe at a video or an audio recording, on your own disk or at a web
address, and tick one box or both.

**Describe video** watches what happens on screen and says it aloud. It finds
the moments where a description can be spoken without covering the dialogue,
asks a vision model what is there, speaks the answer in a Windows voice, and
writes you a new copy of the film with that description mixed into it. You get
the described film, and you get the script of every description with the time it
is spoken, in a plain text form that reads well on a braille display.

**Transcribe audio** writes down what is said. You get the whole transcript,
with the time of each stretch of speech.

**Tick both** and you get a third thing, which is the one I did not expect to
care about as much as I do: the words of the film and the descriptions
interleaved, in the order they happen. For someone who can neither see nor hear
a film, that single document is the whole of it.

## It runs on your computer, and only on your computer

This is the part worth being clear about.

Once HomerScribe is installed, nothing leaves your machine. No part of your
video is uploaded. No request goes to any company's server. There is no account,
no login, and no key to paste in.

That also means there are no tokens. Describing a two hour film means asking an
AI model several hundred questions and giving it several hundred pictures to
look at. Done through a commercial subscription, that is a great deal of
consumption. Done here, it is your own computer working for a while, and the
count is zero.

It also means you can describe material you would not want to send anywhere: a
family recording, a confidential meeting, an unreleased film, a medical video.

## What it will cost you in disk space

HomerScribe itself is small. The AI models it uses are not, and you only need
the ones for the job you want to do.

- **Transcribing** needs about **half a gigabyte**.
- **Describing** needs about **six and a half gigabytes**, because it needs a
  model that can look at pictures as well as a service to run it in.

Both together come to about **seven gigabytes**. The installer offers each
separately, so if you only want transcripts you can skip the larger download
entirely.

They are downloaded once and kept. Nothing is downloaded again when you use the
program.

## Being honest about the quality

I would rather you hear this from me than discover it yourself.

The AI that runs on your own computer is not as capable as the AI you reach
through a commercial service. It cannot be. A model small enough to sit on an
ordinary desktop is a fraction of the size of the ones running in a data centre,
and that difference shows. HomerScribe uses the best model I have found that
still fits, and it is genuinely good; it is not the equal of what a paid cloud
service would give you.

More important than that: **audio description written and performed by
professionals is better than this, and it is better by a wide margin.** Audio
description is a craft. A skilled describer knows what matters in a shot and
what does not, when to stay silent, how to reveal something at the moment the
film reveals it, and how to say a great deal in four seconds. HomerScribe is not
that, and I do not want anyone installing it expecting that.

What it is for is everything with no description at all, which is most things. A
lecture recording. A family video. A documentary that was never described. A
film in a language whose described version was never made. For those, the
question is not whether this is as good as a professional describer. It is
whether it is better than nothing, and it is very much better than nothing.

Transcription is the stronger half. On clear speech it is close to what a person
would write down.

## How long it takes, and what makes the difference

On a computer with a capable graphics card, a feature film takes somewhere over
an hour. Transcribing alone is much quicker.

Without a graphics card, describing is slow: a single description can take
minutes rather than seconds, and a long film becomes an overnight job.
HomerScribe tells you when it has noticed this, says how long the run is likely
to take, and suggests what to change. Transcribing is perfectly comfortable
without one.

If you are choosing a computer with this kind of work in mind, the graphics card
matters far more than the processor or the age of the machine.

## It was built for us

HomerScribe is a Windows program with a dialog laid out for a screen reader,
where every field has an access key and every control says what it is. It tells
you what it is doing as it goes, without taking the keyboard away from you, so
you can work in another window while a film is being described and hear the
progress by switching back to it.

It can be run entirely from the command line as well, so it can be scripted and
given a whole folder of files to work through unattended.

## Try it

[Download the HomerScribe installer for Windows](https://github.com/JamalMazrui/HomerScribe/releases/latest/download/HomerScribe_setup.exe)

Accept the defaults. On the last page of the installer, leave ticked whatever
matches what you want to do, and let it fetch the models; that is the long part,
and it happens once.

Then run HomerScribe. There is already a short sample video in the box, so you
can press OK and watch it work before you go looking for anything of your own.

I would like to hear how it goes, particularly where it goes wrong.

Jamal Mazrui
