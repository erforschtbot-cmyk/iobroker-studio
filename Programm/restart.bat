@echo off
goto END
:END
cls
echo.
echo                 Zugangsdaten
echo      ---------------------------------
echo              IP= %IP%
echo              Benutzername= %BEN%
echo              Passwort= %PSW% 
echo      =================================
echo.
echo.	
echo              Backup-Skript
echo              =============
echo.	
echo   [1] Backup erstellen/wiederherstellen
echo   [2] ioBroker reparieren/upgraden
echo   [3] Putty oeffnen
echo   [4] Download Image und Installation ioBroker
echo   [5] Netzwerk / Datei-Transfer
echo   [6] Beenden
echo.
set asw=0
set /p asw="Bitte Auswahl eingeben: "
if %asw%==1 goto FILES
if %asw%==2 goto REPA
if %asw%==3 goto PUTTY
if %asw%==4 goto DOWN
if %asw%==5 goto WLAN
if %asw%==6 goto STOP
goto END
:FILES
start backup-menu.bat
goto STOP
:REPA
start repa.bat
goto STOP
:WLAN
start wlan.bat
goto STOP
:PUTTY
start putty.bat
goto END
:DOWN
start image.bat
goto STOP
:STOP
exit
