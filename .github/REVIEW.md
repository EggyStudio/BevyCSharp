# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it already keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read.

Reviewed up to `4de242d`. Gamepads (`7f87a47`), the README at 249 lines with its reference in 24
pages under `docs/` (`a0b1fa3`) and the cheatsheet at the root, written from the library's
documentation and held to it by a test (`4de242d`), are settled. The pages were checked for
length, the longest being 327 lines, and the headings check and link check reported are what was
asked for. Writing the cheatsheet from the documentation is better than what was asked, and is
offered to 3DEngine. All three are in the ledger.

## Now

Item 1 is the owner's request of 2026-10-04 and goes on a group at a time. Items 3 to 5 are
taken from [SHARED.md](SHARED.md).

1. **Bevy's examples, a group at a time**, as `661682e` began: the rest of 3D rendering, then
   2D, UI, ECS, animation, audio, input, camera, state, transforms, window, gizmos, picking,
   assets, glTF, scene, time, movement, math and shaders, then the games and showcases, and the
   rest after. Each example's Rust source is read, its row set, and it is written when it can
   be. Something small that is missing is bridged in the batch, and something large becomes an
   entry in TODO.md naming the examples it unlocks, the largest taken between groups. Each
   capture is looked at beside Bevy's picture at `https://bevy.org/examples/`. The verdict below
   is taken with the next batch, which the table in the working tree has begun.
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
   gamepad's gyro, accelerometer, touchpad and light where gilrs offers them.

## Verdicts

1. **An example written in part counts as written** (`661682e`). Five of the 22 rows marked
   `written` say what they leave out: `3d_shapes` has the solids and none of Bevy's segment,
   polyline or seven extrusions, `bloom_3d` three of its six settings, `pbr` its turned label,
   `transparency_3d` its alpha to coverage, and `wireframe` its width and topology. The rows are
   honest and the count above them is not, since 22 of 421 says those five are done. The table
   gains a fifth state, `written in part`, with what is left named as it is in those rows and
   counted in a column of its own, the headline gives the two numbers apart, and each thing left
   out is an entry in TODO.md naming the example waiting on it, as a missing example's is. A
   difference that is not a feature, such as cubes scattered by another random generator, stays
   `written`.

## Decisions

1. **Commits stay local.** The owner pushes `main` from their own tools, and the working session
   commits and does not push, as COMMITS.md and CLAUDE.md say. An earlier entry here that allowed
   pushing is withdrawn by the owner.
2. **CLAUDE.md's bullet on SHARED.md is the owner's.** They approved it on 2026-10-04, and it is
   committed like any other change.

## Replies

- Item 1, the verdict on a row written in part, is taken in the commit carrying this line. The
  table has a fifth state, `written in part`, counted in its own column, and the headline and the
  README give the two numbers apart. `wireframe`, `bloom_3d` and `pbr` were finished through
  Bevy's reflected wireframe width and topology, Bloom's fields and UiTransform, so they are
  written, and `3d_shapes`, `transparency_3d` and `skybox` are written in part, each gap an entry
  in TODO.md naming its example.

