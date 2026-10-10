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
# picture would, as <example>.txt, followed by what it logged itself, as Bevy's own print with info!,
# the lines of the log under the target C# logs with, without their colors and times.
set -euo pipefail
cd "$(dirname "$0")/.."

program=$(ls -t BevyCSharp.Examples/bin/*/net10.0/BevyCSharp.Examples | head -1)
if "$program" --printing | grep -qx "$1"; then
    out="${2:-.github/assets/examples/$1.webp}"
    mkdir -p "$(dirname "$out")"
    text="${out%.webp}.txt"
    "$program" "$1" --drive > "$text" 2> "$text.log"
    sed -E 's/\x1b\[[0-9;]*m//g' "$text.log" | grep -E '^[^ ]+Z +[A-Z]+ csharp: ' | sed -E 's/^[^ ]+Z +//' >> "$text" || true
    rm -f "$text.log"
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

# One that saves its own picture and stops is run as it is, and the picture it saved is its capture.
if [ "$example" = headless_renderer ]; then
    saved="$(dirname "$program")/test_images/000.png"
    rm -f "$saved"
    "$program" "$example" --offscreen >/dev/null 2>&1
    { [ -n "$magick" ] && "$magick" "$saved" -quality 85 -strip "$out" 2>/dev/null; } || cwebp -quiet -q 85 "$saved" -o "$out"
    exit 0
fi

./bcs stop >/dev/null 2>&1 || true
trap './bcs stop >/dev/null 2>&1 || true' EXIT

# Every example is drawn at Bevy's window, or at the size it asks for itself, and kept at the size
# drawn, so the captures are one size and a label reads as Bevy draws it. A 2D or interface example
# is flat, known by its folder or, outside those, by its 2D camera.
flat=false
[ -f "BevyCSharp.Examples/2d/$example.cs" ] || [ -f "BevyCSharp.Examples/ui/$example.cs" ] && flat=true
grep -qs "SpawnCamera2d" BevyCSharp.Examples/*/"$example.cs" && flat=true
# One that asks for an argument to run at all is given it, by name.
arguments=()
case "$example" in
  # A shape of hierarchy to build, which draws nothing, as Bevy's draws nothing.
  transform_hierarchy) arguments=(humanoids_mixed) ;;
  # Waves thrown at once, where Bevy's waits for the left button to throw any.
  bevymark|bevymark_3d) arguments=(--benchmark --waves 20 --per-wave 1000) ;;
  *) ;;
esac
./bcs open --example "$example" --offscreen --quiet -- --size 1280x720 "${arguments[@]}"

# Examples that show something only once they are given input are driven here, by name.
case "$example" in
  # A level, which the screen loads once 1 is pressed.
  loading_screen) ./bcs command input.hold Digit1 2 --quiet ;;
  # A pad pretended, as one in the hands, its left stick pushed up and to the right, its right
  # stick down, and its right trigger far enough down to count as held.
  gamepad_viewer)
    ./bcs command input.axis 0 LeftX 0.6 --quiet
    ./bcs command input.axis 0 LeftY 0.4 --quiet
    ./bcs command input.axis 0 RightY -0.7 --quiet
    ./bcs command input.axis 0 RightTrigger 0.8 --quiet ;;
  # A hundred points sampled inside the cube and a hundred on its surface, which D scatters and M
  # turns from the one to the other.
  random_sampling)
    ./bcs command input.key D --quiet
    ./bcs command frames.wait 2 --quiet
    ./bcs command input.key M --quiet
    ./bcs command frames.wait 2 --quiet
    ./bcs command input.key D --quiet ;;
  *) ;;
esac

# Long enough for pipelines to compile and a scene that moves to be under way. One where things
# fall or fade in is given longer by name.
frames=120
case "$example" in
  # A cake that first appears after five seconds.
  alien_cake_addict) frames=420 ;;
  # A contributor brought forward every three seconds.
  contributors) frames=240 ;;
  # Exposure that adapts as an eye does, which takes its time to settle.
  auto_exposure) frames=900 ;;
  # Decals stamped one a second, which take some seconds to be more than one.
  clustered_decal_maps) frames=600 ;;
  # Lighting that gathers over frames, and a robot that has walked out from behind the console.
  solari) frames=400 ;;
  *) ;;
esac
./bcs command frames.wait "$frames" --quiet --timeout 600
# And until every pipeline asked for has compiled, which no count of frames promises, since what is
# drawn with one still compiling is left out of the picture.
./bcs command pipelines.wait --quiet --timeout 120

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
