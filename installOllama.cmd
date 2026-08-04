@echo off
rem installOllama.cmd -- install Ollama, the local AI service HomerDescribe uses.
rem
rem Run from the installer's final page, or on its own at any time. winget ships
rem with Windows 10 and 11, so nothing has to be downloaded by hand.

where ollama >nul 2>&1
if not errorlevel 1 goto :already

echo Installing Ollama. This is about 1 GB and takes a few minutes.
echo(
winget install --id Ollama.Ollama --exact --accept-source-agreements --accept-package-agreements
if errorlevel 1 goto :failed

echo(
echo Ollama is installed. It runs as a service and starts with Windows.
echo Next, install the vision model with installModels.cmd.
echo(
pause
exit /b 0

:already
echo(
echo Ollama is already installed.
ollama --version
echo(
echo Next, install the vision model with installModels.cmd.
echo(
pause
exit /b 0

:failed
echo(
echo Ollama could not be installed automatically.
echo Download it instead from https://ollama.com/download
echo(
pause
exit /b 1
