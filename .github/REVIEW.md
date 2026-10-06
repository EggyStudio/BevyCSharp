# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it already keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read. A stash of every changed file
takes what was written here since the last commit out of the tree until it is popped, so a stash
names its own paths.

Reviewed up to `9f74107`. N 2.10 finds every method native code calls and fails for a call no
catch covers (`869c9fb`), eight of them, and two caught nothing before taking a lock, the
computed and the joint state's rule, which take it inside the `try`. N 2.9 is `FileHandleTests`
over fourteen loaders in a folder from `TestFolder`, the one helper N 3.4 names, which is new
here (`9f74107`), and none holds a file. This engine stands at 22 checked, 4 with places listed,
8 to take and 9 by review.

Before them, the four costs measuring found were mended, the last with `Gizmos.Lines`
handing a run to the bridge as lines (`de790e4`), which takes many_gizmos to 8.59 ms beside
Bevy's 6.95, a third number for the owner beside 199 and 200, the bridge's being 201. Four of the
norm's checks are in (`a733ca2`): the notices under a test of N 6.5's own name, the packages
against BUILDING.md's list (N 2.8), `ScriptTests` over the scripts the Windows and macOS jobs
run (N 6.6), with the one `grep -oP` written as `grep -oE`, and the table of examples checked in
the workflow (N 5.2).

Before them, the last seven of Bevy's stress tests were written and measured, every
one within two milliseconds of Bevy's and transform_hierarchy faster than it (`d71ff6a`), which
empties the column of rows that can be written, three of the seven in part for what Bevy does
not reflect. `gizmos.rs` is split by moves alone (`2e3fbc3`) and off N 1.3's list.

Before them, `Render2d.SetSpriteFrames` moves many sprites and sprite meshes to the
frames of their sheets in one call, which takes many_animated_sprites from 15.95 ms to 6.75
beside Bevy's 6.51 and many_animated_sprite_meshes from 212.54 to 13.84 beside Bevy's 14.34,
measured again into PERFORMANCE.md, with `SpriteFrameTests`. The call is new to the bridge, so
the number B 1's check reads is 200, a second number for the owner to set beside the 199 the
wireframes took.

Before it, `Gizmos.Lines` builds a long run in an array kept for its thread
between calls, which takes many_gizmos from 12.39 ms to 10.68 beside Bevy's 6.97, measured
again into PERFORMANCE.md. What is left there is each line written into the bridge's general
description of a shape. `App.cs` and `Gizmos.cs` are split by moves alone (`10b5588`,
`f91aa59`) and are off the lists of N 1.3 and N 1.2.

Before them, Bevy's wireframe plugins are added only to an app whose config asks since `9ff5abc`
(`Config.Wireframes`), which gives a frame of a hundred thousand sprite meshes 8 ms back, 13.96
to 5.52 beside Bevy's 5.23, measured again into PERFORMANCE.md. The number B 1's check reads
is 199, and the break is said for the owner to number: a game that draws wireframes asks for
them in its config, and `Render.SetWireframe` refuses in an app that did not.

Before it, `b72f28d` split `Native.cs` and `Render.cs` into partial files by area, by moves
alone, with the five other types `Render.cs` held in files of their own, and `84a55e0` split
`interop.rs` the same way.

Before it, thirteen of Bevy's stress tests are written (`1fc9c9c`), each measured beside Bevy's
own program built from its source, and PERFORMANCE.md names the two scripts that measure and
four costs with their causes: the 2D wireframe plugin added to every app, an array made for
every call of `Gizmos.Lines`, a call for every sprite whose frame turns, and reflected writes
that feed themselves once frames are slow. `many_sprites` was read against Bevy's source and
carries its numbers. Two faults that measuring found are mended with a test each, a request
answered at 30 seconds whatever `--timeout` said and a wrapper's `Option` losing what its
record does not hold. `app.rs` is split by moves and off N 1.3's list (`6991a12`).

Before them, N 7.2's check leaves out a commit of `build/version.txt` alone (`49781de`), as the
owner's `08fb5b5` setting 0.4 is, and reads a list for the owner's other commits. The notices
name the contributors to a crate's repository where nothing else names a holder, and
`bcs_assets_carried` is under the guard, which leaves one entry point on B 3's list.

Before it, the package's notices were settled on the reply, which was read (`00c3db9`).
`THIRD-PARTY-NOTICES.md` names 569 of the 570 crates of the lock, the one left out being the
bridge's own, each with its license, the holders its files name and the texts, 69 of them
written once each, with Bevy's default font, the library's packages and whose the examples and
their assets are. It is packed at the package's root, N 6.4's test holds it to the lock on every
run and in the package where there is one, and the pack workflow runs the script's check. The
218 examples written each say at their head which of Bevy's they are written from. The package
the owner makes next carries all of it.

Before it, four commits were settled, the last being the tests of what is no area of the library
in folders named for what they test (`a714bb8`). The table stands at 239 written, 19 written in
part, none that can be, 105 missing and 58 that do not apply. The lists stand at 337 places for
N 1.2, 25 for N 1.3, 113 for N 1.4 and 55
for N 3.4, with one entry point of B 3 to bring under the guard, `bcs_shader_entity_program`, in
the batch that splits `render/shaders.rs`.

The owner took three more rules into the norm on 2026-10-05, N 2.9, N 2.10 and N 6.5, and then
N 3.7, N 6.7 and N 6.8, and N 6.6 came from 3DEngine with its check. Item 1 has what is left of
them.

## Now

The owner asked that the work does not stop. A batch that ends is followed by the next item
here with no wait for a reply, and the list is long so that it does not run out. Items 3, 6 to 8
and 10 to 14 are taken from [SHARED.md](SHARED.md).

1. **The norm's checks, what is left of them.** The check of N 1.5 reads top folders as the norm
   has it since, with a row for `docs/`, and the check of N 1.4 leaves out a test of what is no
   area of the library with that reason. The rules still `to take` here each have their item:
   N 2.2 and N 6.1 are the settings of item 5, N 2.1 the listing of item 12, N 2.6 the table of
   bad files in item 11, N 2.7 item 2, and N 2.5, N 2.8 and N 5.2 are a game published native in
   the workflow, the list of packages in BUILDING.md with a test that holds the project files to
   it, and the workflow running `build/examples-table.py` and failing when what it writes differs
   from the table checked in. The lists are paid down as the norm says, a listed file mended
   when a batch next touches it, in a commit of its own, the largest first where there is a
   choice, `RenderShaders.cs` among the library's and `render/shaders.rs` among the bridge's.
   The one entry point B 3 still lists is brought under the guard.

   The owner took three more rules into the norm on 2026-10-05. A loader keeps no file open
   once a load returns (N 2.9), which one test over every loader finds, on Linux among the
   entries of `/proc/self/fd` and on Windows by opening the file for writing with no sharing, as
   3DEngine's `FileHandleTests` does. No exception leaves a callback native code calls (N 2.10),
   which here is every method the bridge calls back into, each found by the attribute or the
   delegate it is handed over with and held to catching everything. And the package carries the
   notices (N 6.5), which the test of N 6.4 holds and which gets a test of its own name.

   Three more came the same day, after a log pasted into the reviewing session ended it, and a
   fourth with 3DEngine's check. A script that more than one system runs uses only what each
   system's tools read (N 6.6), which is a test as 3DEngine's `ScriptTests` is (`fd7b17f3`
   there) over the scripts that the jobs of `package.yml` on Windows and macOS run and those
   they call, looking for `sed -i`, `grep -P`, `readarray`, `date -d`, `stat -c`, `sha256sum`
   and `${x,,}`. The one such line today is the `grep -oP` at line 316 of
   `build/build-native.sh`, which is written with `grep -oE` or listed as left out if its branch
   runs on Linux alone. 3DEngine's test of N 2.10 is in at `48fbb663` there and finds the
   methods three ways, by `[UnmanagedCallersOnly]`, by the delegate type they are handed over
   as, and by a binding's virtual methods that its own callbacks reach, and six of its twelve
   caught nothing.

   A test in which the engine logs an error fails, unless the test says it expects that error
   (N 3.7), with a list of the tests that log one today. The errors of this engine are named
   first, an exception from a behavior that the library logs and goes on from, a panic the
   guard catches and whatever the bridge logs at Bevy's error level among them, and then how a
   test hears them, the bridge's log being the process's while tests run side by side. With it,
   it is checked whether a behavior that throws in every frame writes its trace every frame, as
   3DEngine's schedule did, and if so it is logged in full once and counted after.
   3DEngine's hook is `FailOnLoggedErrors` at `99b9c97d` there, with `[ExpectsError]` on a test
   of a failure, and its schedule counts since `c35472ba`. The hook hears by the thread there,
   which Verdict 18 of its REVIEW.md mends, so the ears here follow a test over its awaits
   from the start.

   And a run that fails says what failed in a page, with a test process held to a time and a
   memory (N 6.7, N 6.8). 3DEngine's script is in at `42b162d9` there, `build/test.py` with
   `TestScriptTests` and a stand-in for `dotnet` that hangs, grows and dies, and it is taken
   here. Its annotations carry a cause's first line only, which Verdict 17 there mends, so a
   cause's whole entry is written from the start. The tests here are three processes,
   `cargo test` twice and `dotnet test`, so each is a part from the start and the page has a
   line for each, with the bridge's failures read from what `cargo test` prints under
   `failures:`.
2. **An example is a program somebody could write on the package.** A picture in the README
   opens an example as the way to do a thing, and what opens is written in words the package
   does not have. `BevyCSharp.Examples/Example.cs` holds helpers, in its own words for what
   Bevy's examples say in one word and the bridge in several, and 208 of the 231 examples call
   `app.Startup`, 171 `app.Update`, 56 `Scene.Srgb8` and 25 `SpawnGltf`, beside a mesh spawned
   with its material and its place in one call, a point light, a camera, `Slerp` and `Hsl`. None
   of it compiles outside the examples project. Each of those helpers is something a game says
   in several calls too, so they become the package's own, named and documented as the rest is
   and in the cheatsheet, and `Example.cs` keeps only what drives an example for its capture.
   A check builds a handful of examples in a project of their own on the packed package, as the
   README's walk does, so one that leans on the examples project fails it.
   `stress_tests/StressTest.cs` is such a helper as well. The window's three settings are said
   by each test, as Bevy's says them, and the log of frame times is Bevy's own
   `FrameTimeDiagnosticsPlugin` and `LogDiagnosticsPlugin`, added by the bridge where an app
   asks, so the two programs measured log by the same code and the C# one runs no system of
   its own for it.

   The README's first program is `[Behavior]` on a struct, which 8 examples use while 136 keep
   their state in static fields. Where Bevy's example keeps state on an entity, in a component
   with a system over it, as `cooldown` does with a timer on each button, the example here keeps
   it on the entity in a behavior, and a static field is for what Bevy keeps in a resource or a
   `Local`. The examples written are brought over a group at a time, EXAMPLES.md saying how many
   are, the helpers first since every example reads shorter for them.
3. **What a kinematic body carries, and a clock stepped by hand**, both from a red run of
   3DEngine's on Windows, mended there in `966c2c88`, `15fa305a` and `ee3b47dd`, and measured here
   before anything is changed. There the engine measured where the model put it, 1.75 against
   1.74 at 144 frames a second, and holds 2.00 at every rate since.
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
4. **The gaps, by how many rows each holds**, each bridged from Bevy
   with the examples it unlocks written in its batch: more of Bevy's WGSL reached as its
   lighting is (the deferred buffers, a decal's tag and a volume's voxels), the widgets' events
   as observers, keys observed as they reach a field, and what the table then names most. When
   the captures have settled, they are compared whole with checked-in references by the
   workflow, a small share of pixels allowed to differ between devices, as 3DEngine does for its
   scenes.
5. **A build with no warnings, and a warning failing the workflow.** The managed build passes
   `-warnaserror` in the workflow once it is clean, with a warning that is right to keep turned
   off where it arises and its reason beside it, and `cargo` builds deny warnings the same way.
6. **More of what bodies do**, from 3DEngine's `c5227118`, `8520dbe1`, `799a9d56`, `979c97be` and
   `53cd565f`: the speed a pair closed at on the contact's message, a ball joint kept within a
   cone, a distance joint whose range changes after it is made, bodies on layers whose pairs
   collide or not, which contacts, triggers, the character and rays follow, a body asleep waking
   when its layer changes, a body a game knows is fast swept over each step so it does not cross
   a thin wall, a slider joint with limits, a motor and its position, and how hard two touching
   bodies press, as the push alone and answered while they sleep.
7. **Two more of a character and a collider**, from 3DEngine's `52579d98` and `454e9276`: a
   character that crouches and stands from its component's height, and a collider that is the
   shape of the meshes an entity and those under it show, made once the model has loaded.
8. **A joint in a scene file, and a pad's sensors**, from 3DEngine's `e46058fc` and `73ce6326`:
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
14. **What a script host reads at each compilation**, from 3DEngine's `c06ec659`, where it took
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
