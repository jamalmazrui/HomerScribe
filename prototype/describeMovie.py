"""
describeMovie.py -- build an audio description track for a whole film.

This is the sample script and the full-film script merged into one. With no
window given it describes the entire file; with --start and --minutes it
describes a stretch, exactly as the sample script did.

Work is cached in description.json as it goes, so a run that is interrupted, or
that you stop deliberately, can be resumed with --resume and will not ask the
model about moments it has already described.

Usage:
    python describeMovie.py "video.mkv" --context-file odysseyContext.md
    python describeMovie.py "video.mkv" --start 00:22:30 --minutes 5
    python describeMovie.py "video.mkv" --resume
    python describeMovie.py "video.mkv" --check
"""

import argparse, atexit, base64, datetime, difflib, json, os, platform, re, shutil, subprocess, sys, tempfile, time, traceback, urllib.error, urllib.request, wave

sDefaultClipName = "window.mkv"
sDefaultContextFile = "odysseyContext.md"
sDefaultDetail = "rich"
sDefaultInput = "video.mkv"
sDefaultDescribedName = "described.mkv"
sDefaultJsonName = "description.json"
sDefaultLogName = "describeMovie.log"
sDefaultMarkdownName = "description.md"
sDefaultModel = "qwen2.5vl:7b"
sDefaultOllamaUrl = "http://localhost:11434/api/generate"
sDefaultAnnouncement = "Audio description is on."
sDefaultTrackTitle = "Audio Description"
sDefaultVttName = "description.vtt"
sDefaultWaveName = "description.wav"
iDefaultCheckpoint = 15
iDefaultFrameCount = 4
iDefaultFrameWidth = 512
iDefaultMinutes = 5
iDefaultRate = 1
iDefaultCompare = 10
iDefaultRecent = 2
iDefaultScanReport = 10
iDefaultSampleRate = 48000
iDefaultTimeout = 300
nDefaultAdVolume = 0.9
nDefaultChapter = 600.0
lStopWords = set(["with", "from", "that", "this", "they", "them", "their", "there", "then", "than",
                  "into", "onto", "over", "under", "while", "which", "where", "what", "when", "some",
                  "more", "most", "very", "much", "also", "just", "like", "such", "have", "been",
                  "were", "seems", "appears", "slightly", "against", "toward", "towards"])

nDefaultSameShot = 4.0
nDefaultSimilarity = 0.6
nDefaultCropBottom = 12.0
nDefaultMuxMinutes = 20.0
nDefaultEvery = 14.0
nDefaultForcedLength = 8.0
nDefaultLead = 0.20
nDefaultMinGap = 2.0
nDefaultNoiseFloor = -24.0
nDefaultSilenceLength = 1.0
nDefaultSpacing = 10.0
nDefaultWordsPerSecond = 2.5

sPromptTemplate = (
    "You are writing audio description for a blind viewer of a film. "
    "{sContext}"
    "You are given {iFrames} still frames from one brief moment, in time order, tiled into one picture, "
    "read left to right then top to bottom. Treat them as a single continuous moment, not as separate pictures. "
    "{sRecent}"
    "Say what a sighted viewer can see and the dialogue does not tell them: who is there, what they look like "
    "and wear, what they do, where they are, the light and the weather, faces and expressions, and what changes. "
    "{sStyle}"
    "Write only the description itself, in the present tense, as it would be spoken aloud to the listener. "
    "Never mention frames, images, shots, scenes, panels, the camera, the sequence, or the film. "
    "Never write openings such as \"the first frame\", \"the second frame\", \"the frames show\", "
    "\"the image shows\", \"the scene shifts\", \"the scene turns\", \"the sequence begins\", "
    "\"the view moves\" or \"the camera pans\". Say what happens, and nothing about how it is filmed. "
    "Your words will be spoken in a nearby pause in the dialogue, so they may land a little before or after "
    "the moment itself. Describing what is happening, what has just happened, or what is about to happen is "
    "equally correct. "
    "Say only what is new. Never repeat what you have already said. "
    "Ignore studio logos, title cards, credits and subtitles, and never mention them. "
    "Use no more than {iMaxWords} words. "
    "If there is nothing a blind viewer would need, reply with the single word SKIP."
)

dDetailStyles = {
    "brief": "One short sentence, the single most important thing. ",
    "normal": "One or two sentences. Concrete nouns, few adjectives, no filler. ",
    "rich": "Two or three tight sentences carrying real detail: clothing, colour, texture, light, posture, "
            "expression. Every word must earn its place. Cut anything a listener would not miss. ",
}
dDetailWords = {"brief": 1.0, "normal": 1.6, "rich": 2.4}
dDetailOverrun = {"brief": 0.0, "normal": 1.5, "rich": 3.5}

lOptionalTools = ["ffprobe"]
lRequiredTools = ["ffmpeg", "powershell"]

fLog = None
bVerbose = False

def formatClock(nSeconds):
    """Return elapsed film time as m:ss, or h:mm:ss once past an hour."""
    iWhole = int(nSeconds)
    iHours = iWhole // 3600
    iMinutes = (iWhole % 3600) // 60
    iRest = iWhole % 60
    if iHours > 0: return "%d:%02d:%02d" % (iHours, iMinutes, iRest)
    return "%d:%02d" % (iMinutes, iRest)

def logMessage(sText, sLevel="INFO", sConsole=None):
    """Write a full line to the log, and a friendlier one to the console."""
    sStamp = datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S.%f")[:-3]
    sLine = sStamp + "  " + sLevel.ljust(5) + "  " + sText
    if fLog is not None: fLog.write(sLine + "\n")
    if fLog is not None: fLog.flush()
    if sLevel == "CMD" and not bVerbose: return True
    sShow = sText if sConsole is None else sConsole
    if sShow == "": return True
    if sLevel in ("ERROR", "HINT", "FATAL"): sShow = sLevel.title() + ": " + sShow
    if sLevel == "CMD": sShow = sLine
    try:
        print(sShow)
    except Exception:
        pass
    return True

def logException(oType, oValue, oTrace):
    """Send an unhandled error to the log as well as the console."""
    for sBlock in traceback.format_exception(oType, oValue, oTrace):
        for sLine in sBlock.rstrip().splitlines():
            logMessage(sLine, "FATAL")
    return True

def scriptFolder():
    """Return the folder holding this script."""
    return os.path.dirname(os.path.abspath(__file__))

def logPathFor(sOverride, sName):
    """Return the log path: beside the script, unless one was given."""
    if sOverride.strip() != "": return os.path.abspath(sOverride)
    sFolder = scriptFolder()
    if os.access(sFolder, os.W_OK): return os.path.join(sFolder, sName)
    return os.path.abspath(sName)

def closeLog():
    """Flush and close the log on any exit path."""
    if fLog is not None and not fLog.closed: logMessage("Log closed")
    if fLog is not None and not fLog.closed: fLog.close()
    return True

def runCommand(lArgs, bQuiet=False):
    """Run a program and return a tuple of exit code, stdout, stderr."""
    lQuoted = []
    for sArg in lArgs:
        lQuoted.append("\"" + sArg + "\"" if " " in sArg else sArg)
    logMessage("Command: " + " ".join(lQuoted), "CMD")
    nBegan = time.time()
    try:
        oResult = subprocess.run(lArgs, capture_output=True, text=True, encoding="utf-8", errors="replace")
    except FileNotFoundError:
        logMessage("Program not found: " + lArgs[0] + ". It is not on the PATH of this process.", "ERROR")
        return (-1, "", "program not found: " + lArgs[0])
    except OSError as oError:
        logMessage("Could not start " + lArgs[0] + ": " + str(oError), "ERROR")
        return (-1, "", str(oError))
    nTook = time.time() - nBegan
    sOut = oResult.stdout or ""
    sErr = oResult.stderr or ""
    logMessage("Exit code " + str(oResult.returncode) + " after " + str(round(nTook, 2)) + " seconds", "CMD")
    if oResult.returncode != 0 and not bQuiet: logMessage("Error output: " + sErr.strip()[-1500:], "ERROR")
    return (oResult.returncode, sOut, sErr)

def findTool(sName):
    """Return the full path of a program on the PATH, or an empty string."""
    return shutil.which(sName) or ""

def addToPath(sFolder):
    """Put a folder at the front of the PATH for this process only."""
    sFull = os.path.abspath(sFolder)
    if not os.path.isdir(sFull): logMessage("The folder given by --ffmpeg-dir does not exist: " + sFull, "ERROR")
    if not os.path.isdir(sFull): return False
    os.environ["PATH"] = sFull + os.pathsep + os.environ.get("PATH", "")
    logMessage("Added to the PATH for this run: " + sFull)
    return True

def logEnvironment():
    """Record the details that matter when a failed run has to be diagnosed."""
    logMessage("Script: " + os.path.abspath(__file__))
    logMessage("Python: " + sys.version.replace("\n", " "))
    logMessage("Platform: " + platform.platform())
    logMessage("Working directory: " + os.getcwd())
    logMessage("Command line: " + " ".join(sys.argv))
    return True

def logSettings(oArgs):
    """Record every effective setting, so a log explains its own run."""
    for sName in sorted(vars(oArgs).keys()):
        logMessage("Setting " + sName + " = " + str(getattr(oArgs, sName)))
    return True

def addScriptFolderToPath():
    """Let programs sitting beside the script be found without a PATH change."""
    sFolder = scriptFolder()
    lParts = os.environ.get("PATH", "").split(os.pathsep)
    bAlready = False
    for sPart in lParts:
        if sPart.strip().rstrip("\\/").lower() == sFolder.rstrip("\\/").lower(): bAlready = True
    if bAlready: return False
    os.environ["PATH"] = sFolder + os.pathsep + os.environ.get("PATH", "")
    logMessage("Searching the script folder for programs as well: " + sFolder)
    return True

def checkOptionalTools(lNames):
    """Record which helpful but not essential programs are present."""
    for sName in lNames:
        sPath = findTool(sName)
        if sPath == "": logMessage("Optional program not found: " + sName + ". A slower fallback will be used.")
        if sPath != "": logMessage("Found " + sName + " at " + sPath)
    return True

def checkTools(lNames):
    """Confirm every required program is present and record its version."""
    lMissing = []
    for sName in lNames:
        sPath = findTool(sName)
        if sPath == "": lMissing.append(sName)
        if sPath == "": logMessage("Not found on the PATH: " + sName, "ERROR")
        if sPath != "": logMessage("Found " + sName + " at " + sPath)
        if sPath != "" and sName.lower().startswith("ff"):
            iCode, sOut, sErr = runCommand([sName, "-version"], True)
            lLines = (sOut or sErr).strip().splitlines()
            logMessage("  " + (lLines[0] if len(lLines) > 0 else "version unknown"))
        if sPath != "" and sName.lower().startswith("powershell"):
            iCode, sOut, sErr = runCommand([sName, "-NoProfile", "-Command", "$PSVersionTable.PSVersion.ToString()"], True)
            logMessage("  PowerShell version " + sOut.strip())
    if len(lMissing) == 0: return True
    logMessage("Missing programs: " + ", ".join(lMissing), "ERROR")
    bNeedFfmpeg = False
    for sName in lMissing:
        if sName.lower().startswith("ff"): bNeedFfmpeg = True
    if bNeedFfmpeg: logMessage("Install ffmpeg with:  winget install Gyan.FFmpeg", "HINT")
    if bNeedFfmpeg: logMessage("Then close this terminal and open a NEW one, so the changed PATH is picked up.", "HINT")
    if bNeedFfmpeg: logMessage("Or point at an unpacked copy without changing the PATH, for example:", "HINT")
    if bNeedFfmpeg: logMessage("  --ffmpeg-dir \"C:\\ffmpeg\\bin\"", "HINT")
    if "powershell" in lMissing: logMessage("powershell.exe is normally at C:\\Windows\\System32\\WindowsPowerShell\\v1.0", "HINT")
    if "powershell" in lMissing: logMessage("Add that folder to the PATH, or run this script from a normal Windows console.", "HINT")
    return False

def checkOllama(sUrl, sModel):
    """Confirm Ollama answers and record whether the wanted model is pulled."""
    sBase = sUrl.split("/api/")[0]
    sTagsUrl = sBase + "/api/tags"
    logMessage("Asking Ollama for its model list at " + sTagsUrl)
    try:
        oResponse = urllib.request.urlopen(sTagsUrl, timeout=15)
        dData = json.loads(oResponse.read().decode("utf-8"))
    except Exception as oError:
        logMessage("Ollama did not answer at " + sTagsUrl + ": " + str(oError), "ERROR")
        logMessage("Start it with:  ollama serve      then check:  ollama list", "HINT")
        return False
    lNames = []
    for dModel in dData.get("models", []):
        lNames.append(dModel.get("name", ""))
    logMessage("Ollama holds " + str(len(lNames)) + " models: " + ", ".join(lNames))
    bFound = False
    for sName in lNames:
        if sName == sModel or sName.split(":")[0] == sModel.split(":")[0]: bFound = True
    if not bFound: logMessage("The model " + sModel + " is not in that list.", "ERROR")
    if not bFound: logMessage("Pull it with:  ollama pull " + sModel, "HINT")
    if bFound: logMessage("The model " + sModel + " is available.")
    return bFound

def parseTime(sValue):
    """Return seconds for a value given as seconds, mm:ss, or hh:mm:ss."""
    sText = str(sValue).strip()
    if sText == "": return 0.0
    lParts = sText.split(":")
    nSeconds = 0.0
    for sPart in lParts:
        nSeconds = nSeconds * 60.0 + float(sPart or 0)
    return nSeconds

def formatTimestamp(nSeconds):
    """Return a WebVTT timestamp for a number of seconds."""
    iWhole = int(nSeconds)
    iMilliseconds = int(round((nSeconds - iWhole) * 1000))
    iHours = iWhole // 3600
    iMinutes = (iWhole % 3600) // 60
    iRest = iWhole % 60
    return "%02d:%02d:%02d.%03d" % (iHours, iMinutes, iRest, iMilliseconds)

def probeDuration(sPath):
    """Return the duration of a media file, using ffmpeg when ffprobe is absent."""
    if findTool("ffprobe") != "":
        lArgs = ["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "json", sPath]
        iCode, sOut, sErr = runCommand(lArgs)
        if iCode == 0 and sOut.strip() != "":
            dData = json.loads(sOut)
            nSeconds = float(dData.get("format", {}).get("duration", 0.0) or 0.0)
            if nSeconds > 0.0: return nSeconds
    logMessage("Reading the duration with ffmpeg, because ffprobe gave nothing.", "INFO", "")
    iCode, sOut, sErr = runCommand(["ffmpeg", "-hide_banner", "-i", sPath], True)
    oMatch = re.search(r"Duration:\s*(\d+):(\d\d):(\d\d(?:\.\d+)?)", sErr)
    if oMatch is None: logMessage("No duration could be read from " + sPath, "ERROR")
    if oMatch is None: return 0.0
    return float(oMatch.group(1)) * 3600.0 + float(oMatch.group(2)) * 60.0 + float(oMatch.group(3))

def extractWindow(sInput, sClipPath, nStart, nLength):
    """Cut a window out of the source file, re-encoding only if copying fails."""
    lCopy = ["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-ss", str(nStart), "-i", sInput,
             "-t", str(nLength), "-map", "0:v:0", "-map", "0:a:0", "-c", "copy", "-avoid_negative_ts", "make_zero", sClipPath]
    iCode, sOut, sErr = runCommand(lCopy)
    if iCode == 0 and probeDuration(sClipPath) > 1.0: return True
    logMessage("Stream copy did not give a clean window. Re-encoding the sample instead.")
    lEncode = ["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-ss", str(nStart), "-i", sInput,
               "-t", str(nLength), "-map", "0:v:0", "-map", "0:a:0",
               "-c:v", "libx264", "-crf", "20", "-preset", "veryfast", "-c:a", "aac", "-b:a", "192k", sClipPath]
    iCode, sOut, sErr = runCommand(lEncode)
    if iCode != 0: logMessage(sErr[-1500:])
    return iCode == 0

def audioChannels(sPath):
    """Return how many audio channels the first sound track carries."""
    iCode, sOut, sErr = runCommand(["ffmpeg", "-hide_banner", "-i", sPath], True)
    oMatch = re.search(r"Audio:.*?,\s*\d+\s*Hz,\s*([^,]+),", sErr)
    if oMatch is None: return 0
    sLayout = oMatch.group(1).strip().lower()
    dLayouts = {"mono": 1, "stereo": 2, "2.1": 3, "quad": 4, "5.0": 5, "5.1": 6, "5.1(side)": 6, "6.1": 7, "7.1": 8}
    iChannels = dLayouts.get(sLayout, 0)
    logMessage("Audio layout: " + sLayout + " (" + str(iChannels) + " channels)")
    return iChannels

def runScan(lArgs, nDuration, sLabel):
    """Run a long ffmpeg pass, reporting progress as it goes, and return its error text."""
    lFull = lArgs[:1] + ["-progress", "pipe:1", "-nostats"] + lArgs[1:]
    logMessage("Command: " + " ".join(lFull), "CMD")
    sErrPath = os.path.join(tempfile.gettempdir(), "homerScanError.txt")
    fErr = open(sErrPath, "w", encoding="utf-8", errors="replace")
    nBegan = time.time()
    nLastReport = time.time()
    try:
        oProcess = subprocess.Popen(lFull, stdout=subprocess.PIPE, stderr=fErr, text=True, encoding="utf-8", errors="replace")
    except FileNotFoundError:
        logMessage("Program not found: " + lFull[0] + ". It is not on the PATH of this process.", "ERROR")
        fErr.close()
        return ""
    for sLine in oProcess.stdout:
        oMatch = re.match(r"out_time=(\d+):(\d\d):(\d\d)", sLine.strip())
        if oMatch is None: continue
        nAt = float(oMatch.group(1)) * 3600.0 + float(oMatch.group(2)) * 60.0 + float(oMatch.group(3))
        if time.time() - nLastReport < iDefaultScanReport: continue
        nLastReport = time.time()
        nShare = nAt / max(nDuration, 1.0)
        nLeft = 0.0
        if nShare > 0.01: nLeft = (time.time() - nBegan) * (1.0 - nShare) / nShare / 60.0
        logMessage(sLabel + ": reached " + formatClock(nAt) + " of " + formatClock(nDuration) + ", " + str(int(nShare * 100)) + " percent, about " + str(round(nLeft, 1)) + " minutes left",
                   "INFO", "  " + sLabel + " " + str(int(nShare * 100)) + " percent, at " + formatClock(nAt) + " of " + formatClock(nDuration) + ", about " + str(int(round(nLeft))) + " minutes left")
    oProcess.wait()
    fErr.close()
    logMessage("Exit code " + str(oProcess.returncode) + " after " + str(round(time.time() - nBegan, 1)) + " seconds", "CMD")
    fErr = open(sErrPath, "r", encoding="utf-8", errors="replace")
    sErr = fErr.read()
    fErr.close()
    return sErr

def detectSilences(sPath, nNoiseFloor, nSilenceLength, bCentre=False, nDuration=0.0):
    """Return a list of start and end pairs for every detected silence."""
    lSilences = []
    sFilter = "silencedetect=noise=" + str(nNoiseFloor) + "dB:d=" + str(nSilenceLength)
    if bCentre: sFilter = "pan=mono|c0=FC," + sFilter
    lArgs = ["ffmpeg", "-hide_banner", "-i", sPath, "-af", sFilter, "-f", "null", "-"]
    logMessage("Listening through the whole film at " + str(nNoiseFloor) + " dB to find the quiet moments.",
               "INFO", "Listening through the film at " + str(nNoiseFloor) + " dB for quiet moments. Progress follows.")
    sErr = runScan(lArgs, nDuration, "Listening at " + str(nNoiseFloor) + " dB")
    nStart = -1.0
    for sLine in sErr.splitlines():
        oStart = re.search(r"silence_start:\s*(-?[0-9.]+)", sLine)
        oEnd = re.search(r"silence_end:\s*(-?[0-9.]+)", sLine)
        if oStart is not None: nStart = float(oStart.group(1))
        if oEnd is not None and nStart >= 0.0: lSilences.append((nStart, float(oEnd.group(1))))
        if oEnd is not None: nStart = -1.0
    logMessage("Found " + str(len(lSilences)) + " silences at " + str(nNoiseFloor) + " dB", "INFO", "")
    return lSilences

def chooseGaps(lSilences, nMinGap, nSpacing, nLead):
    """Keep the silences that are long enough and far enough apart to use."""
    lGaps = []
    nLastEnd = -999.0
    for tSilence in lSilences:
        nStart = tSilence[0] + nLead
        nEnd = tSilence[1] - nLead
        nLength = nEnd - nStart
        bLongEnough = nLength >= nMinGap
        bFarEnough = nStart - nLastEnd >= nSpacing
        if bLongEnough and bFarEnough: lGaps.append({"start": round(nStart, 3), "length": round(nLength, 3)})
        if bLongEnough and bFarEnough: nLastEnd = nStart + nLength
    logMessage("Kept " + str(len(lGaps)) + " gaps in the sample", "INFO", "")
    if len(lGaps) == 0: logMessage("No gaps were kept. Raise --noise-floor toward -24 or lower --min-gap.")
    return lGaps

def fillGaps(lGaps, nDuration, nEvery, nForcedLength):
    """Add placed descriptions wherever the film would otherwise go quiet too long."""
    if nEvery <= 0.0: return lGaps
    lResult = []
    nLast = 0.0 - nEvery
    for dGap in sorted(lGaps, key=lambda d: d["start"]):
        while dGap["start"] - nLast > nEvery:
            nNew = nLast + nEvery
            if nNew + nForcedLength > dGap["start"]: break
            lResult.append({"start": round(nNew, 3), "length": nForcedLength, "forced": True})
            nLast = nNew
        lResult.append(dGap)
        nLast = dGap["start"]
    while nDuration - nLast > nEvery:
        nNew = nLast + nEvery
        if nNew + nForcedLength > nDuration: break
        lResult.append({"start": round(nNew, 3), "length": nForcedLength, "forced": True})
        nLast = nNew
    iForced = 0
    for dGap in lResult:
        if dGap.get("forced", False): iForced = iForced + 1
    logMessage("Placed " + str(iForced) + " extra descriptions where no quiet moment was found", "INFO", "")
    return lResult

def findGaps(sPath, oArgs, nDuration):
    """Find places to speak, loosening the threshold until enough are found."""
    logMessage("Working out where to speak. Expect roughly " + str(round(nDuration / 60.0 * 1.7 / 60.0, 1)) + " minutes for the sound scan on a film this long.")
    bCentre = False
    if oArgs.dialogue_channel != "off": bCentre = audioChannels(sPath) >= 6
    if bCentre: logMessage("Listening to the centre channel only, which carries most of the dialogue.")
    if oArgs.dialogue_channel == "on": bCentre = True
    lThresholds = [oArgs.noise_floor]
    if oArgs.escalate:
        for nStep in [-28.0, -24.0, -20.0, -16.0]:
            if nStep > oArgs.noise_floor: lThresholds.append(nStep)
    iWanted = max(2, int(nDuration / (oArgs.spacing * 2.0)))
    lBest = []
    nBestThreshold = oArgs.noise_floor
    for nThreshold in lThresholds:
        lSilences = detectSilences(sPath, nThreshold, oArgs.silence_length, bCentre, nDuration)
        lGaps = chooseGaps(lSilences, oArgs.min_gap, oArgs.spacing, nDefaultLead)
        logMessage("At " + str(nThreshold) + " dB there are " + str(len(lGaps)) + " usable gaps, wanting about " + str(iWanted), "INFO", "")
        if len(lGaps) > len(lBest): lBest = lGaps
        if len(lGaps) > len(lBest) or lBest is lGaps: nBestThreshold = nThreshold
        if len(lBest) >= iWanted: break
    logMessage("Using the " + str(nBestThreshold) + " dB result with " + str(len(lBest)) + " natural gaps", "INFO", "")
    lGaps = fillGaps(lBest, nDuration, oArgs.every, oArgs.forced_length)
    logMessage("Describing " + str(len(lGaps)) + " moments across " + str(round(nDuration / 60.0, 2)) + " minutes, about " + str(round(len(lGaps) / max(nDuration / 60.0, 0.01), 1)) + " a minute")
    return lGaps

def buildMontage(sPath, nStart, nLength, iFrames, iWidth, sWorkDir, nCropBottom=0.0):
    """Tile frames spanning a gap into one time ordered image and return its path."""
    lFramePaths = []
    iIndex = 0
    nSpan = max(nLength, 1.0)
    for iStep in range(iFrames):
        iIndex = iIndex + 1
        nTime = nStart - nSpan * 0.5 + nSpan * (iStep + 0.5) / float(iFrames)
        if nTime < 0.0: nTime = 0.0
        sFramePath = os.path.join(sWorkDir, "frame%d.jpg" % iIndex)
        sVideoFilter = "scale=" + str(iWidth) + ":-2"
        if nCropBottom > 0.0: sVideoFilter = "crop=iw:ih*" + str(round(1.0 - nCropBottom / 100.0, 4)) + ":0:0," + sVideoFilter
        lArgs = ["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-ss", str(round(nTime, 3)), "-i", sPath,
                 "-frames:v", "1", "-vf", sVideoFilter, "-q:v", "3", sFramePath]
        iCode, sOut, sErr = runCommand(lArgs)
        if iCode == 0 and os.path.isfile(sFramePath): lFramePaths.append(sFramePath)
    if len(lFramePaths) == 0: return ""
    if len(lFramePaths) == 1: return lFramePaths[0]
    sMontagePath = os.path.join(sWorkDir, "montage.jpg")
    lArgs = ["ffmpeg", "-hide_banner", "-loglevel", "error", "-y"]
    for sFramePath in lFramePaths:
        lArgs = lArgs + ["-i", sFramePath]
    sLayout = "0_0|w0_0"
    if len(lFramePaths) >= 4: sLayout = "0_0|w0_0|0_h0|w0_h0"
    sInputs = "".join("[" + str(iPos) + ":v]" for iPos in range(min(len(lFramePaths), 4)))
    sFilter = sInputs + "xstack=inputs=" + str(min(len(lFramePaths), 4)) + ":layout=" + sLayout + ",scale=1024:-2"
    lArgs = lArgs + ["-filter_complex", sFilter, "-frames:v", "1", "-q:v", "3", sMontagePath]
    iCode, sOut, sErr = runCommand(lArgs)
    if iCode != 0: logMessage("Montage failed, falling back to a single frame.")
    if iCode != 0: return lFramePaths[0]
    return sMontagePath

def contentWords(sText):
    """Return the meaningful words of a description, for comparing one with another."""
    lWords = []
    for sWord in re.findall(r"[a-z]+", sText.lower()):
        if len(sWord) > 3 and sWord not in lStopWords: lWords.append(sWord)
    return set(lWords)

def worstLikeness(sText, lEarlier):
    """Return how close a description is to the nearest recent one, 0 to 1.

    Two measures are taken, because a model repeats itself in two ways: word for
    word, which a sequence comparison catches, and reshuffled into a new order,
    which only a comparison of the words themselves catches.
    """
    nWorst = 0.0
    if sText.strip() == "": return 0.0
    sOne = re.sub(r"[^a-z ]", "", sText.lower())
    setOne = contentWords(sText)
    for sOld in lEarlier:
        nOrder = difflib.SequenceMatcher(None, sOne, re.sub(r"[^a-z ]", "", sOld.lower())).ratio()
        setOld = contentWords(sOld)
        nShared = 0.0
        if len(setOne) > 0 and len(setOld) > 0: nShared = len(setOne & setOld) / float(min(len(setOne), len(setOld)))
        nLike = max(nOrder, nShared)
        if nLike > nWorst: nWorst = nLike
    return nWorst

def shotSignature(sImagePath, sWorkDir):
    """Reduce a montage to a tiny grey thumbprint, for telling one shot from another."""
    sRawPath = os.path.join(sWorkDir, "signature.raw")
    lArgs = ["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-i", sImagePath,
             "-vf", "scale=16:16,format=gray", "-f", "rawvideo", sRawPath]
    iCode, sOut, sErr = runCommand(lArgs)
    if iCode != 0 or not os.path.isfile(sRawPath): return b""
    fRaw = open(sRawPath, "rb")
    binData = fRaw.read()
    fRaw.close()
    return binData

def signatureDistance(binOne, binTwo):
    """Return the average difference between two thumbprints, 0 meaning identical."""
    if len(binOne) == 0 or len(binOne) != len(binTwo): return 255.0
    iTotal = 0
    for iAt in range(len(binOne)):
        iTotal = iTotal + abs(binOne[iAt] - binTwo[iAt])
    return iTotal / float(len(binOne))

def describeImage(sUrl, sModel, sImagePath, iMaxWords, iFrames, lRecent, sContext="", sDetail="normal", bAgain=False):
    """Ask the model to describe one montage and return the sentence."""
    fImage = open(sImagePath, "rb")
    sImage = base64.b64encode(fImage.read()).decode("ascii")
    fImage.close()
    sRecent = ""
    if len(lRecent) > 0: sRecent = "You already said this about the moments just before: " + " ".join(lRecent) + " Do not say any of it again. Describe only what has changed. "
    if bAgain: sRecent = sRecent + "Your last answer simply repeated what you had already said, which is useless to the listener. Look harder for what is different, and if truly nothing has changed, reply SKIP. "
    dPayload = {"model": sModel,
                "prompt": sPromptTemplate.format(iFrames=iFrames, iMaxWords=iMaxWords, sRecent=sRecent,
                                                 sContext=(sContext.strip() + " " if sContext.strip() != "" else ""),
                                                 sStyle=dDetailStyles.get(sDetail, dDetailStyles["normal"])),
                "images": [sImage],
                "stream": False,
                "options": {"temperature": 0.9 if bAgain else 0.2, "num_predict": 400}}
    oRequest = urllib.request.Request(sUrl, data=json.dumps(dPayload).encode("utf-8"),
                                      headers={"Content-Type": "application/json"})
    try:
        oResponse = urllib.request.urlopen(oRequest, timeout=iDefaultTimeout)
        dData = json.loads(oResponse.read().decode("utf-8"))
    except urllib.error.URLError as oError:
        logMessage("Ollama request failed: " + str(oError))
        return ""
    sText = (dData.get("response") or "").strip()
    if sText.upper().startswith("SKIP"): return ""
    return tidyText(stripFilmTalk(sText))

lStripOpeners = [
    r"^(in|across|throughout)\s+(the|this|these)\s+(first|second|third|fourth|final|last|next|opening)?\s*(frames?|images?|shots?|panels?|scene|sequence)\s*,?\s*",
    r"^(the|this)\s+(first|second|third|fourth|final|last|next|opening|closing)\s+(frame|image|shot|panel)?\s*(shows?|depicts?|captures?|reveals?|presents?|is|features?)\s*",
    r"^(the|these)\s+(frames?|images?|shots?|panels?|pictures?)\s+(show|shows|depict|depicts|capture|captures|reveal|reveals|present|presents)\s*",
    r"^(the\s+)?(sequence|scene|footage|film|clip|montage|shot)\s+(begins|opens|starts)\s+(with|by|on|in)\s*",
    r"^(the|this)\s+(image|picture|frame|photo|photograph|still)\s+(shows?|depicts?|captures?)\s*",
    r"^(we|the\s+viewer)\s+(then\s+)?(see|sees|watch|observe|are\s+shown)\s*",
    r"^(here|now)\s*,\s*",
]

lFilmTalk = [
    r"\b(camera|frames?|panels?|footage|montage|close-?up|wide\s+shot|establishing\s+shot)\b",
    r"\bthe\s+(shot|image|picture|still|sequence)\b",
    r"\b(scene|view|perspective|focus)\s+(then\s+)?(shifts?|cuts?|turns?|changes?|switches?|transitions?)\b",
    r"\b(transitioning|cutting|panning|zooming|tilting)\s+(to|into|across)\b",
    r"\bwe\s+(see|watch|observe|are\s+shown)\b",
    r"\bthis\s+(scene|moment)\s+(shows?|depicts?)\b",
]

lClauseTrims = [
    r",?\s*(as|while|and|with)?\s*the\s+camera[^,.;]*",
    r",?\s*(before\s+|then\s+)?(transitioning|cutting|panning|zooming|shifting)\s+(to|into|across)[^,.;]*",
    r",?\s*(in|filling)\s+the\s+(frame|shot)\b[^,.;]*",
]

def trimClauses(sSentence):
    """Cut away a clause that describes the filming rather than the film."""
    sClean = sSentence
    for sPattern in lClauseTrims:
        sClean = re.sub(sPattern, "", sClean, flags=re.IGNORECASE)
    sClean = re.sub(r"\s+", " ", sClean).strip()
    sClean = re.sub(r"[\s,;:]+([.!?])", r"\1", sClean)
    if sClean != "" and sClean[-1] not in ".!?": sClean = sClean.rstrip(",;: ") + "."
    return sClean

def stripOpener(sSentence):
    """Remove a leading turn of phrase about frames or cameras, if there is one."""
    sClean = sSentence.strip()
    for sPattern in lStripOpeners:
        sClean = re.sub(sPattern, "", sClean, count=1, flags=re.IGNORECASE)
    sClean = re.sub(r"^[\s,;:.\-]+", "", sClean)
    if sClean == "": return ""
    return sClean[0].upper() + sClean[1:]

def hasFilmTalk(sSentence):
    """Say whether a sentence still talks about how the film was made."""
    bFound = False
    for sPattern in lFilmTalk:
        if re.search(sPattern, sSentence, flags=re.IGNORECASE) is not None: bFound = True
    return bFound

def stripFilmTalk(sText):
    """Drop talk of frames, shots and cameras, which a listener does not want to hear."""
    sClean = re.sub(r"\s+", " ", sText).strip()
    lParts = re.findall(r"[^.!?]*[.!?]", sClean)
    if len(lParts) == 0: lParts = [sClean]
    lKept = []
    lFallback = []
    for sPart in lParts:
        sOne = trimClauses(stripOpener(sPart))
        if sOne == "": continue
        lFallback.append(sOne)
        if not hasFilmTalk(sOne): lKept.append(sOne)
    if len(lKept) == 0: lKept = lFallback
    sResult = " ".join(lKept).strip()
    sResult = re.sub(r"\s+([,.;:])", r"\1", sResult)
    if sResult == "": return ""
    return sResult[0].upper() + sResult[1:]

def splitSentences(sText):
    """Return the whole sentences in a piece of text, dropping any dangling tail."""
    lParts = re.findall(r"[^.!?]*[.!?]", sText)
    lClean = []
    for sPart in lParts:
        if sPart.strip() != "": lClean.append(sPart.strip())
    return lClean

def tidyText(sText):
    """Keep only whole sentences, so a description never ends in mid air."""
    lParts = splitSentences(stripFilmTalk(sText))
    if len(lParts) > 0: return " ".join(lParts)
    sTail = sText.strip().rstrip(",;: ")
    if sTail == "": return ""
    return sTail + "."

def trimToWords(sText, iMaxWords):
    """Drop whole sentences from the end until the budget is met, never part of one."""
    lParts = splitSentences(tidyText(sText))
    if len(lParts) == 0: return ""
    lKept = []
    iCount = 0
    for sPart in lParts:
        iWords = len(sPart.split())
        if len(lKept) > 0 and iCount + iWords > iMaxWords: break
        lKept.append(sPart)
        iCount = iCount + iWords
    return " ".join(lKept)

def dropLastSentence(sText):
    """Remove the final sentence, or return the text unchanged when only one is left."""
    lParts = splitSentences(sText)
    if len(lParts) <= 1: return sText
    return " ".join(lParts[:-1])

def listVoices():
    """Print the installed speech voices and return the count."""
    sScript = ("Add-Type -AssemblyName System.Speech; "
               "$oSynth = New-Object System.Speech.Synthesis.SpeechSynthesizer; "
               "$oSynth.GetInstalledVoices() | ForEach-Object { $_.VoiceInfo.Name }")
    iCode, sOut, sErr = runCommand(["powershell", "-NoProfile", "-Command", sScript])
    for sName in sOut.splitlines():
        if sName.strip() != "": logMessage("Voice: " + sName.strip())
    return len(sOut.splitlines())

def speakToWave(sText, sWavePath, sVoice, iRate, sWorkDir):
    """Speak one sentence to a wave file with a built-in Windows voice."""
    sTextPath = os.path.join(sWorkDir, "line.txt")
    fText = open(sTextPath, "w", encoding="utf-8")
    fText.write(sText)
    fText.close()
    lScript = ["Add-Type -AssemblyName System.Speech;",
               "$oSynth = New-Object System.Speech.Synthesis.SpeechSynthesizer;"]
    if sVoice != "": lScript.append("$oSynth.SelectVoice('" + sVoice.replace("'", "''") + "');")
    lScript.append("$oSynth.Rate = " + str(iRate) + ";")
    lScript.append("$oSynth.SetOutputToWaveFile('" + sWavePath.replace("'", "''") + "');")
    lScript.append("$oSynth.Speak([System.IO.File]::ReadAllText('" + sTextPath.replace("'", "''") + "', [System.Text.Encoding]::UTF8));")
    lScript.append("$oSynth.Dispose();")
    iCode, sOut, sErr = runCommand(["powershell", "-NoProfile", "-Command", " ".join(lScript)])
    if iCode != 0: logMessage("Speech failed: " + sErr.strip()[:400])
    return iCode == 0 and os.path.isfile(sWavePath)

def waveDuration(sWavePath):
    """Return the length of a wave file in seconds."""
    oWave = wave.open(sWavePath, "rb")
    nSeconds = oWave.getnframes() / float(oWave.getframerate())
    oWave.close()
    return nSeconds

def toRawPcm(sWavePath, sRawPath, iSampleRate):
    """Convert a wave file to headerless mono sixteen bit samples."""
    lArgs = ["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-i", sWavePath,
             "-ac", "1", "-ar", str(iSampleRate), "-f", "s16le", sRawPath]
    iCode, sOut, sErr = runCommand(lArgs)
    return iCode == 0 and os.path.isfile(sRawPath)

def buildTrack(lClips, nTotalSeconds, sOutWave, iSampleRate):
    """Write a silent track of the full length with each clip at its offset."""
    oWave = wave.open(sOutWave, "wb")
    oWave.setnchannels(1)
    oWave.setsampwidth(2)
    oWave.setframerate(iSampleRate)
    iCursor = 0
    for dClip in lClips:
        iTarget = int(dClip["start"] * iSampleRate)
        if iTarget < iCursor: iTarget = iCursor
        iSilence = iTarget - iCursor
        while iSilence > 0:
            iChunk = min(iSilence, iSampleRate)
            oWave.writeframes(b"\x00\x00" * iChunk)
            iSilence = iSilence - iChunk
        fRaw = open(dClip["raw"], "rb")
        binData = fRaw.read()
        fRaw.close()
        oWave.writeframes(binData)
        iCursor = iTarget + int(len(binData) / 2)
    iTail = int(nTotalSeconds * iSampleRate) - iCursor
    while iTail > 0:
        iChunk = min(iTail, iSampleRate)
        oWave.writeframes(b"\x00\x00" * iChunk)
        iTail = iTail - iChunk
    oWave.close()
    return True

def writeVtt(lItems, sPath, nOffset):
    """Write the description script as a WebVTT file for reading or editing."""
    fVtt = open(sPath, "w", encoding="utf-8", newline="\r\n")
    fVtt.write("WEBVTT\n\n")
    iNumber = 1
    for dItem in lItems:
        nStart = dItem["start"] + nOffset
        nEnd = nStart + dItem["spokenLength"]
        fVtt.write(str(iNumber) + "\n")
        fVtt.write(formatTimestamp(nStart) + " --> " + formatTimestamp(nEnd) + "\n")
        fVtt.write(dItem["text"] + "\n\n")
        iNumber = iNumber + 1
    fVtt.close()
    return True

def muxOutput(sVideo, sAdWave, sOutPath, sTrackTitle, nVolume):
    """Mux the video, the original audio, and a ducked mix with description."""
    sFilter = ("[1:a]aformat=sample_fmts=fltp:sample_rates=48000:channel_layouts=stereo,volume=" + str(nVolume) + ",asplit=2[adDuck][adMix];"
               "[0:a]aformat=sample_fmts=fltp:sample_rates=48000:channel_layouts=stereo[main];"
               "[main][adDuck]sidechaincompress=threshold=0.01:ratio=20:attack=5:release=300[duck];"
               "[duck][adMix]amix=inputs=2:duration=first:normalize=0[mix]")
    lArgs = ["ffmpeg", "-hide_banner", "-y", "-i", sVideo, "-i", sAdWave,
             "-filter_complex", sFilter,
             "-map", "0:v:0", "-c:v", "copy",
             "-map", "[mix]", "-c:a:0", "aac", "-b:a:0", "192k",
             "-map", "0:a:0", "-c:a:1", "copy",
             "-metadata:s:a:0", "title=" + sTrackTitle,
             "-metadata:s:a:1", "title=Original",
             "-disposition:a:0", "default", "-disposition:a:1", "0", sOutPath]
    iCode, sOut, sErr = runCommand(lArgs)
    if iCode != 0: logMessage(sErr[-1500:])
    return iCode == 0

def makeAnnouncement(oArgs, sWorkDir):
    """Speak a short opening line so the listener knows description is running."""
    sWavePath = os.path.join(sWorkDir, "clip000.wav")
    sRawPath = os.path.join(sWorkDir, "clip000.raw")
    if not speakToWave(sDefaultAnnouncement, sWavePath, oArgs.voice, oArgs.rate, sWorkDir): return None
    if not toRawPcm(sWavePath, sRawPath, iDefaultSampleRate): return None
    nSpoken = waveDuration(sWavePath)
    logMessage("Opening announcement is " + str(round(nSpoken, 1)) + " seconds long")
    return {"start": 0.0, "gapLength": nSpoken, "spokenLength": round(nSpoken, 3),
            "text": sDefaultAnnouncement, "raw": sRawPath, "rate": oArgs.rate}

def describeSample(sClipPath, oArgs, sWorkDir, dCache=None, sOutputDir="", nDuration=0.0, sVideoPath="", lSavedGaps=None, dSavedSignature=None):
    """Describe every chosen moment and return the item list."""
    if dCache is None: dCache = {}
    if sOutputDir == "": sOutputDir = os.path.dirname(sWorkDir)
    nBegan = time.time()
    nLastMux = time.time()
    binLast = b""
    iSkipped = 0
    lItems = []
    lRecent = []
    if oArgs.announce:
        dOpening = makeAnnouncement(oArgs, sWorkDir)
        if dOpening is not None: lItems.append(dOpening)
    dSignature = gapSignature(oArgs, nDuration)
    lGaps = []
    if lSavedGaps is not None and dSavedSignature == dSignature and len(lSavedGaps) > 0:
        lGaps = lSavedGaps
        logMessage("Reusing the " + str(len(lGaps)) + " moments worked out by the earlier run. The sound scan is skipped.")
    if len(lGaps) == 0 and lSavedGaps is not None and dSavedSignature != dSignature:
        logMessage("The settings have changed since the earlier run, so the moments are worked out again.")
    if len(lGaps) == 0: lGaps = findGaps(sClipPath, oArgs, nDuration)
    sJsonPath = os.path.join(sOutputDir, sDefaultJsonName)
    iIndex = 0
    for dGap in lGaps:
        iIndex = iIndex + 1
        try:
            iMaxWords = max(6, int(dGap["length"] * oArgs.words_per_second * dDetailWords.get(oArgs.detail, 1.6)))
            nAllowed = dGap["length"] + dDetailOverrun.get(oArgs.detail, 1.5)
            nMiddle = dGap["start"] + dGap["length"] / 2.0
            sKey = ("%09.3f" % dGap["start"]).replace(".", "_")
            sClipWave = os.path.join(sWorkDir, "clip" + sKey + ".wav")
            sClipRaw = os.path.join(sWorkDir, "clip" + sKey + ".raw")
            sText = dCache.get(str(dGap["start"]), "")
            bFromCache = sText != ""
            bHaveAudio = bFromCache and os.path.isfile(sClipRaw) and os.path.getsize(sClipRaw) > 0
            if bHaveAudio:
                nSpoken = os.path.getsize(sClipRaw) / 2.0 / float(iDefaultSampleRate)
                lItems.append({"start": dGap["start"], "gapLength": dGap["length"], "spokenLength": round(nSpoken, 3),
                               "text": sText, "raw": sClipRaw, "rate": oArgs.rate})
                lRecent.append(sText)
                if len(lRecent) > iDefaultCompare: lRecent.pop(0)
                logMessage("Moment " + str(iIndex) + " of " + str(len(lGaps)) + " reused whole from an earlier run",
                           "INFO", formatClock(dGap["start"]) + "  " + sText)
                continue
            if not bFromCache:
                sImagePath = buildMontage(sClipPath, nMiddle, dGap["length"] + 2.0, oArgs.frames, oArgs.frame_width, sWorkDir, oArgs.crop_bottom)
                if sImagePath == "": continue
                if oArgs.same_shot > 0.0:
                    binNow = shotSignature(sImagePath, sWorkDir)
                    nMoved = signatureDistance(binLast, binNow)
                    if len(binLast) > 0 and nMoved < oArgs.same_shot:
                        iSkipped = iSkipped + 1
                        logMessage("Moment " + str(iIndex) + " at " + str(round(dGap["start"], 1)) + "s looks the same as the last one, difference " + str(round(nMoved, 1)) + ". Saying nothing.", "INFO", "")
                        continue
                    binLast = binNow
                sText = describeImage(oArgs.ollama_url, oArgs.model, sImagePath, iMaxWords, oArgs.frames, lRecent[-iDefaultRecent:], oArgs.context, oArgs.detail)
                nLike = worstLikeness(sText, lRecent[-iDefaultCompare:])
                if sText != "" and nLike >= oArgs.similarity:
                    logMessage("That description was " + str(int(nLike * 100)) + " percent the same as a recent one. Asking again.", "INFO", "")
                    sText = describeImage(oArgs.ollama_url, oArgs.model, sImagePath, iMaxWords, oArgs.frames, lRecent[-iDefaultRecent:], oArgs.context, oArgs.detail, True)
                    nLike = worstLikeness(sText, lRecent[-iDefaultCompare:])
                if sText != "" and nLike >= oArgs.similarity:
                    iSkipped = iSkipped + 1
                    logMessage("Still " + str(int(nLike * 100)) + " percent the same. Saying nothing at " + str(round(dGap["start"], 1)) + "s rather than repeating.", "INFO", "")
                    continue
            if bFromCache: logMessage("Reusing the description already written for " + str(dGap["start"]) + "s", "INFO", "")
            if sText == "": logMessage("Moment " + str(iIndex) + " at " + str(dGap["start"]) + "s produced nothing", "INFO", "")
            if sText == "": continue
            sText = trimToWords(sText, iMaxWords)
            iRate = oArgs.rate
            if not speakToWave(sText, sClipWave, oArgs.voice, iRate, sWorkDir): continue
            nSpoken = waveDuration(sClipWave)
            if nSpoken > nAllowed and iRate < 8:
                iRate = min(8, iRate + 3)
                speakToWave(sText, sClipWave, oArgs.voice, iRate, sWorkDir)
                nSpoken = waveDuration(sClipWave)
            while nSpoken > nAllowed and len(splitSentences(sText)) > 1:
                sText = dropLastSentence(sText)
                speakToWave(sText, sClipWave, oArgs.voice, iRate, sWorkDir)
                nSpoken = waveDuration(sClipWave)
            if not toRawPcm(sClipWave, sClipRaw, iDefaultSampleRate): continue
            lItems.append({"start": dGap["start"], "gapLength": dGap["length"], "spokenLength": round(nSpoken, 3),
                           "text": sText, "raw": sClipRaw, "rate": iRate})
            lRecent.append(sText)
            if len(lRecent) > iDefaultCompare: lRecent.pop(0)
            logMessage("Moment " + str(iIndex) + " of " + str(len(lGaps)) + " at " + str(round(dGap["start"], 1)) + "s, " + str(round(nSpoken, 1)) + "s of " + str(round(dGap["length"], 1)) + "s: " + sText,
                       "INFO", formatClock(dGap["start"]) + "  " + sText)
            saveReadable(lItems, oArgs, sOutputDir, nDuration, sVideoPath, lGaps, dSignature)
            if iIndex % 10 == 0:
                nEach = (time.time() - nBegan) / float(iIndex)
                logMessage("Progress: " + str(iIndex) + " of " + str(len(lGaps)) + ", " + str(round(nEach, 1)) + " seconds each, about " + str(round(nEach * (len(lGaps) - iIndex) / 60.0, 1)) + " minutes left",
                           "INFO", "-- " + str(iIndex) + " of " + str(len(lGaps)) + " done, about " + str(int(round(nEach * (len(lGaps) - iIndex) / 60.0))) + " minutes left --")
            if iIndex % oArgs.checkpoint == 0:
                bDueMux = oArgs.mux_minutes > 0.0 and (time.time() - nLastMux) / 60.0 >= oArgs.mux_minutes
                saveOutputs(lItems, oArgs, sOutputDir, nDuration, sVideoPath, bDueMux, lGaps, dSignature)
                if bDueMux: nLastMux = time.time()
            if iIndex == len(lGaps) and iSkipped > 0: logMessage("Left " + str(iSkipped) + " moments silent because nothing had changed.", "INFO", "")
        except KeyboardInterrupt:
            logMessage("Stopped at moment " + str(iIndex) + " of " + str(len(lGaps)) + ". Saving what is finished.")
            logMessage("Run exactly the same command again to carry on from here.", "HINT")
            break
    return lItems

def gapSignature(oArgs, nDuration):
    """Describe the settings that decide where descriptions go, to spot a change."""
    return {"noiseFloor": oArgs.noise_floor, "silenceLength": oArgs.silence_length,
            "minGap": oArgs.min_gap, "spacing": oArgs.spacing, "every": oArgs.every,
            "forcedLength": oArgs.forced_length, "dialogueChannel": oArgs.dialogue_channel,
            "escalate": bool(oArgs.escalate), "duration": round(nDuration, 1)}

def writeCache(lItems, sPath, nWindowStart, lGaps=None, dSignature=None):
    """Save what has been described so far, so an interrupted run can resume."""
    dData = {"windowStartSeconds": round(nWindowStart, 3),
             "items": [{k: v for k, v in d.items() if k != "raw"} for d in lItems]}
    if lGaps is not None: dData["gaps"] = lGaps
    if dSignature is not None: dData["gapSignature"] = dSignature
    fJson = open(sPath, "w", encoding="utf-8", newline="\r\n")
    fJson.write(json.dumps(dData, indent=2))
    fJson.close()
    return True

def writeMarkdown(lItems, sPath, sSourceName, nDuration, sModel, sChapter):
    """Write the descriptions as a Markdown document that can be read on a braille display."""
    fDoc = open(sPath, "w", encoding="utf-8-sig", newline="\r\n")
    fDoc.write("# Audio description of " + sSourceName + "\n\n")
    fDoc.write("- Descriptions: " + str(len(lItems)) + "\n")
    fDoc.write("- Running time: " + formatClock(nDuration) + "\n")
    fDoc.write("- Written by: " + sModel + "\n")
    fDoc.write("- Generated: " + datetime.datetime.now().strftime("%d %B %Y") + "\n\n")
    fDoc.write("Each entry gives the time it is spoken, followed by the description. "
               "Times are counted from the start of the film.\n\n")
    nChapter = -1.0
    for dItem in sorted(lItems, key=lambda d: d["start"]):
        nThis = float(int(dItem["start"] / sChapter)) * sChapter
        if nThis != nChapter:
            nChapter = nThis
            fDoc.write("\n## " + formatClock(nChapter) + " to " + formatClock(min(nChapter + sChapter, nDuration)) + "\n\n")
        fDoc.write("- " + formatClock(dItem["start"]) + " " + dItem["text"].strip() + "\n")
    fDoc.close()
    return True

def saveReadable(lItems, oArgs, sOutputDir, nDuration, sVideoPath, lGaps=None, dSignature=None):
    """Write the small files, cheap enough to do after every single moment."""
    if len(lItems) == 0: return False
    writeCache(lItems, os.path.join(sOutputDir, sDefaultJsonName), 0.0, lGaps, dSignature)
    writeVtt(lItems, os.path.join(sOutputDir, sDefaultVttName), oArgs.time_offset if hasattr(oArgs, "time_offset") else 0.0)
    writeMarkdown(lItems, os.path.join(sOutputDir, sDefaultMarkdownName), os.path.basename(sVideoPath), nDuration, oArgs.model, nDefaultChapter)
    return True

def saveOutputs(lItems, oArgs, sOutputDir, nDuration, sVideoPath, bMux, lGaps=None, dSignature=None):
    """Write the script, the description track, and optionally the described film."""
    if len(lItems) == 0: return False
    saveReadable(lItems, oArgs, sOutputDir, nDuration, sVideoPath, lGaps, dSignature)
    sAdWave = os.path.join(sOutputDir, sDefaultWaveName)
    buildTrack(lItems, nDuration, sAdWave, iDefaultSampleRate)
    logMessage("Saved " + str(len(lItems)) + " descriptions to " + sAdWave, "INFO", "  (progress saved)")
    if not bMux: return True
    sOutPath = os.path.join(sOutputDir, sDefaultDescribedName)
    logMessage("Writing the described film so far. This takes a few minutes on a long film.")
    muxOutput(sVideoPath, sAdWave, sOutPath, sDefaultTrackTitle, oArgs.ad_volume)
    logMessage("Described film written to " + sOutPath)
    return True

def main():
    """Parse arguments and build the described sample."""
    global bVerbose, fLog
    oParser = argparse.ArgumentParser(description="Build an audio description track for a whole film, or for part of one.")
    oParser.add_argument("input", nargs="?", default=sDefaultInput, help="Path of the input video file; " + sDefaultInput + " by default")
    oParser.add_argument("-o", "--output-dir", default="", help="Directory for the outputs")
    oParser.add_argument("-b", "--start", default="0", help="Where the sample begins, in seconds or hh:mm:ss")
    oParser.add_argument("-e", "--minutes", type=float, default=0.0, help="Length to describe in minutes; 0 means the whole film")
    oParser.add_argument("-m", "--model", default=sDefaultModel, help="Name of the Ollama vision model")
    oParser.add_argument("-u", "--ollama-url", default=sDefaultOllamaUrl, help="Address of the Ollama generate endpoint")
    oParser.add_argument("-a", "--frames", type=int, default=iDefaultFrameCount, choices=[1, 2, 4], help="Frames tiled into each montage")
    oParser.add_argument("-v", "--voice", default="", help="Name of the Windows speech voice")
    oParser.add_argument("-r", "--rate", type=int, default=iDefaultRate, help="Speech rate from minus ten to ten")
    oParser.add_argument("-n", "--noise-floor", type=float, default=nDefaultNoiseFloor, help="Level in dB below which sound counts as a gap")
    oParser.add_argument("-s", "--silence-length", type=float, default=nDefaultSilenceLength, help="Shortest silence the detector reports")
    oParser.add_argument("-g", "--min-gap", type=float, default=nDefaultMinGap, help="Shortest gap worth describing")
    oParser.add_argument("-p", "--spacing", type=float, default=nDefaultSpacing, help="Least seconds between descriptions")
    oParser.add_argument("-w", "--words-per-second", type=float, default=nDefaultWordsPerSecond, help="Speaking rate used to budget words")
    oParser.add_argument("-f", "--frame-width", type=int, default=iDefaultFrameWidth, help="Width in pixels of each frame before tiling")
    oParser.add_argument("-c", "--list-voices", action="store_true", help="List the installed speech voices and stop")
    oParser.add_argument("-k", "--keep-window", action="store_true", help="Reuse an existing window.mkv instead of cutting again")
    oParser.add_argument("-R", "--fresh", action="store_true", help="Ignore any earlier run and describe everything again")
    oParser.add_argument("--checkpoint", type=int, default=iDefaultCheckpoint, help="Save the script and description track every this many moments")
    oParser.add_argument("--mux-minutes", type=float, default=nDefaultMuxMinutes, help="Least minutes between writes of the described film; 0 writes it only at the end")
    oParser.add_argument("-y", "--every", type=float, default=nDefaultEvery, help="Guarantee a description at least this often, in seconds; 0 turns it off")
    oParser.add_argument("-z", "--forced-length", type=float, default=nDefaultForcedLength, help="Seconds allowed for a description placed where no quiet moment was found")
    oParser.add_argument("-j", "--dialogue-channel", default="auto", choices=["auto", "on", "off"], help="Listen to the centre channel only, which carries most dialogue")
    oParser.add_argument("--escalate", action="store_true", help="Loosen the threshold and rescan when too few gaps are found; slow on a long film")
    oParser.add_argument("--similarity", type=float, default=nDefaultSimilarity, help="How alike two descriptions may be before one is rejected, 0 to 1")
    oParser.add_argument("--same-shot", type=float, default=nDefaultSameShot, help="How little the picture may change before a moment is passed over; 0 turns the check off")
    oParser.add_argument("--crop-bottom", type=float, default=nDefaultCropBottom, help="Percentage of the frame height to cut off the bottom, to hide burnt-in subtitles")
    oParser.add_argument("--context", default="", help="What the film is, for example: the 2026 film The Odyssey")
    oParser.add_argument("--context-file", default=sDefaultContextFile, help="Text file holding the context, used instead of --context")
    oParser.add_argument("--detail", default=sDefaultDetail, choices=["brief", "normal", "rich"], help="How much description to write for each moment")
    oParser.add_argument("--ad-volume", type=float, default=nDefaultAdVolume, help="Loudness of the description against the film; below 1 sits it under normal dialogue level")
    oParser.add_argument("--no-announce", dest="announce", action="store_false", help="Do not speak the opening line that confirms description is running")
    oParser.add_argument("-d", "--ffmpeg-dir", default="", help="Folder holding ffmpeg.exe, added to the PATH for this run")
    oParser.add_argument("-x", "--check", action="store_true", help="Run the prerequisite checks, write the log, and stop")
    oParser.add_argument("-q", "--verbose", action="store_true", help="Echo every command to the console as well as the log")
    oParser.add_argument("-l", "--log-file", default="", help="Path of the log file, by default beside the script")
    oArgs = oParser.parse_args()
    bVerbose = oArgs.verbose
    if oArgs.context.strip() != "": oArgs.context = "The film is " + oArgs.context.strip() + "."
    if oArgs.context_file != "" and not os.path.isfile(oArgs.context_file):
        sBeside = os.path.join(scriptFolder(), os.path.basename(oArgs.context_file))
        if os.path.isfile(sBeside): oArgs.context_file = sBeside
    if oArgs.context_file != "" and os.path.isfile(oArgs.context_file):
        fContext = open(oArgs.context_file, "r", encoding="utf-8-sig")
        oArgs.context = " ".join(fContext.read().split())
        fContext.close()
        logMessage("Context loaded from " + oArgs.context_file + ", " + str(len(oArgs.context.split())) + " words")
    sInput = os.path.abspath(oArgs.input)
    sOutputDir = os.path.abspath(oArgs.output_dir or os.path.splitext(sInput)[0] + "_described")
    sWorkDir = os.path.join(sOutputDir, "work")
    os.makedirs(sWorkDir, exist_ok=True)
    sLogPath = logPathFor(oArgs.log_file, sDefaultLogName)
    fLog = open(sLogPath, "w", encoding="utf-8", newline="\r\n")
    atexit.register(closeLog)
    logMessage("Log file: " + sLogPath)
    logMessage("describeMovie starting")
    logMessage("Input: " + sInput)
    logMessage("Output directory: " + sOutputDir)
    sys.excepthook = logException
    logEnvironment()
    logSettings(oArgs)
    if oArgs.ffmpeg_dir != "": addToPath(oArgs.ffmpeg_dir)
    addScriptFolderToPath()
    bReady = checkTools(lRequiredTools)
    checkOptionalTools(lOptionalTools)
    checkOllama(oArgs.ollama_url, oArgs.model)
    if oArgs.check: logMessage("Check finished. Prerequisites " + ("passed" if bReady else "FAILED"))
    if oArgs.check: return 0 if bReady else 1
    if not bReady: logMessage("Stopping because a required program is missing.", "ERROR")
    if not bReady: return 1
    if oArgs.list_voices: listVoices()
    if oArgs.list_voices: return 0
    if not os.path.isfile(sInput): logMessage("Input file not found.")
    if not os.path.isfile(sInput): return 1
    nStart = parseTime(oArgs.start)
    nSource = probeDuration(sInput)
    logMessage("Source duration: " + str(round(nSource, 1)) + " seconds, " + str(round(nSource / 60.0, 1)) + " minutes")
    bWhole = oArgs.minutes <= 0.0 and nStart <= 0.0
    sClipPath = sInput
    nClip = nSource
    if bWhole: logMessage("Describing the whole film. No window is cut.")
    if not bWhole:
        nLength = oArgs.minutes * 60.0
        if nLength <= 0.0: nLength = nSource - nStart
        if nStart + nLength > nSource: nLength = max(30.0, nSource - nStart)
        logMessage("Window: " + formatTimestamp(nStart) + " for " + str(round(nLength / 60.0, 2)) + " minutes")
        sClipPath = os.path.join(sOutputDir, sDefaultClipName)
        bHaveClip = oArgs.keep_window and os.path.isfile(sClipPath)
        if not bHaveClip: bHaveClip = extractWindow(sInput, sClipPath, nStart, nLength)
        if not bHaveClip: logMessage("Could not extract the window.", "ERROR")
        if not bHaveClip: return 1
        nClip = probeDuration(sClipPath)
        logMessage("Window written, " + str(round(nClip, 1)) + " seconds")
    dCache = {}
    lSavedGaps = None
    dSavedSignature = None
    sJsonPath = os.path.join(sOutputDir, sDefaultJsonName)
    if not oArgs.fresh and os.path.isfile(sJsonPath):
        fJson = open(sJsonPath, "r", encoding="utf-8-sig")
        dSaved = json.load(fJson)
        fJson.close()
        for dItem in dSaved.get("items", []):
            dCache[str(dItem.get("start", ""))] = dItem.get("text", "")
        lSavedGaps = dSaved.get("gaps", None)
        dSavedSignature = dSaved.get("gapSignature", None)
        logMessage("Picking up where the last run stopped: " + str(len(dCache)) + " descriptions already written")
        logMessage("Pass --fresh to ignore them and start over.", "HINT")
    lItems = describeSample(sClipPath, oArgs, sWorkDir, dCache, sOutputDir, nClip, sClipPath, lSavedGaps, dSavedSignature)
    if len(lItems) == 0: logMessage("No descriptions were produced. Check that Ollama is running and the model is pulled.")
    if len(lItems) == 0: return 1
    saveOutputs(lItems, oArgs, sOutputDir, nClip, sClipPath, True, None, gapSignature(oArgs, nClip))
    logMessage("Finished with " + str(len(lItems)) + " descriptions in " + str(round(nClip / 60.0, 2)) + " minutes")
    fLog.close()
    return 0

if __name__ == "__main__":
    sys.exit(main())
