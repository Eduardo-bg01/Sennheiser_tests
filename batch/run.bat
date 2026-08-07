@echo off
setlocal EnableDelayedExpansion
set "ROOT=%~dp0..\"
pushd "%ROOT%"

set "APP_DIR=%ROOT%bin\"
if exist "%ROOT%AskForSerial2.exe" set "APP_DIR=%ROOT%\"
set "REFURBISH_TOOL=%ROOT%RefurbishToolArvato\RefurbishTool.exe"
if not exist "%REFURBISH_TOOL%" set "REFURBISH_TOOL=%ROOT%bin\RefurbishToolArvato\RefurbishTool.exe"

if not defined MAX_RETRIES set MAX_RETRIES=5
if not defined RETRY_DELAY set RETRY_DELAY=2

:: Clean up previous test files and processes
echo Limpiando archivos y procesos previos...
for %%p in (AskForSerial2.exe AudioTest.exe BluetoothHeadphoneTest.exe MicroTestCloud.exe LevelTest.exe RefurbishTool.exe) do (
    taskkill /f /im %%p >nul 2>&1
)
del /q Prueba_* results.json MicroTest_* test_results* hearingPass* recorded* final_results* tiempo* diferen* >nul 2>&1
if not defined SKIP_SERIAL_PROMPT del /q serial* >nul 2>&1

:: Verify all required executables exist
for %%f in (AskForSerial2.exe BluetoothHeadphoneTest.exe AudioTest.exe MicroTestCloud.exe LevelTest.exe VolumeHelper.exe) do (
    if not exist "%APP_DIR%%%f" (
        echo Error: %%f no encontrado. Ejecuta build-all.bat primero.
        exit /b 1
    )
)

if not exist "%REFURBISH_TOOL%" (
    echo Aviso: RefurbishTool.exe no encontrado, se omite.
) else (
    echo.
    echo Abriendo RefurbishTool...
    start /wait "" "%REFURBISH_TOOL%"
    echo.
)

:: Show Bluetooth connection instructions
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%show_bluetooth_connect.ps1"
echo.

:: Record start time
for /f %%i in ('powershell -command "[int64](Get-Date).ToUniversalTime().Subtract([datetime]\"1970-01-01\").TotalMilliseconds"') do set timestamp=%%i
echo %timestamp% > tiempo1.txt


:GET_SERIAL
if defined SKIP_SERIAL_PROMPT (
    if not exist serial.txt (
        echo Error: serial.txt requerido con SKIP_SERIAL_PROMPT activo.
        exit /b 1
    )
) else (
    start /wait "" "%APP_DIR%AskForSerial2.exe"
    if not exist serial.txt (
        echo Error: AskForSerial2 no genero serial.txt.
        exit /b 1
    )
)

:SET_VOLUME_100_BEFORE_CONTROLS
echo Configurando volumen a 50%% antes de la prueba de controles...
"%APP_DIR%VolumeHelper.exe" 50 >nul 2>&1 || echo No se pudo configurar volumen.

:TEST_CONTROLS
set /a CONTROLS_ATTEMPTS=0
:RETRY_CONTROLS
set /a CONTROLS_ATTEMPTS+=1
start /wait "" "%APP_DIR%BluetoothHeadphoneTest.exe"
call :wait_for_result_file "Prueba_*.txt" "%APP_DIR%Prueba_*.txt" 5
if exist Prueba_*.txt (
    echo [CONTROLS] PASSED
    for /f "usebackq delims=" %%a in (`powershell -NoProfile -Command "$line = (Select-String -Path Prueba_*.txt -Pattern 'Dispositivo').Line; $name = $line.Substring($line.IndexOf(':') + 1).Trim(); Write-Output $name"`) do set "DEVICE_NAME=%%a"
    echo Configurando volumen a 100%% antes de la prueba de audio...
    "%APP_DIR%VolumeHelper.exe" 100 >nul 2>&1 || echo No se pudo configurar volumen.
    goto TEST_AUDIO
)
if exist "%APP_DIR%Prueba_*.txt" (
    copy "%APP_DIR%Prueba_*.txt" . >nul 2>&1
    echo [CONTROLS] PASSED
    for /f "usebackq delims=" %%a in (`powershell -NoProfile -Command "$line = (Select-String -Path Prueba_*.txt -Pattern 'Dispositivo').Line; $name = $line.Substring($line.IndexOf(':') + 1).Trim(); Write-Output $name"`) do set "DEVICE_NAME=%%a"
    echo Configurando volumen a 100%% antes de la prueba de audio...
    "%APP_DIR%VolumeHelper.exe" 100 >nul 2>&1 || echo No se pudo configurar volumen.
    goto TEST_AUDIO
)
if !CONTROLS_ATTEMPTS! LSS %MAX_RETRIES% (
    echo [CONTROLS] FAILED - Intento !CONTROLS_ATTEMPTS!/%MAX_RETRIES%
    goto RETRY_CONTROLS
)
echo [CONTROLS] FAILED - Max retries exceeded
exit /b 3

:TEST_AUDIO
set /a AUDIO_ATTEMPTS=0
:RETRY_AUDIO
set /a AUDIO_ATTEMPTS+=1
start /wait "" "%APP_DIR%AudioTest.exe"
timeout /t %RETRY_DELAY% /nobreak >nul
if exist hearingPass*.txt (
    echo [AUDIO] PASSED
    echo Configurando volumen a 100%% antes de la prueba de microfono...
    "%APP_DIR%VolumeHelper.exe" 100 >nul 2>&1 || echo No se pudo configurar volumen.
    goto TEST_MICROPHONE
)
if !AUDIO_ATTEMPTS! LSS %MAX_RETRIES% (
    echo [AUDIO] FAILED - Intento !AUDIO_ATTEMPTS!/%MAX_RETRIES%
    goto RETRY_AUDIO
)
echo [AUDIO] FAILED - Max retries exceeded
exit /b 2

:TEST_MICROPHONE
set /a MIC_ATTEMPTS=0
:RETRY_MICROPHONE
set /a MIC_ATTEMPTS+=1
start /wait "" "%APP_DIR%MicroTestCloud.exe"
call :wait_for_result_file "MicroTest_*.txt" "%APP_DIR%MicroTest_*.txt" 5
if exist MicroTest_*.txt (
    echo [MICROPHONE] PASSED
    goto SETUP_VOLUME
)
if exist "%APP_DIR%MicroTest_*.txt" (
    copy "%APP_DIR%MicroTest_*.txt" . >nul 2>&1
    echo [MICROPHONE] PASSED
    goto SETUP_VOLUME
)
if !MIC_ATTEMPTS! LSS %MAX_RETRIES% (
    echo [MICROPHONE] FAILED - Intento !MIC_ATTEMPTS!/%MAX_RETRIES%
    goto RETRY_MICROPHONE
)
echo [MICROPHONE] FAILED - Max retries exceeded
exit /b 4

:SETUP_VOLUME
set "LEVEL_VOLUME=100"
if /i "!DEVICE_NAME!"=="MOMENTUM TW 4" set "LEVEL_VOLUME=80"
echo Configurando volumen a !LEVEL_VOLUME!%% antes de la prueba de nivel...
"%APP_DIR%VolumeHelper.exe" !LEVEL_VOLUME! >nul 2>&1 || echo No se pudo configurar volumen.

:ENSURE_REQUESTS
python -c "import requests" >nul 2>&1 || (
    echo Instalando dependencia requests...
    python -m pip install --user requests >nul 2>&1 || echo No se pudo instalar requests.
)

:TEST_LEVELS
set /a LEVELS_ATTEMPTS=0
:RETRY_LEVELS
set /a LEVELS_ATTEMPTS+=1
start /wait "" "%APP_DIR%LevelTest.exe"
timeout /t %RETRY_DELAY% /nobreak >nul
if exist results.json (
    echo [LEVELS] PASSED
    goto GENERATE_RESULTS
)
if !LEVELS_ATTEMPTS! LSS %MAX_RETRIES% (
    echo [LEVELS] FAILED - Intento !LEVELS_ATTEMPTS!/%MAX_RETRIES%
    goto RETRY_LEVELS
)
echo [LEVELS] FAILED - Max retries exceeded
exit /b 5

:GENERATE_RESULTS
for /f %%i in ('powershell -command "[int64](Get-Date).ToUniversalTime().Subtract([datetime]\"1970-01-01\").TotalMilliseconds"') do set timestamp=%%i
echo %timestamp% > tiempo2.txt

python "%ROOT%scripts\getFinalResults.py"

for /f "delims=" %%i in ('powershell -command "$t1 = Get-Content tiempo1.txt; $t2 = Get-Content tiempo2.txt; [math]::Round(($t2 - $t1)/60000,2)"') do set diff_min=%%i
echo %diff_min% > diferencia_minutos.txt

python "%ROOT%scripts\converter.py"

echo Limpiando dispositivos Bluetooth...
powershell -NoProfile -Command "$ErrorActionPreference = 'SilentlyContinue'; Get-PnpDevice -Class Bluetooth | Where-Object { $_.FriendlyName -and $_.FriendlyName -notmatch 'Radio|Adapter|Enumerator|LE Enumerator|Microsoft|Intel|Qualcomm|Broadcom' } | Remove-PnpDevice -Confirm:$false -Force" >nul 2>&1

powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%show_bluetooth_disconnect.ps1"

echo.
echo Pruebas completadas. Tiempo total: %diff_min% min
echo.
popd
exit /b 0

:wait_for_result_file
set "LOCAL_PATTERN=%~1"
set "APP_PATTERN=%~2"
set /a MAX_WAIT=%~3
set /a ELAPSED=0

:wait_for_result_file_loop
if exist "%LOCAL_PATTERN%" exit /b 0
if exist "%APP_PATTERN%" exit /b 0
if !ELAPSED! GEQ !MAX_WAIT! exit /b 1
timeout /t 1 /nobreak >nul
set /a ELAPSED+=1
goto wait_for_result_file_loop
