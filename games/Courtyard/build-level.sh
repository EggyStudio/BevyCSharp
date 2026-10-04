#!/usr/bin/env bash
# Builds Courtyard's level in a running editor opened on this project, through bcs, and saves it.
#
#   ./bcs open --editor --offscreen -- --project games/Courtyard
#   games/Courtyard/build-level.sh
#
# From the editor's starting scene (a sun, a ground and a cube) on a project with no level yet.
set -euo pipefail
cd "$(dirname "$0")/../.."

c() { ./bcs command "$@" >/dev/null; }

# A body of the given kind, with a collider of the given shape fitted to what the entity is drawn
# with, which the physics plugin makes into a body when the game runs.
body() {
    c entity.add "$1" RigidBody
    c entity.set "$1" RigidBody.Kind "$2"
    c entity.add "$1" Collider
    c entity.set "$1" Collider.Shape "$3"
}

# The starting cube goes; the ground and the sun stay, the ground a static body of its own triangles
# for the runner to stand on.
c select Cube
c do Entity/Delete
body Ground Static Mesh

# Four walls around a courtyard sixteen units across, meeting at the corners and standing on the
# ground. The cube is a unit a side, so a wall's scale is its size.
for wall in North South East West; do
    c do Spawn/Cube
    c entity.rename Cube "$wall wall"
    c material.set "$wall wall" color "#8a7f72"
    body "$wall wall" Static Box
done
c entity.set "North wall" Transform.Translation "0,-0.7,-8"
c entity.set "North wall" Transform.Scale "16.5,1,0.5"
c entity.set "South wall" Transform.Translation "0,-0.7,8"
c entity.set "South wall" Transform.Scale "16.5,1,0.5"
c entity.set "East wall" Transform.Translation "8,-0.7,0"
c entity.set "East wall" Transform.Scale "0.5,1,16.5"
c entity.set "West wall" Transform.Translation "-8,-0.7,0"
c entity.set "West wall" Transform.Scale "0.5,1,16.5"

# Three coins to collect, each a sensor, which reports a touch and stops nothing, with an id a save
# can name it by.
i=0
for place in "-5,-0.6,-4" "5,-0.6,-3" "0,-0.6,4"; do
    i=$((i + 1))
    c do Spawn/Sphere
    c entity.rename Sphere "Coin $i"
    c entity.set "Coin $i" Transform.Translation "$place"
    c entity.set "Coin $i" Transform.Scale "0.4,0.4,0.4"
    c entity.add "Coin $i" Coin
    c entity.add "Coin $i" SaveId
    body "Coin $i" Static Sphere
    c entity.set "Coin $i" RigidBody.Sensor true
    c material.set "Coin $i" color "#ffcc33"
    c material.set "Coin $i" emissive "#664400"
done

# The goal, a pad in the far corner.
c do Spawn/Cube
c entity.rename Cube Goal
c entity.set Goal Transform.Translation "5,-1.15,-5"
c entity.set Goal Transform.Scale "1.2,0.05,1.2"
c entity.add Goal Goal
body Goal Static Box
c entity.set Goal RigidBody.Sensor true
c material.set Goal color "#33cc66"
c material.set Goal emissive "#115522"

# The runner, the animated model placed as an instance.
c scene.place models/runner.gltf
c entity.rename runner Runner
c entity.set Runner Transform.Translation "0,-1.2,6"
c entity.add Runner Runner
c entity.add Runner Wallet
c entity.add Runner SaveId

c world.save levels/courtyard.scene.json
c setting "Project/Startup scene" levels/courtyard.scene.json
