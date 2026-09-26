#!/usr/bin/env bash
# Prüft die Integrität der ausgelieferten Dateien und die Existenz der Pflichtdateien.
set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "==> Pflichtdateien"
missing=0
for f in \
    README.md CHECKSUMS.sha256 \
    Next/IoBrokerTool/IoBrokerTool.csproj \
    Next/IoBrokerTool/packages.lock.json \
    Programm/Programm/repair.txt \
    release/ioBroker-Studio-1.2.28-Windows-x64.zip
do
    if [ -e "$f" ]; then printf '  ok    %s\n' "$f"; else printf '  FEHLT %s\n' "$f"; missing=1; fi
done

echo
echo "==> Prüfsummen"
if command -v sha256sum >/dev/null 2>&1; then
    sha256sum -c CHECKSUMS.sha256 || missing=1
else
    echo "sha256sum nicht vorhanden."
    missing=1
fi

echo
if [ "$missing" -eq 0 ]; then echo "Integrität in Ordnung."; else echo "Abweichungen gefunden." >&2; exit 1; fi
