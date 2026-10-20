#!/usr/bin/env bash
# Captures the examples named in a container of Fedora 44, drawn by the Mesa Fedora's own archive
# carries, whose lavapipe draws what the runner's Ubuntu 24.04 cannot, clustered_decals, which Mesa
# 25.2's lavapipe ends with a fault in its rasterizer, and the examples drawn with meshlets and
# Solari. The pack run's fifth capture job runs it for those six on the Linux bridge built with
# --meshlet --solari, and the four shards capture the rest on the runner's own Mesa, which draws
# some scenes several times faster than Fedora's, as BUILDING.md has it.
#
#   build/fetch-bevy-assets.sh
#   build/capture-in-fedora.sh <folder> <example>...
#
# The bridge is the one build/artifacts/linux-x64 holds, the checkout is the container's, and each
# picture goes into the folder as <example>.webp, written by build/capture-example.sh inside. Where
# MESA names a version, as the workflow names it, the job stops at its start if Fedora's Mesa is
# another, since the six's pictures were measured on that one, so a new Mesa is a commit that names
# it and takes the six again rather than a reference that moves on its own.
set -euo pipefail
cd "$(dirname "$0")/.."

if [ $# -lt 2 ]; then
    echo "capture-in-fedora.sh: give a folder and the examples to capture" >&2
    exit 2
fi
out="$1"
shift
runtime=$(command -v podman || command -v docker || true)
if [ -z "$runtime" ]; then
    echo "capture-in-fedora.sh: podman or docker is needed to run Fedora in" >&2
    exit 2
fi
mkdir -p "$out"
out="$(cd "$out" && pwd)"

# Inside, the packages, Mesa's version held to the one named, the examples built with Fedora's .NET,
# and each example captured.
"$runtime" run --rm -i -v "$PWD:/work:z" -v "$out:/out:z" -w /work -e MESA="${MESA:-}" \
    registry.fedoraproject.org/fedora:44 bash -s "$@" <<'INSIDE'
set -u
dnf install -y -q mesa-vulkan-drivers vulkan-loader alsa-lib libwebp-tools ImageMagick dotnet-sdk-10.0 \
    python3 > /dev/null 2>&1 || { echo "capture-in-fedora.sh: Fedora's packages did not install" >&2; exit 1; }
mesa=$(rpm -q --qf '%{VERSION}' mesa-vulkan-drivers)
echo "Fedora 44's Mesa is $mesa"
if [ -n "$MESA" ] && [ "$mesa" != "$MESA" ]; then
    echo "::error title=Mesa changed::Fedora 44's Mesa is $mesa, where the workflow names $MESA, which the pictures of the examples captured here were measured on. Take them again on $mesa, and name it in the workflow."
    exit 1
fi

export DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1
dotnet build BevyCSharp.Examples -v q --nologo -p:BevyCSharpSkipNativeFreshnessCheck=true \
    || { echo "capture-in-fedora.sh: the examples did not build" >&2; exit 1; }

failed=0
for example in "$@"; do
    started=$SECONDS
    if build/capture-example.sh "$example" "/out/$example.webp"; then
        echo "$example captured in $((SECONDS - started)) s"
    else
        echo "::error title=Capture failed::$example could not be captured in Fedora 44, after $((SECONDS - started)) s"
        failed=1
    fi
done
exit $failed
INSIDE
