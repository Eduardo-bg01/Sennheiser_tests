@echo off
setlocal EnableDelayedExpansion
set "ROOT=%~dp0"
pushd "%ROOT%"

set "APP_DIR=%ROOT%bin\"
if exist "%ROOT%SennheiserTestRunner.exe" set "APP_DIR=%ROOT%"

if not exist "%APP_DIR%SennheiserTestRunner.exe" (
    echo Error: SennheiserTestRunner.exe no encontrado. Ejecuta build-all.bat primero.
    exit /b 1
)

echo Running SennheiserTestRunner...
start /wait "" "%APP_DIR%SennheiserTestRunner.exe"

popd
exit /b %ERRORLEVEL%
