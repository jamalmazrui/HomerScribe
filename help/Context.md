# Writing a context file

A context file tells HomerScribe what a film is, so that descriptions use the
right words and avoid the wrong ones. Pass it with `--context-file`.

These notes are about what works with **this** arrangement — a vision model of
about seven billion parameters, shown four frames at a time, on your own
machine. Some of it would be wrong advice for a person, and some of it would be
wrong advice for a larger model.

## The one constraint that governs everything

**The whole file goes into every prompt.** Not once at the start — every time,
for every description. A film with six hundred moments sends it six hundred
times.

So a thousand-line scene-by-scene guide is not merely wasteful. It buries the
instruction that matters under material the model must read past, and it makes
every description slower.

**Keep it under about 250 words.** If you cannot say it in 250, it is probably
not the kind of thing that helps.

## The rule that matters more than any other

**A context file supplies nouns and names. It must not supply observations.**

The difference decides whether HomerScribe is describing a film or reciting
somebody else's description of it.

"Setting: a cave on a rocky island. Present: Odysseus, his crew, Polyphemus" is
context. It gives the model the words to use for what it sees.

"A vast cave entrance; the barrier that prevents escape; his single eye; the
position of men, animals and the exit" is a description. Given that, the model
will produce it — whether or not any of it is in the frame in front of it. The
output would then be a paraphrase of the file, timed to the film, and no more
truthful than whoever wrote the file.

This is easy to get wrong, because a guide written for a human describer is
full of exactly that material, and it is the most useful-looking part.

The test: **could this sentence be false of the picture and still be in the
file?** A setting cannot — the film is either in a cave or it is not. An
observation can, and will be repeated anyway.

## What helps most, in order

**1. Vocabulary.** This is the highest-value content, and the least obvious.

A vision model shown a bronze age warship will say "a boat" unless it has been
told what world this is. Told the film is Bronze Age Mediterranean, it says
"warship", "oars", "bronze armour", "linen robe". The picture has not changed;
the words available to describe it have.

Give the nouns the film will need again and again.

**2. What not to name.** Negative instructions work well and are cheap.

"Do not name anyone who appears disguised or hooded" is one sentence, and it
prevents a whole class of error the model would otherwise make confidently.

**3. Who can be told apart, and how.** A vision model cannot recognise a face.
Giving it a name gives it something to attach, and it will attach it — rightly
or wrongly — for the length of the film.

So a name is only safe when you can say **what makes that person distinguishable
in the picture**. "The presenter is Ali Mazrui, a Black man in a light jacket who
speaks to the camera" works: there is one such person and the model can tell when
it is looking at him. "The protagonists are Odysseus and Telemachus" does not: the
model cannot tell a bearded man of forty from a bearded man of twenty-five with
any reliability, and half the film will carry the wrong name.

The test is whether a stranger could pick the person out of a still frame from
what you wrote. If not, do not give the name — say what is visible instead.

**And say to use the name every time**, not where it seems plain. Measured on a
two and three quarter hour film: the protagonist was named in 117 descriptions
of 341, and an unnamed "bearded man" appeared in 37 more, spread evenly
throughout. The model was following an instruction to name only where confident,
and its confidence varied between one frame and the next.

That is worse for a listener than either extreme. "A bearded man strides across
the deck" at 26 minutes and "Odysseus looks out to sea" at 27 give no way of
knowing they are the same man. Consistently wrong would at least be followable;
inconsistently right is not.

So tie the name to something visible and require it: "Odysseus is the bearded
man of middle years at the centre of most of the film; call him Odysseus every
time rather than describing him afresh." That turns recognising a person, which
the model cannot do, into matching an attribute, which it can.

In practice this means documentaries and lectures take names well, because there
is usually one person who addresses the camera. Drama takes them badly unless a
character is visually singular: the only woman in armour, the only man with a
staff.

A single line saying that everyone else is described by appearance and not named
is worth including whatever the material.

**4. Ambiguities to preserve.** If a film leaves something open — whether a god
acted, whether a memory is real — say so. The model resolves ambiguity by
default, because a flat statement reads more fluently than a careful one.

## What different material needs

The rules above are general; what they amount to is not.

**Documentary.** The presenter's name and appearance, and the subject's
vocabulary — the words for the things being filmed. HomerScribe already tries to
find a presenter for itself, and stating one is more reliable.

**Science and technical.** The most valuable thing you can add, and the least
obvious: say that diagrams, labels and captions appear, and that reading them
aloud is wanted. On NASA material the descriptions that carried real information
were the ones reading labels off schematics — the narration says "as you can see
here" and moves on, and the words on screen are the content.

**Drama.** Setting and period vocabulary. Names only where a character is
visually singular. Whatever the film deliberately withholds — a disguise, an
identity, an ambiguity — stated as something not to give away.

**Silent film.** Say that the dialogue appears as printed cards and that they
should be read out. This is the one case where the on-screen text is the whole of
the speech, and it is what HomerScribe does best.

**Lecture or talk.** Very little is needed and very little will help. The picture
is a person and a slide, the slide is usually read aloud, and there is not much
for a description to add.

**Anything you know nothing about.** Do not invent. HomerScribe looks the film up
for itself and will use what it finds. An empty context file is better than a
speculative one, because a wrong setting supplies wrong nouns with total
confidence.

## What does not help

**Scene-by-scene narrative without times.** HomerScribe knows the timestamp of
each moment and nothing else, so it cannot tell that 41 minutes in is "the
Cyclops's island" unless the file says so.

**With times, it can** — see the next section. A narrative map is then not only
usable but the best thing you can give it.

**Plot, themes, interpretation.** The model is describing four still frames. It
cannot use "the film explores the cost of homecoming", and the words will find
their way into descriptions as commentary, which is the one thing description
must not be.

**Anything the sound already carries.** The dialogue is transcribed separately
and the previous 25 seconds of it are given to the model anyway.

## Dividing a file by time

A heading that begins with a time marks a section that applies only from then
on:

    Christopher Nolan's The Odyssey, 2026. Bronze Age Mediterranean.
    Do not name anyone disguised.

    ## 12:00 Troy and the end of the war
    Wooden horse, burning city, Greek warriors in bronze.

    ## 41:00 The Cyclops's island
    A cave, a giant, sheep. Polyphemus is the giant.

    ## 1:12:30 Circe's island
    A stone hall on a wooded island; men and pigs.

Everything before the first timed heading is sent with every description.
A timed section is sent only while the film is inside it.

**This removes the length limit for anything you can put a time against.** A
forty-section guide costs no more per description than a four-section one,
because only the section covering this minute is sent. The 250-word limit
applies to the general part alone.

Times may be written `41:00` or `1:12:30`. The heading text is used, so give the
section a name that says where the film is.

Where do the times come from? Watching and noting them is the reliable way.
Chapter marks in the file are another. A transcript can anchor some of them —
searching HomerScribe's own `transcribed.md` for a distinctive line of dialogue
gives the time it is spoken — though this works only where the dialogue names
what the scene is about, which is less often than you would hope.

## Building one from a guide written for a person

A guide written for a human describer is the best raw material there is, and
almost none of it transfers unchanged. Working through a 1,043 line one for The
Odyssey, what came across and what did not:

**Kept, per sequence:** the setting, and who is present. Nothing else — about
twenty words each.

**Dropped:** the reasoning behind each principle, the reliability ratings, the
notes on what the describer should decide, and production facts such as where a
set was built. All of it valuable to a person; none of it something a vision
model can act on, and the production facts would be read aloud as though they
were in the picture.

**Dropped hardest, and least obviously:** the "key visual information" bullets.
They are the most useful-looking part of such a guide and the most dangerous
here, because they are descriptions. See the rule above.

Also dropped: anything phrased as advice rather than fact. "Orient the cave
before the violence begins" is a good instruction for someone writing a script
and nothing to a model describing one frame of it.

The result was 50 sections averaging fifty words, each attached to a time.

**The times are the work.** They cannot come out of the guide, and inventing
them is worse than leaving them out — a section naming the Cyclops's island
arriving ten minutes early injects wrong vocabulary with confidence. An untimed
heading is simply ignored, so a file can be filled in a few sections at a time
and used at any stage.

## A worked comparison (a narrative film)

A guide written for a human describer, for the same film, ran to 1,043 lines:
narrative sequences, reliability ratings, the reasoning behind each principle.
Every word of it is sound and most of it is unusable here — not because it is
wrong, but because it answers questions the model is not in a position to ask.

What survived into 246 words: the period and its vocabulary, three names that
can be recognised and an instruction to describe everyone else by sight, the
rule about disguises, the rule about the supernatural, and one sentence telling
it not to explain the story.

The reasoning was dropped. A person needs to know *why* not to name a disguised
character; the model needs only the instruction.

## Checking that it worked

The log answers this without guesswork.

`From here the context section is: ...` appears each time the film moves into a
new section, so you can see which applied and when.

At the end of each film, a `MEASURE:` line reports how many descriptions were
given a timed section, and **how many repeat half or more of their words from
the context**. That second number is the one to watch: a few is normal, since a
description of a cave and a context naming a cave will share words. Many means
the model is reciting rather than describing, and the file has observations in
it that should come out.

## A template

    <What it is: title, year, kind, and the world or subject it belongs to.>

    <The vocabulary of that world or subject: the things that appear again and
    again, named in the words you want used. This is the part that does most
    of the work.>

    <Anyone who can be picked out of a still frame from what you write, with
    what makes them recognisable. Then: everyone else is described by
    appearance and not named. Omit this whole paragraph if nobody qualifies.>

    <Anything that must not be named or resolved — one line each.>

    <Anything on screen that should be read aloud: labels, captions, printed
    cards. Omit if there is none.>

Nothing in that list is compulsory. A file of three lines naming a period and
its vocabulary is a real improvement on nothing; a file that guesses is worse
than nothing.
