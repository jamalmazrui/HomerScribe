@echo off
rem getWhisper.cmd -- install Whisper, and say exactly what happened.
rem
rem STANDALONE ON PURPOSE. It calls no other Homer script, reads no settings,
rem and needs nothing installed. Six rounds of this problem have gone into
rem scripts that depended on something else that was missing, so this one
rem depends on nothing.
rem
rem Run it from anywhere. It writes getWhisper.log BESIDE ITSELF, so the log
rem cannot be lost to a folder that does not exist.

setlocal enabledelayedexpansion
set "log=%~dp0getWhisper.log"
echo getWhisper started %DATE% %TIME%> "%log%"
echo Script: %~f0>> "%log%"
echo Windows: %OS%  Processor: %PROCESSOR_ARCHITECTURE%>> "%log%"
echo ProgramFiles: %ProgramFiles%>> "%log%"
echo LOCALAPPDATA: %LOCALAPPDATA%>> "%log%"

echo(
echo This installs Whisper for HomerScribe. About 500 MB, once.
echo Detail goes to %log%
echo(

rem ---- 1. is it already somewhere? ------------------------------------
echo Looking for an existing Whisper.
set "found="
for /f "delims=" %%W in ('where whisper-cli.exe 2^>nul') do if not defined found set "found=%%W"
if not defined found if exist "%ProgramFiles%\Whisper\whisper-cli.exe" set "found=%ProgramFiles%\Whisper\whisper-cli.exe"
if not defined found if exist "%LOCALAPPDATA%\HomerScribe\whisper\whisper-cli.exe" set "found=%LOCALAPPDATA%\HomerScribe\whisper\whisper-cli.exe"
if defined found (
  echo   Found: !found!
  echo Found existing whisper-cli.exe at !found!>> "%log%"
) else (
  echo   None found.
  echo No existing whisper-cli.exe>> "%log%"
)
echo(

rem ---- 2. where it will go --------------------------------------------
set "dir=%ProgramFiles%\Whisper"
echo Installing into %dir%
echo Target: %dir%>> "%log%"
mkdir "%dir%" >nul 2>&1
if not exist "%dir%" (
  echo(
  echo Could not create %dir%
  echo This needs administrator rights. Right-click this file and choose
  echo "Run as administrator", then try again.
  echo FAILED: could not create %dir% -- not elevated?>> "%log%"
  echo(
  pause
  exit /b 1
)
rem Prove it is writable, not merely present.
echo test> "%dir%\homerWrite.tmp" 2>nul
if not exist "%dir%\homerWrite.tmp" (
  echo(
  echo %dir% exists but cannot be written to.
  echo Right-click this file and choose "Run as administrator".
  echo FAILED: %dir% not writable -- not elevated?>> "%log%"
  echo(
  pause
  exit /b 1
)
del "%dir%\homerWrite.tmp" >nul 2>&1
echo   The folder is writable.
echo(

rem ---- 3. the program --------------------------------------------------
if exist "%dir%\whisper-cli.exe" goto :haveExe
echo Downloading whisper.cpp.
echo Downloading whisper.cpp>> "%log%"
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference='Stop';" ^
  "[Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12;" ^
  "$d='%dir%';" ^
  "$r=Invoke-RestMethod 'https://api.github.com/repos/ggml-org/whisper.cpp/releases/latest';" ^
  "$a=$r.assets | Where-Object { $_.name -match 'bin-x64' } | Select-Object -First 1;" ^
  "if(-not $a){ $a=$r.assets | Where-Object { $_.name -match 'win' -and $_.name -match 'x64' } | Select-Object -First 1 };" ^
  "if(-not $a){ throw 'no Windows x64 asset in the latest release' };" ^
  "Write-Host ('  asset: ' + $a.name);" ^
  "$z=Join-Path $env:TEMP 'getWhisper.zip'; $t=Join-Path $env:TEMP 'getWhisper';" ^
  "if(Test-Path $z){Remove-Item -Force $z}; if(Test-Path $t){Remove-Item -Recurse -Force $t};" ^
  "Invoke-WebRequest $a.browser_download_url -OutFile $z -UseBasicParsing;" ^
  "Expand-Archive $z $t;" ^
  "Get-ChildItem $t -Recurse -Include '*.exe','*.dll' | ForEach-Object { Copy-Item $_.FullName $d -Force };" ^
  "Remove-Item -Force $z; Remove-Item -Recurse -Force $t;" ^
  "if(-not (Test-Path (Join-Path $d 'whisper-cli.exe'))){ $m=Join-Path $d 'main.exe'; if(Test-Path $m){ Copy-Item $m (Join-Path $d 'whisper-cli.exe') } }" >> "%log%" 2>&1
:haveExe

rem ---- 4. the model ----------------------------------------------------
if exist "%dir%\ggml-small.bin" goto :haveModel
echo Downloading the small model. A few hundred megabytes.
echo Downloading ggml-small.bin>> "%log%"
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference='Stop';" ^
  "[Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12;" ^
  "Invoke-WebRequest 'https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin' -OutFile '%dir%\ggml-small.bin' -UseBasicParsing" >> "%log%" 2>&1
:haveModel

rem ---- 5. THE FILES, not the exit codes --------------------------------
echo(
echo Checking what is actually there.
set "bad="
if not exist "%dir%\whisper-cli.exe" set "bad=1"
if not exist "%dir%\ggml-small.bin" set "bad=1"
if defined bad (
  echo(
  echo Whisper did NOT install.
  if not exist "%dir%\whisper-cli.exe" echo   missing: %dir%\whisper-cli.exe
  if not exist "%dir%\ggml-small.bin" echo   missing: %dir%\ggml-small.bin
  echo(
  echo Please send %log% -- it holds what the download said.
  echo FAILED: files missing after download>> "%log%"
  echo(
  pause
  exit /b 1
)
for %%F in ("%dir%\whisper-cli.exe") do echo   whisper-cli.exe  %%~zF bytes
for %%F in ("%dir%\ggml-small.bin") do echo   ggml-small.bin   %%~zF bytes
for %%F in ("%dir%\whisper-cli.exe") do echo OK: whisper-cli.exe %%~zF bytes>> "%log%"
for %%F in ("%dir%\ggml-small.bin") do echo OK: ggml-small.bin %%~zF bytes>> "%log%"
echo(
echo Whisper is installed. HomerScribe can transcribe now.
echo(
pause
endlocal
exit /b 0
