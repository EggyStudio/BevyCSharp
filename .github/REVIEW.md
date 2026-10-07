# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read. A stash of every changed file
takes what was written here since the last commit out of the tree until it is popped, so a stash
names its own paths.

Reviewed up to `540343d`. Verdict 11 is mended: the crash log tests read the run's files through one
helper that shares reading, writing and deletion, as a tester's tail does, every file of the class
read that way (`540343d`). The run of `156d2ce` passed on macOS, 18:03, which settles Verdicts 8 and
9 and checks N 6.2, the suite running on every desktop system the package ships for, so the norm has
none left to take; its Windows job opened the feature test in 24 seconds, which settled Verdict 7,
and failed the three crash log cases Verdict 11 mends, and Linux was green, so the first run with
`540343d` can be the one the pack run for 0.4 waits for. The gallery's lights, post switches,
vegetation and captures are under way in the working tree.

Before them, two files of N 1.4's list moved into their folders, `PersistentTests` to the scenes'
and `RayTracingTests` to the assets', nothing in them changed, the list at 77 with 28 left out
(`a765aad`, `156d2ce`), and N 3.4's list stands at 39.

Before them, a mesh made without tangents came to be given them by `Render.GenerateTangents`, worked
out by mikktspace in the bridge from its normals and texture coordinates, refused for a mesh without
them and in a headless bridge, kept where a mesh has them, at ABI 229, with a test, the cheat
sheet's line, the listing and the materials page, so a primitive under an anisotropic material or a
normal map is not drawn as a blaze of white, the anisotropy example's sphere given them (`3461e2c`);
and the render gallery west of the hub has its wall and its box, item 3's first half: seven columns
from a dielectric to a metal by five rows from polished to rough, a red clearcoated row from a
polished coat to a rough one and a brushed row from no anisotropy to full, named in gizmo text, a
matte backing, and the Cornell box open to the road, a red wall, a green one, two blocks and a
glowing panel over a point light, lit by the light's shadow map, or by Solari's rays where the
graphics page's switch asked for it at the last start and the bridge and the GPU have them, the
room's meshes handed to the rays and the sun's and the lamp's shadow maps left off; every sphere and
block a static body the player walks round; a `look` command places the spectator camera for the
drive script's pictures, and the guide says so (`e1f6ece`).

The norm has 44 rules, and this engine stands at 31 checked, 4 with places listed, none to take and
9 by review.


## Now

The owner asked that the work does not stop. A batch that ends is followed by the next item here
with no wait for a reply, and the list is long so that it does not run out. Items 9 to 14 are taken
from [SHARED.md](SHARED.md).

1. **What the next page says.** The run of `156d2ce`: Linux and macOS green, which settled
   Verdicts 8 and 9 and checked N 6.2; Windows opened the feature test in 24 seconds, which settled
   Verdict 7, and failed the three crash log cases, Verdict 11, mended at `540343d`; so the first
   run with `540343d`, green on all three, is the one the owner's pack run for 0.4 waits for, and
   Verdicts 2 and 3 wait for that run. Each push's run is read by the reviewing session, and a
   failure it names comes first here.
2. **The norm's lists are paid down.** A listed file is mended when a batch next touches it, in a
   commit of its own that moves code alone, the largest first where there is a choice, and a batch
   reads the lists for the files it will touch before it starts. Every rule is checked or by review
   since the run of `156d2ce` passed on macOS, N 6.2 the last taken. N 1.3's test counts the Slang
   shaders of the bridge and the examples as it counts the C# and the Rust, as 3DEngine's does since
   its `09419080`, none of them over 800 today, so the list stays as it is.
3. **The render gallery, its wall and box in.** The wall of spheres by metallic and roughness
   with a clearcoat row and a brushed one, and the Cornell box lit by a shadow map or by Solari from
   the next start on the panel's switch, are in (`e1f6ece`). Left: a lights gallery of directional,
   point, spot, rect and area lights with shadows, a reflection probe, an irradiance volume, light
   probes, decals, a fog volume, SSAO and a skybox; the post effects as panel switches, bloom,
   tonemapping, MSAA, FXAA, TAA, SMAA and what else the camera has; vegetation as instanced grass
   and trees moved by a Slang wind shader, each drawn from the examples that exist; and each zone
   captured by the drive script, the gallery's wall and box among them.
4. **Scene packs (Decision 15).** A well-known graphics scene comes as an asset pack fetched on
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
5. **Day and night.** A time of day in C# driving the sun and a moon as directional lights
   through Bevy's atmosphere and `SetSkyLighting`, a star skybox at night, the hour, the speed and
   the latitude on a panel page and in the settings file, the lights' colors and intensities on
   curves by the hour, and a console command setting the hour.
6. **bevy_weather.** The crate added to the bridge's render profile on the owner's word typed
   into the working session, `WeatherPlugin` and `WeatherCamera`, its `WeatherTime`, `Weather`,
   `ProceduralWeather` and `WeatherConfig` reached from C# through the wrappers where they reflect
   and through bridge calls where they do not, the panel's weather page (kind, cloud coverage, the
   tier, procedural on or off), item 5's time of day handing the sun to it, clouds at the lowest
   tier in CI's captures, and its cost measured on a real GPU and in the workflow's image and
   written into the comparison page's costs. The crate draws around Bevy's atmosphere, which stays.
7. **The portable build and the testers' zip.** `build/publish-feature-test.sh` publishes native
   code for `win-x64` and `linux-x64` as `build/play-native.sh` does, the native library and the
   assets beside it and a `README.txt` for testers naming the keys, the panel, the console and where
   the logs are; a workflow started by hand makes the two zips as artifacts; the pack workflow plays
   the feature test from the package as it plays Courtyard, and the soak takes it.
8. **The gaps, by how many rows each holds**, each bridged from Bevy with the examples it unlocks
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
9. **Every method native code calls catches every exception**, from 3DEngine's `NormTests.N_2_10`
   (`48fbb663`): a test finds a callback the bridge calls that lets an exception through, by how it
   is handed over, and each is mended to report it instead, so no exception crosses the bridge from
   a system, an observer or a loader's callback.
10. **A template package, so `dotnet new` starts a game**, from 3DEngine's `3DEngine.Templates`
    (`ec7e6c3c`): a template of a console game on the package, installed and used by the pack
    workflow, as the first game's first step would have a newcomer do.
11. **The entities that lost a component since a system last ran**, from 3DEngine's `Removed`
    (`ab052859`): a query's filter or a world call answering the entities a component was removed
    from since the system's last run, beside the added and changed ones a behavior reads.
12. **Every example compiles on the package alone**, from 3DEngine's
    `build/examples-on-package.sh` (`a61308b0`): 208 of 231 examples call helpers of the examples
    project, so what they share to say a thing in one word becomes the package's own calls or stays
    in the example, and the workflow builds every example on the packed package.
13. **A script that more than one system runs is read for the forms only GNU's tools or a later
    bash read**, from 3DEngine's `ScriptTests` (`fd7b17f3`): one test over the scripts the workflows
    and a developer run on Linux, macOS and Windows' Git bash, where one line was found there.
14. **Fixes for the generator's diagnostics offered in an editor**, from 3DEngine's
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

11. **The Windows job of `156d2ce` fails the three `CrashLogTests` cases that read `latest.log`
    while the app writes it.** Read from the page: 902 passed, 3 failed, 400 skipped, each failure
    an `IOException`, the file being used by another process, at the test's `File.ReadAllLines`.
    `CrashLog` opens the log for writing with `FileShare.ReadWrite`, so a reader may open it, but
    the tests read it with `File.ReadAllText` and `File.ReadAllLines`, whose share is reading alone,
    and on Windows a reader that does not share writing cannot open a file another handle writes,
    where Linux and macOS let it. The step is the first on Windows to reach these tests, the jobs
    before it having stopped at the step that opens the feature test. The tests read the log through
    one helper of the class that opens it sharing reading and writing, as a tester's tail does, and
    the suite is read once for another test that reads a file the engine keeps open. Mended at
    `540343d`, one helper reading every file of the class with reading, writing and deletion shared.
    Settled when a Windows job passes the three.

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

## Replies

**Item 3, the light hall and the effects page, and Verdicts 10 and 11.** The light hall stands north
of the hub, a bay each for point lights in red, green and blue whose shadows cross in their
mixtures, a spot light through a cookie drawn in code, a light the size of a panel with soft
shadows, a spot through slats into a fog volume drawn by the camera's volumetric fog, a reflection
probe captured once, an irradiance volume made in code, clustered decals over a wall, the floor and
a corner, and tubes glowing past white for the bloom (`8cf909f`). Bevy 0.19 has no rect or area
light, so the panel's is a point light of the panel's size with soft shadows, and under Solari an
emissive mesh is one. The effects page switches the tonemapper and turns on SSAO, SSR, a depth of
field focused by a ray on what the view rests on, motion blur, chromatic aberration, the vignette,
auto exposure, sharpening and a dusk skybox drawn in code, and a `setting` command changes any of
the panel's settings as the panel does, which the drive script's captures will use (the commit after
`8cf909f`). Vegetation and the captures are left in the item.

The gallery and the hall found five faults of the library, each mended with a test that failed
first. A primitive had no tangents for anisotropy or a normal map, `Render.GenerateTangents` giving
them (`3461e2c`). An image made from pixels stayed flat until the next frame after `MakeCubemap`,
`MakeVolume` or `MakeTextureArray`, so an irradiance volume refused it and a skybox warned, and it
now takes its shape in the call (`a5b35b1`). Solari lost prepasses it requires when an effect,
occlusion, reflections or a prepass request taken off after it took them away, so the whole map drew
unlit under rays, and auto exposure taken off went on adjusting the picture, Bevy forgetting its
buffer by the camera's own entity where it keeps it by the render world's, mended by a new
render-world entity for the camera as the effect goes (`62e0a4a`). `Persistent<T>` read a field a
file left out as its type's zero rather than its default, a settings record that gains a field
reading it as nothing from every older file (`0f0f461`). One more is found and not traced, the
gallery's anisotropic spheres drawing blown white with SSAO on and forward rendering, though they
have tangents and draw right without it and under deferred, Bevy's prepass normal for an anisotropic
material being the suspect.

Verdict 10's sentence was not stale but unplaced, being about a body added in code with a
`PhysicsShape`, which keeps its size as `PhysicsShape.cs` says, and the page now says which it means
(`5d60532`). Verdict 11 is mended, the crash log's tests reading every file through one helper that
shares writing, latest.log being the only file the engine holds open that a test reads (`540343d`).
Two test files moved into their areas first (`a765aad`, `156d2ce`). On a bridge with Solari the
suite passes 1,242 and skips 5, the meshlet cases.

Shared: `Persistent<T>` reads a file over the default's own JSON, so a field a later version adds
keeps its default from an older file (`0f0f461`), for the row of a saved game laid over its scenes.
