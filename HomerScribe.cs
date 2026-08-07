// HomerScribe.cs -- audio description for local video files.
//
// Reads a video file, finds the moments where a description can be spoken,
// asks a vision model served by Ollama what is on screen, speaks the answer
// with a built-in Windows voice, and writes a described copy of the film.
//
// Everything runs on this machine. Nothing is uploaded.
//
// Style: Camel Type. Hungarian prefixes on typed identifiers, lower camel case
// for methods and variables, constants named with a Default or Initial word.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Speech.AudioFormat;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System;

namespace Homer
{
    // One setting, shared by the command line and, later, by the dialog.
    // The long form is both the command line parameter and the dialog label.
    // The short form is both the command line letter and the dialog trigger.
    public class Param
    {
        public string sLong;
        public string sShort;
        public string sKind;
        public string sValue;
        public string sHelp;
        public bool bGiven;

        public Param(string sLongIn, string sShortIn, string sKindIn, string sValueIn, string sHelpIn)
        {
            sLong = sLongIn;
            sShort = sShortIn;
            sKind = sKindIn;
            sValue = sValueIn;
            sHelp = sHelpIn;
            bGiven = false;
        }
    }

    // One moment of the film that has been, or is about to be, described.
    public class Moment
    {
        public double nStart;
        public double nLength;
        public double nSpoken;
        public string sText;
        public byte[] binAudio;
        public bool bForced;

        public Moment()
        {
            nStart = 0.0;
            nLength = 0.0;
            nSpoken = 0.0;
            sText = "";
            binAudio = new byte[0];
            bForced = false;
        }
    }

    // Hide the console when HomerScribe was started from Explorer, a shortcut
    // or the hotkey. The test is GetConsoleProcessList: exactly ONE process
    // attached means Windows made that console for HomerScribe alone, so
    // hiding it removes a window nobody asked for. Two or more means it was run
    // from an existing cmd.exe, and that console belongs to the user.
    static class consoleWindow
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetConsoleProcessList([Out] uint[] aiProcessIds, uint iCount);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWindow, int iCommand);

        const int iSwHide = 0;

        public static int attachedCount()
        {
            try
            {
                uint[] aiList = new uint[16];
                return (int)GetConsoleProcessList(aiList, (uint)aiList.Length);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static bool launchedFromGui()
        {
            return attachedCount() == 1;
        }

        // Hiding the window is the polite way. When that does not take -- and on
        // at least one machine it did not -- the console is let go of entirely,
        // which destroys the window because this process is the only one holding
        // it. Nothing is written to it afterwards, so nothing is lost.
        public static bool hide()
        {
            try
            {
                IntPtr hWindow = GetConsoleWindow();
                if (hWindow == IntPtr.Zero) return true;
                ShowWindow(hWindow, iSwHide);
                if (!IsWindowVisible(hWindow)) return true;
                FreeConsole();
                return GetConsoleWindow() == IntPtr.Zero;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWindow);
    }

    // One stretch of speech, as Whisper heard it.
    public class Speech
    {
        public double nStart;
        public double nEnd;
        public string sText;

        public Speech()
        {
            nStart = 0.0;
            nEnd = 0.0;
            sText = "";
        }
    }

    public class HomerScribe
    {
        const int iNothingToDo = 2;
        const int iAlreadyDone = 3;
        const int iDefaultCompare = 10;
        const int iDefaultNames = 12;
        const int iDefaultRecent = 2;
        const int iDefaultSampleRate = 48000;
        const int iDefaultScanReport = 10;
        const int iDefaultSpokenReport = 20;
        const int iDefaultGroupLength = 700;
        const int iDefaultSpokenLimit = 1500;
        const int iDefaultBigPlaylist = 12;
        const int iDefaultTimeout = 300000;
        const int iDefaultHeartbeat = 25;
        const double nDefaultChapter = 600.0;
        const double nDefaultNewScene = 25.0;
        const double nDefaultSlowSeconds = 30.0;
        const double nDefaultTalkative = 70.0;
        const double nDefaultTitleAgreement = 0.8;
        const double nDefaultLead = 0.20;
        // Somewhere to start. The W3C Web Accessibility Initiative's ten
        // Perspectives films, seven and a half minutes in one, freely licensed
        // and published with professionally written descriptions of every shot
        // -- so a first run can be compared against how it should have been done.
        const string sDefaultSource = "https://www.youtube.com/watch?v=3f31oufqFSM";
        const string sDefaultDescribedStem = "described";
        const string sDefaultJsonName = "described.json";
        const string sDefaultLogName = "HomerScribe.log";
        const string sDefaultMarkdownName = "described.md";
        const string sDefaultTranscriptName = "transcribed.md";
        const string sDefaultBothName = "scribed.md";
        const string sDefaultTrackTitle = "Audio Description";
        const string sDefaultVttName = "described.vtt";
        const string sDefaultWaveName = "described.wav";

        static StreamWriter fLog = null;
        static FileStream fLogStream = null;
        static DateTime dtLogFlushed = DateTime.MinValue;
        static string sLogPath = "";
        static readonly object oLogLock = new object();
        static bool bVerbose = false;
        static Dictionary<string, Param> dParams = new Dictionary<string, Param>();

        // ---------- settings ----------

        static void buildParams()
        {
            // The long form is the command line parameter and the dialog label.
            // The short form is the command line letter and the dialog trigger.
            // Settings with no dialog control of their own take no short form,
            // which keeps the natural letters free for the ones that do.
            addParam("source-paths", "s", "string", sDefaultSource, "Video files or YouTube page addresses, separated by spaces; quote any containing a space. The dialog starts browsing in your Videos folder");
            addParam("output-dir", "o", "string", "", "Folder to create each video's results folder in. Empty on the command line means beside the video; the dialog offers your Videos folder");
            addParam("describe", "d", "flag", "no", "Describe what happens on screen and write a described copy of the film");
            addParam("transcribe", "t", "flag", "no", "Write down what is said, from the film's own sound");
            addParam("web-context", "w", "flag", "no", "Learn what the video is by asking the page it came from, or Wikipedia about its title, and use that as context");
            addParam("force", "f", "flag", "no", "Describe everything again, ignoring an earlier run");
            addParam("audio-only", "a", "flag", "no", "Produce sound only: one mp3 of the film's audio with the descriptions mixed in, and no video");
            addParam("view-output", "v", "flag", "no", "Open the results folder when the run finishes");
            addParam("rebuild", "", "flag", "no", "Build the film from descriptions already made, asking the model nothing");
            addParam("log-file", "", "string", "", "Path of the run log; by default it goes with the results");
            addParam("speech", "", "flag", "yes", "Find where descriptions go by detecting speech with Whisper, rather than by listening for silence");
            addParam("whisper-model", "", "string", "small", "Which Whisper model to hear the film with: tiny, base, small, medium or large-v3");
            addParam("dialogue-window", "", "number", "25.0", "Seconds of dialogue before a moment shown to the model, so a description does not restate what was just said");
            addParam("summarise", "", "flag", "yes", "Look thoroughly with the vision model, then have the same model compress what it saw into one spoken description");
            addParam("log-session", "l", "flag", "no", "Keep a copy of the log in each video's own folder");
            addParam("use-configuration", "u", "flag", "no", "Load settings at startup and save them on OK");
            addParam("begin", "b", "string", "0", "Where to start, in seconds or hh:mm:ss");
            addParam("minutes", "e", "number", "0", "How many minutes to describe; 0 means the whole film");
            addParam("model", "m", "string", "qwen2.5vl:7b", "Name of the Ollama vision model");
            addParam("context-file", "c", "string", "", "Text file describing the film, sent with every request");
            addParam("detail", "", "string", "rich", "How much to say: brief, normal or rich");
            addParam("voice", "", "string", "", "Name of the Windows speech voice");
            addParam("rate", "r", "integer", "1", "Speech rate, from minus ten to ten");
            addParam("width", "", "integer", "512", "Width in pixels of each frame before tiling");
            addParam("crop-bottom", "p", "number", "12", "Percentage cut off the bottom of each frame, to hide burnt-in subtitles");
            addParam("noise-floor", "n", "number", "-24", "Level in dB below which sound counts as a gap");
            addParam("min-gap", "g", "number", "2.0", "Shortest gap worth describing");
            addParam("spacing", "", "number", "10.0", "Least seconds between descriptions");
            addParam("every", "y", "number", "14.0", "Guarantee a description at least this often; 0 turns it off");
            addParam("max-words", "", "integer", "45", "Longest a single description may be, however much room the gap allows");
            addParam("words-per-second", "", "number", "2.67", "Speaking rate used to budget words; 2.67 is the 160 words a minute the standards call comfortable");
            addParam("url", "", "string", "http://localhost:11434", "Address of the Ollama service");
            addParam("frames", "", "integer", "4", "Frames tiled into each montage: 1, 2 or 4");
            addParam("silence-length", "", "number", "1.0", "Shortest silence the detector reports");
            addParam("dialogue-channel", "", "string", "auto", "Listen to the centre channel only, which carries most dialogue: auto, on or off");
            addParam("forced-length", "", "number", "8.0", "Seconds allowed for a description placed where no quiet moment was found");
            addParam("max-silence", "", "number", "45.0", "Longest the film may run with nothing said before a description is kept even though it echoes an earlier one");
            addParam("similarity", "", "number", "0.6", "How alike two descriptions may be before one is rejected");
            addParam("same-shot", "", "number", "4.0", "How little the picture may change before a moment is passed over; 0 turns the check off");
            addParam("ad-volume", "", "number", "0.9", "Loudness of the description against the film");
            addParam("checkpoint", "", "integer", "15", "Rebuild the description track every this many moments");
            addParam("mux-minutes", "", "number", "0", "Least minutes between background writes of the film so far; 0 writes it only at the end");
            addParam("browser-cookies", "", "string", "", "Name of a browser whose cookies yt-dlp may use, for a video that asks the viewer to sign in: chrome, edge, firefox, brave or opera");
            addParam("ffmpeg-dir", "", "string", "", "Folder holding ffmpeg.exe, searched in addition to the PATH");
            addParam("objective", "", "flag", "yes", "Ask again when a description states a mood or a judgement instead of what is visible");
            addParam("announce", "", "flag", "yes", "Speak an opening line confirming description is running");
            addParam("announce-progress", "", "flag", "yes", "Speak progress and each description through the dialog's status line");
            addParam("boxes", "", "flag", "no", "Use timed message boxes instead of the status line. They announce reliably but take the keyboard focus");
            addParam("check", "C", "flag", "no", "Check the environment, write the log, and stop");
            addParam("list-voices", "L", "flag", "no", "List the installed speech voices and stop");
            addParam("verbose", "V", "flag", "no", "Echo every command to the console as well as the log");
            addParam("gui", "G", "flag", "no", "Show the settings dialog instead of running at once");
            addParam("help", "?", "flag", "no", "Show this help and stop");
        }

        static void addParam(string sLong, string sShort, string sKind, string sValue, string sHelp)
        {
            dParams[sLong] = new Param(sLong, sShort, sKind, sValue, sHelp);
        }

        static string text(string sLong)
        {
            if (!dParams.ContainsKey(sLong)) return "";
            return dParams[sLong].sValue;
        }

        static double number(string sLong)
        {
            double nValue = 0.0;
            double.TryParse(text(sLong), NumberStyles.Any, CultureInfo.InvariantCulture, out nValue);
            return nValue;
        }

        static int integer(string sLong)
        {
            int iValue = 0;
            int.TryParse(text(sLong), NumberStyles.Any, CultureInfo.InvariantCulture, out iValue);
            return iValue;
        }

        static bool flag(string sLong)
        {
            return text(sLong) == "yes";
        }

        // JavaScriptSerializer returns ArrayList for a JSON array, not object[],
        // so every array it produces is read through here.
        static List<object> toList(object oValue)
        {
            List<object> lItems = new List<object>();
            if (oValue == null) return lItems;
            System.Collections.IEnumerable oSequence = oValue as System.Collections.IEnumerable;
            if (oSequence == null) return lItems;
            foreach (object oItem in oSequence) lItems.Add(oItem);
            return lItems;
        }

        static Dictionary<string, object> toMap(object oValue)
        {
            Dictionary<string, object> dMap = oValue as Dictionary<string, object>;
            if (dMap == null) return new Dictionary<string, object>();
            return dMap;
        }

        static string num(double nValue)
        {
            return nValue.ToString("0.###", CultureInfo.InvariantCulture);
        }

        static bool parseArgs(string[] asArgs)
        {
            int iAt = 0;
            bool bTookInput = false;
            while (iAt < asArgs.Length)
            {
                string sArg = asArgs[iAt];
                string sName = "";
                string sValue = "";
                bool bHasValue = false;
                if (sArg.StartsWith("--"))
                {
                    sName = sArg.Substring(2);
                    int iEquals = sName.IndexOf('=');
                    if (iEquals > 0)
                    {
                        sValue = sName.Substring(iEquals + 1);
                        sName = sName.Substring(0, iEquals);
                        bHasValue = true;
                    }
                }
                else if (sArg.StartsWith("-") && sArg.Length > 1)
                {
                    string sLetter = sArg.Substring(1);
                    foreach (KeyValuePair<string, Param> oPair in dParams)
                    {
                        if (oPair.Value.sShort != "" && oPair.Value.sShort == sLetter) sName = oPair.Key;
                    }
                    if (sName == "")
                    {
                        Console.WriteLine("Unknown option: " + sArg);
                        return false;
                    }
                }
                else
                {
                    // A bare word is a source. Several may be given.
                    string sSoFar = dParams["source-paths"].sValue;
                    if (sSoFar != "") sSoFar = sSoFar + " ";
                    dParams["source-paths"].sValue = sSoFar + quotedIfSpaced(sArg);
                    dParams["source-paths"].bGiven = true;
                    bTookInput = true;
                    iAt = iAt + 1;
                    continue;
                }
                if (!dParams.ContainsKey(sName))
                {
                    Console.WriteLine("Unknown option: " + sArg);
                    return false;
                }
                Param oParam = dParams[sName];
                oParam.bGiven = true;
                if (oParam.sKind == "flag")
                {
                    oParam.sValue = "yes";
                    if (bHasValue && sValue.ToLower() == "no") oParam.sValue = "no";
                    iAt = iAt + 1;
                    continue;
                }
                if (!bHasValue)
                {
                    if (iAt + 1 >= asArgs.Length)
                    {
                        Console.WriteLine("Option " + sArg + " needs a value.");
                        return false;
                    }
                    sValue = asArgs[iAt + 1];
                    iAt = iAt + 1;
                }
                oParam.sValue = sValue;
                iAt = iAt + 1;
            }
            if (bTookInput) return true;
            return true;
        }

        // Forty-odd settings in one flat list is an inventory, not a help
        // screen. They are grouped as a person would ask about them, wrapped to
        // a readable width, and followed by examples, because most people read
        // the examples and nothing else.
        static readonly string[,] asHelpGroups = new string[,] {
            { "What to do, and to what", "describe transcribe source-paths begin minutes" },
            { "Knowing what it is watching", "context-file web-context" },
            { "Where things go", "output-dir audio-only view-output force rebuild" },
            { "What the description says", "detail words-per-second max-words summarise objective" },
            { "The voice", "voice rate ad-volume announce" },
            { "Hearing the film, and where descriptions go", "speech whisper-model dialogue-window every spacing min-gap forced-length noise-floor silence-length dialogue-channel max-silence" },
            { "Not saying the same thing twice", "similarity same-shot" },
            { "The model and the picture", "model url frames width crop-bottom" },
            { "Settings, logs and diagnostics", "use-configuration log-session log-file checkpoint mux-minutes ffmpeg-dir browser-cookies announce-progress boxes gui check list-voices verbose help" },
        };

        static void writeWrapped(string sIndent, string sText, int iWidth)
        {
            string sLine = sIndent;
            foreach (string sWord in sText.Split(' '))
            {
                if (sWord == "") continue;
                if (sLine.Trim() != "" && sLine.Length + 1 + sWord.Length > iWidth)
                {
                    Console.WriteLine(sLine);
                    sLine = new string(' ', sIndent.Length);
                }
                if (sLine.Trim() == "") sLine = sLine + sWord;
                else sLine = sLine + " " + sWord;
            }
            if (sLine.Trim() != "") Console.WriteLine(sLine);
        }

        static void writeOption(string sName)
        {
            if (!dParams.ContainsKey(sName)) return;
            Param oParam = dParams[sName];
            string sHead = "  --" + oParam.sLong;
            if (oParam.sShort != "") sHead = "  -" + oParam.sShort + ", --" + oParam.sLong;
            if (oParam.sKind != "flag") sHead = sHead + " <" + oParam.sKind + ">";
            string sTail = oParam.sHelp;
            if (oParam.sKind == "flag" && oParam.sValue == "yes") sTail = sTail + ". Already on; turn it off with --" + oParam.sLong + " no";
            if (oParam.sKind != "flag" && oParam.sValue != "") sTail = sTail + ". Now: " + oParam.sValue;
            Console.WriteLine(sHead);
            writeWrapped("      ", sTail, 78);
        }

        static void showHelp()
        {
            Console.WriteLine("HomerScribe " + version() + ", describing and transcribing video and audio.");
            Console.WriteLine("");
            writeWrapped("", "Describes what happens on screen, writes down what is said, or both. "
                + "Everything runs on this machine: nothing is uploaded and no account is needed.", 78);
            Console.WriteLine("");
            Console.WriteLine("Usage: HomerScribe --describe and/or --transcribe [files, patterns or addresses]");
            Console.WriteLine("");
            writeWrapped("", "Run it with nothing at all to open the dialog. Name a file called after the "
                + "video and beside it, video.md for video.mkv, and its characters and setting are used "
                + "in the descriptions without being asked for.", 78);
            List<string> lShown = new List<string>();
            for (int iGroup = 0; iGroup < asHelpGroups.GetLength(0); iGroup++)
            {
                Console.WriteLine("");
                Console.WriteLine(asHelpGroups[iGroup, 0]);
                foreach (string sName in asHelpGroups[iGroup, 1].Split(' '))
                {
                    writeOption(sName);
                    lShown.Add(sName);
                }
            }
            bool bAnyLeft = false;
            foreach (KeyValuePair<string, Param> oPair in dParams)
            {
                if (lShown.Contains(oPair.Key)) continue;
                if (!bAnyLeft) Console.WriteLine("");
                if (!bAnyLeft) Console.WriteLine("Other");
                bAnyLeft = true;
                writeOption(oPair.Key);
            }
            Console.WriteLine("");
            Console.WriteLine("What is written, in a folder named after each source");
            Console.WriteLine("");
            writeWrapped("  ", "--describe    described.mkv (or .mp3), and described.md, the script to read", 78);
            writeWrapped("  ", "--transcribe  transcribed.md, what is said", 78);
            writeWrapped("  ", "both          scribed.md as well: the two interleaved, in the order they happen", 78);
            Console.WriteLine("");
            Console.WriteLine("Examples");
            Console.WriteLine("");
            Console.WriteLine("  HomerScribe");
            writeWrapped("      ", "Open the dialog.", 78);
            Console.WriteLine("  HomerScribe --describe \"film.mkv\"");
            writeWrapped("      ", "Describe one film, into a folder called film beside it.", 78);
            Console.WriteLine("  HomerScribe --transcribe \"talk.mp3\"");
            writeWrapped("      ", "Write down what is said in a recording.", 78);
            Console.WriteLine("  HomerScribe --describe --transcribe \"film.mkv\"");
            writeWrapped("      ", "Both, and the interleaved account as well.", 78);
            Console.WriteLine("  HomerScribe --transcribe \"C:\\\\audio\\\\*.mp3\"");
            writeWrapped("      ", "Transcribe every recording in a folder.", 78);
            Console.WriteLine("  HomerScribe --describe \"film.mkv\" --begin 00:22:30 --minutes 5");
            writeWrapped("      ", "Describe five minutes, to hear what it sounds like before committing to the whole film.", 78);
            Console.WriteLine("  HomerScribe --describe --transcribe --check");
            writeWrapped("      ", "Say whether ffmpeg, Whisper, the voices and the model are in place, and stop.", 78);
            Console.WriteLine("");
            writeWrapped("", "Full documentation is in ReadMe.htm beside the program.", 78);
        }

        // BuildVersion lives in Version.cs, generated by buildHomerScribe.cmd
        // from version.txt, which is the single source of the version number.
        static string version()
        {
            return BuildVersion.Version;
        }

        // ---------- the log ----------

        // Where an installed program may write. Beside the executable is
        // C:\Program Files\HomerScribe, which an ordinary user cannot write
        // to, so the settings, the working files and sometimes the log live here.
        static string appDataFolder()
        {
            string sFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HomerScribe");
            try
            {
                Directory.CreateDirectory(sFolder);
            }
            catch (Exception)
            {
            }
            return sFolder;
        }

        // One video's working files. The name alone would collide across
        // folders, so the full path is folded into a short tag.
        static string workFolderFor(string sInput)
        {
            uint iHash = 2166136261;
            foreach (char cOne in sInput.ToLower())
            {
                iHash = (iHash ^ (uint)cOne) * 16777619;
            }
            string sName = Path.GetFileNameWithoutExtension(sInput);
            if (sName.Length > 40) sName = sName.Substring(0, 40);
            foreach (char cBad in Path.GetInvalidFileNameChars())
            {
                sName = sName.Replace(cBad, '_');
            }
            return Path.Combine(appDataFolder(), "work", sName + "-" + iHash.ToString("x8"));
        }

        static string exeFolder()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        // Where the log lives cannot be settled until the settings are known,
        // and in dialog mode that is after the dialog is answered. Early lines
        // are held in memory and written out when the file opens.
        static StringBuilder oEarlyLog = new StringBuilder();

        static string chooseLogFolder()
        {
            if (text("log-file") != "")
            {
                try
                {
                    string sGiven = Path.GetDirectoryName(Path.GetFullPath(text("log-file")));
                    if (sGiven != "") return sGiven;
                }
                catch (Exception)
                {
                }
            }
            // Unticked, the log is still written -- a run that goes wrong must
            // leave a record -- but out of the way rather than among the results.
            if (!flag("log-session")) return appDataFolder();
            if (text("output-dir") != "") return text("output-dir");
            foreach (string sSource in splitPaths(text("source-paths")))
            {
                if (sSource.StartsWith("http")) continue;
                try
                {
                    string sFolder = Path.GetDirectoryName(Path.GetFullPath(sSource));
                    if (sFolder != "" && Directory.Exists(sFolder)) return sFolder;
                }
                catch (Exception)
                {
                }
            }
            return appDataFolder();
        }

        // Open the log somewhere specific. Used first for a provisional log and
        // then, once the settings are known, for the real one.
        static void openLogAt(string sPath)
        {
            string sWanted = sPath;
            if (fLog != null)
            {
                // Already logging somewhere. Move to the new place, carrying
                // everything written so far, so nothing is lost and there is
                // only ever one log for a run.
                string sSoFar = "";
                try
                {
                    fLog.Flush();
                    if (fLogStream != null) fLogStream.Flush(true);
                    string sOld = sLogPath;
                    fLog.Close();
                    fLog = null;
                    fLogStream = null;
                    if (sOld != "" && File.Exists(sOld)) sSoFar = File.ReadAllText(sOld);
                    if (string.Compare(sOld, sWanted, true) == 0) sSoFar = "";
                    if (sSoFar != "" && File.Exists(sOld) && string.Compare(sOld, sWanted, true) != 0) File.Delete(sOld);
                }
                catch (Exception)
                {
                }
                openLogFile(sWanted);
                if (sSoFar != "" && fLog != null)
                {
                    fLog.Write(sSoFar);
                    fLog.Flush();
                }
                return;
            }
            openLogFile(sWanted);
        }

        static void openLog()
        {
            string sPath = text("log-file");
            if (sPath == "") sPath = Path.Combine(chooseLogFolder(), sDefaultLogName);
            if (string.Compare(sLogPath, sPath, true) == 0) return;
            openLogAt(sPath);
            logMessage("Log file: " + sLogPath, "INFO", "Log: " + sLogPath);
            return;
        }

        static void openLogFile(string sPath)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(sPath)));
                fLogStream = new FileStream(sPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                fLog = new StreamWriter(fLogStream, new UTF8Encoding(true));
                fLog.AutoFlush = true;
                sLogPath = sPath;
            }
            catch (Exception oError)
            {
                Console.WriteLine("The log could not be opened at " + sPath + ": " + oError.Message);
                sPath = Path.Combine(appDataFolder(), sDefaultLogName);
                try
                {
                    fLogStream = new FileStream(sPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                    fLog = new StreamWriter(fLogStream, new UTF8Encoding(true));
                    fLog.AutoFlush = true;
                    sLogPath = sPath;
                }
                catch (Exception oSecond)
                {
                    Console.WriteLine("Nor at " + sPath + ": " + oSecond.Message);
                    return;
                }
            }
            lock (oLogLock)
            {
                if (oEarlyLog != null) fLog.Write(oEarlyLog.ToString());
                oEarlyLog = null;
                fLog.Flush();
            }
        }

        static void closeLog()
        {
            if (fLog == null) return;
            logMessage("Log closed", "INFO", "");
            fLog.Flush();
            try
            {
                if (fLogStream != null) fLogStream.Flush(true);
            }
            catch (Exception)
            {
            }
            fLog.Close();
            fLog = null;
            fLogStream = null;
            sLogPath = "";
        }

        static void logMessage(string sText)
        {
            logMessage(sText, "INFO", null);
        }

        static void logMessage(string sText, string sLevel)
        {
            logMessage(sText, sLevel, null);
        }

        // Full detail goes to the file. The console gets something a person
        // would want to read: by default the description that was embedded.
        static void logMessage(string sText, string sLevel, string sConsole)
        {
            string sStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            string sLine = sStamp + "  " + sLevel.PadRight(5) + "  " + sText;
            // The film is written on a second thread while describing carries on,
            // so both may be logging at once.
            lock (oLogLock)
            {
                if (fLog != null)
                {
                    fLog.WriteLine(sLine);
                    fLog.Flush();
                    // Flushing the WRITER hands the text to Windows; flushing
                    // the FILE makes Windows write it out and update the size in
                    // the directory. Without the second, someone watching the
                    // log to see whether a run is still alive sees zero bytes
                    // however much has been written. Done once a second, which
                    // costs nothing and keeps the file honest.
                    if (fLogStream != null && DateTime.Now.Subtract(dtLogFlushed).TotalSeconds >= 1.0)
                    {
                        dtLogFlushed = DateTime.Now;
                        try
                        {
                            fLogStream.Flush(true);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                else if (oEarlyLog != null) oEarlyLog.AppendLine(sLine);
            }
            if (sLevel == "CMD" && !bVerbose) return;
            // The console is hidden, so there is nobody to write to.
            if (bConsoleHidden) return;
            string sShow = sText;
            if (sConsole != null) sShow = sConsole;
            if (sShow == "") return;
            if (sLevel == "ERROR" || sLevel == "HINT" || sLevel == "FATAL") sShow = sLevel.Substring(0, 1) + sLevel.Substring(1).ToLower() + ": " + sShow;
            if (sLevel == "CMD") sShow = sLine;
            try
            {
                Console.WriteLine(sShow);
            }
            catch (Exception)
            {
            }
        }

        // Everything said while one video is being described, kept so a copy can
        // be left in that video's own folder. The running log beside the program
        // still holds the whole session.
        static void logEnvironment()
        {
            logMessage("HomerScribe " + version() + " starting", "INFO", "");
            logMessage("Program: " + System.Reflection.Assembly.GetExecutingAssembly().Location, "INFO", "");
            logMessage("Framework: " + Environment.Version.ToString(), "INFO", "");
            logMessage("Platform: " + Environment.OSVersion.ToString() + ", 64 bit process: " + Environment.Is64BitProcess.ToString(), "INFO", "");
            logMessage("Working directory: " + Environment.CurrentDirectory, "INFO", "");
            logMessage("Command line: " + Environment.CommandLine, "INFO", "");
        }

        static void logSettings()
        {
            List<string> lNames = new List<string>(dParams.Keys);
            lNames.Sort();
            foreach (string sName in lNames)
            {
                logMessage("Setting " + sName + " = " + dParams[sName].sValue, "INFO", "");
            }
        }

        // ---------- spoken status, the bookFido way ----------
        //
        // A timed message box is used rather than a status line, because a
        // screen reader speaks a window that is truly activated, and speaks it
        // without the user asking. The caption carries the position in the film
        // and the body carries the description just embedded. The box closes
        // itself, so nothing has to be dismissed.

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int MessageBoxTimeoutW(IntPtr hWnd, string sText, string sCaption, uint iType, ushort iLanguageId, uint iMilliseconds);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr FindWindowW(string sClassName, string sWindowName);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWindow, out uint iProcessId);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWindow);

        [DllImport("user32.dll")]
        static extern bool BringWindowToTop(IntPtr hWindow);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWindow, IntPtr hProcessId);

        [DllImport("user32.dll")]
        static extern bool AttachThreadInput(uint iAttachThread, uint iAttachToThread, bool bAttach);

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        static extern bool SetFocus(IntPtr hWindow);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern bool PeekMessageW(out MSG oMessage, IntPtr hWindow, uint iFilterMin, uint iFilterMax, uint iRemove);

        struct MSG
        {
            public IntPtr hWindow;
            public uint iMessage;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint iTime;
            public int iPointX;
            public int iPointY;
        }

        const uint iMbOk = 0x00000000;
        const uint iMbSetForeground = 0x00010000;
        const uint iMbTopmost = 0x00040000;
        const int iDefaultBoxMs = 2000;
        const int iDefaultBoxMaxMs = 15000;

        static bool bBoxes = false;
        static bool bAnnouncing = false;
        static bool bGuiMode = false;

        // The dialog is kept, not thrown away, when OK is pressed. Lbc shows it
        // with ShowDialog, which HIDES the form on close rather than disposing
        // it, so the same window can be shown again and left up for the whole
        // run. Its controls are disabled -- the answers are given -- but it is
        // there in Alt+Tab, it carries the progress in its title, and it owns
        // every message box, so nothing HomerScribe says can appear behind
        // something else.
        static LbcDialog oLiveDialog = null;

        static Form ownerForm()
        {
            try
            {
                if (oLiveDialog == null) return null;
                if (oLiveDialog.form == null) return null;
                if (oLiveDialog.form.IsDisposed) return null;
                return oLiveDialog.form;
            }
            catch (Exception)
            {
                return null;
            }
        }

        static void disableDialog(Control oParent)
        {
            foreach (Control oChild in oParent.Controls)
            {
                if (oChild.Controls.Count > 0) disableDialog(oChild);
                oChild.Enabled = false;
            }
        }

        static void keepDialogUp()
        {
            Form oForm = ownerForm();
            if (oForm == null) return;
            try
            {
                disableDialog(oForm);
                attachStatusLine(oForm);
                oForm.Text = "HomerScribe, working";
                bAnnouncing = flag("announce-progress");
                announce("Initializing", -1.0, 1.0, "Starting.");
                oForm.Show();
                oForm.Refresh();
            }
            catch (Exception oError)
            {
                logMessage("The dialog could not be kept on screen: " + oError.Message, "INFO", "");
            }
        }

        // What the window says it is doing, which is what a screen reader reads
        // when the user finds it with Alt+Tab.
        // The window title, for the times when there is no announcement to put
        // there: the long passes between one description and the next. An
        // announcement overwrites it with something fuller.
        static void dialogSays(string sWhat)
        {
            Form oForm = ownerForm();
            if (oForm == null) return;
            try
            {
                oForm.Text = "HomerScribe, " + sWhat;
                if (oStatusLine != null && sLatestStatus == "")
                {
                    oStatusLine.Text = sWhat;
                    oStatusLine.Refresh();
                }
            }
            catch (Exception)
            {
            }
        }

        // The work runs on this thread, so the window only redraws when it is
        // given the chance. Called between moments and while a long pass reports.
        static void pumpDialog()
        {
            if (ownerForm() == null) return;
            try
            {
                Application.DoEvents();
            }
            catch (Exception)
            {
            }
        }

        static void closeDialog()
        {
            try
            {
                if (oLiveDialog != null) oLiveDialog.Dispose();
            }
            catch (Exception)
            {
            }
            oLiveDialog = null;
        }

        static List<Speech> lFilmSpeech = new List<Speech>();
        static string sSpeechWorkDir = "";
        static bool bConsoleHidden = false;
        static string sLastSkippedFolder = "";
        static int iSourceAt = 0;
        static int iSourceCount = 0;
        static string sLastOutputFolder = "";
        static List<string> lResults = new List<string>();
        static List<string> lFailures = new List<string>();
        static string sLastFetchTrouble = "";
        static string sLastStreamedTrouble = "";
        static List<Moment> lLastGaps = null;
        static Dictionary<string, object> dLastSignature = null;

        static DateTime dtWaitingSince = DateTime.MinValue;
        static string sWaitingOn = "";
        static double nWaitingAt = 0.0;
        static Thread threadHeartbeat = null;
        static bool bHeartbeatStop = false;

        static void waitingOn(string sWhat)
        {
            sWaitingOn = sWhat;
            dtWaitingSince = sWhat == "" ? DateTime.MinValue : DateTime.Now;
        }

        // Says, every so often, that a long call is still running. Without it a
        // three minute wait on a slow machine is indistinguishable from a hang,
        // which is exactly how a tester read it.
        static void startHeartbeat()
        {
            if (threadHeartbeat != null) return;
            bHeartbeatStop = false;
            threadHeartbeat = new Thread(delegate()
            {
                while (!bHeartbeatStop)
                {
                    Thread.Sleep(1000);
                    if (dtWaitingSince == DateTime.MinValue) continue;
                    double nWaited = DateTime.Now.Subtract(dtWaitingSince).TotalSeconds;
                    if (nWaited < iDefaultHeartbeat) continue;
                    logMessage("Still " + sWaitingOn + ", " + ((int)nWaited).ToString() + " seconds so far.",
                               "INFO", "  still " + sWaitingOn + ", " + ((int)nWaited).ToString() + " seconds so far");
                    dtWaitingSince = DateTime.Now;
                }
            });
            threadHeartbeat.IsBackground = true;
            threadHeartbeat.Start();
        }

        static void stopHeartbeat()
        {
            bHeartbeatStop = true;
            threadHeartbeat = null;
        }

        // What kind of thing was last announced, so the kind and the time are
        // said once and then not repeated until the kind changes.
        // Messages of one kind are collected and shown TOGETHER, in a single
        // box: the kind and the position of the first of them as the title, the
        // messages themselves as the body, separated by blank lines. A screen
        // reader then reads a title that says what this is and where the film
        // has reached, followed by the whole group, instead of interrupting once
        // per sentence.
        //
        // A group ends when the kind changes, when it has been open long enough,
        // or when it has grown long enough to be worth hearing.
        static List<string> lPending = new List<string>();
        static string sPendingKind = "";
        static double nPendingAt = -1.0;
        static double nPendingTotal = 1.0;
        static DateTime dtPendingSince = DateTime.MinValue;
        static DateTime dtLastSpoken = DateTime.MinValue;

        // "Listening" and "Scanning" are what the work is called in the log.
        // What the listener needs is which part of the job it belongs to.
        static string announceKindFor(string sLabel)
        {
            if (sLabel == "Writing") return "Finalizing";
            return "Initializing";
        }

        // Minutes below the hour, hours and minutes above it, and nothing at
        // all rather than a zero.
        static string spokenTime(double nAt)
        {
            if (nAt < 60.0) return "";
            if (nAt < 3600.0) return ((int)Math.Round(nAt / 60.0)).ToString() + " min";
            int iHours = (int)(nAt / 3600.0);
            int iMinutes = (int)Math.Round((nAt - iHours * 3600.0) / 60.0);
            if (iMinutes >= 60)
            {
                iHours = iHours + 1;
                iMinutes = 0;
            }
            string sSaid = iHours.ToString() + (iHours == 1 ? " hour" : " hours");
            if (iMinutes > 0) sSaid = sSaid + " " + iMinutes.ToString() + " min";
            return sSaid;
        }

        static string spokenPosition(double nAt, double nTotal)
        {
            if (nAt < 0.0) return "";
            int iPercent = (int)(nAt * 100.0 / Math.Max(nTotal, 1.0));
            string sTime = spokenTime(nAt);
            if (sTime == "" && iPercent <= 0) return "";
            if (sTime == "") return iPercent.ToString() + "%";
            return sTime + ", " + iPercent.ToString() + "%";
        }

        static int pendingLength()
        {
            int iTotal = 0;
            foreach (string sOne in lPending) iTotal = iTotal + sOne.Length;
            return iTotal;
        }

        static void flushAnnouncements()
        {
            if (lPending.Count == 0) return;
            // A screen reader reads a dialog's title, and then reads the dialog
            // -- title and all -- when focus lands on it. Anything in the title
            // is therefore heard twice, which is why the title is now the
            // category alone and the position is stated once, at the top of the
            // text where it belongs to the group it introduces.
            string sWhere = spokenPosition(nPendingAt, nPendingTotal);
            string sTitle = sPendingKind;
            if (lPending.Count > 1 && lPending[0] == sWhere) lPending.RemoveAt(0);
            string sBody = string.Join(Environment.NewLine + Environment.NewLine, lPending.ToArray());
            if (sWhere != "" && sBody != sWhere) sBody = sWhere + Environment.NewLine + Environment.NewLine + sBody;
            // A hard ceiling, whatever else goes wrong. One announcement reached
            // thirty two thousand characters, which a screen reader will spend
            // several minutes reading and which made a working program look
            // stopped. Nothing said aloud can be longer than this, and the most
            // recent part is what is kept.
            if (sBody.Length > iDefaultSpokenLimit)
            {
                logMessage("That announcement was " + sBody.Length.ToString() + " characters and has been cut to " + iDefaultSpokenLimit.ToString() + ".", "INFO", "");
                sBody = sBody.Substring(sBody.Length - iDefaultSpokenLimit);
            }
            // Recorded so that a log shows exactly how many boxes were raised
            // and what was in each. A screen reader reads a box once; hearing
            // something twice means it was presented twice.
            logMessage("SAID [" + sTitle + "] " + sBody.Replace(Environment.NewLine, " / "), "INFO", "");
            // EMPTIED FIRST, and always. The live-region path used to return
            // before this, so the collected messages were never cleared: every
            // announcement repeated all of its predecessors and grew as it went.
            // Nothing below may return before the list is emptied.
            lPending.Clear();
            nPendingAt = -1.0;
            dtPendingSince = DateTime.Now;
            dtLastSpoken = DateTime.Now;
            if (!flag("boxes"))
            {
                sayLive(sTitle, sBody);
                return;
            }
            showTimedBox(sTitle, sBody);
        }

        static void announce(string sKind, double nAt, double nTotal, string sContent)
        {
            if (!bAnnouncing) return;
            string sText = sContent.Trim();
            if (sText == "") sText = spokenPosition(nAt, nTotal);
            // A change of kind closes whatever was being collected, because the
            // title names the kind, and is then spoken AT ONCE.
            //
            // Waiting would be wrong twice over. The first thing a person hears
            // after pressing OK should not be half a minute away -- one run was
            // stopped after sixty seconds having heard nothing at all, because
            // the first group had not yet closed. And a change of kind is the
            // most informative moment there is: it says the program has moved
            // from one part of the job to the next.
            bool bNewKind = sKind != sPendingKind;
            if (bNewKind)
            {
                flushAnnouncements();
                sPendingKind = sKind;
                nPendingAt = nAt;
                nPendingTotal = nTotal;
                dtPendingSince = DateTime.Now;
                lPending.Add(sText == "" ? "starting" : sText);
                flushAnnouncements();
                return;
            }
            if (sText == "") return;
            if (lPending.Count == 0)
            {
                // The time in the title is where this group STARTS. Later
                // messages join it however far the film has moved on.
                nPendingAt = nAt;
                nPendingTotal = nTotal;
                dtPendingSince = DateTime.Now;
            }
            lPending.Add(sText);
            // Spoken at once when nothing has been said for a while, so a quiet
            // stretch never leaves the listener wondering; collected into a
            // group when messages are arriving faster than they can be heard.
            if (DateTime.Now.Subtract(dtLastSpoken).TotalSeconds >= iDefaultSpokenReport) flushAnnouncements();
            else if (pendingLength() >= iDefaultGroupLength) flushAnnouncements();
        }

        // The status line in the dialog. Sighted users read it; screen readers
        // are told about it through Say.cs, which raises a UIA notification
        // against a live region, so JAWS, NVDA and Narrator all speak it.
        //
        // This replaces the timed message box. A box announced reliably but it
        // took the keyboard focus for as long as it was up, so the machine could
        // not be used for anything else while a film was being described -- and
        // a two hour film raised three hundred and forty of them.
        static Label oStatusLine = null;
        static string sLatestStatus = "";

        static void attachStatusLine(Form oForm)
        {
            if (oForm == null) return;
            if (oStatusLine != null) return;
            try
            {
                oStatusLine = new Label();
                oStatusLine.AutoSize = false;
                oStatusLine.Dock = DockStyle.Bottom;
                oStatusLine.Height = 44;
                oStatusLine.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
                oStatusLine.AccessibleName = "Status";
                oStatusLine.AccessibleRole = AccessibleRole.StaticText;
                oStatusLine.Text = "";
                oForm.Controls.Add(oStatusLine);
                // Coming back to HomerScribe should tell you where things stand,
                // not leave you waiting for the next announcement.
                oForm.Activated += delegate(object oSender, EventArgs oEvent)
                {
                    if (sLatestStatus == "") return;
                    try
                    {
                        oStatusLine.Text = sLatestStatus;
                        oStatusLine.Refresh();
                        Say.say(sLatestStatus);
                    }
                    catch (Exception)
                    {
                    }
                };
                IntPtr hForce = oStatusLine.Handle;
                Say.attach(oForm);
                logMessage("The status line is attached and speaking through the live region.", "INFO", "");
            }
            catch (Exception oError)
            {
                logMessage("The status line could not be attached: " + oError.Message, "ERROR");
            }
        }

        // Said aloud without taking the focus, and shown on the status line.
        // Is HomerScribe the window the person is working in? A live region
        // speaks whatever the focus is, which is helpful when you are watching
        // the program and an intrusion when you are not.
        static bool weAreInFront()
        {
            try
            {
                IntPtr hFront = GetForegroundWindow();
                if (hFront == IntPtr.Zero) return false;
                // Any window of ours counts, not only the dialog, and this holds
                // even when the dialog's handle cannot be reached.
                uint iOwner = 0;
                GetWindowThreadProcessId(hFront, out iOwner);
                if (iOwner != 0) return iOwner == (uint)Process.GetCurrentProcess().Id;
                Form oForm = ownerForm();
                if (oForm == null) return false;
                return hFront == oForm.Handle;
            }
            catch (Exception)
            {
                return false;
            }
        }

        static void sayLive(string sTitle, string sBody)
        {
            string sWhole = sTitle;
            if (sBody.Trim() != "") sWhole = sTitle + ". " + sBody.Replace(Environment.NewLine + Environment.NewLine, ". ").Replace(Environment.NewLine, " ");
            sWhole = Regex.Replace(sWhole, @"\s+", " ").Trim();
            // Held whether or not it is spoken, so the window can be looked at
            // afterwards and the last message read from it.
            sLatestStatus = sWhole;
            // The window is kept up to date whether or not anyone is looking at
            // it. Alt+Tab across to it at any moment and the title and the status
            // line already say where things stand, to be read with a screen
            // reader's own commands without waiting to be told.
            try
            {
                if (oStatusLine != null)
                {
                    oStatusLine.Text = sWhole;
                    oStatusLine.Refresh();
                }
                Form oForm = ownerForm();
                if (oForm != null) oForm.Text = "HomerScribe, " + sWhole;
            }
            catch (Exception)
            {
            }
            if (!weAreInFront())
            {
                // Written above but not spoken: working in another program should
                // not be interrupted. Coming back reads it out at once.
                logMessage("  (not spoken: HomerScribe is not the window in front)", "INFO", "");
                return;
            }
            try
            {
                Say.say(sWhole);
            }
            catch (Exception oError)
            {
                logMessage("The live region could not speak: " + oError.Message, "ERROR");
            }
        }

        static void showTimedBox(string sCaption, string sBody)
        {
            if (!bAnnouncing) return;
            // Long enough for the words in it to be read out. A group of five
            // descriptions cannot be spoken in the two seconds one line needs.
            int iShowFor = iDefaultBoxMs + sBody.Length * 25;
            if (iShowFor > iDefaultBoxMaxMs) iShowFor = iDefaultBoxMaxMs;
            IntPtr hOwner = IntPtr.Zero;
            Form oOwner = ownerForm();
            if (oOwner != null)
            {
                try
                {
                    hOwner = oOwner.Handle;
                }
                catch (Exception)
                {
                }
            }
            if (hOwner != IntPtr.Zero)
            {
                // Owned by the dialog, on the dialog's own thread, which is this
                // one. It closes itself after its time, so nothing has to wait
                // on another thread and nothing can deadlock.
                MessageBoxTimeoutW(hOwner, sBody, sCaption, iMbOk, 0, (uint)iShowFor);
                return;
            }
            Thread threadBox = new Thread(delegate()
            {
                MessageBoxTimeoutW(IntPtr.Zero, sBody, sCaption, iMbOk | iMbSetForeground | iMbTopmost, 0, (uint)iShowFor);
            });
            threadBox.IsBackground = true;
            threadBox.Start();
            IntPtr hBox = IntPtr.Zero;
            for (int iTry = 0; iTry < 30 && hBox == IntPtr.Zero; iTry = iTry + 1)
            {
                Thread.Sleep(20);
                hBox = FindWindowW("#32770", sCaption);
            }
            if (hBox == IntPtr.Zero) logMessage("The announcement window was not found in time: " + sCaption, "INFO", "");
            else if (!forceForeground(hBox)) logMessage("The announcement window could not take focus: " + sCaption, "INFO", "");
            threadBox.Join();
        }

        // Activates the announcement window using the classic attach recipe, so
        // a screen reader treats it as genuinely foreground and speaks it.
        static bool forceForeground(IntPtr hWindow)
        {
            MSG oMessage;
            if (SetForegroundWindow(hWindow) && GetForegroundWindow() == hWindow) return true;
            PeekMessageW(out oMessage, IntPtr.Zero, 0, 0, 0);
            IntPtr hForeground = GetForegroundWindow();
            uint iOurThread = GetCurrentThreadId();
            uint iForeThread = hForeground == IntPtr.Zero ? 0 : GetWindowThreadProcessId(hForeground, IntPtr.Zero);
            uint iBoxThread = GetWindowThreadProcessId(hWindow, IntPtr.Zero);
            if (iForeThread != 0 && iForeThread != iOurThread) AttachThreadInput(iOurThread, iForeThread, true);
            if (iBoxThread != 0 && iBoxThread != iOurThread) AttachThreadInput(iOurThread, iBoxThread, true);
            SetForegroundWindow(hWindow);
            BringWindowToTop(hWindow);
            SetFocus(hWindow);
            if (iForeThread != 0 && iForeThread != iOurThread) AttachThreadInput(iOurThread, iForeThread, false);
            if (iBoxThread != 0 && iBoxThread != iOurThread) AttachThreadInput(iOurThread, iBoxThread, false);
            return GetForegroundWindow() == hWindow;
        }

        static string formatClock(double nSeconds)
        {
            int iWhole = (int)nSeconds;
            int iHours = iWhole / 3600;
            int iMinutes = (iWhole % 3600) / 60;
            int iRest = iWhole % 60;
            if (iHours > 0) return iHours.ToString() + ":" + iMinutes.ToString("00") + ":" + iRest.ToString("00");
            return iMinutes.ToString() + ":" + iRest.ToString("00");
        }

        // ---------- running other programs ----------

        static string findTool(string sName)
        {
            string sBeside = Path.Combine(exeFolder(), sName + ".exe");
            if (File.Exists(sBeside)) return sBeside;
            // Whisper and anything else installed for this user rather than for
            // the machine, since Program Files is not writable at run time.
            string sMine = Path.Combine(appDataFolder(), "whisper", sName + ".exe");
            if (File.Exists(sMine)) return sMine;
            string sExtra = text("ffmpeg-dir");
            if (sExtra != "")
            {
                string sGiven = Path.Combine(sExtra, sName + ".exe");
                if (File.Exists(sGiven)) return sGiven;
            }
            string sPath = Environment.GetEnvironmentVariable("PATH");
            if (sPath == null) return "";
            foreach (string sFolder in sPath.Split(Path.PathSeparator))
            {
                if (sFolder.Trim() == "") continue;
                string sTry = "";
                try
                {
                    sTry = Path.Combine(sFolder.Trim(), sName + ".exe");
                }
                catch (Exception)
                {
                    continue;
                }
                if (File.Exists(sTry)) return sTry;
            }
            return "";
        }

        static int runCommand(string sProgram, string sArguments, out string sOut, out string sErr)
        {
            sOut = "";
            sErr = "";
            logMessage("Command: " + sProgram + " " + sArguments, "CMD");
            DateTime dtBegan = DateTime.Now;
            Process oProcess = new Process();
            oProcess.StartInfo.FileName = sProgram;
            oProcess.StartInfo.Arguments = sArguments;
            oProcess.StartInfo.UseShellExecute = false;
            oProcess.StartInfo.RedirectStandardOutput = true;
            oProcess.StartInfo.RedirectStandardError = true;
            oProcess.StartInfo.CreateNoWindow = true;
            try
            {
                oProcess.Start();
            }
            catch (Exception oError)
            {
                logMessage("Could not start " + sProgram + ": " + oError.Message, "ERROR");
                return -1;
            }
            sOut = oProcess.StandardOutput.ReadToEnd();
            sErr = oProcess.StandardError.ReadToEnd();
            oProcess.WaitForExit();
            double nTook = DateTime.Now.Subtract(dtBegan).TotalSeconds;
            logMessage("Exit code " + oProcess.ExitCode.ToString() + " after " + num(nTook) + " seconds", "CMD");
            if (oProcess.ExitCode != 0 && sErr.Trim() != "") logMessage("Error output: " + tail(sErr, 1500), "ERROR", "");
            return oProcess.ExitCode;
        }

        static string tail(string sText, int iKeep)
        {
            if (sText.Length <= iKeep) return sText;
            return sText.Substring(sText.Length - iKeep);
        }

        // A long ffmpeg pass, reporting where it has reached so the screen is
        // never silent for minutes at a time.
        static int iLastScanExit = 0;

        static string runScan(string sProgram, string sArguments, double nDuration, string sLabel)
        {
            string sFull = "-progress pipe:1 " + sArguments;
            logMessage("Command: " + sProgram + " " + sFull, "CMD");
            StringBuilder oErr = new StringBuilder();
            DateTime dtBegan = DateTime.Now;
            DateTime dtLast = DateTime.Now;
            DateTime dtSaidScan = DateTime.MinValue;
            Process oProcess = new Process();
            oProcess.StartInfo.FileName = sProgram;
            oProcess.StartInfo.Arguments = sFull;
            oProcess.StartInfo.UseShellExecute = false;
            oProcess.StartInfo.RedirectStandardOutput = true;
            oProcess.StartInfo.RedirectStandardError = true;
            oProcess.StartInfo.CreateNoWindow = true;
            oProcess.ErrorDataReceived += delegate(object oSender, DataReceivedEventArgs oEvent)
            {
                if (oEvent.Data != null) oErr.AppendLine(oEvent.Data);
            };
            try
            {
                oProcess.Start();
            }
            catch (Exception oError)
            {
                logMessage("Could not start " + sProgram + ": " + oError.Message, "ERROR");
                return "";
            }
            oProcess.BeginErrorReadLine();
            string sLine = oProcess.StandardOutput.ReadLine();
            while (sLine != null)
            {
                Match oMatch = Regex.Match(sLine.Trim(), @"^out_time=(\d+):(\d\d):(\d\d)");
                if (oMatch.Success)
                {
                    double nAt = double.Parse(oMatch.Groups[1].Value, CultureInfo.InvariantCulture) * 3600.0
                               + double.Parse(oMatch.Groups[2].Value, CultureInfo.InvariantCulture) * 60.0
                               + double.Parse(oMatch.Groups[3].Value, CultureInfo.InvariantCulture);
                    if (DateTime.Now.Subtract(dtLast).TotalSeconds >= iDefaultScanReport)
                    {
                        dtLast = DateTime.Now;
                        double nShare = nAt / Math.Max(nDuration, 1.0);
                        double nLeft = 0.0;
                        if (nShare > 0.01) nLeft = DateTime.Now.Subtract(dtBegan).TotalSeconds * (1.0 - nShare) / nShare / 60.0;
                        string sLeft = "about " + ((int)Math.Round(nLeft)).ToString() + " minutes left";
                        // The same shape as a description line: the position in
                        // the film, and one word for what is happening.
                        dialogSays(announceKindFor(sLabel).ToLower() + ", " + spokenPosition(nAt, nDuration));
                        pumpDialog();
                        // The same shape as everything else that is spoken: the
                        // kind once, then just how far in it has reached.
                        if (sLabel != "" && DateTime.Now.Subtract(dtSaidScan).TotalSeconds >= iDefaultSpokenReport)
                        {
                            dtSaidScan = DateTime.Now;
                            announce(announceKindFor(sLabel), nAt, nDuration, "");
                        }
                        string sScreen = sLabel + "  " + formatClock(nAt) + " of " + formatClock(nDuration);
                        if (sLabel == "") sScreen = "";
                        logMessage((sLabel == "" ? "Background" : sLabel) + ": reached " + formatClock(nAt) + " of " + formatClock(nDuration) + ", " + ((int)(nShare * 100)).ToString() + " percent", "INFO", sScreen);
                    }
                }
                sLine = oProcess.StandardOutput.ReadLine();
            }
            oProcess.WaitForExit();
            iLastScanExit = oProcess.ExitCode;
            logMessage("Exit code " + oProcess.ExitCode.ToString() + " after " + num(DateTime.Now.Subtract(dtBegan).TotalSeconds) + " seconds", "CMD");
            return oErr.ToString();
        }

        static string quotedIfSpaced(string sItem)
        {
            if (sItem.IndexOf(' ') < 0) return sItem;
            return "\"" + sItem + "\"";
        }

        // Work out what the person meant by a box full of text.
        //
        // The naive rule -- split on spaces unless quoted -- turned
        // "c:\video\The Africans - Program 7.mp4" into sixteen sources, none of
        // which existed. Quoting is a shell convention, and there is no shell
        // here: a dialog box is not a command line, and nobody should have to
        // quote a filename they picked out of a folder.
        //
        // So the file system is asked instead of guessed at, in this order:
        // each line separately; a whole line that already names something; and
        // only then a split, rejoined greedily so that an unquoted path with
        // spaces in it is still found.
        static List<string> splitPaths(string sList)
        {
            List<string> lItems = new List<string>();
            if (sList == null) return lItems;
            foreach (string sLine in sList.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n'))
            {
                if (sLine.Trim() == "") continue;
                foreach (string sOne in splitOneLine(sLine.Trim())) lItems.Add(sOne);
            }
            return lItems;
        }

        static bool namesSomething(string sItem)
        {
            if (sItem == "") return false;
            if (sItem.StartsWith("http://") || sItem.StartsWith("https://")) return true;
            try
            {
                if (File.Exists(sItem) || Directory.Exists(sItem)) return true;
                if (sItem.IndexOf('*') >= 0 || sItem.IndexOf('?') >= 0)
                {
                    string sFolder = Path.GetDirectoryName(sItem);
                    if (sFolder == "" || Directory.Exists(sFolder)) return true;
                }
            }
            catch (Exception)
            {
            }
            return false;
        }

        static List<string> splitOneLine(string sLine)
        {
            List<string> lItems = new List<string>();
            // The whole line is already a path or an address. This is the
            // ordinary case, and no amount of splitting improves on it.
            string sBare = sLine.Trim().Trim('"');
            if (namesSomething(sBare))
            {
                lItems.Add(sBare);
                return lItems;
            }
            // Quoted items are taken as written, since quoting is unambiguous.
            if (sLine.IndexOf('"') >= 0)
            {
                foreach (Match oMatch in Regex.Matches(sLine, "\"([^\"]*)\"|(\\S+)"))
                {
                    string sItem = oMatch.Groups[1].Success ? oMatch.Groups[1].Value : oMatch.Groups[2].Value;
                    if (sItem.Trim() != "") lItems.Add(sItem.Trim());
                }
                return lItems;
            }
            // Several unquoted things on one line. Take the longest run of words
            // that names something real, then carry on from there. A word that
            // names nothing is kept as it stands, so the error message can name
            // what was actually typed.
            string[] asWords = sLine.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            int iAt = 0;
            while (iAt < asWords.Length)
            {
                int iTook = 0;
                for (int iEnd = asWords.Length - 1; iEnd >= iAt; iEnd--)
                {
                    string sTry = string.Join(" ", asWords, iAt, iEnd - iAt + 1);
                    if (!namesSomething(sTry)) continue;
                    lItems.Add(sTry);
                    iTook = iEnd - iAt + 1;
                    break;
                }
                if (iTook == 0)
                {
                    lItems.Add(asWords[iAt]);
                    iTook = 1;
                }
                iAt = iAt + iTook;
            }
            return lItems;
        }

        static string quoted(string sPath)
        {
            return "\"" + sPath + "\"";
        }

        // ---------- looking at the film ----------

        static double parseTime(string sValue)
        {
            double nSeconds = 0.0;
            if (sValue.Trim() == "") return 0.0;
            foreach (string sPart in sValue.Trim().Split(':'))
            {
                double nPart = 0.0;
                double.TryParse(sPart, NumberStyles.Any, CultureInfo.InvariantCulture, out nPart);
                nSeconds = nSeconds * 60.0 + nPart;
            }
            return nSeconds;
        }

        static double probeDuration(string sFfmpeg, string sPath)
        {
            string sOut = "";
            string sErr = "";
            runCommand(sFfmpeg, "-hide_banner -i " + quoted(sPath), out sOut, out sErr);
            Match oMatch = Regex.Match(sErr, @"Duration:\s*(\d+):(\d\d):(\d\d(?:\.\d+)?)");
            if (!oMatch.Success)
            {
                logMessage("No duration could be read from " + sPath, "ERROR");
                return 0.0;
            }
            return double.Parse(oMatch.Groups[1].Value, CultureInfo.InvariantCulture) * 3600.0
                 + double.Parse(oMatch.Groups[2].Value, CultureInfo.InvariantCulture) * 60.0
                 + double.Parse(oMatch.Groups[3].Value, CultureInfo.InvariantCulture);
        }

        static int audioChannels(string sFfmpeg, string sPath)
        {
            string sOut = "";
            string sErr = "";
            runCommand(sFfmpeg, "-hide_banner -i " + quoted(sPath), out sOut, out sErr);
            Match oMatch = Regex.Match(sErr, @"Audio:.*?,\s*\d+\s*Hz,\s*([^,]+),");
            if (!oMatch.Success) return 0;
            string sLayout = oMatch.Groups[1].Value.Trim().ToLower();
            int iChannels = 0;
            if (sLayout == "mono") iChannels = 1;
            if (sLayout == "stereo") iChannels = 2;
            if (sLayout == "5.0") iChannels = 5;
            if (sLayout.StartsWith("5.1")) iChannels = 6;
            if (sLayout.StartsWith("7.1")) iChannels = 8;
            logMessage("Audio layout: " + sLayout + " (" + iChannels.ToString() + " channels)", "INFO", "");
            return iChannels;
        }

        static List<double[]> detectSilences(string sFfmpeg, string sPath, double nNoiseFloor, double nSilenceLength, bool bCentre, double nDuration)
        {
            List<double[]> lSilences = new List<double[]>();
            string sFilter = "silencedetect=noise=" + num(nNoiseFloor) + "dB:d=" + num(nSilenceLength);
            if (bCentre) sFilter = "pan=mono|c0=FC," + sFilter;
            logMessage("Scanning the sound track at " + num(nNoiseFloor) + " dB to find where descriptions can be spoken.",
                       "INFO", "Initializing.");
            string sErr = runScan(sFfmpeg, "-hide_banner -i " + quoted(sPath) + " -af " + quoted(sFilter) + " -f null -", nDuration, "Scanning");
            double nStart = -1.0;
            foreach (string sLine in sErr.Split('\n'))
            {
                Match oStart = Regex.Match(sLine, @"silence_start:\s*(-?[0-9.]+)");
                Match oEnd = Regex.Match(sLine, @"silence_end:\s*(-?[0-9.]+)");
                if (oStart.Success) nStart = double.Parse(oStart.Groups[1].Value, CultureInfo.InvariantCulture);
                if (oEnd.Success && nStart >= 0.0)
                {
                    double nEnd = double.Parse(oEnd.Groups[1].Value, CultureInfo.InvariantCulture);
                    lSilences.Add(new double[] { nStart, nEnd });
                }
                if (oEnd.Success) nStart = -1.0;
            }
            logMessage("Found " + lSilences.Count.ToString() + " silences at " + num(nNoiseFloor) + " dB", "INFO", "");
            return lSilences;
        }

        static List<Moment> chooseGaps(List<double[]> lSilences, double nMinGap, double nSpacing)
        {
            List<Moment> lGaps = new List<Moment>();
            double nLastEnd = -9999.0;
            foreach (double[] anSilence in lSilences)
            {
                double nStart = anSilence[0] + nDefaultLead;
                double nEnd = anSilence[1] - nDefaultLead;
                double nLength = nEnd - nStart;
                if (nLength < nMinGap) continue;
                if (nStart - nLastEnd < nSpacing) continue;
                Moment oMoment = new Moment();
                oMoment.nStart = Math.Round(nStart, 3);
                oMoment.nLength = Math.Round(nLength, 3);
                lGaps.Add(oMoment);
                nLastEnd = nStart + nLength;
            }
            logMessage("Kept " + lGaps.Count.ToString() + " natural gaps", "INFO", "");
            return lGaps;
        }

        static List<Moment> fillGaps(List<Moment> lGaps, double nDuration, double nEvery, double nForcedLength)
        {
            if (nEvery <= 0.0) return lGaps;
            List<Moment> lResult = new List<Moment>();
            double nLast = 0.0 - nEvery;
            int iForced = 0;
            foreach (Moment oGap in lGaps)
            {
                while (oGap.nStart - nLast > nEvery)
                {
                    double nNew = nLast + nEvery;
                    if (nNew + nForcedLength > oGap.nStart) break;
                    Moment oPlaced = new Moment();
                    oPlaced.nStart = Math.Round(nNew, 3);
                    oPlaced.nLength = nForcedLength;
                    oPlaced.bForced = true;
                    lResult.Add(oPlaced);
                    iForced = iForced + 1;
                    nLast = nNew;
                }
                lResult.Add(oGap);
                nLast = oGap.nStart;
            }
            while (nDuration - nLast > nEvery)
            {
                double nNew = nLast + nEvery;
                if (nNew + nForcedLength > nDuration) break;
                Moment oPlaced = new Moment();
                oPlaced.nStart = Math.Round(nNew, 3);
                oPlaced.nLength = nForcedLength;
                oPlaced.bForced = true;
                lResult.Add(oPlaced);
                iForced = iForced + 1;
                nLast = nNew;
            }
            logMessage("Placed " + iForced.ToString() + " extra descriptions where no quiet moment was found", "INFO", "");
            return lResult;
        }

        // ---------- hearing the film ----------
        //
        // Silence detection asks "is there sound?". The question that decides
        // where a description belongs is "is anyone talking?", and on a scored
        // film those are entirely different questions: one measured run found
        // 113 usable gaps by silence and had to invent 588 more on a timer.
        //
        // Whisper answers the real question, and hands over the dialogue as
        // well, so a description need not repeat what was just said.

        static string whisperProgram()
        {
            string sFound = findTool("whisper-cli");
            if (sFound == "") sFound = findTool("main");
            return sFound;
        }

        static string whisperModelPath()
        {
            string sName = "ggml-" + text("whisper-model") + ".bin";
            string sMine = Path.Combine(Path.Combine(appDataFolder(), "whisper"), sName);
            if (File.Exists(sMine)) return sMine;
            string sBeside = Path.Combine(exeFolder(), sName);
            if (File.Exists(sBeside)) return sBeside;
            return "";
        }

        // Whisper wants sixteen kilohertz mono. Producing that is a fraction of
        // the cost of transcribing it.
        static string speechWave(string sFfmpeg, string sInput, string sWorkDir)
        {
            string sWave = Path.Combine(sWorkDir, "speech.wav");
            if (File.Exists(sWave) && new FileInfo(sWave).Length > 1000) return sWave;
            string sOut = "";
            string sErr = "";
            int iCode = runCommand(sFfmpeg, "-hide_banner -loglevel error -y -i " + quoted(sInput)
                                          + " -vn -ac 1 -ar 16000 -c:a pcm_s16le " + quoted(sWave), out sOut, out sErr);
            if (iCode != 0 || !File.Exists(sWave)) return "";
            return sWave;
        }

        static List<Speech> readTranscript(string sPath)
        {
            List<Speech> lSpeech = new List<Speech>();
            int iLooped = 0;
            try
            {
                JavaScriptSerializer oSerializer = new JavaScriptSerializer();
                oSerializer.MaxJsonLength = int.MaxValue;
                Dictionary<string, object> dData = oSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(sPath));
                if (!dData.ContainsKey("transcription")) return lSpeech;
                foreach (object oItem in toList(dData["transcription"]))
                {
                    Dictionary<string, object> dItem = toMap(oItem);
                    if (!dItem.ContainsKey("offsets")) continue;
                    Dictionary<string, object> dOffsets = toMap(dItem["offsets"]);
                    Speech oSpeech = new Speech();
                    oSpeech.nStart = Convert.ToDouble(dOffsets["from"]) / 1000.0;
                    oSpeech.nEnd = Convert.ToDouble(dOffsets["to"]) / 1000.0;
                    if (dItem.ContainsKey("text")) oSpeech.sText = Convert.ToString(dItem["text"]).Trim();
                    // "[Music]", "(applause)" and the like are not speech. They
                    // mark exactly the stretches where a description belongs.
                    if (Regex.IsMatch(oSpeech.sText, @"^[\[\(][^\]\)]*[\]\)]$")) continue;
                    if (oSpeech.nEnd <= oSpeech.nStart) continue;
                    // The same sentence again immediately after itself is the
                    // model stuck in a loop, not someone repeating themselves:
                    // a person saying a line twice is one stretch, not two
                    // identical ones back to back.
                    if (lSpeech.Count > 0 && string.Compare(lSpeech[lSpeech.Count - 1].sText, oSpeech.sText, true) == 0)
                    {
                        iLooped = iLooped + 1;
                        continue;
                    }
                    lSpeech.Add(oSpeech);
                }
            }
            catch (Exception oError)
            {
                logMessage("The transcript could not be read: " + oError.Message, "ERROR");
            }
            if (iLooped > 0) logMessage("Dropped " + iLooped.ToString() + " stretches that merely repeated the one before, which is Whisper looping on music or silence rather than anyone speaking.", "INFO", "");
            return lSpeech;
        }

        // Transcribing a long film is minutes of work, so the result is kept and
        // a resumed run never pays for it twice.
        static List<Speech> transcribe(string sFfmpeg, string sInput, string sWorkDir, double nDuration)
        {
            List<Speech> lSpeech = new List<Speech>();
            string sJson = Path.Combine(sWorkDir, "transcript.json");
            if (File.Exists(sJson))
            {
                lSpeech = readTranscript(sJson);
                if (lSpeech.Count > 0)
                {
                    logMessage("Reusing the transcript from an earlier run: " + lSpeech.Count.ToString() + " spoken stretches.",
                               "INFO", "Reusing the transcript from the earlier run, so there is nothing to listen to again.");
                    return lSpeech;
                }
            }
            string sWhisper = whisperProgram();
            string sModel = whisperModelPath();
            if (sWhisper == "" || sModel == "")
            {
                logMessage("Whisper was not found, so speech cannot be detected. Falling back to listening for silence.", "INFO",
                           "Whisper is not installed, so descriptions are placed by silence instead of speech. Run installWhisper.cmd to improve that.");
                if (sWhisper == "") logMessage("  whisper-cli.exe was not found beside the program, under application data, or on the PATH.", "INFO", "");
                if (sModel == "") logMessage("  ggml-" + text("whisper-model") + ".bin was not found.", "INFO", "");
                return lSpeech;
            }
            logMessage("Whisper: " + sWhisper, "INFO", "");
            logMessage("Whisper model: " + sModel + ", " + num(new FileInfo(sModel).Length / 1048576.0) + " MB", "INFO", "");
            string sWave = speechWave(sFfmpeg, sInput, sWorkDir);
            if (sWave == "")
            {
                logMessage("The sound could not be extracted for transcription.", "ERROR");
                return lSpeech;
            }
            nWholeLength = nDuration;
            logMessage("Listening to the film to find where the speech is.", "INFO", "Initializing.");
            DateTime dtBegan = DateTime.Now;
            waitingOn("listening to the film");
            string sBase = Path.Combine(sWorkDir, "transcript");
            runStreamed(sWhisper, "-m " + quoted(sModel) + " -f " + quoted(sWave) + " -l auto -oj -of " + quoted(sBase), "Listening");
            waitingOn("");
            double nTook = DateTime.Now.Subtract(dtBegan).TotalSeconds;
            if (!File.Exists(sJson))
            {
                logMessage("Whisper produced no transcript.", "ERROR");
                return lSpeech;
            }
            lSpeech = readTranscript(sJson);
            double nSpoken = 0.0;
            foreach (Speech oSpeech in lSpeech) nSpoken = nSpoken + (oSpeech.nEnd - oSpeech.nStart);
            logMessage("Transcribed in " + num(nTook) + " seconds, " + num(nTook / Math.Max(nDuration, 1.0) * 100.0) + " percent of the film's length.", "INFO", "");
            logMessage("Speech: " + lSpeech.Count.ToString() + " stretches, " + formatClock(nSpoken) + " of " + formatClock(nDuration)
                       + ", " + num(nSpoken / Math.Max(nDuration, 1.0) * 100.0) + " percent of the film.",
                       "INFO", "Heard " + lSpeech.Count.ToString() + " stretches of speech, " + ((int)(nSpoken / Math.Max(nDuration, 1.0) * 100.0)).ToString() + " percent of the film.");
            try
            {
                File.Delete(sWave);
            }
            catch (Exception)
            {
            }
            return lSpeech;
        }

        // The quiet between the talking. This is what silence detection was
        // always trying to approximate.
        static List<Moment> gapsFromSpeech(List<Speech> lSpeech, double nDuration)
        {
            List<Moment> lGaps = new List<Moment>();
            double nMinGap = number("min-gap");
            double nSpacing = number("spacing");
            double nLastEnd = -9999.0;
            double nAt = 0.0;
            int iTooShort = 0;
            int iTooClose = 0;
            List<Speech> lSorted = new List<Speech>(lSpeech);
            lSorted.Sort(delegate(Speech oOne, Speech oTwo) { return oOne.nStart.CompareTo(oTwo.nStart); });
            foreach (Speech oSpeech in lSorted)
            {
                double nStart = nAt + nDefaultLead;
                double nEnd = oSpeech.nStart - nDefaultLead;
                if (oSpeech.nEnd > nAt) nAt = oSpeech.nEnd;
                double nLength = nEnd - nStart;
                if (nLength < nMinGap)
                {
                    iTooShort = iTooShort + 1;
                    continue;
                }
                if (nStart - nLastEnd < nSpacing)
                {
                    iTooClose = iTooClose + 1;
                    continue;
                }
                Moment oMoment = new Moment();
                oMoment.nStart = Math.Round(nStart, 3);
                oMoment.nLength = Math.Round(nLength, 3);
                lGaps.Add(oMoment);
                nLastEnd = nStart + nLength;
            }
            // And the quiet after the last word.
            if (nDuration - nAt - nDefaultLead * 2.0 >= nMinGap && nAt + nDefaultLead - nLastEnd >= nSpacing)
            {
                Moment oLast = new Moment();
                oLast.nStart = Math.Round(nAt + nDefaultLead, 3);
                oLast.nLength = Math.Round(nDuration - nAt - nDefaultLead * 2.0, 3);
                lGaps.Add(oLast);
            }
            logMessage("Speech-free intervals usable as gaps: " + lGaps.Count.ToString()
                       + ". Rejected " + iTooShort.ToString() + " as shorter than " + num(nMinGap) + "s and "
                       + iTooClose.ToString() + " as closer than " + num(nSpacing) + "s to the one before.", "INFO", "");
            return lGaps;
        }

        // The quietest instant in a stretch of film, judged from the transcript:
        // the middle of the longest interval between two spoken stretches. Used
        // when a description has to be placed where there is no proper gap.
        static double quietestWithin(List<Speech> lSpeech, double nFrom, double nTo, out double nRoom)
        {
            nRoom = 0.0;
            double nBest = (nFrom + nTo) / 2.0;
            double nAt = nFrom;
            foreach (Speech oSpeech in lSpeech)
            {
                if (oSpeech.nEnd <= nFrom) continue;
                if (oSpeech.nStart >= nTo) break;
                double nGap = oSpeech.nStart - nAt;
                if (nGap > nRoom && oSpeech.nStart > nFrom)
                {
                    nRoom = nGap;
                    nBest = nAt + nGap / 2.0;
                }
                if (oSpeech.nEnd > nAt) nAt = oSpeech.nEnd;
            }
            if (nTo - nAt > nRoom)
            {
                nRoom = nTo - nAt;
                nBest = nAt + nRoom / 2.0;
            }
            return nBest;
        }

        // Fill the long stretches, putting each extra description at the
        // quietest instant rather than on a clock.
        static List<Moment> fillFromQuiet(List<Moment> lGaps, List<Speech> lSpeech, double nDuration, double nEvery, double nForcedLength)
        {
            if (nEvery <= 0.0) return lGaps;
            List<Moment> lResult = new List<Moment>();
            List<double> lEdges = new List<double>();
            foreach (Moment oGap in lGaps) lEdges.Add(oGap.nStart);
            lEdges.Add(nDuration);
            double nPrevious = 0.0;
            int iPlaced = 0;
            double nRoomTotal = 0.0;
            int iAt = 0;
            foreach (double nEdge in lEdges)
            {
                while (nEdge - nPrevious > nEvery)
                {
                    double nRoom = 0.0;
                    double nWindowEnd = Math.Min(nPrevious + nEvery * 1.5, nEdge);
                    double nWhere = quietestWithin(lSpeech, nPrevious + nEvery * 0.5, nWindowEnd, out nRoom);
                    if (nWhere + nForcedLength > nEdge) break;
                    Moment oPlaced = new Moment();
                    oPlaced.nStart = Math.Round(nWhere, 3);
                    oPlaced.nLength = nForcedLength;
                    oPlaced.bForced = true;
                    lResult.Add(oPlaced);
                    iPlaced = iPlaced + 1;
                    nRoomTotal = nRoomTotal + nRoom;
                    nPrevious = nWhere;
                }
                if (iAt < lGaps.Count) lResult.Add(lGaps[iAt]);
                nPrevious = nEdge;
                iAt = iAt + 1;
            }
            logMessage("Placed " + iPlaced.ToString() + " extra descriptions at the quietest point available"
                       + (iPlaced > 0 ? ", with " + num(nRoomTotal / iPlaced) + "s of quiet on average" : "") + ".", "INFO", "");
            return lResult;
        }

        // What was said in the moments before this one, so a description does
        // not tell the listener something they have just heard.
        static string spokenBefore(List<Speech> lSpeech, double nStart)
        {
            double nWindow = number("dialogue-window");
            if (nWindow <= 0.0 || lSpeech == null) return "";
            StringBuilder oSaid = new StringBuilder();
            foreach (Speech oSpeech in lSpeech)
            {
                if (oSpeech.nEnd > nStart) continue;
                if (oSpeech.nEnd < nStart - nWindow) continue;
                if (oSpeech.sText == "") continue;
                oSaid.Append(oSpeech.sText + " ");
            }
            return oSaid.ToString().Trim();
        }

        static bool overlapsSpeech(List<Speech> lSpeech, double nStart, double nEnd)
        {
            if (lSpeech == null) return false;
            foreach (Speech oSpeech in lSpeech)
            {
                if (oSpeech.nStart < nEnd && oSpeech.nEnd > nStart) return true;
            }
            return false;
        }

        static List<Moment> findGaps(string sFfmpeg, string sPath, double nDuration)
        {
            dLastSignature = gapSignature(nDuration);
            if (lCachedGaps != null && sameSignature(dLastSignature, dCachedSignature))
            {
                lLastGaps = lCachedGaps;
                int iReal = 0;
                foreach (Moment oOne in lLastGaps)
                {
                    if (!oOne.bForced) iReal = iReal + 1;
                }
                logMessage("PLACEMENT: " + iReal.ToString() + " real gaps, " + (lLastGaps.Count - iReal).ToString()
                           + " placed on the timer, " + num(iReal * 100.0 / Math.Max(lLastGaps.Count, 1)) + " percent real (reused).", "INFO", "");
                logMessage("Reusing the " + lLastGaps.Count.ToString() + " moments worked out by the earlier run. The sound track is not read again.",
                           "INFO", "Reusing the " + lLastGaps.Count.ToString() + " moments from the earlier run, so there is no scan to wait for.");
                return lLastGaps;
            }
            if (lCachedGaps != null) logMessage("The settings have changed since the earlier run, so the moments are worked out again.", "INFO", "");

            List<Moment> lGaps = null;
            int iFromSpeech = 0;
            if (flag("speech"))
            {
                if (!bSpeechReady) lFilmSpeech = transcribe(sFfmpeg, sPath, sSpeechWorkDir, nDuration);
                if (lFilmSpeech.Count > 0)
                {
                    lGaps = gapsFromSpeech(lFilmSpeech, nDuration);
                    iFromSpeech = lGaps.Count;
                    double nTalk = 0.0;
                    foreach (Speech oSpeech in lFilmSpeech) nTalk = nTalk + (oSpeech.nEnd - oSpeech.nStart);
                    double nShare = nTalk / Math.Max(nDuration, 1.0) * 100.0;
                    if (nShare >= nDefaultTalkative)
                    {
                        string sCrowded = "Somebody is talking for " + ((int)nShare).ToString() + " percent of this film, so there is very little room for description. "
                                        + "Descriptions will fall across the narration whatever is done; they are placed at the quietest points that exist. "
                                        + "Interrupting less often helps more than anything else: with --every 45 a description "
                                        + "falls on speech about half as often as at the default of 14, and with --every 90 less "
                                        + "than a third as often. --detail brief shortens each one, which helps again.";
                        logMessage(sCrowded, "HINT");
                        logMessage("", "INFO", sCrowded);
                    }
                }
            }
            if (lGaps == null)
            {
                // No transcript, so fall back to the old question: where is it
                // quiet? On a scored film this finds very little, which is why
                // the fixed interval below has to do so much of the work.
                bool bCentre = false;
                if (text("dialogue-channel") != "off") bCentre = audioChannels(sFfmpeg, sPath) >= 6;
                List<double[]> lSilences = detectSilences(sFfmpeg, sPath, number("noise-floor"), number("silence-length"), bCentre, nDuration);
                lGaps = chooseGaps(lSilences, number("min-gap"), number("spacing"));
            }
            int iNatural = lGaps.Count;
            if (iFromSpeech > 0) lGaps = fillFromQuiet(lGaps, lFilmSpeech, nDuration, number("every"), number("forced-length"));
            else lGaps = fillGaps(lGaps, nDuration, number("every"), number("forced-length"));
            int iForced = lGaps.Count - iNatural;

            // The measurement that says whether hearing the film was worth it.
            logMessage("PLACEMENT: " + iNatural.ToString() + " real gaps ("
                       + (iFromSpeech > 0 ? "found by listening for speech" : "found by listening for silence")
                       + "), " + iForced.ToString() + " placed on the timer, "
                       + num(iNatural * 100.0 / Math.Max(lGaps.Count, 1)) + " percent real.", "INFO",
                       iNatural.ToString() + " real gaps and " + iForced.ToString() + " placed on the timer.");
            lLastGaps = lGaps;
            logMessage("Describing " + lGaps.Count.ToString() + " moments across " + formatClock(nDuration),
                       "INFO", "Describing " + lGaps.Count.ToString() + " moments across " + formatClock(nDuration) + ". Each description follows as it is made.");
            return lGaps;
        }

        // One ffmpeg call takes several frames spanning the moment and tiles
        // them in time order, which gives a still-image model a sense of motion.
        static bool buildMontage(string sFfmpeg, string sPath, double nMiddle, double nSpan, string sImagePath)
        {
            int iFrames = integer("frames");
            double nCrop = number("crop-bottom");
            double nBegin = Math.Max(nMiddle - nSpan / 2.0, 0.0);
            string sChain = "fps=" + num(iFrames / Math.Max(nSpan, 0.5));
            if (nCrop > 0.0) sChain = sChain + ",crop=iw:ih*" + num(1.0 - nCrop / 100.0) + ":0:0";
            sChain = sChain + ",scale=" + integer("width").ToString() + ":-2";
            if (iFrames == 2) sChain = sChain + ",tile=2x1";
            if (iFrames == 4) sChain = sChain + ",tile=2x2";
            string sArguments = "-hide_banner -loglevel error -y -ss " + num(nBegin) + " -t " + num(nSpan)
                              + " -i " + quoted(sPath) + " -vf " + quoted(sChain) + " -frames:v 1 -q:v 3 " + quoted(sImagePath);
            string sOut = "";
            string sErr = "";
            int iCode = runCommand(sFfmpeg, sArguments, out sOut, out sErr);
            return iCode == 0 && File.Exists(sImagePath);
        }

        static byte[] shotSignature(string sFfmpeg, string sImagePath, string sWorkDir)
        {
            string sRawPath = Path.Combine(sWorkDir, "signature.raw");
            string sOut = "";
            string sErr = "";
            int iCode = runCommand(sFfmpeg, "-hide_banner -loglevel error -y -i " + quoted(sImagePath)
                                          + " -vf " + quoted("scale=16:16,format=gray") + " -f rawvideo " + quoted(sRawPath), out sOut, out sErr);
            if (iCode != 0 || !File.Exists(sRawPath)) return new byte[0];
            return File.ReadAllBytes(sRawPath);
        }

        static double signatureDistance(byte[] binOne, byte[] binTwo)
        {
            if (binOne.Length == 0 || binOne.Length != binTwo.Length) return 255.0;
            long iTotal = 0;
            for (int iAt = 0; iAt < binOne.Length; iAt++)
            {
                iTotal = iTotal + Math.Abs(binOne[iAt] - binTwo[iAt]);
            }
            return (double)iTotal / (double)binOne.Length;
        }

        // ---------- asking the model ----------

        // The prompt follows the published guidance for audio description --
        // the American Council of the Blind's Audio Description Project
        // guidelines and standards, and the Audio Description Coalition's
        // standards. The rules that matter most to a machine describer are:
        // say what is visible and never what it means; present tense, active
        // voice, third person; establish the location first when the scene
        // changes; no filmmaking vocabulary; and never tell the listener
        // something the film has not yet shown them.
        // Everything a describer must know, sent once per request as the system
        // message rather than buried in the middle of the material. Two things
        // are deliberate here beyond the rules themselves.
        //
        // First, the worked examples. A rule stated in prose is weaker than one
        // shown: a model asked not to interpret still writes "his expression is
        // tense" until it sees the same moment written both ways. Measured over a
        // whole film, 132 of 251 descriptions carried an interpretive word
        // despite the rule being stated plainly.
        //
        // Second, the negatives are kept few and concrete. Telling a model never
        // to write "the frames show" puts that phrase in front of it, and it
        // duly appeared. Where a positive form exists it is used instead.
        static string systemRules()
        {
            StringBuilder oRules = new StringBuilder();
            oRules.Append("You write audio description for blind viewers of films, to the standards of the American Council of the Blind. ");
            oRules.Append("Your one discipline is this: report what is visible, and let the listener draw the conclusion. ");
            oRules.Append("Write the evidence, not your reading of it.\n\n");

            oRules.Append("Rewritten examples, each wrong then right:\n");
            oRules.Append("  \"He looks furious.\"  ->  \"He clenches his fist.\"\n");
            oRules.Append("  \"Her expression is tense and anxious.\"  ->  \"Her jaw is set. She grips the doorframe.\"\n");
            oRules.Append("  \"The atmosphere is ominous.\"  ->  \"Torchlight gutters. The hall beyond the doorway is dark.\"\n");
            oRules.Append("  \"The first frame shows a ship at sea.\"  ->  \"A ship rides low in a grey swell.\"\n");
            oRules.Append("  \"The camera pans across the shore.\"  ->  \"The shore stretches away, empty to the headland.\"\n");
            oRules.Append("  \"A man, likely Telemachus, enters.\"  ->  \"A young man in a red cloak enters.\"\n\n");

            oRules.Append("How to write:\n");
            oRules.Append("- Present tense, active voice, third person.\n");
            oRules.Append("- The exact verb, never a vague one with an adverb: strides, staggers, edges, sidles.\n");
            oRules.Append("- Concrete nouns. Clothing, colour, texture, light, posture, what the hands do.\n");
            oRules.Append("- Say who and what first, then where. Detail is the first thing to lose.\n");
            oRules.Append("- Name a person only when you are sure. Otherwise describe them by a feature and use the same feature every time.\n");
            oRules.Append("- Say only what this moment shows. Never anticipate the story.\n\n");

            oRules.Append("What the listener already has:\n");
            oRules.Append("- Every word of dialogue, all the music, and every sound effect. Never narrate a sound.\n");
            oRules.Append("- Subtitles are for people who cannot hear. They are not yours to read, and the words in them ");
            oRules.Append("are already spoken aloud in the film. Never read a subtitle and never mention that subtitles are present.\n");
            oRules.Append("- Words that carry meaning and are NOT subtitles -- a sign, a letter, a title card, a name on a door -- ");
            oRules.Append("are worth reading, introduced as: Words appear: followed by the words.\n\n");

            oRules.Append("Write only the description, as it will be spoken aloud. No preamble, no commentary, ");
            oRules.Append("no mention of frames, shots, scenes, panels, the camera, the sequence, or the film itself.");
            return oRules.ToString();
        }

        static string promptFor(int iMaxWords, List<string> lRecent, string sContext, bool bNewScene, bool bOverSound, List<string> lNames, string sJustSaid)
        {
            StringBuilder oPrompt = new StringBuilder();
            if (sContext.Trim() != "")
            {
                oPrompt.Append("About this film: " + sContext.Trim() + "\n");
                string sFront = presenterIn(sContext);
                if (sFront != "") oPrompt.Append("The person addressing the viewer in this film is " + sFront
                    + ". When someone looks towards the viewer and speaks, name " + sFront + " rather than writing \"a man\".\n");
                oPrompt.Append("\n");
            }
            oPrompt.Append("The picture holds " + integer("frames").ToString() + " frames from one brief moment of the film, in time order, ");
            oPrompt.Append("tiled left to right then top to bottom. They are one continuous moment, not separate pictures.\n\n");
            if (lNames.Count > 0)
            {
                oPrompt.Append("Names you have already used, so keep using them: ");
                foreach (string sName in lNames) oPrompt.Append(sName + ", ");
                oPrompt.Append("\n");
            }
            if (lRecent.Count > 0)
            {
                oPrompt.Append("You have just said, of the moments before this one: ");
                foreach (string sOld in lRecent) oPrompt.Append("\"" + sOld + "\" ");
                oPrompt.Append("\nSay none of that again. Describe only what has changed since.\n");
            }
            if (sJustSaid != "")
            {
                // The listener has just heard this. Telling them again wastes
                // the pause, and the standards are firm that description exists
                // to supply what sound cannot.
                oPrompt.Append("Spoken in the film just before this moment: \"" + sJustSaid + "\"\n");
                oPrompt.Append("The listener heard that. Do not repeat any of it, and do not describe anything it already tells them. ");
                oPrompt.Append("Use it to know who is present and what is happening.\n");
            }
            if (bNewScene) oPrompt.Append("The picture has changed completely, so this is a new scene. If you can see where we now are, open with that in a few words. If you cannot tell, say nothing about the place rather than guessing.\n");
            if (bOverSound) oPrompt.Append("There is no pause here: these words will fall across the music or the sound of the film. That is worth doing only for something that matters. If this moment holds nothing a blind viewer would genuinely miss, answer SKIP.\n");
            oPrompt.Append("\nDescribe this moment in no more than " + iMaxWords.ToString() + " words. ");
            oPrompt.Append("If there is nothing a blind viewer would need, answer with the single word SKIP.");
            return oPrompt.ToString();
        }

        // Words that state a conclusion rather than what was seen. The
        // standards are blunt about this: a judgment is the describer's
        // interpretation, and it takes the listener's own reading away.
        static readonly string[] asJudgmentWords = new string[] {
            "angry", "angrily", "anxious", "anxiously", "atmosphere", "beautiful", "calm", "confident",
            "confused", "determined", "eerie", "excited", "fearful", "furious", "grim", "happy", "hostile",
            "intense", "menacing", "mood", "nervous", "nervously", "ominous", "peaceful", "pensive",
            "reflecting", "sad", "sadly", "serene", "sinister", "somber", "sombre", "suggesting", "suspicious",
            "suspiciously", "tense", "tension", "thoughtful", "threatening", "troubled", "uneasy", "weary",
            "worried", "hinting", "evoking", "conveying", "seemingly", "apparently",
            // Added after measuring a full film: these were the commonest offenders,
            // appearing in 132 of 251 descriptions.
            "intently", "serious", "seriously", "warmly", "gently", "tranquil", "stern", "sternly",
            "emphatically", "relaxed", "suggests", "suggesting", "resolve", "contemplation", "realization",
            "grim", "grimly", "solemn", "tender", "tenderly", "wistful", "melancholy", "dramatic",
            "striking", "haunting", "poignant", "graceful", "gracefully", "elegant",
            // Seen in a run where the checks let them through.
            "distressed", "contemplative", "contemplatively", "dramatically", "silently",
            "observing", "casually", "closely", "quietly", "gazing", "seemingly",
            "apparently", "attentively", "curiously", "anxiously", "eagerly", "wearily"
        };

        // A guess dressed as a description. Naming the wrong character is worse
        // than naming none, and hedging tells the listener nothing either way.
        static readonly string[] asHedgeWords = new string[] {
            "likely", "probably", "possibly", "perhaps", "maybe", "or another", "appears to be",
            "seems to be", "could be", "might be", "presumably", "what appears"
        };

        static string hedgeFound(string sText)
        {
            foreach (string sWord in asHedgeWords)
            {
                if (sText.ToLower().IndexOf(sWord) >= 0) return sWord;
            }
            return "";
        }

        static string judgmentFound(string sText)
        {
            string sLower = " " + sText.ToLower() + " ";
            foreach (string sWord in asJudgmentWords)
            {
                if (sLower.IndexOf(" " + sWord + " ") >= 0) return sWord;
                if (sLower.IndexOf(" " + sWord + ",") >= 0) return sWord;
                if (sLower.IndexOf(" " + sWord + ".") >= 0) return sWord;
            }
            return "";
        }

        static string styleFor(string sDetail)
        {
            if (sDetail == "brief") return "One short sentence, the single most important thing. ";
            if (sDetail == "rich") return "Two or three tight sentences carrying real detail: clothing, colour, texture, light, posture, expression. Every word must earn its place. ";
            return "One or two sentences. Concrete nouns, few adjectives, no filler. ";
        }

        static double overrunFor(string sDetail)
        {
            if (sDetail == "brief") return 0.0;
            if (sDetail == "rich") return 3.5;
            return 1.5;
        }

        // The second stage, after AutoAD-Zero (Oxford VGG): the vision model is
        // asked to look thoroughly, and a language model then compresses what it
        // saw into one spoken description. Perceiving and being concise are
        // different jobs; asked to do both at once a vision model spends its
        // attention on the picture and its words on whatever comes first.
        //
        // No second model is installed: this is the same model with no image
        // attached, so nothing is loaded or unloaded between the two calls.
        static string summarise(string sSeen, int iMaxWords, List<string> lRecent)
        {
            if (sSeen.Trim() == "") return "";
            StringBuilder oRules = new StringBuilder();
            oRules.Append("You turn an observer's notes about one moment of a film into audio description for a blind viewer. ");
            oRules.Append("Report what was seen; never say what it means. Not \"he looks furious\" but \"he clenches his fist\". ");
            oRules.Append("Present tense, active voice, third person, concrete nouns, the exact verb. ");
            oRules.Append("Nothing about frames, shots, scenes, the camera or the film. Nothing that can be heard anyway. ");
            oRules.Append("Answer with the description alone: no preamble, no explanation, no quotation marks.");
            StringBuilder oPrompt = new StringBuilder();
            if (lRecent.Count > 0)
            {
                oPrompt.Append("Already said about the moments just before: ");
                foreach (string sOld in lRecent) oPrompt.Append("\"" + sOld + "\" ");
                oPrompt.Append("\n\n");
            }
            oPrompt.Append("The observer's notes:\n" + sSeen + "\n\n");
            oPrompt.Append("Write that as audio description. HARD LIMIT: " + iMaxWords.ToString() + " words. ");
            oPrompt.Append("Count them. A longer answer is worse than a shorter one, because it will be spoken over the dialogue. ");
            oPrompt.Append("Keep who is there, what they do, and where they are. Drop scenery, clothing, light and weather before going over the limit. ");
            oPrompt.Append("One sentence is usually enough; two at most. ");
            oPrompt.Append("If the notes hold nothing a blind viewer would need, answer SKIP.");
            Dictionary<string, object> dOptions = new Dictionary<string, object>();
            dOptions["temperature"] = 0.2;
            dOptions["num_predict"] = 200;
            dOptions["repeat_penalty"] = 1.15;
            Dictionary<string, object> dPayload = new Dictionary<string, object>();
            dPayload["model"] = text("model");
            dPayload["system"] = oRules.ToString();
            dPayload["prompt"] = oPrompt.ToString();
            dPayload["stream"] = false;
            dPayload["keep_alive"] = "30m";
            dPayload["options"] = dOptions;
            JavaScriptSerializer oSerializer = new JavaScriptSerializer();
            oSerializer.MaxJsonLength = int.MaxValue;
            waitingOn("writing the description");
            string sAnswer = postJson(text("url") + "/api/generate", oSerializer.Serialize(dPayload));
            waitingOn("");
            if (sAnswer == "") return sSeen;
            Dictionary<string, object> dReply = null;
            try
            {
                dReply = oSerializer.Deserialize<Dictionary<string, object>>(sAnswer);
            }
            catch (Exception oError)
            {
                logMessage("The summary could not be read: " + oError.Message, "ERROR");
                return sSeen;
            }
            if (!dReply.ContainsKey("response")) return sSeen;
            string sShort = Convert.ToString(dReply["response"]).Trim();
            sShort = Regex.Replace(sShort, @"^\s*skip\b[\s.:,-]*", "", RegexOptions.IgnoreCase);
            sShort = Regex.Replace(sShort, @"[\s.]*\bskip\s*[.!]?\s*$", "", RegexOptions.IgnoreCase);
            sShort = sShort.Trim().Trim('"');
            if (sShort == "") return "";
            return tidyText(sShort);
        }

        static string describeImage(string sImagePath, int iMaxWords, List<string> lRecent, string sContext, bool bAgain, bool bNewScene, string sJudgment, bool bOverSound, List<string> lNames, string sJustSaid, bool bInsist)
        {
            string sPrompt = promptFor(iMaxWords, lRecent, sContext, bNewScene, bOverSound && !bInsist, lNames, sJustSaid);
            if (bInsist) sPrompt = sPrompt + "\n\nThe listener has heard nothing for a long time, so say something this time. Describe whatever is most worth knowing about this moment, however ordinary. Do not answer SKIP.";
            if (bAgain) sPrompt = sPrompt + "\n\nYour last answer repeated what you had already said, which tells the listener nothing. Look for what is different. If truly nothing has changed, answer SKIP.";
            if (sJudgment != "") sPrompt = sPrompt + "\n\nYour last answer used the word \"" + sJudgment + "\". That states a conclusion. Write what you can see that led you to it, and let the listener conclude for themselves.";
            Dictionary<string, object> dOptions = new Dictionary<string, object>();
            dOptions["temperature"] = (bAgain || sJudgment != "") ? 0.9 : 0.35;
            dOptions["num_predict"] = 400;
            // Repetition is cheaper to prevent than to detect. A penalty at
            // generation time stops the model reaching for the same phrasing it
            // used a moment ago, which is what 352 rejected descriptions over one
            // film were really about.
            dOptions["repeat_penalty"] = 1.15;
            dOptions["repeat_last_n"] = 320;
            dOptions["top_p"] = 0.9;
            Dictionary<string, object> dPayload = new Dictionary<string, object>();
            dPayload["model"] = text("model");
            dPayload["system"] = systemRules();
            dPayload["prompt"] = sPrompt;
            dPayload["images"] = new string[] { Convert.ToBase64String(File.ReadAllBytes(sImagePath)) };
            dPayload["stream"] = false;
            // Hold the model in memory between moments. Without this it can be
            // unloaded during a long run and reloaded from disk, which costs
            // more than every other part of a description put together.
            dPayload["keep_alive"] = "30m";
            dPayload["options"] = dOptions;
            JavaScriptSerializer oSerializer = new JavaScriptSerializer();
            oSerializer.MaxJsonLength = int.MaxValue;
            waitingOn("looking at " + formatClock(nWaitingAt));
            string sAnswer = postJson(text("url") + "/api/generate", oSerializer.Serialize(dPayload));
            waitingOn("");
            if (sAnswer == "") return "";
            Dictionary<string, object> dReply = null;
            try
            {
                dReply = oSerializer.Deserialize<Dictionary<string, object>>(sAnswer);
            }
            catch (Exception oError)
            {
                logMessage("The model's answer could not be read: " + oError.Message, "ERROR");
                return "";
            }
            if (!dReply.ContainsKey("response")) return "";
            string sText = Convert.ToString(dReply["response"]).Trim();
            // The model answers with a description AND the escape word: 21 of 126
            // descriptions in one run ended "... as they move quickly. Skip."
            // Only a leading SKIP was being caught.
            sText = Regex.Replace(sText, @"^\s*skip\b[\s.:,-]*", "", RegexOptions.IgnoreCase);
            sText = Regex.Replace(sText, @"[\s.]*\bskip\s*[.!]?\s*$", "", RegexOptions.IgnoreCase);
            if (sText.Trim() == "") return "";
            return tidyText(sText);
        }

        static string postJson(string sUrl, string sBody)
        {
            try
            {
                HttpWebRequest oRequest = (HttpWebRequest)WebRequest.Create(sUrl);
                oRequest.Method = "POST";
                oRequest.ContentType = "application/json";
                oRequest.Timeout = iDefaultTimeout;
                oRequest.ReadWriteTimeout = iDefaultTimeout;
                byte[] binBody = Encoding.UTF8.GetBytes(sBody);
                oRequest.ContentLength = binBody.Length;
                Stream oSend = oRequest.GetRequestStream();
                oSend.Write(binBody, 0, binBody.Length);
                oSend.Close();
                WebResponse oResponse = oRequest.GetResponse();
                StreamReader oReader = new StreamReader(oResponse.GetResponseStream(), Encoding.UTF8);
                string sAnswer = oReader.ReadToEnd();
                oReader.Close();
                oResponse.Close();
                return sAnswer;
            }
            catch (Exception oError)
            {
                logMessage("The request to " + sUrl + " failed: " + oError.Message, "ERROR");
                return "";
            }
        }

        static bool checkOllama()
        {
            string sUrl = text("url") + "/api/tags";
            logMessage("Asking Ollama for its model list at " + sUrl, "INFO", "");
            string sAnswer = "";
            try
            {
                WebClient oClient = new WebClient();
                sAnswer = oClient.DownloadString(sUrl);
            }
            catch (Exception oError)
            {
                logMessage("Ollama did not answer at " + sUrl + ": " + oError.Message, "ERROR");
                logMessage("Start the Ollama service, then run this again.", "HINT");
                return false;
            }
            JavaScriptSerializer oSerializer = new JavaScriptSerializer();
            oSerializer.MaxJsonLength = int.MaxValue;
            Dictionary<string, object> dReply = oSerializer.Deserialize<Dictionary<string, object>>(sAnswer);
            bool bFound = false;
            List<string> lNames = new List<string>();
            if (dReply.ContainsKey("models"))
            {
                foreach (object oModel in toList(dReply["models"]))
                {
                    Dictionary<string, object> dModel = toMap(oModel);
                    if (!dModel.ContainsKey("name")) continue;
                    string sName = Convert.ToString(dModel["name"]);
                    lNames.Add(sName);
                    if (sName == text("model")) bFound = true;
                    if (sName.Split(':')[0] == text("model").Split(':')[0]) bFound = true;
                }
            }
            logMessage("Ollama holds " + lNames.Count.ToString() + " models: " + string.Join(", ", lNames.ToArray()), "INFO", "");
            if (!bFound) logMessage("The model " + text("model") + " is not installed. Pull it with: ollama pull " + text("model"), "ERROR");
            return bFound;
        }

        // ---------- tidying what the model says ----------

        static readonly string[] asStripOpeners = new string[] {
            @"^(in|across|throughout)\s+(the|this|these)\s+(first|second|third|fourth|final|last|next|opening)?\s*(frames?|images?|shots?|panels?|scene|sequence)\s*,?\s*",
            @"^(the|this)\s+(first|second|third|fourth|final|last|next|opening|closing)\s+(frame|image|shot|panel)?\s*(shows?|depicts?|captures?|reveals?|presents?|is|features?)\s*",
            @"^(the|these)\s+(frames?|images?|shots?|panels?|pictures?)\s+(show|shows|depict|depicts|capture|captures|reveal|reveals)\s*",
            @"^(the\s+)?(sequence|scene|footage|film|clip|montage|shot)\s+(begins|opens|starts)\s+(with|by|on|in)\s*",
            @"^(the|this)\s+(image|picture|frame|photo|photograph|still)\s+(shows?|depicts?|captures?)\s*",
            @"^(we|the\s+viewer)\s+(then\s+)?(see|sees|watch|observe|are\s+shown)\s*"
        };

        // Markers the model puts in front of each tile of the montage. The
        // words after them are a real description and are kept.
        static readonly string[] asTileMarkers = new string[] {
            @"\b(the\s+)?(first|second|third|fourth|next|last|final|top|bottom|left|right|upper|lower)\s+(frame|panel|image|picture|tile)\s*(shows|showing|depicts|is)?\s*:?\s*",
            @"\b(frame|panel|image|tile)\s+(one|two|three|four|below|above|next|left|right)\s*:?\s*",
            @"\bin\s+the\s+(next|following|second|third|fourth|last)\s+(frame|panel|image|tile)\s*,?\s*"
        };

        static readonly string[] asClauseTrims = new string[] {
            @",?\s*(as|while|and|with)?\s*the\s+camera[^,.;]*",
            @",?\s*(before\s+|then\s+)?(transitioning|cutting|panning|zooming|shifting)\s+(to|into|across)[^,.;]*"
        };

        static readonly string[] asFilmTalk = new string[] {
            @"\b(camera|frames?|panels?|footage|montage)\b",
            @"\bthe\s+(shot|image|picture|still|sequence)\b",
            @"\b(scene|view|perspective|focus)\s+(then\s+)?(shifts?|cuts?|turns?|changes?|switches?)\b",
            @"\bwe\s+(see|watch|observe|are\s+shown)\b",
            @"\b(off|on)[\s\-]?screen\b",
            @"\bout\s+of\s+(frame|shot)\b",
            @"\bin\s+(frame|shot)\b"
        };

        static List<string> splitSentences(string sText)
        {
            List<string> lParts = new List<string>();
            // A full stop after a single letter is an initial, not the end of a
            // sentence: "U. S." and "ALI A. MAZRUI" were being cut in half.
            // Likewise the common abbreviations.
            string sMark = "\u0001";
            string sGuarded = Regex.Replace(sText, @"\b([A-Za-z])\.", "$1" + sMark);
            sGuarded = Regex.Replace(sGuarded, @"\b(Mr|Mrs|Ms|Dr|St|Prof|Rev|Jr|Sr|vs|etc|Inc|Ltd)\.", "$1" + sMark, RegexOptions.IgnoreCase);
            foreach (Match oMatch in Regex.Matches(sGuarded, @"[^.!?]*[.!?]"))
            {
                string sPart = oMatch.Value.Replace(sMark, ".").Trim();
                if (sPart != "") lParts.Add(sPart);
            }
            return lParts;
        }

        static string tidyText(string sText)
        {
            string sClean = Regex.Replace(sText, @"\s+", " ").Trim();
            List<string> lParts = splitSentences(sClean);
            if (lParts.Count == 0) lParts.Add(sClean);
            List<string> lKept = new List<string>();
            List<string> lFallback = new List<string>();
            foreach (string sPart in lParts)
            {
                string sOne = sPart;
                foreach (string sPattern in asTileMarkers) sOne = Regex.Replace(sOne, sPattern, "", RegexOptions.IgnoreCase);
                foreach (string sPattern in asStripOpeners) sOne = Regex.Replace(sOne, sPattern, "", RegexOptions.IgnoreCase);
                foreach (string sPattern in asClauseTrims) sOne = Regex.Replace(sOne, sPattern, "", RegexOptions.IgnoreCase);
                sOne = Regex.Replace(sOne, @"\s+", " ").Trim();
                sOne = Regex.Replace(sOne, @"^[\s,;:.\-]+", "");
                // Cutting "the camera" out of "facing away from the camera"
                // leaves "facing away from", which is worse than the fault it
                // fixed. Where a trim leaves a preposition dangling, the phrase
                // it governed goes with it.
                sOne = Regex.Replace(sOne, @"[,;]?\s+\w+ing(\s+away)?\s+(from|toward|towards|into|at|behind|beside|past|across|over|under)\s*[.!?]?$", ".", RegexOptions.IgnoreCase);
                sOne = Regex.Replace(sOne, @"[,;]?\s+(from|toward|towards|into|at|behind|beside|past|across|over|under|with|of)\s*[.!?]?$", ".", RegexOptions.IgnoreCase);
                sOne = Regex.Replace(sOne, @"\s+\.", ".");
                // The convention that introduces on-screen text is said once,
                // however many pieces of text there are.
                sOne = Regex.Replace(sOne, @"(Words appear:|Text appears:)(.*?)\s*(Words appear:|Text appears:)\s*", "$1$2 ", RegexOptions.IgnoreCase);
                if (sOne == "") continue;
                if (sOne[sOne.Length - 1] != '.' && sOne[sOne.Length - 1] != '!' && sOne[sOne.Length - 1] != '?') sOne = sOne.TrimEnd(',', ';', ':', ' ') + ".";
                sOne = sOne.Substring(0, 1).ToUpper() + sOne.Substring(1);
                lFallback.Add(sOne);
                bool bFilmTalk = false;
                foreach (string sPattern in asFilmTalk)
                {
                    if (Regex.IsMatch(sOne, sPattern, RegexOptions.IgnoreCase)) bFilmTalk = true;
                }
                if (!bFilmTalk) lKept.Add(sOne);
            }
            if (lKept.Count == 0) lKept = lFallback;
            return string.Join(" ", lKept.ToArray()).Trim();
        }

        static string trimToWords(string sText, int iMaxWords)
        {
            List<string> lParts = splitSentences(tidyText(sText));
            if (lParts.Count == 0) return "";
            List<string> lKept = new List<string>();
            int iCount = 0;
            foreach (string sPart in lParts)
            {
                int iWords = sPart.Split(' ').Length;
                if (lKept.Count > 0 && iCount + iWords > iMaxWords) break;
                lKept.Add(sPart);
                iCount = iCount + iWords;
            }
            return string.Join(" ", lKept.ToArray());
        }

        // Remove the last comma or semicolon clause, leaving what is still a
        // sentence. Refuses when the result would be too short, would end on a
        // word that needs something after it, or would be only the opening
        // phrase of the sentence, which is a phrase and not a sentence.
        static string dropLastClause(string sText)
        {
            string sBody = sText.TrimEnd('.', '!', '?', ' ');
            int iComma = sBody.LastIndexOf(", ");
            int iSemi = sBody.LastIndexOf("; ");
            int iCut = Math.Max(iComma, iSemi);
            if (iCut < 0) return sText;
            string sShort = sBody.Substring(0, iCut).TrimEnd(',', ';', ' ');
            sShort = Regex.Replace(sShort, @"\s+\b(and|but|or|nor|yet|so|with|without|from|to|into|onto|as|while|when|where|which|who|whom|that|before|after|under|over|beside|behind|beneath|toward|towards|through|across|against|between|among|near|amid|amidst)\s*$", "", RegexOptions.IgnoreCase);
            if (sShort.Split(' ').Length < 4) return sText;
            if (sShort.IndexOf(',') < 0 && Regex.IsMatch(sShort, @"^(at|in|on|under|over|near|beside|behind|above|below|along|across|through|beyond|within|outside|inside|amid|amidst|among|between|during|after|before|by|with|from|against|toward|towards|beneath)\b", RegexOptions.IgnoreCase)) return sText;
            return sShort + ".";
        }

        static string dropLastSentence(string sText)
        {
            List<string> lParts = splitSentences(sText);
            if (lParts.Count <= 1) return sText;
            lParts.RemoveAt(lParts.Count - 1);
            return string.Join(" ", lParts.ToArray());
        }

        static readonly string[] asStopWords = new string[] {
            "with", "from", "that", "this", "they", "them", "their", "there", "then", "than",
            "into", "onto", "over", "under", "while", "which", "where", "what", "when", "some",
            "more", "most", "very", "much", "also", "just", "like", "such", "have", "been", "were"
        };

        static List<string> contentWords(string sText)
        {
            List<string> lWords = new List<string>();
            foreach (Match oMatch in Regex.Matches(sText.ToLower(), "[a-z]+"))
            {
                string sWord = oMatch.Value;
                if (sWord.Length <= 3) continue;
                bool bStop = false;
                foreach (string sStop in asStopWords)
                {
                    if (sWord == sStop) bStop = true;
                }
                if (bStop) continue;
                if (!lWords.Contains(sWord)) lWords.Add(sWord);
            }
            return lWords;
        }

        // A model repeats itself two ways: word for word, and reshuffled.
        // The first needs a sequence comparison, the second a word comparison.
        // The standards ask for the same names and words throughout a whole
        // production. Each moment is written knowing almost nothing of the rest,
        // so the names already used are gathered and handed forward.
        static void gatherNames(string sText, List<string> lNames)
        {
            foreach (Match oMatch in Regex.Matches(sText, @"(?<=[a-z,] )([A-Z][a-z]{2,})"))
            {
                string sName = oMatch.Groups[1].Value;
                if (lNames.Contains(sName)) continue;
                lNames.Add(sName);
                if (lNames.Count > iDefaultNames) lNames.RemoveAt(0);
            }
        }

        static double worstLikeness(string sText, List<string> lEarlier)
        {
            double nWorst = 0.0;
            if (sText.Trim() == "") return 0.0;
            List<string> lOne = contentWords(sText);
            foreach (string sOld in lEarlier)
            {
                List<string> lOld = contentWords(sOld);
                double nShared = 0.0;
                if (lOne.Count > 0 && lOld.Count > 0)
                {
                    int iShared = 0;
                    foreach (string sWord in lOne)
                    {
                        if (lOld.Contains(sWord)) iShared = iShared + 1;
                    }
                    int iUnion = lOne.Count + lOld.Count - iShared;
                    if (iUnion > 0) nShared = (double)iShared / (double)iUnion;
                }
                double nSame = 0.0;
                if (sText == sOld) nSame = 1.0;
                double nLike = Math.Max(nShared, nSame);
                if (nLike > nWorst) nWorst = nLike;
            }
            return nWorst;
        }

        // ---------- speaking ----------

        static SpeechSynthesizer oSynth = null;

        static bool openVoice()
        {
            try
            {
                oSynth = new SpeechSynthesizer();
                if (text("voice") != "") oSynth.SelectVoice(text("voice"));
                oSynth.Rate = integer("rate");
            }
            catch (Exception oError)
            {
                logMessage("The speech voice could not be started: " + oError.Message, "ERROR");
                return false;
            }
            return true;
        }

        static void listVoices()
        {
            SpeechSynthesizer oList = new SpeechSynthesizer();
            foreach (InstalledVoice oVoice in oList.GetInstalledVoices())
            {
                logMessage("Voice: " + oVoice.VoiceInfo.Name, "INFO", oVoice.VoiceInfo.Name);
            }
            oList.Dispose();
        }

        // Speech goes straight into memory as the same format the final track
        // uses, so no temporary wave file and no conversion are needed.
        static byte[] speakToPcm(string sText, int iRate)
        {
            try
            {
                oSynth.Rate = iRate;
                MemoryStream oStream = new MemoryStream();
                oSynth.SetOutputToAudioStream(oStream, new SpeechAudioFormatInfo(iDefaultSampleRate, AudioBitsPerSample.Sixteen, AudioChannel.Mono));
                oSynth.Speak(sText);
                oSynth.SetOutputToNull();
                return oStream.ToArray();
            }
            catch (Exception oError)
            {
                logMessage("Speech failed: " + oError.Message, "ERROR");
                return new byte[0];
            }
        }

        static double pcmSeconds(byte[] binAudio)
        {
            return (double)binAudio.Length / 2.0 / (double)iDefaultSampleRate;
        }

        // ---------- building the track ----------

        static bool buildTrack(List<Moment> lMoments, double nDuration, string sPath)
        {
            try
            {
                FileStream oFile = new FileStream(sPath, FileMode.Create, FileAccess.Write);
                BinaryWriter oWriter = new BinaryWriter(oFile);
                long iSamples = (long)(nDuration * iDefaultSampleRate);
                long iBytes = iSamples * 2;
                oWriter.Write(Encoding.ASCII.GetBytes("RIFF"));
                oWriter.Write((int)(36 + iBytes));
                oWriter.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                oWriter.Write((int)16);
                oWriter.Write((short)1);
                oWriter.Write((short)1);
                oWriter.Write((int)iDefaultSampleRate);
                oWriter.Write((int)(iDefaultSampleRate * 2));
                oWriter.Write((short)2);
                oWriter.Write((short)16);
                oWriter.Write(Encoding.ASCII.GetBytes("data"));
                oWriter.Write((int)iBytes);
                byte[] binSilence = new byte[iDefaultSampleRate * 2];
                long iCursor = 0;
                foreach (Moment oMoment in lMoments)
                {
                    long iTarget = (long)(oMoment.nStart * iDefaultSampleRate);
                    if (iTarget < iCursor) iTarget = iCursor;
                    long iQuiet = iTarget - iCursor;
                    while (iQuiet > 0)
                    {
                        int iChunk = (int)Math.Min(iQuiet, (long)iDefaultSampleRate);
                        oWriter.Write(binSilence, 0, iChunk * 2);
                        iQuiet = iQuiet - iChunk;
                    }
                    oWriter.Write(oMoment.binAudio);
                    iCursor = iTarget + oMoment.binAudio.Length / 2;
                }
                long iTail = iSamples - iCursor;
                while (iTail > 0)
                {
                    int iChunk = (int)Math.Min(iTail, (long)iDefaultSampleRate);
                    oWriter.Write(binSilence, 0, iChunk * 2);
                    iTail = iTail - iChunk;
                }
                oWriter.Close();
                oFile.Close();
            }
            catch (Exception oError)
            {
                logMessage("The description track could not be written: " + oError.Message, "ERROR");
                return false;
            }
            return true;
        }

        static Thread threadMux = null;
        static bool bMuxRunning = false;

        // Writing the film is minutes of ffmpeg work that has nothing to do with
        // the model, so it need not stop the describing. The description track is
        // copied first, because the live one is rewritten at every checkpoint and
        // ffmpeg would otherwise be reading a file as it changes underneath.
        static void startBackgroundMux(string sFfmpeg, string sVideo, string sAdWave, string sOutPath, double nDuration)
        {
            if (bMuxRunning)
            {
                logMessage("The previous copy of the film is still being written, so this one is left until later.", "INFO", "");
                return;
            }
            string sSnapshot = sAdWave + ".mux.wav";
            try
            {
                File.Copy(sAdWave, sSnapshot, true);
            }
            catch (Exception oError)
            {
                logMessage("The description track could not be copied for writing: " + oError.Message, "ERROR");
                return;
            }
            bMuxRunning = true;
            logMessage("Writing the film so far in the background. Describing carries on meanwhile.",
                       "INFO", "  (writing the film so far in the background; describing carries on)");
            threadMux = new Thread(delegate()
            {
                try
                {
                    muxOutput(sFfmpeg, sVideo, sSnapshot, sOutPath, nDuration, true);
                }
                catch (Exception oError)
                {
                    logMessage("Writing the film in the background failed: " + oError.Message, "ERROR", "");
                }
                finally
                {
                    try
                    {
                        if (File.Exists(sSnapshot)) File.Delete(sSnapshot);
                    }
                    catch (Exception)
                    {
                    }
                    bMuxRunning = false;
                }
            });
            threadMux.IsBackground = true;
            threadMux.Start();
        }

        static void waitForMux()
        {
            if (threadMux == null) return;
            if (threadMux.IsAlive) logMessage("Waiting for the background copy of the film to finish.", "INFO", "  (waiting for the background copy to finish)");
            threadMux.Join();
            threadMux = null;
        }

        static bool muxOutput(string sFfmpeg, string sVideo, string sAdWave, string sOutPath, double nDuration, bool bBackground)
        {
            string sFilter = "[1:a]aformat=sample_fmts=fltp:sample_rates=48000:channel_layouts=stereo,volume=" + num(number("ad-volume")) + ",asplit=2[adDuck][adMix];"
                           + "[0:a]aformat=sample_fmts=fltp:sample_rates=48000:channel_layouts=stereo[main];"
                           + "[main][adDuck]sidechaincompress=threshold=0.01:ratio=20:attack=5:release=300[duck];"
                           + "[duck][adMix]amix=inputs=2:duration=first:normalize=0[mix]";
            string sPartAudio = Path.Combine(Path.GetDirectoryName(sOutPath),
                                             Path.GetFileNameWithoutExtension(sOutPath) + ".part" + Path.GetExtension(sOutPath));
            if (flag("audio-only"))
            {
                // Sound only. No video is copied, so this is minutes of work
                // rather than the whole film rewritten, and the result is a
                // fraction of the size.
                string sAudioArgs = "-hide_banner -y -i " + quoted(sVideo) + " -i " + quoted(sAdWave)
                                  + " -filter_complex " + quoted(sFilter)
                                  + " -map " + quoted("[mix]") + " -vn -c:a libmp3lame -q:a 4"
                                  + " -metadata " + quoted("title=" + Path.GetFileNameWithoutExtension(sVideo) + ", with audio description")
                                  + " " + quoted(sPartAudio);
                logMessage("Writing the described audio to " + sOutPath,
                           "INFO", bBackground ? "" : "Finalizing.");
                string sAudioErr = runScan(sFfmpeg, sAudioArgs, nDuration, bBackground ? "" : "Writing");
                if (iLastScanExit != 0)
                {
                    logMessage("The described audio could not be written. ffmpeg said: " + tail(sAudioErr.Trim(), 1200), "ERROR");
                    logMessage("If ffmpeg has no mp3 encoder, install a build that has libmp3lame.", "HINT");
                    try
                    {
                        if (File.Exists(sPartAudio)) File.Delete(sPartAudio);
                    }
                    catch (Exception)
                    {
                    }
                    return false;
                }
                try
                {
                    if (File.Exists(sOutPath)) File.Delete(sOutPath);
                    File.Move(sPartAudio, sOutPath);
                }
                catch (Exception oError)
                {
                    logMessage("The described audio could not be moved into place: " + oError.Message, "ERROR");
                    return false;
                }
                logMessage("The described audio is ready at " + sOutPath, "INFO", "  (described audio written)");
                return true;
            }
            string sArguments = "-hide_banner -y -i " + quoted(sVideo) + " -i " + quoted(sAdWave)
                              + " -filter_complex " + quoted(sFilter)
                              + " -map 0:v:0 -c:v copy"
                              + " -map " + quoted("[mix]") + " -c:a:0 aac -b:a:0 192k"
                              + " -map 0:a:0 -c:a:1 copy"
                              + " -metadata:s:a:0 " + quoted("title=" + sDefaultTrackTitle)
                              + " -metadata:s:a:1 " + quoted("title=Original")
                              + " -disposition:a:0 default -disposition:a:1 0 " + quoted(sOutPath);
            // Written to a temporary name and moved into place only on success, so
            // a run stopped part way cannot leave a half-written film where a
            // whole one used to be.
            //
            // And reported as it goes. Muxing a three hour film takes about five
            // minutes, during which nothing else happens; without progress it is
            // indistinguishable from a hang, which is exactly how it was read.
            // The extension must stay last, or ffmpeg cannot tell what container
            // to write: "described.mp4.part" fails instantly with Invalid
            // argument, while "described.part.mp4" is fine.
            string sPartPath = Path.Combine(Path.GetDirectoryName(sOutPath),
                                            Path.GetFileNameWithoutExtension(sOutPath) + ".part" + Path.GetExtension(sOutPath));
            sArguments = sArguments.Replace(quoted(sOutPath), quoted(sPartPath));
            logMessage("Writing the described film to " + sOutPath,
                       "INFO", bBackground ? "" : "Finalizing.");
            string sMuxErr = runScan(sFfmpeg, sArguments, nDuration, bBackground ? "" : "Writing");
            if (iLastScanExit != 0)
            {
                logMessage("The described film could not be written. ffmpeg said: " + tail(sMuxErr.Trim(), 1200), "ERROR", bBackground ? "" : null);
                try
                {
                    if (File.Exists(sPartPath)) File.Delete(sPartPath);
                }
                catch (Exception)
                {
                }
                return false;
            }
            try
            {
                if (File.Exists(sOutPath)) File.Delete(sOutPath);
                File.Move(sPartPath, sOutPath);
            }
            catch (Exception oError)
            {
                logMessage("The described film was written but could not be moved into place: " + oError.Message, "ERROR");
                return false;
            }
            logMessage("The described film is ready at " + sOutPath, "INFO", bBackground ? "  (the film so far has been written)" : "  (described film written)");
            return true;
        }

        // ---------- what a person can read ----------

        static string timestamp(double nSeconds)
        {
            int iWhole = (int)nSeconds;
            int iMilliseconds = (int)Math.Round((nSeconds - iWhole) * 1000.0);
            return (iWhole / 3600).ToString("00") + ":" + ((iWhole % 3600) / 60).ToString("00") + ":" + (iWhole % 60).ToString("00") + "." + iMilliseconds.ToString("000");
        }

        static void writeVtt(List<Moment> lMoments, string sPath)
        {
            StreamWriter fVtt = new StreamWriter(sPath, false, new UTF8Encoding(true));
            fVtt.WriteLine("WEBVTT");
            fVtt.WriteLine("");
            int iNumber = 1;
            foreach (Moment oMoment in lMoments)
            {
                fVtt.WriteLine(iNumber.ToString());
                fVtt.WriteLine(timestamp(oMoment.nStart) + " --> " + timestamp(oMoment.nStart + oMoment.nSpoken));
                fVtt.WriteLine(oMoment.sText);
                fVtt.WriteLine("");
                iNumber = iNumber + 1;
            }
            fVtt.Close();
        }

        static void writeMarkdown(List<Moment> lMoments, string sPath, string sSourceName, double nDuration)
        {
            StreamWriter fDoc = new StreamWriter(sPath, false, new UTF8Encoding(true));
            fDoc.WriteLine("# Audio description of " + sSourceName);
            fDoc.WriteLine("");
            fDoc.WriteLine("- Descriptions: " + lMoments.Count.ToString());
            fDoc.WriteLine("- Running time: " + formatClock(nDuration));
            fDoc.WriteLine("- Written by: " + text("model"));
            fDoc.WriteLine("- Generated: " + DateTime.Now.ToString("d MMMM yyyy"));
            fDoc.WriteLine("");
            fDoc.WriteLine("Each entry gives the time it is spoken, followed by the description. Times are counted from the start of the film.");
            fDoc.WriteLine("");
            double nChapter = -1.0;
            foreach (Moment oMoment in lMoments)
            {
                double nThis = Math.Floor(oMoment.nStart / nDefaultChapter) * nDefaultChapter;
                if (nThis != nChapter)
                {
                    nChapter = nThis;
                    fDoc.WriteLine("");
                    fDoc.WriteLine("## " + formatClock(nChapter) + " to " + formatClock(Math.Min(nChapter + nDefaultChapter, nDuration)));
                    fDoc.WriteLine("");
                }
                fDoc.WriteLine("- " + formatClock(oMoment.nStart) + " " + oMoment.sText);
            }
            fDoc.Close();
        }

        static bool bTranscribed = false;
        static bool bSpeechReady = false;

        // Writing the record when only a transcript has been made, so the job is
        // remembered as done even though no description exists.
        static string sJsonPathEarly(string sWorkDir)
        {
            return Path.Combine(sWorkDir, sDefaultJsonName);
        }


        static void writeCache(List<Moment> lMoments, string sPath, bool bFinished, List<Moment> lGaps, Dictionary<string, object> dSignature)
        {
            List<object> lItems = new List<object>();
            foreach (Moment oMoment in lMoments)
            {
                Dictionary<string, object> dItem = new Dictionary<string, object>();
                dItem["start"] = oMoment.nStart;
                dItem["length"] = oMoment.nLength;
                dItem["spoken"] = oMoment.nSpoken;
                dItem["text"] = oMoment.sText;
                lItems.Add(dItem);
            }
            Dictionary<string, object> dData = new Dictionary<string, object>();
            dData["items"] = lItems;
            dData["finished"] = bFinished;
            dData["transcribed"] = bTranscribed;
            // The moment list, so a resumed run does not read the whole sound
            // track again, and the settings that produced it, so a changed
            // setting is noticed and the scan repeated.
            if (lGaps != null)
            {
                List<object> lPlaces = new List<object>();
                foreach (Moment oGap in lGaps)
                {
                    Dictionary<string, object> dGap = new Dictionary<string, object>();
                    dGap["start"] = oGap.nStart;
                    dGap["length"] = oGap.nLength;
                    dGap["forced"] = oGap.bForced;
                    lPlaces.Add(dGap);
                }
                dData["gaps"] = lPlaces;
            }
            if (dSignature != null) dData["gapSignature"] = dSignature;
            JavaScriptSerializer oSerializer = new JavaScriptSerializer();
            oSerializer.MaxJsonLength = int.MaxValue;
            StreamWriter fJson = new StreamWriter(sPath, false, new UTF8Encoding(true));
            fJson.Write(oSerializer.Serialize(dData));
            fJson.Close();
        }

        // The settings that decide WHERE descriptions go. If they are unchanged,
        // the moment list from the earlier run is reused and the sound track is
        // not read again -- which on a slow machine is minutes before anything
        // visible happens.
        static Dictionary<string, object> gapSignature(double nDuration)
        {
            Dictionary<string, object> dSignature = new Dictionary<string, object>();
            dSignature["noiseFloor"] = num(number("noise-floor"));
            dSignature["silenceLength"] = num(number("silence-length"));
            dSignature["minGap"] = num(number("min-gap"));
            dSignature["spacing"] = num(number("spacing"));
            dSignature["every"] = num(number("every"));
            dSignature["forcedLength"] = num(number("forced-length"));
            dSignature["dialogueChannel"] = text("dialogue-channel");
            dSignature["duration"] = num(Math.Round(nDuration, 1));
            return dSignature;
        }

        static bool sameSignature(Dictionary<string, object> dOne, Dictionary<string, object> dTwo)
        {
            if (dOne == null || dTwo == null) return false;
            foreach (KeyValuePair<string, object> oPair in dOne)
            {
                if (!dTwo.ContainsKey(oPair.Key)) return false;
                if (Convert.ToString(dTwo[oPair.Key]) != Convert.ToString(oPair.Value)) return false;
            }
            return true;
        }

        static bool bLastCacheFinished = false;
        static bool bLastTranscriptFinished = false;
        static List<Moment> lCachedGaps = null;
        static Dictionary<string, object> dCachedSignature = null;

        static Dictionary<string, string> readCache(string sPath)
        {
            Dictionary<string, string> dCache = new Dictionary<string, string>();
            if (!File.Exists(sPath)) return dCache;
            try
            {
                JavaScriptSerializer oSerializer = new JavaScriptSerializer();
                oSerializer.MaxJsonLength = int.MaxValue;
                Dictionary<string, object> dData = oSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(sPath));
                bLastCacheFinished = dData.ContainsKey("finished") && Convert.ToBoolean(dData["finished"]);
                bLastTranscriptFinished = dData.ContainsKey("transcribed") && Convert.ToBoolean(dData["transcribed"]);
                lCachedGaps = null;
                dCachedSignature = null;
                if (dData.ContainsKey("gapSignature")) dCachedSignature = toMap(dData["gapSignature"]);
                if (dData.ContainsKey("gaps"))
                {
                    List<Moment> lPlaces = new List<Moment>();
                    foreach (object oGap in toList(dData["gaps"]))
                    {
                        Dictionary<string, object> dGap = toMap(oGap);
                        if (!dGap.ContainsKey("start")) continue;
                        Moment oMoment = new Moment();
                        oMoment.nStart = Convert.ToDouble(dGap["start"]);
                        oMoment.nLength = Convert.ToDouble(dGap["length"]);
                        oMoment.bForced = dGap.ContainsKey("forced") && Convert.ToBoolean(dGap["forced"]);
                        lPlaces.Add(oMoment);
                    }
                    if (lPlaces.Count > 0) lCachedGaps = lPlaces;
                }
                if (!dData.ContainsKey("items")) return dCache;
                foreach (object oItem in toList(dData["items"]))
                {
                    Dictionary<string, object> dItem = toMap(oItem);
                    if (!dItem.ContainsKey("start")) continue;
                    double nStart = Convert.ToDouble(dItem["start"]);
                    dCache[num(nStart)] = Convert.ToString(dItem["text"]);
                }
            }
            catch (Exception oError)
            {
                logMessage("The earlier run's results could not be read: " + oError.Message, "ERROR");
            }
            return dCache;
        }

        // What was said, as a document to read. The same shape as the described
        // script, so the two sit together on a braille display.
        static void writeTranscript(List<Speech> lSpeech, string sPath, string sSourceName, double nDuration)
        {
            StreamWriter fDoc = new StreamWriter(sPath, false, new UTF8Encoding(true));
            fDoc.WriteLine("# Transcript of " + sSourceName);
            fDoc.WriteLine("");
            fDoc.WriteLine("- Spoken stretches: " + lSpeech.Count.ToString());
            fDoc.WriteLine("- Running time: " + formatClock(nDuration));
            fDoc.WriteLine("- Heard by: Whisper " + text("whisper-model"));
            fDoc.WriteLine("- Generated: " + DateTime.Now.ToString("d MMMM yyyy"));
            fDoc.WriteLine("");
            fDoc.WriteLine("Each entry gives the time it is spoken, followed by the words. Times are counted from the start.");
            fDoc.WriteLine("");
            double nChapter = -1.0;
            foreach (Speech oSpeech in lSpeech)
            {
                if (oSpeech.sText == "") continue;
                double nThis = Math.Floor(oSpeech.nStart / nDefaultChapter) * nDefaultChapter;
                if (nThis != nChapter)
                {
                    nChapter = nThis;
                    fDoc.WriteLine("");
                    fDoc.WriteLine("## " + formatClock(nChapter) + " to " + formatClock(Math.Min(nChapter + nDefaultChapter, nDuration)));
                    fDoc.WriteLine("");
                }
                fDoc.WriteLine("- " + formatClock(oSpeech.nStart) + " " + oSpeech.sText);
            }
            fDoc.Close();
        }

        // Both at once, in the order they happen. For someone who can neither
        // see nor hear the film this is the whole of it: what was said and what
        // was there to be seen, in one readable sequence. Descriptions are
        // marked, because a reader must be able to tell the film's own words
        // from words written about it.
        static void writeScribed(List<Moment> lMoments, List<Speech> lSpeech, string sPath, string sSourceName, double nDuration)
        {
            List<string[]> lLines = new List<string[]>();
            foreach (Moment oMoment in lMoments)
            {
                if (oMoment.sText == "") continue;
                lLines.Add(new string[] { num(oMoment.nStart), formatClock(oMoment.nStart), "Description", oMoment.sText });
            }
            foreach (Speech oSpeech in lSpeech)
            {
                if (oSpeech.sText == "") continue;
                lLines.Add(new string[] { num(oSpeech.nStart), formatClock(oSpeech.nStart), "Spoken", oSpeech.sText });
            }
            lLines.Sort(delegate(string[] asOne, string[] asTwo)
            {
                double nOne = 0.0;
                double nTwo = 0.0;
                double.TryParse(asOne[0], NumberStyles.Any, CultureInfo.InvariantCulture, out nOne);
                double.TryParse(asTwo[0], NumberStyles.Any, CultureInfo.InvariantCulture, out nTwo);
                return nOne.CompareTo(nTwo);
            });
            StreamWriter fDoc = new StreamWriter(sPath, false, new UTF8Encoding(true));
            fDoc.WriteLine("# " + sSourceName + ", described and transcribed");
            fDoc.WriteLine("");
            fDoc.WriteLine("- Running time: " + formatClock(nDuration));
            fDoc.WriteLine("- Entries: " + lLines.Count.ToString());
            fDoc.WriteLine("- Generated: " + DateTime.Now.ToString("d MMMM yyyy"));
            fDoc.WriteLine("");
            fDoc.WriteLine("What was said and what was there to be seen, in the order it happens. "
                         + "Each entry gives its time, then either Spoken, for the film's own words, "
                         + "or Description, for what a sighted viewer would have seen.");
            fDoc.WriteLine("");
            double nChapter = -1.0;
            foreach (string[] asLine in lLines)
            {
                double nAt = 0.0;
                double.TryParse(asLine[0], NumberStyles.Any, CultureInfo.InvariantCulture, out nAt);
                double nThis = Math.Floor(nAt / nDefaultChapter) * nDefaultChapter;
                if (nThis != nChapter)
                {
                    nChapter = nThis;
                    fDoc.WriteLine("");
                    fDoc.WriteLine("## " + formatClock(nChapter) + " to " + formatClock(Math.Min(nChapter + nDefaultChapter, nDuration)));
                    fDoc.WriteLine("");
                }
                fDoc.WriteLine("- " + asLine[1] + " " + asLine[2] + ": " + asLine[3]);
            }
            fDoc.Close();
        }

        static void saveReadable(List<Moment> lMoments, string sOutputDir, string sWorkDir, string sSourceName, double nDuration)
        {
            if (lMoments.Count == 0) return;
            writeCache(lMoments, Path.Combine(sWorkDir, sDefaultJsonName), false, lLastGaps, dLastSignature);
            writeVtt(lMoments, Path.Combine(sWorkDir, sDefaultVttName));
            writeMarkdown(lMoments, Path.Combine(sOutputDir, sDefaultMarkdownName), sSourceName, nDuration);
        }

        // ---------- the dialog ----------
        //
        // Built with Lbc.cs, the shared layout-by-code module used by DbDo,
        // EdSharp, FileDir and urlFido. LbcDialog supplies the Help button
        // itself, and Escape and Enter behave as Windows expects, so OK and
        // Cancel carry no mnemonic. Every control here is one Jamal named; the
        // remaining settings stay on the command line until he says otherwise.
        // Where Windows expects a program like this to look and to write.
        //
        // urlFido puts it well in its own comment: what must be avoided is
        // falling back on whatever folder the program happened to start in,
        // which drops results among the sources. HomerScribe deals in video,
        // so its known folder is Videos rather than Documents, with Documents
        // and only then the current directory behind it.
        static string defaultVideoFolder()
        {
            try
            {
                string sVideos = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                if (sVideos != "" && Directory.Exists(sVideos)) return sVideos;
            }
            catch (Exception)
            {
            }
            try
            {
                string sDocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (sDocs != "" && Directory.Exists(sDocs)) return sDocs;
            }
            catch (Exception)
            {
            }
            return Directory.GetCurrentDirectory();
        }

        // A browse dialog should open where the person already is. Whatever the
        // field holds wins; the known folder is only the fallback.
        static string initialBrowseFolder(string sFieldText)
        {
            try
            {
                string sText = (sFieldText == null ? "" : sFieldText).Trim();
                List<string> lItems = splitPaths(sText);
                if (lItems.Count > 0)
                {
                    string sOne = lItems[0].Trim('"');
                    string sFolder = Directory.Exists(sOne) ? sOne : Path.GetDirectoryName(sOne);
                    if (sFolder != null && sFolder != "" && Directory.Exists(sFolder)) return sFolder;
                }
            }
            catch (Exception)
            {
            }
            return defaultVideoFolder();
        }

        static bool showDialog()
        {
            while (true)
            {
                string sButton = "";
                string sSources = text("source-paths");
                string sOutput = text("output-dir");
                if (sOutput == "") sOutput = defaultVideoFolder();
                bool bForce = flag("force");
                bool bLogSession = flag("log-session");
                bool bUseConfig = flag("use-configuration");
                bool bAudioOnly = flag("audio-only");
                bool bDescribe = flag("describe");
                bool bTranscribe = flag("transcribe");
                bool bViewOutput = flag("view-output");
                bool bWebContext = flag("web-context");
                closeDialog();
                oLiveDialog = new LbcDialog("HomerScribe", null);
                {
                    LbcDialog oDialog = oLiveDialog;
                    // Band one: what to work on.
                    oDialog.addBand();
                    TextBox oSourceBox = oDialog.addInputBox("&Source paths:", sSources,
                        "One or more files, wildcard patterns, or web addresses to download from, separated by spaces. " +
                        "Put double quotes around any item containing a space.");
                    Button oBrowseButton = oDialog.addButton("&Browse source...",
                        "Choose a file to work on.");
                    oDialog.endBand();

                    // Band two: what to do with it.
                    oDialog.addBand();
                    CheckBox oTranscribeBox = oDialog.addCheckBox("&Transcribe audio", bTranscribe,
                        "Write down what is said, as transcribed.md. With Describe video also ticked, scribed.md is written too: " +
                        "the words and the descriptions interleaved in the order they happen.");
                    CheckBox oDescribeBox = oDialog.addCheckBox("&Describe video", bDescribe,
                        "Describe what happens on screen and write a described copy of the film, plus described.md, the script to read.");
                    CheckBox oAudioBox = oDialog.addCheckBox("&Audio only", bAudioOnly,
                        "Produce sound only: one mp3 holding the film's own audio with the descriptions mixed into it, and no video. " +
                        "Far smaller than the film, quicker to make, and enough when the picture is of no use to the listener.");
                    CheckBox oWebBox = oDialog.addCheckBox("&Web context", bWebContext,
                        "Learn what the video is before describing it. For a web address, the page's own title and description are used. " +
                        "For a file, if it carries a title, Wikipedia is asked about that title and the answer is used only if it clearly matches.");
                    oDialog.endBand();

                    // Band three: where the results go.
                    oDialog.addBand();
                    TextBox oOutputBox = oDialog.addInputBox("&Output directory:", sOutput,
                        "Where each source's folder of results is created. Starts at your Videos folder. " +
                        "Cleared, each folder is created beside its own source instead.");
                    Button oChooseButton = oDialog.addButton("&Choose output...",
                        "Choose the directory to write results into.");
                    oDialog.endBand();

                    oBrowseButton.Click += delegate(object oSender, EventArgs oEvent)
                    {
                        OpenFileDialog oPicker = new OpenFileDialog();
                        oPicker.Title = "Choose a file to work on";
                        try
                        {
                            oPicker.InitialDirectory = initialBrowseFolder(oSourceBox.Text);
                        }
                        catch (Exception)
                        {
                        }
                        oPicker.Filter = "Video and audio|*.mkv;*.mp4;*.avi;*.mov;*.webm;*.mpg;*.mpeg;*.m4v;*.wmv;*.mp3;*.wav;*.m4a;*.flac;*.ogg|All files|*.*";
                        oPicker.Multiselect = true;
                        if (oPicker.ShowDialog(oDialog.form) == DialogResult.OK)
                        {
                            string sPicked = "";
                            foreach (string sOne in oPicker.FileNames)
                            {
                                if (sPicked != "") sPicked = sPicked + " ";
                                sPicked = sPicked + quotedIfSpaced(sOne);
                            }
                            oSourceBox.Text = sPicked;
                        }
                    };

                    oChooseButton.Click += delegate(object oSender, EventArgs oEvent)
                    {
                        FolderBrowserDialog oFolder = new FolderBrowserDialog();
                        oFolder.Description = "Choose the directory to write results into";
                        try
                        {
                            oFolder.SelectedPath = initialBrowseFolder(oOutputBox.Text);
                        }
                        catch (Exception)
                        {
                        }
                        if (oFolder.ShowDialog(oDialog.form) == DialogResult.OK) oOutputBox.Text = oFolder.SelectedPath;
                    };

                    // Band four: the standard controls of a Homer Tools dialog.
                    oDialog.addSeparator();
                    CheckBox oForceBox = oDialog.addCheckBox("&Force overwrite", bForce,
                        "Do the work again, ignoring anything an earlier run had already written.");
                    CheckBox oLogBox = oDialog.addCheckBox("&Log session", bLogSession,
                        "Keep the run log with the results rather than out of the way under your application data.");
                    CheckBox oConfigBox = oDialog.addCheckBox("&Use configuration", bUseConfig,
                        "Load these settings at startup and save them on OK, in " + configPath() + ".");
                    CheckBox oViewBox = oDialog.addCheckBox("&View output", bViewOutput,
                        "Open the folder holding the results when the run finishes.");

                    logMessage("Dialog buttons: Help, Default settings, OK, Cancel", "INFO", "");
                    sButton = oDialog.runWithButtons(new string[] { "Help", "Default settings", "OK", "Cancel" }, false);
                    logMessage("Dialog answered with: " + (sButton == null ? "(nothing)" : sButton), "INFO", "");

                    sSources = (oSourceBox.Text == null ? "" : oSourceBox.Text).Trim();
                    sOutput = (oOutputBox.Text == null ? "" : oOutputBox.Text).Trim();
                    bForce = oForceBox.Checked;
                    bLogSession = oLogBox.Checked;
                    bUseConfig = oConfigBox.Checked;
                    bAudioOnly = oAudioBox.Checked;
                    bDescribe = oDescribeBox.Checked;
                    bTranscribe = oTranscribeBox.Checked;
                    bViewOutput = oViewBox.Checked;
                    bWebContext = oWebBox.Checked;
                }

                if (sButton == null || sButton == "" || sButton == "Cancel")
                {
                    closeDialog();
                    return false;
                }

                // Put everything back as it was out of the box and show the
                // dialog again. The fields are restored rather than emptied,
                // because the seeding happens once before the dialog opens: a
                // blanked field would simply stay blank.
                if (string.Compare(sButton, "Default settings", true) == 0)
                {
                    dParams["source-paths"].sValue = sDefaultSource;
                    dParams["output-dir"].sValue = "";
                    foreach (string sName in asRemembered)
                    {
                        if (dParams[sName].sKind == "flag") dParams[sName].sValue = "no";
                    }
                    forgetConfig();
                    logMessage("Default settings restored.", "INFO", "");
                    Say.say("Default settings restored");
                    continue;
                }

                dParams["source-paths"].sValue = sSources;
                dParams["output-dir"].sValue = sOutput;
                dParams["force"].sValue = bForce ? "yes" : "no";
                dParams["log-session"].sValue = bLogSession ? "yes" : "no";
                dParams["use-configuration"].sValue = bUseConfig ? "yes" : "no";
                dParams["audio-only"].sValue = bAudioOnly ? "yes" : "no";
                dParams["describe"].sValue = bDescribe ? "yes" : "no";
                dParams["transcribe"].sValue = bTranscribe ? "yes" : "no";
                dParams["view-output"].sValue = bViewOutput ? "yes" : "no";
                dParams["web-context"].sValue = bWebContext ? "yes" : "no";

                if (sSources == "")
                {
                    MessageBox.Show("Give at least one file or web address.", "HomerScribe",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    continue;
                }
                if (!bDescribe && !bTranscribe)
                {
                    MessageBox.Show("Tick Describe video, or Transcribe audio, or both."
                        + Environment.NewLine + Environment.NewLine
                        + "Describe video watches the picture and says what happens." + Environment.NewLine
                        + "Transcribe audio writes down what is said." + Environment.NewLine
                        + "Both together also write the two interleaved, in the order they happen.",
                        "HomerScribe", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    continue;
                }
                if (bUseConfig) saveConfig();
                // Answered, so the dialog stays up with its controls disabled,
                // as the window this run belongs to.
                keepDialogUp();
                return true;
            }
        }

        // ---------- the configuration file ----------

        static string configPath()
        {
            return Path.Combine(appDataFolder(), "HomerScribe.ini");
        }

        // A settings file left beside the program by an older build, or put
        // there in a development folder, is still read. It is never written there.
        static string configPathToRead()
        {
            if (File.Exists(configPath())) return configPath();
            string sBeside = Path.Combine(exeFolder(), "HomerScribe.ini");
            if (File.Exists(sBeside)) return sBeside;
            return configPath();
        }

        static void loadConfig()
        {
            string sPath = configPathToRead();
            if (!File.Exists(sPath)) return;
            try
            {
                foreach (InixCodec.Section oSection in InixCodec.read(sPath))
                {
                    foreach (InixCodec.Pair oPair in oSection.Pairs)
                    {
                        if (!dParams.ContainsKey(oPair.Key)) continue;
                        if (!isRemembered(oPair.Key)) continue;
                        // The command line wins over the file.
                        if (dParams[oPair.Key].bGiven) continue;
                        dParams[oPair.Key].sValue = oPair.Value;
                    }
                }
                logMessage("Settings loaded from " + sPath, "INFO", "");
            }
            catch (Exception oError)
            {
                logMessage("The configuration could not be read: " + oError.Message, "ERROR");
            }
        }

        // Only what the dialog offers is remembered. Saving every setting
        // freezes the built-in defaults forever: a file written by an older
        // build goes on handing back its idea of a setting long after the
        // default has changed, with nothing on screen to say so.
        static readonly string[] asRemembered = new string[] {
            "source-paths", "output-dir", "describe", "transcribe", "force", "log-session", "use-configuration", "view-output", "audio-only", "web-context"
        };

        static bool isRemembered(string sName)
        {
            foreach (string sOne in asRemembered)
            {
                if (sOne == sName) return true;
            }
            return false;
        }

        static bool savedSaysUseConfiguration()
        {
            if (!File.Exists(configPathToRead())) return false;
            try
            {
                foreach (InixCodec.Section oSection in InixCodec.read(configPathToRead()))
                {
                    foreach (InixCodec.Pair oPair in oSection.Pairs)
                    {
                        if (oPair.Key == "use-configuration") return oPair.Value == "yes";
                    }
                }
            }
            catch (Exception)
            {
            }
            return false;
        }

        // Forget what was remembered, so "Default settings" really does return
        // the program to how it arrives.
        static void forgetConfig()
        {
            try
            {
                if (File.Exists(configPath())) File.Delete(configPath());
                logMessage("The settings file was removed: " + configPath(), "INFO", "");
            }
            catch (Exception oError)
            {
                logMessage("The settings file could not be removed: " + oError.Message, "ERROR");
            }
        }

        static void saveConfig()
        {
            string sPath = configPath();
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(sPath));
                foreach (KeyValuePair<string, Param> oPair in dParams)
                {
                    if (!isRemembered(oPair.Key)) continue;
                    InixCodec.writeValue(sPath, "Settings", oPair.Key, oPair.Value.sValue);
                }
                logMessage("Settings saved to " + sPath, "INFO", "Settings saved.");
            }
            catch (Exception oError)
            {
                logMessage("The settings could not be saved to " + sPath + ": " + oError.Message, "ERROR");
                if (bGuiMode) MessageBox.Show("The settings could not be saved:" + Environment.NewLine + Environment.NewLine
                    + sPath + Environment.NewLine + Environment.NewLine + oError.Message,
                    "HomerScribe", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ---------- the run ----------

        // Run a long program, reporting the odd line of its output so the
        // screen is not silent for minutes. Used for downloads.
        static double nWholeLength = 0.0;

        static int runStreamed(string sProgram, string sArguments, string sLabel)
        {
            logMessage("Command: " + sProgram + " " + sArguments, "CMD");
            DateTime dtBegan = DateTime.Now;
            DateTime dtLast = DateTime.Now;
            DateTime dtSaid = DateTime.MinValue;
            double nHeardTo = 0.0;
            Process oProcess = new Process();
            oProcess.StartInfo.FileName = sProgram;
            oProcess.StartInfo.Arguments = sArguments;
            oProcess.StartInfo.UseShellExecute = false;
            oProcess.StartInfo.RedirectStandardOutput = true;
            oProcess.StartInfo.RedirectStandardError = true;
            oProcess.StartInfo.CreateNoWindow = true;
            StringBuilder oErr = new StringBuilder();
            oProcess.ErrorDataReceived += delegate(object oSender, DataReceivedEventArgs oEvent)
            {
                if (oEvent.Data != null) oErr.AppendLine(oEvent.Data);
            };
            try
            {
                oProcess.Start();
            }
            catch (Exception oError)
            {
                logMessage("Could not start " + sProgram + ": " + oError.Message, "ERROR");
                return -1;
            }
            oProcess.BeginErrorReadLine();
            while (true)
            {
                string sLine = oProcess.StandardOutput.ReadLine();
                if (sLine == null) break;
                string sTrimmed = sLine.Trim();
                if (sTrimmed != "")
                {
                    logMessage(sTrimmed, "INFO", "");
                    // Each stretch goes to the log as it is heard. It is not
                    // announced here: the words are played out in order with the
                    // descriptions, in the pass that follows.
                    Match oSaid = Regex.Match(sTrimmed, @"^\[(\d+):(\d\d):(\d\d)[^\]]*\]\s*(.*)$");
                    if (sLabel == "Listening" && oSaid.Success && oSaid.Groups[4].Value.Trim() != "")
                    {
                        nHeardTo = double.Parse(oSaid.Groups[1].Value, CultureInfo.InvariantCulture) * 3600.0
                                 + double.Parse(oSaid.Groups[2].Value, CultureInfo.InvariantCulture) * 60.0
                                 + double.Parse(oSaid.Groups[3].Value, CultureInfo.InvariantCulture);
                        string sWhen = int.Parse(oSaid.Groups[1].Value).ToString() + ":" + oSaid.Groups[2].Value + ":" + oSaid.Groups[3].Value;
                        dialogSays("transcribing, " + spokenPosition(nHeardTo, nWholeLength));
                        pumpDialog();
                        logMessage("Transcribing  " + sWhen + "  " + oSaid.Groups[4].Value.Trim(), "INFO", "");
                    }
                    if (DateTime.Now.Subtract(dtLast).TotalSeconds >= iDefaultScanReport)
                    {
                        dtLast = DateTime.Now;
                        // Whisper writes each stretch as "[00:01:23.000 -->
                        // 00:01:29.000]   the words". Report where it has
                        // reached, in the same shape as a description line, and
                        // leave the rest to the log.
                        Match oAt = Regex.Match(sTrimmed, @"^\[(\d+):(\d\d):(\d\d)");
                        string sWhere = "";
                        if (oAt.Success) sWhere = "  " + int.Parse(oAt.Groups[1].Value).ToString() + ":" + oAt.Groups[2].Value + ":" + oAt.Groups[3].Value;
                        dialogSays(announceKindFor(sLabel).ToLower() + ", " + spokenPosition(nHeardTo, nWholeLength));
                        pumpDialog();
                        logMessage("", "INFO", sLabel + sWhere);
                        // Said aloud less often than it is written, because each
                        // announcement holds the screen for a couple of seconds
                        // and this pass can run for a quarter of an hour.
                        if (DateTime.Now.Subtract(dtSaid).TotalSeconds >= iDefaultSpokenReport)
                        {
                            dtSaid = DateTime.Now;
                            announce(announceKindFor(sLabel), nHeardTo, nWholeLength, "");
                        }
                    }
                }
            }
            oProcess.WaitForExit();
            // Kept, not merely logged: whoever called needs to be able to say
            // WHY it failed, and until now the reason was written down and
            // thrown away.
            sLastStreamedTrouble = oErr.ToString();
            logMessage("Exit code " + oProcess.ExitCode.ToString() + " after " + num(DateTime.Now.Subtract(dtBegan).TotalSeconds) + " seconds", "CMD");
            if (oProcess.ExitCode != 0) logMessage("Error output: " + tail(oErr.ToString(), 1500), "ERROR", "");
            return oProcess.ExitCode;
        }

        // Expand a source that names several files at once. A star or a question
        // mark is a pattern, not a path, and Path.GetFullPath throws on one.
        static readonly string[] asMediaKinds = new string[] {
            ".mkv", ".mp4", ".m4v", ".avi", ".mov", ".webm", ".mpg", ".mpeg", ".wmv", ".flv", ".ts", ".m2ts", ".ogv",
            ".mp3", ".wav", ".m4a", ".flac", ".ogg", ".oga", ".opus", ".aac", ".wma", ".aiff", ".aif"
        };

        static bool looksLikeMedia(string sPath)
        {
            string sKind = Path.GetExtension(sPath).ToLower();
            foreach (string sOne in asMediaKinds)
            {
                if (sOne == sKind) return true;
            }
            return false;
        }

        static List<string> expandPattern(string sSource)
        {
            List<string> lFound = new List<string>();
            if (sSource.IndexOf('*') < 0 && sSource.IndexOf('?') < 0)
            {
                lFound.Add(sSource);
                return lFound;
            }
            string sFolder = "";
            string sPattern = sSource;
            try
            {
                sFolder = Path.GetDirectoryName(sSource);
                sPattern = Path.GetFileName(sSource);
            }
            catch (Exception oError)
            {
                logMessage("That does not look like a path: " + sSource + " (" + oError.Message + ")", "ERROR");
                return lFound;
            }
            if (sFolder == "") sFolder = Directory.GetCurrentDirectory();
            if (!Directory.Exists(sFolder))
            {
                logMessage("No such folder: " + sFolder, "ERROR");
                return lFound;
            }
            string[] asFiles = new string[0];
            try
            {
                asFiles = Directory.GetFiles(sFolder, sPattern);
            }
            catch (Exception oError)
            {
                logMessage("The pattern " + sSource + " could not be read: " + oError.Message, "ERROR");
                return lFound;
            }
            Array.Sort(asFiles);
            int iNotMedia = 0;
            foreach (string sFile in asFiles)
            {
                if (!looksLikeMedia(sFile))
                {
                    iNotMedia = iNotMedia + 1;
                    logMessage("  Passing over " + Path.GetFileName(sFile) + ": not a video or a recording.", "INFO", "");
                    continue;
                }
                lFound.Add(sFile);
            }
            if (iNotMedia > 0) logMessage(iNotMedia.ToString() + " file(s) matching the pattern are not video or audio and were passed over.",
                                          "INFO", iNotMedia.ToString() + " matching file(s) are not video or audio and were passed over.");
            logMessage(sSource + " matches " + lFound.Count.ToString() + " files",
                       "INFO", sSource + " matches " + lFound.Count.ToString() + " files.");
            if (lFound.Count == 0) logMessage("Nothing matched " + sSource, "ERROR");
            return lFound;
        }

        // A playlist address names many videos, and yt-dlp is asked which. The
        // download itself passes --no-playlist, so without this a playlist would
        // quietly yield only its first video -- the worst kind of failure,
        // because it looks like success.
        static bool looksLikePlaylist(string sAddress)
        {
            if (sAddress.IndexOf("/playlist", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (Regex.IsMatch(sAddress, @"[?&]list=", RegexOptions.IgnoreCase) && sAddress.IndexOf("watch", StringComparison.OrdinalIgnoreCase) < 0) return true;
            return false;
        }

        static List<string> expandPlaylist(string sAddress)
        {
            List<string> lFound = new List<string>();
            string sYtDlp = findTool("yt-dlp");
            if (sYtDlp == "")
            {
                logMessage("yt-dlp was not found, so the playlist cannot be read.", "ERROR");
                return lFound;
            }
            logMessage("Reading the playlist at " + sAddress, "INFO", "Reading the playlist.");
            announce("Initializing", -1.0, 1.0, "Reading the playlist.");
            string sOut = "";
            string sErr = "";
            int iCode = runCommand(sYtDlp, "--flat-playlist --no-warnings --print " + quoted("%(url)s") + " " + quoted(sAddress), out sOut, out sErr);
            if (iCode != 0 && sOut.Trim() == "")
            {
                logMessage("The playlist could not be read: " + tail(sErr.Trim(), 400), "ERROR");
                return lFound;
            }
            foreach (string sLine in sOut.Replace("\r\n", "\n").Split('\n'))
            {
                string sTrim = sLine.Trim();
                if (sTrim.StartsWith("http")) lFound.Add(sTrim);
            }
            logMessage("The playlist holds " + lFound.Count.ToString() + " videos.",
                       "INFO", "The playlist holds " + lFound.Count.ToString() + " videos.");
            if (lFound.Count > iDefaultBigPlaylist)
            {
                string sBig = "That playlist holds " + lFound.Count.ToString() + " videos. Describing them all would take days. "
                            + "Consider putting just the ones you want in a text file, one address per line, and giving that instead.";
                logMessage(sBig, "HINT");
                announce("Initializing", -1.0, 1.0, sBig);
            }
            return lFound;
        }

        // A plain text file naming one source per line. Handing HomerScribe a
        // list is easier than typing sixteen paths, and a list is what people
        // already keep.
        static List<string> readListFile(string sPath)
        {
            List<string> lFound = new List<string>();
            try
            {
                foreach (string sLine in File.ReadAllLines(sPath))
                {
                    string sTrim = sLine.Trim();
                    if (sTrim == "") continue;
                    if (sTrim.StartsWith("#") || sTrim.StartsWith(";")) continue;
                    foreach (string sOne in splitOneLine(sTrim)) lFound.Add(sOne);
                }
            }
            catch (Exception oError)
            {
                logMessage("The list " + sPath + " could not be read: " + oError.Message, "ERROR");
                return lFound;
            }
            logMessage("The list " + sPath + " names " + lFound.Count.ToString() + " sources.",
                       "INFO", Path.GetFileName(sPath) + " names " + lFound.Count.ToString() + " sources.");
            return lFound;
        }

        // Fetch a video from a web page.
        //
        // This is handed to yt-dlp rather than done in C#. Extracting a video
        // from YouTube is not a matter of reading a page: the addresses are
        // signed by obfuscated JavaScript that has to be run, the signing
        // changes without notice, and formats are negotiated per video. yt-dlp
        // tracks all of that and is updated most weeks. A library inside
        // HomerScribe would have to be maintained against a moving target
        // that has nothing to do with audio description, and would break
        // silently on a Tuesday. Calling the program that already solves the
        // problem is the smaller and more honest dependency.
        //
        // Two details matter. --print implies --simulate unless --no-simulate
        // is given, so without it yt-dlp would report a path and download
        // nothing. And the best video and best audio arrive as separate
        // streams that ffmpeg merges, so yt-dlp is told where ffmpeg is,
        // rather than being left to find it on the PATH.
        // yt-dlp's complaint, reduced to the sentence a person needs. Its output
        // carries warnings about JavaScript runtimes and suchlike that are not
        // the reason for anything.
        static string whyItFailed(string sErr)
        {
            foreach (string sLine in (sErr == null ? "" : sErr).Replace("\r\n", "\n").Split('\n'))
            {
                string sTrim = sLine.Trim();
                if (sTrim.IndexOf("ERROR:", StringComparison.OrdinalIgnoreCase) < 0) continue;
                string sSaid = sTrim.Substring(sTrim.IndexOf("ERROR:", StringComparison.OrdinalIgnoreCase) + 6).Trim();
                // Drop the identifier yt-dlp puts in front of its message.
                sSaid = Regex.Replace(sSaid, @"^\[[^\]]+\]\s*", "");
                sSaid = Regex.Replace(sSaid, @"^[A-Za-z0-9_-]{6,}:\s*", "");
                if (sSaid.Length > 200) sSaid = sSaid.Substring(0, 200);
                if (sSaid != "") return sSaid;
            }
            return "";
        }

        static string fetchFromWeb(string sAddress, string sFolder, string sFfmpeg)
        {
            string sYtDlp = findTool("yt-dlp");
            if (sYtDlp == "")
            {
                logMessage("yt-dlp was not found, so " + sAddress + " cannot be downloaded.", "ERROR");
                logMessage("Install it with:  winget install yt-dlp.yt-dlp", "HINT");
                if (bGuiMode) MessageBox.Show("yt-dlp is needed to download from a web address, and was not found.\r\n\r\n" +
                    "Install it with:  winget install yt-dlp.yt-dlp", "HomerScribe", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return "";
            }
            Directory.CreateDirectory(sFolder);
            string sPathFile = Path.Combine(sFolder, "downloaded.txt");
            try
            {
                if (File.Exists(sPathFile)) File.Delete(sPathFile);
            }
            catch (Exception)
            {
            }
            // What is it called? Asked first, so the video can be put where its
            // results will go and so the listener hears a title rather than an
            // address.
            // TWO things are asked for, and the distinction matters. The title is
            // what the listener should hear. The FILENAME is what yt-dlp will
            // actually write, and only yt-dlp knows that: --restrict-filenames
            // turns "The Africans: A Triple Heritage - Program 1" into
            // "The_Africans_-_A_Triple_Heritage_-_Program_1". Sanitising the
            // title here instead produced a folder by one name holding a file by
            // another, and then a second folder was made for the file.
            string sTitle = "";
            string sStem = "";
            string sNameOut = "";
            string sNameErr = "";
            runCommand(sYtDlp, "--no-playlist --no-warnings --restrict-filenames"
                             + " --print " + quoted("%(title)s")
                             + " --print filename"
                             + " -o " + quoted("%(title)s.%(ext)s")
                             + " " + quoted(sAddress), out sNameOut, out sNameErr);
            List<string> lSaid = new List<string>();
            foreach (string sLine in sNameOut.Replace("\r\n", "\n").Split('\n'))
            {
                if (sLine.Trim() != "") lSaid.Add(sLine.Trim());
            }
            if (lSaid.Count > 0) sTitle = lSaid[0];
            if (lSaid.Count > 1)
            {
                try
                {
                    sStem = Path.GetFileNameWithoutExtension(lSaid[1]);
                }
                catch (Exception)
                {
                    sStem = "";
                }
            }
            if (sStem != "")
            {
                logMessage("It is called \"" + sTitle + "\" and will be written as " + sStem, "INFO", "");
                // The folder takes the name yt-dlp will give the file, so the
                // two agree and the results land beside the video.
                sFolder = Path.Combine(sFolder, sStem);
                Directory.CreateDirectory(sFolder);
                sPathFile = Path.Combine(sFolder, "downloaded.txt");
            }
            string sSaying = sTitle == "" ? sAddress : sTitle;
            logMessage("Downloading " + sAddress + " (" + sSaying + ") with " + sYtDlp, "INFO", "Downloading " + sSaying);
            announce("Initializing", -1.0, 1.0, "Downloading " + sSaying);
            string sCookies = "";
            if (text("browser-cookies") != "") sCookies = " --cookies-from-browser " + text("browser-cookies");
            string sArguments = "--no-playlist --no-simulate --newline --restrict-filenames" + sCookies
                              + " --merge-output-format mkv"
                              + " --ffmpeg-location " + quoted(Path.GetDirectoryName(sFfmpeg))
                              + " --print-to-file after_move:filepath " + quoted(sPathFile)
                              + " -f " + quoted("bv*+ba/b")
                              + " -o " + quoted(Path.Combine(sFolder, "%(title)s.%(ext)s"))
                              + " " + quoted(sAddress);
            int iCode = runStreamed(sYtDlp, sArguments, "Downloading");
            if (iCode != 0)
            {
                sLastFetchTrouble = whyItFailed(sLastStreamedTrouble);
                logMessage("The video could not be fetched. " + (sLastFetchTrouble == "" ? "yt-dlp gave no reason." : "yt-dlp said: " + sLastFetchTrouble),
                           "ERROR", "Could not fetch that video. " + sLastFetchTrouble);
                return "";
            }
            string sPath = "";
            try
            {
                foreach (string sLine in File.ReadAllLines(sPathFile))
                {
                    if (sLine.Trim() != "") sPath = sLine.Trim();
                }
                File.Delete(sPathFile);
            }
            catch (Exception oError)
            {
                logMessage("The downloaded file could not be located: " + oError.Message, "ERROR");
                return "";
            }
            if (sPath == "" || !File.Exists(sPath))
            {
                logMessage("The download finished but the file could not be located.", "ERROR");
                return "";
            }
            logMessage("Downloaded to " + sPath, "INFO", "Downloaded " + Path.GetFileName(sPath));
            return sPath;
        }

        static int run()
        {
            DateTime dtRunBegan = DateTime.Now;
            lResults.Clear();
            lFailures.Clear();
            bool bReady = checkEnvironment();
            if (flag("check"))
            {
                logMessage("Check finished. Prerequisites " + (bReady ? "passed" : "FAILED"),
                           "INFO", "Check finished. Prerequisites " + (bReady ? "passed." : "FAILED."));
                return bReady ? 0 : 1;
            }
            if (!bReady) return 1;
            if (!flag("describe") && !flag("transcribe"))
            {
                string sNothing = "Neither job was asked for. Use --describe, --transcribe, or both.";
                logMessage(sNothing, "ERROR");
                if (bGuiMode) sayToUser(sNothing, "HomerScribe", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return iNothingToDo;
            }
            List<string> lGiven = splitPaths(text("source-paths"));
            List<string> lSources = new List<string>();
            foreach (string sGiven in lGiven)
            {
                if (sGiven.StartsWith("http://") || sGiven.StartsWith("https://"))
                {
                    if (looksLikePlaylist(sGiven))
                    {
                        foreach (string sOne in expandPlaylist(sGiven)) lSources.Add(sOne);
                        continue;
                    }
                    lSources.Add(sGiven);
                    continue;
                }
                // A text file is a list of what to work on, not something to
                // describe. Nothing else could be meant by handing over a .txt.
                if (sGiven.ToLower().EndsWith(".txt") && File.Exists(sGiven))
                {
                    foreach (string sListed in readListFile(sGiven))
                    {
                        if (sListed.StartsWith("http://") || sListed.StartsWith("https://")) lSources.Add(sListed);
                        else foreach (string sOne in expandPattern(sListed)) lSources.Add(sOne);
                    }
                    continue;
                }
                foreach (string sOne in expandPattern(sGiven)) lSources.Add(sOne);
            }
            if (lSources.Count == 0)
            {
                string sTried = text("source-paths").Trim();
                string sSaid = sTried == ""
                    ? "No source was given, so there is nothing to describe."
                    : "Nothing was found matching:" + Environment.NewLine + Environment.NewLine + sTried;
                logMessage(sSaid.Replace(Environment.NewLine, " "), "ERROR");
                if (!bGuiMode) logMessage("Name a video file or a YouTube address, or run HomerScribe with no arguments for the dialog.", "HINT");
                if (bGuiMode) sayToUser(sSaid, "HomerScribe", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // Nothing to do is not a failure to exit on: from the dialog it
                // means a mistyped path, and what is wanted next is the dialog
                // back with the text still in it.
                return iNothingToDo;
            }
            int iWorst = 0;
            int iAt = 0;
            int iSkippedWhole = 0;
            int iDescribedWhole = 0;
            sLastSkippedFolder = "";
            foreach (string sSource in lSources)
            {
                iAt = iAt + 1;
                iSourceAt = iAt;
                iSourceCount = lSources.Count;
                if (lSources.Count > 1) logMessage("Source " + iAt.ToString() + " of " + lSources.Count.ToString() + ": " + sSource,
                                                   "INFO", "Source " + iAt.ToString() + " of " + lSources.Count.ToString() + ": " + sSource);
                string sPath = sSource;
                if (sSource.StartsWith("http://") || sSource.StartsWith("https://"))
                {
                    // The root the results go under. fetchFromWeb makes the
                    // per-video folder inside it once it knows the title, so the
                    // video and its results share one place.
                    string sFolder = text("output-dir");
                    if (sFolder == "") sFolder = Path.Combine(appDataFolder(), "downloads");
                    sPath = fetchFromWeb(sSource, sFolder, findTool("ffmpeg"));
                    if (sPath == "")
                    {
                        lFailures.Add(sSource + (sLastFetchTrouble == "" ? "" : Environment.NewLine + "    " + sLastFetchTrouble));
                        iWorst = 1;
                        continue;
                    }
                }
                string sFull = sPath;
                try
                {
                    sFull = Path.GetFullPath(sPath);
                }
                catch (Exception oError)
                {
                    logMessage("That path cannot be used: " + sPath + " (" + oError.Message + ")", "ERROR");
                    iWorst = 1;
                    continue;
                }
                int iOne = runOne(sFull, sSource.StartsWith("http") ? sSource : "");
                if (iOne == iAlreadyDone) iSkippedWhole = iSkippedWhole + 1;
                if (iOne == 0) iDescribedWhole = iDescribedWhole + 1;
                if (iOne != 0 && iOne != iAlreadyDone) iWorst = iOne;
            }
            // Every one already done. Not a failure, but not a result either,
            // and a run that ends without a word looks like one that never
            // started.
            if (iDescribedWhole == 0 && iSkippedWhole > 0)
            {
                string sSaid = iSkippedWhole == 1
                    ? "That video has already been described, so there was nothing to do."
                    : "All " + iSkippedWhole.ToString() + " of those videos have already been described, so there was nothing to do.";
                sSaid = sSaid + Environment.NewLine + Environment.NewLine
                      + (bGuiMode ? "Tick Force overwrite to describe them again." : "Pass --force to describe them again.");
                logMessage(sSaid.Replace(Environment.NewLine, " "), "INFO", bGuiMode ? "" : sSaid);
                if (flag("view-output") && sLastSkippedFolder != "") showFolder(sLastSkippedFolder);
                if (bGuiMode) sayToUser(sSaid, "HomerScribe", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return iNothingToDo;
            }
            if (iSkippedWhole > 0) logMessage(iSkippedWhole.ToString() + " already described and skipped; " + iDescribedWhole.ToString() + " described.",
                                              "INFO", iSkippedWhole.ToString() + " already described and skipped.");
            dialogSays("finished");
            pumpDialog();
            flushAnnouncements();
            showResults(iDescribedWhole, iSkippedWhole, DateTime.Now.Subtract(dtRunBegan));
            return iWorst;
        }

        // Confirm the programs and the model are in place. Done once, whatever
        // the number of sources.
        // Only what the ticked jobs need. Transcribing wants ffmpeg and Whisper;
        // describing wants ffmpeg, a voice and the vision model. Making a
        // transcribe-only user install five and a half gigabytes of vision model
        // would be worse than a separate program, which is the whole argument
        // against merging.
        static bool checkEnvironment()
        {
            string sFfmpeg = findTool("ffmpeg");
            if (sFfmpeg == "")
            {
                logMessage("ffmpeg was not found beside this program, in --ffmpeg-dir, or on the PATH.", "ERROR");
                logMessage("Install it with:  winget install Gyan.FFmpeg   then open a new terminal.", "HINT");
                return false;
            }
            logMessage("Found ffmpeg at " + sFfmpeg, "INFO", "");
            string sOut = "";
            string sErr = "";
            runCommand(sFfmpeg, "-version", out sOut, out sErr);
            string sYtDlp = findTool("yt-dlp");
            if (sYtDlp == "") logMessage("yt-dlp was not found. Files still work; web addresses do not.", "INFO", "");
            if (sYtDlp != "")
            {
                logMessage("Found yt-dlp at " + sYtDlp, "INFO", "");
                string sVerOut = "";
                string sVerErr = "";
                runCommand(sYtDlp, "--version", out sVerOut, out sVerErr);
                logMessage("  yt-dlp version " + (sVerOut + sVerErr).Trim(), "INFO", "");
            }

            bool bReady = true;
            if (flag("transcribe") || (flag("describe") && flag("speech")))
            {
                string sWhisper = whisperProgram();
                string sModel = whisperModelPath();
                if (sWhisper != "" && sModel != "") logMessage("Whisper is in place: " + sWhisper, "INFO", "");
                if (sWhisper == "" || sModel == "")
                {
                    logMessage("Whisper was not found. Run installWhisper.cmd in the program folder.", flag("transcribe") ? "ERROR" : "INFO",
                               flag("transcribe") ? null : "");
                    if (flag("transcribe")) bReady = false;
                }
            }
            if (flag("describe"))
            {
                if (!checkOllama()) bReady = false;
            }
            if (!flag("describe") && !flag("transcribe"))
            {
                logMessage("Neither describing nor transcribing was asked for.", "INFO", "");
            }
            return bReady;
        }

        // The name of the finished film: its own container, or a single mp3 when
        // only the sound is wanted.
        static string outputName(string sInput)
        {
            if (flag("audio-only")) return sDefaultDescribedStem + ".mp3";
            return sDefaultDescribedStem + Path.GetExtension(sInput);
        }

        // Open the results in Explorer, with what was made selected.
        static void showFolder(string sFolder)
        {
            try
            {
                string sSelect = "";
                foreach (string sFile in Directory.GetFiles(sFolder, sDefaultDescribedStem + ".*"))
                {
                    if (!sFile.EndsWith(".md")) sSelect = sFile;
                }
                if (sSelect == "" && File.Exists(Path.Combine(sFolder, sDefaultTranscriptName))) sSelect = Path.Combine(sFolder, sDefaultTranscriptName);
                if (sSelect != "") Process.Start("explorer.exe", "/select,\"" + sSelect + "\"");
                else Process.Start("explorer.exe", "\"" + sFolder + "\"");
                logMessage("Opened " + sFolder, "INFO", "");
            }
            catch (Exception oError)
            {
                logMessage("The results folder could not be opened: " + oError.Message, "ERROR");
            }
        }

        // What was done, said once at the end. A run started from the dialog has
        // no console to read, so this is the only report the person gets, and a
        // box between sources would stop a batch until somebody pressed a key.
        // A message box with no owner can open behind other windows, and with
        // the console hidden HomerScribe has no window of its own. A hidden,
        // topmost owner form puts it in front and into Alt+Tab, which is the
        // difference between a report and a program that seems to vanish.
        static DialogResult sayToUser(string sText, string sCaption, MessageBoxButtons oButtons, MessageBoxIcon oIcon)
        {
            logMessage("Showing: " + sText.Replace(Environment.NewLine, " | "), "INFO", "");
            try
            {
                Form oOwner = ownerForm();
                if (oOwner != null)
                {
                    DialogResult oOwned = MessageBox.Show(oOwner, sText, sCaption, oButtons, oIcon);
                    logMessage("The message was acknowledged.", "INFO", "");
                    return oOwned;
                }
                // No dialog, so this is a command line run and the console is
                // visible; an ordinary box is enough.
                DialogResult oAnswer = MessageBox.Show(sText, sCaption, oButtons, oIcon);
                logMessage("The message was acknowledged.", "INFO", "");
                return oAnswer;
            }
            catch (Exception oError)
            {
                logMessage("The message could not be shown: " + oError.Message, "ERROR");
                return DialogResult.None;
            }
        }

        static void showResults(int iDone, int iSkipped, TimeSpan oTook)
        {
            StringBuilder oSaid = new StringBuilder();
            if (iDone == 1) oSaid.Append("One source done.");
            if (iDone > 1) oSaid.Append(iDone.ToString() + " sources done.");
            if (iDone == 0) oSaid.Append("Nothing was done.");
            if (iSkipped > 0) oSaid.Append(" " + iSkipped.ToString() + " already done and skipped.");
            oSaid.Append(Environment.NewLine + "Took " + formatClock(oTook.TotalSeconds) + ".");
            if (lFailures.Count > 0) oSaid.Append(" " + lFailures.Count.ToString() + (lFailures.Count == 1 ? " could not be used." : " could not be used."));
            if (lResults.Count > 0)
            {
                oSaid.Append(Environment.NewLine);
                foreach (string sOne in lResults) oSaid.Append(Environment.NewLine + sOne);
            }
            if (lFailures.Count > 0)
            {
                oSaid.Append(Environment.NewLine + Environment.NewLine + "Could not be used:");
                foreach (string sOne in lFailures) oSaid.Append(Environment.NewLine + sOne);
            }
            string sSaid = oSaid.ToString();
            logMessage("RESULTS: " + sSaid.Replace(Environment.NewLine, " | "), "INFO", sSaid);
            if (!bGuiMode) return;
            // View output already says whether the folder should be opened, so
            // it is opened before this is shown rather than asked about after.
            if (flag("view-output") && sLastOutputFolder != "") showFolder(sLastOutputFolder);
            sayToUser(sSaid, "HomerScribe results", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ---------- context from the web ----------
        //
        // A description is far better when the model knows what it is watching.
        // A file called after the video supplies that, but nobody writes one for
        // a video they just downloaded. Where the source itself says what it is,
        // that can be gathered instead.
        //
        // Two sources, both authoritative rather than searched for:
        //
        //   A web address: yt-dlp already knows the title, the uploader, the
        //   description and the tags. That is the page's own account of itself,
        //   not a guess.
        //
        //   A file: the container may carry a title. If it does, Wikipedia is
        //   asked about that title, and the answer is used ONLY if it clearly
        //   matches and clearly describes a film or programme. A confident wrong
        //   article is worse than none: it would have the model naming actors who
        //   are not there.

        static string httpGet(string sUrl)
        {
            try
            {
                HttpWebRequest oRequest = (HttpWebRequest)WebRequest.Create(sUrl);
                oRequest.Method = "GET";
                oRequest.Timeout = 20000;
                // Wikipedia asks that tools identify themselves.
                oRequest.UserAgent = "HomerScribe/" + version() + " (Homer Tools; accessibility)";
                WebResponse oResponse = oRequest.GetResponse();
                StreamReader oReader = new StreamReader(oResponse.GetResponseStream(), Encoding.UTF8);
                string sAnswer = oReader.ReadToEnd();
                oReader.Close();
                oResponse.Close();
                return sAnswer;
            }
            catch (Exception oError)
            {
                logMessage("The request to " + sUrl + " failed: " + oError.Message, "INFO", "");
                return "";
            }
        }

        static string titleOf(string sFfmpeg, string sInput)
        {
            string sOut = "";
            string sErr = "";
            runCommand(sFfmpeg, "-hide_banner -i " + quoted(sInput), out sOut, out sErr);
            Match oMatch = Regex.Match(sOut + sErr, @"^\s*title\s*:\s*(.+)$", RegexOptions.Multiline);
            if (oMatch.Success) return oMatch.Groups[1].Value.Trim();
            return "";
        }

        // How much two titles agree, on words alone. Punctuation, case and the
        // small words are ignored, because "The Odyssey (2026 film)" and
        // "Odyssey" are the same thing and "Odyssey Dawn" is not.
        // A file title is rarely the title of the work. This one reads
        // "The Africans: A Triple Heritage -  Program 7:  A Garden of Eden in
        // Decay", and the article is called "The Africans: A Triple Heritage".
        // So the series title is tried as well as the whole thing, shortest
        // last, and the first confident match wins.
        static List<string> titleCandidates(string sTitle)
        {
            List<string> lTries = new List<string>();
            string sClean = Regex.Replace(sTitle, @"\s+", " ").Trim();
            addTry(lTries, sClean);
            // Drop an episode marker and everything after it.
            addTry(lTries, Regex.Replace(sClean, @"[\s\-,:;]*\b(program|programme|episode|part|chapter|disc|vol|volume)\b\s*\d+.*$", "", RegexOptions.IgnoreCase));
            // Drop a subtitle introduced by a dash.
            int iDash = sClean.IndexOf(" - ");
            if (iDash > 0) addTry(lTries, sClean.Substring(0, iDash));
            // Drop everything after the SECOND colon: the first usually belongs
            // to the work's own title, the second to the episode.
            int iFirst = sClean.IndexOf(':');
            if (iFirst > 0)
            {
                int iSecond = sClean.IndexOf(':', iFirst + 1);
                if (iSecond > 0) addTry(lTries, sClean.Substring(0, iSecond));
            }
            return lTries;
        }

        static void addTry(List<string> lTries, string sOne)
        {
            string sTrim = Regex.Replace(sOne, @"[\s\-:;,]+$", "").Trim();
            if (sTrim == "") return;
            if (contentWords(sTrim).Count < 2) return;
            foreach (string sHave in lTries)
            {
                if (string.Compare(sHave, sTrim, true) == 0) return;
            }
            lTries.Add(sTrim);
        }

        static double titleAgreement(string sOne, string sTwo)
        {
            // sOne is the file's title, sTwo the article's. What matters is
            // whether the ARTICLE's title is contained in the file's, not how
            // alike the two are: a file title carrying an episode name is far
            // longer than the work it belongs to, and scoring on the longer of
            // the two would reject every correct answer.
            List<string> lFile = contentWords(Regex.Replace(sOne, @"\([^)]*\)", " "));
            List<string> lArticle = contentWords(Regex.Replace(sTwo, @"\([^)]*\)", " "));
            // A one-word article title matches far too much to be trusted.
            if (lFile.Count == 0 || lArticle.Count < 2) return 0.0;
            int iShared = 0;
            foreach (string sWord in lArticle)
            {
                if (lFile.Contains(sWord)) iShared = iShared + 1;
            }
            return (double)iShared / (double)lArticle.Count;
        }

        // Who fronts the film, as the article states it.
        static readonly string[] asPresenterPatterns = new string[] {
            @"written and (?:narrated|presented) by (?:Dr\.?|Professor|Prof\.?|Mr\.?|Ms\.?|Mrs\.?)?\s*([A-Z][\w'\-]+(?:\s+[A-Z][\w'\-\.]+){1,3})",
            @"(?:narrated|presented|hosted|written) by (?:Dr\.?|Professor|Prof\.?|Mr\.?|Ms\.?|Mrs\.?)?\s*([A-Z][\w'\-]+(?:\s+[A-Z][\w'\-\.]+){1,3})",
            @"(?:presenter|host|narrator) (?:is|was) (?:Dr\.?|Professor|Prof\.?)?\s*([A-Z][\w'\-]+(?:\s+[A-Z][\w'\-\.]+){1,3})"
        };

        static string presenterIn(string sText)
        {
            foreach (string sPattern in asPresenterPatterns)
            {
                Match oMatch = Regex.Match(sText, sPattern);
                if (oMatch.Success) return oMatch.Groups[1].Value.Trim().TrimEnd('.', ',');
            }
            return "";
        }

        static readonly string[] asFilmWords = new string[] {
            "film", "movie", "documentary", "series", "television", "programme", "program",
            "directed", "starring", "episode", "miniseries", "drama", "broadcast"
        };

        static string wikipediaContext(string sTitle)
        {
            if (sTitle.Trim() == "") return "";
            string sUrl = "https://en.wikipedia.org/w/api.php?action=query&format=json&prop=extracts"
                        + "&exintro=1&explaintext=1&redirects=1&generator=search&gsrlimit=3&gsrsearch="
                        + Uri.EscapeDataString(sTitle);
            string sAnswer = httpGet(sUrl);
            if (sAnswer == "") return "";
            JavaScriptSerializer oSerializer = new JavaScriptSerializer();
            oSerializer.MaxJsonLength = int.MaxValue;
            Dictionary<string, object> dReply = null;
            try
            {
                dReply = oSerializer.Deserialize<Dictionary<string, object>>(sAnswer);
            }
            catch (Exception)
            {
                return "";
            }
            if (!dReply.ContainsKey("query")) return "";
            Dictionary<string, object> dQuery = toMap(dReply["query"]);
            if (!dQuery.ContainsKey("pages")) return "";
            string sBest = "";
            string sBestTitle = "";
            double nBest = 0.0;
            foreach (KeyValuePair<string, object> oPage in toMap(dQuery["pages"]))
            {
                Dictionary<string, object> dPage = toMap(oPage.Value);
                if (!dPage.ContainsKey("title") || !dPage.ContainsKey("extract")) continue;
                string sPageTitle = Convert.ToString(dPage["title"]);
                string sExtract = Convert.ToString(dPage["extract"]).Trim();
                double nAgree = titleAgreement(sTitle, sPageTitle);
                bool bLooksRight = false;
                foreach (string sWord in asFilmWords)
                {
                    if (Regex.IsMatch(sExtract, @"\b" + sWord + @"\b", RegexOptions.IgnoreCase)) bLooksRight = true;
                }
                logMessage("  Wikipedia offered \"" + sPageTitle + "\", agreement " + num(nAgree)
                           + ", looks like a film or programme: " + bLooksRight.ToString(), "INFO", "");
                if (!bLooksRight) continue;
                if (nAgree < nDefaultTitleAgreement) continue;
                if (nAgree <= nBest) continue;
                nBest = nAgree;
                sBest = sExtract;
                sBestTitle = sPageTitle;
            }
            if (sBest == "")
            {
                logMessage("Nothing on Wikipedia matched \"" + sTitle + "\" closely enough to trust, so no context is added.",
                           "INFO", "No confident match for \"" + sTitle + "\", so no web context is used.");
                return "";
            }
            if (sBest.Length > 1500) sBest = sBest.Substring(0, 1500);
            // The one identification a documentary makes safe. Without it the
            // model knows the presenter's name and still writes "a man",
            // because it has no way to put a name to a face.
            string sWho = presenterIn(sBest);
            if (sWho != "")
            {
                logMessage("The presenter is named as " + sWho + ".", "INFO", "The presenter is " + sWho + ".");
                sBest = sBest + " PRESENTER: " + sWho + " presents this film and appears in it throughout. "
                      + "When a person is speaking to the viewer, or walking and talking to the viewer, that is " + sWho
                      + ", and you should name " + sWho + " rather than calling him or her a man or a woman.";
            }
            logMessage("Using the Wikipedia article \"" + sBestTitle + "\" as context, agreement " + num(nBest) + ".",
                       "INFO", "Context taken from the Wikipedia article \"" + sBestTitle + "\".");
            return sBest;
        }

        // What the page says about itself. yt-dlp already fetched this to
        // download the video, so no search is involved and nothing is guessed.
        static string webPageContext(string sAddress)
        {
            string sYtDlp = findTool("yt-dlp");
            if (sYtDlp == "") return "";
            string sOut = "";
            string sErr = "";
            int iCode = runCommand(sYtDlp, "--skip-download --no-playlist --dump-single-json " + quoted(sAddress), out sOut, out sErr);
            if (iCode != 0 || sOut.Trim() == "") return "";
            JavaScriptSerializer oSerializer = new JavaScriptSerializer();
            oSerializer.MaxJsonLength = int.MaxValue;
            Dictionary<string, object> dPage = null;
            try
            {
                dPage = oSerializer.Deserialize<Dictionary<string, object>>(sOut);
            }
            catch (Exception oError)
            {
                logMessage("The page's own description could not be read: " + oError.Message, "INFO", "");
                return "";
            }
            StringBuilder oSaid = new StringBuilder();
            if (dPage.ContainsKey("title")) oSaid.Append("This video is called \"" + Convert.ToString(dPage["title"]) + "\". ");
            if (dPage.ContainsKey("uploader")) oSaid.Append("It was published by " + Convert.ToString(dPage["uploader"]) + ". ");
            if (dPage.ContainsKey("description"))
            {
                string sAbout = Regex.Replace(Convert.ToString(dPage["description"]), @"\s+", " ").Trim();
                // The tail of a description is usually links and appeals to
                // subscribe, which say nothing about what is on screen.
                if (sAbout.Length > 1200) sAbout = sAbout.Substring(0, 1200);
                if (sAbout != "") oSaid.Append("Its own description reads: " + sAbout + " ");
            }
            string sContext = oSaid.ToString().Trim();
            if (sContext != "") logMessage("Context taken from the page itself, " + sContext.Split(' ').Length.ToString() + " words.",
                                           "INFO", "Context taken from the page's own description.");
            return sContext;
        }

        static string webContext(string sFfmpeg, string sInput, string sOriginalAddress)
        {
            if (!flag("web-context")) return "";
            if (sOriginalAddress != "") return webPageContext(sOriginalAddress);
            string sTitle = titleOf(sFfmpeg, sInput);
            if (sTitle == "")
            {
                logMessage("The file carries no title, so there is nothing to look up.", "INFO", "");
                return "";
            }
            logMessage("The file calls itself \"" + sTitle + "\".", "INFO", "");
            foreach (string sTry in titleCandidates(sTitle))
            {
                logMessage("Asking Wikipedia about \"" + sTry + "\".", "INFO", "Looking up \"" + sTry + "\".");
                string sFound = wikipediaContext(sTry);
                if (sFound != "") return sFound;
            }
            return "";
        }

        static int runOne(string sInput, string sWebAddress)
        {
            string sFfmpeg = findTool("ffmpeg");
            if (!File.Exists(sInput))
            {
                logMessage("The file was not found: " + sInput, "ERROR");
                return 1;
            }
            // Each video gets a folder of its own, named after the video, so a
            // run over several videos keeps their results apart. Without an
            // output directory the folder sits beside the video itself.
            string sRoot = Path.GetFileNameWithoutExtension(sInput);
            string sBase = text("output-dir");
            if (sBase == "") sBase = Path.GetDirectoryName(sInput);
            string sOutputDir = Path.Combine(sBase, sRoot);
            // A downloaded video is already sitting in a folder named after
            // itself, because that is where its results are going. Making
            // another folder of the same name inside it would nest one pointlessly
            // within another.
            try
            {
                string sHolding = Path.GetDirectoryName(Path.GetFullPath(sInput));
                if (sHolding != null && string.Compare(Path.GetFileName(sHolding), sRoot, true) == 0) sOutputDir = sHolding;
            }
            catch (Exception)
            {
            }

            // Finished is now per job, since a run may describe, transcribe, or
            // both. Testing for the folder would skip an interrupted run; testing
            // for the film alone would call a transcribe-only run unfinished for
            // ever. So each job is asked separately whether its own output is
            // there AND the record says that job reached the end.
            string sFilmPath = Path.Combine(sOutputDir, outputName(sInput));
            string sTranscriptPath = Path.Combine(sOutputDir, sDefaultTranscriptName);
            bLastCacheFinished = false;
            bLastTranscriptFinished = false;
            if (File.Exists(Path.Combine(workFolderFor(sInput), sDefaultJsonName))) readCache(Path.Combine(workFolderFor(sInput), sDefaultJsonName));
            bool bDescribeDone = File.Exists(sFilmPath) && bLastCacheFinished;
            bool bTranscribeDone = File.Exists(sTranscriptPath) && bLastTranscriptFinished;
            bool bWantDescribe = flag("describe") && !(bDescribeDone && !flag("force"));
            bool bWantTranscribe = flag("transcribe") && !(bTranscribeDone && !flag("force"));
            if (!bWantDescribe && !bWantTranscribe)
            {
                sLastSkippedFolder = sOutputDir;
                logMessage("Skipping " + sRoot + ": everything asked for is already in " + sOutputDir + ". "
                           + (bGuiMode ? "Tick Force overwrite to do it again." : "Pass --force to do it again."),
                           "INFO", "Skipping " + sRoot + ", already done.");
                return iAlreadyDone;
            }
            if (flag("describe") && !bWantDescribe) logMessage("The described film is already here, so only the transcript is made.", "INFO", "Already described; making the transcript only.");
            if (flag("transcribe") && !bWantTranscribe) logMessage("The transcript is already here, so only the description is made.", "INFO", "Already transcribed; describing only.");
            if (Directory.Exists(sOutputDir) && !flag("force"))
            {
                logMessage("An unfinished run is here. Carrying on from where it stopped; nothing already described is described again.",
                           "INFO", "An unfinished run is here. Carrying on from where it stopped.");
            }

            // The video's folder holds only what a person would open: the
            // described film and the script to read. The working files go under
            // the user's application data, out of the way but findable.
            string sWorkDir = workFolderFor(sInput);
            Directory.CreateDirectory(sOutputDir);
            Directory.CreateDirectory(sWorkDir);
            string sSaying = "Processing " + Path.GetFileName(sInput);
            if (iSourceCount > 1) sSaying = "Processing " + iSourceAt.ToString() + " of " + iSourceCount.ToString() + ", " + Path.GetFileName(sInput);
            announce("Initializing", -1.0, 1.0, sSaying);
            logMessage("Working files are under " + sWorkDir, "INFO", "");
            sSpeechWorkDir = sWorkDir;
            lFilmSpeech = new List<Speech>();
            bSpeechReady = false;
            logMessage("Input: " + sInput, "INFO", "");
            logMessage("Results go to: " + sOutputDir, "INFO", "Results go to " + sOutputDir);

            // The context describing this particular film. A file named after the
            // video and sitting beside it -- video.md for video.mkv -- is found
            // without being asked for, which is how a general purpose describer
            // learns the names of one film's characters. An explicit
            // --context-file overrides it.
            string sContext = "";
            string sContextFile = text("context-file");
            if (sContextFile != "" && !File.Exists(sContextFile)) sContextFile = Path.Combine(exeFolder(), Path.GetFileName(sContextFile));
            if (sContextFile == "" || !File.Exists(sContextFile))
            {
                string sBeside = Path.Combine(Path.GetDirectoryName(sInput), sRoot + ".md");
                if (File.Exists(sBeside)) sContextFile = sBeside;
            }
            if (sContextFile != "" && File.Exists(sContextFile))
            {
                sContext = Regex.Replace(File.ReadAllText(sContextFile), @"\s+", " ").Trim();
                logMessage("Context loaded from " + sContextFile + ", " + sContext.Split(' ').Length.ToString() + " words",
                           "INFO", "Context loaded from " + Path.GetFileName(sContextFile) + ", " + sContext.Split(' ').Length.ToString() + " words.");
            }
            string sFromWeb = webContext(sFfmpeg, sInput, sWebAddress);
            if (sFromWeb != "")
            {
                sContext = (sContext + " " + sFromWeb).Trim();
                logMessage("Context is now " + sContext.Split(' ').Length.ToString() + " words, including what was gathered.", "INFO", "");
            }
            if (sContext == "") logMessage("No context file was found. Descriptions will not use character names. Put " + sRoot + ".md beside the video to supply them.",
                                           "INFO", "No context file found, so no character names. Put " + sRoot + ".md beside the video.");

            double nDuration = probeDuration(sFfmpeg, sInput);
            if (nDuration <= 0.0) return 1;

            // A window was asked for, so cut it once and describe that instead.
            double nBegin = parseTime(text("begin"));
            double nWanted = number("minutes") * 60.0;
            if (nBegin > 0.0 || nWanted > 0.0)
            {
                if (nWanted <= 0.0) nWanted = nDuration - nBegin;
                if (nBegin + nWanted > nDuration) nWanted = Math.Max(30.0, nDuration - nBegin);
                string sWindow = Path.Combine(sOutputDir, "window.mkv");
                logMessage("Cutting a window from " + formatClock(nBegin) + " for " + num(nWanted / 60.0) + " minutes",
                           "INFO", "Cutting a window from " + formatClock(nBegin) + ".");
                string sCutOut = "";
                string sCutErr = "";
                int iCut = runCommand(sFfmpeg, "-hide_banner -loglevel error -y -ss " + num(nBegin) + " -i " + quoted(sInput)
                                             + " -t " + num(nWanted) + " -map 0:v:0 -map 0:a:0 -c copy -avoid_negative_ts make_zero " + quoted(sWindow),
                                      out sCutOut, out sCutErr);
                if (iCut != 0 || !File.Exists(sWindow))
                {
                    logMessage("The window could not be cut.", "ERROR");
                    return 1;
                }
                sInput = sWindow;
                nDuration = probeDuration(sFfmpeg, sInput);
                if (nDuration <= 0.0) return 1;
            }
            logMessage("Duration: " + num(nDuration) + " seconds", "INFO", "Film runs " + formatClock(nDuration) + ".");

            // The transcript is wanted by both jobs: by transcribing, obviously,
            // and by describing, to know where the speech is. So it is made
            // once, before either.
            if (bWantTranscribe || (bWantDescribe && flag("speech")))
            {
                sSpeechWorkDir = sWorkDir;
                lFilmSpeech = transcribe(sFfmpeg, sInput, sWorkDir, nDuration);
                bSpeechReady = true;
            }
            if (bWantTranscribe)
            {
                if (lFilmSpeech.Count == 0)
                {
                    logMessage("Nothing could be transcribed, so no transcript is written.", "ERROR");
                    if (!bWantDescribe) return 1;
                }
                else
                {
                    writeTranscript(lFilmSpeech, sTranscriptPath, Path.GetFileName(sInput), nDuration);
                    bTranscribed = true;
                    writeCache(new List<Moment>(), sJsonPathEarly(sWorkDir), false, null, null);
                    logMessage("Transcript written to " + sTranscriptPath,
                               "INFO", "Transcript written: " + lFilmSpeech.Count.ToString() + " spoken stretches.");
                    lResults.Add(Path.GetFileName(sInput) + ": transcript of " + lFilmSpeech.Count.ToString() + " spoken stretches"
                                 + Environment.NewLine + "    " + sTranscriptPath);
                    sLastOutputFolder = sOutputDir;
                }
            }
            if (!bWantDescribe)
            {
                // Transcribing only. None of what follows applies.
                if (flag("view-output")) sLastOutputFolder = sOutputDir;
                return 0;
            }
            if (!openVoice()) return 1;

            Dictionary<string, string> dCache = new Dictionary<string, string>();
            string sJsonPath = Path.Combine(sWorkDir, sDefaultJsonName);
            if (!flag("force")) dCache = readCache(sJsonPath);
            if (dCache.Count > 0) logMessage("Picking up where the last run stopped: " + dCache.Count.ToString() + " descriptions already written",
                                             "INFO", "Carrying on from " + dCache.Count.ToString() + " descriptions already written.");

            List<Moment> lGaps = findGaps(sFfmpeg, sInput, nDuration);
            List<Moment> lDone = new List<Moment>();
            List<string> lRecent = new List<string>();
            List<string> lNames = new List<string>();
            // The presenter is a name in use from the first description, so it
            // stays consistent rather than being arrived at twice.
            string sPresenter = presenterIn(sContext);
            if (sPresenter != "")
            {
                foreach (string sWord in sPresenter.Split(' '))
                {
                    if (sWord.Length > 2 && !lNames.Contains(sWord)) lNames.Add(sWord);
                }
                logMessage("Descriptions may name the presenter, " + sPresenter + ".", "INFO", "");
            }
            byte[] binLast = new byte[0];
            int iIndex = 0;
            int iSkipped = 0;
            int iLastPercent = -1;
            int iSpokenAt = 0;
            bool bWarnedSlow = false;
            int iOverSpeech = 0;
            int iWithDialogue = 0;
            int iForcedDescribed = 0;
            double nLastSpoken = -1.0;
            DateTime dtBegan = DateTime.Now;
            DateTime dtLastMux = DateTime.Now;
            startHeartbeat();

            if (flag("announce"))
            {
                Moment oOpening = new Moment();
                oOpening.nStart = 0.0;
                oOpening.sText = "Audio description is on.";
                oOpening.binAudio = speakToPcm(oOpening.sText, integer("rate"));
                oOpening.nSpoken = pcmSeconds(oOpening.binAudio);
                if (oOpening.binAudio.Length > 0) lDone.Add(oOpening);
            }

            string sImagePath = Path.Combine(sWorkDir, "montage.jpg");
            foreach (Moment oGap in lGaps)
            {
                iIndex = iIndex + 1;
                double nAllowed = oGap.nLength + overrunFor(text("detail"));
                int iMaxWords = Math.Max(6, (int)(nAllowed * number("words-per-second")));
                if (integer("max-words") > 0 && iMaxWords > integer("max-words")) iMaxWords = integer("max-words");
                nWaitingAt = oGap.nStart;
                string sJustSaid = spokenBefore(lFilmSpeech, oGap.nStart);
                if (sJustSaid.Length > 600) sJustSaid = sJustSaid.Substring(sJustSaid.Length - 600);
                string sText = "";
                bool bNewScene = false;
                bool bFromCache = dCache.ContainsKey(num(oGap.nStart));
                if (bFromCache) sText = dCache[num(oGap.nStart)];
                if (!bFromCache)
                {
                    // Rebuilding: speak and assemble what is already written.
                    // A moment never described stays undescribed.
                    if (flag("rebuild")) continue;
                    if (!buildMontage(sFfmpeg, sInput, oGap.nStart + oGap.nLength / 2.0, oGap.nLength + 2.0, sImagePath)) continue;
                    if (number("same-shot") > 0.0)
                    {
                        byte[] binNow = shotSignature(sFfmpeg, sImagePath, sWorkDir);
                        double nMoved = signatureDistance(binLast, binNow);
                        bool bQuietTooLong = nLastSpoken >= 0.0 && oGap.nStart - nLastSpoken >= number("max-silence");
                        if (binLast.Length > 0 && nMoved < number("same-shot") && !bQuietTooLong)
                        {
                            iSkipped = iSkipped + 1;
                            logMessage("Moment " + iIndex.ToString() + " at " + num(oGap.nStart) + "s looks the same as the last one, difference " + num(nMoved) + ". Saying nothing.", "INFO", "");
                            continue;
                        }
                        // The standards ask that a change of place be established
                        // before anything else, general to specific. A picture
                        // that has changed this much is a new place.
                        bNewScene = binLast.Length == 0 || nMoved >= nDefaultNewScene;
                        if (bNewScene) logMessage("The picture changed by " + num(nMoved) + ", so this is treated as a new scene.", "INFO", "");
                        binLast = binNow;
                    }
                    List<string> lShown = lRecent.GetRange(Math.Max(0, lRecent.Count - iDefaultRecent), Math.Min(iDefaultRecent, lRecent.Count));
                    int iLookWords = flag("summarise") ? iMaxWords * 3 : iMaxWords;
                    sText = describeImage(sImagePath, iLookWords, lShown, sContext, false, bNewScene, "", oGap.bForced, lNames, sJustSaid, false);
                    if (flag("summarise") && sText != "")
                    {
                        string sSeen = sText;
                        sText = summarise(sSeen, iMaxWords, lShown);
                        logMessage("Saw: " + sSeen, "INFO", "");
                        logMessage("Said: " + sText, "INFO", "");
                    }
                    List<string> lAgainst = lRecent.GetRange(Math.Max(0, lRecent.Count - iDefaultCompare), Math.Min(iDefaultCompare, lRecent.Count));
                    double nLike = worstLikeness(sText, lAgainst);
                    if (sText != "" && nLike >= number("similarity"))
                    {
                        logMessage("That description was " + ((int)(nLike * 100)).ToString() + " percent the same as a recent one. Asking again.", "INFO", "");
                        sText = describeImage(sImagePath, iMaxWords, lShown, sContext, true, bNewScene, "", oGap.bForced, lNames, sJustSaid, false);
                        nLike = worstLikeness(sText, lAgainst);
                    }
                    // One chance to replace a judgment with what was seen.
                    string sJudged = judgmentFound(sText);
                    if (sText != "" && sJudged != "" && flag("objective"))
                    {
                        logMessage("That description judged rather than observed, at the word \"" + sJudged + "\". Asking again.", "INFO", "");
                        string sBetter = describeImage(sImagePath, iMaxWords, lShown, sContext, false, bNewScene, sJudged, oGap.bForced, lNames, sJustSaid, false);
                        if (sBetter != "" && worstLikeness(sBetter, lAgainst) < number("similarity")) sText = sBetter;
                        string sStill = judgmentFound(sText);
                        if (sStill != "" && sStill.EndsWith("ly"))
                        {
                            // An adverb can be taken out and leave a sentence
                            // behind. "He sits contemplatively outside" becomes
                            // "He sits outside", which is what was actually seen.
                            string sPlainer = Regex.Replace(sText, @"\s*\b" + sStill + @"\b\s*", " ", RegexOptions.IgnoreCase);
                            sPlainer = Regex.Replace(sPlainer, @"\s+", " ").Replace(" ,", ",").Replace(" .", ".").Trim();
                            if (sPlainer.Split(' ').Length >= 4)
                            {
                                logMessage("Removed the judging adverb \"" + sStill + "\".", "INFO", "");
                                sText = sPlainer;
                                sStill = judgmentFound(sText);
                            }
                        }
                        if (sStill != "") logMessage("It still judges, at \"" + sStill + "\". Keeping it rather than losing the moment.", "INFO", "");
                    }
                    if (sText != "" && nLike >= number("similarity"))
                    {
                        // Silence beats a repeat, but not for minutes on end. A
                        // measured run left two stretches of over seven minutes
                        // with nothing said, which is the worse failure.
                        double nSinceLast = oGap.nStart - nLastSpoken;
                        if (nLastSpoken >= 0.0 && nSinceLast < number("max-silence"))
                        {
                            iSkipped = iSkipped + 1;
                            logMessage("Still " + ((int)(nLike * 100)).ToString() + " percent the same. Saying nothing at " + num(oGap.nStart) + "s rather than repeating.", "INFO", "");
                            continue;
                        }
                        logMessage("Still " + ((int)(nLike * 100)).ToString() + " percent the same, but nothing has been said for " + num(nSinceLast) + " seconds, so it is kept.", "INFO", "");
                    }
                    // A guess at who someone is helps nobody: naming the wrong
                    // character is worse than naming none.
                    string sHedged = hedgeFound(sText);
                    if (sText != "" && sHedged != "")
                    {
                        logMessage("That description guessed, at \"" + sHedged + "\". Asking again.", "INFO", "");
                        string sSurer = describeImage(sImagePath, iMaxWords, lShown, sContext, false, bNewScene, "", oGap.bForced, lNames, sJustSaid, false);
                        if (sSurer != "" && hedgeFound(sSurer) == "") sText = sSurer;
                    }
                }
                if (sText == "" && !flag("rebuild") && nLastSpoken >= 0.0 && oGap.nStart - nLastSpoken >= number("max-silence"))
                {
                    logMessage("Nothing said for " + num(oGap.nStart - nLastSpoken) + "s, so this moment is asked again with no leave to skip.", "INFO", "");
                    List<string> lInsistOn = lRecent.GetRange(Math.Max(0, lRecent.Count - iDefaultRecent), Math.Min(iDefaultRecent, lRecent.Count));
                    sText = describeImage(sImagePath, iMaxWords, lInsistOn, sContext, false, bNewScene, "", false, lNames, sJustSaid, true);
                    if (flag("summarise") && sText != "") sText = summarise(sText, iMaxWords, lInsistOn);
                }
                if (sText == "")
                {
                    logMessage("Moment " + iIndex.ToString() + " at " + num(oGap.nStart) + "s produced nothing", "INFO", "");
                    continue;
                }
                sText = trimToWords(sText, iMaxWords);
                int iRate = integer("rate");
                byte[] binAudio = speakToPcm(sText, iRate);
                if (binAudio.Length == 0) continue;
                if (pcmSeconds(binAudio) > nAllowed && iRate < 8)
                {
                    iRate = Math.Min(8, iRate + 3);
                    binAudio = speakToPcm(sText, iRate);
                }
                while (pcmSeconds(binAudio) > nAllowed && splitSentences(sText).Count > 1)
                {
                    sText = dropLastSentence(sText);
                    binAudio = speakToPcm(sText, iRate);
                }
                // One sentence left, so dropping sentences cannot help. Shorten
                // it at clause boundaries, which leaves a sentence rather than a
                // fragment. Words are never cut off the end: "A man stands on a
                // hilltop under bright" is worse than running a second long.
                while (pcmSeconds(binAudio) > nAllowed)
                {
                    string sShorter = dropLastClause(sText);
                    if (sShorter == sText) break;
                    sText = sShorter;
                    binAudio = speakToPcm(sText, iRate);
                }
                // Nothing left to drop. Asking the model to say it again in
                // fewer words is the only way left to shorten it and still have
                // it read as English.
                if (pcmSeconds(binAudio) > nAllowed && flag("summarise"))
                {
                    int iRoom = Math.Max(6, (int)(nAllowed * number("words-per-second") * 0.8));
                    string sTighter = summarise(sText, iRoom, new List<string>());
                    if (sTighter != "" && sTighter.Split(' ').Length < sText.Split(' ').Length)
                    {
                        logMessage("Asked again in " + iRoom.ToString() + " words or fewer, to fit the gap.", "INFO", "");
                        sText = sTighter;
                        binAudio = speakToPcm(sText, iRate);
                    }
                }
                if (pcmSeconds(binAudio) > nAllowed) logMessage("This description still runs " + num(pcmSeconds(binAudio) - nAllowed) + "s past what the gap allows: " + sText, "INFO", "");
                oGap.sText = sText;
                oGap.binAudio = binAudio;
                oGap.nSpoken = pcmSeconds(binAudio);
                nLastSpoken = oGap.nStart;
                if (overlapsSpeech(lFilmSpeech, oGap.nStart, oGap.nStart + oGap.nSpoken)) iOverSpeech = iOverSpeech + 1;
                if (sJustSaid != "") iWithDialogue = iWithDialogue + 1;
                if (oGap.bForced) iForcedDescribed = iForcedDescribed + 1;
                lDone.Add(oGap);
                lRecent.Add(sText);
                gatherNames(sText, lNames);
                if (lRecent.Count > iDefaultCompare) lRecent.RemoveAt(0);
                dialogSays("describing, " + spokenPosition(oGap.nStart, nDuration));
                pumpDialog();
                logMessage("Moment " + iIndex.ToString() + " of " + lGaps.Count.ToString() + " at " + num(oGap.nStart) + "s, " + num(oGap.nSpoken) + "s of " + num(oGap.nLength) + "s: " + sText,
                           "INFO", "Describing    " + formatClock(oGap.nStart) + "  " + sText);
                // The caption carries the position, the body the description
                // itself, so a screen reader reads both without being asked.
                // Everything said in the film since the last description, in one
                // announcement, then the description itself. That is the whole
                // account in the order it happened.
                if (lFilmSpeech.Count > 0)
                {
                    StringBuilder oHeard = new StringBuilder();
                    double nFirst = -1.0;
                    while (iSpokenAt < lFilmSpeech.Count && lFilmSpeech[iSpokenAt].nStart < oGap.nStart)
                    {
                        Speech oSaidThen = lFilmSpeech[iSpokenAt];
                        iSpokenAt = iSpokenAt + 1;
                        if (oSaidThen.sText == "") continue;
                        if (nFirst < 0.0) nFirst = oSaidThen.nStart;
                        oHeard.Append(oSaidThen.sText + " ");
                    }
                    if (oHeard.Length > 0) announce("Transcribing", nFirst, nDuration, oHeard.ToString());
                }
                announce("Describing", oGap.nStart, nDuration, sText);
                int iPercent = (int)(oGap.nStart * 100.0 / Math.Max(nDuration, 1.0));
                if (iPercent != iLastPercent) logMessage("Reached " + iPercent.ToString() + " percent of " + Path.GetFileName(sInput), "INFO", "");
                iLastPercent = iPercent;
                saveReadable(lDone, sOutputDir, sWorkDir, Path.GetFileName(sInput), nDuration);
                bool bSayNow = iIndex <= 3 || iIndex == 5 || iIndex % 10 == 0;
                if (bSayNow)
                {
                    double nEach = DateTime.Now.Subtract(dtBegan).TotalSeconds / (double)iIndex;
                    double nLeftMinutes = nEach * (lGaps.Count - iIndex) / 60.0;
                    string sLeft = "about " + ((int)Math.Round(nLeftMinutes)).ToString() + " minutes left";
                    if (nLeftMinutes >= 90.0) sLeft = "about " + num(Math.Round(nLeftMinutes / 60.0, 1)) + " hours left";
                    logMessage("Progress: " + iIndex.ToString() + " of " + lGaps.Count.ToString() + ", " + num(nEach) + " seconds each",
                               "INFO", "-- " + iIndex.ToString() + " of " + lGaps.Count.ToString() + " done, " + num(nEach) + " seconds each, " + sLeft + " --");
                    if (!bWarnedSlow && iIndex >= 2 && nEach >= nDefaultSlowSeconds)
                    {
                        bWarnedSlow = true;
                        string sSlow = "Each description is taking about " + ((int)nEach).ToString() + " seconds, so this film will take "
                                     + (nLeftMinutes >= 90.0 ? num(Math.Round(nLeftMinutes / 60.0, 1)) + " hours" : ((int)Math.Round(nLeftMinutes)).ToString() + " minutes") + "."
                                     + Environment.NewLine + Environment.NewLine
                                     + "That usually means the model is running on the processor rather than a graphics card. "
                                     + "It is working, not stuck, and it will finish."
                                     + Environment.NewLine + Environment.NewLine
                                     + "To make it quicker: install a smaller model with"
                                     + Environment.NewLine + "    ollama pull qwen2.5vl:3b"
                                     + Environment.NewLine + "and run again with --model qwen2.5vl:3b."
                                     + Environment.NewLine + Environment.NewLine
                                     + Environment.NewLine + Environment.NewLine
                                     + "Or ask the model to do less. Each description currently takes two calls, and each "
                                     + "rejected one takes another:"
                                     + Environment.NewLine + "    --summarise no       one call instead of two, roughly half the time"
                                     + Environment.NewLine + "    --objective no       no second attempt when a description judges rather than observes"
                                     + Environment.NewLine + "    --frames 1 --width 384   less picture to look at"
                                     + Environment.NewLine + Environment.NewLine
                                     + "Whatever happens, nothing is lost. Every description is saved as it is made, so you can "
                                     + "stop and run the same command again to carry on, or run it with --rebuild to make the film "
                                     + "from the descriptions already written.";
                        logMessage(sSlow.Replace(Environment.NewLine, " "), "HINT");
                        Console.WriteLine("");
                        Console.WriteLine(sSlow);
                        Console.WriteLine("");
                        announce("Initializing", oGap.nStart, nDuration, sSlow);
                    }
                }
                if (iIndex % integer("checkpoint") == 0)
                {
                    buildTrack(lDone, nDuration, Path.Combine(sWorkDir, sDefaultWaveName));
                    logMessage("Saved " + lDone.Count.ToString() + " descriptions", "INFO", "  (progress saved)");
                    if (number("mux-minutes") > 0.0 && DateTime.Now.Subtract(dtLastMux).TotalMinutes >= number("mux-minutes"))
                    {
                        startBackgroundMux(sFfmpeg, sInput, Path.Combine(sWorkDir, sDefaultWaveName), Path.Combine(sOutputDir, outputName(sInput)), nDuration);
                        dtLastMux = DateTime.Now;
                    }
                }
            }

            if (lFilmSpeech.Count > 0 && iSpokenAt < lFilmSpeech.Count)
            {
                StringBuilder oRest = new StringBuilder();
                double nFirstRest = -1.0;
                while (iSpokenAt < lFilmSpeech.Count)
                {
                    Speech oSaidLast = lFilmSpeech[iSpokenAt];
                    iSpokenAt = iSpokenAt + 1;
                    if (oSaidLast.sText == "") continue;
                    if (nFirstRest < 0.0) nFirstRest = oSaidLast.nStart;
                    oRest.Append(oSaidLast.sText + " ");
                }
                if (oRest.Length > 0) announce("Transcribing", nFirstRest, nDuration, oRest.ToString());
            }
            flushAnnouncements();
            stopHeartbeat();
            // The report that lets the contribution of hearing the film be
            // judged rather than assumed. The number that matters is how many
            // descriptions land on top of somebody talking: that is the fault
            // silence detection could not avoid, and it should now be near zero.
            if (lDone.Count > 0)
            {
                logMessage("RESULT: " + lDone.Count.ToString() + " descriptions; "
                           + iOverSpeech.ToString() + " overlap speech (" + num(iOverSpeech * 100.0 / lDone.Count) + " percent); "
                           + iForcedDescribed.ToString() + " were placed on the timer rather than in a real gap; "
                           + iWithDialogue.ToString() + " were written knowing what had just been said; "
                           + iSkipped.ToString() + " moments left silent.", "INFO",
                           "Done: " + lDone.Count.ToString() + " descriptions, " + iOverSpeech.ToString() + " of them over speech.");
                if (lFilmSpeech.Count == 0) logMessage("  No transcript was available, so none of that used speech detection.", "INFO", "");
            }
            if (iSkipped > 0) logMessage("Left " + iSkipped.ToString() + " moments silent because nothing had changed.", "INFO", "");
            if (lDone.Count == 0)
            {
                logMessage("No descriptions were produced.", "ERROR");
                return 1;
            }
            saveReadable(lDone, sOutputDir, sWorkDir, Path.GetFileName(sInput), nDuration);
            buildTrack(lDone, nDuration, Path.Combine(sWorkDir, sDefaultWaveName));
            // Any background copy is finished with before the real one is written,
            // so two ffmpeg processes never write the same file.
            waitForMux();
            muxOutput(sFfmpeg, sInput, Path.Combine(sWorkDir, sDefaultWaveName), Path.Combine(sOutputDir, outputName(sInput)), nDuration, false);
            writeCache(lDone, Path.Combine(sWorkDir, sDefaultJsonName), true, lLastGaps, dLastSignature);
            // Both jobs done, so the film can be given whole: what was said and
            // what was there to be seen, in one sequence.
            if (flag("transcribe") && lFilmSpeech.Count > 0 && lDone.Count > 0)
            {
                writeScribed(lDone, lFilmSpeech, Path.Combine(sOutputDir, sDefaultBothName), Path.GetFileName(sInput), nDuration);
                logMessage("Described and transcribed together in " + Path.Combine(sOutputDir, sDefaultBothName),
                           "INFO", "Wrote the interleaved account as well.");
            }
            foreach (string sSpare in new string[] { sDefaultWaveName, "montage.jpg", "signature.raw" })
            {
                try
                {
                    string sSparePath = Path.Combine(sWorkDir, sSpare);
                    if (File.Exists(sSparePath)) File.Delete(sSparePath);
                }
                catch (Exception)
                {
                }
            }
            string sFinished = "Finished with " + lDone.Count.ToString() + " descriptions. The described film is " + Path.Combine(sOutputDir, outputName(sInput));
            logMessage("Finished with " + lDone.Count.ToString() + " descriptions", "INFO", sFinished);
            // No message box here. A box between videos stops a batch dead
            // until somebody presses a key, which defeats the point of giving
            // HomerScribe a folder full of them. The results are collected and
            // shown once, at the end.
            string sOddity = "";
            if (nDuration < 60.0) sOddity = "  (only " + formatClock(nDuration) + " long: the file may be damaged or incomplete)";
            lResults.Add(Path.GetFileName(sInput) + ": " + lDone.Count.ToString() + " descriptions, " + formatClock(nDuration) + sOddity
                         + Environment.NewLine + "    " + Path.Combine(sOutputDir, outputName(sInput)));
            sLastOutputFolder = sOutputDir;
            return 0;
        }

        [STAThread]
        static int Main(string[] asArgs)
        {
            // FIRST, before anything else. No arguments means the dialog is
            // coming, which is known without parsing anything, so the console
            // can go now rather than after the settings are worked out. It was
            // on screen for a moment before, which was long enough to confuse
            // people into thinking it was the program.
            if (asArgs.Length == 0 && consoleWindow.launchedFromGui())
            {
                bConsoleHidden = consoleWindow.hide();
            }
            buildParams();
            if (!parseArgs(asArgs))
            {
                showHelp();
                return 1;
            }
            bVerbose = flag("verbose");
            // The framework's default set of protocols is older than the web
            // it is talking to. Named values are used where 4.8 has them and
            // numbers where it does not, so this compiles whatever is installed.
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | (SecurityProtocolType)3072 | (SecurityProtocolType)12288;
            }
            catch (Exception)
            {
                try
                {
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                }
                catch (Exception)
                {
                }
            }
            if (flag("help"))
            {
                showHelp();
                return 0;
            }
            logEnvironment();

            // Started with nothing on the command line -- from the Start menu,
            // a desktop shortcut, or just the program's name -- so there is
            // nothing to act on and the dialog is what was wanted. Any argument
            // at all means a command line run, unless --gui says otherwise.
            bGuiMode = flag("gui") || asArgs.Length == 0;
            // Started from a shortcut, so the console belongs to HomerScribe
            // and nobody asked for it. Hiding it also stops Control C in that
            // window killing a run. The test follows urlFido, extCheck and 2htm.
            if (bGuiMode && !bConsoleHidden)
            {
                int iAttached = consoleWindow.attachedCount();
                bool bOurs = iAttached == 1;
                logMessage("Console: " + iAttached.ToString() + " process(es) attached, so it is "
                           + (bOurs ? "ours and will be hidden" : "someone else's and is left alone"), "INFO", "");
                if (bOurs)
                {
                    bConsoleHidden = consoleWindow.hide();
                    logMessage("Console hidden: " + bConsoleHidden.ToString(), "INFO", "");
                }
                if (!bOurs)
                {
                    // Started from a console that belongs to somebody else, so it
                    // stays. Writing to it is then wanted, not a nuisance.
                    logMessage("The console belongs to whoever started this, so it is left visible.", "INFO", "");
                }
            }
            if (bConsoleHidden)
            {
                logMessage("The console was hidden before anything else was done.", "INFO", "");
            }
            logMessage("Mode: " + (bGuiMode ? "dialog" : "command line"), "INFO", "");

            // In dialog mode an existing configuration is loaded whether or not
            // it was asked for, so the dialog opens showing last time's answers.
            // Checkboxes start unticked. A settings file is read only when it
            // records that Use configuration was ticked last time, since ticking
            // it is what writes the file.
            // A provisional log, opened before anything can go wrong. It is under
            // application data, which is always writable, and holds whatever
            // happens before the settings say where the real log belongs. If a
            // run dies at the dialog, this is the file that explains it.
            if (fLog == null) openLogAt(Path.Combine(appDataFolder(), sDefaultLogName));
            if (bGuiMode && !dParams["use-configuration"].bGiven && savedSaysUseConfiguration()) dParams["use-configuration"].sValue = "yes";
            if (flag("use-configuration")) loadConfig();

            // Boxes are on by default in dialog mode, off on the command line,
            // where every description is already printed.
            // Announcements are on whenever there is a dialog to speak from.
            // --boxes asks for the old timed message boxes instead of the live
            // region; --announce no turns announcements off altogether.
            bAnnouncing = bGuiMode && flag("announce-progress");
            bBoxes = flag("boxes");

            int iResult = 1;
            try
            {
                if (flag("list-voices"))
                {
                    listVoices();
                    closeLog();
                    return 0;
                }
                while (true)
                {
                    if (bGuiMode && !showDialog())
                    {
                        logMessage("Cancelled at the dialog.", "INFO", "");
                        closeLog();
                        return 0;
                    }
                    // The log's home depends on the settings, and in dialog mode
                    // those are not settled until the dialog is answered.
                    openLog();
                    logSettings();
                    iResult = run();
                    if (!bGuiMode) break;
                    if (iResult != iNothingToDo) break;
                    logMessage("Nothing to describe, so the dialog is shown again.", "INFO", "");
                }
                if (iResult == iNothingToDo) iResult = 1;
            }
            catch (Exception oError)
            {
                logMessage(oError.ToString(), "FATAL");
                iResult = 1;
            }
            finally
            {
                if (oSynth != null) oSynth.Dispose();
                closeDialog();
                closeLog();
            }
            return iResult;
        }
    }
}
