@echo off
:MENU
cls
echo.
echo                 Zugangsdaten
echo      ---------------------------------
echo              IP= %IP%
echo              Benutzername= %BEN%
echo              Passwort= ********
echo      =================================
echo.
echo             NETZWERK / DATEITRANSFER
echo             ========================
echo.
echo   [1] Verbindung zum Zielsystem pruefen
echo   [2] Bekannte Netzwerkgeraete anzeigen
echo   [3] Router-/Heimnetzseite oeffnen
echo   [4] Dateien mit WinSCP uebertragen
echo   [5] Zurueck zum Hauptmenue
echo.
set "asw="
set /p "asw=Bitte Auswahl eingeben: "
if "%asw%"=="1" goto CHECK
if "%asw%"=="2" goto DEVICES
if "%asw%"=="3" goto ROUTER
if "%asw%"=="4" goto WINSCP
if "%asw%"=="5" goto BACK
goto MENU

:CHECK
cls
echo.
echo Pruefe Netzwerk und SSH fuer %BEN%@%IP% ...
echo.
ping -n 2 -w 1500 "%IP%"
echo.
"%~dp0Programm\plink.exe" -batch %BEN%@%IP% -pw %PSW% "printf 'SSH-Verbindung erfolgreich\n'; uname -a"
if errorlevel 1 (
    echo.
    echo SSH-Test fehlgeschlagen.
    echo Moegliche Ursachen: falsche IP/Zugangsdaten, SSH deaktiviert
    echo oder der Host-Schluessel wurde noch nicht mit PuTTY bestaetigt.
    echo In letzterem Fall Hauptmenue [4] einmal oeffnen.
) else (
    echo.
    echo Netzwerk und SSH funktionieren.
)
echo.
pause
goto MENU

:DEVICES
cls
echo.
echo Bekannte Netzwerkgeraete aus dem Windows-ARP-Cache:
echo ==================================================
echo.
arp -a
echo.
echo Hinweis: Angezeigt werden nur Geraete, mit denen Windows kuerzlich
echo kommuniziert hat. Fuer eine vollstaendige Liste ist die Heimnetz-
echo beziehungsweise Netzwerkansicht des Routers genauer.
echo.
pause
goto MENU

:ROUTER
set "GATEWAY="
for /f "usebackq delims=" %%G in (`powershell -NoProfile -Command "Get-NetRoute -DestinationPrefix '0.0.0.0/0' ^| Sort-Object RouteMetric ^| Select-Object -First 1 -ExpandProperty NextHop" 2^>nul`) do set "GATEWAY=%%G"
if not defined GATEWAY (
    echo.
    echo Das Standard-Gateway konnte nicht automatisch ermittelt werden.
    pause
    goto MENU
)
echo.
echo Oeffne die Heimnetzseite unter http://%GATEWAY%/ ...
start "" "http://%GATEWAY%/"
goto MENU

:WINSCP
set "WINSCP_EXE="
if exist "%ProgramFiles%\WinSCP\WinSCP.exe" set "WINSCP_EXE=%ProgramFiles%\WinSCP\WinSCP.exe"
if exist "%ProgramFiles(x86)%\WinSCP\WinSCP.exe" set "WINSCP_EXE=%ProgramFiles(x86)%\WinSCP\WinSCP.exe"
if not defined WINSCP_EXE for %%W in (WinSCP.exe) do set "WINSCP_EXE=%%~$PATH:W"

if not defined WINSCP_EXE (
    echo.
    echo Keine aktuell installierte WinSCP-Version gefunden.
    echo Die offizielle Downloadseite wird geoeffnet.
    echo Das alte mitgelieferte WinSCP wird aus Sicherheitsgruenden nicht gestartet.
    start "" "https://winscp.net/eng/download.php"
    pause
    goto MENU
)

echo.
echo Starte WinSCP fuer %BEN%@%IP%.
echo Das Passwort wird absichtlich nicht in der Windows-Prozesszeile uebergeben.
start "" "%WINSCP_EXE%" "sftp://%BEN%@%IP%/"
goto MENU

:BACK
start restart.bat
exit /b
