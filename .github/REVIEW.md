# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read. A stash of every changed file
takes what was written here since the last commit out of the tree until it is popped, so a stash
names its own paths.

Reviewed up to `d620a10`. The last of the 3D Rendering examples keep on their entities what Bevy's
keep on theirs, 117 of the 119 whose Bevy example keeps state on an entity, the two left being what
the render world alone copies and draws from (`4ccef7e` to `6a84286`), which settles item 3.
Transmission's glass spheres are missing from about one capture in four with TAA on, before this
change as after it, which item 4 holds until the cause is found. Four commits of moves alone take
sixteen places off the lists of N 1.2, N 1.3 and N 1.4 (`5dc052e` to `d620a10`). No verdict is open.

Before them, a windowless run chose its interface's camera after pointing the cameras at its image,
so the interface has a camera from its first frame and `text_debug` runs (`a8d06df`, with a test),
and three more groups of examples keep on their entities what Bevy's keep on theirs (`492e119`,
`f8a3b55`, `a646086`). No verdict is open.

Before them, Verdict 1 was settled (`5264257`): every text file checks out with LF
ends, the norm's tests read files through one helper that ends lines with `\n`, the page's test
reads the script in UTF-8 on either ending and the script writes `\n` alone, and a picture asked
for with no interface running loads nothing on any bridge, the expected error never having been
possible on the headless bridge where the suite runs. Three more groups of examples keep on
their entities what Bevy's keep on theirs (`8637e7c`, `2724064`, `4e15bea`). No verdict is open.

The norm has 43 rules, and this engine stands at 26 checked, 4 with places listed, 4 to take
and 9 by review.


## Now

The owner asked that the work does not stop. A batch that ends is followed by the next item
here with no wait for a reply, and the list is long so that it does not run out. Items 3, 6 to 8
and 10 to 14 are taken from [SHARED.md](SHARED.md).

1. **What the next page says.** The run after `5264257` is pushed shows whether the three failures
   are gone, which the reviewing session reads and says here. The list goes on meanwhile.
2. **The norm's lists are paid down.** A listed file is mended when a batch next touches it, in a
   commit of its own that moves code alone, the largest first where there is a choice, and a
   batch reads the lists for the files it will touch before it starts. The rules still to take
   each have their item: N 2.2 and N 6.1 are the settings of item 5, N 2.1 the listing of item
   12 and N 2.6 the table of bad files in item 11.
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
   scenes. Transmission's glass spheres are missing from about one capture in four with TAA
   on, before `6a84286` as after it, so the cause is found before that job is red for them, or
   the example is compared with its spheres left out and the reason beside it.
5. **A build with no warnings, and a warning failing the workflow.** The managed build passes
   `-warnaserror` in the workflow once it is clean, with a warning that is right to keep turned
   off where it arises and its reason beside it, and `cargo` builds deny warnings the same way.
   The build before a commit runs with `--no-incremental` when it checks for warnings, since
   3DEngine's incremental build passed over a test project an earlier build without the flag
   had left up to date, and a warning reached `main` (its `2b39ddd2`).
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

**Now 3, the clock.** `Config.FrameSeconds` sets Bevy's `TimeUpdateStrategy::ManualDuration` as
the app is built, `Time.FrameSeconds` reads it and changes it while the app runs, `BCS_FRAME_TIME`
sets it where the config left it at zero, `bcs open --frame-time S` puts that variable in the
environment of the app it starts, and `app.frametime [seconds|off]` is the command, all over
`bcs_time_set_frame_seconds` and `bcs_time_frame_seconds` (ABI 204). Letting the set clock go
begins Bevy's real clock again from now, keeping the time it had counted. Bevy reads the
machine's clock as the time since the last frame it counted, so after a run of short frames had
put that ahead of the machine the game's clock would stand still until the machine caught up.
`ClockTests` hold sixty frames of a sixtieth to a second and 64 steps, a frame made slow on
purpose, the release after two seconds of unpaced frames and the command, and
`EnvironmentOptionTests` the variable. `.claude/skills/bcs-cli/SKILL.md` is an instruction file
for agents and is left to the owner to add `--frame-time` and `app.frametime` to, as 3DEngine
left its own. Four commits that move code alone came first: the physics types out of
`PhysicsWorld.cs` (`5dc052e`), the input commands out of `ConsoleWorldCommands.cs` (`7f5ccee`),
`GraphicsBackend` (`404fc32`), and `NativeConfig` with the environment options' tests
(`d620a10`).

Shared: the clock a frame at a time is taken here, as `Config.FrameSeconds` and `Time.FrameSeconds`
like 3DEngine's, with an environment variable and an option of `bcs open` in place of a flag of
the app's own, since each app here reads its own arguments.
