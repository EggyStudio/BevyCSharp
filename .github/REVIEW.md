# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it already keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read.

Reviewed up to `18a469d`. A sampler set on an image that exists (`e3f334b`), `motion_blur`
(`583ac7d`) and `lightmaps` (`18a469d`) were taken on their descriptions and raised nothing. The
table stands at 33 written, 3 written in part, 258 that can be, 74 missing and 53 that do not
apply.

## Now

Item 1 is the owner's request of 2026-10-04 and goes on a group at a time. Items 3 to 5 are
taken from [SHARED.md](SHARED.md).

1. **Bevy's examples, the cheap groups next.** The last three batches wrote two examples and
   bridged a feature for each, which is right for those examples and slow for the count: 258
   rows say `can be written` and each is a claim until its example exists. The 3D group's
   remaining rows wait, and the groups whose examples are short programs with little or nothing
   to draw are written first, many a batch: ECS (24 rows that can be written), application
   (13), input (9), camera (8), audio (6), transforms (5), math (5), state (4), time (3), async
   tasks (3), then 2D (20) and UI (36). An example that turns out to need something missing has
   its row changed to `missing` with what it lacks and is passed over, not bridged in that
   batch, so a batch is spent writing. The larger gaps are then taken in the order of how many
   rows each holds, 2D meshes first. An example with nothing to draw is captured as its console
   output where a picture would be blank, and the table says which. Each capture of one that
   draws is looked at beside Bevy's picture at `https://bevy.org/examples/`.
2. **A build with no warnings, and a warning failing the workflow.** The managed build passes
   `-warnaserror` in the workflow once it is clean, with a warning that is right to keep turned
   off where it arises and its reason beside it, and `cargo` builds deny warnings the same way.
3. **A contact that says how hard its pair hit, and joints with limits**, from 3DEngine's
   `c5227118`: the speed a pair closed at on the contact's message, a ball joint kept within a
   cone, and a distance joint whose range changes after it is made.
4. **Two more of a character and a collider**, from 3DEngine's `52579d98` and `454e9276`: a
   character that crouches and stands from its component's height, and a collider that is the
   shape of the meshes an entity and those under it show, made once the model has loaded.
5. **A joint in a scene file, and a pad's sensors**, from 3DEngine's `e46058fc` and `73ce6326`:
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

