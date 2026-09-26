# 06 — Release-Archiv erzeugen

Ziel: aus dem Quellcode wieder genau den ausgelieferten Ordner bauen, wie er im
Release-ZIP liegt.

## Ablauf

```bash
./scripts/build-windows-x64.sh          # erzeugt ./publish
./scripts/make-release-zip.sh 1.2.28    # erzeugt release/ioBroker-Studio-<ver>-Windows-x64.zip
```

Das Release-ZIP enthält einen Ordner `ioBroker Studio/` mit:

```
ioBroker Studio.exe
av_libglesv2.dll
libHarfBuzzSharp.dll
libSkiaSharp.dll
README.txt                 (= Next/README-Release.txt)
THIRD-PARTY-NOTICES.txt
```

`.pdb`-Symboldateien werden **nicht** mit ausgeliefert.
Der Legacy-Starter `Legacy-start.bat` wird beim Bauen der Anwendung beigelegt oder
separat verteilt; er erwartet den Pfad `ioBroker Studio\ioBroker Studio.exe` relativ zu
sich selbst (siehe `Next/Legacy-start.bat`).

## Versionsnummer

Die Version steht an **zwei** Stellen und muss übereinstimmen:

* `Next/IoBrokerTool/IoBrokerTool.csproj` → `<Version>`
* Name des Release-ZIPs

## Prüfsummen aktualisieren

Nach einem neuen Release-ZIP:

```bash
sha256sum release/*.zip > /tmp/zip.sha
# in CHECKSUMS.sha256 den release/-Eintrag ersetzen
```

## Wiederherstellungsanker

`release/ioBroker-Studio-1.2.28-Windows-x64.zip` ist der zum Stand 1.2.28 ausgelieferte
Stand. Er bleibt als Rückweg im Repository, auch wenn später eine neuere Version
gebaut wird — dann kommen neue Archivversionen **neben** das alte hinzu.

## Nächster Schritt

→ [docs/07-sicherheit.md](07-sicherheit.md)
