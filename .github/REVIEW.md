# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read. A stash of every changed file
takes what was written here since the last commit out of the tree until it is popped, so a stash
names its own paths.

Reviewed up to `08d03b4d`, item 38's first half, the edit mesh with its seven primitives, its
selection and what moves it, and nothing else, carrying the review's files: `EditMesh` under
`BevyCSharp/Modeling`, the primitives placed by their bounds' lowest corner or middle and selected
after, the selection by all, none, invert, index, axis direction, box and slot in the three modes,
move, rotate, scale, grid, snap and material, the counts, the volume, the open edges, closed and
bounds; 22 tests of exact counts, closedness and volume, the box 8, 12 and 6 with 24, the stairs of
four 20, 30 and 12; the guide page between drawing and materials, the cheat sheet's group, the
AGENTS.md row N 1.5 asks for; the suite 1,554 passed and 3 skipped; `SelectionMode` named so since
the sockets' `SelectMode` is in the CLI's server. The Reply fixes the recipe's words for items 38 to
45 and 3DEngine's 59 to 66, the line forms, the command form `model.<verb> <name> <words>`, the
commands outside the recipe, each primitive's outline and numbering so a face's index is the same in
both engines, the selection's rules and the facing tie order, y then x; the one choice differing
from the item stands, a typed measure taken as given and `snap` rounding the selection to the grid,
where a point picked or dragged lands, since a move of a fifth of a meter on a meter's grid would
otherwise move nothing. Read at 00:32 on 2026-10-21 and pushed at 00:32, no build run being in
flight, the pack run of `cf70c650` going on beside it. Next item 46's two commits, the animated
capture, then item 38's second half, the topology operations, the recipe, the build and the
collider, whose bridge change asks Bevy's tangents by a flag bit, the ABI at 299; then items 39 to
45 in order.

Before it, `cf70c650` came, item 37, the feature test moved to `games/FeatureTest`, and nothing
else, carrying the review's file and the ledger's, the tree's reference kept through explicit SDK
imports. Pushed at 00:01, the day's pack run with publish on started then, run 39505038553.

Before it, `2949041f` came, Verdict 40's mend and nothing else, carrying the review's file and the
ledger's, the four games naming `0.5.174` as their default.

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
   at `60141490`. The runs of `c39dd61d`, `e09a2ed8`, `90075005`, `d328fdc3` and `28e2d97b` are
   green on all three, `5d955562`'s cancelled by the push after it, and `b3ac2a0e`'s green on Linux
   in 4 m 40 s with 1,138 passed and 487 skipped and cancelled on Windows and macOS by the push of
   `c11c63d4` at 10:02, whose run is red on all three systems, the bridge's tests not building
   (Verdict 12), the managed tests green, 1,059, 916 and 909. The run of `68aa5dd5`, pushed at 10:47
   before the mend, is red on all three by the same step, and the run of `7ffd96c6`, pushed at
   11:06, is green on all three with the bridge's tests built, so Verdict 12 is settled. The runs of
   `9d3e2e5f` and `ff467a5f`, pushed at 11:33 and 12:07, are green on all three. The owner pushed
   the eleven through `b06ff120` at 15:46 and `21d08b44` at 15:55, whose run cancelled the first's;
   `21d08b44`'s run reads red on `NormTests.N_4_5`, its padded capture sizes not on the rule's list,
   mended at `04cefc8e` before the page named it. The run of `6641dbcc`, pushed at 18:21, is green
   on all three with N 4.5 cleared, 1,165 passed and 497 skipped on Linux in 4 m 12 s; the runs
   between were cancelled by the pushes after them, and `b134dd4d`'s runs at 19:30. The captures'
   comparison, the README's walk and Courtyard run on the pack run alone (`inputs.pack`), so the
   comparison since `d17f0d97` is read on the owner's pack run's page (Decision 8). Each push's run
   is read by the reviewing session, and a failure it names comes first here.

2. **The norm's lists are paid down.** A listed file is mended when a batch next touches it, in a
    commit of its own that moves code alone, the largest first where there is a choice, and a batch
    reads the lists for the files it will touch before it starts. Every rule is checked or by review
    since the run of `156d2ce` passed on macOS, N 6.2 the last taken. N 1.3's test is to count the
    Slang shaders of the bridge and the examples as it counts the C# and the Rust, as 3DEngine's
    does since its `09419080`, none of them over 800 today, which it does not yet (SHARED.md's row,
    taken next). `60141490` takes `NativeTypes.Render.cs` off N 1.3's list, split into ten files
    named for their structs, and those ten types off N 1.2's. `render/assets.rs`, `NormTests.cs` and
    `UiTests.cs`, which stood at 799, 794 and 794, are split at `88a75e54`, `f68bd192` and
    `09e3b339` to 557, 588 and 535, each a commit that moves code alone; `app.rs` is at 709 since
    `c5b6d1d3`. N 7.2's list takes `ff467a5f`, the subject's marks run together and pushed before
    the suite read them, beside the owner's `5a7f2c07`, and the list only gets shorter from there;
    `a0c8b1b0`, the owner's own one-line commit of the showcase's caption, pushed, joins it as the
    owner's with custom_schedule's commit, three left out. N 1.3's count of the Slang shaders done
    at `5e9235ce` on 2026-10-20. The lists read whole against their rules by 04:29 the same day:
    1.4's 30 entries are tests of what is no area of the library, the build, the documents, the
    editor, the examples, the generator, the scripting and the norm's own, listed by nature as the
    list's head says; 4.5's 20 are examples drawn at the window Bevy's own asks for, 18 stress tests
    and the headless renderer at 1920 by 1080 and the scale factor override at 512 by 288, which the
    rule leaves out; B 3's 15 entry points answer a constant or a flag and cannot panic, which the
    rule leaves out; 3.3's six are clocks or waits on things outside the frame, the asset server's
    threads, the file watcher, the socket server and the test folder's retried delete; 4.1's four
    are documents quoting another's words or naming the dash itself; 1.2, 1.3 and 3.4 hold headers
    alone; 7.2 stays. So the lists hold no debt, and nothing moves. Meanwhile, with no item open,
    the coder reads SHARED.md's row on a mesh replaced in place keeping its indirect light,
    3DEngine's `03e0bd36`, under Solari and under Bevy's light probes, whether a frame draws without
    indirect light when a mesh is replaced, with a capture of the frame after the swap against the
    one before, and reports the reading under Replies for the row; a fault found is an item. The
    replaced-mesh row read at `6cca47f7` on 2026-10-20, no fault found, the figures in the head and
    the ledger.

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
   (`AtmosphereBuffer` a component, `init_atmosphere_buffer` gone). The Weather page and the course
   script script pass as before, and the port is offered upstream by the owner (Decision 18).

   **The schema and the files.** `bevy-components.tsv` written again from an editor build as
   BUILDING.md has it, its diff read as what Bevy changed; `ABI_VERSION` and `ExpectedAbiVersion`
   bumped together. `Tonemapping` and `DebandDither` move from `bevy_core_pipeline::tonemapping` to
   `bevy_render::view`, and `games/Courtyard/assets/levels/courtyard.scene.json` names the old
   paths, as a user's scene would; SCENES.md §7 gives Bevy's reflected components no former names,
   so both would be kept and reported in `SceneLoad.Refused` and the camera would lose them. A moved
   Bevy type gets former paths, a column or a sidecar beside the tsv that the generator reads as it
   reads `FormerNames`, so a file written on 0.19 keeps reading, Courtyard's file left as it is for
   the proof.

**What a game's author sees, measured.** `ScreenSpaceTransmission` is opt-in on `Camera3d`, so the
   transmission example and the gallery add it and `docs/materials.md` says so; `Camera2d` defaults
   to `Tonemapping::Linear` and `Tonemapping::None` is a full passthrough, so `TonemapRampTests` may
   fail for `None.png`, which is rewritten with the reason if the change is Bevy's (3DEngine's
   copies follow, SHARED.md); sprites draw through `Mesh2d` with an `alpha_mode` and same-Z order
   may change; Bevy's own sets order weakly, so a bridge system that leaned on an unstated order
   between two of Bevy's may move. The suite on three systems, every capture compared and each
   difference named as Bevy's or a fault, the feature test run through every zone inside
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
   haze under a depth prepass is gone on 0.20, pinned. The captures are compared whole by the
   workflow since `d17f0d97`: each taken in a set frame on a clock of a sixtieth, every pipeline
   compiled in the frame that asks for it, all 329 matching between the laptop's GPU and lavapipe,
   seven within allowances listed with their causes in `build/captures-differ.txt`, which only gets
   shorter, and `build/compare-captures.py` failing a capture past 24 in a channel over 2% of its
   pixels and naming the share and the box. Feathers' three examples with `feathers_number_input`
   and the three camera controllers follow the other gaps, their crates allowed (Decisions 11 and
   12) on the owner's word in the working session, and the four font examples stay missing (Decision
   13). monitor_info opens a fullscreen window on every monitor of the machine, so its bridge part,
   no first window, a run that does not end with its last window, monitor entities, a window
   fullscreen on a monitor named by its entity and a camera aimed at that window, is in at
   `ff467a5f` with headless tests, and the example is written and listed in part until the owner
   runs it on their own screens and gives its capture, which waits on the owner's word. The
   example's walk of every entity for a monitor showed the bridge listed no entities by a reflected
   component, which `All<T>()` does since `28ba2698`. `compressed_image_saver` was to come last
   here, for the scene packs' textures as KTX2 in BCn or ASTC with their mipmaps, less memory after
   the kill of 2026-10-08, and waits with the others: Bevy's feature pulls the `ctt` crate, which
   the lock does not hold, so it takes the owner's word as Feathers' crates do (Decisions 11 and
   25), and its universal twin is basis-universal, already on that list. With every free gap done,
   item 11 and the three move-only splits are in at `c3aaacac` to `09e3b339`, and the working
   session follows TODO.md's order: the option fields without a kind are typed at `ac2817aa` but for
   the tuples, the instants and a node index, said by kind, and custom_cursor_image is in at
   `69fca4bb` with a bridge call for the cursor's atlas, frame and hotspot, and low_power at
   `ea5e1532`, custom_schedule at `bb4cb09f` and asset_settings at `4ea63186`, which needed no
   bridge work, and ghost_nodes at `d91665d9` on a feature pulling no crate (Decision 25), and the
   forward decal at `c16f67e5`, and the glTF extras at `7a6cbf16`, and error_handling at `6641dbcc`,
   the custom projection at `6e28c3cc` and the depth to a texture at `b134dd4d`, the table at 346
   written and 34 missing; the rows that need no crate and no word follow in TODO.md's order, the
   interface material next, the last four of the list read against the lock first. Open again in
   full on 2026-10-18 at 22:25 (Decision 38). Every gap closes, in the table's order by how many
   rows each holds, with the examples each unlocks written in its batch and the comparison kept
   green; the gaps that need a crate, Feathers' widgets and theming, the three camera controllers,
   the dev tools' three and the system information, wait each on the owner's word typed in the
   working session (Decisions 11, 12 and 20, AGENTS.md), which the working session asks for by name
   as it reaches them, taking the gaps that need no word first; the remote protocol's three stay as
   they are. One batch a commit, as before, and the bridge is complete when the table names no gap
   but those the owner declines. Read against `triage.tsv` on 2026-10-18 at 22:31, no gap is left
   that needs no word. The twenty rows not written whole are the camera controllers, five (free, pan
   and the three pan-orbit); Feathers, four (counter, gallery, virtual keyboard, number input); the
   dev tools, four (the FPS overlay, the infinite grid, the scene viewer, debug picking); the system
   information, one (`log_diagnostics` in part); `web_asset`, one; `compressed_image_saver`, one;
   the system fonts, three, which Decision 13 declined; and `monitor_info` in part, which waits on
   the owner's screens; the remote protocol's rows stay as they are. None of the crates needed is in
   `native/Cargo.lock`, and each was measured in a scratch copy of `native/` by adding the Bevy
   feature to the editor profile and comparing the lock's package names: the camera controllers add
   one package, `bevy_camera_controller`; Feathers one; the dev tools one; the system information
   five, `sysinfo` with `ntapi`, `winapi` and its two shims; `web_asset` over https twenty-one,
   `ureq` with `rustls`, `ring`, `webpki` and the platform verifiers, where plain http adds five and
   the example loads an https address; and `compressed_image_saver` seventeen, `ctt` with its five
   compressors, some prebuilt, `ispc-build-utils`, `zstd` and `fast_image_resize`. The working
   session asks the owner for each word by name in the table's order, the camera controllers first,
   then Feathers and the dev tools, then the system information, `web_asset` and the image saver,
   and bridges each as its word is typed; item 29's list, which is reading and no mend, may be
   written meanwhile. On 2026-10-19 the owner typed the words (Decision 39): the camera controllers'
   crate, Feathers', the dev tools' and the system information's come in, the https asset loading
   and the image saver are declined and their rows stay missing as declined, and `monitor_info` is
   captured on virtual monitors in a headless display and never on the owner's screens. The batches
   go in that order, one a commit. The camera controllers came at `670ecf2c` on 2026-10-19, five
   rows written, the table at 363 written and 13 missing, the lock gaining the one crate, the ABI
   286, the five examples captured and the examples' own free camera gone for Bevy's. Feathers'
   first step came at `edee9686` the same day, the core plugin, the dark theme, ten controls and the
   theme tokens, `feathers_counter` written, the table at 364 written and 12 missing, the lock
   gaining `bevy_feathers`, the ABI 287; its steps to come were the number input and the virtual
   keyboard, the gallery's controls, and the fifteen examples moved to Feathers' radio, each a
   commit with its examples. The second step came at `5452a595`, the number input and the virtual
   keyboard with their two examples, the table at 366 written and 10 missing, the ABI 289, the lock
   unchanged. The third came at `1e8c4750`, the gallery's thirty-four controls with the gallery
   written, the color conversions across Bevy's spaces, the table at 367 written and 9 missing,
   Feathers' four rows all in, the ABI 291, the lock unchanged. The last came at `319624bf`, the
   fourteen examples with option rows at Bevy's v0.20.0 on Feathers' radio, a theme call in the
   package, the port's own helper gone for one file mirroring Bevy's three shared helper files, the
   ABI 292, the lock unchanged, Feathers whole in four steps. The dev tools came at `64c1b25d` the
   same day, the overlay, the grid, the picking debug and the scene viewer with their four examples,
   the table at 371 written and 5 missing, the lock gaining `bevy_dev_tools`, the ABI 293. The
   system information came at `8ae312a4` the same day, `log_diagnostics` whole, the table at 372
   written, 1 in part and 5 missing, the lock gaining `sysinfo` and its four, the ABI 294. The two
   declined rows are marked at `b7cd83db` the same day with Decision 39's reasons, the table at 5
   missing with 2 declined. `monitor_info` came at `142dc131` the same day, its picture taken on a
   headless compositor's four virtual outputs in a container by a script under `build/`, the row
   written. Done at `142dc131`, the table at 373 written and 5 missing, 2 declined by Decision 39
   and 3 by Decision 13, no gap left that a word could open.

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
   list only getting shorter. Done at `5d955562`, the two helpers listed. The free camera helper
   left at `670ecf2c` for Bevy's controller and the radio buttons helper at `319624bf` for Feathers'
   radio, and one file mirroring the three helper files Bevy's own examples take in by path stands
   on the list with that reason, by item 29's part (k).

9. **A script that more than one system runs is read for the forms only GNU's tools or a later bash
   read**, from 3DEngine's `ScriptTests` (`fd7b17f3`): one test over the scripts the workflows and a
   developer run on Linux, macOS and Windows' Git bash, where one line was found there. Done at
   `8f02d976`, every script of the checkout read for ten forms, none found.

10. **Fixes for the generator's diagnostics offered in an editor**, from 3DEngine's
    `3DEngine.CodeFixes` (`c6b529d4`): a code fix beside each diagnostic the behavior and command
    generators report, so an editor offers the mend. Done at `d328fdc3`, `BevyCSharp.CodeFixes`
    fixing the five diagnostics with one clear mend and naming the seven without, packed beside the
    generator.

11. **The CLI server writes an answered request before it closes, done at `c3aaacac`.** `app.quit`'s
    answer is completed inside the frame, the app ends at the frame's end, and the server's dispose
    shuts both directions of every open socket, so a connection thread that had not yet written the
    answer writes nothing and the client reads the end of the stream, which Verdict 11 found through
    a test's stop and the CLI's own verb copes with by judging by the session's end. The dispose
    shuts the receive side alone, joins the connection threads within a bound, then closes, so a
    request answered is a request written, with a test that reads `app.quit`'s answer; taken as a
    batch of its own on 2026-10-11 since every free gap is done, the run reading it on three
    platforms, where it was to wait for the batch that next touched the server. Done at `c3aaacac`:
    the waits cancelled first so an answered request keeps its answer, the receive side shut alone,
    the threads joined within a two-second linger and a socket still reading closed after it, with a
    test of three hundred rounds that read every answer where the old server lost ten.

12. **A resource inserted from nothing replaced the plugin's with a zeroed one, done at `e1a32283`
    (Decision 26).** `InsertResource<T>()` with no JSON builds a type that reflects no default field
    by field, so `GlobalUiDebugOptionsRef` came with a line width of zero and the outline off where
    Bevy starts it at one and on, as `68aa5dd5` found, and the same holds for every reflected type
    without a default, uncounted; the examples and the tests insert seven resources from nothing
    today, five kinds. Measured first: the reflected resources without a default counted from the
    components table, and each insert from nothing in the examples and the tests read for whether it
    wants the zeros. Then the rule: an insert from nothing over a resource the world already holds,
    of a type without a default, throws and names `Resource<T>`, since a zeroed copy over the
    plugin's is never meant, and an insert where the world holds none keeps the field-by-field
    build, the remark saying what it gives; a test holds both. Done at `e1a32283` as the measure
    asked: the count 68, 8 and 7, the seven inserts in the examples and guides each guarded, the
    refusal only over a resource the world holds, and a fault of the bridge found and mended with
    it, a resource taken off its entity answered as held.

13. **The README's gallery as one scrolling row of pictures a group, done through `83b371cc`
    (Decision 27).** The owner asked on 2026-10-11 at 15:36, as for 3DEngine's README.
    `build/examples-table.py` writes the gallery between its markers as one table of four pictures a
    row over every written example; it writes instead one table a group in the order Bevy's
    `examples/README.md` lists the groups, which the script already reads, each a single row holding
    the group's written examples in that order, the picture opening the program and the name under
    it as today, with a heading line naming the group before each table, and the examples Bevy keeps
    out of its list in a last row of their own; a row wider than the page scrolls sideways on
    GitHub, which is the point, so no row is wrapped. The script's check reads the result as it
    reads the table today, so the workflow's table check and the README's walk run on it, and the
    raw links keep their form for the package's page. A batch of its own before low_power, since it
    is small and the owner asked; the owner's eye judges the look. Done at `b06ff120`: a heading and
    a one-row table a group in Bevy's order, the examples in each in the order Bevy's list gives
    them, the kept-out ones last, 28 groups, the check passing. The owner asked at 15:52 that the
    pictures be one size, where they differ today: every picture carries the same width and height
    on its tag, 200 wide as before and the height of sixteen by nine, and the captures that are not
    of that shape, four of 304, a square, a four by three and two of five by three, are taken again
    at it where the example's window allows or padded to it by the capture script, so the tag
    distorts nothing; the table check and the captures' comparison read the result, the comparison's
    references following the captures. Done at `21d08b44`: every tag 200 by 113, four captures
    padded to sixteen by nine by the capture script, which pads any capture that needs it;
    `b06ff120`'s gallery had passed N 4.2's 320 lines, mended at `d0e638d7` before its page could
    show it. The owner looked at the pushed page at 16:04 and the pictures still read as different
    sizes. GitHub renders every tag at one box, its own style giving each the full width of its
    cell, an automatic height and the tag's aspect, so the box is uniform; what differs is the
    picture inside it, since a capture of another shape was padded with nothing to sixteen by nine
    and the page fills the padding with its muted color, which reads as a smaller picture. The fit
    fills instead: the capture script scales a shot of another shape so its shorter side fills
    sixteen by nine and cuts the rest around the middle, or the example's window is opened at
    sixteen by nine where the cut would lose something that matters, so every picture fills its box
    edge to edge; the four captures are taken again so, the comparison's references following, and
    the page is read after the push. Done at `04cefc8e`: the capture script fills and cuts around
    the middle, two examples filled so and two opened at a sixteen by nine window where the cut
    would lose their point. The owner looked again at 16:12: each picture is as wide as the name
    under it, a short name's tiny and a long name's large. GitHub squeezes a table to the page's
    width before it lets it scroll, and a cell shrinks to the one thing in it that cannot wrap, the
    name, the picture shrinking with it under the page's own rule of a picture no wider than its
    cell. The script gives each cell a width of its own, `width="200"` on every `td`, which the
    page's sanitizer keeps as it keeps a picture's, so no column goes below it and the row keeps its
    full width and scrolls; a header row of non-breaking spaces is the fallback should the attribute
    be stripped, which the rendered page says after the push. Done at `f60bc166`: every cell 200
    wide, N 4.5's reader widened to a cell with attributes, the rendered page read after the push.
    The owner looked at the pushed page at 16:20 and the pictures still differ. The rendered page
    keeps the cell's width, read here, so the squeeze is the browser's table layout: a cell's width
    is a preference, and when a table is wider than the page the browser shrinks each cell to its
    narrowest unbreakable content, the name, the picture with it, GitHub's table being a block
    capped at the page's width that scrolls only past that minimum. So the minimum comes from
    content: the script pads the run name under each picture, in its code tag, with non-breaking
    spaces, the character itself and not the entity, to thirty characters, which the code font draws
    at some 246 pixels, past the picture's 200 on every platform's monospace and past the longest
    name, so no cell can shrink below its picture and the name reads as a caption bar; the cell's
    width stays as the preference, N 4.5's reader and the prose count checked, and the rendered page
    read after the push. Done at `e92591dd`: every name at thirty characters or more, the page read
    after the push. The owner looked at the pushed pages at 16:30: the look is right, and on a phone
    the caption under each picture is a tenth to a fifth wider than the picture, on a desktop a
    little wider. The caption's padded text sets the cell's width while the picture fills no more
    than its own size, so on a font that draws text wider, as a phone's does, the text overshoots
    the picture. The caption sets the width on purpose and the picture fills it exactly: each
    picture's tag and cell take a nominal width past the caption's widest rendering, so the cell is
    the caption's width on every device and the picture, no wider than its cell by the page's rule,
    fills it edge to edge; and the caption shrinks to the small tag, `<small><code>`, which the page
    keeps, since the padding cannot go below the longest name and the font is the other knob: the
    names padded to 35 characters, the longest, in a small code caption, every tag 260 by 146 and
    every cell 260, so the pictures come out about 260 wide and the same width as their captions;
    the owner tried a padding of 28 with the longer names broken by a `<wbr>` tag and the tags at
    220 on 3DEngine's page at 16:38 and prefers this form, its pictures filling up to the caption's
    width where the other's fell short, the names being Bevy's and the table's keys, so none is
    renamed; the rendered page read after the push on a desktop and a phone. Done at `bbb7de81`:
    captions of 35 set small, every tag and cell 260. The owner looked at both pushed galleries at
    16:58 and finds 3DEngine's the better: its captions are padded to 32, narrower than the picture
    of 260, so the picture shrinks to the caption and the two match edge to edge, where BevyCSharp's
    35 draws past the picture. The padding goes to 32 as 3DEngine's, the one name longer than that,
    `shader_material_screenspace_texture`, left unpadded with its cell a little wider, the tags and
    cells unchanged, so the two galleries read alike. Done at `83b371cc`.

14. **The bcs skill left the `.claude` folder, done at `53fd0c3f` (Decision 28).** The owner asked
    on 2026-10-11 at 15:50 that `.claude/skills/bcs-cli/SKILL.md` go to the root or elsewhere, the
    `.claude` folder and long subfolders holding nothing else being unwanted, as for 3DEngine's. The
    document becomes `.github/BCS.md`, beside the other documents a session reads, its skill header
    dropped since it means nothing outside that folder, its body as it is; AGENTS.md's line on
    controlling a session and its documents table name the new place, which the owner's ask is the
    word for, as does STYLE.md's line on the skills; the folder goes with the move, and `.claude/`
    joins `.gitignore` so the tool's own lock file there never enters the tree. A batch of its own,
    taken when the owner types the ask in the working session, which changes AGENTS.md and the
    tool's own folder only on the owner's word given there and has asked the owner for it, a right
    the reviewing session holds. Done at `53fd0c3f` on the owner's word typed there:
    `.github/BCS.md`, every reference moved, the folder gone and ignored.

15. **The development machine's GPU is named nowhere a user reads, done at `c447a370` and `c16f67e5`
    (Decision 29).** The owner asked on 2026-10-11 at 17:26, as of 3DEngine, that the mention of
    their machine, the RTX 4070, leave the code and comments a user sees: here `Render.Pipeline.cs`,
    the running guide and the comparison with Bevy name it, each where a measurement was taken; the
    measurement stays and the device becomes a plain phrase, the development machine's GPU or a
    laptop GPU of its class. The documents under `.github` keep the name unless the owner says
    otherwise. Done at `c447a370` in the three files, the CPU's model gone with the GPU's; the
    Solari comment at `c16f67e5`, so nothing outside `.github` names the model.

16. **An offscreen click lands only where the pointer has been, done at `4e6eab36`.** Found at
    `d91665d9`: run offscreen through `./bcs`, a click at a point the pointer has not moved to is
    lost, the button example counting two of three clicks at one spot and every one once the pointer
    has been moved there a few frames first, since picking wants the hover a frame before the press.
    The click moves the pointer there and gives the frames the hover takes before it presses, in the
    CLI's verb or in the harness that runs the examples, so a session or a test clicking a place it
    has not hovered loses nothing, measured by the example's three of three, with a test. Done at
    `4e6eab36`, two hover frames before the press and a test of three of three.

17. **The examples' counts under the Examples heading, and a count on each group's heading, done at
    `02598971` (Decision 30).** The owner asked on 2026-10-11 at 19:35. The line the table script
    writes between its markers, of Bevy's 438 examples how many are written here, in part, waiting
    on the bridge and about Rust itself, stands under the status heading today and moves under the
    Examples heading, after its paragraph and before the gallery, the markers with it, so a reader
    sees the count where the pictures are; and each group's heading in the gallery carries its
    count, the written examples in its row and, where Bevy's group holds more, Bevy's count beside
    it, as `### 2D Rendering, 26 of 27`, the form the working session finds reads best and the
    owner's eye judges. The script writes both, so the check reads them, with the prose count kept
    and the README walk run on the result. Done at `02598971`, the heading counting the group's
    examples written whole rather than its pictures, since one that prints has none, said above the
    gallery.

18. **A vertex stream of the game's own on a mesh, Bevy's `custom_vertex_attribute` reached from C#
    (Decision 31), done at `32ae29fb`.** The owner put a packed-vertex game in 3DEngine's `.ref` on
    2026-10-11 at 20:05, one float a vertex unpacked in the shader, and asked for the same in both
    engines; the plan was approved at 20:40 and its full text is kept by the reviewing session, this
    item being what it asks of the bridge. `MeshData` gains `Streams`, each a name, a format of
    five, `UInt`, `UInt2`, `Float`, `Float4` and four bytes read as 0 to 1, a location from 8 to 15
    and its bytes, the shader declaring the input at that location's `TEXCOORDn` as every input here
    is, so the game writes one number twice and they agree; positions stay required, since Bevy's
    mesh pipeline places them at location 0 before the bridge's specialize runs and its prepass,
    culling, meshlets and Solari read them, a voxel vertex then 16 bytes, 12 of position and 4
    packed, where 48. The streams travel in the mesh's own call, appended to `BcsMeshData` and its
    C# mirror in lockstep with a size assertion, so `WriteMesh` keeps them, where a side call as the
    joints' would lose them on the next write, and the joints are carried over in that write too,
    one line; a registry by name of the attribute Bevy wants, the name interned once and the id a
    hash of it set above Bevy's own so it sorts the same on every run, refusing a name that returns
    in another format or location or two streams of one mesh at one location, which C# turns into
    the exception with both values; `mesh_from` inserting each stream as Bevy's values of its
    format; the mesh info's bitmask gaining a bit for a stream of the game's own. In
    `user_specialize`, where a program has a vertex stage of its own and the mesh's layout holds a
    registered stream, the vertex buffers are built again from Bevy's attributes at Bevy's locations
    plus each registered stream the layout holds at its own, the prepass attributes gaining the
    same, and the 2D material the same rebuild; the layout already keys Bevy's pipelines.
    `bcs.slang`'s header says how a stream is declared and carried on, that the Slang class must
    match the format and that `vk::location` is ignored, the drawing and shaders guides their
    sections, and the scene file keeps streams with the mesh. The example is Bevy's
    `custom_vertex_attribute`, a cube with a `Float4` stream at location 8 blended toward red,
    written in C#, its row leaving triage as written examples do, the counts moving by one. Tests:
    native, a mesh from one stream holding the registered id and its values and the same name in
    another format refused; managed, a quad with a `UInt` stream of a color a vertex drawn by a
    program reading `TEXCOORD8` to the pixel, the exception for a wrong length, a location outside 8
    to 15 and a reused name, and a stream in the scene file's round trip. ABI 269 to 270. Done at
    `32ae29fb` as written, the mip generation's layers refused with it.

19. **A world a scene file saves whole: a component's text, a field left out and a resource of the
    game's own, Bevy's `world_serialization` reached from C#, done at `c9b8728f`.** The row's triage
    line names three gaps, each real, read by the working session at `7bc0af69`. First, a component
    struct holds no string, strings being allowed only on managed types and a component's strings
    living in `EcsList<string>` and `EcsMap`: an `EcsText` handle into the same managed store, as
    `EcsList` is, converting to and from `string` implicitly, freed by the same removal and despawn
    hook, given its own copy by `Clone`, written to a scene as a JSON string and drawn by the
    inspector as a text field. Second, nothing marks a field a scene file leaves out: `[NotSaved]`
    on a component's field, Bevy's skip of serializing, which the schema marks, the scene file skips
    on a write and a load leaves at the value the type starts with. Third, a scene file writes no
    resources of the world, its resources being meshes and materials: a resource struct of the
    game's own marked `[SceneResource]` gets a generated schema as a component does,
    `SceneFile.Save` writes those present in a `worldResources` array and `Load` inserts them. The
    example is then Bevy's in shape, a component of two numbers, one of a text and a time since
    start left out, and a resource of a score, the scene saved under the user's folder and its JSON
    printed, and a checked-in JSON scene loaded with the helmet and the log of what was added, since
    Bevy's RON scene names Rust types a C# app cannot load. Generator, scene format, inspector and
    docs work, with the generator's tests for the text and the attribute, a scene round trip of
    each, the inspector's field, and a scene from before reading as before. Done at `c9b8728f` as
    written, with the resources kept out of the editor's undo path and a field left out riding
    through a script reload.

20. **A mesh without normals on purpose, `MeshData.ComputeNormals` (Decision 32), done at
    `7f42cf40`.** `mesh_from` works normals out for every triangle mesh given none, 12 bytes a
    vertex that a shader unpacking its normal from a stream's four bytes never reads, so a packed
    voxel vertex is 28 bytes where it could be 16. `MeshData.ComputeNormals`, true by default so
    every mesh is as it was, false with `Normals` null leaving the mesh without them, the remark on
    `Normals` gaining the switch; Bevy's mesh pipeline places the normal only where the layout holds
    it and a prepass reads the position alone, so such a mesh draws and casts its shadow, and a lit
    material of Bevy's own on it reads a zero normal, said in the remark and the drawing guide's
    stream paragraph with the sixteen bytes. A `flags` field of four bytes appended to `BcsMeshData`
    and its mirror in lockstep, bit 0 the switch, the layout test gaining its offset and the new
    size, the ABI bumped or folded into the row under way's bump; a native test that a mesh with the
    flag and no normals holds none. The shaders guide's stream section says, with this item, why the
    packed bytes are a vertex stream and not a storage buffer read by the vertex id, which the owner
    asked at 23:15: a stream travels with the mesh, uploaded once with its positions and indices and
    read by every draw of it with nothing else bound, where a buffer is bound to the shader or the
    material, so thousands of chunks would each need a binding a draw or one giant buffer with an
    offset a chunk; meshes sharing a shader keep drawing in one batch; the GPU fetches the attribute
    by the vertex index as it fetches the position, where a buffer read needs the vertex id by hand
    and an offset an instance when instanced; the fetch is about as fast either way, so the gain is
    the binding model; and a buffer stays the right tool for what is not per vertex, a palette or
    atlas table every chunk shares, per-chunk constants, or what a compute pass also reads. Done at
    `7f42cf40`, meshes.rs split first into mesh_data.rs.

21. **A behavior's `[Changed]` filter on its own type fires every frame; done at `c8849d71`.** Found
    by the working session writing item 19: the chunks call stamps every row of a component the
    behavior writes before the runner tests the filter, so a behavior that reads and writes one type
    with `[Changed]` on it runs on every row every frame, which the generator's tests note and the
    world_serialization example sidesteps with a read-only query. The fix the session read: fetch
    the chunks unmarked and mark only the rows the body runs on, so a row changed by the behavior
    itself counts as changed for the next frame and an untouched one does not, with a test of a
    behavior that changes one row of three and runs on that one alone the frame after, and the
    example's query restored to Bevy's shape. Done at `c8849d71` as written.

22. **Swarm builds on the package version its project names, done at `5e750133`.**
    `games/Swarm/Swarm.csproj` names 0.0.14-game where nothing sets `BevyCSharpVersion`, and on it
    the game does not build, seven errors for `MainThread` and `CharacterController` the package of
    that version lacks; the workflow builds it on 0.0.0-ci, so no job sees it, and a reader cloning
    the game does. After Decision 33 lifts: the default follows the package the game was last played
    on, or the games take their version from one place the pack writes, and a check that each game
    builds on its own default where the package is published. Found at `9f76fded`. Read again on
    2026-10-18 with Decision 33 lifted, from the working session's measurement. All four games name
    versions nuget.org has not, local packs of earlier sessions, Courtyard and Swarm 0.0.14-game,
    the stress program 0.0.25-game and the first game 0.0.38-game, so a reader cloning the
    repository builds none of them; Courtyard, the stress program and the first game build on
    0.4.129, the newest of the ten published, and Swarm does not, wanting `MainThread`, which no
    published version carries yet. The shape chosen is the session's. Each game's default names the
    newest published version it builds on, three at 0.4.129 and Swarm at the version it needs until
    the next publish carries it, its project's comment saying the checkout is packed under that
    version to build it; the pack job, when it publishes, builds each game on its own default from
    nuget.org after the push, waiting on the index with a bounded retry whose cost the job's limit
    comment counts (N 6.8), and fails with an error naming each game whose default does not build
    and the version that push carried as the one to raise it to, the raise being a one-line commit a
    game; and BUILDING.md says what a game's default is and how a game whose default awaits a
    publish is built. No shared version file, since a game here is built as one outside the
    repository is, from the package and its own project file alone, and the version last played is
    0.0.0-ci, never published. Done at `5e750133` as chosen, the three games at 0.4.129, Swarm at
    0.5.0 as a minimum the first publish of 0.5 meets, the publish check in the pack job and
    BUILDING.md saying the rest. One word to mend in the next commit, under N 4.1 and STYLE.md's
    banned list, the eight places the commit says the version was pushed or published a moment ago
    with the word the list names, in BUILDING.md, the pack job's comment, the script's head, usage
    and error and the project comments, which read "the version this run pushed" instead.

23. **Two workflows an engine, `build.yml` on a push and `pack.yml` by hand (Decision 34), done at
    `b178ae3f` over `0dff9601`; proven by the push run and the pack run of `b4d45938` on
    2026-10-18.** `package.yml` folds into `build.yml`, which gains `push`, `pull_request` and
    `workflow_call` with one input, `pack`, and the `docs` job, its inputs `version`, `targets` and
    `test_os` gone and the matrices literal: `native (rid)` (pack only), `test (os)`, `digest`,
    `docs`, and pack-only `game`, `drive`, `examples`, `storm` and `readme`, each needing `native`;
    the push's paths filter becomes 3DEngine's (`['**', '!.github/**.md', '.github/BUILDING.md',
    '!AGENTS.md', '!games/**.md', '!.github/assets/**']`), so a docs-only push runs the docs check
    it skipped under `paths-ignore`, and `branches` goes. `pack.yml` keeps `workflow_dispatch` with
    `publish` and runs `build` (uses `build.yml` with `pack: true`) then `pack` (needs `build`, 20
    minutes, through `step.py`): the version read in it with `fetch-depth: 0`, the six `native-*`
    artifacts laid out under `build/artifacts`, the notices check, the release notes, `dotnet pack`,
    the templates, what shipped, `NormTests N_6_4|N_6_5`, the upload, the push to nuget.org if asked
    and the summary, so a package is made only when every job passed, where today's pack job needs
    `native` and `test` alone; the `version` and `publish` jobs go. `feature-test.yml` and
    `scenes.yml` are deleted: the tester zips are `build/publish-feature-test.sh` run on the owner's
    machine (BUILDING.md:364-379 rewritten around the recipe at 373-379; the Windows zip needs a
    Windows machine with `--cache`), and the Sponza run is `build/scenes.sh` (the meshlet and solari
    bridge, `build/fetch-slang.sh`, `./bcs scenes fetch intel-sponza`, the feature test built strict
    and run with `SHOTS=build/scenes`), BUILDING.md:356-363 saying so. Both files: `permissions:
    contents: read`, concurrency `build-${{ github.ref }}` cancelling in progress and `pack-${{
    github.ref }}` not, a time limit on every job with its reason (N 6.8), a head comment saying
    what a push runs and what the pack run adds; a push still tests the library alone, the render
    bridge costing a quarter of an hour cold, said in BUILDING.md in one sentence beside 3DEngine's.
    Lines that change: `build/step.py:6-7`, `.github/BUILDING.md:63, 213, 336-379, 481, 498`,
    `README.md:202, 266`. Checked by `WorkflowTests`, `TestScriptTests`, `NormTests`,
    `build/check-docs.py`, a push run green with the pack-only jobs skipped, a docs-only push
    running, and a pack run green with its package, which are Decision 33's gate (Decision 35). The
    norm's and SHARED.md's lines are the reviewer's and ride with the commit.

24. **The docs job leaves the build workflow (Decision 36); the repository's own links are a test of
    the suite; done at `1d246e9f`.** The `docs` job and its `build/check-docs.py --external` go from
    `build.yml`, with the head comment's line on it; a test of the suite, in `DocsScriptTests`, runs
    `build/check-docs.py` without `--external`, so every link of the README and the guide into the
    repository is held to a file and a heading that are there, with no request made (N 4.3), and the
    script's `--external` stays for a run by hand before a package. BUILDING.md:339, 347 and 513 say
    the suite's test and the hand run where they said the job, and the push paragraph's sentence
    that a push of the README alone runs the docs job goes with it. Checked by the test passing here
    and failing on a link to a page that is not there, `WorkflowTests`, `NormTests`, and the push
    run showing `test`, `digest` and nothing else.

25. **The register: STYLE.md's rule and the sweep (Decision 37), done at `f8e9a6d2`, the figures
    Decision 37 adds at `b4d45938`.** STYLE.md gains a section, Register: prose uses the technical
    and generic term, the engine's own where it has one, and no figure of speech or casual
    paraphrase, with the pairs the owner named and their replacements: the light that bounces
    becomes global illumination; gives off light and glowing become emissive; driving a running
    program becomes controlling or scripting it through the command line, and a drive a scripted run
    or a course. The colon joins it: STYLE.md's rule, which allows a colon before a list, tightens
    so that a running sentence holds none, the sentence recast or split where one appended an
    elaboration or a list, as in "makes the program the README opens with: a window with a turning
    cube"; a colon stands before a bulleted list, a code block or a command, and as a label in a
    table, and STYLE.md's check of colons joining clauses reads prose lines of Markdown as well as
    comments. The sweep covers README.md, `docs/`, AGENTS.md, `templates/README.md`, every Markdown
    under `.github`, the comments of the code, the scripts and the workflows, and the sentences a
    program prints; 20 documents hold the drive words some 90 times and one the first pair once, and
    a rough count finds a colon inside a sentence on some 180 prose lines of the documents and 160
    comment lines of the library and the examples. `build/drive-feature-test.sh` and the `drive` job
    keep their names as identifiers, the job's shown name becoming "run the feature test's course"
    and the comments reworded. The REVIEW.md, NORM.md and SHARED.md lines are the reviewer's and are
    swept already. Checked by `NormTests`, the links test of item 24 and `git grep -i` for the pairs
    finding the channel's history alone. One commit. 3DEngine's game repository did this first at
    its `b87e386`, and its STYLE.md's Register section and colon rule, with their checks, are the
    text to take, with this engine's examples in place of the game's. Open again on 2026-10-17 at
    20:34 for the figures Decision 37 adds, a likeness in place of a description and an idiom in
    place of a term. At a glance stands in `BevyCSharp/Assets/GizmoJoint.cs:7`,
    `Assets/Gizmos.Settings.cs:274`, `Ecs/ColorAttribute.cs:6`, the editor's `AssetGrid.cs:51`,
    `EditorDrawn.cs:411, 576`, `EditorRows.cs:132` and `FieldPickers.cs:41`,
    `BevyCSharp.Tests/NormTests.cs:217`, `CHEATSHEET.md:1233`, `docs/gizmos.md:98` and
    `.github/ASSETS.md:137`; a handful of in `Behaviors/BehaviorRunners.cs:14`,
    `Input/Input.cs:198`, `Math/Quat.cs:124`, `Physics/PhysicsWorld.Sync.cs:60`, the editor's
    `EditorStats.cs:204`, `EditorToolbar.cs:67` and `PreviewRenderer.cs:465`,
    `BevyCSharp.FeatureTest/Behaviors/Movement.cs:6` and `.github/PERFORMANCE.md:121`; the knob in
    `Assets/GradingSettings.cs:15` and `Assets/Render.Cameras.cs:229`; out of the box in
    `.github/RENDERING.md:383`; a great deal of in the editor's `DetailsPanel.cs:846` and
    `native/bevy_csharp/src/render/bcs.slang:286`; plumbing in
    `BevyCSharp.Tests/Behaviors/BehaviorTests.cs:141`; a long way in
    `BevyCSharp.Tests/Assets/ExposureTests.cs:32`; and whatever a sweep for such figures finds
    beside them, STYLE.md's Register section carrying the rule with the owner's example. The box a
    joint leaves (`Assets/Render.Skins.cs:121`) and the road to a signpost (`Zones.cs:125`) are
    literal and stay, and the third-party notices are their authors' text. The reviewer's line in
    NORM.md is reworded here. Done at `b4d45938`, 70 rewordings across 64 files, the idiom list
    grown by the knob, the plumbing, the wiring, an escape hatch and for free, a handful made the
    count the code gives, and the literal senses kept.

26. **The reflected variants holding a tuple, an instant or a graph's index get typed properties,
    TODO.md's open line under the components through reflection, item 4's remainder, done at
    `c3bd5349`.** Chosen on 2026-10-18 as the next work, since every other row of the Now list and
    of TODO.md waits on a crate the lock lacks, on a word the owner types in the working session
    (Decisions 11, 12 and 20; Feathers, the camera controllers, the dev tools, `sysinfo`, `ctt`,
    `ureq`, the template's publishing) or on a run on the owner's screens (`monitor_info`), and the
    font examples stay off under Decision 13. The generated wrappers type an enum inside a variant,
    a list, a duration and an integer that holds no zero, and leave an `Option` of a tuple (a text
    layout's cursor, a window's pan gesture, the focus and its cause), of Rust's `Instant` (the real
    clock's first and last update) or of a node index (animation transitions' main animation) with
    its variant names alone, the value on a string path checked when it runs; each gets a typed
    property, with a test a kind, and TODO.md's line and COMPONENTS.md follow. One commit, the eight
    register rewordings riding along. Done at `c3bd5349` as asked, a test a kind and the lists of
    tuples left named in COMPONENTS.md.

27. **Byte mirrors for the components that are plain data, COMPONENTS.md's third tier, TODO.md's
    next open line, in steps, done at `1a6c5d22`.** Chosen on 2026-10-18 at 03:10 over the smaller
    atmosphere line, by the list's order, the working session having measured that 141 of the 352
    described components hold only fixed-size fields or fieldless enums. The build generates a Rust
    export a type reporting its size, alignment and each field's offset, which replace the hand
    layout functions, the generator writes a byte mirror for each type that qualifies, and
    `bcs_component_id_of` resolves any reflected path, its short-name arms leaving with the hand
    mirrors. The first step is a commit of its own and bounded, since `offset_of!` from the bridge's
    crate wants each field public and a Bevy type with private fields does not qualify, two of the
    five hand mirrors perhaps among them: it counts the qualifying types with public fields, emits
    their layout exports and the checks that hold them, and reports the count and the types left out
    before the mirrors are written; the mirrors and the lookup by path follow in commits of their
    own, the hand mirrors staying for the types that do not qualify. The atmosphere's custom medium,
    a bridge call, a settings record, a test and the guide's line, is the line after. Changed at
    03:10 on the working session's finding, before the first step. `offset_of!` compiles only where
    every field on the path is public from the bridge's crate and reaches no field inside a variant,
    so choosing the types would mean parsing Bevy's sources; instead the layout is read through
    reflection on a value made at its default, a reflected field being a reference into the value
    whose address less the value's is the offset the compiler chose, private fields included, which
    a spike on `Transform` gave as 0, 16 and 28, `offset_of!`'s answers, with the size and alignment
    from the component's registered layout. One export, `bcs_reflect_layout` of a type path, an ABI
    bump; the mirrors are generated from layouts dumped into the description as the wrappers are,
    and a test of the suite and a check at a mirror's first use compare the dumped layout with the
    running bridge's measurement, refusing on a difference, so a Bevy upgrade that moves a field
    fails a test and not a game's bytes. The first step holds the export against the hand layouts,
    `Transform`'s three offsets, `GlobalTransform`'s four, `Visibility`'s size with its
    discriminants staying on their own function, and the two visibility types whose field is
    private, and counts of the 141 how many have a default and every byte in a reflected field or
    padding, naming the types left out and why, no default, an ignored field or a variant's field.
    The hand mirrors stay for a type reflection cannot measure, and COMPONENTS.md's bullet says the
    layout is the compiler's, read from a value rather than written at build time. The first step is
    done at `5f8e4e07`, the export holding every hand layout and 115 of 140 candidates mirrorable,
    24 left out for a variant's value and 1 for an ignored field; step two, the dumped layouts, the
    generated mirrors and the two checks, follows. The second step is done at `9b539deb`, the 115
    layouts dumped, 100 mirrors generated with 15 kept on their wrappers for an entity, a duration,
    a range or a color, the suite's test and the first-use check in place; step three, the lookup by
    path retiring the hand mirrors, follows. Step three's shape, chosen at 04:45 on the working
    session's reading. The five hand types stay, being the library's own surface, `Transform` with
    its constructors and methods in 353 files, `GlobalTransform` in 25 and the visibility types in a
    score, and what retires is the hand machinery under them, the three hand layout exports, the
    three verify functions with the size check through the component layout, and the five short-name
    arms of the id lookup, an ABI bump; each hand type names its component by its reflected path and
    carries the description's layout, so the first-use check and the suite's test hold it as they
    hold a generated mirror, a test holds each hand type's declared offsets to the description's
    signature, and the generator writes no mirror for a component a hand type covers, the five
    generated at `9b539deb` leaving the surface. The arms for the name-only handles, the child and
    children relations, the world instance and the atmosphere, and for the bridge's own interaction
    type stay. The third step is done at `1a6c5d22` as chosen, the hand types kept on the
    description's layouts and the hand machinery gone, which ends the item, 95 generated mirrors
    beside the five of the library's.

28. **An atmosphere takes a medium of the game's own, TODO.md's rendering line, one commit, done at
    `c5f19669`.** Chosen on 2026-10-18 as the line after item 27. Bevy's `ScatteringMedium::new`
    takes a game's own scattering and absorption terms where the bridge offers earth's and Mars's
    alone; a bridge call, a C# settings record, a test that a custom medium draws otherwise than
    earth's, and the guide's line. Done at `c5f19669` as asked, with a curve or a picture for a
    falloff or a phase left named in TODO.md.

29. **What was mended for the time being is mended for good (Decision 38), after item 4.** The owner
    said on 2026-10-18 at 22:25 that once everything is bridged, the places where a mend of the
    moment stands are to be mended fundamentally. First the list, in a commit of its own: every
    place mended for the time being, in the code's comments and TODO.md's lines, and in the
    verdicts' mends that bounded, listed or said a figure in place of removing a cause, among them
    the captures' differ lines whose cause is the machine's clock, the pictures taken on a bridge
    without Solari for the `dfg_lut` it enables, the measure's rooms and the figures said and not
    judged, each with what the lasting mend is and what it costs, written here as parts of this
    item, and the reviewer orders them with the owner. Then each part is mended on its own, one
    commit a part, measured where a figure moves (N 3.5). Listed at `2b355276` in TODO.md's section
    "Mended for the time being", thirteen parts, taken here in the reviewer's order, the cheap and
    self-contained first and the ones that are the owner's to decide last: (a) `many_sprites` and
    `many_animated_sprites` seeded under the workflow, two lines off the differ list, done at
    `024c1113` on 2026-10-19, the pictures matching in every pixel across two bridges; (b) Solari's
    panel hiding the device's timings under the workflow, its room measured again for the noise
    alone, done at `ddce5717` on 2026-10-19, the noise at 0.17 percent of pixels across devices
    where the line allowed 3, and Mesa 26.2.2's lavapipe drawing Solari whole, evidence for (h); (c)
    the soak's heap judged after a compacting collection in `memory.collect`, the managed total
    dropped from the answer, done at `30b87d1f` on 2026-10-19 with a second cause mended, the CLI
    server's accept buffer pinned while it waited, the heap within 8,672 bytes of live over
    ten-minute soaks of three games; (d) the soak's other rooms measured from soaks of the three
    programs, the process figure read as private memory without the driver's mappings, done at
    `b9069e8c` on 2026-10-19, each room twice the most its figure moved over the soaks named in the
    check's comment, the process figure the anonymous pages on Linux; (e) a `TimeProvider` on the
    app's clock for the two async examples, their generators seeded under the workflow, two lines
    off, done at `3a2e403a` on 2026-10-19, a time provider on the game's clock as `Time.Provider`,
    the two pictures matching in every pixel across three devices; (f) the bridge's clock held while
    an asset loads and between the drive's commands, one hold serving the two animation examples and
    the drive's swing, every capture taken again, done at `714451b0` on 2026-10-19, the clock held
    while a load runs and between a served session's commands, the drive's readings identical on two
    devices and two differ lines off; (g) `dfg_lut` on in the render profile, the owner's decision,
    since games on the package are then lit a little otherwise and every 3D picture is taken again
    (Decision 25), done at `93600e85` on 2026-10-19, the lookup table's feature on in the render
    profile with no crate added and the lock unchanged, eleven pictures retaken on a bridge with it,
    the three that stood apart between bridges within a pixel in the runner's image; (h) a newer
    Mesa on the runner with a `--meshlet --solari` bridge for the six uncompared examples, the
    owner's decision on the jobs' image; (i) lavapipe's dropped bunnies traced and reported to Mesa,
    outside the repository, and found at `714451b0` on the GPU as well, a different subset of the
    2,500 bunnies drawn at each take, so the differ line's reason is incomplete; (j) the anisotropy
    fault reported to Bevy, the owner's decision, a patched shader declined; (k) the two helpers go
    with item 4's gaps; (l) runners with a graphics card, the owner's purchase; (m) the norm's lists
    are item 2's. The mends follow item 4 as Decision 38 orders, unless the owner says the parts
    that need no decision may start while the words for item 4 are awaited. On 2026-10-19 the owner
    decided (Decision 39): item 4 comes first and then this item in the order above; (g) `dfg_lut`
    goes on in the render profile with the lock byte for byte the same; (h) and (l) are one, the
    capture jobs in a container of a distribution image with a newer Mesa and a Linux bridge built
    with meshlets and Solari, no runner with a graphics card and no Mesa from an archive outside the
    distribution; (j) the anisotropy fault is mended by a patched shader of Bevy's in the bridge,
    which overrides the earlier decline, with a test that fails when Bevy's original changes. Part
    (k) is whole at `319624bf`, half done at `670ecf2c` with the free camera helper gone for Bevy's
    controller, the radio buttons helper gone in Feathers' last step, where Bevy 0.20's own fourteen
    examples with option rows share three files by path, a radio row of Feathers' radios under a
    group, a theme of some sixty token colors replacing the whole and, in two of the fourteen, a
    labeled number input holding a float with its precision and limit, ten of the fourteen written
    from 0.19.1 differing from their sources by 100 to 560 lines in that swap, so the reviewer
    decided on 2026-10-19 that the theme's setting from token colors is the package's own call, the
    port's helper of its own goes, and one file mirroring Bevy's helper files the examples use at
    the pinned version, the three, stays on the package script's list with the reason naming them,
    carrying what they carry and nothing of the port's own, since the list's rule is against helpers
    of the port's own and a mirror of what Bevy's examples share is the port's fidelity, not its
    shortcut; the radio buttons go with Feathers. (h) read by 03:11 on 2026-10-20 against three
    distribution images, each capturing the six from a fresh checkout on lavapipe held to four cores
    with a bridge built with meshlets and Solari, cold in 18 minutes 51 seconds, inside the native
    job's 45. Fedora 44, Mesa 26.2.4 and the .NET SDK from its own archive, draws all six,
    `clustered_decals` 2 pixels off the GPU's picture, `meshlet` 0, `test_meshlet` 467 and
    `solari_reflections` 1,407, all within 2 percent, and `many_meshlet_materials` 48,354, 5.2
    percent, inside its 10 percent line, which the GPU's own takes differing by 6.9 percent explain,
    so the line stays with that reason; `solari` needs some 13 minutes for its 400 frames, past a
    shot's 600 seconds, so its wait rises to 1,500. Ubuntu 26.04's Mesa 26.0.8 crashes on
    `clustered_decals` as the runner's 25.2 does, and Debian trixie's 25.0.7 is older than the
    runner's, both out. But Fedora's lavapipe runs some scenes two to seven times slower than the
    runner's Mesa, `many_foxes` 316 seconds where 152, `multi_asset_sync` 331 where 57,
    `pan_orbit_camera_minimal` 332 where 75 and `error_handling` past 600 where 141, about 6.7
    seconds a frame alone, the heavy scenes alike at 200 to 290 on both, so the four shards run
    there in parallel on four cores with 40 minutes reached 65, 60, 38 and 41 of their 93 or 94
    examples and none finished, everything reached matching its picture with no allowance. The
    reviewer takes the coder's proposal under Decision 39's constraints, a container of a
    distribution image, the distribution's own Mesa and no runner with a graphics card, so the four
    shards stay on the runner's image and bridge, where they pass in 22 to 36 minutes, and a fifth
    capture job runs the six in Fedora 44 through docker from the runner with the meshlet and Solari
    bridge, some 25 to 30 minutes on the development machine, Solari's 13 of it, and compares them,
    the shards still capturing the six as words compared with nothing; the native job's matrix gains
    the Linux entry with meshlets and Solari, uploaded under a name that is not `native-*`, so the
    pack never packs it; the Mesa version is named in the workflow and asserted when the job starts,
    so a change of the distribution's Mesa is a commit that names it with the six retaken, not a
    silent shift of the reference; the job's limit carries its reason (N 6.8), the Solari wait its
    reason in the script's remark, and BUILDING.md says which image draws which pictures. One
    commit, with the SHARED.md row for the reviewer to write. The alternative, every capture in
    Fedora on eight shards or a raised budget, costs two to five times the capture minutes on a Mesa
    slower for some scenes, and is not taken; the owner is told that the shards stay on the runner's
    Mesa, a narrowing of Decision 39's words by the measured cost. (i)'s cause found by 03:14 on
    2026-10-20, and it is not Mesa's. `many_meshlet_materials` drops bunnies on every device because
    its cluster budget is too small for its crowd, Bevy's own example setting 8,192 cluster buffer
    slots for its grid of 2,500, which the port's configuration mirrors, where Bevy's own
    documentation of the field says that too low a number shows missing or blinking meshes; raised
    to 2 to the 20th in a local experiment not kept, two GPU takes on the editor bridge match in
    every pixel and the host's lavapipe comes within 1.2 pixels of them, where at 8,192 the GPU's
    takes differ by 6.9 percent and lavapipe's by 5 to 7. So no Mesa report is written. The reviewer
    decides, on the owner's precedent in Decision 39 of mending Bevy's faults in the port with the
    difference said, that the port's example takes the smallest power of two that draws the crowd
    whole on both devices, measured, the difference from Bevy's 8,192 said in its triage row with
    the documentation's sentence as the reason, so the picture is whole and compared as the others
    are, the differ line comes off and TODO.md's paragraph goes; a report of the budget to Bevy is
    the owner's call, as upstream reports are. Order: (h)'s commit first with the line's reason as
    the budget's, then (i)'s commit taking the line off, two things, two commits. (h) done at
    `17b004c0` on 2026-10-20, as the head says. (i) done at `5195e95a` on 2026-10-20, as the head
    says. (j) done at `0f8e89b4` on 2026-10-20, as the head says, and no part of the item is open.

30. **The templates package is published beside the engine, and the README's install line follows
    (Decision 39).** The owner decided on 2026-10-19 that a pack run that publishes pushes
    `BevyCSharp.Templates` beside the engine's package, which `pack.yml`'s glob over the packed
    files already takes, and that the README's `dotnet new install` line stands once nuget.org lists
    the package. Checked by the publishing pack run's page naming both packages and the README's
    line tried from nuget.org. Done on 2026-10-19 at 22:01 with no commit: the owner uploaded
    `BevyCSharp.Templates` `0.5.120` beside the engine's `0.5.120`, and the README's two program
    blocks ran as written in a clean container of the SDK's image with nothing of the repository's,
    the install from the feed, the project made from the template with the README's first program to
    the letter, a clean build, 120 frames offscreen on lavapipe with exit 0, and the guide's empty
    template built and run the same; no document said the templates waited, so nothing changes.

31. **The README's centered parts are markdown inside one centering block (Decision 40), and
    nuget.org draws them.** The heading, the description and the banners at the README's head stand
    as markdown inside one `<div align="center">` with blank lines inside it, no `<h1>`, `<p>` or
    `<img>` of their own, and every other HTML line of the README is read for whether markdown says
    it, the gallery's tables staying HTML unless thumbnails at the shown width make a markdown table
    render alike on GitHub and nuget.org, measured first; checked on GitHub's rendering and on
    nuget.org's page of `0.5.105` for what it draws today and of the next version uploaded,
    `build/examples-table.py` writing the same. Done at `b68da75c` on 2026-10-19, the head one
    centering block of markdown and the gallery markdown tables of 260 by 146 thumbnails with padded
    names, measured on both sites before it was taken, nuget.org's page of the next upload the
    check. Open again on 2026-10-19 at 22:34 for the banners: the owner asked that the README's
    centered block carry the five banners 3DEngine's carries, .NET, Graphics, Windowing, Shaders and
    Status, as markdown images inside the block, each with this project's own value, .NET 10.0, the
    graphics by what Bevy draws with, the windowing by its window crate, the shaders by what the
    bridge compiles, and the status as the project stands, the labels' colors as 3DEngine's for the
    labels the two share. Done at `8fe38f60` on 2026-10-19, the five in one row with this project's
    values and 3DEngine's colors.

32. **A package's release notes are the commits since the package last published (Decision 41).**
    The pack run's notes are the commits since `build/version.txt` last changed
    (`build/release-notes.sh`), the whole minor each time, 119 lines at `0.5.120`. The script takes
    the latest version of the package the feed lists, from nuget.org's index of the package and that
    version's nuspec, whose repository commit names where that package was built, and writes the
    commits from that commit to the head, newest first, each commit's sentence a line as before;
    where the feed answers nothing, lists no version of the minor, or names a commit the checkout
    does not have, the notes are the minor's as before and the summary says why; the pack job's step
    says which commit the notes start from. Held by a test of the script on a scratch history with a
    feed answer given to it, and read on the next package's page. One commit, after part (g). Done
    at `d482caa3` on 2026-10-19, 14 lines where 134 against the feed's `0.5.120`; the next package's
    page is its check. Widened on 2026-10-19 at 23:25 with 3DEngine's item 37: a commit whose every
    file lies under `.github/` is left out, since the package carries nothing from there, which
    takes the review's and the workflows' commits by what they touch, the README, the guides, the
    license and the notices staying in; a small commit after (g)'s. The rule is in at `a7895887`,
    the notes at 9 lines of the engine's alone. Proven on `0.5.149`'s page on 2026-10-20, 12 lines
    of the engine's alone.

33. **The pack run publishes through nuget.org's trusted publishing and no stored key (Decision
    42).** The pack job gains the identity token's write permission beside its read of the contents,
    a step with the feed's login action, `NuGet/login@v1`, given the nuget.org user name the policy
    belongs to, `EggyStudio` unless the owner says another, which answers a short-lived key, and the
    push step takes that key where it took the secret, the input's description and the head comment
    saying the run publishes through the trusted publisher policy and needs no secret, BUILDING.md's
    publishing section saying the same with the policy's values, workflow `pack.yml`, no environment
    and the package pattern; the step with the secret goes. Checked by a pack run with publish on,
    whose push the feed accepts, the first the owner starts after the policy stands. One commit, the
    pattern `BevyCSharp*`; at 22:54 the owner asked whether the pack could upload by itself, so this
    comes first, right after the step of (g) in hand, and item 32 with it. Done at `e1f516fa` on
    2026-10-19; the first pack run with publish on is its check. Proven on the pack run of
    `3eaa0a44` on 2026-10-20 at 02:00, the login and the push passing and `0.5.149` on the feed.

34. **A behavior's write after a call that moves its entity's table row lands in another entity's
    row.** Found under Verdict 38 on 2026-10-20: a behavior holds its own table row by reference
    while it runs, and a call it makes that inserts a table component on its entity, or takes one
    off, moves the entity to another table and the old table's last row into its place, so what the
    behavior writes after the call lands in that other entity; in `many_foxes` half the foxes'
    started flags landed in other foxes' rows and 499 played twice. Verdict 38's mend makes the
    animator's two components sparse, so a play keeps the row, and names what stays: the graph play
    and the clips insert Bevy's own animation player and graph handle on the entity the first time,
    Bevy's types, whose storage the bridge cannot choose, and a behavior calling them in its update
    would move its row the same way. The item is the general mend, read first for its form and cost:
    every call a behavior can make that changes its own entity's archetype found by reading the
    bridge's entry points, and either the change deferred to the end of the behavior's run, as
    Bevy's own commands are, or the runner resolving the row again after each such call, with a test
    that a behavior playing a graph on its own entity keeps what it writes; the reading says which,
    with the cost. One commit, after Verdict 38's. Read by 07:06 on 2026-10-20. Every bridge entry
    point reaches the world through one module's two functions and no other path, and behind them
    stand about 300 insert and remove sites, the explicit structural calls, the setters that insert
    where missing, meshes, materials, lights and cameras, the animation calls, which insert Bevy's
    player and graph handle and the targets' ids, and the hooks and observers a call fires;
    deferring them would change what each answers within the same body, as an insert followed by a
    read does in the animator, so the form is the runner resolving the row. The bridge keeps a
    per-thread row guard, the entity, the address of its component as the runner handed it, the
    component's id and a moved flag, which the runner gets a pointer to once a run through a new
    native call; at each exit from the world while the guard holds an entity the bridge looks the
    component's address up again and sets moved where it differs, the entity moved to another table,
    the column grown and moved, or the entity or component gone. The sequential loops, one component
    and with others, hand the method a copy of each component it takes by reference and write the
    copy back to the row when moved is clear, and by entity where it is set and the entity still has
    the component, then list the runs again and go on from the row after the entity visited before,
    each moved entity in a set the rest of the run skips, so none is visited twice and the entity
    that took its row is visited; the parallel path changes nothing, since the world is thread-local
    and a method on a worker gets no world from every call. The cost falls on the sequential path
    alone, under 4,096 entities a chunk, on the main thread or under a changed filter, a copy of
    each reference component in and out, three stores to the guard and one load an entity, every
    native call a thread-local load and a branch and one entity lookup more while a guard is set,
    and a move a listing again and a search; the stress movers run parallel from a few thousand on,
    so PERFORMANCE.md's table does not move. Promised is a call that changes the running entity
    itself, its writes landing, each entity visited once and memory valid when a column grows; a
    body that changes other entities directly is still told to queue them, as the behaviors guide
    says. The reviewer takes the form, with the cost measured in nanoseconds an entity before and
    after on a sequential run with a small component and a large one, recorded in the remark; the
    tests as proposed, a behavior playing a graph on its own entity keeping what it writes, one
    adding a component to its own entity through the context keeping its write and visited once with
    the rest, the moved entity not visited again in the frame, and one more where the insert grows
    the column so the row the runner handed out moves, the write landing and nothing read through
    the old address; and the behaviors guide saying what is promised and what is not. One commit,
    carrying Decision 44's sentence. Done at `20f3d5c0` on 2026-10-20, as the head says; the
    sequential walk's cost about three times an entity for the promise, the parallel walk's none.

35. **The build workflow's jobs are named as 3DEngine's are, by their ids, with no expression in a
    name.** The owner wrote on 2026-10-20 that a push run's page shows the pack-only jobs with their
    name templates unexpanded, `capture the examples (${{ matrix.shard }} of 4)` and `native (${{
    matrix.rid }}...)`, since GitHub skips a matrix job before it expands the matrix and prints the
    name as written, where 3DEngine's skipped jobs show plain names because its jobs carry no `name`
    and no matrix. At 07:10 the owner left the shape to the reviewer, for a graph that reads as
    3DEngine's. So: the suite job keeps its matrix, which is never skipped, and names its three
    entries `linux`, `windows` and `macos` through an include and the entry's system in its `name`,
    as 3DEngine's suite jobs are named, since a name's expression expands wherever the job runs;
    every pack-only job drops its `name` and shows its id, `native`, `captures` for the four shards,
    `captures-fedora`, `games`, `drive`, `storm` and `readme`, GitHub appending a matrix entry's
    values where the matrix runs and a skipped job showing its id alone; `digest` shows its id too.
    The matrices stay where they save copies, the native job's seven targets and the four shards,
    since a matrix entry is one node of the graph as a job of its own would be and the steps are one
    text; the suite's steps, which branch on the system five times, stay one text for the same
    reason. The graph then reads as 3DEngine's, the three suites, the digest and the examples on a
    push with the pack-only jobs skipped by id, and the whole set on a pack run. The workflow tests
    follow the ids, and BUILDING.md's job names with them. One commit, after item 34. Done at
    `41ebc700` on 2026-10-20, as the head says. Proven on the build run of `41ebc700` at 11:40 on
    2026-10-20, the page as the item says.

36. **A program reads the force a joint held its bodies with over the last step, from 3DEngine's
    `GetPhysicsJointForce`.** SHARED.md's row, taken on 2026-10-20 since this engine simulates its
    bodies over the same library on the managed side and the flat API reads a contact's impulse and
    no joint's: a bridge game in 3DEngine could not make a weak bridge break where it is weak, since
    the joints' give under load reads alike for a truss and a bare road, where the solver's
    accumulated impulse for the joint's own constraint over a substep is the force in newtons. So
    the physics API gains the read, the impulse magnitude of the joint's constraint over the last
    step, its limit and motor apart, costing nothing until called, with its cheat sheet line, the
    surface listing, the upgrading page's entry and the physics guide's paragraph on joints, and a
    test of a box hanging on a distance joint reading its weight within a percent at the substep
    counts the engine runs. One commit, carrying the review's files. Done at `d56e7cb7` on
    2026-10-20, as the head says.

37. **The feature test moves into the games folder, as 3DEngine's lives (Decision 45).** The owner
    wrote on 2026-10-20 at 23:21 that 3DEngine's feature test under its games folder looks good and
    BevyCSharp is to do the same: `BevyCSharp.FeatureTest` moves to `games/FeatureTest` with its
    history kept by a move git follows, and every place that names the old path follows it, 58 files
    today, the solution, the workflow's drive and storm jobs, the drive, soak, scenes and publish
    scripts, the answer script, the editor's project and its play, the CLI's launch and tools, the
    tests that name it, BUILDING.md, TODO.md, the guides and AGENTS.md's table row. Read first what
    the games' build properties apply to a project under that folder, since the games build on the
    packed package where the feature test tests the tree through a project reference, and say
    whether it keeps the tree's reference by opting out of those properties in its own project file,
    said in its README, or builds on the package as 3DEngine's does with the drive job packing
    first; the first keeps the drive's time and what the test is for. The feature test stays out of
    the README's gallery as before. One commit, the move alone with the references, carrying the
    review's files; the tests that name the path pass after it. Done at `cf70c650` on 2026-10-21, as
    the head says.

38. **An edit mesh a program shapes by operations, with a recipe that rebuilds it, built into what
    the engine draws and collides with (Decision 46).** The owner asked for modeling in both
    engines, Blender's kind, from the command line as well as in a game, grid prototyping first;
    this item is the kernel the rest stands on. An edit mesh is a polygon mesh of vertices, faces as
    loops of vertex indices of any count, a texture coordinate at each face corner, a material slot
    and a smooth flag a face, and edges read from the faces; a selection of vertices, edges or faces
    as sets; and its history as the list of operations that made it, the recipe, so undo drops the
    last line and rebuilds, and a saved model is its recipe, a text file `*.model` of one operation
    a line in the words the commands of item 40 use, read back and rebuilt at load, so a level's
    models are small files a reader understands and a diff reads. The operations of this item: new
    and free; the primitives box, plane, cylinder, cone, wedge, stairs with a count of steps, a
    rise, a run and a width and closed underneath, and arch, each placed by its lowest corner or its
    center on the grid; select all, none, invert, faces by index, faces by the direction their
    normal faces nearest (`+y`, `-x`), vertices and faces inside a box, and the faces sharing a
    material slot; move, rotate about an axis through the selection's center or a given pivot, and
    scale, of the selection, snapped to a grid step the mesh holds (one meter by default, any power
    of two of it down to a sixteenth); extrude faces along their normals by a distance or by a
    vector, the sides filled; inset faces by a distance; delete the selection, the faces left open
    or the hole filled; merge vertices within a distance; flip the selection's normals; triangulate;
    duplicate the selection in place; and join another edit mesh in. The build: faces triangulated
    with ear clipping, so a concave face triangulates right, normals per face or averaged where the
    smooth flag is set and the edge's angle under a threshold, tangents from the texture
    coordinates, one mesh a material slot, through `Modeling.Build`, a mesh handle a material slot
    through `CreateMesh` from a `MeshData`, written again in place through `WriteMesh` after a
    change, so an entity drawn with it follows the edit, and a collider through a
    `ColliderShape.Mesh` over the built mesh. The surface is a static class `Modeling` beside
    `Meshes` and `MeshFiles`, over a sealed class `EditMesh`, each call with its cheat sheet line
    through the script and the surface listing, and a guide page `docs/modeling.md` with a section
    an operation showing the recipe and what it makes. Tests: a test an operation on exact counts of
    vertices, edges and faces, the signed volume of a closed mesh (a two by three by four box 24, a
    stairs the sum of its steps), and watertightness, every edge shared by two faces with opposite
    winding, and a recipe written and read back making the same counts; a capture of each primitive
    in the gallery row. Two commits, the mesh and the primitives with the selection and the
    transforms, then the topology operations, the recipe, the build and the collider. First half
    done at `08d03b4d` on 2026-10-21, the mesh, the primitives, the selection and the transforms
    with the recipe's words in the Reply, as the head says; the second half follows item 46.

39. **Grid prototyping: materials that show a meter, texture coordinates aligned to the world, and
    openings cut by booleans (Decision 46).** The owner's first use: blocking out a level with
    textures of different colors showing a grid of about a meter, stairs, and windows cut by boolean
    operations. The engine makes the grid image, `Images.Grid(int pixelsPerMeter, Color face, Color
    line, Color fine)`, an image of one meter's cell with its edge lines and lines every tenth of a
    meter in a finer tone, 1024 pixels a meter by default with mipmaps, and a prototype material
    from a color, the grid image tinted, in a small set of named colors for floors, walls, ceilings,
    stairs, doors, windows, and a hazard, so a level reads by color as the dev textures of the
    engines of the 2000s read; a face's texture coordinates by world-aligned box mapping, each face
    projected along the axis its normal faces nearest, in meters, so the grid shows one meter
    everywhere and continues across faces, which is the default of every primitive and of every face
    a boolean makes. Booleans: union, difference and intersection of two edit meshes, the second
    placed by a transform, written in the engine as the binary space partition algorithm of csg.js,
    each mesh split by the other's planes, the parts kept by the operation, the triangles then
    merged back into faces where coplanar and sharing edges and the vertices merged, so a window cut
    from a wall is a few faces and not a hundred triangles; coplanar faces of the two meshes handled
    by the algorithm's classification with an epsilon the mesh holds, and the limit said in the
    guide that two faces in one plane are cut as the algorithm cuts them, so an opening is cut with
    a box a little deeper than the wall. The new primitives on it: a room, a box with its inside
    hollowed by a wall thickness, a door and a window, each a cut of a box through a wall selected
    or named, a ramp. Tests: a box cut by a box through its wall has the faces counted, is
    watertight and has the volume of the two less their overlap; a union of two boxes sharing a
    face; the grid image's lines at the right pixels; the box mapping giving a two meter face
    coordinates spanning two; a capture of a room with a window and a door and stairs in it. Two
    commits, the grid image, the materials and the mapping, then the booleans and the openings.

40. **The modeling commands, so a level is blocked out from the terminal, and a window in the game
    to shape by hand (Decision 46).** The owner asked that all of it be possible from the command
    line, simple to use. The engine's catalog gains `model.*` commands, one an operation, each
    taking the model's name first and then the operation's measures in the recipe's words, so a line
    typed is a line of the recipe: `model.box room 8 3 6`, `model.select room +y`, `model.extrude
    room 1`, `model.cut room window 1 1 2 0.5`, `model.material room wall`, `model.undo room`,
    `model.save room resources/models/room.model`, `model.load room <path>`, `model.show room 0 0 0`
    spawning or moving an entity that draws the model and follows its edits, `model.info room`
    answering the counts, the bounds, the volume and the open edges, and `model.list`; a command
    answers the counts after it, so a script reads what it did, and `./bcs command model.<op>`
    reaches each as the catalog does. The window: an ImGui window a program draws with one call,
    `Modeling.DrawWindow(EditMesh)`, and the same window as a panel of the editor, which picks
    entities already, opened on the edit mesh an entity's mesh was built from; it lists the recipe
    with the last lines undone and redone by buttons, shows the selection's counts, offers each
    operation with its measures as fields and a button, and takes the mouse in the 3D view: a click
    selects the face under the pointer by a ray tested against the edit mesh's faces one by one,
    with shift adding and control removing, a drag along the face's normal extrudes or moves the
    selection by the grid step with the distance shown, and the hovered face and the selection drawn
    as outlines over the model, the way the voxel game's aim outlines a block. A guide section on
    blocking out a level from the terminal, as a script of `./bcs command` lines making a room with
    a window. Tests: the commands through the console host making the room of the guide and reading
    its counts, a load after a save giving the same, and the window's picking tested through the ray
    against a known face. Two commits, the commands, then the window.

41. **Texture coordinates shaped and textures given to faces (Decision 46).** The owner asked for UV
    processing and adding textures to surfaces. The selection's texture coordinates by projection,
    planar along an axis or the view, box as item 39's default, cylindrical about an axis, and
    spherical; moved, scaled and rotated as a whole or about their center; set to a texel density,
    so many pixels a meter, read from the material's texture; and reset to the world-aligned
    default. A material slot takes a material of the engine's, a color with a texture and the maps
    the engine's material has, so a face is given a texture by giving its slot one, the built
    model's meshes drawn with each slot's; slots added, renamed and removed, and the faces of a
    removed slot given the first. The texture coordinates of the recipe's operations are recorded as
    operations too, so a saved recipe rebuilds them. A section of the window shows the selection's
    coordinates over the slot's texture. Tests: each projection's coordinates on a box's known face,
    the density on a two meter face with a 512 pixel texture, and a capture of a room with a brick
    wall and a wooden floor from the examples' resources. One commit.

42. **A game that is a level blocked out with the tools, Blockout unless the owner names it
    otherwise (Decision 46).** A game under `games/` built on the package as the others are: a level
    of three rooms, a corridor, stairs between two floors, a window and two doors, made from one
    recipe file of some forty lines in the game's resources and rebuilt at load, in the prototype
    materials; a character walking it through the character controller as Courtyard has, the level
    solid through the built collider; F1 opening the modeling window on the level's edit mesh so the
    level is reshaped while walking in it, the collider rebuilt on a change, and F5 saving the
    recipe; a sun and the global illumination on, so the grid walls show the light. Commands
    `blockout.status` and `blockout.autopilot` for the drive and the soak, the drive walking the
    stairs to the upper floor and reading the character's height, cutting a window by a command and
    reading the counts, and a capture; the game joins the build-all, drive, soak and storm lists and
    the README's gallery with its capture, and the guide's page names it. One commit.

43. **Modeling operations on faces and edges, Blender's kinds (Decision 46).** The owner's later
    wave: selecting a face, insetting and pulling it, merging, beveling. On the kernel: bevel of the
    selected edges by a width with a count of segments, the profile round; loop cut, a cut around a
    ring of faces at a fraction; subdivide the selected faces by a count; bridge two selected face
    loops of the same count; fill a hole of selected edges with one face; dissolve the selected
    faces into one, or the selected edges, keeping the shape; knife, a cut of the selected faces by
    a plane; mirror of the selection across an axis through a pivot, merging on the plane; spin of
    the selected edges about an axis by an angle in steps; and smooth, the vertices moved toward
    their neighbors' mean by a factor. Each in the recipe and the commands, in the window with its
    measures. Tests: each operation's counts on a box, a bevel of a box's twelve edges with two
    segments watertight and the volume between the box's and its inscribed one, a mirror of a half
    giving a whole with no doubled vertices; a capture of a beveled, inset and loop-cut box. Two
    commits, the bevel, the loop cut and the subdivision, then the rest.

44. **Modifiers, a stack applied at the build and never baked until asked (Decision 46).** The owner
    asked for Blender-like modifiers. An edit mesh holds a list of modifiers applied in order at the
    build over the base, which stays editable: mirror across axes, array by a count and an offset or
    along a curve of positions, bevel as item 43's over every edge or the edges past an angle,
    subdivision by Catmull-Clark to a level, solidify by a thickness inward or outward, boolean with
    another edit mesh placed by a transform, and triangulate; each added, reordered, removed and
    applied, applying baking its result into the base and dropping it; in the recipe as `modifier`
    lines and in the commands and the window, the build's time for a stack read in a test against a
    budget. Tests: a mirror on a half box giving a box's counts, an array of three, a subdivision of
    a box to level one giving 26 vertices and level two 98, a solidified plane watertight, and a
    capture of a subdivided and beveled shape. One commit.

45. **What is modeled is exported for Blender and imported from it (Decision 46).** The built mesh
    and its materials written as a glTF 2.0 file, a JSON with its buffer beside or embedded,
    positions, normals, texture coordinates, indices, one primitive a material slot with the slot's
    color and its texture referenced by path, by a writer of the engine's own in managed code, so a
    level blocked out in the game is opened and finished in Blender; and a model file read into an
    edit mesh through the engine's glTF reader, `Read`, triangles merged back into faces where
    coplanar and sharing edges, materials into slots, so a model from Blender is cut and shaped in
    the game. `model.export` and `model.import` beside the save and the load. Tests: a box exported
    and read back with the engine's own reader having the same counts, a file validated against the
    glTF schema's required fields, and a known glTF's import counted. One commit.

46. **An animated capture of a run, frames taken at an interval and written as one file, and one
    made from captures on disk (Decision 48).** The owner asked at about 00:29 on 2026-10-21 for
    captures made into animations from several pictures with a time between them, a second or 250
    ms, as tooling to show things in the documentation and in devlogs. The engine gains a recorder:
    `record <path> <length> [every <step>] [scale <s>]` beside `shot` and `window.shot` in the
    catalog, taking frames from the frame being drawn at a step given in milliseconds or frames
    (`250ms`, `15f`), for a length in seconds or frames or a count of pictures (`5s`, `8`), scaled
    down by a factor where asked, each frame kept in memory as it is taken and the file written when
    the length is reached or `record.stop` is sent, with the frames' own times as the delays, so a
    step of 1 s plays at 1 s and `record ... play <ms>` plays faster or slower; the formats by the
    path's extension, `.gif` by an encoder of the engine's own in managed code, a palette of 256
    colors by median cut over all the frames with ordered dithering, the LZW of the format and a
    loop forever, and `.png` as an animated PNG, the frames as the format's frame chunks over the
    engine's PNG writer, lossless, which GitHub's pages play as they play a GIF; no library is
    taken, the dependency table allowing none for it and, for a program,
    `Capture.StartRecording(path, intervalMs)` and `Capture.StopRecording()` with their cheat sheet
    lines through the script and the surface listing; and a step of its own in the bcs client,
    `./bcs record <path> <length> [--every <step>]`, which sends the command and waits for the file.
    For captures on disk, a script `build/animate.py <out> <pictures...> --delay <ms> [--loop n]
    [--scale s]` assembles PNG or WebP captures into a GIF, an animated PNG or an animated WebP by
    their extension, with Pillow where it is present and the engine's own writers where it is not,
    so a devlog's animation is made from the captures the pack run already takes; the guide's
    section on captures shows both, with one animation of a game in the README's gallery as its
    first use. Tests: a recording of eight frames at 250 ms on the null device whose file parses as
    a GIF with eight frames and the delays right, the same as an animated PNG read back by the
    engine's reader frame by frame, a palette of a known picture within a distance of its colors,
    and the script run over three captures in the suite. Two commits, the recorder with its formats
    and the command, then the client's step and the script. One or two days, before item 38's second
    commit.

## Verdicts

Verdicts 3 and 14 to 32 are settled: 15, the native job's four targets, on the pack run of
`292eead2` on 2026-10-16, all six built after winit became a dependency on every system at that
commit; 14, the memory guard's figure on Windows, on the push of `d2a20b6a` on 2026-10-17, the third
green Windows suite in a row after the private bytes were read at `292eead2`; and on the pack run of
`d2a20b6a` the same day, 3, Courtyard played from the package with its step's page saying so, 16,
the storm's test loading the bridge the job downloaded, which the step named, 17, the pack job
passing with the notes beside the artifacts, and 18, Swarm played into its third wave within its
commands' patience, and 19, the capture job ending within its hour with a page naming each failure,
the examples started and none waited on; and 21, the storm's app made on the runner once the three
libraries winit opens were installed, on the pack run of `5e01491e` on 2026-10-17, the storm passing
in a minute and a half; and 20, the feature test run through its course on lavapipe in 56 minutes,
in a job of its own on the same pack run; 23, the session file's move on Windows, on the third green
Windows suite in a row after `0dff9601`, the push of `85df443c` on 2026-10-17; and 24, the soak, on
the pack run of `b178ae3f` the same day, the play job passing with its ten-minute soak of three
games; and 22, Bevy's files fetched before the examples are built, and 25, the capture's budget and
its four shards, on the pack run of `afcf8301` the same day, the four shards capturing every example
with none blank and ending in 19 to 28 minutes of their 60, each comparing its own, two of them
failing on one picture apiece, which is Verdict 26's; and 27, the memory guard's figure on macOS, on
the push of `b4d45938` on 2026-10-17, the third green macOS suite in a row after `cd2d542d` settled
the heap before the test's first reading and forbade a collection until its second; and 28, the
soak's heap figure, on the pack run of `b4d45938` on 2026-10-18, the play job's ten-minute soak of
three games passing with the live bytes judged; and 26, the captures apart from the checked-in
pictures, on the same pack run, the four shards passing with the two pictures taken again on a
`--render` bridge; and 29, the lost-suite test's Windows figure, on the push of `3d147d49` on
2026-10-18, the third green Windows suite in a row after `b50890cb` held the hang case to a
terabyte, said the reading beside the limit and took a process as a child only where it was not
created before its parent; and 30, the visibility test on a headless build, on the same push, the
three suites passing with the test asking the three ids on every build and the remarks saying every
build has the component; and 31, the capture of `async_compute` on the machine's clock, on the pack
run of `6bac018b` on 2026-10-18, the four shards passing with the example listed beside its sibling
at a room measured on the runner and in a container of its image; and 32, the system information
test's process measures on macOS, held at zero where Bevy's App Store build of sysinfo reads no
process, on the macOS suites of the push run and the pack run of `35ca43b1` and the push run of
`3a2e403a` on 2026-10-19, three green in a row.

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

33. **The command line server's held-clock test fails on the Linux runner with a frame it never
    read.** On the push run of `97ea3055`, 2026-10-19, the Linux suite failed
    `CliServerTests.AClockHeldBetweenCommandsMovesOnlyWhileOneWaits` at its line 297 with a key of
    15 not present in its dictionary, 1,246 passed and 556 skipped in 4 minutes 30 seconds, the
    macOS and Windows suites of the same commit green; the test came with part (f) at `714451b0`,
    whose push run passed it on the three systems, so the fault shows on one runner of two. The test
    reads a frame by its number from what the server answered and the frame it asks for was not
    among them, which a wait that ended on another frame, a reading taken before the server's answer
    or the clock's frames counted apart from the frame count would each give. The mend is read
    before it is chosen: the test's dictionary and what fills it, which frame 15 is and why it was
    absent, with the run's output if the artifact holds it; then the test made to hold what the hold
    promises without reading a frame that may not come, or the server mended where it answers a
    frame it did not run. Mended at `8e9c20b7` the same day, the test reading its times after the
    app has stopped, the server unchanged, as the head says. Settled on the Linux suites of the push
    run of `e7ea1a06` and the push run and the pack run of `3eaa0a44`, 2026-10-19 and 2026-10-20,
    three green in a row after the mend.

34. **The release notes tests cannot delete their scratch repositories on Windows, since git writes
    its objects read-only.** On the pack run of `987676b5` and the push run of `e7ea1a06`,
    2026-10-19, the Windows suite failed both release notes tests in their disposal, an access
    denied on an object folder of the scratch repository with 17 files left in it and no process
    holding one (`TestFolder.cs:73`, `ReleaseNotesTests.cs:23`), 1,067 passed and 560 skipped, the
    Linux and macOS suites passing them; the tests came with item 32 at `d482caa3`, run on Linux
    alone before the push. Git writes its loose objects read-only, and the folder's deletion on
    Windows refuses a read-only file where Linux and macOS do not, so a test folder that holds a
    repository cannot be swept there. The mend is one thing: the test folder's deletion clears the
    read-only attribute of what it sweeps before deleting, with the reason in its remark, or the
    release notes tests sweep their repositories through git, whichever the other tests that make
    repositories, if any, already do. Mended at `7a0f07ea` the same day, the test folder's sweep
    clearing the read-only attribute on a refusal. Settled at three green Windows suites in a row
    after the mend. The push run and the pack run of `3eaa0a44` and the push run of `9b6dceaa`
    passed the Windows suite on 2026-10-20, the third at 03:55, three green in a row. Settled on the
    runs of `3eaa0a44` and `9b6dceaa` on 2026-10-20.

35. **The load hold test's image never loaded on the macOS runner.** On the pack run of `987676b5`,
    2026-10-19, the macOS suite failed `ClockHoldTests.AClockHeldForLoadsStandsStillUntilTheyEnd` at
    its line 42, the image never loaded, 1,061 passed and 567 skipped in 5 minutes 44 seconds, the
    Linux and Windows suites passing it; the test came with part (f) at `714451b0`, whose push run
    passed it on the three systems, so it shows on one runner of two as Verdict 33's did. The mend
    is read before it is chosen: how long the test waits for the load and in what, frames or the
    machine's time, and whether a held clock can hold what the load itself waits on, since a load
    that waits on a frame's step while the step is held would never end; then the test given the
    patience a slow runner needs in the machine's time, or the hold mended where it holds the load,
    whichever the reading supports, with the reason in the remark. Read and mended at `17487d27` the
    same day: the held clock holds nothing the load waits on, the test had given the load a few
    milliseconds unpaced, and it paces its frames at sixty a second up to six hundred. Settled at
    three green macOS suites in a row after the mend. The push run and the pack run of `3eaa0a44`
    and the push run of `9b6dceaa` passed the macOS suite on 2026-10-20, the third at 03:55, three
    green in a row. Settled on the runs of `3eaa0a44` and `9b6dceaa` on 2026-10-20.

36. **The pack job waits 20 minutes for nuget.org to list the package it pushed, and the feed took
    longer.** On the pack run of `3eaa0a44`, 2026-10-20, the pack job logged in and pushed `0.5.149`
    at 02:00, and its last step, which builds each game on its own default from nuget.org, waited 20
    minutes for the feed to list the package and failed at 02:20 saying so, while the feed listed
    the package by 02:47, the templates package of the same push listed within the twenty; so the
    run ends red with the package published and the games unbuilt on it, and the run's page says a
    step failed where the feed was slow. The mend is one thing, under N 3.5: the wait set from what
    the feed takes, read from this run and the next, with the job's own limit raised to hold it, or
    the games' build on their defaults moved out of the pack job into a step that runs on the next
    build run, where the package is listed by then, said in the workflow's comment; and the step
    says, when it gives up, that the package was pushed and stands, so a red run is not read as an
    unpublished package. An aside kept here: the pack step warns that a stable package should not
    take a prerelease dependency, the physics library's, which the owner decides on. Settled when
    the pack job passes whole on the next pack run with publish on. The coder's reading on
    2026-10-20 is the first form, the wait set from what the feed took, the templates listed within
    20 minutes and the engine by 47, so a wait of 45 minutes with the job's limit raised to hold it,
    and the summary moved before the games' step, so a run says the push stood even where the wait
    gives up; the reviewer takes it. Mended at `d0e58c56` on 2026-10-20 with a wait of ninety
    minutes, near twice the 47 measured, the coder's figure where the reviewer's 45 would have given
    up two minutes before the one listing read, and the job's limit at 120; the pack run with
    publish on started on it is the second reading of the feed's time, and its pack job passing
    whole settles the verdict. A reading of the feed's time from the other engine: its `5.2.155`,
    pushed at 08:38 on 2026-10-20, was listed at 08:48, ten minutes. On the pack run of `3b0e54d7`
    on 2026-10-20 the package was pushed at 11:48 and the feed listed `0.5.174` and the templates
    within 24 minutes, inside the ninety, the summary standing before the games' step on the page;
    the games' step then failed on a game's own default, Verdict 40, not on the wait. Settled on the
    pack run of `3b0e54d7` on 2026-10-20.

37. **The Fedora capture job compares its six pictures on the runner, which has no decoder for
    them.** On the pack run of `d0e58c56`, 2026-10-20 at 05:07, the job's compare step failed at its
    first picture, `clustered_decals.webp`, which the compare script said could not be decoded; the
    script decodes through ImageMagick or libwebp's tool, and the shards' job installs both on the
    runner where this job installs them in the container alone, so on the runner the script found
    neither and read nothing. The six pictures the container wrote are whole, lossy WebP of the same
    encoding as the checked-in ones, 1280 by 720 and 1920 by 1080, read from the uploaded artifact.
    The mend is one thing, under N 3.5: the job installs libwebp's tools and ImageMagick on the
    runner before the compare, as the shards' job does, with the reason in its comment, or compares
    inside the container where they are; and the compare script, where it finds no decoder, says so
    by name in place of saying the picture could not be decoded, so the next such page names the
    fault. Settled when the job passes on the next pack run. Mended at `6fcecb1a` on 2026-10-20, as
    the head says. The job passed on the pack run of `3b0e54d7` on 2026-10-20 at 11:17, its six
    compared on the runner. Settled on the pack run of `3b0e54d7` on 2026-10-20.

38. **`many_foxes` differed in 6.7 percent of its pixels on a capture shard, the foxes a step along
    in their run.** On the pack run of `d0e58c56`, 2026-10-20 at 05:00, the first shard's compare
    failed on `many_foxes` alone, 6.7 percent of its pixels past the 2 allowed within x 24 to 1,894
    and y 132 to 899, the whole field of foxes; the uploaded capture against the checked-in picture
    shows the same scene, the same colors and the same shadows, each fox a little further along its
    gallop, so the frame's animation time was not the picture's, where the shards passed the example
    on every run before and the three other shards passed theirs on this one. The capture's frame is
    counted on the set clock with the clock held while a load runs (`714451b0`), so the reading
    comes first: whether the foxes' animation players start on the frame their asset arrives and
    count from a time the hold does not cover, or whether the run had a load the hold did not catch;
    then the mend where it points, the players' start tied to the held clock or the example's
    animation time set at the capture's frame, with the reason in the remark, one commit. Read by
    05:48 on 2026-10-20 on the development machine with logging in a scratch copy of the example,
    two faults in the bridge's animator and neither in the example. The first play on a scene loads
    the file again to read the clips' names, since Bevy freed the file's root asset once the scene
    had loaded, and that load is not among the handles the load hold watches, so the frames it takes
    pass on the clock; on the GPU all 1,000 plays failed in the first frame and succeeded in the
    second, and on a device whose load lands a frame sooner or later every fox starts a frame sooner
    or later. And a play inserts the animator's source and then its animator on the scene root,
    every call after taking the animator off and putting it back, table components all, so each call
    moves the root to another table and the old table's last row into its place while the behavior
    that called it holds its own row by reference, and its started flag lands in another fox's row:
    in the second frame 1,000 plays returned true and 501 foxes still read not started in the third
    and played again, 499 twice and one three times, each replay resetting the clip a frame later,
    the half following the table's order and so moving with the frame the file arrives in. The mend,
    one commit: the two animator components sparse, so the root changes archetype but keeps its row
    and the behavior's write lands on its own; the clock hold keeping the ids of loads the bridge
    starts for the app while the hold is on, holding the clock while any of them loads and dropping
    each once it stops; two engine tests failing before the mend, several scene roots whose behavior
    plays a clip each playing once and keeping what they wrote, and no time passing under a held
    clock between a scene's first play and the one that succeeds; and the pictures of the examples
    that play clips taken again, `many_foxes`, `animated_mesh`, `animated_mesh_control`,
    `morph_targets`, `many_morph_targets`, `mirror` and `scene_viewer`, since their start frames can
    move. The row's move for Bevy's own animation types is item 34. Mended at `05ed21e4` on
    2026-10-20 as the head says, the first shard of the pack run started on it the first of three.
    Settled at three green first shards in a row after the mend. The first shard of the pack run of
    `05ed21e4` passed at 07:40 on 2026-10-20 with the example matching, the first of three. The
    first shard of the pack run of `3b0e54d7` passed at 11:19 on 2026-10-20, the second of three.

39. **`meshlet`'s colored bunnies differ in their patches between the Fedora job's capture and the
    picture.** On the pack run of `05ed21e4`, 2026-10-20 at 07:39, the Fedora job's compare, running
    for the first time with its decoders in place, found `meshlet` differing in 10.9 percent of its
    pixels past the 2 allowed, within x 126 to 616 and y 192 to 607, which is the row of bunnies
    drawn with a color a meshlet, while the plain bunnies on the right, the floor and the shadows
    match; the uploaded capture against the picture shows the same bunnies in the same places with
    their patches cut otherwise, larger and elsewhere, so the meshlets the bridge built from the
    mesh, or the level of detail it chose, differ between the bridge the runner built and the one
    the picture was taken with on the development machine, where the coder's own container run
    matched the picture to no pixel. The reading comes first: the meshlet count and the level drawn
    on the runner against the machine, whether the builder's partition is deterministic across
    builds of the bridge and its dependencies, and whether the runner's four cores or its Mesa pick
    another level; then the mend where it points, a deterministic partition if the builder allows
    one, the example's debug colors keyed to something stable, or, if the partition is not the
    bridge's to fix, the colored bunnies' region left out of the compare with the reason in the
    differ file, which would be its one line. One commit. Settled when the Fedora job passes on the
    next pack run. Read so far by 08:27 on 2026-10-20: the partition is not the bridge's, since the
    example loads Bevy's prebuilt meshlet bunny, pinned by the assets file to one commit, so both
    machines read the same clusters, and the colors are seeded from the instance and meshlet ids,
    both stable; the runner's picture shows the near cluster-colored bunny a whole level coarser,
    larger clusters, other colors, no holes. Two things can choose the level, the
    imperceptible-error formula with its one-pixel threshold, and the cull pass's append into the
    raster clusters' list, which has no bound check, software clusters filling from the left and
    hardware ones from the right, so with the 16,384 budget overrun they overwrite each other in an
    order the atomics set, which the thread count changes; the coder captures the example in Fedora
    44 with the runner's own bridge, once pinned to four cpus and once on all 32, to tell them
    apart. Read further by 08:56: it is neither the thread count nor the runner's build. In Fedora
    44 on Mesa 26.2.4 compared with no allowance, the runner's own bridge from the pack run on all
    32 cores gives the runner's picture to no pixel, as does the coder's bridge at `05ed21e4`; the
    bridge of `5195e95a`, before the anisotropy mend, gives the checked-in picture to no pixel; and
    the head's bridge with the mend gated off by a scratch variable gives the checked-in picture and
    with it on the runner's, each to no pixel. So the engine draws the example the same on both
    machines and both builds, and the picture went stale at `0f8e89b4`, which retook its anisotropy
    pictures on the GPU and never ran the six in Fedora, where the compare could not see it until
    Verdict 37's decoders. What moved is the color of nearly every cluster with the outlines
    coinciding to 89 to 92 percent within a pixel, the same clusters under another seed of the
    instance and meshlet ids, not a coarser level as it first looked. The coder splits the mend's
    two effects next, its resource and system present with the shader not rewritten against the full
    rewrite, to say whether the seed moved with the extra resource entity Bevy spawns or with the
    shader's recompiled pipelines. The reviewer's direction: where the seed moved with an entity's
    id, the example keys its colors to the meshlet's id and the instance's place in the example's
    own list, so the picture holds when an entity is spawned before it, with the reason in the
    example's comment, and the picture is taken again in Fedora once; where it moved with the
    pipelines alone, the picture is taken again with the reason in the commit; `test_meshlet` and
    `many_meshlet_materials` checked against theirs the same way, which matched on the runner. One
    commit. Mended at `3b0e54d7` on 2026-10-20, as the head says, the seed the example's own; the
    pack run started on it is the check. The Fedora job passed on the pack run of `3b0e54d7` on
    2026-10-20 at 11:17 with the seeded picture. Settled on the pack run of `3b0e54d7` on
    2026-10-20.

40. **Swarm's project names `0.5.0` as its default engine version, which the feed never had, so the
    games built on their defaults fail on it after every publish.** On the pack run of `3b0e54d7`,
    2026-10-20 at 12:12, the pack job's last step found `0.5.174` listed and built each game on the
    default its project names, and Swarm's, `0.5.0` since `5e750133`, does not build, nuget.org
    holding no such version, where the other three games name `0.4.129`; the step's own message says
    to raise each to the version the run pushed. So the four games' defaults go to `0.5.174` in one
    commit, with the step's script run on the machine against the feed as the check, as its message
    asks, and item 22's check of Swarm read again against the feed and not a local pack, so a
    default names a published version from here; the step stays as it is, since it found the fault.
    Settled when the step passes whole on the next pack run with publish on. Mended at `2949041f` on
    2026-10-20, as the head says, the script's check passing on the machine against the feed.

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

26. **An insert from nothing over a resource a plugin made, of a type that reflects no default, is
    refused and names `Resource<T>`.** Read by review on 2026-10-11 from the working session's
    question at `68aa5dd5`, where the debug options inserted from nothing came zeroed over Bevy's:
    the field-by-field build stays for a component and for a resource the world lacks, since every
    example sets fields after it, and only the replace of a living resource with zeros is refused, a
    zeroed copy over the plugin's being never meant. The owner may overrule it as any decision here.

27. **The README's gallery is one scrolling row of pictures a group, in Bevy's order of its
    groups.** The owner asked at 15:36 on 2026-10-11, after asking the same of 3DEngine's README,
    which is item 13: the written examples' pictures in one-row tables that scroll sideways rather
    than the grid of four, one a group of Bevy's examples.

28. **The bcs skill leaves `.claude` for `.github/BCS.md`, and the gallery's pictures are one
    size.** The owner asked at 15:50 and 15:52 on 2026-10-11, as of 3DEngine: the skill document out
    of the `.claude` folder and its empty subfolders, which is item 14 with AGENTS.md's line changed
    on that word; and every picture in the README's rows the same size, which item 13 takes.

29. **The development machine's GPU is named nowhere a user reads.** The owner asked at 17:26 on
    2026-10-11, as of 3DEngine, which is item 15: the machine's name out of the code and comments a
    user sees, the measurements kept with the device a plain phrase.

30. **The examples' counts stand under the Examples heading and on each group's heading.** The owner
    asked at 19:35 on 2026-10-11, which is item 17: the line of how many of Bevy's examples are
    written moved under the Examples heading, and each group's heading in the gallery carrying its
    count.

31. **A vertex stream of the game's own, in both engines.** The owner put a game built on 3DEngine's
    3.1.0 package in its `.ref` at 20:05 on 2026-10-11, which fed its shader one packed float a
    vertex and unpacked it there, asked whether such a stream belongs in a game engine and how both
    would do it, and approved the plan at 20:40, which is item 18 here and item 20 in 3DEngine: one
    stream a mesh in five formats, at `TEXCOORD8` and up here beside Bevy's positions and as
    `CUSTOM0` there, beside the vertex and then as the vertex alone.

32. **A position is every mesh's, and the packed data a stream beside it.** The owner refined
    Decision 31 at 22:55 on 2026-10-11, approved at 22:58: a position is mandatory in both engines,
    as Bevy has it, since shadows, motion vectors and everything around a mesh read it, and a game's
    packed four bytes ride in a stream beside it, which is what item 18 gave; 3DEngine's second
    stage becomes a vertex of positions alone, and here the worked-out normals can be left out, item
    20.

33. **No development on either engine until every job is green on every push and pack.** On
    2026-10-16 the owner, after the pushes and pack runs of both engines failed in a row each on a
    different job while the code stood still, stopped development on both: the working sessions mend
    what the verdicts name and nothing else, every push must be green on every job, three in a row,
    and the pack run after them, before an item is taken again, on 3DEngine as here (its Decision
    39). Lifted for this engine on 2026-10-18 under Decision 35, the push run and the pack run of
    `b4d45938` whole green under the two workflows; 3DEngine's Decision 39 stands until its own
    gate.

34. **Two workflows an engine, build and pack, the same shape in both; the tester zips and the scene
    packs are runs on the owner's machine.** The owner found on 2026-10-17 that the two engines'
    workflow sets differed, five files here and four in 3DEngine with graphs that looked unlike each
    other, and ordered each engine down to two: a build workflow on an ordinary push and a pack
    workflow by hand, consistent between them, the pack run keeping every check that ran only there;
    the feature test's zips and the Sponza run leave CI for the owner's machine, the Sponza files
    under `.ref/Sponza`. Written as item 23 here and item 26 in 3DEngine (its Decision 40). The
    norm's "the workflow" names both, and a rule the one it means.

35. **Decision 33's gate is the new workflows.** The owner chose on 2026-10-17 that the rework of
    item 23 starts that day as the first work on both engines, after the verdicts open are mended,
    and that the green lights Decision 33 asks are then asked of the new workflows: a green push and
    a green pack run under them on each engine before features resume (3DEngine's Decision 42). Met
    here on 2026-10-18, the push run of `b4d45938` whole green at 23:38 the day before and its pack
    run whole green at 01:29 with the package made, so Decision 33 is lifted for this engine and
    development resumes with item 22 and the owner's items, the verdicts open mended as they come.

36. **The docs job goes; the repository's links are the suite's to check.** The owner asked on
    2026-10-17 whether the build workflow needs a job that follows the README's and the guide's
    links and found it unnecessary, so the job and its requests to outside addresses leave the
    workflow. N 4.3 stands: a test of the suite follows every link into the repository against the
    checkout with no request made, as 3DEngine's `DocumentLinkTests` does, and a link to an outside
    address is followed by hand with `build/check-docs.py --external` when a package is made.
    Written as item 24.

37. **Prose is technical and generic.** The owner said on 2026-10-17 that phrasings such as "the
    light that bounces" and "how to drive the game" read as casual and do not fit. A feature is
    named by its technical term, the engine's own where it has one, and never by a figure of speech
    or a paraphrase: global illumination where the light bounced; emissive where a block gave off
    light; a running program controlled or scripted through the command line where it was driven,
    and a scripted run or a course where a drive. The owner added at 14:37 that the colon is
    overused, an elaboration or a list appended to a sentence after one where a sentence would do,
    in documents, comments and logs alike, which STYLE.md should have covered; its rule tightens so
    that a running sentence holds no colon. STYLE.md carries both rules with these pairs in each
    repository, every document, comment and message a reader sees is swept (item 25 here, 29 in
    3DEngine, 4 in Voxel-Sandbox), and an identifier keeps its name unless the rename is cheap and
    the thing is new; a new name follows the rule. On 2026-10-17 at 20:34 the owner read the voxel
    game's README's opening, "A voxel game in the manner of Minecraft, and the ground floor of a
    larger one", and asked that no phrasing of the kind stand either. A likeness is not a
    description and a figure of speech is not a term, so a thing is said by what it is and does,
    another product is named only where a rule or a number is taken from it, and a plan is not
    written into a description. The idioms the three repositories hold go with it, in the manner of,
    the ground floor of, out of the box, under the hood, at a glance, a handful of, in the wild, a
    great deal of, the knob, the plumbing and a long way among them, items 25 here, 29 in 3DEngine
    and 5 in Voxel-Sandbox; a word in its literal sense stays, a box a joint leaves or a road to a
    signpost.

38. **Development resumes, the bridge completed first and the mends of the moment made lasting
    after.** On 2026-10-18 at 22:25, with every run green and the pack run of `6bac018b` whole green
    with its package, the owner resumed development on both engines and the voxel game. For this
    engine the order is to bridge everything first, every gap of item 4, and then to mend
    fundamentally what was mended for the time being (item 29). The gaps that need a crate still
    wait on the owner's word typed in the working session (Decisions 11, 12 and 20); the verdicts
    open are mended as they come.

39. **The owner's words of 2026-10-19 on the crates, the order and the mends of the moment.** Typed
    in the working session as AGENTS.md asks: the crates for the camera controllers, free, pan and
    the three pan-orbit, Feathers, the dev tools and the system information may be added; https
    asset loading and the compressed image saver are declined, their rows missing as declined; item
    4 comes first and item 29 after in the reviewer's order, as Decision 38 has it; `dfg_lut` goes
    on in the render profile; the capture jobs move to a container of a distribution image with a
    newer Mesa and a Linux bridge built with meshlets and Solari, with no runner with a graphics
    card and no Mesa from an archive outside the distribution; the anisotropy fault is mended by a
    patched shader of Bevy's in the bridge, held by a test that fails when Bevy's original changes;
    a publishing pack run pushes the templates package beside the engine's, the README's install
    line following once nuget.org lists it (item 30); and `monitor_info` is captured on virtual
    monitors in a headless display.

40. **The README's centered parts are markdown inside a centering block, since nuget.org draws a
    README without its HTML.** The owner wrote at 20:47 on 2026-10-19, having uploaded the packages
    to nuget.org, that the README's centered elements need not be HTML: a centering block, `<div
    align="center">`, holding plain markdown, the heading, the text and the banners, with a blank
    line after the opening tag and before the closing one so the markdown inside is rendered,
    centers on GitHub, and on nuget.org, which draws a package's README without its HTML and shows
    such blocks as their source, the content stands as markdown; the tables of pictures stay HTML,
    since a markdown table cannot size its pictures, unless the script that writes them makes
    thumbnails at the shown width, which is measured on GitHub and nuget.org before it is taken. The
    README is the package's, so what nuget.org draws is what a reader of the package sees.

41. **A package's release notes are the commits since the package last published on nuget.org.** The
    owner wrote at 22:45 on 2026-10-19 that the release notes include everything and the next
    package appends them to the older ones: the notes a pack run writes are the commits since
    `build/version.txt` last changed, so every package of a minor carries the whole minor's notes,
    104 lines at `0.5.105` and 119 at `0.5.120`, 89 at the engine's `5.2.92`. So a package's release
    notes are the commits since the package last published on nuget.org, whose commit the feed's own
    record of the latest version names, read by the pack run from the feed, and the whole minor's
    where the feed answers nothing or lists no version of the minor, a reader of the notes being
    told what changed since what they could have had.

42. **The pack workflow publishes through nuget.org's trusted publishing.** On 2026-10-19 at 22:51
    the owner, making nuget.org's trusted publisher policies for the two repositories, quoted the
    feed's notice that API keys are strongly discouraged for automated publishing and trusted
    publishing is the way for GitHub Actions, and so the pack workflow publishes through it: the
    policy on nuget.org names the package owner `EggyStudio`, the repository owner `EggyStudio`, the
    repository, the workflow file `pack.yml`, no environment, the push scope and the package
    pattern, and the pack job proves it is that workflow with its identity token, takes a
    short-lived key from the feed's login action and pushes with it, so no key is stored in the
    repository. The two policies stood active on nuget.org at 22:53, one a repository, each with the
    package owner and repository owner `EggyStudio`, the workflow `pack.yml`, the push scope and its
    pattern, the feed holding the permanent GitHub ids of the owner and the repository for its
    check.

43. **The reviewer starts the pack runs when a package is due.** The owner wrote at 22:57 on
    2026-10-19 that the reviewer is to start the pack workflow as well, so no one waits on the owner
    for a package and a pack can be made and checked as part of the review; the owner's GitHub login
    on the machine carries the workflow scope, and the pushes were the reviewer's since 22:20. So
    the reviewer starts a pack run with publish on when a package is due: after a commit the voxel
    game or the other engine waits on, after the mend of a fault that made a pack run red, at the
    end of a day's batch, and when the owner asks, and not after every commit, since a pack run is
    more than an hour of the runners' time; the run is read and recorded as every run is, and its
    package is what the game and the other engine take.

44. **The review's files are committed by the batch that comes next, and by the reviewer at most
    once a day.** The owner wrote on 2026-10-20 that the review's commits had become nine of every
    ten in the history and were to stop, and then that the reviewer is to edit the review's files in
    the tree and commit nothing, as it did before 2026-10-19, with one commit a day at most where
    that makes sense. So REVIEW.md, SHARED.md and NORM.md stay edited in the tree and the coder's
    next batch carries them, as AGENTS.md has it; the reviewer commits them in a commit of their own
    only at a day's end where no batch carried them, never for a reading, a run or a package. The
    coder adds one sentence saying so to COMMITS.md in the next batch, in the paragraph on what a
    commit carries, naming the review's files, the batch that comes next and the one commit a day at
    most, and no one who decided it (N 4.7).

45. **The feature test lives in the games folder, as 3DEngine's does.** The owner wrote on
    2026-10-20 at 23:21, seeing 3DEngine's feature test under its games folder, that it looks good
    and BevyCSharp is to do the same; so `BevyCSharp.FeatureTest` becomes `games/FeatureTest`, item
    37, the two engines' trees reading alike, a program beside the games that is not a game to show.

46. **Modeling in both engines, from the kernel to the modifiers, grid prototyping first.** The
    owner wrote at about 23:47 on 2026-10-20 that the engines are to support modeling too, beyond
    the primitives they have, Blender's kind, possible in a game and all of it from the command
    line, simple to use so it is the more powerful: grid prototyping as the essential tool of world
    design, with textures of different colors showing a grid of about a meter, stairs, and windows
    through boolean operations; processing texture coordinates and adding textures to surfaces; and
    later selecting a face, insetting and pulling it, merging, beveling and Blender-like modifiers;
    the reviewer to shape the best solution, adding and removing as it sees fit, and to ask what
    needs asking. The reviewer's shape, the same in both engines by SHARED.md's rows and each
    engine's own underneath: items 38 to 45, an edit mesh of faces of any count with a selection and
    a recipe of operations that is its history, its undo and its saved file, built into the engine's
    meshes and colliders; the grid materials, world-aligned texture coordinates and booleans by
    binary space partition, written in the engine since the dependency table takes no library for
    it; the commands, one an operation in the recipe's words, and an ImGui window a program draws
    with face picking by ray and a drag to extrude, no editor application in 3DEngine and the
    editor's panel in BevyCSharp; texture coordinate projections and materials by slot; a game
    blocking out a level from one recipe; the face and edge operations; a modifier stack applied at
    the build; and glTF written by the engine and read into an edit mesh, so Blender opens what the
    game made and the game cuts what Blender made. Each engine's coder takes the items after its
    current work, item 58 and the feature test's sixth part in 3DEngine and item 37 in BevyCSharp,
    in the items' order, the two in parallel to one text, the words of the recipe and the commands
    the same in both. The owner is asked the game's name, Blockout failing a word, whether the glTF
    export comes earlier than last, and whether a dependency for robust booleans is wanted later
    should the partition's limits show.

47. **The Actions bill is not weighed.** The owner wrote at 00:06 on 2026-10-21 that the bill need
    not be considered, the runs being free of charge for them. So a push's build run is left to run
    beside a pack run started at the same time and is never cancelled for its cost, macOS stays in
    every push, the three systems' suites run on every push as they do, and the reviewer's reading
    of the month's minutes stands as a reading and not as a reason; a run is still cancelled where
    it is doomed, so its figures are not waited for, and a push still waits for the run before it to
    end, so no figure is lost.

48. **Animated captures as tooling for documentation and devlogs.** The owner wrote at about 00:29
    on 2026-10-21 that it is to be possible to capture, or to make animations from several captures,
    with timings such as a second or 250 ms between the pictures, to have more tooling to show
    things in the documentation or in devlogs. So item 46: a recorder in the engine taking frames at
    a step for a length and writing a GIF or an animated PNG by encoders of the engine's own, a
    client step for it, and a script assembling an animation from captures on disk, with one
    animation in the README as its first use; the same in both engines.

## Replies

Shared: a set clock held still while an asset the app asked for is loading and between the commands
of a session served to `bcs`, in the commit carrying this line, for the rows of the set clock and of
a capture taken at a frame. The bridge steps Bevy's `ManualDuration` by nothing for the length of
its time system alone, while an asset C# holds is loading by Bevy's recursive state or while the
server holds no answer, and a wait, `input.hold` and `shot <path> <frame>` count the frames the
clock moved in (`Time.ClockFrames`, a frame whose real step is more than nothing). `bcs open --hold
loads,commands`, `BCS_HOLD_CLOCK` and `app.hold` ask for it, the capture holds for loads and the
drive between commands, whose 25 readings came out the same on a graphics card and on lavapipe, for
3DEngine to consider where its captures and its scripted runs wait on a load or on a script's pace.

Shared: the examples the runner's Mesa cannot draw captured in a container of a distribution image
whose own Mesa draws them, in the commit carrying this line, for the row of the captures compared
across devices. A fifth capture job runs `clustered_decals` and the five drawn with meshlets and
Solari in Fedora 44 on its Mesa 26.2.4, which the workflow names and the job holds Fedora's to, with
a Linux bridge the native job builds with `--meshlet --solari` under a name the pack does not take
(`build/capture-in-fedora.sh`), and compares them, while the four shards stay on Ubuntu 24.04's Mesa
25.2, which draws some scenes two to seven times faster than Fedora's, measured on the same four
cores, for 3DEngine to consider where its runner's Mesa leaves examples uncompared.

Shared: a mesh replaced in place keeps its indirect light under Solari and the light probes, read
for the row of a mesh that replaces one in place, with no fault found and nothing changed. A wall
whose red glow Solari's rays carry to a floor was swapped in one frame for a new mesh asset of the
same shape, and the frame of the swap, the next and one sixty frames on were taken against the frame
before, the mean red of a patch of floor by the wall read 170.4 before, 172.2 in the frame of the
swap and the next and 170.9 after, where the same run with nothing swapped read 170.3, 172.0, 172.2
and 171.2, two runs of each within 0.4. Bevy 0.20's Solari builds the acceleration structure of
every added or changed mesh in the frame it is extracted (`prepare_raytracing_blas`), holding only
the compaction after to 400,000 vertices a frame, which changes nothing drawn. Bevy's environment
maps and irradiance volumes are textures no mesh feeds; the bridge's live capturing probe, which
captures its cubemap from the scene each frame, reflected a swapped red block on a mirror ball with
no pixel of 9,216 changed in the frame of the swap or after. So BevyCSharp's cell is "has, by Bevy's
design".

Shared: the force a joint held its bodies with over the last step, in the commit carrying this line,
for the row 3DEngine took at `fdcfd435`. `PhysicsWorld.JointForce` reads the impulse Bepu kept for
the joint's own constraint over the step's length, the first of the joint's constraints, so a
hinge's motor and limit, a ball joint's cone and a slider's lock, travel and drive are left out, and
answers zero for a joint that is gone, a rope with slack, or before the first step. This engine's world solves a
step in one substep, so the test reads at that count alone, a rope holding a three kilogram box at
29.416 against its weight of 29.43. This engine has no upgrading page, so the change has the guide's
paragraph, the cheat sheet's line and the surface listing without one.

Shared: the edit mesh with its primitives, its selection and what moves it, in the commit carrying
this line, item 38's first half, for the first row of Modeling, and the recipe's words for items 38
to 45, which 3DEngine's items 59 to 66 take, the operations and their order the same so a recipe
written in one engine builds the same mesh in the other, its faces numbered alike. A recipe is
text, one operation a line, its words apart by spaces, lengths in meters, angles in degrees, a
number written as `float.ToString(CultureInfo.InvariantCulture)` writes it, `#` to the end of a
line a comment, a blank line skipped, and `model 1` its first line, the form and its version. A
command is `model.<verb> <name> <the line's words after the verb>`, so `model.box room 8 3 6` is the
line `box 8 3 6` run on the model called room, and the commands outside the recipe are `model.new`,
`model.free`, `model.list`, `model.info`, `model.undo`, `model.redo`, `model.save <path>`,
`model.load <path>`, `model.recipe`, `model.show <x> <y> <z>`, `model.export <path>` and
`model.import <path>`. The lines of item 38:

```
box X Y Z                 plane X Z                 wedge X Y Z
cylinder RADIUS HEIGHT SIDES                        cone RADIUS HEIGHT SIDES
stairs STEPS RISE RUN WIDTH                         arch RADIUS THICKNESS DEPTH SIDES
  each followed by [at X Y Z] [center]
select all | none | invert
select [add | remove] face I...
select [add | remove] +x | -x | +y | -y | +z | -z
select [add | remove] box X0 Y0 Z0 X1 Y1 Z1
select [add | remove] slot NAME
move X Y Z
rotate x | y | z DEGREES [at X Y Z]                 rotate AX AY AZ DEGREES [at X Y Z]
scale F [at X Y Z]                                  scale X Y Z [at X Y Z]
grid STEP                 snap                      material NAME             clear
extrude DISTANCE          extrude X Y Z             inset DISTANCE
delete                    delete fill               merge DISTANCE
flip                      triangulate               duplicate                 join PATH
shade smooth [DEGREES]    shade flat
```

A primitive is placed by the lowest corner of its bounds, at the origin unless `at` gives another,
or by their middle with `center`, and is all that is selected after it. The numbering is each
engine's to match. A box, a cylinder, a wedge, a flight and an arch are an outline pushed out along
an axis, the outline's points at the near end numbered first and then at the far end, the faces
the near end (its corners the outline backwards), the far end (the outline forwards), then a side
for each edge of the outline in order, `[i, i+1, n+i+1, n+i]`, and an outline whose area turns
clockwise about the axis is reversed first. The outlines, as (first, second) coordinates: a box
about Y in (z, x) from (0, 0), (Z, 0), (Z, X), (0, X); a cylinder about Y in (z, x) with point i at
radius times (cos a, sin a), a = 360° (i + ½) / SIDES; a wedge about X in (y, z) from (0, 0), (0,
Z), (Y, Z), which is reversed; a flight about X in (y, z) from (0, 0), then (k+1)·RISE, k·RUN and
(k+1)·RISE, (k+1)·RUN for each step k, then (0, STEPS·RUN); an arch about Z in (x, y), the outside
at radius times (cos a, sin a), a = 180° i / SIDES for i from 0 to SIDES, then the inside from
SIDES down to 0. A cone is a cylinder's outline at the base, numbered first, its point last at the
height over the middle, the base the first face (the outline backwards) and then `[i, i+1, point]`.
A plane is (0, 0, 0), (0, 0, Z), (X, 0, Z), (X, 0, 0), one face. A wedge and a flight rise along +Z,
an arch opens along Z, and a cylinder of four sides is a box facing along X and Z.

The selection is a set of vertices and a set of faces. A face picked brings its corners, an edge is
selected where both its ends are, and removing a face keeps the corners another selected face has.
`invert` selects the faces not selected with their corners and the vertices not selected. A box
picks the vertices inside it by 1e-5 on every side and the faces all of whose corners it picks, and
removing by a box removes every face with a corner it picks. A face faces the way along an axis its
Newell normal is nearest, Y first where two are as near, then X. A turn and a scale are about the
middle of the selection's bounds unless `at` gives a point, a turn counter-clockwise looking back
along its axis, and a scale whose three factors multiply to less than zero reverses in place the
corners of each face whose every corner it moved. One choice differs from the item, which the
guide says: a measure typed is taken as given and never rounded to the grid, as Blender takes a
number typed, since a move of a fifth of a meter on a meter's grid would otherwise move nothing.
The grid step is where a point picked or dragged by hand lands (item 40), and `snap` rounds each
selected vertex to it, halfway going away from zero. `grid` takes a meter or a power of two of it
from a sixteenth to sixty-four. `new` and `free` make and drop a model and are commands, `clear`
empties one in a recipe. The lines of items 39 to 45 are fixed in the Reply of the commit that first
reads them, in either engine, the other following.

Shared: a run recorded as an animated GIF or PNG, in the commit carrying this line, item 46's first
half, for its row, the words those 3DEngine's item 70 takes. The command is `record <path>
<length> [every <step>] [scale <s>] [play <time>]`, the length seconds (`5s`), frames of the clock
(`120f`) or a bare count of pictures (`8`), the step milliseconds (`250ms`), seconds or frames, a
tenth of a second unless given, the scale above zero and up to one, and `play` a time every frame is
shown for in place of the time between it and the next, which the item named `play <ms>`; and
`record.stop` ends one sooner with what it took. A picture is taken when the clock reaches the start
and each step after it, and one that would fall at or past the length is not, so `5s every 250ms`
takes twenty, the last a quarter before the end; the last frame is shown for the step, or where the
step is frames for the time before it. The file is the path's extension, `.gif` or `.png`, written
on a worker under `<path>.part` and moved into place whole once the last picture is back.
`Capture.StartRecording(path, intervalMs, scale)` records until `Capture.StopRecording()`, each
returning a task that finishes with the path once it is written. The GIF's palette is 255 colors by
median cut over every frame's pixels counted in 32 steps a channel, each color the mean of its
pixels, the 256th left clear; a pixel is dithered by Bayer's eight by eight pattern between its
nearest color and the one on the other side of it, so a pixel that is a color of the palette is
never dithered and a still part gives the same indices each frame; a frame after the first is the
rectangle that changed, its unchanged pixels clear, left in place; delays are rounded to hundredths
so their sum stays nearest the sum given. The animated PNG's frames after the first are the
rectangle that changed, written over, rows filtered adaptively. Both are read back in the tests by
a reader that lays each frame over the last as a browser does. The client's step and
`build/animate.py` come with the second half.
