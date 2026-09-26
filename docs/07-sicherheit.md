# 07 — Sicherheitsmodell und bekannte Warnungen

## Sicherheitsmerkmale der Anwendung

* **Host-Key-Prüfung vor Passwortübertragung.** Der SSH-Hostschlüssel wird in einer
  absichtlich abgewiesenen Vorabverbindung erfasst; der Fingerabdruck wird dem Nutzer
  **vor** dem Senden des Passworts zur Bestätigung gezeigt. Neue oder geänderte
  Schlüssel werden erst danach akzeptiert.
* **Passwortspeicherung optional und gebunden.** Nur bei gesetztem Haken, verschlüsselt
  über **Windows-DPAPI** und an das aktuelle Windows-Benutzerkonto gebunden.
* **Sudo-Passwort flüchtig.** Ein abweichendes `sudo`-/`doas`-Kennwort wird nur an den
  laufenden Vorgang gesendet, nicht gespeichert und nicht protokolliert.
* **Keine Zugangsdaten im Repository.** Es liegen keine Schlüssel, Tokens oder
  Zugangsdaten in diesem Repo (siehe `scripts/secret-scan.sh`).

## Bekannte Paket-Warnungen (erwartet)

NuGet meldet beim Restore von `SSH.NET 2025.1.0` zwei **bekannte** Sicherheitshinweise
(NU1903):

* `GHSA-q939-rpr3-3284`
* `GHSA-mggc-4xg6-vcxf`

Beide betreffen `SSH.NET` und sind der zum Release-Zeitpunkt aktuelle Stand des Pakets.
Sie wurden beim Release 1.2.28 bewusst in Kauf genommen, weil ein Upgrade von `SSH.NET`
eigene Tests der SFTP-/Skriptpfade erfordert. Ein Neuaufbau **muss** diese Warnungen
erwarten; sie sind **kein** Buildfehler und ändern das Ergebnis nicht.

> Empfehlung für eine künftige Version: `SSH.NET` aktualisieren, die betroffenen Pfade
> (SFTP-Upload, Skriptausführung, Host-Key-Handling) gezielt testen und die Lock-Datei
> neu erzeugen.

## Angriffsfläche

* Die Anwendung spricht über SSH mit dem Zielsystem und führt dort die **fest
  eingebetteten** Skripte aus `Programm/Programm/*.txt` aus. Es kann kein beliebiger
  Remote-Code „eingeschleust" werden, ohne den Quellcode zu ändern.
* `repair.txt` und `iobroker-install.txt` laden Skripte von offiziellen Quellen
  (`raw.githubusercontent.com/ioBroker/…`, `iobroker.net/install.sh`). Diese Aufrufe
  laufen als Teil des offiziellen Reparatur-/Installationsablaufs.
* Die Legacy-Batch-Variante (`Programm/*.bat`) übergibt Zugangsdaten als
  Kommandozeilenparameter an `plink.exe`/`pscp.exe`; das ist eine bekannte Schwäche der
  alten Variante, die von der neuen Anwendung (SSH.NET) nicht mehr genutzt wird.

## Scannen

```bash
./scripts/secret-scan.sh   # prüft auf übliche Geheimnis-Muster
./scripts/verify-integrity.sh
```

## Nächster Schritt

→ [docs/08-build-nachweis.md](08-build-nachweis.md)
