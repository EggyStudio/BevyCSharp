#!/usr/bin/env bash
# Plays Swarm through bcs from its menu into its third wave, with no display, and fails when a wave
# does not come, the creatures do not reach the hundreds or none of them falls. The frame is
# profiled while the most of them are alive, which is the load the game is here to put the engine
# under.
#
#   dotnet build games/Swarm
#   games/Swarm/play.sh [folder the game was built to]
#
# The game is started offscreen and serving, played by walking the player round a square in the
# middle of the arena faster than a creature walks, as a person keeping away from them would, and
# stopped at the end whatever happened. Captures, the game's log and the profile are left in the
# folder named by SHOTS, or in a temporary one.
set -euo pipefail
cd "$(dirname "$0")/../.."

game="${1:-games/Swarm/bin/Debug/net10.0}"
shots="${SHOTS:-$(mktemp -d)}"

quiet() { ./bcs command "$@" >/dev/null; }
fail() { echo "FAILED: $*" >&2; grep -h "^\[swarm\]\|^\[play\]" "$shots/game.log" >&2 || true; exit 1; }

# One of the numbers swarm.status answers with, by its name.
status() { ./bcs command swarm.status | tr ' ' '\n' | sed -n "s/^$1=//p"; }

trap './bcs stop >/dev/null 2>&1 || true' EXIT

./bcs stop >/dev/null 2>&1 || true
(cd "$game" && { BCS_OFFSCREEN=1 BCS_SERVE=1 ./Swarm; echo "[play] the game exited with $?"; } >"$shots/game.log" 2>&1 &)
for _ in $(seq 1 90); do ./bcs status 2>/dev/null | grep -q "ready.*Swarm" && break; sleep 1; done
./bcs status | grep -q "ready.*Swarm" || fail "the game never answered; its log is $shots/game.log"

# Each frame a sixtieth of a second of the game, however long the machine took to draw it, so the
# walk round the square is the same length on a software renderer as on a graphics card.
quiet app.frametime 0.0166667
quiet frames.wait 30
./bcs shot "$shots/1-menu.png" >/dev/null
[ "$(status mode)" = Menu ] || fail "the game did not start on its menu"

quiet input.key Enter
quiet frames.wait 10
[ "$(status mode)" = Playing ] || fail "Enter did not start play"

# Round the square, a side a second and a third, until the third wave has come and the most of it
# is alive, or the game is over first.
profiled=""
for lap in $(seq 1 40); do
    for keys in D S A W; do
        quiet input.hold "$keys" 80
        [ "$(status mode)" = Playing ] || break 2
        if [ -z "$profiled" ] && [ "$(status alive)" -ge 200 ]; then
            ./bcs command frame.profile 120 >"$shots/profile.txt"
            ./bcs shot "$shots/2-swarm.png" >/dev/null
            profiled=yes
        fi
    done
    [ -n "$profiled" ] && [ "$(status wave)" -ge 3 ] && break
done

./bcs command swarm.status | tee "$shots/status.txt"
[ "$(status wave)" -ge 3 ] || fail "play did not reach the third wave"
[ "$(status most)" -ge 200 ] || fail "no more than $(status most) creatures were alive at once"
[ "$(status felled)" -gt 0 ] || fail "no creature fell"
[ -n "$profiled" ] || fail "the frame was not profiled with the creatures in their hundreds"

cat "$shots/profile.txt"
echo "played to $shots"
