# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it already keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read.

Reviewed up to `435e9a7`. The 2D examples that draw sprites and text (`e3bca6b`) and the interface
examples in seven batches (`f5344e4` to `435e9a7`) were taken on their descriptions and raised
nothing. The table stands at 111 written, 7 written in part, 124 that can be, 122 missing and 57
that do not apply.

## Now

Item 1 is the owner's request of 2026-10-04 and goes on a group at a time. Items 4 to 6 are
taken from [SHARED.md](SHARED.md).

1. **The two pictures that differ from Bevy's are explained before anything else.** `ui/grid`,
   whose square overflows its row, and `camera/first_person_view_model`, whose arm on the
   view-model layer is not drawn, are written and parked because their captures differ and the
   reason is not known. An example that is the same program and draws another picture is the
   most useful thing the table can turn up, since it says the bridge hands Bevy something other
   than what the C# asked for, and every game using a grid or a render layer would meet it. Each
   is taken down to the smallest scene that still differs, compared with what Bevy holds for
   that entity through `entity.get` (the `Node`'s grid fields, the `RenderLayers` on the arm and
   on its camera, and the camera's order), and ends as a fix with a test, or as a row marked
   `missing` naming exactly what is not handed over. The remaining groups in the pass (math,
   then what is left of 3D) follow as before.
2. **The gaps, by how many rows each holds**, once the groups in item 1 are through, each
   bridged from Bevy with the examples it unlocks written in its batch. By the table as it
   stands: 2D meshes with their color materials (13 rows); an order among the systems of one
   stage, one before or after another or a chain, and observers of a component's addition and
   removal and of a game's own events, which the ECS and interface rows wait on; Bevy's
   resources reached through reflection as its components are, which `UiScale` and others wait
   on and which is likely a small bridge for many rows; then text gizmos, editable text and
   Bevy's widgets.
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

## Verdicts

None open.

## Decisions

1. **Commits stay local.** The owner pushes `main` from their own tools, and the working session
   commits and does not push, as COMMITS.md and CLAUDE.md say. An earlier entry here that allowed
   pushing is withdrawn by the owner.
2. **CLAUDE.md's bullet on SHARED.md is the owner's.** They approved it on 2026-10-04, and it is
   committed like any other change.

## Replies

