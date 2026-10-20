#!/usr/bin/env bash
# Builds each game under games/ on the version its own project names where nothing sets
# BevyCSharpVersion, from nuget.org alone, as a reader cloning the repository builds it. The pack
# workflow runs it after it pushes a package, naming the version it pushed, and it first waits for
# nuget.org to list that version, since a game's default may be a minimum that resolves to it.
#
#   build/games-on-defaults.sh <version this run pushed>
#
# The wait is bounded, ninety minutes unless GAMES_WAIT_MINUTES says otherwise, since nuget.org
# validates and indexes a package after its push, and the pack run of 3eaa0a44 found the engine's
# 0.5.149 listed 47 minutes after it, the templates within twenty, so the wait is nearly twice what
# it took. A wait that ends with the package unlisted says the package was pushed and stands, so a
# run red here is not read as one that published nothing. A game whose default does not build is
# named in an error with the version this run pushed as the one to raise it to, which is a commit of
# one line a game (BUILDING.md).
set -uo pipefail
cd "$(dirname "$0")/.."

if [ $# -ne 1 ]; then
    echo "games-on-defaults.sh: give the version this run pushed, as in build/games-on-defaults.sh 0.5.3" >&2
    exit 2
fi
version="$1"
minutes="${GAMES_WAIT_MINUTES:-90}"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

listed=false
for _ in $(seq 1 $((minutes * 2))); do
    if curl -fsS https://api.nuget.org/v3-flatcontainer/bevycsharp/index.json 2>/dev/null | grep -q "\"$version\""; then
        listed=true
        break
    fi
    sleep 30
done
if [ "$listed" != true ]; then
    echo "::error title=The games were not built on their defaults::BevyCSharp $version was pushed and stands on nuget.org, which had not listed it within $minutes minutes of its push, so no game was built on its default. Run build/games-on-defaults.sh $version once nuget.org lists it."
    exit 1
fi
echo "BevyCSharp $version is listed on nuget.org"

# Restored into a folder of its own from nuget.org alone, so neither build/package, which holds the
# package this run packed, nor a package another step restored stands in for what a reader is given.
failed=()
for project in games/*/*.csproj; do
    game="$(basename "$(dirname "$project")")"
    # The feature test lives beside the games and is not one, built on the tree's projects unless a
    # version is named, so it names no default to build on (BUILDING.md).
    [ "$game" = FeatureTest ] && continue
    default="$(sed -n "s/.*<BevyCSharpVersion Condition=[^>]*>\([^<]*\)<.*/\1/p" "$project")"
    if dotnet build "$project" -warnaserror -v q --nologo \
        -p:RestoreSources=https://api.nuget.org/v3/index.json -p:RestorePackagesPath="$work/packages" \
        > "$work/$game.log" 2>&1; then
        echo "$game builds on its default, $default"
    else
        echo "$game does not build on its default, $default"
        grep -E "error" "$work/$game.log" | sort -u | head -8
        failed+=("$game names $default and does not build on it")
    fi
done

if [ ${#failed[@]} -gt 0 ]; then
    said=$(printf '%s%%0A' "${failed[@]}")
    echo "::error title=A game does not build on its default::${said}Raise each to $version, the version this run pushed, in its project's BevyCSharpVersion (BUILDING.md)."
    exit 1
fi
