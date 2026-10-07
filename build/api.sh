#!/usr/bin/env bash
# Writes BevyCSharp/PublicApi.txt again from the built library, every public type and member a game
# can reach, once a change to them is meant. The suite fails while the listing and the library
# differ, so a commit that changes the surface carries the change to this file, where it is read.
set -euo pipefail
cd "$(dirname "$0")/.."

BCS_WRITE_API=1 dotnet test BevyCSharp.Tests/BevyCSharp.Tests.csproj --filter "FullyQualifiedName~PublicSurfaceTests.ThePublicSurface" -v q
git --no-pager diff --stat -- BevyCSharp/PublicApi.txt
