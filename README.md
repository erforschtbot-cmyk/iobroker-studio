# ioBroker Studio

Professionelles Windows-Desktop-Werkzeug zur Wartung entfernter ioBroker-Installationen
über SSH: Backup/Restore, Checkpoints, Reparatur/Upgrade, Systemdiagnose,
Image-/Neuinstallation und ein integriertes SSH-Terminal.

**Autor:** Elzershark · **Stand:** 1.2.28

---

## Zweck dieses Repositories

Dieses Repository ist eine **reproduzierbare Baseline**. Ein fremder Agent oder
Entwickler soll allein durch Lesen dieser Dateien in der Lage sein:

1. das Projekt aus dem Quellcode neu zu **bauen**,
2. die **verteilte Anwendung** wiederherzustellen,
3. die **Remote-Skripte** zu verstehen, die im Betrieb auf dem Linux-Zielsystem laufen,
4. den Build zu **prüfen** (Prüfsummen, Lock-Datei, Nachweis).

Es enthält **Quellcode und Anleitungen**, keine Laufzeitdaten, keine Zugangsdaten.

---

## Schnellstart (Neuaufbau in 3 Schritten)

```bash
# 1. Toolchain installieren (.NET SDK 8.0.423, gepinnt)
./scripts/install-dotnet-8.0.423.sh          # Linux/macOS
#   Windows: winget install Microsoft.DotNet.SDK.8  (siehe docs/01-installation.md)

# 2. Bauen (Cross-Build für Windows x64 ist von Linux aus möglich)
./scripts/build-windows-x64.sh

# 3. Ergebnis prüfen
sha256sum -c CHECKSUMS.sha256
```

Ergebnis: `publish/ioBroker Studio.exe` (selbstenthaltene Single-File-EXE, kein .NET auf
dem Ziel-PC nötig).

---

## Dokumentation (in Bau-Reihenfolge lesen)

| Nr. | Datei | Inhalt |
|-----|-------|--------|
| 01 | [docs/01-installation.md](docs/01-installation.md) | Voraussetzungen, SDK-Installation (Windows/Linux) |
| 02 | [docs/02-build.md](docs/02-build.md) | Vollständiger Build-/Publish-Ablauf, erwartetes Ergebnis |
| 03 | [docs/03-struktur.md](docs/03-struktur.md) | Verzeichnisstruktur, Rolle jeder Komponente |
| 04 | [docs/04-betrieb.md](docs/04-betrieb.md) | Bedienung der Anwendung (aus Nutzersicht) |
| 05 | [docs/05-remote-skripte.md](docs/05-remote-skripte.md) | Die acht Shell-Skripte und ihr Ablauf |
| 06 | [docs/06-release.md](docs/06-release.md) | Neues Release-ZIP bauen |
| 07 | [docs/07-sicherheit.md](docs/07-sicherheit.md) | Sicherheitsmodell, bekannte Paket-Warnung |
| 08 | [docs/08-build-nachweis.md](docs/08-build-nachweis.md) | Belegter Build (SDK, Ergebnis, Datum) |

---

## Repository-Struktur (Kurzform)

```
Next/IoBrokerTool/     C#/Avalonia-Quellcode der Anwendung (das "Produkt")
Next/Legacy-start.bat  Starter der alten Batch-Variante
Next/README-Windows.txt  Nutzer-Doku, wie sie im Release liegt
Programm/              Remote-Skripte (.txt) + Legacy-Batch-Steuerung
Programm/Programm/     Die acht Bash-Skripte, die auf dem Linux-Ziel laufen
release/               Vorbestätigtes Release-Archiv als Wiederherstellungsanker
docs/                  Diese Dokumentation
scripts/               Installations- und Build-Skripte
CHECKSUMS.sha256       Prüfsummen aller ausgelieferten Dateien
```

Details: [docs/03-struktur.md](docs/03-struktur.md)

---

## Wiederherstellungsanker

`release/ioBroker-Studio-1.2.28-Windows-x64.zip` ist die zum Stand 1.2.28 tatsächlich
ausgelieferte, **geprüfte** Anwendung. Sie dient als Rückweg, falls ein Neuaufbau aus
dem Quellcode einmal nicht gelingen sollte. Prüfsumme siehe `CHECKSUMS.sha256`.

Der Ordner `Programm/` enthält die Windows-Programme `putty.exe`, `plink.exe`, `pscp.exe`
und `XXMKLINK.EXE` **nicht** — diese Fremdprogramme stammen von Dritten und werden für
einen Neuaufbau nicht benötigt (siehe [docs/03-struktur.md](docs/03-struktur.md)).

---

## Lizenz / Dritte

ioBroker Studio: Elzershark. Hinweise zu Open-Source-Komponenten:
[THIRD-PARTY-NOTICES.txt](Next/THIRD-PARTY-NOTICES.txt).
