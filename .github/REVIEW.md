# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read. A stash of every changed file
takes what was written here since the last commit out of the tree until it is popped, so a stash
names its own paths.

Reviewed up to `2e7a1c2c`. Six commits, 38 more re-ports, 81 of the 95, and three of the 95 set
aside with their reason: multiline_text_input reads and moves the field's viewport against the text
layout's size for a scrollbar of its own and opens a justify menu on a popover, context_menu lists
its items in a `ListBox` Bevy does not reflect, and multiple_text_inputs sets a field's read-write
mode and observes its edits, none of which the bridge reaches, so each keeps its 0.19.1 head and
goes with the gap that brings what it reads. Right, and item 3 notes them. Of the 38: tonemapping
and headless_renderer step through `Linear` where they stepped through `None`; ssao widens and
narrows its radius on the arrows and starts from Bevy's default where the camera has none; overflow
outlines a hovered logo red alone, as 0.20's does; projection_zoom reads the wheel in lines;
transmission takes screen-space transmission off the camera and gives it back on Space with its
steps and quality kept, and anisotropy gives its camera the same so the lamp's glass shows what is
behind it, both as the guide says a camera must be given it; many_buttons' buttons are widgets
colored from `Hovered` with `--overflow-clip`; the states' menus are widget buttons; callbacks keeps
its three boxed systems and gpu_readback reads its buffer once more on Space; many_foxes and
many_morph_targets list the mesh compression the bridge does not reach, their rows in `triage.tsv`
moved from can to part, and five stress tests say they are. Pictures captured again where the
picture changed, four. Right, each change said in its commit's sentence, the triage made honest
where a port lost a switch, and the heads true. Eleven of the 95 remain, then the twenty with
Feathers.

Before it, fifteen commits came to be read, item 4's re-ports begun, 43 of the 95 that do not wait
on Feathers. The order is the reply's and right: twenty of the 115 are built on Feathers in 0.20,
its panes, number inputs and radio buttons in place of the helpers the ports draw, and item 4
bridges Feathers after the other gaps, so those twenty go with it, box_shadow among them, and the
rest go by how much Bevy changed them, directional_navigation_overrides first. Each re-port is a
commit of its own where Bevy changed what the example does, its sentence saying what: the two
directional navigation examples' buttons widgets with a tab index and the first focused by
`AutoFocus`; size_constraints' rows radio groups answered by their value change; hierarchy's six
ways of building a tree each a state that despawns it on leaving; gradients stepped by widget
buttons; game_menu's buttons widgets and its settings radio groups; computed_states' menu a button
and a checkbox; cooldown's foods buttons an observer answers; button a widget reporting its click as
an `Activate` and drawn each frame from its hover and press; borders' elliptical corners through the
reflected node; solari's R turning ReSTIR on and off; custom_post_processing and many_cubes headed
0.20 with the render world's bind groups and a mesh compression switch listed as what the bridge
does not reach. The 32 changed by Bevy only in paths, names and shaders moved to WESL go in two
batches, bloom_2d stepping through `Linear` where it stepped through `None`, many_gradients saying
it is a stress test, hello_world logging its greeting once at start, 2d_text_gizmos drawing a line
of tabs. A printing example's capture holds the lines it logged after what it printed, since Bevy's
own print with `info!`, the script stripping the log's colors and times (`3126b5e4`), and
BUILDING.md's sentence mended. Pictures are captured again where a re-port changed the picture,
eight of them. Right, each change said and the heads true, EXAMPLES.md following. Item 3's order is
amended to the reply's. The remaining 52 and then the twenty with Feathers.

Before it, one commit came to be read, and step g came to be whole. ReSTIR is on with ray-traced
lighting, chosen by measurement as item 3 asked: a Cornell box lit by a panel at 1280 by 720 on the
RTX 4070 took 0.42 ms of the GPU in Solari's passes with it off and 1.69 ms with it on, and the mean
change of a pixel between two frames of the still box was 9.1 of 255 off and 4.5 on, the grain plain
to see without it; Bevy leaves it off for a denoiser, and its denoiser is DLSS's, which the bridge
does not build, so `bcs_render_set_ray_traced_lighting` turns it on, a test holding a traced camera
to it, the remarks and `docs/ray-tracing.md` giving the numbers and an example. The reply disputes
item 3's second half, the fields reaching `Config`, and is right: the light samples, the bounces and
the world cache are Bevy's per camera, reached through the reflected `SolariLightingRef`, and a copy
in `Config` would be a second home for what the camera holds; item 3 is amended. With it, g's list
is read whole: the documents name 0.20, naga_oil is named nowhere in the bridge, BUILDING.md's
package table has the weather and not the embedding, THIRD-PARTY-NOTICES.md names both, EXAMPLES.md
is written from the lock with the 23 new examples in `triage.tsv`, and the release notes are the
commits' sentences, as BUILDING.md has them, each change a game's author sees named in the commit
that made it. Step h is the owner's: 29 commits since `c7f1cbc6` wait for a push, and the pack run
for 0.4 follows a green run with Verdict 6's fix in it (Decision 8). Item 4 next, box_shadow's
re-port first, while the push and the pack are the owner's.

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
   Linux at `e4c122e3` passed at the bump. Verdict 7 is settled at `b0fa935e`, the page removed
   before the script starts, and `61f80bc6` asks whether it is there first, since the runtime's
   Windows delete throws on a missing folder, a fresh checkout's first run. `SpawnedWindowTests`'
   black capture is traced at `c46fd24e`, the window's image holding its zeros until the pass that
   draws it has its pipeline, and the test captures once `Render.PipelinesReady()` holds;
   `Render.Screenshot`'s remarks for a window and `docs/window.md` say so with step g. The page's
   repeated lines carried 116 warnings of `Screen.Playing` in every run since before `c70f17b`,
   quieted at `37b2118f`, the harness adding `Screen` beside the behaviors it discovers, so the
   warning shows once, from the test that asks for it, and the third repeated line is the 67 errors
   `ShaderMaterialTests` asks for (Decision 7). Package 0.4 is packed on Bevy 0.20 (Decision 8) once
   a push's run is green with Verdict 6's fix in it, item 3's steps being in at `c9c460df`, and
   Verdicts 2 and 3 settle on that pack run's page. Each push's run is read by the reviewing
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
   measurement on the comparison page, and the fields stay the camera's, reached through its
   reflected `SolariLightingRef`, since a copy in `Config` would be a second home for what the
   camera holds (`c9c460df`).

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
   reason and no one who decided (N 4.7). The 115 ports whose heads say v0.20.0 and whose code
   follows 0.19.1 (`384f9150`'s reply) are headed 0.19.1 with the next commit, their pictures
   written again on 0.20 where step f named the difference as Bevy's, and each is ported to 0.20's
   code under item 4 in the order of how much Bevy changed it, its head and picture moving with it,
   the twenty built on Feathers in 0.20 (`faecca0b`'s reply) going with Feathers after the other
   gaps, and multiline_text_input, context_menu and multiple_text_inputs going with the gap that
   brings what each reads, the text field's viewport, the list box and the field's edits
   (`2e7a1c2c`'s reply).

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

**Three more of the 95 wait on what the bridge does not reach, and Tab moves no focus in an app with
no window.** 2d_gizmos and 3d_gizmos animate the dashes of their round gizmos on L through
`GizmoLineConfig::animation_offset`, which the bridge's gizmo configuration does not carry, and
wireframe turns x-ray on and off through `WireframeConfig::xray_mode` and a `WireframeXray`
component, neither of which has a row in the table, so the three keep their 0.19.1 heads for the
gaps that bring them. Porting tab_navigation, a Tab sent by `input.key` to the example drawn
offscreen moved the focus neither from a focused button nor from none, in the committed port as in
this one, where a click focuses and a click on the page clears as they should. The input focus
plugin hands keys to the focused entity or, with none, to the primary window, and an offscreen run
has no window, so either the keys never reach tab navigation there or they never come as the
messages it reads. It is untraced, and belongs to the gap of keys observed as they reach a field.
