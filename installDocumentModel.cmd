@echo off
setLocal
rem installDocumentModel.cmd -- fetch the model that READS a page of print.
rem
rem THIS IS FOR A COMPUTER SHORT OF MEMORY, NOT FOR BETTER RESULTS.
rem
rem The ordinary picture model HomerScribe installs is Qwen2.5-VL at 7B, and it
rem is already one of the best document readers there is: second of twenty-six
rem models on the DocVQA leaderboard at 95.7 percent, behind only its own 72B
rem sibling, and 883 on OCRBench. It reads a scanned page very well.
rem
rem granite3.2-vision is IBM's, built for documents, and about 2.4 GB against
rem the picture model's 5.5. It is strong FOR ITS SIZE -- which is not the same
rem as better. Fetch it if 5.5 GB is more than the machine can hold, and set
rem --document-model granite3.2-vision to use it for pages.
rem
rem Most people should skip this and use what they already have.
rem
rem Takes no arguments. Writes a detailed log beside this script.

set "here=%~dp0"
if "%here:~-1%"=="\" set "here=%here:~0,-1%"
set "log=%here%\installDocumentModel.log"
set "model=granite3.2-vision"

echo Document model install started %date% %time%> "%log%"
echo Script: %~f0>> "%log%"
echo Folder: %here%>> "%log%"
echo Command line: %0 %*>> "%log%"
echo Model: %model%>> "%log%"
echo Windows: %OS%, processor %PROCESSOR_ARCHITECTURE%>> "%log%"
echo(>> "%log%"

where ollama >nul 2>&1
if errorlevel 1 (
  echo Ollama was not found.>> "%log%"
  echo(
  echo Ollama is not installed yet. Run installOllama.cmd first, then this.
  echo Document model install FAILED %date% %time%>> "%log%"
  exit /b 1
)

rem ---- is it already here? -----------------------------------------------
rem "ollama pull" on a model that is already present still contacts the server
rem and checks every layer's digest, which takes time and bandwidth for
rem nothing. Ask first. Every other component in HomerScribe is checked this
rem way and this one was not.
echo Checking whether %model% is already installed.>> "%log%"
ollama list > "%TEMP%\homerModels.txt" 2>&1
type "%TEMP%\homerModels.txt" >> "%log%"
findstr /b /i /c:"%model%" "%TEMP%\homerModels.txt" >nul 2>&1
if not errorlevel 1 (
  del "%TEMP%\homerModels.txt" >nul 2>&1
  echo %model% is already installed. Nothing to do.
  echo Already installed; no download needed.>> "%log%"
  echo Document model install finished %date% %time%>> "%log%"
  exit /b 0
)
rem A model can be listed as "granite3.2-vision:latest", so the bare name is
rem checked too rather than only an exact line.
findstr /i /c:"%model%" "%TEMP%\homerModels.txt" >nul 2>&1
if not errorlevel 1 (
  del "%TEMP%\homerModels.txt" >nul 2>&1
  echo %model% is already installed. Nothing to do.
  echo Already installed under a tag; no download needed.>> "%log%"
  echo Document model install finished %date% %time%>> "%log%"
  exit /b 0
)
del "%TEMP%\homerModels.txt" >nul 2>&1

echo Fetching %model%. This is about 2.4 GB and happens once.
echo Fetching %model%>> "%log%"
ollama pull %model% >> "%log%" 2>&1
if errorlevel 1 (
  echo(
  echo %model% could not be fetched. The log says why: %log%
  echo(
  echo HomerScribe will still read pages using the ordinary picture model.
  echo Pull failed>> "%log%"
  echo Document model install FAILED %date% %time%>> "%log%"
  exit /b 1
)

echo Checking it is really there.>> "%log%"
ollama list >> "%log%" 2>&1
echo(
echo %model% is installed. HomerScribe will use it for any picture that is
echo mostly print, and the ordinary model for photographs.
echo Installed %model%>> "%log%"
echo Document model install finished %date% %time%>> "%log%"
exit /b 0
