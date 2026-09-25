@echo off
rem gitPush.cmd -- stage, commit and push this project, and say what happened.
rem
rem     gitPush                     commit with a default message
rem     gitPush "Add the prefix field."   commit with your own
rem
rem WHAT IT STAGES is whatever .gitignore allows, and in a Homer project that is
rem a whitelist generated from RepoFiles.txt. So "git add -A" here means "add
rem everything the project has named", not "add everything in the folder". If a
rem new file does not go up, add its line to RepoFiles.txt and run
rem homerTidy --gitignore.
rem
rem It refuses to run outside a git repository rather than creating one by
rem accident, and it writes gitPush.log beside this script.
setlocal enabledelayedexpansion
rem The log goes in the project's logs folder, one file per run, named like
rem every other Homer log. The project is the current folder: this script
rem acts on the folder it is run in, as tagRelease does.
if not exist "%CD%\logs" mkdir "%CD%\logs"
for /f %%T in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd-HHmmss"') do set "sNow=%%T"
for %%I in ("%CD%") do set "sApp=%%~nxI"
set "log=%CD%\logs\%sApp%-push-%sNow%.log"
set "message=%~1"
if "%message%"=="" set "message=Fix."

> "%log%" echo gitPush started %date% %time%
>> "%log%" echo Folder: %CD%
>> "%log%" echo Message: %message%

git rev-parse --is-inside-work-tree >nul 2>&1
if errorlevel 1 (
    echo This folder is not a git repository. Run create^<App^>Repo first.
    echo NOT A REPOSITORY>> "%log%"
    endlocal
    exit /b 1
)

git add -A >> "%log%" 2>&1
git commit -m "%message%" >> "%log%" 2>&1
if errorlevel 1 echo Nothing to commit, so nothing was pushed.
git push >> "%log%" 2>&1
if errorlevel 1 (
    echo The push failed. The log has why: %log%
    echo PUSH FAILED>> "%log%"
    endlocal
    exit /b 1
)
git status --short --branch
>> "%log%" echo gitPush finished %date% %time%
echo Pushed: %message%
endlocal & exit /b 0
