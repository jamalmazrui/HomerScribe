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
rem installTextModel.cmd -- fetch the model that READS, for removing ads.
rem
rem HomerScribe uses two kinds of model and they do different work.
rem
rem   qwen2.5vl:7b   the PICTURE model. Looks at a frame of film, a photograph
rem                  or a scanned page. Installed by installModels.cmd.
rem   qwen2.5:7b     the READING model. Reads an hour of transcript and finds
rem                  the advertisements in it. That job has no picture in it
rem                  anywhere, and asking the picture model to do it works
rem                  badly: on one 58-minute episode it found ONE
rem                  advertisement and removed 23 seconds, where this model
rem                  found FIVE and removed 2 minutes 19.
rem
rem It is only needed for Remove ads. Transcribing uses Whisper and describing
rem uses the picture model; neither is affected by this one way or the other.
rem
rem Takes no arguments. Writes a detailed log beside this script.

set "here=%~dp0"
if "%here:~-1%"=="\" set "here=%here:~0,-1%"
rem the log path is set above
set "model=qwen2.5:7b"

echo Reading model install started %date% %time%>> "%log%"
echo Script: %~f0>> "%log%"
echo Model: %model%>> "%log%"
echo Command line: %0 %*>> "%log%"
echo(>> "%log%"

where ollama >nul 2>&1
if errorlevel 1 (
  echo Ollama is not installed yet. Run installOllama.cmd first, then this.
  echo Ollama was not found.>> "%log%"
  exit /b 1
)

rem Already here? A pull on a model that is present still contacts the server
rem and checks every layer, which is time and bandwidth for nothing.
echo Checking whether %model% is already installed.>> "%log%"
ollama list > "%TEMP%\homerTextModel.txt" 2>&1
type "%TEMP%\homerTextModel.txt" >> "%log%"
findstr /i /c:"%model%" "%TEMP%\homerTextModel.txt" >nul 2>&1
if not errorlevel 1 (
  del "%TEMP%\homerTextModel.txt" >nul 2>&1
  echo %model% is already installed. Nothing to do.
  echo Already installed; no download needed.>> "%log%"
  echo Reading model install finished %date% %time%>> "%log%"
  exit /b 0
)
del "%TEMP%\homerTextModel.txt" >nul 2>&1

echo Fetching %model%. This is about 4.7 GB and happens once.
ollama pull %model% >> "%log%" 2>&1
if errorlevel 1 (
  echo(
  echo %model% could not be fetched. The log says why: %log%
  echo Remove ads will still work using the picture model, but it will find
  echo fewer advertisements.
  echo Pull failed>> "%log%"
  exit /b 1
)

echo(
rem THE MODEL, NOT THE EXIT CODE. `ollama pull` can return zero and leave
rem nothing usable -- an interrupted download, a name that no longer exists,
rem a daemon that was not running. So the list is SEARCHED for the model
rem rather than merely logged.
ollama list 2>nul | findstr /i /c:"%model%" >nul
if errorlevel 1 (
  echo(
  echo %model% did NOT install. The command reported success and the model is
  echo not in the list. The log has the detail: %log%
  call "%~dp0homerInstall.cmd" log "FAILED: ollama reported success but %model% is not listed"
  if not defined noPause if not defined HOMER_QUIET pause
  exit /b 1
)
echo %model% is installed. HomerScribe will use it for finding advertisements
echo and will go on using the picture model for describing.
echo Installed %model%>> "%log%"
echo Reading model install finished %date% %time%>> "%log%"
exit /b 0
