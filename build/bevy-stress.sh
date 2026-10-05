#!/usr/bin/env bash
# Runs one of Bevy's own stress tests as an offscreen run of the bridge runs, and prints its average
# frame, to be read beside the same test written in C# (.github/PERFORMANCE.md).
#
#   build/bevy-stress.sh many_sprites [its own arguments]
#   BEVY_STRESS_SIZE=1280x720 build/bevy-stress.sh transform_hierarchy humanoids_active
#
# The test's source is fetched at the Bevy version native/Cargo.lock pins, once, into
# native/stress/examples, with its DefaultPlugins swapped for stress::default_plugins(), which
# leaves the window out and draws into the image an offscreen run of the bridge draws into. It is
# built in release beside native/stress, on the bridge's render profile, and run with the examples'
# built assets, which hold what Bevy's tests load. The line it prints last is frame_ms, the average
# of 240 frames after 300 let settle, as games/Stress/measure.sh asks frame.profile for.
set -euo pipefail
cd "$(dirname "$0")/.."

name=$1
shift

version=$(awk '/^name = "bevy"$/ { getline; gsub(/version = |"/, ""); print; exit }' native/Cargo.lock)
[ -n "$version" ] || { echo "native/Cargo.lock names no bevy version" >&2; exit 1; }

examples=native/stress/examples
mkdir -p "$examples"
base="https://raw.githubusercontent.com/bevyengine/bevy/v$version/examples/stress_tests"
if [ ! -f "$examples/$name.rs" ]; then
    curl -sfL "$base/$name.rs" -o "$examples/$name.rs.part"
    sed 's/\bDefaultPlugins\b/stress::default_plugins()/' "$examples/$name.rs.part" > "$examples/$name.rs"
    rm "$examples/$name.rs.part"
fi
[ -f "$examples/warning_string.txt" ] || curl -sfL "$base/warning_string.txt" -o "$examples/warning_string.txt"

CARGO_TARGET_DIR=native/target cargo build --release --quiet --manifest-path native/stress/Cargo.toml --example "$name"

assets=$(dirname "$(ls -t BevyCSharp.Examples/bin/*/net10.0/BevyCSharp.Examples | head -1)")
BEVY_ASSET_ROOT="$PWD/$assets" exec native/target/release/examples/"$name" "$@"
