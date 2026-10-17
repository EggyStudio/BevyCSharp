#!/usr/bin/env bash
# Runs the feature test through Intel Sponza, from outside its door and from inside its hall, and
# keeps the captures of its reference views with their frame times in build/scenes, beside the rest
# of the course run's captures and the program's log.
#
#   build/scenes.sh
#
# Run by hand on a machine with a GPU rather than in a workflow, since the pack is hundreds of
# megabytes and a runner's lavapipe has neither meshlets nor ray-traced lighting. The bridge is
# built with both, as the testers' zips carry it, so a GPU that has them draws the scene with them,
# and slangc is fetched so the feature test's shaders compile. The pack is fetched once, where
# `./bcs scenes` keeps packs, and the course run loads it where it finds it fetched
# (build/drive-feature-test.sh).
set -eEuo pipefail
cd "$(dirname "$0")/.."

trap 'code=$?; echo "scenes.sh: $BASH_COMMAND ended with $code on line $LINENO" >&2' ERR

build/build-native.sh --render --meshlet --solari
build/fetch-slang.sh
./bcs scenes fetch intel-sponza

# Built with warnings as errors, as the workflow builds every project, which also copies the new
# bridge beside the program. The folder is given whole, since the course script writes the program's
# log into it from the program's own folder.
dotnet build BevyCSharp.FeatureTest -c Release -warnaserror
mkdir -p build/scenes
SHOTS="$PWD/build/scenes" build/drive-feature-test.sh BevyCSharp.FeatureTest/bin/Release/net10.0
cat build/scenes/frame-times.txt
