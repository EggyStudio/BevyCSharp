#!/usr/bin/env bash
# Plays Blockout through bcs with no display, and fails when a step of the game does not happen:
# the level made from its recipe with the walker on the ground floor, the walk up the stairs to the
# room over the hall, a window cut in that room by a command with the counts read and the collider
# made again, the modeling window opened on Tab, the recipe saved on F5 and the walk back down. The
# autopilot's tour of every room is not walked here, since on lavapipe held to four processors the
# play took 583 seconds with it, about a thousand of its frames the tour's.
#
#   dotnet build games/Blockout -p:BevyCSharpVersion=<the version of a package of the checkout>
#   games/Blockout/play.sh [folder the game was built or exported to]
#
# The game is started offscreen and serving and stopped at the end whatever happened, and the
# level's file the save wrote over is put back as it was built. Captures of each step are left in
# the folder named by SHOTS, or in a temporary one.
set -eEuo pipefail
cd "$(dirname "$0")/../.."

game="${1:-games/Blockout/bin/Debug/net10.0}"
shots="${SHOTS:-$(mktemp -d)}"
said="$shots/said.txt"
level="$game/assets/levels/blockout.model"

fail() { echo "FAILED: $*" >&2; grep -h "^\[play\]\|^\[blockout\]" "$shots/game.log" >&2 || true; exit 1; }

# Every command waits as long as ten minutes for its answer, as Courtyard's play does, since a
# software device draws a few frames a second, and bcs writes why a command failed to its output,
# which is kept in a file and passed on once the command has worked.
c() { ./bcs command "$@" --timeout 600 >"$said" || fail "./bcs command $* ended with $?, $(head -n 1 "$said")"; cat "$said"; }
quiet() { c "$@" >/dev/null; }
shot() { ./bcs shot "$1" --timeout 600 >"$said" || fail "./bcs shot $1 ended with $?, $(head -n 1 "$said")"; }

# Walks a route by the game's autopilot, reading its status after each second of the game, and
# fails where the walker is stuck or the route takes longer than four minutes of the game.
walk() {
    local status
    quiet blockout.autopilot "$1"
    for _ in $(seq 1 240); do
        quiet frames.wait 60
        status=$(c blockout.status)
        grep -q "^autopilot $1 done" <<<"$status" && return 0
        grep -q "stuck" <<<"$status" && fail "the walker was stuck walking $1, $(tr '\n' ' ' <<<"$status")"
    done
    fail "the walker did not finish $1 in four minutes of the game, $(tr '\n' ' ' <<<"$status")"
}

# How high the walker's feet are, from the status's first line.
height() { c blockout.status | sed -n 's/^at [^ ]* \([^ ]*\) .*/\1/p'; }

# The faces a model's answer counts, its third number.
faces() { sed -n 's/^[^:]*: [0-9]* vertices, [0-9]* edges, \([0-9]*\) faces.*/\1/p'; }

trap 'code=$?; echo "FAILED: $BASH_COMMAND ended with $code on line $LINENO" >&2' ERR
trap './bcs stop >/dev/null 2>&1 || true; cp games/Blockout/assets/levels/blockout.model "$level" 2>/dev/null || true' EXIT

./bcs stop >/dev/null 2>&1 || true
(cd "$game" && { BCS_OFFSCREEN=1 BCS_SERVE=1 ./Blockout; echo "[play] the game exited with $?"; } >"$shots/game.log" 2>&1 &)
for _ in $(seq 1 90); do ./bcs status 2>/dev/null | grep -q "ready.*Blockout" && break; sleep 1; done
./bcs status | grep -q "ready.*Blockout" || fail "the game never answered; its log is $shots/game.log"

# Each frame a sixtieth of a second of the game, however long the machine took to draw it, so a
# walk takes as many frames on a software device as on a GPU. The first picture waits four seconds
# of the game, since the renderer makes its pipelines while the first frames are drawn, and the
# thirtieth and the sixtieth showed the sky alone.
quiet app.frametime 0.0166667
quiet frames.wait 240
grep -q "^\[blockout\] levels/blockout.model, [0-9]* faces from [0-9]* lines" "$shots/game.log" || fail "the level was not made from its recipe"
c blockout.status | grep -q "on the ground floor" || fail "the walker does not stand on the ground floor, $(c blockout.status | head -n 1)"
shot "$shots/1-hall.png"

# Up the stairs to the room over the hall, whose floor is 3.4 meters up.
walk upstairs
up=$(height)
awk -v y="$up" 'BEGIN { exit !(y > 3.3 && y < 3.6) }' || fail "the walker stands $up meters up after the stairs, where the floor over the hall is 3.4"
shot "$shots/2-upstairs.png"

# A window cut by a command through the room's north wall, two faces more and its four sides
# selected, and the collider made again from the level it left.
before=$(c model.info level | head -n 1 | faces)
colliders=$(c blockout.status | sed -n 's/.*collider made \([0-9]*\) times/\1/p')
quiet model.select level box 0.2 3.4 7.8 9.8 6.6 7.8
cut=$(c model.cut level window 1.4 1.2 4 1)
grep -q "4 faces and" <<<"$cut" || fail "the window's cut did not leave its four sides selected, $cut"
after=$(faces <<<"$cut")
[ "$after" -gt "$before" ] || fail "the window's cut left $after faces of $before"
quiet frames.wait 5
c blockout.status | grep -q "collider made $((colliders + 1)) times" || fail "the collider was not made again after the cut, $(c blockout.status | tail -n 1)"

# Turned round to the wall, the window in it, a half turn at two radians a second.
quiet input.hold ArrowLeft 94
quiet frames.wait 10
shot "$shots/3-window.png"

# Tab opens the modeling window on the level, and closes it again.
quiet input.key Tab
quiet frames.wait 5
c blockout.status | grep -q "modeling window open" || fail "Tab did not open the modeling window"
shot "$shots/4-modeling.png"
quiet input.key Tab
quiet frames.wait 5

# F5 writes the recipe, the window's lines among it.
quiet input.key F5
quiet frames.wait 5
grep -q "^\[blockout\] saved the level to" "$shots/game.log" || fail "F5 did not save the level"
grep -q "^cut window 1.4 1.2 4 1$" "$level" || fail "the saved recipe holds no window's line"

# Back down to the middle of the hall.
walk downstairs
c blockout.status | grep -q "on the ground floor" || fail "the walker did not come back down to the ground floor"
shot "$shots/5-down.png"

echo "played to $shots"
