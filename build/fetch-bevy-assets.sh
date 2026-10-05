#!/usr/bin/env bash
# Downloads the files of Bevy's assets folder that the examples load, at the Bevy release
# native/Cargo.lock pins, into BevyCSharp.Examples/bevy-assets, which a build of the examples copies
# beside the program as its assets. A file already there is kept, so this is cheap to run again.
# A line that is a full address is a file an example loads from Bevy's asset_files repository, and
# is kept at its path after the branch or the commit an example pins it at, which is the path the
# example here loads it by.
#
#   build/fetch-bevy-assets.sh
set -euo pipefail
cd "$(dirname "$0")/.."

version=$(awk '/^name = "bevy"$/ { getline; gsub(/version = |"/, ""); print; exit }' native/Cargo.lock)
[ -n "$version" ] || { echo "native/Cargo.lock names no bevy version" >&2; exit 1; }

into=BevyCSharp.Examples/bevy-assets
while read -r line; do
    case "$line" in ''|'#'*) continue ;; esac
    case "$line" in
      https://*) url="$line"; path="${line#*/raw/}"; path="${path#*/}" ;;
      # Some of Bevy's files have spaces in their names, which a URL writes as %20.
      *) url="https://raw.githubusercontent.com/bevyengine/bevy/v$version/assets/${line// /%20}"; path="$line" ;;
    esac
    [ -f "$into/$path" ] && continue
    mkdir -p "$into/$(dirname "$path")"
    curl -fsSL --retry 3 -o "$into/$path" "$url"
    echo "fetched $path"
done < BevyCSharp.Examples/bevy-assets.txt
