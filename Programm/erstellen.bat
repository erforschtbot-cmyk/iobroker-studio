@echo off
setlocal
set "REMOTE_ARCHIVE=/tmp/iobroker-tool-backup.tar.gz"
set "BACKUP_DIR=%~dp0Backup"

if not exist "%BACKUP_DIR%" mkdir "%BACKUP_DIR%"

echo.
echo Erstelle und pruefe das Backup auf %IP% ...
"%~dp0Programm\plink.exe" -t -v %BEN%@%IP% -pw %PSW% -m "%~dp0Programm\backup-create.txt"
if errorlevel 1 goto REMOTE_ERROR

for /f %%D in ('powershell -NoProfile -Command "Get-Date -Format yyyy-MM-dd_HH-mm-ss"') do set "STAMP=%%D"
if not defined STAMP set "STAMP=Backup"
set "LOCAL_ARCHIVE=%BACKUP_DIR%\ioBroker-Backup_%STAMP%.tar.gz"

echo.
echo Lade das gepruefte Backup herunter ...
"%~dp0Programm\pscp.exe" -v -batch -pw %PSW% %BEN%@%IP%:%REMOTE_ARCHIVE% "%LOCAL_ARCHIVE%"
if errorlevel 1 goto DOWNLOAD_ERROR

if not exist "%LOCAL_ARCHIVE%" goto DOWNLOAD_ERROR
for %%F in ("%LOCAL_ARCHIVE%") do if %%~zF LEQ 0 goto DOWNLOAD_ERROR

"%~dp0Programm\plink.exe" -batch %BEN%@%IP% -pw %PSW% "rm -f %REMOTE_ARCHIVE%"

echo.
echo ==============================================================
echo Backup erfolgreich:
echo %LOCAL_ARCHIVE%
echo ==============================================================
exit /b 0

:REMOTE_ERROR
echo.
echo FEHLER: Das Backup wurde auf dem ioBroker-System nicht erfolgreich erstellt.
echo Es wurde keine lokale Backup-Datei angelegt.
exit /b 1

:DOWNLOAD_ERROR
echo.
echo FEHLER: Das Backup konnte nicht vollstaendig heruntergeladen werden.
echo Das Remote-Archiv bleibt zur Kontrolle unter %REMOTE_ARCHIVE% erhalten.
if exist "%LOCAL_ARCHIVE%" del "%LOCAL_ARCHIVE%"
exit /b 1
