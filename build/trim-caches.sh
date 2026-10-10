#!/usr/bin/env bash
# Keeps the bridge's build caches under the bounds .github/BUILDING.md gives, so they cannot fill
# the disk. build/build-native.sh runs it before each build, and it may be run on its own.
#
#   build/trim-caches.sh
#
# A cache past its bound loses its incremental state first, which is most of its growth and the
# cheapest to make again, and the whole of it only when that is not enough. Every cache is Cargo's
# and comes back on the next build, which only takes longer. The staged libraries in build/target,
# which a managed build copies, are kept whatever the size.
set -euo pipefail
cd "$(dirname "$0")/.."

# The size of a folder in gigabytes, rounded down, by du's kilobytes, which GNU's du and the BSD
# one macOS has both give.
gigabytes() { du -sk "$1" 2>/dev/null | awk '{ print int($1 / 1048576) }'; }

# Trims a Cargo target folder past its bound, its incremental state first and then the whole.
trim() {
    local folder="$1" bound="$2"
    [ -d "$folder" ] || return 0
    local size
    size="$(gigabytes "$folder")"
    [ "$size" -gt "$bound" ] || return 0

    echo "trim-caches: $folder holds $size GB, past its $bound, so its incremental state goes"
    find "$folder" -maxdepth 3 -type d -name incremental -prune -exec rm -rf {} +
    size="$(gigabytes "$folder")"
    [ "$size" -gt "$bound" ] || return 0

    echo "trim-caches: $folder still holds $size GB, so all of it goes"
    rm -rf "$folder"
}

# What cargo check and cargo test write by default, every profile and feature set apart.
trim native/target 80
# The container's builds, which a portable build makes incrementally from.
trim build/target-portable 50

# A local build's target, of which only the staged libraries are kept past the bound.
if [ -d build/target ] && [ "$(gigabytes build/target)" -gt 2 ]; then
    echo "trim-caches: build/target holds $(gigabytes build/target) GB, so all but its staged libraries go"
    find build/target -mindepth 1 -maxdepth 1 ! -name release -exec rm -rf {} +
    find build/target/release -mindepth 1 -maxdepth 1 -type d -exec rm -rf {} +
fi
