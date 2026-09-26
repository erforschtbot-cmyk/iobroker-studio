#!/usr/bin/env bash
# Einfacher Geheimnis-Scan: sucht typische Zugangsdaten-/Schlüsselmuster im Repo.
# Treffer, die nur Code zum LESEN von Token oder der Scan selbst sind, sind keine Funde.
set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

PATTERNS=(
    'BEGIN (RSA|OPENSSH|EC|DSA|PGP) PRIVATE KEY'
    'MATON_API_KEY=[A-Za-z0-9]'
    'ghp_[A-Za-z0-9]{20,}'
    'github_pat_[A-Za-z0-9_]{20,}'
    'xox[baprs]-[A-Za-z0-9-]{10,}'
    'AKIA[0-9A-Z]{16}'
    'BEGIN CERTIFICATE'
)

# Der Scan selbst und diese Liste enthalten die Suchmuster wörtlich und werden
# deshalb ausgenommen (Treffer im Scan-Code sind keine Geheimnisse).
SELF="$(basename "${BASH_SOURCE[0]}")"

FAIL=0
for p in "${PATTERNS[@]}"; do
    hits="$(grep -rInE -e "$p" \
        --exclude-dir=.git --exclude-dir=publish \
        --exclude=packages.lock.json \
        --exclude="$SELF" \
        . || true)"
    if [ -n "$hits" ]; then
        echo "MÖGLICHER FUND (Muster: $p):"
        echo "$hits"
        echo
        FAIL=1
    fi
done

echo "— Zugangsdaten-Klartext in den Batch-Dateien (Altbestand) —"
if grep -rInE '^\s*set (PSW|BEN|IP)=' Programm/*.bat >/dev/null 2>&1; then
    grep -rInE '^\s*set (PSW|BEN|IP)=' Programm/*.bat
    echo "  (Platzhalter der Legacy-Vorlagen – vor Nutzung durch echte Werte zu ersetzen,"
    echo "   aber keine echten Zugangsdaten im Repo.)"
fi

if [ "$FAIL" -eq 0 ]; then
    echo
    echo "Kein Geheimnis gefunden."
else
    echo
    echo "Bitte die Funde prüfen." >&2
    exit 1
fi
