@echo off
setlocal EnableDelayedExpansion
set "ROOT=%~dp0"
pushd "%ROOT%"

set "APP_DIR=%ROOT%"
set "REFURBISH_TOOL=%ROOT%RefurbishToolArvato\RefurbishTool.exe"
if not exist "%REFURBISH_TOOL%" set "REFURBISH_TOOL=%ROOT%bin\RefurbishToolArvato\RefurbishTool.exe"

if not defined MAX_RETRIES set MAX_RETRIES=5
if not defined RETRY_DELAY set RETRY_DELAY=2

:: Reglas de ejecucion por modelo (automatico, ya no requiere VARIANT):
::   - AudioTest      -> siempre se ejecuta.
::   - MicroTestCloud -> desactivado para todos los modelos.
::   - LevelTest      -> solo modelos de familia HD o IE (ver :detect_level_test).
:: Para forzar la prueba de microfono en algun caso especial:
::   set RUN_MICROPHONE=1 && bin\run.bat
if not defined RUN_MICROPHONE set RUN_MICROPHONE=0
if /i "%RUN_MICROPHONE%"=="1" (set "QUICK_AUDIO=0") else (if not defined QUICK_AUDIO set QUICK_AUDIO=1)

:: Clean up previous test files and processes
echo Limpiando archivos y procesos previos...
for %%p in (AskForSerial2.exe AudioTest.exe BluetoothHeadphoneTest.exe MicroTestCloud.exe LevelTest.exe RefurbishTool.exe) do (
    taskkill /f /im %%p >nul 2>&1
)
del /q Prueba_* results.json MicroTest_* test_results* hearingPass* recorded* final_results* tiempo* diferen* resultado.png >nul 2>&1
if not defined SKIP_SERIAL_PROMPT del /q serial* >nul 2>&1

:: Verify all required executables exist
set "NEEDED=AskForSerial2.exe BluetoothHeadphoneTest.exe AudioTest.exe VolumeHelper.exe LevelTest.exe"
if /i "%RUN_MICROPHONE%"=="1" set "NEEDED=!NEEDED! MicroTestCloud.exe"
for %%f in (!NEEDED!) do (
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

:: Calibracion diaria del ruido ambiente (una vez por dia, por maquina).
:: LevelTest con CALIBRATION=1 graba 30s sin reproducir audio y guarda calibracion.txt,
:: que db_chart.py usa como linea base para detectar "solo se escucha ruido ambiente".
set "CALIB_NEEDED=1"
set "CALDATE="
set "TODAY="
if exist calibracion.txt (
    for /f %%d in ('powershell -NoProfile -Command "try { (Get-Content calibracion.txt -Raw | ConvertFrom-Json).date } catch { '' }"') do set "CALDATE=%%d"
    for /f %%t in ('powershell -NoProfile -Command "Get-Date -Format yyyy-MM-dd"') do set "TODAY=%%t"
    if "!CALDATE!"=="!TODAY!" set "CALIB_NEEDED=0"
)
if "%CALIB_NEEDED%"=="1" (
    echo Calibracion diaria requerida - midiendo ruido ambiente...
    set "CALIBRATION=1"
    start /wait "" "%APP_DIR%LevelTest.exe"
    set "CALIBRATION="
) else (
    echo Calibracion del dia vigente ^(calibracion.txt: !CALDATE!^).
)
echo.

:: Show Bluetooth connection instructions
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%show_bluetooth.ps1" -Mode connect
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

:: La seleccion de dispositivo (BT o jack + modelo) la hace el operador
:: dentro de BluetoothHeadphoneTest.exe. El reporte Prueba_*.txt que
:: genera la app es el que usan los scripts aguas abajo.
:SET_VOLUME_50
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
    call :detect_level_test
    echo Configurando volumen a 100%% antes de la prueba de audio...
    "%APP_DIR%VolumeHelper.exe" 100 >nul 2>&1 || echo No se pudo configurar volumen.
    goto TEST_AUDIO
)
if exist "%APP_DIR%Prueba_*.txt" (
    copy "%APP_DIR%Prueba_*.txt" . >nul 2>&1
    echo [CONTROLS] PASSED
    for /f "usebackq delims=" %%a in (`powershell -NoProfile -Command "$line = (Select-String -Path Prueba_*.txt -Pattern 'Dispositivo').Line; $name = $line.Substring($line.IndexOf(':') + 1).Trim(); Write-Output $name"`) do set "DEVICE_NAME=%%a"
    call :detect_level_test
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
if exist hearingPassResults.txt (
    findstr /x /c:"True" hearingPassResults.txt >nul 2>&1
    if !errorlevel! EQU 0 (
        echo [AUDIO] PASSED
        goto POST_AUDIO
    )
    rem El operador rechazo la unidad: se registra el fallo y se continua,
    rem el veredicto sobrevive en hearingPassResults.txt para getFinalResults.py.
    echo [AUDIO] FAILED por operador - se registra el fallo y se continua.
    goto POST_AUDIO
)
if !AUDIO_ATTEMPTS! LSS %MAX_RETRIES% (
    echo [AUDIO] FAILED - Intento !AUDIO_ATTEMPTS!/%MAX_RETRIES%
    goto RETRY_AUDIO
)
echo [AUDIO] FAILED - Max retries exceeded
exit /b 2

:POST_AUDIO
if /i "%RUN_MICROPHONE%"=="1" (
    echo Configurando volumen a 100%% antes de la prueba de microfono...
    "%APP_DIR%VolumeHelper.exe" 100 >nul 2>&1 || echo No se pudo configurar volumen.
    goto TEST_MICROPHONE
)
goto SETUP_VOLUME

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
if /i "%RUN_LEVEL%"=="0" (
    echo [LEVELS] Omitido - el modelo "!DEVICE_NAME!" no es familia HD/IE.
    goto GENERATE_RESULTS
)
set "LEVEL_VOLUME=85"
if /i "!DEVICE_NAME!"=="MOMENTUM TW 4" set "LEVEL_VOLUME=80"
echo Configurando volumen a !LEVEL_VOLUME!%% antes de la prueba de nivel...
"%APP_DIR%VolumeHelper.exe" !LEVEL_VOLUME! >nul 2>&1 || echo No se pudo configurar volumen.

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

python "%ROOT%scripts\getFinalResults.py" --some

for /f "delims=" %%i in ('powershell -command "$t1 = Get-Content tiempo1.txt; $t2 = Get-Content tiempo2.txt; [math]::Round(($t2 - $t1)/60000,2)"') do set diff_min=%%i
echo %diff_min% > diferencia_minutos.txt

python "%ROOT%scripts\converter.py"

echo Limpiando dispositivos Bluetooth...
powershell -NoProfile -Command "$ErrorActionPreference = 'SilentlyContinue'; Get-PnpDevice -Class Bluetooth | Where-Object { $_.FriendlyName -and $_.FriendlyName -notmatch 'Radio|Adapter|Enumerator|LE Enumerator|Microsoft|Intel|Qualcomm|Broadcom' } | Remove-PnpDevice -Confirm:$false -Force" >nul 2>&1

powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%show_bluetooth.ps1" -Mode disconnect

echo.
echo Pruebas completadas. Tiempo total: %diff_min% min
echo.
popd
exit /b 0

:detect_level_test
:: LevelTest solo aplica a modelos de familia HD o IE (ej. "HD 660S2", "IE 200").
:: Se compara el nombre normalizado (mayusculas, sin espacios/simbolos) buscando
:: "HD" o "IE" seguido de un digito, para NO incluir por error otras familias
:: que tambien empiezan con "HD" pero son distintas, como "HDR 175" o "HDB 630".
:: Para agregar/quitar familias, ajusta el patron regex de abajo.
set "RUN_LEVEL=0"
set "LEVEL_MATCH=NO"
for /f %%r in ('powershell -NoProfile -Command "if ((($env:DEVICE_NAME) -replace '[^A-Za-z0-9]','').ToUpper() -match '(HD|IE)[0-9]') { 'YES' } else { 'NO' }"') do set "LEVEL_MATCH=%%r"
if /i "!LEVEL_MATCH!"=="YES" set "RUN_LEVEL=1"
echo Modelo detectado: !DEVICE_NAME!  ^(LevelTest: !RUN_LEVEL!, MicroTest: %RUN_MICROPHONE%^)
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
