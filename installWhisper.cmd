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
rem Everything goes into %LOCALAPPDATA%\HomerScribe\whisper, which the user can
rem write to. Program Files cannot be written to by an ordinary user.
rem
rem   installWhisper.cmd            install the small model
rem   installWhisper.cmd medium     install a different one instead

setlocal enabledelayedexpansion
set "model=small"
set "noPause="
for %%A in (%*) do (
  if /i "%%A"=="noPause" (set "noPause=1") else (set "model=%%A")
)

set "whisperDir=%LOCALAPPDATA%\HomerScribe\whisper"
if not exist "%whisperDir%" mkdir "%whisperDir%" >nul 2>&1

rem Say plainly what is already here before doing anything, as the Ollama and
rem model scripts do. Nothing is downloaded twice.
echo Whisper is installed in %whisperDir%
echo(
if exist "%whisperDir%\whisper-cli.exe" echo   whisper.cpp: already installed
if not exist "%whisperDir%\whisper-cli.exe" echo   whisper.cpp: not yet installed
if exist "%whisperDir%\ggml-%model%.bin" echo   %model% model: already installed
if not exist "%whisperDir%\ggml-%model%.bin" echo   %model% model: not yet installed
echo(
if not exist "%whisperDir%\whisper-cli.exe" goto :fetchAll
if not exist "%whisperDir%\ggml-%model%.bin" goto :fetchAll
echo Nothing to do: Whisper is ready.
echo(
if not defined noPause pause
endlocal
exit /b 0

:fetchAll

rem ---- the program ---------------------------------------------------
if exist "%whisperDir%\whisper-cli.exe" goto :haveProgram
echo Fetching whisper.cpp. This is a small download.
echo(
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference='Stop';" ^
  "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12;" ^
  "$dir = $env:LOCALAPPDATA + '\HomerScribe\whisper';" ^
  "$rel = Invoke-RestMethod -Uri 'https://api.github.com/repos/ggml-org/whisper.cpp/releases/latest' -Headers @{ 'User-Agent' = 'HomerScribe' };" ^
  "$asset = $null;" ^
  "foreach ($a in $rel.assets) { if ($asset -eq $null -and $a.name -match 'bin-x64|win.*x64|windows') { $asset = $a } };" ^
  "if ($asset -eq $null) { throw 'No Windows build was listed in the latest release' };" ^
  "$zip = Join-Path $env:TEMP 'homerWhisper.zip';" ^
  "$tmp = Join-Path $env:TEMP 'homerWhisper';" ^
  "if (Test-Path $zip) { Remove-Item -Force $zip };" ^
  "if (Test-Path $tmp) { Remove-Item -Recurse -Force $tmp };" ^
  "Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zip -UseBasicParsing;" ^
  "Expand-Archive -Path $zip -DestinationPath $tmp;" ^
  "$files = Get-ChildItem -Path $tmp -Recurse -Include '*.exe','*.dll';" ^
  "foreach ($f in $files) { Copy-Item -Path $f.FullName -Destination $dir -Force };" ^
  "Remove-Item -Force $zip; Remove-Item -Recurse -Force $tmp;" ^
  "$cli = Join-Path $dir 'whisper-cli.exe';" ^
  "if (-not (Test-Path $cli)) { $old = Get-ChildItem -Path $dir -Filter 'main.exe'; if ($old -ne $null) { Copy-Item $old[0].FullName $cli -Force } };"
if errorlevel 1 goto :programFailed

:haveProgram
if exist "%whisperDir%\whisper-cli.exe" echo whisper.cpp is in place.

rem ---- the model ------------------------------------------------------
if exist "%whisperDir%\ggml-%model%.bin" goto :haveModel
echo(
echo Fetching the %model% model. This is a few hundred megabytes.
echo(
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference='Stop';" ^
  "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12;" ^
  "$dir = $env:LOCALAPPDATA + '\HomerScribe\whisper';" ^
  "$name = 'ggml-%model%.bin';" ^
  "$url = 'https://huggingface.co/ggerganov/whisper.cpp/resolve/main/' + $name;" ^
  "Invoke-WebRequest -Uri $url -OutFile (Join-Path $dir $name) -UseBasicParsing;"
if errorlevel 1 goto :modelFailed

:haveModel
echo(
if exist "%whisperDir%\ggml-%model%.bin" echo The %model% model is in place.
echo(
echo Whisper is ready. HomerScribe uses it to transcribe speech, and to find
echo where descriptions can be spoken without covering the dialogue.
echo(
if not defined noPause pause
endlocal
exit /b 0

:programFailed
echo(
echo whisper.cpp could not be downloaded.
echo Get a Windows build by hand from
echo   https://github.com/ggml-org/whisper.cpp/releases
echo and put whisper-cli.exe and its dll files in
echo   %whisperDir%
echo(
if not defined noPause pause
endlocal
exit /b 1

:modelFailed
echo(
echo The %model% model could not be downloaded.
echo Get ggml-%model%.bin by hand from
echo   https://huggingface.co/ggerganov/whisper.cpp/tree/main
echo and put it in
echo   %whisperDir%
echo(
if not defined noPause pause
endlocal
exit /b 1
