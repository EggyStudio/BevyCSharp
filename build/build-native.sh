#!/usr/bin/env bash
#
# Builds the native Bevy bridge and stages it where the NuGet package expects it.
#
# The managed package is portable, but the bridge is not: it is a cdylib that has to be built
# once per runtime identifier and shipped under runtimes/<rid>/native/. This script builds the
# host RID by default; pass a target triple to cross-compile one of the others.
#
# Usage:
#   build/build-native.sh                      # host, headless profile
#   build/build-native.sh --render             # host, with Bevy's renderer
#   build/build-native.sh --target x86_64-pc-windows-msvc --render
#
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
NATIVE_DIR="$ROOT/native"
ARTIFACT_DIR="$NATIVE_DIR/artifacts"

FEATURES="headless"
TARGET=""

while [[ $# -gt 0 ]]; do
    case "$1" in
        --render)  FEATURES="render"; shift ;;
        --headless) FEATURES="headless"; shift ;;
        --target)  TARGET="$2"; shift 2 ;;
        -h|--help)
            sed -n '2,14p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'
            exit 0 ;;
        *) echo "unknown argument: $1" >&2; exit 2 ;;
    esac
done

if ! command -v cargo >/dev/null 2>&1; then
    echo "error: cargo not found. Install Rust from https://rustup.rs and re-run." >&2
    exit 1
fi

# Map a Rust target triple onto the .NET runtime identifier and library file name that the
# NuGet runtimes/ layout keys off.
rid_for_target() {
    case "$1" in
        x86_64-unknown-linux-gnu)  echo "linux-x64   libbevy_csharp.so" ;;
        aarch64-unknown-linux-gnu) echo "linux-arm64 libbevy_csharp.so" ;;
        x86_64-pc-windows-msvc)    echo "win-x64     bevy_csharp.dll" ;;
        aarch64-pc-windows-msvc)   echo "win-arm64   bevy_csharp.dll" ;;
        x86_64-apple-darwin)       echo "osx-x64     libbevy_csharp.dylib" ;;
        aarch64-apple-darwin)      echo "osx-arm64   libbevy_csharp.dylib" ;;
        *) echo "" ;;
    esac
}

if [[ -z "$TARGET" ]]; then
    TARGET="$(rustc -vV | awk '/^host:/ {print $2}')"
fi

MAPPING="$(rid_for_target "$TARGET")"
if [[ -z "$MAPPING" ]]; then
    echo "error: no .NET runtime identifier is mapped for target '$TARGET'." >&2
    echo "       Add it to rid_for_target in $(basename "${BASH_SOURCE[0]}")." >&2
    exit 1
fi

read -r RID LIBNAME <<<"$MAPPING"

echo "==> building bevy_csharp"
echo "    target   : $TARGET"
echo "    rid      : $RID"
echo "    features : $FEATURES"

if [[ "$FEATURES" == "render" ]]; then
    echo "    note     : the render profile needs the platform development packages"
    echo "               (X11/Wayland, alsa, udev and a Vulkan loader on Linux)."
fi

cargo build \
    --release \
    --manifest-path "$NATIVE_DIR/Cargo.toml" \
    --target "$TARGET" \
    --no-default-features \
    --features "$FEATURES"

BUILT="$NATIVE_DIR/target/$TARGET/release/$LIBNAME"
if [[ ! -f "$BUILT" ]]; then
    echo "error: cargo reported success but '$BUILT' is missing." >&2
    exit 1
fi

mkdir -p "$ARTIFACT_DIR/$RID"
cp "$BUILT" "$ARTIFACT_DIR/$RID/$LIBNAME"

# Also stage it where a plain `cargo build` would have put it, so running straight out of the
# repo picks it up without a pack step.
mkdir -p "$NATIVE_DIR/target/release"
cp "$BUILT" "$NATIVE_DIR/target/release/$LIBNAME"

echo "==> staged $ARTIFACT_DIR/$RID/$LIBNAME"
