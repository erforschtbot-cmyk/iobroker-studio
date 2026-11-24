# 08 — Build-Nachweis

Dieser Nachweis belegt, dass das Repository **aus sich heraus** baubar ist. Er wurde
nicht behauptet, sondern ausgeführt.

## Rahmen

| Feld | Wert |
|------|------|
| Datum | 2026-11-01 (Version 1.2.39) |
| Build-Host | Linux 7.1.8 (CachyOS), x64 |
| SDK | **.NET 8.0.423** (`dotnet --version` → `8.0.423`) |
| Bezug des SDK | `scripts/install-dotnet-8.0.423.sh` (offizielles dotnet-install.sh, Version gepinnt) |
| Quelle | frisch kopierter Baum aus `Next/` + `Programm/Programm/` |
| Ziel | `win-x64`, self-contained, Single-File |

## Ausgeführter Befehl

```bash
dotnet publish Next/IoBrokerTool/IoBrokerTool.csproj \
  -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true \
  -o publish
```

Ergebnis der Kompilierung: `IoBrokerTool -> …/ioBroker Studio.dll` und
`IoBrokerTool -> publish/` — **erfolgreich**, ohne Fehler.

## Erzeugte Artefakte

| Datei | Größe (Bytes) | SHA-256 |
|-------|---------------|---------|
| `ioBroker Studio.exe` | 43 855 123 | `69ffbdf8f46380658da3d14ff35cdbeb334bdaf2a240d0f7bcc7fdd5fe6bdb1e` (Referenz) |
| `av_libglesv2.dll` | 5 426 176 | `9b203e40323b49dad29546a52b8b67d200bba8ff4cab9709a79cede23ba847d4` |
| `libHarfBuzzSharp.dll` | 1 816 088 | `145ade963eba427027d5e1381db5d51a0ef9e98e9fef9efbfa6074ca20da246f` |
| `libSkiaSharp.dll` | 11 626 016 | `647688159aa6af0baa8aaf831884c23515f18a72c28b7ddded66828495cf5427` |

> Die EXE-Prüfsumme ist **nur ein Referenzwert** des hier gelaufenen Builds. Ein
> .NET-Single-File-Build kann je nach Build-Host und Zeitstempeln abweichen; verbindlich
> sind Quellcode, Lock-Datei und Toolchain-Version.

## Erwartete Warnungen (kein Fehler)

```
warning NU1903: Paket "SSH.NET" 2025.1.0 weist eine bekannte Sicherheitsanfälligkeit auf
  https://github.com/advisories/GHSA-mggc-4xg6-vcxf
  https://github.com/advisories/GHSA-q939-rpr3-3284
```

Zwei Hinweise, beide in [docs/07-sicherheit.md](07-sicherheit.md) eingeordnet. Der Build
läuft mit **0 Fehlern** durch.

## Wiederholbarkeit

1. `./scripts/build-windows-x64.sh` in einem frischen Checkout ausführen.
2. Erwartetes Ergebnis: `publish/ioBroker Studio.exe` mit den oben genannten
   Begleitbibliotheken.
3. Abweichungen wären ein Beleg für eine geänderte Toolchain — dann SDK-Version prüfen
   (`dotnet --version` muss `8.0.423` sein) und `packages.lock.json` verwenden.

## Bezug

Zurück zur Übersicht: [../README.md](../README.md)

## Build 1.2.39 (2026-11-01)

Nachweis für die Fassung mit nicht-interaktivem Fixer und den beiden
System-Reparaturstufen:

* `./scripts/build-windows-x64.sh` → erfolgreich, keine Fehler
* `python3 -m unittest discover -s tests -v` → 3 Tests, OK
* `./scripts/make-release-zip.sh 1.2.39` → `release/ioBroker-Studio-1.2.39-Windows-x64.zip`
  (SHA-256 `b15de2ae62ffe4dc2aa99a22ff30c3e9e70fea574f3676bb95d027410bf37fb7`)
* `./scripts/verify-integrity.sh` → "Integrität in Ordnung."

Geprüft wurde außerdem, dass die eingebettete Ressource `repair.sh` im
kompilierten `ioBroker Studio.dll` die neuen Markierungen
`STUDIO_REPAIR_BOOTTARGET`/`STUDIO_REPAIR_OSUPDATES` enthält. Ein Starttest
unter Windows wurde **nicht** durchgeführt.
