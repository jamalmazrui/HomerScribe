@echo off
rem installModels.cmd -- install the vision model HomerScribe describes with.
rem
rem HomerScribe needs ONE model, and it must be a VISION model: one that can be
rem shown a picture. qwen2.5vl:7b is the default, about 5.5 GB. A text-only model
rem such as llama3.2 cannot see the picture at all.
rem
rem Ollama is found by looking where it is installed, not with "where". A console
rem that was open before Ollama was installed keeps its old PATH and will not see
rem it -- which is how a tester was told Ollama was missing minutes after
rem installing it.
rem
rem   installModels.cmd                          the default model
rem   installModels.cmd qwen2.5vl:3b             a smaller, quicker one
rem   installModels.cmd qwen2.5vl:7b gemma3:4b   more than one

setlocal enabledelayedexpansion

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
set "models="
set "noPause="
for %%A in (%*) do (
  if /i "%%A"=="noPause" (set "noPause=1") else (set "models=!models! %%A")
)
if "!models!"=="" set "models=qwen2.5vl:7b"

call :findOllama
if not defined ollamaExe goto :noOllama

rem The service may still be starting, especially right after installation.
for /l %%N in (1,1,30) do (
  "!ollamaExe!" list >nul 2>&1
  if not errorlevel 1 goto :ready
  echo Waiting for Ollama to start...
  timeout /t 2 /nobreak >nul
)
echo(
echo Ollama is installed but not answering. Start it from the Start menu, then
echo run this again.
echo(
if not defined noPause pause
endlocal
exit /b 1

:ready
set "failed="
for %%M in (!models!) do call :oneModel %%M

echo(
"!ollamaExe!" list
echo(
if defined failed goto :someFailed
rem The flag says the pulls reported success. This says the models are there.
for %%M in (%models%) do (
  "!ollamaExe!" list 2>nul | findstr /i /c:"%%M" >nul
  if errorlevel 1 set "failed=1"
)
if defined failed goto :someFailed
echo HomerScribe is ready.
echo(
if not defined noPause pause
endlocal
exit /b 0

:oneModel
set "model=%~1"
"!ollamaExe!" list 2>nul | findstr /i /c:"%model%" >nul
if not errorlevel 1 (
  echo %model% is already installed.
  goto :eof
)
echo(
echo Installing %model%. This is several gigabytes and takes a while.
echo(
"!ollamaExe!" pull %model%
if errorlevel 1 set "failed=yes"
if errorlevel 1 echo %model% could not be pulled.
goto :eof

:noOllama
echo(
echo Ollama is not installed, so no model can be pulled.
echo Run installOllama.cmd first.
echo(
if not defined noPause pause
endlocal
exit /b 1

:someFailed
echo(
echo At least one model could not be pulled. Check that Ollama is running, then
echo try again, or pull it by hand, for example:
echo   ollama pull qwen2.5vl:7b
echo(
if not defined noPause pause
endlocal
exit /b 1

:findOllama
set "progFiles86=%ProgramFiles(x86)%"
set "ollamaExe="
where ollama >nul 2>&1
if not errorlevel 1 set "ollamaExe=ollama"
if not defined ollamaExe if exist "%LOCALAPPDATA%\Programs\Ollama\ollama.exe" set "ollamaExe=%LOCALAPPDATA%\Programs\Ollama\ollama.exe"
if not defined ollamaExe if exist "%ProgramFiles%\Ollama\ollama.exe" set "ollamaExe=%ProgramFiles%\Ollama\ollama.exe"
if not defined ollamaExe if exist "!progFiles86!\Ollama\ollama.exe" set "ollamaExe=!progFiles86!\Ollama\ollama.exe"
if not defined ollamaExe if exist "%USERPROFILE%\AppData\Local\Programs\Ollama\ollama.exe" set "ollamaExe=%USERPROFILE%\AppData\Local\Programs\Ollama\ollama.exe"
goto :eof
