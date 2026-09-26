@echo off
goto END
:END
cls
echo.
echo.
echo                 Zugangsdaten
echo      ---------------------------------
echo              IP= %IP%
echo              Benutzername= %BEN%
echo              Passwort= %PSW% 
echo      =================================
echo.
echo.
echo       Reparatur/Upgrade ioBroker/Node
echo             =============
echo.	
echo   [1] Universelle Linux-Reparatur (Node/JS-Controller/Adapter)
echo   [6] Putty starten
echo   [7] Beenden
echo.
set asw=0
set /p asw="Bitte Auswahl eingeben: "
if %asw%==1 goto NODE
if %asw%==6 goto PUTTY
if %asw%==7 goto STOP
goto END
:NODE
cd Programm
start "ioBroker Universelle Reparatur" cmd /k plink.exe -t -v %BEN%@%IP% -pw %PSW% -m repair.txt
cd ..
goto END
:PUTTY
start putty.bat
goto END
:STOP
start restart.bat
exit
