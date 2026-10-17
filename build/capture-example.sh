#!/usr/bin/env bash
# Captures one of the examples as EXAMPLES.md shows it: opened offscreen at Bevy's window of 1280
# by 720, driven where it waits for input, run long enough to settle, captured and closed. The pack
# run captures every example this way, and a written or changed example's capture in
# .github/assets/examples is taken with it.
#
#   dotnet build BevyCSharp.Examples
#   build/capture-example.sh <example> [webp]
#
# The picture goes to .github/assets/examples/<example>.webp unless another path is given, written
# by ImageMagick, or by libwebp's cwebp where ImageMagick was built without WebP. An
# example with nothing to draw is run headless instead, and what it prints goes beside where the
# picture would, as <example>.txt, followed by what it logged itself, as Bevy's own print with info!,
# the lines of the log under the target C# logs with, and those of Bevy's log of its diagnostics,
# which an example of the diagnostics store prints, without their colors and times.
set -eEuo pipefail
cd "$(dirname "$0")/.."
trap 'code=$?; echo "capture-example.sh: $BASH_COMMAND ended with $code on line $LINENO" >&2' ERR

# Run as the workflow runs them, where Bevy's examples that roll dice seed them, as
# alien_cake_addict does, and contributors takes its fixed list of names rather than this
# checkout's history, so a capture taken here and one taken there are of the same scene.
export GITHUB_ACTIONS=true

program=$(ls -t BevyCSharp.Examples/bin/*/net10.0/BevyCSharp.Examples | head -1)

# Bevy's files fetched where they are missing, and the examples built again where the program was
# built before one of them was there, since only a build copies them beside it. An example captured
# without the files it loads comes out blank, as 58 did on the pack run of d2a20b6a.
build/fetch-bevy-assets.sh
built=$(dirname "$program")
if comm -23 <(cd BevyCSharp.Examples/bevy-assets && find . -type f | sort) <(cd "$built/assets" && find . -type f | sort) | grep -q .; then
    dotnet build BevyCSharp.Examples -c "$(basename "$(dirname "$built")")" -v q --nologo
fi
if "$program" --printing | grep -qx "$1"; then
    out="${2:-.github/assets/examples/$1.webp}"
    mkdir -p "$(dirname "$out")"
    text="${out%.webp}.txt"
    # On the set clock a picture is taken on, so what it prints of time, and which of its fixed
    # steps falls between which frames, is the same on every run.
    BCS_FRAME_TIME=0.0166667 "$program" "$1" --drive > "$text" 2> "$text.log"
    # A message of several lines is kept whole, its lines after the first carrying no time.
    sed -E 's/\x1b\[[0-9;]*m//g' "$text.log" \
        | awk '/^[^ ]+Z +[A-Z]+ / { keep = ($0 ~ /^[^ ]+Z +[A-Z]+ (csharp|bevy_diagnostic): /) } keep' \
        | sed -E 's/^[^ ]+Z +//' >> "$text" || true
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
# Each frame a sixtieth of a second of the example's clock however long the device takes to draw it,
# so what moves by the clock stands at the same place in a capture on any device, which a capture
# compared with its checked-in picture asks. A device drawing on the CPU, as the workflow's does,
# compiles for seconds, and on the machine's clock an example turning its camera has gone half
# round by the frame its capture is taken at.
# One that does not start ends this with 3, which build/capture-examples.sh tells from one that
# started and could not be captured, and stops on when examples do not start one after another.
./bcs open --example "$example" --offscreen --quiet --frame-time 0.0166667 -- --size 1280x720 "${arguments[@]}" || exit 3

# Each command waits as long as ten minutes for its answer, where bcs waits thirty seconds, since a
# software device, as the workflow's runner has, draws some examples a frame every few seconds, and
# one that fails is named with its code under bcs's own words of why.
c() { ./bcs command "$@" --quiet --timeout 600 || { echo "capture-example.sh: ./bcs command $* ended with $? in $example" >&2; exit 1; }; }

# Examples that show something only once they are given input are driven here, by name.
case "$example" in
  # A level, which the screen loads once 1 is pressed.
  loading_screen) c input.hold Digit1 2 ;;
  # A pad pretended, as one in the hands, its left stick pushed up and to the right, its right
  # stick down, and its right trigger far enough down to count as held.
  gamepad_viewer)
    c input.axis 0 LeftX 0.6
    c input.axis 0 LeftY 0.4
    c input.axis 0 RightY -0.7
    c input.axis 0 RightTrigger 0.8 ;;
  # A hundred points sampled inside the cube and a hundred on its surface, which D scatters and M
  # turns from the one to the other.
  random_sampling)
    c input.key D
    c frames.wait 2
    c input.key M
    c frames.wait 2
    c input.key D ;;
  *) ;;
esac

# The frame the picture is taken in, counted from the app's first, late enough for a scene that
# moves to be under way and what it loads to have arrived at sixty frames a second. One where things
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
  # A bar that fills over two seconds and empties over two more, so empty at the second second and
  # half full at the third.
  ui_material) frames=180 ;;
  # An image that shrinks to nothing and grows again every two seconds, gone at the second second
  # and at half its size a quarter of a period later.
  dynamic_mip_generation) frames=150 ;;
  # Thousands of meshes, which a device drawing on the CPU takes seconds a frame over, taken as
  # soon as they stand, which they do by the thirtieth.
  multi_asset_sync|many_morph_targets) frames=30 ;;
  *) ;;
esac
# Taken in that frame, which the app holds the request for, rather than in whichever frame it has
# reached when a wait ends, which a fast device puts many frames later than a slow one. On the set
# clock every pipeline compiles in the frame that asks for it, so that frame is the same moment of
# the scene, drawn whole, on any device.
shot="${out%.webp}.shot.png"
./bcs shot "$shot" "$frames" --quiet --timeout 600 \
    || { echo "capture-example.sh: ./bcs shot of frame $frames ended with $? in $example" >&2; exit 1; }

# Every picture sixteen by nine, as the README's gallery shows each in a box of that shape, so a
# shot of another shape, as the square window Bevy's resizing test opens, is scaled until it fills
# 1280 by 720 and cut around its middle. An example whose cut would lose what it shows opens a
# window of that shape instead, as grid and scale_factor_override do.
if [ -n "$magick" ]; then
    read -r width height < <("$magick" "$shot" -format '%w %h\n' info:)
    if [ $((width * 9)) -ne $((height * 16)) ]; then
        "$magick" "$shot" -resize '1280x720^' -gravity center -extent 1280x720 "$shot"
    fi
fi

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
