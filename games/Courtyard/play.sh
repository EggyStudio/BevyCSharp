#!/usr/bin/env bash
# Plays Courtyard through bcs from its menu to the win, with no display, and fails when a step of
# the game does not happen, from the menu and the walk through each coin, the pause, a save and a
# load to the goal.
#
#   dotnet build games/Courtyard
#   games/Courtyard/play.sh [folder the game was built or exported to]
#
# The game is started offscreen and serving, played by holding keys as a person would, and stopped
# at the end whatever happened. Captures of each step are left in the folder named by SHOTS, or
# in a temporary one.
set -eEuo pipefail
cd "$(dirname "$0")/../.."

game="${1:-games/Courtyard/bin/Debug/net10.0}"
shots="${SHOTS:-$(mktemp -d)}"
said="$shots/said.txt"

fail() { echo "FAILED: $*" >&2; grep -h "^\[play\]" "$shots/game.log" >&2 || true; exit 1; }

# Every command waits as long as ten minutes for its answer, where bcs waits thirty seconds, since a
# software device, as the workflow's runner has, draws a few frames a second and a hold or a wait
# runs for as many of them as it names. Swarm's play and the feature test's course run ended pack
# runs on a command's timeout with nothing said but bcs's exit code, so one that fails here says its
# words, its code and the first line bcs gave of why, and anything else that fails is named by the
# trap below. bcs writes why to its output, as it writes the answer, so the output is kept in a file
# and passed on once the command has worked.
c() { ./bcs command "$@" --timeout 600 >"$said" || fail "./bcs command $* ended with $?, $(head -n 1 "$said")"; cat "$said"; }
quiet() { c "$@" >/dev/null; }
shot() { ./bcs shot "$1" --timeout 600 >"$said" || fail "./bcs shot $1 ended with $?, $(head -n 1 "$said")"; }

# Where an entity is, as "x z", from the line entity.get prints its translation on.
where() {
    c entity.get "$1" | sed -n 's/^Transform.Translation = //p' | awk -F, '{ print $1, $3 }'
}

# Walks the runner to a point, holding whichever of WASD points at it for as many frames as the
# nearer of the two distances takes at the runner's speed, a little short so it is not overshot,
# and stops early once a command given after the point succeeds.
go_to() {
    local tx=$1 tz=$2 step x z plan
    shift 2
    for step in $(seq 1 60); do
        [ $# -gt 0 ] && "$@" && return 0
        read -r x z < <(where Runner)
        plan=$(awk -v x="$x" -v z="$z" -v tx="$tx" -v tz="$tz" -v speed=4 'BEGIN {
            k = ""; far = 1000; dx = tx - x; dz = tz - z;
            if (dx > 0.4) { k = k ",D"; far = (dx < far) ? dx : far } else if (dx < -0.4) { k = k ",A"; far = (-dx < far) ? -dx : far }
            if (dz > 0.4) { k = k ",S"; far = (dz < far) ? dz : far } else if (dz < -0.4) { k = k ",W"; far = (-dz < far) ? -dz : far }
            if (k == "") exit;
            frames = int(far / speed * 60 * 0.8);
            if (frames < 1) frames = 1;
            if (frames > 60) frames = 60;
            print substr(k, 2), frames }')
        [ -z "$plan" ] && return 0

        # Unquoted, the keys and the frames are the command's two words.
        quiet input.hold $plan
    done
    fail "the runner did not reach $tx,$tz and stands at $x,$z"
}

coins() { c entity.get Runner | sed -n 's/^Wallet.Coins = //p'; }
carrying() { [ "$(coins)" -ge "$1" ]; }
won() { grep -q "\[courtyard\] won with" "$shots/game.log"; }

trap 'code=$?; echo "FAILED: $BASH_COMMAND ended with $code on line $LINENO" >&2' ERR
trap './bcs stop >/dev/null 2>&1 || true' EXIT

./bcs stop >/dev/null 2>&1 || true
(cd "$game" && { BCS_OFFSCREEN=1 BCS_SERVE=1 ./Courtyard; echo "[play] the game exited with $?"; } >"$shots/game.log" 2>&1 &)
for _ in $(seq 1 90); do ./bcs status 2>/dev/null | grep -q "ready.*Courtyard" && break; sleep 1; done
./bcs status | grep -q "ready.*Courtyard" || fail "the game never answered; its log is $shots/game.log"

# Each frame a sixtieth of a second of the game, however long the machine took to draw it, since a
# walk is planned in frames at the runner's speed. A software renderer, as the workflow's runner
# has, draws a few frames a second, and on the machine's clock each held frame carried the runner
# several times as far as planned, past every coin and into the wall.
quiet app.frametime 0.0166667
quiet frames.wait 30
shot "$shots/1-menu.png"
c entity.get Runner | grep -q "^CharacterController" && fail "the runner is a character before play starts"

quiet input.key Enter
quiet frames.wait 10
c entity.get Runner | grep -q "^CharacterController" || fail "Enter did not start play"

# The coins nearest first, and a save made after the first.
go_to 0 4 carrying 1
quiet frames.wait 5
[ "$(coins)" = 1 ] || fail "the first coin was not picked up"
shot "$shots/2-one-coin.png"

quiet input.key F5
quiet frames.wait 5

go_to -5 -4 carrying 2
quiet frames.wait 5
[ "$(coins)" = 2 ] || fail "the second coin was not picked up"

# Paused, the runner holds still with W down.
quiet input.key Escape
quiet frames.wait 5
shot "$shots/3-paused.png"
read -r before _ < <(where Runner)
quiet input.hold D 20
read -r after _ < <(where Runner)
[ "$before" = "$after" ] || fail "the runner walked while paused, from $before to $after"
quiet input.key Escape
quiet frames.wait 5

# The load puts the game back as the save had it, with one coin.
quiet input.key F9
quiet frames.wait 10
[ "$(coins)" = 1 ] || fail "the load did not bring back the save's one coin"
shot "$shots/4-loaded.png"

# From the save, with the first coin gone and the runner back where the level puts it. The last
# coin is come at from the side away from the goal, which it is beside, so the runner does not
# reach the goal on the way.
go_to -5 -4 carrying 2
go_to 5 -1 carrying 3
go_to 5 -3 carrying 3
quiet frames.wait 5
[ "$(coins)" = 3 ] || fail "the coins after the load were not picked up"

go_to 5 -5 won
quiet frames.wait 10
grep -q "\[courtyard\] won with 3 coins" "$shots/game.log" || fail "reaching the goal with every coin did not win"
shot "$shots/5-won.png"

echo "played to $shots"
