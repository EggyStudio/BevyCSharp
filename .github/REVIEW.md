# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read. A stash of every changed file
takes what was written here since the last commit out of the tree until it is popped, so a stash
names its own paths.

Reviewed up to `9ba00a4`. Verdict 9 is mended: the leak test reads the heap after every tenth app
and judges how far its floor rose, the least reading from the twentieth app to the fiftieth against
the least from the seventieth to the hundredth, and where `BCS_GCDUMP` names `dotnet-gcdump`, which
the macOS job installs at a pinned version, `HeapCensus` counts the heap's types after the twentieth
app and the hundredth so a failure names what grew, as 3DEngine's `596535ce` has it (`070e5e0`). The
same commit mends Verdict 7's third half: the step that opens the feature test has `timeout-minutes`
of four, prints each `bcs` answer and writes it to a file of its own rather than reading it through
`$(...)`, whose pipe the program `bcs` starts may inherit on Windows and hold open as long as it
runs, which fits the wait the run of `cec88ad` sits in and is told by the next Windows job. The
character controller flies with `Fly` set, at `Move` in every direction with gravity held off and
walls, floors and ceilings still stopping it, and `PhysicsWorld.Place` puts a dynamic body at a
point at rest and writes its transform, each with a test, in the listing, the cheat sheet and the
physics page (`aa5c9e0`). The feature test has its player, item 3's character: a capsule on
`CharacterController` with a figure and a visor, walking, running, sprinting on Shift, crouching on
C and jumping on Space with coyote time and jump buffering of a little over a tenth of a second
each, the mouse turning the view while Tab locks the cursor or the right button is held and the
right stick at any time, the modes walking, creative and spectator stepped by F3 held with F4 and
named at the top of the screen for a moment, a double tap of jump taking off and landing in creative
mode, the free camera kept for spectator mode, F5 stepping the view from the eyes, from behind and
from in front with the camera brought in where a wall stands between, a fall below the map putting
it back at the start of the zone it was last in, a teleport putting it at a zone's start facing its
middle, the panel's Player page, the `mode` and `player` commands and the guide's paragraph
(`9ba00a4`); the zones and the drive script are under way in the working tree. No suite count was
reported with the three.

Before them, the feature test's program came in: a hub of signposts to its six zones written on
their boards in gizmo text, the admin panel on F1 or a pad's start button, a list of pages steered
by the arrows or the pad's cross with each row stepped left and right, graphics, audio, controls,
debug draws, teleports, spawns and the time scale, its settings read through `Persistent` before the
window opens, the F3 overlay of the frame time, the place and facing, the entities and the `memory`
command's pairs, F3 toggling as it is let go so F3 held with another key is free for the modes, the
console, and the sample's F-key effects moved onto the panel with `feature.gi`, `feature.rtao` and
`feature.crates` as commands (`ca13f97`), which settled its item; the owner typed their word in the
working session for AGENTS.md's row and for `bevy_weather` at item 8. Verdict 8 is mended, the
fixed-step cases and the physics tests stepping their clocks a frame at a time (`a915784`), after a
move of two test files into their folders, N 1.4's list at 79 and N 3.4's at 41 (`19433d1`). The
suite: 1,230 passed, 9 skipped.

Before them, the Windows step came to open the program headless and running until stopped, since the
job's bridge has no renderer (`08ac049`), Verdict 7's second mend; a game gets the editor's console
in one call, `ImGuiConsole` drawn over the window on the key under Escape with the log, its filters,
its history and completion, `ConsoleView` moved into the library and an ImGui frame that draws
nothing clearing what the last one drew (`0cef072`); and the sample is `BevyCSharp.FeatureTest`,
opened by `bcs open --feature-test` with `--sample` kept, in the solution, the workflow, the docs
and AGENTS.md's table on the owner's word (`cec88ad`), the program itself under way. The run of
`e58d4bc` on macOS built the bridge and passed the attribute test, which settles Verdicts 5 and 6,
and failed one test, `FixedUpdateTests`' overstep case, the second to measure the machine's clock,
which is Verdict 8; the run of `cec88ad` was in progress at 17:37. The suite: 1,227 passed, 9
skipped, as last reported.

The norm has 44 rules, and this engine stands at 30 checked, 4 with places listed, 1 to take and 9
by review.


## Now

The owner asked that the work does not stop. A batch that ends is followed by the next item here
with no wait for a reply, and the list is long so that it does not run out. Items 10 to 15 are taken
from [SHARED.md](SHARED.md).

1. **What the next page says.** The run of `cec88ad` passed on Linux, failed on macOS in the
   overstep case before its mend and in the leak test's two readings, Verdict 9, mended since, and
   its Windows step opening the feature test through `bcs` was still waiting at 18:21, Verdict 7's
   third half, mended since, so the first run with `070e5e0` tells of the three; the pack run of
   `75e8953` was cancelled, so Verdicts 2 and 3 wait for the next pack run. Each push's run is read
   by the reviewing session, and a failure it names comes first here.
2. **The norm's lists are paid down.** A listed file is mended when a batch next touches it, in a
   commit of its own that moves code alone, the largest first where there is a choice, and a batch
   reads the lists for the files it will touch before it starts. The rules still to take each have
   their item: N 6.2 is the macOS job's first green run.
3. **The course, its character in.** The player is in (`9ba00a4`), a capsule on
   `CharacterController`: walk, run, sprint, crouch, jump with coyote time and jump buffering, the
   three modes stepped by F3 held with F4 with an overlay naming them, F5 stepping the view, the
   panel's page, the `mode` command and respawn at the zone's start; air control, riding a moving
   platform and pushing crates are the controller's and are shown on the zones. The zones: ramps at
   15, 30, 45 and 60 degrees, stairs of several step heights, a narrow beam and a crouch tunnel; a
   moving platform, an elevator, a rotating disc and a conveyor; gaps of growing width, an ice patch
   of low friction, a bounce pad and a pit that respawns; pushable crates, balls, a hinge door, a
   slider lift, a rope of distance joints and a pressure plate reading `ContactImpulse`; a terrain
   of a heightmap mesh with a Mesh collider under it all. A drive script walks each zone through
   `bcs` and asserts it, played by the pack workflow as Courtyard is.
4. **The render gallery.** A PBR sphere grid by metallic and roughness with rows for clearcoat
   and anisotropy; a Cornell box lit by shadow maps and by Solari where the GPU has it, a panel
   switch; a lights gallery of directional, point, spot, rect and area lights with shadows, a
   reflection probe, an irradiance volume, light probes, decals, a fog volume, SSAO and a skybox;
   the post effects as panel switches, bloom, tonemapping, MSAA, FXAA, TAA, SMAA and what else the
   camera has; and vegetation as instanced grass and trees moved by a Slang wind shader, each drawn
   from the examples that exist and each zone captured by the drive script.
5. **Scene packs (Decision 15).** A well-known graphics scene comes as an asset pack fetched on
   demand and is never checked in. `scenes/<name>.json` holds the scene's source, its license and
   attribution, the pack's URL among this repository's release assets, its size and its SHA-256;
   `build/make-scene-pack.py` makes a pack from the official download, Intel Sponza first from its
   glTF, the textures resized to 1K and written as KTX2 with BC7, BC5 and BC1 blocks, mipmapped and
   zstd-compressed, which the bridge's `ktx2` and `zstd_rust` read with no new crate, the meshes as
   they are, `AssetPack.Write` packing the folder and the attribution written into
   THIRD-PARTY-NOTICES.md; the panel's Scenes page lists the manifests, fetches a pack into the
   user's cache under `Persistent`'s data directory with a progress bar, checks the hash, shows the
   attribution and loads the scene, the pack opened while the app runs on both sides of the bridge,
   or chosen before a start where the bridge cannot swap one; `bcs scenes fetch <name>` does the
   same from a terminal; a workflow started by hand loads each pack and captures reference views
   with a frame time, the packs cached between runs, and the push workflows never fetch one. Sponza
   stands on the map at ground level beside the course with its main wooden door open, so the
   character walks in, its floors and walls under Mesh colliders. Its heaviest meshes, the
   photogrammetry pieces of hundreds of thousands of triangles, are drawn as meshlets where the
   bridge is built with `--meshlet` and the GPU has 64-bit texture atomics, through
   `Render.CreateMeshletMesh` with the cut kept in the user's cache or cut at pack time where the
   bridge can write a meshlet mesh, the plain meshes the fallback `Render.MeshletsActive` chooses,
   and a panel switch comparing the frame time of the two, the picture drawn once a pixel while
   meshlets run; the feature test's bridge and the published one are built with the meshlet and
   Solari additions. The owner publishes the pack the script makes as a release asset, and Bistro,
   the classic Sponza and San Miguel wait.
6. **Day and night.** A time of day in C# driving the sun and a moon as directional lights
   through Bevy's atmosphere and `SetSkyLighting`, a star skybox at night, the hour, the speed and
   the latitude on a panel page and in the settings file, the lights' colors and intensities on
   curves by the hour, and a console command setting the hour.
7. **bevy_weather.** The crate added to the bridge's render profile on the owner's word typed
   into the working session, `WeatherPlugin` and `WeatherCamera`, its `WeatherTime`, `Weather`,
   `ProceduralWeather` and `WeatherConfig` reached from C# through the wrappers where they reflect
   and through bridge calls where they do not, the panel's weather page (kind, cloud coverage, the
   tier, procedural on or off), item 6's time of day handing the sun to it, clouds at the lowest
   tier in CI's captures, and its cost measured on a real GPU and in the workflow's image and
   written into the comparison page's costs. The crate draws around Bevy's atmosphere, which stays.
8. **The portable build and the testers' zip.** `build/publish-feature-test.sh` publishes native
   code for `win-x64` and `linux-x64` as `build/play-native.sh` does, the native library and the
   assets beside it and a `README.txt` for testers naming the keys, the panel, the console and where
   the logs are; a workflow started by hand makes the two zips as artifacts; the pack workflow plays
   the feature test from the package as it plays Courtyard, and the soak takes it.
9. **The gaps, by how many rows each holds**, each bridged from Bevy with the examples it unlocks
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
10. **Every method native code calls catches every exception**, from 3DEngine's
    `NormTests.N_2_10` (`48fbb663`): a test finds a callback the bridge calls that lets an exception
    through, by how it is handed over, and each is mended to report it instead, so no exception
    crosses the bridge from a system, an observer or a loader's callback.
11. **A template package, so `dotnet new` starts a game**, from 3DEngine's `3DEngine.Templates`
    (`ec7e6c3c`): a template of a console game on the package, installed and used by the pack
    workflow, as the first game's first step would have a newcomer do.
12. **The entities that lost a component since a system last ran**, from 3DEngine's `Removed`
    (`ab052859`): a query's filter or a world call answering the entities a component was removed
    from since the system's last run, beside the added and changed ones a behavior reads.
13. **Every example compiles on the package alone**, from 3DEngine's
    `build/examples-on-package.sh` (`a61308b0`): 208 of 231 examples call helpers of the examples
    project, so what they share to say a thing in one word becomes the package's own calls or stays
    in the example, and the workflow builds every example on the packed package.
14. **A script that more than one system runs is read for the forms only GNU's tools or a later
    bash read**, from 3DEngine's `ScriptTests` (`fd7b17f3`): one test over the scripts the workflows
    and a developer run on Linux, macOS and Windows' Git bash, where one line was found there.
15. **Fixes for the generator's diagnostics offered in an editor**, from 3DEngine's
    `3DEngine.CodeFixes` (`c6b529d4`): a code fix beside each diagnostic the behavior and command
    generators report, so an editor offers the mend.

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

7. **The Windows jobs fail the step that opens the sample through `bcs`, and the page says only
   `exit code 6`.** Read from the pages of `360669ef`, `e3295b5` and `6f4d8590`: `Open the sample
   through bcs` ended with `Process completed with exit code 6`, nothing else, in all three. The
   step runs `bcs open --sample`, `bcs command app.status` and `bcs stop` bare, so `bcs`'s answer, a
   JSON envelope with the code and the sentence the exit code stands for, went to a log nobody
   reads, which N 6.7 does not allow, and the step is the first proof of `a753c57`'s start line on
   Windows, so whether the sample opened at all is unread. Two things: the step keeps each `bcs`
   answer and, where one fails, prints its code and sentence and the last lines of the sample's log
   at the path `bcs` names, as 3DEngine's `drive-game.sh` does since its `1c848a20`; and the cause
   is found with that on the next run. Mended at `6a2c4b2`, each answer kept and a failing one said
   by `build/bcs-answer.py` with a test. The run of `e58d4bc` said the cause: `bcs` answered
   `NOT_READY`, the sample did not start serving within 90 seconds, and its log ends with the
   sample's own refusal, that the bridge was built without Bevy's renderer so it opens no window,
   with `--headless` offered. The job's bridge is headless by design, so the step opens the sample
   headless, as the sample's own line says, and the start line on Windows is proved by that opening.
   Mended at `08ac049`, the step opening the program headless and running until stopped. With that
   mend, the run of `cec88ad` opened the feature test headless and the step was still running at
   17:56, thirteen minutes in, where the opening, the status and the stop take seconds, so one of
   the three waits, the program run with `--frames 0` or the stop; the step takes a
   `timeout-minutes` of a few minutes, so a wait fails there with its last answer and not at the
   job's sixty, and what waited is read from that. Mended at `070e5e0`, the step given
   `timeout-minutes` of four, each answer printed and written to a file of its own rather than read
   through `$(...)`, whose pipe a program `bcs` starts may inherit on Windows and hold open as long
   as it runs, which would keep the step waiting on the opening's answer until the program ended, as
   the run of `cec88ad` waits; the next Windows job tells whether that was the wait. Settled when a
   Windows job's step opens the feature test through `bcs` and ends with its three answers printed.

8. **The macOS job of `e58d4bc` fails the overstep case of `FixedUpdateTests`, the second test to
   measure the machine's clock.** Read from the page: 894 passed, 1 failed, 405 skipped, the bridge
   built and the attribute test passed, and `TheOverstepGrowsBetweenStepsAndStaysUnderOne` found the
   overstep fall from 0.97 to 0.12 between two readings, which on a runner whose frame outlasted the
   fixed step is a fixed step run between them, where the test expects every frame shorter than a
   step. The same mend as Verdict 6's: the harness steps its clock a frame at a time, a frame
   shorter than the fixed step, so the overstep grows by the same fraction each frame whatever the
   machine took, and the suite's other fixed step cases are read for the same assumption in the same
   batch. Mended at `a915784`, the fixed-step cases and the physics tests stepping their clocks a
   frame at a time. Settled when a macOS job passes it.

9. **The macOS job of `cec88ad` fails `AppLeakTests`, which judges the heap by two readings.**
   Read from the page: the heap held 75.6 MB after twenty apps and 80.8 MB after a hundred, against
   the test's 5, beside the overstep case whose mend was not in that run. The test takes the heap
   after the twentieth app and the hundredth alone, which 3DEngine's own hundred showed falls on a
   trough and a crest of a heap that rises and falls back on macOS by several megabytes every few
   dozen apps, its census finding nothing of a closed app kept (its Verdict 32). The mend 3DEngine
   took at its `596535ce`: the heap read after every tenth app, the floor judged, the least reading
   from the twentieth to the fiftieth against the least from the seventieth to the hundredth, and
   where the macOS job installs `dotnet-gcdump` a census of the heap's types after the twentieth app
   and the hundredth, a failure naming the types that grew, so a leak is told from the runtime's own
   tide. The SHARED.md row on an app's whole life carries the floor. Mended at `070e5e0`, the heap
   read after every tenth app, the floor judged, and `HeapCensus` counting the heap's types where
   `BCS_GCDUMP` names `dotnet-gcdump`, which the macOS job installs at a pinned version. Settled
   when a macOS job passes the test.

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

14. **The sample becomes a feature test.** The owner decided on 2026-10-07 that
    `BevyCSharp.Sample` is renamed `BevyCSharp.FeatureTest` and stays in the solution, puts every
    feature on one map with a capsule character on four course zones, a PBR sphere grid, a Cornell
    box, vegetation, a lights gallery and Bevy's atmosphere with a C# time of day, has an admin
    panel and a Source-style console in Dear ImGui with settings kept in a file, logs a crash to a
    file beside the executable, and is published native for Windows x64 and Linux x64 as a zip for
    testers. `bevy_weather` comes as its own batch once the map stands, for stars, the moon, clouds,
    fog, rain and snow drawn around Bevy's atmosphere, its crate added on the owner's word in the
    working session, and volumetric clouds are not written by hand.

15. **Graphics test scenes come as packs fetched on demand, never checked in.** The owner
    decided on 2026-10-07: Intel Sponza first, its textures at 1K, the pack a release asset of this
    repository, fetched by the feature test's panel and by `bcs` on demand and cached for the user,
    with Bistro, the classic Sponza and San Miguel left for later; the repository holds the
    manifests and the script that makes a pack. Sponza stands on the feature test's map at ground
    level with its main door open, for the character to walk in, and its heaviest meshes are drawn
    as meshlets, to test them, where the GPU can.

## Replies
