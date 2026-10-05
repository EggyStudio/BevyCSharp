# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it already keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read.

Reviewed up to `6a2e78a`. Bevy's resources reached through the wrappers its components have
(`6a2e78a`) is settled on its description and the reply read before it. The table stands at 149
written, 7 written in part, 97 that can be, 111 missing and 57 that do not apply.

## Now

Item 1 is the owner's request of 2026-10-04 and goes on a group at a time. Items 4 to 6 are
taken from [SHARED.md](SHARED.md).

1. **Bevy's examples, what is left of the pass.** Math, then the rest of 3D, an example that
   needs something missing marked and passed over. A picture that differs from Bevy's for no
   known reason is taken down to the smallest scene that still differs and explained before
   the pass goes on, as `grid` was, since that is where the table finds a fault.
2. **The gaps, by how many rows each holds**, once the groups in item 1 are through, each
   bridged from Bevy with the examples it unlocks written in its batch. By the table as it
   stands: a standard material extended by a shader, which seven rows wait on; then
   text gizmos, editable text and Bevy's widgets.
   When the captures have settled, they are compared whole with checked-in references by the
   workflow, a small share of pixels allowed to differ between devices, as 3DEngine's
   `771f10e9` does for its scenes, so an example that stops drawing as it did fails a run.
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
7. **The three shapes no wrapper types**, when a batch next touches the generator: a list inside
   a component (box shadows, gradients), an enum inside a variant (a sprite's slicer, an
   orthographic projection), and a range of numbers (`VisibilityRange`), which are the 13
   string paths the examples still hold.
8. **Three things 3DEngine's fourth game turned up, checked here** (`3c9c7ac8` in its checkout),
   each taken if it is missing and answered under Replies if it is not. A behavior method that
   writes a resource, draws interface or plays a sound while others run beside it on worker
   threads. A script compiled while the game runs naming the game's own types, with the scripts
   watched being the project's and not a copy in the build folder, which Courtyard would show.
   And a game written in behaviors alone with hundreds of entities, played by the workflow and
   profiled, which `games/Stress` measures and no game here plays.

## Verdicts

None open.

## Decisions

1. **Commits stay local.** The owner pushes `main` from their own tools, and the working session
   commits and does not push, as COMMITS.md and AGENTS.md say. An earlier entry here that allowed
   pushing is withdrawn by the owner.
2. **AGENTS.md's bullet on SHARED.md is the owner's.** They approved it on 2026-10-04, and it is
   committed like any other change.

## Replies
- Item 1, the rest of 3D. Math holds no row that can be written, each waiting on Bevy's math, and
  is marked. `meshlet` and `solari` are written against a bridge built with `--meshlet --solari`
  on this machine's adapter. `meshlet` needed Bevy's material that colors each cluster, which the
  bridge adds as `Render.CreateClusterMaterial()`, and `solari` showed that a model's mesh with no
  texture coordinates was refused for ray tracing, where Bevy's example gives it coordinates of
  zero, which the bridge now does, with a test of each. `solari` is written in part, its path
  tracer and its scene of many lights left out, as TODO.md says.
