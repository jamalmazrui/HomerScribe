@echo off
rem installModels.cmd -- install the vision model HomerDescribe describes with.
rem
rem HomerDescribe needs ONE model: a vision model, which is a model that can be
rem shown a picture. qwen2.5vl:7b is the default, about 5.5 GB. A text-only model
rem such as llama3.2 cannot see the picture at all and will describe nothing, so
rem having one installed does not help.
rem
rem   installModels.cmd                     install the default model
rem   installModels.cmd qwen2.5vl:3b        install a different one instead
rem   installModels.cmd qwen2.5vl:7b gemma3:4b   install more than one

setlocal enabledelayedexpansion
set "models=%*"
if "%models%"=="" set "models=qwen2.5vl:7b"

where ollama >nul 2>&1
if errorlevel 1 goto :noOllama

set "failed="
for %%M in (%models%) do call :oneModel %%M

echo(
ollama list
echo(
if defined failed goto :someFailed
echo HomerDescribe is ready. Try:  HomerDescribe --check
echo(
pause
endlocal
exit /b 0

:oneModel
set "model=%~1"
rem Already here? Then say so and move on rather than re-fetching gigabytes.
ollama list 2>nul | findstr /i /c:"%model%" >nul
if not errorlevel 1 (
  echo %model% is already installed.
  goto :eof
)
echo(
echo Installing %model%. This is several gigabytes and takes a while.
echo(
ollama pull %model%
if errorlevel 1 set "failed=yes"
if errorlevel 1 echo %model% could not be pulled.
goto :eof

:noOllama
echo(
echo Ollama is not installed yet, so no model can be pulled.
echo Run installOllama.cmd first, then run this again.
echo(
pause
endlocal
exit /b 1

:someFailed
echo(
echo At least one model could not be pulled. Check that Ollama is running,
echo then try again, or pull it by hand, for example:
echo   ollama pull qwen2.5vl:7b
echo(
pause
endlocal
exit /b 1
