# Camel Type: C# Coding Guidelines

Camel Type is a coding style designed for systematic readability, optimized for efficient navigation and review, especially by screen reader users. The following rules apply to C# code. An equivalent document exists for JavaScript (see `CamelType_JavaScript.md`), and the same conventions carry to other procedural and object-oriented languages with minor adaptations.

---

## 1. Why Camel Type

Camel Type carries type information in the variable name itself rather than in separate annotations. A reader sees `sPath`, `bFound`, `iCount`, `lsFiles` and knows at a glance what kind of data each one holds, without cross-referencing a declaration, hovering for an IDE tooltip, or memorizing the shape of the surrounding code. This has several benefits:

- **Plain-text readability**: the source reads clearly in a text editor with no syntax highlighting, in a `diff`, in a code-review email, or on a printout.
- **Screen-reader efficiency**: every identifier conveys its type audibly without the user having to seek back to the declaration. Hungarian prefixes are short, distinct syllables that pronounce cleanly.
- **Reduced memory load**: when reading unfamiliar code, the reader does not have to mentally track the type of every variable introduced several lines ago.
- **Diff-friendliness**: type information stays attached to the name, so a rename or a refactor that changes types shows up as a name change rather than a silent semantic drift.
- **Consistency across languages**: Camel Type's prefix conventions apply uniformly across C#, JavaScript, Python, VBScript, and other languages. A developer who knows one Camel Type codebase can read another with minimal adjustment.

These benefits extend to sighted developers as well. Explicit naming is a form of documentation that never goes stale, because the compiler checks the variable's actual type against its declared type at every use.

One related benefit of this style worth noting: identifiers built from lower camel case break naturally into syllables a screen reader pronounces as distinct words — `sFileName` reads as "s, file, name," not as a single unreadable block. This is one reason Camel Type also recommends against ALL-CAPS filenames like `README.md`, which a screen reader may either spell out letter-by-letter or pronounce as one run-on sound, and which also carries an unintended "shouting" connotation in written English.

---

## 2. Variable and Argument Naming

Use Hungarian prefix notation to indicate type. Prefix rules:

- `a` — array
- `b` — boolean
- `bin` — binary buffer (`byte[]`, `Stream`)
- `dt` — date-time (`DateTime`, `DateTimeOffset`, `TimeSpan`)
- `f` — file (`FileStream`, `FileInfo`)
- `h` — window or OS handle (e.g., `IntPtr` from P/Invoke)
- `i` — integer (`int`, `long`, `short`, `byte`)
- `l` — list-like collection (rare in C#; prefer `ls` for `List<T>`)
- `ls` — `List<T>` specifically
- `n` — real number (`float`, `double`, `decimal`)
- `s` — string
- `d` — dictionary (`Dictionary<K,V>`)
- `hs` — hash set (`HashSet<T>`)
- `o` — **reserved for COM objects only** (e.g., `oWord`, `oExcel`, `oWorkbook`, `oRange`, `oSlide`). Do not use `o` as a generic "other object" prefix.
- `v` — variant (`object`, `dynamic`, or mixed types where the actual class is unknown or varies)

For **class instances** (your own classes, library objects), use the **lowercase class name** as the prefix, OR a common abbreviation if one is universally understood. If there is only one instance of that class in scope, the class name prefix is the entire variable name — no additional suffix needed.

Examples:

```csharp
string sFilePath = "C:\\data\\report.docx";
int iRowCount = 0;
bool bFound = false;
List<string> lsErrors = new List<string>();
Dictionary<string, int> dCounts = new Dictionary<string, int>();
byte[] binFileBytes = File.ReadAllBytes(sFilePath);

// One instance of each class → class-name prefix alone
StreamWriter writer = new StreamWriter(sFilePath);
StringBuilder sb = new StringBuilder();
HttpClient httpClient = new HttpClient();

// Two instances of the same class → distinguishing suffix
StreamWriter writerLog = new StreamWriter(sLogPath);
StreamWriter writerOut = new StreamWriter(sOutPath);

// COM objects use the o-prefix (the ONLY use of o in Camel Type)
dynamic oWord = com.createApp("Word.Application");
dynamic oDocument = oWord.Documents.Open(sFilePath);
dynamic oRange = oDocument.Content;

// Managed equivalents use the class-name prefix, NOT o
Form form = new Form();              // not oForm
Button btnOk = new Button();         // not oBtn
OpenFileDialog dialog = new OpenFileDialog();  // not oDlg
Exception ex = caughtException;     // not oError
```

Common class abbreviations are acceptable when they are universally understood: `sb` for `StringBuilder`, `ex` for `Exception`, `args` for method arguments array.

---

## 3. Constant Naming

Constants use the **same lower camel case naming convention as variables**, including the Hungarian type prefix. They are distinguished only by being declared with `const` or `static readonly`, not by any difference in capitalization or formatting.

```csharp
const string sDefaultEncoding = "utf-8";
const int iTimeoutMs = 60000;
const int iMinDataRows = 2;
static readonly HashSet<string> hsSupportedExts =
    new HashSet<string> { ".docx", ".xlsx", ".pdf" };
```

**Note on PascalCase constants**: C# convention often uses `PascalCase` for `public const` and `public static readonly` fields (`int.MaxValue`, `DateTime.Now`). Camel Type diverges from this — constants follow the same naming rules as variables. For `public` constants on classes that participate in a public API consumed by non-Camel-Type code, you may choose to follow the external convention as an interoperability concession; otherwise, lower camel case applies.

---

## 4. Capitalization

Use **lower camel case** for all custom names: local variables, constants, parameters, and private/internal methods. Standard C# PascalCase applies to things the language or .NET framework require:

- Class names: `PascalCase` (required; `class wordConverter` compiles but fights the language).
- `public` method, property, and event names: this is a judgment call. Strict Camel Type uses lower camel case everywhere the language allows it, including public methods. Pragmatic Camel Type may use PascalCase for `public` surface area that external callers will see, to match the .NET naming convention visible in IntelliSense. Pick one and be consistent within a file.
- Namespaces: PascalCase is the universal convention and essentially required for interoperability.

Avoid `snake_case` entirely unless an external API forces it.

---

## 5. Variable and Constant Declarations

- Declare all variables and constants at the **top of their enclosing method or class scope**, before any logic.
- Group declarations by type. One declaration statement per type group. Names are listed **alphabetically** within the group, separated by commas on a single line.
- The declaration statements themselves also appear in **alphabetical order** by the prefix letter of the type they declare.
- Declare constants in their own group immediately above the variable declarations, following the same alphabetical rules.
- Explicitly initialize `let`... sorry, for C#: initialize mutable variables on the lines immediately following the declarations, or inline when the declaration itself specifies an initial value.

Example inside a method:

```csharp
private static int processFolder(string sInputDir, string sOutputDir)
{
    const int iMaxFiles = 1000;
    const string sLogSuffix = ".log";

    bool bAnyFailed, bAnySucceeded;
    int iFailed, iProcessed;
    string sBaseName, sCurrentPath;

    bAnyFailed = false;
    bAnySucceeded = false;
    iFailed = 0;
    iProcessed = 0;
    sBaseName = "";
    sCurrentPath = "";

    // ... logic ...
}
```

For class-level fields, the same rules apply: group by type, alphabetize within and between groups, declare constants above variables.

---

## 6. `var` versus Explicit Types

Prefer explicit types over `var` when the type is not obvious from the right-hand side. The whole point of Camel Type is to make the type visible in the variable name; `var` doesn't undermine that if the name already carries a prefix, but explicit types reinforce the contract.

`var` is acceptable when:

- The right-hand side is a `new T(...)` expression that makes the type obvious.
- The type is a long generic that would obscure the statement (e.g., `Dictionary<string, List<Tuple<int, string>>>`).
- The type is anonymous (`new { X = 1, Y = 2 }`) or the result of a LINQ query that produces anonymous shapes.

When in doubt, prefer the explicit type. It helps future readers and resists IDE-dependent comprehension.

---

## 7. Methods

- Define all routines as **methods that return a value**, even if the return value is not always consumed. This aligns with "no subprocedures" from other Camel Type language variants. In C#, a `void` method is still called a method, but Camel Type prefers methods that return `bool`, `int`, a count, or a status object so that callers can chain or inspect results. Where a `void` method is clearly the right shape (e.g., event handlers, disposable operations), `void` is fine.
- Use **single-line syntax** for simple one-consequence conditionals:

  ```csharp
  if (sPath == null) throw new ArgumentNullException(nameof(sPath));
  if (bReady) return true;
  ```

- Method names follow the same lower camel case rule as variables, with no Hungarian type prefix: `convertOne`, `splitSourceField`, `deriveOutputDir`. (See the capitalization notes in section 4 for the PascalCase-for-public-API judgment call.)

---

## 8. Loops

Prefer **foreach** loops over index-based `for` loops whenever iterating a collection.

```csharp
foreach (var sFile in lsFiles) processFile(sFile);
```

Use index-based `for` only when the index itself is needed — for example, when processing pairs of adjacent elements, or when modifying the collection by position.

Avoid LINQ when a straightforward `foreach` is clearer. LINQ is powerful but its method-chain syntax does not always read as clearly as an explicit loop, especially for readers navigating by screen reader one method call at a time.

---

## 9. Using Declarations

Use `using` statements (or the `using` declaration from C# 8+) for any `IDisposable` resource. Place `using` declarations near the top of the enclosing scope, right after the variable declarations, so the scope of the disposable is clear.

```csharp
using (var reader = new StreamReader(sPath)) {
    // ... use reader ...
}
```

or, in C# 8+ block-bodied methods:

```csharp
using var reader = new StreamReader(sPath);
// reader is disposed at end of enclosing scope
```

---

## 10. Imports (`using` Directives)

Place all `using` directives at the top of the file, in the following order:

1. `System` and `System.*` namespaces (built-in BCL), alphabetized.
2. A blank line, then third-party and project namespaces, alphabetized.

Each `using` directive on its own line.

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

using Markdig;
```

---

## 11. String Delimiters

Use **double quotes** for all strings (C# has no single-quoted strings for `string`; single quotes delimit `char`). Use verbatim strings `@"..."` for paths and regex patterns where escape sequences would be noisy. Use interpolated strings `$"..."` for formatted output.

---

## 12. Magic Numbers and Strings

Never use literal numeric or string values in logic. Declare them as named constants and reference the constant name. This applies to:

- Loop bounds, thresholds, retry counts, timeouts
- Error messages, format strings, separator characters
- Error codes, exit codes, enum-like integer values
- File extensions, URL schemes, MIME types

If a built-in `.NET` constant already exists (e.g., `Environment.NewLine`, `Path.DirectorySeparatorChar`, `int.MaxValue`), use it directly rather than defining your own.

---

## 13. Object and Collection Initializers

Keep short initializers on a single line when they fit. Break to multiple lines only when the line would otherwise exceed a reasonable width (typically ~100 characters).

```csharp
var d = new Dictionary<string, int> { { "alpha", 1 }, { "beta", 2 } };

var dFields = new Dictionary<string, string> {
    { "name", sName },
    { "email", sEmail },
    { "phone", sPhone },
    { "address", sAddress }
};
```

---

## 14. Error Handling

- Catch specific exception types when possible, not bare `catch {}` blocks.
- Use the variable name `ex` for exceptions (acceptable universal abbreviation of the class name `Exception`).
- Prefer re-throwing with `throw;` rather than `throw ex;` to preserve the stack trace.

```csharp
try {
    convertOne(sFile);
} catch (FileNotFoundException ex) {
    Console.Error.WriteLine("File not found: " + ex.FileName);
    return false;
} catch (Exception ex) {
    logger.error("Unexpected: " + ex.Message);
    throw;
}
```

---

## 15. File Organization

- One file per class is a common convention but not required. A single-file program (like `2htm.cs`) can legitimately contain many `static class` sections.
- Within a class, order members as: constants, fields, constructor (if any), methods in alphabetical order. The entry method (`Main`, or the highest-level dispatcher `run`) is an acceptable exception — place it near the top of the `program` class to make the file's entry point easy to find.
- Use `#region` sparingly; large comment banners separating logical sections work better with screen readers.

---

## 16. Comments

Write comments as complete sentences in prose, not as telegraphic labels. A comment should explain **why** a piece of code does what it does, not restate **what** it does — the code already says what.

```csharp
// Refuse to overwrite the input file with its own output. This can
// happen in plain-text mode when the input is a .txt file: the
// output path derived from the basename would collide with the
// source. Without this guard, --force would silently destroy the
// user's source file.
if (sInPath.Equals(sOutPath, StringComparison.OrdinalIgnoreCase))
    return false;
```

---

## 17. Cross-Program Naming Conventions

When the same concept appears across multiple programs in a related project (e.g., a family of companion tools that share a common command-line and GUI layout), use the **same identifier name** in each program for that concept. Consistency of naming across programs makes it easy for a reader who knows one program to understand another.

The following shared names are conventional in the author's project family:

| Concept | Identifier |
|---|---|
| Program's display name | `sProgramName` |
| Program's version string | `sProgramVersion` |
| Config directory name | `sConfigDirName` |
| Config file name | `sConfigFileName` |
| Log file name | `sLogFileName` |
| Source-input variable (the user's source files / URLs / etc.) | `sSource` |
| Output directory variable | `sOutputDir` |
| GUI layout: left margin | `iLayoutLeft` |
| GUI layout: right margin | `iLayoutRight` |
| GUI layout: top margin | `iLayoutTop` |
| GUI layout: gap between adjacent controls | `iLayoutGap` |
| GUI layout: gap between rows | `iLayoutRowGap` |
| GUI layout: width of leading labels | `iLayoutLabelWidth` |
| GUI layout: width of buttons | `iLayoutButtonWidth` |
| GUI layout: height of buttons | `iLayoutButtonHeight` |
| GUI layout: height of text fields and rows | `iLayoutTextHeight` |
| GUI layout: form (dialog) width | `iLayoutFormWidth` |

These are typically declared on the `program` class as `public const` (string and int values) or `public static readonly` (array values).

The following shared classes are conventional in the project family (using lowerCamelCase for class names per Camel Type):

| Class | Purpose |
|---|---|
| `program` | The top-level program class (entry point, argument parsing, dispatch) |
| `logger` | Diagnostic logger with `open`, `close`, `info`, `warn`, `error`, `debug` methods |
| `configManager` | Loads and saves a per-program INI under `%LOCALAPPDATA%\<program>\` |
| `guiDialog` | The parameter dialog with `show` / `run` methods |
| `comHelper` | COM lifecycle helper (creating apps, releasing references, retrying ops). Used only when the program drives Office or other COM servers. |

For the `logger` class, all six methods (`open`, `close`, `info`, `warn`, `error`, `debug`) should be present even if the program currently calls only some of them. This keeps the surface uniform across the family so future code (or a developer moving between programs) can use any level method without having to check whether it exists.

A consequence of these conventions is that the layout-constants block is the same across the family's GUI dialogs, so changes that adjust the visual rhythm of one dialog (e.g., a wider button) propagate trivially to the others by editing the same-named constant.

---

## 18. Summary

Camel Type in C# means: Hungarian type prefixes on identifiers, lower camel case throughout (except PascalCase for class names, namespaces, and optionally public API), alphabetized grouped declarations at the top of each scope, `foreach` over `for`, methods that return meaningful values, explicit types over `var` when not obvious, constants named like variables but declared with `const` or `static readonly`, named constants in place of all magic numbers, double-quoted strings, and `using` directives grouped and alphabetized at the top of the file. The goal throughout is that the source is as self-describing as the language allows, so that a reader — including a reader using a screen reader — can comprehend any section of code from the text alone, without hovering, without cross-referencing, and without holding a mental map of types declared fifty lines earlier.
