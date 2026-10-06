# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read. A stash of every changed file
takes what was written here since the last commit out of the tree until it is popped, so a stash
names its own paths.

Reviewed up to `dfa22f2`. A mesh made in code is skinned to joint entities, four joints and weights
a vertex, inverse bindposes made from transforms and Bevy's `SkinnedMesh` with the bounds that
follow (ABI 217), `custom_skinned_mesh` written, 271, which closes the animation gap (`3208c70`);
the input focus moves between interface nodes by direction with edges a game draws before the
nearest node (ABI 218), `directional_navigation` and its overrides written, 273 (`f127b1f`); and a
touch and its phase moved into files of their names, N 1.2's list at 229 (`dfa22f2`). Feathers'
three widget examples need `bevy_feathers`, a crate the lock does not hold, which waits on the
owner's word; the reply on input as Bevy's messages (ABI 219) is being written, its commit to come.
Nothing was pushed since `0013c52`, whose run passed on both systems.

Before them, an app came to be told once for each state it never added rather than once for each
system scoped to it, the suite's own `Screen` behaviors having been the 45 lines, and the console
server says it is serving at the info level, which the page tells from a warning (`0013c52`), which
settles item 13. Verdict 3's cause, found by playing Courtyard from the package on lavapipe pinned
to four cores: the walk was planned in frames at sixty a second on the machine's clock, so each held
frame overshot past the coin, and `play.sh` sets a sixtieth of a second a frame through
`app.frametime`, said as the likeliest cause since nobody read the run's log; `build/step.py` and
`build/page.py` are taken from 3DEngine as the default shell of the game, examples, README and pack
jobs, the test and native jobs keeping their own, and `test.py` reads the page's code from `page.py`
(`aa55e0d`); Verdicts 2 and 3 settle with a pack run. The run of `0013c52` passed on both systems.

Before them, a warning came to fail the workflow on both sides, the managed build with
`-warnaserror` and every cargo build with `CARGO_BUILD_WARNINGS=deny`, every public member of the
library is documented, 297 generated enum variants by the generator and four types whose comments
had slid onto a neighbor given them back, and `NormTests.N_4_7` reads the README, the cheatsheet and
`docs/` for anyone named as deciding, with COMMITS.md's rule (`4308619`), which settles items 4 and
15 and takes N 2.2, N 4.7 and N 6.1; two commits of moves alone came beside it, 43 types into files
of their names and the compute tests split, N 1.2's list at 231 and N 1.4's at 88 to mend
(`5b38404`, `ff593c1`). The pack run of `421d4e1` failed in the package tests, Verdict 2's, and in
the step that plays Courtyard, which is Verdict 3. The run of `5b38404` passed on both systems.

The norm has 44 rules, and this engine stands at 29 checked, 4 with places listed, 2 to take
and 9 by review.


## Now

The owner asked that the work does not stop. A batch that ends is followed by the next item
here with no wait for a reply, and the list is long so that it does not run out. Items 4 to 6
and 8 to 12 are taken from [SHARED.md](SHARED.md).

1. **What the next page says.** The run of `ba5f72c` passed on both systems, the first green run
   with the page. Each push's run is read by the reviewing session, and a failure it names comes
   first here.
2. **The norm's lists are paid down.** A listed file is mended when a batch next touches it, in a
   commit of its own that moves code alone, the largest first where there is a choice, and a
   batch reads the lists for the files it will touch before it starts. The rules still to take
   each have their item: N 2.1 is the listing of item 10 and N 2.6 the table of bad files in
   item 9.
3. **The gaps, by how many rows each holds**, each bridged from Bevy
   with the examples it unlocks written in its batch: more of Bevy's WGSL reached as its
   lighting is (the deferred buffers, a decal's tag and a volume's voxels), the widgets' events
   as observers, keys observed as they reach a field, and what the table then names most. When
   the captures have settled, they are compared whole with checked-in references by the
   workflow, a small share of pixels allowed to differ between devices, as 3DEngine does for its
   scenes. Transmission's glass spheres are missing from about one capture in four with TAA
   on, before `6a84286` as after it, so the cause is found before that job is red for them, or
   the example is compared with its spheres left out and the reason beside it.
   `dragdrop_picking`'s pale preview draws over the words Bevy sorts it under (`b548987`'s reply),
   untraced, and is traced before those captures are compared.
4. **More of what bodies do**, from 3DEngine's `c5227118`, `8520dbe1`, `799a9d56`, `979c97be` and
   `53cd565f`: the speed a pair closed at on the contact's message, a ball joint kept within a
   cone, a distance joint whose range changes after it is made, bodies on layers whose pairs
   collide or not, which contacts, triggers, the character and rays follow, a body asleep waking
   when its layer changes, a body a game knows is fast swept over each step so it does not cross
   a thin wall, a slider joint with limits, a motor and its position, and how hard two touching
   bodies press, as the push alone and answered while they sleep.
5. **Two more of a character and a collider**, from 3DEngine's `52579d98` and `454e9276`: a
   character that crouches and stands from its component's height, and a collider that is the
   shape of the meshes an entity and those under it show, made once the model has loaded.
6. **A joint in a scene file, and a pad's sensors**, from 3DEngine's `e46058fc` and `73ce6326`:
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
11. **A first game told from an empty folder, a step at a time**, from 3DEngine's `d5d2578d`.
    `docs/making-a-game.md` describes Courtyard finished, and nothing here walks a newcomer from
    an empty folder and the package to a small game in a dozen steps, each step a whole program
    the workflow builds and runs and the page is held to line for line.
12. **What a script host reads at each compilation**, from 3DEngine's `c06ec659`, where it took
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

2. **The pack job of `421d4e1` fails in `NormTests.N_6_4` and `N_6_5`, the package tests, before
   they open the package.** Read from the owner's paste of the job: `DirectoryNotFoundException` for
   `BevyCSharp.Tests/bin/Release/net10.0/build/package/BevyCSharp.0.4.88.nupkg`. The step exports
   `BCS_PACKAGE` as `ls build/package/*.nupkg`, a path relative to the checkout, and the tests open
   it from their own working directory, `bin/Release/net10.0`, so the two never meet; the tests
   passed only where nobody set the variable and they skipped. Two things: the tests resolve a
   relative `BCS_PACKAGE` against the repository root, as the norm's other tests find their files,
   so the variable works from any directory, and the step exports an absolute path as well,
   `$PWD/...`, so the job does not lean on the test. The pack job was the first to run these tests
   with a package in hand, which is why the test suite never showed it. The same job's build prints
   21 warnings, CS1573, CS1735, CS8604, CS8620, xUnit2029 and RS1032, which is item 4's
   `-warnaserror` standing unpaid.

3. **The pack run of `421d4e1` fails in the step that plays Courtyard, with exit code 1 and no
   reason.** Read from the run's jobs: `package / play Courtyard` failed at its ninth step, `Play
   Courtyard`, after the bridge and the package were built, and the job's annotations hold the
   build's warnings and `Process completed with exit code 1`, nothing of the game. The pack run of
   `b263f6d` had skipped the step, so this is the first time the game was played from the package
   since the page came, and whatever stopped it is in a log nobody reads, as 3DEngine's first-person
   walk was (its Verdict 28). Two things, as there: the game is played here as the workflow plays
   it, from the packed package by `bcs`, to find what stopped it; and every step of the pack
   workflow that runs a program ends with a page or an `::error::` naming the step, the command, its
   exit code and its last lines at a warning or worse, which 3DEngine's `build/step.py` and
   `build/page.py` do, taken from there (SHARED.md), with their tests under `TestScriptTests`.
   Settled when a pack run plays Courtyard and says so.

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

7. **The page's repeated lines are warnings and errors.** The owner chose it on 2026-10-06, after
   the page of `98f6d8e5` repeated the engine's banner, so the section counts what is logged at
   warning or error or with no level and is left out when nothing repeats.

8. **The next package is 0.4.** The owner chose it on 2026-10-06 for the ABI's moves from 199 and
   the surface added since 0.3.2, `build/version.txt` holding `0.4` so the patch counts itself,
   packed when the owner chooses.

9. **A document a game's author reads names no one who decided.** The owner asked on 2026-10-06
   that release notes and the documents under `docs/`, the README and the cheatsheet give reasons
   and not who wanted what, which is N 4.7, and who chose what stays here under Decisions.

## Replies

**Now 3, input as Bevy's messages.** Each change of the keyboard, the mouse, a touch and a pad is
read as its message, one for each change in the order it came (`input_messages.rs`, ABI 219). The
bridge drains every kind into one array a frame through cursors of its own, and the app posts each
to the message bus as the window's messages are, so `ctx.Read<KeyboardInput>()` reads them as
Bevy's examples read theirs. `MouseButtonInput`, `MouseMotion`, `CursorMoved`, `MouseWheel`, the
touchpad's `PinchGesture`, `RotationGesture` and `DoubleTapGesture`, `TouchInput` with its
`TouchForce`, the pad's `GamepadConnectionEvent`, `GamepadButtonChangedEvent`,
`GamepadButtonStateChangedEvent` and `GamepadAxisChangedEvent`, and Bevy's ordered `GamepadEvent`
are new, each in a file of its name, and `KeyboardInput` is posted as it stands. `TouchPhase` gains
Bevy's `Moved` and `Canceled`, moved first out of `Input.cs` with `Touch` (`dfa22f2`).
`keyboard_input_events`, `mouse_input_events`, `touch_input_events` and `gamepad_input_events` are
written, 277, each printing its messages as C# writes the records. The mouse's capture rolls the
wheel alone, since a capture runs with no window and nothing drawn and a pretended button there is
refused, and the touch example prints nothing without a touch screen, beside `touch_input` among
the captures held to saying nothing. `InputMessageTests` holds a pad's connection, its button
pressed and let go and its stick tilted as messages of their kinds and in one order, a key, a
button and the wheel offscreen, and the mirror's layout field by field. A stick's message carries
the value Bevy filters through its dead zone, 0.579 for 0.6, where the frame's `Gamepad` reads 0.6.
The suite passed, 1,099 with 9 skipped. The fonts' four wait on Bevy's system font discovery, which
on Linux brings `yeslogic-fontconfig-sys`, a crate the lock does not hold, and links fontconfig,
asked of the owner beside `bevy_feathers`. Bevy's math, three examples, is next.

**Now 3, Bevy's math, points sampled in a shape.** `Cuboid` and `Sphere` are shapes as values,
measured as Bevy measures them, each sampling a point inside it or on its surface from a `Random` a
game seeds, as Bevy's `ShapeSample` does. A box's surface is landed on by each face's area and a
ball's inside filled evenly, through the cube root of an even draw, as Bevy's are. `random_sampling`
is written, 278, scattering a hundred points inside its cube and a hundred on its surface for its
capture, which `capture-example.sh` drives by name, its points other than Bevy's since .NET's
generator draws its own from the seed. Its light is Bevy's default point light, a million lumens,
where `LightSettings` starts at ten thousand. `ShapeSamplingTests` holds the box's faces landed on
as their areas say, a fifth and two fifths, an eighth of the ball within half its radius, and a seed
drawing the same points again. `docs/math.md` is new, linked from the README, which stands at its
320 lines. The suite passed, 1,102 with 9 skipped. Bounding volumes and their casts are next.
