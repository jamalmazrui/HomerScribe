// HomerDescribe.cs -- audio description for local video files.
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

    public class HomerDescribe
    {
        const int iNothingToDo = 2;
        const int iAlreadyDone = 3;
        const int iDefaultCompare = 10;
        const int iDefaultNames = 12;
        const int iDefaultRecent = 2;
        const int iDefaultSampleRate = 48000;
        const int iDefaultScanReport = 10;
        const int iDefaultTimeout = 300000;
        const double nDefaultChapter = 600.0;
        const double nDefaultNewScene = 25.0;
        const double nDefaultLead = 0.20;
        const string sDefaultDescribedStem = "described";
        const string sDefaultJsonName = "described.json";
        const string sDefaultLogName = "HomerDescribe.log";
        const string sDefaultMarkdownName = "described.md";
        const string sDefaultTrackTitle = "Audio Description";
        const string sDefaultVttName = "described.vtt";
        const string sDefaultWaveName = "described.wav";

        static StreamWriter fLog = null;
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
            addParam("source-paths", "s", "string", "", "Video files or YouTube page addresses, separated by spaces; quote any containing a space. The dialog starts browsing in your Videos folder");
            addParam("output-dir", "o", "string", "", "Folder to create each video's results folder in. Empty on the command line means beside the video; the dialog offers your Videos folder");
            addParam("force", "f", "flag", "no", "Describe everything again, ignoring an earlier run");
            addParam("rebuild", "", "flag", "no", "Build the film from descriptions already made, asking the model nothing");
            addParam("log-file", "", "string", "", "Path of the run log; by default it goes with the results");
            addParam("log-session", "l", "flag", "no", "Keep the run log with the results; unticked, it is kept out of the way under your application data");
            addParam("use-configuration", "u", "flag", "no", "Load settings at startup and save them on OK");
            addParam("begin", "b", "string", "0", "Where to start, in seconds or hh:mm:ss");
            addParam("minutes", "e", "number", "0", "How many minutes to describe; 0 means the whole film");
            addParam("model", "m", "string", "qwen2.5vl:7b", "Name of the Ollama vision model");
            addParam("context-file", "c", "string", "", "Text file describing the film, sent with every request");
            addParam("detail", "t", "string", "rich", "How much to say: brief, normal or rich");
            addParam("view-output", "v", "flag", "no", "Open the results folder when the run finishes");
            addParam("audio-only", "a", "flag", "no", "Produce sound only: one mp3 of the film's audio with the descriptions mixed in, and no video");
            addParam("voice", "", "string", "", "Name of the Windows speech voice");
            addParam("rate", "r", "integer", "1", "Speech rate, from minus ten to ten");
            addParam("width", "w", "integer", "512", "Width in pixels of each frame before tiling");
            addParam("crop-bottom", "p", "number", "12", "Percentage cut off the bottom of each frame, to hide burnt-in subtitles");
            addParam("noise-floor", "n", "number", "-24", "Level in dB below which sound counts as a gap");
            addParam("min-gap", "g", "number", "2.0", "Shortest gap worth describing");
            addParam("spacing", "", "number", "10.0", "Least seconds between descriptions");
            addParam("every", "y", "number", "14.0", "Guarantee a description at least this often; 0 turns it off");
            addParam("words-per-second", "d", "number", "2.67", "Speaking rate used to budget words; 2.67 is the 160 words a minute the standards call comfortable");
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
            addParam("mux-minutes", "", "number", "0", "Minutes between background writes of the film so far; 0, the default, writes it only at the end");
            addParam("ffmpeg-dir", "", "string", "", "Folder holding ffmpeg.exe, searched in addition to the PATH");
            addParam("summarise", "", "flag", "yes", "Look thoroughly with the vision model, then have the same model compress what it saw into one spoken description");
            addParam("objective", "", "flag", "yes", "Ask again when a description states a mood or a judgement instead of what is visible");
            addParam("announce", "", "flag", "yes", "Speak an opening line confirming description is running");
            addParam("boxes", "", "flag", "", "Show each description in a timed message box a screen reader will read");
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
                else if (sArg == "/?" || sArg == "/help")
                {
                    dParams["help"].sValue = "yes";
                    iAt = iAt + 1;
                    continue;
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

        // Forty-four settings in one flat list is not a help screen, it is an
        // inventory. They are grouped as a person would ask about them, wrapped
        // to a readable width, and followed by examples, because most people
        // read the examples and nothing else.
        static readonly string[,] asHelpGroups = new string[,] {
            { "What to describe", "source-paths begin minutes" },
            { "Where things go", "output-dir audio-only view-output force rebuild" },
            { "What it says", "context-file detail words-per-second summarise objective" },
            { "The voice", "voice rate ad-volume announce" },
            { "Where descriptions go", "every spacing min-gap forced-length noise-floor silence-length dialogue-channel max-silence" },
            { "Not saying the same thing twice", "similarity same-shot" },
            { "The model and the picture", "model url frames width crop-bottom" },
            { "Settings, logs and diagnostics", "use-configuration log-session log-file checkpoint mux-minutes ffmpeg-dir boxes gui check list-voices verbose help" },
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
            Console.WriteLine("HomerDescribe " + version() + ", audio description for local video files.");
            Console.WriteLine("");
            writeWrapped("", "Describes what happens on screen, speaks it in a Windows voice, and writes "
                + "a copy of the film with the description as its first audio track. Everything "
                + "runs on this machine.", 78);
            Console.WriteLine("");
            Console.WriteLine("Usage: HomerDescribe [videos, patterns or web addresses] [options]");
            Console.WriteLine("");
            writeWrapped("", "Run it with nothing at all to open the dialog. Name a file called after "
                + "the video and beside it, video.md for video.mkv, and its characters and setting "
                + "are used without being asked for.", 78);
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
            Console.WriteLine("Examples");
            Console.WriteLine("");
            Console.WriteLine("  HomerDescribe");
            writeWrapped("      ", "Open the dialog.", 78);
            Console.WriteLine("  HomerDescribe \"film.mkv\"");
            writeWrapped("      ", "Describe one film, writing the results into a folder called film beside it.", 78);
            Console.WriteLine("  HomerDescribe \"C:\\video\\*.mp4\" --audio-only");
            writeWrapped("      ", "Describe every mp4 in that folder, producing an mp3 of each rather than a film.", 78);
            Console.WriteLine("  HomerDescribe \"film.mkv\" --begin 00:22:30 --minutes 5");
            writeWrapped("      ", "Describe five minutes from twenty two and a half minutes in, to hear what it sounds like before committing to the whole film.", 78);
            Console.WriteLine("  HomerDescribe \"film.mkv\" --check");
            writeWrapped("      ", "Say whether ffmpeg, the voices and the model are all in place, and stop.", 78);
            Console.WriteLine("");
            writeWrapped("", "Full documentation is in ReadMe.htm beside the program.", 78);
        }

        static string version()
        {
            return BuildVersion.Version;
        }

        // ---------- the log ----------

        static string exeFolder()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        // Where the log lives cannot be settled until the settings are known,
        // and in dialog mode that is after the dialog has been answered. So
        // early lines are held in memory and written out once the file opens.
        //
        // It must not simply sit beside the program: installed, that is
        // C:\Program Files\HomerDescribe, which an ordinary user cannot write
        // to. It goes with the results instead.
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
            // leave a record -- but it is kept out of the way rather than sitting
            // among the results.
            if (!flag("log-session")) return appDataFolder();
            if (text("output-dir") != "") return text("output-dir");
            // No output folder given, so results go beside each video. The run
            // log goes beside the first of them.
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

        // A stable, private folder for one video's working files. The name of
        // the video alone would collide across folders, so the full path is
        // folded into a short tag.
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

        // Open the results in Explorer, with the described film selected, so
        // the thing just made is what the cursor lands on.
        static void showFolder(string sFolder)
        {
            try
            {
                string sSelect = "";
                foreach (string sFile in Directory.GetFiles(sFolder, sDefaultDescribedStem + ".*"))
                {
                    if (!sFile.EndsWith(".md")) sSelect = sFile;
                }
                if (sSelect != "") Process.Start("explorer.exe", "/select,\"" + sSelect + "\"");
                else Process.Start("explorer.exe", "\"" + sFolder + "\"");
                logMessage("Opened " + sFolder, "INFO", "");
            }
            catch (Exception oError)
            {
                logMessage("The results folder could not be opened: " + oError.Message, "ERROR");
            }
        }

        static string appDataFolder()
        {
            string sFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HomerDescribe");
            try
            {
                Directory.CreateDirectory(sFolder);
            }
            catch (Exception)
            {
            }
            return sFolder;
        }

        static void openLog()
        {
            if (fLog != null) return;
            string sPath = text("log-file");
            if (sPath == "") sPath = Path.Combine(chooseLogFolder(), sDefaultLogName);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(sPath)));
                fLog = new StreamWriter(sPath, false, new UTF8Encoding(true));
            }
            catch (Exception oError)
            {
                Console.WriteLine("The log could not be opened at " + sPath + ": " + oError.Message);
                sPath = Path.Combine(appDataFolder(), sDefaultLogName);
                try
                {
                    fLog = new StreamWriter(sPath, false, new UTF8Encoding(true));
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
            logMessage("Log file: " + sPath, "INFO", "Log: " + sPath);
        }

        static void closeLog()
        {
            if (fLog == null) return;
            logMessage("Log closed", "INFO", "");
            fLog.Flush();
            fLog.Close();
            fLog = null;
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
                }
                else if (oEarlyLog != null) oEarlyLog.AppendLine(sLine);
            }
            if (sLevel == "CMD" && !bVerbose) return;
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

        static void logEnvironment()
        {
            logMessage("HomerDescribe " + version() + " starting", "INFO", "HomerDescribe " + version());
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

        static bool bBoxes = false;
        static bool bGuiMode = false;
        static string sLastSkippedFolder = "";

        static void showTimedBox(string sCaption, string sBody)
        {
            if (!bBoxes) return;
            Thread threadBox = new Thread(delegate()
            {
                MessageBoxTimeoutW(IntPtr.Zero, sBody, sCaption, iMbOk | iMbSetForeground | iMbTopmost, 0, (uint)iDefaultBoxMs);
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
                        if (nLeft < 1.5) sLeft = "nearly there";
                        string sScreen = "  " + ((int)(nShare * 100)).ToString() + " percent scanned, " + sLeft;
                        if (sLabel == "") sScreen = "";
                        logMessage((sLabel == "" ? "Background" : sLabel) + ": reached " + formatClock(nAt) + " of " + formatClock(nDuration) + ", " + ((int)(nShare * 100)).ToString() + " percent",
                                   "INFO", sScreen);
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

        // Split a space separated list, keeping anything inside double quotes together.
        static List<string> splitPaths(string sList)
        {
            List<string> lItems = new List<string>();
            foreach (Match oMatch in Regex.Matches(sList, "\"([^\"]*)\"|(\\S+)"))
            {
                string sItem = oMatch.Groups[1].Success ? oMatch.Groups[1].Value : oMatch.Groups[2].Value;
                if (sItem.Trim() != "") lItems.Add(sItem.Trim());
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
                       "INFO", "Finding the pauses where descriptions can be spoken. This reads the whole film once, before anything is described, and takes a few minutes on a long one.");
            string sErr = runScan(sFfmpeg, "-hide_banner -i " + quoted(sPath) + " -af " + quoted(sFilter) + " -f null -", nDuration, "Scanning at " + num(nNoiseFloor) + " dB");
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

        static List<Moment> findGaps(string sFfmpeg, string sPath, double nDuration)
        {
            bool bCentre = false;
            if (text("dialogue-channel") != "off") bCentre = audioChannels(sFfmpeg, sPath) >= 6;
            List<double[]> lSilences = detectSilences(sFfmpeg, sPath, number("noise-floor"), number("silence-length"), bCentre, nDuration);
            List<Moment> lGaps = chooseGaps(lSilences, number("min-gap"), number("spacing"));
            lGaps = fillGaps(lGaps, nDuration, number("every"), number("forced-length"));
            logMessage("Describing " + lGaps.Count.ToString() + " moments across " + formatClock(nDuration),
                       "INFO", "Scan finished. Describing " + lGaps.Count.ToString() + " moments across " + formatClock(nDuration) + ". Each description follows as it is made.");
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

        static string promptFor(int iMaxWords, List<string> lRecent, string sContext, bool bNewScene, bool bOverSound, List<string> lNames)
        {
            StringBuilder oPrompt = new StringBuilder();
            if (sContext.Trim() != "") oPrompt.Append("About this film: " + sContext.Trim() + "\n\n");
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
            "striking", "haunting", "poignant", "graceful", "gracefully", "elegant"
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

        // The second stage, after AutoAD-Zero (Xie et al., Oxford VGG): a vision
        // model is asked to look thoroughly, and a language model then compresses
        // what it saw into one spoken description. Training-free, and competitive
        // with models fine-tuned on real audio description.
        //
        // The reason it works is that perceiving and being concise are different
        // jobs. Asked to do both at once, a vision model spends its attention on
        // the picture and its words on whatever comes first. Separating them lets
        // the first stage look hard and the second stage write well.
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
            string sAnswer = postJson(text("url") + "/api/generate", oSerializer.Serialize(dPayload));
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

        static string describeImage(string sImagePath, int iMaxWords, List<string> lRecent, string sContext, bool bAgain, bool bNewScene, string sJudgment, bool bOverSound, List<string> lNames)
        {
            string sPrompt = promptFor(iMaxWords, lRecent, sContext, bNewScene, bOverSound, lNames);
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
            string sAnswer = postJson(text("url") + "/api/generate", oSerializer.Serialize(dPayload));
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

        static readonly string[] asClauseTrims = new string[] {
            @",?\s*(as|while|and|with)?\s*the\s+camera[^,.;]*",
            @",?\s*(before\s+|then\s+)?(transitioning|cutting|panning|zooming|shifting)\s+(to|into|across)[^,.;]*"
        };

        static readonly string[] asFilmTalk = new string[] {
            @"\b(camera|frames?|panels?|footage|montage)\b",
            @"\bthe\s+(shot|image|picture|still|sequence)\b",
            @"\b(scene|view|perspective|focus)\s+(then\s+)?(shifts?|cuts?|turns?|changes?|switches?)\b",
            @"\bwe\s+(see|watch|observe|are\s+shown)\b"
        };

        static List<string> splitSentences(string sText)
        {
            List<string> lParts = new List<string>();
            foreach (Match oMatch in Regex.Matches(sText, @"[^.!?]*[.!?]"))
            {
                if (oMatch.Value.Trim() != "") lParts.Add(oMatch.Value.Trim());
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
        // sentence. Refuses when the result would be too short to say anything,
        // or would end on a word that needs something after it.
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
            // A sentence that opens with a place or a time -- "At an ancient
            // site near water, shadows stretch across the stones" -- must not be
            // cut back to that opening, which leaves a phrase and no sentence.
            // If nothing but the opening phrase would remain, leave it alone.
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

        // The name of the finished thing: the film in its own container, or a
        // single mp3 when only the sound is wanted.
        static string outputName(string sInput)
        {
            if (flag("audio-only")) return sDefaultDescribedStem + ".mp3";
            return sDefaultDescribedStem + Path.GetExtension(sInput);
        }

        static bool muxOutput(string sFfmpeg, string sVideo, string sAdWave, string sOutPath, double nDuration, bool bBackground)
        {
            string sFilter = "[1:a]aformat=sample_fmts=fltp:sample_rates=48000:channel_layouts=stereo,volume=" + num(number("ad-volume")) + ",asplit=2[adDuck][adMix];"
                           + "[0:a]aformat=sample_fmts=fltp:sample_rates=48000:channel_layouts=stereo[main];"
                           + "[main][adDuck]sidechaincompress=threshold=0.01:ratio=20:attack=5:release=300[duck];"
                           + "[duck][adMix]amix=inputs=2:duration=first:normalize=0[mix]";
            if (flag("audio-only"))
            {
                // Sound only. No video is copied, so this is minutes of work
                // rather than the whole film rewritten, and the result is a
                // fraction of the size.
                string sAudioArgs = "-hide_banner -y -i " + quoted(sVideo) + " -i " + quoted(sAdWave)
                                  + " -filter_complex " + quoted(sFilter)
                                  + " -map " + quoted("[mix]") + " -vn -c:a libmp3lame -q:a 4"
                                  + " -metadata " + quoted("title=" + Path.GetFileNameWithoutExtension(sVideo) + ", with audio description")
                                  + " " + quoted(sOutPath + ".part");
                logMessage("Writing the described audio to " + sOutPath,
                           "INFO", bBackground ? "" : "Writing the described audio. No video is copied, so this is quick.");
                runScan(sFfmpeg, sAudioArgs, nDuration, bBackground ? "" : "Writing the audio");
                if (iLastScanExit != 0)
                {
                    logMessage("The described audio could not be written. If ffmpeg has no mp3 encoder, install a build that has libmp3lame.", "ERROR");
                    try
                    {
                        if (File.Exists(sOutPath + ".part")) File.Delete(sOutPath + ".part");
                    }
                    catch (Exception)
                    {
                    }
                    return false;
                }
                try
                {
                    if (File.Exists(sOutPath)) File.Delete(sOutPath);
                    File.Move(sOutPath + ".part", sOutPath);
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
            string sPartPath = sOutPath + ".part";
            sArguments = sArguments.Replace(quoted(sOutPath), quoted(sPartPath));
            logMessage("Writing the described film to " + sOutPath,
                       "INFO", bBackground ? "" : "Writing the described film. This takes roughly a minute for every thirty minutes of film.");
            runScan(sFfmpeg, sArguments, nDuration, bBackground ? "" : "Writing the film");
            if (iLastScanExit != 0)
            {
                logMessage("The described film could not be written.", "ERROR", bBackground ? "" : null);
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

        static void writeCache(List<Moment> lMoments, string sPath, bool bFinished)
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
            JavaScriptSerializer oSerializer = new JavaScriptSerializer();
            oSerializer.MaxJsonLength = int.MaxValue;
            StreamWriter fJson = new StreamWriter(sPath, false, new UTF8Encoding(true));
            fJson.Write(oSerializer.Serialize(dData));
            fJson.Close();
        }

        static bool bLastCacheFinished = false;

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

        static void saveReadable(List<Moment> lMoments, string sOutputDir, string sWorkDir, string sSourceName, double nDuration)
        {
            if (lMoments.Count == 0) return;
            writeCache(lMoments, Path.Combine(sWorkDir, sDefaultJsonName), false);
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
        // which drops results among the sources. HomerDescribe deals in video,
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
                // The boxes open holding whatever was last entered, so a
                // mistyped path can be corrected rather than typed again.
                string sSources = text("source-paths");
                string sOutput = text("output-dir");
                if (sOutput == "") sOutput = defaultVideoFolder();
                bool bForce = flag("force");
                bool bLogSession = flag("log-session");
                bool bUseConfig = flag("use-configuration");
                bool bViewOutput = flag("view-output");
                bool bAudioOnly = flag("audio-only");
                using (LbcDialog oDialog = new LbcDialog("HomerDescribe", null))
                {
                    oDialog.addBand();
                    TextBox oSourceBox = oDialog.addInputBox("&Source paths:", sSources,
                        "One or more video files, or YouTube page addresses to download from, separated by spaces. " +
                        "Put double quotes around any item containing a space.");
                    Button oBrowseButton = oDialog.addButton("&Browse source...",
                        "Choose a video file to describe.");

                    oDialog.addBand();
                    TextBox oOutputBox = oDialog.addInputBox("&Output directory:", sOutput,
                        "Where each video's folder of results is created. Starts at your Videos folder. " +
                        "Cleared, each video's folder is created beside the video itself instead.");
                    Button oChooseButton = oDialog.addButton("&Choose output...",
                        "Choose the directory to write results into.");
                    oDialog.endBand();

                    oBrowseButton.Click += delegate(object oSender, EventArgs oEvent)
                    {
                        OpenFileDialog oPicker = new OpenFileDialog();
                        oPicker.Title = "Choose a video to describe";
                        try
                        {
                            oPicker.InitialDirectory = initialBrowseFolder(oSourceBox.Text);
                        }
                        catch (Exception)
                        {
                        }
                        oPicker.Filter = "Video files|*.mkv;*.mp4;*.avi;*.mov;*.webm;*.mpg;*.mpeg;*.m4v;*.wmv|All files|*.*";
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

                    oDialog.addSeparator();
                    CheckBox oForceBox = oDialog.addCheckBox("&Force overwrite", bForce,
                        "Describe everything again, ignoring anything an earlier run had already written.");
                    CheckBox oLogBox = oDialog.addCheckBox("&Log session", bLogSession,
                        "Write a copy of HomerDescribe.log into the output directory as well as beside the program.");
                    CheckBox oConfigBox = oDialog.addCheckBox("&Use configuration", bUseConfig,
                        "Load these settings at startup and save them on OK, in " + configPath() + ".");
                    CheckBox oAudioBox = oDialog.addCheckBox("&Audio only", bAudioOnly,
                        "Produce sound only: one mp3 holding the film's own audio with the descriptions mixed into it, and no video. " +
                        "Far smaller than the film, quicker to make, and enough when the picture is of no use to the listener.");
                    CheckBox oViewBox = oDialog.addCheckBox("&View output", bViewOutput,
                        "Open the folder holding the described film and its script when the run finishes, so it does not have to be hunted for.");

                    sButton = oDialog.runWithButtons(new string[] { "OK", "Cancel" });

                    sSources = (oSourceBox.Text == null ? "" : oSourceBox.Text).Trim();
                    sOutput = (oOutputBox.Text == null ? "" : oOutputBox.Text).Trim();
                    bForce = oForceBox.Checked;
                    bLogSession = oLogBox.Checked;
                    bUseConfig = oConfigBox.Checked;
                    bViewOutput = oViewBox.Checked;
                    bAudioOnly = oAudioBox.Checked;
                }

                if (sButton == null || sButton == "" || sButton == "Cancel") return false;

                dParams["source-paths"].sValue = sSources;
                dParams["output-dir"].sValue = sOutput;
                dParams["force"].sValue = bForce ? "yes" : "no";
                dParams["log-session"].sValue = bLogSession ? "yes" : "no";
                dParams["use-configuration"].sValue = bUseConfig ? "yes" : "no";
                dParams["view-output"].sValue = bViewOutput ? "yes" : "no";
                dParams["audio-only"].sValue = bAudioOnly ? "yes" : "no";

                if (sSources == "")
                {
                    MessageBox.Show("Give at least one video file or YouTube address.", "HomerDescribe",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    continue;
                }
                if (bUseConfig) saveConfig();
                return true;
            }
        }

        // ---------- the configuration file ----------

        static string configPath()
        {
            return Path.Combine(appDataFolder(), "HomerDescribe.ini");
        }

        // A settings file left beside the program by an earlier build, or put
        // there deliberately in a development folder, is still read. It is never
        // written there.
        static string configPathToRead()
        {
            if (File.Exists(configPath())) return configPath();
            string sBeside = Path.Combine(exeFolder(), "HomerDescribe.ini");
            if (File.Exists(sBeside)) return sBeside;
            return configPath();
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
                        // A key the dialog no longer offers is left behind rather
                        // than applied, so an old file cannot pin a stale default.
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

        // Only what the dialog actually offers is remembered. Saving every
        // setting freezes the built-in defaults forever: a settings file written
        // by an older build kept handing back its idea of mux-minutes long after
        // the default had changed, and nothing on screen said so.
        static readonly string[] asRemembered = new string[] {
            "source-paths", "output-dir", "force", "log-session", "use-configuration", "view-output", "audio-only"
        };

        static bool isRemembered(string sName)
        {
            foreach (string sOne in asRemembered)
            {
                if (sOne == sName) return true;
            }
            return false;
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
                    "HomerDescribe", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ---------- the run ----------

        // Run a long program, reporting the odd line of its output so the
        // screen is not silent for minutes. Used for downloads.
        static int runStreamed(string sProgram, string sArguments, string sLabel)
        {
            logMessage("Command: " + sProgram + " " + sArguments, "CMD");
            DateTime dtBegan = DateTime.Now;
            DateTime dtLast = DateTime.Now;
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
            string sLine = oProcess.StandardOutput.ReadLine();
            while (sLine != null)
            {
                string sTrimmed = sLine.Trim();
                if (sTrimmed != "")
                {
                    logMessage(sTrimmed, "INFO", "");
                    if (DateTime.Now.Subtract(dtLast).TotalSeconds >= iDefaultScanReport)
                    {
                        dtLast = DateTime.Now;
                        logMessage("", "INFO", "  " + sLabel + ": " + sTrimmed);
                    }
                }
                sLine = oProcess.StandardOutput.ReadLine();
            }
            oProcess.WaitForExit();
            logMessage("Exit code " + oProcess.ExitCode.ToString() + " after " + num(DateTime.Now.Subtract(dtBegan).TotalSeconds) + " seconds", "CMD");
            if (oProcess.ExitCode != 0) logMessage("Error output: " + tail(oErr.ToString(), 1500), "ERROR", "");
            return oProcess.ExitCode;
        }

        // Expand a source that names several files at once. A star or a question
        // mark is a pattern, not a path, and Path.GetFullPath throws on one.
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
            foreach (string sFile in asFiles) lFound.Add(sFile);
            logMessage(sSource + " matches " + lFound.Count.ToString() + " files",
                       "INFO", sSource + " matches " + lFound.Count.ToString() + " files.");
            if (lFound.Count == 0) logMessage("Nothing matched " + sSource, "ERROR");
            return lFound;
        }

        // Fetch a video from a web page.
        //
        // This is handed to yt-dlp rather than done in C#. Extracting a video
        // from YouTube is not a matter of reading a page: the addresses are
        // signed by obfuscated JavaScript that has to be run, the signing
        // changes without notice, and formats are negotiated per video. yt-dlp
        // tracks all of that and is updated most weeks. A library inside
        // HomerDescribe would have to be maintained against a moving target
        // that has nothing to do with audio description, and would break
        // silently on a Tuesday. Calling the program that already solves the
        // problem is the smaller and more honest dependency.
        //
        // Two details matter. --print implies --simulate unless --no-simulate
        // is given, so without it yt-dlp would report a path and download
        // nothing. And the best video and best audio arrive as separate
        // streams that ffmpeg merges, so yt-dlp is told where ffmpeg is,
        // rather than being left to find it on the PATH.
        static string fetchFromWeb(string sAddress, string sFolder, string sFfmpeg)
        {
            string sYtDlp = findTool("yt-dlp");
            if (sYtDlp == "")
            {
                logMessage("yt-dlp was not found, so " + sAddress + " cannot be downloaded.", "ERROR");
                logMessage("Install it with:  winget install yt-dlp.yt-dlp", "HINT");
                if (bGuiMode) MessageBox.Show("yt-dlp is needed to download from a web address, and was not found.\r\n\r\n" +
                    "Install it with:  winget install yt-dlp.yt-dlp", "HomerDescribe", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            logMessage("Downloading " + sAddress + " with " + sYtDlp, "INFO", "Downloading " + sAddress);
            showTimedBox("Downloading", sAddress);
            string sArguments = "--no-playlist --no-simulate --newline --restrict-filenames"
                              + " --merge-output-format mkv"
                              + " --ffmpeg-location " + quoted(Path.GetDirectoryName(sFfmpeg))
                              + " --print-to-file after_move:filepath " + quoted(sPathFile)
                              + " -f " + quoted("bv*+ba/b")
                              + " -o " + quoted(Path.Combine(sFolder, "%(title)s.%(ext)s"))
                              + " " + quoted(sAddress);
            int iCode = runStreamed(sYtDlp, sArguments, "Downloading");
            if (iCode != 0)
            {
                // YouTube changes how it serves video every few weeks, and an
                // older yt-dlp stops working: "Precondition check failed",
                // HTTP 400 on the player API, and then "Requested format is not
                // available" because no video format was found at all. The cure
                // is almost always a newer yt-dlp.
                logMessage("The download failed. An out of date yt-dlp is the usual cause.", "ERROR");
                logMessage("Updating yt-dlp and trying once more.", "INFO", "The download failed. Updating yt-dlp and trying once more.");
                string sUpOut = "";
                string sUpErr = "";
                runCommand(sYtDlp, "-U", out sUpOut, out sUpErr);
                foreach (string sLine in (sUpOut + sUpErr).Split('\n'))
                {
                    if (sLine.Trim() != "") logMessage("  " + sLine.Trim(), "INFO", "");
                }
                iCode = runStreamed(sYtDlp, sArguments, "Downloading");
            }
            if (iCode != 0)
            {
                string sAdvice = "The download failed again, even after updating yt-dlp.\r\n\r\n"
                               + "Try by hand, to see what it says:\r\n"
                               + "  yt-dlp -U\r\n"
                               + "  yt-dlp \"" + sAddress + "\"\r\n\r\n"
                               + "Some videos cannot be downloaded at all: private, age restricted, or members only. "
                               + "If the address plays only when signed in, HomerDescribe cannot reach it either.";
                logMessage(sAdvice.Replace("\r\n", " "), "HINT");
                if (bGuiMode) MessageBox.Show(sAdvice, "HomerDescribe", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            // Checked on every attempt, because a person may have started
            // Ollama between one try and the next.
            bool bReady = checkEnvironment();
            if (flag("check"))
            {
                logMessage("Check finished. Prerequisites " + (bReady ? "passed" : "FAILED"),
                           "INFO", "Check finished. Prerequisites " + (bReady ? "passed." : "FAILED."));
                return bReady ? 0 : 1;
            }
            if (!bReady) return 1;
            List<string> lGiven = splitPaths(text("source-paths"));
            List<string> lSources = new List<string>();
            foreach (string sGiven in lGiven)
            {
                if (sGiven.StartsWith("http://") || sGiven.StartsWith("https://")) lSources.Add(sGiven);
                else foreach (string sOne in expandPattern(sGiven)) lSources.Add(sOne);
            }
            if (lSources.Count == 0)
            {
                string sTried = text("source-paths").Trim();
                string sSaid = sTried == ""
                    ? "No source was given, so there is nothing to describe."
                    : "Nothing was found matching:" + Environment.NewLine + Environment.NewLine + sTried;
                logMessage(sSaid.Replace(Environment.NewLine, " "), "ERROR");
                if (!bGuiMode) logMessage("Name a video file or a YouTube address, or run HomerDescribe with no arguments for the dialog.", "HINT");
                if (bGuiMode) MessageBox.Show(sSaid, "HomerDescribe", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // Nothing to do is not a failure to report and exit on: from the
                // dialog it means the person mistyped a path, and what they want
                // next is the dialog back with their text still in it.
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
                if (lSources.Count > 1) logMessage("Source " + iAt.ToString() + " of " + lSources.Count.ToString() + ": " + sSource,
                                                   "INFO", "Source " + iAt.ToString() + " of " + lSources.Count.ToString() + ": " + sSource);
                string sPath = sSource;
                if (sSource.StartsWith("http://") || sSource.StartsWith("https://"))
                {
                    string sFolder = text("output-dir");
                    if (sFolder == "") sFolder = Path.Combine(exeFolder(), "downloads");
                    sPath = fetchFromWeb(sSource, sFolder, findTool("ffmpeg"));
                    if (sPath == "")
                    {
                        iWorst = 1;
                        continue;
                    }
                }
                int iOne = 1;
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
                try
                {
                    iOne = runOne(sFull);
                }
                finally
                {
                    // Wherever the run ended, this video's own folder gets its
                    // own copy of everything that was said about it.
                    string sBase = text("output-dir");
                    if (sBase == "") sBase = Path.GetDirectoryName(sFull);
                }
                if (iOne == iAlreadyDone) iSkippedWhole = iSkippedWhole + 1;
                if (iOne == 0) iDescribedWhole = iDescribedWhole + 1;
                if (iOne != 0 && iOne != iAlreadyDone) iWorst = iOne;
            }
            // Every one of them was already done. That is not a failure, but it
            // is not a result either, and a run that ends without a word looks
            // like a program that did not start.
            if (iDescribedWhole == 0 && iSkippedWhole > 0)
            {
                string sSaid = iSkippedWhole == 1
                    ? "That video has already been described, so there was nothing to do."
                    : "All " + iSkippedWhole.ToString() + " of those videos have already been described, so there was nothing to do.";
                sSaid = sSaid + Environment.NewLine + Environment.NewLine
                      + (bGuiMode ? "Tick Force overwrite to describe them again." : "Pass --force to describe them again.");
                logMessage(sSaid.Replace(Environment.NewLine, " "), "INFO", bGuiMode ? "" : sSaid);
                if (bGuiMode && MessageBox.Show(sSaid + Environment.NewLine + Environment.NewLine
                        + "Open the folder holding what was described before?",
                        "HomerDescribe", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    showFolder(sLastSkippedFolder);
                }
                return iNothingToDo;
            }
            if (iSkippedWhole > 0) logMessage(iSkippedWhole.ToString() + " already described and skipped; " + iDescribedWhole.ToString() + " described.",
                                              "INFO", iSkippedWhole.ToString() + " already described and skipped.");
            return iWorst;
        }

        // Confirm the programs and the model are in place. Done once, whatever
        // the number of sources.
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
            if (sYtDlp == "") logMessage("yt-dlp was not found. Video files still work; web addresses do not.", "INFO", "");
            if (sYtDlp != "")
            {
                logMessage("Found yt-dlp at " + sYtDlp, "INFO", "");
                string sVerOut = "";
                string sVerErr = "";
                runCommand(sYtDlp, "--version", out sVerOut, out sVerErr);
                logMessage("  yt-dlp version " + (sVerOut + sVerErr).Trim(), "INFO", "");
            }
            return checkOllama();
        }

        static int runOne(string sInput)
        {
            string sFfmpeg = findTool("ffmpeg");
            if (!File.Exists(sInput))
            {
                logMessage("The video file was not found: " + sInput, "ERROR");
                return 1;
            }
            // Each video gets a folder of its own, named after the video, so a
            // run over several videos keeps their results apart. Without an
            // output directory the folder sits beside the video itself.
            string sRoot = Path.GetFileNameWithoutExtension(sInput);
            string sBase = text("output-dir");
            if (sBase == "") sBase = Path.GetDirectoryName(sInput);
            string sOutputDir = Path.Combine(sBase, sRoot);

            // Finished means the described film is there, not merely that the
            // folder is. The folder appears the moment work begins, so testing
            // for the folder would have skipped every interrupted run instead of
            // resuming it, which is the opposite of what is wanted.
            string sFilmPath = Path.Combine(sOutputDir, outputName(sInput));
            // Finished means BOTH the film is there AND the record says the run
            // reached the end. A film left half written by an interrupted run
            // exists on disk and must not be mistaken for a finished one.
            bLastCacheFinished = false;
            if (File.Exists(Path.Combine(workFolderFor(sInput), sDefaultJsonName))) readCache(Path.Combine(workFolderFor(sInput), sDefaultJsonName));
            if (File.Exists(sFilmPath) && bLastCacheFinished && !flag("force"))
            {
                sLastSkippedFolder = sOutputDir;
                logMessage("Skipping " + sRoot + ": " + sFilmPath + " already exists. "
                           + (bGuiMode ? "Tick Force overwrite to describe it again." : "Pass --force to describe it again."),
                           "INFO", "Skipping " + sRoot + ", already described.");
                return iAlreadyDone;
            }
            if (Directory.Exists(sOutputDir) && !flag("force"))
            {
                logMessage("An unfinished run is here. Carrying on from where it stopped; nothing already described is described again.",
                           "INFO", "An unfinished run is here. Carrying on from where it stopped.");
            }

            // The video's folder holds only what a person would open: the
            // described film and the script to read. The working files -- the
            // record used to resume, the caption file, the description track,
            // the montages -- go under the user's application data, where they
            // are out of the way but still findable.
            string sWorkDir = workFolderFor(sInput);
            Directory.CreateDirectory(sOutputDir);
            Directory.CreateDirectory(sWorkDir);
            logMessage("Working files are under " + sWorkDir, "INFO", "");
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

            if (!openVoice()) return 1;

            Dictionary<string, string> dCache = new Dictionary<string, string>();
            string sJsonPath = Path.Combine(sWorkDir, sDefaultJsonName);
            if (!flag("force")) dCache = readCache(sJsonPath);
            if (dCache.Count > 0) logMessage("Picking up where the last run stopped: " + dCache.Count.ToString() + " descriptions already written",
                                             "INFO", "Carrying on from " + dCache.Count.ToString() + " descriptions already written.");
            if (flag("rebuild") && dCache.Count == 0)
            {
                logMessage("Rebuild was asked for, but there are no descriptions here to rebuild from.", "ERROR");
                return 1;
            }
            if (flag("rebuild")) logMessage("Rebuilding the film from " + dCache.Count.ToString() + " descriptions already made. The model is not consulted.",
                                            "INFO", "Rebuilding the film from " + dCache.Count.ToString() + " descriptions already made. Nothing is described again.");

            List<Moment> lGaps = findGaps(sFfmpeg, sInput, nDuration);
            List<Moment> lDone = new List<Moment>();
            List<string> lRecent = new List<string>();
            List<string> lNames = new List<string>();
            byte[] binLast = new byte[0];
            int iIndex = 0;
            int iSkipped = 0;
            int iLastPercent = -1;
            double nLastSpoken = -1.0;
            DateTime dtBegan = DateTime.Now;
            DateTime dtLastMux = DateTime.Now;

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
                string sText = "";
                bool bFromCache = dCache.ContainsKey(num(oGap.nStart));
                if (bFromCache) sText = dCache[num(oGap.nStart)];
                if (!bFromCache)
                {
                    // Rebuilding: speak and assemble what is already written,
                    // and ask the model nothing. A moment never described stays
                    // undescribed.
                    if (flag("rebuild")) continue;
                    if (!buildMontage(sFfmpeg, sInput, oGap.nStart + oGap.nLength / 2.0, oGap.nLength + 2.0, sImagePath)) continue;
                    bool bNewScene = false;
                    if (number("same-shot") > 0.0)
                    {
                        byte[] binNow = shotSignature(sFfmpeg, sImagePath, sWorkDir);
                        double nMoved = signatureDistance(binLast, binNow);
                        if (binLast.Length > 0 && nMoved < number("same-shot"))
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
                    sText = describeImage(sImagePath, iLookWords, lShown, sContext, false, bNewScene, "", oGap.bForced, lNames);
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
                        sText = describeImage(sImagePath, iMaxWords, lShown, sContext, true, bNewScene, "", oGap.bForced, lNames);
                        nLike = worstLikeness(sText, lAgainst);
                    }
                    // One chance to replace a judgment with what was seen.
                    string sJudged = judgmentFound(sText);
                    if (sText != "" && sJudged != "" && flag("objective"))
                    {
                        logMessage("That description judged rather than observed, at the word \"" + sJudged + "\". Asking again.", "INFO", "");
                        string sBetter = describeImage(sImagePath, iMaxWords, lShown, sContext, false, bNewScene, sJudged, oGap.bForced, lNames);
                        if (sBetter != "" && worstLikeness(sBetter, lAgainst) < number("similarity")) sText = sBetter;
                        if (judgmentFound(sText) != "") logMessage("It still judges, at \"" + judgmentFound(sText) + "\". Keeping it rather than losing the moment.", "INFO", "");
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
                        string sSurer = describeImage(sImagePath, iMaxWords, lShown, sContext, false, bNewScene, "", oGap.bForced, lNames);
                        if (sSurer != "" && hedgeFound(sSurer) == "") sText = sSurer;
                    }
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
                // Still too long, and it is one sentence, so dropping sentences
                // cannot help. Shorten it at clause boundaries instead: a
                // description of this kind opens with a main clause and hangs
                // detail off it in commas, so cutting the last comma clause
                // leaves a sentence rather than a fragment. Words are never cut
                // off the end -- "A man stands on a hilltop under bright" is
                // worse than a description that runs a second long.
                while (pcmSeconds(binAudio) > nAllowed)
                {
                    string sShorter = dropLastClause(sText);
                    if (sShorter == sText) break;
                    sText = sShorter;
                    binAudio = speakToPcm(sText, iRate);
                }
                // No clauses left to drop. Ask the model to say it again in
                // fewer words, which is the only way left to shorten it and
                // still have it read as English.
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
                lDone.Add(oGap);
                lRecent.Add(sText);
                gatherNames(sText, lNames);
                if (lRecent.Count > iDefaultCompare) lRecent.RemoveAt(0);
                logMessage("Moment " + iIndex.ToString() + " of " + lGaps.Count.ToString() + " at " + num(oGap.nStart) + "s, " + num(oGap.nSpoken) + "s of " + num(oGap.nLength) + "s: " + sText,
                           "INFO", formatClock(oGap.nStart) + "  " + sText);
                // The caption carries the position, the body the description
                // itself, so a screen reader reads both without being asked.
                int iPercent = (int)(oGap.nStart * 100.0 / Math.Max(nDuration, 1.0));
                string sCaption = formatClock(oGap.nStart);
                if (iPercent != iLastPercent) sCaption = sCaption + ", " + iPercent.ToString() + " percent of " + Path.GetFileName(sInput);
                if (iPercent != iLastPercent) logMessage("Reached " + iPercent.ToString() + " percent of " + Path.GetFileName(sInput), "INFO", "");
                iLastPercent = iPercent;
                showTimedBox(sCaption, sText);
                saveReadable(lDone, sOutputDir, sWorkDir, Path.GetFileName(sInput), nDuration);
                if (iIndex % 10 == 0)
                {
                    double nEach = DateTime.Now.Subtract(dtBegan).TotalSeconds / (double)iIndex;
                    logMessage("Progress: " + iIndex.ToString() + " of " + lGaps.Count.ToString() + ", " + num(nEach) + " seconds each",
                               "INFO", "-- " + iIndex.ToString() + " of " + lGaps.Count.ToString() + " done, about " + ((int)Math.Round(nEach * (lGaps.Count - iIndex) / 60.0)).ToString() + " minutes left --");
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
            writeCache(lDone, Path.Combine(sWorkDir, sDefaultJsonName), true);
            // The run is over, so the bulky intermediates go. The record stays,
            // because --rebuild works from it and it costs a few kilobytes.
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
            if (bGuiMode) MessageBox.Show(sFinished, "HomerDescribe finished", MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (flag("view-output")) showFolder(sOutputDir);
            return 0;
        }

        [STAThread]
        static int Main(string[] asArgs)
        {
            buildParams();
            if (!parseArgs(asArgs))
            {
                showHelp();
                return 1;
            }
            bVerbose = flag("verbose");
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
            logMessage("Mode: " + (bGuiMode ? "dialog" : "command line"), "INFO", "");

            // Checkboxes start unticked. A settings file is read only when it
            // records that Use configuration was ticked last time -- otherwise
            // the checkbox could never be turned on, since turning it on is what
            // writes the file.
            if (bGuiMode && !dParams["use-configuration"].bGiven && savedSaysUseConfiguration()) dParams["use-configuration"].sValue = "yes";
            if (flag("use-configuration")) loadConfig();

            // Boxes are on by default in dialog mode, off on the command line,
            // where every description is already printed.
            bBoxes = bGuiMode;
            if (dParams["boxes"].bGiven) bBoxes = flag("boxes");

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
                closeLog();
            }
            return iResult;
        }
    }
}
