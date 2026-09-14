// PdfRead.cs -- the PdfPig half of reading a PDF.
//
// SEPARATE FROM HomerScribe.cs ON PURPOSE. This is the only code in the
// program that depends on a NuGet package, and it is the only code I could not
// compile against that package before delivering it. Keeping it in its own
// file means the build can leave it out when PdfPig is not present, and a
// mistake in here cannot stop HomerScribe building at all.
//
// buildHomerScribe.cmd includes this file only when PdfPig.dll is beside it.
// Without it, HomerScribe behaves exactly as it did before: a PDF is explained
// rather than read.
//
// PdfPig is Apache 2.0, .NET Standard, and supports .NET back to 4.5. It hands
// back letters WITH POSITIONS AND FONT SIZES rather than a string, which is
// what makes the heading inference in HomerScribe.cs possible.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace Homer
{
    public partial class HomerScribe
    {
        // Every page's lines and pictures, taken out of the file.
        //
        // Letters are grouped into lines by their vertical position, because
        // PdfPig gives letters and a line is what a reader needs. Two letters
        // within a third of their own height are on the same line; that
        // tolerance is what keeps a subscript with its paragraph and a
        // superscript off its own line.

        // Words, grouped into blocks, put in the order a person reads them.
        //
        // PdfPig's own layout analysis: NearestNeighbourWordExtractor builds
        // words from letters by spacing, DocstrumBoundingBoxes groups them into
        // blocks, and UnsupervisedReadingOrderDetector orders the blocks. All
        // three are better tested than anything written here, and the last is
        // the one that makes a two-column page read down each column rather
        // than across the gutter.
        static bool readInOrder(Page oPage, PdfPage oOut)
        {
            try
            {
                // GetWords hands back an IEnumerable, not a list. The compiler
                // said so plainly:
                //   error CS0266: Cannot implicitly convert type
                //   'IEnumerable<Word>' to 'IReadOnlyList<Word>'
                // Taking a list of it once is better than leaving it lazy,
                // since it is walked more than once below.
                List<Word> lWords = new List<Word>(oPage.GetWords(NearestNeighbourWordExtractor.Instance));
                if (lWords.Count == 0) return false;
                List<TextBlock> lBlocks = new List<TextBlock>(DocstrumBoundingBoxes.Instance.GetBlocks(lWords));
                if (lBlocks.Count == 0) return false;
                IEnumerable<TextBlock> lOrdered =
                    UnsupervisedReadingOrderDetector.Instance.Get(lBlocks);
                if (lOrdered == null) return false;
                foreach (TextBlock oBlock in lOrdered)
                {
                    foreach (TextLine oTextLine in oBlock.TextLines)
                    {
                        PageLine oLine = new PageLine();
                        oLine.sText = oTextLine.Text;
                        oLine.nTop = oTextLine.BoundingBox.Bottom;
                        double nSize = 0.0;
                        int iCount = 0;
                        string sFont = "";
                        bool bSawBold = false;
                        bool bSawItalic = false;
                        foreach (Word oWord in oTextLine.Words)
                        {
                            foreach (Letter oLetter in oWord.Letters)
                            {
                                nSize = nSize + oLetter.PointSize;
                                iCount = iCount + 1;
                                // FontDetails, not Font: the older name is obsolete, and this
                                // one carries IsBold and IsItalic outright rather than
                                // leaving them to be read out of the typeface name.
                                if (sFont == "" && oLetter.FontDetails != null)
                                {
                                    sFont = oLetter.FontDetails.Name;
                                    if (oLetter.FontDetails.IsBold) bSawBold = true;
                                    if (oLetter.FontDetails.IsItalic) bSawItalic = true;
                                }
                            }
                        }
                        oLine.nSize = iCount == 0 ? 0.0 : nSize / iCount;
                        if (sFont == null) sFont = "";
                        sFont = sFont.ToLower();
                        // The font's own flags first, since it states what the
                        // name only implies; the name is still read, because
                        // plenty of PDFs set neither flag and say "Times-Bold".
                        oLine.bBold = bSawBold || sFont.Contains("bold") || sFont.Contains("black")
                                    || sFont.Contains("heavy") || sFont.Contains("semibold");
                        oLine.bItalic = bSawItalic || sFont.Contains("italic") || sFont.Contains("oblique");
                        if (oLine.sText != null && oLine.sText.Trim() != "") oOut.lLines.Add(oLine);
                    }
                }
                return oOut.lLines.Count > 0;
            }
            catch (Exception)
            {
                // A page the analysis cannot handle falls back below rather
                // than taking the whole document down.
                return false;
            }
        }

        // The older way: letters sorted by position and grouped into lines.
        // Kept because a page read badly beats a page not read at all.
        static bool readByPosition(Page oPage, PdfPage oOut)
        {
            List<Letter> lLetters = new List<Letter>();
            foreach (Letter oLetter in oPage.Letters) lLetters.Add(oLetter);
            lLetters.Sort(delegate(Letter oLeft, Letter oRight)
            {
                int iSame = oRight.BoundingBox.Bottom.CompareTo(oLeft.BoundingBox.Bottom);
                if (iSame != 0) return iSame;
                return oLeft.BoundingBox.Left.CompareTo(oRight.BoundingBox.Left);
            });
            PageLine oLine = null;
            double nLastBottom = double.NaN;
            foreach (Letter oLetter in lLetters)
            {
                double nBottom = oLetter.BoundingBox.Bottom;
                double nHeight = Math.Max(1.0, oLetter.BoundingBox.Height);
                bool bNewLine = oLine == null
                              || double.IsNaN(nLastBottom)
                              || Math.Abs(nBottom - nLastBottom) > nHeight / 3.0;
                if (bNewLine)
                {
                    if (oLine != null && oLine.sText.Trim() != "") oOut.lLines.Add(oLine);
                    oLine = new PageLine();
                    oLine.nTop = nBottom;
                    oLine.nSize = oLetter.PointSize;
                    // Bold and italic, taken from the font's own name.
                    //
                    // He wants emphasis kept and font CHOICES and SIZES discarded: a
                    // size decides a heading level and then has no further business
                    // in the Markdown, and a typeface name has none at any point. A
                    // PDF names its fonts "Times-BoldItalic" or "Arial,Bold", so the
                    // name carries the emphasis and nothing else is needed.
                    string sFont = "";
                    try
                    {
                        if (oLetter.FontDetails != null)
                            {
                                sFont = oLetter.FontDetails.Name;
                                if (oLetter.FontDetails.IsBold) oLine.bBold = true;
                                if (oLetter.FontDetails.IsItalic) oLine.bItalic = true;
                            }
                    }
                    catch (Exception)
                    {
                    }
                    if (sFont == null) sFont = "";
                    sFont = sFont.ToLower();
                    oLine.bBold = sFont.Contains("bold") || sFont.Contains("black")
                                || sFont.Contains("heavy") || sFont.Contains("semibold");
                    oLine.bItalic = sFont.Contains("italic") || sFont.Contains("oblique");
                }
                oLine.sText = oLine.sText + oLetter.Value;
                // The size of a line is the size of most of it, so
                // a capital dropped cap does not make a heading of
                // a paragraph.
                if (oLetter.PointSize > 0 && oLine.sText.Length > 3)
                    oLine.nSize = (oLine.nSize * 3.0 + oLetter.PointSize) / 4.0;
                nLastBottom = nBottom;
            }
            if (oLine != null && oLine.sText.Trim() != "") oOut.lLines.Add(oLine);


            return oOut.lLines.Count > 0;
        }

        // Do these bytes already begin like a picture?
        static bool looksLikeAnImage(byte[] aBytes)
        {
            if (aBytes == null || aBytes.Length < 12) return false;
            if (aBytes[0] == 0xFF && aBytes[1] == 0xD8 && aBytes[2] == 0xFF) return true;          // JPEG
            if (aBytes[0] == 0x89 && aBytes[1] == 0x50 && aBytes[2] == 0x4E && aBytes[3] == 0x47)  // PNG
                return true;
            if (aBytes[4] == 0x6A && aBytes[5] == 0x50) return true;                               // JPEG 2000
            return false;
        }

        // An ASCII85 stream, decoded. The bytes back unchanged if it was not one.
        //
        // ASCII85 packs four bytes into five printable characters, "z" stands
        // for four zero bytes, and "~>" ends it. Whitespace is ignored, which
        // matters because a PDF wraps these streams at 80 characters.
        static byte[] unAscii85(byte[] aBytes)
        {
            if (aBytes == null || aBytes.Length < 8) return aBytes;
            try
            {
                List<byte> lOut = new List<byte>();
                uint iGroup = 0;
                int iHave = 0;
                foreach (byte iAt in aBytes)
                {
                    char cAt = (char)iAt;
                    if (cAt == '~') break;
                    if (char.IsWhiteSpace(cAt)) continue;
                    if (cAt == 'z' && iHave == 0)
                    {
                        lOut.Add(0); lOut.Add(0); lOut.Add(0); lOut.Add(0);
                        continue;
                    }
                    if (cAt < '!' || cAt > 'u') return aBytes;      // not ASCII85 at all
                    iGroup = iGroup * 85 + (uint)(cAt - '!');
                    iHave = iHave + 1;
                    if (iHave != 5) continue;
                    lOut.Add((byte)(iGroup >> 24)); lOut.Add((byte)(iGroup >> 16));
                    lOut.Add((byte)(iGroup >> 8)); lOut.Add((byte)iGroup);
                    iGroup = 0;
                    iHave = 0;
                }
                if (iHave > 1)
                {
                    // A part group is padded with 'u' and the padding dropped.
                    for (int iPad = iHave; iPad < 5; iPad = iPad + 1) iGroup = iGroup * 85 + 84;
                    byte[] aTail = new byte[] { (byte)(iGroup >> 24), (byte)(iGroup >> 16),
                                                (byte)(iGroup >> 8), (byte)iGroup };
                    for (int iAdd = 0; iAdd < iHave - 1; iAdd = iAdd + 1) lOut.Add(aTail[iAdd]);
                }
                byte[] aGot = lOut.ToArray();
                return aGot.Length > 8 ? aGot : aBytes;
            }
            catch (Exception)
            {
                return aBytes;
            }
        }

        // A deflated stream, inflated. Empty where it was not one.
        static byte[] inflated(byte[] aBytes)
        {
            if (aBytes == null || aBytes.Length < 3) return aBytes;
            // A zlib stream starts 0x78; raw deflate does not. Try both, since
            // a PDF may carry either.
            int[] aSkip = aBytes[0] == 0x78 ? new int[] { 2, 0 } : new int[] { 0, 2 };
            foreach (int iSkip in aSkip)
            {
                try
                {
                    using (MemoryStream oIn = new MemoryStream(aBytes, iSkip, aBytes.Length - iSkip))
                    using (DeflateStream oOut = new DeflateStream(oIn, CompressionMode.Decompress))
                    using (MemoryStream oGot = new MemoryStream())
                    {
                        byte[] aChunk = new byte[65536];
                        int iRead = 0;
                        while ((iRead = oOut.Read(aChunk, 0, aChunk.Length)) > 0)
                            oGot.Write(aChunk, 0, iRead);
                        byte[] aOut = oGot.ToArray();
                        if (aOut.Length > 0 && looksLikeAnImage(aOut)) return aOut;
                    }
                }
                catch (Exception)
                {
                }
            }
            return aBytes;
        }

        // IS THIS PDF TAGGED?
        //
        // A tagged PDF carries its own structure: real headings with real
        // levels, lists that know they are lists, tables that mark which cells
        // are headers, alt text on every figure, and a reading order the author
        // set rather than one inferred from where the ink fell.
        //
        // Everything HomerScribe infers -- font size into heading level, length
        // into heading or paragraph, word height on a scan -- is a
        // reconstruction of what a tagged PDF simply states. Where the tags are
        // there, they beat the inference every time, because they are what the
        // author meant rather than what the layout suggests.
        //
        // This only REPORTS it so far. Reading the tags is the next piece of
        // work, and saying whether they exist is worth having on its own: a
        // lawyer handed a brief can be told whether it was made accessible or
        // merely printed to PDF.
        public static bool bTaggedDocument = false;
        public static string sTaggedSaid = "";

        static bool documentIsTagged(PdfDocument oDoc, out string sHow)
        {
            sHow = "";
            try
            {
                // /MarkInfo /Marked true in the catalogue is the declaration,
                // and /StructTreeRoot is the tree itself. A document can claim
                // one without the other, so both are looked for and reported.
                bool bMarked = false;
                bool bHasTree = false;
                UglyToad.PdfPig.Tokens.DictionaryToken oCatalog = oDoc.Structure.Catalog.CatalogDictionary;
                if (oCatalog != null)
                {
                    UglyToad.PdfPig.Tokens.IToken oMarkInfo;
                    if (oCatalog.TryGet(UglyToad.PdfPig.Tokens.NameToken.Create("MarkInfo"), out oMarkInfo))
                    {
                        UglyToad.PdfPig.Tokens.DictionaryToken oMark =
                            oMarkInfo as UglyToad.PdfPig.Tokens.DictionaryToken;
                        UglyToad.PdfPig.Tokens.IToken oIsMarked;
                        if (oMark != null
                            && oMark.TryGet(UglyToad.PdfPig.Tokens.NameToken.Create("Marked"), out oIsMarked))
                        {
                            UglyToad.PdfPig.Tokens.BooleanToken oBool =
                                oIsMarked as UglyToad.PdfPig.Tokens.BooleanToken;
                            if (oBool != null) bMarked = oBool.Data;
                        }
                    }
                    UglyToad.PdfPig.Tokens.IToken oTree;
                    if (oCatalog.TryGet(UglyToad.PdfPig.Tokens.NameToken.Create("StructTreeRoot"), out oTree))
                        bHasTree = oTree != null;
                }
                if (bMarked && bHasTree) sHow = "it is tagged: it declares itself marked and carries a structure tree";
                else if (bHasTree) sHow = "it carries a structure tree but does not declare itself marked";
                else if (bMarked) sHow = "it declares itself marked but carries no structure tree, so there is nothing to read";
                else sHow = "it is not tagged, so its structure has to be inferred from the layout";
                return bHasTree;
            }
            catch (Exception oError)
            {
                sHow = "whether it is tagged could not be determined: " + oError.Message;
                return false;
            }
        }

        // ---- reading a tagged PDF's own structure ------------------------
        //
        // A structure tree is a dictionary tree and PdfPig hands over the
        // tokens, which is all that is needed. No library reads these on .NET
        // Framework: PdfPig says accessibility tagging is out of scope, iText
        // does it but is AGPL and would force HomerScribe off its MIT licence,
        // and VellumPdf is .NET 10 and writes rather than reads.
        //
        //   /StructTreeRoot   /K kids, /RoleMap custom name -> standard name
        //   each element      /S the type (H1..H6, P, L, LI, Table, Figure),
        //                     /Alt alternative text, /ActualText replacement
        //                     text, /Lang, and /K its own kids
        //
        // ROLE MAPPING IS APPLIED FIRST. Matterhorn checkpoint 02: only
        // standard PDF 1.7 tags may be used, and a custom tag must carry a role
        // map entry saying which standard tag it stands for. A document using
        // "Heading1" for "H1" is perfectly valid, and without the role map it
        // would look untagged.

        // public, because lTagged is public and a public field cannot hold a
        // less accessible type:
        //   error CS0052: Inconsistent accessibility: field type
        //   'List<HomerScribe.TaggedItem>' is less accessible than field
        //   'HomerScribe.lTagged'
        // The class was written before the field that exposes it, and its
        // default accessibility was never revisited.
        public class TaggedItem
        {
            public string sKind = "";      // the standard tag, after role mapping
            public string sAlt = "";       // alternative text, where the tag carries one
            public int iLevel;             // 1 to 6 for a heading, 0 otherwise
        }

        // The document's stated language, from the catalogue's /Lang.
        public static string sDocumentLanguage = "";

        // A language tag, or nothing.
        //
        // His ADA Checklist carries a /Lang of binary rubbish — an undecoded
        // byte string, not a language — and HomerScribe wrote it straight into
        // the Markdown's YAML front matter, where Pandoc choked:
        //
        //   Pandoc could not make the Word version: YAML parse exception at
        //   line 0, column 0
        //
        // So that document got no Word version at all, and the only sign was
        // one line in its log. A value read out of a file is data, not a fact,
        // and this one was neither checked nor escaped.
        //
        // A language tag looks like "en" or "en-US": letters, digits and
        // hyphens, and short. Anything else is not one.
        static string languageOnly(string sSaid)
        {
            if (sSaid == null) return "";
            sSaid = sSaid.Trim();
            if (sSaid.Length < 2 || sSaid.Length > 16) return "";
            if (!Regex.IsMatch(sSaid, @"^[A-Za-z]{2,8}(-[A-Za-z0-9]{1,8})*$")) return "";
            return sSaid;
        }

        // Figure descriptions the author wrote, in reading order.
        public static List<string> lTaggedFigureAlt = new List<string>();

        static Dictionary<string, string> dRoleMap = new Dictionary<string, string>();

        // The standard tag a name stands for, following the role map.
        static string standardTag(string sName)
        {
            if (sName == null) return "";
            string sAt = sName;
            // A role map can point at another custom name, so it is followed --
            // with a limit, since a broken document can point in a circle.
            for (int iHop = 0; iHop < 8; iHop = iHop + 1)
            {
                string sNext;
                if (!dRoleMap.TryGetValue(sAt, out sNext)) break;
                if (sNext == null || sNext == sAt) break;
                sAt = sNext;
            }
            return sAt;
        }

        // ---- the tag reader proper --------------------------------------
        //
        // His ten accessibility documents made the case with numbers. Nine are
        // tagged, and against what the tags say the inference is wrong in both
        // directions: 62 tagged headings where it found 10, 29 where it made
        // 118. Lists are worse — four documents with 6, 23, 32 and 65 tagged
        // lists produced NO list items at all.
        //
        // HOW A TAG FINDS ITS WORDS. A structure element's /K holds marked
        // content identifiers — numbers — and the page's content stream marks
        // each run of text with the matching identifier. PdfPig hands over
        // those runs through GetMarkedContents(), so the join is: element -> id
        // -> run of letters -> words.
        //
        // Where anything about this fails, the inference still runs. A tagged
        // document that cannot be read this way is no worse off than an
        // untagged one.

        // public, for the same reason TaggedItem is: lPieces is public and a
        // public field cannot hold a less accessible type. I made this exact
        // mistake on TaggedItem six builds ago and repeated it verbatim.
        public class TaggedPiece
        {
            public string sKind = "";
            public int iLevel;
            public string sText = "";
            public string sAlt = "";
            public int iPage;
            // Inside a list, however deeply. A tagged list puts its words in a
            // P inside an LBody inside an LI, so the piece that CARRIES the
            // text is a paragraph and only its ancestors say it is a list item.
            // Asking the piece alone gave his German checklist nine plain
            // paragraphs where nine list items were tagged.
            public bool bInList;
        }

        // An identifier's words, wherever on the document they sit.
        //
        // THE LOOP THIS REPLACES SEARCHED PAGES 1 TO iPage+2, and iPage only
        // advanced when something matched — so on a document whose first
        // elements did not match, it never looked past page three. His
        // 655-page handbook yielded 5,228 words against 65,577 on the pages;
        // two others came out at about a tenth as well. The tags were fine and
        // the lookup was blind.
        //
        // Every page is searched now, starting at the one we think we are on,
        // since that is nearly always right and makes the search cost nothing
        // in the ordinary case.
        static string wordsForMark(Dictionary<string, string> dText, int iId, int iFrom, int iPages,
                                   out int iFoundOn)
        {
            iFoundOn = 0;
            string sSaid;
            if (iFrom < 1) iFrom = 1;
            for (int iAt = iFrom; iAt <= iPages; iAt = iAt + 1)
            {
                if (!dText.TryGetValue(iAt.ToString() + ":" + iId.ToString(), out sSaid)) continue;
                iFoundOn = iAt;
                return sSaid;
            }
            for (int iAt = 1; iAt < iFrom; iAt = iAt + 1)
            {
                if (!dText.TryGetValue(iAt.ToString() + ":" + iId.ToString(), out sSaid)) continue;
                iFoundOn = iAt;
                return sSaid;
            }
            return "";
        }

        // Every marked run of text on every page, by page and identifier.
        static Dictionary<string, string> textByMark(PdfDocument oDoc)
        {
            Dictionary<string, string> dText = new Dictionary<string, string>();
            int iPage = 0;
            foreach (Page oPage in oDoc.GetPages())
            {
                iPage = iPage + 1;
                try
                {
                    foreach (MarkedContentElement oMark in oPage.GetMarkedContents())
                        gatherMark(oMark, iPage, dText);
                }
                catch (Exception)
                {
                }
            }
            return dText;
        }

        static void gatherMark(MarkedContentElement oMark, int iPage, Dictionary<string, string> dText)
        {
            if (oMark == null) return;
            try
            {
                if (oMark.MarkedContentIdentifier >= 0)
                {
                    StringBuilder oSaid = new StringBuilder();
                    foreach (Letter oLetter in oMark.Letters) oSaid.Append(oLetter.Value);
                    string sSaid = oSaid.ToString().Trim();
                    if (sSaid != "")
                    {
                        string sKey = iPage.ToString() + ":" + oMark.MarkedContentIdentifier.ToString();
                        if (dText.ContainsKey(sKey)) dText[sKey] = dText[sKey] + " " + sSaid;
                        else dText[sKey] = sSaid;
                    }
                }
                foreach (MarkedContentElement oChild in oMark.Children) gatherMark(oChild, iPage, dText);
            }
            catch (Exception)
            {
            }
        }

        // Everything the tree says, in the order the tree says it.
        //
        // The order matters as much as the content: the structure tree IS the
        // document's logical reading order, stated by whoever made it, which is
        // better than any inference from where the ink fell on the page.
        static List<TaggedItem> itemsFromTree(PdfDocument oDoc, out string sTrouble)
        {
            List<TaggedItem> lItems = new List<TaggedItem>();
            sTrouble = "";
            dRoleMap = new Dictionary<string, string>();
            try
            {
                UglyToad.PdfPig.Tokens.DictionaryToken oCatalog = oDoc.Structure.Catalog.CatalogDictionary;
                if (oCatalog == null) return lItems;
                UglyToad.PdfPig.Tokens.IToken oRootToken;
                if (!oCatalog.TryGet(UglyToad.PdfPig.Tokens.NameToken.Create("StructTreeRoot"), out oRootToken))
                    return lItems;
                UglyToad.PdfPig.Tokens.DictionaryToken oRoot =
                    resolved(oDoc, oRootToken) as UglyToad.PdfPig.Tokens.DictionaryToken;
                if (oRoot == null) return lItems;

                // The role map first, so every tag below can be translated.
                UglyToad.PdfPig.Tokens.IToken oRoleToken;
                if (oRoot.TryGet(UglyToad.PdfPig.Tokens.NameToken.Create("RoleMap"), out oRoleToken))
                {
                    UglyToad.PdfPig.Tokens.DictionaryToken oRoles =
                        resolved(oDoc, oRoleToken) as UglyToad.PdfPig.Tokens.DictionaryToken;
                    if (oRoles != null)
                    {
                        // DictionaryToken.Data is keyed by STRING, not by
                        // NameToken. The compiler said so exactly:
                        //   error CS0030: Cannot convert
                        //   KeyValuePair<string, IToken> to
                        //   KeyValuePair<NameToken, IToken>
                        foreach (KeyValuePair<string,
                                              UglyToad.PdfPig.Tokens.IToken> oPair in oRoles.Data)
                        {
                            UglyToad.PdfPig.Tokens.NameToken oTo =
                                oPair.Value as UglyToad.PdfPig.Tokens.NameToken;
                            if (oTo != null) dRoleMap[oPair.Key] = oTo.Data;
                        }
                    }
                }

                UglyToad.PdfPig.Tokens.IToken oKids;
                if (oRoot.TryGet(UglyToad.PdfPig.Tokens.NameToken.Create("K"), out oKids))
                    walkTree(oDoc, oKids, lItems, 0);
            }
            catch (Exception oError)
            {
                sTrouble = oError.Message;
            }
            return lItems;
        }

        // Whatever a token points at, or the token itself.
        //
        // Every other reader here went through asDictionary, which returns null
        // for anything that is not a dictionary — including an array, which is
        // what /K usually holds.
        static UglyToad.PdfPig.Tokens.IToken resolved(PdfDocument oDoc,
                                                      UglyToad.PdfPig.Tokens.IToken oToken)
        {
            try
            {
                for (int iHop = 0; iHop < 8; iHop = iHop + 1)
                {
                    UglyToad.PdfPig.Tokens.IndirectReferenceToken oRef =
                        oToken as UglyToad.PdfPig.Tokens.IndirectReferenceToken;
                    if (oRef == null) return oToken;
                    oToken = oDoc.Structure.TokenScanner.Get(oRef.Data).Data;
                }
                return oToken;
            }
            catch (Exception)
            {
                return oToken;
            }
        }

        static void walkTree(PdfDocument oDoc, UglyToad.PdfPig.Tokens.IToken oToken,
                             List<TaggedItem> lItems, int iDepth)
        {
            // A tree can be deep, and a broken one can be circular.
            if (iDepth > 40 || lItems.Count > 200000) return;
            try
            {
                // RESOLVE FIRST, THEN ASK WHAT IT IS.
                //
                // /K is very often an indirect reference to an array, and this
                // asked "is it an array?" of the reference itself, which it is
                // not — so the whole tree was dropped at the first hop and his
                // tagged document reported "no headings, lists, tables or
                // figures" from a tree that plainly has them.
                oToken = resolved(oDoc, oToken);
                UglyToad.PdfPig.Tokens.ArrayToken oArray = oToken as UglyToad.PdfPig.Tokens.ArrayToken;
                if (oArray != null)
                {
                    foreach (UglyToad.PdfPig.Tokens.IToken oOne in oArray.Data)
                        walkTree(oDoc, oOne, lItems, iDepth + 1);
                    return;
                }
                UglyToad.PdfPig.Tokens.DictionaryToken oNode =
                    oToken as UglyToad.PdfPig.Tokens.DictionaryToken;
                if (oNode == null) return;

                UglyToad.PdfPig.Tokens.IToken oType;
                if (oNode.TryGet(UglyToad.PdfPig.Tokens.NameToken.Create("S"), out oType))
                {
                    UglyToad.PdfPig.Tokens.NameToken oName = oType as UglyToad.PdfPig.Tokens.NameToken;
                    if (oName != null)
                    {
                        TaggedItem oItem = new TaggedItem();
                        oItem.sKind = standardTag(oName.Data);
                        Match oHead = Regex.Match(oItem.sKind, @"^H([1-9])$");
                        if (oHead.Success) oItem.iLevel = int.Parse(oHead.Groups[1].Value);
                        oItem.sAlt = stringFrom(oNode, "Alt");
                        if (oItem.sAlt == "") oItem.sAlt = stringFrom(oNode, "ActualText");
                        lItems.Add(oItem);
                    }
                }

                UglyToad.PdfPig.Tokens.IToken oKids;
                if (oNode.TryGet(UglyToad.PdfPig.Tokens.NameToken.Create("K"), out oKids))
                    walkTree(oDoc, oKids, lItems, iDepth + 1);
            }
            catch (Exception)
            {
            }
        }

        // MATCHED BY ORDER, NOT BY PAGE NUMBER.
        //
        // A structure element names its page with /Pg, an indirect reference to
        // the page object — and PdfPig does not expose a page's object number,
        // so that reference cannot be turned into a page number without going
        // round the library.
        //
        // It does not need to be. The structure tree IS the reading order, so
        // the figures in it come in the same order as the figures on the pages.
        // The nth Figure element describes the nth picture. That is exact for a
        // well-made document and no worse than nothing for a bad one.
        //
        // A stub returning zero was written here first. It would have compiled,
        // run, and quietly matched nothing.
        static List<string> figureDescriptionsInOrder(List<TaggedItem> lItems)
        {
            List<string> lAlt = new List<string>();
            foreach (TaggedItem oItem in lItems)
            {
                if (oItem.sKind != "Figure") continue;
                lAlt.Add(oItem.sAlt);
            }
            return lAlt;
        }

        static string stringFrom(UglyToad.PdfPig.Tokens.DictionaryToken oNode, string sKey)
        {
            try
            {
                UglyToad.PdfPig.Tokens.IToken oToken;
                if (!oNode.TryGet(UglyToad.PdfPig.Tokens.NameToken.Create(sKey), out oToken)) return "";
                UglyToad.PdfPig.Tokens.StringToken oText = oToken as UglyToad.PdfPig.Tokens.StringToken;
                if (oText != null) return oText.Data == null ? "" : oText.Data.Trim();
                UglyToad.PdfPig.Tokens.HexToken oHex = oToken as UglyToad.PdfPig.Tokens.HexToken;
                if (oHex != null) return oHex.Data == null ? "" : oHex.Data.Trim();
                return "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static List<TaggedPiece> lPieces = new List<TaggedPiece>();
        public static string sPiecesTrouble = "";
        static int iMarkPages = 0;

        // The document as its own tags describe it, in the order they give.
        static List<TaggedPiece> piecesFromTree(PdfDocument oDoc, out string sTrouble)
        {
            List<TaggedPiece> lOut = new List<TaggedPiece>();
            sTrouble = "";
            try
            {
                Dictionary<string, string> dText = textByMark(oDoc);
                iMarkPages = oDoc.NumberOfPages;
                if (dText.Count == 0)
                {
                    sTrouble = "the pages carry no marked content, so the tags cannot be joined to the words";
                    return lOut;
                }
                UglyToad.PdfPig.Tokens.DictionaryToken oCatalog = oDoc.Structure.Catalog.CatalogDictionary;
                if (oCatalog == null) return lOut;
                UglyToad.PdfPig.Tokens.IToken oRootToken;
                if (!oCatalog.TryGet(UglyToad.PdfPig.Tokens.NameToken.Create("StructTreeRoot"), out oRootToken))
                    return lOut;
                UglyToad.PdfPig.Tokens.DictionaryToken oRoot =
                    resolved(oDoc, oRootToken) as UglyToad.PdfPig.Tokens.DictionaryToken;
                if (oRoot == null) return lOut;
                UglyToad.PdfPig.Tokens.IToken oKids;
                if (oRoot.TryGet(UglyToad.PdfPig.Tokens.NameToken.Create("K"), out oKids))
                    walkForPieces(oDoc, oKids, lOut, dText, 0, 1);
            }
            catch (Exception oError)
            {
                sTrouble = oError.Message;
            }
            return lOut;
        }

        static void walkForPieces(PdfDocument oDoc, UglyToad.PdfPig.Tokens.IToken oToken,
                                  List<TaggedPiece> lOut, Dictionary<string, string> dText,
                                  int iDepth, int iPage)
        {
            walkForPieces(oDoc, oToken, lOut, dText, iDepth, iPage, false);
        }

        static void walkForPieces(PdfDocument oDoc, UglyToad.PdfPig.Tokens.IToken oToken,
                                  List<TaggedPiece> lOut, Dictionary<string, string> dText,
                                  int iDepth, int iPage, bool bInList)
        {
            if (iDepth > 40 || lOut.Count > 200000) return;
            try
            {
                oToken = resolved(oDoc, oToken);
                UglyToad.PdfPig.Tokens.ArrayToken oArray = oToken as UglyToad.PdfPig.Tokens.ArrayToken;
                if (oArray != null)
                {
                    foreach (UglyToad.PdfPig.Tokens.IToken oOne in oArray.Data)
                        walkForPieces(oDoc, oOne, lOut, dText, iDepth + 1, iPage, bInList);
                    return;
                }
                // A bare number is a marked content identifier: the words
                // belonging to whichever element we are inside.
                UglyToad.PdfPig.Tokens.NumericToken oNumber = oToken as UglyToad.PdfPig.Tokens.NumericToken;
                if (oNumber != null)
                {
                    if (lOut.Count > 0)
                    {
                        int iFoundOn = 0;
                        string sSaid = wordsForMark(dText, oNumber.Int, iPage, iMarkPages, out iFoundOn);
                        if (sSaid != "")
                        {
                            TaggedPiece oLast = lOut[lOut.Count - 1];
                            oLast.sText = (oLast.sText + " " + sSaid).Trim();
                            oLast.iPage = iFoundOn;
                        }
                    }
                    return;
                }
                UglyToad.PdfPig.Tokens.DictionaryToken oNode =
                    oToken as UglyToad.PdfPig.Tokens.DictionaryToken;
                if (oNode == null) return;

                UglyToad.PdfPig.Tokens.IToken oType;
                if (oNode.TryGet(UglyToad.PdfPig.Tokens.NameToken.Create("S"), out oType))
                {
                    UglyToad.PdfPig.Tokens.NameToken oName = oType as UglyToad.PdfPig.Tokens.NameToken;
                    if (oName != null)
                    {
                        TaggedPiece oPiece = new TaggedPiece();
                        oPiece.sKind = standardTag(oName.Data);
                        Match oHead = Regex.Match(oPiece.sKind, @"^H([1-9])$");
                        if (oHead.Success) oPiece.iLevel = int.Parse(oHead.Groups[1].Value);
                        else if (oPiece.sKind == "H") oPiece.iLevel = 1;
                        // A list anywhere above makes this a list item, even
                        // where the tag on this piece is only P.
                        if (oPiece.sKind == "L" || oPiece.sKind == "LI"
                            || oPiece.sKind == "LBody" || oPiece.sKind == "Lbl") bInList = true;
                        oPiece.bInList = bInList;
                        oPiece.sAlt = stringFrom(oNode, "Alt");
                        if (oPiece.sAlt == "") oPiece.sAlt = stringFrom(oNode, "ActualText");
                        lOut.Add(oPiece);
                    }
                }
                // An MCR dictionary names an identifier and sometimes its page.
                UglyToad.PdfPig.Tokens.IToken oMcid;
                if (oNode.TryGet(UglyToad.PdfPig.Tokens.NameToken.Create("MCID"), out oMcid))
                {
                    UglyToad.PdfPig.Tokens.NumericToken oId = oMcid as UglyToad.PdfPig.Tokens.NumericToken;
                    if (oId != null && lOut.Count > 0)
                    {
                        int iFoundOn = 0;
                        string sSaid = wordsForMark(dText, oId.Int, iPage, iMarkPages, out iFoundOn);
                        if (sSaid != "")
                        {
                            TaggedPiece oLast = lOut[lOut.Count - 1];
                            oLast.sText = (oLast.sText + " " + sSaid).Trim();
                            oLast.iPage = iFoundOn;
                        }
                    }
                }

                UglyToad.PdfPig.Tokens.IToken oKids;
                if (oNode.TryGet(UglyToad.PdfPig.Tokens.NameToken.Create("K"), out oKids))
                    walkForPieces(oDoc, oKids, lOut, dText, iDepth + 1,
                                  lOut.Count > 0 ? lOut[lOut.Count - 1].iPage : iPage, bInList);
            }
            catch (Exception)
            {
            }
        }

        public static List<TaggedItem> lTagged = new List<TaggedItem>();
        public static string sTagSummary = "";

        // What was found in the tree, in a sentence somebody would read.
        static string summaryOfTags(List<TaggedItem> lItems, string sTrouble)
        {
            if (sTrouble != "") return "its structure tree could not be read: " + sTrouble;
            if (lItems.Count == 0) return "its structure tree holds nothing that could be read";
            int iHeadings = 0;
            int iFigures = 0;
            int iWithAlt = 0;
            int iTables = 0;
            int iLists = 0;
            foreach (TaggedItem oItem in lItems)
            {
                if (oItem.iLevel > 0) iHeadings = iHeadings + 1;
                else if (oItem.sKind == "Figure")
                {
                    iFigures = iFigures + 1;
                    if (oItem.sAlt != "") iWithAlt = iWithAlt + 1;
                }
                else if (oItem.sKind == "Table") iTables = iTables + 1;
                else if (oItem.sKind == "L") iLists = iLists + 1;
            }
            List<string> lSaid = new List<string>();
            if (iHeadings > 0) lSaid.Add(iHeadings.ToString() + " headings");
            if (iLists > 0) lSaid.Add(iLists.ToString() + " lists");
            if (iTables > 0) lSaid.Add(iTables.ToString() + " tables");
            if (iFigures > 0)
                lSaid.Add(iFigures.ToString() + " figures, " + iWithAlt.ToString() + " with alternative text");
            if (lSaid.Count == 0) return "its structure tree names no headings, lists, tables or figures";
            return "its structure tree names " + string.Join(", ", lSaid.ToArray());
        }

        // EVERYTHING THIS FILE REMEMBERS, FORGOTTEN.
        //
        // Ten documents in one run reported identical tag counts in pairs: the
        // 63-page iOS guide and the 76-page Toolkit both "96 headings, 313 list
        // items"; the 15-page Training guide and the 46-page Charts guide both
        // "6 headings, 0 list items". The second document of each pair was
        // being described by the first document's tags.
        //
        // These are static because one document is read at a time — which is
        // true, and says nothing about what happens when the NEXT one starts.
        // Nothing cleared them, so a document whose own tree was unreadable
        // silently inherited its predecessor's.
        //
        // Cleared here, at the one place that cannot be forgotten: the moment a
        // PDF is opened.
        static void forgetTheLastDocument()
        {
            bTaggedDocument = false;
            sTaggedSaid = "";
            sTagSummary = "";
            sDocumentLanguage = "";
            sPiecesTrouble = "";
            iMarkPages = 0;
            lTagged = new List<TaggedItem>();
            lPieces = new List<TaggedPiece>();
            lTaggedFigureAlt = new List<string>();
            dRoleMap = new Dictionary<string, string>();
        }

        static List<PdfPage> pagesOfPdf(string sPath, string sWorkDir, out string sTrouble)
        {
            List<PdfPage> lPages = new List<PdfPage>();
            sTrouble = "";
            forgetTheLastDocument();
            try
            {
                using (PdfDocument oDoc = PdfDocument.Open(sPath))
                {
                    // The document's stated language, which a screen reader
                    // uses to decide how to pronounce it.
                    sDocumentLanguage = "";
                    try
                    {
                        UglyToad.PdfPig.Tokens.DictionaryToken oCat =
                            oDoc.Structure.Catalog.CatalogDictionary;
                        if (oCat != null) sDocumentLanguage = languageOnly(stringFrom(oCat, "Lang"));
                    }
                    catch (Exception)
                    {
                    }
                    string sTagged = "";
                    bTaggedDocument = documentIsTagged(oDoc, out sTagged);
                    sTaggedSaid = sTagged;
                    // What the document says about itself, read before anything
                    // is inferred about it.
                    lTagged = new List<TaggedItem>();
                    if (bTaggedDocument)
                    {
                        string sTreeTrouble = "";
                        lTagged = itemsFromTree(oDoc, out sTreeTrouble);
                        sTagSummary = summaryOfTags(lTagged, sTreeTrouble);
                        lTaggedFigureAlt = figureDescriptionsInOrder(lTagged);
                        // And the document as the tags actually describe it.
                        string sPieceTrouble = "";
                        lPieces = piecesFromTree(oDoc, out sPieceTrouble);
                        sPiecesTrouble = sPieceTrouble;
                    }
                    int iAt = 0;
                    foreach (Page oPage in oDoc.GetPages())
                    {
                        iAt = iAt + 1;
                        PdfPage oOut = new PdfPage();
                        oOut.iNumber = iAt;

                        // ---- the text layer, IN READING ORDER ----
                        //
                        // PdfPig ships this and HomerScribe was not using it.
                        // Words are assembled spatially, grouped into blocks by
                        // Docstrum, and the blocks put in the order a person
                        // would read them. Sorting letters top to bottom, which
                        // is what this did before, reads a two-column page
                        // straight across the gutter and produces nonsense --
                        // and a lawyer's brief is very often two columns.
                        //
                        // The old sorting is kept as a fallback: where the
                        // analysis throws, a page read badly beats a page not
                        // read at all.
                        if (readInOrder(oPage, oOut)) { }
                        else { oOut.lLines.Clear(); readByPosition(oPage, oOut); }
                        // ---- the picture layer ----
                        //
                        // Written out biggest first. On a scanned page the
                        // biggest IS the page; on a text page it is a figure
                        // that wants alt text.
                        List<IPdfImage> lFound = new List<IPdfImage>();
                        try
                        {
                            foreach (IPdfImage oImage in oPage.GetImages()) lFound.Add(oImage);
                        }
                        catch (Exception)
                        {
                        }
                        lFound.Sort(delegate(IPdfImage oLeft, IPdfImage oRight)
                        { return (oRight.WidthInSamples * oRight.HeightInSamples)
                                 .CompareTo(oLeft.WidthInSamples * oLeft.HeightInSamples); });
                        int iImage = 0;
                        foreach (IPdfImage oImage in lFound)
                        {
                            // Anything smaller than this is a rule, a bullet or
                            // a logo, not something worth describing.
                            if (oImage.WidthInSamples < 64 || oImage.HeightInSamples < 64) continue;
                            iImage = iImage + 1;
                            if (iImage > 6) break;
                            // RawBytes ONLY, and the extension chosen from what
                            // the bytes actually are.
                            //
                            // TryGetBytes does not exist on IPdfImage, and
                            // TryGetPng's shape varies between versions --
                            // guessing at either is what produced
                            //   error CS1061: IPdfImage does not contain a
                            //   definition for TryGetBytes
                            // The raw stream is always there, and for a scanned
                            // page it IS a JPEG. So: take the bytes, look at
                            // their first few, and name the file accordingly.
                            byte[] aBytes = null;
                            try
                            {
                                aBytes = oImage.RawBytes.ToArray();
                            }
                            catch (Exception)
                            {
                                aBytes = null;
                            }
                            if (aBytes == null || aBytes.Length < 512) continue;
                            // FLATE FIRST, WHERE THE BYTES ARE NOT AN IMAGE YET.
                            //
                            // His journal stores 101 of its 112 pages as
                            // /Filter [/FlateDecode /DCTDecode] -- a JPEG,
                            // deflated. RawBytes hands back the raw stream, so
                            // those do not begin FF D8 FF and were all passed
                            // over: the run read 11 pages and skipped 101.
                            //
                            // Inflating is the whole of the fix, and .NET has
                            // had it all along. A zlib stream begins 78 and a
                            // check byte; DeflateStream wants the two header
                            // bytes gone.
                            // ASCII85 FIRST, THEN FLATE.
                            //
                            // A PDF may wrap an image in more than one filter,
                            // and the order matters: /Filter [/ASCII85Decode
                            // /DCTDecode] means the bytes are a JPEG, printed
                            // as ASCII85 text. The inflater only knew about
                            // Flate, so those bytes never looked like a picture
                            // and every page was passed over — a purpose-built
                            // two-page scan reported "0 pictures in all".
                            if (!looksLikeAnImage(aBytes)) aBytes = unAscii85(aBytes);
                            if (!looksLikeAnImage(aBytes)) aBytes = inflated(aBytes);
                            if (aBytes == null || aBytes.Length < 512) continue;
                            string sKind = ".bin";
                            if (aBytes.Length > 3 && aBytes[0] == 0xFF && aBytes[1] == 0xD8 && aBytes[2] == 0xFF)
                                sKind = ".jpg";
                            else if (aBytes.Length > 8 && aBytes[0] == 0x89 && aBytes[1] == 0x50
                                     && aBytes[2] == 0x4E && aBytes[3] == 0x47)
                                sKind = ".png";
                            else if (aBytes.Length > 12 && aBytes[4] == 0x6A && aBytes[5] == 0x50)
                                sKind = ".jp2";
                            // Anything else is a raw bitmap with no header, and
                            // ffmpeg cannot open one without being told its
                            // shape. Those are passed over rather than written
                            // as a file nothing can read.
                            if (sKind == ".bin")
                            {
                                // Counted, not silently dropped. A chart in a
                                // born-digital PDF is often Flate-compressed
                                // raw samples with no header, and four of his
                                // documents reported "0 pictures" with no way
                                // to tell whether they had none or whether
                                // these were being discarded.
                                oOut.iSkippedPictures = oOut.iSkippedPictures + 1;
                                continue;
                            }
                            string sOut = Path.Combine(sWorkDir, "page" + iAt.ToString("000")
                                                     + "_" + iImage.ToString() + sKind);
                            try
                            {
                                File.WriteAllBytes(sOut, aBytes);
                                oOut.lImages.Add(sOut);
                            }
                            catch (Exception)
                            {
                            }
                        }

                        // A page with almost no text and a big picture is a
                        // scan, whatever else it claims.
                        int iChars = 0;
                        foreach (PageLine oOne in oOut.lLines) iChars = iChars + oOne.sText.Trim().Length;
                        oOut.bScanned = iChars < 40 && oOut.lImages.Count > 0;
                        lPages.Add(oOut);
                    }
                }
            }
            catch (Exception oError)
            {
                sTrouble = oError.Message;
            }
            return lPages;
        }
    }
}
