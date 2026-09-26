@echo off
:MENU
cls
echo.
echo                 Zugangsdaten
echo      ---------------------------------
echo              IP= %IP%
echo              Benutzername= %BEN%
echo              Passwort= %PSW%
echo      =================================
echo.
echo           IMAGE / IOBROKER-INSTALLATION
echo           =============================
echo.
echo   [1] Raspberry Pi Imager oeffnen/downloaden
echo   [2] ioBroker auf dem vorbereiteten Linux installieren
echo   [3] Zurueck zum Hauptmenue
echo.
echo   Empfohlener Ablauf:
echo     1. Raspberry Pi OS Lite 64-Bit im Imager auswaehlen.
echo     2. Unter "OS anpassen" Benutzer, Passwort, SSH und
echo        bei Bedarf WLAN konfigurieren.
echo     3. Datentraeger schreiben und den Raspberry Pi starten.
echo     4. Obige Zugangsdaten im Hauptmenue eintragen.
echo     5. Hier Punkt [2] ausfuehren.
echo.
set "abw="
set /p "abw=Bitte Auswahl eingeben: "
if "%abw%"=="1" goto IMAGER
if "%abw%"=="2" goto IOBROKER
if "%abw%"=="3" goto BACK
goto MENU

:IMAGER
if exist "%ProgramFiles%\Raspberry Pi Imager\rpi-imager.exe" (
    start "" "%ProgramFiles%\Raspberry Pi Imager\rpi-imager.exe"
    goto MENU
)
if exist "%ProgramFiles(x86)%\Raspberry Pi Imager\rpi-imager.exe" (
    start "" "%ProgramFiles(x86)%\Raspberry Pi Imager\rpi-imager.exe"
    goto MENU
)
echo.
echo Raspberry Pi Imager ist nicht installiert.
echo Die offizielle Downloadseite wird geoeffnet.
start "" "https://www.raspberrypi.com/software/"
pause
goto MENU

:IOBROKER
call iobroker.bat
pause
goto MENU

:BACK
start restart.bat
exit /b
