#!/usr/bin/env bash
# Drives the feature test's player through the hub and each station of the course through bcs,
# offscreen, and fails at the first station that does not do what it is for, then captures each
# zone drawn rather than played, the render gallery, the light hall and the meadow.
#
#   dotnet build BevyCSharp.FeatureTest
#   build/drive-feature-test.sh [folder the feature test was built to]
#
# The program is started offscreen and serving, with each frame a sixtieth of a second of its clock
# however long the machine took to draw it, so a walk is planned in frames. Each station is tried
# from where `player.put` sets the player down, facing the way the course runs, east, as a teleport
# to the course leaves it, and judged by where `player` says it is after. A failure is said as an
# error annotation with the program's last lines at a warning or worse, and a capture of each
# station is left in the folder SHOTS names, or a temporary one.
set -euo pipefail
cd "$(dirname "$0")/.."

program="${1:-BevyCSharp.FeatureTest/bin/Debug/net10.0}"

# The program holds to a memory cap, eight gigabytes where the environment names none, and
# past it stops and says so, rather than take the machine's memory (MemoryGuard).
export BCS_MEMORY_CAP_GB="${BCS_MEMORY_CAP_GB:-8}"
shots="${SHOTS:-$(mktemp -d)}"
log="$shots/feature-test.log"
ground=-1.2

quiet() { ./bcs command "$@" > /dev/null; }
fail() {
  local said=""
  [ -f "$log" ] && said=$(grep -E 'WARN|ERROR|panicked|Unhandled' "$log" | tail -n 3 | tr '\n' ' ' || true)
  echo "::error title=feature test::$1. ${said}" >&2
  exit 1
}

# The player's place as "x y z", read from the line `player` answers.
where() { ./bcs command player | sed -n 's/^at \([-0-9.]*\) \([-0-9.]*\) \([-0-9.]*\),.*/\1 \2 \3/p'; }
# A piece's place as "x y z", read from its transform's line.
piece() { ./bcs command entity.get "$@" | sed -n 's/^Transform.Translation = //p' | tr ',' ' '; }
# A height above the ground, as a number bcs can be given.
up() { awk -v g="$ground" -v d="$1" 'BEGIN { print g + d }'; }
# Whether one sum compares with another as asked, awk working out both.
holds() { awk "BEGIN { exit !(($1) $2 ($3)) }"; }
# Puts the player at a station's start and waits for it to settle.
at() { quiet player.put "$1" "$2" "$3"; quiet frames.wait 20; }
shot() { ./bcs shot "$shots/$1.png" > /dev/null; }

trap './bcs stop > /dev/null 2>&1 || true' EXIT
./bcs stop > /dev/null 2>&1 || true

(cd "$program" && { ./BevyCSharp.FeatureTest --offscreen --frames 0 --serve; echo "[drive] the feature test exited with $?"; } > "$log" 2>&1 &)
for _ in $(seq 1 90); do ./bcs status 2> /dev/null | grep -q "ready.*BevyCSharp.FeatureTest" && break; sleep 1; done
./bcs status | grep -q "ready.*BevyCSharp.FeatureTest" || fail "the feature test never answered"

quiet app.frametime 0.0166667
quiet frames.wait 30
quiet mode walking
shot 1-hub

# The hub, with a walk north that the turning cube stops, and a jump.
read -r _ y z < <(where)
quiet input.hold W 60
quiet frames.wait 70
read -r _ _ walked < <(where)
holds "$walked" "<" "$z - 2" || fail "the player did not walk north from z $z, and stands at z $walked"
quiet input.key Space
quiet frames.wait 12
read -r _ up _ < <(where)
holds "$up" ">" "$y + 0.6" || fail "a jump took the player only to y $up"
quiet frames.wait 60

# Creative mode's flight on a double tap, rising on Space, and spectator mode leaving it standing.
quiet mode creative
quiet frames.wait 5
quiet input.key Space
quiet frames.wait 4
quiet input.key Space
quiet frames.wait 5
./bcs command player | grep -q flying || fail "a double tap of jump in creative mode did not take off"
quiet input.hold Space 40
quiet frames.wait 45
read -r _ flown _ < <(where)
holds "$flown" ">" "$y + 3" || fail "flying with Space held rose only to y $flown"
quiet mode walking
quiet frames.wait 90
quiet mode spectator
quiet frames.wait 5
./bcs command player | grep -q spectator || fail "the mode command did not set spectator mode"
quiet mode walking

# The course, from its start, facing east along it.
quiet teleport course
quiet frames.wait 20
shot 2-course

# The ramps, each walked east into as far as its landing's middle, three climbed to their landings
# and the one at 60 degrees not. The frames are the landing's distance at the run's 4.5 a second.
lanes=(-19 -15.8 -12.6 -9.4)
frames=(108 68 53 45)
for i in 0 1 2 3; do
  at 48.5 "$ground" "${lanes[$i]}"
  quiet input.hold W "${frames[$i]}"
  quiet frames.wait "$((frames[i] + 5))"
  read -r _ top _ < <(where)
  if [ "$i" -lt 3 ]; then
    holds "$top" ">" "$ground + 1.3" || fail "the ramp in lane ${lanes[$i]} was not climbed, the player at y $top"
  else
    holds "$top" "<" "$ground + 0.8" || fail "the 60 degree ramp was climbed, which is steeper than the character stands on, to y $top"
  fi
done

# Stairs of 0.15 climbed and of 0.4 not, its steps over the character's step height.
at 60.5 "$ground" -19
quiet input.hold W 70
quiet frames.wait 75
read -r _ stairs _ < <(where)
holds "$stairs" ">" "$ground + 1.3" || fail "the stairs of 0.15 were not climbed, the player at y $stairs"
at 60.5 "$ground" -12.6
quiet input.hold W 90
quiet frames.wait 95
read -r _ high _ < <(where)
holds "$high" "<" "$ground + 0.3" || fail "the stairs of 0.4 were climbed to y $high"

# The beam, walked along from its first landing nearly to its last without falling off.
at 76.5 "$(up 1.1)" -8
quiet input.hold W 135
quiet frames.wait 140
read -r beam_x beam_y _ < <(where)
holds "$beam_x" ">" 86 || fail "the beam was not walked along, the player at x $beam_x"
holds "$beam_y" ">" "$ground + 0.8" || fail "the player fell off the beam, at y $beam_y"

# The tunnel, which stops the player standing and lets it through crouched.
at 76 "$ground" -13
quiet input.hold W 90
quiet frames.wait 95
read -r stood _ _ < <(where)
holds "$stood" "<" 79 || fail "the player walked into the tunnel standing, to x $stood"
at 76 "$ground" -13
quiet input.hold W,C 360
quiet frames.wait 365
read -r crawled _ _ < <(where)
holds "$crawled" ">" 86 || fail "the player did not crawl through the tunnel, only to x $crawled"
shot 3-tunnel

# Ice, which keeps some of the speed after the key is let go, where plain ground stops the player at
# once. The frames each command of bcs takes pass as well, so the slide read is the part left of it.
at 52.5 "$(up 0.2)" 0
quiet input.hold W 60
quiet frames.wait 61
read -r let_go _ _ < <(where)
quiet frames.wait 30
read -r slid _ _ < <(where)
holds "$slid" ">" "$let_go + 0.25" || fail "the player stopped on the ice, from x $let_go to $slid"

# The bounce pad, which throws the player high.
at 66 "$(up 0.3)" 0
quiet frames.wait 12
read -r _ thrown _ < <(where)
holds "$thrown" ">" "$ground + 3" || fail "the bounce pad threw the player only to y $thrown"
quiet frames.wait 120

# The pit, whose sensor puts the player back at the course's start.
at 78 "$(up 2)" 0
quiet frames.wait 40
read -r back _ _ < <(where)
holds "$back" "<" 47 || fail "the pit did not put the player back at the course's start, it stands at x $back"

# The moving platform carries the player and the elevator lifts it, each put on where it is now,
# and the disc turns it.
read -r px py pz < <(piece Moving platform)
quiet player.put "$px" "$(awk -v y="$py" 'BEGIN { print y + 0.3 }')" "$pz"
quiet frames.wait 10
read -r before _ _ < <(where)
quiet frames.wait 90
read -r after _ _ < <(where)
holds "$(awk -v a="$after" -v b="$before" 'BEGIN { print (a > b ? a - b : b - a) }')" ">" 1 || fail "the moving platform did not carry the player, from x $before to $after"
read -r ex ey ez < <(piece Elevator)
quiet player.put "$ex" "$(awk -v y="$ey" 'BEGIN { print y + 0.3 }')" "$ez"
quiet frames.wait 10
read -r _ low _ < <(where)
quiet frames.wait 120
read -r _ lifted _ < <(where)
read -r _ ey2 _ < <(piece Elevator)
holds "$(awk -v a="$lifted" -v b="$low" 'BEGIN { print (a > b ? a - b : b - a) }')" ">" 1 || fail "the elevator did not move the player, from y $low to $lifted with the elevator at $ey2"
at 82 "$(up 1)" 10
read -r turn_x _ turn_z < <(where)
quiet frames.wait 90
read -r turned_x _ turned_z < <(where)
holds "$(awk -v a="$turn_x" -v b="$turned_x" -v c="$turn_z" -v d="$turned_z" 'BEGIN { print sqrt((a-b)^2 + (c-d)^2) }')" ">" 1 || fail "the disc did not turn the player"

# The conveyor, which carries the player east with no key held.
at 86 "$(up 0.3)" 10
quiet frames.wait 60
read -r carried _ _ < <(where)
holds "$carried" ">" 88 || fail "the conveyor did not carry the player, it stands at x $carried"

# The pressure plate, whose lamp lights under the player.
at 80 "$(up 0.4)" 24
quiet frames.wait 40
grep -q "\[Playground\] the plate is pressed" "$log" || fail "the pressure plate did not light its lamp under the player"
shot 4-playground

# The terrain, which the player lands on above the ground under it.
at 100 "$(up 8)" 24
quiet frames.wait 120
read -r _ hill _ < <(where)
holds "$hill" ">" "$ground + 0.3" || fail "the player fell through the terrain to y $hill"

# The zones drawn rather than played, from the spectator camera, the gallery's wall and box, each
# bay of the light hall from its aisle, and the meadow. A zone that failed to build ends the program
# or leaves its pieces out, which the first entity asked of each says.
quiet mode spectator
quiet frames.wait 5
view() { quiet look -- "$2" "$3" "$4" "$5" "$6" "$7"; quiet frames.wait "${8:-40}"; shot "$1"; }
for piece in "Gallery wall" "Cornell lamp" "Reflection probe" "Irradiance volume" "Fog volume" "Tree 1, crown"; do
  ./bcs command entity.get "$piece" > /dev/null 2>&1 || fail "the zones were not built, $piece is missing"
done
view 5-gallery -55 4 -3 -63.5 3 -1 60
view 6-cornell -62 2.8 -13 -72 1.8 -13
bay=7
for z in -60 -68 -76 -84; do
  view "$bay-hall-west" 0 1.3 "$z" -10 0.2 "$z"
  view "$((bay + 1))-hall-east" 0 1.3 "$z" 10 0.2 "$z"
  bay=$((bay + 2))
done
view 15-meadow 12 1.5 80 0 0.5 70

# The effects page's ambient occlusion and dusk sky through the setting command, each put back as
# it was after, so a run on the working machine leaves its settings as it found them.
setting() { ./bcs command setting "$1" | sed -n 's/^[A-Za-z]* = "\{0,1\}\([^"]*\)"\{0,1\}$/\1/p'; }
occlusion=$(setting AmbientOcclusion)
backdrop=$(setting Backdrop)
quiet setting "AmbientOcclusion true"
quiet setting "Backdrop Dusk"
view 16-effects -55 4 -3 -63.5 3 -1 60
quiet setting "AmbientOcclusion $occlusion"
quiet setting "Backdrop $backdrop"

# Sponza, where its pack has been fetched, which the push workflows never do, from outside its
# door and from inside its hall.
if ./bcs scenes | awk '$1 == "intel-sponza" && $4 == "fetched" { found = 1 } END { exit !found }'; then
  quiet scene.load intel-sponza
  for _ in $(seq 1 60); do grep -q "Intel Sponza stands" "$log" && break; quiet frames.wait 10; done
  grep -q "Intel Sponza stands" "$log" || fail "Sponza did not load from its pack"
  view 17-sponza 45 4 -45 60 3 -60 120
  view 18-sponza-hall 57 2.5 -57 80 2 -80
  quiet scene.unload
fi

echo "drove the feature test, captures in $shots"
