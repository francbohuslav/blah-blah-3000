@echo off
rem Only starts an already built BlahBlah3000 (building is done by build-and-run.cmd).
if not exist "%~dp0publish\BlahBlah3000.exe" (
  echo publish\BlahBlah3000.exe is missing, run build-and-run.cmd first.
  pause
  exit /b 1
)
start "" "%~dp0publish\BlahBlah3000.exe"
