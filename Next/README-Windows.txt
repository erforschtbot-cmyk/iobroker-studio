ioBroker Studio 1.2.39
===================

Start
-----
1. Den gesamten Ordner "ioBroker Studio" auf einen Windows-x64-Rechner kopieren.
2. "ioBroker Studio.exe" starten.
3. IP/Hostname, SSH-Benutzer, Port und Passwort eintragen.
4. Einmal "Verbinden" wählen und den SSH-Fingerabdruck kontrollieren.

Eine Installation von .NET, Python, PuTTY oder anderen Laufzeiten ist nicht nötig.

Der Button "HILFE & ANLEITUNG" erklärt alle Funktionen in einfacher Sprache.
Zusätzlich besitzt jeder Button, jedes Eingabefeld und jede Checkbox einen
Hinweis, der beim Verweilen mit dem Mauszeiger angezeigt wird.

Integriertes Terminal
---------------------
Die rechte obere Karte ist ein vollständiges SSH-Terminal. Nach "Verbinden"
wird sie automatisch mit dem Zielsystem verbunden. In die schwarze Fläche
klicken und Linux-Befehle direkt eingeben. Farben, Cursor, Pfeiltasten,
Scrollback und interaktive Konsolenprogramme werden unterstützt. Rechtsklick
kopiert markierten Text oder fügt Text aus der Zwischenablage ein.
Zum Kopieren Text markieren und rechtsklicken. Ist nichts markiert, fügt ein
Rechtsklick den Inhalt der Windows-Zwischenablage ein. Strg+C behält seine
normale Linux-Funktion zum Abbrechen eines laufenden Befehls.

Zugangsdaten
------------
Mit "Passwort merken" wird das Passwort über Windows-DPAPI verschlüsselt und an
das aktuelle Windows-Benutzerkonto gebunden. Ohne Haken wird kein Passwort
gespeichert. Der bestätigte SSH-Hostschlüssel wird gespeichert und bei späteren
Verbindungen kontrolliert. Bei einer Erstverbindung wird der Fingerabdruck vor
der Passwortübertragung abgerufen und bestätigt.

Fordert Linux während einer Aktion zusätzlich ein sudo- oder doas-Passwort an,
öffnet ioBroker Studio automatisch einen sicheren Eingabedialog. Dieses
Administratorkennwort darf vom SSH-Passwort abweichen. Es wird nur an
den laufenden Vorgang gesendet, nicht gespeichert und nicht protokolliert.

Backups
-------
Backups landen standardmäßig unter:
  Dokumente\ioBroker Backups

Vor einem Restore wird das Archiv geprüft. Die vorhandenen ioBroker-Daten
werden auf dem Linux-System zunächst umbenannt und bei Fehlern automatisch
zurückgesetzt. Zusätzlich prüft Studio Archivtypen und freien Speicherplatz.
Ein abgebrochener Restore oder Reparaturlauf versucht ioBroker automatisch
wieder in einen sicheren, gestarteten Zustand zu bringen.

Protokolle
----------
Alle längeren Aktionen erscheinen im Live-Protokoll und können als .log-Datei
gespeichert werden. Remote-Protokolle liegen zusätzlich unter /tmp auf dem
Linux-Zielsystem.

Reparatur & Upgrade
-------------------
Nach dem Anklicken erscheint eine Auswahl für Basisreparatur, Node.js,
JS-Controller, native Module und Adapter. Alle Haken sind vorausgewählt und
führen zusammen den vollständigen bisherigen Reparaturlauf aus. Einzelne
Stufen können gezielt abgewählt werden. Systemdiagnose, kontrolliertes Stoppen,
Endkontrolle und Wiederstart bleiben immer aktiv.

Zwei weitere, standardmäßig abgewählte Stufen betreffen das Linux-System:
"Boot-Ziel auf Serverbetrieb" stellt multi-user.target ein, wenn der Rechner mit
grafischer Oberfläche startet, und "Systempakete aktualisieren" führt das
reguläre Paket-Update der Distribution aus (ohne Distributionswechsel). Beide
wirken erst beim nächsten Neustart.

Rückfragen des offiziellen ioBroker-Fixers werden ohne Eingabe mit "Nein"
beantwortet, damit der Lauf nicht auf eine Tastatureingabe wartet. Bewusste
Entscheidungen trifft man über die beiden System-Stufen.

Systemvoraussetzungen
---------------------
- Windows 10/11, 64-Bit
- Netzwerkzugriff auf das Linux-/ioBroker-Ziel
- aktivierter SSH-Zugang
- für Systemänderungen root, sudo oder doas auf dem Zielsystem

Wichtiger Hinweis
-----------------
Reparaturen, Updates, Restore und Neuinstallation verändern das Zielsystem.
Die Anwendung zeigt vor diesen Aktionen eine ausdrückliche Bestätigung.

Copyright
---------
ioBroker Studio: Elzershark
Hinweise zu verwendeten Open-Source-Komponenten stehen in
THIRD-PARTY-NOTICES.txt.
