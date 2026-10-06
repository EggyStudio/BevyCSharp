# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read. A stash of every changed file
takes what was written here since the last commit out of the tree until it is popped, so a stash
names its own paths.

Reviewed up to `1a4b821`. A Slang material reaches the clustered decals over it through Bevy's own
iterator, laying them on as the standard material does or reading each one's tag and textures, with
`clustered_decals` written and `LitShaderTests` holding it within 12 a channel (`4bbcec0`), and
reads the light an irradiance volume gives through Bevy's own function, `Render.TryImageSize` giving
an image's size (ABI 205), with `irradiance_volumes` and the whole of `tonemapping` written
(`1a4b821`); the light probes' tests moved to `Assets` by a move alone, so N 1.4's list stands at 96
to mend (`67d179b`). The deferred buffers wait behind the pointer's events as observers, ten rows to
their one or two, since a material drawing into them needs a stage of its own whose outputs follow
the camera's prepasses, and picking moves into the render profile, decided here on 2026-10-06, its
size and build time measured. No verdict is open.

Before them, two commits of moves alone split the bridge's `programs.rs` and `reflect.rs` into parts
and put the lit shader's tests in `Assets`, so N 1.3's list stands at 19 from 21, 12 of them in the
bridge, and N 1.4's at 97 to mend (`5be8291`, `56ad9a2`).

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

**Now 3, the pointer's events as observers.** A game observes what a pointer does to an entity as
Bevy's `Pointer<E>`, `ecs.Observe<Pointer<Click>>(button, on => ...)`, with Bevy's seventeen kinds
from `Over` to `Cancel` in `BevyCSharp/Input`, each carrying the entity, the pointer, where it is
and what it did, most with where it met the entity. The first observer of a kind has the bridge
spawn a Bevy observer of it (`pointer.rs`), which copies the event into one shape and calls C# with
the world on loan at its first step only, and C# takes it up the parents as Bevy does, so
`on.Propagate(false)` stops it (ABI 206).

Picking is in the render profile as you said: `mesh_picking` moved there and `sprite_picking`
added, file_watcher, ImGui and reflect_documentation staying in the editor. Measured as one clean
release build of the render profile each, on this machine and not in the container: 836 s and
138,009,608 bytes before, 839 s and 138,862,344 after, 3 s and 0.6% more, the first run having had
a short managed build overlap it. Mesh picking is compiled in and added only where an app asks,
`Config.MeshPicking`, which the editor has, since Bevy leaves it out of its default plugins for the
ray it casts at every mesh as the pointer moves, and its own programs add it where they pick one.
Sprites are picked as Bevy's default plugins pick them, a sprite carrying `Pickable`.

An offscreen run had no window for a pointer, so `SyntheticInput` there puts the mouse's pointer on
the image the run draws into, through Bevy's `PointerInput`, and its button and place reach
`ctx.Input`. Bevy's ray map asks for a primary window first and built no ray at all without one, so
meshes and sprites went unpicked offscreen, and `offscreen.rs` adds the rays it would have cast for
cameras drawing into that image, where Bevy's documentation of the map says to add such rays. An
offscreen editor now takes clicks on the scene through picking as a window does.

`sprite_picking`, `dragdrop_picking`, `entity_disabling` and `ui_drag_and_drop` are written, each
driven offscreen by `bcs` and seen to do what Bevy's does, 246 written, and `mesh_picking` and
`simple_picking` are on observers in the render profile. `mesh_picking` still marks the point under
the pointer from a ray it casts each frame, since the hits Bevy keeps on its pointer are not
reachable from C#, and its row says so. `PointerTests` holds the mirror's layout, a click on a node
heard there and at its parent and stopped at the node, a click on a cube saying where it met the
face and which way that faces, and a sprite picked where it is drawn and nowhere else.
`PickRayTests` moved to `Assets` first by a move alone (`96a61e6`). Two things were seen and not
mended. A move and a press at a new place in one frame miss, the hover being a frame behind, as a
window's mouse is in Bevy. And `dragdrop_picking`'s pale preview draws over the words "Drop here",
where Bevy sorts it under them, which was not traced.

Shared: a game observes what a pointer does to an entity as Bevy's `Pointer<E>`, taken here, and an
offscreen run is pointed at through its image, which may be worth a look in 3DEngine's offscreen
runs.
