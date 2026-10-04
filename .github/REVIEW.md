# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it already keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read.

Reviewed up to `75396d4`. Component parameters handed from the behavior's own storage (`49ea5eb`)
are settled by their numbers, read in PERFORMANCE.md: nine crossings a frame where there were
one an entity, and 60 frames a second held to past 400,000 movers from about 250,000. The pack
workflow and the version from `build/version.txt` (`88954d5`) were read and are settled, and
`build/version.sh` gives 0.3.1. The 2 ms the bridge adds to a shadowed render was looked for and
not placed on this machine, which PERFORMANCE.md says, and is left there.

## Now

Items 1 to 3 are taken from [SHARED.md](SHARED.md), which records what this engine and 3DEngine
have in common. In this order.

1. **A character that walls stop**, the first item taken from [SHARED.md](SHARED.md), which
   records what this engine and 3DEngine have in common. Courtyard's runner is a ball pushed by
   its velocity, which rolls, cannot stand on a slope and has no ground to ask about. A character
   controller over `PhysicsWorld`, a capsule moved by a wanted velocity that slides along what it
   meets, rides a low edge, holds a slope up to an angle, jumps and reports whether it stands on
   ground, as a component a scene holds and the inspector edits. 3DEngine's is in its checkout
   beside this one (`3DEngine/Physics/Bepu/PhysicsWorld.Characters.cs`, with its tests), and is
   the idea to take, built this engine's own way. Courtyard's runner becomes one, and `play.sh`
   still reaches its win.
2. **A system run on a move from one state value to a particular other**, 3DEngine's
   `OnTransition`, beside `[OnEnter]` and `[OnExit]`.
3. **A pointer dragged a step a frame by one command**, 3DEngine's `input.drag`, so a script can
   swipe, drag a transform handle or move a panel, which `input.move`, `input.press` and
   `input.release` in separate calls cannot time.
4. **A build with no warnings, and a warning failing the workflow.** 3DEngine's first runs on
   GitHub carried dozens of annotations nobody had seen, a `stackalloc` in a loop among them. The
   managed build passes `-warnaserror` in the workflow once it is clean, with a warning that is
   right to keep turned off where it arises and its reason beside it, and `cargo` builds deny
   warnings the same way.
5. **Gamepads** wait on the owner (decision 2).

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
