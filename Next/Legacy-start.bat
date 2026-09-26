@echo off
setlocal
set "APP=%~dp0ioBroker Studio\ioBroker Studio.exe"
if not exist "%APP%" (
    echo ioBroker Studio wurde nicht gefunden:
    echo "%APP%"
    pause
    exit /b 1
)
start "" "%APP%"
exit /b 0
