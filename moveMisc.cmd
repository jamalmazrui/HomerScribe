@echo off
rem moveMisc.cmd -- move the contents of an accidentally unpacked Word document
rem out of C:\HomerScribe and into C:\HomerDescribe.
rem
rem A .docx is a zip archive. Unpacking one in place scatters its insides across
rem the folder it was unpacked into: [Content_Types].xml, and the _rels, docProps
rem and word folders. None of those belong in the source of a program, and one of
rem them, "word", is close enough to a real folder name to be easy to overlook.
rem
rem Nothing else is touched. The announcement drafts (Facebook, LinkedIn, devs,
rem in .md and .docx) are left where they are: .gitignore already keeps them out
rem of the repository, and they are wanted beside the project.
rem
rem A log is written beside this script.

setlocal enabledelayedexpansion
set "here=%~dp0"
set "here=%here:~0,-1%"
set "log=%here%\moveMisc.log"
set "target=C:\HomerDescribe"
set "moved=0"
set "missing=0"
set "failed=0"

echo moveMisc started %date% %time%> "%log%"
echo Script: %~f0>> "%log%"
echo Working directory: %here%>> "%log%"
echo Command line: %0 %*>> "%log%"
echo Target: %target%>> "%log%"
echo(>> "%log%"

if not exist "%target%" goto :makeTarget
echo Target already exists.>> "%log%"
goto :haveTarget

:makeTarget
echo Creating %target%>> "%log%"
mkdir "%target%" 2>> "%log%"
if errorlevel 1 goto :noTarget
echo Created.>> "%log%"

:haveTarget
call :moveOne "[Content_Types].xml"
call :moveFolder "_rels"
call :moveFolder "docProps"
call :moveFolder "word"

echo(>> "%log%"
echo Moved: %moved%  Not present: %missing%  Failed: %failed%>> "%log%"
echo moveMisc finished %date% %time%>> "%log%"
echo(
echo Moved %moved% item(s) to %target%. Not present: %missing%. Failed: %failed%.
echo Details are in %log%
echo(
if %failed% GTR 0 goto :someFailed
endlocal
exit /b 0

:moveOne
set "item=%here%\%~1"
if not exist "%item%" goto :notThere
echo Moving file %~1>> "%log%"
move /y "%item%" "%target%\" >> "%log%" 2>&1
if errorlevel 1 goto :moveFailed
set /a moved+=1
echo   moved.>> "%log%"
goto :eof

:moveFolder
set "item=%here%\%~1"
if not exist "%item%\" goto :notThere
echo Moving folder %~1>> "%log%"
if exist "%target%\%~1\" rmdir /s /q "%target%\%~1" >> "%log%" 2>&1
move /y "%item%" "%target%\" >> "%log%" 2>&1
if errorlevel 1 goto :moveFailed
set /a moved+=1
echo   moved.>> "%log%"
goto :eof

:notThere
echo Not present, nothing to do: %~1>> "%log%"
set /a missing+=1
goto :eof

:moveFailed
echo   FAILED to move %~1>> "%log%"
set /a failed+=1
goto :eof

:noTarget
echo ERROR: %target% could not be created. Nothing has been moved.>> "%log%"
echo(
echo ERROR: %target% could not be created. Nothing has been moved.
echo See %log%
echo(
endlocal
exit /b 1

:someFailed
echo Some items could not be moved. They may be open in another program.
echo(
endlocal
exit /b 1
