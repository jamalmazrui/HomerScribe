# checkConfig.ps1 -- does HomerScribe.exe.config name the versions actually here?
#
# Twice now a build has succeeded with a config that redirected System.Memory
# to 4.0.2.0 while 4.0.5.0 sat beside the executable, and twice HomerScribe
# threw FileLoadException on the first PDF. Checking that the file EXISTS is
# not the same as checking that it is RIGHT.
#
# Exit 0 when every assembly present is redirected to its own version.

$ErrorActionPreference = 'Stop'
function Say($m) { Write-Output ("  [config] " + $m) }

$configPath = Join-Path $PWD 'HomerScribe.exe.config'
if (-not (Test-Path $configPath)) { Say 'HomerScribe.exe.config is missing'; exit 1 }

try {
  [xml]$doc = Get-Content -Path $configPath -Raw
} catch {
  Say ('HomerScribe.exe.config is not valid XML: ' + $_.Exception.Message)
  exit 1
}

# Every managed dll here that is not PdfPig's own, taken from the folder rather
# than from a list. A list is what missed Microsoft.Bcl.HashCode.
$names = @(Get-ChildItem -Path $PWD -Filter '*.dll' |
           Where-Object { $_.Name -notlike 'UglyToad.PdfPig*' } |
           ForEach-Object { $_.BaseName })
$wrong = 0
$checked = 0
foreach ($name in $names) {
  $dll = Join-Path $PWD ($name + '.dll')
  if (-not (Test-Path $dll)) { continue }
  $checked = $checked + 1
  $real = [Reflection.AssemblyName]::GetAssemblyName($dll).Version.ToString()
  $node = $doc.configuration.runtime.assemblyBinding.dependentAssembly |
          Where-Object { $_.assemblyIdentity.name -eq $name } | Select-Object -First 1
  if (-not $node) { Say ($name + ' is here but has no redirect'); $wrong = $wrong + 1; continue }
  $to = $node.bindingRedirect.newVersion
  if ($to -ne $real) {
    Say ($name + ' is ' + $real + ' but the redirect points at ' + $to)
    $wrong = $wrong + 1
  }
}

if ($checked -eq 0) { Say 'no dependency assemblies are present to check'; exit 0 }
if ($wrong -gt 0) { Say ([string]$wrong + ' redirect or redirects do not match what is here'); exit 1 }
Say ([string]$checked + ' redirects match the assemblies present')
exit 0
