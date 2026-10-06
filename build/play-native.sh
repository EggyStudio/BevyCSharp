#!/usr/bin/env bash
# Publishes Courtyard from the package as native code, as a player gets it, and plays it from its
# menu to its win with no display, failing where it does not (NORM.md, N 2.5). A native publish
# keeps only what the compiler sees used, so a type the library reaches by reflection, where a
# generator could have registered it, fails here first.
#
#   build/play-native.sh <version> [options for dotnet publish]
#
# The package is the version named, packed into build/package beforehand, as the workflow packs one
# for its run. The game is published for the machine it runs on, and the options are passed on, as
# -p:CppCompilerAndLinker=gcc on a machine with no clang. Captures of each step are left in the
# folder named by SHOTS, or in a temporary one.
set -euo pipefail

version="$1"
shift
cd "$(dirname "$0")/.."

case "$(uname -s)-$(uname -m)" in
  Linux-x86_64) rid=linux-x64 ;;
  Linux-aarch64) rid=linux-arm64 ;;
  Darwin-arm64) rid=osx-arm64 ;;
  Darwin-x86_64) rid=osx-x64 ;;
  *) rid=win-x64 ;;
esac

out="$(mktemp -d)/Courtyard"
trap 'rm -rf "$(dirname "$out")"' EXIT
dotnet publish games/Courtyard -c Release -r "$rid" -p:PublishAot=true -p:BevyCSharpVersion="$version" -o "$out" "$@"

# The program the publish wrote, native code with no assembly beside it to run in its place.
if [ -f "$out/Courtyard.dll" ]; then
  echo "the publish left Courtyard.dll beside the program, so it is not native code" >&2
  exit 1
fi

games/Courtyard/play.sh "$out"
echo "Courtyard published native played to its win"
