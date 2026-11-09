#!/usr/bin/env bash
# Plays a game for a while through ./bcs as a player left at it would, a round at a time, and reads
# what the program holds (memory.collect) at the end of a round, no more often than every five
# seconds, into build/soak/<game>.txt. build/soak-check.py then fails when anything it holds climbs
# without leveling off, as a leak does. A game whose rounds are slow plays on past the seconds
# given, by as much as half of them again, until it has been read as often as the check judges.
#
#   build/soak.sh <courtyard|stress|swarm|blockout|feature-test> <seconds> [folder the game was built to]
#
# A round ends where the game is the same each time, so two readings of a game that leaks nothing
# match. Courtyard's starts from the menu, saves, walks a square, loads the save, pauses and goes
# back to the menu. Swarm's starts a game, fights its first wave walking a square and ends the game.
# Blockout's walks two meters out and back by its autopilot, cuts a window in the room over the hall
# by a command and takes it back, which makes the level and its collider again twice, and opens and
# closes the modeling window.
# The stress program spawns its cubes once and draws them for as long as it runs, so its rounds are
# frames alone, and they find what a frame leaks. The feature test's walks a square on the hub
# under the day and the weather, which go on as they would for a tester left at it, and its
# folder is the one the pack workflow builds it to from the package.
#
# The game is started offscreen and serving and stopped at the end whatever happened, and every
# call names it, so several are played at once (build/soaks.sh).
set -eEuo pipefail
cd "$(dirname "$0")/.."

name="$1"
seconds="$2"
case "$name" in
    courtyard) program=Courtyard; args=() ;;
    stress) program=Stress; args=(drawn 1000) ;;
    swarm) program=Swarm; args=() ;;
    blockout) program=Blockout; args=() ;;
    feature-test) program=BevyCSharp.FeatureTest; args=(--offscreen --frames 0 --serve) ;;
    *) echo "soak.sh: no game called $name, only courtyard, stress, swarm, blockout and feature-test" >&2; exit 2 ;;
esac
if [ "$name" = feature-test ]; then
    folder="${3:-games/FeatureTest/bin/Release/net10.0}"
    # Eight gigabytes where the environment names none, as the scripted run gives it, since on
    # lavapipe with no shader cache of Mesa's, as the soak's job of its own starts, the feature test
    # reaches the cap it is given otherwise, a quarter of the runner's 15.6 gigabytes, while its
    # pipelines compile.
    export BCS_MEMORY_CAP_GB="${BCS_MEMORY_CAP_GB:-8}"
else
    folder="${3:-games/$program/bin/Debug/net10.0}"
fi

mkdir -p build/soak
out="$PWD/build/soak/$name.txt"
log="$PWD/build/soak/$name.log"
said="$PWD/build/soak/$name.said"
: > "$out"

# Each call of bcs that fails says its words, its code and the first line bcs gave of why, as the
# games' plays do, where set -e ended one with nothing said, and anything else that fails is named
# by the trap. The four games play at once, so each line names its game.
c() { ./bcs command "$@" --name "$program" --timeout 600 >"$said" || fail "./bcs command $* ended with $?, $(head -n 1 "$said")"; cat "$said"; }
quiet() { c "$@" >/dev/null; }
fail() { echo "FAILED: $name: $*" >&2; tail -20 "$log" >&2 || true; exit 1; }

trap 'code=$?; echo "FAILED: $name: $BASH_COMMAND ended with $code on line $LINENO" >&2' ERR
trap './bcs stop --name "$program" >/dev/null 2>&1 || true' EXIT
(cd "$folder" && { BCS_OFFSCREEN=1 BCS_SERVE=1 "./$program" "${args[@]}"; echo "[soak] the game exited with $?"; } >"$log" 2>&1 &)
# Ten minutes for the game to answer, as long as each command is given, and its answer taken once
# rather than asked for again. On lavapipe with no shader cache of Mesa's, as the feature test's
# soak in a job of its own starts, the game says it is ready at its first frame and then answers
# nothing for minutes while its pipelines compile, so a second asking reads it unreachable, and two
# minutes of asking is too short there.
answered=0
for _ in $(seq 1 600); do ./bcs status 2>/dev/null | grep -q "ready.*$program" && { answered=1; break; }; sleep 1; done
[ "$answered" = 1 ] || fail "the game never answered"

# Each frame a sixtieth of a second of the game, so a round walks as far on a slow machine as on a
# fast one.
[ "$name" = stress ] || quiet app.frametime 0.0166667
quiet frames.wait 60
# The feature test's global illumination at the quality GLOBAL_ILLUMINATION names, the game's own
# where it names none, as the scripted run's (build/drive-feature-test.sh), and put back as it was
# at the end. The workflow names Off, since its tracing takes a large share of each frame on
# lavapipe, and rounds that long would not reach the readings the check judges by the soak's last
# second.
illumination_was=""
if [ "$name" = feature-test ] && [ -n "${GLOBAL_ILLUMINATION:-}" ]; then
    illumination_was=$(c setting GlobalIllumination | sed -n 's/^GlobalIllumination = //p')
    quiet setting "GlobalIllumination $GLOBAL_ILLUMINATION"
fi
# The feature test's pipelines compiled before its first round, which on lavapipe with no shader
# cache of Mesa's take minutes and grow what it holds while they do, so its readings are not of
# compiles.
if [ "$name" = feature-test ]; then quiet mode walking; quiet pipelines.wait; fi

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
        blockout)
            quiet blockout.autopilot loop
            for _ in $(seq 1 60); do
                quiet frames.wait 15
                c blockout.status | grep -q "^autopilot loop done" && break
            done
            quiet model.select level box 0.2 3.4 7.8 9.8 6.6 7.8
            quiet model.cut level window 1.4 1.2 4 1
            quiet model.undo level
            quiet model.undo level
            quiet input.key Tab
            quiet frames.wait 5
            quiet input.key Tab
            quiet frames.wait 5 ;;
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

# Six readings, as build/soak-check.py judges no fewer, which a slow game's rounds on lavapipe can
# fall short of in the seconds given, so the soak plays on past them until it has six, for half the
# seconds again at most.
fewest=$(python3 build/soak-check.py --fewest)
rounds=0
readings=0
end=$((SECONDS + seconds))
cap=$((end + seconds / 2))
next=$SECONDS
while ((SECONDS < end)) || ((readings < fewest && SECONDS < cap)); do
    round
    rounds=$((rounds + 1))
    if ((SECONDS >= next)); then
        # Read into a name first, since a substitution inside echo's words would leave a failed
        # reading as an empty line and the soak going on.
        at=$SECONDS
        reading=$(c memory.collect)
        echo "$at $reading" >>"$out"
        readings=$((readings + 1))
        next=$((SECONDS + 5))
    fi
done
grep -q "^\[soak\] the game exited" "$log" && fail "the game stopped while it was played"
if [ -n "$illumination_was" ]; then quiet setting "GlobalIllumination $illumination_was"; fi
echo "$name: $rounds rounds in $((SECONDS - end + seconds)) seconds, $readings readings in $out"
