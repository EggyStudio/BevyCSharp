# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read. A stash of every changed file
takes what was written here since the last commit out of the tree until it is popped, so a stash
names its own paths.

Reviewed up to `c70f17b`. `MeshletTests` moved into the assets' folder, nothing in it changed,
N 1.4's list at 76 with 28 left out (`c70f17b`), pushed at 06:15 on 2026-10-08 and green on the
three systems. Sponza's meshlets, item 3, are under way in the bridge's `meshlets.rs` and
`assets.rs`. At 07:10 the owner decided that every tonemapper here comes to 3DEngine (Decision 16),
this side's part the ramp references, item 14, after item 3.

Before them, scene packs came most of the way in, item 3's meshlets and workflow left: a pack mounts
under a folder of the asset root while an app runs and both sides read it, the bridge's default
source reading the list of carried files at each read; `ScenePacks` reads the manifests in
`scenes/`, fetches a pack over HTTPS or from a file into a folder every game on the machine shares,
`BCS_SCENE_PACKS` naming another, telling its progress and keeping it only where its SHA-256 is the
manifest's, and mounts it under `packs/` and its name, with `bcs scenes`, `bcs scenes fetch` and
`bcs scenes pack`, four tests, the listing, the cheat sheet and the assets page (`c27fad0`);
`build/make-scene-pack.py` makes a pack from a glTF scene, every texture resized to a power of two,
mipmapped and written as KTX2 with BC7 for colors, BC5 for normal maps and BC1 for the rest, each
level supercompressed with zstd from Python 3.14's own library, by encoders written in the script,
BC7 in its mode 6 refined once by least squares, the glTF pointed at them with its cameras left out,
the same pack byte for byte from the same input, and Intel Sponza's manifest names the pack it made,
208,923,293 bytes with the scene's lights left out, credited in the notices by
`build/third-party-notices.py` and the `scenes` folder in AGENTS.md's table as N 1.5 asks
(`9d1154a`); the feature test's Scenes page fetches a pack with a bar and loads it into the scenes
zone, Sponza with its main door swung open and its floors, walls, columns and arches as mesh
colliders, its attribution over the doorway, `scene.load` and `scene.unload` as commands, and the
drive script capturing Sponza where its pack is fetched, which the push workflows never do
(`02dab48`). Walking in found a static body on an entity under others standing at its own transform
taken as the world's, so a spawned scene's walls stopped nothing; it is put where its parents put
it, with a test (`28a54cd`). The lists were missed once, `Persistent.cs` changed at `0f0f461` while
on N 1.2's list and moved two batches later, and are read for each batch's files since. The pack is
made and not published; the owner publishes it as the release `scene-packs` with the asset
`intel-sponza.pack`, after weighing the line Sponza's own license file sets beside its CC BY 4.0
text, which limits commercial use. The suite: 1,248 passed, 5 skipped.

Before them, two types of N 1.2's list moved into files of their names, `IPersistentValue` and
`UserData` out of the persistent value's file, nothing in them changed, the list at 186 (`2b5c353`).

The norm has 44 rules, and this engine stands at 31 checked, 4 with places listed, none to take and
9 by review.


## Now

The owner asked that the work does not stop. A batch that ends is followed by the next item here
with no wait for a reply, and the list is long so that it does not run out. Items 8 to 13 are taken
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
3. **Scene packs (Decision 15), Sponza in, the meshlets and the workflow left.** The manifests,
   the fetch with its hash into the shared folder, the mount while the app runs, `bcs scenes`, the
   pack maker with its own BC7, BC5 and BC1 encoders, Intel Sponza's manifest, and the Scenes page
   loading Sponza into its zone with the door open and its walls as mesh colliders are in
   (`c27fad0`, `9d1154a`, `02dab48`). Left: Sponza's heaviest meshes, the photogrammetry pieces of
   hundreds of thousands of triangles, drawn as meshlets where the bridge is built with `--meshlet`
   and the GPU has 64-bit texture atomics, through `Render.CreateMeshletMesh` with the cut kept in
   the user's cache or cut at pack time where the bridge can write a meshlet mesh, the plain meshes
   the fallback `Render.MeshletsActive` chooses, and a panel switch comparing the frame time of the
   two, the picture drawn once a pixel while meshlets run, the feature test's bridge and the
   published one built with the meshlet and Solari additions; the workflow started by hand that
   fetches each pack, caches it between runs and captures reference views with a frame time, the
   push workflows never fetching one; and `make-scene-pack.py` saying at its start that it needs
   Python 3.14 for the standard library's zstd, where an older Python fails at the import. The
   feature test's memory with Sponza is measured and bounded first, since on 2026-10-08 the kernel
   killed it at 21 GB anonymous and 14 GB shared, where the workflow's runner has 16 GB: the pack's
   textures uploaded as the pack holds them, the meshes let go once uploaded, the meshlets' scratch
   freed, and the drive script reading the process's peak. The owner publishes the pack the script
   made as the release `scene-packs` with the asset `intel-sponza.pack`, and Bistro, the classic
   Sponza and San Miguel wait.
4. **Day and night.** A time of day in C# driving the sun and a moon as directional lights
   through Bevy's atmosphere and `SetSkyLighting`, a star skybox at night, the hour, the speed and
   the latitude on a panel page and in the settings file, the lights' colors and intensities on
   curves by the hour, and a console command setting the hour.
5. **bevy_weather.** The crate added to the bridge's render profile on the owner's word typed
   into the working session, `WeatherPlugin` and `WeatherCamera`, its `WeatherTime`, `Weather`,
   `ProceduralWeather` and `WeatherConfig` reached from C# through the wrappers where they reflect
   and through bridge calls where they do not, the panel's weather page (kind, cloud coverage, the
   tier, procedural on or off), item 4's time of day handing the sun to it, clouds at the lowest
   tier in CI's captures, and its cost measured on a real GPU and in the workflow's image and
   written into the comparison page's costs. The crate draws around Bevy's atmosphere, which stays.
6. **The portable build and the testers' zip.** `build/publish-feature-test.sh` publishes native
   code for `win-x64` and `linux-x64` as `build/play-native.sh` does, the native library and the
   assets beside it and a `README.txt` for testers naming the keys, the panel, the console and where
   the logs are; a workflow started by hand makes the two zips as artifacts; the pack workflow plays
   the feature test from the package as it plays Courtyard, and the soak takes it.
7. **The gaps, by how many rows each holds**, each bridged from Bevy with the examples it unlocks
   written in its batch: more of Bevy's WGSL reached as its lighting is (the deferred buffers, a
   decal's tag and a volume's voxels), the widgets' events as observers, keys observed as they reach
   a field, and what the table then names most. When the captures have settled, they are compared
   whole with checked-in references by the workflow, a small share of pixels allowed to differ
   between devices, as 3DEngine does for its scenes. Transmission's glass spheres are missing from
   about one capture in four with TAA on, before `6a84286` as after it, so the cause is found before
   that job is red for them, or the example is compared with its spheres left out and the reason
   beside it. `dragdrop_picking`'s pale preview draws over the words Bevy sorts it under
   (`b548987`'s reply), untraced, and is traced before those captures are compared, as is the
   gallery's anisotropic spheres drawing blown white under SSAO with forward rendering though they
   have tangents and draw right under deferred, Bevy's prepass normal for an anisotropic material
   the suspect (`edd577c`'s reply), and the camera's volumetric fog hazing the whole picture, the
   sky with it, once a depth prepass is on the camera, which the hall works round by putting the fog
   on the camera only while it is inside (`6a19213`'s reply). Feathers' three examples and the two
   camera controllers follow the other gaps, their crates allowed (Decisions 11 and 12) on the
   owner's word in the working session, and the four font examples stay missing (Decision 13).
8. **Every method native code calls catches every exception**, from 3DEngine's `NormTests.N_2_10`
   (`48fbb663`): a test finds a callback the bridge calls that lets an exception through, by how it
   is handed over, and each is mended to report it instead, so no exception crosses the bridge from
   a system, an observer or a loader's callback.
9. **A template package, so `dotnet new` starts a game**, from 3DEngine's `3DEngine.Templates`
   (`ec7e6c3c`): a template of a console game on the package, installed and used by the pack
   workflow, as the first game's first step would have a newcomer do.
10. **The entities that lost a component since a system last ran**, from 3DEngine's `Removed`
    (`ab052859`): a query's filter or a world call answering the entities a component was removed
    from since the system's last run, beside the added and changed ones a behavior reads.
11. **Every example compiles on the package alone**, from 3DEngine's
    `build/examples-on-package.sh` (`a61308b0`): 208 of 231 examples call helpers of the examples
    project, so what they share to say a thing in one word becomes the package's own calls or stays
    in the example, and the workflow builds every example on the packed package.
12. **A script that more than one system runs is read for the forms only GNU's tools or a later
    bash read**, from 3DEngine's `ScriptTests` (`fd7b17f3`): one test over the scripts the workflows
    and a developer run on Linux, macOS and Windows' Git bash, where one line was found there.
13. **Fixes for the generator's diagnostics offered in an editor**, from 3DEngine's
    `3DEngine.CodeFixes` (`c6b529d4`): a code fix beside each diagnostic the behavior and command
    generators report, so an editor offers the mend.
14. **The tonemappers' ramp references for 3DEngine (Decision 16).** SHARED.md's ramp drawn by a
    Slang material of its own through each of the eight tonemappers, with dither and color grading
    off and nothing between the material's value and the tonemapper, captured at 8 bits in sRGB, one
    PNG a tonemapper named as `Tonemapper` names it, from the feature test's panel or a test, kept
    where 3DEngine's coder copies them from, with `tonemapping_luts` confirmed on in the bridge,
    since without it Bevy draws AgX, Tony McMapface and Blender's filmic from placeholders. After
    item 3's memory bound and meshlets, before item 4.

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

**Item 3, the feature test's memory, measured and bounded.** Each run was measured in a scope of
16 GB with a cap of its own, the resident size, the bridge's allocations and wgpu's counts read each
second. Sponza drawn plain stands at 3.6 GB, 1.6 GB of it anonymous, of which the bridge's
allocator holds 260 MB and its textures go up as BC7 and BC5 as the pack holds them, so letting the
meshes and textures go from the main world would save little and is left. Drawn as meshlets it
passed 12 GB in a quarter of a minute while the bridge's allocations, the managed heap and every
count wgpu keeps, its allocator's bytes, buffers, pipelines and command encoders, stood still, so
the memory was the NVIDIA driver's own. Turning things off one at a time led to the shadow views.
With no shadows the meshlet meshes stood at 1.7 GB, with the sun's four cascades alone Sponza stood
at 6.4 GB, and Bevy's own meshlet example, flat at 1.2 GB with its one cascade, climbed to 2.8 GB
with one shadowed point light and to 7.5 GB with five and four cascades, the bunny alone. Bevy 0.19
runs every meshlet pass in every shadow view whether or not a meshlet mesh is in it, and each view
came to hold a few hundred megabytes of the driver's memory over its first fifteen seconds, then
leveled. Where meshlets run, the light hall's lamps now cast no shadows and the sun alone does, and
Sponza as meshlets stands at 6.5 GB and 60 frames a second, its peak 7 GB. What the driver keeps
was not traced further, and `Config.MeshletClusters` says what was found. The `memory` command
gains the peak and the `gpu.` pairs, read from wgpu with its counters on, and the drive script
writes the peak beside the frame times. `MemoryGuard`, the safeguard asked for after the kill,
holds every app `bcs`, the drive script, the suite and the feature test start to 8 GB or a quarter
of the machine's memory where that is less, and past it the run says why on the console and in a
crash file and ends with code 86 through `_exit`, since an ordinary exit with Bevy's threads
drawing crashed in the driver and its core dump held the memory fifteen seconds more. The scratch
is off `/tmp`, the Sponza download kept in `~/.cache/bevycsharp`, and `tonemapping_luts` is on in
the render profile, for item 14.
