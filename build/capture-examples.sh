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
for example in $(BevyCSharp.Examples/bin/*/net10.0/BevyCSharp.Examples --list); do
    if ! build/capture-example.sh "$example" "$into/$example.png"; then
        failed+=("$example (did not start or could not be captured)")
        continue
    fi

    # The spread of the picture's bytes, which a picture of one flat color has none of. A PNG
    # of eight-bit channels with no interlacing, as a capture is, read without a library.
    if ! python3 - "$into/$example.png" <<'PY'
import struct, sys, zlib
data = open(sys.argv[1], "rb").read()
width, height, depth, kind = struct.unpack(">IIBB", data[16:26])
chunks, at = b"", 8
while at < len(data):
    length, name = struct.unpack(">I4s", data[at:at + 8])
    if name == b"IDAT":
        chunks += data[at + 8:at + 8 + length]
    at += 12 + length
raw = zlib.decompress(chunks)
# Palette indices for a capture brought down to 256 colors, otherwise gray, RGB or RGBA.
channels = {0: 1, 2: 3, 3: 1, 4: 2, 6: 4}[kind]
stride = width * channels
# Every pixel's first channel, the filter byte of each row left out. A picture of thin lines on a
# plain ground has few values, and a picture of nothing has one.
values = set()
for y in range(height):
    values.update(raw[y * (stride + 1) + 1:(y + 1) * (stride + 1)][::channels])
sys.exit(0 if len(values) > 2 else 1)
PY
    then
        failed+=("$example (blank)")
    fi
done

if [ ${#failed[@]} -gt 0 ]; then
    printf 'Examples that failed:\n' >&2
    printf '  %s\n' "${failed[@]}" >&2
    exit 1
fi
echo "captured every example into $into"
