#!/usr/bin/env bash
# Measures what a frame holds at growing counts, one run of the stress program for each, and prints
# a row of figures per count until a frame takes longer than a sixtieth of a second, then one more.
#
#   dotnet build games/Stress -c Release
#   games/Stress/measure.sh movers 1000 2000 5000 10000 20000 50000 100000
#   games/Stress/measure.sh drawn 1000 2000 5000 10000 20000 50000
#
# Each run is let settle for a few seconds, its pipelines compiled and its bodies come to rest,
# then asked for frame.profile over 240 frames, whose last line is the row. The rows are names and
# numbers, as frame.profile writes them, with the count in front.
set -euo pipefail
cd "$(dirname "$0")/../.."

mode=$1
shift
program="${STRESS:-games/Stress/bin/Release/net10.0/Stress}"
log="$(mktemp)"

stop() { ./bcs stop >/dev/null 2>&1 || true; }
trap stop EXIT

over=0
for count in "$@"; do
    stop
    "$program" "$mode" "$count" >"$log" 2>&1 &
    for _ in $(seq 1 120); do ./bcs status 2>/dev/null | grep -q "ready.*Stress" && break; sleep 1; done

    ./bcs command frames.wait 300 >/dev/null
    row=$(./bcs command frame.profile 240 | tail -1)
    echo "count=$count $row"

    frame=$(sed -n 's/.*frame_ms=\([0-9.]*\).*/\1/p' <<<"$row")
    if awk -v f="$frame" 'BEGIN { exit !(f > 16.667) }'; then
        over=$((over + 1))
        [ "$over" -ge 2 ] && break
    fi
done
