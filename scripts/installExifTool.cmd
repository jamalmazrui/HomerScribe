@echo off
setLocal enableDelayedExpansion

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
rem ---- WHY THIS USES WINGET NOW -------------------------------------------
rem
rem The single-file exiftool.exe is DEPRECATED. Phil Harvey stopped releasing
rem it in July 2024, at about version 12.88, so a single file is frozen at
rem whatever it was then -- missing every camera and format added since.
rem
rem It was also the SLOWER arrangement, not the tidier one. The single file was
rem a self-extracting archive that unpacked its whole Perl runtime into a
rem temporary folder on EVERY run. That is why antivirus software kept flagging
rem it, and why the current packaging measurably halved processing time for
rem people doing many files.
rem
rem The exiftool_files folder beside the executable is therefore not clutter:
rem it is the unpacked runtime, kept where it belongs instead of being rebuilt
rem every time the program starts.
rem
rem Oliver Betz packages that as a proper installer, and winget carries it.
rem --scope machine puts it where every Homer app can share one copy.

echo Downloading and installing ExifTool, so descriptions can be written into photographs.
echo(

where winget >nul 2>&1
if errorlevel 1 (
  echo winget was not found, so ExifTool cannot be fetched automatically.
  echo HomerScribe will still describe pictures; it just cannot write the
  echo descriptions into them.
  call "%~dp0homerInstall.cmd" log "FAILED: winget is not on this machine"
  if not defined noPause pause
  exit /b 1
)

winget install --id OliverBetz.ExifTool --exact --scope machine ^
  --accept-package-agreements --accept-source-agreements >> "%log%" 2>&1

rem THE FILE, NOT THE EXIT CODE.
set "found="
for /f "delims=" %%E in ('where exiftool.exe 2^>nul') do if not defined found set "found=%%E"
if not defined found if exist "%ProgramFiles%\ExifTool\exiftool.exe" set "found=%ProgramFiles%\ExifTool\exiftool.exe"
if not defined found (
  echo(
  echo ExifTool did NOT install. The log has what winget said: %log%
  call "%~dp0homerInstall.cmd" log "FAILED: exiftool.exe not found after winget"
  if not defined noPause pause
  exit /b 1
)
echo   ExifTool is at %found%
call "%~dp0homerInstall.cmd" log "OK: exiftool.exe at %found%"
echo(
echo ExifTool is ready.
if not defined noPause pause
exit /b 0
