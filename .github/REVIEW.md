# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read. A stash of every changed file
takes what was written here since the last commit out of the tree until it is popped, so a stash
names its own paths.

Reviewed up to `df97905`. Text is drawn with a line under or through it, Bevy's `Underline` and
`Strikethrough` reflecting but not as components, and a font's OpenType features and variable axes
are set as tagged values (ABI 212), so eight of Bevy's text examples are written or made whole, 263,
the three that need Bevy's system font discovery and `font_atlas_debug` left and said so
(`f0be6da`); the animation settings, state and finished message moved into files of their names,
N 1.2's list at 276 (`df97905`). The reply on animation built in code, clips from curves played from
a graph (ABI 213), is written, its commit to come. The run of `f0be6da` was cancelled by the push of
`df97905`, whose run is under way. No verdict is open.

Before them, the console server came to close the connections still open as it stops and joins their
threads, so a closed app leaves none alive, held by a test with a caller connected (`b67fb85`), and
the page's repeated lines count what is logged at warning or error or with no level, leaving the
section out when nothing repeats (`a80d289`), which settles items 14 and 15. The owner chose on
2026-10-06 that the next package is 0.4, Decision 8. A key reaching the focused entity is observed
as Bevy's `FocusedInput<KeyboardInput>`, taken up the parents as the pointer's events are (ABI 211),
an offscreen run's fields take keys from the bridge's own dispatch, the focus is given and moved
from C# through Bevy's `InputFocus`, and `multiline_text_input` and `multiple_text_inputs` are
written, 255, a field set again having been inserted over itself and drawing nothing, mended on the
way (`f46edec`); the server tests moved to `Cli` by a move alone, N 1.4's list at 90 to mend
(`15974f1`). The owner pushed, and the run of `15974f1` is under way. No verdict is open.

Before them, a game came to read a key by what it types or by its name as well as by where it is,
Bevy's `ButtonInput<Key>` through the same calls with a `LogicalKey` (ABI 210), and a pretended key
reads as a keyboard's would, named where it types nothing, so Bevy's text fields take a pretended
Backspace, Enter or arrow, and released as it was pressed, where one that typed a character stayed
down for good; `keyboard_input` is written, 253 (`b23e532`). Two commits of moves alone put the
editable text tests in `Assets` and split the bridge's `ui.rs` into its parts, N 1.3's list at 18
with 11 in the bridge and N 1.4's at 91 to mend (`915514e`, `194a3b7`).

The norm has 43 rules, and this engine stands at 26 checked, 4 with places listed, 4 to take
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

7. **The page's repeated lines are warnings and errors.** The owner chose it on 2026-10-06, after
   the page of `98f6d8e5` repeated the engine's banner, so the section counts what is logged at
   warning or error or with no level and is left out when nothing repeats.

8. **The next package is 0.4.** The owner chose it on 2026-10-06 for the ABI's moves from 199 and
   the surface added since 0.3.2, `build/version.txt` holding `0.4` so the patch counts itself,
   packed when the owner chooses.

## Replies

**Now 3, animation built in code, clips.** A clip is made in code, as Bevy's examples make one
(`clips.rs`, ABI 213). An `AnimationCurve` is values sampled at times or two values eased between
by Bevy's own easing, played back and forth where asked, for a transform's translation, rotation or
scale, an interface node's scale or rotation, or a text's color through a property the bridge
declares as Bevy's example declares its own. It is aimed at an `AnimationTarget` made from the names
on the path to an entity, the graph made from the clip is played by `Animation.PlayGraph`, and each
entity moved carries its target and player through `Animation.Animate`. `animated_transform` and
`animated_ui` are written and `eased_motion` is whole, 266, each seen to move offscreen as Bevy's
does. `AnimationClipTests` holds the curve's layout, a clip moving an entity halfway at its middle
and holding its last value after, and an eased curve going there and back. `Animation.cs` held three
other public types, which moved to files of their own first (`df97905`), N 1.2 at 276. Events on a
clip, blend graphs with masks and a skinned mesh built in code are this gap's next batches.

**Now 3, animation built in code, events.** An event a game declares, implementing
`IAnimationEvent` as a Rust type derives Bevy's `AnimationEvent`, is placed on a clip at a time with
`Animation.AddEvent`, at its player or at the entity a target names (ABI 214). The clip holds a
number through Bevy's `add_event_fn`, and reaching it queues a call into C#, which triggers the
event as it was given at that entity. The first observer of such an event installs the call. A
model's clip loads by its label with `Animation.LoadClip`, and `AddEvent` answers false until it
has arrived. `animation_events` and `animated_mesh_events` are written, 268. The first sets its
message from a clip with nothing but a length, and the fox throws up dust where each foot lands,
each seen offscreen. `AnimationClipTests` holds an event at the player and one at a target heard
once each, in their times' order. The blend graphs with their masks and a skinned mesh built joint
by joint are this gap's last.
