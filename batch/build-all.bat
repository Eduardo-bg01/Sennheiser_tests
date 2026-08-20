@echo off
setlocal enabledelayedexpansion

set "ROOT=%~dp0..\"
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

REM Build all projects in sequence
echo Building projects...
call :build_project "VolumeHelper" "%ROOT%tools\VolumeHelper\VolumeHelper.csproj" "temp0" "" || goto :fail
call :build_project "BluetoothHeadphoneTest" "%ROOT%apps\FunctionalButtonTest\BluetoothHeadphoneTest.csproj" "temp1" "" || goto :fail
call :build_project "MicroTestCloud" "%ROOT%apps\MicroTestCloud\MicroTestCloud\MicroTestCloud.csproj" "temp2" "%ROOT%apps\MicroTestCloud\MicroTestCloud\PistaAudio" || goto :fail
call :build_project "AskForSerial2" "%ROOT%apps\pruebasAudifonos\AskForSerial2\AskForSerial2\AskForSerial2.csproj" "temp3" "" || goto :fail
call :build_project "AudioTest" "%ROOT%apps\pruebasAudifonos\AudioTest\AudioTest\AudioTest.csproj" "temp4" "" || goto :fail
call :copy_audio_asset "%ROOT%apps\pruebasAudifonos\AudioTest\AudioTest\karmaPolice.wav"
call :build_project "LevelTest" "%ROOT%apps\pruebasAudifonos\LevelTest\HeadPhoneTest2\HeadPhoneTest2.csproj" "temp5" "%ROOT%apps\pruebasAudifonos\LevelTest\HeadPhoneTest2\audio" || goto :fail

echo.
echo All builds completed successfully.
echo.
echo Copying runtime files...
copy /Y "%~dp0run.bat" "%BIN_DIR%\run.bat" >nul
if exist "%ROOT%show_bluetooth.ps1" copy /Y "%ROOT%show_bluetooth.ps1" "%BIN_DIR%\show_bluetooth.ps1" >nul
call :copy_asset_folder "%ROOT%scripts" "%BIN_DIR%\scripts" || goto :fail
if exist "%ROOT%miniDSP.jpg" copy /Y "%ROOT%miniDSP.jpg" "%BIN_DIR%\miniDSP.jpg" >nul
if exist "%ROOT%serial.txt" copy /Y "%ROOT%serial.txt" "%BIN_DIR%\serial.txt" >nul

echo.
echo Packaging complete. Executables: %BIN_DIR%
echo Run tests with: %BIN_DIR%\run.bat
exit /b 0

REM ========== Subroutines ==========

:build_project
REM Parameters: %1=name, %2=csproj, %3=tempdir, %4=assets
echo Building %~1...
%DOTNET% publish -c Release %PUBLISH_FLAGS% "%~2" -o "%BIN_DIR%\%~3"
if errorlevel 1 exit /b 1
call :copy_publish_output "%BIN_DIR%\%~3"
if errorlevel 1 exit /b 1
if not "%~4"=="" (
    for %%F in (%~4) do set "ASSET_NAME=%%~nxF"
    call :copy_asset_folder "%~4" "%BIN_DIR%\!ASSET_NAME!"
    if errorlevel 1 exit /b 1
)
exit /b 0

:copy_audio_asset
if exist "%~1" (
    copy /Y "%~1" "%BIN_DIR%\" >nul
) else (
    echo Warning: missing audio asset %~1
)
exit /b 0

:copy_publish_output
set "SOURCE_DIR=%~1"
robocopy "%SOURCE_DIR%" "%BIN_DIR%" /E /NFL /NDL /NJH /NJS /NP >nul
if errorlevel 8 exit /b 1
rmdir /s /q "%SOURCE_DIR%" >nul 2>&1
exit /b 0

:copy_asset_folder
set "SOURCE=%~1"
set "TARGET=%~2"
if exist "%SOURCE%" (
    robocopy "%SOURCE%" "%TARGET%" /E /NFL /NDL /NJH /NJS /NP >nul
) else (
    echo Warning: asset folder not found %SOURCE%
    exit /b 0
)
exit /b 0

:fail
echo Build failed!
exit /b 1
