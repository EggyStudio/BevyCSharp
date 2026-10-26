#!/usr/bin/env bash
# Takes the feature test's player through the hub and each station of the course through bcs,
# offscreen, and fails at the first station that does not do what it is for, then captures each
# zone drawn rather than played, the render gallery, the light hall and the meadow.
#
#   dotnet build games/FeatureTest
#   build/drive-feature-test.sh [folder the feature test was built to]
#
# The program is started offscreen and serving, with each frame a sixtieth of a second of its clock
# however long the machine took to draw it, so a walk is planned in frames, and the clock held still
# between commands, so it moves only while one waits and each command finds the game at the same
# moment of its clock on any device, however many frames came before it. Each station is tried
# from where `player.put` sets the player down, facing the way the course runs, east, as a teleport
# to the course leaves it, and judged by where `player` says it is after. A failure is said as an
# error annotation with the program's last lines at a warning or worse, and a capture of each
# station is left in the folder SHOTS names, or a temporary one.
set -eEuo pipefail
cd "$(dirname "$0")/.."

program="${1:-games/FeatureTest/bin/Debug/net10.0}"

# The program holds to a memory cap, eight gigabytes where the environment names none, and
# past it stops and says so, rather than take the machine's memory (MemoryGuard).
export BCS_MEMORY_CAP_GB="${BCS_MEMORY_CAP_GB:-8}"
shots="${SHOTS:-$(mktemp -d)}"
log="$shots/feature-test.log"
said="$shots/said.txt"
ground=-1.2

fail() {
  local logged=""
  [ -f "$log" ] && logged=$(grep -E 'WARN|ERROR|panicked|Unhandled' "$log" | tail -n 3 | tr '\n' ' ' || true)
  echo "::error title=feature test::${1%.}. ${logged}" >&2
  exit 1
}

# Every command waits as long as ten minutes for its answer, where bcs waits thirty seconds, since
# most of them wait for frames, as many as 365 for the crawl through the tunnel, and a software
# device, as the workflow's runner has, draws the hub under its weather a few frames a second. The
# pack run of d2a20b6a ended here on a command's timeout with nothing said but bcs's exit code, so
# one that fails says its words, its code and the first line bcs gave of why, as Swarm's play does,
# and anything else that fails is named by the trap below. bcs writes why to its output, as it
# writes the answer, so the output is kept in a file and passed on once the command has worked. The
# timeout comes before the command's words, since bcs passes on whatever follows a `--` as it is.
c() { ./bcs command --timeout 600 "$@" > "$said" || fail "./bcs command $* ended with $?, $(head -n 1 "$said")"; cat "$said"; }
quiet() { c "$@" > /dev/null; }

# The player's place as "x y z", read from the line `player` answers.
where() { c player | sed -n 's/^at \([-0-9.]*\) \([-0-9.]*\) \([-0-9.]*\),.*/\1 \2 \3/p'; }
# A piece's place as "x y z", read from its transform's line.
piece() { c entity.get "$@" | sed -n 's/^Transform.Translation = //p' | tr ',' ' '; }
# A height above the ground, as a number bcs can be given.
up() { awk -v g="$ground" -v d="$1" 'BEGIN { print g + d }'; }
# Whether one sum compares with another as asked, awk working out both.
holds() { awk "BEGIN { exit !(($1) $2 ($3)) }"; }
# Puts the player at a station's start and waits for it to settle.
at() { quiet player.put "$1" "$2" "$3"; quiet frames.wait 20; }
shot() { ./bcs shot "$shots/$1.png" --timeout 600 > "$said" || fail "./bcs shot $1 ended with $?, $(head -n 1 "$said")"; }

trap 'code=$?; echo "FAILED: $BASH_COMMAND ended with $code on line $LINENO" >&2' ERR
trap './bcs stop > /dev/null 2>&1 || true' EXIT
./bcs stop > /dev/null 2>&1 || true

(cd "$program" && { ./BevyCSharp.FeatureTest --offscreen --frames 0 --serve; echo "[course] the feature test exited with $?"; } > "$log" 2>&1 &)
for _ in $(seq 1 90); do ./bcs status 2> /dev/null | grep -q "ready.*BevyCSharp.FeatureTest" && break; sleep 1; done
./bcs status | grep -q "ready.*BevyCSharp.FeatureTest" || fail "the feature test never answered"

quiet app.frametime 0.0166667
quiet app.hold commands

# The day held still at eight in the morning, the sun low in the east as the views were framed
# under, so each run captures the same light, and put back as it was at the end, as the effects
# page's settings are.
day_speed=$(c setting DaySpeed | sed -n 's/^DaySpeed = //p')
day_hour=$(c setting Hour | sed -n 's/^Hour = //p')
quiet setting "DaySpeed 0"
quiet day.hour 8

# The weather held to a partly cloudy sky with the forecast off, so each run captures the same sky,
# and put back as it was at the end with the day.
weather_was=()
for name in WeatherKind Procedural; do
  weather_was+=("$name $(c setting "$name" | sed -n "s/^$name = //p")")
done
quiet setting "WeatherKind PartlyCloudy"
quiet setting "Procedural false"

quiet frames.wait 30
quiet mode walking
shot 1-hub

# The hub's frame time under the weather, beside Sponza's, which on the workflow's software
# renderer measures the weather's cost there. Each is a frame's mean on the machine's clock over 30
# frames, as frame.profile measures it, where the rate app.status says is the set clock's, a
# sixtieth of a second a frame on any device, and less where held frames pass none.
frame_time() {
  c frame.profile 30 | tail -n 1 | sed -n 's/.*frame_ms=\([0-9.]*\).*/\1/p' \
    | awk -v name="$1" '{ printf "%s %.1f ms\n", name, $1 }' >> "$shots/frame-times.txt"
}
frame_time "1-hub, under a partly cloudy sky"

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
c player | grep -q flying || fail "a double tap of jump in creative mode did not take off"
quiet input.hold Space 40
quiet frames.wait 45
read -r _ flown _ < <(where)
holds "$flown" ">" "$y + 3" || fail "flying with Space held rose only to y $flown"
quiet mode walking
quiet frames.wait 90
quiet mode spectator
quiet frames.wait 5
c player | grep -q spectator || fail "the mode command did not set spectator mode"
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
# once. The wait after the walk is part of the slide, so the slide read is the part left of it.
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
#
# The platform and the elevator swing back and forth, and each is judged by how far it took the
# player from where it stood over a set count of frames. The clock stands still between commands,
# so the run comes to each at the same point of its swing on every device. A clock running between
# commands brought each device to another point, and on lavapipe the platform's 90 frames straddled
# the end of its swing, taking the player from x 60.96 to 61.00. A wait added before them moves
# that point, and the count is chosen again where a check then fails.
moved() {
  local axis=$1 x y z start
  read -r x y z < <(where) || return 1
  start=${!axis}
  quiet frames.wait "$2"
  read -r x y z < <(where) || return 1
  awk -v a="${!axis}" -v b="$start" 'BEGIN { d = a - b; print (d < 0 ? -d : d) }'
}
read -r px py pz < <(piece Moving platform)
quiet player.put "$px" "$(awk -v y="$py" 'BEGIN { print y + 0.3 }')" "$pz"
quiet frames.wait 10
carried=$(moved x 90)
holds "$carried" ">" 1 || fail "the moving platform did not carry the player, which went $carried from where it stood"
read -r ex ey ez < <(piece Elevator)
quiet player.put "$ex" "$(awk -v y="$ey" 'BEGIN { print y + 0.3 }')" "$ez"
quiet frames.wait 10
lifted=$(moved y 90)
read -r _ ey2 _ < <(piece Elevator)
holds "$lifted" ">" 1 || fail "the elevator did not move the player, which went $lifted from where it stood, the elevator at y $ey2"
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

# The zones drawn rather than played, from the spectator camera, the gallery's wall, its row of
# modeled primitives, its room cut by booleans and its box, each bay of the light hall from its
# aisle, and the meadow. A zone that failed to build ends the program or leaves its pieces out,
# which the first entity asked of each says.
quiet mode spectator
quiet frames.wait 5
view() { quiet look -- "$2" "$3" "$4" "$5" "$6" "$7"; quiet frames.wait "${8:-40}"; shot "$1"; }
for piece in "Gallery wall" "Cornell lamp" "Modeled arch" "Room door" "Reflection probe" "Irradiance volume" "Fog volume" "quaking-aspen 70"; do
  ./bcs command --timeout 600 entity.get "$piece" > "$said" || fail "the zones were not built, $piece is missing, $(head -n 1 "$said")"
done
view 5-gallery -55 4 -3 -63.5 3 -1 60
view 5-models -57.1 2.6 0.5 -57.1 -0.6 9
view 5-room -66.6 1.2 5 -66.8 0.3 13
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
setting() { c setting "$1" | sed -n 's/^[A-Za-z]* = "\{0,1\}\([^"]*\)"\{0,1\}$/\1/p'; }
occlusion=$(setting AmbientOcclusion)
backdrop=$(setting Backdrop)
quiet setting "AmbientOcclusion true"
quiet setting "Backdrop Dusk"
view 16-effects -55 4 -3 -63.5 3 -1 60
quiet setting "AmbientOcclusion $occlusion"
quiet setting "Backdrop $backdrop"

# Sponza, where its pack has been fetched, which no workflow does, from outside its door and from
# inside its hall, each view's frame time written beside the captures, as build/scenes.sh keeps
# them.
if ./bcs scenes | awk '$1 == "intel-sponza" && $4 == "fetched" { found = 1 } END { exit !found }'; then
  quiet scene.load intel-sponza
  for _ in $(seq 1 60); do grep -q "Intel Sponza stands" "$log" && break; quiet frames.wait 10; done
  grep -q "Intel Sponza stands" "$log" || fail "Sponza did not load from its pack"
  timed() { view "$@"; frame_time "$1"; }
  timed 17-sponza 45 4 -45 60 3 -60 120
  timed 18-sponza-hall 57 2.5 -57 80 2 -80 120
  timed 19-sponza-gallery 74 6.5 -66 62 5.5 -78 120
  quiet scene.unload
fi

# Night over the hub, the moon and the stars over it, and the day put back as it was.
quiet day.hour 23
view 20-night 0 3 -20 0 16 20 60
quiet setting "DaySpeed $day_speed"
quiet setting "Hour $day_hour"
for was in "${weather_was[@]}"; do quiet setting "$was"; done

# The most memory the run held, beside the frame times, so a run that climbed toward its cap shows
# in what it kept before a run passes the cap and is stopped.
c memory | tr ' ' '\n' | awk 'last == "peak" { printf "peak %.2f GB\n", $1 / 2^30 } { last = $1 }' \
  | tee -a "$shots/frame-times.txt"

echo "ran the feature test's course, captures in $shots"
