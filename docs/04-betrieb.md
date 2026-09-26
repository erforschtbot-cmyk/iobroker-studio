# 04 — Betrieb der Anwendung (Nutzersicht)

> Quelle: `Next/README-Release.txt` (liegt im ausgelieferten Ordner) und
> `Next/README-Windows.txt`. Diese Seite fasst zusammen, was das Programm tut und
> welche Vorgänge das Zielsystem verändern.

## Start

1. Ordner mit der Anwendung auf einen Windows-x64-Rechner kopieren.
2. `ioBroker Studio.exe` starten (oder `Legacy-start.bat`).
3. IP/Hostname, SSH-Benutzer, Port und Passwort eintragen.
4. „Verbinden" wählen und den SSH-Fingerabdruck prüfen.

Es ist **keine** Installation von .NET, Python, PuTTY oder anderen Laufzeiten nötig.
Der Button „HILFE & ANLEITUNG" erklärt alle Funktionen; jeder Button, jedes Eingabefeld
und jede Checkbox hat einen Tooltip.

## Funktionsblöcke

| Bereich | Wirkung |
|---------|---------|
| **Backup** | Packt `iobroker-data`, `node_modules` und Paketinformationen in ein Archiv; optional Download nach Windows |
| **Restore (Datei)** | Lädt ein Archiv hoch, stoppt ioBroker, entpackt direkt nach `/opt/iobroker`, startet ioBroker |
| **Checkpoint** | Schneller Linux-Checkpoint unter `/opt/iobroker/.studio-checkpoint`; optional einmalig als Datei exportiert |
| **Reparatur & Upgrade** | Auswahl: Basisreparatur, Node.js, JS-Controller, native Module, Adapter. Diagnose, Stoppen, Endkontrolle und Wiederstart laufen immer |
| **Diagnose** | Führt `iob diag` aus, sendet das nötige Enter automatisch, zeigt die unveränderte Ausgabe live |
| **Integriertes Terminal** | Vollständiges SSH-Terminal; per Pfeilbutton in eigenes Fenster abdockbar |
| **PuTTY** | Direkter Schnellzugriff über mitgeliefertes `putty.exe` |
| **Image / Neuinstallation** | Offizieller ioBroker-Installer (`iobroker.net/install.sh`) auf einem frischen Ziel |

## Umgang mit Zugangsdaten

* „Passwort merken" verschlüsselt das Passwort per **Windows-DPAPI**, gebunden an das
  aktuelle Windows-Benutzerkonto. Ohne Haken wird nichts gespeichert.
* Der bestätigte **SSH-Hostschlüssel** wird gespeichert und bei späteren Verbindungen
  kontrolliert. Bei Erstverbindung wird der Fingerabdruck **vor** der
  Passwortübertragung abgerufen und bestätigt.
* Fordert Linux zusätzlich ein `sudo`-/`doas`-Passwort (darf vom SSH-Passwort
  abweichen), öffnet Studio einen sicheren Eingabedialog. Dieses Kennwort wird nur an
  den laufenden Vorgang gesendet — **nicht gespeichert, nicht protokolliert**.

## Protokolle

Alle längeren Aktionen erscheinen im Live-Protokoll und sind als `.log` speicherbar.
Remote-Protokolle liegen zusätzlich unter `/tmp` auf dem Linux-Zielsystem.

## Verhalten bei Fehlern

* **Reparatur:** Gilt nur dann als fehlgeschlagen, wenn ioBroker anschließend nicht
  startet. Läuft ioBroker trotz Teilproblemen, lautet das Ergebnis „abgeschlossen –
  Hinweise" mit nummerierter Liste der fehlgeschlagenen Stufen/Pfade.
* **Restore:** Vor dem Entpacken wird das Archiv geprüft; vorhandene Daten werden nur
  vorübergehend umbenannt und bei Fehlern automatisch zurückgesetzt. Ein abgebrochener
  Restore-/Reparaturlauf versucht ioBroker wieder in einen gestarteten Zustand zu bringen.

## Verteilte Ordnerstruktur (Release)

```
ioBroker Studio/
├── ioBroker Studio.exe
├── av_libglesv2.dll
├── libHarfBuzzSharp.dll
├── libSkiaSharp.dll
├── README.txt
└── THIRD-PARTY-NOTICES.txt
```

Backups landen standardmäßig unter `Dokumente\ioBroker Backups`.

## Nächster Schritt

→ [docs/05-remote-skripte.md](05-remote-skripte.md)
