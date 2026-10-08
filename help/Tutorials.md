# HomerScribe -- Tutorials

**Version 1.0**  
September 2026  
Copyright 2026 by Jamal Mazrui  
MIT License

Seven short walks, each a real job done from start to finish, each about three
minutes. Take the overview and walk one first; the rest go in any order.

**Simulations, made with AI.** One synthetic voice does the work; a second
answers as a screen reader would. The screen reader's lines are written the
way a reader speaks at its middle verbosity setting -- the name, the kind of
control, its state, and the access key -- so what you hear in the walk is what
you will hear on your own machine. You always know which voice is talking.

Every key is named in full the first time it appears, with the word it comes
from: Alt plus T is Transcribe, T for Transcribe.

## How to listen

The audio is in `help\tutorials`, one `.mp3` per walk, named as the walks are
numbered. Open the folder and play the one you want in any media player. In
FileDir, put the cursor on `Tutorials.m3u` there and press Control+Shift+H for
the Homer Player: it lists all seven as tracks, and Control+Page Down and
Control+Page Up move between them.

The written version of every walk follows below, and `TutorialFeed.xml` beside
this file is a podcast feed of the same audio.

<!-- walkthrough: written by makeTutorials.py, do not edit between the markers -->

## 0 - Overview

A simulated walk through HomerScribe, made with AI: a person working, and a screen reader answering.

**Before you start:** Nothing yet. This one is two minutes of what to expect.

### Step 1

HomerScribe listens to a film or a recording and writes down what is said, and looks at a film and writes down what can be seen. It runs on your own computer. Nothing is uploaded.

Everything it makes is a document you can read with any screen reader.

### Step 2

Two voices take these walks. I am the narrator, the person doing the work. The other voice is the screen reader, and it says only what a screen reader would say. You always know which of us is talking.

The reader's lines are written the way a screen reader speaks at its middle verbosity setting: name, kind of control, state, then the access key.

### Step 3

The walks are numbered from zero to nine. I say the number and the title; the reader says what the walk covers.

### Step 4

One, User Interface.

Screen reader:

- The dialog and its parts, the Alt letters that tick each box, the keys that read a field, and where help lives.

### Step 5

Two, Install and Launch.

Screen reader:

- The installer, the boxes on its last page for the tools HomerScribe needs, and the key that opens it.

### Step 6

Three, Transcribe a Recording.

Screen reader:

- An hour of speech becomes a document you can search.

### Step 7

Four, Describe a Video.

Screen reader:

- A film gets a written description of what can be seen.

### Step 8

Five, A Web Address, Sound Only.

Screen reader:

- Work from a web address, fetching only the sound.

### Step 9

Six, Remove the Advertisements.

Screen reader:

- A podcast episode without its advertisements.

### Step 10

Seven, Read a PDF.

Screen reader:

- A PDF becomes a document a screen reader reads well.

### Step 11

Eight, Stay Current.

Screen reader:

- The help box, and updating to a newer version.

### Step 12

Nine, Conclusion.

Screen reader:

- What to carry away, the words HomerScribe uses, and where to learn more.

### Step 13

Ten walks, about three minutes each. Take one and two first; the tasks go in any order.

### Step 14

Every key is named with the word it comes from, so the key and the command stay together in memory. Alt plus T is Transcribe, T for Transcribe. Alt plus D is Describe, D for Describe.

In a Homer dialog, the Alt key for a control is the underlined letter of its label, and the reader says it after the control's name.

### Step 15

One key before anything else. If the reader says something and you miss it, Insert Up Arrow says the current line again. That is the screen reader's own key, not HomerScribe's. Use it freely; nothing in these walks depends on catching a line the first time.

Insert plus Up Arrow is Say Line in most screen readers; some use their own modifier key in place of Insert.

### Step 16: Insert+Tab

Its partner: Insert Tab says where focus is, the control's name, kind and state. When you are not sure where you are, it answers without moving you.

Insert plus Tab is Say Focus in most screen readers. Here, with nothing open yet, it reads whatever the desktop has in focus.

### Step 17

And one habit. When a window changes, I press Insert T to read its title, to verify where I am. You will hear me do it in every walk. It costs a second and it has saved me an hour.

Insert plus T is Say Window Title in most screen readers.

### Step 18: Alt+Control+H

HomerScribe opens from the desktop with Alt plus Control plus H, H for HomerScribe. Let me press it, so you hear what the start of every walk sounds like.

Screen reader:

- HomerScribe dialog
- Source paths: edit, https colon slash slash www dot youtube dot com slash watch question mark v equals 3 f 31 o u f q F S M

The Source paths box starts with a sample video already in it: the W3C's Perspectives films, seven and a half minutes, freely licensed and published with professional descriptions, so a first run can be compared with how it should be done.

### Step 19: Escape

The reader said the window is a dialog, and focus is in the Source paths edit box, which already holds a web address. That address is a sample film, and it is what walk four uses. I will close the dialog now with Escape.

Escape is Cancel in every Homer dialog. The reader says nothing, because the desktop that focus returns to is where it was before.

### Step 20

The reader did not say anything, and that is right. Escape closed the dialog and focus went back to where it was. Silence after a key is information too, and I will always tell you when it is the correct answer.

When nothing is announced after a key, verify with Insert T if you are unsure.

**Something to try:** Take walks one and two first; then pick the task that matches what you want to do today.

## 1 - User Interface

HomerScribe is one dialog. Each part is named by the narrator and then shown by the screen reader: the boxes for what to work on and where to write, the boxes that say what to do, the keys that read a field, the keys that report progress, and the four kinds of help.

**Before you start:** HomerScribe is installed.

### Step 1

HomerScribe is one dialog, built the way every Homer dialog is: a label, then the control it names. I name each part; the reader shows it, as it would on your machine.

### Step 2: Alt+Control+H

Alt plus Control plus H, H for HomerScribe, opens it from anywhere.

Screen reader:

- HomerScribe dialog
- Source paths: edit, https colon slash slash www dot youtube dot com slash watch question mark v equals 3 f 31 o u f q F S M

### Step 3: Shift+F1

The window's name, then where focus is: Source paths, an edit box, holding the sample film's address. Here go the files, folders, wildcard patterns or web addresses to work on. Shift plus F1 reads any field's tip.

Screen reader:

- One or more files, wildcard patterns, or web addresses to download from, separated by spaces. Put double quotes around any item containing a space.

Confirm the wording with a live run: buildTutorials -live.

### Step 4: Alt+F8

Every field has a tip, in the same words the help box uses. Alt plus F8 reads the whole field aloud, or says Blank.

Screen reader:

- https colon slash slash www dot youtube dot com slash watch question mark v equals 3 f 31 o u f q F S M

Confirm the wording with a live run: buildTutorials -live.

### Step 5: Tab

Tab moves in the order the controls were added: Browse source, then the output folder, then the boxes that say what to do.

Screen reader:

- Browse source... button, Alt plus B

Confirm the wording with a live run: buildTutorials -live.

### Step 6: Alt+T

Now the rule behind every key here: the word gives the letter. Alt plus a letter goes straight to its control, and for a check box it ticks or clears it. Alt plus T, T for Transcribe.

Screen reader:

- Transcribe audio check box, checked, Alt plus T

### Step 7: Alt+T

Checked. Again, and it is cleared.

Screen reader:

- Transcribe audio check box, not checked, Alt plus T

Confirm the wording with a live run: buildTutorials -live.

### Step 8: Alt+O

D for Describe, A for Audio only, W for Web context, R for Remove ads, F for Force overwrite, L for Log session. Alt plus O goes to the Output directory box, O for Output.

Screen reader:

- Output directory: edit

Confirm the wording with a live run: buildTutorials -live.

### Step 9: F7

Where the documents are written; blank means beside each source. A dialog this size has many controls, so F7 lists them all; a letter jumps to one and Enter goes to it.

Screen reader:

- Controls list box

Confirm the wording with a live run: buildTutorials -live.

### Step 10: Escape

Escape closes the list and returns where I was. In the dialog itself, Enter presses OK from anywhere, Control plus Enter from inside a box of many lines, and Escape cancels.

Screen reader:

- Output directory: edit

Confirm the wording with a live run: buildTutorials -live.

### Step 11: Insert+T

While the work runs, the dialog stays, its controls resting, and its title says what is happening. Two keys ask how it goes: Shift plus F6 says how far through the current file; Control plus Page Down and Page Up move between its stages. And Insert plus T, the reader's own key, reads the title.

Screen reader:

- HomerScribe dialog

### Step 12

Help is in four places, the same in every Homer program. I say which; the reader says the key. The help box, listing every field and key, and the version.

Screen reader:

- F1, the help box

### Step 13

The tip for the field you are on.

Screen reader:

- Shift plus F1

### Step 14

A list of the controls, to reach any one.

Screen reader:

- F7

### Step 15

And the guide and the hotkeys, as documents in the Start menu.

Screen reader:

- HomerScribe documentation

### Step 16: Alt+O

A planned misstep: Alt plus O is the Output box, so pressing it to accept does not press OK. I press it, and the reader shows where I landed.

Screen reader:

- Output directory: edit

Confirm the wording with a live run: buildTutorials -live.

### Step 17

The answer is Enter, which presses OK from anywhere. What this walk taught. I name the part; the reader gives the key.

### Step 18

Tick Transcribe audio.

Screen reader:

- Alt plus T

### Step 19

Hear a field's tip.

Screen reader:

- Shift plus F1

### Step 20

Hear a whole field.

Screen reader:

- Alt plus F8

### Step 21

Accept from anywhere.

Screen reader:

- Enter

**Something to try:** Open HomerScribe, Tab through every control once, and press Shift plus F1 on each to hear its tip.

## 2 - Install and Launch

HomerScribe_setup.exe installs HomerScribe and, on its last page, offers the tools it uses: Whisper for transcribing, Ollama and a vision model for describing, and the rest. Then the desktop key opens it.

**Before you start:** HomerScribe_setup.exe is downloaded from the HomerScribe page on GitHub.

### Step 1

One want: HomerScribe on this computer, with the tools it needs, in one sitting. Everything it uses runs here; once installed, nothing is uploaded.

### Step 2: Enter

I open the downloaded setup program. Windows asks for administrator rights; I say yes.

Screen reader:

- Setup - HomerScribe dialog
- Welcome to the HomerScribe Setup Wizard

### Step 3: Enter

The ordinary installer: Enter takes Next on each page, and the defaults are right.

Screen reader:

- Select Destination Location

Confirm the wording with a live run: buildTutorials -live.

### Step 4: Enter

I take the defaults to the end, where it copies the program and its documents.

Screen reader:

- Installing

Confirm the wording with a live run: buildTutorials -live.

### Step 5: Tab

The last page is the one that matters. It offers each tool HomerScribe uses, as a box saying what it does and its size, already ticked when it is missing.

Screen reader:

- Install Whisper check box, checked

Confirm the wording with a live run: buildTutorials -live.

### Step 6: Down Arrow

Each box's first word is the machine's state read for you: Install when it is missing, Update when a newer one is out, Reinstall when it is current. Transcribing needs Whisper alone, about half a gigabyte.

Screen reader:

- Install Ollama check box, checked

Confirm the wording with a live run: buildTutorials -live.

### Step 7: Down Arrow

Describing needs more: Ollama, the service that runs the model, and the vision model itself, about five and a half gigabytes, downloaded once.

Screen reader:

- Install qwen2.5vl 7b check box, checked, describes video and pictures, about 5.5 GB

Confirm the wording with a live run: buildTutorials -live.

### Step 8: Space

The text model, about four point seven gigabytes, is only for removing advertisements. A planned misstep: if you will never do that, clear it. Space clears a box.

Screen reader:

- not checked

Confirm the wording with a live run: buildTutorials -live.

### Step 9: Enter

Leave the rest as they are. Enter starts them, one after another, each in a window that says what it is fetching.

Screen reader:

- Finish button

Confirm the wording with a live run: buildTutorials -live.

### Step 10: Alt+Control+H

When they finish, a results box says what was installed and where. Then the key that opens HomerScribe from anywhere: Alt plus Control plus H.

Screen reader:

- HomerScribe dialog
- Source paths: edit, https colon slash slash www dot youtube dot com slash watch question mark v equals 3 f 31 o u f q F S M

### Step 11

Installed and open, with the sample film ready. If a tool was left out, run the setup again and tick its box; a tool already present costs nothing. What this walk taught. I name the step; the reader gives the answer.

### Step 12

What a box's first word tells you.

Screen reader:

- Install, Update or Reinstall

### Step 13

What transcribing needs.

Screen reader:

- Whisper

### Step 14

The key that opens HomerScribe.

Screen reader:

- Alt plus Control plus H

**Something to try:** Run the setup a second time. Every box on the last page should now say Update or Reinstall, not Install.

## 3 - Transcribe a Recording

A one-hour MP3 of a screen reader class becomes a document of what was said.

**Before you start:** A recording named Introduction_to_Windows.mp3 is in the Videos folder. HomerScribe is installed.

### Step 1

A friend sent me a recording of a screen reader class, an hour long, and I want it as text I can search. That is a transcription, and it is HomerScribe's simplest job.

### Step 2: Alt+Control+H

Alt plus Control plus H, H for HomerScribe, opens the dialog from anywhere.

Screen reader:

- HomerScribe dialog
- Source paths: edit, https colon slash slash www dot youtube dot com slash watch question mark v equals 3 f 31 o u f q F S M

### Step 3: Control+A

Focus is in Source paths, holding the sample address. I will select it all and type my own file's path over it.

Screen reader:

- selected https colon slash slash www dot youtube dot com slash watch question mark v equals 3 f 31 o u f q F S M

### Step 4

Now the path. I type it, and the reader echoes as I go, so I will say it plainly instead: C colon, backslash Users, backslash Jamal, backslash Videos, backslash Introduction underscore to underscore Windows dot m p 3.

Type the full path, or press Alt plus B, B for Browse source, and pick the file from a standard Open dialog instead.

### Step 5: Alt+T

Next, say what to do with it. Alt plus T is Transcribe audio, T for Transcribe.

Screen reader:

- Transcribe audio check box, checked, Alt plus T

Alt plus a letter toggles a check box directly; you do not have to Tab to it first.

### Step 6: Enter

The reader said checked. That is the whole request: one file, one box. The output goes to the Videos folder unless I change it, and I will not. Enter presses OK from anywhere in the dialog.

Screen reader:

- HomerScribe, working
- Initializing, 1 hour and 6 minutes long.

The dialog stays on screen for the whole run with its controls disabled, and its title says what is happening. Alt plus Tab always finds it.

### Step 7

Two things happened. The window's title changed to HomerScribe, working, and an announcement said how long the recording is: an hour and six minutes. Hearing the length first is how you know it opened the right file.

### Step 8

Now it listens. Every so often it says how far it has got, as a place in the recording and a percentage. I will let one go by.

Screen reader:

- Initializing, 30 min, 45%

On a processor without a graphics card, transcribing takes about a quarter of the recording's own length. An hour of speech is a quarter of an hour of waiting.

### Step 9: Insert+T

Thirty minutes into the recording, forty five percent done. If I am doing something else and miss one of these, Insert Up Arrow repeats the last line the reader said. Let me verify where I am with Insert T.

Screen reader:

- HomerScribe, working

### Step 10

Still working. When it finishes, it names the file, and then a results box tells me what it made.

Screen reader:

- Done. Introduction to Windows dot m p 3
- HomerScribe results dialog
- One source done. Took 19 minutes. Introduction to Windows dot m p 3: transcript of 1682 spoken stretches. C colon backslash Users backslash Jamal backslash Videos backslash Introduction to Windows backslash transcribed dot m d. Log of this run: C colon backslash Users backslash Jamal backslash Videos backslash Introduction to Windows backslash HomerScribe dot log.
- OK button

The results box says what was done, how long it took, and where the document is, with a log beside it.

### Step 11: Enter

Nineteen minutes for an hour of speech, and 1682 spoken stretches written into transcribed dot m d, in a folder named for the recording. Enter presses OK.

Screen reader:

- HomerScribe dialog
- Source paths: edit

OK closes the results box and focus returns to the dialog, controls enabled again, ready for the next job.

### Step 12

The transcript is a Markdown document. Its first heading names the recording; then comes what was said, in order, with a heading every ten minutes so you can jump by heading. Names that Whisper has never read are spelled as written in the vocabulary setting, so the names of screen readers and programs come out spelled right rather than as they sound.

If a recording is full of names it gets wrong, add them to the vocabulary setting in the configuration, separated by commas.

**Something to try:** Transcribe a podcast episode of your own. Read the transcript's first heading: it names the recording and how long it runs.

## 4 - Describe a Video

The sample film in the Source paths box gets descriptions of every shot, as a document and as a described copy.

**Before you start:** HomerScribe is installed, with Ollama and its picture model. The dialog still holds the sample address.

### Step 1

This time I want to know what is on screen. HomerScribe looks at the film, one moment at a time, and writes a description for each, using a picture model on my own machine.

### Step 2: Alt+Control+H

Alt plus Control plus H opens the dialog.

Screen reader:

- HomerScribe dialog
- Source paths: edit, https colon slash slash www dot youtube dot com slash watch question mark v equals 3 f 31 o u f q F S M

### Step 3: Alt+D

I will leave the sample address as it is. It is the W3C's Perspectives film, seven and a half minutes, published with professional descriptions, so I can judge the result against the real thing. Alt plus D is Describe video, D for Describe.

Screen reader:

- Describe video check box, checked, Alt plus D

Alt plus D is shared with Default settings. Windows cycles between the two; if the reader names Default settings, press Alt plus D once more.

### Step 4: Alt+T

Checked. I also want the words that are spoken, woven in with the descriptions, so Alt plus T as well.

Screen reader:

- Transcribe audio check box, checked, Alt plus T

### Step 5: Enter

Both boxes are checked. Enter presses OK.

Screen reader:

- HomerScribe, working
- Initializing, Downloading Perspectives colon Video Captions

A web address is downloaded first. HomerScribe fetches yt-dlp and ffmpeg itself when it needs them.

### Step 6

It is downloading the film, and it named it from the page. Next it listens for where the speech is, then it looks. The announcements change their first word as the job changes.

Screen reader:

- Initializing, 7 min, 100%
- Describing, 1 of 42

### Step 7

Describing one of forty two. Each description is spoken as it is made, so you can follow along or go and do something else. Here is one.

Screen reader:

- Describing, 12 of 42, A woman with short grey hair sits at a kitchen table, a laptop open in front of her, morning light from a window on her left.

Every description is also written into the document, so nothing depends on hearing it now.

### Step 8

When the last description is written, it puts the film back together with the descriptions as a spoken track, and says so.

Screen reader:

- Finalizing, writing described dot m k v
- Done. Perspectives colon Video Captions
- HomerScribe results dialog
- One source done. Took 9 minutes. Perspectives colon Video Captions: 42 descriptions, 7 minutes. C colon backslash Users backslash Jamal backslash Videos backslash Perspectives Video Captions. Log of this run: C colon backslash Users backslash Jamal backslash Videos backslash Perspectives Video Captions backslash HomerScribe dot log.
- OK button

### Step 9: Enter

Forty two descriptions for a seven minute film, in nine minutes. Enter presses OK.

Screen reader:

- HomerScribe dialog
- Source paths: edit

OK closes the results box and focus returns to the dialog, controls enabled again, ready for the next job.

### Step 10

The folder holds described dot m k v, the film with the description as its first sound track; described dot m d, the descriptions as a document; described dot v t t, the same as timed captions; transcribed dot m d, the words; and scribed dot m d, words and descriptions together in the order they happen. Read scribed dot m d for the whole film in one document.

described dot m k v opens in any media player. In FileDir, Control plus Shift plus H plays it in the Homer Player.

**Something to try:** Open described dot m d and read the first three descriptions. Then play described dot m k v and listen to the same three.

## 5 - A Web Address, Sound Only

A lecture on YouTube becomes one described MP3, a quarter the size of the video, with the page's own title guiding the descriptions.

**Before you start:** HomerScribe is installed, with Ollama. A YouTube address is on the clipboard.

### Step 1

A lecture on YouTube, an hour of slides and a speaker. I do not need the video; I need to know what the slides say, and I want one file I can put on my phone.

### Step 2: Alt+Control+H

Alt plus Control plus H opens the dialog, in Source paths as always.

Screen reader:

- HomerScribe dialog
- Source paths: edit, https colon slash slash www dot youtube dot com slash watch question mark v equals 3 f 31 o u f q F S M

### Step 3: Control+A

Control plus A selects the sample address, and Control plus V pastes mine over it.

Screen reader:

- selected https colon slash slash www dot youtube dot com slash watch question mark v equals 3 f 31 o u f q F S M

### Step 4: Control+V

Control plus V.

The reader is silent on a paste. Press Insert plus Up Arrow to read the line if you want to check it.

### Step 5: Alt+D

Silence on a paste is normal. Now the boxes. Alt plus D, Describe. Alt plus A, Audio only, A for Audio. Alt plus W, Web context, W for Web.

Screen reader:

- Describe video check box, checked, Alt plus D

### Step 6: Alt+A

Alt plus A.

Screen reader:

- Audio only check box, checked, Alt plus A

Audio only produces one described dot m p 3: the film's own sound with the descriptions mixed in, and no video. It is quicker to make and a quarter the size.

### Step 7: Alt+W

Alt plus W.

Screen reader:

- Web context check box, checked, Alt plus W

Web context reads the page's title and description first, so the model knows it is looking at a lecture on, say, tax law, and describes the slides as slides rather than guessing.

### Step 8: Enter

Three boxes checked. Enter.

Screen reader:

- HomerScribe, working
- Initializing, Downloading Introduction to Estate Planning, Lecture 4

### Step 9: Insert+T

It named the lecture from the page. That title is what Web context gives the model. Let me verify the window while it works, with Insert T.

Screen reader:

- HomerScribe, describing 17 of 88

### Step 10

Describing seventeen of eighty eight. The title carries the progress, so Insert T is all it takes to check on a long run from another window. I will let it finish.

Screen reader:

- Done. Introduction to Estate Planning, Lecture 4
- HomerScribe results dialog
- One source done. Took 22 minutes. Introduction to Estate Planning, Lecture 4: 88 descriptions, 58 minutes. C colon backslash Users backslash Jamal backslash Videos backslash Introduction to Estate Planning Lecture 4. Log of this run: C colon backslash Users backslash Jamal backslash Videos backslash Introduction to Estate Planning Lecture 4 backslash HomerScribe dot log.
- OK button

### Step 11: Enter

Enter presses OK. The folder holds described dot m p 3, one file with the lecture and its descriptions, and described dot m d to read.

Screen reader:

- HomerScribe dialog
- Source paths: edit

OK closes the results box and focus returns to the dialog, controls enabled again, ready for the next job.

**Something to try:** Give HomerScribe the address of a playlist. It says "Processing 3 of 9" as it goes, and each film gets its own folder.

## 6 - Remove the Advertisements

A podcast episode loses its advertisements and keeps everything else, with the original untouched.

**Before you start:** HomerScribe is installed, with Ollama and its reading model. An episode named Episode_212.mp3 is in the Videos folder.

### Step 1

A podcast I like carries four minutes of advertisements in every hour. HomerScribe can find them and cut them, and it is careful about it: nothing is cut unless the model is at least ninety five out of a hundred sure.

### Step 2: Alt+Control+H

Alt plus Control plus H.

Screen reader:

- HomerScribe dialog
- Source paths: edit, https colon slash slash www dot youtube dot com slash watch question mark v equals 3 f 31 o u f q F S M

### Step 3: Alt+B

Alt plus B, B for Browse source, opens a standard Open dialog, which is easier than typing a path.

Screen reader:

- Choose a file to work on dialog
- File name: edit

### Step 4: Enter

I type Episode underscore 212 dot m p 3 and press Enter. The Videos folder is where the dialog starts.

Screen reader:

- HomerScribe dialog
- Source paths: edit, C colon backslash Users backslash Jamal backslash Videos backslash Episode 212 dot m p 3

Browse puts the chosen path into Source paths and returns focus there.

### Step 5: Alt+R

The path is in the box, and focus came back to it. Alt plus R is Remove ads, R for Remove.

Screen reader:

- Remove ads check box, checked, Alt plus R

### Step 6: Enter

Checked. Enter.

Screen reader:

- HomerScribe, working
- Initializing, 58 minutes long.

### Step 7

Fifty eight minutes. It listens to the whole episode first, then reads what was said and asks the model where the advertisements are. That reading is the slow part.

Screen reader:

- Initializing, 58 min, 100%
- Reading, where the advertisements are

### Step 8

And the result.

Screen reader:

- Done. Episode 212 dot m p 3
- HomerScribe results dialog
- One source done. Took 16 minutes. Episode 212 dot m p 3: 3 advertisements removed, 4 minutes of 58 minutes. C colon backslash Users backslash Jamal backslash Videos backslash Episode 212 backslash stripped dot m p 3. Log of this run: C colon backslash Users backslash Jamal backslash Videos backslash Episode 212 backslash HomerScribe dot log.
- OK button

### Step 9: Enter

Three advertisements, four minutes of fifty eight, written as a new file named stripped, in the episode's own folder. The original is untouched. Enter.

Screen reader:

- HomerScribe dialog
- Source paths: edit

OK closes the results box and focus returns to the dialog.

### Step 10

Beside the new file is a document saying what was cut, where each cut was, and what the model heard that made it sure. If it cut something it should not have, that document is how you would know.

If nothing was ninety five percent certain, nothing is cut, and the document says so.

**Something to try:** Run it on an episode with a mid-roll break. Read the summary line at the top of the document it writes: it says how much was cut and from where.

## 7 - Read a PDF

A PDF of a scanned report becomes a Markdown document, and a Word document beside it.

**Before you start:** HomerScribe is installed, with Ollama, Tesseract and Pandoc. A file named Annual_Report.pdf is in the Videos folder.

### Step 1

A scanned report came as a PDF with no text in it, just pictures of pages. HomerScribe reads the pages, recognises the text, describes any pictures, and writes a document.

### Step 2: Alt+Control+H

Alt plus Control plus H. Then Alt plus B to browse.

Screen reader:

- HomerScribe dialog
- Source paths: edit, https colon slash slash www dot youtube dot com slash watch question mark v equals 3 f 31 o u f q F S M

### Step 3: Alt+B

Alt plus B.

Screen reader:

- Choose a file to work on dialog
- File name: edit

### Step 4: Enter

Annual underscore Report dot p d f, Enter.

Screen reader:

- HomerScribe dialog
- Source paths: edit, C colon backslash Users backslash Jamal backslash Videos backslash Annual Report dot p d f

### Step 5: Enter

A PDF needs no box ticked: HomerScribe knows what to do with one. Enter.

Screen reader:

- HomerScribe, working
- Preparing, Opening Annual Report dot p d f

### Step 6

Preparing. It counts the pages and checks whether they carry text. These do not, so it reads them with Tesseract, page by page.

Screen reader:

- Reading, Page 12 of 40

A PDF that already has text is read directly and is much quicker. Only scanned pages go through recognition.

### Step 7

Page twelve of forty. Pictures on a page get described as it goes.

Screen reader:

- Describing, Page 15, a bar chart of revenue by region, five bars, the tallest labelled North America

### Step 8

And the result.

Screen reader:

- Done. Annual Report dot p d f
- HomerScribe results dialog
- One source done. Took 11 minutes. Annual Report dot p d f: 40 pages read, 6 pictures described. C colon backslash Users backslash Jamal backslash Videos backslash Annual Report backslash Annual Report dot m d. Log of this run: C colon backslash Users backslash Jamal backslash Videos backslash Annual Report backslash HomerScribe dot log.
- OK button

### Step 9: Enter

Forty pages and six pictures, eleven minutes. Enter. The folder holds Annual underscore Report dot m d, and, because Pandoc is installed, Annual underscore Report dot d o c x with the same page breaks as the original.

Screen reader:

- HomerScribe dialog
- Source paths: edit

Without Pandoc, only the Markdown is written. The installer offers Pandoc as a box on its last page.

**Something to try:** Give it a PDF with pictures in it. Each picture gets a description where it sits in the text.

## 8 - Stay Current

F1 in the dialog lists every field and key, checks the web for a newer HomerScribe, and offers to install it.

**Before you start:** HomerScribe is installed and its dialog is open. The computer is online.

### Step 1

Every Homer dialog answers F1 with the same kind of help: the fields, what each is for, the keys that work anywhere in the dialog, and, at the end, which version this is and whether a newer one is out.

### Step 2: Alt+Control+H

Alt plus Control plus H opens the dialog. Then F1.

Screen reader:

- HomerScribe dialog
- Source paths: edit, https colon slash slash www dot youtube dot com slash watch question mark v equals 3 f 31 o u f q F S M

### Step 3: F1

F1.

Screen reader:

- Help: HomerScribe dialog
- Help text edit, read only, Fields in this dialog:

The help is a read-only text box, so you can arrow through it line by line, and Control plus F8 copies all of it.

### Step 4: DownArrow

Focus is in a read-only text box. Down Arrow reads it a line at a time. The first lines are the fields.

Screen reader:

- Source paths: edit, One or more files, wildcard patterns, or web addresses to download from, separated by spaces. Put double quotes around any item containing a space.

### Step 5

Each field, with its tip. Further down, the dialog keys.

Screen reader:

- Control plus Enter presses the accept button from anywhere.
- F7 lists the dialog's controls in navigation order; choose one and press OK to focus it.

F7 is the answer when a dialog has more controls than you want to Tab through: a list, a letter, Enter.

### Step 6: Control+End

And at the end, the version. Control plus End goes to the last line.

Screen reader:

- Update to the version on the web now? Yes fetches it and starts its setup program.

### Step 7: UpArrow

Up Arrow for the line before it.

Screen reader:

- This is version 1.0.239. Version 1.0.240 is on the web.

### Step 8: Enter

A newer version is out. The box's buttons are Yes and No, and because a newer version exists, Yes is the default: Enter takes it. If this were already the newest, No would be the default, and if the web could not be reached, there would be an OK button and nothing to decide.

Screen reader:

- Setup - HomerScribe dialog
- Welcome to the HomerScribe Setup Wizard

Yes fetches HomerScribe_setup.exe from the latest release and starts it. The installer asks for administrator rights, closes HomerScribe when it needs to, and its last page offers the same boxes as a first install.

### Step 9

The setup program is running. From here it is the ordinary installer: Enter for Next, and on the last page the boxes for anything that needs installing or updating are already ticked.

**Something to try:** Press F1 when there is no newer version. The box ends with "the newest on the web", and No is the default.

## 9 - Conclusion

The conclusion and summary of the walks; the words HomerScribe uses, in two voices; then more information: the help built into the dialog and where to learn more.

**Before you start:** Nothing is needed; this walk is listened to.

### Step 1

To finish: what to carry away, the words HomerScribe uses, and where to get help. First, the summary. Everything runs on this computer, and everything it makes is a document a screen reader reads well: headings to jump by, in the order things happen.

### Step 2

One dialog does every job, and one box says which: tick what you want, and Enter. The window stays while it works, and its title says how far it is.

Screen reader:

- Alt plus T, Transcribe audio
- Alt plus D, Describe video

### Step 3

Every run ends in a results box naming what it made and where, with a log beside it; when something is wrong, that log is what to send. And before committing to a feature film, describe five minutes of it.

### Step 4

Now the words, in alphabetical order. I say the term; the reader says what it means.

### Step 5

Audio only.

Screen reader:

- The box that fetches only the sound from a web address, for a transcript without the pictures.

### Step 6

description.

Screen reader:

- A sentence about what can be seen, written for a gap in the speech of a film.

### Step 7

gap.

Screen reader:

- A stretch of a film without speech, long enough for a description to be read in.

### Step 8

Ollama.

Screen reader:

- The service that runs the vision and text models on this computer.

### Step 9

Remove ads.

Screen reader:

- The box that finds a podcast's advertisements and cuts them from the sound.

### Step 10

results box.

Screen reader:

- The box at the end of every run: what was done, how long it took, and where the documents are.

### Step 11

Source paths.

Screen reader:

- The box for what to work on: files, wildcard patterns, folders or web addresses.

### Step 12

transcript.

Screen reader:

- The document of what was said, in order, with a heading every ten minutes.

### Step 13

vision model.

Screen reader:

- The model that looks at a film's pictures and writes the descriptions.

### Step 14

vocabulary.

Screen reader:

- The setting of names Whisper should spell right, separated by commas.

### Step 15

Whisper.

Screen reader:

- The speech recognizer that writes down what is said.

### Step 16

yt-dlp.

Screen reader:

- The program that fetches a film or its sound from a web address.

### Step 17

Last, more information. Help is built into the dialog. The help box, with every field, every key, and the version.

Screen reader:

- F1, the help box

### Step 18

The tip for one field, and a list of every control.

Screen reader:

- Shift plus F1
- F7

### Step 19

The guide, HomerScribe dot md, explains every choice the program makes; Hotkeys dot md lists every key three ways; and the ReadMe gets you started. All three are in the Start menu, under HomerScribe documentation, and on the HomerScribe page on GitHub, at github dot com slash JamalMazrui slash HomerScribe.

**Something to try:** Describe five minutes of a film you know well, and read the description beside the film.

<!-- walkthrough ends -->
