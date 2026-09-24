@echo off
setLocal

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
  pause
  exit /b 1
)
call "%~dp0homerInstall.cmd" setup "%~f0"




:afterLogSetup
rem installTesseract.cmd -- fetch Tesseract, which reads the words on a scan.
rem
rem WHY HOMERSCRIBE USES IT. A scanned page can be read two ways. The picture
rem model takes about twenty-five seconds a page and can invent a word that
rem was never there -- fluent, plausible, and wrong. Tesseract takes about a
rem second, cannot invent anything, and reports how sure it was of every word.
rem On clean print its accuracy is at least as good.
rem
rem So Tesseract reads the words and the model keeps the two jobs it is better
rem at: describing the pictures, and reading a page Tesseract could not.
rem
rem Optional. Without it HomerScribe still reads scanned pages, using the
rem picture model, which works and is much slower.
rem
rem Takes no arguments. Writes a detailed log beside this script.

set "here=%~dp0"
if "%here:~-1%"=="\" set "here=%here:~0,-1%"
rem the log path is set above

echo Tesseract install started %date% %time%>> "%log%"
echo Script: %~f0>> "%log%"
echo Folder: %here%>> "%log%"
echo Command line: %0 %*>> "%log%"
echo(>> "%log%"

where tesseract >nul 2>&1
if not errorlevel 1 (
  echo Tesseract is already installed.
  echo Already on the path>> "%log%"
  tesseract --version >> "%log%" 2>&1
  exit /b 0
)
if exist "%ProgramFiles%\Tesseract-OCR\tesseract.exe" (
  echo Tesseract is already installed in Program Files.
  echo Already in Program Files>> "%log%"
  exit /b 0
)

where winget >nul 2>&1
if errorlevel 1 (
  echo(
  echo winget was not found, so Tesseract could not be fetched automatically.
  echo HomerScribe will still read scanned pages using the picture model.
  echo winget not found>> "%log%"
  exit /b 1
)

echo Fetching Tesseract. This is about 60 MB and happens once.
echo Fetching with winget>> "%log%"
winget install --id UB-Mannheim.TesseractOCR --accept-source-agreements --accept-package-agreements --silent >> "%log%" 2>&1

where tesseract >nul 2>&1
if not errorlevel 1 goto :ok
if exist "%ProgramFiles%\Tesseract-OCR\tesseract.exe" goto :ok

echo(
echo Tesseract could not be fetched. The log says what was tried: %log%
echo HomerScribe will still read scanned pages using the picture model.
echo Install failed>> "%log%"
exit /b 1

:ok
echo(
echo Tesseract is installed. Scanned pages will be read in about a second each
echo rather than twenty-five, and the model will describe their pictures.
echo Installed>> "%log%"
echo Tesseract install finished %date% %time%>> "%log%"
exit /b 0
