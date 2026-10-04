# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it already keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read.

Reviewed up to `d67623c`. Sub-states of several parents (`e901c2c`) and the click on an offscreen
scene (`1bfc668`) were read and raised nothing. TODO.md was read whole for the list below.

## Now

TODO.md's remaining entries are mostly limits that were chosen, and three things a game or the
editor needs are either missing from it or held back by a reason that has stopped holding. In
this order.

1. **Skeletal animation.** The README's status section says animation has no bridge, and TODO.md
   has no entry for it, so the largest gap between this engine and a 3D game is on no list. A
   glTF file's clips are loaded by Bevy and nothing plays them. `bevy_animation` is to be
   compiled into the render profile and bridged: the clips of a model by name, playing, stopping,
   pausing, looping, speed, seeking, and blending from one clip to another over a time, on the
   entity a scene was spawned under, with a message when a clip finishes. It lands with a test
   that draws a skinned model at rest and mid-clip and finds the pixels differ, an entry in
   TODO.md for what is left (masks, additive layers, a state machine over clips), and a card or
   a console command in the editor to play a selected model's clips.
2. **Undo for a despawn.** TODO.md says a despawn is not recorded because a mesh built in memory
   could not be brought back. The scene writer has since learned to write a primitive's recipe,
   a mesh's geometry and a material's settings for what was made in memory (TODO.md, The editor,
   first entry), so the subtree a despawn removes can be written to scene JSON as it goes and
   spawned from it on undo, with its ids kept so references to it resolve again. Deleting with
   no way back is the editor's most costly gap for whoever uses it. If something still cannot be
   written, the despawn of that entity is refused from the history with a line in the console
   saying what, and the rest are recorded.
3. **A project file** (decision 2), which the startup scene, the physics step, the export's
   target and the theme are waiting on.
4. **Gamepads** wait on the owner (decision 3).

## Verdicts

None open.

## Decisions

1. **Commits stay local.** The owner pushes `main` from their own tools, and the working session
   commits and does not push, as COMMITS.md and CLAUDE.md say. An earlier entry here that allowed
   pushing is withdrawn by the owner.
2. **A project's settings live in `assets/project.json`.** TODO.md says a project setting has
   nowhere to live and suggests the scene file. A setting of the project is not a property of
   one scene, and a game has several scenes, so it is a file of its own beside them, read through
   `AssetFiles` so that an export carrying its assets in its assembly or a pack carries it too,
   written by the editor's settings tab under a Project heading, and versioned as data assets
   are. It holds the startup scene, the fixed step and the export's choices first. The editor's
   theme is saved there as well when the user asks for it to ship, which closes the entry about a
   theme only the running build has. Recorded in EDITOR.md and PLAY.md.
3. **Gamepads are proposed for the render profile, and the owner decides.** TODO.md excludes
   `bevy_gilrs` because it needs libudev headers when the bridge is built on Linux. The render
   profile already takes ALSA for audio on the same terms, with `build-native.sh` installing it
   in the container and naming the package for a local build, and the headless profile would
   still build with a C compiler alone. A game engine with no gamepad is a larger cost than one
   more package in a profile that has one. Nothing is done on this until the owner says so in
   the working session.

## Replies
