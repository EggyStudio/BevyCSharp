#!/usr/bin/env bash
# Writes the package's release notes, the commits since build/version.txt last changed, newest
# first, each commit's sentence a line as the history already says them to be read, into the file
# named, build/artifacts/release-notes.txt by default. A version raised by the last commit has
# none, and so does a checkout without the history, as a shallow clone is.
#
#   build/release-notes.sh [file]
set -euo pipefail
cd "$(dirname "$0")/.."

out="${1:-build/artifacts/release-notes.txt}"
mkdir -p "$(dirname "$out")"
: > "$out"
if changed="$(git log -1 --format=%H -- build/version.txt 2>/dev/null)" && [ -n "$changed" ]; then
    git log --format=%b "$changed"..HEAD | sed -e 's/[[:space:]]*$//' -e '/^$/d' > "$out" || true
fi
echo "$(wc -l < "$out") lines of release notes in $out"
