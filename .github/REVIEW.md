# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it already keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read.

Reviewed up to `966f607`. The last seven shader examples (`966f607`) are settled on the reply,
which was read. `gpu_readback` was set beside Bevy's and reads the same three things, a whole
buffer, a part of it and an image, and says where its reads differ. The table stands at 207
written, 12 written in part, 46 that can be, 98 missing and 58 that do not apply.

## Now

The owner asked that the work does not stop. A batch that ends is followed by the next item
here with no wait for a reply, and the list is long so that it does not run out. Items 3, 6 to 8
and 10 to 13 are taken from [SHARED.md](SHARED.md).

1. **A picture in the README opens the example's own source.** The owner chose this on
   2026-10-05 over Bevy's live demos, so that a picture leads to the C# that drew it and nothing
   is cached from Bevy's site. Every picture in the gallery, the 150 that open bevy.org and the
   rest that open nothing, links to its file as a full address,
   `https://github.com/EggyStudio/BevyCSharp/blob/main/BevyCSharp.Examples/<group>/<name>.cs`,
   the README being the package's page too. What the live links needed goes with them:
   `BevyCSharp.Examples/bevy-live.txt`, the `--live` flag of `build/examples-table.py` with the
   code that asks bevy.org, the `live in Bevy` link on each row of EXAMPLES.md, and the pass
   `build/check-docs.py` gives addresses under `https://bevy.org/examples/`. The sentence above
   the gallery says a picture opens the program that drew it. A row of EXAMPLES.md keeps its link
   to Bevy's source and its link to the C# file. `check-docs.py` holds each picture's link to a
   file in the checkout, with no request made, so an example that is renamed cannot leave a dead
   link. It is small and comes first, after the commit in hand.
2. **The 46 rows that say `can be written` are written**, until that column is empty, many a
   batch. In this order from here: application, usage, picking and the rest, then the stress
   tests, which are also numbers for PERFORMANCE.md beside Bevy's own.
   An example that needs something missing has its row changed and is passed over. A picture
   that differs from Bevy's for no known reason is taken down to the smallest scene that still
   differs and explained before the pass goes on.
3. **What a kinematic body carries, and a clock stepped by hand**, both from a red run of
   3DEngine's on Windows (its REVIEW.md, Verdicts 1 to 3), and measured here before anything is
   changed.
   `PhysicsWorld.Step` puts a kinematic body at its entity's place and gives it the distance
   moved since the last step over one step's time. An entity moved once a frame then has a body
   whose speed in a step is the frame's distance over a step's time, and zero in every further
   step of the frame. Friction is too weak to follow those jumps, so what rests on the body
   settles at the speed it has in most steps and not at its average. A model of one dimension
   gives, for a platform at 2.00 and Bevy's 64 steps a second, a crate at 2.12 at 60 frames a
   second, 1.82 at 144, 2.52 at 50 and 0.07 at 30. `Step` is public and takes its seconds, so a
   test moves a platform's entity once a frame, steps as many times as a frame of that length
   holds, and reads the crate's pace, as a table over 144, 75, 60, 50 and 30 frames a second and
   over frames of uneven length.
   If the engine agrees with the model, the body moves at its entity's speed, which is the
   distance the entity moved between two frames over the time between them, through every step
   of the next frame, and each step aims at the entity's last place moved on by that speed for
   the time simulated since. That holds 2.00 at every rate in the model. A turn gives the body no
   spin today, so nothing is carried round on a turning platform, and it is given the same way.
   An entity moved in fixed steps has to stay exact.
   The clock is Bevy's own `TimeUpdateStrategy::ManualDuration`, reached from C# and from `bcs`,
   so a test or a capture advances a set amount a frame and is the same on every machine.
   `Time.Step` runs frames at the delta they would have had, which is the machine's.
4. **The gaps, by how many rows each holds**, once item 2 is through, each bridged from Bevy
   with the examples it unlocks written in its batch: more of Bevy's WGSL reached as its
   lighting is (the deferred buffers, a decal's tag and a volume's voxels), the widgets' events
   as observers, keys observed as they reach a field, and what the table then names most. When
   the captures have settled, they are compared whole with checked-in references by the
   workflow, a small share of pixels allowed to differ between devices, as 3DEngine does for its
   scenes.
5. **A build with no warnings, and a warning failing the workflow.** The managed build passes
   `-warnaserror` in the workflow once it is clean, with a warning that is right to keep turned
   off where it arises and its reason beside it, and `cargo` builds deny warnings the same way.
6. **A contact that says how hard its pair hit, joints with limits, and collision layers**, from
   3DEngine's `c5227118` and `8520dbe1`: the speed a pair closed at on the contact's message, a
   ball joint kept within a cone, a distance joint whose range changes after it is made, and
   bodies on layers whose pairs collide or not, which contacts, triggers, the character and rays
   follow, a body asleep waking when its layer changes.
7. **Two more of a character and a collider**, from 3DEngine's `52579d98` and `454e9276`: a
   character that crouches and stands from its component's height, and a collider that is the
   shape of the meshes an entity and those under it show, made once the model has loaded.
8. **A joint in a scene file, and a pad's sensors**, from 3DEngine's `e46058fc` and `73ce6326`:
   a joint as an entity naming its two bodies, so a level hangs a door where it stands, and a
   gamepad's gyro, accelerometer, touchpad and light where gilrs offers them. Two small things
   of the command line are checked in the same batch and taken if they are missing: `entity.set`
   writing a field that holds a list from items split by semicolons, a command that pretends
   files dropped on the window, a command's parameter with a default being left off, and a
   placed scene file spawned again when it is written while the level runs.
9. **The three shapes no wrapper types**, when a batch next touches the generator: a list inside
   a component (box shadows, gradients), an enum inside a variant (a sprite's slicer, an
   orthographic projection), and a range of numbers (`VisibilityRange`), which are the 13
   string paths the examples still hold.
10. **Three things 3DEngine's fourth game turned up, checked here** (`3c9c7ac8` in its checkout),
    each taken if it is missing and answered under Replies if it is not. A behavior method that
    writes a resource, draws interface or plays a sound while others run beside it on worker
    threads. A script compiled while the game runs naming the game's own types, with the scripts
    watched being the project's and not a copy in the build folder, which Courtyard would show.
    And a game written in behaviors alone with hundreds of entities, played by the workflow and
    profiled, which `games/Stress` measures and no game here plays.
11. **Two things nothing here has tried**, from 3DEngine's `044d2396` and `3442e2cd`, where each
    found faults at once. Courtyard and the stress program played for ten minutes by a script
    while managed memory, the bridge's allocations, entities and assets are read at intervals
    through a `bcs` command, anything that keeps climbing found and fixed, and a short form of
    the run in the workflow. And every loader given a missing, an empty, a cut short and a random
    file (scenes, data assets, saves, materials, meshes, images, models, sounds, shaders and
    scripts), each answering with a message that names the file and no exception or panic
    crossing the bridge, as one table in a test.
12. **The public surface written down, and release notes from the commits**, from 3DEngine's
    `fc5aef49`: a listing of every public type and member a tool writes from the built assembly,
    checked in, with a test that fails when the two differ, so a change to what a game calls is
    read as one, and the pack workflow writing the package's release notes from the commits
    since `build/version.txt` last changed. In the same batch it is checked whether a ray here
    stops at a sensor, which there threw a car's wheel and a character's ground check.
13. **A first game told from an empty folder, a step at a time**, from 3DEngine's `d5d2578d`.
    `docs/making-a-game.md` describes Courtyard finished, and nothing here walks a newcomer from
    an empty folder and the package to a small game in a dozen steps, each step a whole program
    the workflow builds and runs and the page is held to line for line.

## Verdicts

None open.

## Decisions

1. **Commits stay local.** The owner pushes `main` from their own tools, and the working session
   commits and does not push, as COMMITS.md and AGENTS.md say. An earlier entry here that allowed
   pushing is withdrawn by the owner.
2. **AGENTS.md's bullet on SHARED.md is the owner's.** They approved it on 2026-10-04, and it is
   committed like any other change.
3. **A picture opens the example's own source, and not Bevy's live demo of it.** The owner chose
   it on 2026-10-05, and item 1 of the Now list carries it out.

## Replies
- Item 1, application, usage and picking. The seven rows are written. Picking a mesh is a ray cast
  from the pointer, since the bridge drains clicks rather than observing Bevy's pointer events,
  and `debug_frustum_culling` tests each box against the camera's reflected `Frustum`, since no
  wrapper reads `VisibleEntities`. `MeshShape.Extrusion` makes a flat shape solid, which
  `mesh_picking` needed and which gives `3d_shapes` Bevy's middle row and its Tab, with a test.
  `3d_shapes` drew its torus too thin, its measures being the inner and outer radius, and draws
  Bevy's default one now. `headless` runs its second app from the hook that runs after the first
  stops, and `headless_renderer`'s capture is the picture it saves itself.
- Item 1, the README's pictures. Every picture in the gallery opens its example's C# file by full
  address, and the sentence above it says so. `bevy-live.txt`, `--live` with the code that asked
  bevy.org, the live links on the rows of EXAMPLES.md and the pass `check-docs.py` gave bevy.org
  are gone. `check-docs.py` now reads an HTML link's address as it reads a Markdown one, so each
  picture's link is held to a file in the checkout with no request made.
- Item 2, the glTF rows and hello_world. The five glTF rows are written, with a helper walking a
  scene's entities nearest first as Bevy's `iter_descendants` does, and `edit_material_on_gltf`
  changes a helmet once its parts carry their material names and those have loaded, since no
  event says a scene is ready. `fps_overlay` is changed to missing and passed over, since Bevy's
  FPS overlay is in its dev tools, which the bridge does not compile in.
