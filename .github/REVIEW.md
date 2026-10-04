# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it already keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read.

Reviewed up to `324f919`. A level's cameras (`f7b087e`), the message after a load (`3ab5b22`) and
keys held for a number of frames (`324f919`) are settled, on the play script reaching its win
with each.

## Now

In this order.

1. **The smaller two from Courtyard**: the editor says once that a game's states are not entered
   while editing, and `entity.get` prints a name and a visibility class as values.
2. **Measure what a frame holds.** Nothing has measured this engine, and its cost is in a place
   Bevy alone does not have, the crossing between C# and the bridge. Two stress programs under
   `games/` or the sample: one grows the number of entities a behavior moves each frame, the
   other the number of drawn mesh entities with a few materials, lights and physics bodies. Each
   reports the frame's time split into managed systems, the crossings (how many a frame and what
   they cost together), Bevy's own schedule and the render, and the count at which it leaves 60
   frames a second, through a `bcs` command so a run is repeatable. The numbers, the machine and
   the three largest costs go into a section of RENDERING.md or a document beside it. No
   optimization is in this batch. The next batch is the largest cost the numbers show.
3. **Every attribute the generators accept is compiled and run by a test**, each asserted to run
   when it should and not otherwise, with the list taken from the generators' own tables so an
   attribute without a case fails. In 3DEngine a behavior attribute failed to compile for four
   batches while the suite passed, because nothing used it.
4. **The README's install followed by a stranger**, in a container with only the packed package:
   a new project, the package added, the README's first program as written, built and run
   headless. Each step it leaves out or gets wrong is fixed, and the package workflow repeats
   the walk.
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
