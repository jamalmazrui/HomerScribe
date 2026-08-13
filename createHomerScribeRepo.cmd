@echo off
setlocal EnableExtensions
rem createHomerScribeRepo.cmd -- thin wrapper that runs the PowerShell worker
rem and captures everything to createHomerScribeRepo.log, in the manner of the
rem createbookFidoRepo pipeline. PowerShell does the git and gh work, because
rem invoking gh from a batch file falls into a batch trap when gh is installed as
rem a .cmd shim: without "call", control transfers to the shim and never returns,
rem ending the script silently.
rem
rem ONE-TIME bootstrap: turns this folder into a git repository, creates
rem https://github.com/JamalMazrui/HomerScribe, and pushes the first commit.
rem After this, publishing a release is tagRelease's job.
rem
rem This is a MAINTAINER script. It is listed in .gitignore, so it is not part of
rem the HomerScribe distribution and never shows up in the public source
rem browser -- the same treatment as tagRelease.
rem
rem Usage:
rem   createHomerScribeRepo.cmd            create the repo and push
rem   createHomerScribeRepo.cmd -DryRun    report what would happen, changing
rem                                          nothing. Worth running first.
rem
rem Requirements: git and gh on the PATH, gh authenticated (gh auth login),
rem PowerShell 5.1 or later.

if not defined sLogging (
    set "sLogging=1"
    cmd /d /c ""%~f0"" %* > "%~dp0createHomerScribeRepo.log" 2>&1
    type "%~dp0createHomerScribeRepo.log"
    exit /b
)

cd /d "%~dp0"
if not exist "%~dp0createHomerScribeRepo.ps1" (
    echo ERROR: createHomerScribeRepo.ps1 was not found next to this script.
    exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0createHomerScribeRepo.ps1" %*
exit /b %errorlevel%
