@echo off
rem Builds BlahBlah3000 into publish\ and starts it right away.
cd /d "%~dp0"

rem A running instance locks BlahBlah3000.exe and the native libraries, so the publish could not replace them.
tasklist /fi "imagename eq BlahBlah3000.exe" 2>nul | find /i "BlahBlah3000.exe" >nul
if not errorlevel 1 (
  echo BlahBlah3000 is running, closing it...
  taskkill /im BlahBlah3000.exe /f >nul 2>&1
  ping -n 3 127.0.0.1 >nul
)

dotnet publish BlahBlah3000.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish -v quiet -nologo
if errorlevel 1 goto failed

rem The Whisper model cannot load without the native libraries next to the exe (the CPU build and the Vulkan build).
for %%F in (
  publish\BlahBlah3000.exe
  publish\runtimes\win-x64\whisper.dll
  publish\runtimes\win-x64\ggml-whisper.dll
  publish\runtimes\vulkan\win-x64\whisper.dll
  publish\runtimes\vulkan\win-x64\ggml-vulkan-whisper.dll
) do (
  if not exist "%%F" (
    echo Missing after publish: %%F
    goto failed
  )
)

start "" "%~dp0publish\BlahBlah3000.exe"
exit /b 0

:failed
echo.
echo Build failed.
pause
exit /b 1
