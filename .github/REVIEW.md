# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it already keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read.

Reviewed up to `1ad2179`. Bevy's six games (`1ad2179`) are settled on the description and the reply,
which was read, with an offscreen camera spawned after startup found never to draw and fixed.
The gizmo examples and the rest of Bevy's gizmos are in the working tree and are settled when
committed. The table stands at 183 written, 12 written in part, 70 that can be, 98 missing and
58 that do not apply.

## Now

The owner asked that the work does not stop. A batch that ends is followed by the next item
here with no wait for a reply, and the list is long so that it does not run out. Items 4 to 6
and 8 to 10 are taken from [SHARED.md](SHARED.md).

1. **The 70 rows that say `can be written` are written**, until that column is empty, many a
   batch. Assets, animation, the games and gizmos are done. In this order from here: shaders,
   application, usage, picking and the rest, then the stress tests, which are also numbers for
   PERFORMANCE.md beside Bevy's own.
   An example that needs something missing has its row changed and is passed over. A picture
   that differs from Bevy's for no known reason is taken down to the smallest scene that still
   differs and explained before the pass goes on.
2. **The gaps, by how many rows each holds**, once item 1 is through, each bridged from Bevy
   with the examples it unlocks written in its batch: more of Bevy's WGSL reached as its
   lighting is (the deferred buffers, a decal's tag and a volume's voxels), the widgets' events
   as observers, keys observed as they reach a field, and what the table then names most. When
   the captures have settled, they are compared whole with checked-in references by the
   workflow, a small share of pixels allowed to differ between devices, as 3DEngine does for its
   scenes.
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
9. **Two things nothing here has tried**, from 3DEngine's `044d2396` and `3442e2cd`, where each
   found faults at once. Courtyard and the stress program played for ten minutes by a script
   while managed memory, the bridge's allocations, entities and assets are read at intervals
   through a `bcs` command, anything that keeps climbing found and fixed, and a short form of
   the run in the workflow. And every loader given a missing, an empty, a cut short and a random
   file (scenes, data assets, saves, materials, meshes, images, models, sounds, shaders and
   scripts), each answering with a message that names the file and no exception or panic
   crossing the bridge, as one table in a test.
10. **The public surface written down, and release notes from the commits**, from 3DEngine's
    `fc5aef49`: a listing of every public type and member a tool writes from the built assembly,
    checked in, with a test that fails when the two differ, so a change to what a game calls is
    read as one, and the pack workflow writing the package's release notes from the commits
    since `build/version.txt` last changed. In the same batch it is checked whether a ray here
    stops at a sensor, which there threw a car's wheel and a character's ground check.

## Verdicts

None open.

## Decisions

1. **Commits stay local.** The owner pushes `main` from their own tools, and the working session
   commits and does not push, as COMMITS.md and AGENTS.md say. An earlier entry here that allowed
   pushing is withdrawn by the owner.
2. **AGENTS.md's bullet on SHARED.md is the owner's.** They approved it on 2026-10-04, and it is
   committed like any other change.

## Replies
  `extra_asset_source` from the asset batch was filed under Bevy's source file name rather than
  its example's, so the table still counted it as one to write. It is renamed in this batch.
- Item 1, shaders, the materials. Nine of the shader rows are written, each with its WGSL put into
  Slang. `bcs::VertexOutput` now carries the instance index Bevy's vertex shader always hands on,
  and `bcs::tag` reads a mesh's `MeshTag` with it, and `TextureSettings.Layers` loads a stacked
  picture as an array texture, which `array_texture`, `storage_buffer` and `automatic_instancing`
  needed, with a test of both. `shader_defs` compiles its program with the define and without,
  where Bevy specializes one material's pipeline, and `shader_material_bindless` binds per
  material, as Bevy does without bindless support. The seven rows left in the group, compute,
  passes and drawing on a camera, are the next batch.
