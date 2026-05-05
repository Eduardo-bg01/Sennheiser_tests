@echo off
setlocal

if not exist serial.txt (
    echo TEMP-SERIAL-123>serial.txt
)

set SKIP_SERIAL_PROMPT=1
call run.bat