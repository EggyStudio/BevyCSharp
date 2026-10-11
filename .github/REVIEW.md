# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read. A stash of every changed file
takes what was written here since the last commit out of the tree until it is popped, so a stash
names its own paths.

Reviewed up to `02598971`. One commit, item 17, unpushed, ahead of ui_material: the count's markers
sit under the Examples heading after its paragraph and before the gallery, the status section
pointing to them in one sentence, and each group's heading in the gallery carries its count, `27 of
29`, `all 13`, `1 of 1`, which the script writes so the check reads them; the heading counts the
group's examples written whole, not the pictures in its row, since an example that prints has
nothing to draw and no picture, which would have read the input group as one of twelve with all
twelve written, and one written in part is not counted, as the prose count counts, the paragraph
above the gallery saying so and that an example which prints stands in EXAMPLES.md rather than the
row. The prose at 302, the norm, document and path tests passing in a worktree at the commit, the
README's walk run after ui_material on the package packed then. Right, the count that means
something chosen over the literal one and said where the reader meets it. On ui_material's handoff,
the working session corrected the reviewing session's ask: a second call that re-read the table on a
mismatch would pack a bind group against the new layout for the key Bevy had already stored from the
first read, which the GPU refuses, so on a mismatch it returns Bevy's retry, Bevy keeps the previous
bind group and both calls read the new version the next frame; the choice is a pure function of the
version handed and the current one, packed, stale or missing, with a native test over each case, and
the thread-local's remark says why it exists. Then ui_material's commit, and the rows in the order
given.

Before it, `b134dd4d` and `6e28c3cc` came. `6e28c3cc`, custom_projection, ABI 267: a projection type
the bridge declares, Bevy's perspective with entries of its matrix written over its clip matrix and
its sub-view, the update, the far plane and the frustum's corners the perspective's so it follows
the window, set from C# with the field of view, the near and far planes and the entries by column,
row and value, a column or row past three refused on both sides and a non-camera refused; a native
test reads the two entries landing and every other entry staying the perspective's,
`ProjectionTests` the variant read back as custom with both refusals; the example is Bevy's scene
with its two entries, its capture sheared down and to the left as Bevy's; the cameras guide has its
paragraph and block. `b134dd4d`, render_depth_to_texture, which needed no bridge work after all, its
triage line stale: a depth-only camera at order minus one drawing into no target of 256 by 256 with
a depth prepass and no multisampling, a watch of its depth giving an eight-bit image each frame
shown on an unlit rectangle, the turning cube, the light, the main camera, the instructions, the
cone gizmo at the depth camera and the keys by Bevy's spherical coordinates with the inclination
clamped, the capture showing the cube's depth on the plane as Bevy's, the triage note saying the
depth is shown through the watch's image where Bevy's material reads a depth texture of its own, and
the compute guide saying a camera can draw its depth alone into no target and be watched. The suite:
1,421 passed and 2 skipped of 1,423; cargo's 89 and 155; the norm, document and path tests 20 of 20,
the prose at 299. EXAMPLES.md reads 346 written, 2 in part and 34 missing. Right, the projection's
one new type kept to what C# cannot say, and the depth reached through the watch the engine already
had rather than a texture copy built for one example. The owner asked at 19:35 for the examples'
counts under the Examples heading and a count on each group's heading, which is item 17 (Decision
30), a batch of its own with ui_material's commit or right after. The rows that need no crate and no
word go on in the order given: the interface material, a custom vertex attribute, mip generation,
occlusion culling, the world's serialization, the transform gizmo, picking's debug, the pitch and
the decodable, then the atmosphere's Mars medium, render recovery, Solari's reflections and Bevy's
scene notation, each of those four read against the lock first, since a crate waits on the owner's
word.

Before it, `6641dbcc` came, error_handling, with no bridge work: the ten thousand points come from
the icosphere's triangles read back through `Render.TryReadMesh`, each triangle picked by its
cumulative area with a binary search and a point picked evenly within it by the folded pair, red
emissive spheres parented to the sphere; the errors map onto what C# has, a pointer-move observer
sliding the sphere and throwing where there is no transform with mesh picking on, a system catching
a resource never inserted and logging the captured error, a system catching the refused add to an
entity that never was and logging it as an error with its context, and Bevy's spawn at an id left
out for having no counterpart; the run logs both as Bevy's does and the capture shows the dotted
sphere over the green plane; the running guide says the engine's handling is Bevy's warn handler,
the fail-fast setting its panic, and that a system dealing with its own error catches it. The norm,
document and path tests 20 of 20, the prose at 299. EXAMPLES.md reads 344 written, 2 in part and 36
missing. Right, the sampling done as Bevy's sampler does it from the mesh the engine already reads
back, and the one thing without a counterpart left out and said. custom_projection is under way at
ABI 267: a projection type the bridge declares, Bevy's perspective with entries written over its
clip matrix and its sub-view, the update, the far plane and the frustum's corners the perspective's
so it follows the window, set from C# with the field of view, the near and far planes and the
entries, a column or row past three refused on both sides, with a native test of the entries landing
and the rest staying the perspective's, a managed test reading the variant back as custom with both
refusals, the example Bevy's scene with its two entries, and the cameras guide's paragraph and
block.

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
   since the run of `156d2ce` passed on macOS, N 6.2 the last taken. N 1.3's test counts the Slang
   shaders of the bridge and the examples as it counts the C# and the Rust, as 3DEngine's does since
   its `09419080`, none of them over 800 today. `60141490` takes `NativeTypes.Render.cs` off N 1.3's
   list, split into ten files named for their structs, and those ten types off N 1.2's.
   `render/assets.rs`, `NormTests.cs` and `UiTests.cs`, which stood at 799, 794 and 794, are split
   at `88a75e54`, `f68bd192` and `09e3b339` to 557, 588 and 535, each a commit that moves code
   alone; `app.rs` is at 709 since `c5b6d1d3`. N 7.2's list takes `ff467a5f`, the subject's marks
   run together and pushed before the suite read them, beside the owner's `5a7f2c07`, and the list
   only gets shorter from there; `a0c8b1b0`, the owner's own one-line commit of the showcase's
   caption, pushed, joins it as the owner's with custom_schedule's commit, three left out.
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
   interface material next, the last four of the list read against the lock first.
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
    driving a session and its documents table name the new place, which the owner's ask is the word
    for, as does STYLE.md's line on the skills; the folder goes with the move, and `.claude/` joins
    `.gitignore` so the tool's own lock file there never enters the tree. A batch of its own, taken
    when the owner types the ask in the working session, which changes AGENTS.md and the tool's own
    folder only on the owner's word given there and has asked the owner for it, a right the
    reviewing session holds. Done at `53fd0c3f` on the owner's word typed there: `.github/BCS.md`,
    every reference moved, the folder gone and ignored.

15. **The development machine's GPU is named nowhere a user reads, done at `c447a370` and `c16f67e5`
    (Decision 29).** The owner asked on 2026-10-11 at 17:26, as of 3DEngine, that the mention of
    their machine, the RTX 4070, leave the code and comments a user sees: here `Render.Pipeline.cs`,
    the running guide and the comparison with Bevy name it, each where a measurement was taken; the
    measurement stays and the device becomes a plain phrase, the development machine's GPU or a
    laptop GPU of its class. The documents under `.github` keep the name unless the owner says
    otherwise. Done at `c447a370` in the three files, the CPU's model gone with the GPU's; the
    Solari comment at `c16f67e5`, so nothing outside `.github` names the model.

16. **An offscreen click lands only where the pointer has been.** Found at `d91665d9`: driven
    offscreen through `./bcs`, a click at a point the pointer has not moved to is lost, the button
    example counting two of three clicks at one spot and every one once the pointer has been moved
    there a few frames first, since picking wants the hover a frame before the press. The click
    moves the pointer there and gives the frames the hover takes before it presses, in the CLI's
    verb or in the harness that drives the examples, so a session or a test clicking a place it has
    not hovered loses nothing, measured by the example's three of three, with a test. With the batch
    that next touches the CLI's input.

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

## Replies

Shared: an interface node drawn by a Slang program of the game's own, through Bevy's
`UiMaterialPlugin` with the program's layout and shaders put in place of the type's at specialize,
the key carrying the version they were read from and the bind group waiting a frame where a compile
replaced that version in between (`ui_material.rs`, `bcs_ui.slang`), for 3DEngine to consider where
its interface draws a shader of a game's own.
