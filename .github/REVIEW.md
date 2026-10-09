# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read. A stash of every changed file
takes what was written here since the last commit out of the tree until it is popped, so a stash
names its own paths.

Reviewed up to `2ed99011`. Three commits, each its own. Verdict 4's mend (`22be0bc`):
`prefer_desktop_title_bar` moves out of app.rs into `title_bar.rs`, code alone, app.rs at 781 lines,
the three profiles checked and the norm's tests passed before the commit; right. Verdict 5's
(`4571689`): `MemoryGuard.ResidentBytes` keeps its largest reading in a compare-and-swap loop,
`PeakBytes` answers the largest of that, this reading and `PeakWorkingSet64`, the `memory` command's
`process` and `peak` pairs read through the guard, and `MemoryGuardTests` holds the peak at or above
a reading; right, with the limit said in its remarks, that without a cap the readings are the ones
taken, which the soak and the cap do not meet. Both settle on the next run's page. Item 3
(`2ed99011`), the deferred batch: a program's `Deferred` stage returns `bcs::deferred(surface,
mesh)`, Bevy's packed surface written through a fourth prelude over `pbr_deferred_functions`, the
material's method `Deferred` where the stage exists so a forward camera leaves it out as Bevy's own
deferred materials are left out, the program's prepass vertex shader given to the deferred prepass
under Bevy's label for it, `Role::Deferred` the ninth role (ABI 231), and
`PrepassVertexOutput.instance_index` without `nointerpolation`, since Slang copied the mark onto a
struct WGSL refuses it on; `DeferredMaterialTests` reads the buffer back, sees the lit color and a
forward camera drawing nothing, and refuses a deferred stage without a prepass vertex shader; the
`ssr` example is written, its water Bevy's shader in Slang through the stage, 293 written. Right,
and the prelude is the fourth to be rewritten under WESL in the spike. The suite: 1,275 passed, 2
skipped and 1 failed, `ImGuiConsoleTests.TheKeyOpensItOverTheTopRunsWhatIsTypedAndClosesIt`, which
the reply notes as a flake under load and leaves; it is not one, and Verdict 6 models it from the
ImGui pass's code. With item 3 out, the list is renumbered to ten, Bevy 0.20 is item 3, and the
coder is on its spike.

Before them, the file watcher's panic at exit came to be settled: from the frame an exit is decided
until the app is destroyed or another is made, `crash::ending` holds a flag the hook reads, and a
panic outside the guard in that window is kept in `last()` and printed by the process's hook but not
written as a crash, a native test holding such a panic to being kept and not written (`6363357`).
Two remarks. A fault of Bevy's own in that window, a panic on a render thread as the app ends, is
hidden from the crash file along with the watcher's, which is the price chosen, and it is read again
at the upgrade, where the watcher's send on a closed channel may be mended upstream and the window
could close. The reply gives no count of the suite for this commit, which each batch's reply does,
so the next reply carries it. Today the owner decided the engine moves to Bevy 0.20 (Decisions 17 to
20), item 4 after the deferred batch in flight, and the list is renumbered to eleven. The runs of
`2443936` and `6363357` were read with it and are red, Verdicts 4 and 5, which come before item 3's
commit.

Before it, the feature test's zips came to be settled, `build/publish-feature-test.sh <version>` publishes the
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

The norm has 44 rules, and this engine stands at 31 checked, 4 with places listed, none to take and
9 by review.


## Now

The owner asked that the work does not stop. A batch that ends is followed by the next item here
with no wait for a reply, and the list is long so that it does not run out. Items 5 to 10 are taken
from [SHARED.md](SHARED.md).

1. **What the next page says.** The runs of `2443936` and `6363357` are red, macOS failing
   `MemoryCommandTests` on both with the memory command's peak at 0 (Verdict 5) and all three
   systems failing `NormTests.N_1_3` at `6363357` with app.rs at 810 lines (Verdict 4); both are
   mended at `22be0bc` and `4571689` and settle on the next run's page. The page's repeated lines
   carry 116 warnings of `Screen.Playing` in every run since before `c70f17b`, StateTests' behaviors
   scoped to a state no other app adds and registered in every app by the module initializer, which
   drowns what else repeats (Decision 7); they are quieted in the batch that next touches the tests,
   the test's behaviors registered only where their state is. Package 0.4 is packed on Bevy 0.20
   (Decision 8), and Verdicts 2 and 3 settle on that pack run's page. Each push's run is read by the
   reviewing session, and a failure it names comes first here.

2. **The norm's lists are paid down.** A listed file is mended when a batch next touches it, in a
   commit of its own that moves code alone, the largest first where there is a choice, and a batch
   reads the lists for the files it will touch before it starts. Every rule is checked or by review
   since the run of `156d2ce` passed on macOS, N 6.2 the last taken. N 1.3's test counts the Slang
   shaders of the bridge and the examples as it counts the C# and the Rust, as 3DEngine's does since
   its `09419080`, none of them over 800 today, so the list stays as it is.

3. **Bevy 0.20.** The owner chose it on 2026-10-09 (Decisions 17 to 20), and the crates' word is
   typed by the owner into the working session, as AGENTS.md has it, before a manifest changes. Bevy
   0.20.0 was published on 2026-10-08 with wgpu and naga 30, winit 0.30, gilrs 0.11 and nonmax 0.5
   unchanged, a minimum Rust of 1.97.1 that this machine, the portable image and the runners meet,
   and every one of the 70 `bevy/*` features the bridge names. In this order, each step a commit of
   its own where it stands alone.

   **The spike first.** 0.20 replaces naga_oil with WESL: `#import` becomes `import ...;`, `#ifdef
   X` becomes `@if(X)`, `#define_import_path` is gone, GLSL is gone, and Bevy's modules move
   (`bevy_pbr::forward_io` to `bevy_pbr::render::forward_io`, `mesh_view_bindings` under
   `bevy_pbr::render`, `prepass_utils` to `bevy_pbr::prepass::utils`). Nothing a game wrote changes,
   since games write Slang and no shader in the repository uses those forms. The bridge's glue in
   `programs/wgsl.rs` does, ten `#import bevy_pbr::{...}` blocks and some thirty `#ifdef`s, built at
   `programs.rs:569` with `Shader::from_wgsl`, which becomes `Shader::from_wesl` for a glued unit
   while plain Slang output stays `from_wgsl`, and `programs.rs:555` names units for naga_oil's
   registry where WESL imports by path. Whether WESL's parser takes slangc's WGSL with the glue in
   front is unknown, so the first commit compiles the feature test's nine programs and the examples'
   shaders through `from_wesl` on 0.20 and reports what it refused, before anything else is touched.

   **The manifests and the mechanical moves**, with `cargo check` on headless, render and editor,
   with meshlets and Solari, and `cargo test`: observers `On<Add, A>` to `On<Add<A>>` (observe.rs,
   solari.rs, meshlets.rs); flat pointer events, `Pointer<Press>` to `PointerPress` (pointer.rs,
   input.rs, pick.rs); `ShaderBuffer` typed over `AlignedVec`, `new` taking a `Vec<T>`, `set_data`
   and `resize_in_place` gone (43 uses across pools, rays, views, compute, material, shaders,
   passes, values and instances); extraction generic over worlds, `#[extract_app(RenderApp)]`,
   `SyncComponent<RenderApp>`, `TemporaryRenderEntity::default()` and `extract()` in `bevy_extract`
   (47 uses); `ViewPrepassTextures::depth` an `Option<DepthStencilAttachment>` and
   `ViewDepthTexture` renamed `ViewDepthStencilTexture` (views.rs, passes.rs, watch.rs);
   `ExtractedWindows` a `Query<&ExtractedWindow>` (window.rs); `bevy_shape` and `bevy_curve` split
   from `bevy_math` (14 uses); `to_dynamic` returning `Result` (reflected.rs, render/scene.rs);
   `Ptr::as_ptr` returning `*const u8` (ecs.rs, reflected.rs); `Name::from(&str)` wanting `'static`
   (`clips.rs:259`); `DeferredWorld::query` deprecated; and `ui::Interaction` deprecated, an error
   under the workflow's denied warnings, so `Ui.InteractionOf` and `UiInteraction` keep their
   surface over `Hovered` and `Pressed`. In the interface, `BorderRadius` fields are `CornerRadius`,
   `EditableText` and `TextInput` are two components with `TextScroll` gone into
   `EditableText::viewport`, `FontSource`'s generic families are constructors, Escape releases text
   focus and propagates, `FocusCause::Auto` is new, `Node` requires `EmSize`, and the default font
   size is `rem(1)` where it was `px(20)`. `SolariLighting` has ReSTIR off by default and new fields
   (world cache size, light samples, temporal accumulation, bounces); `restir` is chosen by
   measurement on the comparison page and the fields reach `Config.RayTracedLighting`.

   **The embedding ported in.** `bevy_embedded_assets` 0.16 targets 0.19 alone; its build script
   (139 lines, reading `BEVY_ASSET_PATH`, which `build/build-native.sh --embed` already sets), its
   reader (382) and its plugin (179), in `.ref/bevy_embedded_assets-main`, become the bridge's own
   `build.rs` and an `embedded` module behind the `embed` feature, replacing `EmbeddedAssetPlugin`
   at `app.rs:91`, `cargo-emit` left out since a build script prints its own directives, the crate's
   MIT or Apache-2.0 notice kept for what was taken, and the dependency removed (Decision 17).

   **The weather vendored and ported.** `bevy_weather` 0.2.0 targets 0.19.1 alone and has one
   maintainer; its source in `.ref/bevy_weather-main` becomes a workspace member
   `native/bevy_weather` in upstream's shape with its LICENSE.md, the bridge depending on it by
   path, and is ported there: four shader loads whose `sky.wgsl` and `precipitation.wgsl` carry five
   preprocessor lines, converted to WESL and renamed `.wesl`; five `AsBindGroup` derives and eight
   material impls; 34 atmosphere sites against an atmosphere that is per camera in 0.20
   (`AtmosphereBuffer` a component, `init_atmosphere_buffer` gone). The Weather page and the drive
   script pass as before, and the port is offered upstream by the owner (Decision 18).

   **The schema and the files.** `bevy-components.tsv` written again from an editor build as
   BUILDING.md has it, its diff read as what Bevy changed; `ABI_VERSION` and `ExpectedAbiVersion`
   bumped together. `Tonemapping` and `DebandDither` move from `bevy_core_pipeline::tonemapping` to
   `bevy_render::view`, and `games/Courtyard/assets/levels/courtyard.scene.json` names the old
   paths, as a user's scene would; SCENES.md §7 gives Bevy's reflected components no former names,
   so both would be kept and reported in `SceneLoad.Refused` and the camera would lose them. A moved
   Bevy type gets former paths, a column or a sidecar beside the tsv that the generator reads as it
   reads `FormerNames`, so a file written on 0.19 keeps reading, Courtyard's file left as it is for
   the proof.

   **What a game's author sees, measured.** `ScreenSpaceTransmission` is opt-in on `Camera3d`, so
   the transmission example and the gallery add it and `docs/materials.md` says so; `Camera2d`
   defaults to `Tonemapping::Linear` and `Tonemapping::None` is a full passthrough, so
   `TonemapRampTests` may fail for `None.png`, which is rewritten with the reason if the change is
   Bevy's (3DEngine's copies follow, SHARED.md); sprites draw through `Mesh2d` with an `alpha_mode`
   and same-Z order may change; Bevy's own sets order weakly, so a bridge system that leaned on an
   unstated order between two of Bevy's may move. The suite on three systems, every capture compared
   and each difference named as Bevy's or a fault, the feature test driven through every zone inside
   `systemd-run --user --scope -p MemoryMax=20G -p MemorySwapMax=0`, the soak, Courtyard from the
   package. The suite is run once more with `ScheduleBuildSettings::shuffle_seed` set, which
   `bevy/debug` offers and the headless profile has, and what it finds is mended or listed. The
   crash file's logic of `6363357` is read against 0.20, where a panic in a system becomes an error
   for `FallbackErrorHandler`, which re-panics by default, so the hook fires twice; a Rust panic
   reported to the managed side as C# exceptions are is noted for item 5 and not taken here.

   **Documents and the table.** `docs/compared-with-bevy.md` and `docs/how-it-works.md` name 0.19,
   PLAY.md names the two crates' 0.19 releases at lines 132 and 180, BUILDING.md's bridge package
   table loses the embedding's row and changes the weather's, and `slang.rs:3` explains WGSL by
   naga_oil; `THIRD-PARTY-NOTICES.md` written again, the vendored weather and the embedding's source
   named; EXAMPLES.md written again from the lock, 0.20's list holding 422 examples with 23 new to
   triage in `triage.tsv` (`deferred_raymarch`, `mesh_shader_intro`, `sprite_material`,
   `shader_material_2d_bindless`, `pipeline_constants`, `gpu_component_array_buffer`,
   `compressed_image_saver`, `extra_source`, `headless_tabs`, `character_creation`,
   `feathers_number_input`, `inline_image`, `fixed_node`, `overflow_transform`, `draggable_slider`,
   `mutation_by_reflection`, `mines`, `many_meshlet_materials` and three `pan_orbit_camera_*`) and
   22 gone from it; the release notes name each change above that a game's author sees, with its
   reason and no one who decided (N 4.7).

   **The pack run for 0.4**, green on Linux, macOS and Windows and playing Courtyard, settles the
   item (Decision 8).

4. **The gaps, by how many rows each holds**, each bridged from Bevy with the examples it unlocks
   written in its batch: a decal's tag and a volume's voxels through the WESL glue,
   `deferred_raymarch` on the deferred buffers, the widgets' events as observers with
   `headless_tabs` and `draggable_slider`, keys observed as they reach a field, `sprite_material`
   and `shader_material_2d_bindless` as 2D materials, `inline_image` and `fixed_node` in the
   interface, `pipeline_constants` and `gpu_component_array_buffer` in shaders, mesh shaders from
   Slang through SPIR-V on Vulkan (`mesh_shader_intro`) as a gap of its own, and what the table then
   names most. When the captures have settled, they are compared whole with checked-in references by
   the workflow, a small share of pixels allowed to differ between devices, as 3DEngine does for its
   scenes. Transmission's glass spheres are missing from about one capture in four with TAA on,
   before `6a84286` as after it, so the cause is found before that job is red for them, or the
   example is compared with its spheres left out and the reason beside it. `dragdrop_picking`'s pale
   preview draws over the words Bevy sorts it under (`b548987`'s reply), untraced, and is traced
   before those captures are compared, as is the gallery's anisotropic spheres drawing blown white
   under SSAO with forward rendering though they have tangents and draw right under deferred, Bevy's
   prepass normal for an anisotropic material the suspect (`edd577c`'s reply), and the camera's
   volumetric fog hazing the whole picture, the sky with it, once a depth prepass is on the camera,
   which the hall works round by putting the fog on the camera only while it is inside (`6a19213`'s
   reply). Feathers' three examples with `feathers_number_input` and the three camera controllers
   follow the other gaps, their crates allowed (Decisions 11 and 12) on the owner's word in the
   working session, and the four font examples stay missing (Decision 13). `compressed_image_saver`
   comes last here, for the scene packs' textures as KTX2 in BCn or ASTC with their mipmaps, less
   memory after the kill of 2026-10-08.

5. **Every method native code calls catches every exception**, from 3DEngine's `NormTests.N_2_10`
   (`48fbb663`): a test finds a callback the bridge calls that lets an exception through, by how it
   is handed over, and each is mended to report it instead, so no exception crosses the bridge from
   a system, an observer or a loader's callback.
6. **A template package, so `dotnet new` starts a game**, from 3DEngine's `3DEngine.Templates`
   (`ec7e6c3c`): a template of a console game on the package, installed and used by the pack
   workflow, as the first game's first step would have a newcomer do.
7. **The entities that lost a component since a system last ran**, from 3DEngine's `Removed`
   (`ab052859`): a query's filter or a world call answering the entities a component was removed
   from since the system's last run, beside the added and changed ones a behavior reads.
8. **Every example compiles on the package alone**, from 3DEngine's
   `build/examples-on-package.sh` (`a61308b0`): 208 of 231 examples call helpers of the examples
   project, so what they share to say a thing in one word becomes the package's own calls or stays
   in the example, and the workflow builds every example on the packed package.
9. **A script that more than one system runs is read for the forms only GNU's tools or a later
   bash read**, from 3DEngine's `ScriptTests` (`fd7b17f3`): one test over the scripts the workflows
   and a developer run on Linux, macOS and Windows' Git bash, where one line was found there.
10. **Fixes for the generator's diagnostics offered in an editor**, from 3DEngine's
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

4. **The runs of `6363357` fail `NormTests.N_1_3` on Linux, macOS and Windows,
   `native/bevy_csharp/src/app.rs` at 810 lines.** It had 800 at `2443936`, at the rule's edge, and
   the ten lines of `crash::ending` took it over; the list `build/norm/1.3.txt` only gets shorter,
   so the file is not added to it, and app.rs is cut under 800 in a commit that moves code alone,
   what stands alone first, as item 2 has it for a listed file. The commit was made with the crash
   log's tests run and not the suite, which `NormTests` is part of and takes seconds, so each commit
   runs the norm's tests before it is made and each reply gives the suite's count. Settled when a
   run passes N 1.3.

5. **The macOS job fails `MemoryCommandTests.MemoryReadsWhatTheProgramHoldsAsNameAndNumberPairs` at
   `2443936` and `6363357`, with `peak is 0`.** `ConsoleMemoryCommands.Peak` answers
   `Process.PeakWorkingSet64`, which is 0 on macOS in the runner's .NET where it is a number on
   Linux and Windows, and the test holds every pair above 0; `c70f17b`'s macOS job passed before the
   pair came. `MemoryGuard` already reads the resident size every frame (`ResidentBytes`,
   `Environment.WorkingSet`, which macOS answers), so the peak is the largest resident size the
   guard has read since the app began, the same reading on every system and the one the soak and the
   memory cap use, with `PeakWorkingSet64` taken where it says more; the test keeps its assertion.
   Settled when a macOS run passes the test.

6. **The suite of `2ed99011` fails
   `ImGuiConsoleTests.TheKeyOpensItOverTheTopRunsWhatIsTypedAndClosesIt` once, the top of the
   picture unchanged with the console open, and the reply leaves it as a flake under load.** Modeled
   from the code, and not a flake. The ImGui pass (`imgui/render.rs`) returns before anything else
   while `frame.calls` is empty, so its pipeline is queued with `queue_render_pipeline` in the first
   frame that has something to draw, the frame the console opens, and Bevy's cache compiles it on a
   task in wall time while the pass answers `the pipeline is not ready` and draws nothing; the test
   waits three frames after `IsOpen` and captures, frames of an offscreen run that take what they
   take, so under the suite's load the compile outlasts the wait and the capture holds the frame
   before the console. Alone, the compile fits in three frames, which is why it passed three times
   after. Two things. The pass queues its pipeline the first frame it runs, before the empty check,
   since the format is the view's and not the draw list's, so the fifteen frames the test waits
   before its first picture compile it, and every pass of the bridge that queues on its first draw
   is read for the same. And a picture test that waits in frames on work done in wall time, a
   compile or an upload, waits on the thing itself where the engine can say it, as the feature test
   waits on `shader.list`; the wait in frames stays only where nothing can be asked. No retry and no
   longer wait. Settled when the pass queues at its first frame and the suite passes whole once more
   under load.

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
   packed when the owner chooses. On 2026-10-09 the owner chose that 0.4 waits for Bevy 0.20 and is
   packed on it, its release notes naming the move.

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
    word in the working session. On 2026-10-09 the owner extended it to the pan-orbit controller,
    which 0.20 upstreamed into the same crate as `PanOrbitCamera`, so the word, when typed, names
    all three.

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

17. **The embedding is the bridge's own.** The owner chose on 2026-10-09 that
    `bevy_embedded_assets`, whose 0.16 targets Bevy 0.19 alone, is ported into the bridge's source
    as a build script and an asset reader behind the `embed` feature, its MIT or Apache-2.0 notice
    kept, and the dependency removed.

18. **The weather is vendored and ported.** The owner chose on 2026-10-09 that `bevy_weather`, whose
    0.2.0 targets Bevy 0.19.1 alone and has one maintainer, is kept as a workspace member under
    `native/` in upstream's shape with its license and ported to 0.20 there, the port offered
    upstream, over waiting with the weather off and over dropping it.

19. **Bevy 0.20 comes after the deferred batch in flight and before the other gaps.** The owner
    chose it on 2026-10-09, over upgrading at once with the batch set aside and over finishing every
    gap on 0.19, since each gap bridged into Bevy's WGSL on 0.19 would be written again under WESL.

20. **The crates' word for the upgrade.** Bevy 0.20.0 with wgpu and naga 30, the embedding ported in
    and its crate removed, `bevy_weather` vendored under `native/` and ported, and nothing else
    added, typed by the owner into the working session, as AGENTS.md asks, before a manifest
    changes. DLSS waits for a tester to ask, Solari and meshlets stay additions outside the package,
    and Feathers' new widgets and theming stay under Decision 11.

## Replies

**Verdict 6, the ImGui pass.** The pass queues its pipeline the first frame it runs, before it
asks whether there is anything to draw, since the format is the view's, so the frames before the
console opens compile it. The bridge's other passes were read for the same. The pass of a program
(`passes.rs`), a dispatch (`compute.rs`) and a camera's draws and dispatches (`views.rs`) queue on
the first use of a program, keyed by its generation and the formats the use brings, so nothing
earlier knows what to queue; the rounded corners queue on the first frame a view has them, the
component coming with the request; and the watch's pipelines are queued at startup. None waits on
something besides what it is asked to draw, so the ImGui pass was the one. The console test keeps
its frame waits, since nothing can be asked about the pass's pipeline today. The norm's tests passed
before the commit, and the suite passed 1,276 and skipped 2, the console test among the passed.
