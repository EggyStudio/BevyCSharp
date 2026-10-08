#!/usr/bin/env bash
# Publishes the feature test from the package as native code for the machine it runs on, as a
# tester gets it, and zips it into build/feature-test as BevyCSharp-FeatureTest-<rid>.zip, the
# program with the bridge and the interface's library beside it, its assets, the scene packs'
# manifests and a README.txt for testers naming the keys, the panel, the console and where the logs
# are.
#
#   build/publish-feature-test.sh <version> [--cache <folder>] [options for dotnet publish]
#
# The package is the version named, packed into build/package beforehand with the bridge for this
# machine, as build/play-native.sh takes one. Native code is published for the system it is made
# on, so a workflow makes the Windows zip on Windows and the Linux one on Linux. The options are
# passed on, as -p:CppCompilerAndLinker=gcc on a machine with no clang. The folder the zip was made
# from is left beside it, for build/drive-feature-test.sh to drive.
#
# A tester's machine has no slangc, so the program reads its shaders from the cache under its
# assets, which the program fills where slangc is found (build/fetch-slang.sh). The published
# program is run once here, offscreen and serving, every program it can use is made
# (feature.shaders), and the run waits until each has compiled, so the cache that ships holds every
# one. Any session serving is stopped first, as the drive script stops one. On a machine where the
# program cannot draw, as on a runner with no GPU, --cache names the cache a run elsewhere filled,
# which is taken in its place, since an entry is named by the shader's path under the assets, its
# defines and the bridge's own modules, and every checkout has the same text with the same line
# ends (.gitattributes).
set -euo pipefail

version="$1"
shift
cache=""
if [ "${1:-}" = --cache ]; then
  cache="$(cd "$2" && pwd)"
  shift 2
fi
cd "$(dirname "$0")/.."

case "$(uname -s)-$(uname -m)" in
  Linux-x86_64) rid=linux-x64 ;;
  Linux-aarch64) rid=linux-arm64 ;;
  Darwin-arm64) rid=osx-arm64 ;;
  Darwin-x86_64) rid=osx-x64 ;;
  *) rid=win-x64 ;;
esac

name="BevyCSharp-FeatureTest-$rid"
out="build/feature-test/$name"
rm -rf "$out" "$out.zip"
dotnet publish BevyCSharp.FeatureTest -c Release -r "$rid" -p:PublishAot=true -p:BevyCSharpVersion="$version" -o "$out" "$@"

# The program the publish wrote, native code with no assembly beside it to run in its place.
if [ -f "$out/BevyCSharp.FeatureTest.dll" ]; then
  echo "the publish left BevyCSharp.FeatureTest.dll beside the program, so it is not native code" >&2
  exit 1
fi

# The cache the checkout's own runs left, which the publish copied with the assets, put aside, so
# only what this build compiles ships.
rm -rf "$out/assets/.slang-cache"

fail() { ./bcs stop > /dev/null 2>&1 || true; echo "publish-feature-test.sh: $1" >&2; tail -n 20 "$out/logs/latest.log" >&2 || true; exit 1; }
if [ -n "$cache" ]; then
  cp -R "$cache" "$out/assets/.slang-cache"
else
  ./bcs stop > /dev/null 2>&1 || true
  (cd "$out" && ./BevyCSharp.FeatureTest --offscreen --frames 0 --serve > /dev/null 2>&1 &)
  for _ in $(seq 1 120); do ./bcs status 2> /dev/null | grep -q "ready.*BevyCSharp.FeatureTest" && break; sleep 1; done
  ./bcs status | grep -q "ready.*BevyCSharp.FeatureTest" || fail "the published program never answered"
  ./bcs command shader.status | grep -q "slangc was found" || fail "slangc was not found, so the shaders cannot be compiled into the cache; build/fetch-slang.sh fetches it"

  ./bcs command feature.shaders > /dev/null
  listed=""
  for _ in $(seq 1 180); do
    listed=$(./bcs command shader.list)
    grep -q "compiling" <<< "$listed" || break
    sleep 1
  done
  grep -q "compiling\|failed" <<< "$listed" && fail "not every shader compiled: $listed"
  for shader in BevyCSharp.FeatureTest/assets/shaders/*.slang; do
    grep -q "shaders/$(basename "$shader")" <<< "$listed" || fail "the program never made $(basename "$shader")"
  done
  ./bcs stop > /dev/null
fi

# What a tester has no use for, the run's log, the symbols the native compiler writes beside the
# program and the library's documentation, and what a tester reads first.
rm -rf "$out/logs"
rm -f "$out"/*.dbg "$out"/*.pdb "$out"/*.xml
cp BevyCSharp.FeatureTest/README.txt "$out/README.txt"

# Zipped by Python's own module, which each system the script runs on has, where zip is missing on
# Windows. There it is python, since python3 may be the store's stand-in for one.
case "$(uname -s)" in
  MINGW* | MSYS* | CYGWIN*) python=python ;;
  *) python=python3 ;;
esac
(cd build/feature-test && "$python" -m zipfile -c "$name.zip" "$name")
echo "build/feature-test/$name.zip"
