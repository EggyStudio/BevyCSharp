#!/usr/bin/env bash
# Plays a game for a while through ./bcs as a player left at it would, a round at a time, and reads
# what the program holds (memory.collect) at the end of a round, no more often than every five
# seconds, into build/soak/<game>.txt. build/soak-check.py then fails when anything it holds climbs
# without leveling off, as a leak does.
#
#   build/soak.sh <courtyard|stress|swarm|feature-test> <seconds> [folder the game was built to]
#
# A round ends where the game is the same each time, so two readings of a game that leaks nothing
# match. Courtyard's starts from the menu, saves, walks a square, loads the save, pauses and goes
# back to the menu. Swarm's starts a game, fights its first wave walking a square and ends the game.
# The stress program spawns its cubes once and draws them for as long as it runs, so its rounds are
# frames alone, and they find what a frame leaks. The feature test's walks a square on the hub
# under the day and the weather, which go on as they would for a tester left at it, and its
# folder is the one the pack workflow builds it to from the package.
#
# The game is started offscreen and serving and stopped at the end whatever happened, and every
# call names it, so the three are played at once.
set -euo pipefail
cd "$(dirname "$0")/.."

name="$1"
seconds="$2"
case "$name" in
    courtyard) program=Courtyard; args=() ;;
    stress) program=Stress; args=(drawn 1000) ;;
    swarm) program=Swarm; args=() ;;
    feature-test) program=BevyCSharp.FeatureTest; args=(--offscreen --frames 0 --serve) ;;
    *) echo "soak.sh: no game called $name, only courtyard, stress, swarm and feature-test" >&2; exit 2 ;;
esac
if [ "$name" = feature-test ]; then
    folder="${3:-BevyCSharp.FeatureTest/bin/Release/net10.0}"
else
    folder="${3:-games/$program/bin/Debug/net10.0}"
fi

mkdir -p build/soak
out="$PWD/build/soak/$name.txt"
log="$PWD/build/soak/$name.log"
: > "$out"

c() { ./bcs command "$@" --name "$program" --timeout 600; }
quiet() { c "$@" >/dev/null; }
fail() { echo "FAILED: $name: $*" >&2; tail -20 "$log" >&2 || true; exit 1; }

trap './bcs stop --name "$program" >/dev/null 2>&1 || true' EXIT
(cd "$folder" && { BCS_OFFSCREEN=1 BCS_SERVE=1 "./$program" "${args[@]}"; echo "[soak] the game exited with $?"; } >"$log" 2>&1 &)
for _ in $(seq 1 120); do ./bcs status 2>/dev/null | grep -q "ready.*$program" && break; sleep 1; done
./bcs status | grep -q "ready.*$program" || fail "the game never answered"

# Each frame a sixtieth of a second of the game, so a round walks as far on a slow machine as on a
# fast one.
[ "$name" = stress ] || quiet app.frametime 0.0166667
quiet frames.wait 60
if [ "$name" = feature-test ]; then quiet mode walking; fi

round() {
    case "$name" in
        courtyard)
            quiet input.key Enter
            quiet frames.wait 10
            quiet input.key F5
            quiet input.hold D 30
            quiet input.hold S 30
            quiet input.hold A 30
            quiet input.hold W 30
            quiet input.key F9
            quiet frames.wait 10
            quiet input.key Escape
            quiet frames.wait 10
            quiet input.key Escape
            quiet state.set Mode Menu
            quiet frames.wait 10 ;;
        stress)
            quiet frames.wait 120 ;;
        feature-test)
            quiet input.hold D 30
            quiet input.hold S 30
            quiet input.hold A 30
            quiet input.hold W 30
            quiet frames.wait 10 ;;
        swarm)
            quiet input.key Enter
            quiet input.hold D 40
            quiet input.hold S 40
            quiet input.hold A 40
            quiet input.hold W 40
            quiet state.set Mode Over
            quiet frames.wait 10 ;;
    esac
}

rounds=0
end=$((SECONDS + seconds))
next=$SECONDS
while ((SECONDS < end)); do
    round
    rounds=$((rounds + 1))
    if ((SECONDS >= next)); then
        echo "$SECONDS $(c memory.collect)" >>"$out"
        next=$((SECONDS + 5))
    fi
done
grep -q "^\[soak\] the game exited" "$log" && fail "the game stopped while it was played"
echo "$name: $rounds rounds in $seconds seconds, $(wc -l <"$out") readings in $out"
