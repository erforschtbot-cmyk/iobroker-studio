#!/usr/bin/env bash
set -euo pipefail

SRC="/home/openclaw/.openclaw/workspace-allgemein/media/inbound/openclaw-staged-eb307a3a-d282-4485-8c2e-e0b3c0e2c352/input-image---2e76b69d-d3fd-46ad-8e3d-0a3e987791a8.png"
OUT="/tmp/banner.png"
TMP=$(mktemp -d)

TEAL="#18A999"
BG="#0E1622"
WHITE="#FFFFFF"
MUTED="#B7C6D4"
DIM="#6E8195"

FB="/usr/share/fonts/noto/NotoSans-Bold.ttf"
FM="/usr/share/fonts/noto/NotoSans-Medium.ttf"
FR="/usr/share/fonts/noto/NotoSans-Regular.ttf"

# --- 1. Screenshot: app window without OS titlebar, rounded, bordered ---
# App-Screenshot aus drei sauberen Bändern (Kopfleiste + Kartenzeile 2 + Protokoll)
magick "$SRC" -crop 1463x112+0+36 +repage "$TMP/b1.png"
magick "$SRC" -crop 1463x274+0+433 +repage "$TMP/b2.png"
magick "$SRC" -crop 1463x285+0+722 +repage "$TMP/b3.png"
magick "$TMP/b1.png" "$TMP/b2.png" "$TMP/b3.png" -background "#0E1622" -gravity north -append "$TMP/shotfull.png"
magick "$TMP/shotfull.png" -resize 760x "$TMP/shot.png"
SW=$(magick identify -format %w "$TMP/shot.png")
SH=$(magick identify -format %h "$TMP/shot.png")

magick -size ${SW}x${SH} xc:none -fill white \
  -draw "roundrectangle 0,0,$((SW-1)),$((SH-1)),18,18" "$TMP/mask.png"

magick "$TMP/shot.png" "$TMP/mask.png" -alpha off -compose CopyOpacity -composite "$TMP/shot_r.png"

# border
magick -size ${SW}x${SH} xc:none -fill none -stroke "#ffffff22" -strokewidth 1 \
  -draw "roundrectangle 0.5,0.5,$((SW-1)).5,$((SH-1)).5,18,18" "$TMP/border.png"

# shadow
magick -size $((SW+80))x$((SH+80)) xc:none -fill "#000000" \
  -draw "roundrectangle 40,40,$((SW+40)),$((SH+40)),18,18" \
  -blur 0x22 -channel A -evaluate multiply 0.55 +channel "$TMP/shadow.png"

# --- 2. Background with teal glow ---
magick -size 1280x640 xc:"$BG" "$TMP/bg0.png"
magick -size 1000x1000 radial-gradient:"#18A99966-#00000000" -resize 1000x1000 "$TMP/glow.png"
magick "$TMP/bg0.png" "$TMP/glow.png" -geometry +660-330 -compose screen -composite "$TMP/bg.png"

# subtle grid
magick -size 1280x640 xc:none -stroke "#ffffff08" -strokewidth 1 \
  -draw "$(for x in $(seq 0 40 1280); do echo -n "line $x,0 $x,640 "; done)" \
  -draw "$(for y in $(seq 0 40 640); do echo -n "line 0,$y 1280,$y "; done)" "$TMP/grid.png"
magick "$TMP/bg.png" "$TMP/grid.png" -compose over -composite "$TMP/bg2.png"

# --- 3. Assemble ---
SHOT_X=492
SHOT_Y=$(( (640 - SH) / 2 ))
magick "$TMP/bg2.png" \
  "$TMP/shadow.png" -geometry +$((SHOT_X-40))+$((SHOT_Y-40)) -composite \
  "$TMP/shot_r.png" -geometry +$SHOT_X+$SHOT_Y -composite \
  "$TMP/border.png" -geometry +$SHOT_X+$SHOT_Y -composite \
  "$TMP/canvas.png"

# --- 4. Logo tile from screenshot ---
magick "$SRC" -crop 60x60+34+66 +repage -resize 56x56 "$TMP/logo_raw.png"
magick -size 56x56 xc:none -fill white -draw "roundrectangle 0,0,55,55,14,14" "$TMP/lmask.png"
magick "$TMP/logo_raw.png" "$TMP/lmask.png" -alpha off -compose CopyOpacity -composite "$TMP/logo.png"

# --- 5. Texts ---
magick "$TMP/canvas.png" \
  "$TMP/logo.png" -geometry +72+72 -composite \
  -font "$FB" -pointsize 46 -fill "$WHITE" -annotate +142+118 "ioBroker Studio" \
  -font "$FM" -pointsize 14 -kerning 2.2 -fill "$TEAL" -annotate +145+146 "SYSTEM MANAGEMENT CONSOLE" \
  -stroke "#ffffff14" -strokewidth 1 -draw "line 72,176 470,176" -stroke none \
  -font "$FR" -pointsize 21 -kerning 0 -fill "$MUTED" \
  -annotate +72+226 "Wartung, Backup und Reparatur" \
  -annotate +72+258 "entfernter ioBroker-Systeme über SSH." \
  -font "$FR" -pointsize 17 -fill "$DIM" \
  -font "$FM" -pointsize 15 -fill "#7D93A8" -annotate +72+470 "github.com/erforschtbot-cmyk/iobroker-studio" \
  -font "$FR" -pointsize 17 -fill "$DIM" \
  -annotate +72+574 "v1.2.39 · Windows x64 · frei herunterladbar" \
  "$TMP/step.png"

# --- 6. Chips ---
draw_chips() {
  local y=$1; shift
  local x=72
  local args=()
  for label in "$@"; do
    w=$(magick -font "$FM" -pointsize 16 label:"$label" -format %w info:)
    cw=$(( w + 24 ))
    args+=( -fill "#16232F" -stroke "#18A99955" -strokewidth 1
            -draw "roundrectangle $x,$y,$((x+cw)),$((y+34)),14,14" )
    args+=( -stroke none -fill "#D5E2EC" -font "$FM" -pointsize 16
            -annotate +$((x+12))+$((y+22)) "$label" )
    x=$(( x + cw + 10 ))
  done
  magick "$TMP/step.png" "${args[@]}" "$TMP/step.png"
}

draw_chips 322 "Backup & Restore" "Checkpoints" "Reparatur"
draw_chips 374 "Systemdiagnose" "Terminal" "Netzwerk"

cp "$TMP/step.png" "$OUT"
magick identify "$OUT"
