#!/usr/bin/env bash
#
# Builds the native Bevy bridge and stages it where the NuGet package expects it.
#
# The managed package is portable, but the bridge is not: it is a cdylib that has to be built
# once per runtime identifier and shipped under runtimes/<rid>/native/. This script builds the
# host RID by default; pass a target triple to cross-compile one of the others.
#
# Everything this script generates lives under build/, both cargo's target directory and the
# staged per-RID artifacts. The repository root stays clean; only native/ (the Rust sources)
# and the project folders live there.
#
# Usage:
#   build/build-native.sh                      # host, headless profile
#   build/build-native.sh --render             # host, with Bevy's renderer
#   build/build-native.sh --editor             # the above plus the HTML and CSS UI
#   build/build-native.sh --render --portable  # build in a container, for older machines
#   build/build-native.sh --local              # override a PORTABLE=1 in build-native.local
#   build/build-native.sh --clean              # remove build/target and build/artifacts first
#
# On Linux a binary runs only where glibc is at least as new as the one it was built against, so
# building on a current distribution produces something that will not load on an older one. The
# --portable flag builds inside a Debian container instead, which lowers that floor far enough to
# cover any supported distribution. It needs podman or docker.
#
set -euo pipefail

# This script's own directory. All build output is written here.
BUILD_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# One level up, purely to locate the Rust sources. Nothing is ever written there.
REPO_ROOT="$(cd "$BUILD_DIR/.." && pwd)"

NATIVE_DIR="$REPO_ROOT/native"
TARGET_DIR="$BUILD_DIR/target"
ARTIFACT_DIR="$BUILD_DIR/artifacts"

FEATURES="headless"
TARGET=""
CLEAN=0
PORTABLE=0

# Per-machine defaults, not checked in.
#
# A binary only runs on a glibc at least as new as the one it was built against, so on a machine
# whose glibc is newer than the machines its output has to run on, every ordinary build produces
# something unloadable there. Setting `PORTABLE=1` in this file makes the container path the
# default for that machine and leaves every other machine, and CI, building as before.
if [[ -f "$BUILD_DIR/build-native.local" ]]; then
    # shellcheck source=/dev/null
    source "$BUILD_DIR/build-native.local"
fi

# Old enough that its glibc floor covers every supported distribution, new enough to carry a
# Rust that understands the crate's edition.
PORTABLE_IMAGE="docker.io/library/rust:1-bookworm"

while [[ $# -gt 0 ]]; do
    case "$1" in
        --render)   FEATURES="render"; shift ;;
        --editor)   FEATURES="editor"; shift ;;
        --headless) FEATURES="headless"; shift ;;
        --target)   TARGET="$2"; shift 2 ;;
        --clean)    CLEAN=1; shift ;;
        --portable) PORTABLE=1; shift ;;
        --local)    PORTABLE=0; shift ;;
        -h|--help)
            sed -n '2,18p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'
            exit 0 ;;
        *) echo "unknown argument: $1" >&2; exit 2 ;;
    esac
done

if [[ ! -f "$NATIVE_DIR/Cargo.toml" ]]; then
    echo "error: no Rust workspace at '$NATIVE_DIR'." >&2
    echo "       This script expects to live in <repo>/build/ with the sources in <repo>/native/." >&2
    exit 1
fi

CONTAINER=""
if [[ $PORTABLE -eq 1 ]]; then
    for candidate in podman docker; do
        if command -v "$candidate" >/dev/null 2>&1; then CONTAINER="$candidate"; break; fi
    done
    if [[ -z "$CONTAINER" ]]; then
        echo "error: --portable needs podman or docker, and neither is installed." >&2
        exit 1
    fi
elif ! command -v cargo >/dev/null 2>&1; then
    echo "error: cargo not found. Install Rust from https://rustup.rs and re-run." >&2
    echo "       If it is installed, add it to PATH: export PATH=\"\$HOME/.cargo/bin:\$PATH\"" >&2
    exit 1
fi

if [[ $CLEAN -eq 1 ]]; then
    echo "==> cleaning $TARGET_DIR and $ARTIFACT_DIR"
    rm -rf "$TARGET_DIR" "$ARTIFACT_DIR"
fi

# Map a Rust target triple onto the .NET runtime identifier and library file name that the
# NuGet runtimes/ layout keys off.
rid_for_target() {
    case "$1" in
        x86_64-unknown-linux-gnu)   echo "linux-x64        libbevy_csharp.so" ;;
        aarch64-unknown-linux-gnu)  echo "linux-arm64      libbevy_csharp.so" ;;
        x86_64-unknown-linux-musl)  echo "linux-musl-x64   libbevy_csharp.so" ;;
        aarch64-unknown-linux-musl) echo "linux-musl-arm64 libbevy_csharp.so" ;;
        x86_64-pc-windows-msvc)     echo "win-x64          bevy_csharp.dll" ;;
        aarch64-pc-windows-msvc)    echo "win-arm64        bevy_csharp.dll" ;;
        x86_64-pc-windows-gnu)      echo "win-x64          bevy_csharp.dll" ;;
        x86_64-apple-darwin)        echo "osx-x64          libbevy_csharp.dylib" ;;
        aarch64-apple-darwin)       echo "osx-arm64        libbevy_csharp.dylib" ;;
        *) echo "" ;;
    esac
}

if [[ -z "$TARGET" ]]; then
    if [[ $PORTABLE -eq 1 ]]; then
        TARGET="x86_64-unknown-linux-gnu"
    else
        TARGET="$(rustc -vV | awk '/^host:/ {print $2}')"
    fi
fi

# A render build links against ALSA, and alsa-sys reports a missing header as a compiler error
# from a crate the caller never asked for. Saying so here, by name, is worth the four lines.
if [[ $PORTABLE -eq 0 && ( "$FEATURES" == "render" || "$FEATURES" == "editor" ) && "$(uname -s)" == "Linux" ]]; then
    if ! pkg-config --exists alsa 2>/dev/null; then
        echo "error: a render build needs ALSA's development headers, which are not installed." >&2
        echo "       Bevy's audio links against them through cpal." >&2
        echo "         Fedora, RHEL  : sudo dnf install alsa-lib-devel" >&2
        echo "         Debian, Ubuntu: sudo apt install libasound2-dev" >&2
        echo "         Arch          : sudo pacman -S alsa-lib" >&2
        echo "       Or build with --portable, which installs them into a container instead." >&2
        exit 1
    fi
fi

if [[ $PORTABLE -eq 1 && "$TARGET" != *linux-gnu ]]; then
    echo "error: --portable only applies to Linux targets, not '$TARGET'." >&2
    exit 1
fi

MAPPING="$(rid_for_target "$TARGET")"
if [[ -z "$MAPPING" ]]; then
    echo "error: no .NET runtime identifier is mapped for target '$TARGET'." >&2
    echo "       Add it to rid_for_target in $(basename "${BASH_SOURCE[0]}")." >&2
    exit 1
fi

read -r RID LIBNAME <<<"$MAPPING"

echo "==> building bevy_csharp"
echo "    sources  : $NATIVE_DIR"
echo "    output   : $TARGET_DIR"
echo "    target   : $TARGET"
echo "    rid      : $RID"
echo "    features : $FEATURES"

if [[ "$FEATURES" == "render" || "$FEATURES" == "editor" ]]; then
    echo "    note     : builds Bevy's renderer, winit and wgpu. This takes several minutes"
    echo "               the first time and produces a much larger library."
fi

if [[ $PORTABLE -eq 1 ]]; then
    echo "    glibc    : whatever $PORTABLE_IMAGE carries, rather than this machine's"

    # A separate target directory, because objects built against two different C libraries must
    # not share one. The registry is mounted so the container reuses crates already downloaded.
    TARGET_DIR="$BUILD_DIR/target-portable"
    mkdir -p "$TARGET_DIR" "${CARGO_HOME:-$HOME/.cargo}/registry"

    # A render build needs ALSA's development headers, because Bevy's audio links against it.
    # The image does not carry them, so they are installed into the container that is about to be
    # thrown away rather than onto the machine.
    SETUP=""
    if [[ "$FEATURES" == "render" || "$FEATURES" == "editor" ]]; then
        SETUP="apt-get update -qq && apt-get install -y -qq --no-install-recommends libasound2-dev >/dev/null && "
    fi

    "$CONTAINER" run --rm \
        -v "$REPO_ROOT:/src:z" \
        -v "${CARGO_HOME:-$HOME/.cargo}/registry:/usr/local/cargo/registry:z" \
        -w /src \
        "$PORTABLE_IMAGE" \
        sh -c "${SETUP}cargo build \
            --release \
            --manifest-path native/Cargo.toml \
            --target-dir build/$(basename "$TARGET_DIR") \
            --no-default-features \
            --features $FEATURES"

    # No --target inside the container, so cargo writes to the plain release directory.
    BUILT="$TARGET_DIR/release/$LIBNAME"
else
    cargo build \
        --release \
        --manifest-path "$NATIVE_DIR/Cargo.toml" \
        --target-dir "$TARGET_DIR" \
        --target "$TARGET" \
        --no-default-features \
        --features "$FEATURES"

    BUILT="$TARGET_DIR/$TARGET/release/$LIBNAME"
fi

if [[ ! -f "$BUILT" ]]; then
    echo "error: cargo reported success but '$BUILT' is missing." >&2
    exit 1
fi

# The per-RID slot the NuGet package picks up at pack time.
mkdir -p "$ARTIFACT_DIR/$RID"
cp "$BUILT" "$ARTIFACT_DIR/$RID/$LIBNAME"

# A flat copy for repo-local development: the projects copy this next to their own output, so
# running the sample or the tests works without packing first. Always under build/target, which
# is where the project files look, whichever way the build was done.
mkdir -p "$BUILD_DIR/target/release"
cp "$BUILT" "$BUILD_DIR/target/release/$LIBNAME"

echo "==> staged $ARTIFACT_DIR/$RID/$LIBNAME"
echo "==> staged $BUILD_DIR/target/release/$LIBNAME (for local runs)"

if [[ "$TARGET" == *linux-gnu ]] && command -v objdump >/dev/null 2>&1; then
    floor=$(objdump -T "$BUILT" | grep -oP 'GLIBC_\K[0-9]+\.[0-9]+' | sort -uV | tail -1)
    echo "==> needs glibc $floor or newer"

    # A machine that defaulted to the container path did so because what it builds otherwise does
    # not load where it has to run. Overriding that stages a library that fails at load with a
    # GLIBC version error, and the staged copy is what every project here picks up, so the next
    # run of anything hits it. Worth saying loudly rather than leaving to be discovered.
    if [[ $PORTABLE -eq 0 && -f "$BUILD_DIR/build-native.local" ]] \
        && grep -q '^PORTABLE=1' "$BUILD_DIR/build-native.local"; then
        echo
        echo "!!  This checkout defaults to --portable and you passed --local."
        echo "!!  What is staged now needs glibc $floor, which is this machine's, not the one it"
        echo "!!  has to run on. Rebuild without --local before running anything."
    fi
fi
