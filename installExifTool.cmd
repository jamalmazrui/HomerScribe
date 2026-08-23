@echo off
setLocal enableDelayedExpansion
rem installExifTool.cmd -- put a single-file ExifTool where HomerScribe will
rem find it.
rem
rem ONE FILE, AND ONLY ONE FILE. exiftool.exe must be a self-contained binary
rem with nothing beside it: no "exiftool_files" folder, no Perl DLLs.
rem
rem THIS SCRIPT FETCHES NOTHING, and that is deliberate rather than an
rem oversight. Nothing currently published is a single file:
rem
rem   exiftool.org / SourceForge 13.59  a small launcher PLUS an
rem                                     "exiftool_files" folder holding Perl
rem   winget OliverBetz.ExifTool        the same thing, installed
rem   Image-ExifTool-<ver>.tar.gz       Perl source, needs Perl installed
rem
rem The one-file form is the OLD exiftool.org format, a packed archive, and it
rem was replaced by the launcher deliberately: unpacking a packed archive on
rem every invocation is slow, and HomerScribe runs ExifTool once per picture.
rem
rem None of this costs anything. HomerScribe carries the definitions of the two
rem IPTC accessibility properties itself and hands them to ExifTool with
rem -config, so even an old single-file copy writes every field.
rem
rem Takes no arguments. Writes a detailed log beside this script.

set "here=%~dp0"
if "%here:~-1%"=="\" set "here=%here:~0,-1%"
set "log=%here%\installExifTool.log"
set "target=%LOCALAPPDATA%\HomerScribe\exiftool"

echo ExifTool setup started %date% %time%> "%log%"
echo Script: %~f0>> "%log%"
echo Folder: %here%>> "%log%"
echo Command line: %0 %*>> "%log%"
echo Target: %target%>> "%log%"
echo Windows: %OS%, processor %PROCESSOR_ARCHITECTURE%>> "%log%"
echo Requirement: a single-file exiftool.exe, with no exiftool_files folder>> "%log%"
echo(>> "%log%"

echo Looking for a single-file ExifTool.
echo The log is %log%
echo(

rem ---- already in place and single? --------------------------------------
set "haveExif="
call :trySingle "%target%"
if defined haveExif (
  echo ExifTool %exifVer% is already in place at %target%
  echo Already in place at %target%, version %exifVer%>> "%log%"
  goto :done
)

rem ---- anywhere else on this machine? ------------------------------------
call :trySingle "%here%"
call :trySingle "C:\HomerScribe"
call :trySingle "%ProgramFiles%\HomerScribe"
call :trySingle "%LOCALAPPDATA%\Programs\HomerScribe"
if not defined haveExif goto :nothingToFetch

echo Copying the single-file ExifTool %exifVer% from %haveExif%
echo Copying from %haveExif%, version %exifVer%>> "%log%"
if not exist "%target%" mkdir "%target%" >nul 2>&1
copy /y "%haveExif%\exiftool.exe" "%target%\exiftool.exe" >nul 2>&1
set "haveExif="
call :trySingle "%target%"
if not defined haveExif goto :nothingToFetch
echo(
echo ExifTool %exifVer% is installed at %target%
echo HomerScribe will find it there and write descriptions into your pictures.
echo Installed to %target%, version %exifVer%>> "%log%"
goto :done

:nothingToFetch
echo Nothing was fetched: no single-file build is published.>> "%log%"
echo(
echo No single-file exiftool.exe was found, and none can be fetched.
echo(
echo HomerScribe wants ONE binary, with no "exiftool_files" folder beside it.
echo Nothing currently published is in that form: the downloads from
echo exiftool.org and SourceForge, and the winget package
echo OliverBetz.ExifTool, are all a small launcher plus a folder of Perl.
echo(
echo If you have a self-contained exiftool.exe, copy it into either of:
echo   %target%
echo   the HomerScribe program folder
echo and it will be used.
echo(
echo Without one, pictures are still described and still renamed. Only the
echo writing of descriptions into the files themselves is skipped.
echo ExifTool setup finished %date% %time%>> "%log%"
exit /b 1

:done
echo ExifTool setup finished %date% %time%>> "%log%"
exit /b 0

rem ---- is there a single-file exiftool.exe in this folder? ----------------
rem Sets haveExif and exifVer if the folder holds an exiftool.exe that RUNS
rem and has no "exiftool_files" beside it. A folder beside the exe
rem disqualifies it whatever its version.
:trySingle
if defined haveExif goto :eof
if "%~1"=="" goto :eof
if not exist "%~1\exiftool.exe" goto :eof
if exist "%~1\exiftool_files" (
  echo Passing over %~1: it needs an exiftool_files folder beside it.>> "%log%"
  goto :eof
)
set "exifVer="
for /f "usebackq delims=" %%v in (`"%~1\exiftool.exe" -ver 2^>nul`) do set "exifVer=%%v"
if not defined exifVer (
  echo Passing over %~1: exiftool.exe would not run.>> "%log%"
  goto :eof
)
set "haveExif=%~1"
echo Found a single-file ExifTool at %~1, version %exifVer%>> "%log%"
goto :eof
