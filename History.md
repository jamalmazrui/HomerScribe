# HomerScribe History

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
