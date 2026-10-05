#!/usr/bin/env bash
# Measures one of Bevy's stress tests twice, Bevy's own program and the C# one written from it, and
# prints the two frames side by side for PERFORMANCE.md.
#
#   dotnet build BevyCSharp.Examples -c Release
#   build/measure-stress.sh many_sprites
#   build/measure-stress.sh many_sprites --colored
#   ROUNDS=5 build/measure-stress.sh many_cubes
#
# Each is drawn offscreen into an image of the size the test asks for, 1920 by 1080 unless
# BEVY_STRESS_SIZE says otherwise, unpaced, let settle for 300 frames and measured over 240, the C#
# one by frame.profile and Bevy's by build/bevy-stress.sh's clock at the same place in the frame.
# The two are run in turn for some rounds, three unless ROUNDS says, since a laptop's clock moves
# between runs, and the median of each is printed in milliseconds.
set -euo pipefail
cd "$(dirname "$0")/.."

name=$1
shift
size="${BEVY_STRESS_SIZE:-1920x1080}"
rounds="${ROUNDS:-3}"
program=BevyCSharp.Examples/bin/Release/net10.0/BevyCSharp.Examples
log="$(mktemp)"

stop() { ./bcs stop >/dev/null 2>&1 || true; }
trap 'stop; rm -f "$log"' EXIT

median() { sort -n | awk '{ a[NR] = $1 } END { print (NR % 2) ? a[(NR + 1) / 2] : (a[NR / 2] + a[NR / 2 + 1]) / 2 }'; }

csharp=()
bevy=()
for _ in $(seq 1 "$rounds"); do
    stop
    "$program" "$name" --offscreen --serve --size "$size" "$@" >"$log" 2>&1 &
    for _ in $(seq 1 300); do ./bcs status 2>/dev/null | grep -q "ready.*BevyCSharp.Examples" && break; sleep 1; done
    ./bcs command frames.wait 300 --timeout 1200 >/dev/null
    row=$(./bcs command frame.profile 240 --timeout 1200 | tail -1)
    csharp+=("$(sed -n 's/.*frame_ms=\([0-9.]*\).*/\1/p' <<<"$row")")
    stop

    bevy+=("$(BEVY_STRESS_SIZE="$size" build/bevy-stress.sh "$name" "$@" 2>/dev/null | sed -n 's/^frame_ms=//p')")
done

echo "$name $* bevy_ms=$(printf '%s\n' "${bevy[@]}" | median) bevycsharp_ms=$(printf '%s\n' "${csharp[@]}" | median) rounds=$rounds"
