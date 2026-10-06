# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read. A stash of every changed file
takes what was written here since the last commit out of the tree until it is popped, so a stash
names its own paths.

Reviewed up to `7f5286c`. An animation graph is built in code from blends and clips, played several
nodes at once at their weights and masked by groups of bones (ABI 216), and `animation_graph` and
`animation_masks` are written (`5d5a982`). Verdict 2's mend: `NormTests.Package()` reads a relative
`BCS_PACKAGE` from the repository root and fails naming a file that is not there, and the pack step
exports a whole path, tried four ways (`7f5286c`); the verdict settles when a pack job passes. Moves
on the way take N 1.2's list to 233, N 1.3's to 17 with 11 in the bridge and N 1.4's to 88 to mend.
The run of `2158ee2` passed on both systems and `7f5286c`'s is under way.

Before them, a game came to place its own events on an animation clip, made in code or loaded from a
model, heard at the player or at a target as the clip reaches them (ABI 214), and `animation_events`
and `animated_mesh_events` are written, 268 (`2158ee2`). The run of `421d4e1` passed on both
systems.

Before them, an animation clip came to be built in code from curves sampled or eased by Bevy's own
easing, aimed at entities by name and played from a graph (ABI 213), and `animated_transform`,
`animated_ui` and the whole of `eased_motion` are written with it (`421d4e1`). The run of `df97905`
passed on both systems.

The norm has 44 rules, and this engine stands at 29 checked, 4 with places listed, 2 to take
and 9 by review.


## Now

The owner asked that the work does not stop. A batch that ends is followed by the next item
here with no wait for a reply, and the list is long so that it does not run out. Items 5 to 7
and 9 to 13 are taken from [SHARED.md](SHARED.md).

1. **What the next page says.** The run of `ba5f72c` passed on both systems, the first green run
   with the page. Each push's run is read by the reviewing session, and a failure it names comes
   first here.
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
   `dragdrop_picking`'s pale preview draws over the words Bevy sorts it under (`b548987`'s reply),
   untraced, and is traced before those captures are compared.
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
14. **The warnings the suite repeats, read from the page.** The page of `421d4e1` repeats, on
   both systems, 45 lines of `A system is scoped to Screen.Playing, but no state of type Screen was
   added, so it will never run`, from examples whose systems are scoped by `[InState]` while the app
   running them added no such state, and 4 lines of `[bcs] serving on`, counted because they carry
   no level. Each scoped system is either meant to stay silent in an app without its state, and then
   the warning is said once per app rather than per system or the example adds its state, or it is a
   system that never runs by mistake, mended; and a line of `bcs`'s own carries its level so the
   page can tell it from a warning.
15. **N 4.7 taken** (Decision 9), with item 4. `NormTests` over `README.md`, `CHEATSHEET.md` and
   `docs/` looks for `the owner`, `the reviewing session` and `REVIEW.md` and passes with no list,
   the release notes step item 11 brings does the same when it comes, and COMMITS.md says a message
   names no one who decided, since release notes are made from the messages.

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

**Now 4, a build with no warnings, and N 2.2 and N 6.1.** The 21 warnings the pack job printed are
mended, and a warning now fails the workflow. The test job builds the solution with `-warnaserror`,
the game job builds Courtyard the same way, and every cargo build in the workflow runs with
`CARGO_BUILD_WARNINGS=deny`, cargo's `build.warnings` setting, stable in 1.98. It fails a build that
printed a warning and judges the warnings cargo replays for a cached crate too, where `RUSTFLAGS`
would be part of every crate's fingerprint and build them all again. The bridge printed none in any
profile, its tests' targets included, and Courtyard and Stress build with none on the last package
packed here.
`BevyCSharp.csproj` makes CS1591 an error with the faults of documentation `3DEngine.csproj` lists,
which takes N 2.2. Of the 301 public members it found undocumented, 297 were the variants of the
enums the reflected wrappers' generator writes, which it now documents one by one, and four were
types whose comments had slid onto a neighbor in an earlier move, `AssetHandle`, `CameraSettings`,
`SpriteSettings` and `UiJustify`, each given its own back. `Render.TryReadMaterial` says its
settings are there when it answers true, which mended the three examples' CS8602. BUILDING.md has a
section on warnings with the check before a commit, `dotnet build --no-incremental -warnaserror` and
the three profiles' `cargo check` denying warnings, and COMMITS.md sends a commit through it. The
batch's library files and two of its tests were on N 1.2's, 1.3's and 1.4's lists and were mended
first, in `5b38404`, which moved 43 types into files of their names, split `ComputeShaderTests` in
two and moved it and `EcsWorldTests` into their areas. The suite passed, 1,067 with 9 skipped.

**Now 15, N 4.7 taken.** `NormTests.N_4_7` reads `README.md`, `CHEATSHEET.md` and every page of
`docs/` for `the owner`, `the reviewing session` and `REVIEW.md`, case ignored since a sentence can
begin with one, and passes with no list, none of them being there. A page made to name the owner
failed it. COMMITS.md says a message gives the reason for a change and names no one who asked for
it or decided it, since the release notes are made from the messages, citing N 4.7. The step that
writes the notes comes with item 11 and takes the same words. The norm's 18 tests pass with it, and
item 14 is next.

Shared: the build before a commit runs with `--no-incremental` and `-warnaserror` here now, written
in COMMITS.md and BUILDING.md, as 3DEngine's `2b39ddd2` has it. A generator that writes public code
documents each member, so the library's CS1591 holds over what it writes as well.

**Now 13, the warnings the suite repeats.** The 45 lines came from the suite's own behaviors and not
from examples. `StateTests.cs` declares three systems scoped to `Screen.Playing`, and the fifteen or
so test apps that discover every behavior to test something else never add `Screen`, so each said
the line once a system. Those systems are meant to stay silent there, and two tests hold an app
without `Screen` (`ReadingAStateThatWasNeverAddedSaysSo`, `AScopedBehaviorInAnAppWithoutThatStateDoesNotRun`),
so declaring the state on its enum was not the mend. An app is now told once for each state it
lacks, naming the first value a system is scoped to and saying every other system scoped to that
state is idle too, since a game with many systems scoped to a state it forgot made one mistake.
That leaves about fifteen lines a run, one an app, which the page will show while the suite runs
apps of that kind. `StateScopeReportTests` holds one line an app over two apps, and a report made to
fire every time failed it with three. The `bcs` banner begins with `INFO`, as Bevy's lines carry
theirs, so the page's `LOGGED` pattern reads it as information and leaves it out. `docs/states.md`
says a state never added is reported once. The suite passed, 1,069 with 9 skipped. The move before
it (`ff593c1`) took the toggle registry and the registration scope out of `BehaviorConditions.cs`,
N 1.2's list at 231. Verdict 3 is next.
