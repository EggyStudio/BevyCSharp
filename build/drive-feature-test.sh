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
#
# WEATHER_QUALITY names the weather's quality for the run, the game's own where it names none. The
# workflow names Off, since the weather's kernels take a large share of each frame on its software
# device and the course would not finish within the job's limit under them, and without the weather
# each view waits five frames for its shot where forty let the clouds settle. Rain and snow over the
# playground at the end run the weather's kernels on that device all the same, so a fault of theirs
# on a software device fails the run there.
#
# GLOBAL_ILLUMINATION names global illumination's quality for the run in the same way, the game's
# own where it names none, Medium unless a tester set another. The workflow names Off, since its
# tracing takes a large share of each frame on the runner's software device, as the weather's
# kernels do. Where the run's is on, the light hall's irradiance volume's bay is taken a second time
# with it off.
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

# Each station and view says its name as it ends, with the time it took and the time the run has
# taken, so a job ended at its limit says in its log how far the run went and where the time went.
# The processor is said first, since a software device's pace follows the processor the runner
# draws, which differs from run to run, so a run that reaches the job's limit says which one it had.
since=$SECONDS
clock() { printf '%d:%02d' $(($1 / 60)) $(($1 % 60)); }
took() {
  echo "[course] $1 took $(clock $((SECONDS - since))), the run $(clock "$SECONDS")"
  since=$SECONDS
}
processor=$(sed -n 's/^model name[[:space:]]*: //p' /proc/cpuinfo 2> /dev/null | head -n 1 || true)
echo "[course] on ${processor:-a processor that does not name itself}, $(getconf _NPROCESSORS_ONLN) threads"

# Each command is given as long as its kind takes on lavapipe at the pace of the runner's slower
# processor, and twice that or more, so one that never answers fails the run where it stands, where
# ten minutes for every command would let a hang read as slowness up to the job's limit. A wait for
# frames in the course, and a key held for frames, which answers once they have passed, is given
# three seconds a frame and a minute more, above the longest a frame of the course takes there. A
# wait for frames in a view, a wait for pipelines and the load of a scene are given fifteen minutes,
# since a view's frames and its pipelines' compiles take minutes there. A shot is given five
# minutes, a few of the slowest view's frames, and any other command two minutes, since each answers
# within a frame. Every command before the first wait for pipelines has answered is given fifteen
# minutes too, since with no shader cache of Mesa's, as the runner starts, the start's compiles hold
# each frame for seconds and the hub's first frames take minutes.
#
# A command that fails would say nothing of itself but bcs's exit code, so one that fails says its
# words, its code, its bound and the first line bcs gave of why, as Swarm's play does, and anything
# else that fails is named by the trap below. bcs writes why to its output, as it writes the answer,
# so the output is kept in a file and passed on once the command has worked. The timeout comes
# before the command's words, since bcs passes on whatever follows a `--` as it is. Each command's
# time is written to commands.txt beside the captures.
cold=1
views=0
bound() {
  if [ "$cold" = 1 ]; then echo 900; return; fi
  case "$1" in
    frames.wait | frame.profile) if [ "$views" = 1 ]; then echo 900; else echo $((60 + 3 * $2)); fi ;;
    input.hold) echo $((60 + 3 * $3)) ;;
    pipelines.wait | scene.load) echo 900 ;;
    *) echo 120 ;;
  esac
}
c() {
  local bound from=$SECONDS
  bound=$(bound "$@")
  ./bcs command --timeout "$bound" "$@" > "$said" || fail "./bcs command $* ended with $? within its $bound seconds, $(head -n 1 "$said")"
  if [ "$1" = pipelines.wait ]; then cold=0; fi
  echo "$((SECONDS - from)) $*" >> "$shots/commands.txt"
  cat "$said"
}
quiet() { c "$@" > /dev/null; }

# The player's place as "x y z", read from the line `player` answers.
where() { c player | sed -n 's/^at \([-0-9.]*\) \([-0-9.]*\) \([-0-9.]*\),.*/\1 \2 \3/p'; }
# A piece's place as "x y z", read from its transform's line.
piece() { c entity.get "$@" | sed -n 's/^Transform.Translation = //p' | tr ',' ' '; }
# A setting's value, without the quotes the setting command gives a choice's or a text's in.
setting() { c setting "$1" | sed -n 's/^[A-Za-z]* = "\{0,1\}\([^"]*\)"\{0,1\}$/\1/p'; }
# A height above the ground, as a number bcs can be given.
up() { awk -v g="$ground" -v d="$1" 'BEGIN { print g + d }'; }
# Whether one sum compares with another as asked, awk working out both.
holds() { awk "BEGIN { exit !(($1) $2 ($3)) }"; }
# Puts the player at a station's start and waits for it to settle.
at() { quiet player.put "$1" "$2" "$3"; quiet frames.wait 20; }
# A shot once every pipeline its view asked for has compiled, the clock held still while they do,
# since what is drawn with one compiling is left out of the picture, a view's walls, floor or
# objects missing from it.
shot() {
  quiet pipelines.wait
  ./bcs shot "$shots/$1.png" --timeout 300 > "$said" || fail "./bcs shot $1 ended with $? within its 300 seconds, $(head -n 1 "$said")"
}

trap 'code=$?; echo "FAILED: $BASH_COMMAND ended with $code on line $LINENO" >&2' ERR
trap './bcs stop > /dev/null 2>&1 || true' EXIT
./bcs stop > /dev/null 2>&1 || true

(cd "$program" && { ./BevyCSharp.FeatureTest --offscreen --frames 0 --serve; echo "[course] the feature test exited with $?"; } > "$log" 2>&1 &)
# Three minutes for the feature test to answer, and its answer taken once rather than asked for
# again, since on lavapipe with no shader cache of Mesa's, as the workflow's runner starts, it says
# it is ready at its first frame and then answers nothing for minutes while its pipelines compile,
# which a second asking can read as unreachable (build/soak.sh).
answered=0
for _ in $(seq 1 180); do ./bcs status 2> /dev/null | grep -q "ready.*BevyCSharp.FeatureTest" && { answered=1; break; }; sleep 1; done
[ "$answered" = 1 ] || fail "the feature test never answered"
took "the start"

quiet app.frametime 0.0166667
took "the first command"
adapter=$(sed -n 's/^\[Renderer\] adapter: //p' "$log" | head -n 1)
echo "[course] drawn by ${adapter:-a device the log does not name}"
quiet app.hold commands

# The day held still at eight in the morning, the sun low in the east as the views were framed
# under, so each run captures the same light, and put back as it was at the end, as the effects
# page's settings are.
day_speed=$(c setting DaySpeed | sed -n 's/^DaySpeed = //p')
day_hour=$(c setting Hour | sed -n 's/^Hour = //p')
quiet setting "DaySpeed 0"
quiet day.hour 8

# The weather held to a partly cloudy sky with the forecast off, so each run captures the same sky,
# at the quality WEATHER_QUALITY names, and global illumination at the quality GLOBAL_ILLUMINATION
# names, each put back as it was at the end with the day.
settings_was=()
for name in WeatherKind Procedural WeatherQuality GlobalIllumination; do
  settings_was+=("$name $(c setting "$name" | sed -n "s/^$name = //p")")
done
quiet setting "WeatherKind PartlyCloudy"
quiet setting "Procedural false"
if [ -n "${WEATHER_QUALITY:-}" ]; then quiet setting "WeatherQuality $WEATHER_QUALITY"; fi
if [ -n "${GLOBAL_ILLUMINATION:-}" ]; then quiet setting "GlobalIllumination $GLOBAL_ILLUMINATION"; fi
quality=$(setting WeatherQuality)
illumination=$(setting GlobalIllumination)

quiet frames.wait 30
quiet mode walking
shot 1-hub

# The hub's frame time at the run's weather, beside Sponza's, which on the workflow's software
# renderer measures the weather's cost there. Each is a frame's mean on the machine's clock over 30
# frames, as frame.profile measures it, where the rate app.status says is the set clock's, a
# sixtieth of a second a frame on any device, and less where held frames pass none.
frame_time() {
  c frame.profile 30 | tail -n 1 | sed -n 's/.*frame_ms=\([0-9.]*\).*/\1/p' \
    | awk -v name="$1" '{ printf "%s %.1f ms\n", name, $1 }' >> "$shots/frame-times.txt"
}
frame_time "1-hub, the weather $quality"
took "the hub's shot"

# The hub, with a walk north that the turning cube stops, and a jump. A held key answers once its
# frames have passed, the clock held still between commands, so the wait after each hold is the
# settling alone, where a wait of the hold's frames again would run the course's walking frames
# twice, a large share of the job's time on the runner's software device.
read -r _ y z < <(where)
quiet input.hold W 60
quiet frames.wait 10
read -r _ _ walked < <(where)
holds "$walked" "<" "$z - 2" || fail "the player did not walk north from z $z, and stands at z $walked"
quiet input.key Space
quiet frames.wait 12
read -r _ up _ < <(where)
holds "$up" ">" "$y + 0.6" || fail "a jump took the player only to y $up"
quiet frames.wait 60
took "the hub's walk and jump"

# Creative mode's flight on a double tap, rising on Space, and spectator mode leaving it standing.
quiet mode creative
quiet frames.wait 5
quiet input.key Space
quiet frames.wait 4
quiet input.key Space
quiet frames.wait 5
c player | grep -q flying || fail "a double tap of jump in creative mode did not take off"
quiet input.hold Space 40
quiet frames.wait 5
read -r _ flown _ < <(where)
holds "$flown" ">" "$y + 3" || fail "flying with Space held rose only to y $flown"
quiet mode walking
quiet frames.wait 90
quiet mode spectator
quiet frames.wait 5
c player | grep -q spectator || fail "the mode command did not set spectator mode"
quiet mode walking
took "the modes"

# The course, from its start, facing east along it.
quiet teleport course
quiet frames.wait 20
shot 2-course
took "the course's start"

# The ramps, each walked east into as far as its landing's middle, three climbed to their landings
# and the one at 60 degrees not. The frames are the landing's distance at the run's 4.5 a second.
# The steep one is read fifty frames after the push, since a character pushed up a slope steeper
# than it stands on slides back down over them, and a reading soon after the push finds it partway
# up.
lanes=(-19 -15.8 -12.6 -9.4)
frames=(108 68 53 45)
for i in 0 1 2 3; do
  at 48.5 "$ground" "${lanes[$i]}"
  quiet input.hold W "${frames[$i]}"
  if [ "$i" -lt 3 ]; then quiet frames.wait 5; else quiet frames.wait 50; fi
  read -r _ top _ < <(where)
  if [ "$i" -lt 3 ]; then
    holds "$top" ">" "$ground + 1.3" || fail "the ramp in lane ${lanes[$i]} was not climbed, the player at y $top"
  else
    holds "$top" "<" "$ground + 0.8" || fail "the 60 degree ramp was climbed, which is steeper than the character stands on, to y $top"
  fi
done
took "the ramps"

# Stairs of 0.15 climbed and of 0.4 not, its steps over the character's step height.
at 60.5 "$ground" -19
quiet input.hold W 70
quiet frames.wait 5
read -r _ stairs _ < <(where)
holds "$stairs" ">" "$ground + 1.3" || fail "the stairs of 0.15 were not climbed, the player at y $stairs"
at 60.5 "$ground" -12.6
quiet input.hold W 90
quiet frames.wait 5
read -r _ high _ < <(where)
holds "$high" "<" "$ground + 0.3" || fail "the stairs of 0.4 were climbed to y $high"
took "the stairs"

# The beam, walked along from its first landing nearly to its last without falling off.
at 76.5 "$(up 1.1)" -8
quiet input.hold W 135
quiet frames.wait 5
read -r beam_x beam_y _ < <(where)
holds "$beam_x" ">" 86 || fail "the beam was not walked along, the player at x $beam_x"
holds "$beam_y" ">" "$ground + 0.8" || fail "the player fell off the beam, at y $beam_y"
took "the beam"

# The tunnel, which stops the player standing and lets it through crouched.
at 76 "$ground" -13
quiet input.hold W 90
quiet frames.wait 5
read -r stood _ _ < <(where)
holds "$stood" "<" 79 || fail "the player walked into the tunnel standing, to x $stood"
at 76 "$ground" -13
quiet input.hold W,C 360
quiet frames.wait 5
read -r crawled _ _ < <(where)
holds "$crawled" ">" 86 || fail "the player did not crawl through the tunnel, only to x $crawled"
shot 3-tunnel
took "the tunnel"

# Ice, which keeps some of the speed after the key is let go, where plain ground stops the player at
# once. The wait after the walk is part of the slide, so the slide read is the part left of it.
at 52.5 "$(up 0.2)" 0
quiet input.hold W 60
quiet frames.wait 1
read -r let_go _ _ < <(where)
quiet frames.wait 30
read -r slid _ _ < <(where)
holds "$slid" ">" "$let_go + 0.25" || fail "the player stopped on the ice, from x $let_go to $slid"
took "the ice"

# The bounce pad, which throws the player high.
at 66 "$(up 0.3)" 0
quiet frames.wait 12
read -r _ thrown _ < <(where)
holds "$thrown" ">" "$ground + 3" || fail "the bounce pad threw the player only to y $thrown"
quiet frames.wait 120
took "the bounce pad"

# The pit, whose sensor puts the player back at the course's start.
at 78 "$(up 2)" 0
quiet frames.wait 40
read -r back _ _ < <(where)
holds "$back" "<" 47 || fail "the pit did not put the player back at the course's start, it stands at x $back"
took "the pit"

# The moving platform carries the player and the elevator lifts it, each put on where it is now,
# and the disc turns it.
#
# The platform and the elevator swing back and forth, and each is judged by how far the player's
# place ranged over a count of frames, read every fifteen, long enough that the swing covers more
# than a meter wherever in it the run comes to it, the platform's 3.5 meters each way at 0.6 a
# second over 180 frames and the elevator's 3.4 at 0.5 over 240. Two readings a set count of frames
# apart would read the swing at whatever point the run came to it, which every wait before them
# moves, and can find the player back where it stood at a turn of the swing.
ranged() {
  local axis=$1 x y z low high
  read -r x y z < <(where) || return 1
  low=${!axis}
  high=$low
  for _ in $(seq 1 $(($2 / 15))); do
    quiet frames.wait 15
    read -r x y z < <(where) || return 1
    low=$(awk -v a="$low" -v b="${!axis}" 'BEGIN { print (b < a ? b : a) }')
    high=$(awk -v a="$high" -v b="${!axis}" 'BEGIN { print (b > a ? b : a) }')
  done
  awk -v a="$high" -v b="$low" 'BEGIN { print a - b }'
}
read -r px py pz < <(piece Moving platform)
quiet player.put "$px" "$(awk -v y="$py" 'BEGIN { print y + 0.3 }')" "$pz"
quiet frames.wait 10
carried=$(ranged x 180)
holds "$carried" ">" 1 || fail "the moving platform did not carry the player, whose place ranged over $carried in 180 frames"
took "the moving platform"
read -r ex ey ez < <(piece Elevator)
quiet player.put "$ex" "$(awk -v y="$ey" 'BEGIN { print y + 0.3 }')" "$ez"
quiet frames.wait 10
lifted=$(ranged y 240)
read -r _ ey2 _ < <(piece Elevator)
holds "$lifted" ">" 1 || fail "the elevator did not move the player, whose place ranged over $lifted in 240 frames, the elevator at y $ey2"
took "the elevator"
at 82 "$(up 1)" 10
read -r turn_x _ turn_z < <(where)
quiet frames.wait 90
read -r turned_x _ turned_z < <(where)
holds "$(awk -v a="$turn_x" -v b="$turned_x" -v c="$turn_z" -v d="$turned_z" 'BEGIN { print sqrt((a-b)^2 + (c-d)^2) }')" ">" 1 || fail "the disc did not turn the player"
took "the disc"

# The conveyor, which carries the player east with no key held.
at 86 "$(up 0.3)" 10
quiet frames.wait 60
read -r carried _ _ < <(where)
holds "$carried" ">" 88 || fail "the conveyor did not carry the player, it stands at x $carried"
took "the conveyor"

# The pressure plate, whose lamp lights under the player.
at 80 "$(up 0.4)" 24
quiet frames.wait 40
grep -q "\[Playground\] the plate is pressed" "$log" || fail "the pressure plate did not light its lamp under the player"
shot 4-playground
took "the pressure plate"

# The playground's mound, whose top the player lands on three meters over the ground.
at 100 "$(up 8)" 29
quiet frames.wait 120
read -r _ hill _ < <(where)
holds "$hill" ">" "$ground + 2.5" || fail "the player fell through the mound's top to y $hill"
took "the mound"

# The zones drawn rather than played, from the spectator camera, the gallery's spheres, its row of
# modeled primitives, its room cut by booleans and its box, each bay of the light hall from its
# aisle, and the meadow. A zone that failed to build ends the program or leaves its pieces out,
# which the first entity asked of each says.
# The mode's banner stays a second and a half of the clock, ninety frames, so it is waited out
# before the first view.
quiet mode spectator
quiet frames.wait 90
# Each view waits forty frames for its shot, or as many as it names, so the clouds and global
# illumination's probes settle after the camera's jump, and five without the weather and global
# illumination where it names none. The effects view names sixty, since five leave its floor and its
# spheres streaked after the page switched the dusk and the occlusion on. The view's pipelines are
# waited for before its frames, the clock still while they compile, as well as before its shot, so
# the frames it settles over draw all it holds, and the compile's time, a frame of seconds on
# lavapipe while one runs, falls in the wait given for it. From here each wait for frames is a
# view's, given a view's bound (above).
views=1
settle() { if [ -z "$1" ] && [ "$quality" = Off ] && [ "$illumination" = Off ]; then echo 5; else echo "${1:-40}"; fi; }
view() {
  quiet look -- "$2" "$3" "$4" "$5" "$6" "$7"
  quiet pipelines.wait
  quiet frames.wait "$(settle "${8:-}")"
  shot "$1"
  took "the view $1"
}
for piece in "Clearcoat sphere 1" "Cornell lamp" "Modeled arch" "Room door" "Reflection probe" "Irradiance volume" "Fog volume" "quaking-aspen 70"; do
  ./bcs command --timeout 120 entity.get "$piece" > "$said" || fail "the zones were not built, $piece is missing, $(head -n 1 "$said")"
done
took "the zones' pieces"
view 5-gallery -55 4 -3 -63.5 3 -1 60
view 5-models -57.1 2.6 0.5 -57.1 -0.6 9
view 5-room -67.2 1.2 5 -67.4 0.3 13
view 6-cornell -62 2.8 -13 -72 1.8 -13
bay=7
for z in -28 -36 -44 -52; do
  view "$bay-hall-west" 0 1.3 "$z" -10 0.2 "$z"
  view "$((bay + 1))-hall-east" 0 1.3 "$z" 10 0.2 "$z"
  bay=$((bay + 2))
done
# The irradiance volume's bay again with global illumination off where the run's is on, through the
# setting the graphics page's row changes, since the probes' light stands in for the volume's
# there, and the bay is there to show the volume's.
if [ "$illumination" != Off ]; then
  quiet setting "GlobalIllumination Off"
  view 10-hall-east-gi-off 0 1.3 -36 10 0.2 -36
  quiet setting "GlobalIllumination $illumination"
fi
view 15-meadow 12 1.5 80 0 0.5 70

# The effects page's ambient occlusion and dusk sky through the setting command, each put back as it
# was after, so a run leaves the settings of the machine it runs on as it found them.
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

# Night over the hub, the moon and the stars over it.
quiet day.hour 23
view 20-night 0 3 -20 0 16 20 60

# Rain and then snow over the playground in the morning, thirty frames each at the run's quality for
# the weather, or the game's Medium where the run is without it, so the clouds', the fog's and the
# precipitation's kernels run on whatever device draws the drive, and the program ending in them
# fails the next command. The weather is put off and on around each kind, which then comes at once
# rather than easing in over twenty seconds of the clock.
quiet day.hour 8
on=$quality
if [ "$quality" = Off ]; then on=Medium; fi
weathered() {
  quiet setting "WeatherQuality Off"
  quiet setting "WeatherKind $2"
  quiet setting "WeatherQuality $on"
  quiet look -- 70 8 36 88 1 12
  quiet pipelines.wait
  quiet frames.wait 30
  shot "$1"
  took "the view $1"
}
weathered 21-rain Rain
weathered 22-snow Snow

# The day, the weather and global illumination put back as they were.
quiet setting "DaySpeed $day_speed"
quiet setting "Hour $day_hour"
for was in "${settings_was[@]}"; do quiet setting "$was"; done
took "the settings put back"

# The most memory the run held, beside the frame times, so a run that climbed toward its cap shows
# in what it kept before a run passes the cap and is stopped.
c memory | tr ' ' '\n' | awk 'last == "peak" { printf "peak %.2f GB\n", $1 / 2^30 } { last = $1 }' \
  | tee -a "$shots/frame-times.txt"

echo "ran the feature test's course, captures in $shots"
