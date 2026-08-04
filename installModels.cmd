@echo off
rem installModels.cmd -- install the vision model HomerDescribe describes with.
rem
rem HomerDescribe needs ONE model, and it must be a VISION model: one that can be
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
echo HomerDescribe is ready.
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
