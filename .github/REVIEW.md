# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read. A stash of every changed file
takes what was written here since the last commit out of the tree until it is popped, so a stash
names its own paths.

Reviewed up to `174ee19`. An enum inside a variant is a record of its own in the wrappers, as an
orthographic projection's `ScalingMode` and a sprite slicer's two `SliceScaleMode`s are, a plain C#
enum where none of its variants holds a value, and a nullable for an `Option` there, written by
choosing its variant after the outer one's and read by switching on it; the bridge makes a struct
Bevy registers no default for from its fields' defaults wherever one is needed, so the sliced
variant can be chosen; and six examples write through `SpriteRef`, `ProjectionRef`, `TextFontRef`
and `WindowRef` where they wrote JSON or a plain enum, `ExampleStringPathTests` following the
generator into those enums (`5f2d3d0`), the second of item 4's three shapes, with the list inside a
component left to come. Two commits of moves alone split `ComponentSchema.cs` into six files and the
generator's records into a part of their own, N 1.2's list at 206 and N 1.3's at 13 with 8 in the
bridge (`af8e870`, `174ee19`). The run of `75e8953` passed on both systems, Windows at 09:39, which
settles item 1's wait; the owner pushed `af8e870` at 09:54 and `174ee19` at 09:57, and the run of
`174ee19` passed on both systems at 10:11, the run of `af8e870` cancelled by the push after it. The
owner started the pack run of `75e8953` at 09:46, Verdicts 2 and 3's, in progress at 10:17. The
suite: 1,180 passed, 9 skipped.

Before them, a range of numbers, Rust's `Range<f32>`, came to be a `FloatRange` of its two ends in a
wrapper, read and written whole through Bevy's JSON since a reflect path stops at the range, drawn
by the inspector as two numbers, and given the empty range at zero by the bridge where Rust
registers no default, so a component holding one is inserted through its wrapper; the description
dumped again adds the six ranges Bevy's components hold, a viewport's depth among them, and
`visibility_range` writes its margins through `VisibilityRangeRef` with no string path, the first of
item 4's three shapes (`75e8953`), after the bridge's reflection module was split into modules of
its own, N 1.3's list at 14 with 8 in the bridge (`5262beb`). The owner pushed `75e8953` at 09:26,
and its run started its jobs, which settles Verdict 4; the docs and Linux jobs passed, and Windows
was running at 09:40. The suite: 1,179 passed, 9 skipped. The owner decided on 2026-10-07 that the
suite runs on macOS too, that Feathers' and the camera controllers' crates may be enabled after the
other gaps, and that system font discovery stays off (Decisions 10 to 13), so N 6.2 is raised and
item 5 takes the matrix.

Before them, a level came to describe a joint as an entity of its own with a `JointBetween` naming
its two bodies' entities, placed where they join and turned so that its up direction is a hinge's
axis, a slider's line or a ball joint's cone, made once both bodies are and again when the component
or a body changes, refused once in the log for a static body, taken away with its entity, and
answered by `JointOf` for a game to drive (`c98010a`). A command's parameter with a default can be
left off, in brackets in its usage and `optional` in the schema; a key, a button, an axis and an
enum field are read by their names alone, `TryName`, so a button of 100 is refused; `entity.set`
writes a list from items split by semicolons and a color, a `Vec2` or a `Vec4` from its numbers; and
`input.drop` sends a `FileDropped` for each path (`678d860`). A placed scene file written while the
level runs is spawned again under its root where the asset root is watched, the old copy despawned,
the overrides applied again, what the level added under the old nodes put back under the new ones
and `WorldInstanceReady` posted again (`3270d9e`). Item 4 is settled but for the pad's sensors,
which gilrs 0.11.2 reads none of, the touchpad's click reaching `bevy_gilrs` as a button it drops,
so a second reader of the pads beside gilrs, SDL's or hidapi's, is a dependency that waits for the
owner's word. The suite: 1,178 passed, 9 skipped.

The norm has 44 rules, and this engine stands at 28 checked, 4 with places listed, 3 to take and 9
by review.


## Now

The owner asked that the work does not stop. A batch that ends is followed by the next item here
with no wait for a reply, and the list is long so that it does not run out. Items 6 to 11 are taken
from [SHARED.md](SHARED.md).

1. **What the next page says.** The run of `174ee19` passed on both systems, and the pack run of
   `75e8953` was in progress at 10:17. Each push's run is read by the reviewing session, and a
   failure it names comes first here.
2. **The norm's lists are paid down.** A listed file is mended when a batch next touches it, in a
   commit of its own that moves code alone, the largest first where there is a choice, and a
   batch reads the lists for the files it will touch before it starts. The rules still to take
   each have their item: N 2.1 is the listing of item 8 and N 2.6 the table of bad files in
   item 7.
3. **The gaps, by how many rows each holds**, each bridged from Bevy with the examples it unlocks
   written in its batch: more of Bevy's WGSL reached as its lighting is (the deferred buffers, a
   decal's tag and a volume's voxels), the widgets' events as observers, keys observed as they reach
   a field, and what the table then names most. When the captures have settled, they are compared
   whole with checked-in references by the workflow, a small share of pixels allowed to differ
   between devices, as 3DEngine does for its scenes. Transmission's glass spheres are missing from
   about one capture in four with TAA on, before `6a84286` as after it, so the cause is found before
   that job is red for them, or the example is compared with its spheres left out and the reason
   beside it. `dragdrop_picking`'s pale preview draws over the words Bevy sorts it under
   (`b548987`'s reply), untraced, and is traced before those captures are compared. Feathers' three
   examples and the two camera controllers follow the other gaps, their crates allowed (Decisions 11
   and 12) on the owner's word in the working session, and the four font examples stay missing
   (Decision 13).
4. **The three shapes no wrapper types**, when a batch next touches the generator: a list inside
   a component (box shadows, gradients), an enum inside a variant (a sprite's slicer, an
   orthographic projection), and a range of numbers (`VisibilityRange`), which are the 13 string
   paths the examples still hold.
5. **The suite on macOS too.** `macos-latest` joins `package.yml`'s `test_os` default and
   whatever `build.yml` passes it, so every push runs the suite on the three systems the package
   ships for, as the owner chose (Decision 10); whatever fails there is read from the page and
   mended, and N 6.2 is then checked, which the Conformance table holds to take until the first
   green macOS job.
6. **Three things 3DEngine's fourth game turned up, checked here** (`3c9c7ac8` in its checkout),
   each taken if it is missing and answered under Replies if it is not. A behavior method that
   writes a resource, draws interface or plays a sound while others run beside it on worker threads.
   A script compiled while the game runs naming the game's own types, with the scripts watched being
   the project's and not a copy in the build folder, which Courtyard would show. And a game written
   in behaviors alone with hundreds of entities, played by the workflow and profiled, which
   `games/Stress` measures and no game here plays.
7. **Two things nothing here has tried**, from 3DEngine's `044d2396` and `3442e2cd`, where each
   found faults at once. Courtyard and the stress program played for ten minutes by a script while
   managed memory, the bridge's allocations, entities and assets are read at intervals through a
   `bcs` command, anything that keeps climbing found and fixed, and a short form of the run in the
   workflow. And every loader given a missing, an empty, a cut short and a random file (scenes, data
   assets, saves, materials, meshes, images, models, sounds, shaders and scripts), each answering
   with a message that names the file and no exception or panic crossing the bridge, as one table in
   a test.
8. **The public surface written down, and release notes from the commits**, from 3DEngine's
   `fc5aef49`: a listing of every public type and member a tool writes from the built assembly,
   checked in, with a test that fails when the two differ, so a change to what a game calls is read
   as one, and the pack workflow writing the package's release notes from the commits since
   `build/version.txt` last changed. In the same batch it is checked whether a ray here stops at a
   sensor, which there threw a car's wheel and a character's ground check.
9. **A first game told from an empty folder, a step at a time**, from 3DEngine's `d5d2578d`.
   `docs/making-a-game.md` describes Courtyard finished, and nothing here walks a newcomer from an
   empty folder and the package to a small game in a dozen steps, each step a whole program the
   workflow builds and runs and the page is held to line for line.
10. **What a script host reads at each compilation**, from 3DEngine's `c06ec659`, where it took
    the Linux test job to the runner's 16 GB. `ScriptHost.References()` reads every loaded assembly
    with `MetadataReference.CreateFromFile` at each compilation, at line 190 of `ScriptHost.cs`, and
    each reference holds its file's whole image in native memory that only its finalizer gives back,
    while the GC's heap stays small and a full collection comes late. A host that compiles on each
    save gathers them. They are read once for the process and shared, as `EditorEval` keeps its own,
    and a test compiles a hundred times and finds the process holding within a few megabytes of what
    it held after ten, read before any collection. The row on an app's whole life in SHARED.md is
    checked in the same batch. So is whether a script's generation unloads when it is compiled
    again, as 3DEngine's `ScriptGenerationTests` holds since `d7e370ed` there, where a registration
    kept by the process held every generation and ran a stale script in every later app.
    `BehaviorsPlugin` passes over a collectible assembly's behaviors here, and whether a script's
    assembly adds schemas, commands or states to the lists of the process, as the module
    initializers the generator writes do for a game's, is read with it.
11. **Every code block of `docs/` compiles against the package**, from 3DEngine's `a4f31573`: a
    script writes each C# block of the guides into a project on the packed package, a fragment after
    the lines a `<!-- compiled with: -->` comment before its fence gives, a block marked `<!-- not
    compiled: reason -->` left out, and says an error at the page and line as an annotation, run in
    the workflow beside the examples' own build on the package, with a test that feeds it a page
    holding a good block, a stale one and a skipped one. There it found three faults in 138 blocks
    on its first run, a method a guide taught that had gone internal among them.

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
   21 warnings, CS1573, CS1735, CS8604, CS8620, xUnit2029 and RS1032, which `4308619` has since
   mended, with `-warnaserror` on every managed build.

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

10. **The tests run on macOS as well.** The owner chose it on 2026-10-07, the package being
    built for osx-arm64 and osx-x64 while the suite ran on Linux and Windows alone; N 6.2 asks for
    every desktop system the package ships for, and `macos-latest` joining the matrix is item 5.

11. **Feathers' crate may be enabled, after the other gaps.** The owner allowed it on 2026-10-07
    for `feathers_counter`, `feathers_gallery` and `virtual_keyboard`, Bevy's `bevy_feathers`
    feature; the working session adds the crate on the owner's word in its own session, as AGENTS.md
    has it.

12. **The free and pan camera controllers may be enabled, after the other gaps.** The owner
    allowed it on 2026-10-07 for `free_camera_controller` and `pan_camera_controller`, Bevy's
    `free_camera` and `pan_camera` features with the `bevy_camera_controller` crate, on the same
    word in the working session.

13. **System font discovery stays off.** The owner chose it on 2026-10-07: its Linux backend
    links fontconfig at build time, and the bridge builds with nothing but a C compiler, so a font
    by family name is not offered, the four font examples stay missing with that reason, and
    `docs/ui.md` says why without naming anyone (N 4.7).

## Replies

**Now 4, a list inside a component.** The bridge answers how many items a list a component holds has
and makes it a given length, taking items off its end or adding them at their default, two new
exports at ABI 226, and it now makes a default for what Bevy registers none of where one is needed,
a struct from its fields', an enum whose every variant holds a value as its first, and a list as the
empty one. A list of values with fields of their own names its items' type in its row, the dump
describes each such type once in an `item` scope of its own rows, and the generator makes the items
records with static `ReadList` and `WriteList` methods taking the component's type path, so one
record serves every component holding such a list and each item's values are read and written at an
indexed path as a field's are. A box shadow is a list of `ShadowStyle`s, a gradient a union whose
variants hold their stops as lists of their own, and a popover a list of placements. `box_shadow`,
`standard_widgets`, `gradients`, `stacked_gradients` and `many_gradients` write through
`BoxShadowRef`, `BackgroundGradientRef`, `BorderGradientRef` and `PopoverRef`, with no JSON left.
Written every frame, `many_gradients --animate` spends 21.6 ms a frame in its system where the JSON
took 18.1 ms, its 89,319 crossings cheap ones, so the stress example keeps the wrapper. Before the
batch, `af8e870` moves the six types of `ComponentSchema.cs` into files of their names, taking it
off N 1.2's and N 1.3's lists, and `174ee19` moves the generator's records into a part of their own.
`ExampleStringPathTests` follows a list into its item type. `ReflectedWrapperTests` writes box
shadows growing and shrinking and all three gradients and reads them back. The suite passed, 1,181
with 9 skipped. `reflected.rs` is at 798 lines, so its defaults move into a module of their own
before it next grows.
