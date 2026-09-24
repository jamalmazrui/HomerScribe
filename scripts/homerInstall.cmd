@echo off
rem homerInstall.cmd -- the common half of every Homer install<Component>.cmd.
rem
rem Called, not run: an install script does `call "%~dp0homerInstall.cmd" setup`
rem and then uses what this sets. It exists because the same forty lines had
rem been copied into a dozen scripts across four apps and had drifted in every
rem one -- EdSharp logging to its own folder, HomerScribe logging beside itself
rem inside Program Files, two logging nowhere at all.
rem
rem WHAT IT SETS
rem   sApp      the application name, taken from the folder the caller sits in
rem   sLogDir   %LOCALAPPDATA%\<app>\logs
rem   log       a full path, <App>-<script>-yyyyMMdd-HHmmss.log
rem   sQuiet    1 when the installer is driving and nothing may wait for a key
rem
rem WHAT IT DOES NOT DO
rem   It does not hide the console. The console says briefly what was found and
rem   what was done; this file's log holds the detail, so a failure can be
rem   diagnosed from a file rather than from a window that has closed.

if /i "%~1"=="setup" goto :setup
if /i "%~1"=="log" goto :writeLog
if /i "%~1"=="say" goto :saySo
exit /b 0

:setup
rem The app is the folder the CALLING script lives in, or its parent when that
rem folder is "scripts" -- so one file serves an app whatever it is called.
for %%d in ("%~dp1.") do set "sApp=%%~nxd"
if not defined sCallerDir set "sCallerDir=%~dp1"
for %%d in ("%sCallerDir%.") do set "sApp=%%~nxd"
if /i "%sApp%"=="scripts" for %%d in ("%sCallerDir%..") do set "sApp=%%~nxd"
if /i "%sApp%"=="exec" for %%d in ("%sCallerDir%..") do set "sApp=%%~nxd"
if not defined sApp set "sApp=Homer"

set "sLogDir=%LOCALAPPDATA%\%sApp%\logs"
if not exist "%sLogDir%" mkdir "%sLogDir%" >nul 2>&1

for /f "tokens=2 delims==" %%T in ('wmic os get localdatetime /value 2^>nul') do set "sNow=%%T"
if not defined sNow set "sNow=00000000000000"
set "log=%sLogDir%\%sApp%-%sScript%-%sNow:~0,8%-%sNow:~8,6%.log"

set "sQuiet="
if defined HOMER_QUIET set "sQuiet=1"

call "%~f0" log "%sScript% started %DATE% %TIME%"
call "%~f0" log "Script: %sCallerDir%%sScript%.cmd"
call "%~f0" log "App: %sApp%"
call "%~f0" log "Log: %log%"
call "%~f0" log "Quiet: %sQuiet%"
exit /b 0

:writeLog
rem Always appends. A single > here would erase the header written above, and
rem the log would arrive missing exactly the part that says what machine it
rem ran on.
echo %~2>> "%log%" 2>nul
exit /b 0

:saySo
rem One line on the console, and the same line in the log, so the two agree.
echo %~2
echo %~2>> "%log%" 2>nul
exit /b 0
