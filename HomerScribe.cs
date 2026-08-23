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
using System.IO.Compression;
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
        // Captions name who is speaking. Whisper cannot, so this is empty for
        // anything heard rather than read.
        public string sWho;

        public Speech()
        {
            nStart = 0.0;
            nEnd = 0.0;
            sText = "";
            sWho = "";
        }
    }

    public class HomerScribe
    {
        const int iNothingToDo = 2;
        const int iAlreadyDone = 3;
        const string sAlreadyDone = "*already*";
        // Returned by fetchFromWeb when the captions were taken and the film
        // was deliberately not. There is no local file to hand back.
        const string sCaptionsOnly = "*captions*";
        const int iDefaultCompare = 10;
        const int iDefaultNames = 12;
        const int iDefaultRecent = 2;
        const int iDefaultRememberEvery = 25;
        const int iDefaultSampleRate = 48000;
        const int iDefaultScanReport = 10;
        const int iDefaultSpokenReport = 20;
        const int iDefaultGroupLength = 700;
        const int iDefaultEarlyMessages = 8;
        const int iDefaultGiveUpAfter = 12;
        const double nDefaultEchoWorry = 0.5;
        const int iDefaultConsoleLine = 160;
        const int iDefaultPumpRest = 40;
        const int iDefaultSpokenLimit = 1500;
        const int iDefaultBigPlaylist = 12;
        const int iDefaultTimeout = 300000;
        const int iDefaultHeartbeat = 25;
        const double nDefaultChapter = 600.0;
        const double nDefaultNewScene = 25.0;
        const double nDefaultSlowSeconds = 30.0;
        const double nDefaultLookBack = 14.0;
        const double nDefaultTalkative = 70.0;
        const double nDefaultCrowded = 85.0;
        const double nDefaultTalkativeEvery = 45.0;
        const double nDefaultEvery = 14.0;
        const double nDefaultTooQuiet = 2.0;
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
        // Named to match described.mkv. It was descriptions.md, which was the
        // odd one out among the documents.
        const string sDefaultPicturesName = "described.md";
        // What the model can be shown. Ollama reads these; anything else is
        // turned into a PNG first, and anything that cannot be is passed over.
        const string sDefaultSeenKinds = ".png,.jpg,.jpeg,.webp,.gif";
        // What it can be shown once ffmpeg has turned it into a PNG.
        const string sDefaultConvertKinds = ".bmp,.tif,.tiff";
        // Where a description can be stored inside the picture itself. GIF has
        // only a comment block and BMP has nowhere at all, which is a fact
        // about those formats and not something to be worked around.
        const string sDefaultMetadataKinds = ".png,.jpg,.jpeg,.webp,.tif,.tiff";
        const string sDefaultPartMetadataKinds = ".gif";
        // Longest root of a friendly name. There is no Microsoft figure for a
        // RECOMMENDED length -- 255 is the ceiling for one name and 260 for a
        // whole path -- and portable practice sits well under both.
        //
        // Seventy-nine, after PEP 8's line length, which he raised. It was
        // sixty, and that was never the binding constraint: across
        // twenty-four real pictures the longest name the model produced was
        // twenty-four characters. The allowance matters only once the model is
        // told to use it, which it now is.
        const int iDefaultNameLength = 79;
        // The joining words that are lowered when an identifier is turned back
        // into a phrase. Nothing else is touched, so a name keeps its capital.
        const string sDefaultSmallWords = "a,an,and,as,at,be,by,for,from,in,into,of,on,onto,or,over,"
                                        + "the,to,under,up,with,without,near,beside,among,against,during";
        // Characters that are LEGAL in a Windows file name and break things
        // anyway. From Rentitle, where the comment beside them records that
        // they stopped file-not-found errors in Python and in FileDir.
        const string sDefaultBadLetters = "\u2018\u2019\u201a\u201c\u201d\u2026\u2013\u2014\u2039\u203a"
                                        + "\u00ab\u00bb\u00a1\u00a8\u00a3\u00a5\u20ac\u2217\u00d7\u221a"
                                        + "\u2264\u2310\u25aa\u25ab\u25bc\u25cb\u25cf\u25e6\u2192\u2190"
                                        + "\u21b5\u00a7\u00a9\u00ae\u00b0\u00b5\u00b6\u00b7\u2630\u2713"
                                        + "\u272a\u274f\u2705\u274c\u2022\u29c9\u00bc\u00bd\u00be\u279a"
                                        + "\u221e\u00aa\u2122\u00ff\u2500\u2502\u250c\u2534\u253c\u2588";
        // Answers that are not names. A vision model hands back "Image" and
        // "Photo" as readily as anything, and a folder of pictures all called
        // Image is worse than one full of IMG_4471. Rentitle carries the same
        // idea as a list of placeholder document titles.
        const string sDefaultNoNames = "image,images,picture,pictures,photo,photos,photograph,screenshot,"
                                     + "untitled,unnamed,unknown,none,null,title,document,file,img,scan,"
                                     + "no title,not known,n a,unspecified,new,copy";
        // Rentitle stops after a thousand. Better to give up than to loop.
        const int iDefaultMostClashes = 1000;
        // Captions. Two cues closer together than this belong to one passage,
        // and a passage stops growing once it is about a paragraph long.
        const double nDefaultJoinWithin = 2.0;
        // A passage stops at about a paragraph -- but only where a sentence
        // stops. Breaking mid-clause reads as though the speaker was cut off,
        // and in prose that is a real loss where in a timed list it was
        // invisible. So it may run on to the second figure while it waits for
        // a full stop.
        const int iDefaultParagraph = 260;
        const int iDefaultParagraphMost = 600;
        // What a caption track must call itself to be used. Only English is
        // supported, so anything else is left to Whisper.
        //
        // This is an EXACT list on purpose. Anything beginning "en-" used to
        // count, and that is precisely how YouTube names a machine translation
        // OUT OF English: en-ar is Arabic, en-sq is Albanian. Asking for those
        // fetched sixty-six tracks from one video and earned a 429.
        //
        // "en-ca" and "en-in" are left out although they read like Canadian and
        // Indian English. YouTube uses them for Catalan and Indonesian, and
        // getting a translation is worse than missing a regional spelling.
        const string sDefaultEnglishTags = "en,eng,english,en-orig,en-en,en-us,en-gb,en-au,en-nz,en-ie";

        static StreamWriter fLog = null;
        static FileStream fLogStream = null;
        static DateTime dtLogFlushed = DateTime.MinValue;
        static string sLogPath = "";

        // Everything logged while one film is being worked on, kept so that a
        // copy can be left in that film's own folder. Somebody looking at a
        // film's results should not have to search a session log for the part
        // that belongs to it.
        static StringBuilder oFileLog = null;
        static bool bSecondInstance = false;
        static readonly DateTime dtSessionBegan = DateTime.Now;
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
            addParam("captions", "", "flag", "yes", "Use the film's own English captions as the transcript when it has them, instead of listening to it with Whisper");
            addParam("auto-captions", "", "flag", "no", "Also accept captions a machine made, such as YouTube's automatic ones. They carry no speaker names and none of the sounds that are not speech, so listening to the film usually gives a richer transcript");
            addParam("whisper-model", "", "string", "small", "Which Whisper model to hear the film with: tiny, base, small, medium or large-v3");
            addParam("speaker-window", "", "number", "120.0", "Seconds either side of a moment in which a caption speaker counts as being in the same scene. Scene-sized on purpose, which is much wider than the dialogue window");
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
            addParam("width", "", "integer", "512", "Width in pixels of each frame before tiling. Cost rises faster than the pixels: 768 is 2.25 times the pixels of 512 and measured 3.9 times the time, for no measurable gain");
            addParam("crop-bottom", "p", "number", "18", "Percentage cut off the bottom of each frame, to hide burnt-in subtitles");
            addParam("noise-floor", "n", "number", "-24", "Level in dB below which sound counts as a gap");
            addParam("min-gap", "g", "number", "5.0", "Shortest gap worth describing");
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
            addParam("player-client", "", "string", "", "Which YouTube player to ask for the video: web, web_safari, tv, ios or mweb. Empty tries the usual one and then the others when it is refused");
            addParam("name-length", "", "integer", "79", "Longest a picture's new name may be, before its extension. Cut at a word, never in the middle of one");
            addParam("picture-width", "", "integer", "1024", "Longest side, in pixels, a picture is reduced to before the model is shown it");
            addParam("update-tools", "", "flag", "yes", "Update yt-dlp and try once more when a video is refused every other way");
            addParam("update-channel", "", "string", "nightly", "Which yt-dlp release to update to: nightly, stable or master. Nightly is what yt-dlp recommends and carries this week's fixes");
            addParam("browser-session", "", "flag", "yes", "When a video is refused every other way, try again borrowing a browser's signed-in session, from Edge then Chrome then Firefox");
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
            if (sPath == "")
            {
                string sStem = Path.GetFileNameWithoutExtension(sDefaultLogName);
                sPath = Path.Combine(chooseLogFolder(), sStem + "-" + dtSessionBegan.ToString("yyyyMMdd-HHmmss") + Path.GetExtension(sDefaultLogName));
            }
            if (string.Compare(sLogPath, sPath, true) == 0) return;
            openLogAt(sPath);
            logMessage("Log file: " + sLogPath, "INFO", "Log: " + sLogPath);
            return;
        }

        // Another HomerScribe is running. Not an error -- running two on halves
        // of a list is a reasonable thing to do, since transcribing uses the
        // processor and describing the graphics card, so the two overlap.
        static bool anotherIsRunning()
        {
            try
            {
                Process[] aoOthers = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
                return aoOthers.Length > 1;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // A copy of this film's own log, beside its results. The session log still
        // holds everything; this holds only what belongs here.
        static void writeFileLog(string sOutputDir)
        {
            if (oFileLog == null) return;
            try
            {
                Directory.CreateDirectory(sOutputDir);
                string sWhere = Path.Combine(sOutputDir, sDefaultLogName);
                StreamWriter fOne = new StreamWriter(sWhere, false, new UTF8Encoding(true));
                fOne.Write(oFileLog.ToString());
                fOne.Close();
                logMessage("A log of this film alone is in " + sWhere, "INFO", "");
            }
            catch (Exception oError)
            {
                logMessage("The per-film log could not be written: " + oError.Message, "ERROR");
            }
            oFileLog = null;
        }

        static void openLogFile(string sPath)
        {
            // A second instance writes its own log rather than overwriting the
            // first one's. The number is the process, so the two are told apart.
            if (bSecondInstance)
            {
                try
                {
                    string sFolder = Path.GetDirectoryName(sPath);
                    string sStem = Path.GetFileNameWithoutExtension(sPath);
                    sPath = Path.Combine(sFolder, sStem + "-" + Process.GetCurrentProcess().Id.ToString() + Path.GetExtension(sPath));
                }
                catch (Exception)
                {
                }
            }
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(sPath)));
                fLogStream = new FileStream(sPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
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
                    fLogStream = new FileStream(sPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
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
            // An un-timestamped log from an older version is stale and shares the
            // obvious name, so it gets reached for. Moved aside, so the only
            // HomerScribe.log left in a results folder is the per-film one,
            // which is current by construction.
            try
            {
                string sStale = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(sPath)), sDefaultLogName);
                if (File.Exists(sStale) && string.Compare(sStale, sPath, true) != 0)
                {
                    string sAside = Path.Combine(Path.GetDirectoryName(sStale),
                                                 Path.GetFileNameWithoutExtension(sStale) + "-superseded" + Path.GetExtension(sStale));
                    if (File.Exists(sAside)) File.Delete(sAside);
                    File.Move(sStale, sAside);
                }
            }
            catch (Exception)
            {
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
                if (oFileLog != null) oFileLog.AppendLine(sLine);
            }
            if (sLevel == "CMD" && !bVerbose) return;
            // The console is hidden, so there is nobody to write to.
            if (bConsoleHidden) return;
            // With a dialog on screen the console carries the same messages the
            // dialog gave, written by announce, and nothing else. The log stream
            // -- command lines, exit codes, paths -- belongs in the log, which is
            // where somebody looking for it will go. Errors are kept, since an
            // error that never reached an announcement would otherwise vanish.
            if (bGuiMode && !bVerbose && sLevel != "ERROR" && sLevel != "FATAL" && sLevel != "HINT") return;
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
        // What this build actually does, listed at startup. A version number says
        // when a build was made, not what was decided by then, and five rounds of
        // analysis were spent on logs from builds that did not contain the change
        // under discussion. Each line is a behaviour, named so that its presence
        // or absence in a log is unambiguous.
        static readonly string[] asBehaviours = new string[] {
            "speech-placement", "room-required", "room-floor-is-min-gap",
            "moment-at-start-of-quiet", "look-back-window", "drop-if-covers-speech",
            "film-memory", "presenter-named", "subtitle-filter", "whisper-loop-filter",
            "empty-transcript-check", "force-clears-memory", "status-words", "montage-ahead", "per-film-log", "stop-if-source-vanishes", "picture-required",
            "captions-preferred", "captions-english-only", "captions-keep-sound", "captions-never-place", "video-heading",
            "captions-one-track", "captions-track-from-page", "captions-fetched-apart",
            "captions-without-the-film", "captions-converted-to-vtt", "sleep-between-requests",
            "documents-as-prose", "times-only-in-headings", "descriptions-labelled", "publisher-blurb-tidied",
            "other-ways-to-fetch", "update-tools-on-refusal", "tool-age-reported",
            "build-updates-yt-dlp", "no-update-nagging", "browser-session-last-resort", "first-reason-reported",
            "pictures-from-archives", "clash-numbering-whole-group", "metadata-standing-reported",
            "notes-inside-archives", "caption-speaker-roster", "speaker-near-the-moment",
            "names-as-phrases", "names-repaired", "no-guessed-identities",
            "fewest-leading-zeros", "file-name-as-context", "pictures-normalised", "partial-answer-rescued",
            "metadata-written", "renamed-copies-archived", "placeholder-names-refused", "wider-illegal-letters",
            "metadata-read-back", "exiftool-version-logged", "minor-errors-ignored",
            "metadata-self-test", "accessibility-tags-taught", "newest-exiftool-chosen",
            "single-file-exiftool-only", "opening-names-the-film", "fuller-picture-names",
            "done-between-sources", "source-box-selected", "described-md-in-both-places",
            "fields-read-back-not-assumed", "film-announced-before-work", "person-captions-only"
        };

        static void logEnvironment()
        {
            StringBuilder oWhat = new StringBuilder();
            foreach (string sOne in asBehaviours)
            {
                if (oWhat.Length > 0) oWhat.Append(", ");
                oWhat.Append(sOne);
            }
            logMessage("This build does: " + oWhat.ToString(), "INFO", "");
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
        // The film's own captions, when it has usable ones. Kept apart from
        // lFilmSpeech on purpose: these are the WORDS, and lFilmSpeech is the
        // MAP OF WHERE THE SPEECH FALLS. They are not interchangeable, because
        // a cue is put on screen early and taken off late so that a reader can
        // finish it. Placement uses lFilmSpeech and nothing else.
        static List<Speech> lCaptions = new List<Speech>();
        // Where the transcript's words came from, in words fit for a heading.
        // Empty means Whisper heard them.
        static string sTranscriptFrom = "";
        // What the video says about itself, for the head of every document.
        static string sVideoTitle = "";
        static string sVideoBy = "";
        static string sVideoAbout = "";
        static string sVideoAddress = "";
        // The English caption tracks the page says it has: the ones a person
        // wrote, and the ones a machine made. Kept apart because a person's
        // track is always preferred, and YouTube's own classification settles
        // that better than looking at the contents afterwards.
        static List<string> lTracksWritten = new List<string>();
        static List<string> lTracksAuto = new List<string>();
        // How long the video runs, from the page rather than from a file,
        // because with captions alone there is no file to ask.
        static double nVideoSeconds = 0.0;
        static string sCaptionsOnlyFolder = "";
        static string sCaptionsOnlyStem = "";
        // Why the film could not be had, when the words were got anyway.
        static string sCouldNotDescribe = "";
        static string sSpeechWorkDir = "";
        static bool bConsoleHidden = false;
        static string sLastSkippedFolder = "";
        static int iSourceAt = 0;
        static int iSourceCount = 0;
        static string sLastOutputFolder = "";
        static List<string> lResults = new List<string>();
        static List<string> lFailures = new List<string>();
        static List<string> lListsRead = new List<string>();
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
        static int iSaidSoFar = 0;

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
            // The console gets it whether or not it was spoken: it is a written
            // record to be read back at leisure, not an interruption.
            if (!bConsoleHidden)
            {
                try
                {
                    string sOneLine = Regex.Replace(sTitle + ": " + sBody, @"\s+", " ").Trim();
                    if (sOneLine.Length > iDefaultConsoleLine) sOneLine = sOneLine.Substring(0, iDefaultConsoleLine - 1) + "\u2026";
                    Console.WriteLine(sOneLine);
                }
                catch (Exception)
                {
                }
            }
            // EMPTIED FIRST, and always. The live-region path used to return
            // before this, so the collected messages were never cleared: every
            // announcement repeated all of its predecessors and grew as it went.
            // Nothing below may return before the list is emptied.
            lPending.Clear();
            nPendingAt = -1.0;
            dtPendingSince = DateTime.Now;
            // Only a message that was actually SPOKEN counts as having been said.
            // A withheld one used to reset this clock, so the message after it
            // waited twenty seconds behind something nobody heard. That is the
            // silence after "Starting": the first message of a run is withheld
            // while the window settles to the front after OK, and it took the
            // next twenty seconds down with it.
            if (weAreInFront()) dtLastSpoken = DateTime.Now;
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
            // Nothing is held back at the start of a run. Messages are collected
            // into groups so that descriptions arriving every few seconds do not
            // interrupt constantly, and a message waits up to twenty seconds for
            // company. That is right in the middle of a film and wrong at the
            // beginning, where messages are rare, each one is informative, and
            // somebody is waiting to hear that the program is alive. After the
            // first few, grouping resumes exactly as before.
            if (iSaidSoFar < iDefaultEarlyMessages)
            {
                iSaidSoFar = iSaidSoFar + 1;
                flushAnnouncements();
                return;
            }
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
                // Pumped here, as 2htm does after every status update. Setting
                // the text is not enough: until the queue is pumped, Windows has
                // not repainted the window or told anybody the title changed, and
                // a screen reader reads what it was told last.
                Application.DoEvents();
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

        // One match, not one matches. A count of nought is a real answer and
        // is said plainly.
        static string counted(int iHowMany, string sSingular, string sPlural)
        {
            return iHowMany.ToString() + " " + (iHowMany == 1 ? sSingular : sPlural);
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

        // yt-dlp complains on the console when it is more than ninety days
        // old. That is a fair thing for it to do and the wrong place to say it
        // here, where the console is carrying progress a listener is following.
        // --no-update is what the flag is documented for.
        static string quietly()
        {
            return " --no-update";
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


        // How many, which one, and where it is going. Said as early as it can be
        // known, because it is the confirmation that the program understood what
        // it was given.
        static string processingSaid(string sSource, int iAt, int iCount)
        {
            string sSaid = iCount == 1
                ? "Processing 1 file"
                : "Processing " + iAt.ToString() + " of " + iCount.ToString() + " files";
            if (sSource.StartsWith("http")) return sSaid + ", " + sSource;
            string sBase = "";
            string sFolder = "";
            try
            {
                sBase = Path.GetFileName(sSource);
                sFolder = Path.GetFileNameWithoutExtension(sSource);
            }
            catch (Exception)
            {
                return sSaid + ", " + sSource;
            }
            sSaid = sSaid + ", " + sBase;
            // Only worth saying when it is not simply the name without its
            // extension, which is the ordinary case and would be repetition.
            string sPlain = Path.GetFileNameWithoutExtension(sBase);
            if (sFolder != "" && string.Compare(sFolder, sPlain, false) != 0) sSaid = sSaid + ", results in " + sFolder;
            return sSaid;
        }

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
        // A file may carry more than one sound track: a commentary, another
        // language, or a silent one left by an encoder. Knowing how many there
        // are turns "the transcript is empty" from a mystery into a lead.
        // Is there a picture at all? A recording has none, and there is then
        // nothing to describe: asked to describe twenty-nine mp3 files, the model
        // invented a sentence apiece from a blank montage and each was written
        // out as though it meant something.
        static bool hasPicture(string sFfmpeg, string sInput)
        {
            string sOut = "";
            string sErr = "";
            runCommand(sFfmpeg, "-hide_banner -i " + quoted(sInput), out sOut, out sErr);
            string sBoth = sOut + sErr;
            foreach (Match oOne in Regex.Matches(sBoth, @"Stream #\d+:\d+.*?: Video:"))
            {
                // Cover art is carried as a video stream of one still frame.
                // Not a picture to describe.
                int iFrom = oOne.Index;
                string sAfter = sBoth.Substring(iFrom, Math.Min(400, sBoth.Length - iFrom));
                if (sAfter.IndexOf("attached pic", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                return true;
            }
            return false;
        }

        static int audioTrackCount(string sFfmpeg, string sInput)
        {
            string sOut = "";
            string sErr = "";
            runCommand(sFfmpeg, "-hide_banner -i " + quoted(sInput), out sOut, out sErr);
            return Regex.Matches(sOut + sErr, @"Stream #\d+:\d+.*: Audio:").Count;
        }

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


        // ---------- captions ----------
        //
        // A film that carries captions carries something Whisper cannot make.
        // A person wrote them, so the words are right; they name who is
        // speaking; and they mark what can be heard but not spoken -- a door
        // slamming, music starting -- which no transcript of speech holds. So
        // when a transcript is wanted and the film has English captions, the
        // captions ARE the transcript.
        //
        // What they are not is a map of where the speech falls. A cue is put on
        // screen early and taken off late so that a reader can finish it, and
        // the passages below are merged further still. Feeding those timings to
        // the placement rule would shrink and shift the measured quiet, and the
        // cost would show only as a listener losing dialogue. Placement
        // therefore goes on using Whisper's stretches, measured from the sound
        // itself.

        static double secondsOfStamp(string sStamp)
        {
            double nSeconds = 0.0;
            double nPart = 0.0;
            int iHours = 0;
            int iMinutes = 0;
            string[] asBits = sStamp.Trim().Replace(",", ".").Split(':');
            try
            {
                if (asBits.Length == 3)
                {
                    int.TryParse(asBits[0], out iHours);
                    int.TryParse(asBits[1], out iMinutes);
                    double.TryParse(asBits[2], NumberStyles.Any, CultureInfo.InvariantCulture, out nPart);
                    nSeconds = iHours * 3600.0 + iMinutes * 60.0 + nPart;
                }
                else if (asBits.Length == 2)
                {
                    int.TryParse(asBits[0], out iMinutes);
                    double.TryParse(asBits[1], NumberStyles.Any, CultureInfo.InvariantCulture, out nPart);
                    nSeconds = iMinutes * 60.0 + nPart;
                }
            }
            catch (Exception)
            {
                nSeconds = 0.0;
            }
            return nSeconds;
        }

        // What belongs to the display rather than to the words.
        static string cleanCaptionLine(string sLine)
        {
            string sClean = Regex.Replace(sLine, @"<[^>]+>", "");
            sClean = Regex.Replace(sClean, @"\{[^}]*\}", "");
            sClean = sClean.Replace("&nbsp;", " ").Replace("&amp;", "&");
            sClean = sClean.Replace("&lt;", "<").Replace("&gt;", ">");
            sClean = sClean.Replace("&quot;", "\"").Replace("&#39;", "'");
            // The marks that open a new speaker's line.
            sClean = Regex.Replace(sClean, @"^\s*(>>+|-)\s+", "");
            return Regex.Replace(sClean, @"\s+", " ").Trim();
        }

        // Captions name the speaker in capitals before a colon, sometimes in
        // brackets. The name is taken off the front and kept separately, so the
        // reader is told who spoke without the name being buried in the words.
        static string speakerOf(string sSaid, out string sRest)
        {
            sRest = sSaid;
            Match oNamed = Regex.Match(sSaid, @"^\s*[\[\(]?([A-Z][A-Z0-9'\.\- ]{1,28})[\]\)]?\s*:\s*(.+)$");
            if (!oNamed.Success)
            {
                // A caption also names a speaker in brackets with no colon at
                // all -- TED writes "(Audience) Good." The test that keeps this
                // apart from a sound is what comes after: a speaker is followed
                // by a sentence, which starts with a capital, where "(Music)
                // plays softly" carries on in lower case.
                Match oBracket = Regex.Match(sSaid, @"^\s*[\[\(]([A-Z][A-Za-z]*(?: [A-Z][A-Za-z]*){0,2})[\]\)]\s+([A-Z\""].*)$");
                if (oBracket.Success)
                {
                    sRest = oBracket.Groups[2].Value.Trim();
                    return oBracket.Groups[1].Value.Trim();
                }
                return "";
            }
            string sWho = oNamed.Groups[1].Value.Trim();
            // Four words is a name and a note about it -- MAN ON RADIO. More
            // than that is a shouted sentence that happens to end in a colon.
            if (sWho == "" || sWho.Split(' ').Length > 4) return "";
            sRest = oNamed.Groups[2].Value.Trim();
            if (sRest == "") return "";
            return sWho;
        }

        // Every cue, as a stretch. SRT and WebVTT differ only in the separator
        // and some decoration, so one reader does both.
        //
        // Note what is NOT dropped here. readTranscript throws away a stretch
        // that is nothing but brackets, because from Whisper "[Music]" is an
        // artefact. From a caption file it is the opposite: somebody wrote it
        // down deliberately, and it is exactly what a deafblind reader has no
        // other way of learning.
        static List<Speech> readCaptions(string sText)
        {
            List<Speech> lCues = new List<Speech>();
            string sWhole = sText.Replace("\r\n", "\n").Replace("\r", "\n");
            Regex oTiming = new Regex(@"(\d{1,3}:\d{2}:\d{2}[\.,]\d{1,3}|\d{1,3}:\d{2}[\.,]\d{1,3})"
                                    + @"\s*-->\s*"
                                    + @"(\d{1,3}:\d{2}:\d{2}[\.,]\d{1,3}|\d{1,3}:\d{2}[\.,]\d{1,3})");
            string[] asLines = sWhole.Split('\n');
            int iAt = 0;
            while (iAt < asLines.Length)
            {
                Match oFound = oTiming.Match(asLines[iAt]);
                if (!oFound.Success)
                {
                    iAt = iAt + 1;
                    continue;
                }
                double nFrom = secondsOfStamp(oFound.Groups[1].Value);
                double nTo = secondsOfStamp(oFound.Groups[2].Value);
                iAt = iAt + 1;
                List<string> lSaid = new List<string>();
                while (iAt < asLines.Length && asLines[iAt].Trim() != "" && !oTiming.IsMatch(asLines[iAt]))
                {
                    // A line holding nothing but a number is SRT's cue index,
                    // not something anybody said.
                    if (!Regex.IsMatch(asLines[iAt].Trim(), @"^\d+$"))
                    {
                        string sClean = cleanCaptionLine(asLines[iAt]);
                        if (sClean != "") lSaid.Add(sClean);
                    }
                    iAt = iAt + 1;
                }
                string sSaid = string.Join(" ", lSaid.ToArray()).Trim();
                if (sSaid == "") continue;
                if (nTo <= nFrom) nTo = nFrom + 1.0;
                string sRest = "";
                Speech oCue = new Speech();
                oCue.nStart = nFrom;
                oCue.nEnd = nTo;
                oCue.sWho = speakerOf(sSaid, out sRest);
                oCue.sText = oCue.sWho == "" ? sSaid : sRest;
                lCues.Add(oCue);
            }
            return lCues;
        }

        // Where the end of what we already have is also the start of what has
        // just arrived, that shared part is a repeat and only the rest is new.
        // A few letters agreeing by chance is not a repeat, so a partial match
        // must be a run of words and must end where a word ends.
        static int overlapLength(string sHave, string sNew)
        {
            int iMost = Math.Min(sHave.Length, sNew.Length);
            int iTry = iMost;
            while (iTry > 0)
            {
                if (string.CompareOrdinal(sHave, sHave.Length - iTry, sNew, 0, iTry) == 0)
                {
                    if (iTry == sNew.Length || iTry == sHave.Length) return iTry;
                    if (iTry >= 12 && sNew[iTry] == ' ') return iTry;
                }
                iTry = iTry - 1;
            }
            return 0;
        }

        // A rolling track shows each line two or three times as it scrolls up
        // the screen, and each cue holds the one before it with more added. So
        // the useful question is not how two cues compare but which words have
        // not been written down yet. The words already used are remembered, and
        // only what is new is added.
        //
        // This was got wrong first time. Comparing each cue with the one before
        // asked whether the OLD held the NEW, when a rolling caption grows the
        // other way, so nothing matched and every line was written twice.
        static List<Speech> joinRolling(List<Speech> lCues)
        {
            List<Speech> lOut = new List<Speech>();
            string sSeen = "";
            foreach (Speech oCue in lCues)
            {
                string sNew = oCue.sText.Substring(overlapLength(sSeen, oCue.sText)).Trim();
                if (sNew == "") continue;
                sSeen = (sSeen + " " + sNew).Trim();
                if (sSeen.Length > 400) sSeen = sSeen.Substring(sSeen.Length - 400);
                bool bStartAgain = lOut.Count == 0;
                if (!bStartAgain)
                {
                    Speech oEnding = lOut[lOut.Count - 1];
                    bStartAgain = oCue.sWho != oEnding.sWho
                               || (oEnding.sText.Length >= iDefaultParagraph && endsSentence(oEnding.sText))
                               || oEnding.sText.Length >= iDefaultParagraphMost
                               || oCue.nStart - oEnding.nEnd > nDefaultJoinWithin
                               || soundOnly(sNew) || soundOnly(oEnding.sText);
                }
                if (bStartAgain)
                {
                    Speech oFresh = new Speech();
                    oFresh.nStart = oCue.nStart;
                    oFresh.nEnd = oCue.nEnd;
                    oFresh.sWho = oCue.sWho;
                    oFresh.sText = sNew;
                    lOut.Add(oFresh);
                }
                else
                {
                    Speech oLast = lOut[lOut.Count - 1];
                    oLast.sText = (oLast.sText + " " + sNew).Trim();
                    oLast.nEnd = oCue.nEnd;
                }
            }
            return lOut;
        }

        // A track written by a person does not roll. Cues that follow each
        // other closely and belong to the same speaker are gathered into a
        // passage, so the document reads as prose rather than as a list of
        // fragments. A change of speaker always starts a new passage: running
        // two people's words together would put one person's words in the
        // other's mouth.
        static List<Speech> joinCaptions(List<Speech> lCues)
        {
            List<Speech> lOut = new List<Speech>();
            foreach (Speech oCue in lCues)
            {
                if (lOut.Count > 0)
                {
                    Speech oLast = lOut[lOut.Count - 1];
                    if (oCue.sText == oLast.sText || oLast.sText.IndexOf(oCue.sText, StringComparison.Ordinal) >= 0)
                    {
                        oLast.nEnd = Math.Max(oLast.nEnd, oCue.nEnd);
                        continue;
                    }
                    if (oCue.sWho == oLast.sWho && oCue.nStart - oLast.nEnd <= nDefaultJoinWithin
                        && (oLast.sText.Length < iDefaultParagraph
                            || (!endsSentence(oLast.sText) && oLast.sText.Length < iDefaultParagraphMost))
                        && !soundOnly(oCue.sText) && !soundOnly(oLast.sText))
                    {
                        oLast.sText = (oLast.sText + " " + oCue.sText).Trim();
                        oLast.nEnd = oCue.nEnd;
                        continue;
                    }
                }
                lOut.Add(oCue);
            }
            return lOut;
        }

        // A cue holding nothing but a sound: "[door slams]", "(Applause)".
        // It is the one thing a caption file has that a transcript of speech
        // never does, so it must never be folded into the speech around it.
        // A full stop that belongs to an abbreviation is not the end of
        // anything. TED's description reads "for commercial purposes (e.g."
        // when split naively, and that fragment reached the top of the TED
        // transcript looking like a fault in the program.
        static bool endsAbbreviation(string sText)
        {
            string sTrim = sText.TrimEnd();
            if (!sTrim.EndsWith(".")) return false;
            foreach (string sShort in new string[] { "e.g", "i.e", "etc", "vs", "cf", "al",
                                                     "Mr", "Mrs", "Ms", "Dr", "Prof", "Rev",
                                                     "St", "Ave", "No", "Inc", "Ltd", "Co",
                                                     "Jr", "Sr", "Fig", "approx", "Dept" })
            {
                if (sTrim.EndsWith(" " + sShort + ".", StringComparison.OrdinalIgnoreCase)) return true;
                if (sTrim.EndsWith("(" + sShort + ".", StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Compare(sTrim, sShort + ".", true) == 0) return true;
            }
            // A lone initial: "Ken A." is still mid-name.
            return Regex.IsMatch(sTrim, @"(^|[\s\(])[A-Z]\.$");
        }

        // A passage may end here without sounding cut off.
        static bool endsSentence(string sText)
        {
            string sTrim = sText.TrimEnd();
            if (sTrim == "") return true;
            // A closing quotation mark or bracket after the stop still counts.
            while (sTrim.Length > 1 && "\"\u201d\u2019')]".IndexOf(sTrim[sTrim.Length - 1]) >= 0)
                sTrim = sTrim.Substring(0, sTrim.Length - 1).TrimEnd();
            if (sTrim == "") return true;
            return ".!?".IndexOf(sTrim[sTrim.Length - 1]) >= 0;
        }

        static bool soundOnly(string sText)
        {
            return Regex.IsMatch(sText, @"^[\[\(][^\]\)]*[\]\)]$");
        }

        static bool englishTag(string sTag)
        {
            string sLower = sTag.Trim().ToLower().Replace("_", "-");
            if (sLower == "") return false;
            // A track sometimes carries a note after its language: "en-US-cc"
            // for closed captions, "eng-forced" for forced subtitles. The note
            // says how the track is meant to be used, not what language it is.
            // "-hi" for hearing impaired is deliberately NOT stripped: "en-hi"
            // is YouTube's Hindi translation, and losing a rare marker beats
            // reading a transcript in the wrong language.
            foreach (string sNote in new string[] { "-cc", "-sdh", "-forced", "-default", "-dubbed" })
            {
                while (sLower.EndsWith(sNote)) sLower = sLower.Substring(0, sLower.Length - sNote.Length);
            }
            foreach (string sOne in sDefaultEnglishTags.Split(','))
            {
                if (sLower == sOne) return true;
            }
            return false;
        }

        // YouTube's automatic captions carry a timing tag around each word, so
        // that the words can be lit up as they are said. A track written by a
        // person never does. This is a surer test than the file's name.
        static bool looksAutomatic(string sText)
        {
            return Regex.IsMatch(sText, @"<\d{2}:\d{2}:\d{2}\.\d{3}>");
        }

        // A subtitle track inside the film. Every track is asked its language,
        // so an English one is taken even when it is not the first, and a film
        // carrying only other languages is left to Whisper.
        static string captionsInFilm(string sFfmpeg, string sInput, string sWorkDir, out string sWhy)
        {
            sWhy = "";
            string sOut = "";
            string sErr = "";
            // ffmpeg lists every stream when asked to open a file and given
            // nothing to do with it, the way probeDuration and hasPicture
            // already use. A subtitle line reads
            //   Stream #0:2(eng): Subtitle: subrip (default)
            // and the language in brackets is missing when nobody set it.
            // Their order in that listing is their order in the file, so the
            // third subtitle stream found is 0:s:2.
            runCommand(sFfmpeg, "-hide_banner -i " + quoted(sInput), out sOut, out sErr);
            List<string> lTags = new List<string>();
            foreach (Match oOne in Regex.Matches(sOut + sErr, @"Stream #\d+:\d+(?:\(([^)]*)\))?[^\r\n]*?: Subtitle:"))
            {
                lTags.Add(oOne.Groups[1].Success ? oOne.Groups[1].Value.Trim() : "");
            }
            if (lTags.Count == 0)
            {
                sWhy = "The film holds no subtitle track.";
                return "";
            }
            int iWanted = -1;
            int iTrack = 0;
            foreach (string sTag in lTags)
            {
                if (iWanted < 0 && englishTag(sTag)) iWanted = iTrack;
                iTrack = iTrack + 1;
            }
            if (iWanted < 0 && lTags.Count == 1 && lTags[0] == "")
            {
                // One track, and nobody said what language it is in. Taking it
                // is the useful guess, and the heading says the language was
                // never stated.
                iWanted = 0;
                sWhy = "unlabelled";
            }
            if (iWanted < 0)
            {
                sWhy = "The film's subtitle " + counted(lTags.Count, "track is", "tracks are") + " in "
                     + string.Join(", ", lTags.ToArray()) + ", and only English is supported.";
                return "";
            }
            string sTemp = Path.Combine(sWorkDir, "captions.srt");
            string sExOut = "";
            string sExErr = "";
            int iEx = runCommand(sFfmpeg, "-v error -y -i " + quoted(sInput)
                                        + " -map 0:s:" + iWanted.ToString() + " -c:s srt " + quoted(sTemp),
                                 out sExOut, out sExErr);
            if (iEx != 0 || !File.Exists(sTemp))
            {
                // A track of pictures rather than text -- the kind a DVD or a
                // Blu-ray carries -- cannot be turned into words without
                // reading the pictures, which is a different job.
                sWhy = "The subtitle track could not be read out as text. It may be a track of pictures rather than words. ffmpeg said: " + tail(sExErr, 200);
                return "";
            }
            string sText = "";
            try
            {
                sText = File.ReadAllText(sTemp);
            }
            catch (Exception oError)
            {
                sWhy = "The extracted subtitle file could not be read: " + oError.Message;
                return "";
            }
            try
            {
                File.Delete(sTemp);
            }
            catch (Exception)
            {
            }
            return sText;
        }

        // yt-dlp leaves its subtitles beside the video, named for the language:
        // "<name>.en.vtt". Where more than one is there, the one a person wrote
        // is preferred, judged by what is in it rather than what it is called.
        static string captionsBeside(string sInput, out string sWhy)
        {
            sWhy = "";
            string sBest = "";
            string sBestText = "";
            string sStem = Path.GetFileNameWithoutExtension(sInput);
            string sFolder = Path.GetDirectoryName(Path.GetFullPath(sInput));
            List<string> lFound = new List<string>();
            try
            {
                foreach (string sName in Directory.GetFiles(sFolder))
                {
                    string sBare = Path.GetFileName(sName);
                    string sLower = sBare.ToLower();
                    if (!sLower.EndsWith(".vtt") && !sLower.EndsWith(".srt")) continue;
                    if (!sBare.StartsWith(sStem, StringComparison.OrdinalIgnoreCase)) continue;
                    // Whatever sits between the stem and the extension is the
                    // language. Nothing there at all is taken as English.
                    string sMiddle = Path.GetFileNameWithoutExtension(sBare);
                    sMiddle = sMiddle.Length > sStem.Length ? sMiddle.Substring(sStem.Length).Trim('.') : "";
                    if (sMiddle != "" && !englishTag(sMiddle)) continue;
                    lFound.Add(sName);
                }
            }
            catch (Exception oError)
            {
                sWhy = "The folder beside the film could not be read: " + oError.Message;
                return "";
            }
            if (lFound.Count == 0)
            {
                sWhy = "There is no English caption file beside the film.";
                return "";
            }
            foreach (string sName in lFound)
            {
                string sText = "";
                try
                {
                    sText = File.ReadAllText(sName);
                }
                catch (Exception)
                {
                    continue;
                }
                bool bAuto = looksAutomatic(sText);
                logMessage("  Caption file beside the film: " + Path.GetFileName(sName)
                           + ", " + (bAuto ? "made automatically" : "written by a person"), "INFO", "");
                if (sBest == "" || (!bAuto && looksAutomatic(sBestText)))
                {
                    sBest = sName;
                    sBestText = sText;
                }
            }
            if (sBest == "")
            {
                sWhy = "No caption file beside the film could be read.";
                return "";
            }
            logMessage("  Taking " + Path.GetFileName(sBest) + ".", "INFO", "");
            return sBestText;
        }

        // The transcript, from the film's own captions when it has usable ones.
        // An empty list means Whisper should do the work instead, and the
        // reason is written to the log either way.
        static List<Speech> captionsFor(string sFfmpeg, string sInput, string sWorkDir, out string sWhere)
        {
            List<Speech> lFrom = new List<Speech>();
            bool bUnlabelled = false;
            string sWhyFilm = "";
            string sWhyBeside = "";
            sWhere = "";
            if (!flag("captions"))
            {
                logMessage("Captions are turned off, so the film is listened to instead.", "INFO", "");
                return lFrom;
            }
            string sText = captionsInFilm(sFfmpeg, sInput, sWorkDir, out sWhyFilm);
            if (sText != "")
            {
                bUnlabelled = sWhyFilm == "unlabelled";
                sWhere = "the film's own captions";
            }
            else
            {
                logMessage("No captions inside the film. " + sWhyFilm, "INFO", "");
                sText = captionsBeside(sInput, out sWhyBeside);
                if (sText != "") sWhere = "a caption file beside the film";
                else logMessage("None beside it either. " + sWhyBeside, "INFO", "");
            }
            if (sText.Trim() == "")
            {
                logMessage("No captions were found, so the film will be listened to with Whisper.", "INFO", "");
                return lFrom;
            }
            // How the track was made decides how it is read. Only a track that
            // rolls needs the repeats taking out, and doing that to a
            // hand-written track would trim words that were meant to be there.
            bool bRolling = looksAutomatic(sText);
            // And now it decides something larger: whether these captions are
            // trusted over listening to the film at all.
            bCaptionsAuto = bRolling;
            logMessage("These captions were " + (bRolling ? "made automatically, and roll up the screen, so the repeats are taken out."
                                                          : "written by a person, so they are read as they stand."), "INFO", "");
            lFrom = bRolling ? joinRolling(readCaptions(sText)) : joinCaptions(readCaptions(sText));
            if (lFrom.Count == 0)
            {
                logMessage("Captions were found, but nothing could be read out of them. The film will be listened to with Whisper instead.", "INFO", "");
                sWhere = "";
                return lFrom;
            }
            int iWords = 0;
            int iNamed = 0;
            int iSounds = 0;
            foreach (Speech oCue in lFrom)
            {
                iWords = iWords + oCue.sText.Split(' ').Length;
                if (oCue.sWho != "") iNamed = iNamed + 1;
                if (Regex.IsMatch(oCue.sText, @"^[\[\(][^\]\)]*[\]\)]$")) iSounds = iSounds + 1;
            }
            if (bUnlabelled)
            {
                logMessage("The film's one subtitle track does not say what language it is in. It is taken as English.", "INFO", "");
                sWhere = sWhere + ", whose language is not stated";
            }
            logMessage("Captions from " + sWhere + ": " + counted(lFrom.Count, "passage", "passages")
                       + ", " + counted(iWords, "word", "words")
                       + ", " + counted(iNamed, "naming a speaker", "naming a speaker")
                       + ", " + counted(iSounds, "marking a sound rather than speech", "marking a sound rather than speech")
                       + ", up to " + formatClock(lFrom[lFrom.Count - 1].nEnd) + ".",
                       "INFO", "Read " + counted(lFrom.Count, "passage", "passages") + " of the film's own captions.");
            return lFrom;
        }

        // Transcribing a long film is minutes of work, so the result is kept and
        // a resumed run never pays for it twice.
        static List<Speech> transcribe(string sFfmpeg, string sInput, string sWorkDir, double nDuration)
        {
            List<Speech> lSpeech = new List<Speech>();
            string sJson = Path.Combine(sWorkDir, "transcript.json");
            if (File.Exists(sJson) && !flag("force"))
            {
                lSpeech = readTranscript(sJson);
                if (lSpeech.Count > 0)
                {
                    logMessage("Reusing the transcript from an earlier run: " + lSpeech.Count.ToString() + " spoken stretches.", "INFO", "");
                    announce("Resuming", -1.0, 1.0, "The transcript was made in an earlier run, so there is nothing to listen to again.");
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
            logMessage("Audio tracks in this file: " + audioTrackCount(sFfmpeg, sInput).ToString(), "INFO", "");
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
            // Almost nothing heard in a film long enough to expect speech. One
            // episode spent 42 percent of its own length being listened to and
            // returned six seconds of speech from fifty-seven minutes. Nothing
            // noticed: every description then went on the timer, and the report
            // said 1.1 percent of them landed on speech, which looked like the
            // best result in the series and meant nothing, since there was no
            // speech recorded for them to land on.
            double nShareHeard = nSpoken / Math.Max(nDuration, 1.0) * 100.0;
            if (nDuration > 300.0 && nShareHeard < nDefaultTooQuiet)
            {
                string sWrong = "Only " + formatClock(nSpoken) + " of speech was heard in " + formatClock(nDuration)
                              + ". Either this film has almost no speech in it -- a silent film, or one carrying only music -- "
                              + "or the sound track is in a form Whisper could not read, or the file holds more than one audio "
                              + "track and the wrong one was taken. Descriptions will be placed by listening for silence, which "
                              + "is the right thing to do in any of those cases.";
                logMessage(sWrong, "INFO");
                announce("Initializing", -1.0, 1.0, sWrong);
                bSpeechDoubtful = true;
            }
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
            if (lGaps.Count > 0)
            {
                List<double> lLengths = new List<double>();
                foreach (Moment oOne in lGaps) lLengths.Add(oOne.nLength);
                lLengths.Sort();
                logMessage("Those gaps run from " + num(lLengths[0]) + "s to " + num(lLengths[lLengths.Count - 1])
                           + "s, the middle one " + num(lLengths[lLengths.Count / 2]) + "s. A description needs about "
                           + num(12.0 / number("words-per-second")) + "s to say twelve words.", "INFO", "");
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
            int iNoRoom = 0;
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
                    // Not enough quiet here for anything to be heard. Placing a
                    // description anyway costs the listener the sentence being
                    // spoken as well as the description, so it is not placed.
                    if (nRoom < number("min-gap"))
                    {
                        iNoRoom = iNoRoom + 1;
                        nPrevious = nWhere;
                        continue;
                    }
                    Moment oPlaced = new Moment();
                    // At the START of the quiet, with the same margin a natural
                    // gap keeps, not in the middle of it. Placing it centrally
                    // gave a description half the room that had been measured,
                    // so the second half of it ran into the speech.
                    oPlaced.nStart = Math.Round(nWhere - nRoom / 2.0 + nDefaultLead, 3);
                    if (oPlaced.nStart < 0.0) oPlaced.nStart = 0.0;
                    oPlaced.nLength = Math.Max(nForcedLength, nRoom - nDefaultLead * 2.0);
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
            if (iNoRoom > 0) logMessage("Passed over " + iNoRoom.ToString() + " places with less than " + num(number("min-gap"))
                                        + "s of quiet. A description there would cost the listener the speech it covered as well as itself.",
                                        "INFO", "");
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

        // Who the captions say was speaking around this moment.
        //
        // He asked the obvious question: captions carry times, so why does a
        // name at 12:03 not tell you who is on screen at 12:03? It partly
        // does. In an interview, a talk, a piece to camera or most television
        // drama the speaker IS in shot a good deal of the time.
        //
        // Three things weaken it, and the third is peculiar to this program.
        // Narration is never in shot. Dialogue cuts to the listener's face as
        // often as the speaker's, especially on the line that matters. And
        // HomerScribe describes IN THE GAPS BETWEEN SPEECH, by design -- so at
        // the moment a description is made, nobody is speaking at all, and the
        // nearest speaker is on one side of the silence or the other.
        //
        // What survives all three is worth having: whoever spoke just before
        // or just after is very likely in the SCENE, if not in the frame. So
        // it is given as that and no more, and the model is left to check it
        // against the picture.
        static string spokeAround(List<Speech> lFrom, double nAt)
        {
            if (lFrom == null || lFrom.Count == 0) return "";
            // NOT the dialogue window. That is twenty-five seconds, sized for
            // quoting the line just spoken so a description does not repeat it,
            // which is a different question from this one.
            //
            // "Who is in this scene" is a scene-sized question. At twenty-five
            // seconds this found nobody at all across two NOVA documentaries --
            // descriptions go in the rare silences of a narrated film, and the
            // nearest labelled cue was always further off than that. Two
            // minutes either way is about the length of a scene.
            double nWindow = number("speaker-window");
            if (nWindow <= 0.0) nWindow = 120.0;
            List<string> lWho = new List<string>();
            foreach (Speech oCue in lFrom)
            {
                if (oCue.sWho == null || oCue.sWho.Trim() == "") continue;
                if (oCue.nEnd < nAt - nWindow) continue;
                if (oCue.nStart > nAt + nWindow) continue;
                // The same filter the roster uses, which this was missing.
                // His logs read "GPS and FORTIER and NARRATOR speaking around
                // 22:02": a narrator is never in shot, and GPS is not a person
                // at all. Nearly every hint carried NARRATOR, which turned the
                // useful single-name case into a three-name guess.
                if (namesFromCaptions(new List<Speech>() { oCue }).Count == 0) continue;
                bool bHaveIt = false;
                foreach (string sOne in lWho)
                {
                    if (string.Compare(sOne, oCue.sWho.Trim(), true) == 0) bHaveIt = true;
                }
                if (!bHaveIt) lWho.Add(oCue.sWho.Trim());
            }
            if (lWho.Count == 0) return "";
            // One name is evidence. A dozen is a cast list, and saying "these
            // twelve are around here somewhere" invites exactly the guessing
            // the prompt forbids.
            if (lWho.Count > 3) return "";
            return string.Join(" and ", lWho.ToArray());
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

        // How talkative is this film, and what follows from it. Measured once,
        // before anything depends on it.
        static void settleSpacing(double nDuration)
        {
            double nTalk = 0.0;
            foreach (Speech oSpeech in lFilmSpeech) nTalk = nTalk + (oSpeech.nEnd - oSpeech.nStart);
            double nShare = nTalk / Math.Max(nDuration, 1.0) * 100.0;
            if (nShare < nDefaultTalkative) return;
            if (!dParams["every"].bGiven && number("every") != nDefaultTalkativeEvery)
            {
                dParams["every"].sValue = num(nDefaultTalkativeEvery);
                logMessage("Somebody is talking for " + ((int)nShare).ToString() + " percent of this film, so descriptions are spaced "
                           + num(nDefaultTalkativeEvery) + " seconds apart rather than " + num(nDefaultEvery)
                           + ". Closer together, most moments fall on speech and the model declines to describe them, which costs as much "
                           + "as a description does. Pass --every to choose for yourself.", "INFO",
                           "Spacing descriptions " + ((int)nDefaultTalkativeEvery).ToString() + " seconds apart, since this film is mostly talking.");
                // The ceiling on silence must stay above the spacing, or every
                // declined moment is immediately asked again and the model never
                // gets to decline at all.
                if (!dParams["max-silence"].bGiven && number("max-silence") < nDefaultTalkativeEvery * 2.0)
                {
                    dParams["max-silence"].sValue = num(nDefaultTalkativeEvery * 2.0);
                    logMessage("The longest silence allowed is raised to " + num(nDefaultTalkativeEvery * 2.0)
                               + " seconds to match, so the model may still decline a moment that holds nothing.", "INFO", "");
                }
            }
            if (nShare >= nDefaultCrowded && !dParams["every"].bGiven)
            {
                dParams["every"].sValue = "0";
                if (!dParams["max-silence"].bGiven) dParams["max-silence"].sValue = "0";
                string sNoRoom = "Somebody is talking for " + ((int)nShare).ToString() + " percent of this film. There is no room to "
                               + "describe it, so descriptions are placed ONLY where there is a genuine pause, and there will be few of them. "
                               + "A description spoken over the narration is worse than none: the listener loses the sentence being spoken and "
                               + "cannot attend to the description either. Pass --every to fill the film regardless.";
                logMessage(sNoRoom, "INFO");
                logMessage("", "INFO", sNoRoom);
                return;
            }
            string sCrowded = "Somebody is talking for " + ((int)nShare).ToString() + " percent of this film, so there is very little room "
                            + "for description. Descriptions will fall across the narration whatever is done; they are placed at the quietest "
                            + "points that exist. Interrupting less often helps more than anything else: with --every 45 a description falls on "
                            + "speech about half as often as at the default of 14, and with --every 90 less than a third as often. "
                            + "--detail brief shortens each one, which helps again.";
            logMessage(sCrowded, "HINT");
            logMessage("", "INFO", sCrowded);
        }

        static List<Moment> findGaps(string sFfmpeg, string sPath, double nDuration)
        {
            // Measured BEFORE the cache is consulted. The transcript is cheap
            // when it has already been made, and the spacing depends on it.
            if (flag("speech") && !bSpeechReady)
            {
                lFilmSpeech = transcribe(sFfmpeg, sPath, sSpeechWorkDir, nDuration);
                bSpeechReady = true;
            }
            if (flag("speech") && lFilmSpeech.Count > 0 && !bSpeechDoubtful) settleSpacing(nDuration);

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
                // Already made above, before the cache was consulted.
                if (lFilmSpeech.Count > 0 && !bSpeechDoubtful)
                {
                    lGaps = gapsFromSpeech(lFilmSpeech, nDuration);
                    iFromSpeech = lGaps.Count;
                    double nTalk = 0.0;
                    foreach (Speech oSpeech in lFilmSpeech) nTalk = nTalk + (oSpeech.nEnd - oSpeech.nStart);
                    double nShare = nTalk / Math.Max(nDuration, 1.0) * 100.0;
                    // On a film that is mostly talking, most moments are placed
                    // on the timer, and the model is asked about a moment where
                    // it will decline. This one: 216 of 412 moments produced
                    // nothing, half the calls spent to be told there was nothing
                    // to say, and 86 percent of what survived landed on the
                    // narration anyway. Both faults have the same cure, and the
                    // program already measured the condition and advised the cure
                    // in words. It now applies it, when the interval was left at
                    // its default.
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
        // The montage for the moment after this one, made while the model is
        // working on this one. It depends only on the film and a timestamp --
        // nothing from the previous description -- so making it early alters
        // nothing the model is shown. The same frames, the same picture, the
        // same prompt. Only the waiting is removed.
        static Thread threadMontage = null;
        static string sReadyMontage = "";
        static bool bReadyMontageGood = false;

        static void montageAheadOfTime(string sFfmpeg, string sPath, double nMiddle, double nSpan, string sImagePath)
        {
            waitForMontage();
            sReadyMontage = sImagePath;
            bReadyMontageGood = false;
            threadMontage = new Thread(delegate()
            {
                try
                {
                    bReadyMontageGood = buildMontage(sFfmpeg, sPath, nMiddle, nSpan, sImagePath);
                }
                catch (Exception)
                {
                    bReadyMontageGood = false;
                }
            });
            threadMontage.IsBackground = true;
            threadMontage.Start();
        }

        static bool waitForMontage()
        {
            if (threadMontage == null) return false;
            try
            {
                threadMontage.Join();
            }
            catch (Exception)
            {
            }
            threadMontage = null;
            return bReadyMontageGood;
        }

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
            oRules.Append("are already spoken aloud in the film. NEVER read a subtitle or a caption, in any language, "
                        + "and never mention that subtitles are present.\n");
            oRules.Append("- Words that carry meaning and are NOT subtitles -- a sign, a letter, a title card, a name on a door -- ");
            oRules.Append("are worth reading, introduced as: Words appear: followed by the words.\n\n");

            oRules.Append("Write only the description, as it will be spoken aloud. No preamble, no commentary, ");
            oRules.Append("no mention of frames, shots, scenes, panels, the camera, the sequence, or the film itself.");
            return oRules.ToString();
        }

        static string promptFor(int iMaxWords, List<string> lRecent, string sContext, bool bNewScene, bool bOverSound, List<string> lNames, string sJustSaid, double nAt)
        {
            StringBuilder oPrompt = new StringBuilder();
            if (sEstablished != "")
            {
                oPrompt.Append("What this film has established so far: " + sEstablished + "\n");
                oPrompt.Append("Take that as known. Do not work it out again and do not repeat it; describe what is new in this moment.\n\n");
            }
            if (sContext.Trim() != "")
            {
                oPrompt.Append("About this film: " + sContext.Trim() + "\n");
                string sHere = contextForMoment(nAt);
                if (sHere != "")
                {
                    oPrompt.Append("Where the film has reached: " + sHere + "\n");
                    oPrompt.Append("That is where the film is, not what is in this picture. Use it for the right words and names; describe only what you can see.\n");
                    if (sHere != sContextLast)
                    {
                        sContextLast = sHere;
                        logMessage("From here the context section is: " + sHere, "INFO", "");
                    }
                }
                string sFront = presenterIn(sContext);
                if (sFront != "") oPrompt.Append("The person addressing the viewer in this film is " + sFront
                    + ". When someone looks towards the viewer and speaks, name " + sFront + " rather than writing \"a man\".\n");
                oPrompt.Append("\n");
            }
            oPrompt.Append("The picture holds " + integer("frames").ToString() + " frames from one brief moment of the film, in time order, ");
            oPrompt.Append("tiled left to right then top to bottom. They are one continuous moment, not separate pictures.\n\n");
            if (lNames.Count > 0)
            {
                // A list of names with nothing to attach them to produces a name
                // a third of the time and "a bearded man" the rest, spread evenly
                // through a film -- worse for a listener than either extreme,
                // since there is no telling whether the bearded man at 26 minutes
                // is the one named at 27.
                oPrompt.Append("Names you have already used in this film: ");
                foreach (string sName in lNames) oPrompt.Append(sName + ", ");
                oPrompt.Append("\n");
                oPrompt.Append("Be consistent. If somebody here matches how one of those was described, use that name again rather than ");
                oPrompt.Append("saying what they look like: a listener cannot tell that \"a bearded man\" and a name are the same person. ");
                oPrompt.Append("If nobody matches, describe by appearance and do not guess a name.\n");
            }
            // The film's own cast list, said to be that.
            //
            // This used to be poured into the list above, which is headed
            // "Names you have already used in this film" and asks the model to
            // match somebody against how one of those was DESCRIBED. For a name
            // out of the captions there is no earlier description to match, so
            // the instruction was unanswerable and the names went unused. That
            // was my plumbing, not a limit of the idea.
            if (lSpeakerRoster.Count > 0)
            {
                oPrompt.Append("\nThe film's captions name these people as speaking somewhere in it: ");
                oPrompt.Append(string.Join(", ", lSpeakerRoster.ToArray()));
                oPrompt.Append("\n");
                oPrompt.Append("That is the film's own cast list, spelled as its makers spell it. It does NOT say who ");
                oPrompt.Append("is in this shot. Use one of those names when you can actually tell that this is that ");
                oPrompt.Append("person — because you were told what they look like, or because the picture itself says ");
                oPrompt.Append("so, such as a caption on screen naming them. Otherwise describe people by what you can ");
                oPrompt.Append("see and give no name. Never pick a name from that list because it is the only one ");
                oPrompt.Append("left, or because somebody of about the right sort is in shot.\n");
            }
            if (lRecent.Count > 0)
            {
                oPrompt.Append("You have just said, of the moments before this one: ");
                foreach (string sOld in lRecent) oPrompt.Append("\"" + sOld + "\" ");
                oPrompt.Append("\nSay none of that again. Describe only what has changed since.\n");
            }
            // Last, because a model weighs the end of a prompt most, and this
            // is the one piece of caption evidence tied to THIS moment rather
            // than to the film as a whole.
            if (sSpeakerNear != "")
            {
                oPrompt.Append("\nAround this moment in the film, the captions have " + sSpeakerNear + " speaking.\n");
                oPrompt.Append("Whoever speaks either side of a silence is almost certainly in this SCENE. They are ");
                oPrompt.Append("not necessarily in this FRAME: a film cuts to the listener as often as the speaker, ");
                oPrompt.Append("and this moment is a silence between lines. If exactly one person is named there and ");
                oPrompt.Append("exactly one person is in shot, saying who it is will usually be right. If several are ");
                oPrompt.Append("named, or several are in shot, do not guess which is which.\n");
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
            // Nought, whatever the detail. The room was measured and the margins
            // are already inside it; anything past that is over the dialogue.
            return 0.0;
        }

        // The second stage, after AutoAD-Zero (Oxford VGG): the vision model is
        // asked to look thoroughly, and a language model then compresses what it
        // saw into one spoken description. Perceiving and being concise are
        // different jobs; asked to do both at once a vision model spends its
        // attention on the picture and its words on whatever comes first.
        //
        // No second model is installed: this is the same model with no image
        // attached, so nothing is loaded or unloaded between the two calls.
        static string sEstablished = "";
        static int iEstablishedAt = 0;

        // A short account of what this film has turned out to be, written from
        // the descriptions so far by the same model with no picture attached.
        // Cheap -- one text call every twenty-five descriptions -- and it gives
        // every later description a memory of the film it belongs to.
        static void rememberFilm(List<Moment> lDone)
        {
            if (lDone.Count < iDefaultRememberEvery) return;
            if (lDone.Count - iEstablishedAt < iDefaultRememberEvery) return;
            iEstablishedAt = lDone.Count;
            StringBuilder oSoFar = new StringBuilder();
            int iFrom = Math.Max(0, lDone.Count - 60);
            for (int iAt = iFrom; iAt < lDone.Count; iAt++)
            {
                if (lDone[iAt].sText != "") oSoFar.Append(lDone[iAt].sText + " ");
            }
            if (oSoFar.Length < 200) return;
            StringBuilder oPrompt = new StringBuilder();
            if (sEstablished != "") oPrompt.Append("What was established earlier in this film:\n" + sEstablished + "\n\n");
            oPrompt.Append("Descriptions written since:\n" + oSoFar.ToString() + "\n\n");
            oPrompt.Append("In no more than 70 words, say what has been established about this film: who keeps appearing and how they are dressed, ");
            oPrompt.Append("where it takes place, and what is going on. Facts only, nothing about the descriptions themselves. ");
            oPrompt.Append("This is a note to yourself, to save working it out again.");
            Dictionary<string, object> dOptions = new Dictionary<string, object>();
            dOptions["temperature"] = 0.2;
            dOptions["num_predict"] = 160;
            Dictionary<string, object> dPayload = new Dictionary<string, object>();
            dPayload["model"] = text("model");
            dPayload["prompt"] = oPrompt.ToString();
            dPayload["stream"] = false;
            dPayload["keep_alive"] = "30m";
            dPayload["options"] = dOptions;
            JavaScriptSerializer oSerializer = new JavaScriptSerializer();
            oSerializer.MaxJsonLength = int.MaxValue;
            waitingOn("noting what the film has established");
            string sAnswer = postJsonPumping(text("url") + "/api/generate", oSerializer.Serialize(dPayload));
            waitingOn("");
            if (sAnswer == "") return;
            try
            {
                Dictionary<string, object> dReply = oSerializer.Deserialize<Dictionary<string, object>>(sAnswer);
                if (!dReply.ContainsKey("response")) return;
                string sNote = Convert.ToString(dReply["response"]).Trim();
                if (sNote.Length > 700) sNote = sNote.Substring(0, 700);
                if (sNote.Split(' ').Length < 8) return;
                sEstablished = sNote;
                logMessage("What the film has established, after " + lDone.Count.ToString() + " descriptions: " + sNote, "INFO", "");
            }
            catch (Exception)
            {
            }
        }

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
            string sAnswer = postJsonPumping(text("url") + "/api/generate", oSerializer.Serialize(dPayload));
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

        static string describeImage(string sImagePath, int iMaxWords, List<string> lRecent, string sContext, bool bAgain, bool bNewScene, string sJudgment, bool bOverSound, List<string> lNames, string sJustSaid, bool bInsist, double nAt)
        {
            string sPrompt = promptFor(iMaxWords, lRecent, sContext, bNewScene, bOverSound && !bInsist, lNames, sJustSaid, nAt);
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
            string sAnswer = postJsonPumping(text("url") + "/api/generate", oSerializer.Serialize(dPayload));
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

        // The same call, with the window kept alive while it runs. The call blocks
        // this thread for ten seconds or more, and nothing pumps the message queue
        // meanwhile: Windows decides the program has stopped, the window never
        // repaints, and a screen reader reads the title it was last told about --
        // which is why Alt+Tabbing back said "Starting" two hours into a run.
        // The work goes to a thread and this one pumps until it returns.
        static string postJsonPumping(string sUrl, string sBody)
        {
            if (ownerForm() == null) return postJson(sUrl, sBody);
            string sAnswer = "";
            Thread oCall = new Thread(delegate()
            {
                sAnswer = postJson(sUrl, sBody);
            });
            oCall.IsBackground = true;
            oCall.Start();
            while (oCall.IsAlive)
            {
                try
                {
                    Application.DoEvents();
                }
                catch (Exception)
                {
                }
                Thread.Sleep(iDefaultPumpRest);
            }
            return sAnswer;
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

        // Function words that give a language away and are NOT English words.
        // Every entry was checked against English: "no", "me", "die", "sie" and
        // the like are left out, because a false match would silence a caption
        // that should have been read.
        static readonly string[] asNotEnglish = new string[] {
            // Spanish
            "el", "la", "los", "las", "que", "de", "del", "para", "por", "con", "pero", "esta", "está", "esto",
            "eso", "esa", "ese", "como", "una", "uno", "muy", "más", "mas", "donde", "dónde", "cuando", "cuándo",
            "porque", "también", "tambien", "hay", "son", "está", "usted", "señor", "señora", "gracias", "nada",
            "todo", "todos", "aquí", "aqui", "allí", "ahora", "siempre", "nunca", "puede", "tiene", "hacer",
            // French
            "le", "les", "des", "une", "est", "pas", "vous", "nous", "dans", "qui", "mais", "avec", "pour",
            "tout", "plus", "cette", "être", "etre", "avez", "faire", "bien", "très", "tres", "alors", "aussi",
            // German
            "der", "das", "ist", "nicht", "und", "ich", "wir", "mit", "auf", "aber", "sich", "auch", "eine",
            "einen", "haben", "sind", "wird", "kann", "noch", "schon", "immer",
            // Portuguese and Italian
            "não", "nao", "você", "voce", "ele", "ela", "isso", "che", "non", "sono", "questo", "della",
            "anche", "però", "quando", "perché", "perche", "molto", "sempre", "grazie"
        };


        // Does this look like a language the film is not being spoken in? Judged
        // on function words, which are what differ most between languages and
        // what a subtitle is full of.
        static bool notTheFilmsLanguage(string sText)
        {
            List<string> lWords = new List<string>();
            foreach (Match oWord in Regex.Matches(sText.ToLower(), @"[a-zà-ÿ']+"))
            {
                lWords.Add(oWord.Value.Trim('\''));
            }
            if (lWords.Count < 3) return false;
            int iForeign = 0;
            foreach (string sWord in lWords)
            {
                foreach (string sOne in asNotEnglish)
                {
                    if (sOne == sWord)
                    {
                        iForeign = iForeign + 1;
                        break;
                    }
                }
            }
            // Two function words of another language in a short line, or a fifth
            // of a longer one, is not a coincidence.
            if (iForeign >= 2 && lWords.Count <= 12) return true;
            return (double)iForeign / (double)lWords.Count >= 0.2;
        }

        // Words on screen that merely repeat what is being said are a caption of
        // the dialogue, whatever else they might be.
        static bool echoesTheDialogue(string sQuoted, string sJustSaid)
        {
            if (sJustSaid.Trim() == "") return false;
            List<string> lSeen = contentWords(sQuoted);
            if (lSeen.Count < 3) return false;
            List<string> lHeard = contentWords(sJustSaid);
            if (lHeard.Count == 0) return false;
            int iShared = 0;
            foreach (string sWord in lSeen)
            {
                if (lHeard.Contains(sWord)) iShared = iShared + 1;
            }
            return (double)iShared / (double)lSeen.Count >= 0.6;
        }

        // Take the subtitle out and leave the description. The convention that
        // introduces on-screen text carries the text with it, so both go.
        static string withoutSubtitles(string sText, string sJustSaid)
        {
            if (sText.Trim() == "") return sText;
            string sLeft = sText;
            foreach (Match oQuote in Regex.Matches(sText, @"(?:Words appear|Text appears|A caption reads|The subtitle reads|Subtitles read)\s*:?\s*""?([^""\r\n]{3,160})""?", RegexOptions.IgnoreCase))
            {
                string sQuoted = oQuote.Groups[1].Value.Trim();
                if (!notTheFilmsLanguage(sQuoted) && !echoesTheDialogue(sQuoted, sJustSaid)) continue;
                logMessage("Removed what looks like a subtitle: " + sQuoted, "INFO", "");
                sLeft = sLeft.Replace(oQuote.Value, "");
            }
            foreach (Match oQuote in Regex.Matches(sText, @"""([^""\r\n]{6,160})"""))
            {
                string sQuoted = oQuote.Groups[1].Value.Trim();
                if (!notTheFilmsLanguage(sQuoted) && !echoesTheDialogue(sQuoted, sJustSaid)) continue;
                logMessage("Removed what looks like a subtitle: " + sQuoted, "INFO", "");
                sLeft = sLeft.Replace(oQuote.Value, "");
            }
            sLeft = Regex.Replace(sLeft, @"\s+", " ").Replace(" .", ".").Replace(" ,", ",").Trim();
            sLeft = Regex.Replace(sLeft, @"[,;:]\s*$", ".");
            if (sLeft.Trim(' ', '.', ',', ';', ':', '"').Split(' ').Length < 3) return "";
            return sLeft;
        }

        // Sections of the context file headed by a time, so that a long guide can
        // be carried without sending all of it with every description. A heading
        // looks like "## 41:00 The Cyclops's island" or "## 1:12:30 Ithaca".
        // Everything before the first such heading belongs to the whole film.
        static List<double> lContextAt = new List<double>();
        static List<string> lContextSaid = new List<string>();

        static string splitContextByTime(string sWhole)
        {
            lContextAt.Clear();
            lContextSaid.Clear();
            if (sWhole == null) return "";
            StringBuilder oGeneral = new StringBuilder();
            StringBuilder oSection = null;
            foreach (string sLine in sWhole.Replace("\r\n", "\n").Split('\n'))
            {
                Match oHead = Regex.Match(sLine.Trim(), @"^#{1,6}\s+(?:(\d+):)?(\d{1,2}):(\d{2})\b\s*(.*)$");
                if (oHead.Success)
                {
                    double nWhen = (oHead.Groups[1].Value == "" ? 0.0 : double.Parse(oHead.Groups[1].Value) * 3600.0)
                                 + double.Parse(oHead.Groups[2].Value) * 60.0
                                 + double.Parse(oHead.Groups[3].Value);
                    oSection = new StringBuilder();
                    if (oHead.Groups[4].Value.Trim() != "") oSection.Append(oHead.Groups[4].Value.Trim() + ". ");
                    lContextAt.Add(nWhen);
                    lContextSaid.Add("");
                    continue;
                }
                if (oSection != null)
                {
                    if (sLine.Trim() != "") oSection.Append(sLine.Trim() + " ");
                    lContextSaid[lContextSaid.Count - 1] = Regex.Replace(oSection.ToString(), @"\s+", " ").Trim();
                    continue;
                }
                if (sLine.Trim() != "") oGeneral.Append(sLine.Trim() + " ");
            }
            if (lContextAt.Count > 0) logMessage("The context file is divided into " + lContextAt.Count.ToString()
                                                 + " timed sections; each description is given only the one covering it.", "INFO", "");
            return Regex.Replace(oGeneral.ToString(), @"\s+", " ").Trim();
        }

        static string sContextLast = "";
        static int iContextUsed = 0;
        static int iContextEchoed = 0;

        // Words this description shares with the context it was given, as a
        // fraction of the description. High means the model is repeating what it
        // was told rather than describing the picture.
        static double echoOfContext(string sText, string sGiven)
        {
            if (sText == "" || sGiven == "") return 0.0;
            List<string> lSaid = contentWords(sText);
            if (lSaid.Count == 0) return 0.0;
            List<string> lGiven = contentWords(sGiven);
            if (lGiven.Count == 0) return 0.0;
            int iShared = 0;
            foreach (string sWord in lSaid)
            {
                if (lGiven.Contains(sWord)) iShared = iShared + 1;
            }
            return (double)iShared / (double)lSaid.Count;
        }

        static string contextForMoment(double nAt)
        {
            if (lContextAt.Count == 0) return "";
            int iBest = -1;
            for (int iAt = 0; iAt < lContextAt.Count; iAt++)
            {
                if (lContextAt[iAt] <= nAt) iBest = iAt;
            }
            if (iBest < 0) return "";
            return lContextSaid[iBest];
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
        // Take out the part that judges, provided what is left is still a
        // description. A whole sentence goes if another sentence survives it;
        // otherwise the trailing clause holding the word goes. If neither leaves
        // enough behind, the text is returned unchanged and kept as it was --
        // an interpretive description is better than none.
        static string withoutJudgingPart(string sText, string sWord)
        {
            if (sWord == "") return sText;
            List<string> lParts = splitSentences(sText);
            if (lParts.Count > 1)
            {
                StringBuilder oKept = new StringBuilder();
                foreach (string sPart in lParts)
                {
                    if (Regex.IsMatch(sPart, @"\b" + Regex.Escape(sWord) + @"\b", RegexOptions.IgnoreCase)) continue;
                    if (oKept.Length > 0) oKept.Append(" ");
                    oKept.Append(sPart.Trim());
                }
                string sLeft = oKept.ToString().Trim();
                // Three words is a description: "A man walks." Requiring six
                // rejected perfectly good remainders and sent the text down the
                // clause-cutting path, which mangled it.
                if (sLeft.Split(' ').Length >= 3) return sLeft;
            }
            // One sentence, so the clause carrying the word is dropped instead.
            string sShorter = sText;
            for (int iTry = 0; iTry < 3; iTry++)
            {
                if (!Regex.IsMatch(sShorter, @"\b" + Regex.Escape(sWord) + @"\b", RegexOptions.IgnoreCase)) break;
                string sCut = dropLastClause(sShorter);
                if (sCut == sShorter) break;
                sShorter = sCut;
            }
            if (sShorter != sText && !Regex.IsMatch(sShorter, @"\b" + Regex.Escape(sWord) + @"\b", RegexOptions.IgnoreCase)
                && sShorter.Split(' ').Length >= 6) return sShorter;
            // Nothing safe to cut. A word like "suggesting" often joins two
            // observed things: dropping from it to the end leaves the first.
            Match oAt = Regex.Match(sText, @"\s*\b" + Regex.Escape(sWord) + @"\b.*$", RegexOptions.IgnoreCase);
            if (oAt.Success)
            {
                string sHead = sText.Substring(0, oAt.Index).Trim().TrimEnd(',', ';', ' ');
                sHead = Regex.Replace(sHead, @"\s+\b(and|but|or|with|as|while|that|which)\s*$", "", RegexOptions.IgnoreCase);
                // "The mood is tense" must not become "The mood is." A sentence
                // left hanging on its verb is worse than one that judges.
                // Cutting at the word can leave a short trailing phrase with
                // nothing to do: "by the shore, their posture". If the last
                // clause is that short it goes with the rest.
                int iComma = sHead.LastIndexOf(", ");
                if (iComma > 0 && sHead.Substring(iComma + 2).Trim().Split(' ').Length <= 3) sHead = sHead.Substring(0, iComma).Trim();
                bool bDangling = Regex.IsMatch(sHead, @"\b(is|are|was|were|be|been|being|seems?|appears?|looks?|feels?|remains?)\s*$", RegexOptions.IgnoreCase);
                if (!bDangling && sHead.Split(' ').Length >= 6)
                {
                    if (!sHead.EndsWith(".")) sHead = sHead + ".";
                    return sHead;
                }
            }
            return sText;
        }

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
        // Everybody the captions name as speaking, in the order they first
        // speak. This is a ROSTER, not a claim about any one frame: a film cuts
        // to the listener as often as to the speaker, and a narrator is never
        // in shot. What it is good for is knowing which names this film uses
        // and how its makers spell them, so the model neither invents a name
        // nor invents a spelling.
        //
        // On its own it cannot attach a name to a face. The context file is
        // where appearances live. The captions say who exists; the context file
        // says what they look like; a name can be used where the two meet.
        static List<string> namesFromCaptions(List<Speech> lFrom)
        {
            List<string> lWho = new List<string>();
            foreach (Speech oCue in lFrom)
            {
                string sWho = oCue.sWho == null ? "" : oCue.sWho.Trim();
                if (sWho == "") continue;
                // A note about the sound rather than a person: MAN ON RADIO is
                // useful, NARRATOR and AUDIENCE name nobody who can be seen.
                // Not a name, however the captions write it.
                //
                // Subtitles for the deaf label an unidentified speaker by what
                // they are: MAN, WOMAN, GIRL, MAN 2, REPORTER. Those went into
                // the roster as though they were people, and it showed: the
                // only roster entries that ever turned up in a description of
                // his NOVA film were MAN and WOMAN, which the description would
                // have used anyway. Worse than useless -- it tells the model
                // that "MAN" is a name this film uses, which invites it to
                // treat a generic word as an identification.
                if (Regex.IsMatch(sWho,
                        @"^(narrator|announcer|audience|all|both|crowd|voice ?over|v\.?o\.?|"
                      + @"man|woman|boy|girl|child|kid|baby|male|female|"
                      + @"reporter|interviewer|host|presenter|speaker|guest|caller|operator|"
                      + @"doctor|nurse|officer|teacher|student|driver|waiter|clerk|"
                      + @"computer|television|tv|radio|phone|telephone|recording|automated voice)"
                      + @"\s*\d*$", RegexOptions.IgnoreCase)) continue;
                // Nor a role with a number or a qualifier: "MAN 2", "SECOND
                // WOMAN", "MAN ON RADIO" names nobody you could pick out.
                if (Regex.IsMatch(sWho, @"^(first|second|third|another|other|young|old|older|elderly)\s+"
                      + @"(man|woman|boy|girl|child|voice|speaker)\s*\d*$", RegexOptions.IgnoreCase)) continue;
                bool bHaveIt = false;
                foreach (string sOne in lWho)
                {
                    if (string.Compare(sOne, sWho, true) == 0) bHaveIt = true;
                }
                if (!bHaveIt) lWho.Add(sWho);
            }
            return lWho;
        }

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

        // ---------- writing for a reader ----------
        //
        // Everything below exists to be listened to. The rules are the W3C Web
        // Accessibility Initiative's, from its Transcripts guidance, and the
        // reasoning is in the History entry rather than repeated here.

        // A section heading a person would say out loud. "0:10:00 to 0:20:00"
        // is four numbers; "From 10 minutes" is a place in the film.
        static string sectionTitle(double nFrom)
        {
            if (nFrom < 30.0) return "From the start";
            string sSaid = spokenTime(nFrom);
            if (sSaid == "") return "From the start";
            return "From " + sSaid.Replace(" min", " minutes");
        }

        // Sections earn their place only on a film long enough to want to move
        // about in. Below that they are two more things to read past.
        static bool wantsSections(double nDuration)
        {
            return nDuration >= nDefaultChapter * 2.0;
        }

        // A publisher's description is written to sell the video. The part
        // worth keeping says what the video is; the rest is addresses, appeals
        // to subscribe, and permission notices, and a screen reader says an
        // address one character at a time.
        static string tidyDescription(string sAbout)
        {
            if (sAbout == "") return "";
            StringBuilder oKept = new StringBuilder();
            List<string> lPieces = new List<string>();
            string sFlat = Regex.Replace(sAbout, @"\s+", " ").Trim();
            foreach (string sPiece in Regex.Split(sFlat, @"(?<=[\.\!\?])\s+"))
            {
                string sOne = sPiece.Trim();
                if (sOne == "") continue;
                // The splitter breaks after any full stop, including the one in
                // "e.g.". Put such a piece back on the one before it rather
                // than judging half a sentence on its own.
                if (lPieces.Count > 0 && endsAbbreviation(lPieces[lPieces.Count - 1]))
                {
                    lPieces[lPieces.Count - 1] = lPieces[lPieces.Count - 1] + " " + sOne;
                    continue;
                }
                lPieces.Add(sOne);
            }
            foreach (string sOne in lPieces)
            {
                // An address, and whatever sentence was carrying it.
                if (Regex.IsMatch(sOne, @"https?://|www\.")) continue;
                if (Regex.IsMatch(sOne, @"\b(subscribe|follow us|like us|our channel|twitter|facebook|instagram|patreon|"
                                       + @"permission to use|copyright|all rights reserved|usage policy|for more information, see)\b",
                                  RegexOptions.IgnoreCase)) continue;
                // What is left of a sentence once its address is gone is often
                // three words of nothing: "Visit", "See also".
                if (sOne.Split(' ').Length < 4) continue;
                // An ellipsis at the end is somebody else's truncation, and
                // the words before it are half a thought.
                if (sOne.TrimEnd().EndsWith("...") || sOne.TrimEnd().EndsWith("\u2026")) continue;
                // A bracket opened and never closed is half a thought.
                if (sOne.Split('(').Length != sOne.Split(')').Length) continue;
                if (oKept.Length > 0) oKept.Append(" ");
                oKept.Append(sOne);
                if (oKept.Length > 700) break;
            }
            // A description cut off part way leaves a fragment at the end, and
            // a fragment read aloud sounds like a fault in the program.
            while (oKept.Length > 0 && (!endsSentence(oKept.ToString()) || endsAbbreviation(oKept.ToString())))
            {
                // The search must start BEFORE the last character. Ending on
                // "e.g." the stop being complained about IS the last character,
                // and looking from the end would find it again, set the same
                // length, and go round for ever.
                string sSoFar = oKept.ToString().TrimEnd();
                int iBack = sSoFar.Length < 2 ? -1 : sSoFar.LastIndexOfAny(new char[] { '.', '!', '?' }, sSoFar.Length - 2);
                if (iBack < 0) { oKept.Length = 0; break; }
                oKept.Length = iBack + 1;
            }
            string sLeft = oKept.ToString().Trim();
            // Too little left to be worth a heading and a paragraph.
            if (sLeft.Split(' ').Length < 12) return "";
            return sLeft;
        }

        // The head of every document: what it is, where it came from, how long
        // it runs, and how it was made. Four or five lines, and no more, since
        // everything here is read before the reader reaches a single word of
        // the film.
        static bool writeHead(StreamWriter fDoc, string sHeading, string sSourceName, double nDuration, string sMadeBy)
        {
            fDoc.WriteLine("# " + sHeading);
            fDoc.WriteLine("");
            if (sVideoTitle != "" && sVideoTitle != sSourceName) fDoc.WriteLine("- Title: " + sVideoTitle);
            if (sVideoBy != "") fDoc.WriteLine("- Published by: " + sVideoBy);
            if (nDuration > 0.0) fDoc.WriteLine("- Running time: " + formatClock(nDuration));
            fDoc.WriteLine("- " + sMadeBy);
            fDoc.WriteLine("- Made: " + DateTime.Now.ToString("d MMMM yyyy"));
            if (sVideoAddress != "")
            {
                string sLabel = sVideoTitle == "" ? "the original video" : sVideoTitle;
                fDoc.WriteLine("- Watch: [" + sLabel.Replace("[", "(").Replace("]", ")") + "](" + sVideoAddress + ")");
            }
            fDoc.WriteLine("");
            string sAbout = tidyDescription(sVideoAbout);
            if (sAbout != "")
            {
                fDoc.WriteLine("## About this video");
                fDoc.WriteLine("");
                fDoc.WriteLine(sAbout);
                fDoc.WriteLine("");
            }
            return true;
        }

        // One paragraph of the film's own words. A speaker's name goes in bold
        // at the front, which is how a transcript has always been laid out and
        // which a screen reader can be asked to announce.
        static string saidAs(Speech oSpeech)
        {
            if (oSpeech.sWho == "") return oSpeech.sText;
            return "**" + oSpeech.sWho + ".** " + oSpeech.sText;
        }

        static void writeMarkdown(List<Moment> lMoments, string sPath, string sSourceName, double nDuration)
        {
            StreamWriter fDoc = new StreamWriter(sPath, false, new UTF8Encoding(true));
            bool bSections = wantsSections(nDuration);
            writeHead(fDoc, "What can be seen in " + sSourceName, sSourceName, nDuration,
                      counted(lMoments.Count, "description", "descriptions") + ", written by " + text("model"));
            fDoc.WriteLine("What a sighted viewer would have seen, in the order it happens.");
            fDoc.WriteLine("");
            double nChapter = -1.0;
            if (!bSections)
            {
                fDoc.WriteLine("## The description");
                fDoc.WriteLine("");
            }
            foreach (Moment oMoment in lMoments)
            {
                if (bSections)
                {
                    double nThis = Math.Floor(oMoment.nStart / nDefaultChapter) * nDefaultChapter;
                    if (nThis != nChapter)
                    {
                        nChapter = nThis;
                        fDoc.WriteLine("## " + sectionTitle(nChapter));
                        fDoc.WriteLine("");
                    }
                }
                fDoc.WriteLine(oMoment.sText);
                fDoc.WriteLine("");
            }
            fDoc.Close();
        }

        // yt-dlp is updated at most once in a session, however many videos are
        // refused.
        // Set when ExifTool does not know the two IPTC accessibility fields by
        // name, which means that copy predates them: they were added to the
        // standard in October 2021. Decided once by the self test, then held.
        // Formats this copy of ExifTool has told us it cannot write. Learnt
        // during the run rather than listed, because the answer depends on the
        // version in use.
        static List<string> lNoRoomKinds = new List<string>();
        // Whether the captions in hand were made by a machine. HomerScribe has
        // always been able to tell -- looksAutomatic() decides from per-word
        // timing tags and from cues that roll up the screen repeating the line
        // before -- and the logs have said which all along. What it did not do
        // was act on it.
        // The film's cast list from its captions, and whoever the captions
        // have speaking around the moment being described. Statics rather than
        // two more arguments threaded through five call sites, which is how
        // lCaptions and the rest already travel.
        static List<string> lSpeakerRoster = new List<string>();
        static string sSpeakerNear = "";
        static bool bCaptionsAuto = false;
        static bool bNoAltTags = false;
        // Set when the definitions below are being supplied to make up for it.
        static bool bTeachAltTags = false;
        static string sAltConfigPath = "";
        static bool bTriedUpdate = false;
        static bool bTranscribed = false;
        static bool bSpeechReady = false;
        static bool bSpeechDoubtful = false;

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
            bool bSections = wantsSections(nDuration);
            bool bAnySounds = false;
            bool bAnyNames = false;
            foreach (Speech oSpeech in lSpeech)
            {
                if (soundOnly(oSpeech.sText)) bAnySounds = true;
                if (oSpeech.sWho != "") bAnyNames = true;
            }
            // One transcript, whichever way it was made, with a line saying
            // which way that was. Two files of near-identical words would only
            // make the reader choose between them.
            writeHead(fDoc, "What is said in " + sSourceName, sSourceName, nDuration,
                      sTranscriptFrom == ""
                      ? "Heard by Whisper " + text("whisper-model")
                      : "Taken from " + sTranscriptFrom);
            // A note only where there is something for it to explain. Telling
            // a reader what a speaker's name looks like, in a film where
            // nobody is named, is a sentence spent on nothing.
            if (sTranscriptFrom != "")
            {
                fDoc.WriteLine("A person wrote these words down while the film was made, so they are more exact than any machine could hear."
                             + (bAnyNames ? " A name in bold is whoever is speaking." : "")
                             + (bAnySounds ? " Words inside brackets are a sound rather than speech, such as a door slamming or music starting." : ""));
                fDoc.WriteLine("");
            }
            double nChapter = -1.0;
            if (!bSections)
            {
                fDoc.WriteLine("## What is said");
                fDoc.WriteLine("");
            }
            foreach (Speech oSpeech in lSpeech)
            {
                if (oSpeech.sText == "") continue;
                if (bSections)
                {
                    double nThis = Math.Floor(oSpeech.nStart / nDefaultChapter) * nDefaultChapter;
                    if (nThis != nChapter)
                    {
                        nChapter = nThis;
                        fDoc.WriteLine("## " + sectionTitle(nChapter));
                        fDoc.WriteLine("");
                    }
                }
                fDoc.WriteLine(saidAs(oSpeech));
                fDoc.WriteLine("");
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
                // A caption in brackets is a sound, not speech -- and it is the
                // single thing this document exists to carry that no transcript
                // of speech ever holds. Labelling it Spoken would lose it.
                string sKind = soundOnly(oSpeech.sText) ? "Sound" : "Spoken";
                lLines.Add(new string[] { num(oSpeech.nStart), formatClock(oSpeech.nStart), sKind, saidAs(oSpeech) });
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
            bool bSections = wantsSections(nDuration);
            writeHead(fDoc, "A descriptive transcript of " + sSourceName, sSourceName, nDuration,
                      counted(lLines.Count, "entry", "entries")
                      + (sTranscriptFrom == "" ? ", the words heard by Whisper " + text("whisper-model")
                                               : ", the words taken from " + sTranscriptFrom));
            // "Descriptive transcript" is the term of art, and the W3C Web
            // Accessibility Initiative names this document as the one a reader
            // who is both Deaf and blind needs. Saying so lets a reader who
            // knows the term recognise what they have.
            fDoc.WriteLine("Everything the film offers, in the order it happens: what was said, what could be heard, "
                         + "and what could be seen. This is what is called a descriptive transcript, and it is the whole "
                         + "of the film for a reader who can neither watch nor listen to it.");
            fDoc.WriteLine("");
            fDoc.WriteLine("**Paragraphs beginning \"Description\" are not part of the film.** They were written about it "
                         + "afterwards, by a machine looking at the picture. Everything else is the film's own.");
            fDoc.WriteLine("");
            double nChapter = -1.0;
            if (!bSections)
            {
                fDoc.WriteLine("## The film");
                fDoc.WriteLine("");
            }
            foreach (string[] asLine in lLines)
            {
                double nAt = 0.0;
                double.TryParse(asLine[0], NumberStyles.Any, CultureInfo.InvariantCulture, out nAt);
                if (bSections)
                {
                    double nThis = Math.Floor(nAt / nDefaultChapter) * nDefaultChapter;
                    if (nThis != nChapter)
                    {
                        nChapter = nThis;
                        fDoc.WriteLine("## " + sectionTitle(nChapter));
                        fDoc.WriteLine("");
                    }
                }
                // Only what was added carries a label. An unlabelled paragraph
                // is the film speaking for itself, which is most of them, and
                // labelling those too would double the reading for nothing.
                fDoc.WriteLine(asLine[2] == "Description" ? "**Description.** " + asLine[3] : asLine[3]);
                fDoc.WriteLine("");
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
                    sButton = oDialog.runWithButtons(new string[] { "Help", "Default settings", "OK", "Cancel" }, true);
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
                // Only OK means go. Anything else -- a button added later, or one
                // whose handling changed underneath us -- returns to the dialog
                // rather than being taken for agreement.
                if (string.Compare(sButton, "OK", true) != 0)
                {
                    logMessage("The dialog returned \"" + sButton + "\", which is not OK, so it is shown again.", "INFO", "");
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
            if (bSecondInstance)
            {
                logMessage("Not saving the settings: another HomerScribe is running and they are shared.", "INFO", "");
                return;
            }
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
            if (oProcess.ExitCode != 0)
            {
                string sTrouble = oErr.ToString();
                // A geographic refusal drags a list of countries behind it.
                int iList = sTrouble.IndexOf("The video is available in", StringComparison.OrdinalIgnoreCase);
                if (iList > 0) sTrouble = sTrouble.Substring(0, iList).Trim() + " [country list omitted]";
                logMessage("Error output: " + tail(sTrouble, 1500), "ERROR", "");
            }
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
            logMessage("Reading the playlist at " + sAddress, "INFO", "");
            announce("Initializing", -1.0, 1.0, "Reading the playlist, which may take a moment.");
            string sOut = "";
            string sErr = "";
            int iCode = runCommand(sYtDlp, "--flat-playlist --no-warnings" + quietly() + " --print " + quoted("%(url)s") + " " + quoted(sAddress), out sOut, out sErr);
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
        static readonly string[] asListKinds = new string[] { ".txt", ".md", ".lst", ".list", ".markdown" };

        static bool looksLikeList(string sPath)
        {
            string sKind = Path.GetExtension(sPath).ToLower();
            foreach (string sOne in asListKinds)
            {
                if (sOne == sKind) return true;
            }
            return false;
        }

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
        // yt-dlp names its releases by date, "2026.07.04". YouTube changes
        // often enough that an old copy is a real suspect rather than a
        // formality, so the age is worked out and said once at startup.
        static int daysOldTool(string sVersion)
        {
            Match oWhen = Regex.Match(sVersion == null ? "" : sVersion, @"(\d{4})\.(\d{2})\.(\d{2})");
            if (!oWhen.Success) return -1;
            try
            {
                DateTime dtMade = new DateTime(int.Parse(oWhen.Groups[1].Value),
                                               int.Parse(oWhen.Groups[2].Value),
                                               int.Parse(oWhen.Groups[3].Value));
                return (int)DateTime.Now.Subtract(dtMade).TotalDays;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        // The ways of asking for a video, in the order they are tried. The
        // first is what has always been asked for; the rest are what to do when
        // that is refused. A player client is which of YouTube's own apps
        // yt-dlp pretends to be, and they are not all served alike.
        static List<string[]> waysToFetch()
        {
            List<string[]> lWays = new List<string[]>();
            string sPinned = text("player-client");
            if (sPinned != "")
            {
                lWays.Add(new string[] { "the " + sPinned + " player, as set",
                                         " --extractor-args " + quoted("youtube:player_client=" + sPinned) });
                return lWays;
            }
            lWays.Add(new string[] { "the usual way", "" });
            // Not a player at all: one stream carrying both picture and sound,
            // which is served differently from the best of each taken apart.
            // Tried early because it is the cheapest thing that often works.
            lWays.Add(new string[] { "a single combined stream", " ONE-STREAM" });
            // Then the players. The television and iOS ones ask for less and
            // are commonly served when the web one is not; the web player is
            // tried last because it is the one most often handed an empty menu.
            lWays.Add(new string[] { "the television player", " --extractor-args " + quoted("youtube:player_client=tv") });
            lWays.Add(new string[] { "the iOS player", " --extractor-args " + quoted("youtube:player_client=ios") });
            lWays.Add(new string[] { "the mobile web player", " --extractor-args " + quoted("youtube:player_client=mweb") });
            lWays.Add(new string[] { "the Safari web player", " --extractor-args " + quoted("youtube:player_client=web_safari") });
            lWays.Add(new string[] { "the web player", " --extractor-args " + quoted("youtube:player_client=web") });
            // A player whose menu is empty leaves nothing for "best video plus
            // best audio" to match, so the last try asks for anything at all.
            lWays.Add(new string[] { "any format at all", " ANY-FORMAT" });
            return lWays;
        }

        // A refusal is worth asking a different way. A video that is private,
        // deleted or geo-blocked is not: it will be just as absent from every
        // player, and six tries and an update would be six tries and an update
        // spent to learn nothing.
        static bool wasRefused(string sTrouble)
        {
            if (sTrouble == null || sTrouble == "") return true;
            // "Requested format is not available" from a player client does NOT
            // mean the video is missing. It means THAT player was handed an
            // empty or different menu -- the web player needs a token it was
            // not given, and so offers nothing that matches. Treating it as a
            // dead end stopped the chain after ONE alternative on 21 August,
            // and the four remaining ways were never tried.
            return Regex.IsMatch(sTrouble, @"(403|429|forbidden|too many requests|unable to download video data|"
                                         + @"fragment|throttl|precondition|requested format is not available|"
                                         + @"no video formats|only images are available|sign in to confirm)",
                                 RegexOptions.IgnoreCase);
        }

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
                // Cut at the country list, which yt-dlp appends to a geographic
                // refusal and which runs to several hundred names.
                int iList = sSaid.IndexOf(". The video is available in", StringComparison.OrdinalIgnoreCase);
                if (iList < 0) iList = sSaid.IndexOf("available in the following countries", StringComparison.OrdinalIgnoreCase);
                if (iList > 0) sSaid = sSaid.Substring(0, iList).Trim();
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
            // The page's own details are asked for HERE only when the answer
            // decides what happens next -- that is, when a transcript alone is
            // wanted and the caption tracks settle whether the film needs
            // fetching at all. Otherwise the question waits until after the
            // download.
            //
            // This ordering is not fussiness. Until 1.0.153 the question was
            // always asked first, which put a THIRD full extraction of the
            // video in front of every download where 1.0.146 had two. Every
            // download since has met a 403 on the media stream, and the last
            // version known to fetch a film successfully is the last version
            // that did not make that extra call. That may be coincidence and it
            // may not; asking only when the answer is needed costs nothing
            // either way.
            bool bWordsAlone = flag("transcribe") && !flag("describe") && flag("captions");
            if (bWordsAlone) webMetadata(sAddress);
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
            runCommand(sYtDlp, "--no-playlist --no-warnings --restrict-filenames" + quietly()
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
            if (sStem != "" && !flag("force"))
            {
                string sWouldBe = Path.Combine(sFolder, sDefaultDescribedStem + ".mkv");
                string sWouldBeMp3 = Path.Combine(sFolder, sDefaultDescribedStem + ".mp3");
                string sWouldBeText = Path.Combine(sFolder, sDefaultTranscriptName);
                bool bFilmThere = File.Exists(sWouldBe) || File.Exists(sWouldBeMp3);
                bool bTextThere = File.Exists(sWouldBeText);
                bool bWantedFilm = flag("describe");
                bool bWantedText = flag("transcribe");
                if ((!bWantedFilm || bFilmThere) && (!bWantedText || bTextThere))
                {
                    logMessage("Not downloading " + sAddress + ": what was asked for is already in " + sFolder + ". "
                               + "Tick Force overwrite to do it again.", "INFO",
                               "Already done, so it is not downloaded again: " + (sTitle == "" ? sAddress : sTitle));
                    sLastSkippedFolder = sFolder;
                    return sAlreadyDone;
                }
            }
            string sSaying = sTitle == "" ? sAddress : sTitle;
            string sCookies = "";
            if (text("browser-cookies") != "") sCookies = " --cookies-from-browser " + text("browser-cookies");
            // Only the words were asked for, and the page has them written
            // down. Then the film is not needed at all: not to hear, since the
            // captions say what is said, and not to look at, since nothing is
            // being described. Fetching it would be a hundred megabytes and
            // several minutes spent to produce a file nobody asked for.
            // sStem empty means yt-dlp would not say what it will call the
            // file, and without that there is no per-video folder and no stem
            // to find the captions by afterwards. Then the film is fetched, as
            // before, and its name learnt from the download.
            if (flag("transcribe") && !flag("describe") && flag("captions") && sStem != "")
            {
                bool bByHandEarly = false;
                if (trackToFetch(out bByHandEarly) != "")
                {
                    logMessage("Only a transcript was asked for and this video carries its own captions, "
                               + "so the film itself is not downloaded.", "INFO",
                               "Taking the captions only, without the film: " + sSaying);
                    if (fetchCaptions(sYtDlp, sAddress, sFolder, sFfmpeg, sCookies))
                    {
                        sCaptionsOnlyFolder = sFolder;
                        sCaptionsOnlyStem = sStem;
                        return sCaptionsOnly;
                    }
                    logMessage("The captions could not be had after all, so the film will be fetched and listened to instead.",
                               "INFO", "");
                }
            }
            logMessage("Downloading " + sAddress + " (" + sSaying + ") with " + sYtDlp, "INFO", "Downloading " + sSaying);
            announce("Initializing", -1.0, 1.0, "Downloading " + sSaying);
            // The captions are NOT asked for here. They used to be, and a
            // subtitle yt-dlp could not fetch then aborted the whole download:
            // on 21 August every one of four sources failed that way and no
            // video came down at all. They are fetched by a command of their
            // own once the film is safely here, where failing costs only the
            // captions.
            // Every way of asking, in turn, stopping at the first that works.
            // The first is exactly what 1.0.146 asked for and what has always
            // been asked for since; the rest exist because YouTube has begun
            // refusing it, and one of them is usually still served.
            int iCode = 1;
            bool bSaidWhy = false;
            // The reason worth reporting is why the USUAL way failed. A later
            // attempt complaining that a player offers no matching format
            // describes that attempt, not the problem, and putting it in the
            // results box sent me looking in the wrong place.
            string sFirstTrouble = "";
            foreach (string[] asWay in waysToFetch())
            {
                string sExtra = asWay[1];
                string sFormat = "bv*+ba/b";
                if (sExtra == " ONE-STREAM")
                {
                    sExtra = "";
                    sFormat = "b/bv*+ba";
                }
                if (sExtra == " ANY-FORMAT")
                {
                    sExtra = "";
                    sFormat = "bv*+ba/b/best/worst";
                }
                string sArguments = "--no-playlist --no-simulate --newline --restrict-filenames" + sCookies + sExtra + quietly()
                                  + " --merge-output-format mkv"
                                  + " --ffmpeg-location " + quoted(Path.GetDirectoryName(sFfmpeg))
                                  + " --print-to-file after_move:filepath " + quoted(sPathFile)
                                  + " -f " + quoted(sFormat)
                                  + " -o " + quoted(Path.Combine(sFolder, "%(title)s.%(ext)s"))
                                  + " " + quoted(sAddress);
                if (bSaidWhy) logMessage("Refused. Trying " + asWay[0] + ".", "INFO", "Refused. Trying " + asWay[0] + ".");
                iCode = runStreamed(sYtDlp, sArguments, "Downloading");
                if (iCode == 0)
                {
                    // The earlier complaints no longer describe anything: the
                    // film is here. Leaving them set would put a stale reason
                    // in a later message.
                    sLastFetchTrouble = "";
                    if (bSaidWhy) logMessage("That worked: " + asWay[0] + ". Set --player-client to keep it and save the tries.",
                                             "INFO", "Fetched using " + asWay[0] + ".");
                    break;
                }
                sLastFetchTrouble = whyItFailed(sLastStreamedTrouble);
                if (sFirstTrouble == "") sFirstTrouble = sLastFetchTrouble;
                if (!wasRefused(sLastFetchTrouble))
                {
                    logMessage("That is not a refusal, so asking a different way would not help: " + sLastFetchTrouble,
                               "INFO", "");
                    break;
                }
                if (!bSaidWhy)
                {
                    logMessage("The usual way was refused: " + (sLastFetchTrouble == "" ? "no reason given" : sLastFetchTrouble)
                               + ". Trying the other ways of asking.", "INFO", "");
                    bSaidWhy = true;
                }
            }
            // Nothing was served. An out-of-date yt-dlp is the commonest reason
            // for that, so it is updated here rather than left as something to
            // be told to do, and the usual way tried once more.
            // Still refused, and no browser cookies were offered. A signed-in
            // session is often served what an anonymous one is not, and Edge is
            // on every Windows machine. This is the sober half of "drive the
            // browser": borrow its identity, not its download.
            if (iCode != 0 && sCookies == "" && flag("browser-session"))
            {
                foreach (string sBrowser in new string[] { "edge", "chrome", "firefox" })
                {
                    logMessage("Refused every way. Trying again with " + sBrowser + "'s cookies.",
                               "INFO", "Trying again with " + sBrowser + "'s cookies.");
                    string sWith = "--no-playlist --no-simulate --newline --restrict-filenames" + quietly()
                                 + " --cookies-from-browser " + sBrowser
                                 + " --merge-output-format mkv"
                                 + " --ffmpeg-location " + quoted(Path.GetDirectoryName(sFfmpeg))
                                 + " --print-to-file after_move:filepath " + quoted(sPathFile)
                                 + " -f " + quoted("bv*+ba/b")
                                 + " -o " + quoted(Path.Combine(sFolder, "%(title)s.%(ext)s"))
                                 + " " + quoted(sAddress);
                    iCode = runStreamed(sYtDlp, sWith, "Downloading");
                    if (iCode == 0)
                    {
                        sLastFetchTrouble = "";
                        logMessage("That worked, using " + sBrowser + "'s cookies. Set --browser-cookies " + sBrowser
                                   + " to do it first and save the tries.", "INFO", "Fetched using " + sBrowser + "'s cookies.");
                        break;
                    }
                    // A browser that is not installed, or whose cookies cannot
                    // be read, is not worth complaining about at length.
                    logMessage("  " + sBrowser + ": " + whyItFailed(sLastStreamedTrouble), "INFO", "");
                }
            }
            if (iCode != 0 && sFirstTrouble != "") sLastFetchTrouble = sFirstTrouble;
            if (iCode != 0 && wasRefused(sLastFetchTrouble) && flag("update-tools") && !bTriedUpdate)
            {
                bTriedUpdate = true;
                logMessage("Every way of asking was refused. Updating yt-dlp and trying once more.",
                           "INFO", "Updating yt-dlp and trying once more.");
                string sUpOut = "";
                string sUpErr = "";
                string sChannel = text("update-channel");
                // Nightly is where a fix for something that broke this week is.
                // yt-dlp's own guidance is to be on it before reporting a fault.
                runCommand(sYtDlp, sChannel == "" || string.Compare(sChannel, "stable", true) == 0
                                   ? "-U" : "--update-to " + quoted(sChannel), out sUpOut, out sUpErr);
                logMessage("yt-dlp said: " + tail(sUpOut + sUpErr, 300), "INFO", "");
                string sNowOut = "";
                string sNowErr = "";
                runCommand(sYtDlp, "--version", out sNowOut, out sNowErr);
                logMessage("yt-dlp is now version " + (sNowOut + sNowErr).Trim(), "INFO", "");
                string sAgain = "--no-playlist --no-simulate --newline --restrict-filenames" + sCookies + quietly()
                              + " --merge-output-format mkv"
                              + " --ffmpeg-location " + quoted(Path.GetDirectoryName(sFfmpeg))
                              + " --print-to-file after_move:filepath " + quoted(sPathFile)
                              + " -f " + quoted("bv*+ba/b")
                              + " -o " + quoted(Path.Combine(sFolder, "%(title)s.%(ext)s"))
                              + " " + quoted(sAddress);
                iCode = runStreamed(sYtDlp, sAgain, "Downloading");
                if (iCode == 0)
                {
                    sLastFetchTrouble = "";
                    logMessage("That worked. yt-dlp was out of date.", "INFO", "That worked: yt-dlp was out of date.");
                }
                else
                {
                    sLastFetchTrouble = whyItFailed(sLastStreamedTrouble);
                }
            }
            if (iCode != 0)
            {
                sLastFetchTrouble = whyItFailed(sLastStreamedTrouble);
                logMessage("The video could not be fetched. " + (sLastFetchTrouble == "" ? "yt-dlp gave no reason." : "yt-dlp said: " + sLastFetchTrouble),
                           "ERROR", "Could not fetch that video. " + sLastFetchTrouble);
                // The film is out of reach, but the words may not be. If a
                // transcript was also asked for and the page carries captions,
                // take those. Half of what was wanted, with the reason the
                // other half failed said plainly, beats nothing at all --
                // and the captions come down a different road from the media,
                // which is the road that is shut.
                if (flag("transcribe") && flag("captions") && sStem != "")
                {
                    bool bByHandLate = false;
                    if (!bWordsAlone) webMetadata(sAddress);
                    if (trackToFetch(out bByHandLate) != "")
                    {
                        logMessage("The film cannot be had, so it cannot be described. Trying for its captions instead, "
                                   + "since a transcript was wanted too and captions do not come down the same road as the film.",
                                   "INFO", "Could not fetch the film. Trying for its captions instead.");
                        if (fetchCaptions(sYtDlp, sAddress, sFolder, sFfmpeg, sCookies))
                        {
                            sCaptionsOnlyFolder = sFolder;
                            sCaptionsOnlyStem = sStem;
                            sCouldNotDescribe = sLastFetchTrouble == "" ? "it could not be fetched" : sLastFetchTrouble;
                            return sCaptionsOnly;
                        }
                    }
                }
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
            // Now that the film is safely here, ask the page what it says about
            // itself, for the head of every document and for the caption track.
            if (!bWordsAlone) webMetadata(sAddress);
            // Now the captions, in a command of their own, asking for exactly
            // one track chosen from what the page said it had. Whatever happens
            // here, the film is already down and the run goes on: no captions
            // means Whisper does the transcript, which is the old behaviour and
            // a perfectly good one.
            if (flag("transcribe") && flag("captions"))
            {
                if (!fetchCaptions(sYtDlp, sAddress, sFolder, sFfmpeg, sCookies))
                    logMessage("So the film will be listened to instead.", "INFO", "");
            }
            return sPath;
        }

        // The captions, and nothing else. --skip-download is the whole point:
        // it asks for the words without asking for the film, which is both far
        // less to fetch and the part of YouTube that has gone on working while
        // media requests were being refused.
        //
        // --convert-subs vtt matters more than it looks. YouTube also serves
        // srv3 and ttml, which readCaptions cannot parse, and "vtt/srt/best"
        // will fall back to one of those quite happily.
        static bool fetchCaptions(string sYtDlp, string sAddress, string sFolder, string sFfmpeg, string sCookies)
        {
            bool bByHand = false;
            string sTrack = trackToFetch(out bByHand);
            if (sTrack == "")
            {
                logMessage("The page named no English caption track.", "INFO", "");
                return false;
            }
            logMessage("Fetching one caption track, " + sTrack + ", "
                       + (bByHand ? "written by a person." : "made automatically, since nobody wrote one."), "INFO", "");
            string sOut = "";
            string sErr = "";
            int iCode = runCommand(sYtDlp, "--no-playlist --skip-download --restrict-filenames --sleep-requests 1" + sCookies + quietly()
                                         + (bByHand ? " --write-subs" : " --write-auto-subs")
                                         + " --sub-langs " + quoted(sTrack)
                                         + " --sub-format " + quoted("vtt/srt/best")
                                         + " --convert-subs vtt"
                                         + " --ffmpeg-location " + quoted(Path.GetDirectoryName(sFfmpeg))
                                         + " -o " + quoted(Path.Combine(sFolder, "%(title)s.%(ext)s"))
                                         + " " + quoted(sAddress), out sOut, out sErr);
            if (iCode != 0)
            {
                logMessage("The captions could not be fetched. yt-dlp said: " + tail(sErr, 200), "INFO", "");
                return false;
            }
            logMessage("Captions fetched.", "INFO", "");
            return true;
        }

        // A transcript made from captions alone, with no film on the disk. The
        // ordinary route through runOne cannot serve, because every step of it
        // begins by opening a file.
        static int transcribeFromCaptions(string sAddress)
        {
            string sFolder = sCaptionsOnlyFolder;
            string sStem = sCaptionsOnlyStem;
            // runOne opens this, and runOne is not on this route.
            lCaptions = new List<Speech>();
            sTranscriptFrom = "";
            // captionsBeside looks in the folder holding the path it is given
            // and matches on the stem, so a name that was never written serves
            // perfectly well to point at where the captions landed.
            string sWouldBe = Path.Combine(sFolder, sStem + ".mkv");
            string sWhy = "";
            string sText = captionsBeside(sWouldBe, out sWhy);
            if (sText.Trim() == "")
            {
                logMessage("The captions were fetched but cannot be found beside where the film would have been. " + sWhy, "ERROR");
                lFailures.Add(sAddress + Environment.NewLine + "    The captions were fetched and then could not be read.");
                return 1;
            }
            bool bRolling = looksAutomatic(sText);
            logMessage("These captions were " + (bRolling ? "made automatically, and roll up the screen, so the repeats are taken out."
                                                          : "written by a person, so they are read as they stand."), "INFO", "");
            lCaptions = bRolling ? joinRolling(readCaptions(sText)) : joinCaptions(readCaptions(sText));
            if (lCaptions.Count == 0)
            {
                logMessage("Captions were fetched but nothing could be read out of them.", "ERROR");
                lFailures.Add(sAddress + Environment.NewLine + "    Captions were fetched but nothing could be read out of them.");
                return 1;
            }
            sTranscriptFrom = "the video's own captions";
            double nRuns = nVideoSeconds;
            if (nRuns <= 0.0) nRuns = lCaptions[lCaptions.Count - 1].nEnd;
            int iSounds = 0;
            int iNamed = 0;
            foreach (Speech oCue in lCaptions)
            {
                if (soundOnly(oCue.sText)) iSounds = iSounds + 1;
                if (oCue.sWho != "") iNamed = iNamed + 1;
            }
            string sPath = Path.Combine(sFolder, sDefaultTranscriptName);
            writeTranscript(lCaptions, sPath, sVideoTitle == "" ? sStem : sVideoTitle, nRuns);
            bTranscribed = true;
            logMessage("Transcript written to " + sPath + ": " + counted(lCaptions.Count, "passage", "passages")
                       + ", " + counted(iNamed, "naming a speaker", "naming a speaker")
                       + ", " + counted(iSounds, "marking a sound rather than speech", "marking a sound rather than speech")
                       + ". The film itself was not downloaded, because nothing in a transcript needs it.",
                       "INFO", "Transcript written from the captions: " + counted(lCaptions.Count, "passage", "passages") + ".");
            lResults.Add((sVideoTitle == "" ? sStem : sVideoTitle) + ": transcript of "
                         + counted(lCaptions.Count, "passage of captions", "passages of captions")
                         + (sCouldNotDescribe == "" ? "" : ", but NOT described: " + sCouldNotDescribe)
                         + Environment.NewLine + "    " + sPath);
            if (sCouldNotDescribe != "")
            {
                logMessage("Nothing was described for this one. The film itself could not be fetched: " + sCouldNotDescribe
                           + ". The words were got from the captions, which come down a different road.", "ERROR");
                announce("Error", -1.0, 1.0, "Transcribed from captions, but the film could not be fetched, so nothing was described.");
            }
            sLastOutputFolder = sFolder;
            writeFileLog(sFolder);
            return 0;
        }

        // ---------- pictures in an archive ----------

        static bool looksLikeArchive(string sPath)
        {
            return string.Compare(Path.GetExtension(sPath), ".zip", true) == 0;
        }

        static bool kindIsIn(string sList, string sKind)
        {
            foreach (string sOne in sList.Split(','))
            {
                if (string.Compare(sOne, sKind, true) == 0) return true;
            }
            return false;
        }

        // What can be done with a picture of this kind, in words fit for a
        // report. Empty means it is not a picture at all.
        static string pictureKind(string sName)
        {
            string sKind = Path.GetExtension(sName).ToLower();
            if (kindIsIn(sDefaultSeenKinds, sKind)) return "seen";
            if (kindIsIn(sDefaultConvertKinds, sKind)) return "converted";
            return "";
        }

        // An identifier turned back into words.
        //
        // Asked for a phrase "fit to be a file name", the model answered in
        // code: OutdoorElderlyLady, Kenyan_ID_2024, Child_Bike_Ride_Sea_View.
        // Not one of twenty-four names held a space. The prompt now says what
        // shape is wanted and shows it, but a model does as it pleases, so
        // whatever comes back is repaired here as well.
        //
        // A word is lowered ONLY when it is a small joining word AND is not
        // already in capitals, so "JamalMazruiAmazonPoster" keeps its names
        // and "WomanInWhiteTee" loses its stray capital: the error it can make
        // is leaving a word capitalised, never destroying a name.
        static string spacedOut(string sName)
        {
            if (sName.Trim().IndexOf(' ') >= 0) return sName;
            string sWork = Regex.Replace(sName, @"[_\-]+", " ").Trim();
            if (sWork.IndexOf(' ') < 0)
            {
                sWork = Regex.Replace(sWork, @"([a-z0-9])([A-Z])", "$1 $2");
                sWork = Regex.Replace(sWork, @"([A-Z]+)([A-Z][a-z])", "$1 $2");
            }
            List<string> lWords = new List<string>();
            foreach (string sOne in sWork.Split(' '))
            {
                if (sOne != "") lWords.Add(sOne);
            }
            if (lWords.Count < 2) return sName;
            StringBuilder oSaid = new StringBuilder();
            int iAt = 0;
            foreach (string sWord in lWords)
            {
                if (oSaid.Length > 0) oSaid.Append(" ");
                bool bSmall = false;
                foreach (string sLittle in sDefaultSmallWords.Split(','))
                {
                    if (string.Compare(sWord, sLittle, true) == 0) bSmall = true;
                }
                if (iAt > 0 && bSmall && sWord != sWord.ToUpper()) oSaid.Append(sWord.ToLower());
                else oSaid.Append(sWord);
                iAt = iAt + 1;
            }
            return oSaid.ToString();
        }

        // The opening of a description, cut where a phrase ends rather than at
        // a word count.
        //
        // Cutting at eleven words gave "a yellow sun low in a pale blue", which
        // stops in the middle of a thing. Trailing joining words are dropped
        // until the last word is one that can end a phrase, and a comma inside
        // the budget is preferred to any of it: a description's first clause is
        // almost always the subject of the picture.
        static string trimToPhrase(string sSaid, int iWords)
        {
            string sCut = trimToWords(sSaid, iWords);
            if (sCut == "") return "";
            // A comma within the budget is a natural stop, if it leaves enough.
            int iComma = sCut.IndexOf(',');
            if (iComma > 0 && sCut.Substring(0, iComma).Split(' ').Length >= 6)
                sCut = sCut.Substring(0, iComma);
            return hangingTrimmed(sCut);
        }

        // A phrase with any trailing joining word taken off, so that it ends
        // somewhere a phrase can end. "...triangular hills against a" becomes
        // "...triangular hills".
        static string hangingTrimmed(string sSaid)
        {
            string[] asHanging = ("a,an,the,of,in,on,at,to,for,from,by,with,and,or,but,as,into,onto,"
                                + "over,under,near,beside,between,through,about,is,are,was,were,that,"
                                + "which,who,while,its,their,his,her,against,behind,above,below,along").Split(',');
            string sCut = sSaid;
            bool bTrimmed = true;
            while (bTrimmed)
            {
                bTrimmed = false;
                string[] asWords = sCut.Split(' ');
                if (asWords.Length < 3) break;
                string sLast = asWords[asWords.Length - 1].Trim(',', ';', ':', '.').ToLower();
                foreach (string sOne in asHanging)
                {
                    if (sLast != sOne) continue;
                    sCut = string.Join(" ", asWords, 0, asWords.Length - 1);
                    bTrimmed = true;
                    break;
                }
            }
            return sCut.TrimEnd(',', ';', ':', '.', ' ');
        }

        // The PROPER NOUNS in a piece of text: the people and places somebody
        // typed, and nothing else.
        //
        // A run of two or more capitalised words is a name -- "Jamal Mazrui",
        // "Lake Tahoe". So is a word in full capitals -- "ORCA", "NIRA". A
        // single capitalised word on its own is not: "Smiling" and "Kenyan"
        // begin sentences and describe things, and treating them as names
        // produces nonsense.
        static List<string> namesFrom(string sText)
        {
            List<string> lFound = new List<string>();
            if (sText == null || sText == "") return lFound;
            // At most three words to a name. Left greedy, "Alamin A Mazrui
            // Kenya National Id" comes back as one long run and reads as
            // nonsense in front of a description. Three covers a first name, a
            // middle name and a surname, or a place like Lake Tahoe.
            //
            // This is a rule of thumb and it will sometimes take a word too
            // many, because a file name is not grammar. The cost is a slightly
            // clumsy name; what it puts there is always something a person
            // actually typed, which is the part worth having.
            foreach (Match oRun in Regex.Matches(sText,
                     @"\b([A-Z][a-z]+(?:\s+[A-Z][a-z]+){1,2}|[A-Z]{2,})\b"))
            {
                string sOne = oRun.Groups[1].Value.Trim();
                // Not a name, however it is capitalised.
                if (Regex.IsMatch(sOne, @"^(JPG|JPEG|PNG|GIF|BMP|TIF|TIFF|WEBP|IMG|DSC|PXL|WA|PDF|ID)$",
                                  RegexOptions.IgnoreCase)) continue;
                bool bHaveIt = false;
                foreach (string sHad in lFound)
                {
                    if (string.Compare(sHad, sOne, true) == 0) bHaveIt = true;
                }
                if (!bHaveIt) lFound.Add(sOne);
            }
            return lFound;
        }

        // A description with the names from the file name put back into it.
        //
        // He asked for exactly this and it is the right rule: the file name is
        // worth mining for the PEOPLE AND PLACES somebody typed, not for its
        // wording. Twenty of forty-nine names in his run were the file name
        // handed back -- "Jamal_Mazrui_signature.jpg" became "Jamal Mazrui
        // Signature", which is tidier and says nothing new.
        //
        // Keeping the whole short name instead gave "Jamal Mazrui Signature,
        // handwritten signature on a white sheet of paper", which says
        // signature twice. Only the names go in front now, and only those the
        // description has not already used.
        // sCalled is the FILE NAME, not the model's answer, and that matters.
        // The model title-cases what it writes, so every word in "Jamal Mazrui
        // Signature" looks like a name and the whole phrase gets taken as one.
        // A file name keeps its natural casing -- "Jamal_Mazrui_signature" has
        // the person capitalised and the noun not -- which is the signal that
        // makes this work at all.
        static string joinNames(string sCalled, string sFuller)
        {
            if (sFuller == "") return sCalled;
            List<string> lWanted = new List<string>();
            foreach (string sName in namesFrom(sCalled))
            {
                if (sFuller.IndexOf(sName, StringComparison.OrdinalIgnoreCase) < 0) lWanted.Add(sName);
            }
            if (lWanted.Count == 0) return sFuller;
            // And the names must not crowd out the picture. Past about thirty
            // characters the front of the name stops being a name and starts
            // being the file name again, which is what all this was to avoid.
            while (lWanted.Count > 1 && string.Join(" and ", lWanted.ToArray()).Length > 30)
                lWanted.RemoveAt(lWanted.Count - 1);
            if (string.Join(" and ", lWanted.ToArray()).Length > 40) return sFuller;
            // The fuller phrase reads better without its opening article once
            // something is put in front of it.
            string sRest = Regex.Replace(sFuller, "^(a|an|the) ", "", RegexOptions.IgnoreCase);
            if (sRest.Length > 0) sRest = char.ToLower(sRest[0]) + sRest.Substring(1);
            return string.Join(" and ", lWanted.ToArray()) + ", " + sRest;
        }

        // An answer that names nothing. "Image", "Photo", "Untitled", or a run
        // of digits. Rentitle refuses the same class of thing among document
        // titles, for the same reason: a folder where every file is called
        // Image is worse than one where none of them is called anything.
        static bool looksLikeNoName(string sName)
        {
            string sBare = Regex.Replace(sName == null ? "" : sName, @"[^A-Za-z0-9 ]", " ").Trim().ToLower();
            sBare = Regex.Replace(sBare, @"\s+", " ");
            if (sBare == "") return true;
            if (Regex.IsMatch(sBare, @"^[\d ]+$")) return true;
            foreach (string sOne in sDefaultNoNames.Split(','))
            {
                if (sBare == sOne) return true;
            }
            // "Image 3", "Photo of something" is fine; "Image 3" alone is not.
            return Regex.IsMatch(sBare, @"^(image|picture|photo|photograph|scan|screenshot|file|img)\s*\d*$");
        }

        // A name a person would be glad to see. Sentence case, spaces kept,
        // and nothing in it that any of Windows, macOS or Linux objects to.
        static string friendlyName(string sSaid)
        {
            string sName = sSaid == null ? "" : sSaid.Trim();
            // The model likes to answer in quotes, or with a lead-in.
            sName = Regex.Replace(sName, "^[\"\u201c\u2018']+|[\"\u201d\u2019']+$", "");
            sName = Regex.Replace(sName, @"^\s*(the |a |an )?(image|picture|photo|photograph)\s+(shows|depicts|is of)\s+",
                                  "", RegexOptions.IgnoreCase);
            // An extension the model added itself. One answer came back as
            // "BananaSmile.png", which would have been written out as
            // BananaSmile.png.jpg.
            sName = Regex.Replace(sName, @"\.(png|jpe?g|gif|webp|bmp|tiff?)$", "", RegexOptions.IgnoreCase);
            sName = spacedOut(sName);
            // SUBSTITUTE, do not delete. Rentitle's rule, and it is right:
            // blanking an ampersand turns "Jeannie & Jim" into "Jeannie Jim"
            // and loses the word. These carry their meaning across instead.
            sName = sName.Replace(":", " - ").Replace(";", " - ").Replace("&", " and ");
            sName = sName.Replace("[", "(").Replace("<", "(").Replace("]", ")").Replace(">", ")");
            sName = sName.Replace("/", " and ").Replace("\\", " ");
            // What is left that Windows, macOS or a zip would object to.
            sName = Regex.Replace(sName, @"[""|?*]", " ");
            sName = Regex.Replace(sName, @"[\x00-\x1f]", " ");
            // And what none of them object to but other programs do.
            foreach (char cBad in sDefaultBadLetters)
            {
                if (sName.IndexOf(cBad) >= 0) sName = sName.Replace(cBad, ' ');
            }
            // Anything outside plain text, which includes emoji, whose two-part
            // encoding has broken every tool that assumed one letter per unit.
            sName = Regex.Replace(sName, @"[^\u0020-\u007e]", " ");
            // Runs of separators left behind by all of that.
            while (sName.IndexOf("--") >= 0) sName = sName.Replace("--", " - ");
            sName = Regex.Replace(sName, @"\s+", " ").Trim();
            sName = Regex.Replace(sName, @"^[\s\-]+|[\s\-]+$", "");
            // Wrapped in parentheses from end to end, they say nothing.
            while (sName.Length > 2 && sName.StartsWith("(") && sName.EndsWith(")"))
                sName = sName.Substring(1, sName.Length - 2).Trim();
            // A leading dot hides the file; a trailing one confuses the
            // extension that is about to be added.
            while (sName.StartsWith(".")) sName = "Dot " + sName.Substring(1).Trim();
            sName = sName.TrimEnd('.', ' ');
            sName = Regex.Replace(sName, @"\s+", " ").Trim();
            if (sName == "") return "Picture";
            // Sentence case: the first letter up, the rest left as the model
            // wrote it, since it holds the proper nouns.
            sName = char.ToUpper(sName[0]) + (sName.Length > 1 ? sName.Substring(1) : "");
            // Cut at a word, never mid-word.
            int iLongest = integer("name-length");
            if (iLongest < 12) iLongest = iDefaultNameLength;
            if (sName.Length > iLongest)
            {
                string sCut = sName.Substring(0, iLongest);
                int iSpace = sCut.LastIndexOf(' ');
                if (iSpace > iLongest / 3) sCut = sCut.Substring(0, iSpace);
                sName = sCut.TrimEnd('.', ',', ';', ':', ' ');
                // And not on a word that cannot end a phrase. Cutting at the
                // last space is not enough: it produced "...triangular hills
                // against a", which stops in the middle of a thing. Wherever
                // the cut lands, back off until the last word can stand there.
                sName = hangingTrimmed(sName);
            }
            // Names Windows keeps for itself, whatever the extension.
            foreach (string sTaken in new string[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3",
                                                     "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
                                                     "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6",
                                                     "LPT7", "LPT8", "LPT9" })
            {
                if (string.Compare(sName, sTaken, true) == 0) sName = sName + " picture";
            }
            return sName == "" ? "Picture" : sName;
        }

        // Number every member of a clashing group, not merely the later ones.
        //
        // Leaving the first bare puts it LAST, because "-" is character 45 and
        // "." is 46, so "Sunset-002.jpg" sorts before "Sunset.jpg". Numbering
        // the whole group keeps alpha order and archive order agreeing, which
        // is the point of padding the number in the first place.
        static bool numberClashes(List<string> lNames)
        {
            Dictionary<string, int> dHowMany = new Dictionary<string, int>();
            foreach (string sName in lNames)
            {
                string sKey = sName.ToLower();
                dHowMany[sKey] = (dHowMany.ContainsKey(sKey) ? dHowMany[sKey] : 0) + 1;
            }
            // As few leading zeros as will do. The width comes from the
            // LARGEST clashing group in this archive: a pair gets -1 and -2, a
            // dozen gets -01 to -12. Not from the number of pictures, because
            // only names that clash ever sit beside each other and only a
            // clashing group has to sort. Not from each group separately
            // either, so that every numbered name in one folder has the same
            // shape rather than -1..-3 sitting beside -01..-12.
            int iBiggest = 0;
            foreach (int iHowMany in dHowMany.Values)
            {
                if (iHowMany > 1 && iHowMany > iBiggest) iBiggest = iHowMany;
            }
            int iWidth = iBiggest.ToString().Length;
            if (iWidth < 1) iWidth = 1;
            Dictionary<string, int> dSoFar = new Dictionary<string, int>();
            for (int iAt = 0; iAt < lNames.Count; iAt = iAt + 1)
            {
                string sKey = lNames[iAt].ToLower();
                if (dHowMany[sKey] < 2) continue;
                int iNext = (dSoFar.ContainsKey(sKey) ? dSoFar[sKey] : 0) + 1;
                dSoFar[sKey] = iNext;
                // Rentitle stops after a thousand rather than going on for
                // ever. Past that the name is given up on and the original
                // kept, which at least cannot collide with itself.
                if (iNext > iDefaultMostClashes) continue;
                lNames[iAt] = lNames[iAt] + "-" + iNext.ToString(new string('0', iWidth));
            }
            return true;
        }

        // One picture, one question. A still is not a moment in a film: there
        // is nothing before it to avoid repeating and no listener waiting for
        // the dialogue to resume, so it gets rules of its own rather than the
        // film ones bent to fit.
        static string[] describeStill(string sImagePath, string sNotes, string sCalled)
        {
            StringBuilder oRules = new StringBuilder();
            oRules.Append("You are describing a single still picture for somebody who cannot see it.\n");
            oRules.Append("Write only what is actually visible. Do not guess at what is outside the frame, ");
            oRules.Append("at what happened before or after, or at what anyone is feeling.\n");
            oRules.Append("Do not begin with \"This image shows\" or \"The picture depicts\". Describe the thing itself.\n");
            oRules.Append("If there is readable text in the picture, quote it, because it is often the whole point.\n");
            // One answer came back as "BezosWithFriends". There was no note in
            // that archive, so the model had nothing to go on and named a
            // living person from a face. The rule belongs here, where it
            // applies whether or not a note was left.
            oRules.Append("Do NOT guess at who anyone is. Unless you have been told below how to recognise a ");
            oRules.Append("particular person, describe people by what you can see and give no names, however ");
            oRules.Append("familiar a face looks.\n");
            if (sCalled != "")
            {
                // A name written by a person is a statement; a face is only a
                // resemblance. So this loosens nothing above: it adds a source
                // that is allowed where a face is not.
                oRules.Append("\nWhoever kept this picture called it: \"" + sCalled + "\"\n");
                List<string> lWho = namesFrom(sCalled);
                if (lWho.Count > 0)
                    oRules.Append("The names in it are: " + string.Join(", ", lWho.ToArray())
                                  + ". Use those in your caption where they fit what you can see.\n");
                oRules.Append("They were there and you were not, so any people or places named there are right. ");
                oRules.Append("Use them where what you can see fits — if it names two people and you can see two ");
                oRules.Append("people, name them; if it names a lake and you can see water, say the lake. ");
                oRules.Append("Where it does not fit what is in front of you, ignore it and say nothing about it.\n");
            }
            if (sNotes != "")
            {
                // The note is a way of RECOGNISING people, not a claim that any
                // of them is here. Given a note saying who tends to appear, a
                // model will use those names whether or not the people are in
                // the frame, which is worse than no names at all: a reader
                // cannot tell a real identification from a guess.
                oRules.Append("\nSomebody who knows these pictures has left this note about them:\n");
                oRules.Append(sNotes + "\n");
                oRules.Append("Use the note ONLY to recognise what is actually in front of you. ");
                oRules.Append("If somebody in this picture matches how the note describes them, use that name. ");
                oRules.Append("If you cannot tell, say what you can see about them and do NOT guess at a name. ");
                oRules.Append("The note says who MAY appear across a set of pictures; it does not say who is in THIS one.\n");
            }
            oRules.Append("\nAnswer with JSON and nothing else, in this exact shape:\n");
            oRules.Append("{\"name\": \"...\", \"description\": \"...\"}\n\n");
            int iRoom = integer("name-length");
            if (iRoom < 12) iRoom = iDefaultNameLength;
            // The shape is SHOWN, not merely described. Told to write something
            // "fit to be a file name", the model heard "identifier" and
            // answered OutdoorElderlyLady and Kenyan_ID_2024 -- not one of
            // twenty-four answers held a space. Examples are what fix that.
            oRules.Append("name: a short caption saying what this picture is, in ordinary words.\n");
            oRules.Append("  * Write it as a PHRASE with SPACES between the words, like a caption.\n");
            oRules.Append("  * Sentence case: a capital on the first word and on names, lower case elsewhere.\n");
            oRules.Append("  * NOT runTogetherLikeThis. NOT with_underscores. No file extension on the end.\n");
            // A NUMBER OF WORDS, not a ceiling in characters. Told it had 79
            // characters to play with, the model used 21 to 25 of them: a
            // ceiling is permission to stop, not a target. Asked for eight to
            // fourteen words, it has something to aim at.
            oRules.Append("  * EIGHT TO FOURTEEN WORDS. Three or four words is too short — it will not tell this ");
            oRules.Append("picture apart from a similar one. Say who or what, and where or doing what.\n");
            oRules.Append("  * At most " + iRoom.ToString() + " characters. No punctuation except commas and apostrophes.\n");
            oRules.Append("  * Make it DISTINCTIVE: say what marks this picture out from a similar one, so that ");
            oRules.Append("two pictures do not end up with the same name.\n");
            oRules.Append("  * Do NOT simply give back the name the file already has. Take the PEOPLE AND PLACES ");
            oRules.Append("it mentions and use those, but say what is VISIBLE around them. A file called ");
            oRules.Append("\"Jamal_Mazrui_signature.jpg\" wants a name like \"Jamal Mazrui's signature in blue ink on ");
            oRules.Append("a plain white sheet\", not \"Jamal Mazrui signature\".\n");
            oRules.Append("  Good: \"Elderly woman in a blue floral dress beside a dry stone wall\"\n");
            oRules.Append("  Good: \"Kenyan national identity card issued in Mombasa in 2024\"\n");
            oRules.Append("  Good: \"Three children on bicycles on a seafront path at low tide\"\n");
            oRules.Append("  Too short: \"Elderly woman outdoors\"   Too short: \"Kenyan ID card\"\n");
            oRules.Append("  Bad: \"OutdoorElderlyLady\" (run together)   Bad: \"Kenyan_ID_2024\" (underscores)\n");
            oRules.Append("description: two or three sentences describing the picture fully.");

            Dictionary<string, object> dOptions = new Dictionary<string, object>();
            dOptions["temperature"] = 0.4;
            // Enough room to close its own braces. At 500 one answer stopped
            // mid-word and the JSON could not be parsed.
            dOptions["num_predict"] = 900;
            dOptions["top_p"] = 0.9;
            Dictionary<string, object> dPayload = new Dictionary<string, object>();
            dPayload["model"] = text("model");
            dPayload["prompt"] = oRules.ToString();
            dPayload["images"] = new string[] { Convert.ToBase64String(File.ReadAllBytes(sImagePath)) };
            dPayload["stream"] = false;
            dPayload["keep_alive"] = "30m";
            dPayload["format"] = "json";
            dPayload["options"] = dOptions;
            JavaScriptSerializer oSerializer = new JavaScriptSerializer();
            oSerializer.MaxJsonLength = int.MaxValue;
            string sAnswer = postJsonPumping(text("url") + "/api/generate", oSerializer.Serialize(dPayload));
            if (sAnswer == "") return new string[] { "", "" };
            string sSaid = "";
            try
            {
                Dictionary<string, object> dReply = oSerializer.Deserialize<Dictionary<string, object>>(sAnswer);
                if (dReply.ContainsKey("response")) sSaid = Convert.ToString(dReply["response"]).Trim();
            }
            catch (Exception oError)
            {
                logMessage("The model's answer could not be read: " + oError.Message, "ERROR");
                return new string[] { "", "" };
            }
            if (sSaid == "") return new string[] { "", "" };
            string sName = "";
            string sAbout = "";
            try
            {
                Dictionary<string, object> dSaid = oSerializer.Deserialize<Dictionary<string, object>>(sSaid);
                if (dSaid.ContainsKey("name")) sName = Convert.ToString(dSaid["name"]).Trim();
                if (dSaid.ContainsKey("description")) sAbout = Convert.ToString(dSaid["description"]).Trim();
            }
            catch (Exception)
            {
                // Usually not prose at all, but JSON that ran out of room and
                // stopped mid-word, so it will not parse. One answer reached
                // the report as the name
                //   { name Winter Couple at a Snowy Forest , description A man
                // which is the raw wreckage with its punctuation stripped. The
                // two fields are pulled out by pattern instead.
                Match oName = Regex.Match(sSaid, "\"name\"\\s*:\\s*\"([^\"]*)\"");
                Match oAbout = Regex.Match(sSaid, "\"description\"\\s*:\\s*\"([^\"]*)");
                if (oName.Success) sName = oName.Groups[1].Value.Trim();
                if (oAbout.Success) sAbout = oAbout.Groups[1].Value.Trim();
                if (sName == "" && sAbout == "")
                {
                    logMessage("  The model did not answer in the shape asked for; using what it said as the description.", "INFO", "");
                    sAbout = sSaid;
                }
                else
                {
                    logMessage("  The model's answer was cut short; the name and description were recovered from it.", "INFO", "");
                }
            }
            if (sAbout == "") sAbout = sSaid;
            sAbout = tidyText(sAbout);
            // A name carrying the shape of the answer rather than its content.
            if (sName.IndexOf('{') >= 0 || sName.IndexOf("description", StringComparison.OrdinalIgnoreCase) >= 0
                || sName.IndexOf("\"name\"", StringComparison.OrdinalIgnoreCase) >= 0) sName = "";
            // Or naming nothing at all.
            if (looksLikeNoName(sName)) sName = "";
            // Or too short to tell one picture from another. The description is
            // always specific and always long enough, so it is the better
            // source when the model has been terse. Asked for eight to fourteen
            // words it often gives four, and four words name a category rather
            // than a picture.
            if (sName != "" && sName.Split(' ').Length < 5)
            {
                string sFuller = trimToPhrase(sAbout, 12);
                if (sFuller.Split(' ').Length > sName.Split(' ').Length && !looksLikeNoName(sFuller))
                {
                    logMessage("  The name it gave was only " + counted(sName.Split(' ').Length, "word", "words")
                               + " (\"" + sName + "\"); adding what the picture shows.", "INFO", "");
                    sName = joinNames(sCalled, sFuller);
                }
            }
            if (sName == "") sName = trimToPhrase(sAbout, 12);
            if (looksLikeNoName(sName)) sName = trimToPhrase(sAbout, 14);
            return new string[] { friendlyName(sName), sAbout };
        }

        // What the file name says, once the camera's part is taken out.
        //
        // "Jeannie & Jim - Lake Tahoe.jpg" was named by somebody who was
        // there, and that is better evidence than a model can get from the
        // picture. "IMG-20230113-WA0000.jpg" says nothing, and offering it as
        // context only invites the model to invent a meaning for it.
        static string nameAsHint(string sBare)
        {
            string sSaid = Path.GetFileNameWithoutExtension(sBare);
            // SEPARATORS FIRST, before any pattern looks at the name. "_" is a
            // word character, so \b never falls either side of one: in
            // "IMG_20240115_WA0042" the camera pattern could not match across
            // the underscores and the whole thing was offered to the model as
            // context, and in "signature_proper_orientation" the housekeeping
            // words could not match either. Both were the same fault, found a
            // week apart, because this line was in the wrong place twice.
            sSaid = Regex.Replace(sSaid, @"[_\-]+", " ");
            // What a camera, a phone or a chat program wrote.
            sSaid = Regex.Replace(sSaid, @"\b(IMG|DSC|DSCN|DCIM|PXL|MVIMG|WA|SAM|PICT|PANO|Screenshot|photo)[\s_\-]?\d+\b",
                                  " ", RegexOptions.IgnoreCase);
            sSaid = Regex.Replace(sSaid, @"\b\d{6,}\b", " ");
            sSaid = Regex.Replace(sSaid, @"\b(copy|final|edited|cropped|resized|small|large|orig|original|"
                                        + @"proper|orientation|version|new|old)\b", " ", RegexOptions.IgnoreCase);
            sSaid = Regex.Replace(sSaid, @"\(\s*\d+\s*\)", " ");
            sSaid = Regex.Replace(sSaid, @"\s+", " ").Trim();
            sSaid = sSaid.Trim(' ', '.', ',', '&', '-');
            // A word or two of nothing is not context. Neither is a number.
            if (sSaid.Length < 6) return "";
            if (Regex.IsMatch(sSaid, @"^[\d\s]+$")) return "";
            if (sSaid.Split(' ').Length < 2) return "";
            return sSaid;
        }

        // A note left in the archive. Kept short on purpose: it goes into every
        // request it applies to, and a long one crowds out the picture.
        static string noteBeside(string sPath)
        {
            if (!File.Exists(sPath)) return "";
            try
            {
                string sSaid = Regex.Replace(File.ReadAllText(sPath), @"\s+", " ").Trim();
                // A heading marker is for a reader, not for the model.
                sSaid = Regex.Replace(sSaid, @"(^|\s)#+\s*", " ").Trim();
                if (sSaid.Length > 1500) sSaid = sSaid.Substring(0, 1500).TrimEnd() + " ...";
                return sSaid;
            }
            catch (Exception oError)
            {
                logMessage("  The note " + Path.GetFileName(sPath) + " could not be read: " + oError.Message, "INFO", "");
                return "";
            }
        }

        // A version turned into something that sorts correctly.
        //
        // "13.11" is LATER than "13.8" -- ExifTool numbers its releases that
        // way -- but as decimals 13.8 is the larger and the older copy would
        // be chosen. Each part is a whole number, so the second is scaled
        // rather than treated as a fraction.
        static double versionRank(string sVersion)
        {
            Match oParts = Regex.Match(sVersion.Trim(), @"^(\d+)(?:\.(\d+))?");
            if (!oParts.Success) return -1.0;
            double nMajor = 0.0;
            double nMinor = 0.0;
            double.TryParse(oParts.Groups[1].Value, System.Globalization.NumberStyles.Integer,
                            System.Globalization.CultureInfo.InvariantCulture, out nMajor);
            if (oParts.Groups[2].Success)
                double.TryParse(oParts.Groups[2].Value, System.Globalization.NumberStyles.Integer,
                                System.Globalization.CultureInfo.InvariantCulture, out nMinor);
            return nMajor * 1000.0 + nMinor;
        }

        // Every ExifTool on this machine, run, and the newest chosen.
        //
        // He installed one with winget while an older one sat in the build
        // folder, and asked which would be used. Guessing at where winget puts
        // things is how that question gets answered wrongly, so instead every
        // likely place is tried, each candidate is RUN to learn its version,
        // and all of them are logged with the choice and the reason. A log
        // then answers the question without anybody going to look.
        //
        // The places cover winget's two habits -- a real install under Program
        // Files or the user's Programs folder, and a shim under WinGet\Links --
        // as well as the package folder it unpacks into, HomerScribe's own
        // folder, and whatever is on the PATH.
        static string exifToolProgram()
        {
            List<string> lWhere = new List<string>();
            lWhere.Add(Path.Combine(exeFolder(), "exiftool.exe"));
            lWhere.Add(Path.Combine(appDataFolder(), "exiftool", "exiftool.exe"));
            lWhere.Add(@"C:\HomerScribe\exiftool.exe");
            foreach (string sRoot in new string[] {
                Environment.GetEnvironmentVariable("ProgramFiles"),
                Environment.GetEnvironmentVariable("ProgramFiles(x86)"),
                Environment.GetEnvironmentVariable("ProgramW6432") })
            {
                if (sRoot != null && sRoot != "") lWhere.Add(Path.Combine(sRoot, "ExifTool", "exiftool.exe"));
            }
            string sLocal = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            if (sLocal != null && sLocal != "")
            {
                lWhere.Add(Path.Combine(sLocal, "Programs", "ExifTool", "exiftool.exe"));
                // winget's shim folder, which is on the PATH of a shell opened
                // after the install but not of one opened before it.
                lWhere.Add(Path.Combine(sLocal, "Microsoft", "WinGet", "Links", "exiftool.exe"));
                string sPackages = Path.Combine(sLocal, "Microsoft", "WinGet", "Packages");
                try
                {
                    if (Directory.Exists(sPackages))
                    {
                        foreach (string sOne in Directory.GetDirectories(sPackages, "*ExifTool*"))
                        {
                            foreach (string sExe in Directory.GetFiles(sOne, "exiftool.exe", SearchOption.AllDirectories))
                                lWhere.Add(sExe);
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
            string sOnPath = findTool("exiftool");
            if (sOnPath != "") lWhere.Add(sOnPath);

            string sBest = "";
            string sBestVersion = "";
            double nBest = -1.0;
            List<string> lSeen = new List<string>();
            foreach (string sOne in lWhere)
            {
                if (sOne == null || sOne == "" || !File.Exists(sOne)) continue;
                bool bAlready = false;
                foreach (string sHad in lSeen)
                {
                    if (string.Compare(sHad, sOne, true) == 0) bAlready = true;
                }
                if (bAlready) continue;
                lSeen.Add(sOne);
                // A single file, with nothing beside it. His requirement, and
                // it disqualifies every current package: they are a small
                // launcher plus an "exiftool_files" folder holding Perl. An
                // older single-file copy loses nothing, because HomerScribe
                // supplies the accessibility definitions itself.
                string sBeside = Path.Combine(Path.GetDirectoryName(sOne), "exiftool_files");
                if (Directory.Exists(sBeside))
                {
                    logMessage("  " + sOne + " -- passed over: it needs an exiftool_files folder beside it", "INFO", "");
                    continue;
                }
                string sOut = "";
                string sErr = "";
                int iCode = runCommand(sOne, "-ver", out sOut, out sErr);
                string sVersion = (sOut + sErr).Trim();
                if (iCode != 0 || sVersion == "" || !Regex.IsMatch(sVersion, @"^\d+(\.\d+)?"))
                {
                    logMessage("  " + sOne + " -- will not run", "INFO", "");
                    continue;
                }
                // NOT as a decimal. ExifTool released 13.11 after 13.8, so as
                // numbers 13.8 looks the newer of the two and the older copy
                // would win. Each part is compared as a whole number instead.
                double nVersion = versionRank(sVersion);
                logMessage("  " + sOne + " -- version " + sVersion, "INFO", "");
                if (nVersion > nBest)
                {
                    nBest = nVersion;
                    sBest = sOne;
                    sBestVersion = sVersion;
                }
            }
            if (lSeen.Count == 0)
            {
                logMessage("No single-file ExifTool was found, so the descriptions cannot be written into the "
                           + "pictures. HomerScribe wants one binary with no exiftool_files folder beside it, and "
                           + "nothing currently published is in that form. Put a self-contained exiftool.exe beside "
                           + "HomerScribe.exe and it will be used. The pictures are still described and still "
                           + "renamed either way.",
                           "ERROR", "No single-file ExifTool found; the descriptions are not written into the pictures.");
                return "";
            }
            logMessage("Chosen: " + sBest + ", version " + sBestVersion
                       + (lSeen.Count > 1 ? ", the newest of " + lSeen.Count.ToString() + " found." : "."),
                       "INFO", "");
            return sBest;
        }

        // Where a description can live inside a picture, by format.
        //
        //   "full"    -- XMP, IPTC and EXIF all writable.
        //   "comment" -- a plain comment block and nothing named. GIF.
        //   "none"    -- nowhere at all. BMP has no metadata container.
        //   "vector"  -- SVG: the model cannot see it, so there is nothing to
        //                write. Its own <title> and <desc> would be the best
        //                home of any format, which is worth coming back for.
        static string metadataKindOf(string sName)
        {
            string sKind = Path.GetExtension(sName).ToLower();
            if (sKind == ".svg" || sKind == ".svgz") return "vector";
            if (kindIsIn(sDefaultMetadataKinds, sKind)) return "full";
            if (kindIsIn(sDefaultPartMetadataKinds, sKind)) return "comment";
            return "none";
        }

        // Teaching ExifTool the two accessibility properties.
        //
        // A copy older than October 2021 does not know them by name, and my
        // first conclusion -- that it therefore could not write them -- was
        // wrong. ExifTool has been able to write tags it does not know for far
        // longer than these tags have existed, given their definition. Its own
        // documentation says any namespace may be written by giving a family 1
        // group name, "including namespaces which are not pre-defined by
        // ExifTool".
        //
        // So this is that definition: the Iptc4xmpCore namespace, its URI as
        // the IPTC publishes it, and the two properties as lang-alt, which is
        // what the standard calls for and what a current ExifTool writes. The
        // bytes that land in the file are the same either way.
        static string writeAltConfig(string sWorkDir)
        {
            string sPath = Path.Combine(sWorkDir, "accessibility.config");
            StringBuilder oSaid = new StringBuilder();
            oSaid.Append("# Written by HomerScribe. Defines the two IPTC accessibility properties\n");
            oSaid.Append("# for a copy of ExifTool from before October 2021, which does not carry\n");
            oSaid.Append("# them. Delete freely: it is written afresh whenever it is needed.\n");
            oSaid.Append("%Image::ExifTool::UserDefined = (\n");
            oSaid.Append("    'Image::ExifTool::XMP::Main' => {\n");
            oSaid.Append("        Iptc4xmpCore => {\n");
            oSaid.Append("            SubDirectory => {\n");
            oSaid.Append("                TagTable => 'Image::ExifTool::UserDefined::Iptc4xmpCore',\n");
            oSaid.Append("            },\n");
            oSaid.Append("        },\n");
            oSaid.Append("    },\n");
            oSaid.Append(");\n");
            oSaid.Append("%Image::ExifTool::UserDefined::Iptc4xmpCore = (\n");
            oSaid.Append("    GROUPS    => { 0 => 'XMP', 1 => 'XMP-iptcCore', 2 => 'Image' },\n");
            oSaid.Append("    NAMESPACE => { 'Iptc4xmpCore' => 'http://iptc.org/std/Iptc4xmpCore/1.0/xmlns/' },\n");
            oSaid.Append("    WRITABLE  => 'string',\n");
            oSaid.Append("    AltTextAccessibility  => { Writable => 'lang-alt' },\n");
            oSaid.Append("    ExtDescrAccessibility => { Writable => 'lang-alt' },\n");
            oSaid.Append(");\n");
            oSaid.Append("1;  #end\n");
            try
            {
                StreamWriter fConfig = new StreamWriter(sPath, false, new UTF8Encoding(false));
                fConfig.Write(oSaid.ToString());
                fConfig.Close();
            }
            catch (Exception oError)
            {
                logMessage("The accessibility definitions could not be written: " + oError.Message, "ERROR");
                return "";
            }
            return sPath;
        }

        // The description, written into the picture itself.
        //
        // The same words go into several places on purpose, because different
        // software looks in different ones and a description nobody finds is
        // no use:
        //
        //   XMP-iptcCore:AltTextAccessibility  the short phrase. The IPTC
        //     added this in 2021 for exactly this job; it is what a web page
        //     should use as alt text, and it is capped at 250 characters.
        //   XMP-iptcCore:ExtDescrAccessibility the full description, uncapped.
        //     Its companion, for when alt text is not enough.
        //   XMP-dc:Description, IPTC:Caption-Abstract   what most photo
        //     software actually displays.
        //   EXIF:XPTitle, EXIF:XPComment       what Windows Explorer shows and
        //     what a screen reader reads out of the properties. For somebody
        //     working at a Windows machine this is the one that matters.
        //
        // Arguments go in a file rather than on the command line: a
        // description holds quotation marks, ampersands and accented letters,
        // and every one of those is a way for a command line to go wrong.
        static bool writeMetadata(string sExifTool, string sPath, string sShort, string sLong, string sKind)
        {
            if (sExifTool == "" || sKind == "none" || sKind == "vector") return false;
            string sArgsPath = sPath + ".exifargs.txt";
            List<string> lArgs = new List<string>();
            lArgs.Add("-overwrite_original");
            // Ignore minor errors. One PNG was refused for "IFD0 pointer
            // references previous IFD0 directory" -- damage already in the
            // file and nothing to do with what is being added. Without this,
            // a picture that is slightly wrong gets no description at all.
            lArgs.Add("-m");
            lArgs.Add("-charset");
            lArgs.Add("UTF8");
            lArgs.Add("-charset");
            lArgs.Add("filename=UTF8");
            // The short one is capped by the standard, not by choice.
            string sAlt = sShort.Length > 250 ? sShort.Substring(0, 250) : sShort;
            if (sKind == "comment")
            {
                // All a GIF has.
                lArgs.Add("-Comment=" + sLong);
            }
            else
            {
                lArgs.Add("-codedcharacterset=utf8");
                // Written unless ExifTool neither knows them nor has been
                // taught them.
                if (!bNoAltTags || bTeachAltTags)
                {
                    lArgs.Add("-XMP-iptcCore:AltTextAccessibility=" + sAlt);
                    lArgs.Add("-XMP-iptcCore:ExtDescrAccessibility=" + sLong);
                }
                lArgs.Add("-XMP-dc:Description=" + sLong);
                lArgs.Add("-XMP-dc:Title=" + sAlt);
                lArgs.Add("-IPTC:Caption-Abstract=" + sLong);
                lArgs.Add("-IPTC:ObjectName=" + sAlt);
                lArgs.Add("-EXIF:ImageDescription=" + sAlt);
                lArgs.Add("-EXIF:XPTitle=" + sAlt);
                lArgs.Add("-EXIF:XPComment=" + sLong);
                lArgs.Add("-XMP:Software=HomerScribe");
            }
            lArgs.Add(sPath);
            try
            {
                // No byte order mark: ExifTool reads an argument file as plain
                // UTF-8 and three stray bytes would become part of the first
                // argument.
                StreamWriter fArgs = new StreamWriter(sArgsPath, false, new UTF8Encoding(false));
                foreach (string sOne in lArgs) fArgs.WriteLine(sOne.Replace("\r", " ").Replace("\n", " "));
                fArgs.Close();
            }
            catch (Exception oError)
            {
                logMessage("  The metadata could not be prepared: " + oError.Message, "ERROR");
                return false;
            }
            string sOut = "";
            string sErr = "";
            // -config must come FIRST, before anything else on the line.
            string sBefore = bTeachAltTags && sAltConfigPath != "" ? "-config " + quoted(sAltConfigPath) + " " : "";
            int iCode = runCommand(sExifTool, sBefore + "-@ " + quoted(sArgsPath), out sOut, out sErr);
            try
            {
                File.Delete(sArgsPath);
            }
            catch (Exception)
            {
            }
            string sSaid = sOut + sErr;
            // A warning does not set the exit code, so this is checked by
            // name. The self test has normally settled it already.
            if (sSaid.IndexOf("is not defined", StringComparison.OrdinalIgnoreCase) >= 0)
                logMessage("  ExifTool did not recognise a field: " + tail(sSaid, 200), "INFO", "");
            if (iCode != 0)
            {
                // "Writing of WEBP files is not yet supported" is not a fault
                // and not a refusal: it is this copy of ExifTool saying that
                // this format has nowhere to put a description, which is
                // exactly what BMP says about itself. Which formats can be
                // written varies by version -- WebP writing came long after
                // 11.79 -- so the tool is asked rather than a list kept here.
                Match oNoRoom = Regex.Match(sSaid, @"Writing of (\w+) files is not yet supported",
                                            RegexOptions.IgnoreCase);
                if (oNoRoom.Success)
                {
                    string sKindNow = Path.GetExtension(sPath).ToLower();
                    if (!lNoRoomKinds.Contains(sKindNow)) lNoRoomKinds.Add(sKindNow);
                    logMessage("  This ExifTool cannot write " + oNoRoom.Groups[1].Value.ToUpper()
                               + " files, so " + Path.GetFileName(sPath) + " keeps its description in "
                               + sDefaultPicturesName + " only. A newer ExifTool may be able to.", "INFO", "");
                    return false;
                }
                logMessage("  ExifTool would not write to " + Path.GetFileName(sPath) + ": " + tail(sSaid, 200), "ERROR");
                return false;
            }
            return true;
        }

        // A tag name with its punctuation taken out, for comparing one against
        // another. "Caption-Abstract" and "CaptionAbstract" are the same tag.
        static string plainName(string sName)
        {
            return Regex.Replace(sName == null ? "" : sName, "[^A-Za-z0-9]", "").ToLower();
        }

        // What ExifTool actually does with one picture, before doing it to
        // all of them.
        //
        // Written because I kept asking him to check things by hand. A run
        // should answer its own questions. This copies the first picture aside,
        // writes every field into the copy, reads all of them back, and logs
        // each one BY NAME as found or missing -- so one log says which
        // ExifTool is in use, which fields land, which do not, and what was
        // run to find out.
        //
        // It also decides how the rest of the run will write: plainly, or with
        // the accessibility definitions supplied.
        static bool selfTestMetadata(string sExifTool, string sPicture, string sWorkDir)
        {
            if (sExifTool == "") return false;
            string sTry = Path.Combine(sWorkDir, "selftest" + Path.GetExtension(sPicture));
            try
            {
                File.Copy(sPicture, sTry, true);
            }
            catch (Exception oError)
            {
                logMessage("The self test could not copy a picture: " + oError.Message, "INFO", "");
                return false;
            }
            logMessage("Checking what this ExifTool will write, using a copy of "
                       + Path.GetFileName(sPicture) + ".", "INFO", "");
            string[] asFields = new string[] {
                "XMP-iptcCore:AltTextAccessibility", "XMP-iptcCore:ExtDescrAccessibility",
                "XMP-dc:Description", "XMP-dc:Title", "IPTC:Caption-Abstract", "IPTC:ObjectName",
                "EXIF:ImageDescription", "EXIF:XPTitle", "EXIF:XPComment" };
            for (int iGo = 0; iGo < 2; iGo = iGo + 1)
            {
                // First as it stands; then, if the accessibility fields did not
                // land, again with the definitions supplied.
                writeMetadata(sExifTool, sTry, "HomerScribe self test", "HomerScribe self test description.", "full");
                string sOut = "";
                string sErr = "";
                StringBuilder oAsk = new StringBuilder();
                foreach (string sField in asFields) oAsk.Append(" -" + sField);
                string sBefore = bTeachAltTags && sAltConfigPath != "" ? "-config " + quoted(sAltConfigPath) + " " : "";
                runCommand(sExifTool, sBefore + "-s -f -q -m -charset UTF8" + oAsk.ToString() + " " + quoted(sTry),
                           out sOut, out sErr);
                bool bAlt = false;
                foreach (string sField in asFields)
                {
                    string sBare = sField.Substring(sField.IndexOf(':') + 1);
                    // The tag name is matched with the punctuation taken out of
                    // BOTH sides. ExifTool's -s prints the tag's own name, and
                    // IPTC's is "Caption-Abstract" -- hyphen and all. The first
                    // version of this stripped the hyphen from what it looked
                    // for but not from what ExifTool printed, so that one field
                    // was reported missing on every run while being written
                    // perfectly well.
                    string sValue = "";
                    foreach (string sLine in sOut.Replace("\r\n", "\n").Split('\n'))
                    {
                        int iColon = sLine.IndexOf(':');
                        if (iColon <= 0) continue;
                        if (plainName(sLine.Substring(0, iColon)) != plainName(sBare)) continue;
                        sValue = sLine.Substring(iColon + 1).Trim();
                        break;
                    }
                    // "-f" makes ExifTool print a dash for a field it has not got.
                    bool bThere = sValue != "" && sValue != "-";
                    if (sBare.IndexOf("Accessibility", StringComparison.OrdinalIgnoreCase) >= 0 && bThere) bAlt = true;
                    logMessage("    " + sField + ": " + (bThere ? "written" : "NOT written"), "INFO", "");
                }
                if (bAlt)
                {
                    logMessage("  The accessibility fields are being written"
                               + (bTeachAltTags ? ", using the definitions HomerScribe supplies." : "."), "INFO", "");
                    break;
                }
                if (iGo == 0)
                {
                    // Not a reason to give up. ExifTool can be told what they
                    // are, and it has been able to for far longer than the
                    // fields have existed.
                    bNoAltTags = true;
                    sAltConfigPath = writeAltConfig(sWorkDir);
                    bTeachAltTags = sAltConfigPath != "";
                    logMessage("  This ExifTool does not know the IPTC accessibility fields by name -- they were "
                               + "added to the standard in October 2021. Supplying their definitions and trying "
                               + "again.", "INFO", "");
                    if (!bTeachAltTags) break;
                }
                else
                {
                    bTeachAltTags = false;
                    logMessage("  The accessibility fields still will not write, even with their definitions "
                               + "supplied. The description still goes into the caption, title and comment "
                               + "fields, which is where Windows Explorer and most photo software look.",
                               "ERROR", "");
                }
            }
            try
            {
                File.Delete(sTry);
            }
            catch (Exception)
            {
            }
            return true;
        }

        // A value from ExifTool's JSON, whatever shape it arrived in.
        static string valueAsText(object oValue)
        {
            if (oValue == null) return "";
            object[] aMany = oValue as object[];
            if (aMany != null)
            {
                List<string> lEach = new List<string>();
                foreach (object oOne in aMany)
                {
                    string sOne = valueAsText(oOne);
                    if (sOne != "") lEach.Add(sOne);
                }
                return string.Join(", ", lEach.ToArray());
            }
            Dictionary<string, object> dOne = oValue as Dictionary<string, object>;
            if (dOne != null)
            {
                List<string> lEach = new List<string>();
                foreach (KeyValuePair<string, object> oPair in dOne)
                {
                    string sOne = valueAsText(oPair.Value);
                    if (sOne != "") lEach.Add(oPair.Key + ": " + sOne);
                }
                return string.Join("; ", lEach.ToArray());
            }
            return Regex.Replace(Convert.ToString(oValue), @"\s+", " ").Trim();
        }

        // Everything each picture in a folder now says about itself.
        //
        // Read back out of the finished files rather than assembled from what
        // was sent to them, for the same reason the counts are: what was sent
        // is a hope, what reads back is the fact. Anything already in the file
        // is listed too -- the camera, the date, somebody else's caption --
        // because the question a reader has is what this file says about
        // itself, not what this program did to it.
        static bool writeDescribedPage(string sExifTool, string sFolder, string sPagePath,
                                       string sTitle, string sIntro)
        {
            StreamWriter fDoc = null;
            try
            {
                fDoc = new StreamWriter(sPagePath, false, new UTF8Encoding(true));
                fDoc.WriteLine("# " + sTitle);
                fDoc.WriteLine("");
                fDoc.WriteLine(sIntro);
                fDoc.WriteLine("");
                if (sExifTool == "")
                {
                    fDoc.WriteLine("ExifTool was not available, so nothing could be read back out of these files.");
                    fDoc.Close();
                    return false;
                }
                string sOut = "";
                string sErr = "";
                // -G1 gives the group each field belongs to, which is what
                // makes a bare name like "Description" tell you where it lives.
                string sBefore = bTeachAltTags && sAltConfigPath != "" ? "-config " + quoted(sAltConfigPath) + " " : "";
                runCommand(sExifTool, sBefore + "-j -G1 -m -charset UTF8 -charset filename=UTF8 " + quoted(sFolder),
                           out sOut, out sErr);
                if (sOut.Trim() == "")
                {
                    fDoc.WriteLine("Nothing could be read back out of these files.");
                    logMessage("Nothing came back when reading the pictures for " + Path.GetFileName(sPagePath)
                               + ": " + tail(sErr, 160), "ERROR");
                    fDoc.Close();
                    return false;
                }
                JavaScriptSerializer oSerializer = new JavaScriptSerializer();
                oSerializer.MaxJsonLength = int.MaxValue;
                object[] aFiles = oSerializer.Deserialize<object[]>(sOut);
                // In the order they read, which is the order they sit in the
                // folder, so the document and a directory listing agree.
                List<string> lNamesHere = new List<string>();
                Dictionary<string, Dictionary<string, object>> dByName
                    = new Dictionary<string, Dictionary<string, object>>();
                foreach (object oItem in aFiles)
                {
                    Dictionary<string, object> dOne = toMap(oItem);
                    string sWhich = dOne.ContainsKey("SourceFile") ? Convert.ToString(dOne["SourceFile"]) : "";
                    sWhich = sWhich == "" ? "(unnamed)" : Path.GetFileName(sWhich.Replace('/', '\\'));
                    // The document itself is in the folder by now; it is not a
                    // picture and has nothing to say here.
                    if (string.Compare(sWhich, Path.GetFileName(sPagePath), true) == 0) continue;
                    if (!dByName.ContainsKey(sWhich))
                    {
                        lNamesHere.Add(sWhich);
                        dByName[sWhich] = dOne;
                    }
                }
                lNamesHere.Sort(delegate(string sLeft, string sRight)
                { return string.Compare(sLeft, sRight, StringComparison.OrdinalIgnoreCase); });
                foreach (string sWhich in lNamesHere)
                {
                    fDoc.WriteLine("## " + sWhich);
                    fDoc.WriteLine("");
                    // Sorted by field name, ignoring case, as he asked.
                    List<string> lFields = new List<string>();
                    foreach (KeyValuePair<string, object> oPair in dByName[sWhich])
                    {
                        if (oPair.Key == "SourceFile") continue;
                        // The disk's business, not the picture's: dates,
                        // permissions, the folder it happens to sit in. They
                        // change every time the file is copied.
                        if (oPair.Key.StartsWith("System:") || oPair.Key.StartsWith("ExifTool:")) continue;
                        if (valueAsText(oPair.Value) == "") continue;
                        lFields.Add(oPair.Key);
                    }
                    lFields.Sort(delegate(string sLeft, string sRight)
                    {
                        string sBareLeft = sLeft.Substring(sLeft.IndexOf(':') + 1);
                        string sBareRight = sRight.Substring(sRight.IndexOf(':') + 1);
                        int iSame = string.Compare(sBareLeft, sBareRight, StringComparison.OrdinalIgnoreCase);
                        if (iSame != 0) return iSame;
                        return string.Compare(sLeft, sRight, StringComparison.OrdinalIgnoreCase);
                    });
                    if (lFields.Count == 0) fDoc.WriteLine("- This file carries no metadata at all.");
                    foreach (string sField in lFields)
                    {
                        string sBare = sField.Substring(sField.IndexOf(':') + 1);
                        string sGroup = sField.IndexOf(':') > 0 ? sField.Substring(0, sField.IndexOf(':')) : "";
                        fDoc.WriteLine("- **" + sBare + "**" + (sGroup == "" ? "" : " (" + sGroup + ")")
                                       + ": " + valueAsText(dByName[sWhich][sField]));
                    }
                    fDoc.WriteLine("");
                }
                fDoc.Close();
                logMessage("Written to " + sPagePath + ": what "
                           + counted(lNamesHere.Count, "picture", "pictures") + " now says about itself.",
                           "INFO", "");
                return true;
            }
            catch (Exception oError)
            {
                logMessage("The field list could not be written to " + sPagePath + ": " + oError.Message, "ERROR");
                try
                {
                    if (fDoc != null) fDoc.Close();
                }
                catch (Exception)
                {
                }
                return false;
            }
        }

        // What is ACTUALLY in the pictures now.
        //
        // One call over the whole folder, because an exit code says a program
        // finished and not that the work was done. Everything reported to him
        // as "carrying its description" is counted here, from the files
        // themselves, after the fact.
        static int countDescribedFiles(string sExifTool, string sFolder, out int iWithAltText)
        {
            iWithAltText = 0;
            if (sExifTool == "") return 0;
            string sOut = "";
            string sErr = "";
            // The same definitions are needed to READ the fields back on a
            // copy that does not know them, or the check would report them
            // missing after writing them perfectly well.
            string sBefore = bTeachAltTags && sAltConfigPath != "" ? "-config " + quoted(sAltConfigPath) + " " : "";
            int iCode = runCommand(sExifTool, sBefore + "-j -q -m -charset UTF8"
                                            + " -XMP-dc:Description -EXIF:XPComment -Comment"
                                            + " -XMP-iptcCore:AltTextAccessibility " + quoted(sFolder),
                                   out sOut, out sErr);
            if (sOut.Trim() == "") return 0;
            int iHave = 0;
            try
            {
                JavaScriptSerializer oSerializer = new JavaScriptSerializer();
                oSerializer.MaxJsonLength = int.MaxValue;
                foreach (object oItem in oSerializer.Deserialize<object[]>(sOut))
                {
                    Dictionary<string, object> dOne = toMap(oItem);
                    bool bAny = false;
                    foreach (string sField in new string[] { "Description", "XPComment", "Comment" })
                    {
                        if (dOne.ContainsKey(sField) && Convert.ToString(dOne[sField]).Trim() != "") bAny = true;
                    }
                    if (bAny) iHave = iHave + 1;
                    if (dOne.ContainsKey("AltTextAccessibility")
                        && Convert.ToString(dOne["AltTextAccessibility"]).Trim() != "") iWithAltText = iWithAltText + 1;
                }
            }
            catch (Exception oError)
            {
                logMessage("The check of what was written could not be read: " + oError.Message, "INFO", "");
                return 0;
            }
            if (iCode != 0) logMessage("  ExifTool grumbled while checking: " + tail(sErr, 160), "INFO", "");
            return iHave;
        }

        // EVERY picture, turned into a plain PNG of a modest size before the
        // model is shown it.
        //
        // Not only the ones Ollama cannot read. On 21 August, twenty-five of
        // forty-nine pictures in one archive were refused with
        //   (400) Bad Request
        // each within half a second -- too fast to be inference, so rejected on
        // sight. Same archive, same prompt, same extension: image.jpg went
        // through and Phil2.jpg did not, which points at something in the
        // picture that the decoder would not take. Rather than find out which
        // of progressive encoding, colour space, bit depth or sheer size it
        // was, every picture now arrives in the one shape known to work.
        //
        // It also cuts what has to be encoded and sent. A photograph straight
        // off a camera is several thousand pixels wide; the model is shown 512
        // for a film frame, so a thousand for a still is already generous.
        static string asPngFor(string sFfmpeg, string sPath, string sWorkDir)
        {
            int iWide = integer("picture-width");
            if (iWide < 64) iWide = 1024;
            string sPng = Path.Combine(sWorkDir, Path.GetFileNameWithoutExtension(sPath) + ".seen.png");
            string sOut = "";
            string sErr = "";
            // Reduced only if it is bigger than the limit, never enlarged, and
            // the shape is kept. -pix_fmt rgb24 settles the colour space, which
            // is one of the things a decoder can refuse.
            string sScale = "scale='if(gt(max(iw,ih)," + iWide.ToString() + "),if(gte(iw,ih)," + iWide.ToString()
                          + ",-2),iw)':'if(gt(max(iw,ih)," + iWide.ToString() + "),if(gte(iw,ih),-2,"
                          + iWide.ToString() + "),ih)'";
            int iCode = runCommand(sFfmpeg, "-hide_banner -loglevel error -y -i " + quoted(sPath)
                                          + " -frames:v 1 -vf " + quoted(sScale) + " -pix_fmt rgb24 " + quoted(sPng),
                                   out sOut, out sErr);
            if (iCode != 0 || !File.Exists(sPng))
            {
                logMessage("  ffmpeg could not read " + Path.GetFileName(sPath) + ": " + tail(sErr, 160), "INFO", "");
                return "";
            }
            return sPng;
        }

        // One archive, from end to end.
        static int describeArchive(string sZipPath)
        {
            string sFfmpeg = findTool("ffmpeg");
            string sRoot = Path.GetFileNameWithoutExtension(sZipPath);
            string sBase = text("output-dir");
            if (sBase == "") sBase = Path.GetDirectoryName(Path.GetFullPath(sZipPath));
            string sOutputDir = Path.Combine(sBase, sRoot);
            string sWorkDir = Path.Combine(workFolderFor(sZipPath), "pictures");
            if (oFileLog == null) oFileLog = new StringBuilder();
            string sPagePath = Path.Combine(sOutputDir, sDefaultPicturesName);
            if (File.Exists(sPagePath) && !flag("force"))
            {
                logMessage("Skipping " + sRoot + ": " + sDefaultPicturesName + " is already in " + sOutputDir + ".",
                           "INFO", "Skipping " + sRoot + ", already done.");
                sLastSkippedFolder = sOutputDir;
                return iAlreadyDone;
            }
            List<string> lInside = new List<string>();
            List<string> lPassedOver = new List<string>();
            List<string> lNotesFound = new List<string>();
            try
            {
                Directory.CreateDirectory(sOutputDir);
                Directory.CreateDirectory(sWorkDir);
                using (ZipArchive oZip = ZipFile.OpenRead(sZipPath))
                {
                    foreach (ZipArchiveEntry oEntry in oZip.Entries)
                    {
                        // A folder inside the archive, or a name that would
                        // climb out of the folder it is being written into.
                        if (oEntry.Name == "") continue;
                        // A note rather than a picture. Kept, not counted as
                        // something passed over, since it is going to be used.
                        if (string.Compare(Path.GetExtension(oEntry.Name), ".md", true) == 0)
                        {
                            string sNotePath = Path.Combine(sWorkDir, Path.GetFileName(oEntry.Name));
                            oEntry.ExtractToFile(sNotePath, true);
                            lNotesFound.Add(Path.GetFileName(oEntry.Name));
                            continue;
                        }
                        if (pictureKind(oEntry.Name) == "")
                        {
                            lPassedOver.Add(oEntry.FullName);
                            continue;
                        }
                        string sHere = Path.Combine(sWorkDir, Path.GetFileName(oEntry.Name));
                        oEntry.ExtractToFile(sHere, true);
                        lInside.Add(sHere);
                    }
                }
            }
            catch (Exception oError)
            {
                logMessage("The archive could not be read: " + oError.Message, "ERROR");
                lFailures.Add(sZipPath + Environment.NewLine + "    Could not be read: " + oError.Message
                              + Environment.NewLine + "    A password-protected archive, or one using a compression"
                              + " method Windows does not open, will fail here.");
                return 1;
            }
            // The note that applies to every picture: one named after the
            // archive itself, or failing that a plain context.md.
            string sWholeNote = noteBeside(Path.Combine(sWorkDir, sRoot + ".md"));
            if (sWholeNote == "") sWholeNote = noteBeside(Path.Combine(sWorkDir, "context.md"));
            // And anything given on the command line, which is more general
            // still and so comes first of all.
            string sGivenNote = "";
            if (text("context-file") != "" && File.Exists(text("context-file")))
                sGivenNote = noteBeside(text("context-file"));
            logMessage("The archive holds " + counted(lInside.Count, "picture", "pictures")
                       + ", " + counted(lNotesFound.Count, "note", "notes")
                       + " and " + counted(lPassedOver.Count, "other file", "other files") + ".",
                       "INFO", sRoot + " holds " + counted(lInside.Count, "picture", "pictures") + ".");
            foreach (string sNote in lNotesFound) logMessage("  Note found: " + sNote, "INFO", "");
            if (sWholeNote != "")
                logMessage("A note for the whole archive, " + counted(sWholeNote.Split(' ').Length, "word", "words")
                           + ", will be sent with every picture.", "INFO", "");
            foreach (string sOther in lPassedOver)
            {
                // A drawing rather than a picture. Worth naming separately,
                // because it is not junk in the archive: it is something the
                // model cannot be shown. SVG carries its own title and desc
                // elements, which would be the best home for a description of
                // any format here, and is worth coming back for.
                if (metadataKindOf(sOther) == "vector")
                    logMessage("  Passed over: " + sOther + " is a drawing rather than a picture, and nothing "
                               + "here can turn it into something the model could look at.", "INFO", "");
                else logMessage("  Passed over, not a picture: " + sOther, "INFO", "");
            }
            if (lInside.Count == 0)
            {
                logMessage("There is nothing to describe in " + sRoot + ".", "ERROR");
                lFailures.Add(sZipPath + Environment.NewLine + "    No image files in it.");
                return 1;
            }
            List<string> lNames = new List<string>();
            List<string> lAbout = new List<string>();
            List<string> lOriginal = new List<string>();
            int iAt = 0;
            foreach (string sPicture in lInside)
            {
                iAt = iAt + 1;
                string sBare = Path.GetFileName(sPicture);
                announce("Initializing", -1.0, 1.0, "Picture " + iAt.ToString() + " of "
                         + lInside.Count.ToString() + ": " + sBare);
                // What the original was, so that a pattern among refusals or
                // failures is visible rather than guessed at.
                try
                {
                    logMessage("  " + sBare + ": " + (new FileInfo(sPicture).Length / 1024).ToString() + " KB as it stands.",
                               "INFO", "");
                }
                catch (Exception)
                {
                }
                string sShow = asPngFor(sFfmpeg, sPicture, sWorkDir);
                if (sShow == "")
                {
                    logMessage("  " + sBare + " could not be turned into something the model can read.", "ERROR");
                    continue;
                }
                // Most general first, most particular last, so the nearest
                // note has the last word.
                StringBuilder oNotes = new StringBuilder();
                if (sGivenNote != "") oNotes.Append(sGivenNote);
                if (sWholeNote != "")
                {
                    if (oNotes.Length > 0) oNotes.Append(" ");
                    oNotes.Append(sWholeNote);
                }
                string sOwnNote = noteBeside(Path.Combine(sWorkDir, Path.GetFileNameWithoutExtension(sBare) + ".md"));
                if (sOwnNote != "")
                {
                    if (oNotes.Length > 0) oNotes.Append(" ");
                    oNotes.Append(sOwnNote);
                    logMessage("  " + sBare + " has a note of its own, "
                               + counted(sOwnNote.Split(' ').Length, "word", "words") + ".", "INFO", "");
                }
                string sCalled = nameAsHint(sBare);
                if (sCalled != "") logMessage("  Its name says: " + sCalled, "INFO", "");
                waitingOn("looking at " + sBare);
                string[] asSaid = describeStill(sShow, oNotes.ToString(), sCalled);
                waitingOn("");
                if (asSaid[1] == "")
                {
                    logMessage("  " + sBare + ": the model said nothing.", "ERROR");
                    continue;
                }
                lOriginal.Add(sBare);
                lNames.Add(asSaid[0]);
                lAbout.Add(asSaid[1]);
                logMessage("  " + sBare + " -> \"" + asSaid[0] + "\": " + asSaid[1], "INFO",
                           asSaid[0]);
            }
            if (lOriginal.Count == 0)
            {
                logMessage("Nothing in " + sRoot + " could be described.", "ERROR");
                lFailures.Add(sZipPath + Environment.NewLine + "    Nothing in it could be described.");
                return 1;
            }
            numberClashes(lNames);

            // ---- the pictures themselves ----
            //
            // Described once, metadata written once, then copied. The copy in
            // the archive is byte for byte the one in the folder, differing
            // only in its name, so there is no second pass to drift.
            logMessage("Looking for ExifTool, which writes the descriptions into the pictures.", "INFO", "");
            string sExifTool = exifToolProgram();
            if (sExifTool == "")
                logMessage("ExifTool was not found, so the descriptions cannot be written into the pictures. "
                           + "They are still in " + sDefaultPicturesName + ". Run installExifTool.cmd, or reinstall "
                           + "HomerScribe, to have them written into the files as well.",
                           "ERROR", "The descriptions cannot be written into the pictures: ExifTool is missing.");
            else
            {
                // The version was logged by the search above, with every other
                // candidate beside it.
                bNoAltTags = false;
                bTeachAltTags = false;
                sAltConfigPath = "";
                // NOT simply the first picture. In test.zip the first is a BMP,
                // and ExifTool refuses those outright -- "Writing of BMP files
                // is not yet supported" -- so every field came back missing and
                // the test concluded the accessibility definitions had failed,
                // when on a JPEG the same definitions had worked an hour
                // earlier. The test needs a picture that can actually hold the
                // full set, or it is testing the picture and not ExifTool.
                string sTestOn = "";
                foreach (string sOne in lOriginal)
                {
                    if (metadataKindOf(sOne) == "full")
                    {
                        sTestOn = Path.Combine(sWorkDir, sOne);
                        break;
                    }
                }
                if (sTestOn != "") selfTestMetadata(sExifTool, sTestOn, sWorkDir);
                else logMessage("No picture here can hold the full set of fields, so there is nothing to test "
                                + "the writing on. Each picture is still written to as far as its format allows.",
                                "INFO", "");
            }
            string sStageDir = Path.Combine(sWorkDir, "named");
            try
            {
                if (Directory.Exists(sStageDir)) Directory.Delete(sStageDir, true);
                Directory.CreateDirectory(sStageDir);
            }
            catch (Exception)
            {
            }
            int iWritten = 0;
            int iNoRoom = 0;
            int iRefused = 0;
            for (int iOne = 0; iOne < lOriginal.Count; iOne = iOne + 1)
            {
                string sFrom = Path.Combine(sWorkDir, lOriginal[iOne]);
                string sHere = Path.Combine(sOutputDir, lOriginal[iOne]);
                string sKind = metadataKindOf(lOriginal[iOne]);
                try
                {
                    File.Copy(sFrom, sHere, true);
                }
                catch (Exception oError)
                {
                    logMessage("  " + lOriginal[iOne] + " could not be copied out: " + oError.Message, "ERROR");
                    continue;
                }
                if (sKind == "none" || sKind == "vector"
                    || lNoRoomKinds.Contains(Path.GetExtension(lOriginal[iOne]).ToLower())) iNoRoom = iNoRoom + 1;
                else if (writeMetadata(sExifTool, sHere, lNames[iOne], lAbout[iOne], sKind)) iWritten = iWritten + 1;
                // Counted as having nowhere to put one, not as a failure, if
                // that is what ExifTool just told us.
                else if (lNoRoomKinds.Contains(Path.GetExtension(lOriginal[iOne]).ToLower())) iNoRoom = iNoRoom + 1;
                else iRefused = iRefused + 1;
                // And the same file again under the name it earned.
                try
                {
                    File.Copy(sHere, Path.Combine(sStageDir, lNames[iOne] + Path.GetExtension(lOriginal[iOne])), true);
                }
                catch (Exception oError)
                {
                    logMessage("  " + lNames[iOne] + " could not be put in the archive: " + oError.Message, "ERROR");
                }
            }
            // ---- what each copy now says about itself ----
            //
            // One for the folder and one for the archive, and they are NOT
            // copies of each other: the folder holds these pictures under
            // their original names and the archive holds them under their new
            // ones, so each lists its own by the names it actually carries.
            // Written before the archive is made, so that the archive contains
            // its own.
            writeDescribedPage(sExifTool, sOutputDir, Path.Combine(sOutputDir, sDefaultPicturesName),
                sRoot + ": what these pictures say about themselves",
                "Every picture in this folder, under the name it has here, and every field that now has a "
                + "value in it. Read back out of the files themselves, so what is listed is what is there — "
                + "including anything that was already in the picture before HomerScribe saw it. The same "
                + "pictures under their new names, with the same fields, are in **" + sRoot + ".zip**.");
            writeDescribedPage(sExifTool, sStageDir, Path.Combine(sStageDir, sDefaultPicturesName),
                sRoot + ": what these pictures say about themselves",
                "Every picture in this archive, under its new name, and every field that now has a value in "
                + "it. Read back out of the files themselves, so what is listed is what is there — including "
                + "anything that was already in the picture before HomerScribe saw it. The same pictures "
                + "under their original names are in the folder this archive came from.");

            // ---- the archive of renamed copies ----
            string sZipOut = Path.Combine(sOutputDir, sRoot + ".zip");
            bool bZipped = false;
            try
            {
                if (File.Exists(sZipOut)) File.Delete(sZipOut);
                ZipFile.CreateFromDirectory(sStageDir, sZipOut, CompressionLevel.Optimal, false);
                bZipped = true;
            }
            catch (Exception oError)
            {
                logMessage("The archive of renamed pictures could not be made: " + oError.Message, "ERROR");
            }
            // Counted from the files, not from the exit codes.
            int iWithAltText = 0;
            int iReallyThere = countDescribedFiles(sExifTool, sOutputDir, out iWithAltText);
            if (sExifTool != "" && iReallyThere != iWritten)
                logMessage("ExifTool reported " + iWritten.ToString() + " written, but reading the files back finds "
                           + iReallyThere.ToString() + " carrying a description. The second figure is the true one.",
                           "ERROR", "");
            iWritten = iReallyThere;
            logMessage("Descriptions found in " + counted(iWritten, "picture", "pictures")
                       + " on reading them back"
                       + (iWithAltText == 0 ? ", none of them in the IPTC accessibility fields"
                                            : ", " + iWithAltText.ToString() + " of them in the IPTC accessibility fields")
                       + "; " + iNoRoom.ToString() + " of a kind with nowhere to put one"
                       + (iRefused == 0 ? "" : "; " + iRefused.ToString() + " refused by ExifTool")
                       + (bZipped ? ". The renamed copies are in " + Path.GetFileName(sZipOut) + "." : "."),
                       "INFO", "Descriptions written into " + counted(iWritten, "picture", "pictures") + ".");

            // The old report -- what can be seen, the name given, whether the
            // format could hold it -- has been replaced by writeDescribedPage
            // above, which lists what each picture NOW SAYS ABOUT ITSELF. The
            // descriptions are not lost: they are in the files, in
            // ImageDescription and Caption-Abstract and the rest, which is
            // where he wanted them and where the new document reads them from.
            //
            // What that document cannot show is the things that never became
            // pictures, so they are named here instead.
            if (lPassedOver.Count > 0 || lNotesFound.Count > 0)
            {
                try
                {
                    StreamWriter fMore = new StreamWriter(sPagePath, true, new UTF8Encoding(false));
                    fMore.WriteLine("## Other files in the archive");
                    fMore.WriteLine("");
                    foreach (string sNote in lNotesFound)
                        fMore.WriteLine("- **" + sNote + "**: a note, used as context for every picture it applies to.");
                    foreach (string sOther in lPassedOver)
                    {
                        if (metadataKindOf(sOther) == "vector")
                            fMore.WriteLine("- **" + sOther + "**: a drawing rather than a picture. Nothing here can "
                                            + "turn it into something the model could look at.");
                        else fMore.WriteLine("- **" + sOther + "**: not a picture, so it was passed over.");
                    }
                    fMore.WriteLine("");
                    fMore.Close();
                }
                catch (Exception oError)
                {
                    logMessage("The list of other files could not be added: " + oError.Message, "INFO", "");
                }
            }
            logMessage("Described " + counted(lOriginal.Count, "picture", "pictures") + ".",
                       "INFO", "Described " + counted(lOriginal.Count, "picture", "pictures") + ".");
            // iNoRoom, not iCannot. They counted different things and said so
            // in the same breath: the results box read "2 of a kind that cannot
            // hold one" while the log read "1 of a kind with nowhere to put
            // one". A GIF is neither -- it holds a plain comment -- and only
            // formats with nowhere at all belong in this figure.
            lResults.Add(sRoot + ": " + counted(lOriginal.Count, "picture described", "pictures described")
                         + ", " + counted(iWritten, "carrying its description inside it", "carrying their descriptions inside them")
                         + (iNoRoom == 0 ? "" : ", " + iNoRoom.ToString() + " of a kind that cannot hold one")
                         + Environment.NewLine + "    " + sOutputDir
                         + (bZipped ? Environment.NewLine + "    " + sZipOut : ""));
            sLastOutputFolder = sOutputDir;
            writeFileLog(sOutputDir);
            try
            {
                Directory.Delete(sWorkDir, true);
            }
            catch (Exception)
            {
            }
            return 0;
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
            lListsRead.Clear();
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
                // describe. Nothing else could be meant by handing one over.
                if (looksLikeList(sGiven) && !File.Exists(sGiven))
                {
                    announce("Rejected", -1.0, 1.0, Path.GetFileName(sGiven) + " was not found.");
                    string sGone = sGiven + Environment.NewLine
                                 + "    That list was not found. It may have been moved or renamed, or saved somewhere else by the browser.";
                    logMessage(sGone.Replace(Environment.NewLine, " "), "ERROR");
                    lFailures.Add(sGone);
                    continue;
                }
                if (looksLikeArchive(sGiven))
                {
                    foreach (string sOne in expandPattern(sGiven)) lSources.Add(sOne);
                    continue;
                }
                if (looksLikeList(sGiven) && File.Exists(sGiven))
                {
                    lListsRead.Add(sGiven);
                    foreach (string sListed in readListFile(sGiven))
                    {
                        if (sListed.StartsWith("http://") || sListed.StartsWith("https://"))
                        {
                            if (looksLikePlaylist(sListed))
                            {
                                foreach (string sOne in expandPlaylist(sListed)) lSources.Add(sOne);
                                continue;
                            }
                            lSources.Add(sListed);
                            continue;
                        }
                        foreach (string sOne in expandPattern(sListed)) lSources.Add(sOne);
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
                // The list was read and understood; it simply named nothing.
                // Saying "nothing was found matching" sends someone looking for
                // a file that is sitting right there.
                if (lListsRead.Count > 0)
                {
                    sSaid = "The list " + Path.GetFileName(lListsRead[0]) + " was read, but it names no sources."
                          + Environment.NewLine + Environment.NewLine
                          + "Every line in it is blank or begins with # or ; which marks a note. "
                          + "Take the # off the line you want to use, or add a file or address on a line of its own.";
                }
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
                iSourceAt = iAt;
                iSourceCount = lSources.Count;
                announce("Initializing", -1.0, 1.0, processingSaid(sSource, iAt, lSources.Count));
                // This film's own log begins here, before anything is fetched.
                // It used to begin inside runOne, which left out how the film
                // was obtained -- and on the captions-only route left out
                // which caption track was chosen and why, which was most of
                // what there was to say.
                oFileLog = new StringBuilder();
                string sPath = sSource;
                if (sSource.StartsWith("http://") || sSource.StartsWith("https://"))
                {
                    // The root the results go under. fetchFromWeb makes the
                    // per-video folder inside it once it knows the title, so the
                    // video and its results share one place.
                    string sFolder = text("output-dir");
                    if (sFolder == "") sFolder = Path.Combine(appDataFolder(), "downloads");
                    sPath = fetchFromWeb(sSource, sFolder, findTool("ffmpeg"));
                    if (sPath == sAlreadyDone)
                    {
                        announce("Skipped", -1.0, 1.0, "Already done: " + sSource);
                        iSkippedWhole = iSkippedWhole + 1;
                        continue;
                    }
                    // The captions came and the film was left where it was.
                    // There is no local file, so none of what follows applies.
                    if (sPath == sCaptionsOnly)
                    {
                        int iWords = transcribeFromCaptions(sSource);
                        if (iWords == 0) iDescribedWhole = iDescribedWhole + 1;
                        else iWorst = iWords;
                        continue;
                    }
                    if (sPath == "")
                    {
                        announce("Error", -1.0, 1.0, (sLastFetchTrouble == "" ? "Could not be fetched." : sLastFetchTrouble));
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
                // An archive of pictures. Nothing that follows applies: there
                // is no duration, no sound, and nothing to place in time.
                if (looksLikeArchive(sFull) && File.Exists(sFull))
                {
                    int iZip = describeArchive(sFull);
                    if (iZip == iAlreadyDone)
                    {
                        announce("Skipped", -1.0, 1.0, "Already done: " + Path.GetFileName(sFull));
                        iSkippedWhole = iSkippedWhole + 1;
                    }
                    else if (iZip == 0) iDescribedWhole = iDescribedWhole + 1;
                    else iWorst = iZip;
                    continue;
                }
                if (!sSource.StartsWith("http") && !looksLikeMedia(sFull) && File.Exists(sFull))
                {
                    announce("Rejected", -1.0, 1.0, Path.GetFileName(sFull) + " is not a video, a recording, or a list of sources.");
                    string sNot = sFull + Environment.NewLine + "    Not a video or a recording, and not a list of sources.";
                    logMessage(sNot.Replace(Environment.NewLine, " "), "ERROR");
                    lFailures.Add(sNot);
                    iWorst = 1;
                    continue;
                }
                int iOne = runOne(sFull, sSource.StartsWith("http") ? sSource : "");
                if (iOne == iAlreadyDone)
                {
                    announce("Skipped", -1.0, 1.0, "Already done: " + Path.GetFileName(sFull));
                    iSkippedWhole = iSkippedWhole + 1;
                }
                if (iOne == 0) iDescribedWhole = iDescribedWhole + 1;
                if (iOne != 0 && iOne != iAlreadyDone)
                {
                    iWorst = iOne;
                    bool bAlreadyNamed = false;
                    foreach (string sOne in lFailures)
                    {
                        if (sOne.StartsWith(sFull) || sOne.StartsWith(sSource)) bAlreadyNamed = true;
                    }
                    announce("Error", -1.0, 1.0, Path.GetFileName(sFull) + " could not be finished. The log says why.");
                    if (!bAlreadyNamed) lFailures.Add(sFull + Environment.NewLine
                        + "    Something went wrong while working on it. The log says what.");
                }
                // A plain full stop between one source and the next. Asked for,
                // and worth having: after a long silent stretch of work the
                // only signal was the next "Processing" line, which says a new
                // thing has begun without ever saying the last one ended.
                // Nothing is said where the source failed or was skipped --
                // those have already spoken for themselves, and "Done" after
                // "Error" would be a lie.
                //
                // The MESSAGE is what finished, not the word "Done" again. A
                // screen reader reads the title and then reads the box, title
                // and all, so passing "Done" as both the category and the
                // message made it say the word twice -- which is the very
                // thing flushAnnouncements was written to avoid, and I walked
                // straight into it.
                if (iOne == 0)
                {
                    string sFinished = sSource.StartsWith("http") && sVideoTitle != ""
                                     ? sVideoTitle : Path.GetFileName(sFull);
                    announce("Done", -1.0, 1.0, sFinished == "" ? "That one is finished." : sFinished);
                }
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
                int iDays = daysOldTool(sVerOut + sVerErr);
                // Recorded, not complained about. Keeping yt-dlp current is the
                // build's job, and telling somebody at run time about a thing
                // they cannot act on there and then is noise.
                logMessage("  yt-dlp version " + (sVerOut + sVerErr).Trim()
                           + (iDays < 0 ? "" : ", " + counted(iDays, "day", "days") + " old"), "INFO", "");
            }

            bool bReady = true;
            // An archive of pictures has no sound in it, so a run over
            // archives alone needs nothing that listens.
            bool bPicturesOnly = true;
            foreach (string sGiven in splitPaths(text("source-paths")))
            {
                if (!looksLikeArchive(sGiven)) bPicturesOnly = false;
            }
            if (bPicturesOnly && flag("transcribe"))
                logMessage("These sources are archives of pictures, which have no sound. Transcribe audio does not apply to them.",
                           "INFO", "Archives of pictures have no sound, so there is nothing to transcribe.");
            if (!bPicturesOnly && (flag("transcribe") || (flag("describe") && flag("speech"))))
            {
                string sWhisper = whisperProgram();
                string sModel = whisperModelPath();
                if (sWhisper != "" && sModel != "") logMessage("Whisper is in place: " + sWhisper, "INFO", "");
                if (sWhisper == "" || sModel == "")
                {
                    // Transcribing no longer needs Whisper if the film carries
                    // its own captions, so a missing Whisper only stops the run
                    // when captions are turned off. A film that turns out to
                    // have none is refused later, by name, with the reason.
                    bool bFatal = flag("transcribe") && !flag("captions");
                    logMessage("Whisper was not found. Run installWhisper.cmd in the program folder."
                               + (bFatal ? "" : " A film carrying its own English captions can still be transcribed from those."),
                               bFatal ? "ERROR" : "INFO", bFatal ? null : "");
                    if (bFatal) bReady = false;
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
            if (iDone == 0 && lFailures.Count == 0) oSaid.Append("Nothing was done.");
            if (iDone == 0 && lFailures.Count > 0) oSaid.Append(lFailures.Count == 1
                ? "Nothing was done: the one source given could not be used."
                : "Nothing was done: none of the " + lFailures.Count.ToString() + " sources given could be used.");
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
                int iNeedSignIn = 0;
                foreach (string sOne in lFailures)
                {
                    if (sOne.IndexOf("sign in", StringComparison.OrdinalIgnoreCase) >= 0
                        || sOne.IndexOf("cookies", StringComparison.OrdinalIgnoreCase) >= 0) iNeedSignIn = iNeedSignIn + 1;
                }
                if (iNeedSignIn > 0 && text("browser-cookies") == "")
                {
                    oSaid.Append(Environment.NewLine + Environment.NewLine
                               + iNeedSignIn.ToString() + " of these asked you to sign in. If you are signed in to that site in a browser, "
                               + "--browser-cookies chrome (or edge, firefox, brave, opera) lets the download use it.");
                }
                oSaid.Append(Environment.NewLine + Environment.NewLine + "Could not be used:");
                foreach (string sOne in lFailures) oSaid.Append(Environment.NewLine + sOne);
            }
            if (sLogPath != "") oSaid.Append(Environment.NewLine + Environment.NewLine + "Log of this run:" + Environment.NewLine + sLogPath);
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
        // What the page says the video is: its title, who published it, and
        // what they wrote about it. Asked once and kept, because two things
        // want it -- the context sent to the model, and the heading of every
        // document written. It used to be fetched only for the model and then
        // thrown away.
        static bool webMetadata(string sAddress)
        {
            if (sAddress == "") return false;
            if (sVideoAddress == sAddress && sVideoTitle != "") return true;
            string sYtDlp = findTool("yt-dlp");
            if (sYtDlp == "") return false;
            string sOut = "";
            string sErr = "";
            int iCode = runCommand(sYtDlp, "--skip-download --no-playlist" + quietly() + " --dump-single-json " + quoted(sAddress), out sOut, out sErr);
            if (iCode != 0 || sOut.Trim() == "")
            {
                logMessage("The page would not say what the video is. yt-dlp said: " + tail(sErr, 200), "INFO", "");
                return false;
            }
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
                return false;
            }
            sVideoAddress = sAddress;
            if (dPage.ContainsKey("title")) sVideoTitle = Convert.ToString(dPage["title"]).Trim();
            if (dPage.ContainsKey("uploader")) sVideoBy = Convert.ToString(dPage["uploader"]).Trim();
            nVideoSeconds = 0.0;
            if (dPage.ContainsKey("duration"))
            {
                try
                {
                    nVideoSeconds = Convert.ToDouble(dPage["duration"]);
                }
                catch (Exception)
                {
                    nVideoSeconds = 0.0;
                }
            }
            if (dPage.ContainsKey("description")) sVideoAbout = Regex.Replace(Convert.ToString(dPage["description"]), @"\s+", " ").Trim();
            // What captions the video has, asked here rather than guessed at
            // download time. "subtitles" holds what a person uploaded;
            // "automatic_captions" holds what the machine made AND every
            // translation of it, which is where the trouble came from.
            lTracksWritten = new List<string>();
            lTracksAuto = new List<string>();
            try
            {
                if (dPage.ContainsKey("subtitles")) lTracksWritten = englishAmong(toMap(dPage["subtitles"]));
                if (dPage.ContainsKey("automatic_captions")) lTracksAuto = englishAmong(toMap(dPage["automatic_captions"]));
            }
            catch (Exception oTrouble)
            {
                logMessage("The list of caption tracks could not be read: " + oTrouble.Message, "INFO", "");
            }
            logMessage("English caption tracks on the page: "
                       + (lTracksWritten.Count == 0 ? "none written by a person" : string.Join(", ", lTracksWritten.ToArray()) + " written by a person")
                       + "; "
                       + (lTracksAuto.Count == 0 ? "none made automatically" : string.Join(", ", lTracksAuto.ToArray()) + " made automatically")
                       + ".", "INFO", "");
            logMessage("The page calls it \"" + sVideoTitle + "\""
                       + (sVideoBy == "" ? "" : ", published by " + sVideoBy)
                       + ", with " + counted(sVideoAbout == "" ? 0 : sVideoAbout.Split(' ').Length, "word", "words") + " about it.", "INFO", "");
            return true;
        }

        // The English among a set of caption tracks, best first. "en" plainly
        // is the one to want; a regional spelling will do; anything else in the
        // list is not English at all and is left out.
        static List<string> englishAmong(Dictionary<string, object> dTracks)
        {
            List<string> lFound = new List<string>();
            List<string> lRest = new List<string>();
            if (dTracks == null) return lFound;
            foreach (string sKey in dTracks.Keys)
            {
                if (!englishTag(sKey)) continue;
                if (string.Compare(sKey, "en", true) == 0) lFound.Add(sKey);
                else lRest.Add(sKey);
            }
            lRest.Sort();
            foreach (string sOne in lRest) lFound.Add(sOne);
            return lFound;
        }

        // The one track to ask for, and whether a person wrote it. An empty
        // name means the page named no English track at all.
        static string trackToFetch(out bool bByHand)
        {
            bByHand = lTracksWritten.Count > 0;
            List<string> lUse = bByHand ? lTracksWritten : lTracksAuto;
            if (lUse.Count == 0) return "";
            return lUse[0];
        }

        static string webPageContext(string sAddress)
        {
            if (!webMetadata(sAddress)) return "";
            StringBuilder oSaid = new StringBuilder();
            if (sVideoTitle != "") oSaid.Append("This video is called \"" + sVideoTitle + "\". ");
            if (sVideoBy != "") oSaid.Append("It was published by " + sVideoBy + ". ");
            if (sVideoAbout != "")
            {
                // The tail of a description is usually links and appeals to
                // subscribe, which say nothing about what is on screen.
                string sAbout = sVideoAbout;
                if (sAbout.Length > 1200) sAbout = sAbout.Substring(0, 1200);
                oSaid.Append("Its own description reads: " + sAbout + " ");
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
            // What a video says about itself belongs to that video, and a run
            // may cover many. These are cleared unless they were fetched for
            // THIS address a moment ago in fetchFromWeb. Without this, a local
            // file following a downloaded one inherited its title, publisher
            // and description, and said so at the head of its documents.
            if (sWebAddress != sVideoAddress)
            {
                sVideoTitle = "";
                sVideoBy = "";
                sVideoAbout = "";
                sVideoAddress = sWebAddress;
                lTracksWritten = new List<string>();
                lTracksAuto = new List<string>();
                nVideoSeconds = 0.0;
            }
            sCouldNotDescribe = "";
            if (!File.Exists(sInput))
            {
                logMessage("The file was not found: " + sInput, "ERROR");
                announce("Rejected", -1.0, 1.0, Path.GetFileName(sInput) + " was not found.");
                lFailures.Add(sInput + Environment.NewLine + "    Not found. It may have been moved, renamed or deleted since the path was saved.");
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
            if (bWantDescribe && !hasPicture(sFfmpeg, sInput))
            {
                string sNoPicture = Path.GetFileName(sInput) + " is a recording with no picture in it, so there is nothing to describe.";
                bWantDescribe = false;
                if (bWantTranscribe)
                {
                    logMessage(sNoPicture + " It will be transcribed instead.", "INFO", "");
                    announce("Initializing", -1.0, 1.0, sNoPicture + " Transcribing it instead.");
                }
                else
                {
                    logMessage(sNoPicture + " Tick Transcribe audio for a recording.", "ERROR");
                    announce("Rejected", -1.0, 1.0, sNoPicture + " Tick Transcribe audio for a recording.");
                    lFailures.Add(sInput + Environment.NewLine
                                  + "    A recording with no picture. Describing needs a picture; transcribing does not.");
                    return 1;
                }
            }
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
            // Where a list was given -- a playlist, a pattern, or a file naming
            // several -- the position in it is said, because knowing there are
            // eight more to come is worth as much as knowing which one this is.
            // A single named file has no position worth stating.
            // The position and the name were said before the fetch, in run().
            // Saying them again here would repeat them for a local file.
            if (flag("force"))
            {
                int iCleared = 0;
                string[] asOurs = new string[] {
                    Path.Combine(sOutputDir, sDefaultTranscriptName),
                    Path.Combine(sOutputDir, sDefaultBothName),
                    Path.Combine(sOutputDir, sDefaultMarkdownName),
                    Path.Combine(sWorkDir, sDefaultJsonName),
                    Path.Combine(sWorkDir, "transcript.json"),
                    Path.Combine(sWorkDir, sDefaultVttName) };
                foreach (string sOld in asOurs)
                {
                    try
                    {
                        if (!File.Exists(sOld)) continue;
                        File.Delete(sOld);
                        iCleared = iCleared + 1;
                    }
                    catch (Exception)
                    {
                    }
                }
                foreach (string sOld in Directory.GetFiles(sOutputDir, sDefaultDescribedStem + ".*"))
                {
                    try
                    {
                        File.Delete(sOld);
                        iCleared = iCleared + 1;
                    }
                    catch (Exception)
                    {
                    }
                }
                // And what was already read out of them, or the moments live on
                // in memory after their record has gone.
                lCachedGaps = null;
                dCachedSignature = null;
                bLastCacheFinished = false;
                bLastTranscriptFinished = false;
                if (iCleared > 0) logMessage("Force overwrite: removed " + iCleared.ToString() + " file(s) left by the earlier run, so this one starts clean.",
                                             "INFO", "Starting clean: what the earlier run left has been removed.");
                logMessage("The moments and the transcript held in memory from that record are discarded too.", "INFO", "");
            }
            // Opened in run() before the fetch, so that how the film was
            // obtained is in its own log. Only made here if this is a local
            // file that never went through run()'s fetch.
            if (oFileLog == null) oFileLog = new StringBuilder();
            logMessage("Working files are under " + sWorkDir, "INFO", "");
            sSpeechWorkDir = sWorkDir;
            lFilmSpeech = new List<Speech>();
            lCaptions = new List<Speech>();
            sTranscriptFrom = "";
            bSpeechReady = false;
            sEstablished = "";
            iEstablishedAt = 0;
            sContextLast = "";
            iContextUsed = 0;
            iContextEchoed = 0;
            lCachedGaps = null;
            dCachedSignature = null;
            bSpeechDoubtful = false;
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
                sContext = splitContextByTime(File.ReadAllText(sContextFile));
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
            // A file that was not downloaded still often knows its own title,
            // carried as a tag inside it. That belongs in the heading too.
            if (sVideoTitle == "")
            {
                string sOwnTitle = titleOf(sFfmpeg, sInput);
                if (sOwnTitle != "") sVideoTitle = sOwnTitle;
            }

            // The transcript is wanted by both jobs: by transcribing, obviously,
            // and by describing, to know where the speech is. So it is made
            // once, before either.
            // The film's own captions come first, because a person wrote them.
            // They are the WORDS. They are not the map of where the speech
            // falls: a cue is shown early and taken away late so that a reader
            // can finish it, which is a different thing from when the words are
            // said. So a run that describes still listens to the film, and uses
            // what it hears for placement only.
            // What this film IS, said once, before either job starts.
            //
            // The opening added in 1.0.173 is spoken INSIDE described.mkv, so
            // it is only heard when the finished film is played, and only when
            // describing was asked for. He was listening for it during the run
            // and there was nothing: the last thing said was "Downloading",
            // then a long silence, then progress figures.
            StringBuilder oWhatItIs = new StringBuilder();
            if (sVideoTitle != "") oWhatItIs.Append(sVideoTitle + ". ");
            if (sVideoBy != "") oWhatItIs.Append("Published by " + sVideoBy + ". ");
            if (nDuration > 0.0)
            {
                int iMinutes = (int)Math.Round(nDuration / 60.0);
                if (iMinutes >= 60)
                {
                    int iHours = iMinutes / 60;
                    int iRest = iMinutes % 60;
                    oWhatItIs.Append(counted(iHours, "hour", "hours"));
                    if (iRest > 0) oWhatItIs.Append(" and " + counted(iRest, "minute", "minutes"));
                    oWhatItIs.Append(" long.");
                }
                else if (iMinutes > 0) oWhatItIs.Append(counted(iMinutes, "minute", "minutes") + " long.");
            }
            if (oWhatItIs.Length > 0)
                announce("Initializing", -1.0, 1.0, oWhatItIs.ToString().Trim());

            if (bWantTranscribe) lCaptions = captionsFor(sFfmpeg, sInput, sWorkDir, out sTranscriptFrom);
            // A person's captions are trusted over Whisper, as he asked. A
            // machine's are not, because the two are not comparable: a person
            // writes down who is speaking and what can be heard that is not
            // speech -- a door, music starting, laughter -- and no machine
            // produces the last of those. Automatic captions are the words and
            // nothing else, with punctuation only where the recogniser guessed,
            // and Whisper is at least as good at the words and better at the
            // sentences. So the richer transcript is the one that listens.
            if (lCaptions.Count > 0 && bCaptionsAuto && !flag("auto-captions"))
            {
                logMessage("These captions were made by a machine, not written by a person, so they carry no speaker "
                           + "names and none of the sounds that are not speech. The film is listened to instead, "
                           + "which gives better sentences. Pass --auto-captions yes to use them anyway.",
                           "INFO", "");
                lCaptions.Clear();
                sTranscriptFrom = "";
            }
            bool bMustListen = (bWantDescribe && flag("speech")) || (bWantTranscribe && lCaptions.Count == 0);
            if (bMustListen)
            {
                if (bWantDescribe && lCaptions.Count > 0)
                    logMessage("The captions are the transcript. The film is still listened to, because where the speech falls "
                               + "decides where a description can go, and a caption's timing says when it is shown rather than "
                               + "when it is said.", "INFO", "");
                sSpeechWorkDir = sWorkDir;
                lFilmSpeech = transcribe(sFfmpeg, sInput, sWorkDir, nDuration);
                bSpeechReady = true;
            }
            else if (bWantTranscribe)
            {
                logMessage("The film's own captions are the transcript, so it does not have to be listened to at all.",
                           "INFO", "Using the film's own captions, so there is nothing to listen to.");
            }
            if (bWantTranscribe)
            {
                List<Speech> lWords = lCaptions.Count > 0 ? lCaptions : lFilmSpeech;
                if (lWords.Count == 0)
                {
                    logMessage("Nothing could be transcribed: the film carries no English captions, and nothing was heard in it.", "ERROR");
                    if (!bWantDescribe) return 1;
                }
                else
                {
                    string sHow = lCaptions.Count > 0 ? "passage of captions" : "spoken stretch";
                    string sHowMany = lCaptions.Count > 0 ? "passages of captions" : "spoken stretches";
                    writeTranscript(lWords, sTranscriptPath, Path.GetFileName(sInput), nDuration);
                    bTranscribed = true;
                    writeCache(new List<Moment>(), sJsonPathEarly(sWorkDir), false, null, null);
                    logMessage("Transcript written to " + sTranscriptPath,
                               "INFO", "Transcript written: " + counted(lWords.Count, sHow, sHowMany) + ".");
                    if (!bWantDescribe) writeFileLog(sOutputDir);
                    lResults.Add(Path.GetFileName(sInput) + ": transcript of " + counted(lWords.Count, sHow, sHowMany)
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
            if (dCache.Count > 0)
            {
                logMessage("Picking up where the last run stopped: " + dCache.Count.ToString() + " descriptions already written",
                           "INFO", "");
                announce("Resuming", -1.0, 1.0, Path.GetFileName(sInput) + ", carrying on from "
                         + dCache.Count.ToString() + " descriptions already written.");
            }

            List<Moment> lGaps = findGaps(sFfmpeg, sInput, nDuration);
            List<Moment> lDone = new List<Moment>();
            List<string> lRecent = new List<string>();
            List<string> lNames = new List<string>();
            // Whoever the captions name as speaking. Not who is on screen --
            // see namesFromCaptions -- but the set of names this film uses, so
            // that a name reached for is one the film actually has.
            // The roster is NOT put into lNames any more. lNames is headed
            // "Names you have already used in this film" and asks the model to
            // match somebody against how one of them was DESCRIBED -- true of
            // names it coined itself, meaningless for names off a caption
            // track, and that mismatch is why thirty-nine names went unused.
            lSpeakerRoster = namesFromCaptions(lCaptions);
            // Logged from the roster itself. When the roster moved out of
            // lNames in 1.0.178 this line was left reading lNames, so it
            // stopped firing -- and six runs later there was no way to tell
            // from a log whether the cast list had reached the model at all.
            // Moving a thing and leaving its evidence behind is the same
            // mistake as trusting an exit code.
            if (lSpeakerRoster.Count > 0)
                logMessage("The captions name " + counted(lSpeakerRoster.Count, "speaker", "speakers") + ": "
                           + string.Join(", ", lSpeakerRoster.ToArray())
                           + ". This is the film's cast list, given to the model as that. It is not a claim about "
                           + "who is on screen at any moment.",
                           "INFO", "The captions name " + counted(lSpeakerRoster.Count, "speaker", "speakers") + ".");
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
            int iNothing = 0;
            int iNoPicture = 0;
            bool bVanished = false;
            int iTooLong = 0;
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
                // It used to say only "Audio description is on." He noticed
                // that the film's own closing credits named the programme while
                // nothing at the start did. The documents have opened with the
                // title, the publisher and the running time since 1.0.150; the
                // spoken opening never did, and a listener who starts a
                // described film should be told what they are listening to.
                StringBuilder oOpening = new StringBuilder();
                if (sVideoTitle != "") oOpening.Append(sVideoTitle + ". ");
                if (sVideoBy != "") oOpening.Append("Published by " + sVideoBy + ". ");
                if (nDuration > 0.0)
                {
                    // Spoken as words, not as a clock. "One hour and
                    // fifty-three minutes" is what a listener wants; "1:53:04"
                    // is what a screen reader would spell out digit by digit.
                    int iMinutes = (int)Math.Round(nDuration / 60.0);
                    if (iMinutes >= 60)
                    {
                        int iHours = iMinutes / 60;
                        int iRest = iMinutes % 60;
                        oOpening.Append(counted(iHours, "hour", "hours"));
                        if (iRest > 0) oOpening.Append(" and " + counted(iRest, "minute", "minutes"));
                        oOpening.Append(" long. ");
                    }
                    else if (iMinutes > 0) oOpening.Append(counted(iMinutes, "minute", "minutes") + " long. ");
                }
                oOpening.Append("Audio description is on.");
                Moment oSaid = new Moment();
                oSaid.nStart = 0.0;
                oSaid.sText = oOpening.ToString().Trim();
                oSaid.binAudio = speakToPcm(oSaid.sText, integer("rate"));
                oSaid.nSpoken = pcmSeconds(oSaid.binAudio);
                logMessage("The film opens by saying: " + oSaid.sText, "INFO", "");
                if (oSaid.binAudio.Length > 0) lDone.Add(oSaid);
            }

            // Two montage files used alternately: the one being looked at is
            // never the one being written.
            string sImagePath = Path.Combine(sWorkDir, "montage.jpg");
            string sNextImagePath = Path.Combine(sWorkDir, "montage-next.jpg");
            foreach (Moment oGap in lGaps)
            {
                iIndex = iIndex + 1;
                double nAllowed = oGap.nLength + overrunFor(text("detail"));
                int iMaxWords = Math.Max(6, (int)(nAllowed * number("words-per-second")));
                if (integer("max-words") > 0 && iMaxWords > integer("max-words")) iMaxWords = integer("max-words");
                nWaitingAt = oGap.nStart;
                string sJustSaid = spokenBefore(lFilmSpeech, oGap.nStart);
                // The captions know who was talking; Whisper does not. This is
                // handed to the prompt as its own thing rather than glued onto
                // the end of the dialogue, where it read as part of what was
                // said.
                string sWhoAbout = spokeAround(lCaptions, oGap.nStart);
                sSpeakerNear = sWhoAbout;
                if (sWhoAbout != "")
                    logMessage("  The captions have " + sWhoAbout + " speaking around " + formatClock(oGap.nStart) + ".",
                               "INFO", "");
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
                    // The window looked at is NOT the gap. A gap is a pause in the
                    // speech, and what the listener needs described is usually what
                    // happened while somebody was talking, just before it. The
                    // published systems separate the INTERVAL being described from
                    // the PLACEMENT PERIOD it is spoken in; this program was
                    // conflating the two and describing the pause.
                    double nSpan = Math.Max(oGap.nLength + 2.0, nDefaultLookBack);
                    double nCentre = oGap.nStart + oGap.nLength - nSpan / 2.0;
                    if (nCentre - nSpan / 2.0 < 0.0) nCentre = nSpan / 2.0;
                    // Made already, while the model was working on the moment
                    // before this one, unless this is the first.
                    bool bHaveIt = false;
                    if (sReadyMontage != "")
                    {
                        bHaveIt = waitForMontage();
                        if (bHaveIt)
                        {
                            try
                            {
                                if (File.Exists(sImagePath)) File.Delete(sImagePath);
                                File.Move(sReadyMontage, sImagePath);
                            }
                            catch (Exception)
                            {
                                bHaveIt = false;
                            }
                        }
                        sReadyMontage = "";
                    }
                    if (!bHaveIt && !buildMontage(sFfmpeg, sInput, nCentre, nSpan, sImagePath))
                    {
                        // Gone? Nothing that follows can work, and trying six
                        // hundred more times helps nobody.
                        if (!File.Exists(sInput))
                        {
                            string sVanished = Path.GetFileName(sInput) + " is no longer where it was. It was read at the start of this run, "
                                             + "so it has been moved, renamed or deleted since. Nothing more can be done with it.";
                            logMessage(sVanished + " Full path: " + sInput, "ERROR");
                            announce("Error", -1.0, 1.0, sVanished);
                            lFailures.Add(sInput + Environment.NewLine + "    Disappeared while it was being worked on.");
                            bVanished = true;
                            break;
                        }
                        iNoPicture = iNoPicture + 1;
                        // Frames can fail one at a time for ordinary reasons, but
                        // not many in a row: that is the file or the disk, not
                        // this moment.
                        if (iNoPicture >= iDefaultGiveUpAfter)
                        {
                            string sBadly = "Could not take a picture from " + Path.GetFileName(sInput) + " " + iNoPicture.ToString()
                                          + " times in a row, so this film is left. The log has what ffmpeg said.";
                            logMessage(sBadly, "ERROR");
                            announce("Error", -1.0, 1.0, sBadly);
                            lFailures.Add(sInput + Environment.NewLine + "    Frames could not be read from it.");
                            bVanished = true;
                            break;
                        }
                        continue;
                    }
                    iNoPicture = 0;
                    // And the next one is started now, so it is ready by the
                    // time this description is finished.
                    if (iIndex < lGaps.Count)
                    {
                        Moment oNext = lGaps[iIndex];
                        double nNextSpan = Math.Max(oNext.nLength + 2.0, nDefaultLookBack);
                        double nNextCentre = oNext.nStart + oNext.nLength - nNextSpan / 2.0;
                        if (nNextCentre - nNextSpan / 2.0 < 0.0) nNextCentre = nNextSpan / 2.0;
                        montageAheadOfTime(sFfmpeg, sInput, nNextCentre, nNextSpan, sNextImagePath);
                    }
                    if (number("same-shot") > 0.0)
                    {
                        byte[] binNow = shotSignature(sFfmpeg, sImagePath, sWorkDir);
                        double nMoved = signatureDistance(binLast, binNow);
                        bool bQuietTooLong = number("max-silence") > 0.0 && nLastSpoken >= 0.0 && oGap.nStart - nLastSpoken >= number("max-silence");
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
                    sText = describeImage(sImagePath, iLookWords, lShown, sContext, false, bNewScene, "", oGap.bForced, lNames, sJustSaid, false, oGap.nStart);
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
                        sText = describeImage(sImagePath, iMaxWords, lShown, sContext, true, bNewScene, "", oGap.bForced, lNames, sJustSaid, false, oGap.nStart);
                        nLike = worstLikeness(sText, lAgainst);
                    }
                    // One chance to replace a judgment with what was seen.
                    string sJudged = judgmentFound(sText);
                    if (sText != "" && sJudged != "" && flag("objective"))
                    {
                        logMessage("That description judged rather than observed, at the word \"" + sJudged + "\". Asking again.", "INFO", "");
                        string sBetter = describeImage(sImagePath, iMaxWords, lShown, sContext, false, bNewScene, sJudged, oGap.bForced, lNames, sJustSaid, false, oGap.nStart);
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
                        if (sStill != "")
                        {
                            string sPlainer = withoutJudgingPart(sText, sStill);
                            if (sPlainer != sText && judgmentFound(sPlainer) == "")
                            {
                                logMessage("Dropped the part that judged, at \"" + sStill + "\": " + sText, "INFO", "");
                                sText = sPlainer;
                                sStill = "";
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
                        string sSurer = describeImage(sImagePath, iMaxWords, lShown, sContext, false, bNewScene, "", oGap.bForced, lNames, sJustSaid, false, oGap.nStart);
                        if (sSurer != "" && hedgeFound(sSurer) == "") sText = sSurer;
                    }
                }
                if (sText == "" && !flag("rebuild") && number("max-silence") > 0.0 && nLastSpoken >= 0.0 && oGap.nStart - nLastSpoken >= number("max-silence"))
                {
                    logMessage("Nothing said for " + num(oGap.nStart - nLastSpoken) + "s, so this moment is asked again with no leave to skip.", "INFO", "");
                    List<string> lInsistOn = lRecent.GetRange(Math.Max(0, lRecent.Count - iDefaultRecent), Math.Min(iDefaultRecent, lRecent.Count));
                    sText = describeImage(sImagePath, iMaxWords, lInsistOn, sContext, false, bNewScene, "", false, lNames, sJustSaid, true, oGap.nStart);
                    if (flag("summarise") && sText != "") sText = summarise(sText, iMaxWords, lInsistOn);
                }
                if (sText != "")
                {
                    string sNoSubs = withoutSubtitles(sText, sJustSaid);
                    if (sNoSubs != sText)
                    {
                        logMessage("Was: " + sText, "INFO", "");
                        sText = sNoSubs;
                        if (sText == "") logMessage("Nothing was left of that description once the subtitle was taken out.", "INFO", "");
                    }
                }
                if (sText == "")
                {
                    iNothing = iNothing + 1;
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
                if (pcmSeconds(binAudio) > nAllowed)
                {
                    double nOver = pcmSeconds(binAudio) - nAllowed;
                    if (overlapsSpeech(lFilmSpeech, oGap.nStart, oGap.nStart + pcmSeconds(binAudio)))
                    {
                        // It would be spoken across the dialogue, and it could
                        // not be made shorter. Better to say nothing here.
                        iTooLong = iTooLong + 1;
                        logMessage("Dropped: it runs " + num(nOver) + "s past the gap and would be spoken over the dialogue: " + sText, "INFO", "");
                        continue;
                    }
                    logMessage("This description runs " + num(nOver) + "s past the gap, into silence, which is harmless: " + sText, "INFO", "");
                }
                string sGiven = sContext + " " + contextForMoment(oGap.nStart);
                if (contextForMoment(oGap.nStart) != "") iContextUsed = iContextUsed + 1;
                double nEcho = echoOfContext(sText, sGiven);
                if (nEcho >= nDefaultEchoWorry)
                {
                    iContextEchoed = iContextEchoed + 1;
                    logMessage("This description repeats the context heavily (" + num(nEcho * 100.0) + " percent of its words): " + sText,
                               "INFO", "");
                }
                oGap.sText = sText;
                oGap.binAudio = binAudio;
                oGap.nSpoken = pcmSeconds(binAudio);
                nLastSpoken = oGap.nStart;
                if (overlapsSpeech(lFilmSpeech, oGap.nStart, oGap.nStart + oGap.nSpoken)) iOverSpeech = iOverSpeech + 1;
                if (sJustSaid != "") iWithDialogue = iWithDialogue + 1;
                if (oGap.bForced) iForcedDescribed = iForcedDescribed + 1;
                lDone.Add(oGap);
                rememberFilm(lDone);
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
            waitForMontage();
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
                           + iNothing.ToString() + " moments were asked about and produced nothing; "
                           + iTooLong.ToString() + " were dropped because they would not fit their gap and would have covered the dialogue; "
                           + iSkipped.ToString() + " were passed over because the picture had not changed.", "INFO",
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
            List<Speech> lBothWords = lCaptions.Count > 0 ? lCaptions : lFilmSpeech;
            if (flag("transcribe") && lBothWords.Count > 0 && lDone.Count > 0)
            {
                writeScribed(lDone, lBothWords, Path.Combine(sOutputDir, sDefaultBothName), Path.GetFileName(sInput), nDuration);
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
            // Everything worth knowing about this film's descriptions, in one
            // place, so a question about quality does not need the whole log
            // read to answer it.
            if (lDone.Count > 0)
            {
                List<int> lWords = new List<int>();
                List<double> lSaid = new List<double>();
                List<double> lRoom = new List<double>();
                int iJudging = 0;
                int iNamed = 0;
                foreach (Moment oOne in lDone)
                {
                    if (oOne.sText == "") continue;
                    lWords.Add(oOne.sText.Split(' ').Length);
                    lSaid.Add(oOne.nSpoken);
                    lRoom.Add(oOne.nLength);
                    if (judgmentFound(oOne.sText) != "") iJudging = iJudging + 1;
                    foreach (string sName in lNames)
                    {
                        if (sName != "" && oOne.sText.IndexOf(sName, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            iNamed = iNamed + 1;
                            break;
                        }
                    }
                }
                lWords.Sort();
                lSaid.Sort();
                lRoom.Sort();
                if (lWords.Count > 0)
                {
                    logMessage("MEASURE: words per description, middle " + lWords[lWords.Count / 2].ToString()
                               + ", longest " + lWords[lWords.Count - 1].ToString()
                               + "; seconds spoken, middle " + num(lSaid[lSaid.Count / 2])
                               + ", longest " + num(lSaid[lSaid.Count - 1])
                               + "; room available, middle " + num(lRoom[lRoom.Count / 2]) + "s.", "INFO", "");
                    logMessage("MEASURE: context was given to " + iContextUsed.ToString() + " of " + lWords.Count.ToString()
                               + " descriptions from a timed section; " + iContextEchoed.ToString()
                               + " repeat half or more of their words from the context, which would mean reciting rather than describing.", "INFO", "");
                    logMessage("MEASURE: " + iJudging.ToString() + " of " + lWords.Count.ToString()
                               + " still judge rather than observe; " + iNamed.ToString() + " name somebody; "
                               + "the film's memory was rewritten " + (iEstablishedAt / iDefaultRememberEvery).ToString() + " times.", "INFO", "");
                }
            }
            if (bVanished)
            {
                logMessage("Nothing is written for " + Path.GetFileName(sInput) + ", since the run did not finish.", "INFO", "");
                writeFileLog(sOutputDir);
                return 1;
            }
            writeFileLog(sOutputDir);
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
            bSecondInstance = anotherIsRunning();
            if (fLog == null) openLogAt(Path.Combine(appDataFolder(), sDefaultLogName));
            if (bSecondInstance) logMessage("Another HomerScribe is already running, so this one writes its own log and leaves the settings alone.", "INFO", "");
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
