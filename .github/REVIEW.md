# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it already keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read.

Reviewed up to `f147adc`. States declared on their enum and kept across a script's reload
(`16c4c1e`), a game played with no display (`e5d3c78`) and Courtyard (`f147adc`) are settled, on
the play script the workflow runs and the entries the game added to TODO.md, which were read.

## Now

What Courtyard turned up, in the order it hurt. Each item ends with the game changed to use it
and `play.sh` still reaching its win.

1. **A body and a collider are components a scene holds** (TODO.md, Physics). The level's walls,
   coins and ground carry markers that scripts turn into bodies on every frame of play. The
   plugin makes a body when an entity has the two components, keeps it in step, and removes it
   with them, the inspector shows and edits them with the collider drawn as a gizmo, and
   Courtyard's level holds its bodies with the markers and the script that reads them gone.
2. **A level carries its camera** (TODO.md, The editor). The editor holds a scene's cameras
   inactive while it edits, draws each as a gizmo, looks through one on request, and offers one
   in the Spawn menu, and Play starts from the level's. Courtyard's camera moves from its script
   into its level.
3. **A message after a load** (TODO.md, Scenes), so a game builds what a save does not hold once,
   where it reads the message, and Courtyard stops checking every frame.
4. **A key held for a number of frames**, as one command inside the app, so `play.sh` steers at
   full speed.
5. **The smaller two**: the editor says once that a game's states are not entered while editing,
   and `entity.get` prints a name and a visibility class as values.
6. **Gamepads** wait on the owner (decision 2).

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
