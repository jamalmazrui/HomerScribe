@echo off
rem acceptReuse.cmd -- runs acceptReuse.py, passing its arguments on.
python "%~dp0acceptReuse.py" %*
exit /b %errorlevel%
