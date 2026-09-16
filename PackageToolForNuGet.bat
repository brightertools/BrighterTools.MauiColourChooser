@echo off
pwsh -NoProfile -File "%~dp0eng\pack.ps1" %*
exit /b %ERRORLEVEL%
