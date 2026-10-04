# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it already keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read.

Reviewed up to `d6a03d2`. The frame profile, `games/Stress` and PERFORMANCE.md (`d6a03d2`) were
read and are settled. The numbers are what the first three items are ordered by.

## Now

In this order, each of the first three ending with `measure.sh` run again and PERFORMANCE.md
holding the numbers before and after.

1. **The physics sync reads only what changed.** `PhysicsWorld.Sync` reads every body's
   `RigidBody`, `Collider` and `Transform` each fixed step, 6.6 ms at 5,000 bodies, whether or
   not anything moved. It is to walk the bodies whose components changed since it last ran,
   through the change detection the ECS has, and write back only the bodies the simulation
   moved, which leaves a sleeping body costing nothing.
2. **Find what the shadows' 10 ms is before calling it Bevy's.** Three lights cost 8 to 10 ms of
   CPU in the render at a thousand cubes, with the GPU under 2 ms, and that is far from what
   Bevy's own examples spend on a scene this size, so the number is more likely something this
   bridge sets or leaves unset than the engine's floor. The same scene written in Rust against
   the same Bevy build, with no bridge, is measured beside it. If it is as slow, PERFORMANCE.md
   says so with both numbers. If it is not, the difference is found, with the shadow map sizes
   the bridge asks for, a pass encoded for each face whether or not anything is in it, entities
   that are not batched because of a component the bridge adds, and a debug or validation
   setting left on in release as the first places to look.
3. **A behavior reaches its entity's other components without a crossing each.** A behavior that
   moves a `Transform` pays a call into the bridge an entity, about as much again as its own
   work. The generator already walks a behavior's own component by chunk. It is to hand a method
   the other components it names as parameters from the same chunk, so the common behavior
   crosses once a chunk, and the README's examples use that form. Verified by the movers run
   showing crossings a frame fall from one an entity to a few a chunk.
4. **Every attribute the generators accept is compiled and run by a test**, each asserted to run
   when it should and not otherwise, with the list taken from the generators' own tables so an
   attribute without a case fails. In 3DEngine a behavior attribute failed to compile for four
   batches while the suite passed, because nothing used it.
5. **The README's install followed by a stranger**, in a container with only the packed package:
   a new project, the package added, the README's first program as written, built and run
   headless. Each step it leaves out or gets wrong is fixed, and the package workflow repeats
   the walk.
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
