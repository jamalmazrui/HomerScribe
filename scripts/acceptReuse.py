"""acceptReuse.py -- proves that HomerScribe reuses a transcript only for the recording that made it.

Run by scripts\\check through accept.inix, or by itself: scripts\\acceptReuse.cmd. It needs the built
exec\\HomerScribe.exe, FFmpeg and Whisper, which are what HomerScribe itself needs to transcribe.

WHY IT EXISTS (14 October 2026, from an audit by another AI). HomerScribe keeps a finished transcript and reuses it
on the next run of the same recording. That saves minutes, and it is wrong whenever the recording is not the same:
the old words are then credited to a different recording. The rules were fixed on 13 and 14 October; this test runs
the real program, with the real Whisper, four times on clips cut from two of HomerScribe's own tutorials:

  1. a first run must hear the recording;
  2. an unchanged run must reuse the transcript, and give the same words;
  3. a different recording of exactly the same length, with its time kept, must be heard again, and give different
     words -- the case a check by size and time could not catch;
  4. --force must hear it again although nothing changed.

The console says one line per step; the log in logs says everything, with every command and its exit code.
"""
import datetime, glob, hashlib, os, platform, shutil, subprocess, sys, tempfile, time

sHearing = "Listening to the film"
sMadeAgain = "so it is made again"
# A changed recording now starts clean before its transcript cache is looked at (15 October 2026), so the cache's
# "so it is made again" may not be written; the results' own "so they are made again" says the same thing.
sMadeAgainAll = "so they are made again"
sReused = "Reusing the finished transcript from an earlier run"
sAlreadyDone = "already done"
# THE LOG'S WORDS, NOT THE CONSOLE'S (15 October 2026). "already done" is only in HomerScribe's short console line;
# the run's log file, which is what this reads, says "Skipping <n>: everything asked for is already in <folder>". So a
# correct skip was never seen, and step 2 failed on 14 and 15 October. Either wording now counts.
sAlreadyIn = "everything asked for is already in"
lsTextKinds = [".md", ".srt", ".txt", ".vtt"]

sRoot = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sStamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
sLogPath = os.path.join(sRoot, "logs", "HomerScribe-accept-" + sStamp + ".log")


def log(sText):
    """Writes a line to the log, the moment it happens."""
    os.makedirs(os.path.dirname(sLogPath), exist_ok=True)
    with open(sLogPath, "a", encoding="utf-8-sig" if not os.path.exists(sLogPath) else "utf-8") as f:
        f.write(datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S") + "  " + sText + "\n")


def say(sText):
    """One line for the console, and the same line in the log."""
    print(sText, flush=True)
    log(sText)


def run(lsCommand, iTimeout=900):
    """Runs a command, logs it with its exit code and output, and returns (code, output)."""
    log("run: " + subprocess.list2cmdline(lsCommand))
    try:
        oResult = subprocess.run(lsCommand, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=iTimeout, stdin=subprocess.DEVNULL)
    except subprocess.TimeoutExpired:
        log("  timed out after " + str(iTimeout) + " seconds")
        return 124, ""
    except FileNotFoundError as oError:
        log("  not found: " + str(oError))
        return 127, ""
    sOut = (oResult.stdout or "") + (oResult.stderr or "")
    log("  exit " + str(oResult.returncode))
    for sLine in sOut.splitlines()[-40:]: log("  | " + sLine)
    return oResult.returncode, sOut


def findFfmpeg():
    """FFmpeg where HomerScribe looks for it: machine-wide first, then the PATH."""
    for sRootFolder in (os.environ.get("ProgramFiles", ""), os.environ.get("ProgramFiles(x86)", "")):
        if not sRootFolder: continue
        sPath = os.path.join(sRootFolder, "ffmpeg", "bin", "ffmpeg.exe")
        if os.path.isfile(sPath): return sPath
    return shutil.which("ffmpeg") or ""


def cutClip(sFfmpeg, sSource, sTarget):
    """Twenty seconds of speech as 16-bit mono at 16 kHz, so two clips are exactly the same number of bytes."""
    iCode, sOut = run([sFfmpeg, "-y", "-hide_banner", "-loglevel", "error", "-ss", "5", "-t", "20", "-i", sSource,
                       "-ac", "1", "-ar", "16000", "-c:a", "pcm_s16le", sTarget])
    return iCode == 0 and os.path.isfile(sTarget)


def transcribe(sExe, sRecording, sOutput, sLogFile, bForce=False):
    """One HomerScribe run, transcribing only; returns (exit code, the run's log)."""
    lsCommand = [sExe, sRecording, "--transcribe", "--output-dir", sOutput, "--log-file", sLogFile]
    if bForce: lsCommand.append("--force")
    iCode, sOut = run(lsCommand)
    sRunLog = open(sLogFile, encoding="utf-8-sig", errors="replace").read() if os.path.isfile(sLogFile) else ""
    log("  its log holds " + str(len(sRunLog.splitlines())) + " lines")
    return iCode, sRunLog


def transcriptText(sOutput):
    """Every transcript file HomerScribe wrote, joined, so two runs can be compared."""
    lsAll = sorted(s for s in glob.glob(os.path.join(sOutput, "**", "*"), recursive=True)
                   if os.path.isfile(s) and os.path.splitext(s)[1].lower() in lsTextKinds)
    # ONLY THE TEST RECORDING'S TRANSCRIPTS (14 October 2026): HomerScribe names its output for its source. On 14 October
    # it added the source given to its built-in sample video and transcribed the sample, so all four steps measured the
    # wrong input; that is now fixed in HomerScribe, and any output named for something else is reported, not counted.
    lsFiles = [s for s in lsAll if "recording" in os.path.relpath(s, sOutput).lower()]
    lsOther = [os.path.relpath(s, sOutput) for s in lsAll if s not in lsFiles]
    if lsOther: say("NOTE: HomerScribe also wrote transcripts of something other than the test recording: " + ", ".join(lsOther))
    log("  transcript files: " + (", ".join(os.path.relpath(s, sOutput) for s in lsFiles) or "none"))
    # The heading's "- Made:" line is the day of the run, so two honest runs either side of midnight would differ;
    # it is set aside. Everything else -- the words, their times and what heard them -- must match.
    sText = "\n".join(open(s, encoding="utf-8-sig", errors="replace").read() for s in lsFiles)
    return "\n".join(sLine for sLine in sText.splitlines() if not sLine.startswith("- Made: "))


def main():
    log("acceptReuse.py; Python " + sys.version.split()[0] + " (" + sys.executable + ") on " + platform.platform())
    log("project " + sRoot + "; working directory " + os.getcwd() + "; command line " + subprocess.list2cmdline(sys.argv))
    sExe = os.path.join(sRoot, "exec", "HomerScribe.exe")
    sFfmpeg = findFfmpeg()
    lsTutorials = [os.path.join(sRoot, "help", "tutorials", s) for s in ("3_Transcribe.mp3", "4_Describe.mp3")]
    log("program " + sExe + "; ffmpeg " + (sFfmpeg or "not found") + "; clips from " + ", ".join(lsTutorials))
    lsMissing = [s for s in [sExe] + lsTutorials if not os.path.isfile(s)]
    if not sFfmpeg: lsMissing.append("ffmpeg.exe (run scripts\\installCommon.cmd)")
    if lsMissing:
        say("FAIL: cannot test without " + "; ".join(lsMissing))
        return 1

    sWork = tempfile.mkdtemp(prefix="HomerScribeAccept-")
    log("working in " + sWork)
    lsFailed = []
    try:
        sFirst, sSecond = os.path.join(sWork, "first.wav"), os.path.join(sWork, "second.wav")
        if not (cutClip(sFfmpeg, lsTutorials[0], sFirst) and cutClip(sFfmpeg, lsTutorials[1], sSecond)):
            say("FAIL: FFmpeg could not cut the two clips; the log says why")
            return 1
        iFirst, iSecond = os.path.getsize(sFirst), os.path.getsize(sSecond)
        log("clip sizes " + str(iFirst) + " and " + str(iSecond) + " bytes")
        for sClip in (sFirst, sSecond):
            log("clip " + os.path.basename(sClip) + " sha256 " + hashlib.sha256(open(sClip, "rb").read()).hexdigest())
        if iFirst != iSecond:
            say("FAIL: the two clips differ in size, so step 3 would not test what it says")
            return 1
        sRecording = os.path.join(sWork, "recording.wav")
        sOutput = os.path.join(sWork, "results")
        shutil.copyfile(sFirst, sRecording)

        def step(sName, bOk, sWhy):
            say(("PASS: " if bOk else "FAIL: ") + sName + ("" if bOk else " -- " + sWhy))
            if not bOk: lsFailed.append(sName)

        iCode, sLog1 = transcribe(sExe, sRecording, sOutput, os.path.join(sWork, "run1.log"))
        if "Whisper was not found" in sLog1:
            say("FAIL: Whisper is not installed, so nothing can be heard; run scripts\\installWhisper.cmd")
            return 1
        sWords1 = transcriptText(sOutput)
        step("1. a first run hears the recording", iCode == 0 and sHearing in sLog1 and sWords1.strip() != "",
             "exit " + str(iCode) + ", heard " + str(sHearing in sLog1) + ", words " + str(len(sWords1.split())))

        iCode, sLog2 = transcribe(sExe, sRecording, sOutput, os.path.join(sWork, "run2.log"))
        sWords2 = transcriptText(sOutput)
        # UNCHANGED MAY MEAN SKIPPED (14 October 2026): HomerScribe skips a recording whose results are already there
        # and were made from it, before the transcript cache is reached -- the better outcome, and not a failure. Either
        # that or reuse passes, provided nothing was heard again and the words are the same.
        # A skip must end with exit code 0, as reuse must (15 October 2026): HomerScribe now ends a run with everything
        # already done as a success, and an exit code of 1 there would again be a real fault.
        bSkipped = sAlreadyDone in sLog2 or sAlreadyIn in sLog2
        step("2. an unchanged recording reuses its transcript or is skipped as done, with the same words",
             iCode == 0 and (sReused in sLog2 or bSkipped) and sHearing not in sLog2 and sWords2 == sWords1,
             "exit " + str(iCode) + ", reused " + str(sReused in sLog2) + ", skipped " + str(bSkipped) + ", same words " + str(sWords2 == sWords1))

        nTime = os.path.getmtime(sRecording)
        shutil.copyfile(sSecond, sRecording)
        os.utime(sRecording, (nTime, nTime))
        log("recording replaced by the second clip: " + str(os.path.getsize(sRecording)) + " bytes, time kept at " + str(nTime))
        iCode, sLog3 = transcribe(sExe, sRecording, sOutput, os.path.join(sWork, "run3.log"))
        sWords3 = transcriptText(sOutput)
        step("3. a different recording of the same size and time is heard again, with different words",
             iCode == 0 and sReused not in sLog3 and (sMadeAgain in sLog3 or sMadeAgainAll in sLog3) and sWords3 != sWords1,
             "exit " + str(iCode) + ", reused " + str(sReused in sLog3) + ", made again " + str(sMadeAgain in sLog3 or sMadeAgainAll in sLog3) + ", words differ " + str(sWords3 != sWords1))

        iCode, sLog4 = transcribe(sExe, sRecording, sOutput, os.path.join(sWork, "run4.log"), bForce=True)
        step("4. --force hears it again although nothing changed", iCode == 0 and sReused not in sLog4 and sHearing in sLog4,
             "exit " + str(iCode) + ", reused " + str(sReused in sLog4))
    finally:
        # EVIDENCE KEPT WHEN A STEP FAILS (14 October 2026, from an audit by another AI): the working folder was removed
        # whatever happened, so a failure left only this summary, not the runs' own logs, the outputs or the clips. When a
        # step fails, the whole folder -- each run's log, every transcript, the two clips -- is kept in logs, named for
        # this run, and the console says where; when all pass, it is removed.
        if lsFailed:
            sEvidence = os.path.join(sRoot, "logs", "HomerScribe-accept-" + sStamp + "-evidence")
            try:
                shutil.copytree(sWork, sEvidence)
                say("The evidence of this run is kept in " + sEvidence)
            except Exception as oError:
                log("Could not keep the evidence in " + sEvidence + ": " + str(oError) + "; it is left in " + sWork)
                sWork = ""
        if sWork:
            shutil.rmtree(sWork, ignore_errors=True)
            log("removed " + sWork)
    if lsFailed:
        say("acceptReuse: " + str(len(lsFailed)) + " of 4 steps failed; the log is " + sLogPath)
        return 1
    say("acceptReuse: all 4 steps passed")
    return 0


if __name__ == "__main__":
    sys.exit(main())
