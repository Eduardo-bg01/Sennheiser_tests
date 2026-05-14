@echo off
setlocal EnableDelayedExpansion
set "ROOT=%~dp0"
pushd "%ROOT%"

set "APP_DIR=%ROOT%bin\"
if exist "%ROOT%AskForSerial2.exe" (
    set "APP_DIR=%ROOT%"
)

if not defined MAX_RETRIES set MAX_RETRIES=5
if not defined RETRY_DELAY set RETRY_DELAY=2

echo Borrando archivos para empezar las pruebas...
taskkill /f /im AskForSerial2.exe >nul 2>&1
taskkill /f /im AudioTest.exe >nul 2>&1
taskkill /f /im BluetoothHeadphoneTest.exe >nul 2>&1
taskkill /f /im MicroTestCloud.exe >nul 2>&1
taskkill /f /im LevelTest.exe >nul 2>&1
del /q Prueba_* >nul 2>&1
del /q results.json >nul 2>&1
del /q MicroTest_* >nul 2>&1
del /q test_results* >nul 2>&1
del /q hearingPass* >nul 2>&1
del /q recorded* >nul 2>&1
del /q final_results* >nul 2>&1
del /q tiempo* >nul 2>&1
del /q diferen* >nul 2>&1

if not defined SKIP_SERIAL_PROMPT (
    del /q serial* >nul 2>&1
)


:: ============================================================
:: VENTANA: Instrucciones para conectar Bluetooth
:: ============================================================
echo Mostrando instrucciones de conexion Bluetooth...
powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%show_bluetooth_connect.ps1"


if not exist "%APP_DIR%AskForSerial2.exe" (
    echo Falta AskForSerial2.exe. Ejecuta build-all.bat primero.
    exit /b 1
)

if not exist "%APP_DIR%BluetoothHeadphoneTest.exe" (
    echo Falta BluetoothHeadphoneTest.exe. Ejecuta build-all.bat primero.
    exit /b 1
)

if not exist "%APP_DIR%AudioTest.exe" (
    echo Falta AudioTest.exe. Ejecuta build-all.bat primero.
    exit /b 1
)

if not exist "%APP_DIR%MicroTestCloud.exe" (
    echo Falta MicroTestCloud.exe. Ejecuta build-all.bat primero.
    exit /b 1
)

if not exist "%APP_DIR%LevelTest.exe" (
    echo Falta LevelTest.exe. Ejecuta build-all.bat primero.
    exit /b 1
)

for /f %%i in ('powershell -command "[int64](Get-Date).ToUniversalTime().Subtract([datetime]\"1970-01-01\").TotalMilliseconds"') do set timestamp=%%i

echo %timestamp% > tiempo1.txt

:: Set system master volume to 80% (tries Python script, prints helpful message on failure)
echo Configurando volumen del sistema a 80%%...
set "VOL_PERCENT=80"
if exist "%~dp0\.venv\Scripts\python.exe" (
    "%~dp0\.venv\Scripts\python.exe" "%~dp0\scripts\set_volume.py" %VOL_PERCENT% >nul 2>&1 || echo No se pudo configurar el volumen con el entorno virtual Python.
) else (
    python "%~dp0\scripts\set_volume.py" %VOL_PERCENT% >nul 2>&1 || echo No se pudo configurar el volumen. Instala Python y ejecuta `pip install pycaw comtypes` para soporte.
)


:GET_SERIAL
    if defined SKIP_SERIAL_PROMPT (
        if not exist serial.txt (
            echo Falta serial.txt y SKIP_SERIAL_PROMPT esta activo.
            exit /b 1
        )
    ) else (
        start /wait "" "%APP_DIR%AskForSerial2.exe"
        if not exist serial.txt (
            echo AskForSerial2 no genero serial.txt. Debes ingresar un serial valido para continuar.
            exit /b 1
        )
    )

if not exist serial.txt (
    echo No se encontro serial.txt despues de AskForSerial2.
    exit /b 1
)

set /a AUDIO_ATTEMPTS=0
:TEST_AUDIO
    set /a AUDIO_ATTEMPTS+=1
    start /wait "" "%APP_DIR%AudioTest.exe"
    timeout /t %RETRY_DELAY% /nobreak >nul
    dir hearingPass*.txt >nul 2>&1
    if !ERRORLEVEL! NEQ 0 (
        if !AUDIO_ATTEMPTS! GEQ %MAX_RETRIES% (
            echo [AUDIO TEST FAILED] Alcanzado maximo de intentos ^(!AUDIO_ATTEMPTS!/%MAX_RETRIES%^).
            dir hearingPass* 2>nul
            exit /b 2
        )
        echo [AUDIO TEST FAILED] Reintentando ^(!AUDIO_ATTEMPTS!/%MAX_RETRIES%^)...
        goto TEST_AUDIO
    )
    echo [AUDIO TEST PASSED]

set /a CONTROLS_ATTEMPTS=0
:TEST_CONTROLS
    set /a CONTROLS_ATTEMPTS+=1
    start /wait "" "%APP_DIR%BluetoothHeadphoneTest.exe"
    timeout /t 5 /nobreak >nul
    set CONTROLS_FILE_FOUND=0
    dir Prueba_*.txt >nul 2>&1
    if !ERRORLEVEL! EQU 0 set CONTROLS_FILE_FOUND=1
    if !CONTROLS_FILE_FOUND! EQU 0 (
        dir "%APP_DIR%Prueba_*.txt" >nul 2>&1
        if !ERRORLEVEL! EQU 0 set CONTROLS_FILE_FOUND=1
    )
    if !CONTROLS_FILE_FOUND! EQU 0 (
        if !CONTROLS_ATTEMPTS! GEQ %MAX_RETRIES% (
            echo [CONTROLS TEST FAILED] Alcanzado maximo de intentos ^(!CONTROLS_ATTEMPTS!/%MAX_RETRIES%^).
            dir Prueba_* 2>nul
            dir "%APP_DIR%Prueba_*" 2>nul
            exit /b 3
        )
        echo [CONTROLS TEST FAILED] Reintentando ^(!CONTROLS_ATTEMPTS!/%MAX_RETRIES%^)...
        goto TEST_CONTROLS
    )
    copy "%APP_DIR%Prueba_*.txt" . >nul 2>&1
    echo [CONTROLS TEST PASSED]

set /a MIC_ATTEMPTS=0
:TEST_MICROPHONE
    set /a MIC_ATTEMPTS+=1
    start /wait "" "%APP_DIR%MicroTestCloud.exe"
    timeout /t 5 /nobreak >nul
    set MIC_FILE_FOUND=0
    dir MicroTest_*.txt >nul 2>&1
    if !ERRORLEVEL! EQU 0 set MIC_FILE_FOUND=1
    if !MIC_FILE_FOUND! EQU 0 (
        dir "%APP_DIR%MicroTest_*.txt" >nul 2>&1
        if !ERRORLEVEL! EQU 0 set MIC_FILE_FOUND=1
    )
    if !MIC_FILE_FOUND! EQU 0 (
        if !MIC_ATTEMPTS! GEQ %MAX_RETRIES% (
            echo [MICROPHONE TEST FAILED] Alcanzado maximo de intentos ^(!MIC_ATTEMPTS!/%MAX_RETRIES%^).
            dir MicroTest_* 2>nul
            dir "%APP_DIR%MicroTest_*" 2>nul
            exit /b 4
        )
        echo [MICROPHONE TEST FAILED] Reintentando ^(!MIC_ATTEMPTS!/%MAX_RETRIES%^)...
        goto TEST_MICROPHONE
    )
    copy "%APP_DIR%MicroTest_*.txt" . >nul 2>&1
    echo [MICROPHONE TEST PASSED]

set /a LEVELS_ATTEMPTS=0
:TEST_LEVELS
    set /a LEVELS_ATTEMPTS+=1
    start /wait "" "%APP_DIR%LevelTest.exe"
    timeout /t %RETRY_DELAY% /nobreak >nul
    if not exist results.json (
        if !LEVELS_ATTEMPTS! GEQ %MAX_RETRIES% (
            echo [LEVELS TEST FAILED] Alcanzado maximo de intentos ^(!LEVELS_ATTEMPTS!/%MAX_RETRIES%^).
            dir results.* 2>nul
            exit /b 5
        )
        echo [LEVELS TEST FAILED] Reintentando ^(!LEVELS_ATTEMPTS!/%MAX_RETRIES%^)...
        goto TEST_LEVELS
    )
    echo [LEVELS TEST PASSED]


for /f %%i in ('powershell -command "[int64](Get-Date).ToUniversalTime().Subtract([datetime]\"1970-01-01\").TotalMilliseconds"') do set timestamp=%%i

echo %timestamp% > tiempo2.txt

if exist "%ROOT%getFinalResults.exe" (
    "%ROOT%getFinalResults.exe"
) else (
    python "%ROOT%scripts\getFinalResults.py"
)

for /f "delims=" %%i in ('powershell -command "$t1 = Get-Content tiempo1.txt; $t2 = Get-Content tiempo2.txt; [math]::Round(($t2 - $t1)/60000,2)"') do set diff_min=%%i

echo %diff_min% > diferencia_minutos.txt
echo Tiempo total (min): %diff_min%

:: Convert to XML and optionally upload using converter.py
if exist "%ROOT%converter.exe" (
    "%ROOT%converter.exe"
) else if exist "%ROOT%scripts\converter.py" (
    python "%ROOT%scripts\converter.py"
) else (
    echo converter.py not found, skipping conversion
)

echo Limpiando dispositivos Bluetooth emparejados...
powershell -NoProfile -Command "$ErrorActionPreference = 'SilentlyContinue'; $devices = Get-PnpDevice -Class Bluetooth | Where-Object { $_.FriendlyName -and $_.FriendlyName -notmatch 'Radio|Adapter|Enumerator|LE Enumerator|Microsoft|Intel|Qualcomm|Broadcom' }; foreach ($device in $devices) { $device | Remove-PnpDevice -Confirm:$false -Force }" >nul 2>&1


:: ============================================================
:: VENTANA: Instrucciones para desconectar Bluetooth
:: ============================================================
echo Mostrando instrucciones de desconexion Bluetooth...
powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%show_bluetooth_disconnect.ps1"



echo Pruebas completadas. Resultados guardados.
echo Dispositivos Bluetooth desconectados y limpiados.
popd
