#!/usr/bin/env bash
# Follows the README as a stranger would, in a container holding the .NET SDK and nothing of this
# repository but a packed package: a new project, the package added as Install says, the README's
# first program as it is written, built, and run with no display as Install says. A step the
# README leaves out or gets wrong fails here.
#
#   dotnet pack BevyCSharp/BevyCSharp.csproj -c Release -p:Version=1.2.3   # into build/package
#   build/readme-walk.sh [version]
#
# The program is taken out of README.md rather than written down here, the first C# block under
# the title and the one under "In Program.cs", so the walk follows the README as it reads now.
# CONTAINER names the container tool, podman or docker, whichever is found otherwise.
set -euo pipefail
cd "$(dirname "$0")/.."

version="${1:-$(ls build/package/BevyCSharp.*.nupkg | sed 's/.*BevyCSharp\.\(.*\)\.nupkg/\1/' | sort -V | tail -1)}"
engine="${CONTAINER:-$(command -v podman || command -v docker)}"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

# The first program, and what Program.cs holds, as the README shows them.
awk '
    /^```csharp/ && !done { inside = 1; next }
    /^```/ && inside { inside = 0; done = 1; next }
    inside { print }
' README.md > "$work/Spin.cs"

awk '
    /^In Program.cs/ { after = 1; next }
    after && /^```csharp/ { inside = 1; next }
    inside && /^```/ { exit }
    inside { print }
' README.md > "$work/Program.cs"

[ -s "$work/Spin.cs" ] && [ -s "$work/Program.cs" ] || { echo "the README's first program was not found" >&2; exit 1; }

cp "build/package/BevyCSharp.$version.nupkg" "$work/"

# The steps as Install gives them, with the package in a folder of its own as the only source
# besides nuget.org, then what Install says Linux needs, which this image lacks: ALSA's library and
# a Vulkan driver that draws on the processor. Run with no display, it ends after 120 frames.
cat > "$work/walk.sh" <<WALK
set -euo pipefail
export DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p /packages && cp /given/*.nupkg /packages/

dotnet new console -o /game --framework net10.0 >/dev/null
cd /game
dotnet nuget add source /packages --name local >/dev/null
dotnet add package BevyCSharp --version '$version' >/dev/null
cp /given/Spin.cs Spin.cs
cp /given/Program.cs Program.cs

dotnet build -c Release
echo '[walk] built'

apt-get update -qq >/dev/null
apt-get install -y -qq --no-install-recommends mesa-vulkan-drivers libvulkan1 >/dev/null
apt-get install -y -qq --no-install-recommends libasound2t64 >/dev/null 2>&1 \
    || apt-get install -y -qq --no-install-recommends libasound2 >/dev/null

BCS_OFFSCREEN=1 BCS_FRAMES=120 timeout 180 dotnet run -c Release --no-build
echo '[walk] ran 120 frames with no display'
WALK

"$engine" run --rm -v "$work:/given:ro,z" mcr.microsoft.com/dotnet/sdk:10.0 bash /given/walk.sh
