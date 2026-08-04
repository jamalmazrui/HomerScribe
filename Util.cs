// Util.cs -- part of the shared Homer toolkit (namespace Homer).
//
// String utilities, written in Camel Type and ported from Jamal Mazrui's
// HomerLib.vbs so the VBScript and C# Homer libraries share one naming
// convention and behavior. FileDir, EdSharp, and DbDo can all use this
// single implementation instead of each carrying its own copy. Compiled
// directly into the host assembly alongside the other Homer .cs files;
// no COM registration, no strong name, no GAC.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Microsoft.Win32;

namespace Homer {

public static class Util {

// Section break (with form feed) and end-of-document marker, used when
// concatenating documents; formerly Lbc.SB and Lbc.EOD.
public static string sSectionBreak = "\r\n----------\r\n\f\r\n";
public static string sEndOfDocument = "\r\n----------\r\nEnd of Document\r\n";

// Human-readable byte size, e.g. "1.5 K". (Not in HomerLib; kept here
// because FileDir's listings rely on it.)
public static string formatBytes(long lBytes) {
double nValue = 0;
string sSuffix = "";
if (lBytes < 1024d) { nValue = lBytes; sSuffix = ""; }
else if (lBytes < Math.Pow(1024d, 2)) { nValue = lBytes / 1024d; sSuffix = "K"; }
else if (lBytes < Math.Pow(1024d, 3)) { nValue = lBytes / Math.Pow(1024d, 2); sSuffix = "M"; }
else if (lBytes < Math.Pow(1024d, 4)) { nValue = lBytes / Math.Pow(1024d, 3); sSuffix = "G"; }
else if (lBytes < Math.Pow(1024d, 5)) { nValue = lBytes / Math.Pow(1024d, 4); sSuffix = "T"; }
nValue = Math.Round(nValue, 1);
return nValue.ToString() + " " + sSuffix;
} // formatBytes method

// Singular or plural form, depending on whether the count equals one.
// Argument order matches HomerLib's StringPlural(sItem, iCount).
public static string stringPlural(string sItem, int iCount) {
string sReturn = iCount.ToString() + " " + sItem;
if (iCount != 1) sReturn += "s";
return sReturn;
} // stringPlural method

public static string stringQuote(string sText) {
return "\"" + sText + "\"";
} // stringQuote method

public static string stringSingleQuote(string sText) {
return "'" + sText + "'";
} // stringSingleQuote method

public static string stringUnquote(string sText) {
if (sText.Length >= 2 && stringLead(sText, "\"", false) && stringTrail(sText, "\"", false)) return sText.Substring(1, sText.Length - 2);
return sText;
} // stringUnquote method

public static string stringSingleUnquote(string sText) {
if (sText.Length >= 2 && stringLead(sText, "'", false) && stringTrail(sText, "'", false)) return sText.Substring(1, sText.Length - 2);
return sText;
} // stringSingleUnquote method

public static bool stringEqual(string s1, string s2) {
return String.Compare(s1, s2, false) == 0;
} // stringEqual method

public static bool stringEquiv(string s1, string s2) {
return String.Compare(s1, s2, true) == 0;
} // stringEquiv method

public static string stringCapitalize(string sText) {
if (sText.Length > 0) return Char.ToUpper(sText[0]) + sText.Substring(1);
return sText;
} // stringCapitalize method

public static string stringProper(string sText) {
if (sText.Length > 0) return Char.ToUpper(sText[0]) + sText.Substring(1).ToLower();
return sText;
} // stringProper method

public static bool stringContains(string sText, string sMatch, bool bIgnoreCase) {
return sText.IndexOf(sMatch, bIgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) >= 0;
} // stringContains method

public static bool stringLead(string sText, string sLead, bool bIgnoreCase) {
if (sLead.Length > sText.Length) return false;
return String.Compare(sText.Substring(0, sLead.Length), sLead, bIgnoreCase) == 0;
} // stringLead method

public static bool stringTrail(string sText, string sTrail, bool bIgnoreCase) {
if (sTrail.Length > sText.Length) return false;
return String.Compare(sText.Substring(sText.Length - sTrail.Length), sTrail, bIgnoreCase) == 0;
} // stringTrail method

public static bool stringStartsWith(string sText, string sLead, bool bIgnoreCase) {
return stringLead(sText, sLead, bIgnoreCase);
} // stringStartsWith method

public static bool stringEndsWith(string sText, string sTrail, bool bIgnoreCase) {
return stringTrail(sText, sTrail, bIgnoreCase);
} // stringEndsWith method

public static string stringChopLeft(string sText, int iCount) {
iCount = Math.Min(iCount, sText.Length);
return sText.Substring(iCount);
} // stringChopLeft method

public static string stringChopRight(string sText, int iCount) {
iCount = Math.Min(iCount, sText.Length);
return sText.Substring(0, sText.Length - iCount);
} // stringChopRight method

public static string stringPadLeft(string sText, int iLength, string sChar) {
int iPad = 0;
if (iLength > sText.Length) iPad = iLength - sText.Length;
return new String(sChar.Length > 0 ? sChar[0] : ' ', iPad) + sText;
} // stringPadLeft method

public static string stringPadRight(string sText, int iLength, string sChar) {
int iPad = 0;
if (iLength > sText.Length) iPad = iLength - sText.Length;
return sText + new String(sChar.Length > 0 ? sChar[0] : ' ', iPad);
} // stringPadRight method

public static string stringReplaceAll(string sText, string sMatch, string sReplace) {
if (sReplace.IndexOf(sMatch) >= 0) return sText;
string sReturn = sText;
while (sReturn.IndexOf(sMatch) >= 0) sReturn = sReturn.Replace(sMatch, sReplace);
return sReturn;
} // stringReplaceAll method

public static int stringCount(string sText, string sChar) {
return sText.Length - sText.Replace(sChar, "").Length;
} // stringCount method

public static string stringTrimWhiteSpace(string sText) {
return sText.Trim();
} // stringTrimWhiteSpace method

public static string stringConvertToMacLineBreak(string sText) {
sText = sText.Replace("\r\n", "\r");
sText = sText.Replace("\n", "\r");
return sText;
} // stringConvertToMacLineBreak method

public static string stringConvertToUnixLineBreak(string sText) {
sText = sText.Replace("\r\n", "\n");
sText = sText.Replace("\r", "\n");
return sText;
} // stringConvertToUnixLineBreak method

public static string stringConvertToWinLineBreak(string sText) {
sText = sText.Replace("\r\n", "\n");
sText = sText.Replace("\r", "\n");
sText = sText.Replace("\n", "\r\n");
return sText;
} // stringConvertToWinLineBreak method

// Convert common smart punctuation to ASCII. Reconstructed with explicit
// Unicode code points (the original Lbc literals were codepage-mangled).
public static string stringConvertQuotes(string sText) {
string sReturn = sText.Replace("\u201C", "\"");
sReturn = sReturn.Replace("\u201D", "\"");
sReturn = sReturn.Replace("\u2018", "'");
sReturn = sReturn.Replace("\u2019", "'");
sReturn = sReturn.Replace("\u2013", "-");
sReturn = sReturn.Replace("\u2014", "-");
sReturn = sReturn.Replace("\u2026", "...");
sReturn = sReturn.Replace(((char) 65533).ToString(), "'");
return sReturn;
} // stringConvertQuotes method

// ---- COM reflection helpers (late binding), ported from Lbc ----

public static object createObject(string sProgID) {
Type t = Type.GetTypeFromProgID(sProgID);
return Activator.CreateInstance(t);
} // createObject method

public static object objectMethod(string sProgID, string sMethod, object[] aArgs) {
Type t = Type.GetTypeFromProgID(sProgID);
object o = Activator.CreateInstance(t);
return t.InvokeMember(sMethod, BindingFlags.InvokeMethod, null, o, aArgs);
} // objectMethod method

public static object callMethod(object o, string sMethod, object[] aArgs) {
return o.GetType().InvokeMember(sMethod, BindingFlags.InvokeMethod, null, o, aArgs);
} // callMethod method

public static object callMethod(object o, string sMethod) {
return callMethod(o, sMethod, new object[] {});
} // callMethod method

public static object callMethod(object o, string sMethod, string sValue) {
return callMethod(o, sMethod, new object[] {sValue});
} // callMethod method

public static object callMethod(object o, string sMethod, int iValue) {
return callMethod(o, sMethod, new object[] {iValue});
} // callMethod method

public static object setProperty(object o, string sProperty, object[] aArgs) {
return o.GetType().InvokeMember(sProperty, BindingFlags.SetProperty, null, o, aArgs);
} // setProperty method

public static object setProperty(object o, string sProperty, string sValue) {
return setProperty(o, sProperty, new object[] {sValue});
} // setProperty method

public static object setProperty(object o, string sProperty, int iValue) {
return setProperty(o, sProperty, new object[] {iValue});
} // setProperty method

public static object getProperty(object o, string sProperty, object[] aArgs) {
return o.GetType().InvokeMember(sProperty, BindingFlags.GetProperty, null, o, aArgs);
} // getProperty method

public static object getProperty(object o, string sProperty) {
return getProperty(o, sProperty, new object[] {});
} // getProperty method

// ---- File utilities, ported from Lbc ----

public static string file2String(string sFile) {
if (!File.Exists(sFile)) return "";
StreamReader textReader = new StreamReader(sFile);
string sBody = textReader.ReadToEnd();
textReader.Close();
return sBody;
} // file2String method

public static void string2File(string sBody, string sFile) {
StreamWriter textWriter = new StreamWriter(sFile);
textWriter.Write(sBody);
textWriter.Close();
} // string2File method

public static string getUniqueName(string sSource) {
if (!Directory.Exists(sSource) && !File.Exists(sSource)) return sSource;
string sTarget = "";
string sDir = Path.GetDirectoryName(sSource);
string sRoot = Regex.Replace(Path.GetFileNameWithoutExtension(sSource), @"_\d\d$", "");
string sExt = Path.GetExtension(sSource);
for (int i = 1; i < 100; i++) {
sTarget = Path.Combine(sDir, sRoot + "_" + i.ToString().PadLeft(2, '0') + sExt);
if (!Directory.Exists(sTarget) && !File.Exists(sTarget)) break;
}
if (Directory.Exists(sTarget) || File.Exists(sTarget)) sTarget = "";
return sTarget;
} // getUniqueName method

public static string getUniqueFileName(string sSource) {
if (!Directory.Exists(sSource) && !File.Exists(sSource)) return sSource;
string sTarget = "";
string sDir = Path.GetDirectoryName(sSource);
string sRoot = Regex.Replace(Path.GetFileNameWithoutExtension(sSource), @"_\d\d$", "");
string sExt = Path.GetExtension(sSource);
for (int i = 1; i < 1000; i++) {
sTarget = Path.Combine(sDir, sRoot + "_" + i.ToString().PadLeft(3, '0') + sExt);
if (!Directory.Exists(sTarget) && !File.Exists(sTarget)) break;
}
return sTarget;
} // getUniqueFileName method

public static string getLegalFileRoot(string sRoot) {
StringBuilder sb = new StringBuilder();
for (int i = 0; i < sRoot.Length; i++) {
if (Char.IsLetterOrDigit(sRoot, i)) sb.Append(sRoot.Substring(i, 1));
else sb.Append("_");
}
string sReturn = Regex.Replace(sb.ToString(), @"_+", "_");
sReturn = sReturn.Trim(new char[] {'_', ' '});
sReturn = sReturn.Replace("_", " ").Trim();
return sReturn;
} // getLegalFileRoot method

public static string getExtensions(string[] aFiles) {
List<string> ls = new List<string>(aFiles.Length);
foreach (string sFile in aFiles) {
string sExt = Path.GetExtension(sFile).TrimStart('.').ToLower();
if (sExt.Length > 0 && !ls.Contains(sExt)) ls.Add(sExt);
}
ls.Sort();
return String.Join(" ", ls.ToArray());
} // getExtensions method

public static string[] getFilesWithExtensions(string[] aFiles, string sExtensions) {
string[] aResults = ("." + sExtensions.Trim().Replace(" ", " .")).Split(' ');
List<string> ls = new List<string>(aFiles);
for (int i = ls.Count - 1; i >= 0; i--) {
if (Array.IndexOf(aResults, Path.GetExtension(ls[i]).ToLower()) == -1) ls.RemoveAt(i);
}
return ls.ToArray();
} // getFilesWithExtensions method

public static string getPathType(string sPath) {
if (Directory.Exists(sPath)) return "Folder";
object oSystem = createObject("Scripting.FileSystemObject");
object oFile = callMethod(oSystem, "GetFile", new object[] {sPath});
return (string) getProperty(oFile, "Type");
} // getPathType method

public static string getLfn(string sPath) {
object oShell = createObject("WScript.Shell");
object oShortcut = callMethod(oShell, "CreateShortcut", "temp.lnk");
setProperty(oShortcut, "TargetPath", sPath);
return (string) getProperty(oShortcut, "TargetPath");
} // getLfn method

[DllImport("kernel32.dll", CharSet = CharSet.Auto, EntryPoint = "GetShortPathName")]
static extern int getShortPathName(string sPath, StringBuilder sbShort, int iLength);

public static string getShortPath(string sLfn) {
StringBuilder sbShort = new StringBuilder(255);
getShortPathName(sLfn, sbShort, sbShort.Capacity);
return sbShort.ToString();
} // getShortPath method


// ---- Character encodings (replaces the Encoding.exe utility) ----
//
// Encoding.exe offered three things this code reproduces: detection of a file's
// encoding, a list of encodings to choose from, and conversion with an optional
// .bak backup.  Two of its conventions are kept because they matter in practice
// and .NET has no name for either:
//
//   utf-8b   UTF-8 WITH a byte-order mark   (what most Windows programs expect)
//   utf-8n   UTF-8 with NO byte-order mark  (the norm on Linux and the Mac)
//
// and one pseudo-encoding:
//
//   asciify  reduce to ASCII, transliterating what can be transliterated -- an
//            accented letter loses its accent, a curly quote becomes a straight
//            one, an ellipsis becomes three periods -- rather than dropping the
//            character outright.

public static string[] getEncodingNames() {
// The names offered to the user: the two UTF-8 conventions and asciify first,
// then every encoding this computer actually has.
List<string> lsNames = new List<string>();
lsNames.Add("asciify");
lsNames.Add("utf-8b");
lsNames.Add("utf-8n");
foreach (EncodingInfo encodingInfo in Encoding.GetEncodings()) lsNames.Add(encodingInfo.Name.ToLower());
lsNames.Sort(StringComparer.OrdinalIgnoreCase);
return lsNames.ToArray();
} // getEncodingNames method

public static Encoding getEncodingByName(string sName) {
// Turn a name from the list above into an Encoding.  Returns null for "asciify",
// which is not an encoding but a transformation the caller applies first.
string sKey = sName.Trim().ToLower().Replace("-", "").Replace("_", "");
if (sKey == "asciify") return null;
if (sKey == "utf8b" || sKey == "utf8sig") return new UTF8Encoding(true);
if (sKey == "utf8n") return new UTF8Encoding(false);
if (sKey == "utf8") return new UTF8Encoding(true);   // Windows convention: with BOM
return Encoding.GetEncoding(sName.Trim());
} // getEncodingByName method

public static string getFileEncodingName(string sFile) {
// The name to REPORT for a file, using the utf-8b / utf-8n distinction so the
// answer says whether a byte-order mark is present -- which is exactly what the
// user needs to know, and what a bare "utf-8" would hide.
Encoding en = getFileEncoding(sFile);
if (en is UTF8Encoding) return hasBom(sFile) ? "utf-8b" : "utf-8n";
return en.WebName.ToLower();
} // getFileEncodingName method

public static bool hasBom(string sFile) {
try {
byte[] aBom = new byte[3];
using (FileStream fs = new FileStream(sFile, FileMode.Open, FileAccess.Read, FileShare.Read)) { fs.Read(aBom, 0, 3); }
return (aBom[0] == 0xEF && aBom[1] == 0xBB && aBom[2] == 0xBF);
}
catch { return false; }
} // hasBom method

public static string asciify(string sText) {
// Reduce text to ASCII, transliterating rather than discarding wherever possible.
// Decomposing to NFD splits an accented letter into its base letter plus a
// combining mark, so dropping the marks leaves the base letter ("e" for an
// e-acute).  A few punctuation characters have no decomposition and are mapped by
// hand.  Anything still above ASCII after that is dropped, as Encoding.exe did.
if (sText == null || sText.Length == 0) return "";
sText = sText.Replace("\u2018", "'").Replace("\u2019", "'");
sText = sText.Replace("\u201C", "\"").Replace("\u201D", "\"");
sText = sText.Replace("\u2013", "-").Replace("\u2014", "--");
sText = sText.Replace("\u2026", "...");
sText = sText.Replace("\u00A0", " ");
sText = sText.Replace("\u00AB", "<<").Replace("\u00BB", ">>");
sText = sText.Replace("\u00E6", "ae").Replace("\u00C6", "AE");
sText = sText.Replace("\u00F8", "o").Replace("\u00D8", "O");
sText = sText.Replace("\u00DF", "ss");
sText = sText.Replace("\u20AC", "EUR").Replace("\u00A3", "GBP");
string sNormal = sText.Normalize(NormalizationForm.FormD);
StringBuilder sb = new StringBuilder(sNormal.Length);
foreach (char c in sNormal) {
if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
if (c < 128) sb.Append(c);
}
return sb.ToString();
} // asciify method

public static void convertFileEncoding(string sFile, string sTargetName, bool bBackup) {
// Rewrite sFile in the named encoding.  The source encoding is DETECTED, not
// assumed, so a file is read correctly whatever it is in.  With bBackup, the
// original is kept alongside as <name>.bak -- the behavior of Encoding.exe's
// "backup" task, which is the one FileDir used.
Encoding enFrom = getFileEncoding(sFile);
string sBody = File.ReadAllText(sFile, enFrom);
Encoding enTo = getEncodingByName(sTargetName);
if (enTo == null) {                 // asciify
sBody = asciify(sBody);
enTo = Encoding.ASCII;
}
if (bBackup) {
string sBak = sFile + ".bak";
if (File.Exists(sBak)) File.Delete(sBak);
File.Copy(sFile, sBak);
}
File.WriteAllText(sFile, sBody, enTo);
} // convertFileEncoding method

// Determine a text file's encoding: a byte-order mark if there is one, otherwise
// content-based detection.  The .NET base class library has no charset detector,
// so detection uses the Ude library (a port of the Mozilla universal detector)
// when Ude.dll is present at build time -- the HAVEUDE symbol.  This is what the
// retired Encoding.exe did, and EdSharp already uses the same library, so all the
// apps now detect encodings the same way.  Without Ude.dll the result degrades to
// Encoding.Default, which is the old BOM-only behavior.
public static Encoding getFileEncoding(string sFile) {
try {
byte[] aBom = new byte[4];
using (FileStream fs = new FileStream(sFile, FileMode.Open, FileAccess.Read, FileShare.Read)) { fs.Read(aBom, 0, 4); }
if (aBom[0] == 0xEF && aBom[1] == 0xBB && aBom[2] == 0xBF) return Encoding.UTF8;
if (aBom[0] == 0x00 && aBom[1] == 0x00 && aBom[2] == 0xFE && aBom[3] == 0xFF) return new UTF32Encoding(true, true);
if (aBom[0] == 0xFF && aBom[1] == 0xFE && aBom[2] == 0x00 && aBom[3] == 0x00) return Encoding.UTF32;
if (aBom[0] == 0xFF && aBom[1] == 0xFE) return Encoding.Unicode;
if (aBom[0] == 0xFE && aBom[1] == 0xFF) return Encoding.BigEndianUnicode;
}
catch {}
return detectEncodingNoBom(sFile);
} // getFileEncoding method

public static Encoding detectEncodingNoBom(string sFile) {
// Content-based detection for a file with no byte-order mark.  A clearly detected
// legacy or wide encoding (windows-1252, UTF-16 without a BOM, Shift-JIS, ...) is
// honored, so the file is read -- and later saved -- without corruption.
Encoding enDefault = Encoding.Default;
#if HAVEUDE
try {
byte[] aBytes = File.ReadAllBytes(sFile);
if (aBytes.Length == 0) return enDefault;
Ude.CharsetDetector charsetDetector = new Ude.CharsetDetector();
charsetDetector.Feed(aBytes, 0, aBytes.Length);
charsetDetector.DataEnd();
string sCharset = charsetDetector.Charset;
if (String.IsNullOrEmpty(sCharset)) return enDefault;
return charsetName2Encoding(sCharset, enDefault);
}
catch { return enDefault; }
#else
return enDefault;
#endif
} // detectEncodingNoBom method

public static Encoding charsetName2Encoding(string sName, Encoding enDefault) {
// Map a detector charset name to a .NET Encoding.  Unknown names fall back to the
// caller's default rather than throwing.
string sKey = sName.Trim().Replace("-", "").Replace("_", "").ToLower();
if (sKey == "ascii" || sKey == "usascii") return enDefault;
if (sKey == "utf8") return new UTF8Encoding(false);
if (sKey == "utf16le" || sKey == "utf16" || sKey == "unicode") return Encoding.Unicode;
if (sKey == "utf16be") return Encoding.BigEndianUnicode;
if (sKey == "utf32" || sKey == "utf32le") return Encoding.UTF32;
if (sKey == "utf32be") return new UTF32Encoding(true, false);
try { return Encoding.GetEncoding(sName); }
catch { }
return enDefault;
} // charsetName2Encoding method

// ---- Shell / process, ported from Lbc ----

[DllImport("shell32.dll", EntryPoint = "ShellExecute")]
static extern int shellExecute(int i1, int i2, string sFile, int i3, int i4, int i5);

public static int shellDefault(string sFile) {
return shellExecute(0, 0, sFile, 0, 0, 1);
} // shellDefault method

public static int run(string sPath) {
return Interaction.Shell(sPath, AppWinStyle.NormalFocus, false, -1);
} // run method

public static int runWait(string sPath) {
return Interaction.Shell(sPath, AppWinStyle.NormalFocus, true, -1);
} // runWait method

public static int runHide(string sPath) {
return Interaction.Shell(sPath, AppWinStyle.Hide, false, -1);
} // runHide method

public static int runHideWait(string sPath) {
return Interaction.Shell(sPath, AppWinStyle.Hide, true, -1);
} // runHideWait method

public static string getProgramOutput(string sExe, string sParams) {
Process process = new Process();
ProcessStartInfo startInfo = new ProcessStartInfo(sExe, sParams);
startInfo.UseShellExecute = false;
startInfo.RedirectStandardOutput = true;
startInfo.CreateNoWindow = true;
startInfo.WindowStyle = ProcessWindowStyle.Hidden;
process.StartInfo = startInfo;
process.Start();
string sOutput = process.StandardOutput.ReadToEnd();
process.WaitForExit();
process.Close();
return sOutput;
} // getProgramOutput method

public static void invokeVerb(string sPath, string sVerb) {
object o = createObject("Shell.Application");
o = callMethod(o, "Namespace", new object[] {Path.GetDirectoryName(sPath)});
o = callMethod(o, "ParseName", new object[] {Path.GetFileName(sPath)});
o = callMethod(o, "InvokeVerb", new object[] {sVerb});
} // invokeVerb method

public static void properties(string sPath) {
invokeVerb(sPath, "P&roperties");
invokeVerb(sPath, "Properties");
} // properties method

public static string[] verbs(string sPath) {
object o = createObject("Shell.Application");
o = callMethod(o, "Namespace", new object[] {Path.GetDirectoryName(sPath)});
o = callMethod(o, "ParseName", new object[] {Path.GetFileName(sPath)});
try {
o = callMethod(o, "Verbs", new object[] {});
}
catch {
return new string[] {};
}
int iCount = (int) getProperty(o, "Count");
StringBuilder sb = new StringBuilder();
for (int i = 0; i < iCount; i++) {
object oVerb = callMethod(o, "Item", new object[] {(int) i});
string sVerb = (string) getProperty(oVerb, "Name");
if (sVerb.Trim() != "") sb.Append(sVerb + "\n");
}
return sb.ToString().Trim().Split('\n');
} // verbs method

public static string findOnPath(string sName) {
string sPaths = Directory.GetCurrentDirectory() + ";" + Environment.GetEnvironmentVariable("PATH");
foreach (string sDir in sPaths.Split(';')) {
string sPath = sDir.Trim(new char[] {'"', ' '});
if (sPath.Length == 0) continue;
string sFile = Path.Combine(sPath, sName);
if (File.Exists(sFile)) return sFile;
}
return "";
} // findOnPath method

[DllImport("Shell32.dll", EntryPoint = "SHFormatDrive")]
static extern int shFormatDrive(IntPtr h, int iDrive, int iFormatId, int iFlags);

public static int formatDrive(IntPtr h, string sDrive) {
string sDrives = "abcdefghijklmnopqrstuvwxyz";
int iDrive = sDrives.IndexOf(sDrive.Substring(0, 1).ToLower());
return shFormatDrive(h, iDrive, 0, 0);
} // formatDrive method

// ---- Window helpers (P/Invoke), ported from Lbc ----

[DllImport("user32.dll", EntryPoint = "GetForegroundWindow")]
public static extern int getForegroundWindow();

[DllImport("user32.dll", EntryPoint = "SetForegroundWindow")]
public static extern int setForegroundWindow(int iHandle);

[DllImport("user32.dll", EntryPoint = "AttachThreadInput")]
static extern int attachThreadInput(int iThread1, int iThread2, int iAttach);

[DllImport("user32.dll", EntryPoint = "GetActiveWindow")]
static extern IntPtr getActiveWindow();

[DllImport("user32.dll", EntryPoint = "BringWindowToTop")]
static extern int bringWindowToTop(IntPtr h);

[DllImport("user32.dll", EntryPoint = "ShowWindow")]
static extern int showWindow(IntPtr h, int iState);

[DllImport("kernel32.dll", EntryPoint = "GetCurrentThreadId")]
static extern int getCurrentThreadId();

[DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId")]
static extern int getWindowThreadProcessId(IntPtr h, int iProcess);

public static bool forceWindow(IntPtr h) {
int iForegroundThread = getWindowThreadProcessId((IntPtr) getForegroundWindow(), 0);
int iAppThread = getCurrentThreadId();
if (iForegroundThread == iAppThread) {
bringWindowToTop(h);
showWindow(h, 3);
}
else {
attachThreadInput(iForegroundThread, iAppThread, 1);
bringWindowToTop(h);
showWindow(h, 3);
attachThreadInput(iForegroundThread, iAppThread, 0);
}
return getActiveWindow() == h;
} // forceWindow method

public static void activateTitle(string sTitle) {
object oShell = createObject("WScript.Shell");
callMethod(oShell, "AppActivate", sTitle);
} // activateTitle method

// ---- Clipboard, ported from Lbc ----

public static void path2Clipboard(string[] aPaths, bool bCut) {
IDataObject data = new DataObject(DataFormats.FileDrop, aPaths);
MemoryStream memo = new MemoryStream(4);
byte[] aBytes = bCut ? new byte[] {2, 0, 0, 0} : new byte[] {5, 0, 0, 0};
memo.Write(aBytes, 0, aBytes.Length);
data.SetData("Preferred DropEffect", memo);
data.SetData(String.Join("\r\n", aPaths).Trim());
Clipboard.SetDataObject(data, true);
} // path2Clipboard method

public static bool bytesCompare(byte[] a1, byte[] a2) {
if (a1.Length != a2.Length) return false;
for (int i = 0; i < a1.Length; i++) if (a1[i] != a2[i]) return false;
return true;
} // bytesCompare method

public static string[] clipboard2Path(out bool bCut) {
bCut = false;
if (!Clipboard.ContainsFileDropList()) return new string[] {};
IDataObject data = Clipboard.GetDataObject();
string[] aPaths = (string[]) data.GetData(DataFormats.FileDrop, true);
MemoryStream stream = (MemoryStream) data.GetData("Preferred DropEffect", true);
byte[] aBytes = new byte[] {(byte) stream.ReadByte(), 0, 0, 0};
bCut = bytesCompare(aBytes, new byte[] {2, 0, 0, 0});
return aPaths;
} // clipboard2Path method

public static void path2Link(string sPath, string sLink) {
if (sLink == "") sLink = sPath + ".lnk";
object o = createObject("WScript.Shell");
o = callMethod(o, "CreateShortcut", new object[] {sLink});
setProperty(o, "TargetPath", new object[] {sPath});
setProperty(o, "WorkingDirectory", new object[] {Path.GetDirectoryName(sPath)});
setProperty(o, "WindowStyle", new object[] {1});
callMethod(o, "Save", new object[] {});
} // path2Link method

// ---- URI / web, ported from Lbc ----

public static string getUrl() {
// Return a web address to offer as a default, or "" if none is available.
//
// This used to enumerate the Shell.Application Windows collection and read
// Internet Explorer's address bar.  Internet Explorer no longer exists on
// Windows, so that returned nothing useful.  The clipboard is the replacement
// and works with every browser: copy a link, then run the command.
string sText = "";
try {
if (System.Windows.Forms.Clipboard.ContainsText()) sText = System.Windows.Forms.Clipboard.GetText();
}
catch { return ""; }
if (sText == null) return "";
sText = sText.Trim();
int iBreak = sText.IndexOfAny(new char[] {'\r', '\n'});
if (iBreak >= 0) sText = sText.Substring(0, iBreak).Trim();
if (sText.Length == 0) return "";
if (sText.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) return sText;
if (sText.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return sText;
if (sText.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase)) return sText;
if (sText.StartsWith("www.", StringComparison.OrdinalIgnoreCase)) return "http://" + sText;
return "";
} // getUrl method

public static string getFileFromUri(string sUri) {
Uri oUri = new Uri(sUri);
string sFile = oUri.LocalPath;
try { sFile = Path.GetFileName(sFile); }
catch { sFile = ""; }
if (sFile.Length == 0) {
sFile = Uri.UnescapeDataString(oUri.PathAndQuery);
StringBuilder sb = new StringBuilder();
for (int i = 0; i < sFile.Length; i++) {
if (Char.IsLetterOrDigit(sFile, i)) sb.Append(sFile.Substring(i, 1));
else sb.Append("_");
}
sFile = Regex.Replace(sb.ToString(), @"_+", "_");
sFile = sFile.Trim(new char[] {'_', ' '});
if (sFile.Length == 0) sFile = "page";
if (!sFile.ToLower().EndsWith(".htm") && !sFile.ToLower().EndsWith(".html")) sFile += ".htm";
}
if (Path.GetExtension(sFile).Length == 0) sFile += ".htm";
return sFile.Replace("_", " ").Trim();
} // getFileFromUri method

public static string[] getFtpDir(string sUserName, string sPassword, string sUrl) {
FtpWebRequest request = (FtpWebRequest) FtpWebRequest.Create(sUrl);
request.Credentials = new NetworkCredential(sUserName, sPassword);
request.Proxy = new WebProxy();
request.Method = WebRequestMethods.Ftp.ListDirectory;
FtpWebResponse response = (FtpWebResponse) request.GetResponse();
StreamReader reader = new StreamReader(response.GetResponseStream());
StringBuilder sb = new StringBuilder();
string sLine = "";
while ((sLine = reader.ReadLine()) != null) sb.Append(sLine + "\n");
reader.Close();
return sb.ToString().Trim().Split('\n');
} // getFtpDir method

// ---- INI config (Win32), ported from Lbc ----

[DllImport("kernel32.dll", EntryPoint = "GetPrivateProfileString")]
static extern int getPrivateProfileString(string sAppName, string sKeyName, string sDefault, StringBuilder sbReturn, int iSize, string sFileName);

[DllImport("kernel32.dll", EntryPoint = "WritePrivateProfileString")]
static extern bool writePrivateProfileString(string sAppName, string sKeyName, string sString, string sFileName);

public static string readValue(string sFile, string sSection, string sKey, string sDefault) {
StringBuilder sbBuffer = new StringBuilder(256);
if (getPrivateProfileString(sSection, sKey, sDefault, sbBuffer, sbBuffer.Capacity, sFile) != 0) return sbBuffer.ToString();
return sDefault;
} // readValue method

public static bool writeValue(string sFile, string sSection, string sKey, string sValue) {
return writePrivateProfileString(sSection, sKey, sValue, sFile);
} // writeValue method

// ---- JAWS PATH init, ported from Lbc ----

public static string getRegString(RegistryKey key, string sSubKey, string sName) {
RegistryKey subkey = null;
string sData = "";
try {
subkey = key.OpenSubKey(sSubKey);
sData = subkey.GetValue(sName).ToString();
}
catch {}
finally {
if (subkey != null) subkey.Close();
}
return sData;
} // getRegString method

public static string getJfwPath() {
RegistryKey key = Registry.LocalMachine;
string sSubKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\";
string sPath = getRegString(key, sSubKey + "jfw.exe", "Path");
if (sPath == "") {
string[] aVersion = {"12", "11", "10", "90", "81", "80", "8", "71", "70", "7", "62", "61", "60", "6"};
foreach (string sVersion in aVersion) {
sPath = getRegString(key, sSubKey + "jaws" + sVersion + ".exe", "");
if (sPath != "") {
sPath = Path.GetDirectoryName(sPath);
break;
}
}
}
if (sPath != "" && !sPath.EndsWith(@"\")) sPath = String.Concat(sPath, @"\");
return sPath;
} // getJfwPath method

public static bool initJfw() {
string sDir = getJfwPath();
if (sDir.Length == 0) return false;
string sPath = Environment.GetEnvironmentVariable("PATH");
sDir += ";";
if (!sPath.ToLower().Contains(sDir.ToLower())) Environment.SetEnvironmentVariable("PATH", sDir + sPath);
return true;
} // initJfw method

} // class Util

} // namespace Homer
