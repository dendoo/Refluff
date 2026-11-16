@echo off
rem Windows blocks unsigned .ps1 files by default; this runs upload.ps1 with the policy relaxed for this call only.
rem Usage: upload.bat -Account <builder_login> [-Desc "text"] [-Preview]
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0upload.ps1" %*
exit /b %ERRORLEVEL%
