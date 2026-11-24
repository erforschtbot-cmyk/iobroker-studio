#!/usr/bin/env bash
# Reproduzierbarer Build von ioBroker Studio für Windows x64.
#
# Wichtig: Es wird ein FRISCHER Baum aufgebaut, in dem die von der csproj
# erwartete Struktur <wurzel>/Next/IoBrokerTool + <wurzel>/Programm/Programm
# garantiert eingehalten wird. Nur so findet der Build die EmbeddedResource-Dateien.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
WORK="${BUILD_DIR:-/tmp/iobroker-studio-build}"
OUT="${OUT_DIR:-$ROOT/publish}"

DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet-sdk}"
DOTNET="$DOTNET_ROOT/dotnet"

if [ ! -x "$DOTNET" ]; then
    echo "FEHLER: $DOTNET nicht gefunden. Zuerst scripts/install-dotnet-8.0.423.sh ausführen." >&2
    exit 1
fi

echo "==> Frischer Arbeitsbaum: $WORK"
rm -rf "$WORK"
mkdir -p "$WORK/Next" "$WORK/Programm/Programm"
cp -a "$ROOT/Next/IoBrokerTool" "$WORK/Next/IoBrokerTool"
cp -a "$ROOT/Programm/Programm/." "$WORK/Programm/Programm/"

echo "==> Restore (locked mode, Ziel-RID win-x64)"
# Die Sperrdatei enthaelt nur den Ziel-RID net8.0/win-x64. Ohne die explizite
# RuntimeIdentifier-Angabe vergleicht NuGet gegen net8.0 und bricht mit NU1004 ab
# ("Die Runtimebezeichner des Projekts wurden geaendert").
"$DOTNET" restore "$WORK/Next/IoBrokerTool/IoBrokerTool.csproj" \
    --locked-mode -p:RuntimeIdentifier=win-x64 -p:PublishSingleFile=true

echo "==> Publish (win-x64, self-contained, single file)"
"$DOTNET" publish "$WORK/Next/IoBrokerTool/IoBrokerTool.csproj" \
    -c Release -r win-x64 --self-contained true \
    -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true \
    --no-restore \
    -o "$OUT"

echo
echo "==> Ergebnis in $OUT:"
ls -la "$OUT"
echo
echo "Hinweis: Die NU1903-Warnungen zu SSH.NET sind erwartet (docs/07-sicherheit.md)."
