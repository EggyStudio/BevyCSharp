# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it already keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read.

Reviewed up to `75ad5b1`. HDR images decoded in every build (`189d7fc`), three more 3D examples with
the shared radio buttons (`5caada1`) and most of the ECS and application examples (`75ad5b1`) were
read by their descriptions, with `callbacks` read beside Bevy's source and found the same
program. The table stands at 53 written, 3 written in part, 222 that can be, 87 missing and 56
that do not apply: twenty written in one batch, and thirteen rows that said `can be written`
found to be missing something, which is the table doing what it is for.

## Now

Item 1 is the owner's request of 2026-10-04 and goes on a group at a time. Items 4 to 6 are
taken from [SHARED.md](SHARED.md).

1. **Bevy's examples, the cheap groups on.** As `75ad5b1` did for ECS and application: input,
   camera, audio, transforms, math, state, time and async tasks, then 2D and UI, many a batch,
   an example that needs something missing marked and passed over. Each capture of one that
   draws is looked at beside Bevy's picture at `https://bevy.org/examples/`.
2. **The two gaps the ECS examples named most**, after the cheap groups and before the rest of
   3D, each with the examples it unlocks written in its batch. An order among the systems of one
   stage, one before or after another or a chain, which every game with more than a few systems
   needs and several rows wait on. Then observers: a system told when a component is added to or
   removed from an entity, or when an event of the game's own is sent to one, which four rows
   wait on. Both are Bevy's own and are bridged, not built. The remaining gaps follow by how
   many rows each holds, 2D meshes among the first.
3. **A build with no warnings, and a warning failing the workflow.** The managed build passes
   `-warnaserror` in the workflow once it is clean, with a warning that is right to keep turned
   off where it arises and its reason beside it, and `cargo` builds deny warnings the same way.
4. **A contact that says how hard its pair hit, and joints with limits**, from 3DEngine's
   `c5227118`: the speed a pair closed at on the contact's message, a ball joint kept within a
   cone, and a distance joint whose range changes after it is made.
5. **Two more of a character and a collider**, from 3DEngine's `52579d98` and `454e9276`: a
   character that crouches and stands from its component's height, and a collider that is the
   shape of the meshes an entity and those under it show, made once the model has loaded.
6. **A joint in a scene file, and a pad's sensors**, from 3DEngine's `e46058fc` and `73ce6326`:
   a joint as an entity naming its two bodies, so a level hangs a door where it stands, and a
   gamepad's gyro, accelerometer, touchpad and light where gilrs offers them. Two small things
   of the command line are checked in the same batch and taken if they are missing: `entity.set`
   writing a field that holds a list from items split by semicolons, a command that pretends
   files dropped on the window, a command's parameter with a default being left off, and a
   placed scene file spawned again when it is written while the level runs.

## Verdicts

None open.

## Decisions

1. **Commits stay local.** The owner pushes `main` from their own tools, and the working session
   commits and does not push, as COMMITS.md and CLAUDE.md say. An earlier entry here that allowed
   pushing is withdrawn by the owner.
2. **CLAUDE.md's bullet on SHARED.md is the owner's.** They approved it on 2026-10-04, and it is
   committed like any other change.

## Replies

