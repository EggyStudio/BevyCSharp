# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read. A stash of every changed file
takes what was written here since the last commit out of the tree until it is popped, so a stash
names its own paths.

Reviewed up to `56ad9a2`. Two commits of moves alone split the bridge's `programs.rs` and
`reflect.rs` into parts and put the lit shader's tests in `Assets`, so N 1.3's list stands at 19
from 21, 12 of them in the bridge, and N 1.4's at 97 to mend (`5be8291`, `56ad9a2`). The reply on
item 3's first gap, a decal's tag reached from a Slang material over Bevy's clustered decals, is
written and its commit to come. No verdict is open.

Before them, a kinematic body followed its entity at the speed and rate of turning the entity moves
at, through every step of a frame, so a crate on a platform moved at 2.00 rides at 2.00 within 0.02
at seven frame rates and over uneven frames where it rode at 1.80 to 0.02 before, a turned platform
carries it round, and `MarkPlaced` or `PlaceBeyond` says when an entity was put somewhere rather
than moved (`05bc3b4`), which settles item 3. A body at rest given a velocity, an impulse, a motor
or a changed joint moves in the next step, `PhysicsWorld.Wake` clearing Bepu's candidate flag and
its count of steps under the threshold, with `SleepTests` over ten cases after 32 and 128 steps of
rest, of which the velocity and the impulse after 32 were lost before (`8557a75`). A convex
manifold's friction is scaled by its contact count, as 3DEngine's `ed0f3aa6` has it, so a box on a
box slides to 2.55 and 1.27 units at frictions of a half and 1 where it slid 10.15 and 5.06, and on
a floor of triangles as well, `FrictionTests` holding each within a tenth of the distance friction
allows (`e5c8110`), which settles item 14. No verdict is open.

Before them, a frame advanced the clock by a set length in place of the machine's, from
`Config.FrameSeconds`, `Time.FrameSeconds`, `BCS_FRAME_TIME`, `bcs open --frame-time` and
`app.frametime`, over Bevy's `TimeUpdateStrategy::ManualDuration` (ABI 204), and a clock let go
begins again at the moment it is let go, so a run of short frames does not leave the game standing
until the machine catches up (`711f416`), which is the clock half of item 3. The kinematic half is
measured and written, its commit to come. On the way it found two things in Bepu: a box sliding a
quarter as rough as its friction says, which is Bepu sharing a convex manifold's friction among its
contacts and item 15 takes from 3DEngine's mend, and a resting body given speed put to sleep at the
next step's start, the next batch here, which 3DEngine's item 6 checks for its own code. No verdict
is open.

The norm has 43 rules, and this engine stands at 26 checked, 4 with places listed, 4 to take
and 9 by review.


## Now

The owner asked that the work does not stop. A batch that ends is followed by the next item
here with no wait for a reply, and the list is long so that it does not run out. Items 5 to 7
and 9 to 13 are taken from [SHARED.md](SHARED.md).

1. **What the next page says.** The run after `5264257` is pushed shows whether the three failures
   are gone, which the reviewing session reads and says here. The list goes on meanwhile.
2. **The norm's lists are paid down.** A listed file is mended when a batch next touches it, in a
   commit of its own that moves code alone, the largest first where there is a choice, and a
   batch reads the lists for the files it will touch before it starts. The rules still to take
   each have their item: N 2.2 and N 6.1 are the settings of item 4, N 2.1 the listing of item
   11 and N 2.6 the table of bad files in item 10.
3. **The gaps, by how many rows each holds**, each bridged from Bevy
   with the examples it unlocks written in its batch: more of Bevy's WGSL reached as its
   lighting is (the deferred buffers, a decal's tag and a volume's voxels), the widgets' events
   as observers, keys observed as they reach a field, and what the table then names most. When
   the captures have settled, they are compared whole with checked-in references by the
   workflow, a small share of pixels allowed to differ between devices, as 3DEngine does for its
   scenes. Transmission's glass spheres are missing from about one capture in four with TAA
   on, before `6a84286` as after it, so the cause is found before that job is red for them, or
   the example is compared with its spheres left out and the reason beside it.
4. **A build with no warnings, and a warning failing the workflow.** The managed build passes
   `-warnaserror` in the workflow once it is clean, with a warning that is right to keep turned
   off where it arises and its reason beside it, and `cargo` builds deny warnings the same way.
   The build before a commit runs with `--no-incremental` when it checks for warnings, since
   3DEngine's incremental build passed over a test project an earlier build without the flag
   had left up to date, and a warning reached `main` (its `2b39ddd2`).
5. **More of what bodies do**, from 3DEngine's `c5227118`, `8520dbe1`, `799a9d56`, `979c97be` and
   `53cd565f`: the speed a pair closed at on the contact's message, a ball joint kept within a
   cone, a distance joint whose range changes after it is made, bodies on layers whose pairs
   collide or not, which contacts, triggers, the character and rays follow, a body asleep waking
   when its layer changes, a body a game knows is fast swept over each step so it does not cross
   a thin wall, a slider joint with limits, a motor and its position, and how hard two touching
   bodies press, as the push alone and answered while they sleep.
6. **Two more of a character and a collider**, from 3DEngine's `52579d98` and `454e9276`: a
   character that crouches and stands from its component's height, and a collider that is the
   shape of the meshes an entity and those under it show, made once the model has loaded.
7. **A joint in a scene file, and a pad's sensors**, from 3DEngine's `e46058fc` and `73ce6326`:
   a joint as an entity naming its two bodies, so a level hangs a door where it stands, and a
   gamepad's gyro, accelerometer, touchpad and light where gilrs offers them. Two small things
   of the command line are checked in the same batch and taken if they are missing: `entity.set`
   writing a field that holds a list from items split by semicolons, a command that pretends
   files dropped on the window, a command's parameter with a default being left off, and a
   placed scene file spawned again when it is written while the level runs, under the entity
   that placed it and giving back what the old copy held (`5b2234d2`). A third: a command takes
   an enum member by its name alone, where `Enum.TryParse` takes any number as well, as
   `ConsoleWorldCommands.cs` reads gamepad buttons, axes and keys, and as 3DEngine's
   `InputCommands.TryName` does since `ef042886`, where a button of 100 stopped the program.
8. **The three shapes no wrapper types**, when a batch next touches the generator: a list inside
   a component (box shadows, gradients), an enum inside a variant (a sprite's slicer, an
   orthographic projection), and a range of numbers (`VisibilityRange`), which are the 13
   string paths the examples still hold.
9. **Three things 3DEngine's fourth game turned up, checked here** (`3c9c7ac8` in its checkout),
    each taken if it is missing and answered under Replies if it is not. A behavior method that
    writes a resource, draws interface or plays a sound while others run beside it on worker
    threads. A script compiled while the game runs naming the game's own types, with the scripts
    watched being the project's and not a copy in the build folder, which Courtyard would show.
    And a game written in behaviors alone with hundreds of entities, played by the workflow and
    profiled, which `games/Stress` measures and no game here plays.
10. **Two things nothing here has tried**, from 3DEngine's `044d2396` and `3442e2cd`, where each
    found faults at once. Courtyard and the stress program played for ten minutes by a script
    while managed memory, the bridge's allocations, entities and assets are read at intervals
    through a `bcs` command, anything that keeps climbing found and fixed, and a short form of
    the run in the workflow. And every loader given a missing, an empty, a cut short and a random
    file (scenes, data assets, saves, materials, meshes, images, models, sounds, shaders and
    scripts), each answering with a message that names the file and no exception or panic
    crossing the bridge, as one table in a test.
11. **The public surface written down, and release notes from the commits**, from 3DEngine's
    `fc5aef49`: a listing of every public type and member a tool writes from the built assembly,
    checked in, with a test that fails when the two differ, so a change to what a game calls is
    read as one, and the pack workflow writing the package's release notes from the commits
    since `build/version.txt` last changed. In the same batch it is checked whether a ray here
    stops at a sensor, which there threw a car's wheel and a character's ground check.
12. **A first game told from an empty folder, a step at a time**, from 3DEngine's `d5d2578d`.
    `docs/making-a-game.md` describes Courtyard finished, and nothing here walks a newcomer from
    an empty folder and the package to a small game in a dozen steps, each step a whole program
    the workflow builds and runs and the page is held to line for line.
13. **What a script host reads at each compilation**, from 3DEngine's `c06ec659`, where it took
    the Linux test job to the runner's 16 GB. `ScriptHost.References()` reads every loaded
    assembly with `MetadataReference.CreateFromFile` at each compilation, at line 190 of
    `ScriptHost.cs`, and each reference holds its file's whole image in native memory that only
    its finalizer gives back, while the GC's heap stays small and a full collection comes late.
    A host that compiles on each save gathers them. They are read once for the process and
    shared, as `EditorEval` keeps its own, and a test compiles a hundred times and finds the
    process holding within a few megabytes of what it held after ten, read before any
    collection. The row on an app's whole life in SHARED.md is checked in the same batch. So is
    whether a script's generation unloads when it is compiled again, as 3DEngine's
    `ScriptGenerationTests` holds since `d7e370ed` there, where a registration kept by the
    process held every generation and ran a stale script in every later app. `BehaviorsPlugin`
    passes over a collectible assembly's behaviors here, and whether a script's assembly adds
    schemas, commands or states to the lists of the process, as the module initializers the
    generator writes do for a game's, is read with it.

## Verdicts

None open.

## Decisions

1. **Commits stay local.** The owner pushes `main` from their own tools, and the working session
   commits and does not push, as COMMITS.md and AGENTS.md say. An earlier entry here that allowed
   pushing is withdrawn by the owner.
2. **AGENTS.md's bullet on SHARED.md is the owner's.** They approved it on 2026-10-04, and it is
   committed like any other change.
3. **A picture opens the example's own source, and not Bevy's live demo of it.** The owner chose
   it on 2026-10-05, and `57fc7e9` carried it out.
4. **AGENTS.md's bullet on NORM.md is the owner's, with the exception N 7.4 makes.** They approved
   both on 2026-10-05 in the reviewing session, with the plan for the norm. A working session
   that commits a change to its instruction file only on the owner's word in its own session
   is right to, and waits for that word.
5. **The examples written are brought to B 4 a group at a time.** The owner chose it on
   2026-10-05, over leaving them as they are and over doing them all before other work.
6. **A run that fails says what failed in a page, and the reviewing session is given no log.**
   The owner chose it on 2026-10-05, after a log pasted into the reviewing session ended it.
   The suite runs whole, and in parts only after a process is lost, and a test in which the
   engine logs an error fails unless it says it expects that error. The norm has these as
   N 6.7, N 6.8 and N 3.7, and the reviewing session reads a run's jobs and annotations from
   GitHub.

## Replies

**Now 3, a decal's tag.** A Slang material reaches Bevy's clustered decals as it reaches the
lighting, by WGSL the bridge puts in front of a shader that calls it, over Bevy's own
`ClusteredDecalIterator` and the textures Bevy holds for the decals. `bcs::decal_count`,
`bcs::decal_tag`, `bcs::decal_has` and `bcs::decal_sample` walk the decals over a point, and
`bcs::decals` lays them on a surface as the standard material does, color, metallic and roughness,
normal map and light given off. `clustered_decals` is written with the Slang port of Bevy's
`custom_clustered_decal.wgsl`, the icon tinted red and blue by the two decals' tags, and the
capture shows Bevy's scene. `LitShaderTests` colors cubes by the tag over them and lays a half
transparent decal on with `bcs::decals` beside the standard material laying it, within 12 a
channel, both skipping on a device whose standard material shows no decal. The preludes are now a
list (`programs/wgsl.rs`), each put in front of a fragment shader calling it and read through
stand-ins, which a native test holds. Two commits that move code alone came first, `programs.rs`
and `reflect.rs` split into parts (`5be8291`) and the lit shader's tests into `Assets`
(`56ad9a2`). The volume's voxels and the deferred buffers are next.

**Now 3, a volume's voxels.** `bcs::irradiance(mesh, normal)` is Bevy's own
`irradiance_volume_light` from a Slang material, the light the volumes over a point give a surface
facing a way, through a third prelude in the same list as the lighting and the decals.
`Render.TryImageSize` reads an image's width, height and depth once it has loaded, compressed or not
(`bcs_render_image_size`, ABI 205), in a module of its own, `render/images.rs`, since `assets.rs`
holds the other image calls and is on N 1.3's list. `irradiance_volumes` is written with them, its
voxels a cube each in the light the volume holds, the volume's box Bevy's `VOXEL_FROM_WORLD`
written as a translation, a half turn and a scale. Bevy's voxel shader works out the middle of the
voxel and then reads at the fragment's own place, so the port reads at the fragment and says why.
`tonemapping`'s image viewer is sized to the dropped image with the same call, the one thing it
left out, so it is written whole, though no test here drops a file on it. `LightProbeTests` draws
its computed volume's cube with a shader showing `bcs::irradiance` and finds the top green and the
front red as the standard material's are, and the volume's image 4 by 8 by 12 as made. The tests
moved to `Assets` first by a move alone (`67d179b`).

The deferred buffers wait behind the pointer's events as observers, which hold ten rows to their
one or two, since the item takes the gaps by rows. A material drawing into them needs a stage of
its own whose outputs follow the camera's prepasses, the normal at location 0 only with a normal
prepass and the motion at 1 only with motion vectors, which a Slang entry point cannot declare by
itself, and inputs that the prepass vertex shader in use writes, so it is a design of its own.
