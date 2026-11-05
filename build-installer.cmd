@echo off
rem Publishes BlahBlah3000 to installer\staging and builds the Inno Setup installer into installer\output.
cd /d "%~dp0"

if exist installer\staging rmdir /s /q installer\staging
dotnet publish BlahBlah3000.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o installer\staging -v quiet -nologo
if errorlevel 1 goto failed

rem Whisper.net ships native libraries for every platform; the Linux ones are useless in a Windows installer.
powershell -NoProfile -Command "Get-ChildItem installer\staging\runtimes -Directory -Recurse | Where-Object Name -like 'linux-*' | Remove-Item -Recurse -Force"
if errorlevel 1 goto failed

set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" (
  echo Inno Setup 6 was not found.
  goto failed
)

"%ISCC%" /Qp installer\BlahBlah3000.iss
if errorlevel 1 goto failed

echo.
echo Installer created in installer\output.
exit /b 0

:failed
echo.
echo Build failed.
pause
exit /b 1
