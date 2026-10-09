# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read. A stash of every changed file
takes what was written here since the last commit out of the tree until it is popped, so a stash
names its own paths.

Reviewed up to `2443936`. Item 3 is settled: `build/publish-feature-test.sh <version>` publishes the
feature test from a package in build/package as native code for the machine it runs on, with the
bridge, the interface's library, the assets, the scene packs' manifests and a README for testers
naming the keys, the panel, the console, the logs, the settings and the memory cap, and zips it
under build/feature-test; since a tester has no `slangc`, it runs the published program offscreen
first, makes every shader program with the new `feature.shaders`, the occlusion's two among them,
and waits until `shader.list` has each of the nine ready, or takes a cache filled elsewhere with
`--cache`, keyed by the shader's path, its defines and the bridge's modules with every checkout at
LF; the feature test builds on the package where `BevyCSharpVersion` is named, through a
`nuget.config` beside it; `feature-test.yml`, started by hand, makes the Linux zip with Lavapipe
filling the cache and the Windows zip from that cache, each an artifact, the bridge built with
meshlets and Solari, and the pack workflow drives the feature test built on its package and soaks it
beside Courtyard, the stress program and Swarm; the Linux zip was driven through every zone with a
peak of 4.14 GB, run where no `slangc` could be found with every shader read from its cache, and
soaked for 150 seconds leveling near 2.85 GB, which `soak-check.py` passes, while the Windows zip
waits on the workflow's first run (`2443936`). With it every feature-test item of Decision 14 is
done, the program, the player, the course, the gallery, the scene packs, day and night, the weather
and the zips. With item 3 out, the list is renumbered to nine, the SHARED.md items 4 to 9, and the
coder goes on to the gaps, item 3, the file watcher's panic at exit first. The suite: 1,270 passed,
2 skipped.

Before them, bevy_weather came to be settled, 0.2.0, the release on Bevy 0.19.1, is in the bridge's
render profile on the owner's word typed into the coder's session on 2026-10-07, its thunder through
Bevy's audio, BUILDING.md's packages and the notices naming it; `Config.Weather` asks for
`WeatherPlugin`, which the bridge keeps out where meshlets run, since its atmosphere would end the
app there, leaving the camera's tonemapper and bloom to `SetPostProcessing` and keeping the exposure
its sky is calibrated for, ABI 230; its resources and components reflect into wrappers from the
schema dump, the presets, which do not, go through `Weather.SetPreset` over the crate's fifteen, and
`Weather.Active` says whether it runs; four tests, off unless asked, the clock and a preset reaching
it, a preset number refused, and kept out under meshlets; the feature test has it on by default with
a Weather page, the day handing it the sun, the moon and its clock with the earth's tilt taken off
so its sun rises at six, the drive script holding it partly cloudy at the lowest tier with the
forecast off and writing the hub's frame time, every zone passing with a peak of 4.28 GB; over the
hub at noon at 1280 by 720 it adds under a millisecond at Potato and Low, about 2 ms at Medium, 5 to
9 at High and 10 to 13 at Ultra on the coder's GPU, in the comparison page with a `--timings`
argument to measure it, and the sky guide has its section (`87798b7`). Before it,
`build/docs-on-package.py` mapped the pattern `3DEngine` to the packed folder, a line kept from its
port, so `BevyCSharp` came from nuget.org or the cache, and `Bevy.Reflected` was missing from the
usings it gives every page, both mended, the guides' 195 blocks building on a fresh package
(`1155200`), which the package workflow's run proves. The reply gives no count of the suite, which
is asked for. With that item out, the list was renumbered.

Before them, day and night came to be settled, `DayNight` in the feature test runs the hour on at
the settings' `DaySpeed` hours a game minute, one by default, and places the sun for the equinox at
the settings' latitude, 45 by default, rising at six, 45 degrees up at noon and setting at eighteen;
a full moon half an hour behind the opposite of the sun is a second directional light, whose disk
and moonlit sky Bevy's atmosphere draws; the sun's and the moon's illuminance and color, the ambient
light and the stars' brightness are curves by the hour, each light brought to nothing at the
horizon, and whichever is up casts the shadows, so a night costs no more shadow views than a day,
which the meshlet bound needs; the camera under the atmosphere is lit from its sky through
`SetSkyLighting` with a star cubemap drawn in code behind it, drawn over by the atmosphere's
transmittance, the stars fading in at dusk and turning about the pole star; the panel's Time page
holds the hour, the speed and the latitude, `day.hour` sets or says the hour, and the drive script
holds the day at eight with the speed at nothing, captures the hub at 23:00 as `20-night` and puts
both back, its run passing every zone and Sponza with a peak of 3.67 GB (`97e523d`). Found there and
left: Bevy's file watcher panics as the app ends, an event sent on a channel the asset server
closed, which the crash hook writes as a crash, so the next run says the last one crashed; it goes
first in the gaps' item. With that item out, the list was renumbered.

The norm has 44 rules, and this engine stands at 31 checked, 4 with places listed, none to take and
9 by review.


## Now

The owner asked that the work does not stop. A batch that ends is followed by the next item here
with no wait for a reply, and the list is long so that it does not run out. Items 4 to 9 are taken
from [SHARED.md](SHARED.md).

1. **What the next page says.** The runs of `540343d`, `edd577c` and `c70f17b` passed on Linux,
   macOS and Windows, the last at 06:28 on 2026-10-08, so main is green for the owner's pack run for
   0.4, and Verdicts 2 and 3 settle on that run's page. Each push's run is read by the reviewing
   session, and a failure it names comes first here.
2. **The norm's lists are paid down.** A listed file is mended when a batch next touches it, in a
   commit of its own that moves code alone, the largest first where there is a choice, and a batch
   reads the lists for the files it will touch before it starts. Every rule is checked or by review
   since the run of `156d2ce` passed on macOS, N 6.2 the last taken. N 1.3's test counts the Slang
   shaders of the bridge and the examples as it counts the C# and the Rust, as 3DEngine's does since
   its `09419080`, none of them over 800 today, so the list stays as it is.
3. **The gaps, by how many rows each holds**, first Bevy's file watcher panicking as the app
   ends, an event sent on a channel the asset server closed (`file_watcher.rs:269`), which the crash
   hook writes as a crash so the next run says the last one crashed, found at `97e523d` and left,
   the watcher stopped before the asset server goes or a panic after the app began ending kept out
   of the crash file; then each gap bridged from Bevy with the examples it unlocks written in its
   batch: more of Bevy's WGSL reached as its lighting is (the deferred buffers, a decal's tag and a
   volume's voxels), the widgets' events as observers, keys observed as they reach a field, and what
   the table then names most. When the captures have settled, they are compared whole with
   checked-in references by the workflow, a small share of pixels allowed to differ between devices,
   as 3DEngine does for its scenes. Transmission's glass spheres are missing from about one capture
   in four with TAA on, before `6a84286` as after it, so the cause is found before that job is red
   for them, or the example is compared with its spheres left out and the reason beside it.
   `dragdrop_picking`'s pale preview draws over the words Bevy sorts it under (`b548987`'s reply),
   untraced, and is traced before those captures are compared, as is the gallery's anisotropic
   spheres drawing blown white under SSAO with forward rendering though they have tangents and draw
   right under deferred, Bevy's prepass normal for an anisotropic material the suspect (`edd577c`'s
   reply), and the camera's volumetric fog hazing the whole picture, the sky with it, once a depth
   prepass is on the camera, which the hall works round by putting the fog on the camera only while
   it is inside (`6a19213`'s reply). Feathers' three examples and the two camera controllers follow
   the other gaps, their crates allowed (Decisions 11 and 12) on the owner's word in the working
   session, and the four font examples stay missing (Decision 13).
4. **Every method native code calls catches every exception**, from 3DEngine's `NormTests.N_2_10`
   (`48fbb663`): a test finds a callback the bridge calls that lets an exception through, by how it
   is handed over, and each is mended to report it instead, so no exception crosses the bridge from
   a system, an observer or a loader's callback.
5. **A template package, so `dotnet new` starts a game**, from 3DEngine's `3DEngine.Templates`
   (`ec7e6c3c`): a template of a console game on the package, installed and used by the pack
   workflow, as the first game's first step would have a newcomer do.
6. **The entities that lost a component since a system last ran**, from 3DEngine's `Removed`
   (`ab052859`): a query's filter or a world call answering the entities a component was removed
   from since the system's last run, beside the added and changed ones a behavior reads.
7. **Every example compiles on the package alone**, from 3DEngine's
   `build/examples-on-package.sh` (`a61308b0`): 208 of 231 examples call helpers of the examples
   project, so what they share to say a thing in one word becomes the package's own calls or stays
   in the example, and the workflow builds every example on the packed package.
8. **A script that more than one system runs is read for the forms only GNU's tools or a later
   bash read**, from 3DEngine's `ScriptTests` (`fd7b17f3`): one test over the scripts the workflows
   and a developer run on Linux, macOS and Windows' Git bash, where one line was found there.
9. **Fixes for the generator's diagnostics offered in an editor**, from 3DEngine's
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
    every desktop system the package ships for, and `macos-latest` joined the matrix at `6528ea2`.

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

16. **Every tonemapper here comes to 3DEngine.** The owner decided on 2026-10-08 that 3DEngine
    takes Bevy's eight tonemappers, the three drawn through lookup tables from Bevy's own data and
    the others ported from Bevy's shader, so the two engines draw one picture from one value; this
    side's part is the proof, a ramp drawn through each tonemapper and kept as references that
    3DEngine's test compares its own picture with, the ramp defined in SHARED.md.

## Replies

**Item 3, the file watcher's panic as the app ends.** From the frame an exit is decided, where the
`Cleanup` callbacks run, until the app is destroyed, a panic outside the guard is kept and printed
by the process's hook and not written to the crash file, so the watcher's send on the asset server's
closed channel no longer makes the next run say the last one crashed. Destroying the app, or making
another, ends that, since its threads are gone with it and a later panic is a crash again, which
`CrashLogTests`' panic on a thread of Bevy's, made after other tests' apps have ended, needed. A
native test holds a panic during the ending to being kept and not written. The suite's tests of the
crash log and of an app's end pass; the whole suite was last run at `2443936`, 1,270 passed and 2
skipped, and is run again with the next batch.
