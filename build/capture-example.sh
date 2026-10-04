#!/usr/bin/env bash
# Captures one of the examples as EXAMPLES.md shows it: opened offscreen at 800 by 450, driven
# where it waits for input, run long enough to settle, captured and closed. The package workflow
# captures every example this way, and a written or changed example's capture in
# .github/assets/examples is taken with it.
#
#   dotnet build BevyCSharp.Examples
#   build/capture-example.sh <example> [png]
#
# The picture goes to .github/assets/examples/<example>.png unless another path is given.
set -euo pipefail
cd "$(dirname "$0")/.."

example="$1"
out="${2:-.github/assets/examples/$example.png}"
mkdir -p "$(dirname "$out")"

./bcs stop >/dev/null 2>&1 || true
trap './bcs stop >/dev/null 2>&1 || true' EXIT

./bcs open --example "$example" --offscreen --quiet -- --size 800x450

# Examples that show something only once they are given input are driven here, by name.
case "$example" in
  *) ;;
esac

# Long enough for pipelines to compile and a scene that moves to be under way. One where things
# fall or fade in is given longer by name.
frames=120
case "$example" in
  *) ;;
esac
./bcs command frames.wait "$frames" --quiet --timeout 600

./bcs shot "$out" --quiet --timeout 120

# Down to a palette of 256 colors where ImageMagick is there to do it, which keeps a capture about
# a fifth of the size with no difference a reader of EXAMPLES.md can see, since the repository
# carries one for every example.
if command -v magick >/dev/null 2>&1; then
    magick "$out" -colors 256 -define png:compression-level=9 -strip "$out"
elif command -v convert >/dev/null 2>&1; then
    convert "$out" -colors 256 -define png:compression-level=9 -strip "$out"
fi
