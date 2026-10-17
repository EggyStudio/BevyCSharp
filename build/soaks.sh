#!/usr/bin/env bash
# Soaks the games named at once for the seconds given (build/soak.sh) and judges what each held
# (build/soak-check.py), as the pack run does, keeping the readings, the logs, each soak's errors
# and each check's words in build/soak.
#
#   build/soaks.sh <seconds> <courtyard|stress|swarm|feature-test>...
#
# A failure is said as one error carrying each soak's FAILED lines and each value the check found
# climbing, or too few readings to judge, so the page names the program and the figure. The pack run
# of 5e01491e said only that a game grew or could not be played, since its one error came after the
# scripts and build/step.py adds its page only to a step that said none.
set -uo pipefail
cd "$(dirname "$0")/.."

seconds="$1"
shift
mkdir -p build/soak

pids=()
for game in "$@"; do
    build/soak.sh "$game" "$seconds" 2> "build/soak/$game.err" &
    pids+=($!)
done
failed=0
for pid in "${pids[@]}"; do wait "$pid" || failed=1; done

said=""
for game in "$@"; do
    cat "build/soak/$game.err" >&2
    python3 build/soak-check.py "build/soak/$game.txt" > "build/soak/$game.check" || failed=1
    cat "build/soak/$game.check"
    said+=$(grep -h "^FAILED" "build/soak/$game.err"; grep -E "CLIMBS|too few" "build/soak/$game.check")
    said+=$'\n'
done

if [ "$failed" = 1 ]; then
    said=$(sed '/^$/d' <<< "$said")
    said=${said//%/%25}
    echo "::error title=The soak failed::$* played for $seconds seconds, and one grew or could not be played.%0A${said//$'\n'/%0A}"
    exit 1
fi
