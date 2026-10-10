# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read. A stash of every changed file
takes what was written here since the last commit out of the tree until it is popped, so a stash
names its own paths.

Reviewed up to `a5eed5d3`. Ten commits. The eight tests are traced and no row was missing
(`4acec927`): `MemoryGuard` stopped the test host at 3.76 GB against a cap of 3.75, a quarter of the
15 GB .NET reads under a 20 GB scope, and `dotnet test` printed its Passed line for what had run, so
a cut run passed for a whole one; the host grew because each app drawing offscreen left some ten
megabytes it had freed in glibc's arenas, an arena a thread and the GPU's driver starting threads
for every app, thirty apps growing it 317 MB and 1 MB with one arena, the silent plugin holding
nothing since Bevy's own plugin with no device grows the same; `suite.runsettings` starts the host
with `MALLOC_ARENA_MAX=2`, which must be in the environment before the runtime makes its arenas, and
thirty apps grow 18 MB. Right, a trace that went to the cause, and the model offered was wrong, the
loaders' rows being fixed lists. The reading of how a run is counted is kept here: a whole run is
the listing and eleven, the two arc theories listed once for seven rows and six. The rest: Verdict
6's test touches its quarter gigabyte from `NativeMemory.Alloc` and holds three quarters of the
growth (`6715339c`), settling on the next run; the vendored weather's manifest spells `missing_docs`
(`95b280fb`); the silent plugin's systems run after transforms are propagated, where Bevy plays its
own, so `audio::checked` refuses a file no decoder reads before a decoder is built from it and
panics, with a test (`c84f33ea`), a fault the plugin had and the suite found; and the six files item
2 listed are mended in six commits that move code alone, largest first, `views.rs` into four,
`post.rs` into three, `compute.rs`, `window.rs`, `slang.rs` and `ecs.rs` into one or two beside
them, each off N 1.3's list as it went, the list at five, every file under 800, the paths kept by
re-exports, the bridge's tests at 118 and 67. The suite over all ten: 1,280 passed, 2 skipped and 1
failed of 1,283, `SpawnedWindowTests` reading a spawned window's picture back black once and passing
three times alone, which the reply watches for; it is traced instead, before step f, since f
compares captures and a black one is what it would compare, the capture racing the spawned window's
first presented frame the first thing to read. A cut run passing for a whole one is Verdict 7. Steps
f, g and h remain.

Before it, Decision 23 came to be carried out, by another road than the item named: Bevy 0.20 keeps
`AudioOutput` private to its crate, so no output with no device can be handed to it, and a run with
no window adds a plugin of the bridge's own in place of Bevy's (`audio/silent.rs`), which registers
the same assets and settings, opens nothing, and gives each sound a sink carrying Bevy's
`AudioSinkPlayback` that decodes the clip with the window and the loop Bevy would give it and draws
from it on the app's real clock, so a pause, a speed, a seek, a loop's refused seek and a despawn at
the end behave as on a device and a game waiting on a sound's end works; `Config.AudioWithoutWindow`
switches back to Bevy's plugin and `Audio.IsSilent` says which a run uses; the windowed path passes
`Config.SpatialScale` to Bevy's plugin, which it never did; the audio tests that skipped without a
device run on every machine; ABI 233. Right, and the road taken is the sound one, since a sink that
only drops a sound would have broken every game that waits on one. One thing before the move-only
commits: the suite ran 1,274 tests where the bump ran 1,279 and this batch adds three, eight tests
fewer with no test's source changed since `601c6264` and the listing the same, which the reply says
and leaves untraced. The rows a theory finds at run time are the place, and the first suspect is the
audio loaders: `BadFileTests` and `FileHandleTests` give every loader its bad files and its handle,
and the suite runs headless, so if the silent plugin registers the audio asset and not its loader's
extensions, the four formats' rows are gone, which is eight for two cases or for two tests; the
results file of this run against the bump's names the eight, and the reply says which and why before
the moves. The suite: 1,272 passed and 2 skipped of 1,274. The runs of `e4c122e3`, `340639b1` and
`c7f1cbc6` were read after: the bump is green on Linux and Windows in CI, Verdicts 4 and 5 settle on
them, and one test is red on macOS and Windows, Verdict 6.

Before it, Bevy 0.20 came in, item 3's steps b to e in one commit of 393 files: the bridge, the
vendored weather and the library on 0.20.0 with wgpu and naga 30, every profile compiling with
warnings denied, the bridge's 116 tests passing with meshlets and Solari, the lock losing naga_oil
and gaining WESL, `bevy/bevy_curve` named, ABI 232 on both sides; the moves as listed, lifecycle
observers, flat pointer events, `bevy::curve` and `bevy::shape`, the typed `ShaderBuffer`,
`constants` on every stage, `#[extract_app(RenderApp)]`, the depth and stencil types, extracted
windows as render entities, the corner radius circular from one length; the glue WESL under 0.20's
module paths with the volumes' import under both defines, a glued unit from `from_wesl` under a
module path of its own and every other from `from_wgsl`, the spike's three mends in
`reflect/mend.rs` with their tests; the weather's two shaders WESL and its Rust compiled unchanged;
the four faults of 0.20's worked around where each is done, `spirv_compute.rs`, the empty curve
list, `exposure.rs` and the ray scene's new group; the schema dumped again, the generator leaving a
self-holding variant out of its union, and moved types given former paths from
`bevy-former-paths.tsv` through `FormerPathsGenerator`, so a file written on 0.19 reads its
tonemapper, Courtyard's left as it is with a test placing one by its old path; the examples' table
from 0.20's list; the notices written again; `compared-with-bevy.md` and `how-it-works.md` on 0.20.
Right throughout, and the interaction is a design of its own worth saying in the release notes: a
node carries Bevy's `Button` and `Hovered`, and `Interaction` names a small component of the
bridge's, `PointerOnNode`, kept from the two in `PreUpdate` after picking, since thirteen examples
react through a change of it and `Hovered` does not change on a press; a hover counts the nodes
inside a node, as Bevy's does, which a game that read the old answer sees. `SpriteMesh` is gone with
the two stress examples Bevy dropped. The suite: 1,276 passed, 2 skipped and 1 failed, N 6.5 against
a local package packed on 0.19, which passes against one packed from this tree and which the pack
run proves. Steps f, g's captures and release notes, and h remain: the captures compared and each
difference named, the feature test driven and soaked under the memory scope, Courtyard from the
package, the shuffle-seed run, the crash file read against 0.20's panics, the release notes naming
what a game's author sees, and the pack run for 0.4. The coder takes Decision 23 next, then the six
listed files in move-only commits, then those steps. The norm's Annex B names the followed engine as
Bevy 0.20.0 from this pass.

The norm has 44 rules, and this engine stands at 31 checked, 4 with places listed, none to take and
9 by review.


## Now

The owner asked that the work does not stop. A batch that ends is followed by the next item here
with no wait for a reply, and the list is long so that it does not run out. Items 5 to 10 are taken
from [SHARED.md](SHARED.md).

1. **What the next page says.** The bump's run, `340639b1`, is green on Linux and Windows, 1,038
   passing on Linux, and red on macOS by one test, `MemoryGuardTests.TheMemoryHeldIsReadAsItGrows`,
   which `c7f1cbc6`'s run fails on Windows as well (Verdict 6, mended in `6715339c` and settled by
   the run of the push that carries it); N 1.3 and the memory command's peak pass on every system
   since `e4c122e3`, so Verdicts 4 and 5 are settled, and the examples' table check that failed
   Linux at `e4c122e3` passed at the bump. Verdict 7 is open, a test host the guard stops passing
   for a whole run under `dotnet test` alone. Before item 3's step f, `SpawnedWindowTests` is
   traced: in the run over `a5eed5d3`'s ten commits it read a spawned window's picture back black
   once and passed three times alone, and step f compares captures, so a black one is what it would
   compare; the capture racing the spawned window's first presented frame is the first thing to
   read, and the reply gives a model and the trace that confirmed it, since a test that fails once
   in four runs is a fault with a cause and not a flake. The page's repeated lines carry 116
   warnings of `Screen.Playing` in every run since before `c70f17b`, StateTests' behaviors scoped to
   a state no other app adds and registered in every app by the module initializer, which drowns
   what else repeats (Decision 7); they are quieted in the batch that next touches the tests, the
   test's behaviors registered only where their state is. Package 0.4 is packed on Bevy 0.20
   (Decision 8) once Verdict 6's run is green, Verdict 7 is settled and item 3's remaining steps are
   in, and Verdicts 2 and 3 settle on that pack run's page. Each push's run is read by the reviewing
   session, and a failure it names comes first here.

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

6. **The macOS jobs of `e4c122e3`, `340639b1` and `c7f1cbc6` and the Windows jobs of the first and
   the last fail `MemoryGuardTests.TheMemoryHeldIsReadAsItGrows`.** Read from the pages and modeled:
   the test allocates 256 MB on the GC's heap, touches a byte a page, and holds the resident size to
   have grown by more than 200 MB; it grew by 79 and 163 MB on Windows and by 196 to 202 MB on
   macOS, and passed on every system through `6363357` and fails since `2ed99011`, the deferred
   batch and the two mends. What changed is the suite around it, not the reading, and the GC
   keeps memory it freed earlier in the process
   committed and resident, so an array it places there adds nothing to the working set when touched,
   and how much it had kept depends on what ran before, which the collection's order moved; the test
   measures the GC's retention and not the guard. Two things. The memory touched is taken outside
   the GC, `NativeMemory.Alloc` of 256 MB written a byte a page and freed after, so the pages must
   be new to the process and the growth is the allocation's less what the system trims, held at
   three quarters with the reason said, or the growth is held against what the GC had committed and
   unused before the array, read from `GC.GetGCMemoryInfo`. And no looser bound on the heap's array
   alone, which would pass by what ran before. Settled when the test passes on all three systems.

7. **A test host the memory guard stopped passed for a whole run.** Read from the reply of
   `4acec927`: the guard's stop ends the host through `Environment.Exit`, `dotnet test` printed its
   Passed line for the tests that had run, and the suite's count fell by eight with nobody told,
   which three replies carried before the trace. CI reads a run through `build/test.py`, whose page
   counts the tests without a result against the listing and says a process lost to its memory
   limit, which is why the bump's pages were whole; a run on the coder's machine through `dotnet
   test` alone is not. Two things. The guard's stop in a test host is a failure that cannot read as
   a pass: the host exits with a code vstest reports as a crashed host, the cap's line last in its
   output, and the page says it as a loss to the cap. And `./bcs test` runs the suite through
   `build/test.py`, so a local count is held to the listing as CI's is, and a reply's count is the
   page's. Settled when a suite stopped by the guard says so in both places.

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

21. **No document or comment a reader sees names the owner or a session.** The owner ordered it on
    2026-10-09 after a dependency row in 3DEngine's DESIGN.md said who admitted a crate and when; N
    4.7 reaches every Markdown file but REVIEW.md, SHARED.md, NORM.md, AGENTS.md and COMMITS.md and
    the comments of every source, script, manifest and workflow, in both repositories, and who chose
    what stays in REVIEW.md's Decisions.

22. **Content streamed on the go stays in the ledger, and the browser stays out.** The owner chose
    on 2026-10-09, after reading a browser port of a large game that downloads its world as it is
    played, that the idea is recorded in SHARED.md as the file layer that port has (packs on a
    static host, reads by byte range into a block cache, a recorded first-run set, prefetch by the
    game's own streaming), to consider until a game here ships a world too large to download first,
    over a streaming file layer in both engines and over an HTTP source alone, since nothing here
    needs it today; and that the browser is out, as BUILDING.md has it, since .NET in the browser
    calls only native code linked into its own build and Bevy's web runtime is another toolchain
    that owns the page's loop.

23. **A run that shows no window makes no sound.** The owner ordered it on 2026-10-10, since a
    hidden or offscreen run, a test or a soak, played through the machine's speakers; its audio goes
    to no device or a device of silence, sounds still run their course, and a config field turns
    real audio on for such a run.

## Replies

**Verdict 7 is in.** `./bcs test` runs the suite through `build/test.py suite`, with any `--filter`
passed on, and reads its counts from the page's `digest.json`, so a host lost before every listed
test had a result is TEST_RUN_ERROR, exit 6, whatever its tally said, and a reply's count is the
page's. `build/test.py` says a lost host whose output holds MemoryGuard's line beside vstest's abort
was stopped at its memory cap, a loss of its own on the page, runs the suite again in parts as for
any loss, and takes a `--filter` that holds the listing and every part to it. MemoryGuard says its
line again last, after the crash file, so it ends the host's output whatever Bevy's threads wrote
meanwhile. A run with a cap of 0.15 GB showed both places. `./bcs test` exited 6 naming the suite
and its part as stopped at the cap with 17 listed tests without a result, and the cap's line was the
last of each lost entry's lines on the page. Under `dotnet test` alone the host still reads as
crashed with a tally that says Passed, which vstest prints and nothing in the host can change, and
vstest's reason keeps the start of the host's error output, which Bevy's lines fill in a long run,
so the cap's line shows there only in a short one. BUILDING.md says so beside the script. The page
also counted two cases of a theory whose names are cut to the same as one, so it read 1,282 where
vstest ran 1,284, and each is counted now, with a test. The whole suite through `./bcs test` ran all
1,284, passed every one but the 2 skipped, left none without a result and held 1.3 GB at most.

Shared: `build/test.py` saying a host its memory guard stopped as stopped at its cap, its `--filter`
held over the listing and every part, and its count of theory cases cut to one name are 3DEngine's
to take, its script being the same one, where its guard and its theories do the same.

**`SpawnedWindowTests`' black capture is traced.** The window's picture is written into its image by
a pass whose pipeline Bevy compiles off the main thread, and until it has, the image holds the zeros
it was made with. The test captured at the tenth frame, which a quiet machine reaches with some
thirty pipelines still compiling, the count reaching none between the twenty-seventh frame and the
thirty-fifth. Forty runs of the test's scene on a quiet machine were never black. Forty with every
core busy were black twice, and forty more once, that run read again at the twenty-fifth frame and
drawn by then, so the picture comes late rather than not at all. The test captures from the tenth
frame once `Render.PipelinesReady()` says every pipeline asked for has compiled, and twenty runs of
the class with every core busy, forty captures, gave no black one. The other captures at the tenth
frame, in `OffscreenTests` and `RenderTargetTests`, read a picture's size alone, which the image has
from its making, and the examples' captures wait 120 frames or more, past the thirty a quiet machine
takes, so step f compares pictures that were drawn.
