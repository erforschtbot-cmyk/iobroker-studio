# 02 — Build und Publish

## Voraussetzung

* SDK 8.0.423 installiert und `dotnet --version` liefert `8.0.423`
  (siehe [docs/01-installation.md](01-installation.md)).

## Wichtig: Projektbaum und Ressourcen-Pfade

Das Projekt `Next/IoBrokerTool/IoBrokerTool.csproj` bindet die acht Remote-Skripte als
**EmbeddedResource** relativ zum Projekt ein:

```
../../Programm/Programm/backup-create.txt   →  IoBrokerTool.Scripts.backup-create.sh
../../Programm/Programm/backup-restore.txt  →  IoBrokerTool.Scripts.backup-restore.sh
../../Programm/Programm/checkpoint-create.txt  →  …checkpoint-create.sh
../../Programm/Programm/checkpoint-restore.txt →  …checkpoint-restore.sh
../../Programm/Programm/checkpoint-export.txt  →  …checkpoint-export.sh
../../Programm/Programm/repair.txt             →  …repair.sh
../../Programm/Programm/error-diagnostics.txt  →  …error-diagnostics.sh
../../Programm/Programm/iobroker-install.txt   →  …iobroker-install.sh
```

Deshalb **muss** die Verzeichnisstruktur eingehalten werden:

```
<wurzel>/Next/IoBrokerTool/IoBrokerTool.csproj
<wurzel>/Programm/Programm/*.txt
```

Wird nur `Next/IoBrokerTool` kopiert, bricht der Build mit fehlenden
`EmbeddedResource`-Dateien ab. Das Build-Skript `scripts/build-windows-x64.sh` legt
genau diese Struktur automatisch an.

## Build (Linux-Cross nach Windows x64)

```bash
./scripts/build-windows-x64.sh
```

Das Skript:

1. kopiert `Next/` und `Programm/Programm/` in ein frisches Arbeitsverzeichnis,
2. stellt die Pakete in **locked mode** aus `packages.lock.json` wieder her,
3. publiziert:

```bash
dotnet publish Next/IoBrokerTool/IoBrokerTool.csproj \
  -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true \
  -o publish
```

## Erwartetes Ergebnis

```
publish/ioBroker Studio.exe        ~40–45 MB   (Name aus <AssemblyName>)
publish/av_libglesv2.dll           ~ 5 MB
publish/libHarfBuzzSharp.dll       ~ 2 MB
publish/libSkiaSharp.dll           ~11 MB
publish/*.pdb                      (Debug-Symbole, optional)
```

Die EXE ist selbstenthalten; die drei Skia-/HarfBuzz-Bibliotheken werden daneben
benötigt. Für die Verteilung gehören sie mit in den Release-Ordner.

Belegter Referenz-Build: [docs/08-build-nachweis.md](08-build-nachweis.md).

## Warnung beim Restore ist erwartet

NuGet meldet bei `SSH.NET 2025.1.0` zwei bekannte Sicherheitshinweise (NU1903). Das ist
**erwartet** und wird nicht als Buildfehler gewertet — Details und Einordnung:
[docs/07-sicherheit.md](07-sicherheit.md).

## Bauen unter Windows

```powershell
cd <wurzel>
dotnet publish Next\IoBrokerTool\IoBrokerTool.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o publish
```

## Nächster Schritt

→ [docs/03-struktur.md](03-struktur.md)
