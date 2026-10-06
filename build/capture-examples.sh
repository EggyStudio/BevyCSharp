#!/usr/bin/env bash
# Captures every example written in BevyCSharp.Examples, one after another, and fails where one
# does not start or comes out a single flat color, as an example that drew nothing does. The
# package workflow runs it on every package, and it is how the captures in
# .github/assets/examples are taken again after a change that touches many examples.
#
#   dotnet build BevyCSharp.Examples
#   build/capture-examples.sh [folder]
#
# The pictures go to .github/assets/examples unless another folder is given.
set -uo pipefail
cd "$(dirname "$0")/.."

into="${1:-.github/assets/examples}"
mkdir -p "$into"
build/fetch-bevy-assets.sh

failed=()
program=$(ls -t BevyCSharp.Examples/bin/*/net10.0/BevyCSharp.Examples | head -1)
printing=$("$program" --printing)

# Examples that show and print nothing by design, Bevy's two empty applications and its two that
# play music in a window with no camera, and those that wait for a file dropped, the mouse or a
# touch, which a capture has none of, so their capture is not held to saying something.
empty="audio drag_and_drop empty empty_defaults mouse_grab mouse_input soundtrack touch_input touch_input_events"

for example in $("$program" --list); do
    if ! build/capture-example.sh "$example" "$into/$example.webp"; then
        failed+=("$example (did not start or could not be captured)")
        continue
    fi

    if [[ " $empty " == *" $example "* ]]; then
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
