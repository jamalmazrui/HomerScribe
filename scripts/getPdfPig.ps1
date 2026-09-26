$ErrorActionPreference = 'Stop'
function Say($m) { Write-Output ("  [pdfpig] " + $m) }

# PdfPig's assembly is UglyToad.PdfPig.dll, and the package is seven assemblies
# whose types are spread across them. Its .NET Framework build also needs
# System.Memory and companions for Span and ReadOnlyMemory.
# VERSIONS ARE PINNED, AND THE REASON IS WRITTEN DOWN.
#
# Taken from BuildEdSharp.ps1, which pins every package and says why beside
# each one -- ReverseMarkdown at 4.7.1 because 5.x ships net8.0 only and net48
# cannot reference it; HtmlAgilityPack at 1.12.1 to match what ReverseMarkdown
# was compiled against, after a mismatch crashed on somebody's machine.
#
# This script asked nuget.org for "the latest PdfPig" with no pin at all. That
# is the same fragility: the day PdfPig ships a version targeting only net8.0,
# or changes the assemblies it needs, a build that worked yesterday fails with
# nothing changed here. 0.1.16 is what HomerScribe has been built and tested
# against, it targets net462 among others, and net48 consumes it.
#
# Raise it deliberately, having read the release notes, and change this comment
# when you do.
$c_sPdfPigVersion = '0.1.16'
$mainName = 'UglyToad.PdfPig.dll'
# NOT A HAND-WRITTEN LIST ANY MORE.
#
# It was six names I had guessed at, and PdfPig then asked for a seventh --
# Microsoft.Bcl.HashCode -- which was not among them. Guessing a dependency
# list gets one assembly closer each time and never arrives.
#
# The package says what it needs, in its own .nuspec. That is read instead, and
# followed for the dependencies' dependencies too.
$alsoNeeded = @()

$pkg = Join-Path $PWD 'packages'
# THE DLLs, THE CONFIG AND pdfpig.name GO IN exec, beside the executable the
# build writes there (25 Sep 2026). They were written to the project root,
# where every tidy found them as strays and where the DLLs had crept into the
# repository; exec is the folder for built and fetched binaries, never in git.
$out = Join-Path $PWD 'exec'
if (-not (Test-Path $out)) { New-Item -ItemType Directory -Path $out | Out-Null }

function WriteConfig {
  # CALLED ON EVERY PATH OUT, including the one that finds everything already
  # present. When this ran only at the end, a folder that already had the DLLs
  # exited early and the config was never rewritten -- so the wrong redirect
  # from the previous version stayed, and the same FileLoadException came back.
  # An early return that skips newly added work is the third fault of this
  # exact shape in this project.
  $redirects = ''
  # Every managed dll beside the executable that is not PdfPig's own. Built
  # from what is THERE rather than from a list, so an assembly that arrives
  # later is redirected without anybody remembering to add it.
  $these = @(Get-ChildItem -Path $out -Filter '*.dll' |
             Where-Object { $_.Name -notlike 'UglyToad.PdfPig*' } |
             ForEach-Object { $_.Name })
  foreach ($need in $these) {
    $path = Join-Path $out $need
    if (-not (Test-Path $path)) { continue }
    try {
      $an = [Reflection.AssemblyName]::GetAssemblyName($path)
    } catch {
      Say ('could not read the version of ' + $need); continue
    }
    $token = ($an.GetPublicKeyToken() | ForEach-Object { $_.ToString('x2') }) -join ''
    $ver = $an.Version.ToString()
    Say ($need + ' is version ' + $ver + ', token ' + $token)
    $redirects += @"
      <dependentAssembly>
        <assemblyIdentity name="$($an.Name)" publicKeyToken="$token" culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-$ver" newVersion="$ver" />
      </dependentAssembly>

"@
  }
  $config = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <startup>
    <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8" />
  </startup>
  <runtime>
    <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
$redirects    </assemblyBinding>
  </runtime>
</configuration>
"@
  Set-Content -Path (Join-Path $out 'HomerScribe.exe.config') -Value $config -Encoding UTF8

  # THE BUILD READS pdfpig.name TO LEARN WHICH DLL TO REFERENCE, and this
  # script never wrote it -- so the build always concluded PdfPig could not be
  # found, however many assemblies were sitting right there. The file holds the
  # main assembly's name and nothing else.
  Set-Content -Path (Join-Path $out 'pdfpig.name') -Value $mainName -Encoding ASCII -NoNewline
  Write-Host ("  [pdfpig] pdfpig.name written: " + $mainName)
  Say 'HomerScribe.exe.config written from the versions on disk'
}



function PickBest($items, $wantName) {
  foreach ($tfm in @('net4', 'netstandard2')) {
    $inTfm = @($items | Where-Object { $_.FullName -like ('*' + $tfm + '*') })
    $exact = $inTfm | Where-Object { $_.Name -eq $wantName } | Select-Object -First 1
    if ($exact) { return $exact }
  }
  $exact = $items | Where-Object { $_.Name -eq $wantName } | Select-Object -First 1
  if ($exact) { return $exact }
  return $null
}

function FindUnder($root, $name) {
  if (-not (Test-Path $root)) { return @() }
  return @(Get-ChildItem -Path $root -Recurse -Filter $name -ErrorAction SilentlyContinue)
}

# Nothing to do only when the main assembly AND every dependency are present.
# No early exit: the dependency list is not known until the nuspecs are read,
# and an early exit that skips work added later is the fault that has cost
# three builds here already. Copying a file that is already correct is cheap.

# A package from nuget.org, unpacked under packages.
#
# This is how the dependencies arrive. nuget will not fetch them: it answers
#   Package "PdfPig.0.1.16" is already installed.
# and stops, so -DependencyVersion never gets a chance to resolve anything. A
# package installed once is installed, whatever was asked for the second time.
# Downloading each one by name needs nothing installed and cannot be skipped.
function FetchPackage($id) {
  Say ('downloading ' + $id + ' from nuget.org')
  [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
  $ProgressPreference = 'SilentlyContinue'
  $zip = Join-Path $env:TEMP ($id + '.nupkg.zip')
  $out = Join-Path $env:TEMP ('nupkg_' + $id)
  try {
    # The pinned version for PdfPig itself; whatever it asks for, for the rest.
    $url = 'https://www.nuget.org/api/v2/package/' + $id
    if ($id -eq 'PdfPig') { $url = $url + '/' + $c_sPdfPigVersion }
    Say ('  from ' + $url)
    Invoke-WebRequest -Uri $url -OutFile $zip -UseBasicParsing
    if (Test-Path $out) { Remove-Item -Recurse -Force $out }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::ExtractToDirectory($zip, $out)
    if (-not (Test-Path $pkg)) { New-Item -ItemType Directory -Path $pkg | Out-Null }
    $into = Join-Path $pkg ($id + '_downloaded')
    if (Test-Path $into) { Remove-Item -Recurse -Force $into }
    Copy-Item -Path $out -Destination $into -Recurse -Force
    return $true
  } catch {
    Say ($id + ' could not be downloaded: ' + $_.Exception.Message)
    return $false
  }
}

if (-not (FindUnder $pkg $mainName)) { FetchPackage 'PdfPig' | Out-Null }

# What does the package say it needs? Follow it, and what those need.
function DependenciesOf($id) {
  $out = @()
  $specs = @(Get-ChildItem -Path $pkg -Recurse -Filter ($id + '.nuspec') -ErrorAction SilentlyContinue)
  foreach ($spec in $specs) {
    try {
      [xml]$x = Get-Content -Path $spec.FullName -Raw
    } catch {
      continue
    }
    foreach ($d in $x.GetElementsByTagName('dependency')) {
      $depId = $d.GetAttribute('id')
      if ($depId -and ($out -notcontains $depId)) { $out += $depId }
    }
  }
  return $out
}

# SEEDED, AND THEN WALKED.
#
# PdfPig asks for Microsoft.Bcl.HashCode 6.0.0 at run time -- his log proved
# it, listing the thirteen assemblies present and that one absent -- and two
# builds of reading nuspecs did not produce it. Rather than a sixth guess at
# why the reading misses it, the ones PdfPig is known to need are named
# outright, and the walk still runs on top to catch anything else.
#
# A list is a poor way to FIND dependencies, which is why the walk stays. It is
# a perfectly good way to make sure of one already known by name.
$queue = @('Microsoft.Bcl.HashCode')
foreach ($d in (DependenciesOf 'PdfPig')) {
  if ($queue -notcontains $d) { $queue += $d }
}
Say ([string]$queue.Count + ' to consider at the start: ' + ($queue -join ', '))
$seen = @('PdfPig')
while ($queue.Count -gt 0) {
  $id = $queue[0]
  $queue = @($queue | Select-Object -Skip 1)
  if ($seen -contains $id) { continue }
  $seen += $id
  if (FindUnder $pkg ($id + '.dll')) {
    Say ($id + ' is already under packages')
  } else {
    Say ('fetching ' + $id)
    if (FetchPackage $id) {
      if (FindUnder $pkg ($id + '.dll')) { Say ($id + ' arrived') }
      else { Say ($id + ' DOWNLOADED BUT NO ' + $id + '.dll IS IN IT') }
    } else {
      Say ($id + ' COULD NOT BE FETCHED')
    }
  }
  if (($alsoNeeded -notcontains ($id + '.dll'))) { $alsoNeeded += ($id + '.dll') }
  foreach ($more in (DependenciesOf $id)) {
    if (($seen -notcontains $more) -and ($queue -notcontains $more)) { $queue += $more }
  }
}
Say ([string]$alsoNeeded.Count + ' dependencies named by the packages themselves')
foreach ($n in $alsoNeeded) { Say ('  needs ' + $n) }

# A LAST SWEEP, because a nuspec can group its dependencies by target framework
# and a reader can miss a group. Rather than trust the walk, look at what the
# chosen folder actually contains: nuget lays every assembly a build needs in
# the same lib folder, so anything there that is not PdfPig's own is a
# dependency whether a nuspec mentioned it or not.
$mainForFolder = PickBest (FindUnder $pkg $mainName) $mainName
if ($mainForFolder) {
  foreach ($f in Get-ChildItem -Path $mainForFolder.Directory.FullName -Filter '*.dll') {
    if ($f.Name -like 'UglyToad.PdfPig*') { continue }
    if ($alsoNeeded -notcontains $f.Name) {
      Say ('also in the same folder: ' + $f.Name)
      $alsoNeeded += $f.Name
    }
  }
}

# WHAT THE ASSEMBLY ITSELF REFERENCES (25 Sep 2026). PdfPig's nuspec names
# one dependency for net462, yet the assembly references System.Memory and
# the other shims that .NET Framework 4.8 does not ship. Old copies at the
# project root had been covering for this; when the products moved to exec
# they were left behind and the config check failed. So the references are
# read from the assembly, and each shim it names is fetched and copied.
if ($mainForFolder) {
  try {
    $asm = [System.Reflection.Assembly]::ReflectionOnlyLoadFrom($mainForFolder.FullName)
    foreach ($ref in $asm.GetReferencedAssemblies()) {
      $n = $ref.Name
      if ($n -match '^(System\.Memory|System\.Buffers|System\.Numerics\.Vectors|System\.Runtime\.CompilerServices\.Unsafe|System\.Threading\.Tasks\.Extensions|System\.ValueTuple|Microsoft\.Bcl\.[A-Za-z]+)$') {
        if ($alsoNeeded -notcontains ($n + '.dll')) { Say ('referenced by ' + $mainName + ': ' + $n); $alsoNeeded += ($n + '.dll') }
      }
    }
  } catch { Say ('could not read the references of ' + $mainName + ': ' + $_.Exception.Message) }
}

$main = PickBest (FindUnder $pkg $mainName) $mainName
if (-not $main) { Say ('no ' + $mainName + ' found anywhere under ' + $pkg); exit 1 }
Say ('taking ' + $main.FullName)

$copied = 0
foreach ($f in Get-ChildItem -Path $main.Directory.FullName -Filter '*.dll') {
  Copy-Item $f.FullName (Join-Path $out $f.Name) -Force
  Say ('copied ' + $f.Name)
  $copied = $copied + 1
}

# The dependencies, which live in their own packages.
foreach ($need in $alsoNeeded) {
  if (Test-Path (Join-Path $out $need)) { continue }
  # The package is named after the assembly: System.Memory.dll lives in the
  # System.Memory package. Fetch it if it is not already under packages.
  $pick = PickBest (FindUnder $pkg $need) $need
  if (-not $pick) {
    $id = $need -replace '\.dll$', ''
    if (FetchPackage $id) { $pick = PickBest (FindUnder $pkg $need) $need }
  }
  if ($pick) {
    Copy-Item $pick.FullName (Join-Path $out $need) -Force
    Say ('copied dependency ' + $need)
    $copied = $copied + 1
  } else {
    Say ('dependency NOT FOUND: ' + $need)
  }
}

Say ([string]$copied + ' file or files copied into exec')
foreach ($need in $alsoNeeded) {
  if (-not (Test-Path (Join-Path $out $need))) { Say ('still missing: ' + $need) }
}

if (-not (Test-Path (Join-Path $out $mainName))) { Say 'the main assembly did not arrive'; exit 1 }

WriteConfig
