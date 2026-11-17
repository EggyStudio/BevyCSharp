#!/usr/bin/env bash
# Turns an FBX into glTF with Blender, for build/make-scene-pack.py to pack, Blender run headless in
# a container of Fedora 44 with Blender from Fedora's repository, since a scene's makers may publish
# it as FBX and Python reads no FBX of its own. An FBX fetched from elsewhere is read by a program
# as large as Blender, so the container has no network, the FBX's folder is mounted read-only and
# the out folder alone is writable. The image is built the first time, as bcs-blender:f44, and the
# run says Blender's version, the scene's size and its lights by their kinds (build/fbx-to-gltf.py).
#
#   build/fbx-to-gltf.sh <in.fbx> <out folder>
#
# The glTF points at the textures where the FBX names them, under the FBX's folder as the container
# mounts it, so a pass such as build/orca-gltf.py points them at the folder they lie in here.
set -euo pipefail
cd "$(dirname "$0")/.."

fbx=$(cd "$(dirname "$1")" && pwd)/$(basename "$1")
out=$2
mkdir -p "$out"
out=$(cd "$out" && pwd)

if ! podman image exists bcs-blender:f44; then
  context=$(mktemp -d)
  printf '%s\n' 'FROM registry.fedoraproject.org/fedora:44' \
    'RUN dnf install -y --setopt=install_weak_deps=False blender && dnf clean all' > "$context/Containerfile"
  podman build -t bcs-blender:f44 "$context"
  rm -rf "$context"
fi

name=$(basename "$fbx" .fbx)
podman run --rm --network=none --userns=keep-id -e HOME=/tmp \
  -v "$(dirname "$fbx"):/in:ro,z" -v "$PWD/build:/build:ro,z" -v "$out:/out:z" \
  bcs-blender:f44 blender -b --factory-startup --python /build/fbx-to-gltf.py -- \
  "/in/$name.fbx" "/out/$name.gltf" 2>&1 | grep -E '^[0-9]+ |^no lights' || true
podman run --rm --network=none bcs-blender:f44 rpm -q blender
[ -f "$out/$name.gltf" ] || { echo "Blender wrote no $out/$name.gltf" >&2; exit 1; }
echo "wrote $out/$name.gltf"
