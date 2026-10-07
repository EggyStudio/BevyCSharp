#!/usr/bin/env bash
# Builds every step of docs/first-game.md's game, each a whole program under games/FirstGame/steps,
# in a copy of the game's project against the package in build/package, and runs it offscreen for
# sixty frames, so no step the page shows can stop compiling or starting. With --shots it also
# plays each through ./bcs, walking the player to the right as the step's picture shows, and
# writes the picture into .github/assets/first-game.
#
#   build/first-game.sh [package version] [--shots]
set -euo pipefail
cd "$(dirname "$0")/.."
root="$PWD"
version="${1:-}"
shots="${2:-}"
work="$(mktemp -d)"
trap './bcs stop --name Coins >/dev/null 2>&1 || true; rm -rf "$work"' EXIT

fail() { echo "FAILED: step $1: $2" >&2; exit 1; }
c() { ./bcs command "$@" --name Coins --timeout 600 >/dev/null; }

for step in games/FirstGame/steps/*.cs; do
    n="$(basename "$step" .cs)"
    project="$work/Coins"
    rm -rf "$project"
    mkdir -p "$project"
    cp games/FirstGame/Coins.csproj "$project/"
    cp -r games/FirstGame/assets "$project/"
    sed "s#../../build/package#$root/build/package#" games/FirstGame/nuget.config >"$project/nuget.config"
    cp "$step" "$project/Program.cs"
    echo "step $n"

    args=(-warnaserror -v q --nologo)
    [ -n "$version" ] && args+=(-p:BevyCSharpVersion="$version")
    dotnet build "$project" "${args[@]}" >"$work/build.log" 2>&1 || { cat "$work/build.log"; fail "$n" "it did not build"; }

    out="$project/bin/Debug/net10.0"
    (cd "$out" && BCS_OFFSCREEN=1 BCS_FRAMES=60 ./Coins >run.log 2>&1) || { cat "$out/run.log"; fail "$n" "it did not run"; }
    if grep -E "ERROR|failed:|panicked|Unhandled exception" "$out/run.log"; then fail "$n" "it logged an error"; fi

    if [ "$shots" = "--shots" ]; then
        (cd "$out" && { BCS_OFFSCREEN=1 BCS_SERVE=1 ./Coins >serve.log 2>&1 & })
        for _ in $(seq 1 60); do ./bcs status 2>/dev/null | grep -q "ready.*Coins" && break; sleep 1; done
        c app.frametime 0.0166667
        c frames.wait 60
        # A step where the player walks is shown after a walk to the right, onto the first coin,
        # and a step where the game is won is shown won.
        case "$n" in 01 | 02 | 03) ;; *) c input.hold D 60 ;; esac
        case "$n" in 11 | 12) c state.set Mode Won ;; esac
        c frames.wait 10
        ./bcs shot "$work/$n.png" --name Coins >/dev/null
        ./bcs stop --name Coins >/dev/null
        mkdir -p .github/assets/first-game
        magick "$work/$n.png" -resize 640x360 -quality 80 -strip ".github/assets/first-game/$n.webp"
    fi
done
echo "every step of the first game built and ran"
