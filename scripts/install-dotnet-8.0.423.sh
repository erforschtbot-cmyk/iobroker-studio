#!/usr/bin/env bash
# Installiert das gepinnte .NET SDK 8.0.423 nach ~/.dotnet-sdk.
# Quelle: offizielles dotnet-install.sh von https://dot.net/v1/dotnet-install.sh
set -euo pipefail

VERSION="8.0.423"
INSTALL_DIR="${DOTNET_INSTALL_DIR:-$HOME/.dotnet-sdk}"
SCRIPT="/tmp/dotnet-install.sh"

if [ -x "$INSTALL_DIR/dotnet" ] && "$INSTALL_DIR/dotnet" --version 2>/dev/null | grep -qx "$VERSION"; then
    echo ".NET SDK $VERSION ist bereits unter $INSTALL_DIR vorhanden."
    exit 0
fi

echo "Lade dotnet-install.sh …"
curl -fsSL -o "$SCRIPT" https://dot.net/v1/dotnet-install.sh
chmod +x "$SCRIPT"

echo "Installiere .NET SDK $VERSION nach $INSTALL_DIR …"
"$SCRIPT" --version "$VERSION" --install-dir "$INSTALL_DIR"

echo
echo "Fertig. Umgebung setzen:"
echo "  export DOTNET_ROOT=\"$INSTALL_DIR\""
echo "  export PATH=\"\$DOTNET_ROOT:\$PATH\""
"$INSTALL_DIR/dotnet" --version
