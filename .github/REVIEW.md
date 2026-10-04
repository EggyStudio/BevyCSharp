# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it already keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read.

Reviewed up to `c856ff9`. The fifth state in the table (`e4fb1e8`) settles the verdict, with three
of the five finished through reflection and the rest counted apart. The table stands at 32
written, 3 written in part, 259 that can be, 74 missing and 53 that do not apply. `clearcoat` was
read beside Bevy's source and is the same scene, with its four spheres, the golf ball, the
skybox, the environment map and the light that changes kind. The rectangular light (`c039287`)
raised nothing.

## Now

Item 1 is the owner's request of 2026-10-04 and goes on a group at a time. Items 3 to 5 are
taken from [SHARED.md](SHARED.md).

1. **Bevy's examples, a group at a time**, as `661682e` began: the rest of 3D rendering, then
   2D, UI, ECS, animation, audio, input, camera, state, transforms, window, gizmos, picking,
   assets, glTF, scene, time, movement, math and shaders, then the games and showcases, and the
   rest after. Each example's Rust source is read, its row set, and it is written when it can
   be. Something small that is missing is bridged in the batch, and something large becomes an
   entry in TODO.md naming the examples it unlocks, the largest taken between groups. Each
   capture is looked at beside Bevy's picture at `https://bevy.org/examples/`. 
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
   writing a field that holds a list from items split by semicolons, and a command that
   pretends files dropped on the window.

## Verdicts

None open.

## Decisions

1. **Commits stay local.** The owner pushes `main` from their own tools, and the working session
   commits and does not push, as COMMITS.md and CLAUDE.md say. An earlier entry here that allowed
   pushing is withdrawn by the owner.
2. **CLAUDE.md's bullet on SHARED.md is the owner's.** They approved it on 2026-10-04, and it is
   committed like any other change.

## Replies

