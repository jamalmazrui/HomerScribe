# HomerScribe History

## 1.0.182, 22 August 2026

- **`License.md` now names HomerScribe and its author**, which it did not. It
  opened with a bare "# License" and the MIT text, and the only place the
  program appeared was the copyright line.
- It now opens by saying that **HomerScribe was written by Jamal Mazrui, its
  author and developer**, and is released under the MIT License, followed by a
  plain-English summary: use it, change it, sell it, keep the notice, no
  warranty.
- **The MIT text itself is untouched, word for word**, and the file says so and
  says why: it is left exactly as it is written everywhere else so that a
  person, a legal team, or a program that scans for licenses recognises it at
  once. Rewriting a standard license to mention your own program is how a
  standard license stops being one. A line above it explains that where the
  text says "the Software" it means HomerScribe — the program, its source, and
  the documents that come with it.
- Checked before delivery by normalising the whitespace and comparing the body
  against the canonical MIT wording: identical.

## 1.0.181, 22 August 2026

- **`Announce.md` rewritten to announce the CHANGES rather than the program.**
  It had grown by having new sections added to an announcement of the whole
  thing, so the two new features sat second and third of nine headings. For a
  post telling people what is new, that is the wrong shape. It now opens with
  the pictures, then the captions, then the honest limits, and it is 1,018
  words rather than 1,700.
- **Reading level measured rather than assumed.** Flesch-Kincaid across the
  documentation set: `Announce.md` was already 7.6 and is now **6.0**;
  `ReadMe.md` 8.5, `HomerScribe.md` 8.0, `Hotkeys.md` 2.9, `History.md` 8.2,
  `Developer.md` 8.3. So nothing was above ninth grade to begin with; what
  needed fixing was the framing, not the words.
- The tone is deliberately humble: it says plainly that trying to improve
  descriptions with caption speaker names **has not worked yet**, that
  description is the weaker half, and that none of this replaces a described
  version made by people who do it for a living.
- Every claim in it was checked against the source before delivery — eleven of
  them, each traced to the function that does the thing.

## 1.0.180, 22 August 2026

The documentation is now laid out the way Jamal wants every Homer Tools app
documented, which he set down today as a standing preference rather than a
one-off request.

- **`ReadMe.md` is now the short way in**: what HomerScribe is, what you need,
  how to install it, a Quick Start, and a list of where everything else lives.
  It was 1,440 lines and is now under 200.
- **`HomerScribe.md` is new — the complete guide.** Every setting, every
  document it writes, what it does with captions and with pictures, and what to
  do when something goes wrong. That is the 31 sections that used to make
  `ReadMe.md` unreadable as an introduction.
- **`Hotkeys.md` is new**, listing every key three ways: by what you want to
  do, by where you are when you press it, and by the key itself. Built from the
  actual bindings in the source rather than from memory — the twelve `Alt`
  access keys in the dialog, and the editing, reading and navigation keys the
  accessible dialog provides.
- **Every `.md` now has a matching `.htm`**, made with Pandoc, with a table of
  contents and a real page title. Nine of each.
- The installer packages `HomerScribe.md`, `Hotkeys.md` and `video_formats.md`
  along with their `.htm` companions.

## 1.0.179, 22 August 2026

Two faults his four new runs exposed, one of them mine from yesterday.

- **The nearby-speaker hint fired for the first time — and carried NARRATOR.**
  His logs read *"GPS and FORTIER and NARRATOR speaking around 22:02"*. A
  narrator is never in shot, and GPS is not a person at all. `spokeAround` was
  taking any speaker label, while the cast list applied a filter that rejects
  exactly those. Nearly every hint carried a narrator, turning the useful
  single-name case into a three-name guess. **The same filter now applies to
  both.** On his eleven real hints that takes the ones naming exactly one
  person from one in eleven to three in eleven.
- **The cast list stopped being logged**, because when it moved out of `lNames`
  in 1.0.178 its log line was left reading `lNames`. Six runs later there was
  no way to tell from a log whether it had reached the model at all. Moving a
  thing and leaving its evidence behind is the same mistake as trusting an exit
  code. It now logs from the roster itself.
- `Announce.md` gains the **Windows installer** and **Project on GitHub** links
  at the top as well as at the foot, and says plainly that captions a person
  wrote are used while captions a machine made are not.
- `Review.md` gains a measured account of what the runs show: transcription is
  better for the caption work, descriptions are not yet, and the announcement
  says only the first.

## 1.0.178, 22 August 2026

He asked why speaker names from a person's captions were not adding value, and
he was right to. My previous answer — that names without appearances cannot be
attached to faces — was true as far as it went and beside the point. **The
names were never offered to the model as names.**

- The roster was poured into `lNames`, which the prompt renders as *"Names you
  have already used in this film"*, followed by *"if somebody here matches how
  one of those was DESCRIBED, use that name again"*.
- That paragraph is for CONTINUITY: names the model coined itself, whose
  appearance it knows from its own earlier sentences. For a name off a caption
  track none of it holds — the model has never used it and there is no earlier
  description to match against, so the instruction is unanswerable. Handed a
  list labelled as its own prior work when it had done no such work, the model
  did the only sensible thing: nothing.
- **The roster now has its own paragraph**, saying what it is: the film's cast
  list, spelled as its makers spell it, which does NOT say who is in this shot.
  A name is to be used only where the model can actually tell — because it was
  told what that person looks like, or because the picture itself says so, such
  as an on-screen caption naming them. Never because a name is the only one
  left.
- **Whoever spoke near the moment is now its own paragraph too**, and last,
  because a model weighs the end of a prompt most. It is the one piece of
  caption evidence tied to this moment rather than to the film as a whole:
  where exactly one person is named there and exactly one person is in shot,
  saying who it is will usually be right.
- **That was being looked for in the dialogue window**, twenty-five seconds,
  which is sized for quoting the line just spoken so a description does not
  repeat it. "Who is in this scene" is a scene-sized question, so it has its own
  setting, `--speaker-window`, at two minutes either way. More than three names
  in the window and nothing is said at all: a dozen is a cast list, and offering
  one invites the guessing the prompt forbids.
- Being straight about which of these is proven: **the mislabelling is a
  definite bug with a definite fix**. The window is a reasoned adjustment whose
  effect is not yet demonstrated — the five description moments in his NOVA
  film had caption cues nearby at any window size, so what was missing was a
  cue that named somebody, not a cue. The new log line — *the captions have X
  speaking around 3:16* — makes the next run answer it.
- New behaviours: `roster-named-as-a-cast-list`, `speaker-window-scene-sized`.

## 1.0.177, 22 August 2026

- **"Done" was spoken twice**, and the reason is in a comment I wrote myself in
  `flushAnnouncements`: a screen reader reads a dialog's title, then reads the
  dialog — title and all — when focus lands on it, so anything in the title is
  heard twice. The title is the CATEGORY. I passed "Done" as the category AND
  "Done" as the message, walking straight into the thing that comment exists to
  prevent. **The message is now what finished** — "Done. Introduction to Web
  Accessibility" — which is the useful half anyway.
- **Nothing said what the film was before the work began.** The opening added
  in 1.0.173 is spoken INSIDE `described.mkv`, so it is only heard when the
  finished film is played, and only when describing was asked for at all. He
  was listening for it during the run, where the last thing said was
  "Downloading" and then a long silence. **The title, the publisher and the
  running time are now announced once**, when the film is in hand and before
  either job starts.
- **Captions a person wrote are now trusted over Whisper; captions a machine
  made are not.** He asked whether the two can be told apart. They can, and
  HomerScribe always could — `looksAutomatic` decides from per-word timing tags
  and from cues that roll up the screen repeating the line before, and his logs
  have been saying "written by a person" or "made automatically" all along.
  What it did not do was act on the difference: any captions at all displaced
  Whisper.
- The two are not comparable. **A person's captions carry the words, who says
  them, and the sounds that are not speech** — a door, music starting,
  laughter. No machine produces that last part, and it is the one a deafblind
  reader cannot get any other way. **A machine's captions are the words and
  nothing else**, with punctuation only where the recogniser guessed, and
  Whisper is at least as good at the words and better at the sentences. So for
  a machine-made track the richer transcript is the one that listens.
- `--auto-captions` (default no) accepts them anyway. `--captions` is unchanged.
- New behaviours: `film-announced-before-work`, `person-captions-only`.

## 1.0.176, 22 August 2026

- **`descriptions.md` is now `described.md`**, matching `described.mkv`. It was
  the odd one out among the documents.
- **There is one in the output folder and one inside the archive**, and they
  are not copies of each other. The folder holds the pictures under their
  original names and the archive holds them under their new ones, so each
  document lists its own copies by the names they actually carry there.
- **The content is now what is IN each picture, not what HomerScribe put
  there.** A heading per file, then every field that has a value, sorted by
  field name without regard to case, with the group each field belongs to in
  brackets so a bare name like `Description` tells you where it lives.
- Anything that was already in the file is listed beside what HomerScribe
  wrote — the camera, the date, the lens, somebody else's caption — because the
  question a reader has is what this file says about itself, not what this
  program did to it.
- **Read back out of the finished files with ExifTool**, not assembled from
  what was sent to them. Same reason as the counts: what was sent is a hope,
  what reads back is the fact. This is now true of every figure and every
  field HomerScribe reports about a picture.
- The `System` and `ExifTool` groups are left out: file dates, permissions and
  the folder a file happens to sit in belong to the disk rather than the
  picture, and they change every time it is copied.
- The old report is gone, and with it `metadataStanding`, which existed only to
  phrase a line in it. The descriptions themselves are not lost — they are in
  the files, in `ImageDescription` and `Caption-Abstract` and the rest, which
  is where they were wanted and where the new document reads them from. What
  that document cannot show is the things that never became pictures, so the
  notes and the passed-over files are named in a short closing section.
- New behaviours: `described-md-in-both-places`, `fields-read-back-not-assumed`.

## 1.0.175, 22 August 2026

He asked whether captions improved the descriptions. The 08:20 run is the first
that can answer it: 39 speakers in the roster, and five descriptions placed.

- **The answer is no, and now it is measured rather than argued.** Not one of
  the real names — Susana Martinez-Conde, Thalia Wheatley, Anil Seth and the
  rest — appears in any of the five descriptions.
- **The only roster entries that did appear were MAN and WOMAN**, which the
  descriptions would have used anyway. That is a bug, not a result. Subtitles
  for the deaf label an unidentified speaker by what they are: MAN, WOMAN,
  MAN 2, SECOND WOMAN, REPORTER. Those were going into the roster as though
  they were people's names, which tells the model that "MAN" is a name this
  film uses and invites it to read a generic word as an identification. They
  are now kept out, along with roles like HOST, DOCTOR and COMPUTER. On his own
  NOVA roster that removes seven entries and keeps twelve.
- **Why the real names went unused is the thing I predicted and can now show.**
  A roster says who exists. It says nothing about what any of them looks like,
  so the model has no way to attach a name to a face — and it correctly
  declines to guess, because it is told not to. The captions supply the WHO;
  only a context file supplies the WHAT THEY LOOK LIKE; a name gets used where
  the two meet, and here only one of the two was present.
- **The nearby-speaker hint fired zero times**, which is also explicable rather
  than broken. NOVA is wall-to-wall narration: five descriptions in
  fifty-four minutes, each placed in one of the rare silences. A labelled
  caption cue was never close enough to one of those silences to fall inside
  the dialogue window.
- The new spoken opening worked: *"Your Brain: Who's in Control? | Full
  Documentary | NOVA | PBS. Published by NOVA PBS Official. 54 minutes long.
  Audio description is on."*

## 1.0.174, 22 August 2026

The format test ran properly this time, and the log answered two questions
without anybody having to look.

- **The accessibility fields work on his 2019 ExifTool.** The self test picked
  the TIFF this time rather than the BMP, and the log shows both fields NOT
  written when asked plainly, then both written with the definitions supplied.
  `IPTC:Caption-Abstract` also reads as written, which confirms the punctuation
  fix in 1.0.172.
- **ExifTool 11.79 cannot write WebP at all**: *Writing of WEBP files is not
  yet supported*. WebP writing came long after that version. So one of my
  format lists was wrong for this copy — and the honest fix is not to correct a
  list, because which formats can be written depends on the version in use.
  **HomerScribe now learns it from ExifTool during the run**: a picture refused
  that way is counted as a format with nowhere to put a description, exactly as
  a BMP is, rather than reported as a failure. The log says so and adds that a
  newer ExifTool may manage it.
- **Names were still being cut mid-phrase.** "A bright yellow sun rises behind
  two dark green triangular hills against a" stops in the middle of a thing.
  Cutting at the last space before the limit is not enough — wherever the cut
  lands, the last word has to be one that can end a phrase. The backing-off
  that `trimToPhrase` already did is now shared with the length cut in
  `friendlyName`, so no route to a name can leave a dangling article or
  preposition. That one becomes "...two dark green triangular hills", and the
  other two test names come out at 75 and 76 characters.
- The rest of the format test passed as predicted: 4 pictures, 2 notes and 1
  other file; the SVG named as a drawing; both notes found and used; the
  camera-junk GIF name correctly yielding no context.

## 1.0.173, 22 August 2026

Six things his two runs turned up. Three of the four predictions for
`test.zip` were right; the fourth was not, and the metadata result was a false
alarm caused by my own test.

- **The self test picked the wrong picture.** It used the first in the archive,
  and in `test.zip` that is a BMP — a format ExifTool refuses outright:
  *Writing of BMP files is not yet supported*. So every field came back NOT
  written and it announced that the accessibility definitions had failed, when
  the same definitions had worked perfectly on a JPEG an hour earlier. **It now
  picks the first picture whose format can hold the full set**, and says so
  where none can. Nothing was ever wrong with the writing.
- **The camera-junk name got through**: `IMG_20240115_WA0042.gif` was offered to
  the model as context. Same trap as the housekeeping words a day earlier, one
  step earlier in the same function — the camera pattern ran while the
  underscores were still there, and `_` is a word character, so
  `\bIMG[\s_-]?\d+\b` cannot match across `IMG_20240115_`. **Separators are now
  normalised before any pattern looks at the name**, which is where that line
  should have been from the start. Five camera forms added to the checks.
- **And twenty of the forty-nine names mostly repeated the file's existing
  name**, which is the real answer to why so little looked renamed.
  `Jamal_Mazrui_signature.jpg` became "Jamal Mazrui Signature" — tidier, and
  saying nothing new. The cause is the file-name context added in 1.0.163: the
  file name is handed to the model as authoritative, and the model hands it
  straight back as the name.
- Replacing such a name with the description would lose the names, and the
  names are the one part no model could have worked out. So a short name is
  now **kept and extended**: "Jamal Mazrui Signature, handwritten signature on
  a white sheet of paper". The prompt also says outright not to give back the
  name the file already has — use its people and places, but say what is
  visible.
- **The names were too short** — 21 to 25 characters against an allowance of 79,
  and he was right to say so. Two changes. The prompt now asks for **eight to
  fourteen words** rather than naming a character ceiling, because a ceiling is
  permission to stop rather than something to aim at. And where an answer comes
  back in fewer than five words, the name is taken from the description
  instead, cut at a phrase rather than a word count — `trimToPhrase` prefers a
  comma and backs off any trailing joining word, so a name no longer ends "in a
  pale blue". On his four test pictures that turns 21–25 characters into 50–68.
- **The spoken opening now names the film.** It has always said only "Audio
  description is on." He noticed the film's own closing credits named NOVA
  while nothing at the start did. The documents have opened with the title, the
  publisher and the running time since 1.0.150; the spoken opening never did.
  It now gives the title, who published it, and how long it runs — the running
  time in words, since a clock reading would be spelt out digit by digit.
- **"Done" between one source and the next.** After a long silent stretch the
  only signal was the next "Processing" line, which says a new thing has begun
  without ever saying the last one ended. Not said where a source failed or was
  skipped: those have spoken for themselves, and "Done" after "Error" would be
  a lie.
- **The first field's text is selected when the dialog opens.** Windows
  convention for a data entry field, and Microsoft's own guidance: highlighting
  the value lets you type or paste over it, or press Tab to leave it alone.
  WinForms does this when a box is reached by Tab but not when it is simply
  made the active control before the form opens, which is how this one starts —
  so it is done explicitly, on Shown, and only for an editable box with
  something in it.
- **Two counts of the same thing disagreed**: the results box said "2 of a kind
  that cannot hold one" while the log said "1 of a kind with nowhere to put
  one". A GIF is neither — it holds a plain comment — and only formats with
  nowhere at all belong in that figure. Counted once now.
- The rest of the format test passed: 4 pictures, 2 notes and 1 other file; the
  SVG named as a drawing rather than passed over as junk; both notes found.
- **And the caption speaker roster fired for the first time**, on the NOVA
  films: *The captions name 38 speakers* on one and 26 on the other.
- New behaviours: `opening-names-the-film`, `fuller-picture-names`,
  `done-between-sources`, `source-box-selected`.

## 1.0.172, 22 August 2026

- **It works, and on his 2019 copy.** The self test settled the question it was
  built for. ExifTool 11.79, asked plainly, refuses both accessibility fields:

      XMP-iptcCore:AltTextAccessibility: NOT written
      XMP-iptcCore:ExtDescrAccessibility: NOT written

  Given HomerScribe's own definitions with `-config`, the same copy writes them:

      XMP-iptcCore:AltTextAccessibility: written
      XMP-iptcCore:ExtDescrAccessibility: written

  And the run finished **49 pictures described, 49 carrying their descriptions,
  49 of them in the IPTC accessibility fields**, counted by reading the files
  back rather than by trusting an exit code. He was right that a current
  ExifTool was not needed, and the version requirement is gone for good.
- The single-file rule held too. The log shows the winget copy passed over by
  name — *it needs an exiftool_files folder beside it* — and his own
  single-file 11.79 chosen instead.
- **One line of that self test was still wrong, and it was my parsing.**
  `IPTC:Caption-Abstract` was reported "NOT written" on every pass while being
  written perfectly well. ExifTool's `-s` prints the tag's own name, and IPTC's
  is `Caption-Abstract`, hyphen and all; the check stripped the hyphen from what
  it looked for but not from what ExifTool printed, so it never matched. Tag
  names are now compared with the punctuation taken out of both sides.
- That the report was the thing at fault rather than the work is the same shape
  of error as counting exit codes, and it is worth noticing that the self test
  is what exposed it.

## 1.0.171, 21 August 2026

- **One file, and only one file.** `exiftool.exe` must be a self-contained
  binary with nothing beside it — no `exiftool_files` folder, no Perl DLLs.
  His requirement, and it now governs the build, the installer and the
  program.
- **This rules out everything currently published, and it is worth recording
  why so nobody quietly undoes it.** The exiftool.org download, the SourceForge
  download and the winget package `OliverBetz.ExifTool` are all the same thing:
  a small launcher plus an `exiftool_files` folder holding Perl. The
  `Image-ExifTool` tarball is Perl source and needs Perl installed. **Nobody
  publishes a current single-file build.** The one-file form is the older
  exiftool.org format, a packed archive, and the launcher replaced it
  deliberately — unpacking a packed archive on every invocation is slow, and
  HomerScribe runs ExifTool once per picture.
- A third-party single file does exist on GitHub. It is a personal archive with
  no following whose own README points at somebody else's releases, and it is
  not something to hang a build on.
- **None of this costs anything**, which is the point. Since 1.0.168
  HomerScribe carries the definitions of the two IPTC accessibility properties
  itself and hands them to ExifTool with `-config`, so an older single-file copy
  writes all nine fields. His existing 11.79 is a perfectly good answer.
- So the build no longer fetches anything. It looks for a single-file copy — in
  its own folder, `C:\HomerScribe`, and the installed program folders — says
  which it found and how old it is, and **deletes any `exiftool_files` folder a
  previous attempt left behind**. The installer packages the one file and not
  the folder.
- **The program passes over any ExifTool with an `exiftool_files` folder beside
  it**, naming it in the log, so the rule is visible rather than silent.
- `installExifTool.cmd` no longer pretends it can fetch one. It moves a
  single-file copy into place if there is one, and otherwise says plainly what
  is wanted and why nothing can be downloaded.
- New behaviour: `single-file-exiftool-only`.

## 1.0.170, 21 August 2026

- **The build could not fetch ExifTool, and his log says exactly why.** Both
  download attempts came back "Too small to be the archive". That message is
  the one part of the attempt that worked: the size check caught that what
  arrived was not a zip.
- **SourceForge's `/download` address serves a web page, not the file** — one
  reading "Your download will start shortly", with the real file behind a
  redirect. What landed was a few tens of kilobytes of HTML. And exiftool.org
  404s, because it now points its download links at SourceForge and says so on
  its own front page.
- **So the build asks winget first, and it should have all along.** `winget
  install -e --id OliverBetz.ExifTool` is the supported way to install ExifTool
  on Windows. winget knows the current version, knows where to get it, and
  handles the mirrors. Scraping a download page knows none of those things.
  After it runs, the build copies the result in from wherever winget put it.
- A direct fetch remains as a last resort, and is now aimed correctly: Oliver
  Betz's own installer first — which is what winget would have fetched anyway —
  then `downloads.sourceforge.net`, which is the real download host rather than
  the page that advertises it. The size check stays, since it is what caught
  this.
- `installExifTool.cmd` gets the same chain.
- One note for whoever edits these scripts next, mine included: **anchor on the
  line, not the substring.** Locating a block by `:exifToolDone` matches inside
  `goto :exifToolDone` too, and that is how a duplicated block got into
  `installExifTool.cmd` earlier today. Both edits here were line-anchored and
  both were checked for undefined and duplicated labels afterwards.

## 1.0.169, 21 August 2026

- **No Windows ExifTool needs Perl installed.** Worth saying plainly, because
  the worry would have kept a better copy out of use. Both Windows
  distributions carry Perl inside them: the exiftool.org package keeps it in
  the `exiftool_files` folder, and the self-contained build has the whole of
  Perl packed into one executable. What *does* need Perl installed is the
  `Image-ExifTool` source tarball, which is the Unix and Mac route, and that is
  the part of the install page that mentions it.
- **`winget install exiftool` resolves to `OliverBetz.ExifTool`** — an Inno
  Setup installer of a self-contained package, currently 13.59, by the same
  Oliver Betz whose launcher the official exiftool.org packages already use. It
  is not more cumbersome than the 2019 copy; it is the same shape of thing,
  four years newer, and it is the one to use.
- **HomerScribe now finds every ExifTool on the machine, runs each one, logs
  them all with their versions, and chooses the newest** — saying which and
  why. The places searched cover winget's two habits, a real install under
  Program Files or the user's Programs folder and a shim under `WinGet\Links`,
  as well as the package folder it unpacks into, HomerScribe's own folder, and
  the PATH. Guessing where winget puts things is how that question gets
  answered wrongly; the log now answers it.
- **A bug my own test caught before delivery.** Comparing versions as decimals
  makes 13.8 look newer than 13.11 — but ExifTool released 13.11 *after* 13.8,
  so the older copy would have been chosen. Each part is now compared as a
  whole number.
- `buildHomerScribe.cmd` and `installExifTool.cmd` look in the same places, and
  the failure advice now offers the winget line and says that no Perl is needed.
- New behaviour: `newest-exiftool-chosen`.

## 1.0.168, 21 August 2026

He asked three things, and was right about all three.

- **"Are you using correct command-line syntax?"** Yes, and it should have been
  written down. Writing is `exiftool -@ <argfile>` with `-overwrite_original`,
  `-m` and `-charset UTF8`, one `-TAG=value` line per field — an argument file
  rather than a command line, because a description holds quotation marks,
  ampersands and accented letters. Reading is `exiftool -j -q -m -charset UTF8
  -TAG -TAG ... <folder>`, parsed as JSON. **ExifTool does both halves**, and
  the count reported comes from the reading half.
- **"I am surprised the latest ExifTool is really needed."** He was right and I
  was wrong. The two properties genuinely are new — the IPTC added them in
  October 2021 and his copy is from October 2019 — but concluding that an older
  ExifTool therefore *cannot write them* was a mistake. **ExifTool has been able
  to write tags it does not know for far longer than these tags have existed**,
  given their definition. Its own documentation says any namespace may be
  written by giving a family 1 group name, "including namespaces which are not
  pre-defined by ExifTool".
- **So HomerScribe now carries the definition itself.** When a copy does not
  recognise the fields, a small configuration file is written naming the
  `Iptc4xmpCore` namespace, its URI as the IPTC publishes it, and the two
  properties as `lang-alt`, and the write is repeated with `-config`. The same
  bytes land in the same place a current ExifTool would have put them. **The
  version requirement is gone**; a newer copy is merely preferred.
- The definitions were checked before delivery: extracted from the compiled-in
  text, passed through `perl -c`, and evaluated to confirm the namespace URI,
  the group names and both properties as `lang-alt`.
- **"Do not ask me to check things manually."** Fair, and it had happened more
  than once. **HomerScribe now self-tests before the first picture**: it copies
  that picture aside, writes every field into the copy, reads them all back, and
  logs each one BY NAME as written or not written, with the version in use and
  the commands run. Then it chooses how to write the rest and says which way it
  chose and why. One log now answers every question I had been putting to him.
- New behaviours: `metadata-self-test`, `accessibility-tags-taught`.

## 1.0.167, 21 August 2026

- **The read-back worked, and it caught the thing it was built for.** The 19:03
  run reports 49 of 49 pictures carrying their descriptions, verified by
  reading the files, and says plainly: *none of them in the IPTC accessibility
  fields*. The version line explains why. ExifTool **11.79**, from 2019; the
  fields were added at 12.41.
- **And that is my fault from 1.0.165.** I made the build prefer a copy of
  ExifTool already on the machine over downloading one. Right in principle, and
  it is what the ffmpeg step does. But his copy was four years old, so
  preferring it is precisely what kept the accessibility fields missing — the
  optimisation defeated the fix it was sitting next to.
- **The build now asks how old a copy is before preferring it.** Anything below
  12.41 is passed over and a current one fetched. `installExifTool.cmd` does the
  same. Versions are compared as version numbers, not as decimals, because 11.9
  and 11.79 compare the wrong way round otherwise.
- If the copy in use is still too old, the build log says so in as many words
  rather than leaving it to be discovered in a run.
- `Announce.md` now describes the picture feature as it actually is, the
  paragraph saying metadata was not written yet having stopped being true.

## 1.0.166, 21 August 2026

- **It worked, and it reported more than it had done.** The 18:45 run wrote
  descriptions into 48 of 49 pictures and made `images.zip` — but the figure
  "48 carrying their descriptions inside them" was my code trusting an exit
  code, and the log shows what that missed:

      Warning: Tag 'XMP-iptcCore:AltTextAccessibility' is not defined
      Warning: Tag 'XMP-iptcCore:ExtDescrAccessibility' is not defined

- Those are WARNINGS. ExifTool still exits zero. So the two IPTC accessibility
  fields — the whole reason for choosing ExifTool — were quietly dropped from
  every one of the 48, and HomerScribe called each of them a success. It only
  came to light because one file ALSO had a real error, so its output was
  printed. The tag names are right; that copy of ExifTool simply predates them,
  and **which copy was in use had never been logged**.
- **The count is now read back out of the files.** After the writing, one
  ExifTool call over the whole folder asks what is actually in those pictures,
  and that is the number reported. Where it disagrees with the number of
  successful writes, both are logged and the read-back one is used. An exit
  code says a program finished; it does not say the work was done.
- **ExifTool's version is logged**, once, at the start. Nothing above could
  have been diagnosed without it.
- **`-m`, ignore minor errors.** The one refusal was a PNG with `IFD0 pointer
  references previous IFD0 directory` — damage already in the file and nothing
  to do with the description being added. That picture now gets its description.
- **Where the accessibility fields are not supported they are dropped, the
  write is retried without them, and the log says so plainly** — that ExifTool
  is too old, that the caption, title and comment fields are still written, and
  that `installExifTool.cmd` will fetch a current copy. Better a smaller true
  claim than a larger false one.
- New behaviours: `metadata-read-back`, `exiftool-version-logged`,
  `minor-errors-ignored`.

## 1.0.165, 21 August 2026

- **The ExifTool fetch was asking the wrong server**, and his build log caught
  it: every attempt came back `Not Found`.
- exiftool.org says so on its own front page, twice. The site is a *"temporary
  stop-gap"* because its host disabled it, and *"the download links now point
  to SourceForge"*. My script read the right FILE NAME off that page and then
  built the wrong BASE address. The archive lives at
  `sourceforge.net/projects/exiftool/files/`, and three places are now tried in
  turn. The version number is taken from `exiftool.org/ver.txt`, which is
  published for the purpose and steadier than a pattern over a page.
- **There are two kinds of `exiftool.exe`, and the test was wrong for one of
  them.** The packaged one is a small launcher needing an `exiftool_files`
  folder beside it. Oliver Betz also publishes a self-contained build of about
  eight megabytes that needs nothing beside it -- and that is the one Jamal
  already had. Testing for the folder would have made every build fetch again
  on a machine with a perfectly good copy. **The test is now that the program
  RUNS**, which is the only thing that actually matters.
- **A copy already on the machine is used before anything is downloaded**, the
  same courtesy the ffmpeg step has always paid: the build folder, then
  `C:\HomerScribe`, then the installed program folder. His copy would have been
  found on the first try.
- `installExifTool.cmd` carries all of the same corrections, and its message on
  failure now tells the truth about where to go and says that a self-contained
  build is equally welcome.

## 1.0.164, 21 August 2026

- **The picture feature is finished.** Phases two, three and four.
- **The description is written into the picture**, with ExifTool, which the
  build now fetches and the installer packages. The same words go into several
  places on purpose, because different software looks in different ones:
  `AltTextAccessibility` and `ExtDescrAccessibility`, the two fields the IPTC
  added in 2021 for exactly this; `dc:Description` and `Caption-Abstract`,
  which most photo software displays; and `XPTitle` and `XPComment`, which are
  what Windows Explorer shows and a screen reader reads out of a file's
  properties. That last pair is the one that matters at a Windows machine.
- Arguments go to ExifTool in a file rather than on a command line. A
  description holds quotation marks, ampersands and accented letters, and every
  one of those is a way for a command line to go wrong.
- **Two copies of every picture, both carrying the description.** The output
  folder holds them under their original names; `<archive>.zip` holds them
  under their new descriptive ones. Described once, metadata written once, then
  copied, so the two are identical apart from the name and there is no second
  pass to drift.
- **Every format accounted for.** PNG, JPEG, WebP and TIFF take the full set of
  fields. GIF takes a plain comment and nothing named. BMP has no metadata
  container at all. SVG is a drawing the model cannot be shown, and is now
  named as that in the log rather than passed over as though it were junk --
  its own title and desc elements would be the best home of any format here,
  and it is worth coming back for.
- **ExifTool is bundled at his instruction**, so `License.md` names it and
  points at its source. It is under the same terms as Perl -- the Artistic
  License or the GPL, the redistributor's choice. HomerScribe's own MIT terms
  are untouched: it runs ExifTool as a separate program and never links it.
  `installExifTool.cmd` remains for a machine where the bundled copy is
  missing.

### What Rentitle taught

He sent his own file-renaming program, which does this job from document
metadata. Six of its rules were missing here, and every one is the kind of
thing learnt from a file that broke something.

- **Substitute, do not delete.** A colon becomes " - ", an ampersand " and ", a
  bracket a parenthesis. Blanking them, as this was doing, turned "Jeannie &
  Jim" into "Jeannie Jim" and lost the word.
- **The illegal list is longer than Windows says.** Rentitle bans a long run of
  smart quotes, box-drawing characters, bullets and accented letters, with a
  comment recording that they stopped file-not-found errors in Python and in
  FileDir. Every one is LEGAL in a Windows file name. They break other things
  anyway.
- **Some titles are not titles.** Rentitle refuses "untitled", "none", "Title",
  "Presentation1". A vision model hands back "Image" and "Photo" just as
  readily, and a folder where every picture is called Image is worse than one
  full of IMG_4471. Such an answer is now refused and the name taken from the
  description instead.
- **A name of digits is not a name.**
- **A leading dot hides the file**; it becomes "Dot".
- **There is a limit to trying.** Rentitle stops after a thousand collisions.
- One difference worth recording: Rentitle pads its suffix to three digits
  always. He asked here for the fewest zeros that will do, so that is what this
  does. The newer instruction wins, but the two are not the same rule.

- New behaviours: `metadata-written`, `renamed-copies-archived`,
  `placeholder-names-refused`, `wider-illegal-letters`.

## 1.0.163, 21 August 2026

- **The names came right.** Twenty-four of twenty-four now read as phrases with
  spaces, none run together, none with underscores, the longest 76 characters
  and the middle one 27. "Elderly woman in a blue floral dress by a stone wall"
  where it used to say "OutdoorElderlyLady".
- **As few leading zeros as will do**, as he asked. The width is taken from the
  LARGEST clashing group in the archive: a pair gets `-1` and `-2` however many
  pictures there are, and a group of twelve gets `-01` to `-12`. Not from the
  number of pictures, since only names that clash ever sit together and only a
  clashing group has to sort. Not from each group separately either, so every
  numbered name in one folder has the same shape rather than `-1` sitting
  beside `-01`.
- **The original file name is now offered as context.** "Jeannie & Jim - Lake
  Tahoe.jpg" was named by somebody who was there, and that is better evidence
  than a model can take from the picture. What the camera wrote is stripped
  first -- `IMG-20230113-WA0000` says nothing and offering it only invites the
  model to invent a meaning for it.
- This does not loosen the rule against guessing at identities, and the prompt
  says why: a name written by a person is a statement, a face is only a
  resemblance. Use the name the file gives IF what you see fits; never a name a
  face suggests.
- **A truncated answer became a name.** One picture was reported as
  `{ name Winter Couple at a Snowy Forest , description A man and a woman
  stand` -- the model's JSON ran past its token budget, stopped mid-word, would
  not parse, and the fallback took the wreckage. The two fields are now pulled
  out of partial JSON by pattern, a name still carrying JSON is refused, and
  the budget is raised so an answer has room to close its own braces.
- **Twenty-five of forty-nine pictures were refused by Ollama** with
  `(400) Bad Request`, each within half a second -- too fast to be inference,
  so rejected on sight. Same archive, same prompt, same extension: `image.jpg`
  went through and `Phil2.jpg` did not, which points at something in the
  picture the decoder would not take. **Every picture now goes through ffmpeg
  into a plain PNG** of at most 1024 pixels on its longest side, in a settled
  colour space, before the model is shown it. That removes the whole class of
  problem and cuts what has to be encoded and sent. `--picture-width` changes
  the size, and each original's size is logged so a pattern would be visible.
- Two faults were found while testing rather than by reading. The file-name
  strip removed housekeeping words BEFORE turning underscores into spaces, and
  `_` is a word character, so `signature_proper_orientation` never matched
  `\bproper\b`. And `captionTest.py` was asserting that a mixed list of names
  sorts into the order it was made in, which is true only within one clashing
  group.
- `captionTest.py` is at nineteen checks.
- New behaviours: `fewest-leading-zeros`, `file-name-as-context`,
  `pictures-normalised`, `partial-answer-rescued`.

## 1.0.162, 21 August 2026

- **Picture names may now run to 79 characters**, after PEP 8's line length,
  and `--name-length` sets it.
- **But the length was never what was binding.** Measured on his own run of
  twenty-four pictures against a cap of sixty: the longest name was
  **twenty-four characters**, and **not one of the twenty-four held a space**.
  The model returned identifiers -- `OutdoorElderlyLady`, `Kenyan_ID_2024`,
  `Child_Bike_Ride_Sea_View`. Raising the cap alone would have changed nothing.
- The cause was my prompt. It asked for a phrase "fit to be a file name", and
  "file name" is what did it: the model heard "identifier" and answered in
  code. **The prompt now shows the shape rather than describing it** -- spaces,
  sentence case, no CamelCase, no underscores, no extension, the budget stated
  in characters, and three worked examples, two good and two bad.
- **And whatever comes back is repaired anyway.** `spacedOut` turns
  `WomanInWhiteTee` into "Woman in White Tee" and `Kenyan_ID_2024` into
  "Kenyan ID 2024". A word is lowered only when it is a small joining word and
  is not already in capitals, so `JamalMazruiAmazonPoster` keeps both names and
  `NIRA_Birth_Certificate` keeps its initials. The error it can make is leaving
  a word capitalised; it cannot destroy a name.
- **An extension the model invents is taken off.** One answer was
  `BananaSmile.png`, which would have been written out as
  `BananaSmile.png.jpg`.
- **It is now told never to guess at who anyone is.** One picture came back as
  `BezosWithFriends`. There was no note in that archive, so the model had
  nothing to go on and named a living person from a face. The rule was in the
  note branch only; it belongs in the prompt whether or not a note exists.
- All twenty-four of his real names are now a permanent check in
  `captionTest.py`, which is at sixteen checks.
- New behaviours: `names-as-phrases`, `names-repaired`,
  `no-guessed-identities`.

## 1.0.161, 21 August 2026

- **The license question settled and written down.** HomerScribe stays MIT.
  It links nothing: every other program is run separately and spoken to through
  a command line, a file, or a local web request, which the Free Software
  Foundation's own guidance treats as how separate programs talk. Putting them
  side by side in an installer is mere aggregation, which leaves each one's
  license to itself.
- `License.md` now says which program is packaged and which is fetched, what
  each is under, and what would change the answer -- linking a GPL or LGPL
  library into HomerScribe.exe, which it must never do.
- The ffmpeg choice is now stated as the deliberate act it is: the build takes
  BtbN's **LGPL** build, never a GPL one, because a GPL build would oblige
  anyone redistributing HomerScribe to supply ffmpeg's source too. The three
  LGPL duties -- name the license, say where the source is, allow the copy to
  be replaced -- are each met, and `License.md` says how.
- `ReadMe.md` gains a short License section near the end, with links rather
  than bare addresses.

## 1.0.160, 21 August 2026

- **A missing semicolon failed his build**, at the end of the run of
  concatenated strings added in 1.0.159:
  `HomerScribe.cs(7502,111): error CS1002: ; expected`. Fixed.
- **And `buildHomerScribe.cmd` had a byte order mark**, which it must not:
  cmd.exe reads the first line literally, so three stray bytes in front of
  `@echo off` are an error on line one. My own delivery put it there, by
  writing every file with the same encoding as the C# source. Batch files are
  now written without one.
- **`csCheck.py` is new**, and exists because neither fault could be seen by
  what I was checking. Braces, parentheses and brackets all balanced -- a
  missing statement terminator changes none of them, so the check was measuring
  the wrong thing. It takes no arguments, writes a log beside itself, and looks
  for: a statement that never ends; unbalanced braces; a byte order mark on a
  batch file; and a `rem` stranded inside a `^` continuation, which hands the
  word "rem" to the compiler as a file name.
- Writing it turned up the reason the fault slipped through. Blanking string
  literals to check the braces left the offending line reading as a bare `+`,
  which every check took for an unfinished continuation and skipped. A literal
  now becomes a single `S`, so the line reads `+ S` and the missing terminator
  is visible. **Tested both ways**: the check is silent on the corrected source
  and names line 7502 on the broken one.

## 1.0.159, 21 August 2026

- **Who the captions say was speaking around a moment is now given to the
  model**, with its limits stated in the same breath. He asked the obvious
  question and he was right to: captions carry times, so a name at 12:03 must
  say something about who is on screen at 12:03.
- It partly does. In an interview, a talk, a piece to camera or much television
  drama, the speaker is in shot a good deal of the time. My earlier answer, that
  captions do not tell you who is on screen, was too absolute.
- Three things weaken it, and the third belongs to this program alone.
  Narration is never in shot. Dialogue cuts to the listener's face as often as
  the speaker's, most of all on the line that matters. And **HomerScribe
  describes in the gaps BETWEEN speech, by design** -- so at the moment a
  description is made nobody is speaking at all, and the nearest speaker is on
  one side of the silence or the other. The correlation is weakest exactly
  where descriptions happen.
- What survives all three is still worth having: whoever spoke either side of
  the silence is almost certainly in the SCENE, if not in the frame. So it is
  offered as exactly that, and the model is told to use the name only if
  somebody in the picture matches how that person has been described to it.
- New behaviour: `speaker-near-the-moment`.

## 1.0.158, 21 August 2026

- **An archive can carry notes about its own pictures.** A file named after the
  archive -- `holidays.md` inside `holidays.zip` -- is sent with EVERY picture
  in it. A file named after one picture -- `image07.md` beside `image07.jpg` --
  is sent with that one only. Both, where both exist, and anything given by
  `--context-file` before them: most general first, most particular last, so
  the nearest note has the last word. A `.md` in the archive is counted as a
  note, not as something passed over.
- **The note is framed as a way of RECOGNISING people, not as a claim that they
  are present.** This matters more than it looks. A note saying who tends to
  appear and how to tell them apart is an invitation to write those names into
  every picture, because a model uses a name it has been given. The instruction
  is therefore explicit: use a name only where what is actually visible matches
  what the note describes; where you cannot tell, say what you can see and do
  not guess. That is the rule that took naming in film from a scatter of
  half-named strangers to something a listener could follow.
- **The captions' speakers are now gathered as a roster** and offered to the
  describing model as names this film uses. What this is NOT is a claim about
  who is on screen: a film cuts to the listener as often as the speaker, and a
  narrator is never in shot. Handing the model "Penelope is speaking" would
  produce a confident Penelope in frames she is not in.
- What a roster IS good for is knowing which names the film uses and how its
  makers spell them, so that a name reached for is a real one. It cannot attach
  a name to a face by itself -- the context file is where appearances live. The
  captions say who exists; the context file says what they look like; a name is
  used where the two meet. Roles that name nobody visible -- NARRATOR,
  AUDIENCE, ALL, CROWD, VOICEOVER -- are left out.
- **Measured on his own four runs, this yields nothing**: nought, nought,
  nought, and one, which was "Audience" and is filtered. Documentary and
  conference captions rarely name speakers. A drama with proper subtitles for
  the deaf and hard of hearing names them constantly, and that is where it will
  earn its place.
- New behaviours: `notes-inside-archives`, `caption-speaker-roster`.

## 1.0.157, 21 August 2026

- **A zip of pictures can now be a source**, on its own or by wildcard. Every
  image in the archive is described, and `descriptions.md` is written to a
  folder named after the archive. Nothing is renamed and no metadata is written
  yet: this is the first of four steps, and it exists so the QUALITY of the
  names can be judged before anything is built on them.
- Read from the archive: `.png`, `.jpg`, `.jpeg`, `.webp` and `.gif` go to the
  model as they are; `.bmp`, `.tif` and `.tiff` are turned into a PNG by ffmpeg
  first. Anything else in the archive is passed over, counted and named in the
  log, so nothing looks silently lost.
- **Every picture is reported on for metadata standing**, as asked: whether the
  format can carry the description, can carry only a plain comment, or has
  nowhere to put one at all. GIF is the middle case and BMP the last -- that is
  a fact about those formats, not something to be worked around.
- **The clash rule, and why every member of a group is numbered.** His
  convention is `-001`, so that alpha order is also the order the pictures came
  in. Numbering only the second and later members breaks that, because "-" is
  character 45 and "." is 46: `Sunset-002.jpg` sorts BEFORE `Sunset.jpg`, so the
  plain one lands last. Tested, not assumed. So every member of a clashing group
  is numbered from `-001`, and a name that occurs once is left plain. Three
  digits as asked, widened past 999 so that being right outlasts sorting.
- Alternatives weighed and not taken: a distinguishing detail instead of a
  number, which needs a second question per picture and can still clash; and the
  original file name in brackets, which traces back but is long where he asked
  for succinct. `descriptions.md` lists everything in archive order, so the
  mapping is kept without lengthening any name.
- Names are cut at a word and never mid-word, stripped of anything Windows,
  macOS or a zip objects to, and kept clear of the names Windows reserves.
- Transcribe audio does not apply to an archive, and says so rather than
  quietly doing nothing. A run over archives alone does not need Whisper.
- `captionTest.py` is at fifteen checks, four of them on the naming rules.
- New behaviours: `pictures-from-archives`, `clash-numbering-whole-group`,
  `metadata-standing-reported`.

## 1.0.156, 21 August 2026

- **The chain of alternatives stopped after one, and that was my fault.** In the
  14:46 run the usual way was refused with a 403, the web player was tried, and
  yt-dlp answered "Requested format is not available". My own guard read that as
  "the video is absent, asking differently will not help" and abandoned the
  remaining four ways. It is not that at all: it means that player was handed an
  empty menu, so nothing matched "best video plus best audio". It is a refusal
  wearing a different coat, and it is now treated as one.
- **The reason reported was the wrong one.** The results box said "Requested
  format is not available" for all four videos when the actual trouble was the
  403 that came first. The first failure is now what gets reported; a later
  attempt's complaint describes that attempt, not the problem.
- **The order is better.** The single combined stream is now tried second, being
  the cheapest thing that often works. Then the television, iOS and mobile web
  players, which ask for less and are commonly served; the web player last,
  since it is the one most often given an empty menu. A seventh try asks for any
  format at all, for the case where a player's menu holds something but not the
  usual pairing.
- **A browser's session is borrowed before giving up.** When every way is
  refused and no cookies were offered, HomerScribe now tries again with Edge's
  cookies, then Chrome's, then Firefox's. A signed-in session is often served
  what an anonymous one is not. Whichever works is named, so it can be set with
  `--browser-cookies` and the tries paid for once. `--browser-session no` turns
  it off.
- New behaviours: `browser-session-last-resort`, `first-reason-reported`.

## 1.0.155, 21 August 2026

- **The build keeps yt-dlp current, and the program stops mentioning it.**
- `buildHomerScribe.cmd` now runs `yt-dlp --update-to nightly` on every build.
  It is one quick call that says "up to date" and stops when there is nothing
  to do, and it never fails the build: no network, or a copy that cannot write
  to itself, and the build carries on with what is there. The version before
  and after goes in the build log. The yt-dlp that ships in the installer is
  therefore never older than the build.
- **Nightly, not stable.** yt-dlp's own README calls nightly "the recommended
  channel for regular users", and asks that anyone hitting a problem on stable
  move to nightly before reporting it. YouTube changes what it serves, at times
  deliberately to break downloaders, and the fix for something that broke this
  week is on nightly this week. `--update-channel stable` or `master` if wanted.
- **No warning at run time.** The version and its age are recorded in the log as
  a plain fact and nothing is said about them. Keeping the tool current is the
  build's job, and telling somebody at run time about a thing they cannot act on
  there and then is noise.
- **yt-dlp's own nagging is silenced too.** It prints a complaint on the console
  when it is more than ninety days old, which is fair of it and the wrong place
  here, where the console is carrying progress a listener is following. Every
  call now passes `--no-update`, which is what that flag is documented for.
- The update on refusal, added in 1.0.154, now goes to the same channel.
- New behaviours: `build-updates-yt-dlp`, `no-update-nagging`.

## 1.0.154, 21 August 2026

- **The extra information call was not the cause, and I was wrong to suspect
  it.** The 13:59, 14:02 and 14:05 runs each show exactly one yt-dlp command
  before the download, as in 1.0.146, and the refusal is unchanged. The
  reordering in 1.0.153 was right on its own merits and fixed nothing. At the
  point of download HomerScribe now does what 1.0.146 did, character for
  character, and gets a different answer, so the difference is not in the
  program.
- **A refused film is now asked for a different way, and then another.** The
  usual request first, exactly as before; then the web player, the Safari web
  player, the television player, the iOS player, and finally a single combined
  stream instead of the best picture and best sound taken apart. It stops at
  the first that is served and writes down which one that was.
- **Only a refusal is retried.** A video that is private, deleted or
  geo-blocked is just as absent from every player, so six tries would learn
  nothing; the reason is read and the attempt abandoned.
- **yt-dlp is updated when nothing else works**, once per session, and the
  usual request tried again. Its age is now worked out from its version at
  startup and said in the log, with a word of warning past thirty days. At
  2026.07.04 the copy in use is seven weeks old against a service that changes
  most weeks.
- **`--player-client`** pins one once it is known -- web, web_safari, tv, ios or
  mweb -- so the tries are paid for once rather than on every video.
  **`--update-tools`** turns the automatic update off.
- New behaviours: `other-ways-to-fetch`, `update-tools-on-refusal`,
  `tool-age-reported`.

## 1.0.153, 21 August 2026

- **A third request was being made of YouTube before every download, and it was
  not there before the caption work.** 1.0.147 added a `--dump-single-json`
  call to learn the title and the caption tracks, and put it in front of
  everything. So each video was extracted three times where 1.0.146 extracted
  it twice, and only then downloaded.
- The last version known to fetch a film successfully is 1.0.146, and it is
  also the last version that did not make that call. That is a correlation, not
  a demonstration, and I had been asserting the opposite: that the download
  command was character for character unchanged and therefore nothing here
  could be the cause. The command was unchanged. The COMMAND was not the only
  thing that changed, and I should have looked at the sequence before saying so.
- **The question is now asked only when the answer is needed.** Wanting a
  transcript alone, it decides whether the film is needed at all, so it comes
  first. Wanting the film, it comes after the film is safely down. On that path
  the download is once again preceded by exactly one request, as in 1.0.146.
- `ytCheck.py` now settles it rather than guessing. Its first three trials run
  the SAME download after nothing, after one information call, and after two,
  and it says in as many words which of those three it was.

## 1.0.152, 21 August 2026

- **Half is better than nothing.** When the film cannot be fetched but a
  transcript was also asked for, the captions are now taken anyway and
  `transcribed.md` is written. The results box says plainly that nothing was
  described and why.
- This matters because the two things come down different roads. Every
  information call and every caption file has succeeded on this machine
  throughout; only the signed media stream is being refused. A run that wanted
  both used to report that nothing at all could be done, when half of it was
  sitting there for the asking.
- **The 13:44 run was not a regression.** Describe video was ticked as well as
  Transcribe audio, so the film really was needed, and the fetch met the same
  403 that has stood since 12:27. The log shows `Setting describe = yes` and
  the caption tracks being read correctly for all four sources before the
  download was even attempted.

## 1.0.151, 21 August 2026

- **"e.g." is no longer the end of a sentence.** The first run of 1.0.150 put
  this at the top of the TED transcript: *"For more information on using TED
  for commercial purposes (e.g."* The sentence splitter broke at the full stop
  inside the abbreviation, and the fragment that was left passed every test
  because it ended with a full stop. A piece broken that way is now joined back
  on to the one before it, and a run of common abbreviations is recognised.
- The trim-back that guarantees a blurb ends on a full stop **would have hung**
  on exactly that input: the offending stop was the last character, so
  searching from the end found it again and set the same length for ever. It
  now searches from before the last character, so every pass is strictly
  shorter. Found by porting the function to Python and running it against TED's
  real description, which is also now a check in `captionTest.py`.
- A blurb sentence with a bracket opened and never closed is dropped too.
- **Each film's own log now begins at the fetch.** It began inside `runOne`,
  which left out how the film was obtained -- and on the captions-only route
  left out which caption track was chosen and why, which was most of what there
  was to say. The per-film logs from the 13:14 run were four lines long.
- `captionTest.py` is at eleven checks, two of them on the publisher's blurb.

## 1.0.150, 21 August 2026

- **The three documents are rewritten to be read.** A log is for a machine.
  `described.md`, `transcribed.md` and `scribed.md` are for a person, and a
  person listening rather than skimming, which is a harder audience than
  either. The rules below are the W3C Web Accessibility Initiative's, from its
  guidance on transcripts.
- **No times in the body.** The Initiative puts it plainly: timestamps are
  usually unnecessary clutter, need not be as granular as the captions, and
  need no end times. So a time now appears only in a section heading, where a
  screen reader can move between them, and only on a film over twenty minutes.
  Below that there is no time anywhere. A section heading reads "From 10
  minutes" rather than "0:10:00 to 0:20:00", because that is a place in the
  film rather than four numbers.
- **Paragraphs, not bullets.** A hundred and thirty list items is a hundred and
  thirty announcements of the word "bullet" before anything is said. And a
  passage now ends where a SENTENCE ends: it may run on half as long again
  while it waits for a full stop, because a paragraph broken mid-clause reads
  as though the speaker was cut off. In a timed list that was invisible.
- **`scribed.md` is a descriptive transcript**, and says so. That is the term
  of art, and the Initiative names this document as the one a reader who is
  both Deaf and blind needs -- adding that where you have one, you do not need
  a separate basic transcript. It was being written as though it were an extra.
- **Only what was added is labelled.** A description carries a bold
  "Description." at the front. Nothing else does, so an unlabelled paragraph is
  the film's own words. The Initiative asks only that added material be
  distinguishable, and labelling both sides would double the reading to say the
  same thing.
- **The heading is four or five lines** and no more, since every word of it is
  read before the reader reaches a single word of the film. On the one-minute
  video the old heading was 61 percent of the whole document. Measured on the
  four-minute W3C introduction: 1,788 bytes of heading became 678, and the
  document as a whole 4,913 bytes became 3,737.
- **A note only where there is something to explain.** Telling a reader what a
  speaker's name looks like, in a film where nobody is named, is a sentence
  spent on nothing.
- **The publisher's blurb is tidied or dropped.** Sentences carrying a web
  address go, and so do appeals to subscribe and permission notices; a screen
  reader reads an address one character at a time. What is left never ends on a
  fragment. Too little left and the whole section is dropped.
- **A speaker named in brackets is now recognised.** TED writes "(Audience)
  Good." with no colon, and it was being run into the sentence before. What
  keeps this apart from a sound is what follows: a speaker is followed by a
  sentence starting with a capital, where "(Music) plays softly" carries on in
  lower case.
- New behaviours: `documents-as-prose`, `times-only-in-headings`,
  `descriptions-labelled`, `publisher-blurb-tidied`.

## 1.0.149, 21 August 2026

- **A transcript no longer downloads the film.** When only Transcribe audio is
  ticked and the video carries its own English captions, the captions are
  fetched with `--skip-download` and the film is never touched. There is
  nothing in a transcript that needs it: the captions say what is said, and
  nothing is being described.
- This is the right thing to do on its own merits -- about thirty kilobytes
  instead of a hundred megabytes, and seconds instead of minutes -- and it also
  steps straight past what has been failing. In the 12:17 run every caption
  file came down whole while every media request was refused with a 403.
  Captions are reachable on that machine; the media stream is not.
- Taken from Jamal's own youtube-dl scripts, which have been doing exactly this
  for years: `--write-sub --write-auto-sub --skip-download`. Two more of their
  settings came across as well.
  - **`--convert-subs vtt`.** YouTube also serves srv3 and ttml, which the
    caption reader cannot parse, and asking for "vtt/srt/best" would fall back
    to one of those quite happily. Now whatever arrives is turned into VTT.
  - **`--sleep-requests 1`**, a pause between requests, which is what a service
    that counts them is asking for.
- With Describe video ticked the film is still fetched, because there is
  something to look at. And a video with no English captions is still fetched
  and listened to. The fallback is automatic and the log says which happened.
- The running time in the heading now comes from the page rather than from the
  file, since on this route there is no file to ask.
- New behaviours: `captions-without-the-film`, `captions-converted-to-vtt`,
  `sleep-between-requests`.

## 1.0.148, 21 August 2026

- **Fixes the caption fetch, which failed every source in the first run of
  1.0.147.** Nothing was downloaded and all four videos were refused.
- The cause was one flag. `--sub-langs "en.*"` is a REGULAR EXPRESSION in
  yt-dlp, not a language, and it matched every machine translation OUT of
  English. On the Ken Robinson talk that was sixty-six tracks -- Albanian,
  Arabic, Armenian, Azerbaijani and on through Vietnamese -- each a separate
  request. YouTube answered 429 Too Many Requests part way through the second
  video, and the two after it were refused with 403 before their video data
  could be fetched.
- **The language rule is now an exact list.** Anything beginning "en-" used to
  count as English, which is precisely how a translation is named. "en-ca" and
  "en-in" are deliberately left out although they read like Canadian and Indian
  English: YouTube uses them for Catalan and Indonesian. "-hi" is no longer
  trimmed as a hearing-impaired marker either, because "en-hi" is Hindi.
- **The track is now chosen, not guessed.** The `--dump-single-json` call
  already made for the title also returns "subtitles", the tracks a person
  uploaded, and "automatic_captions", the machine ones and every translation.
  The English tracks are picked out of that by name and **exactly one** is
  asked for, preferring a person's over a machine's on YouTube's own say-so.
  Replayed against the four real track lists from the failed run: 23, 3, 66 and
  3 tracks listed, 2, 3, 3 and 3 of them English, one request each.
- **A caption failure can no longer kill the film.** The subtitle flags used to
  ride on the download command, so yt-dlp giving up on a subtitle abandoned the
  video too. Captions are now fetched by a second command after the film is
  safely down. If it fails, that is logged and Whisper writes the transcript,
  which is what happened before captions existed and is a perfectly good answer.
- **A video's details no longer leak into the next one's documents.** The
  title, publisher and description were never cleared between sources, so a
  local file following a downloaded one would head its documents with the
  downloaded one's title. `Developer.md` said this was handled. It said so
  wrongly, and was only found by going to check.
- New behaviours in the fingerprint: `captions-one-track`,
  `captions-track-from-page`, `captions-fetched-apart`.

## 1.0.147, 21 August 2026

- **A film's own captions are now the transcript.** Somebody wrote them, so the
  words are right. They say who is speaking. And they write down what can be
  heard but not spoken -- a door slamming, music starting -- which no transcript
  of speech has ever held. Whisper cannot do any of those three things.
- They are taken from a subtitle track inside the film, or from a caption file
  sitting beside it, whichever is there. English only: every track is asked its
  language, and anything else is left to Whisper. A film with one track that does
  not say what language it is in is taken as English, and the document says so.
- **A downloaded video now brings its captions with it.** yt-dlp is asked for
  them whenever a transcript is wanted.
- **One transcript, not two.** `transcribed.md` is still the only transcript,
  with a line at the top saying where the words came from. Two files of nearly
  the same words would only make the reader choose between them.
- **Captions decide the words, never where a description goes.** This is the part
  that matters most and is easiest to get wrong. A caption is put on screen early
  and taken away late, so that a reader can finish it. That is not when the words
  are said. Using those timings to place descriptions would shrink and shift the
  quiet the placement rule measures, and the damage would not show in any count
  -- only in a listener losing dialogue. So a run that describes still listens to
  the film with Whisper, and uses what it hears for placement alone.
- `scribed.md`, the document for a reader who can neither see nor hear, gains
  what it was always missing: a **Sound** entry for something heard but not
  spoken, and a speaker's name against the words. That was the whole reason for
  wanting captions.
- **The rolling repeat, got wrong and then got right.** YouTube's automatic
  captions scroll up the screen, and each cue holds the one before it with more
  added. The first version compared each cue with the one before and asked
  whether the OLD held the NEW. A rolling caption grows the other way, so nothing
  matched and every line was written down twice. Tested before it was believed,
  found within the minute, and replaced: the words already written down are
  remembered, and only what has not been seen yet is added.
- **A sound stays its own entry.** Found the same way, before any film was run:
  a talk writes "(Laughter)" and "(Applause)" as cues of their own, a second or
  two after the line before, and both merge rules swallowed them into the
  surrounding prose. `scribed.md` would then have labelled the whole passage
  Spoken. A cue that is nothing but a sound is now never merged, in either
  direction.
- **Whisper is no longer required in order to transcribe**, so long as captions
  are left on. A film that turns out to have none is refused by name, with the
  reason.
- Every document -- `described.md`, `transcribed.md` and `scribed.md` -- now
  opens with what the video says about itself: its title, who published it,
  where it came from, and what the publisher wrote about it. yt-dlp was already
  being asked for all of that; it was being used for the model's context and then
  thrown away. The web address is written as a link with the title as its text,
  never bare, because a screen reader reads a bare address one character at a
  time.
- New setting `--captions`, on by default. Turn it off to make Whisper do the
  work in every case, which is how to compare the two.
- New behaviours in the build fingerprint: `captions-preferred`,
  `captions-english-only`, `captions-keep-sound`, `captions-never-place`,
  `video-heading`. **Check the `This build does:` line before analysing any log
  about captions.**

## 1.0.146, 17 August 2026

- Built without a History entry. Recorded here so that a version in a log can be
  looked up.

## 1.0.145, 17 August 2026

- The build now asks the repository which versions are already released, and
  steps over any number it finds. A working copy whose `version.txt` has fallen
  behind can no longer mint a number that is already spent.
- That is what had happened. `version.txt` held 1.0.143 while 1.0.144 was
  released and committed, so the build stamped a second 1.0.144 and tagRelease
  refused it, publishing nothing. The refusal was right; the number should never
  have been offered, and the wasted work was the whole build.
- One `git ls-remote --tags` is the only network call the build makes. If it
  fails the plain increment is used, so a machine with no network still builds,
  and tagRelease stays the check of last resort it has always been.
- `Developer.md` and `ReadMe.md` say so too, since a versioning rule that is
  only in the script is a rule nobody reads.

## 1.0.144, 13 August 2026

- A recording is no longer described. Asked to describe twenty-nine mp3 files,
  HomerScribe produced one description for each: the model inventing a sentence
  from a blank montage, written out as though it meant something. There is no
  picture in a recording and nothing to describe.
  It now asks ffmpeg whether there is a picture at all. Cover art does not count
  -- it is carried as a video stream of one still frame and is not something to
  describe. With Transcribe audio also ticked it transcribes and says so; with
  only Describe ticked it refuses the file and says to tick Transcribe audio.
- Tested against ffmpeg's own output for three cases: an mp3, an mp3 carrying
  cover art, and a film.

## 1.0.143, 13 August 2026

- The repository is now what it should be: thirty files, the program and its
  documents and nothing else. Every note, script, film and build product is off
  GitHub and still on the disk.
- tidyRepo now names the ten largest objects in the repository and the path each
  came from, saying plainly when one belongs to no commit at all and is merely
  waiting to be collected. I had said I could not tell what was holding 516 MB;
  that was a failure of effort rather than of possibility, since git can simply
  be asked. Several rounds of guessing were the wrong response to a question with
  a one-command answer.

## 1.0.143, 13 August 2026

- Added finishRepo.py, which settles the size question from evidence rather than
  guesswork and needs no arguments. It reads the pack index for the largest
  objects actually stored, names the file each belongs to from the object list,
  and says how much is reachable and how much is not. Anything large that does
  not belong is then removed and the pack rebuilt.
- The reason nothing had worked: repack without -A keeps unreachable objects
  inside the pack, so the pack never shrinks. With -A they are written out loose
  and the prune that follows deletes them. Measured on a test repository: 38.16
  MiB to 2.35 KiB, and on another with a file deleted in a later commit, 57.24
  MiB to 1.69 KiB.
- It also removes every leftover folder from the earlier runs.

## 1.0.142, 13 August 2026

- Files purged by one run came back in the next. The commit purgeRepo takes
  before rewriting used "git add -A", which stages UNTRACKED files as well --
  and files purged earlier were sitting on disk untracked, exactly as intended.
  It hoovered them straight back in: committed, rewritten past, and pushed.
  Three returned that way. It uses "git add -u" now, which stages changes to
  files git already tracks and nothing else, which is all that was ever wanted.
- .gitignore covers context/video* and video.htm, which the replacement I sent
  had dropped without noticing: purgeRepo had added them itself in an earlier
  run and my file overwrote that.
- Tested by running the whole thing twice over. What is purged in the first run
  is still purged after the second, and every file is still on disk.

## 1.0.141, 13 August 2026

- The Python scripts and the Odyssey context files are taken out of the
  repository. The build script uses no Python at all -- checked rather than
  assumed -- so none of it belongs there, and one film's context is not part of
  the program. The installer no longer claims to ship measure.py and
  placement_test.py. All of them stay in the working folder.
- Two faults in purgeRepo, both about files in a subfolder: the copy set aside
  used only the base name, so two files of the same name in different folders
  would have become one; and the restore never recreated the folder, which git
  removes once it holds nothing tracked, so the move failed and the exception
  was swallowed. Found by testing rather than reading: context/The_Odyssey.md
  and prototype/describeMovie.py disappeared from the disk.
- When the collection cannot free the space, a fresh copy of the repository is
  now fetched and its .git swapped in rather than the user being told to do it.
  Only the hidden folder is replaced; every file stays where it is, and the old
  one is kept aside until the next run.

## 1.0.140, 13 August 2026

Two things tidyRepo did not finish, both found from its own log.

- The space was not reclaimed: the pack was 516 MB before the collection and
  516 MB after. git gc leaves an unreachable object where it is when it is
  already inside a pack, so nothing moved. It now clears everything that can
  still be holding them -- the backups filter-branch keeps, the reflog including
  unreachable entries, the stash, stale remote-tracking refs -- and then repacks
  from scratch rather than collecting. Measured on a test repository: 7.9 MB of
  git folder down to 192 KB.
- The backup folders could not be deleted: git marks its object files read-only
  and Windows refuses. The flag is now cleared and the deletion retried, which
  is the standard remedy.
- If the size still does not move, it says so and gives the one certain cure, a
  fresh clone, rather than reporting success.

## 1.0.139, 13 August 2026

- Added tidyRepo.py, which finishes the repository clean-up in one run and takes
  no arguments: it checks the set-aside files came back, takes the testing
  material and build products out of the history, updates the remote to the
  address GitHub asks for, reclaims the space, and removes the backup folders
  once it can see nothing is missing.
- Two faults in purgeRepo.py found by testing it, both of which had let files
  come back after being removed. filter-branch rewrites the commits and leaves
  the INDEX alone, so the purged files were still staged and the next commit put
  them straight back; the index is now brought into line, mixed rather than
  hard, so nothing leaves the disk. And purgeRepo never accepted paths on the
  command line at all, though I had said it did -- it silently used its own list
  instead, which is why a first attempt removed the wrong files.
- Verified end to end: source alone in the repository and on the remote, every
  other file still on disk.

## 1.0.138, 13 August 2026

- purgeRepo.py failed with "You have unstaged changes", and the cause was itself:
  its own log is written as it runs, and the repository was tracking it, so the
  tree was dirtied by the act of recording what was being done. The check passed,
  the log was written, and the rewrite then refused.
  The log is now untracked before anything looks at the tree, along with
  fixRepo.log and buildHomerScribe.log, and *.log is added to .gitignore.
- Genuine uncommitted changes are now found before the folder is copied rather
  than after, and committed rather than discarded -- they are the user's changes
  and a commit is the one operation here that cannot lose anything. Line endings
  are the usual reason: git calls a file changed when it would write it back
  differently.
- Tested on two repositories, one with the log tracked and one with a genuinely
  modified file: both rewritten, the executables gone from local history and from
  the remote, the source and the modified file intact, and everything still on
  disk.

## 1.0.137, 13 August 2026

- Added purgeRepo.py, which removes a file from every commit rather than merely
  from the future. It uses git filter-repo when installed and git filter-branch
  otherwise, the latter being part of git and needing nothing fetched.
- Two things it got wrong and now does not, both found by testing rather than
  reasoning. Rewriting the history checks the working tree out again, and a file
  that is in no commit any more is deleted from disk along with it -- which for a
  5.7 GB video would be unforgivable. And setting the files aside by MOVING them
  leaves the tree dirty, which makes the rewrite refuse to run; they are copied
  instead and the originals put back afterwards.
- Verified against a repository built to match: the installer stayed on disk
  byte for byte, it and the other executables left every commit and the remote,
  and the source history and tags were untouched.

## 1.0.137, 13 August 2026

- purgeRepo.py now also offers to remove the three stray *_HomerScribe.exe files
  and the describer's guide, which are in the history and were not on its list.
  A file that turns out not to be in the history is reported and skipped, so
  naming one harmlessly costs nothing.
- Tested end to end against a repository with two executables committed and
  pushed: both gone from the local history and from the remote, the source
  changes intact, both files still on disk, and a copy of the folder taken
  first. It used git filter-branch, filter-repo not being installed, which is
  the fallback working as intended.

## 1.0.136, 13 August 2026

- fixRepo.py used 50 MB as the size that blocks a push. That is GitHub's warning;
  the limit that blocks is 100. So an 87 MB installer looked like the cause, and
  since it was already in the remote's history the script stopped over a file
  that was never the problem -- the 5.7 GB video was.
  It now asks two separate questions: what is over GitHub's limit and blocked
  the push, and what should not be tracked at all whatever its size. Only the
  first, when already in the remote's history, is a reason to stop; a merely
  unwanted file is untracked from here on and its old copies stay where they are,
  costing space and blocking nothing.
- Where it must stop, it now gives the git filter-repo command needed and says
  to copy the folder first.
- A new .gitignore covering build products, fetched tools, media, run output and
  the maintainer's own drafts and notes. Checked against every file in the
  repository as it stands: nothing that belongs is excluded and nothing that
  does not is kept.

## 1.0.135, 12 August 2026

- Added fixRepo.py, for a push rejected because a file is too large. It rewinds
  to the remote's last commit keeping every file on disk, drops the large files
  from the index, adds patterns to .gitignore, commits and pushes. Nothing is
  deleted from disk.
  Tested against four repositories built to match: one with a large file in an
  unpushed commit, which it repaired; one where the large file was already on the
  remote, which it correctly refused; one with nothing to push; and a folder that
  is not a repository.

## 1.0.134, 12 August 2026

From counting how often a film's protagonist was named.

- On a two and three quarter hour film he was named in 117 descriptions of 341,
  and an unnamed "bearded man" appeared in 37 more, spread evenly throughout --
  not absent, INCONSISTENT. The model was following an instruction to name only
  where the picture made it plain, and its confidence varied from one frame to
  the next.
  That is worse for a listener than either extreme. "A bearded man strides across
  the deck" at 26 minutes and "Odysseus looks out to sea" at 27 give no way of
  knowing they are the same man; consistently wrong would at least be followable.
- The prompt used to say "names you have already used, so keep using them",
  which is a list with nothing to attach to. It now says to use a name again
  whenever somebody matches how that person was described, and explains why: a
  listener cannot tell that "a bearded man" and a name are the same person.
- Context.md and the Odyssey context file both changed to tie a name to
  something visible and require its use every time, rather than leaving the model
  to judge when it is sure. That turns recognising a person, which it cannot do,
  into matching an attribute, which it can.

## 1.0.133, 12 August 2026

- Context.md was written entirely from one narrative film and gave advice that
  is wrong elsewhere. Two corrections.
  The naming rule said to name the two or three people the picture makes plain.
  That holds for a documentary, where one person addresses the camera, and fails
  for drama: a model cannot tell a bearded man of forty from a bearded man of
  twenty-five, and half a film will carry the wrong name. A name is now advised
  only where you can say what makes that person distinguishable in a still frame
  -- the test being whether a stranger could pick them out from what you wrote.
  Added what different material needs: documentary, science and technical, drama,
  silent film, lecture, and the case of knowing nothing about it. The science
  entry is the one most likely to be missed -- saying that labels and captions
  appear and should be read out, since on such material the words on screen are
  the content and the narration says "as you can see here".
- The template no longer assumes a cast, and says which parts to leave out.

## 1.0.132, 12 August 2026

Three symptoms, one cause: the window was never given the chance to redraw.

- A model call blocks this thread for ten seconds or more, and nothing pumped the
  message queue meanwhile. So Windows decided the program had stopped, which is
  the "Not responding" a screen reader reports; the window never repainted; and
  a screen reader coming back to it read the title it had last been told about,
  which is why Alt+Tabbing back two hours into a run said "Starting".
  The model call now goes to a thread and this one pumps until it returns.
- The queue is also pumped immediately after every status update, as 2htm does.
  Setting the text is not enough on its own: until the queue is pumped, Windows
  has not repainted the window or told anybody the title changed.
- Measured from that run, and NOT explained by these changes: 32.5 seconds a
  description against 10.3 the run before, with the same model, picture size,
  frames and detail. That is still to be accounted for.

## 1.0.131, 12 August 2026

- When there is a dialog, the console now carries the same messages the dialog
  gave, one to a line, in the order they were spoken. It carried the log stream
  instead: command lines, exit codes and paths, which are useful when there is
  no dialog and unreadable when there is. Those are in the log, where somebody
  looking for them will go.
- Lines are written whether or not the message was spoken aloud, since the
  console is a record to be read back rather than an interruption. Errors still
  appear in both, and --verbose puts everything back.
- Running from the command line with no dialog is unchanged.

## 1.0.130, 12 August 2026

- Fixed a build failure I introduced with the timed context sections. The code
  that builds a prompt asked which section covers this moment, but had never been
  told which moment it was: nothing in a prompt had depended on the time before.
  The time is now passed to it, through describeImage and its five callers.
  It failed to compile rather than doing something wrong, which is the good
  outcome, but it should not have reached you.

## 1.0.129, 12 August 2026

- The context sections now carry setting and who is present, and nothing else.
  They had carried the "key visual information" bullets from the guide they came
  from, which are descriptions: given one, a model produces it whether or not it
  is in the frame, and the output becomes a paraphrase of the file rather than an
  account of the picture. 44 sections, about twenty words each.
- The prompt now says outright what a section is for: where the film is, not
  what is in this picture; use it for the right words and names, describe only
  what can be seen.
- The log measures whether that held. Each move into a new section is logged, and
  a MEASURE line at the end of each film reports how many descriptions were given
  a section and how many repeat half or more of their words from it -- which
  would mean reciting rather than describing.
- Context.md leads with the rule this comes down to: a context file supplies
  nouns and names, never observations. The test given is whether a sentence could
  be false of the picture and still be in the file.

## 1.0.128, 12 August 2026

- context/video-sequences.md: all 50 sequences of a describer's guide for The
  Odyssey turned into timed sections, each about fifty words of setting, who is
  present, and what is seen. Three carry times taken from the film's own
  transcript; the rest are marked TIME and are ignored until filled in, so the
  file works at any stage of being completed.
- Context.md gained a section on doing this: what transfers from a guide written
  for a person and what does not. Reasoning, reliability ratings and production
  facts all go -- the last would be read aloud as though they were in the
  picture -- and anything phrased as advice rather than fact goes with them.

## 1.0.127, 12 August 2026

- A context file may now be divided by time. A heading beginning with a time --
  "## 41:00 The Cyclops's island" -- marks a section sent only while the film is
  inside it; everything before the first such heading is sent with every
  description.
  I had said a scene-by-scene guide was unusable because a moment could not be
  matched to a scene. That was wrong: nothing had been built to match them. The
  length objection goes with it, since a forty-section guide now costs no more
  per description than a four-section one.

## 1.0.126, 12 August 2026

- Added Context.md: how to write a context file for this arrangement, which is
  not how one would brief a person. The governing fact is that the whole file
  goes into EVERY prompt, so it must be short -- under about 250 words -- and
  vocabulary helps far more than plot, since a vision model shown a bronze age
  warship says "a boat" until it is told what world it is looking at.
- Added context/video.md for Nolan's The Odyssey, 246 words, distilled from a
  1,043 line guide written for a human describer. What survived: the period and
  its vocabulary, the three names the picture makes plain and an instruction to
  describe everyone else by sight, the rule about disguises, the rule about the
  supernatural. The reasoning was dropped -- a person needs to know why, the
  model needs only the instruction.

## 1.0.125, 12 August 2026

From a run where the film was moved or deleted while it was being described.

- A source that disappears mid-run now stops that film at once, says so, and is
  named in the results as an error. It used to fail on every moment in turn --
  639 times in 47 seconds, silently -- and then report "1 descriptions" as
  though that were a result.
- Frames that cannot be read twelve times in a row also stop the film, since
  that is the file or the disk rather than any particular moment. One failure
  can happen for ordinary reasons; a dozen cannot.
- Nothing is written from a film that fell over. An empty described film left in
  a results folder looks finished, and the next run would skip it as already
  done.
- The new MEASURE lines earned their place immediately: "room available, middle
  0s" is what identified this as a missing file rather than a fault in the
  placement, which was working correctly throughout.

## 1.0.124, 12 August 2026

- Dropped two opening messages that told the user nothing they could act on:
  looking for the programs, and asking Ollama for its models. Both were under
  the hood.
- The first thing said about the work is now how many files there are, which one
  this is, and where its results go: "Processing 1 file, video.mkv" or
  "Processing 3 of 9 files, The Africans - Episode 4.mp4". The results folder is
  named only when it is not simply the file name without its extension, since
  saying that would be repetition.

## 1.0.123, 12 August 2026

- The first eight messages of a run are spoken at once rather than collected.
  Messages are grouped so that descriptions arriving every few seconds do not
  interrupt constantly, and a message waits up to twenty seconds for company.
  That is right in the middle of a film and wrong at the beginning, where they
  are rare, each is informative, and somebody is waiting to hear the program is
  alive: "Starting" was spoken and everything after it waited twenty seconds.
  After the first few, grouping resumes exactly as before.
- A message that was WITHHELD no longer counts as having been said. It reset the
  clock that decides whether the next one waits, so a message nobody heard could
  silence the one after it for twenty seconds.
- The rule that keeps HomerScribe quiet when it is not the window in front is
  unchanged, as is everything that decides it.

## 1.0.122, 12 August 2026

- A description was allowed its gap PLUS up to 3.5 seconds at --detail rich.
  That allowance was written when a moment sat in whatever silence happened to
  exist and a little overrun was the price of a whole sentence. Under the current
  design it is wrong: a moment is placed in measured room and given the whole of
  it, with a margin already kept at each end, so a licensed overrun is licensed
  talking over the dialogue. It is nought now, whatever the detail.
  This is also why the rule meant to drop an overrunning description had never
  fired in two versions: descriptions were not exceeding their allowance, they
  were using it. Eleven overlapped speech on the last run and not one exceeded
  what it was allowed.
- Two MEASURE lines are written at the end of each film: words and seconds per
  description against the room available, how many still judge rather than
  observe, how many name somebody, and how often the film's memory was
  rewritten. One block that answers the questions that have needed a whole log
  read to answer.

## 1.0.121, 12 August 2026

- Naming the session log for its session left the old un-timestamped
  HomerScribe.log sitting beside it, stale. It is the obvious name to reach for,
  so it was reached for, and a fresh run was compared against a two day old one.
  An older log of that name is now moved aside as HomerScribe-superseded.log the
  first time a timestamped one is written, so the only HomerScribe.log left in a
  results folder is the per-film one, which is current by construction.
- The results box now ends with the path of this run's log, so the right file is
  named rather than guessed at.

## 1.0.120, 11 August 2026

- Each finished film now gets its own log, in its own results folder, holding
  only the entries that belong to it. Somebody looking at one film's results
  should not have to search a whole session for the part about it.
- The session log is named for when the session began --
  HomerScribe-20260811-134216.log -- so a later run never erases an earlier one,
  and it is opened for appending rather than rewriting. It was already flushed
  to disk every second, so it stays current while a run is going on.

## 1.0.119, 11 August 2026

- Removed the batch look-ahead added in the previous version. Films are worked
  on one at a time again.
- Within one film, the ffmpeg work for the next moment is now done while the
  model works on this one: cutting and tiling the frames, and reducing them to a
  thumbprint. That work depends only on the film and a timestamp, so doing it
  early alters nothing the model is shown -- the same frames, the same picture,
  the same prompt. It is the only saving inside a single film that costs
  nothing.
  The ready file is renamed into place rather than juggling two path variables
  through a loop with many exits, and the run waits for the thread before
  finishing so nothing is left writing.

## 1.0.118, 11 August 2026

- On a batch, the next film is transcribed while the current one is described.
  The two use different hardware, so neither waits for the other: about 1.6 times
  quicker per film, 120 minutes becoming roughly 75.
  It was chosen over the alternatives for what it does NOT touch. The background
  work writes one file -- the next film's transcript, in that film's own folder
  -- and shares nothing else. When that film's turn comes the ordinary path finds
  it, exactly as it finds the transcript of an interrupted run. Not one line of
  the describing path changed. If it fails or is unfinished, that film is
  transcribed as usual.
- It is started only once the current film's descriptions begin, by which time
  that film's own transcript is made, so two transcriptions never contend for the
  processor.

## 1.0.117, 11 August 2026

- The build no longer crawls when it fetches ffmpeg. PowerShell's
  Invoke-WebRequest draws a progress meter unless told not to, and for a hundred
  megabyte file that meter costs far more than the transfer: tens of times
  slower, and it is what prints about writing a request stream. Every download
  now sets $ProgressPreference to SilentlyContinue first.
- And it usually will not download at all. Building in a second folder is the
  ordinary case rather than a rare one, so the build looks for ffmpeg, ffprobe
  and yt-dlp in an existing HomerScribe folder and copies them. They are large
  and change rarely; fetching them again to sit beside a second copy of the same
  source is waste.

## 1.0.116, 11 August 2026

- Two HomerScribes may now be run at once without any care being taken. The
  second notices the first, writes its own log rather than overwriting it, and
  does not save the shared settings while the other may be reading them. Working
  folders were already separate. This is worth doing because transcribing uses
  the processor and describing the graphics card, so two runs on halves of a list
  overlap: about 1.4 times quicker on four films, 1.5 on eight, with a ceiling of
  1.6.
- Fixed a check added two versions ago that would have blocked exactly this. It
  asked whether a process named HomerScribe was running, which matches one in ANY
  folder -- including the case where building is entirely safe because the file
  being written is a different file. The only question that matters is whether
  this file can be opened for writing, so that test is now the authoritative one
  and the process check merely explains the answer.

## 1.0.115, 11 August 2026

- The opening no longer goes quiet after "Starting". Between that word and the
  first message about the actual work, HomerScribe finds its programs, asks
  yt-dlp its version, looks for Whisper, asks Ollama for its model list, reads a
  list file and expands a playlist -- any of which can take seconds, and all of
  which was silent. Each step now says what it is about to do before doing it,
  so the longest silence is one step rather than all of them.
- The moment the sources are known it says how many there are and names the
  first, which is the confirmation that the program understood what it was
  given.
- Names are said as a person would say them: no folders, no extension, and
  underscores read as the spaces they stand for. A file called
  The_Africans_-_Episode_1.mkv is announced as "The Africans - Episode 1".

## 1.0.114, 11 August 2026

Written after a review of the whole project, which is in Review.md.

- The montage no longer samples the gap. It sampled frames from the start of the
  gap to its end -- but a gap is a pause in the speech, frequently the least
  eventful part of a film, and what the listener needs described usually happened
  while somebody was talking just before it. The published systems separate the
  INTERVAL being described from the PLACEMENT PERIOD it is spoken in; this
  program was conflating them and describing the pause. The window now runs back
  from the end of the gap far enough to cover the run-up, never shorter than the
  gap itself. This is the change most likely to improve the descriptions
  themselves rather than their placement.
- Every run now logs "This build does:" and the names of the behaviours compiled
  into it. Five rounds of analysis in this project were spent on logs from builds
  that did not contain the change being analysed, because nothing in a log said
  what was in the build and a version number says only when it was made.
- Added Review.md: why the same kinds of mistake recurred, and what now prevents
  each. Five kinds, each with its safeguard.

## 1.0.113, 10 August 2026

Two of my own decisions reversed by measurement.

- The frames go back to 512 pixels from 768. I raised them arguing that quality
  comes before time; the measurement says 2.25 times the pixels cost 3.9 times
  the time -- attention is quadratic, so image tokens are worse than linear --
  and bought nothing detectable. The same film took 6 hours 6 rather than 2
  hours, with overlap and interpretive language unchanged. The setting now
  records that cost so the next person to reach for it knows.
- A moment placed on the timer started at the MIDDLE of the quiet that had been
  measured for it, so a description had half the room the program thought it
  had, and the second half ran into the speech. That is where the residual 6.6
  percent came from, and why the rule added in the previous version to drop an
  overrunning description never once fired: the overrun was not detected,
  because the room was recorded as larger than what remained. The moment now
  starts where the quiet starts, keeping the same margin a natural gap keeps,
  and is given the whole of the quiet that was measured.
- placement_test.py shared the same fault and has been corrected. It now reports
  nought overlap from 4 percent speech to 93.

## 1.0.112, 10 August 2026

Measured on the same film across three runs as the floor was corrected:

    min-gap   descriptions   over speech
      2s          315          38  (12.1%)
      4s          315          38  (12.1%)
      5s          279          17  ( 6.1%)

Speech occupies 11.1 percent of that film, so at the old floor a description
landed on speech as often as speech occurred -- chance. It now lands on it about
half as often as chance would give, for 36 fewer descriptions out of 315, all of
which would have been spoken over the dialogue.

- The remaining overlap had one cause: the ladder that shortens a description to
  fit its gap said it anyway when it could not. Those seventeen ran past their
  gap into the speech. A description that will not fit AND would be spoken over
  the dialogue is now dropped. Running past the end into more silence is
  harmless and still allowed, and the two cases are told apart rather than
  treated alike.

## 1.0.111, 10 August 2026

- Added placement_test.py, which runs the real placement rules against generated
  speech patterns from 4 percent speech to 93. Judging the algorithm from runs
  on particular films cannot tell a general improvement from one that happens to
  suit those films; this can.
- It immediately found what no run had. With the floor at four seconds, a
  talkative film put 21 percent of its descriptions over speech -- because a
  description cannot be cut below about twelve words, and four seconds does not
  hold twelve words, so the words overran the room they were given.
  --min-gap is now five seconds, above the shortest description the program is
  willing to say. Across the whole range the figure is nought.

## 1.0.110, 10 August 2026

The room rule ran for the first time, and its own figures showed its floor was
wrong.

- A timer-placed moment needed two seconds of quiet, while a description takes
  four and a half at its shortest and eight at its usual length. So a moment was
  accepted with room for a quarter of what would be said in it and the rest ran
  into the speech. On a film that is 11.1 percent speech, 12.1 percent of the
  descriptions landed on speech: the placement was doing no better than chance
  at the one thing it exists to do.
- The floor is now --min-gap, the same requirement a natural pause has to meet.
  There was never a reason for a timer-placed description to need less room than
  any other; it is the same description.
- One fewer number to be wrong about: the separate two-second constant is gone.

## 1.0.109, 10 August 2026

- Force overwrite cleared the record from disk and left it in memory. The
  already-done check reads that record BEFORE the clearing happens, so the
  moments it held survived the deletion of the file they came from, and the run
  reused moments whose record no longer existed -- announcing "(reused)" while
  doing it.
  This is why the room rule has never once run: every run since it was written
  reused moments worked out before it existed. The memory is now discarded with
  the files, and reset between films.
- Measured from that run, on a two hour forty-five minute film at 11 percent
  speech: 304 descriptions, 36 of them over speech (11.8 percent), the film
  memory refreshed twelve times, and 12.4 seconds a description against 9.7
  before -- the cost of the wider frames.

## 1.0.108, 10 August 2026

- Force overwrite did not apply to the transcript. It was reused whenever the
  file existed, so the one part of the work that takes twenty minutes was the
  one part force did not redo. It now redoes it, and clears the described film,
  the script, the transcript, the interleaved account and the records before
  starting -- everything HomerScribe itself wrote, and nothing else, since a
  folder may hold a context file or notes that are the user's.
- The model now carries a memory of the film it is describing. It cannot learn:
  the model is fixed and forgets everything between calls. But every 25
  descriptions the same model is asked, with no picture, to write 70 words on
  what the film has established -- who keeps appearing, where it takes place,
  what is going on -- and that note goes into every later description with an
  instruction to take it as known. Before this, the hundred and fiftieth
  description knew the two before it and nothing of the other 148.

## 1.0.107, 10 August 2026

- When several sources ask you to sign in, the results now say so once and name
  the setting that answers it. One run had 34 refusals of 81 quoting yt-dlp's own
  advice to use cookies from a browser, which HomerScribe has a setting for; the
  advice was repeated in yt-dlp's terms eighty times and in the program's terms
  never.
- Recorded from that run, which the new status words made legible in nine
  minutes: of 106 videos in that playlist, 25 were already described and 81 could
  not be fetched -- 40 blocked by country, 29 private, 5 needing a sign-in, 7
  withdrawn. The playlist holds 25 usable films and all 25 are done.

## 1.0.106, 10 August 2026

- Added the Resuming status. A film described in part by an earlier run has
  those descriptions read back and only the rest asked for -- the record is
  written after every single description, so an interrupted run loses at most
  one -- but it said so only in the log and on a console that is hidden. It now
  announces "Resuming", with how many descriptions it is carrying forward.
- A transcript made in an earlier run announces the same thing, since not
  listening to an hour of film again is the largest single saving there is and
  the silence while it did not happen was indistinguishable from the silence
  while it did.

## 1.0.105, 10 August 2026

- A source that is skipped, refused or fails now says one word: Skipped, Error
  or Rejected, followed by the reason. Every one of those endings was silent, so
  a run said "Processing 7 of 98 files" and then nothing until the next -- which
  on a playlist where two videos in five have been withdrawn sounds like a
  program racing through work it is not doing. Seven separate paths ended that
  way and all seven now speak.
- The word is the kind of the announcement, so a screen reader hears it before
  the name, and a run of refusals groups into one message rather than
  interrupting once each.

## 1.0.104, 10 August 2026

One rule where there were three thresholds.

- A talkative rule at 70 percent, a crowded rule at 85, and a fixed interval
  underneath both were all proxies for a single question that can be asked
  directly: is there room here for anything to be heard? The code already
  measured the quiet at each point it considered and then placed a description
  regardless. It now declines when there is less than two seconds of it.
- The thresholds become unnecessary. A film with room gets many descriptions, a
  film without gets few, and nothing has to be classified in advance: about 240
  an hour at 10 percent speech, 210 at 35, 145 at 80, and 27 at 95.
- The time spent is now proportional to what there is to gain, which is the
  answer to "80 percent of the quality in the least time": the moments that were
  costing ten seconds each to produce a description nobody could hear are simply
  not attempted.

## 1.0.103, 10 August 2026

Quality before time, as asked.

- Each frame is now 768 pixels wide rather than 512. Everything the program does
  afterwards -- the two passes, the checks, the context -- works on what the
  model could see, and it was reading faces, signs and gestures out of a 512
  wide picture. This costs time in the model and nothing in correctness, which
  is the trade that was asked for.
- Added measure.py, which reads a results folder and prints a short report:
  descriptions, how many landed on speech, their length, spacing, how many judge
  rather than observe, and a sample. Two kilobytes instead of a twenty megabyte
  log, so a question about quality can be answered without uploading a run.

## 1.0.102, 10 August 2026

The entries between 1.0.39 and 1.0.101 were lost. Each was written as a
replacement against the entry above it, and when that replacement found nothing
to match it did nothing and said nothing -- the same silent no-op that has
caused two build failures in this project. Twenty-six entries survived out of
about ninety. Rather than invent the missing ones, the work of that period is
recorded here in one place, and the numbering now matches what a build actually
produces.

What changed between 1.0.39 and 1.0.102:

- The program became HomerScribe, describing video and transcribing speech,
  either or both, with scribed.md interleaving them in the order they happen.
- Descriptions are placed by listening for SPEECH rather than for silence, using
  Whisper. Above 70 percent speech the spacing widens to 45 seconds; above 85
  percent nothing is placed on the timer at all, because a description spoken
  over narration costs the listener the narration too.
- A gap must be at least 4 seconds to count as room. Two seconds holds five
  words, which is a fragment that runs into the speech that follows.
- Whisper's repetition loop is filtered out, and a transcript that comes back
  almost empty is recognised rather than believed.
- Subtitles are refused three ways: the frame is cropped, the rules forbid it,
  and what comes back is checked for a language the film is not spoken in or for
  text that repeats the dialogue.
- Descriptions that judge rather than observe have the judging word, clause or
  sentence removed, provided what remains is still a sentence.
- The presenter is named, since a vision model cannot recognise a face: whoever
  addresses the viewer is the presenter, and saying so took one documentary from
  naming nobody to naming him in 53 of 94 descriptions.
- Progress is spoken through a UIA live region on the dialog's status line
  rather than by timed message boxes, so the keyboard is never taken; only while
  HomerScribe is the window in front, though the title and status line stay
  current regardless.
- Sources may be files, wildcards, web addresses, playlists, or a text file
  listing any of those. Everything that could not be used is named in the
  results with its reason.
- The log opens before anything can fail and grows visibly on disk.

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
