@echo off
setlocal

if not exist serial.txt (
    echo TEMP-SERIAL-123>serial.txt
)

set SKIP_SERIAL_PROMPT=1
if not defined MAX_RETRIES set MAX_RETRIES=3
if not defined RETRY_DELAY set RETRY_DELAY=1
call run.bat