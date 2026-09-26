# 01 — Voraussetzungen und Toolchain-Installation

## Was gebaut wird

Eine **Windows-x64-Desktopanwendung** (.NET 8, Avalonia UI), verteilt als
selbstenthaltene Single-File-EXE. Auf dem Ziel-PC ist **keine** .NET-, Python- oder
PuTTY-Installation nötig.

## Gepinnte Toolchain

| Komponente | Version | Anmerkung |
|------------|---------|-----------|
| .NET SDK | **8.0.423** | exakt diese Version, sie hat den Release 1.2.28 erzeugt |
| Ziel-Framework | `net8.0` | siehe `Next/IoBrokerTool/IoBrokerTool.csproj` |
| Runtime-Identifier | `win-x64` | |
| Paketquellen | nuget.org | exakte Versionen in `Next/IoBrokerTool/packages.lock.json` |
| Betriebssystem zum Bauen | Windows 10/11 x64 **oder** Linux x64 | Cross-Build von Linux ist belegt (docs/08) |
| Git | ≥ 2.30 | |

Die Paketversionen sind **nicht** geraten: `packages.lock.json` hält jede direkte und
transitive Abhängigkeit mit Content-Hash fest. Beim Bauen mit `--locked-mode` werden
genau diese Versionen verwendet.

## Variante A — Bauen unter Linux (belegt)

```bash
# SDK 8.0.423 nach ~/.dotnet-sdk installieren
./scripts/install-dotnet-8.0.423.sh

# Umgebung setzen (in jeder neuen Shell nötig)
export DOTNET_ROOT="$HOME/.dotnet-sdk"
export PATH="$DOTNET_ROOT:$PATH"
dotnet --version          # muss 8.0.423 ausgeben
```

Die Skripte erledigen das automatisch.

## Variante B — Bauen unter Windows

1. .NET SDK 8.0.423 installieren:
   - Download: https://dotnet.microsoft.com/download/dotnet/8.0
   - oder: `winget install Microsoft.DotNet.SDK.8`
2. Prüfen: `dotnet --version` → `8.0.423`
3. Weiter mit [docs/02-build.md](02-build.md)

> Hinweis: Eine abweichende SDK-Version kann andere Unterkomponenten ziehen. Für einen
> byte-genauen Nachbau exakt 8.0.423 verwenden. Die Lock-Datei verhindert, dass
> NuGet-Pakete unbemerkt wechseln.

## Keine weiteren Systempakete nötig

Der Publish ist selbstenthalten. Für den Cross-Build unter Linux wurden **keine**
zusätzlichen Distributionspakete benötigt.

## Nächster Schritt

→ [docs/02-build.md](02-build.md)
