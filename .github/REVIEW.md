# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it already keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read.

Reviewed up to `9dc91a6`. The editor opened on a game's own folder, with the commands the level
was built through, was read by its description and raised nothing.

## Now

In this order.

1. **One small game, made the way a user would make it.** Every part has tests of its own, and
   nothing has gone the whole way from an empty project to a game somebody else can run. A new
   project outside this repository's references, on the packed package: its level built in the
   editor, driven through `./bcs` so the steps can be repeated (entities placed, a model placed
   as an instance, materials and lights set, the scene saved), behaviors written as scripts and
   reloaded while it runs, an animated model the player moves, physics bodies with contacts that
   score, a sound, a HUD and a pause menu in `Ui`, menu, play and pause as states, a save and a
   load through `SaveGame`, the startup scene named in `project.json`, then Play from the editor
   and an export with its assets in a pack, run from the exported folder. It stays small, a
   room and a goal. Everything that had to be worked around, looked up in the engine's source or
   could not be done is written down as it is met. Each becomes a fix in the batch when it is
   small and an entry in TODO.md when it is not, and the report lists them. The game is kept
   under `games/` and played offscreen by the test workflow where the bridge allows.
2. **What the game turned up**, in the order it hurt.
3. **Gamepads** wait on the owner (decision 2).

## Verdicts

None open.

## Decisions

1. **Commits stay local.** The owner pushes `main` from their own tools, and the working session
   commits and does not push, as COMMITS.md and CLAUDE.md say. An earlier entry here that allowed
   pushing is withdrawn by the owner.
2. **Gamepads are proposed for the render profile, and the owner decides.** TODO.md excludes
   `bevy_gilrs` because it needs libudev headers when the bridge is built on Linux. The render
   profile already takes ALSA for audio on the same terms, with `build-native.sh` installing it
   in the container and naming the package for a local build, and the headless profile would
   still build with a C compiler alone. A game engine with no gamepad is a larger cost than one
   more package in a profile that has one. Nothing is done on this until the owner says so in
   the working session.

## Replies
