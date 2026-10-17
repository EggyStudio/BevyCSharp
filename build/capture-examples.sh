#!/usr/bin/env bash
# Captures every example written in BevyCSharp.Examples, one after another, and fails where one
# does not start or comes out a single flat color, as an example that drew nothing does. The
# package workflow runs it on every package, and it is how the captures in
# .github/assets/examples are taken again after a change that touches many examples.
#
#   build/capture-examples.sh [folder] [options for dotnet build]
#
# The pictures go to .github/assets/examples unless another folder is given. The examples are built
# here with the options given, as -p:BevyCSharpSkipNativeFreshnessCheck=true where the bridge was
# built elsewhere.
set -uo pipefail
cd "$(dirname "$0")/.."

into="${1:-.github/assets/examples}"
[ $# -gt 0 ] && shift
mkdir -p "$into"

# Bevy's files first and the build after them, since the build copies beside the program what
# bevy-assets holds when it runs, and compiles in the picture embedded_asset carries only where its
# file is there. The pack run of d2a20b6a built the examples in a step before this fetched, and the
# 58 examples that load a file of Bevy's were captured blank.
build/fetch-bevy-assets.sh || exit 1
dotnet build BevyCSharp.Examples "$@" || exit 1

failed=()
program=$(ls -t BevyCSharp.Examples/bin/*/net10.0/BevyCSharp.Examples | head -1)
printing=$("$program" --printing)

# Examples that show and print nothing by design, Bevy's two empty applications and its two that
# play music in a window with no camera, those that wait for a file dropped, the mouse or a touch,
# which a capture has none of, and monitor_info, which opens a window on each monitor and finds none
# in a run with no display, so their capture is not held to saying something.
empty="audio drag_and_drop empty empty_defaults monitor_info mouse_grab mouse_input soundtrack touch_input touch_input_events"

# Examples whose picture is a color or two by their design, as Bevy's is, which the count of colors
# below would take for one that drew nothing: a clear color alone (clear_color,
# persisting_window_settings, window_settings), one square of one color on another (mesh2d,
# fixed_node), and nothing drawn, gpu_readback spawning no camera and transform_hierarchy building a
# hierarchy of entities with nothing on them to draw. Each is still compared with its checked-in
# picture.
flat="clear_color fixed_node gpu_readback mesh2d persisting_window_settings transform_hierarchy window_settings"

# What an example that did not start said in the log bcs open keeps, without the colors Bevy writes
# or a stack's frames, which push the exception's own lines out of any tail. Its first lines at an
# error say why, and its last lines follow them.
said() {
    local clean
    clean=$(sed -E 's/\x1b\[[0-9;]*m//g' "$1" 2>/dev/null | grep -v '^ *at ' | cut -c1-240)
    grep -m 4 -E 'Unhandled exception|--->|panicked at|ERROR' <<< "$clean"
    tail -n 10 <<< "$clean"
}

# Three examples in a row that do not start end the run, since a program that cannot start twice
# more will not start at all, and bcs waits a minute and a half for each before it says so. On the
# pack run of 292eead2 no example found the bridge, and the step waited through them until the job's
# hour ran out, its page holding the cancellation alone. The first one's log is read before the next
# opens and writes over it.
unstarted=()
for example in $("$program" --list); do
    build/capture-example.sh "$example" "$into/$example.webp"
    code=$?
    if [ "$code" -eq 3 ]; then
        [ ${#unstarted[@]} -eq 0 ] && first=$(said build/sessions/BevyCSharp.Examples.log)
        unstarted+=("$example")
        failed+=("$example (did not start)")
        if [ ${#unstarted[@]} -eq 3 ]; then
            echo "${unstarted[*]} did not start, one after another, so no other example is tried. What ${unstarted[0]} said:" >&2
            printf '%s\n' "$first" >&2
            if [ "${GITHUB_ACTIONS:-}" = true ]; then
                lines=${first//%/%25}
                printf '::error title=Examples do not start::%s did not start, one after another, so no other example was tried. What %s said:%%0A%s\n' \
                    "${unstarted[*]}" "${unstarted[0]}" "${lines//$'\n'/%0A}"
            fi
            exit 1
        fi
        continue
    fi
    unstarted=()
    if [ "$code" -ne 0 ]; then
        failed+=("$example (could not be captured)")
        continue
    fi

    if [[ " $empty " == *" $example "* || " $flat " == *" $example "* ]]; then
        continue
    fi

    # One that prints is captured as its output, which has to say something.
    if grep -qx "$example" <<< "$printing"; then
        [ -s "$into/$example.txt" ] || failed+=("$example (printed nothing)")
        continue
    fi

    # How many colors the picture holds, which a picture of one flat color has one of. A picture of
    # thin lines on a plain ground has few, and a picture of nothing has one or two. Decoded to PPM
    # by ImageMagick, or by libwebp's dwebp where ImageMagick was built without WebP.
    magick=$(command -v magick || command -v convert || true)
    colors=$({ { [ -n "$magick" ] && "$magick" "$into/$example.webp" ppm:- 2>/dev/null; } \
            || dwebp -quiet "$into/$example.webp" -ppm -o -; } | python3 -c '
import sys
data = sys.stdin.buffer.read()
fields, at = [], 0
while len(fields) < 4:
    while data[at:at + 1].isspace(): at += 1
    if data[at:at + 1] == b"#":
        at = data.index(b"\n", at)
        continue
    end = at
    while not data[end:end + 1].isspace(): end += 1
    fields.append(data[at:end]); at = end
pixels = data[at + 1:]
print(len({pixels[i:i + 3] for i in range(0, len(pixels), 3)}))' 2>/dev/null || echo 0)
    if [ "$colors" -le 2 ]; then
        failed+=("$example (blank)")
    fi
done

if [ ${#failed[@]} -gt 0 ]; then
    printf 'Examples that failed:\n' >&2
    printf '  %s\n' "${failed[@]}" >&2
    # Every one in the workflow's error, which a reader not signed in sees, where the error
    # build/step.py would give the step holds its last few lines alone.
    if [ "${GITHUB_ACTIONS:-}" = true ]; then
        printf '::error title=%s examples failed::%s\n' "${#failed[@]}" "$(printf '%s%%0A' "${failed[@]}")"
    fi
    exit 1
fi
echo "captured every example into $into"
