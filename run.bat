echo "Borrando archivos para empezar las pruebas"
del Prueba_*
del results.json
del MicroTest_*
del test_results*
del hearingPass*
del recorded*
del final_results*
del tiempo*
del diferen*

if not defined SKIP_SERIAL_PROMPT (
    del serial*
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

    if exist serial.txt (
        :TEST_AUDIO
            start /wait "" "bin\AudioTest.exe"
            timeout /t 2 /nobreak
            if not exist hearingPass*.txt (
                echo [AUDIO TEST FAILED] Reintentando...
                goto TEST_AUDIO
            )

        :TEST_CONTROLS
            start /wait "" "bin\BluetoothHeadphoneTest.exe"
            timeout /t 2 /nobreak
            if not exist Prueba_*.txt (
                echo [CONTROLS TEST FAILED] Reintentando...
                goto TEST_CONTROLS
            )

        :TEST_MICROPHONE
            start /wait "" "bin\MicroTestCloud.exe"
            timeout /t 2 /nobreak
            if not exist MicroTest_*.txt (
                echo [MICROPHONE TEST FAILED] Reintentando...
                goto TEST_MICROPHONE
            )

        :TEST_LEVELS
            start /wait "" "bin\LevelTest.exe"
            timeout /t 2 /nobreak
            if not exist results.json (
                echo [LEVELS TEST FAILED] Reintentando...
                goto TEST_LEVELS
            )

        python getFinalResults.py
    )

for /f %%i in ('powershell -command "[int64](Get-Date).ToUniversalTime().Subtract([datetime]\"1970-01-01\").TotalMilliseconds"') do set timestamp=%%i

echo %timestamp% > tiempo2.txt

for /f "delims=" %%i in ('powershell -command "$t1 = Get-Content tiempo1.txt; $t2 = Get-Content tiempo2.txt; [math]::Round(($t2 - $t1)/60000,2)"') do set diff_min=%%i

echo %diff_min% > diferencia_minutos.txt
echo Tiempo total (min): %diff_min%
