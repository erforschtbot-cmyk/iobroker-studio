@echo off
setlocal
echo.
echo Zielsystem: %BEN%@%IP%
echo.
echo Das Ziel muss bereits gestartet und per SSH erreichbar sein.
echo Der offizielle ioBroker-Installer installiert automatisch die aktuell
echo empfohlene Node.js-Version. Es wird keine Version im Tool festgelegt.
echo.
set "CONFIRM="
set /p "CONFIRM=Installation mit JA starten: "
if /I not "%CONFIRM%"=="JA" (
    echo Installation abgebrochen.
    exit /b 0
)

echo.
echo Starte den offiziellen ioBroker-Installer ...
"%~dp0Programm\plink.exe" -t -v %BEN%@%IP% -pw %PSW% -m "%~dp0Programm\iobroker-install.txt"
if errorlevel 1 (
    echo.
    echo FEHLER: Die ioBroker-Installation wurde nicht erfolgreich beendet.
    echo Bitte die Meldungen und den genannten Remote-Logpfad pruefen.
    exit /b 1
)

echo.
echo ==============================================================
echo ioBroker-Installation erfolgreich abgeschlossen.
echo ==============================================================
exit /b 0
