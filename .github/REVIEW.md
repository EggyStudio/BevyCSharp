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

Items 1, 2, 3 and 5 are taken from [SHARED.md](SHARED.md), which records what this engine and
3DEngine have in common. Items 2 and 6 are the owner's decisions of 2026-10-04. In this order.

1. **A character that walls stop**, the first item taken from [SHARED.md](SHARED.md), which
   records what this engine and 3DEngine have in common. Courtyard's runner is a ball pushed by
   its velocity, which rolls, cannot stand on a slope and has no ground to ask about. A character
   controller over `PhysicsWorld`, a capsule moved by a wanted velocity that slides along what it
   meets, rides a low edge, holds a slope up to an angle, jumps and reports whether it stands on
   ground, as a component a scene holds and the inspector edits. 3DEngine's is in its checkout
   beside this one (`3DEngine/Physics/Bepu/PhysicsWorld.Characters.cs`, with its tests), and is
   the idea to take, built this engine's own way. Courtyard's runner becomes one, and `play.sh`
   still reaches its win.
2. **Gamepads** (decision 2), which the owner decided for. `bevy_gilrs` goes into the render and
   editor profiles, with buttons, sticks, triggers, connection and rumble reaching C# as the
   keyboard does, a command that presses a pad's button for `bcs`, and Courtyard's runner
   steered by a stick beside the keys.
3. **A system run on a move from one state value to a particular other**, 3DEngine's
   `OnTransition`, beside `[OnEnter]` and `[OnExit]`.
4. **A pointer dragged a step a frame by one command**, 3DEngine's `input.drag`, so a script can
   swipe, drag a transform handle or move a panel, which `input.move`, `input.press` and
   `input.release` in separate calls cannot time.
5. **A build with no warnings, and a warning failing the workflow.** 3DEngine's first runs on
   GitHub carried dozens of annotations nobody had seen, a `stackalloc` in a loop among them. The
   managed build passes `-warnaserror` in the workflow once it is clean, with a warning that is
   right to keep turned off where it arises and its reason beside it, and `cargo` builds deny
   warnings the same way.
6. **The README is split**, which the owner decided for. It is about 3,400 lines and changes in
   most commits. README.md keeps what the project is, the install, a first behavior, running and
   driving a game, the status, building and the license, and links to the rest. The section
   called The engine, some 2,300 lines, moves into one document an area under `.github/` or
   `docs/`, each linked from the README's contents, with nothing dropped. The README walk still
   takes its program from where the walk script looks, the package's readme is checked if it
   embeds README.md, and SHARED.md's rows that name a README section are told to the reviewing
   session with a `Shared:` line so they follow.

## Verdicts

None open.

## Decisions

1. **Commits stay local.** The owner pushes `main` from their own tools, and the working session
   commits and does not push, as COMMITS.md and CLAUDE.md say. An earlier entry here that allowed
   pushing is withdrawn by the owner.
2. **Gamepads go into the render profile.** The owner decided on 2026-10-04. `bevy_gilrs` needs
   libudev's headers when the bridge is built on Linux, and the render profile already takes
   ALSA for audio on the same terms, so `build-native.sh` installs them in the container and
   names the package for a local build as it does for ALSA, and the headless profile still
   builds with a C compiler alone. TODO.md's entry that excludes gamepads is rewritten around
   what is left, and BUILDING.md names the package.
3. **CLAUDE.md's bullet on SHARED.md is the owner's.** They approved it on 2026-10-04, and it is
   committed like any other change.

## Replies

- Shared: the character controller is taken in the commit carrying this line, as a `CharacterController` component beside a dynamic `RigidBody` and `Collider`, which the game steers through `Move` and `Jump` and reads `Grounded` and `GroundNormal` from. It slides, rides a low edge, climbs a step, holds a slope and jumps as 3DEngine's does, and stays upright with its entity turned by the game. Crouching is not taken, and TODO.md holds it.
