@echo off
setlocal enabledelayedexpansion

set "ROOT=%~dp0"
set "BIN_DIR=%ROOT%bin"
set "DOTNET=dotnet"

echo Checking for .NET SDK...
for /f "usebackq tokens=*" %%S in (`%DOTNET% --list-sdks 2^>nul`) do goto :sdkfound
echo No .NET SDK is installed.
echo Install the .NET 9 SDK from: https://aka.ms/dotnet/download
exit /b 1

:sdkfound
echo.
echo Creating bin directory...
if not exist "%BIN_DIR%" mkdir "%BIN_DIR%"

echo.
echo Building BluetoothHeadphoneTest...
%DOTNET% publish -c Release "%ROOT%apps\FunctionalButtonTest\BluetoothHeadphoneTest.csproj" -o "%BIN_DIR%\temp1"
if errorlevel 1 goto :fail
move "%BIN_DIR%\temp1\BluetoothHeadphoneTest.exe" "%BIN_DIR%\" >nul 2>&1
rmdir /s /q "%BIN_DIR%\temp1" >nul 2>&1

echo Building MicroTestCloud...
%DOTNET% publish -c Release "%ROOT%apps\MicroTestCloud\MicroTestCloud\MicroTestCloud.csproj" -o "%BIN_DIR%\temp2"
if errorlevel 1 goto :fail
move "%BIN_DIR%\temp2\MicroTestCloud.exe" "%BIN_DIR%\" >nul 2>&1
rmdir /s /q "%BIN_DIR%\temp2" >nul 2>&1

echo Building AskForSerial2...
%DOTNET% publish -c Release "%ROOT%apps\pruebasAudifonos\AskForSerial2\AskForSerial2\AskForSerial2.csproj" -o "%BIN_DIR%\temp3"
if errorlevel 1 goto :fail
move "%BIN_DIR%\temp3\AskForSerial2.exe" "%BIN_DIR%\" >nul 2>&1
rmdir /s /q "%BIN_DIR%\temp3" >nul 2>&1

echo Building AudioTest...
%DOTNET% publish -c Release "%ROOT%apps\pruebasAudifonos\AudioTest\AudioTest\AudioTest.csproj" -o "%BIN_DIR%\temp4"
if errorlevel 1 goto :fail
move "%BIN_DIR%\temp4\AudioTest.exe" "%BIN_DIR%\" >nul 2>&1
rmdir /s /q "%BIN_DIR%\temp4" >nul 2>&1

echo.
echo All builds completed successfully.
echo Executables are in: %BIN_DIR%
echo.
echo To run all tests, execute: run.bat
exit /b 0

:fail
echo Build failed.
exit /b 1
