@echo off
setlocal EnableDelayedExpansion

if not defined MAX_RETRIES set MAX_RETRIES=5
if not defined RETRY_DELAY set RETRY_DELAY=2

echo "Borrando archivos para empezar las pruebas"
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

if not exist bin\AskForSerial2.exe (
    echo Falta bin\AskForSerial2.exe. Ejecuta build-all.bat primero.
    exit /b 1
)

if not exist bin\AudioTest.exe (
    echo Falta bin\AudioTest.exe. Ejecuta build-all.bat primero.
    exit /b 1
)

if not exist bin\BluetoothHeadphoneTest.exe (
    echo Falta bin\BluetoothHeadphoneTest.exe. Ejecuta build-all.bat primero.
    exit /b 1
)

if not exist bin\MicroTestCloud.exe (
    echo Falta bin\MicroTestCloud.exe. Ejecuta build-all.bat primero.
    exit /b 1
)

if not exist bin\LevelTest.exe (
    echo Falta bin\LevelTest.exe. Ejecuta build-all.bat primero.
    exit /b 1
)

for /f %%i in ('powershell -command "[int64](Get-Date).ToUniversalTime().Subtract([datetime]\"1970-01-01\").TotalMilliseconds"') do set timestamp=%%i

echo %timestamp% > tiempo1.txt

:GET_SERIAL
    if defined SKIP_SERIAL_PROMPT (
        if not exist serial.txt (
            echo Falta serial.txt y SKIP_SERIAL_PROMPT esta activo.
            exit /b 1
        )
    ) else (
        start /wait "" "bin\AskForSerial2.exe"
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
    start /wait "" "bin\AudioTest.exe"
    timeout /t %RETRY_DELAY% /nobreak >nul
    if not exist hearingPass*.txt (
        if !AUDIO_ATTEMPTS! GEQ %MAX_RETRIES% (
            echo [AUDIO TEST FAILED] Alcanzado maximo de intentos ^(!AUDIO_ATTEMPTS!/%MAX_RETRIES%^).
            exit /b 2
        )
        echo [AUDIO TEST FAILED] Reintentando ^(!AUDIO_ATTEMPTS!/%MAX_RETRIES%^)...
        goto TEST_AUDIO
    )

set /a CONTROLS_ATTEMPTS=0
:TEST_CONTROLS
    set /a CONTROLS_ATTEMPTS+=1
    start /wait "" "bin\BluetoothHeadphoneTest.exe"
    timeout /t %RETRY_DELAY% /nobreak >nul
    if not exist Prueba_*.txt (
        if !CONTROLS_ATTEMPTS! GEQ %MAX_RETRIES% (
            echo [CONTROLS TEST FAILED] Alcanzado maximo de intentos ^(!CONTROLS_ATTEMPTS!/%MAX_RETRIES%^).
            exit /b 3
        )
        echo [CONTROLS TEST FAILED] Reintentando ^(!CONTROLS_ATTEMPTS!/%MAX_RETRIES%^)...
        goto TEST_CONTROLS
    )

set /a MIC_ATTEMPTS=0
:TEST_MICROPHONE
    set /a MIC_ATTEMPTS+=1
    start /wait "" "bin\MicroTestCloud.exe"
    timeout /t %RETRY_DELAY% /nobreak >nul
    if not exist MicroTest_*.txt (
        if !MIC_ATTEMPTS! GEQ %MAX_RETRIES% (
            echo [MICROPHONE TEST FAILED] Alcanzado maximo de intentos ^(!MIC_ATTEMPTS!/%MAX_RETRIES%^).
            exit /b 4
        )
        echo [MICROPHONE TEST FAILED] Reintentando ^(!MIC_ATTEMPTS!/%MAX_RETRIES%^)...
        goto TEST_MICROPHONE
    )

set /a LEVELS_ATTEMPTS=0
:TEST_LEVELS
    set /a LEVELS_ATTEMPTS+=1
    start /wait "" "bin\LevelTest.exe"
    timeout /t %RETRY_DELAY% /nobreak >nul
    if not exist results.json (
        if !LEVELS_ATTEMPTS! GEQ %MAX_RETRIES% (
            echo [LEVELS TEST FAILED] Alcanzado maximo de intentos ^(!LEVELS_ATTEMPTS!/%MAX_RETRIES%^).
            exit /b 5
        )
        echo [LEVELS TEST FAILED] Reintentando ^(!LEVELS_ATTEMPTS!/%MAX_RETRIES%^)...
        goto TEST_LEVELS
    )

python getFinalResults.py

for /f %%i in ('powershell -command "[int64](Get-Date).ToUniversalTime().Subtract([datetime]\"1970-01-01\").TotalMilliseconds"') do set timestamp=%%i

echo %timestamp% > tiempo2.txt

for /f "delims=" %%i in ('powershell -command "$t1 = Get-Content tiempo1.txt; $t2 = Get-Content tiempo2.txt; [math]::Round(($t2 - $t1)/60000,2)"') do set diff_min=%%i

echo %diff_min% > diferencia_minutos.txt
echo Tiempo total (min): %diff_min%
