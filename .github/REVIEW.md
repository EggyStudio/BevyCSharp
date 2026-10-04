# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it already keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read.

Reviewed up to `d058156`. Animation (`f79472b`) and undo for a delete (`d058156`) were read, and
each raised a verdict.

## Now

In this order.

1. **Verdict 1**, since it makes an undo do nothing without saying so.
2. **A project file** (decision 2), which the batch in progress has begun.
3. **Verdict 2.**
4. **Gamepads** wait on the owner (decision 3).

## Verdicts

1. **Undoing a delete leaves the history before it pointing at dead entities** (`d058156`).
   `EditorEntity.Restore` spawns the deleted subtree again, so each entity comes back under a
   new `Entity`, and it repoints the components that referred to the old ones. The history's
   earlier records were not repointed. A field edit, a rename and the other records capture the
   `Entity` they were made on (`ComponentFields.cs:147`, `EditorEntity.cs:39`), so after editing
   a field, deleting the entity and undoing the delete, the next undo writes to an entity that is
   gone and the field keeps its value. The history is to keep the map a restore produces, old
   entity to new, and every record is to resolve its entity through it before it runs, following
   the map through more than one delete and undo. Verified by a test that edits a field, deletes
   the entity, undoes twice and finds the field at its first value, and then redoes all three.
2. **Animation plays the first player under a model and only named clips** (`f79472b`,
   `native/bevy_csharp/src/animation.rs`). `animator` stops at the first `AnimationPlayer` it
   finds under the root, and a glTF file with more than one animated root has a player for each,
   so the rest never move. Clips are taken from `named_animations`, so a clip the file gave no
   name cannot be played, where it could be offered by its number. Both are to work, or to be
   entries in TODO.md's Animation section with what each needs.

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
