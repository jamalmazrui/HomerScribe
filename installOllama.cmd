@echo off
rem installOllama.cmd -- install Ollama, the local AI service HomerDescribe uses.
rem
rem Run from the installer's final page, or on its own at any time.
rem
rem Two things learnt from a tester's first install:
rem
rem 1. Without --silent, winget hands over to Ollama's own installer, which puts
rem    up windows and may open a browser. They have to be closed by hand and it
rem    is not clear whether anything is still running.
rem
rem 2. After installing, ollama is NOT on the PATH of any console that was
rem    already open, including this one. Anything that looks for it with "where"
rem    will not find it, and will wrongly report that Ollama is not installed.
rem    So this script finds the program by looking where it is put.

setlocal enabledelayedexpansion

call :findOllama
if defined ollamaExe goto :already

echo Installing Ollama. This is about 1 GB and takes a few minutes.
echo Nothing is asked of you while it runs.
echo(
winget install --id Ollama.Ollama --exact --silent --accept-source-agreements --accept-package-agreements
if errorlevel 1 goto :failed

echo(
echo Waiting for Ollama to start.
call :findOllama
if not defined ollamaExe goto :installedButLost

rem The service takes a few seconds to answer after the program appears.
for /l %%N in (1,1,30) do (
  "!ollamaExe!" list >nul 2>&1
  if not errorlevel 1 goto :ready
  timeout /t 2 /nobreak >nul
)

:ready
echo(
echo Ollama is installed and running. It starts with Windows from now on.
echo(
echo Installing the vision model next.
echo(
if exist "%~dp0installModels.cmd" call "%~dp0installModels.cmd" noPause
echo(
pause
endlocal
exit /b 0

:already
echo(
echo Ollama is already installed.
"!ollamaExe!" --version
echo(
echo Next, install the vision model with installModels.cmd.
echo(
pause
endlocal
exit /b 0

:installedButLost
echo(
echo Ollama was installed, but this window cannot see it yet, which is normal:
echo a console keeps the PATH it started with. Open a NEW command window, or
echo run installModels.cmd from the Start menu folder, to install the model.
echo(
pause
endlocal
exit /b 0

:failed
echo(
echo Ollama could not be installed automatically.
echo Download it instead from https://ollama.com/download
echo(
pause
endlocal
exit /b 1

:findOllama
rem The parenthesis in the variable name ProgramFiles(x86) must not appear
rem inside a parenthesised block, so it is copied out first.
set "progFiles86=%ProgramFiles(x86)%"
set "ollamaExe="
where ollama >nul 2>&1
if not errorlevel 1 set "ollamaExe=ollama"
if not defined ollamaExe if exist "%LOCALAPPDATA%\Programs\Ollama\ollama.exe" set "ollamaExe=%LOCALAPPDATA%\Programs\Ollama\ollama.exe"
if not defined ollamaExe if exist "%ProgramFiles%\Ollama\ollama.exe" set "ollamaExe=%ProgramFiles%\Ollama\ollama.exe"
if not defined ollamaExe if exist "!progFiles86!\Ollama\ollama.exe" set "ollamaExe=!progFiles86!\Ollama\ollama.exe"
if not defined ollamaExe if exist "%USERPROFILE%\AppData\Local\Programs\Ollama\ollama.exe" set "ollamaExe=%USERPROFILE%\AppData\Local\Programs\Ollama\ollama.exe"
goto :eof
