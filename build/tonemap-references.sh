#!/usr/bin/env bash
# Writes the tonemapper references in BevyCSharp.Tests/references/tonemapping again, SHARED.md's
# ramp drawn through each of Bevy's eight tonemappers and kept as an eight-bit sRGB PNG named as
# Bevy names the tonemapper, once a change to what one draws is meant. 3DEngine's tonemappers are
# held to these pictures, so a change here is offered to it under Replies in REVIEW.md. Needs a
# bridge with the renderer and slangc (build/fetch-slang.sh).
set -euo pipefail
cd "$(dirname "$0")/.."

BCS_WRITE_TONEMAP_REFERENCES=1 dotnet test BevyCSharp.Tests/BevyCSharp.Tests.csproj --filter "FullyQualifiedName~TonemapRampTests" -v q
git --no-pager diff --stat -- BevyCSharp.Tests/references/tonemapping
