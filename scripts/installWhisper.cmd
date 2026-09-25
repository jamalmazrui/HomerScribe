@echo off
rem installWhisper.cmd -- install Whisper for speech detection and transcripts.
rem
rem Whisper is OpenAI's speech recognition model: open source, MIT licensed, and
rem entirely local once installed. No account, no login, nothing sent anywhere.
rem
rem WHAT THIS IS FOR. HomerScribe finds the moments where a description can be
rem spoken by listening for silence, which cannot tell a musical passage from a
rem spoken one. On a scored film that leaves it placing most descriptions on a
rem timer rather than in real pauses. Whisper detects SPEECH, which is the
rem question that actually matters, and yields a transcript at the same time.
rem
rem The small model is installed: about 465 MB, 3.4 percent word error rate, and
rem quick enough on a processor alone. Larger models exist and are not worth the
rem download for this purpose.
rem
rem Everything goes into %ProgramFiles%\Whisper, shared by every Homer app, which
rem write to. Program Files cannot be written to by an ordinary user.
rem
rem   installWhisper.cmd            install the small model
rem   installWhisper.cmd medium     install a different one instead

setlocal enabledelayedexpansion

rem ---- the log -------------------------------------------------------------
rem The common half lives in the kit, so a fix reaches every install script in
rem every Homer app rather than the one being edited. It sets sApp, sLogDir,
rem log and sQuiet, and writes the environment header.
set "sScript=%~n0"
set "sCallerDir=%~dp0"
rem IF THE SHARED HALF IS MISSING, SAY SO. It was left out of the installer
rem once, and every script that calls it died at this line -- no message, no
rem log folder, nothing to diagnose from. A missing file must announce itself.
if not exist "%~dp0homerInstall.cmd" (
  echo(
  echo homerInstall.cmd is missing from %~dp0
  echo That file is part of HomerScribe. Reinstall, or copy it from the
  echo HomerScribe zip into this folder, and run this again.
  echo(
  if not defined noPause pause
  exit /b 1
)
call "%~dp0homerInstall.cmd" setup "%~f0" %*

set "model=small"
set "noPause="
for %%A in (%*) do (
  if /i "%%A"=="noPause" (set "noPause=1") else (set "model=%%A")
)

rem MACHINE-WIDE, NOT INSIDE AN APP'S FOLDER.
rem
rem Whisper used to land in %LOCALAPPDATA%\HomerScribe\whisper. That is one
rem user's copy, invisible to the other Homer apps, and it is inside a folder
rem named after this app. Whisper is a shared component: one copy in its own
rem default directory, found by every Homer tool, and untouched when any single
rem app is upgraded or removed.
rem
rem Program Files needs administrator rights, which a Homer installer already
rem requires.
set "whisperDir=%ProgramFiles%\Whisper"
if not exist "%whisperDir%" mkdir "%whisperDir%" >nul 2>&1

rem Say plainly what is already here before doing anything, as the Ollama and
rem model scripts do. Nothing is downloaded twice.
echo Looking for Whisper in %whisperDir%
echo(
rem AN EXISTING COPY ANYWHERE COUNTS. Looking only in this script's own target
rem folder means a Whisper another Homer app already installed, or one on the
rem PATH, is invisible -- and the script fetches 500 MB the machine already has.
rem That is the whole point of a shared machine-wide component.
for /f "delims=" %%W in ('where whisper-cli.exe 2^>nul') do (
  if not exist "%whisperDir%\whisper-cli.exe" (
    echo   whisper.cpp: already installed at %%W
    call "%~dp0homerInstall.cmd" log "Found an existing whisper-cli.exe at %%W"
    set "whisperDir=%%~dpW"
    set "whisperDir=!whisperDir:~0,-1!"
  )
)
if exist "%whisperDir%\whisper-cli.exe" echo   whisper.cpp: already installed
if not exist "%whisperDir%\whisper-cli.exe" echo   whisper.cpp: not yet installed
if exist "%whisperDir%\ggml-%model%.bin" echo   %model% model: already installed
if not exist "%whisperDir%\ggml-%model%.bin" echo   %model% model: not yet installed
echo(
rem PowerShell below reads this, so the download lands where this script
rem decided -- not where it assumed.
set "HOMER_WHISPER_DIR=%whisperDir%"
if not exist "%whisperDir%\whisper-cli.exe" goto :fetchAll
if not exist "%whisperDir%\ggml-%model%.bin" goto :fetchAll
echo Nothing to do: Whisper is ready.
echo(
rem No key press when the installer runs this hidden.
if not defined noPause pause
endlocal
exit /b 0

:fetchAll

rem ---- the program ---------------------------------------------------
if exist "%whisperDir%\whisper-cli.exe" goto :haveProgram
echo Downloading whisper.cpp. This is a small download.
echo(
rem HOW THE DOWNLOAD IS CHOSEN, and why it is not "latest".
rem
rem On 20 August 2026 whisper.cpp changed its release process. A version tag
rem such as v1.9.4 now carries only the two source archives; the compiled
rem binaries are published under nightly build tags such as b5130, which are
rem marked pre-release. GitHub's /releases/latest returns the newest release
rem that is NOT a pre-release -- so it returns v1.9.4, whose two assets contain
rem no Windows build, and this script said "No Windows build was listed in the
rem latest release". That single line was the whole regression.
rem
rem So the LIST of releases is read, newest first, and the first one carrying a
rem Windows x64 CPU build is taken -- whichever tag it is under. Two naming
rem schemes are accepted: the current whisper-bin-win-<variant>-x64.zip and the
rem older whisper-bin-x64.zip. CUDA, Vulkan and arm builds are skipped: the
rem CPU build runs on every machine and needs no driver.
rem
rem If GitHub's API cannot be read at all (rate limit, no network), a pinned
rem release known to carry whisper-bin-x64.zip is fetched directly, so the
rem install does not depend on an API answer.
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference='Stop';" ^
  "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12;" ^
  "$dir = $env:HOMER_WHISPER_DIR;" ^
  "$hdr = @{ 'User-Agent' = 'HomerScribe-installWhisper'; 'Accept' = 'application/vnd.github+json' };" ^
  "$url = $null; $picked = '';" ^
  "try {" ^
  "  $rels = Invoke-RestMethod -Uri 'https://api.github.com/repos/ggml-org/whisper.cpp/releases?per_page=30' -Headers $hdr;" ^
  "  foreach ($r in $rels) {" ^
  "    foreach ($a in $r.assets) {" ^
  "      $n = $a.name.ToLower();" ^
  "      $isWin = ($n -match 'win' -and $n -match 'x64') -or ($n -match '^whisper-bin-x64\.zip$');" ^
  "      $isGpu = $n -match 'cuda|cublas|vulkan|arm|openvino|sycl';" ^
  "      if ($isWin -and -not $isGpu -and $n -match '\.zip$') { $url = $a.browser_download_url; $picked = $r.tag_name + ' / ' + $a.name; break }" ^
  "    }" ^
  "    if ($url) { break }" ^
  "  }" ^
  "} catch { Write-Output ('GitHub API could not be read: ' + $_.Exception.Message) }" ^
  "if (-not $url) { $url = 'https://github.com/ggml-org/whisper.cpp/releases/download/v1.9.2/whisper-bin-x64.zip'; $picked = 'pinned v1.9.2 / whisper-bin-x64.zip' };" ^
  "Write-Output ('asset: ' + $picked);" ^
  "Write-Output ('url:   ' + $url);" ^
  "$zip = Join-Path $env:TEMP 'homerWhisper.zip';" ^
  "$tmp = Join-Path $env:TEMP 'homerWhisper';" ^
  "if (Test-Path $zip) { Remove-Item -Force $zip };" ^
  "if (Test-Path $tmp) { Remove-Item -Recurse -Force $tmp };" ^
  "Invoke-WebRequest -Uri $url -OutFile $zip -UseBasicParsing -Headers $hdr;" ^
  "Write-Output ('downloaded ' + [math]::Round((Get-Item $zip).Length / 1MB, 1) + ' MB');" ^
  "Expand-Archive -Path $zip -DestinationPath $tmp -Force;" ^
  "$files = Get-ChildItem -Path $tmp -Recurse -Include '*.exe','*.dll';" ^
  "Write-Output ('unpacked ' + $files.Count + ' program files');" ^
  "foreach ($f in $files) { Copy-Item -Path $f.FullName -Destination $dir -Force };" ^
  "Remove-Item -Force $zip; Remove-Item -Recurse -Force $tmp;" ^
  "$cli = Join-Path $dir 'whisper-cli.exe';" ^
  "if (-not (Test-Path $cli)) { $old = Get-ChildItem -Path $dir -Filter 'main.exe'; if ($old -ne $null) { Copy-Item $old[0].FullName $cli -Force } };" ^
  "if (-not (Test-Path $cli)) { throw 'whisper-cli.exe was not in the archive' };" ^
  "Write-Output ('whisper-cli.exe is at ' + $cli)" >> "%log%" 2>&1
if errorlevel 1 goto :programFailed

:haveProgram
if exist "%whisperDir%\whisper-cli.exe" echo whisper.cpp is in place.

rem ---- the model ------------------------------------------------------
if exist "%whisperDir%\ggml-%model%.bin" goto :haveModel
echo(
echo Downloading the %model% model. This is a few hundred megabytes.
echo(
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference='Stop';" ^
  "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12;" ^
  "$dir = $env:HOMER_WHISPER_DIR;" ^
  "$name = 'ggml-%model%.bin';" ^
  "$url = 'https://huggingface.co/ggerganov/whisper.cpp/resolve/main/' + $name;" ^
  "Invoke-WebRequest -Uri $url -OutFile (Join-Path $dir $name) -UseBasicParsing;" >> "%log%" 2>&1
if errorlevel 1 goto :modelFailed

:haveModel
echo(
if exist "%whisperDir%\ggml-%model%.bin" echo The %model% model is in place.
echo(
rem THE FILES, NOT THE EXIT CODE.
rem
rem This said "Whisper is ready" because PowerShell returned zero. A download
rem can return zero and leave nothing usable -- a redirect to an HTML error
rem page, an archive with a different layout, a copy that silently failed for
rem want of administrator rights. Checking that the artifact EXISTS is not the
rem same as checking that the command succeeded, and this script was doing the
rem second while claiming the first.
if not exist "%whisperDir%\whisper-cli.exe" goto :notReady
if not exist "%whisperDir%\ggml-%model%.bin" goto :notReady
echo Whisper is ready. HomerScribe uses it to transcribe speech, and to find
echo where descriptions can be spoken without covering the dialogue.
echo(
rem No key press when the installer runs this hidden.
if not defined noPause pause
endlocal
exit /b 0

:notReady
rem Reached when the commands reported success and the files are not there.
echo(
echo Whisper did NOT install correctly.
if not exist "%whisperDir%\whisper-cli.exe" echo   whisper-cli.exe is missing from %whisperDir%
if not exist "%whisperDir%\ggml-%model%.bin" echo   the %model% model is missing from %whisperDir%
echo(
echo The commands reported success, so this is worth reporting. The log has the
echo detail: %log%
call "%~dp0homerInstall.cmd" log "FAILED: commands succeeded but the files are not present in %whisperDir%"
if not defined noPause pause
endlocal
exit /b 1

:programFailed
call "%~dp0homerInstall.cmd" log "FAILED: whisper.cpp download or unpack failed; PowerShell output is above"
echo(
echo whisper.cpp could not be downloaded.
echo Get a Windows build by hand from
echo   https://github.com/ggml-org/whisper.cpp/releases
echo and put whisper-cli.exe and its dll files in
echo   %whisperDir%
echo(
rem No key press when the installer runs this hidden.
if not defined noPause pause
endlocal
exit /b 1

:modelFailed
call "%~dp0homerInstall.cmd" log "FAILED: ggml-%model%.bin download failed; PowerShell output is above"
echo(
echo The %model% model could not be downloaded.
echo Get ggml-%model%.bin by hand from
echo   https://huggingface.co/ggerganov/whisper.cpp/tree/main
echo and put it in
echo   %whisperDir%
echo(
rem No key press when the installer runs this hidden.
if not defined noPause pause
endlocal
exit /b 1
