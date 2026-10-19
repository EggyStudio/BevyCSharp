#!/usr/bin/env bash
# Captures monitor_info on four monitors of a compositor that has no screen of its own, run in a
# container, so the picture is the same on any machine with podman or docker and no window opens
# on the machine's own screens. An offscreen run reports no monitors and opens nothing, which is
# why monitor_info is captured this way and the rest of the examples by capture-example.sh, which
# hands monitor_info here where a container runtime is found.
#
#   dotnet build BevyCSharp.Examples
#   build/capture-monitor-info.sh [webp]
#
# The picture goes to .github/assets/examples/monitor_info.webp unless another path is given, the
# grid of the four monitors scaled to Bevy's window of 1280 by 720.
#
# The container is Ubuntu 24.04 with .NET's SDK, given sway on four headless outputs laid out by
# build/capture-monitor-info.sway, Mesa's lavapipe to draw with and grim to take the picture. The
# example runs through sway's Xwayland, whose RandR gives each output a CRTC, which are the
# monitors winit lists on X11 and where Bevy's exclusive fullscreen fills one, where on Wayland
# winit ignores exclusive fullscreen and the windows stay tiled. The examples are run from where
# they were built, which the container reads from the checkout.
set -euo pipefail
cd "$(dirname "$0")/.."

out="${1:-.github/assets/examples/monitor_info.webp}"
runtime=$(command -v podman || command -v docker || true)
if [ -z "$runtime" ]; then
    echo "capture-monitor-info.sh: podman or docker is needed to run the compositor in" >&2
    exit 2
fi
program=$(ls -t BevyCSharp.Examples/bin/*/net10.0/BevyCSharp.Examples 2>/dev/null | head -1 || true)
if [ -z "$program" ]; then
    echo "capture-monitor-info.sh: build the examples first, with dotnet build BevyCSharp.Examples" >&2
    exit 2
fi
magick=$(command -v magick || command -v convert || true)
if [ -z "$magick" ] && ! command -v cwebp >/dev/null 2>&1; then
    echo "capture-monitor-info.sh: ImageMagick or cwebp is needed to write the WebP" >&2
    exit 1
fi

mkdir -p build/sessions "$(dirname "$out")"
shot=build/sessions/monitor_info.png
log=build/sessions/monitor_info-container.log
rm -f "$shot"

# Inside the container, the packages, then sway, then the example once Xwayland is up, and the
# picture once the four windows fill their outputs and have drawn.
"$runtime" run --rm -i -v "$PWD:/src:z" -w /src mcr.microsoft.com/dotnet/sdk:10.0-noble \
    bash -s "$(dirname "$program")" "$shot" > "$log" 2>&1 <<'INSIDE' || true
set -u
built="$1"
shot="$2"
apt-get update -qq > /dev/null
apt-get install -y -qq --no-install-recommends sway grim xwayland mesa-vulkan-drivers libvulkan1 \
    libudev1 libasound2t64 libxkbcommon0 libxkbcommon-x11-0 libx11-6 libx11-xcb1 libxcursor1 \
    libxrandr2 libxi6 > /dev/null 2>&1 || { echo "the packages did not install"; exit 1; }

export XDG_RUNTIME_DIR=/tmp/xdg
mkdir -p -m 700 "$XDG_RUNTIME_DIR"

# Sway refuses to start where it finds Nvidia's driver in the kernel the container shares, though
# it draws with pixman here and touches no GPU.
WLR_BACKENDS=headless WLR_RENDERER=pixman WLR_HEADLESS_OUTPUTS=4 WLR_LIBINPUT_NO_DEVICES=1 \
    sway --unsupported-gpu -c build/capture-monitor-info.sway > /tmp/sway.log 2>&1 &
for _ in $(seq 1 100); do [ -e /tmp/.X11-unix/X0 ] && break; sleep 0.2; done
[ -e /tmp/.X11-unix/X0 ] || { echo "sway's Xwayland did not start"; cat /tmp/sway.log; exit 1; }

DISPLAY=:0 dotnet "$built/BevyCSharp.Examples.dll" monitor_info > /tmp/app.log 2>&1 &

# Four windows fullscreen, then a few seconds for lavapipe to draw each. Sway's socket and its
# Wayland display are named as it starts, and swaymsg and grim each reach sway through one.
export SWAYSOCK=$(ls "$XDG_RUNTIME_DIR"/sway-ipc.*.sock 2>/dev/null | head -1)
export WAYLAND_DISPLAY=$(cd "$XDG_RUNTIME_DIR" && ls wayland-* 2>/dev/null | grep -v lock | head -1)
for _ in $(seq 1 240); do
    [ "$(swaymsg -t get_tree 2>/dev/null | grep -c '"fullscreen_mode": 1')" -ge 4 ] && break
    sleep 0.5
done
sleep 5
grim "$shot" || { echo "grim took no picture"; cat /tmp/app.log; exit 1; }
echo "taken"
INSIDE

if [ ! -s "$shot" ]; then
    echo "capture-monitor-info.sh: no picture was taken. What the container said:" >&2
    tail -n 30 "$log" >&2
    exit 1
fi

# The grid scaled to Bevy's window, lossless, as a flat capture is.
if [ -n "$magick" ]; then
    "$magick" "$shot" -resize 1280x720 -define webp:lossless=true -strip "$out"
else
    cwebp -quiet -lossless -resize 1280 720 "$shot" -o "$out"
fi
rm -f "$shot"
