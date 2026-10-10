# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read. A stash of every changed file
takes what was written here since the last commit out of the tree until it is popped, so a stash
names its own paths.

Reviewed up to `5d955562`. One commit, item 8: every example builds on the package alone, the script
removing the catalog, the program, the examples' record and the capture's input from its copy and
building a library, so an example reaching into the runner fails there; the nine capture drivers
leave their examples for one `Drives.cs` with a sentence each of what it presses, the stress tests'
warning is written into each of the 19 as Bevy's each say theirs, the transforms' cube scene into
each of its four, and the opened size comes from `Window.Size()`, which answers offscreen, read once
a frame into a field where a method may run on a worker thread, as Bevy's system reads its window
once; `FreeCamera` moves to a file of its own and, with the radio buttons, stays a helper the script
lists with its reason, each note saying what it stands in for and that it goes when that can be
asked for, no decision or person named (N 4.7). On a fresh pack every example builds beside the two
helpers, and a driven example prints its checked-in capture exactly. Right, the runner's files taken
out of the copy so the check cannot be fooled, and the two helpers' standing said in the code's own
words. The five test classes touched pass, 61 of 61, the whole suite waiting for the comparison
commit so it does not run against the lavapipe pass for the CPU. Item 9 next, the scripts read for
GNU-only forms wherever they run.

Before it, one commit came to be read, after the home disk filled: at 21:05 it held 19 GB of 1.9 TB,
this repository's build trees holding the room, `native/target` 213 GB of five profile and feature
combinations over two Bevy versions, `build/target` 26 GB of stale and cross-compiled folders, and
the bare Bevy comparison harness's 58 GB outside the repository, and a capture of the retake was
lost to it at 20:20. Some 171 GB came back, every byte of it rebuildable, and the staged libraries,
the portable build's cache, the artifacts, the package and the examples' binaries the lavapipe pass
runs from were kept; the disk reads 190 GB free. The bounds: `build/trim-caches.sh`, run by
`build-native.sh` before every native build and by hand after a long run of `cargo check` and `cargo
test`, holds `native/target` to 80 GB and `build/target-portable` to 50, a cache past its bound
losing its incremental state first and the whole of it only when that is not enough, and
`build/target` to 2 GB with its staged libraries kept whatever the size, in `du -sk` and `find -exec
rm {} +` that GNU's and BSD's tools both read; BUILDING.md's new section carries the table. Right,
the bound held where the growth comes from and the cheapest state let go first; the lesson is a row
of SHARED.md, since it was the machine's disk and not this repository's alone. Item 8 goes on as
set: the drivers to one file, the warn and the cube scene written in, the opened size from
`Window.Size()`, which answers offscreen, read once a frame in `PreUpdate` where a method may run
off the main thread, and `FreeCamera` and `RadioButtons` on the script's list with their decisions.

Before it, two commits came to be read, item 7 at `7a2a99ac`: `EcsWorld.Removed<T>()` and
`RemovedById` list the entities that lost a component, by a removal or a despawn, since the running
system last asked, oldest first, as Bevy's `RemovedComponents<T>` gives a Rust system; each C#
system's closure holds a cursor a component, which `removals.rs` makes the running one in a
thread-local while the system runs and gives back whatever the body does, and the call reads Bevy's
removal messages from the cursor or the oldest kept up to the newest and moves the cursor on only
when the entities fitted, so a caller with too small a buffer asks again; outside a system it is
refused, Bevy keeps a removal two frames, which the remarks and the behaviors guide say, and ABI
243. Two Rust tests see a removal and a despawn once each and another system's cursor see both, a
too-small buffer moving nothing; `RemovedTests` runs two systems that each see the stripped and the
gone exactly once and throws outside a system. Right, the cursor the system's own, the retry on a
small buffer, and the two frames said. Then `c67c7981`: a second template, `bevycsharp-empty`, makes
the first game's first step with the project's name as the window's title, so the guide's step one
is the two install and new commands as 3DEngine's are, held by `FirstGameTests` against
`steps/01.cs`, both templates' placeholders and defaults checked; `pack-templates.sh` writes the
version beside each template's `template.json` and moves it over, since macOS's `sed` reads `-i`
otherwise, the thing item 9's wider test would have found. The suite before the two: 1,348 passed
and 2 skipped, the bridge's 131. Item 4's retake on the fixed clock is done, 185 pictures changed in
bytes and one text capture new, so every picture changes when it is committed, which the owner is
told; `many_cubes` lost its capture to a full disk at 20:20, said to be another session's copies,
and was retaken; seven examples flat by their design and byte-identical to their pictures fail the
blank check and go on a list with that reason; the lavapipe run began at 20:58 under its own
sessions and runs some hours, the comparison commit after it. Item 8 meanwhile, every example on the
package alone.

The norm has 44 rules, and this engine stands at 31 checked, 4 with places listed, none to take and
9 by review.


## Now

The owner asked that the work does not stop. A batch that ends is followed by the next item here
with no wait for a reply, and the list is long so that it does not run out. Items 5 to 10 are taken
from [SHARED.md](SHARED.md).

1. **What the next page says.** The run of `a54dda9e`, the first since `c7f1cbc6` and the first
   green on every system since the bump, passed 1,051 on Linux, 922 on macOS and 929 on Windows with
   425 to 435 skipped where the runner draws nothing, and its page repeats only the five lines a
   test prints and the two warnings a test asks for; Verdicts 4, 5 and 6 are settled, the runs of
   `7849ecf6`, `d6764154`, `827b382e`, `095bccd7` and `2359425d` are green on all three as well, and
   the pack job did not run, so Verdicts 2 and 3 settle on the pack run's page, which is the owner's
   to start (Decision 8). Verdict 7 is settled at `b0fa935e`, the page removed before the script
   starts, and `61f80bc6` asks whether it is there first, since the runtime's Windows delete throws
   on a missing folder, a fresh checkout's first run. `SpawnedWindowTests`' black capture is traced
   at `c46fd24e`, the window's image holding its zeros until the pass that draws it has its
   pipeline, and the test captures once `Render.PipelinesReady()` holds; `Render.Screenshot`'s
   remarks for a window and `docs/window.md` say so with step g. The page's repeated lines carried
   116 warnings of `Screen.Playing` in every run since before `c70f17b`, quieted at `37b2118f`, the
   harness adding `Screen` beside the behaviors it discovers, so the warning shows once, from the
   test that asks for it, and the third repeated line is the 67 errors `ShaderMaterialTests` asks
   for (Decision 7). Package 0.4 is ready to pack on Bevy 0.20 (Decision 8), `a54dda9e`'s run green
   and item 3's steps in at `c9c460df`, and Verdicts 2 and 3 settle on that pack run's page. The
   cheat sheet is its writer's again at `d6764154`. N 4.7's list in `NormTests` names ASKS.md, which
   is in, and the owner's `5a7f2c07` is on `build/norm/7.2.txt`, both at `5bbbe1a5`. The run of
   `60141490`, with `e9a8508e`'s mesh shaders and Verdict 9's mend, is green on all three, and each
   system skips three more than `2359425d`'s run, the three mesh shader tests, so the workflow's
   devices run no mesh shaders and those tests and `mesh_shader_intro`'s capture are the laptop's
   alone, which the captures job compares as it draws them, the lit cube alone. Verdict 9 is settled
   at `60141490`. The runs of `c39dd61d`, `e09a2ed8` and `90075005` are green on all three. Each
   push's run is read by the reviewing session, and a failure it names comes first here.

2. **The norm's lists are paid down.** A listed file is mended when a batch next touches it, in a
   commit of its own that moves code alone, the largest first where there is a choice, and a batch
   reads the lists for the files it will touch before it starts. Every rule is checked or by review
   since the run of `156d2ce` passed on macOS, N 6.2 the last taken. N 1.3's test counts the Slang
   shaders of the bridge and the examples as it counts the C# and the Rust, as 3DEngine's does since
   its `09419080`, none of them over 800 today. `60141490` takes `NativeTypes.Render.cs` off N 1.3's
   list, split into ten files named for their structs, and those ten types off N 1.2's.
   `render/assets.rs` at 783 lines, `app.rs` at 790 and `NormTests.cs` at 794 split with their next
   addition.

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
   gaps, and context_menu going with the gap that brings the list box, multiple_text_inputs in at
   `5bbbe1a5` with the field's edits and multiline_text_input at `827b382e` with its viewport and
   cursor (`2e7a1c2c`'s reply); 2d_gizmos, 3d_gizmos and wireframe the same, for the gizmo lines'
   animation offset and the wireframe's x-ray, and tab_navigation's Tab, which moves no focus in an
   offscreen run, is traced with the keys' gap (`a54dda9e`'s reply).

   **The pack run for 0.4**, green on Linux, macOS and Windows and playing Courtyard, settles the
   item (Decision 8).

4. **The gaps, by how many rows each holds**, each bridged from Bevy with the examples it unlocks
   written in its batch: a decal's tag and a volume's voxels through the WESL glue (in with
   `340639b1`), `deferred_raymarch` on the deferred buffers (`7849ecf6`), the widgets' events as
   observers with `headless_tabs` and `draggable_slider` (`01b5ac3e`), keys observed as they reach a
   field (`5bbbe1a5`), `sprite_material` and `shader_material_2d_bindless` as 2D materials
   (`467efee0` and `855c4b7e`), `inline_image` and `fixed_node` in the interface (`095bccd7`),
   `pipeline_constants` (`4e3dd60a`) and `gpu_component_array_buffer` (`2359425d`) in shaders, mesh
   shaders from Slang through SPIR-V (`e9a8508e`), and what the table then names most, in this
   order: the four rows that could be written as they were, done at `af4242ae`; the standard
   material's depth and specular maps, done at `e09a2ed8` with `pbr_specular_textures` on (Decision
   25); and Bevy's diagnostics store, done at `90075005`, `log_diagnostics` in part for the system
   information behind the `sysinfo` crate. The dev tools' three, `fps_overlay`, `infinite_grid` and
   `scene_viewer`, wait on the owner's word for their crate as Feathers' and the system information
   do (Decision 11), and the remote protocol's three stay as they are, `./bcs` being this engine's
   own. With the gaps that wait on no word done, the captures' comparison below is next, the
   transmission flake first. When the captures have settled, they are compared whole with checked-in
   references by the workflow, a small share of pixels allowed to differ between devices, as
   3DEngine does for its scenes. The four faults named before that job goes red are traced at
   `d98c299d`: transmission's missing spheres do not reproduce on 0.20; `dragdrop_picking`'s preview
   over the words is Bevy's own, the same on bare 0.20; the anisotropic spheres blown white under a
   normal prepass are Bevy's shader skipping anisotropy's setup with the tangent frame, pinned by a
   test that fails when Bevy mends it, no report filed from here as the owner has it; and the fog's
   haze under a depth prepass is gone on 0.20, pinned. The comparison job then, as proposed and
   taken: every example captured at a fixed frame time of a sixtieth, so its clock is the frame
   count on any device, lavapipe on the machine's clock having turned transmission's camera half
   round; every checked-in capture taken again on that clock, the owner told since every picture
   changes; `build/compare-captures.py` over the webp's pixels comparing each capture with its
   checked-in one as 3DEngine's reference frames are compared, a pixel differing past 24 in a
   channel and a capture failing past 2% of its pixels, a text capture compared as text, with a list
   beside it in N 4.5's form naming each example allowed more, its measured share and its reason,
   which only gets shorter, an example the runner's device draws less of (mesh shaders, ray queries,
   meshlets) listed with that reason or its capture carrying the device's refusal for the script to
   honor, and the page naming a failure's share and where it lies; the whole set run on lavapipe
   here first and each failure named before the job goes red, the long capture run told to the
   engine's session first, which measures its lamps' cost on the same GPU. The retake on the fixed
   clock is done, 185 pictures changed and seven examples flat by their design listed with that
   reason, and the lavapipe run began at 20:58; the comparison commit comes after it with
   `build/captures-differ.txt`. Feathers' three examples with `feathers_number_input` and the three
   camera controllers follow the other gaps, their crates allowed (Decisions 11 and 12) on the
   owner's word in the working session, and the four font examples stay missing (Decision 13).
   `compressed_image_saver` comes last here, for the scene packs' textures as KTX2 in BCn or ASTC
   with their mipmaps, less memory after the kill of 2026-10-08.

5. **Every method native code calls catches every exception**, from 3DEngine's `NormTests.N_2_10`
   (`48fbb663`): a test finds a callback the bridge calls that lets an exception through, by how it
   is handed over, and each is mended to report it instead, so no exception crosses the bridge from
   a system, an observer or a loader's callback. Done at `f12a5879`: the test has had the first two
   sources since `869c9fbe` and takes the third, the overrides a binding's callbacks reach, finding
   none, and every method it finds catches every exception.
6. **A template package, so `dotnet new` starts a game**, from 3DEngine's `3DEngine.Templates`
   (`ec7e6c3c`): a template of a console game on the package, installed and used by the pack
   workflow, as the first game's first step would have a newcomer do. Done at `fef6bbeb`, `dotnet
   new bevycsharp` making the README's first program, packed beside the engine and walked by the
   README job; its publishing on nuget.org beside the engine, a second package in the owner's name,
   waits on the owner's word (Decision 8), and the README's install line needs it;
   `docs/first-game.md` starts from a second template, `bevycsharp-empty`, since `c67c7981`, as
   3DEngine's does.
7. **The entities that lost a component since a system last ran**, from 3DEngine's `Removed`
   (`ab052859`): a query's filter or a world call answering the entities a component was removed
   from since the system's last run, beside the added and changed ones a behavior reads. Done at
   `7a2a99ac`, `EcsWorld.Removed<T>()` and `RemovedById` with a cursor a system, as Bevy's
   `RemovedComponents<T>`.
8. **Every example compiles on the package alone**, from 3DEngine's `build/examples-on-package.sh`
   (`a61308b0`): some 55 of 331 examples reach a helper of the examples project, so what they share
   to say a thing in one word becomes the package's own calls or stays in the example, and the
   workflow builds every example on the packed package with the shared files left out, so a reach
   into the project fails the build. The capture drivers move to one file beside the catalog,
   `StressTest.Warn` and the transforms' cube scene are written into each example as Bevy's carry
   theirs, and the opened size comes from a library call or is read as Bevy's read it; `FreeCamera`
   and `RadioButtons` stay helpers on the script's list with their reasons, the first waiting on the
   owner's word for Bevy's camera controller crate (Decision 12), whose camera then replaces it, the
   second a helper Bevy's own examples share and Feathers' radio would replace (Decision 11), the
   list only getting shorter. Done at `5d955562`, the two helpers listed.
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

24. **A game's session writes what the engine lacks in `.github/ASKS.md`, here as in 3DEngine.** The
    owner ordered it on 2026-10-10, when the session making a voxel game in `3DEngine.Game` found
    the sun's shadows costing the GPU 14.8 ms and the CPU 13 ms over some 1,800 draws, every chunk
    drawn once a cascade with no culling, and had no way to tell the engine's session: the game's
    session writes an entry there with what it measured and how, the reviewing session turns it into
    an item of the Now list by its weight and writes the item's number under the entry, and the
    engine's session reads REVIEW.md as before; no game of this engine has written one yet;
    AGENTS.md names the file beside SHARED.md and NORM.md, with the owner's word, and the engine's
    session writes nothing in it. ASKS.md and AGENTS.md's bullet on it are the reviewing session's
    and are committed with whichever batch comes next, as REVIEW.md is; a game's own files are the
    game's session's to commit.

25. **A feature of a crate already in the tree that pulls no crate in is the working session's to
    turn on; a feature that pulls a crate in waits on the owner's word as Decision 11 has it.** Read
    by review on 2026-10-10 from the rule that nothing else is added, asked over Bevy's
    `pbr_specular_textures`, which turns on code in a crate the tree builds, where Feathers' and the
    dev tools' features pull `bevy_feathers` and `bevy_dev_tools` in. The owner's word overrides it.

## Replies
