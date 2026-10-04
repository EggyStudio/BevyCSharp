# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it already keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read.

Reviewed up to `e973128`. The physics sync (`ba5cff4`) is settled by its numbers, 6.6 ms to 0.36 ms
at 5,000 bodies. The shadows measured beside Bevy alone (`e973128`, the method read) settle the
question asked: about 8.7 ms is Bevy's own, in a release build with the same scene, and about
2 ms is the bridge's and not yet placed, which TODO.md holds.

## Now

The owner asked for item 1, after the same was done in 3DEngine and run green there. In this
order.

1. **A package made by hand, a version that counts commits, and no markers in commit messages.**
   - `build/version.txt` holds the major and minor, set by the owner, and `build/version.sh`
     prints them with a patch that is the number of commits since that file last changed, so
     each commit raises it and a new minor starts it at 0. 3DEngine's `build/version.sh` is the
     model and can be taken as it is. It replaces the count of every commit over `VersionPrefix`
     in `build.yml` and `republish.yml`. That count is higher than one that starts again, so the
     file starts at a minor above the one published (`0.3` is proposed), and the owner is asked
     in the working session what it holds.
   - A `pack` workflow on `workflow_dispatch`, run from the Actions tab, builds the six bridges
     through `package.yml`, runs the tests on Linux and Windows, packs only once they pass, and
     keeps the `.nupkg` as the run's artifact to download and upload to nuget.org by hand. It
     has a `publish` box, off unless ticked, for pushing it from the run.
   - `[publish]` and `[repack]` are no longer read from commit messages, and a push runs the
     tests on Linux and Windows and nothing else. `republish.yml` goes if `pack` covers what it
     did. CLAUDE.md, COMMITS.md and BUILDING.md lose what they say about the markers in the same
     batch, the CLAUDE.md change with the owner's word in the working session.
   - The workflows name `ubuntu-24.04` in place of `ubuntu-latest`, which becomes Ubuntu 26 from
     2026-10-19, and use actions on a Node version GitHub still runs, since 3DEngine's first runs
     were annotated for both.
   - Verified by `build/version.sh` printing a patch of 0 at the commit that sets the file and 1
     at the next, and by the owner's first `pack` run, whose result they bring back.
2. **A behavior reaches its entity's other components without a crossing each.** A behavior that
   moves a `Transform` pays a call into the bridge an entity, about as much again as its own
   work. The generator already walks a behavior's own component by chunk. It is to hand a method
   the other components it names as parameters from the same chunk, so the common behavior
   crosses once a chunk, and the README's examples use that form. Verified by the movers run
   showing crossings a frame fall from one an entity to a few a chunk.
3. **The 2 ms the bridge adds to the render**, by the render installers added to the plain scene
   one at a time, as `e973128` began.
4. **A character that walls stop**, the first item taken from [SHARED.md](SHARED.md), which
   records what this engine and 3DEngine have in common. Courtyard's runner is a ball pushed by
   its velocity, which rolls, cannot stand on a slope and has no ground to ask about. A character
   controller over `PhysicsWorld`, a capsule moved by a wanted velocity that slides along what it
   meets, rides a low edge, holds a slope up to an angle, jumps and reports whether it stands on
   ground, as a component a scene holds and the inspector edits. 3DEngine's is in its checkout
   beside this one (`3DEngine/Physics/Bepu/PhysicsWorld.Characters.cs`, with its tests), and is
   the idea to take, built this engine's own way. Courtyard's runner becomes one, and `play.sh`
   still reaches its win.
5. **A system run on a move from one state value to a particular other**, 3DEngine's
   `OnTransition`, beside `[OnEnter]` and `[OnExit]`.
6. **A pointer dragged a step a frame by one command**, 3DEngine's `input.drag`, so a script can
   swipe, drag a transform handle or move a panel, which `input.move`, `input.press` and
   `input.release` in separate calls cannot time.
7. **Gamepads** wait on the owner (decision 2).

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
