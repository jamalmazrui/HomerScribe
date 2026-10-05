## HomerScribe Now Batch Converts PDF to DOCX

Blind lawyers, students and researchers are constantly sent PDF documents that
were never made accessible. A scanned brief, a policy paper, a textbook — the
screen reader finds nothing, or finds the words in the wrong order with no
headings to navigate by.

HomerScribe now converts such documents in batch, on your own computer, using
only free and open source components.

### It examines three layers of every page

A PDF may carry as many as three descriptions of the same page, and their
quality varies enormously:

- **The image layer** — the page as a picture, which is all a scan has.
- **The text layer** — the words themselves, which may be exact, or may be
  unusable when the spaces were never written or a font lacks a translation
  table.
- **The tagged layer** — the structure its author declared: genuine headings,
  lists, and descriptions of pictures.

HomerScribe reads whichever of these a document has, then **measures them
against one another** and converts from whichever holds up best. Tags that
hold only a fraction of a document's words are refused in favour of the text.
A text layer whose words run together is abandoned for the picture, which
Tesseract reads at roughly a second a page. The log records which layer was
used, and why the others were not.

### Page numbers survive the conversion

Page 14 of the Word document is page 14 of the PDF. When a colleague refers to
a passage on page 14, both of you are looking at the same page — which is the
difference between a conversion you can read and one you can argue from.

### It reports what the original got wrong

Where a PDF declares no headings, or holds a picture with no description,
HomerScribe says so. That is a fault in the document you were sent, and worth
knowing about.

### Current limitations

Tables are converted as text rather than as tables. Nested lists in untagged
PDFs are sometimes flattened.

### Getting HomerScribe

For more information about the open source HomerScribe project, visit

<https://github.com/JamalMazrui/HomerScribe>

To download the HomerScribe Installer for Windows, visit

<https://github.com/JamalMazrui/HomerScribe/releases/latest/download/HomerScribe_setup.exe>
