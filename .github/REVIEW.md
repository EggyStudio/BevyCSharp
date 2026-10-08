# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read. A stash of every changed file
takes what was written here since the last commit out of the tree until it is popped, so a stash
names its own paths.

Reviewed up to `97e523d`. Item 3 is settled: `DayNight` in the feature test runs the hour on at the
settings' `DaySpeed` hours a game minute, one by default, and places the sun for the equinox at the
settings' latitude, 45 by default, rising at six, 45 degrees up at noon and setting at eighteen; a
full moon half an hour behind the opposite of the sun is a second directional light, whose disk and
moonlit sky Bevy's atmosphere draws; the sun's and the moon's illuminance and color, the ambient
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
first in the gaps' item. With item 3 out, the list is renumbered to eleven, the SHARED.md items 6 to
11, and the coder goes on to bevy_weather, item 3.

Before them, the tonemappers' ramp references came to be settled: `TonemapRampTests` draws
SHARED.md's ramp with a Slang material of its own, `tonemap_ramp.slang`, whose formula and eight
rows are the row's to the letter, on a plane filling a 1024 by 8 view of an HDR camera with dither,
multisampling, bloom and antialiasing off and no grading, once for each of the eight tonemappers,
reads the eight-bit sRGB picture back and holds it to the PNG kept for that tonemapper within two
levels for a formula and four for a table, writes them with `BCS_WRITE_TONEMAP_REFERENCES` set,
which `build/tonemap-references.sh` sets, and a second test reads each kept picture for a ramp that
starts black and never darkens; the eight are in `BevyCSharp.Tests/references/tonemapping`, each
named as Bevy names its tonemapper, RGBA at eight bits in sRGB with the rows unfiltered, which a
read of their headers confirms, drawn on an RTX 4070 with Bevy 0.19.1 and `tonemapping_luts` on,
`None` matching the ramp through a plain sRGB encode within one level, and BUILDING.md says how they
are made and kept (`64ec311`). The 3DEngine coder was told where they are.

Before them, the scene packs came to be settled: Sponza's heaviest meshes are drawn as meshlets
where the bridge is built with them and the GPU can, `Render.CreateMeshletMesh` keeping the cut in
the user's cache and `Render.MeshletsActive` choosing the plain meshes otherwise, `scene.meshlets`
on the panel comparing the frame time of the two, the scenes workflow started by hand fetching each
pack into the actions cache and capturing the reference views with their frame times, and
`make-scene-pack.py` saying first that it needs Python 3.14 (`7c9a619`); the feature test's memory,
measured in a scope of 16 GB with the bridge's allocations and wgpu's counts read each second:
Sponza plain stands at 3.6 GB, its textures going up as the pack holds them, and as meshlets it
passed 12 GB in a quarter of a minute with every count still, the NVIDIA driver's own, which a
bisect led to the shadow views, Bevy 0.19 running every meshlet pass in every shadow view, each
holding a few hundred megabytes of the driver's memory, Bevy's own meshlet example climbing from 1.2
GB with one cascade to 7.5 GB with five lights, so where meshlets run the light hall's lamps cast no
shadows and Sponza as meshlets stands at 6.5 GB and 60 frames a second with a peak of 7, what the
driver keeps not traced further and said in `Config.MeshletClusters`; `MemoryGuard` holds every app
`bcs`, the drive script, the suite and the feature test start to 8 GB or a quarter of the machine's
memory, ending the run with exit code 86 through `_exit` and a crash file, since an ordinary exit
crashed in the driver and its core dump held the memory fifteen seconds more, the `memory` command
giving the peak and wgpu's pairs and the drive script writing the peak beside the frame times
(`b646b79`); `tonemapping_luts` is on in the render profile for the ramp references, and the scratch
is off `/tmp`. The pack's publishing stays the owner's. With that item out, the list was renumbered.
The suite: 1,257 passed, 2 skipped.

The norm has 44 rules, and this engine stands at 31 checked, 4 with places listed, none to take and
9 by review.


## Now

The owner asked that the work does not stop. A batch that ends is followed by the next item here
with no wait for a reply, and the list is long so that it does not run out. Items 6 to 11 are taken
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
3. **bevy_weather.** The crate added to the bridge's render profile on the owner's word typed
   into the working session, `WeatherPlugin` and `WeatherCamera`, its `WeatherTime`, `Weather`,
   `ProceduralWeather` and `WeatherConfig` reached from C# through the wrappers where they reflect
   and through bridge calls where they do not, the panel's weather page (kind, cloud coverage, the
   tier, procedural on or off), day and night's time of day handing the sun to it, clouds at the
   lowest tier in CI's captures, and its cost measured on a real GPU and in the workflow's image and
   written into the comparison page's costs. The crate draws around Bevy's atmosphere, which stays.
4. **The portable build and the testers' zip.** `build/publish-feature-test.sh` publishes native
   code for `win-x64` and `linux-x64` as `build/play-native.sh` does, the native library and the
   assets beside it and a `README.txt` for testers naming the keys, the panel, the console and where
   the logs are; a workflow started by hand makes the two zips as artifacts; the pack workflow plays
   the feature test from the package as it plays Courtyard, and the soak takes it.
5. **The gaps, by how many rows each holds**, first Bevy's file watcher panicking as the app
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
6. **Every method native code calls catches every exception**, from 3DEngine's `NormTests.N_2_10`
   (`48fbb663`): a test finds a callback the bridge calls that lets an exception through, by how it
   is handed over, and each is mended to report it instead, so no exception crosses the bridge from
   a system, an observer or a loader's callback.
7. **A template package, so `dotnet new` starts a game**, from 3DEngine's `3DEngine.Templates`
   (`ec7e6c3c`): a template of a console game on the package, installed and used by the pack
   workflow, as the first game's first step would have a newcomer do.
8. **The entities that lost a component since a system last ran**, from 3DEngine's `Removed`
   (`ab052859`): a query's filter or a world call answering the entities a component was removed
   from since the system's last run, beside the added and changed ones a behavior reads.
9. **Every example compiles on the package alone**, from 3DEngine's
   `build/examples-on-package.sh` (`a61308b0`): 208 of 231 examples call helpers of the examples
   project, so what they share to say a thing in one word becomes the package's own calls or stays
   in the example, and the workflow builds every example on the packed package.
10. **A script that more than one system runs is read for the forms only GNU's tools or a later
    bash read**, from 3DEngine's `ScriptTests` (`fd7b17f3`): one test over the scripts the workflows
    and a developer run on Linux, macOS and Windows' Git bash, where one line was found there.
11. **Fixes for the generator's diagnostics offered in an editor**, from 3DEngine's
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

**Item 3, bevy_weather.** The crate is in the bridge's render profile at 0.2.0, the release on Bevy
0.19.1, on the owner's word typed into the working session on 2026-10-07, with its thunder through
Bevy's audio, BUILDING.md's packages and the notices naming it. `Config.Weather` asks for
`WeatherPlugin`, and the bridge keeps it out where meshlets run, since its atmosphere would end the
app there, and leaves the camera's tonemapper and bloom to `SetPostProcessing` while keeping the
exposure its sky is calibrated for, ABI 230. Its resources and components reflect, so the schema
dump gives them wrappers, `WeatherConfigRef`, `WeatherTimeRef`, `WeatherRef`,
`ProceduralWeatherRef`, `WeatherCameraRef`, `SunLightRef`, `MoonLightRef` and the rest, and what
does not reflect is a bridge call, `Weather.SetPreset` over the crate's fifteen presets, with
`Weather.Active` saying whether it runs. Four tests: off unless asked, its clock at noon and
midnight and a preset taken at once reaching it, a preset number refused, and kept out under
meshlets. In the feature test the weather is on by default, its page setting the kind, the cloud
cover, the tier and the forecast, and the day hands it the sun and the moon, marked as its own, and
its clock, held still at this hour and latitude with the earth's tilt taken off so its sun rises at
six as the day's does. The drive script holds it partly cloudy at the lowest tier with the forecast
off, puts the settings back after, and writes the hub's frame time under it beside Sponza's, which
the scenes workflow's run on its software renderer will measure; the run here passed every zone
with a peak of 4.28 GB. Over the hub at noon at 1280 by 720, against the same view without it, it
adds under a millisecond at Potato and Low, about 2 ms at Medium, 5 to 9 ms at High and 10 to 13 ms
at Ultra on the RTX 4070, the feature test's own work holding its frame near 16 ms, which the
comparison page now says with the way to measure it, a `--timings` argument the feature test gained
for it. The guide's sky page has a section on it. A local pack of the package was needed for N 6.5,
whose newest package in build/package predated the crate, and building the guides' blocks on it
showed `build/docs-on-package.py` mapping the pattern `3DEngine` to the packed folder, a line its
port from 3DEngine kept, so `BevyCSharp` came from nuget.org or the cache, here this repository's
own 0.1.0 of August, and `Bevy.Reflected` missing from the library's usings it gives every page.
Both are mended, and the guides' 195 blocks build on the package; the package workflow's run with
`--version 0.0.0-ci` will say whether it had been building them on a published package.
