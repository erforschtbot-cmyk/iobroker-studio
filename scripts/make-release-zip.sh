#!/usr/bin/env bash
# Erzeugt das Release-Archiv aus dem Publish-Ordner.
# Aufruf: ./scripts/make-release-zip.sh <version>   (z. B. 1.2.28)
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION="${1:?Versionsnummer angeben, z. B. 1.2.28}"
PUBLISH="${PUBLISH_DIR:-$ROOT/publish}"
DIST="$ROOT/release"
NAME="ioBroker-Studio-${VERSION}-Windows-x64"
STAGE="$(mktemp -d)/ioBroker Studio"

[ -f "$PUBLISH/ioBroker Studio.exe" ] || { echo "FEHLER: $PUBLISH/ioBroker Studio.exe fehlt. Erst bauen." >&2; exit 1; }

mkdir -p "$STAGE" "$DIST"
cp -a "$PUBLISH/ioBroker Studio.exe" \
      "$PUBLISH/av_libglesv2.dll" \
      "$PUBLISH/libHarfBuzzSharp.dll" \
      "$PUBLISH/libSkiaSharp.dll" "$STAGE/"
cp -a "$ROOT/Next/README-Release.txt" "$STAGE/README.txt"
cp -a "$ROOT/Next/THIRD-PARTY-NOTICES.txt" "$STAGE/"

( cd "$(dirname "$STAGE")" && zip -r -q "$DIST/$NAME.zip" "ioBroker Studio" )
rm -rf "$(dirname "$STAGE")"

echo "==> $DIST/$NAME.zip"
sha256sum "$DIST/$NAME.zip"
