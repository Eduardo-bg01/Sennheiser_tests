@echo off
setlocal enabledelayedexpansion

set "ROOT=%~dp0"
set "BIN_DIR=%ROOT%bin"
set "DOTNET=dotnet"
set "RUNTIME_ID=win-x64"
set "PUBLISH_FLAGS=-r %RUNTIME_ID% --self-contained true -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false"

echo Checking for .NET SDK...
for /f "usebackq tokens=*" %%S in (`%DOTNET% --list-sdks 2^>nul`) do goto :sdkfound
echo No .NET SDK is installed.
echo Install the .NET 9 SDK from: https://aka.ms/dotnet/download
exit /b 1

:sdkfound
echo.
echo Creating bin directory...
if not exist "%BIN_DIR%" mkdir "%BIN_DIR%"

echo Building SennheiserTestRunner...
%DOTNET% publish -c Release %PUBLISH_FLAGS% "%ROOT%apps\SennheiserTestRunner\SennheiserTestRunner.csproj" -o "%BIN_DIR%"
if errorlevel 1 (
    echo Build failed!
    exit /b 1
)

echo.
echo Copying runtime files...
copy /Y "%ROOT%run.bat" "%BIN_DIR%\run.bat" >nul
if exist "%ROOT%show_bluetooth_connect.ps1" copy /Y "%ROOT%show_bluetooth_connect.ps1" "%BIN_DIR%\show_bluetooth_connect.ps1" >nul
if exist "%ROOT%show_bluetooth_disconnect.ps1" copy /Y "%ROOT%show_bluetooth_disconnect.ps1" "%BIN_DIR%\show_bluetooth_disconnect.ps1" >nul
if exist "%ROOT%scripts" (
    if not exist "%BIN_DIR%\scripts" mkdir "%BIN_DIR%\scripts"
    xcopy /E /I /Y "%ROOT%scripts\*" "%BIN_DIR%\scripts\" >nul
)
if exist "%ROOT%miniDSP.jpg" copy /Y "%ROOT%miniDSP.jpg" "%BIN_DIR%\miniDSP.jpg" >nul
if exist "%ROOT%serial.txt" copy /Y "%ROOT%serial.txt" "%BIN_DIR%\serial.txt" >nul

REM Copy content assets that are referenced by path (not embedded) from child projects
if exist "%ROOT%apps\MicroTestCloud\MicroTestCloud\PistaAudio" (
    if not exist "%BIN_DIR%\PistaAudio" mkdir "%BIN_DIR%\PistaAudio"
    xcopy /E /I /Y "%ROOT%apps\MicroTestCloud\MicroTestCloud\PistaAudio\*" "%BIN_DIR%\PistaAudio\" >nul
)
if exist "%ROOT%apps\pruebasAudifonos\AudioTest\AudioTest\karmaPolice.wav" (
    copy /Y "%ROOT%apps\pruebasAudifonos\AudioTest\AudioTest\karmaPolice.wav" "%BIN_DIR%\karmaPolice.wav" >nul
)
if exist "%ROOT%apps\pruebasAudifonos\LevelTest\HeadPhoneTest2\audio" (
    if not exist "%BIN_DIR%\audio" mkdir "%BIN_DIR%\audio"
    xcopy /E /I /Y "%ROOT%apps\pruebasAudifonos\LevelTest\HeadPhoneTest2\audio\*" "%BIN_DIR%\audio\" >nul
)

echo.
echo Build complete. Executable: %BIN_DIR%\SennheiserTestRunner.exe
echo Run tests with: %BIN_DIR%\run.bat
exit /b 0
