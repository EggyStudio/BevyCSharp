# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read. A stash of every changed file
takes what was written here since the last commit out of the tree until it is popped, so a stash
names its own paths.

Reviewed up to `cec88ad`. The Windows step opens the program headless and running until stopped,
since the job's bridge has no renderer (`08ac049`), Verdict 7's second mend; a game gets the
editor's console in one call, `ImGuiConsole` drawn over the window on the key under Escape with the
log, its filters, its history and completion, `ConsoleView` moved into the library and an ImGui
frame that draws nothing clearing what the last one drew (`0cef072`); and the sample is
`BevyCSharp.FeatureTest`, opened by `bcs open --feature-test` with `--sample` kept, in the solution,
the workflow, the docs and AGENTS.md's table on the owner's word (`cec88ad`), the program itself
under way. The run of `e58d4bc` on macOS built the bridge and passed the attribute test, which
settles Verdicts 5 and 6, and failed one test, `FixedUpdateTests`' overstep case, the second to
measure the machine's clock, which is Verdict 8; the run of `cec88ad` was in progress at 17:37. The
suite: 1,227 passed, 9 skipped, as last reported.

Before them, the three verdicts came to be mended: the bridge allows `linker_messages` in the
workspace's lints with the macOS linker's note as the reason (`470fec7`); the fixed-update case
steps its clock a 240th of a second a frame and asserts twenty fixed steps within one (`128cecd`);
and the Windows step keeps each `bcs` answer and hands a failing one to `build/bcs-answer.py`, which
says the command, its exit code, the code and sentence `bcs` answered and the end of the log the
answer names, on the page and the summary, with a test (`6a2c4b2`). The crash log is in as an engine
feature, the feature test's first commit: `CrashLog` writes `logs/latest.log` beside the executable
from the first app of a process, C#'s lines and Bevy's, which the bridge's log layer collects for
the console's ring, five runs kept, and a crash to `logs/crash-<time>.txt` with the system, .NET,
the ABI, the adapter, the backend, the last panic the bridge caught and the log's last 200 lines,
for an exception nothing caught, a task's exception nobody looked at and a panic on one of Bevy's
threads, which the bridge's hook hands over as it happens, the next start saying where the last
crash's file is, at ABI 228 (`e58d4bc`). Two offscreen runs of the sample in about thirty on the
working session's machine lost the device as they ended and died by a signal, untraced, which
becomes a verdict if a run shows it. The pack run of `75e8953` stays cancelled. The suite: 1,227
passed, 9 skipped.

Before them, the console tests had moved into the diagnostics folder, N 1.4's list at 81
(`3e6293d`). The runs: the first three with a macOS job, `360669ef`, `e3295b5` and `6f4d8590`,
failed there twice over, the bridge's tests not building under denied warnings for a linker message
of macOS, which is Verdict 5, and the fixed-update case of `GeneratorAttributeTests` counting 106
and 111 steps where it expects 10 to 30, which is Verdict 6; on Windows the step opening the sample
through `bcs` ended with exit code 6 and nothing else on the page, which is Verdict 7; and Linux
passed each. The run of `3e6293d` was in progress at 16:40, and the pack run of `75e8953` stays
cancelled. The suite: 1,221 passed, 9 skipped.

The norm has 44 rules, and this engine stands at 30 checked, 4 with places listed, 1 to take and 9
by review.


## Now

The owner asked that the work does not stop. A batch that ends is followed by the next item here
with no wait for a reply, and the list is long so that it does not run out. Items 11 to 16 are taken
from [SHARED.md](SHARED.md).

1. **What the next page says.** The run of `e58d4bc` passed on Linux, built the bridge on macOS
   and failed one test there, Verdict 8, and failed the Windows step with its cause on the page,
   mended at `08ac049`; the run of `cec88ad` was in progress at 17:37, and the pack run of `75e8953`
   was cancelled, so Verdicts 2 and 3 wait for the next pack run. Each push's run is read by the
   reviewing session, and a failure it names comes first here.
2. **The norm's lists are paid down.** A listed file is mended when a batch next touches it, in a
   commit of its own that moves code alone, the largest first where there is a choice, and a batch
   reads the lists for the files it will touch before it starts. The rules still to take each have
   their item: N 6.2 is the macOS job's first green run.
3. **The feature test, first the program (Decision 14).** `BevyCSharp.Sample` is renamed
   `BevyCSharp.FeatureTest`, in the solution, the README's first line, `bcs open --sample` and the
   Windows job, and AGENTS.md's table row on the owner's word, keeping the window, offscreen,
   headless and `--serve` modes so CI runs it. A hub map with signposts to the zones and a free
   camera. The admin panel in Dear ImGui through `ImGuiRuntime`, opened by F1 or a pad button, a
   list menu steered by keyboard and pad as a mod menu is, with pages for graphics (window, vsync,
   anti-aliasing, shadows, a quality tier), audio volumes, controls, debug draws (colliders, gizmos,
   wireframe), teleports to the zones, things to spawn, noclip, fly and the time scale, each setting
   kept through `Persistent` and read at start. A debug overlay toggled by F3 shows the frame time,
   the position and the facing, the entities alive and what the `memory` command answers. The
   console as a library piece a game gets by one call, `ImGuiConsole`, the `ConsoleLog` ring with
   its levels and an input with history and completion over the commands, opened by the key under
   Escape as the editor's is. And the crash log as an engine feature: `AppDomain.UnhandledException`
   and unobserved task exceptions written to `logs/crash-<time>.txt` beside the executable with the
   exception, `ConsoleLog`'s last 200 lines, the OS, .NET, the ABI, the adapter and the backend, the
   bridge's panic hook writing the same file for a panic inside Bevy, `logs/latest.log` of every run
   rotated, and the next start saying where the last crash's file is. The headless run and the
   Windows job's `bcs open` hold the program in CI.
4. **The character and the course.** The capsule on `CharacterController`: walk, run, sprint,
   crouch, jump with coyote time and jump buffering, air control, riding a moving platform, pushing
   crates, respawn at the zone's start. The zones: ramps at 15, 30, 45 and 60 degrees, stairs of
   several step heights, a narrow beam and a crouch tunnel; a moving platform, an elevator, a
   rotating disc and a conveyor; gaps of growing width, an ice patch of low friction, a bounce pad
   and a pit that respawns; pushable crates, balls, a hinge door, a slider lift, a rope of distance
   joints and a pressure plate reading `ContactImpulse`; a terrain of a heightmap mesh with a Mesh
   collider under it all. Three modes are cycled as Minecraft cycles them, F3 held with F4 stepping
   to the next with an overlay naming it: walking, the character under gravity with collisions;
   creative, flying with collisions, a double tap of jump taking off and landing; and spectator, a
   free camera through everything with no collisions; F5 cycles the view, first person, third person
   behind and third person in front; the panel's entries and a `mode` console command set the same.
   A drive script walks each zone through `bcs` and asserts it, played by the pack workflow as
   Courtyard is.
5. **The render gallery.** A PBR sphere grid by metallic and roughness with rows for clearcoat
   and anisotropy; a Cornell box lit by shadow maps and by Solari where the GPU has it, a panel
   switch; a lights gallery of directional, point, spot, rect and area lights with shadows, a
   reflection probe, an irradiance volume, light probes, decals, a fog volume, SSAO and a skybox;
   the post effects as panel switches, bloom, tonemapping, MSAA, FXAA, TAA, SMAA and what else the
   camera has; and vegetation as instanced grass and trees moved by a Slang wind shader, each drawn
   from the examples that exist and each zone captured by the drive script.
6. **Scene packs (Decision 15).** A well-known graphics scene comes as an asset pack fetched on
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
7. **Day and night.** A time of day in C# driving the sun and a moon as directional lights
   through Bevy's atmosphere and `SetSkyLighting`, a star skybox at night, the hour, the speed and
   the latitude on a panel page and in the settings file, the lights' colors and intensities on
   curves by the hour, and a console command setting the hour.
8. **bevy_weather.** The crate added to the bridge's render profile on the owner's word typed
   into the working session, `WeatherPlugin` and `WeatherCamera`, its `WeatherTime`, `Weather`,
   `ProceduralWeather` and `WeatherConfig` reached from C# through the wrappers where they reflect
   and through bridge calls where they do not, the panel's weather page (kind, cloud coverage, the
   tier, procedural on or off), item 7's time of day handing the sun to it, clouds at the lowest
   tier in CI's captures, and its cost measured on a real GPU and in the workflow's image and
   written into the comparison page's costs. The crate draws around Bevy's atmosphere, which stays.
9. **The portable build and the testers' zip.** `build/publish-feature-test.sh` publishes native
   code for `win-x64` and `linux-x64` as `build/play-native.sh` does, the native library and the
   assets beside it and a `README.txt` for testers naming the keys, the panel, the console and where
   the logs are; a workflow started by hand makes the two zips as artifacts; the pack workflow plays
   the feature test from the package as it plays Courtyard, and the soak takes it.
10. **The gaps, by how many rows each holds**, each bridged from Bevy with the examples it
    unlocks written in its batch: more of Bevy's WGSL reached as its lighting is (the deferred
    buffers, a decal's tag and a volume's voxels), the widgets' events as observers, keys observed
    as they reach a field, and what the table then names most. When the captures have settled, they
    are compared whole with checked-in references by the workflow, a small share of pixels allowed
    to differ between devices, as 3DEngine does for its scenes. Transmission's glass spheres are
    missing from about one capture in four with TAA on, before `6a84286` as after it, so the cause
    is found before that job is red for them, or the example is compared with its spheres left out
    and the reason beside it. `dragdrop_picking`'s pale preview draws over the words Bevy sorts it
    under (`b548987`'s reply), untraced, and is traced before those captures are compared. Feathers'
    three examples and the two camera controllers follow the other gaps, their crates allowed
    (Decisions 11 and 12) on the owner's word in the working session, and the four font examples
    stay missing (Decision 13).
11. **Every method native code calls catches every exception**, from 3DEngine's
    `NormTests.N_2_10` (`48fbb663`): a test finds a callback the bridge calls that lets an exception
    through, by how it is handed over, and each is mended to report it instead, so no exception
    crosses the bridge from a system, an observer or a loader's callback.
12. **A template package, so `dotnet new` starts a game**, from 3DEngine's `3DEngine.Templates`
    (`ec7e6c3c`): a template of a console game on the package, installed and used by the pack
    workflow, as the first game's first step would have a newcomer do.
13. **The entities that lost a component since a system last ran**, from 3DEngine's `Removed`
    (`ab052859`): a query's filter or a world call answering the entities a component was removed
    from since the system's last run, beside the added and changed ones a behavior reads.
14. **Every example compiles on the package alone**, from 3DEngine's
    `build/examples-on-package.sh` (`a61308b0`): 208 of 231 examples call helpers of the examples
    project, so what they share to say a thing in one word becomes the package's own calls or stays
    in the example, and the workflow builds every example on the packed package.
15. **A script that more than one system runs is read for the forms only GNU's tools or a later
    bash read**, from 3DEngine's `ScriptTests` (`fd7b17f3`): one test over the scripts the workflows
    and a developer run on Linux, macOS and Windows' Git bash, where one line was found there.
16. **Fixes for the generator's diagnostics offered in an editor**, from 3DEngine's
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
   Mended at `08ac049`, the step opening the program headless and running until stopped. Settled
   when the Windows job opens the sample through `bcs`.

8. **The macOS job of `e58d4bc` fails the overstep case of `FixedUpdateTests`, the second test to
   measure the machine's clock.** Read from the page: 894 passed, 1 failed, 405 skipped, the bridge
   built and the attribute test passed, and `TheOverstepGrowsBetweenStepsAndStaysUnderOne` found the
   overstep fall from 0.97 to 0.12 between two readings, which on a runner whose frame outlasted the
   fixed step is a fixed step run between them, where the test expects every frame shorter than a
   step. The same mend as Verdict 6's: the harness steps its clock a frame at a time, a frame
   shorter than the fixed step, so the overstep grows by the same fraction each frame whatever the
   machine took, and the suite's other fixed step cases are read for the same assumption in the same
   batch. Settled when a macOS job passes it.

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
