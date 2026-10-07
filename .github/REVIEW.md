# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read. A stash of every changed file
takes what was written here since the last commit out of the tree until it is popped, so a stash
names its own paths.

Reviewed up to `4eb8838`. The state slots moved into a module of their own and the patch delegate
into a file of its name, and the joint state tests into their area's folder, N 1.2's list at 228,
N 1.3's at 16 and N 1.4's at 87 to mend (`fee3190`, `4eb8838`). Hermite, cardinal, B-spline and
Bezier splines are made into cubic curves sampled along as Bevy makes them, and `cubic_splines` is
written (`9f5537a`), the last of the math gap. Shapes in the plane give boxes and circles about them
as Bevy bounds them, rays and swept volumes meet those as Bevy casts them, and `bounding_2d` is
written (`cbe9a1f`). Verdict 4's mend: the two plays name their logs folder in their own steps,
where the runner context is allowed, and `WorkflowTests` holds every expression in the workflows to
the contexts GitHub allows at its place, naming the refused line of `aa55e0d`'s file when run over
it, listed under N 1.4 with its reason (`3397439`); the verdict settles when a run starts its jobs.
The suite: 1,109 passed, 9 skipped.

Before them, each change of the keyboard, the mouse, a touch and a pad is read as its Bevy message
in the order it came, 14 message records drained into the bus each frame (ABI 219), and the four
input event examples are written, 277 (`b0f9841`); a box and a ball are shapes as values that points
are sampled in and on as Bevy samples them, and `random_sampling` is written (`a31e3b3`). The owner
pushed, and both workflows failed before any job began, which is Verdict 4.

Before them, a mesh made in code came to be skinned to joint entities, four joints and weights a
vertex, inverse bindposes made from transforms and Bevy's `SkinnedMesh` with the bounds that follow
(ABI 217), `custom_skinned_mesh` written, 271, which closes the animation gap (`3208c70`); the input
focus moves between interface nodes by direction with edges a game draws before the nearest node
(ABI 218), `directional_navigation` and its overrides written, 273 (`f127b1f`); and a touch and its
phase moved into files of their names, N 1.2's list at 229 (`dfa22f2`). Feathers' three widget
examples need `bevy_feathers`, a crate the lock does not hold, which waits on the owner's word; the
reply on input as Bevy's messages (ABI 219) is being written, its commit to come. Nothing was pushed
since `0013c52`, whose run passed on both systems.

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

4. **Both workflows fail at `a31e3b3` before any job begins.** Read from the runs: `build.yml`
   and `package.yml` each ended in failure within a minute of the push with no job at all, which is
   the failure of a workflow file GitHub refuses, and both files parse as YAML. What changed in them
   since `0013c52`'s green run is `aa55e0d`: `build/step.py` as the default shell of four jobs, and
   the game job's `env` with `BCS_STEP_LOGS: ${{ runner.temp }}/courtyard...`. A job's `env` cannot
   read the `runner` context, which GitHub gives to steps alone, so the file is refused, and
   `build.yml` with it, since it calls `package.yml`. The value moves into the steps that need it,
   or `step.py` reads `RUNNER_TEMP` itself at run time, and a check that every `${{ }}` in the
   workflows names a context its place allows runs with the norm's tests, so a refused file is seen
   before a push. Settled when a run of the mend starts its jobs.

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

**Now 3, the states, three examples.** Each transition of a state reaches C# as Bevy's
`StateTransitionEvent` of its enum, drained from the bridge into the bus each frame and read the
frame after (ABI 220). The first value is a move from nothing, and a value set again is a move to
itself, which runs its `[OnExit]`, its `[OnEnter]` and an `[OnTransition]` from it to itself, as
Bevy's `NextState::set` does. `EcsWorld.DespawnOnEnter` is Bevy's own, and `DespawnWhen` takes a
rule over the transition, asked as the transition is read, so its entity goes a frame later than
Bevy's would. A state carries three computed states rather than two, and a joint reads a state
computed from one state by working it out again from that state's value, which `computed_states`
needs for its tutorial. A computed state that is absent is no longer reported as a state never
added. `state_scoped` and `computed_states` are written, 282, the second's buttons in a
`MenuButton` behavior as B 4 has it, and `custom_transitions` is written in part, its restart run
by the state's own entering and leaving where Bevy's runs schedules of its own, which TODO now
lists among the parts. The menus are clicked in a window alone, so the games were checked
offscreen started in play, with turbo, the pause, both tutorial texts and the restart.
`JointStateTests` moved into `Core` first (`4eb8838`). The README's gallery took a row, so the
package's sentence went to one line and the package's layout to BUILDING.md. The suite passed,
1,127 with 9 skipped. A second window is next.

Shared: `DespawnOnEnter` and `DespawnWhen`, neither of which 3DEngine has beside its
`DespawnOnExit`, and a joint state reading a computed one. 3DEngine has the transition as a message
already, as `StateTransition<TState>`.

**Now 3, a second window, two examples.** A window past the first is Bevy's `Window` spawned
through reflection, as it could be already, and the bridge adds what reflection cannot name, a
camera aimed at the window's entity, `Render.SetCameraTarget(camera, window)` (ABI 221). An
offscreen run opens no window, so each window a game spawns draws into an image of its own at the
window's size and scale, which `Render.Screenshot(path, window)` and a new `window.shot` read, and
`window.list` names each window by its index and title. Bevy tells pictures drawn into an image
apart by their scale as well, so a capture asked at a scale of one, of a window at two, was given a
blank picture of its own and wrote it over the window's. The scale is now kept with the image and
named by the capture, and `SpawnedWindowTests` holds both scales. `multiple_windows` and
`multi_window_text` are written, 284, both windows of each checked offscreen, the second's text
twice as large. `monitor_info` waits on a window made fullscreen on a monitor named by its entity,
which reflection cannot name either, and joins the single rows in TODO. The suite passed, 1,130
with 9 skipped. Next is `mesh2d_arcs`, Bevy's circular sector and segment with their bounds and
the angle their meshes map an image at, and then `shader_material_2d`, a Slang material drawn on a
2D mesh.
