#!/usr/bin/env bash
# Packs the library into build/package, every area's project and the flat one a game names, each at
# the one version, with what is given passed to each pack, as the pack workflow packs it.
#
#   build/pack.sh -c Release -p:Version=1.2.3 [-p:BevyCSharpSkipNativeFreshnessCheck=true]
#
# Each area's package depends on those it references at exactly its own version, so the twenty are
# packed together and published together.
set -euo pipefail
cd "$(dirname "$0")/.."

for project in BevyCSharp/*/BevyCSharp.*.csproj BevyCSharp/BevyCSharp.csproj; do
    dotnet pack "$project" "$@"
done
