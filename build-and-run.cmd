@echo off
rem Builds BlahBlah3000 into publish\ and starts it right away.
cd /d "%~dp0"
dotnet publish BlahBlah3000.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish -v quiet -nologo
if errorlevel 1 (
  echo.
  echo Build failed.
  pause
  exit /b 1
)
start "" "%~dp0publish\BlahBlah3000.exe"
