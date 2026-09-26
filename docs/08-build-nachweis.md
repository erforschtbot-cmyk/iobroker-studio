# 08 — Build-Nachweis

Dieser Nachweis belegt, dass das Repository **aus sich heraus** baubar ist. Er wurde
nicht behauptet, sondern ausgeführt.

## Rahmen

| Feld | Wert |
|------|------|
| Datum | 2026-09-26 |
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
| `ioBroker Studio.exe` | 43 851 027 | `6069fd31008456760757cec4f96ecfb585f8af770b0b593c2d62cdedf13d72bf` (Referenz) |
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
