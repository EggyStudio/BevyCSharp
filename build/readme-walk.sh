#!/usr/bin/env bash
# Follows the README as a stranger would, in a container holding the .NET SDK and nothing of this
# repository but the packed packages: the template installed and a game made from it as Install
# says, built, and run with no display as Install says. A step the README leaves out or gets wrong
# fails here.
#
#   dotnet pack BevyCSharp/BevyCSharp.csproj -c Release -p:Version=1.2.3   # into build/package
#   build/pack-templates.sh 1.2.3
#   build/readme-walk.sh [version]
#
# The game the template makes is held to the README's first program, the first C# block under the
# title and the one under "In Program.cs", taken out of README.md rather than written down here, so
# the README shows what a newcomer's first command makes.
# CONTAINER names the container tool, podman or docker, whichever is found otherwise.
set -euo pipefail
cd "$(dirname "$0")/.."

# The engine's package packed last unless one is named, since a version number says nothing about
# which of the packages lying in build/package was made from this checkout.
version="${1:-$(ls -t build/package/BevyCSharp.[0-9]*.nupkg | head -1 | sed 's/.*BevyCSharp\.\([0-9].*\)\.nupkg/\1/')}"
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

cp "build/package/BevyCSharp.$version.nupkg" "build/package/BevyCSharp.Templates.$version.nupkg" "$work/"

# The steps as Install gives them, with the template installed from its package rather than from
# nuget.org and the engine taken from a folder of its own, which the template's --package-folder
# writes a nuget.config for, then what Install says Linux needs, which this image lacks: ALSA's
# library and a Vulkan driver that draws on the processor. Run with no display, it ends after 120
# frames.
cat > "$work/walk.sh" <<WALK
set -euo pipefail
export DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p /packages && cp /given/BevyCSharp.$version.nupkg /packages/

dotnet new install /given/BevyCSharp.Templates.$version.nupkg >/dev/null
dotnet new bevycsharp -o /game --package-folder /packages >/dev/null
cd /game
diff -u /given/Spin.cs Spin.cs
diff -u /given/Program.cs Program.cs
echo "[walk] the template made the README's program"

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
