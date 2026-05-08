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

echo.
echo Building BluetoothHeadphoneTest...
%DOTNET% publish -c Release %PUBLISH_FLAGS% "%ROOT%apps\FunctionalButtonTest\BluetoothHeadphoneTest.csproj" -o "%BIN_DIR%\temp1"
if errorlevel 1 goto :fail
call :copy_publish_output "%BIN_DIR%\temp1"
if errorlevel 1 goto :fail

echo Building MicroTestCloud...
%DOTNET% publish -c Release %PUBLISH_FLAGS% "%ROOT%apps\MicroTestCloud\MicroTestCloud\MicroTestCloud.csproj" -o "%BIN_DIR%\temp2"
if errorlevel 1 goto :fail
call :copy_publish_output "%BIN_DIR%\temp2"
if errorlevel 1 goto :fail
call :copy_asset_folder "%ROOT%apps\MicroTestCloud\MicroTestCloud\PistaAudio" "%BIN_DIR%\PistaAudio"
if errorlevel 1 goto :fail

echo Building AskForSerial2...
%DOTNET% publish -c Release %PUBLISH_FLAGS% "%ROOT%apps\pruebasAudifonos\AskForSerial2\AskForSerial2\AskForSerial2.csproj" -o "%BIN_DIR%\temp3"
if errorlevel 1 goto :fail
call :copy_publish_output "%BIN_DIR%\temp3"
if errorlevel 1 goto :fail

echo Building AudioTest...
%DOTNET% publish -c Release %PUBLISH_FLAGS% "%ROOT%apps\pruebasAudifonos\AudioTest\AudioTest\AudioTest.csproj" -o "%BIN_DIR%\temp4"
if errorlevel 1 goto :fail
call :copy_publish_output "%BIN_DIR%\temp4"
if errorlevel 1 goto :fail
if exist "%ROOT%apps\pruebasAudifonos\AudioTest\AudioTest\karmaPolice.wav" (
	copy /Y "%ROOT%apps\pruebasAudifonos\AudioTest\AudioTest\karmaPolice.wav" "%BIN_DIR%\" >nul
) else (
	echo Warning: missing AudioTest asset karmaPolice.wav in source tree.
)

echo Building LevelTest...
%DOTNET% publish -c Release %PUBLISH_FLAGS% "%ROOT%apps\pruebasAudifonos\LevelTest\HeadPhoneTest2\HeadPhoneTest2.csproj" -o "%BIN_DIR%\temp5"
if errorlevel 1 goto :fail
call :copy_publish_output "%BIN_DIR%\temp5"
if errorlevel 1 goto :fail
call :copy_asset_folder "%ROOT%apps\pruebasAudifonos\LevelTest\HeadPhoneTest2\audio" "%BIN_DIR%\audio"
if errorlevel 1 goto :fail

echo.
echo All builds completed successfully.
echo Copying runner scripts...
copy /Y "%ROOT%run.bat" "%BIN_DIR%\run.bat" >nul
if exist "%ROOT%getFinalResults.py" copy /Y "%ROOT%getFinalResults.py" "%BIN_DIR%\getFinalResults.py" >nul
if exist "%ROOT%converter.py" copy /Y "%ROOT%converter.py" "%BIN_DIR%\converter.py" >nul
if exist "%ROOT%serial.txt" copy /Y "%ROOT%serial.txt" "%BIN_DIR%\serial.txt" >nul

echo.
echo Packaging complete.
echo Executables are in: %BIN_DIR%
echo.
echo To run all tests, execute: %BIN_DIR%\run.bat
exit /b 0

:copy_publish_output
set "SOURCE_DIR=%~1"
robocopy "%SOURCE_DIR%" "%BIN_DIR%" /E /NFL /NDL /NJH /NJS /NP >nul
if errorlevel 8 exit /b 1
rmdir /s /q "%SOURCE_DIR%" >nul 2>&1
exit /b 0

:copy_asset_folder
set "SOURCE_DIR=%~1"
set "DEST_DIR=%~2"
if not exist "%SOURCE_DIR%" exit /b 0
robocopy "%SOURCE_DIR%" "%DEST_DIR%" /E /NFL /NDL /NJH /NJS /NP >nul
if errorlevel 8 exit /b 1
exit /b 0

:fail
echo Build failed.
exit /b 1
