@echo off
setLocal
rem installPandoc.cmd -- fetch Pandoc, which writes the Word version of a PDF.
rem
rem NO MICROSOFT OFFICE IS INVOLVED, and that is deliberate. A .docx is a zip of
rem XML and anything can write one; the common shortcut is to drive Word itself
rem through its COM interface, which needs Office installed, licensed, and
rem running, and which fails on a machine that has none. HomerScribe uses
rem Pandoc instead: free, open source under the GPL, a single program with no
rem runtime behind it, and the same converter the other Homer Tools already use.
rem
rem Optional. Without it HomerScribe still writes described.md, and says in the
rem log that no Word version was made.
rem
rem Takes no arguments. Writes a detailed log beside this script.

set "here=%~dp0"
if "%here:~-1%"=="\" set "here=%here:~0,-1%"
set "log=%here%\installPandoc.log"

echo Pandoc install started %date% %time%> "%log%"
echo Script: %~f0>> "%log%"
echo Folder: %here%>> "%log%"
echo Command line: %0 %*>> "%log%"
echo(>> "%log%"

where pandoc >nul 2>&1
if not errorlevel 1 (
  echo Pandoc is already installed.
  echo Already on the path>> "%log%"
  pandoc --version >> "%log%" 2>&1
  exit /b 0
)

where winget >nul 2>&1
if errorlevel 1 (
  echo(
  echo winget was not found, so Pandoc could not be fetched automatically.
  echo HomerScribe will still write the Markdown version of a PDF.
  echo winget not found>> "%log%"
  exit /b 1
)

echo Fetching Pandoc. This is about 30 MB and happens once.
echo Fetching with winget>> "%log%"
winget install --id JohnMacFarlane.Pandoc --accept-source-agreements --accept-package-agreements --silent >> "%log%" 2>&1

where pandoc >nul 2>&1
if errorlevel 1 (
  echo(
  echo Pandoc could not be fetched. The log says what was tried: %log%
  echo HomerScribe will still write the Markdown version of a PDF.
  echo Install failed>> "%log%"
  exit /b 1
)

echo(
echo Pandoc is installed. A PDF will now produce a Word version alongside the
echo Markdown, paginated the same way as the original.
pandoc --version >> "%log%" 2>&1
echo Installed>> "%log%"
echo Pandoc install finished %date% %time%>> "%log%"
exit /b 0
