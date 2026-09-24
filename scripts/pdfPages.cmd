@echo off
setLocal
rem pdfPages.cmd -- turn a scanned PDF into a zip of page pictures that
rem HomerScribe can describe.
rem
rem Takes no arguments: run it and it converts every PDF sitting beside it.
rem Give it paths and it converts those instead.
rem
rem     pdfPages.cmd
rem     pdfPages.cmd "C:\Scans\HMS_Vol_15_No_1.pdf"
rem
rem The wrapper exists so nobody has to remember how to invoke Python. It
rem passes its own arguments straight through.

set "here=%~dp0"
if "%here:~-1%"=="\" set "here=%here:~0,-1%"

set "py="
where py >nul 2>&1 && set "py=py -3"
if not defined py where python >nul 2>&1 && set "py=python"
if not defined py (
  echo Python was not found on this computer.
  echo pdfPages needs it to read PDF files. Install it from python.org,
  echo or from the Microsoft Store, then run this again.
  exit /b 9
)

%py% "%here%\pdfPages.py" %*
exit /b %errorlevel%
