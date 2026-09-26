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
echo                 Backup-Menue
echo                 ============
echo.
echo   [1] Neues Backup erstellen
echo   [2] Vorhandenes Backup wiederherstellen
echo   [3] Zurueck zum Hauptmenue
echo.
set "asw="
set /p "asw=Bitte Auswahl eingeben: "
if "%asw%"=="1" goto CREATE
if "%asw%"=="2" goto RESTORE
if "%asw%"=="3" goto BACK
goto MENU

:CREATE
call erstellen.bat
pause
goto MENU

:RESTORE
call setzen.bat
pause
goto MENU

:BACK
start restart.bat
exit /b
