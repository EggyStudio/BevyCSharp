#!/usr/bin/env bash
# Captures one of the examples as EXAMPLES.md shows it: opened offscreen at Bevy's window of 1280
# by 720, driven where it waits for input, run long enough to settle, captured and closed. The package workflow
# captures every example this way, and a written or changed example's capture in
# .github/assets/examples is taken with it.
#
#   dotnet build BevyCSharp.Examples
#   build/capture-example.sh <example> [webp]
#
# The picture goes to .github/assets/examples/<example>.webp unless another path is given, written
# by ImageMagick, or by libwebp's cwebp where ImageMagick was built without WebP. An
# example with nothing to draw is run headless instead, and what it prints goes beside where the
# picture would, as <example>.txt.
set -euo pipefail
cd "$(dirname "$0")/.."

program=$(ls -t BevyCSharp.Examples/bin/*/net10.0/BevyCSharp.Examples | head -1)
if "$program" --printing | grep -qx "$1"; then
    out="${2:-.github/assets/examples/$1.webp}"
    mkdir -p "$(dirname "$out")"
    "$program" "$1" --drive > "${out%.webp}.txt"
    exit 0
fi

example="$1"
out="${2:-.github/assets/examples/$example.webp}"
mkdir -p "$(dirname "$out")"
magick=$(command -v magick || command -v convert || true)
if [ -z "$magick" ] && ! command -v cwebp >/dev/null 2>&1; then
    echo "capture-example.sh: ImageMagick or cwebp is needed to write the WebP" >&2
    exit 1
fi

./bcs stop >/dev/null 2>&1 || true
trap './bcs stop >/dev/null 2>&1 || true' EXIT

# Every example is drawn at Bevy's window, or at the size it asks for itself, and kept at the size
# drawn, so the captures are one size and a label reads as Bevy draws it. A 2D or interface example
# is flat, known by its folder or, outside those, by its 2D camera.
flat=false
[ -f "BevyCSharp.Examples/2d/$example.cs" ] || [ -f "BevyCSharp.Examples/ui/$example.cs" ] && flat=true
grep -qs "SpawnCamera2d" BevyCSharp.Examples/*/"$example.cs" && flat=true
./bcs open --example "$example" --offscreen --quiet -- --size 1280x720

# Examples that show something only once they are given input are driven here, by name.
case "$example" in
  *) ;;
esac

# Long enough for pipelines to compile and a scene that moves to be under way. One where things
# fall or fade in is given longer by name.
frames=120
case "$example" in
  # Exposure that adapts as an eye does, which takes its time to settle.
  auto_exposure) frames=900 ;;
  # Decals stamped one a second, which take some seconds to be more than one.
  clustered_decal_maps) frames=600 ;;
  # Lighting that gathers over frames, and a robot that has walked out from behind the console.
  solari) frames=400 ;;
  *) ;;
esac
./bcs command frames.wait "$frames" --quiet --timeout 600

shot="${out%.webp}.shot.png"
./bcs shot "$shot" --quiet --timeout 120

# A flat capture is lossless WebP, which is smaller than a palette PNG and keeps text sharp. A 3D
# one is WebP at quality 85, a fraction of full color PNG with no banding in a sky, fog or bloom,
# where a palette rings them.
if $flat; then
    { [ -n "$magick" ] && "$magick" "$shot" -define webp:lossless=true -strip "$out" 2>/dev/null; } \
        || cwebp -quiet -lossless "$shot" -o "$out"
else
    { [ -n "$magick" ] && "$magick" "$shot" -quality 85 -strip "$out" 2>/dev/null; } \
        || cwebp -quiet -q 85 "$shot" -o "$out"
fi
rm -f "$shot"
