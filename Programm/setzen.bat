@echo off
setlocal EnableExtensions EnableDelayedExpansion
set "BACKUP_DIR=%~dp0Backup"
set "REMOTE_ARCHIVE=/tmp/iobroker-tool-restore.tar.gz"
set /a COUNT=0

if not exist "%BACKUP_DIR%\*.tar.gz" (
    echo.
    echo Keine Backups in "%BACKUP_DIR%" gefunden.
    exit /b 1
)

echo.
echo Verfuegbare Backups:
echo.
for /f "delims=" %%F in ('dir /b /a-d /o-d "%BACKUP_DIR%\*.tar.gz" 2^>nul') do (
    set /a COUNT+=1
    set "BACKUP_!COUNT!=%%F"
    for %%S in ("%BACKUP_DIR%\%%F") do echo   [!COUNT!] %%F  ^(%%~zS Bytes^)
)
echo   [0] Abbrechen
echo.

set "CHOICE="
set /p "CHOICE=Backup auswaehlen: "
if "%CHOICE%"=="0" exit /b 0
for /f "delims=0123456789" %%A in ("%CHOICE%") do goto INVALID
if not defined CHOICE goto INVALID
if %CHOICE% LSS 1 goto INVALID
if %CHOICE% GTR %COUNT% goto INVALID

for %%N in (%CHOICE%) do set "SELECTED=!BACKUP_%%N!"
echo.
echo ACHTUNG: Dieses Backup wird wiederhergestellt:
echo !SELECTED!
set "CONFIRM="
set /p "CONFIRM=Zum Fortfahren exakt JA eingeben: "
if /I not "!CONFIRM!"=="JA" (
    echo Wiederherstellung abgebrochen.
    exit /b 0
)

echo.
echo Lade das Backup zur Vorabpruefung hoch ...
"%~dp0Programm\pscp.exe" -v -batch -pw %PSW% "%BACKUP_DIR%\!SELECTED!" %BEN%@%IP%:%REMOTE_ARCHIVE%
if errorlevel 1 goto UPLOAD_ERROR

echo.
echo Pruefe und restauriere das Backup ...
"%~dp0Programm\plink.exe" -t -v %BEN%@%IP% -pw %PSW% -m "%~dp0Programm\backup-restore.txt"
if errorlevel 1 goto RESTORE_ERROR

echo.
echo ==============================================================
echo Wiederherstellung erfolgreich abgeschlossen.
echo ==============================================================
exit /b 0

:INVALID
echo Ungueltige Auswahl.
exit /b 1

:UPLOAD_ERROR
echo.
echo FEHLER: Das Backup konnte nicht vollstaendig hochgeladen werden.
exit /b 1

:RESTORE_ERROR
echo.
echo FEHLER: Die Wiederherstellung war nicht erfolgreich.
echo Die Remote-Routine hat bei einem Entpackfehler die alten Daten
echo automatisch zurueckgesetzt. Bitte die angezeigten Details pruefen.
exit /b 1
