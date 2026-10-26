# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read. A stash of every changed file
takes what was written here since the last commit out of the tree until it is popped, so a stash
names its own paths.

Reviewed up to `929a1cf7`, item 70's fourth phase's leaves in the scene field, pushed at 07:13 on
2026-10-26 after the run of `fc02fec5` went green, its own run green at 07:30 with Windows in 17
minutes, carrying the review's files with two faults and a lead under Replies, SHARED.md, DEVLOG.md
and a captioned picture with its note, the commit's sentence naming the guard's cap of 8 GB inside
the 20 GB scope. MaterialSettings.LightThrough, public, is the share of global illumination's light
a meter of a surface lets through, with three bridge calls setting and reading it and setting a
material's field tint, ABI 314, the bridge keeping them beside the material and writing the share in
the fourth channel of each draw's color; the field's splat and stamp paint the alpha as
lightThroughPaint gives it and the resolve scales the glow alike, bcs_field gaining
lightThroughPaint, Of, At, Depth and Kept and bcs_gi seesPastLeaves and fractional texel marks;
gi_trace crosses up to four light-through surfaces and is built twice, a NO_LIGHT_THROUGH build for
a frame where nothing lets light through, the share kept before an interval in an R16Float image
beside the distances since the images have no two-channel half format, gi_ambient reading it,
gi_screen crossing under a flag in texels.z and gi_merge asking seesPastLeaves with the field's
colors. The field's gather reads the leaf and bark materials through their standard base and skips
the impostor, and takes a mesh with a visibility range only where the field's eye stands within it,
the two faults; the leaves let half through (Trees.LeafLightThrough), the leaves, the cards and the
bark set's mean colors tinting them in the field. The slab test reads 0.44 and 0.32 at 2.5 m up at
Low and High against 3DEngine's 0.36 and 0.40 and the geometry's 0.36, and 0.24 and 0.24 at 5.5 m
against 0.37 and 0.32 and the geometry's 0.41, the 5.5 m gap and a tenth held back by an all-through
slab at Low a lead; the floor under nine tupelos takes 0.21 of what solid leaves hold back, the same
in three runs; the presets' crowns hold 0.77 to 1.53 m² of leaf a m³, letting 0.46 to 0.68 through a
meter. The suite whole at the 8 GB cap, 1,973 passed, 3 skipped, 1,976 the listing plus 11, the
scope's peak 5.4 GB; the three profiles, cargo test and the Windows and macOS checks clean.

Before it, `fc02fec5` came, item 70's fourth phase's wind, pushed at 04:05 on 2026-10-26 after the
run of `6777b172` went green, its run green at 04:20.

Before it, `6777b172` came, item 70's third phase's impostors, pushed at 02:28 on 2026-10-26 after
the run of `b9cd8d74` went green, its run green at 02:46.

Items done and verdicts settled before 2026-10-23 stand in REVIEW-ARCHIVE.md beside this file, their
numbers kept, moved there at 19:30 on 2026-10-23 so this file holds the current work alone and is
read whole before a batch; the archive is read when an item or a verdict named here is wanted in
full.

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

70. **Trees and foliage grow from a seed inside Weber and Penn's parameters, through the modeling,
    with no asset files.** From Decision 63, the owner's word at 21:26 on 2026-10-24 and their
    choice of the hybrid, grown method at about 21:5x. Weber and Penn's species parameters of 1995,
    the crown's envelope, the branch angles, the taper and the leaves' arrangement, stay the
    controls an artist sets, and within them the branch structure grows from a seed by space
    colonization after Runions, Lane and Prusinkiewicz (2007) with light competition through a
    shadow grid after Palubicki and others (2009), so crowns fill naturally and react to neighbors
    and walls; the branches are smooth tapered generalized cylinders with joints through the
    modeling's EditMesh with a bark material, the leaves small geometry clusters where meshlets run
    and alpha-cut cards elsewhere with Bevy's diffuse transmission, octahedral impostors at distance
    with instancing for forests, and the wind from a per-vertex branch hierarchy as SpeedTree's and
    Pivot Painter's, which item 69's wind moves; the same seed grows the same tree bit for bit. The
    paper's four presets, the quaking aspen, the black tupelo, the weeping willow and the California
    black oak, with bushes and ferns beside them, are the library's first entries; a tree entity
    placed by preset and seed in the level file of item 65 and the editor of item 67, an example and
    a guide page showing the parameters; tests of determinism by seed, each preset's height and
    spread within the paper's values and the triangle counts within bounds, and captures of the four
    presets held against the paper's figures for their overall shape. Phases proposed by the coder,
    the growth and the branches first, then the leaves and the presets, then the entity, the
    impostors and the editor; the wind with item 69. After item 69 and before 65. The reviewer's
    additions at 21:35, each its own small part. A species grows a handful of variants by seed,
    eight or so, and a forest instances the variants with a yaw and a scale a tree, as SpeedTree
    does, rather than growing each tree; the growth runs on a worker at the level's load and the
    grown skeleton, nodes with parent, place and radius, may be kept in the level file beside the
    seed so a tree is the same on every machine whatever the floating point of the growth. The bark
    and the leaves' pictures are generated too, small noise-made textures and leaf masks, so no
    texture set becomes the large file the owner ruled out. Each tree gets its colliders, a capsule
    for the trunk and the thick branches. The levels of detail cross-fade by dither so no tree pops,
    the cylinders' segments falling first, then the clusters to cards, then the impostor. The leaves
    are written into the scene field as cells that pass a share of light, as 3DEngine's item 100
    gives its field, so the global illumination under a canopy is a shade and not a cave, the share
    shared with 3DEngine through SHARED.md. The paper's parameters that name a growth map to the
    growth's own terms, the envelope to the attractors' placement, the down and rotate angles and
    the curve to the tropisms, the splits to the branching's chance, said in the guide so an
    artist's expectations hold. A leaf color per preset with a seasonal tint is cheap and goes in
    with the presets. The coder's phases at 11:22 on 2026-10-25, taken as laid out, each a commit
    with the suite green and pictures. The first, the growth and the branches, TreeSpecies holding
    the paper's controls, height and scale, the envelope's eight shapes, the base size, three levels
    with down and rotate angles, curve, splits, branches and length a level, the taper and the
    radius ratio, Trees.Grow running space colonization from attractors seeded in the envelope with
    influence and kill radii, tropisms from the down angle and the curve and radii by the pipe
    model, the random numbers a PCG of the library's own so the skeleton is the same on every .NET,
    EditMesh gaining Tube over a chain of points and radii with a child starting inside its parent
    so the joints close without booleans and the bark's UVs along the length, Trees.Build giving an
    EditMesh with a plain bark material, and one aspen-like tree in a picture. The second,
    Palubicki's shadow grid, the leaves as alpha-cut cards with a generated mask and diffuse
    transmission by the paper's leaf parameters, a noise-made bark, the four presets with a bush and
    a fern and a leaf color with a seasonal tint, the guide page mapping the paper's terms to the
    growth's, and the four presets pictured beside the paper's figures. The third, the tree entity
    by preset and seed grown on a worker at load with about eight variants a species instanced with
    a yaw and a scale, capsule colliders on the trunk and the thick branches, the levels of detail
    through VisibilityRange's dither, fewer segments, then leaf cards, then octahedral impostors
    drawn once a variant into an atlas by an offscreen camera and blended from the three nearest
    views, an example, and the feature test's sphere-crowned trees grown with a small forest. The
    fourth, a tree material bending by a per-vertex branch hierarchy of trunk, branch and twig
    pivots with their phases moved by WeatherState.Wind, the leaves' clusters as meshlet geometry,
    and the leaves in the scene field as cells passing a share of light with a Shared line for
    3DEngine's item 100. The level file's and the editor's placing wait for items 65 and 67, the
    kept skeleton going with 65. The first phase at `70212d49` on 2026-10-25, the growth and the
    branches with Tube, the three departures in the head; the second next, the shadow grid, the
    leaves, the bark, the presets against the paper and the guide page. The second phase splits in
    two at 12:18, taken. The growth first, Palubicki's shadow grid with Borchert and Honda's
    allocation of the light a bud receives, shading boxes for walls and neighbors, the paper's
    pruning envelope as the eighth shape for the willow, base splits, curve back and the taper up to
    the paper's three, the four presets from the appendix with a bush and a fern, tests of each
    preset's height and spread against the paper's values, a picture of the six bare and a note of
    their heights and spreads; then the dressing, the leaves as alpha-cut cards with a generated
    mask and diffuse transmission by the paper's leaf controls, a bark from noise, a leaf color a
    preset with a seasonal tint, docs/trees.md mapping the paper's terms to the growth's, and the
    four presets pictured in leaf. The paper's plates stay out of the repository, the note saying
    how each preset's outline compares. The second phase's growth at `c7e65c23` on 2026-10-25, the
    shadow grid, the pruning envelope, the six presets bare against the paper's figures in the note,
    three departures in the head; item 74's pipeline next, then the dressing. At 15:21 the dressing
    comes next, on item 74's sets, the leaf cards cut by the manifest's boxes, and item 75's depth
    goes onto its bark material after. The second phase's dressing at `4ee79828` on 2026-10-25, the
    leaves on the six presets from item 74's atlases with the guide page; item 75's depth on the
    bark material next, then the third phase, the entity, the variants and the forest. The third
    phase in three commits at 16:44, taken. The planting and the forest, Trees.Plant making a root
    entity turned by a yaw from the seed and scaled within a tenth with the stems' mesh and bark
    material on the root and the leaves on a child, one of eight variants picked by the seed and
    grown on a worker the first time a species asks for it, cached by species value for the app, a
    Tick system handing the finished meshes to the waiting trees on the main thread, static capsule
    colliders on child entities along the trunk and every stem thicker than about 8 cm where a
    physics world is given, Trees.PlantForest scattering by Poisson disk, and the feature test's
    ring of thirteen sphere-crowned trees becoming grown presets with a small forest beside the
    meadow as the reading and the picture, the crowns still until the wind; then the levels of
    detail through VisibilityRange's dither, the full tree, then half the tube sides with the thin
    twigs dropped and the leaves halved and drawn larger by root two, then branch cards made at load
    by an offscreen camera, one a thick stem's end; then octahedral impostors, each variant drawn
    once from an eight by eight grid of directions into an atlas, a Slang material blending the
    three nearest views, crossfaded in past the second level. Two answers. A Tree component comes
    with the planting, an EcsText species name and a uint seed, with a registry of species by name
    holding the presets and Trees.Register, and a tree planted by a level file or an editor is an
    entity with the component alone that the Tick system grows and dresses, Trees.Plant adding the
    component to its root, so item 65's level file and item 67's editor place a tree by name and
    seed and nothing more; and the feature test's forest stands as the example, the examples project
    holding Bevy's examples alone under N 5.2, docs/trees.md naming the forest as where to look. The
    third phase's planting and forest at `1bd18a6c` on 2026-10-25, the Tree component, the registry,
    the variants on a worker and the feature test's forest, the departures and the fault in the
    head, the leaves thinning with distance open for the levels of detail, which come next. The
    thinning settled at `d0da9cf8` on 2026-10-25, the mips keeping each leaf box's share; the rest
    of the levels of detail in two commits as proposed, the dither between the full tree and the
    reduced one, then the branch cards drawn at load into a target, read back and given levels on
    the CPU by the same sampled share, since the GPU's mip generation would thin them as the atlas
    did. The dither levels at `ddf51f5a` on 2026-10-25, the full and the reduced tree crossing by
    VisibilityRange, Bevy's shadow origin fault worked around in the bridge; item 76's volume
    shading next, before the branch cards and the impostors, since both are pictures drawn of the
    leaves and would miss it otherwise. The far level at `b9cd8d74` on 2026-10-26, the limbs as thin
    tubes and the foliage on crossed cards pictured once a variant, three faults in the head; the
    impostors next. The impostors at `6777b172` on 2026-10-26, one quad past twenty heights painted
    from three of sixty-four frames, two departures and a fault's mend in the head; (d) next in
    three commits as the coder proposed on 2026-10-26, the wind first, each stem and leaf vertex
    carrying its sway with the trunk bending about its foot, the branches swinging about their
    pivots and the leaves fluttering from a wind uniform written each frame from the camera's
    WeatherState, the cards by their limb and the impostor by the trunk; then the leaves in the
    scene field, each crown's leaf area density written as cells passing the share Beer and Lambert
    give, with a canopy's shade pictured with and without; then the meshlet leaf clusters where the
    bridge is built with meshlets, the coder first reading whether Bevy 0.20's meshlet pass can draw
    the leaf material's two lightings and saying under Replies what it would take if not. The wind
    at `fc02fec5` on 2026-10-26, the sway streams and the shared vertex stages, two trees of one
    variant out of step, the cost within the spread; d2 next, the leaves in the scene field. The
    leaves in the field at `929a1cf7` on 2026-10-26, LightThrough on the material and in the field's
    alpha, the floor under nine tupelos at 0.21 of the held-back light, two faults and a lead in the
    head; item 77 next, the trees' memory, then d3.

77. **The trees' memory for the process read and cut, and the memory guard's stop read as a stop.**
    From the test host's stops of 2026-10-26, read at 06:3x. The suite's host passed 3.75 GB of
    memory after 469 tests, the guard's cap inside a 20 GB scope, a quarter of the 15 GB the GC sees
    there and 8 GB outside one, its _exit leaving no core and vstest's message cutting the guard's
    line off, so the stop read as a crash twice; a 16 GB runner's cap is 4 GB and build/test.py
    reruns the suite in parts after such a stop, so CI may meet it and absorb it unseen. Two parts.
    The memory the trees hold for the process is read by species and variant, the eight variants'
    skeletons and leaves, the far-level plans with each card's stems and leaves, the pictures'
    staging and the atlases, and what a planted forest and the suite leave behind, the readings in a
    note; then cut, the plans' per-card stems and leaves released once the cards are pictured, the
    meshes released after upload or the variant cache bounded, as the reading shows, so the suite's
    host peaks under 3 GB and a forest of a thousand trees of three species holds no more than the
    reading says it needs. And build/test.py prints the guard's own line when the host exits with
    its code, the cap and the measured peak in the run's summary, so a stop reads as a stop. One or
    two commits, after item 70's leaves in the scene field (d2) and before its meshlet clusters
    (d3).

65. **A level is a file of brushes and entities the engine loads, and the prototyping look is its
    palette.** From Decision 61. A text level file holds brushes, convex solids with a texture and a
    world-aligned mapping a face, and entities with a class, keyvalues and the connections of item
    66; the engine loads it into a scene, the brushes meshed with their collision, the entities
    spawned by class; the palette is Unreal's level prototyping look, orange and gray with a
    world-aligned meter grid as Source's dev textures have, with rounded doorways among the brush
    shapes; a test loading a small level and reading its brushes and entities, and the feature
    test's rooms rebuilt from such a file as the first level. Phases proposed by the coder; after
    item 64's first phase. The coder's phases are the format and the loader, convex brushes as
    planes as .map and .vmf have them, built through EditMesh with a material and alignment a face,
    entities with class, keys and outputs, round-trip tests and colliders; then the prototype
    material, a world-aligned meter grid in Unreal's palette with Source's marks; then the feature
    test's hub from a file, the other rooms under item 68.

66. **Entities connect through outputs and inputs as Source's do, in the engine.** From Decision 61.
    An entity's output names a target entity, an input on it, a parameter, a delay and whether it
    fires once, the engine firing them at run time in order with their delays, the level file
    carrying the connections, and the classes of the feature test's level, a door, a button, a
    trigger, a light and a sound, holding their inputs and outputs; a test of a button opening a
    door after a delay and once. After item 65.

67. **The level editor edits the file in Hammer's four panes or Unreal's single view, switchable.**
    From Decision 61, level editing for artists who do not program. The editor opens a level file,
    shows it in four panes, the view and the top, front and side, or in one view, switched by a key,
    draws brushes with a block tool, moves their faces and vertices, applies textures with the
    world-aligned mapping, places entities and edits their keyvalues, and holds the connections of
    item 66 in a panel of outputs, targets, inputs, parameters, delays and fire once; it saves the
    file the engine loads. Phases proposed by the coder; after item 66. The coder's phases are four
    panes or one view, switchable, with a power-of-two grid; the block tool, select, move and resize
    with snapping, undo and saving; the clip tool, vertex editing kept convex and face texturing;
    entities, their properties and item 66's connections panel.

68. **The feature test's level is remade in the new tools and looks as the owner asked.** From
    Decision 61, the owner finding the level ugly. The level is rebuilt as a file of item 65 in the
    prototyping palette with the editor of item 67, its rooms, lights and the course the drive walks
    kept so the feature test's checks hold, under the sky of item 64 with the clean HUD of item 63;
    the captures of the feature test retaken and the owner shown the result. After item 67.

## Verdicts

Verdicts 33 to 42, 44 and 45 are settled, said under each in REVIEW-ARCHIVE.md; 2 and 43 are open,
43 mended at `42abf357`.

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

43. **The pack run of `09fc95dd` fails its defaults step after pushing 0.5.193, Blockout naming
    0.5.178, which has no EditMesh.** On the pack run 39566353797 on 2026-10-21 every job passed,
    the pack job pushed BevyCSharp 0.5.193 to the feed, and its defaults step then failed: Blockout
    names 0.5.178 in its BevyCSharpVersion and does not build on it, `EditMesh` not found at
    Blockout.cs line 25, the step saying to raise each game to 0.5.193, the version the run pushed.
    The coder foresaw it under item 42, BUILDING.md allowing a game's default to lag until a publish
    carries what it needs, and the step not allowing it; the step runs after the push since a game
    cannot name a version before it exists. The mend: Blockout's BevyCSharpVersion raised to
    0.5.193, and the check's words or BUILDING.md's agreeing on what a new game may name before its
    first publish, under N 3.5. One commit, small, with the coder's next batch. Mended at `42abf357`
    on 2026-10-23, the version and BUILDING.md's sentence; settles when a pack run's defaults step
    passes.

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

49. **A screenshot on F2 in every program.** The owner wrote shortly before 03:42 on 2026-10-21
    that, as in Minecraft, a screenshot is to be taken in the game with F2, the same for BevyCSharp,
    the captures put in the root or a subfolder. So item 47 here, item 73 in 3DEngine and item 40 in
    the voxel game: a built-in key writing the frame into a screenshots folder beside the program,
    named by the time as the source names its own, movable and off by a setting.

50. **F1 hides the interface and F3 shows the debug overlay, in both engines.** The owner asked
    shortly before 07:05 on 2026-10-21 for F1 to hide the interface as Minecraft does, as an engine
    feature, F2 the screenshot, and F3 a debug overlay like the newest Minecraft's, sorted in parts
    logically with modules a game adds, world generation among them, showing the system, the camera
    and the renderer, memory, fps and frametime with a graph like the RivaTuner overlay's, both for
    BevyCSharp and 3DEngine, F1 hiding the interface and ImGui but not the F3 overlay so everything
    hides but the overlay over the renderer, and Shift+F3 showing the frame debugger too; and
    answered the reviewer's questions shortly before 07:06: the interface hidden by a scope, F3 the
    overlay's on its release alone with any chord left to the program, the look Minecraft's with no
    header, window styling or border, text on a transparent backing, and the order as the reviewer
    sees fit. So item 49 here and item 78 in 3DEngine, the same in both, after Blockout here and
    after the cascade faults there, and the voxel game taking them when the package comes. Shortly
    before 07:10 the owner added that Minecraft hides its HUD only while the player's camera shows
    and not in the pause or any other screen, so hiding every panel at any point, even in menus,
    would be wrong; the scope gives that, the HUD inside it hidden and a screen drawn outside it
    never, which the items say.

51. **3DEngine's radiance cascades come to this engine.** The owner wrote to the coder shortly
    before 07:15 on 2026-10-21, on Blockout's lighting, that the radiance cascades of 3DEngine were
    always intended for this engine as well, built there first, and can be done here too, and said
    to ask the reviewer. So items 50 to 52, the three phases as 3DEngine built them, the field, the
    cascades with the additive pass and the reference, then the screen's probes, the glow lights and
    the mends of the day, each read from 3DEngine's code and RENDERING.md as the model with its
    kernels taken through the bridge; items 50 and 51 after item 49 and before 43 to 45, item 52
    after them; Blockout keeps its lighting and switches to the cascades once item 51 exists. The
    ledger's row changes from to consider to to take. Asked shortly before 07:28 whether the
    cascades are ready to copy, the reviewer said not yet, the method still moving in 3DEngine with
    its faults, the fill cascades and the budget open, and the owner said that if not yet ready,
    BevyCSharp waits; so item 50, the field, goes after items 43 to 45, and items 51 and 52 wait
    until 3DEngine's items 71, 72 and 74 are mended and the voxel game's captures say the light
    holds, Blockout and the games keeping Solari where the GPU has ray queries and the sun with
    ambient and occlusion elsewhere.

52. **A denoiser for the ray-traced tier of global illumination, beside the cascades, in both
    engines.** The owner asked at 08:42 on 2026-10-21 why BevyCSharp has no denoiser, was answered
    that Bevy's Solari at 0.20 knows only DLSS Ray Reconstruction, which the bridge does not build,
    ReSTIR halving the noise meanwhile, and said shortly before 08:56 that a denoiser for Solari is
    necessary as the higher-definition solution for global illumination beside the radiance
    cascades, and that it is implemented for both engines in the future. So the engines carry two
    tiers, the cascades everywhere, which need no denoiser since they integrate over directions, and
    a ray-traced lighting with a denoiser where the GPU has ray queries, here over Solari's output
    as item 54, after item 50 and before items 51 and 52, and in 3DEngine over a ray-traced pass of
    its own once its cascades settle, its Decision 59, no item there yet. The owner holds the source
    of H-Trace, a Unity asset received from its developer as a patron; it is read for the method's
    shape and its constants as the modpack's shaders were read by the voxel game, and nothing of its
    code enters either repository, the license being the owner's to use and not to publish, unless
    the developer grants it in writing, which the owner may ask.

53. **The owner's cloud sessions take GPU-free items on branches of their own.** On 2026-10-23 the
    owner opened a cloud session on each engine with 200 dollars of usage of their own, four cores,
    no GPU and no display, each able to push to one branch of its own and never to main, this
    engine's on `claude/festive-faraday-r9drnv`, with Cargo installed and the .NET SDK to install,
    the headless suite running there. A cloud session reads REVIEW.md, COMMITS.md, STYLE.md and
    NORM.md as a coder does, commits in COMMITS.md's form on its branch, edits none of the review's
    files, and reports to the reviewer in a few lines; the reviewer reads its commits from the
    branch, and the local coder cherry-picks them onto main as the first part of its next batch,
    taking the captures and the device tests the cloud cannot, so main keeps one writer and the
    pushes stay the reviewer's.

54. **Branches are named for their work, never claude/ or a random string, and work stays on main
    where one session writes.** The owner wrote at 20:16 on 2026-10-23 not to create branches named
    claude or the like and not to name them with random characters, but to work on main, or, where
    several sessions on one repository make a branch necessary, on a branch with a descriptive name
    and no claude/ prefix. So the cloud session's work moves to a branch named modeling-operations,
    and its claude/ branch is deleted once its commits are on main; a branch exists only while a
    second writer needs one.

55. **Every commit carries the owner's identity, and the cloud session's branch was deleted for not
    carrying it.** At 20:25 on 2026-10-23 the owner saw that the commits on the cloud sessions'
    branches named Claude as their sole contributor, said no to it and asked for the branches to be
    deleted and gone for good; the reviewer deleted `claude/festive-faraday-r9drnv` and its workflow
    runs. N 7.6 holds the rule: every commit's author and committer are the owner's identity on
    every branch, a session setting it before its first commit, a commit with another identity
    pushed nowhere and rewritten first. The cherry-picks on main, `ae596af8` and `4f80c464` for item
    43, carry the owner's identity, as item 44's will. The cloud session was told to stop pushing,
    set the identity, rewrite the authors of every local commit not on main and push nothing until
    the owner confirms `modeling-operations` in that session.

56. **Slang reads the same in both engines, plain names and no module prefix.** The owner asked at
    21:31 on 2026-10-23 why the Slang in BevyCSharp cannot be used as in 3DEngine without `::` and,
    told that the bridge's modules alone wrap their names in namespaces, said yes to dropping them;
    so the bridge's modules give their names plainly, each helper named for its work as 3DEngine's
    are, item 56, and a shader's words are the same in both engines where they hold the same thing.

57. **Until a full release, both engines are used by nobody.** The owner wrote at 21:36 on
    2026-10-23 that an upgrading entry is not needed since nobody uses the engine, said "Drop" to
    the rule that every new public name has its line on 3DEngine's upgrading page, and added that
    until a full release both engines are to be considered used by no one. So a change of the public
    surface is made outright, with no entry for a name gained or lost, no deprecation and no path
    kept for an older caller; the package's release notes written from the commit messages are the
    record of what changed, and the upgrading page this engine never had stays absent, the Shared
    row's upgrading guide not kept. The owner's word ends this at the full release.

58. **The two cheat sheets have one form.** The owner wrote at 21:39 on 2026-10-23 that the cheat
    sheets are to be coherent: 3DEngine's lines one-liners as BevyCSharp's are, and BevyCSharp's
    section headers as 3DEngine's, plain words without the code styling. So both pages are a `#
    Cheatsheet` heading, a paragraph saying what the page is and what writes it, then `## Group`
    headings in plain words, each group a guide link line and one fenced block, and in the block one
    line a member, the signature and then `//` and the first sentence of its documentation on the
    same line however long, no continuation lines and no sub-heading a type.

59. **A devlog under `.github/devlog`, with AGENTS.md's row for it.** The owner asked at about 04:35
    on 2026-10-24 that the pictures the sessions made be saved with a text in a folder under
    .github; `.github/devlog/README.md` holds what landed by day, newest first, with the
    repository's captures and the items closed, written by the reviewing session and carried by the
    coder's commits like REVIEW.md, and the owner gave this repository's session the word for
    AGENTS.md's row at about 04:50. At about 07:56 the owner asked that the devlog be `DEVLOG.md` at
    the repository's root, with no `.github/devlog` folder, the engine's pictures being its
    committed captures; `.github/devlog/README.md` goes and AGENTS.md's row names `DEVLOG.md`.

60. **A session saves the pictures of its work in the repository, N 4.8.** The owner wrote at about
    18:5x on 2026-10-24 that this session had kept none of the day's radiance cascade captures, item
    51's readings and pairs, which they would have loved to see, the session answering that no
    document said the pictures in progress should be saved, and asked for the rule written where the
    sessions read it and the same in the three repositories. N 4.8 says it, one text in both
    engines' NORM.md and the voxel game's, the engines' pictures under .github/assets/readings/ and
    the game's under .github/devlog/, each a WebP named by the date and the item in the commit of
    its work with the commit's sentence as its caption, DEVLOG.md generated from them; the day's
    captures still in this session's scratch space go in with the next commit.

61. **The feature test's level is redone, the look, the sky, the file, the entities' connections and
    an editor.** The owner wrote to this session at about 19:30 on 2026-10-24 that they hate the
    feature test level, it looks ugly, and set its remaking as the work after item 52, ahead of the
    Now list. The look is Unreal's level prototyping style, orange and gray blockout with rounded
    doorways, with the best of Source and Hammer, level editing for artists who do not program; the
    sky photoreal after Hillaire 2020 with Cody Bennett's post as the reference, keeping 3DEngine's
    vibrance, a lower hemisphere taking the fog's and the horizon's color, and its tone curve and
    grading; the volumetric clouds, the blurred sun dot, the frame text at the bottom left and the
    stray pixels in the screen's middle removed, a green FPS number in a top corner added. The
    owner's answers, the map a level file of brushes and entities the editor opens and edits, built
    in phases; Source's entity I/O as an engine feature with an editor panel, output, target, input,
    parameter, delay and fire once; Hammer's four panes and Unreal's single view, switchable;
    Unreal's palette with a world-aligned meter grid as Source's dev textures have; item 52 first.
    Items 63 to 68 hold it, the coder proposing the phases when 52 is in.

62. **The fog, the clouds and the rest of the weather are written in the engine, and the weather
    crate goes.** The owner wrote to this session at about 21:3x on 2026-10-24 to implement fog and
    clouds natively rather than through the crate, using Bevy's sky, and then the other things of
    the weather crate natively too; asked how GTA does it, the coder said GTA V keys an artist
    timecycle hourly for sky, fog, light, exposure and grading, RDR2 lights a physical atmosphere
    with lookup textures, raymarched clouds and froxel fog together under timecycle art direction,
    and GTA VI's is unpublished. Item 69 holds it after item 64, the volumetric fog and clouds lit
    by the atmosphere first, then rain, snow, thunder, wind, the stars and the moon, and the
    vendored crate removed when they stand.

63. **Trees and foliage are grown, not loaded.** The owner wrote at 21:26 on 2026-10-24 that
    BevyCSharp should have trees, and rather than relying on assets and large files a way to use and
    expand the modeling to create a wide variety of trees and foliage as SpeedTree does, with the
    Weber and Penn algorithm. Asked by the coder at about 21:5x whether the paper's own branching or
    a grown one, the owner chose the hybrid, grown method, the paper's parameters as the controls
    and the branches grown inside them by space colonization with light competition. Item 70 holds
    it after the weather and before the level file.

64. **The devlog written as the work goes.** The owner asked on 2026-10-25 at 05:13 that the
    sessions write the devlog more often than at a day's end, though not at every commit. N 4.8
    gains the cadence, an item that changes what is seen ending with at least one captioned picture,
    a pair before and after where it mends and one a phase where it is built in phases, and
    DEVLOG.md regenerated by the review as each item is archived, the coder's next commit carrying
    it as it carries the review's files; the three devlogs were regenerated at 05:16 with the day's
    pictures and items. The same in 3DEngine (Decision 71) and Voxel-Sandbox (Decision 47).

65. **The devlog carries notes beside the pictures.** The owner asked on 2026-10-25 at about 12:01,
    shown the table of the screen probes' cost at four resolutions from 3DEngine's item 122, that it
    be written in a devlog or something along those lines. N 4.8 gains the note, a reading worth the
    devlog's reader with no picture to carry it, a table or a few sentences, saved beside the
    pictures as a Markdown file named the same way, and DEVLOG.md carries it on its day under the
    pictures; the generator reads them, and the first note is 3DEngine's
    2026-10-25-item-122-resolution.md. The same in 3DEngine (Decision 73) and Voxel-Sandbox
    (Decision 49).

66. **Free textures ship as a bounded collection; the models stay reference.** The owner put trees'
    textures and reference models under .ref/Vegetation, vegetation2 and vegetation3, 6 GB of PBR
    sets, leaf scans and OBJ models, and wrote on 2026-10-25 at about 13:00 that they are free to
    use, that the review should check them and choose what to implement texture-wise as source
    assets the way the Source engine ships textures, that a collection can grow from them with a
    document requesting assets the owner then provides, that the review may compress, change the
    resolution, crop, adjust and alter the colors as it likes so the tree generator has textures and
    can build the leaves, that the textures be memory efficient for the repository's size and for
    rendering alike, that the review choose what it needs and skip the rest, and that the models are
    reference and not to be imported into the generation. Decision 63's rule against asset files
    narrows to models and large files; a texture collection ships, bounded at 32 MB with entries at
    most 1024 square as supercompressed KTX2, item 74, and .github/ASSETS.md is the review's request
    list. Amended at 14:22 on the owner's word to the coder, the sorted folder .vegetation ships as
    the collection and no document says more of it. The paper's presets take the collection's bark
    and leaves where an entry fits and the generated ones elsewhere.

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
clockwise about the axis is reversed first. The outlines are given as (first, second) coordinates, a
box about Y in (z, x) from (0, 0), (Z, 0), (Z, X), (0, X); a cylinder about Y in (z, x) with point i at
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
corners of each face whose every corner it moved. One choice differs from the item, as the guide
says. A measure typed is taken as given and never rounded to the grid, as Blender takes a
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

Shared: the client's step and the script for item 46, in the commit carrying this line, the words
3DEngine's item 70 takes. `./bcs record <path> <length> [--every <step>] [--scale <s>] [--play
<time>]` sends `record` with the path made full and quoted, then asks `record.status` for as long as
it answers `recording`, `finishing` or `writing`, and answers with the path and its size where
it says `wrote <n> frames to <path>`, or `NOT_RECORDED` with what it said, so a length on the app's
clock is waited for however long it takes by the machine's. `build/animate.py <out> <pictures...>
--delay <ms> [--loop <n>] [--scale <s>]` makes PNG or WebP captures into a GIF, an animated PNG or
an animated WebP by the out's extension, each fitted to the first's size, with Pillow where it is
installed, its GIF's palette cut over every frame laid in a strip, and where it is not through
`./bcs animate` with the engine's own writers, PNG in and GIF or animated PNG out, a WebP either
way needing Pillow; `BCS_ANIMATE_WRITER=engine` asks for the engine's where Pillow is installed, as
the suite's test does to try both. The README's first animation is Swarm's third wave, recorded by
`./bcs record` and set above the examples' generated gallery, since that block is the examples
script's and its pictures are the captures the pack run compares.

Shared: the edit mesh's topology operations, its recipe, its build and its collider, in the commit
carrying this line, item 38's second half, for the first row of Modeling, in the first half's
words, with what keeps the numbering the same in both engines. An extrude moves the selected faces
as one region. A rim edge is an edge of a selected face that no other selected face runs the other
way, taken in the order of the faces' indices and of their corners; each rim corner is copied, the
copies numbered after the last vertex in the order the rim meets them, an edge's start before its
end; the region's faces take the copies, a corner inside the region moves itself, and each rim edge
(a, b) takes a side face [a, b, b', a'] appended in rim order in its face's slot. By a distance, a
corner moves so each distinct normal of the region's faces round it is met at the distance, d n for
one, d (n1 + n2) / (1 + n1 · n2) for two, and for more the least squares answer by Cramer's rule,
the mean normal where that is singular. An inset goes face by face in index order, each corner
moved in by d (a + b) / (1 + a · b), a and b the inward normals of its two edges (the face's normal
crossed with the edge), its new corners numbered in the face's order, the face taking them and a
band face [c_i, c_i+1, q_i+1, q_i] appended for each edge. A delete takes the selected faces, or with
none the faces using a selected vertex; a fill follows the rim, the taken faces' edges a kept face
runs the other way, from its lowest vertex, each loop a face appended in the taken faces' direction
in the slot of its first edge's face. A merge joins each selected vertex, in index order, to the
first kept vertex within the distance, drops a corner equal to the next one round, and removes a
face left with fewer than three. After a delete or a merge the vertices no face uses go and the
rest are numbered again in order. A triangulate cuts each selected face in index order by ear
clipping, the face laid flat across the axis its Newell normal is nearest and turned
counter-clockwise, the ear cut the first of the corners left whose triangle (before, it, after)
turns counter-clockwise and holds no other corner left, inside or on its sides, the first triangle
kept at the face's index and the rest appended, a face with no ear left finished as a fan. A
duplicate copies the selected faces in index order, each corner's copy numbered at its first use. A
join appends the other model's vertices and faces in order, its slots matched by name. `shade
smooth` with no angle is thirty degrees. An undo runs the lines before the last from an empty mesh,
and a recipe's file is UTF-8 with `model 1` first. The build makes one mesh a slot, each face's
corners copied in order and cut by the same ear clipping; a corner's normal is the face's, or for a
smoothed face the mean, weighted by area, of the smoothed faces round the corner within the face's
angle of it; its texture coordinates are its place in meters along two directions chosen by the
axis the face's normal is nearest, y first and then x where two are as near: +y (x, z), -y (x, -z),
+x (-z, -y), -x (z, -y), +z (x, -y), -z (-x, -y); and tangents are mikktspace's from those, which
this engine has from `Render.GenerateTangents`, so the bridge is unchanged. The collider is the edit
mesh's own vertices with the ear clipping's triangles. The feature test's gallery holds a row of the
seven primitives north of its wall, solid as their triangles, and the drive takes it as `5-models`.

Shared: the grid image, its mipmaps and the prototype materials, in the commit carrying this line,
item 39's first half, for the second row of Modeling, with what keeps them the same in both engines.
`Images.Grid(pixelsPerMeter = 1024, face, line, fine)` is the image of one meter's cell, from 16 to
4096 pixels a side. A meter's line is max(2, ppm / 128) pixels wide, half on each edge so cells
tiled make it whole, and a tenth's line max(1, ppm / 512) wide, starting at
`round(t * ppm / 10 - width / 2)` with a half rounded up for t from 1 to 9, a pixel on a meter's
line taking the line's color over a tenth's. The defaults are sRGB grays, 0.9 for the face, 0.35 for
the meter's lines and 0.68 for the tenths'. It is made with its mipmaps, each level half the one
above down to a pixel, each pixel the mean of the four above it taken as light, decoded from sRGB,
averaged and encoded to the nearest byte, through a new export of the bridge for an image given its
levels, which takes the ABI to 299; its sampler repeats, filters linearly between pixels and between
levels, and takes sixteen samples along a slant. `Modeling.PrototypeColors` names the seven parts
with sRGB colors, floor (0.52, 0.53, 0.55), wall (0.82, 0.79, 0.72), ceiling (0.70, 0.77, 0.86),
stairs (0.92, 0.56, 0.22), door (0.38, 0.72, 0.38), window (0.36, 0.62, 0.92) and hazard (0.90,
0.22, 0.16), and `Modeling.Prototype(name)` is the grid tinted by its color, rough at 0.85 and not
metal, any other name taking the grid's own gray, with `Modeling.Prototype(color)` for any color,
one grid an app and one material a color. A slot's name picks its part's material where the model is
drawn, so no line is added to the recipe. Item 39's world-aligned box mapping came with item 38's
build, by the directions that commit's Reply gives.

Shared: the booleans and the openings, in the commit carrying this line, item 39's second half, for
the Modeling row of booleans, with their lines and what keeps a mesh after one the same in both
engines.

```
union PATH [at X Y Z]        subtract PATH [at X Y Z]        intersect PATH [at X Y Z]
cut <a solid primitive's line>        (box, cylinder, cone, wedge, stairs, arch, room or ramp)
cut window WIDTH HEIGHT ALONG SILL                  cut door WIDTH HEIGHT ALONG
room X Y Z THICKNESS [at X Y Z] [center]            ramp RISE RUN WIDTH [at X Y Z] [center]
epsilon E
```

The partition is csg.js's, ported with its notice, in doubles, each face cut by the ear clipping
into triangles before it, a point in a plane within the epsilon, 1e-5 unless `epsilon` says another,
and its pieces kept in the order csg.js leaves them, a node's own polygons, then its front's, then
its back's. What it leaves is made into faces again. The points are welded within ten times the
epsilon, numbered in the order the pieces bring them. Each edge is cut at every welded point within
the weld of it, so neighbors share whole edges. Pieces in one plane, their normals within 1e-6 of
one another and their offsets within the weld, of one slot and one smoothing, are joined along an
edge one runs and the other runs back, the first such pair in order joined and the search begun
again, a joining refused where its loop would hold a vertex twice. A corner lying straight between
its neighbors in every face it is in is then taken from all of them, the vertices no face uses go and
the rest are numbered again in order, and the second solid's faces are selected. `cut` writes the
primitive's own line after it. A room is the outside box's prism then the inside's, its walls the
thickness in from every side, its faces wound back, sixteen vertices and twelve faces. A window or a
door goes through the wall whose face is selected first, refused where that face is a floor or a
ceiling, its normal more than 0.7 up or down. The wall is every selected face in that face's plane,
its normal within 0.9999 and its offset within a millimeter, measured along right, the world's up
crossed with the normal, from its least, for ALONG to the opening's middle, and along up, the normal
crossed with right, from its least, for SILL to the opening's bottom. The box is a centimeter past
each face, as deep as a ray from a centimeter outside the face, at the middle of the opening, finds
the nearest other face it leaves the wall through, less that centimeter, or the mesh's diagonal
where it finds none, and a door's box starts a centimeter below the wall's bottom. A ramp is a
wedge's outline about X, (y, z) from (0, 0), (0, RUN), (RISE, RUN), as wide as WIDTH.

Shared: the modeling commands, in the commit carrying this line, item 40's first half, for the
Modeling row of commands, in the words of item 38's Reply. Each line of the recipe is a command,
`model.<verb> <name> <the line's words after the verb>`, thirty-one of them, which runs the line on
the model of that name and answers with its counts, `<name>: <v> vertices, <e> edges, <f> faces, <sf>
faces and <sv> vertices selected`, so a script reads what each did. A primitive (box, plane,
cylinder, cone, wedge, stairs, arch, room or ramp), a join or a union makes the model where there is
none of the name, and any other verb on a name no model has is refused as `NO_SUCH_MODEL`, so a
mistyped name makes no model; a line the recipe cannot read is refused as `BAD_ARGUMENT` with the
recipe's own message. Outside the recipe, `model.new <name>` makes an empty one, `model.free <name>`
forgets it and takes away what draws it, `model.list` gives each one's counts, `model.info <name>`
its counts, bounds, volume, open edges and whether closed, slots, grid, epsilon and the recipe's
length, `model.recipe <name>` its file's text, `model.undo` and `model.redo <name>` the counts after
(`NOTHING_TO_UNDO`, `NOTHING_TO_REDO`), `model.save <name> <path>` and `model.load <name> <path>`
the recipe's file under the asset root, and `model.show <name> [<x> <y> <z>]` a root entity at the
point with a child for each slot in that slot's prototype material, its meshes written again once a
frame where the model changed. A game's mesh answers to them once `Modeling.Register(name, mesh)`
names it, `Modeling.Find(name)` and `Modeling.Names` read them, and the names last while the app
runs, the next app starting with none. `model.export` and `model.import` wait for item 45.

Shared: the screenshot key, in the commit carrying this line, item 47, for its row, in the words
3DEngine's item 73 takes. `Capture.ScreenshotKey`, F2 unless set and none with `null`, saves the
frame being drawn through the capture `shot` takes, read after the frame's input with the press left
for the program, into `Capture.ScreenshotFolder`, a folder named `screenshots` beside the program,
made on first use, the file named by the local time as `yyyy-MM-dd_HH.mm.ss.png`, the second in one
second `_2` and the third `_3` as Minecraft counts them, the names given out remembered since a file comes a frame or two
after its press, and a line of the log, `[screenshot] saved to <path>`, saying where. A headless run
takes none. `Capture.Screenshot()` takes one from code. Nothing of this engine's, its editor, the
feature test or the examples held F2, so no key of its own moves to F3.

Shared: the modeling window, in the commit carrying this line, item 40's second half, for the
Modeling row, in the words of item 38's Reply. `Modeling.DrawWindow(ctx, mesh, camera, placed)`
draws an ImGui window titled `Model` on an edit mesh, and `Modeling.DrawPanel` draws its contents
where a program has begun a window of its own. It shows `<v> vertices, <e> edges, <f> faces` and
`<sf> faces and <sv> vertices selected`, a grid list from a sixteenth of a meter to 64 by powers of
two writing `grid <step>`, buttons writing `select all`, `select none`, `select invert` and `select
+x` through `select -z`, Undo and Redo, the recipe's lines in a list kept scrolled to its end, and
each verb with its measures as text fields, grouped Add (box, plane, cylinder, cone, wedge, stairs,
arch, room, ramp), Change (move, rotate, scale, material, extrude, inset, merge, shade, delete, flip,
triangulate, duplicate, snap) and Cut (cut, union, subtract, intersect, join), its button running
`<verb> <fields>` with `at` written before a place, and a line that throws shows `<line>: <message>`
under the selection's buttons. In the 3D view a ray from the camera through the pointer, turned into
the mesh's space by the inverse of `placed`, meets the first face by its triangles. A left press on
a face released within 4 pixels of it, counted as the sum of the two axes, writes `select face <n>`,
`select add face <n>` with shift and `select remove face <n>` with control. Past 4 pixels it is a
drag, whose distance is the point of the face's normal line through its middle nearest the ray, in
the mesh's space, rounded to the grid step with halves away from zero and shown beside the pointer
as `<d> m`. On release a distance of 0 writes nothing, and otherwise the face is selected first
where it is not and the line is `extrude <d>`, or `move <nx·d> <ny·d> <nz·d>` where alt was held in
the last frame the button was down. The outlines are drawn from the frame's tick, the selection in
orange (1, 0.55, 0.1), the face under the pointer in yellow (1, 0.95, 0.2) and the dragged face at
its distance in cyan (0.3, 0.85, 1). `Modeling.WantsPointer` is true where a window's pointer was
over a face or pressed on one this frame or the last, and the editor's alt orbit begins only where
it is false. `Modeling.Source(handle)` gives the edit mesh behind a mesh of `Meshes`, which the
editor's Model tab opens on for the entity selected, its picking and its marquee off while the tab
is drawn.

Shared: texture coordinates and material slots, in the commit carrying this line, item 41, for the
Modeling row, in the words of item 38's Reply. A face holds coordinates of its own, one for each
corner, or none for the default, each corner's place in meters along two directions picked by the
axis its normal is nearest (the largest absolute part, Y winning a tie, then X), (X, Z) for +Y,
(X, -Z) for -Y, (-Z, -Y) for +X, (Z, -Y) for -X, (X, -Y) for +Z and (-X, -Y) for -Z. A face's own
coordinates stay on its corners as they move. `uv planar +x` to `-z` or `<x y z>` projects along a
direction, the way from the faces toward the projector, taking the default's directions for that
side where the direction's Y is above 0.999 in size, and otherwise right as the world's up crossed
with the direction, made a length of one, and down as right crossed with the direction. `uv box`
writes the default onto the faces. `uv cylinder x|y|z|<x y z> [at <x y z>]` wraps about an axis
through the point, by default the middle of the box holding the selected faces' corners, across
being the planar right of the axis and side the axis crossed with it, each corner's angle the
arctangent of its offset along side over along across, taken within a half turn of the angle of the
face's mean corner, a corner on the axis taking that angle, the first coordinate the angle times
the mean distance of the selection's corners from the axis (one where that is below a millionth)
and the second minus the height along the axis. `uv sphere [at <x y z>]` takes X and minus Z for the
angle, and minus the arctangent of the height over the level distance for the second, both times
the mean distance from the point. `uv move <u> <v>`, `uv scale <s> [<t>] [each]` and
`uv rotate <degrees> [each]`, turning from the first coordinate toward the second, act about the
middle of the box holding the selected faces' coordinates, or each face's own with `each`, and
write the default onto a face first. `uv density <pixels a meter> <texture width>` scales about that
middle by the density over the width over the square root of the summed size of the faces'
coordinate areas over their summed area in square meters, and refuses where those cover nothing.
`uv reset` takes them back to the default. A flip and a mirroring scale reverse a face's coordinates
with its corners, a triangulation, a duplicate and a join copy them, an extrude's cap keeps them, and
an inset's smaller face and the pieces a boolean leaves take the least squares affine fit of the
face's coordinates over its plane at their new corners, the merge after a boolean joining pieces
only where their fits are equal. The faces an operation adds, an extrude's sides, an inset's ring, a
fill's cap and the cutter's faces, take the default. A slot is a name with a look, changed by
`slot add <slot>`, `slot rename <slot> <new>`, `slot remove <slot>`, refused for the first, whose
faces go to the first slot and after which the slots move down by one, `slot color <slot> <r> <g>
<b> [<a>]` in sRGB as typed, `slot texture`, `normal-map`, `occlusion-map`,
`metallic-roughness-map` and `emissive-map <slot> <path>`, with `none` taking one away,
`slot emissive <slot> <r> <g> <b>` in linear numbers, and `slot metallic` and `roughness <slot>
<value>` from zero to one, a look's line making the slot where there is none, and a recipe made
again starting from a first slot called `default` with no look. A look with nothing given is the
prototype material of the slot's name, a color alone tints the grid, a texture takes the grid's
place tinted by the color or white, the roughness is 0.85 without a texture and 0.5 with one, an
emissive map alone is emissive in white, an alpha below one blends, every texture repeats, the picture and
the emissive map are read as colors and the other maps as data, and an app makes one material a
look. A join or a boolean keeps this mesh's look for a slot both have and takes the other's for one
it lacks. `model.uv <name> <words>` and `model.slot <name> <words>` run the lines, a density of one
number given the width of the texture of the first selected face's slot, the grid's 1024 where it
has none, and refused while the texture loads or where no app runs, and `model.info` lists each slot
with its look in the recipe's words. The window's Texture group has box, sphere, reset and from the
view, the way back toward the camera in the mesh's space, rows for the other lines, and the
selected faces' coordinates drawn over the slot's picture, a square of whole repeats from the
repeat their least coordinates fall in, eight pictures a side at most, and its Slots group a row a
look's line. The examples' resources hold no brick, so the test's wall takes the parallax example's
block texture with its normal map, shown a picture a meter, and the floor the chair's wood.

Shared: Blockout, in the commit carrying this line, item 42, for its row, in the words of item 38's
Reply. The level is `games/Blockout/assets/levels/blockout.model`, whose recipe words are the
engines' shared ones, so item 63 takes the file as it is. It is cut from one block, `box 22 6.6 8`,
so every cut works on one closed solid. The hall is x 0.2 to 9.8 and z 0.2 to 7.8 under a slab, the
room over it is open to the sky, the corridor is x 10 to 15.8 and z 3 to 5, and the second room is x
16 to 21.8, with walls 0.2 thick. There are two doors 1.2 by 2.2 at z 4, a window 1.6 by 1.2 on a
sill of 1, a stairwell over the stairs from z 2, and a lamp, an emissive panel joined under the
hall's ceiling. The stairs, 16 steps of 0.2 by 0.3 and 1.2 wide, are joined by `union stairs 16 0.2
0.3 1.2 at 0.2 0.2 1.4`, since `union`, `subtract` and `intersect` take a solid primitive's line as
`cut` does, made where its own words place it, refused with an `at` of the boolean's own, and its
faces put in the mesh's first slot. A flight added with `stairs` touches the floor and the wall as a
second solid, and a later cut welds their edges, four faces along each, all of them counted open.
The walker is a capsule 0.7 wide and 1.8 tall at an offset of 0.9, of 70 kg, with the character
controller, walking at 4 m/s along the view, jumping at 4.5 when grounded, its eyes 1.6 up, starting
at (5, 0.2, 4) looking along -Z. The right button held turns the view 0.004 radians a pixel and the
arrows 2 radians a second, its pitch held within 1.4, and no key acts while a field has the caret,
which `ImGuiRuntime.Typing` says. The camera has a field of view of 70, the clear color (0.55, 0.7,
0.9) and one sample a pixel. The sun gives 10,000 lux with shadows from (-6, 30, -24) toward (11, 0,
4), the sky's light is (0.75, 0.82, 1) at 400 with ambient occlusion at the medium quality, and a
point lamp of 400,000 lumens and a range of 16 hangs at (5, 2.9, 4). Where ray-traced lighting comes
on, the parts are handed to it and the sky's light and the occlusion go off. Tab opens and closes
the modeling window, which opens without taking focus so the opening key moves into no field, and F5
saves the recipe to its file with `[blockout] saved the level to <path>` in the log. The level
answers the model commands as `level`, a model loaded under that name becomes the level, and its
parts and its collider, a static body of `Modeling.Collider` removed and added again, are made again
when its version moves. `blockout.status` answers five lines, `at x y z, on the ground` or `in the
air`, `on the ground floor` or `upper floor` above 3 m; `lighting ray traced` or `the sun, the sky's
light and ambient occlusion`; `modeling window open` or `closed`; `autopilot off`, `<route> at point
n of m`, `<route> done` or `<route> stuck before point n of m`; and `level V vertices, E edges, F
faces, collider made N times`. `blockout.autopilot upstairs|downstairs|loop|tour|off` walks routes
of points on the floors, each reached within 0.3 m along the ground, stuck after three seconds no
nearer by a centimeter. Upstairs is (2.5, 1), (0.8, 1), (0.8, 6.8), (5, 6.8) and (5, 4.5),
downstairs the same back to (5, 4), the loop (7, 4) and (5, 4), and the tour goes through both doors
to (19, 1.5) by the window and back, then up and down. The play script waits 240 frames at a
sixtieth of a second each, walks upstairs and reads 3.3 to 3.6 m, cuts `cut window 1.4 1.2 4 1`
through the upper room's north wall after `select box 0.2 3.4 7.8 9.8 6.6 7.8`, reads more faces,
its four sides selected and the collider made once more, turns 94 frames to take the window's
picture, opens and closes the window on Tab, saves on F5 and finds the window's line in the file,
walks downstairs and the tour, and puts the level's file back. The soak's round walks the loop, cuts
that window and takes it and the selection back, and opens and closes the window.

Shared: F1's hidden scope and F3's overlay, in the commit carrying this line, item 49's first half,
for item 78. `bool Ui.BeginHidden()` opens the scope and answers whether it draws this frame, false
while `Ui.Hidden`, and `Ui.EndHidden()` closes it, refused where none is open, the scope counted a
thread. A program's own ImGui windows go inside an `if` on the answer. The interface nodes made
inside it with `Ui.SpawnNode` and `Ui.SpawnText` are recorded and hidden by the engine while
`Ui.Hidden` holds, given back the visibility they had after, and what is made under them follows
them. `Ui.Hidden` is settable, and `Ui.HiddenKey` is F1, `null` for none, turned over on a press
while no ImGui field has the caret, the press left for the program. `Debug.OverlayShown` is
settable, and `Debug.OverlayKey` is F3, `null` for none, turned over as it is let go where no other
key went down while it was held, and not while a field has the caret. The overlay is the engine's
own interface nodes over everything else (a global z-index of 1,000,000), the pointer passing
through. It is two columns with a gap of 2 pixels at the window's edges, each line light gray (0.88)
text of 15 pixels on black at half, padded 3 pixels at the sides and 1 above, an empty line a gap of
a line's height, and its words are written again four times a second while it shows. The left column
says `BevyCSharp <version> (<editor|render|headless> bridge, ABI <n>)`, `<fps> fps, <ms> ms a frame,
frame <n>`, a gap, `XYZ <x> / <y> / <z>` to three places, `Facing <north|northeast|...> (towards
<positive|negative> <X|Z>), heading <deg>, pitch <deg>` (north is -Z and the heading turns toward
+X), `Field of view <deg> degrees` in words since the engine's font has no degree sign, or `Field of
view none, the projection is orthographic`, or `No camera`, then a gap, `Entities <n>` and `Meshes
<n>, materials <n>, images <n>, sounds <n>`. The right column says `.NET <version>, <architecture>`,
`<OS> (<kernel>)`, `CPU <model>, <physical cores> cores, <threads> threads`, `Memory <process MB> MB
of <installed>, managed <MB> MB`, a gap, `GPU <name> (<device type in words>), <backend>`, `Driver
<name> <version>`, `GPU memory <allocated> MB of <reserved> MB reserved` or `GPU memory not reported
by its backend`, `Display <w> x <h>, scale <s>` or `Display offscreen, <w> x <h>`, a gap, `Meshes in
view <n>, triangles <n>`, `Lights <n> directional, <n> point, <n> spot, <n> with shadow maps`,
`Lighting ray traced, its global illumination Solari's` or `Lighting rasterized, no global
illumination`, and `Meshlets on` where they are. The camera is the active one of the highest order,
a 3D one first. The OS, kernel, CPU, cores and installed memory are read once a process through
sysinfo, and the bridge's adapter description gains the driver's version as its fifth field. A mesh
whose data went to the render world counts with no triangles. The editor turns the engine's F1 off,
since its F1 opens its menu and its panels are screens, and Bevy's `debug_picking` example turns the
overlay's key off for its own F3. The feature test's panel moves to F4, not while F3 is held, F3
with F4 staying its mode's chord, its overlay gives way to the engine's, and its HUD, the readout
and the mode's label, goes inside the scope. This engine has no upgrading page, its release notes
being the commits.

Shared: the overlay's strip, its profiler and its sections, in the commit carrying this line, item
49's second half, for item 78. Every frame's time is kept in a ring of 240 while the overlay is
hidden too, the first frame's, which has none, left out, so it opens on the last four seconds. The
left column's first two lines stand over the strip, an image 240 by 48 pixels drawn again every
frame while the overlay shows, a column a frame with the newest at the right, on black at half with
rows of gray (160, 160, 160 at 160) at 60 and 30 frames a second, a bar green (80, 220, 100) to 60,
yellow (240, 200, 60) to 30 and red (240, 80, 70) past it, half a millisecond allowed over each,
its top 50 milliseconds and a bar a pixel at least. Beside the strip are `Now <ms> ms, average <ms>
ms, 1% low <fps> fps`, the one percent low the slowest hundredth of the frames held, one at least,
their times averaged and taken as a rate, and `GPU <ms> ms a frame, <n> passes added`, the passes
whose names have no slash, or `GPU time not measured, Config.GpuTimings measures it`, or `GPU time
not measured, the adapter has no timestamp queries`. The camera's lines and the rest follow under
the strip. F3 let go with Shift held turns `Debug.ProfilerShown` over, showing the overlay with it,
and F3 alone turns the overlay over and hides the profiler with it, so F3 shows the overlay again
without it, as Minecraft's does. The profiler adds to the right column a gap, `Passes, the slowest
first` and a line a pass, `<name> GPU <ms> ms, CPU <ms> ms` to two places, `none` for one not timed,
ordered by the GPU's time and then the CPU's, 24 of them and `and <n> more`, or `Pass timings not
measured, Config.GpuTimings measures them`. `Debug.AddOverlaySection(string name,
Func<IEnumerable<string>> lines)` adds a section, or replaces the one of its name where it stands,
and `Debug.RemoveOverlaySection(name)` answers whether there was one. The sections last while the
process does and are asked four times a second, from a copy of the list. Each draws in the left
column under the engine's lines as a gap, its name and its lines, and one whose lines throw says
`<name>: <exception type>, <message>` in their place, logging nothing. Bevy's FPS overlay, where the
app asked for it, has its text and its graph turned off while the engine's overlay shows and given
back as they were. The first half's empty lines had no height, since a line made for a gap was
never written, and in this commit a line's first writing sets its backing either way, so each gap
is a line high. The feature test adds its `memory` command's names and numbers as
a section named `What the program holds`, a few pairs to a line of about sixty letters, and its
debug page gains `Profiler (Shift+F3)`.

Shared: the scene's distance field, in the commit carrying this line, item 50's first half, for the
row of 3DEngine's global illumination. `Render.SetSceneField(cascades, cellSize = 0.25f, budget =
1)` takes 3DEngine's settings, cascades clamped to 8 and 0 for none, the setting the running app's.
The plan is 3DEngine's `SceneFieldPlan` with its constants, 64 cells, a band of 4, settling after 8
frames or 1,024 after a move, 256 boxes, bricks of 4, a corner on 8 cells, a lead of 0.35, and its
tests on the CPU, a mesh being the number the bridge gives it and a generation that moves on when
Bevy says its data changed or it went, where 3DEngine's is its array of vertices. Its parts are not
taken, so a moving or skinned mesh is stamped as the box around all of it. A frame that drew a mesh
twice in one place, the same mesh, matrix and material, is told (`DrewTwice`), and the field then
gathers each mesh once, so the frames after are read by place: one such pair among the feature
test's 261 meshes read every frame whole, 0.68 ms of the plan's in a build that is not optimized,
and 0.18 ms read by place. The kernels are 3DEngine's field_splat, field_resolve and field_stamp,
their distances alone, the paint, faces, emission and lent light coming with the cascades of probes
that read them, changed where the bridge's bindings ask: named values for push constants,
`Atomic<uint>` and `.min` for `InterlockedMin`, the still field read as a texture in the stamp since
an image of half floats is written and not read and written, the info a structured buffer, and the
resolve clearing each word it reads for the next build where 3DEngine fills the buffer before it.
The images start at 10,000 as 3DEngine's are cleared. The bridge gathers the meshes drawn, each
entity visible down its hierarchy whatever a view's frustum leaves, without NotShadowCaster, with
its matrix, its standard material's base color and emission, whether it culls no back faces, and
Bevy's bounds or the mesh's own, and the camera the overlay names, and pools the triangles of the
meshes a frame's builds read in one buffer, sixteen bytes a corner with its color and open edge,
made again with the meshes asked for where the rest would not fit. The field is built from the
engine's tick at the frame's start, so a box follows its mesh a frame behind, and nothing is built
until the three kernels have compiled. `bcs_field.slang`, a module of the bridge, holds the layout
and 3DEngine's reads of it in this bridge's names, `fieldDistance`, `normal`, `hold` and the
rest. `gi.state` says the lighting, the field's line, each cascade's corner and cell, what the frame
built and stamped and the GPU memory, `field.set` sets it, and the overlay's renderer lines carry
`Scene field 4 of 4 cascades built, cells 0.25 m, 128 m across, meshes 260, still 234, boxes 26`
or `Scene field off`. A stage handed as source with a path beside it is named by the path, so the
kernels' GPU times read `shader field_splat`, `field_resolve` and `field_stamp`. In the feature
test on the RTX 4070 a build of a cascade of 234 meshes took 0.55 ms of the GPU, a resolve 0.03 and
a stamp of 26 boxes in 418 bricks 0.013, and the engine's tick 0.78 ms of the CPU with the field
where 0.17 without it, in a build that is not optimized. The tests read the field back on the
device: a box's distance at every cell within 0.01 and the band's beyond, a tunnel between walls a
cell apart clear along its middle, a box moved each frame stamped where it stopped and the cells it
left at the band, and four cascades built one or two a frame as the budget says. The view of the
field, the window of its slices and the contact shadows come in the second commit.

Shared: the field's contact shadows and its window, in the commit carrying this line, item 50's
second commit, for the row of 3DEngine's global illumination. `Render.SetSceneFieldShadows(
FieldShadows)` takes `PastShadowMap`, the default, `Everywhere` or `Off`, where 3DEngine's
`ContactShadows` defaults to on everywhere, and `Render.SetContactShadows` here is Bevy's own
screen-space ones, so the name differs. Each directional light is traced from a cell and a half out
along its way through the field, 32 finest cells at most, softened by eight times the nearest
distance over the length run, faded across the field's last band, as ao.slang's `sunReaching`. The
light it takes back is a draw over the camera at AfterOpaque blended by adding less than nothing,
from the camera's G-buffer, Bevy's `directional_light` ported to Slang (Burley's diffuse, GGX with
the correlated Smith visibility, Schlick's Fresnel with the f90 Bevy uses, the multiple scattering
from the polynomial F_ab, since the DFG table is not bound to a draw), times the shadow map's own
share, times the view's exposure. Past the map, the map holds a point by its last cascade's far bound
and its overlap, and the field takes the rest. Clearcoat, anisotropy, the atmosphere and light
textures are not ported. While the shadows are on, the camera the field lies around draws Bevy's
materials deferred, its depth and G-buffer asked beside whatever the program asked, and keeps its
picture in floats, Bevy's `Hdr` given it where it had none and taken off after, since a picture of
eight bits clamps the light taken back to nothing before the blend. A program's own camera draws and
prepass stay, the engine's kept apart and handed to the bridge with them. On the RTX 4070, a pillar's
shadow on the ground under a low sun with no tone mapping differs from the map's own by 1.2 levels on
average on the pixels both darken, the field covering 73% of the map's shadow, its tip softer than the
map's, and the lit ground by 0.4; past the map, a picture the map shadows is the same with the field
on. `Render.DrawSceneFieldWindow()` draws the field's settings, the contact shadows' choice, a slice
across X, Y or Z of a cascade through the eye or a chosen cell, sampled between cells, red inside,
black within a sixth of a cell of a surface and gray outside in bands a cell apart, and what
`gi.state` says, through ImGui; the feature test's debug page and `feature.field on` show it, and
the guide's capture is its slice at the gallery from above in the second cascade. The bridge's eye
gains the camera's entity, ABI 302.

Shared: the Slang modules' plain names, in the commit carrying this line, item 56, for 3DEngine's
row of the shader language. Every module of the bridge, bcs, bcs2d, bcs_ui, bcs_sprite,
bcs_compute, bcs_scene, bcs_ray, bcs_field and bcs_pass with its deferred part, drops its public
namespace, and a shader uses its names plainly after the import, functions in camelCase, types and
constants in PascalCase and globals in camelCase, as 3DEngine's modules write theirs. Each helper is
named for its module and its work, the 3D material's `meshView`, `meshGlobals`, `meshTransform`,
`meshWorldToClip`, `meshLocalToWorld`, `meshStandardVertex`, `meshSurface`, `meshLight`,
`meshFinish`, `meshLit`, `meshDeferred`, `meshDecals` and `meshIrradiance`, with the types
`MeshView`, `MeshVertex`, `MeshVertexOutput`, `Surface`, `Deferred`, `Mesh` and `DecalMap`; the 2D
material's `mesh2d` the same; the interface's `uiView`, `uiTransform`, `UiVertex`; the sprite's
`spriteSampleFinalColor`, `spriteFinalColor`, `spriteImage` and the flags `SpriteFlipX` and the
rest; compute's `computeGlobals` and the raw buffer's `bufferElement`, `bufferStore` and
`bufferCount`; the scene's `Instance`, `Material`, `PoolVertex`, `PoolMesh`, `instanceToWorld`,
`instancePosition`, `poolCorner`, `intersectTriangle`, `intersectBox`, `irradianceTexel` and
`IrradiancePositiveX` and the rest; the ray scene's `rayTrace`, `rayVisible`, `RayHit`,
`RaySurface`, `RayMaterial` and the bindings `rayTlas` and the rest; the pass's `passView`,
`passGlobals`, `passPicture`, `passDepth`, `passLights`, `passUv`, `passSample`, `passLoadDepth`,
`passDepthAt`, `passWorldPosition`, `passViewDepth`, `passDirectionalShadow`, `passPointShadow`,
`passSurfaceOf`, `PassSurface`, `PassDeferred` and the rest, its types `DirectionalLight`,
`PointLight`, `Lights` and `SceneInfo` plain; and where both engines hold the same thing 3DEngine's
own names, `fullscreenTriangle` and `FullscreenOutput` for the triangle over the view, and for the
distance field `FieldInfo` (3DEngine's `SceneFieldInfo`), `FieldCascade`, `fieldOn`, `fieldHold`,
`bandHold`, `fieldCascade`, `fieldCoarsestBuilt`, `fieldDistance`, `fieldDistanceIn`, `fieldNormal`,
`fieldCell`, `faceBits`, `faceWay`, `FieldBand` and `EyeClear`. No two modules declare one name,
which a test holds beside the one that no module declares a namespace. The bridge binds every
module global by its slot, so nothing on its side changed but the text it writes out, and the
modules' hash with it, which makes every cache entry compile again once.

Shared: the denoiser's readings and its demodulation, in the commit carrying this line, item 54's
readings, for 3DEngine's denoiser over its ray-traced pass. `build/denoiser-readings.py` draws each
of the feature test's views 256 times standing still with the denoiser off, their mean the
reference, and reads one grainy frame and one filtered frame as their root mean square distance
from it in levels of 255, eight of each. A strafe of thirty frames at a tenth of a meter a frame
past a pillar is read over the pixels the pillar left, found by casting each pixel into the room
from where the camera stopped and cutting the segment to each earlier eye by the pillar, a band as
wide as the pillar's parallax against the wall behind it rather than all it passed over. Divided
by the base color alone, the checkered ground seen far off came out further from its mean than a
grainy frame, 7.45 against 5.79, since a dark surface seen at a slant sends back more by its
specular reflection than by its color and the filter spread that among the lighter squares.
Divided by the diffuse color plus F0 times A plus B of the split sum at the angle it is seen at,
from Bevy's polynomial, every view is nearer, 0.9 to 4.9 against 5.8 to 14.3, and the strafe's
pixels 13 against 31. The node takes 3.6 ms at 1920 by 1080 in Bevy's Solari scene and 5.7 ms with
every pixel filtered on an RTX 4070 Laptop GPU, about what Solari's lighting takes, held within 7 ms
by a test.

Shared: the denoiser's stages timed one by one and brought down from 3.6 to 1.9 ms at 1920 by 1080,
in the commit carrying this line, item 59, for 3DEngine's denoiser over its ray-traced pass. Each
stage is a span of its own under the node's, so render.timings gives the demodulation, the
temporal stage and each à-trous pass, and every stage proved to cost about what moving the picture
through memory costs, the five à-trous passes half a millisecond each at 25 taps of the depth, the
G-buffer's sixteen bytes and a 32-bit light. The demodulation writes each pixel's distance and
packed normal in eight bytes, which every tap reads instead, the light, the moments and the history
are half floats, the variance is estimated in the temporal stage and the last à-trous pass
remodulates, seven dispatches where there were nine, each pass 0.25 to 0.29 ms. A half float loses
the mean of the square less the square of the mean wherever the light varies by under a few
hundredths, which left a view of grass 1.7 levels further from its mean, so the moments are kept as
the mean and the variance about it, updated as (1 - a) v + a (1 - a) (x - m)^2, and the filtered
light carries the root of its variance, which the passes take down a tenth at a time. A workgroup
whose light all varies by under a hundredth skips the pass, which gained a few hundredths of a
millisecond in the later passes. Taps read through workgroup memory for the steps of one and two
were slower, 2.6 ms, the eight kilobytes lowering every pass's occupancy where the texture cache
held the overlap without them, and the weight's three factors as one power of two gained nothing,
so neither is kept. The seven views and the strafe read within a level of before. With every
pixel filtered the node takes 2.5 ms, each early pass 0.55 ms at about 65 texture fetches a pixel,
three quarters of the rate the GPU's texture units fetch at, so a further cut is in fewer fetches,
the guide and the light read as one or through buffers, rather than in fewer bytes. The budget test
takes the least of 600 frames' own measurements, since a pass's time counts another program's work
on the GPU and read 8 ms smoothed beside a game.

Shared: the field's colors and far cascade and global illumination's world probes, in the commit
carrying this line, item 51's first part, for 3DEngine's rows of the field and of global
illumination. The splat paints each cell the color of the triangle whose code it kept and the
resolve writes it and the face's way into images laid out as the field, with the still meshes' own
for the stamp; `SetSceneField`'s `farCascade` adds 3DEngine's far cascade, its cell from
`FarCellFor`, held to the far plane the bridge reads from the camera's projection, `FieldInfo`
taking nine cascades and the far one's place. The probes are 3DEngine's trace, merge and gather
with their mends to date, the glow lights, the emissive surfaces' light and the environment map's
sky left for the emitters' part; what differs lies in the bindings, a probe's held light and the
partial sums storage buffers, since WGSL reads and writes an image of half floats in one dispatch only
where the adapter allows it; the images written alone are Slang's `WTexture3D`; and the trace's sum
of the 27 probes around one is taken by every workgroup and kept where the probe's share asks for
it, so every barrier is reached in uniform control flow. The bridge gathers the lights in Bevy's
units times the camera's exposure, the sixteen lamps nearest the camera, a spot light's cone as
Bevy's falloff from its inner to its outer, and the sky as the camera's ambient light or the
world's. Read on the RTX 4070 Laptop GPU, a first-cascade probe toward a black wall 200 wide forty
units ahead holds 1.12 of a sky of pi with the far cascade and 3.14 without, where 3DEngine's read
1.14 and 3.14; a probe a meter before a sunlit red wall gathers 6.77, 1.17, 1.17 on its face toward
it; in the feature test's Cornell box a cascade's trace takes 0.33 to 0.36 ms, its merge 0.04 to
0.10 and its gather 0.03, the means of the program's dispatches, so `Low` about 0.9 ms and `High`
about 1.8 with sixteen lamps marched from each hit.

Shared: global illumination's light on Bevy's surfaces, in the commit carrying this line, item 51's
second. A pass after the opaque surfaces takes back the diffuse share of Bevy's ambient term, worked
out from the G-buffer with `F_AB` at a perceptual roughness of one, and adds the surface's diffuse
color times the light between the probes around it times its occlusion, with 3DEngine's sky over a
ground past the last cascade and each directional light's share of the ground's light through its
shadow map. The probes' sky is the ambient light's brightness times 0.45, the share Bevy's ambient
term returns from a white surface facing the view, so an open floor reads 92 of 255 with the pass
and 92 without, and a white block's side by a sunlit red wall reads 237, 136, 132 with it and 75,
75, 75 without, both held by tests. On the RTX 4070 Laptop GPU in a Cornell box at 1280 by 720 the
probes take 0.9 ms at `Low`, 1.2 at `Medium` and 1.4 at `High`, and the pass 0.23 ms at its least
and 1.1 at its median over 600 frames; in the feature test the pass's span reads no GPU time, where
Bevy keeps no more than 256 timestamps a frame. The path-traced reference with `gi.reference` and
`gi.compare` follows in a commit of its own.

Shared: global illumination's path-traced reference, in the commit carrying this line, item 51's
second part's reference in a commit of its own. 3DEngine's gi_reference over a ray scene of the
meshes the camera draws, built on the geometry pool with the engine's instance and material buffers,
run as an engine dispatch on the camera in SPIR-V, the first face lit as Bevy lights it and every
face after as the probes do, `gi.reference` and `gi.compare` as 3DEngine's with the camera's light
before tonemapping read through a copy that also reads the picture's size. The departures are that a
texture counts as its base color, a mesh's vertices' colors are left out, the sky is the light from
all around, and a point light's shadow starts a tenth of a unit from it as Bevy's maps do, since the
carried lamp's bulb would otherwise shadow its whole room. The bounce rooms are `BounceRoomsTests`
with 3DEngine's rule, the views 0.11, 0.12, 0.25, 0.11 to 0.12, 0.02, 1.00, 0.05 to 0.07, 0.06 and
0.30 from their references, where 3DEngine's read 0.12 to 0.14, 0.09, 0.20 to 0.24, 0.27 to 0.30,
0.02, 0.22, 0.11 to 0.12, 0.11 to 0.12 and 0.05 to 0.06, the strip's and the glowing blocks' wide
until the emitters come, and the Cornell box's floor 2.5% under its reference at `High`. Bevy's
deferred lighting shows a material's emissive light unexposed, its alpha lost in the G-buffer, so
the feature test's lamp panel reads 40,053 where its reference reads 49 on a camera the field's
shadows or global illumination make deferred, which is this engine's alone.

Shared: global illumination's emitters, in the commit carrying this line, the first part of item
51's third. 3DEngine's emission painted into the field beside the colors, its lending for an emitter
thinner than a cell and its glow lights (`GlowLights`, `bcs_glow.slang`), touching boxes joined, the
carried cells marked so a probe's ray leaves them, the four brightest at each pixel of the pass and
the two brightest at a probe's hit. The gather gives the emission in the picture's units as Bevy
shows it, the exposure taken by the share the emissive alpha sets. The departures are that an
emitter is its box whole with every face emitting, since the field's gather hands over no vertices,
and that the pass marches to a light's middle, where 3DEngine's pixels with no screen probe near
march to the halves of its faces, since the halves read alike in the bounce rooms and cost 2.0 to
3.5 ms against 1.3 at 1280 by 720. The square's closed form clips a face an edge at a time with its
loops unrolled, since with small arrays indexed as rolled loops ran it cost 2.7 ms of the pass
alone, which 3DEngine may meet where its model pass reads glow lights. In the bounce rooms the
strip's view reads 0.18 to 0.19 where it read 1.00 (3DEngine 0.22), the glowing blocks' 0.05 where
0.30 (3DEngine 0.05 to 0.06), the Cornell box's 0.10 to 0.11 where 0.11, the rest unchanged. The
Cornell box's floor reads 13.2% over its reference, 15.5% with the panel dark in both, so the
probes' indirect light is over there, which the previous commit's 2.5% under hid behind the panel's
missing light; its bound moves from 3% to 16%. The environment map's sky and the glossy term follow.

Shared: global illumination's sky from the camera's environment map, in the commit carrying this
line, the second part of item 51's third. A dispatch on the camera copies the map's light along each
way onto an octahedron of 64 texels a side, times the exposure, blurred as a quarter roughness blurs
it (`gi_sky.slang`), since the probes are traced before the cameras draw, and the last cascade's
rays that meet nothing take it beside the ambient light, the frame before's. The pass takes back the
diffuse light Bevy's environment_map_light gave each surface, its multiple scattering and the
diffuse color times what the dielectric's scattering leaves, and adds the map's light from above to
the sky over a ground; the reference's paths that meet nothing take the map along their way. Under
the tests' cubemap filtered by Bevy an open floor reads 70, 71, 116 with global illumination on and
67, 67, 115 off, and a closed room's floor 4, 3, 13 on and 67, 67, 115 off, Bevy's own lighting
lighting it through the walls. The engine's dispatches on a camera are kept by owner, so the
reference and the sky's copy share a camera. The glossy term follows.

Shared: the glossy term, in the commit carrying this line, the last part of item 51's third.
3DEngine's present one in the pass, Bevy's reflection of its ambient term and environment map taken
back and given again, traced below a roughness of 0.5 through the picture's depth in 3DEngine's
steps and reaches and past it through the field, a hit on the picture taking the frame before's
picture, which the camera keeps copied before tonemapping as an engine image with history, and a hit
past it shadeHit's light, fading to Bevy's reflection from a quarter of the roughness to a half, and
Bevy's reflection times skyAt's share of the sky along the mirror way. The departures are that the
frame before's depth is not read to ask whether the frame before showed the surface the reflection
met, and the frame before's picture is read at its own size where 3DEngine blurs it down its mips by
the roughness. A mirror floor shows a sunlit red wall at 244, 115, 107 on and 2, 2, 2 off, and a
closed room's rough metal floor under an environment map reflects 0, 0, 14 on where 78, 58, 119 off.
The pass takes 1.5 ms with the mirror floor and 1.3 with every surface rough at 1280 by 720, and the
bounce rooms read as before, their surfaces at a roughness of 0.5. The rooms' test waits three
frames of ready pipelines before it reads the picture, since the larger pass's pipeline came after
the probes' ninety frames in some runs and those read no pass at all.

Item 61's colons, in the commit carrying this line. Each hit of STYLE.md's two checks was read, the
labels, the named arguments, the quoted strings and the component labels of the shaders' comments
left, and 36 joints recast, in RENDERING.md, docs/components.md, AGENTS.md's three on the owner's
word typed in this session, the cheat sheet's three through the summaries it is written from, and
the comments of the code, the scripts holding none. ASKS.md's two hits are left to the reviewing
session, since AGENTS.md has this session write nothing there. STYLE.md's grep misses a colon after
a closing tag, `</c>:` or `/>:`, which twenty XML summaries held, and `[a-z)\`>]` in its class finds
them.

Item 60 in the commit carrying this line, on choice (A). The bridge gives Bevy's
`emissive_exposure_weight` the emissive's alpha, which `MaterialSettings.Emissive` defaults to 0 as
Bevy does, and reads it back, into the material buffer and into the field's gather from that field;
the callers that relied on the unexposed look, the examples ported from Bevy, Swarm, Courtyard's
scene, the editor's script, the console's glow, the modeling slots and the tests, pass 0, and the
editor's emissive color leaves the weight as it was. The feature test's emitters, of thousands of
nits, keep 1 and are exposed, the Cornell box's panel at about 40. `render/mended.rs` mends Bevy's
`deferred/functions.wesl` to apply the weight as the G-buffer is written, refused and said where
Bevy's text moves, and `DeferredEmissiveTests` holds an exposed panel deferred against forward and
within 2% of its reference, 38,165 against 38.2 unmended. STYLE.md's two checks take `>` in their
class, and they find no hit beyond the ones item 61 recast.

Shared: the field's budget in triangles a frame, 3DEngine's item 74, in the commit carrying this
line, the first part of item 52. The plan's builds and the tests are 3DEngine's, the still meshes
within a cascade splatted nearest the eye first up to the budget, a mesh at least, a build of more
going on over the frames after with its cascade read as it was last built and its meshes not yet
built in stamped as boxes, a mesh that settles taken in and a splatted one that leaves starting the
build again, and a finer cascade coming first. The departures are that the bridge counts each mesh's
triangles into the gather's record once a generation, so the budget weighs a mesh before it is
pooled, and that the resolve clears the codes it reads, so a build's first part clears them only
after a build left unfinished, by a dispatch of the resolve that clears alone. Five hundred sections
of 1,024 triangles drawn from the first frame splatted 855,040 triangles in one frame without a
budget, the slowest splat dispatch 9.3 ms, and under the default of 32,768 over 27 frames, the
slowest 1.4 ms, the field the same to the cell. `SetSceneField` takes `triangleBudget`, `field.set`
a fifth word, and `gi.state` says what the frame splatted and what waits. The screen's probes and
the glow-only pass follow.

Shared: the screen probes, the glow-only pass and the edge leak's mend, in the commit carrying this
line, the second part of item 52. The probes are 3DEngine's, dispatches on the camera after its
prepass writing the camera's images with their history, the trace, the blend with the probes around
and the frame before's, and the glow lights' light on images half the picture's size, which the
pass reads beside the world's probes; `Render.SetGlobalIlluminationScreenProbes` and `gi.screen`
leave them out, when they stand for the glow lights alone where one is lit. The departures are the
camera's depth read whole, what a probe sees of each glow light in four images of codes where
3DEngine keeps a buffer, a face's way in the depth's march found from the depth, the pass's plane
from the depth's nearer neighbors, and no command reading one probe's rays. The bounce rooms keep
their bounds read with the probes traced and left unread, the old path, since left out they stand
for the glow lights in every view and read the glowing blocks' room 0.06 to 0.07 where 0.049; with
them on the Cornell floor reads 3.5% over where 12.8% and every view lies within 3DEngine's bound at
160 by 90 or this engine's own where that is wider, the corridor's 0.25 either way. The blotches are
3DEngine's term, its mend of a hit's mean over a cell in `glowLightsAt` here too, and in the feature
test's Cornell box at High the probes leave streaks a tile wide on the back wall, saved beside the
box without them. The edge leak's mend holds a pinched cell inside in the resolve, its clear a
dispatch of its own after it, the floor past a wall's two halves 15 levels at its brightest unmended
and 3 mended, as with the edge closed. The probes take 1.4 ms at `Low` and 1.6 at `High` in a Cornell
box at 1280 by 720 with one glow light, their blend 0.02 and the glow texels 0.12, the pass 0.54 at
its median where 0.73 to 0.83 with them left out. The stepped edges need no mend, the pass marching
to a light's middle.

Shared: the sky below the horizon in the fog's color, in the commit carrying this line, item 64's
second part, the row 3DEngine has at `bfaa056b`. The fog is the engine's own draw over the picture
before tonemapping, a layer over the ground thinning by e every so high, its color along each way
the sky's a few degrees above the horizon read nearly sharp from the camera's sky-lit environment
map, and below the horizon that color darkening to a share of it over a span of degrees, where
3DEngine blends the fog's color over a few degrees above the horizon and takes a horizon color a
program asks for. The scene's light is the environment map's as before, lit by the atmosphere alone,
and a camera drawing several samples a pixel draws no fog, its depth unread.

Shared: the look in the tonemapper's place, in the commit carrying this line, item 64's third part.
The frame is 3DEngine's as its composite runs it, an exposure, the engine's curve with the knee of
`8ae947ee` by default or the BSL curve with its four numbers, saturation and a tint in linear light,
and contrast about the middle once encoded, held within two levels of a model over SHARED.md's ramp,
and the model held to TonemapCurveTests' seven figures within a thousandth; the vivid preset is the
block game's 0.9, 1.3, 1.05 and 1 at an exposure of 1.6, saturation 1.4 and contrast 1.12. The
departures are Bevy's bloom added before the look, not divided by a count of levels; Bevy's vignette
before the look where 3DEngine darkens the corners after the contrast; Bevy's dither of half a level,
which 3DEngine has none of, on by default and off in the tests; and Bevy's auto exposure, which its
tonemapper applies and a look leaves out. The ramp through a look could be a reference picture kept
in both engines, as the eight tonemappers' are.

Shared: the look keyed by the sun's height, in the commit carrying this line, item 64's third part
second commit, this engine's own, which 3DEngine may take beside its look. `LookCycle` keeps looks at
heights of the sun in degrees, the look between two keys each number moved evenly with the height
and the curve and dither the nearer key's, the lowest and highest held past the ends; `Day()` is
the vivid look at 30 degrees and above, saturation 1.48 and 1.55, contrast 1.14 and 1.16 and a warm
tint at 8 and 2, saturation 1.2 and a blue tint at minus 6, and saturation 1 and a cool tint at minus
18, every key on the vivid curve and exposure. A look given again to a camera that has one changes
its numbers alone, so a program gives it every frame. The feature test's Look row has Day, its
default.

Shared: the fog in froxels and the sun through the atmosphere for any shader on a camera, in the
commit carrying this line, item 69's first phase. A camera's froxels, 80 by 45 by 64 at Low, 120 by
68 by 96 at Medium and 160 by 90 by 128 at High, sliced deepening from half a meter to the distance
asked, are filled after the prepass with the layer's density, a three-octave value noise the wind
carries and up to eight boxes, and lit by the sky's color along the way, the closed form's, split
into the sky's mean light from above and below and the sun's share past it, which the brightest
directional light's shadow cascades take away, with point and spot lights scattered evenly; each
froxel's point moves through its slice by the golden ratio's fraction and blends at 0.12 with last
frame's where the point stood, and the slices are added up from the eye in closed form a slice, as
Hillaire's account of Frostbite has it. Over the sky only the shadows' darkening, the lamps' light
and the boxes are laid, since the layer's unshadowed light along a way is the sky's there, the boxes'
share taken away worked out in closed form along the way through each. Bevy's transmittance
lookup texture and the planet's radii reach every camera shader as `passAtmosphereTransmittance` and
`passHasAtmosphere`, for the clouds next. 3DEngine's haze is a dome and a terrain term, which may take
the froxels' split of the sky's light.

Shared: the engine's clouds, in the commit carrying this line, item 69's second phase, which 3DEngine
may take for its own sky. A shell from 1,500 to 4,500 meters over a planet of earth's radius is
marched at half the picture's size after the prepass, 48, 72 or 128 steps along a way and 4 or 6
doubling toward the sun, through a 128 cubed Perlin-Worley and Worley volume, a 32 cubed Worley
detail and a 512 square weather map, all tiling and made once on the GPU; the density is Schneider's
for Horizon Zero Dawn, coverage a soft threshold over the weather, a profile thinning over most of
the height so the threshold narrows each cloud into a dome, the detail wearing the edges. The sun's
light comes through Bevy's transmittance lookup at each point, the march toward the sun at half the
density, Wrenninge's four octaves with a lobe of 0.8 forward and minus 0.3 back, and the sky's light
from above and below; the air in front fades a cloud toward the environment map's color, which holds
no sun disc, a cover under a hundredth through is made whole, and the history is blended at 0.12
except where the cover differs, so the disc does not burn through a cloud's lagging edge. The draw
lays it over sky pixels before the fog. A flat overcast deck waits on phase (d)'s presets.

Shared: the clouds' shadow, in the commit carrying this line, item 69's second phase, second commit.
A second entry point of the clouds' kernel marches 16 steps from the ground toward the sun through
the shell for each texel of a 512 square laid in the sun's own axes, 16 kilometers wide about the
camera and moved a texel at a time, at 0.06 ms, and the bridge lays it over the sun as Bevy's tiled
directional light picture, giving the light's global transform the square's scale and middle after
Bevy works it out each frame and leaving its turn the program's, through
`bcs_render_set_directional_cookie` at ABI 307; the froxels' fog reads the same square for the sun's
share. The crate built its cookie of 128 texels on the CPU at 1.5 to 20 ms and owned its sun.

Shared: the sky's clock and the night sky, in the commit carrying this line, item 69's third phase,
which 3DEngine's day sky may read beside its own. `SkyClock` puts the sun by the hour, the day of
the year and the latitude, its declination from the axial tilt and its right ascension along the
ecliptic, the moon a synodic month behind the sun along the ecliptic without its five degrees, the
stars by the local sidereal time, the sun's hour angle and right ascension, with tests holding the
equinox noon at 90 less the latitude, the solstices' noons within a degree, the full moon opposite the
sun and lit whole, the pole star still and a star back after 23.9345 hours. `Render.SetNightSky`
adds over sky pixels before the clouds, by its owner's order, stars hashed on a cube's faces, one cell
in five of 160 a side, a twentieth power of a number for their brightness, under half a pixel wide
and twinkling near the horizon, the galaxy's band about the galactic pole with its bulge and dark
lanes, and the moon a sphere lit from the sun's way with earthshine and noise seas, all through the
atmosphere's transmittance, the stars and galaxy coming out as the sun goes from three degrees up to
nine down. The clouds, the fog and the shadow now take the directional light that gets most through
the air, so the moon lights them at night.

Shared: the weather state, in the commit carrying this line, item 69's fourth phase, which
3DEngine's sky may read beside its own. `WeatherState` eases its `WeatherConditions` toward a target
by half the way every 30 seconds, the target one of the crate's fifteen presets, as the crate sets
them, or a forecast from the clock's day and hour, a storminess of three octaves of value noise over
1.5 weather systems a day scaled by a climate's mean and swing and read up a ladder of seven presets
from clear to a thunderstorm, the temperature by the season and the hour, the rain falling as snow
below a degree and a mist at dawn on calm days, the same for the same seed. The wind gusts by the
conditions' share about their speed and wavers about their direction, and the ground wets in rain,
dries over some 400 seconds and whitens under snow. `Render.SetWeather` lays the conditions over a
camera's clouds and fog every frame, the clouds' cover, base, top, density and wind and the fog's
visibility in log space toward 80 meters, and a camera whose settings stand otherwise takes the
numbers alone, so its images, dispatches and draws are not given again. The clouds gain `Stratus`, a
flat deck low in the layer with a wide threshold carrying the body's noise, which the crate's
overcast and storm are, and the deck reaches past where the heaps have any cover, or it ends along
the weather map's texels in straight steps.

Shared: the rain and the snow, in the commit carrying this line, item 69's fifth phase. A compute
pass on the camera moves 16,384 to 65,536 particles a frame in two boxes held in the world about it,
two in three in a near box a quarter of the far one's reach, wrapped at their sides so walking moves
through the rain. A drop falls at the speed Atlas's fit gives its size, the sizes spreading wider the
harder it rains, and a flake at about a meter a second with a flutter, both on the wind, each
particle's kind following from its number and the rain's share of the fall every frame, so a change
of weather turns the drops in the air to flakes. Those in front of the camera are counted with an
atomic into an indirect draw with the light each sends to the eye worked out once, the brightest
light through the air with its shadow and the clouds', the environment map's and the lamps', and are
drawn as streaks over a thirtieth of a second capped at 25 centimeters and flakes over a 120th, a
pixel and a half wide at least and fainter by how much narrower they are to the power 0.6. A camera
of its own looking straight down draws the scene's depth over 96 meters into a map of heights 256 a
side, switched on through Bevy's reflected camera for a few frames once the camera has moved 12
meters, and a particle ends below that height, splashing into a ring of 4,096 where it lands within
15 meters. The bridge gains a frame point after temporal antialiasing and before motion blur, bloom
and depth of field, at ABI 308, since the antialiasing's history kept a speck moving with no motion
of its own at a tenth of its strength. Some 0.05 ms of the GPU at 1280 by 720.

Shared: the thunder, in the commit carrying this line, item 69's sixth phase. Strikes are a Poisson
process at the weather's rate a minute, the gap from an exponential spread, landing between a nearest
and a farthest distance as evenly over the ground as rings of it grow, two in three to the ground.
Each flickers over 0.55 seconds as the crate's envelope does, a fast rise, a fall and two or three
return strokes at seeded times, and drives a point light of 4 billion lumens at its peak through
Bevy's reflected light only where its intensity changes, a glow in the clouds' march, a light
scattered evenly about a point 300 meters into the base and falling off as a bell 600 meters wide,
with the clouds' history off while it flashes, and a bolt drawn added after temporal antialiasing, a
channel of 128 segments by midpoint displacement seven times over at a quarter of each length, with
three to six branches split five times, the core a pixel and a half wide at least and dimmer as the
channel is narrower, a glow ten times as wide, fading into the clouds' base, at 0.02 ms. The thunder
is made on the CPU into a stream of samples a sixth of a second ahead, noise through two one-pole
low-passes whose cutoff falls from 3,000 hertz near or 250 far to 110, lifted by the square root of
the band it lets through, three to six rolls at seeded times, a crack for a strike within two
kilometers, softly clipped, heard after the distance over 343 meters a second, its streams kept for
the next clap. The crate drew no bolt and played a clip the game gave it.

Shared: the clouds in the sky lighting, in the commit carrying this line, item 69's environment map
phase, for 3DEngine's sky light beside its own. Bevy's atmosphere draws the sky into the cube its
lighting is filtered from on the camera's frame after the prepass, and its filtering reads the cube
as the next frame begins, so three dispatches on the camera after the prepass lay the clouds over
it. A copy of the cube, which a pass cannot read and write at once, is taken first and lights both
cloud marches with the clear sky, so the clouds are not lit by their own undersides; a march into a
cube of 64 texels a face, six slices of a 3D image, twice a texel from points a step apart and
averaged, keeps the clouds' light before the exposure and how much of the sky gets through; and the
merge writes the cube back through a two-dimensional array view of its six faces, which the bridge
gives a cube image for a storage binding at ABI 309, with `bcs_render_sky_lighting_cube` handing the
managed side the atmosphere's cube alone. The fog's horizon color is read sharp at five ways over
thirty degrees and averaged, since the clouds along the horizon otherwise stood up the picture as
stripes through the froxels. Some 0.18 ms of the GPU at 1280 by 720.

Item 73: the four shards were started by a command typed on 2026-10-20 at 01:07, `podman run -d
--name shardN --memory 16g -v ~/.cache/bevycsharp/shard-N:/work:z fedora:44 sleep infinity` and a
`podman exec` of a work script kept in each shard's own folder, and no script of the repository
starts a shard, so there is no line to give an init. Nothing uses them; each holds the idle dotnet
and VBCSCompiler of that night beside the defunct. `podman rm -f` of the four was refused by this
run's permissions until the owner allowed it, and the four were removed at 10:37 on 2026-10-25,
their defunct entries with them. A container started for work from here is started with `--init`
and removed as its work ends, as BUILDING.md's caches section says.

Shared: the weather crate's removal, in the commit carrying this line, item 69's last phase.
native/bevy_weather leaves the workspace with its lock entry, and the bridge loses its render
profile's dependency, `render::weather` with `bcs_weather_active` and `bcs_weather_set_preset`, and
`BcsConfig.weather`, at ABI 310. `Config.Weather` and the `Weather` facade leave the managed side,
`WeatherPreset` staying as the engine's presets. The component list, written again from an editor
build, loses the crate's 284 lines, and `Bevy.Reflected` the 530 lines of its wrappers. The feature
test's weather is the engine's alone, its tier gone from the page, the settings and the drive, and
the guide's section on the weather is the engine's.

Shared: a tube in the modeling and trees grown on it, in the commit carrying this line, item 70's
first phase. `EditMesh.Tube` sweeps a ring of a number of sides along a chain of points with a
radius at each, the rings turned by the rotation minimizing frame of Wang and others (2008), the
sides smoothed and the ends flat, its recipe line `tube SIDES X Y Z RADIUS` and on, which a model
file of 3DEngine's would read once it has the operation. `Trees.Grow` grows a skeleton of points
with parent, place and radius from a `TreeSpecies` of Weber and Penn's controls and a seed, by space
colonization with buds after Palubicki, a PCG of the engine's own for the numbers, and
`Trees.Build` makes it into an edit mesh, a tube a stem.

Shared: the trees' light and presets, in the commit carrying this line, item 70's phase (b1). The
growth gains Palubicki's shadow grid, a point darkening its voxel and a pyramid under it, a tip
growing while light reaches it and a side bud past the first level breaking only with room and
light, and `Trees.Grow` an overload taking boxes that no stem enters and that take the sky from
their side. `TreeSpecies` gains the paper's base splits, curve back, flare and pruning envelope, and
carries the appendix's four species with its values and a bush and a fern, which a model file of
3DEngine's could read as data.

The public listing loses two lines and gains their renames in the commit carrying this line, item
70's phase (b2), as N 2.4 asks. `TreeSpecies.Bark` and `TreeSpecies.Leaves`, the names of a
species' texture sets since `70f92473`, become `BarkSet` and `LeafSet`, since the paper's own
`Leaves` is its count of leaves a stem, which the species gains under that name with `LeafShape`,
`LeafScale`, `LeafScaleX`, `LeafLevel`, `LeafBend` and the bark, leaf and fall colors.

Shared: a grown tree's leaves and materials, in the commit carrying this line, item 70's phase (b2).
`Trees.BuildLeaves` shares the paper's count of leaves, its leaves a stem times the stems its levels'
branches make, among the twigs a grown tree has, at most 30,000 cards, each drawn larger up to 1.6
times where the count is cut, set off its twig by up to one and a half of its lengths, turned to face
up and out, and cut from a leaf set's atlas by the boxes the import finds, or a frond strip along
each last stem where the species' leaves are below zero. `Trees.BarkMaterial` and
`Trees.LeafMaterial` bind a set's surface as occlusion, metallic-roughness and diffuse transmission
at once, with generated noise bark and leaf masks where a species names no set, and a fall share
tinting the leaves' mean color to the species' fall color.

Shared: trees planted by name and seed, in the commit carrying this line, item 70's phase (c1). A
`Tree` component holds a species' registered name and a seed, `Trees.Register` names a species with
the presets registered under theirs, and a system grows each species' eight variants on a worker the
first time a tree asks for one and dresses every tree under a child turned and sized by its seed,
with its stems, its leaves and static capsules along the trunk and each stem twelve centimeters
thick or more, the tree's own entity given Bevy's visibility where it has none. `Trees.PlantForest`
spreads a forest over a disc by a Poisson disk.

Shared: leaf atlases that keep their leaves with distance, in the commit carrying this line, item
70's phase (c2), its first reading. An alpha-cut leaf atlas's smaller levels lose a leaf once its
box is a texel or two across, where keeping the share of the whole atlas's texels past the cut
still holds, so each level's opacity is scaled to keep the share of each leaf's box past one half
as a card samples it, read between the level's texels, which holds a crown's leaves to four times
the distance that loses them otherwise.

Shared: a tree's levels of detail, in the commit carrying this line, item 70's phase (c2). A
planted tree is drawn whole within two and a half of its heights of the camera and in a reduced
level past three, dithered between, the reduced level of half the tube sides, the stems thinner
than a twenty-fifth of the trunk left out and every other leaf drawn the square root of two larger
at its place, which covers the same pixels within a tenth where the two change. A level's shadow is
measured from the scene's camera, which needed Bevy's shadow origin set on it here.

Shared: a crown's leaves shaded by the sky each sees, in the commit carrying this line, item 76's
first part. The leaves' area is gathered into a grid over the crown, and each leaf takes the share
of the sky reaching it through the leaves round it by Beer and Lambert's law, along the zenith and a
ring of eight ways, written as its vertex gray from the whole of its color to half, which darkens its
ambient, indirect and transmitted light. A normal bent out of the crown and baked into a
double-sided card mirrors the sky from below, so the bend waits for a material lighting the diffuse
term by it alone.

Shared: a crown's diffuse light by normals bent out of it, in the commit carrying this line, item
76's second part. Each leaf carries the way out of the crown, the way its grid's smoothed leaf area
falls fastest, in its second texture coordinates, and the leaf material lights each pixel twice
from the engine's own lighting, the diffuse light by the leaf's normal bent half way toward it and
the specular and the light let through by its own, so the crown's sun side stands out more than
three times as far and a crown seen from below mirrors no sky, at 7.5 ms of a 1080p forest's opaque
pass against 5.2 lit once.

Shared: a planted tree's far level of limbs and branch cards, in the commit carrying this line, item
70's phase (c2), its last part. Past six of its heights a tree is drawn as its sixteen thickest
stems and each one's foliage on two crossed cards in the plane its leaves spread most in, pictured
once a variant by an orthographic camera on a render layer of its own into an atlas, read back and
given levels that keep each card's leaves, with a normal map of the leaves drawn on the CPU and the
leaf set's mean surface, which the import records. The far level covers the crown within a fifth and
lights it within a sixth of the reduced level, and halves a distant forest's shadow pass.

Item 70's impostors, in the commit carrying this line, two departures from the plan. The material is
WESL in the bridge, as the leaf material is, where the plan named a Slang one, since a Slang shader
compiles through slangc on the machine it first runs on and a planted tree draws on every machine.
Its fragment reads three atlases, the color, the normal a leaf's diffuse light is lit by and the
leaf's own normal turned to each frame's eye, so the blend is lit twice as the leaf material lights
a leaf, and it takes the leaf set's mean roughness and metallic where the cards are fully rough,
without which the tupelo's and the oak's impostors read 0.78 and 0.83 of their cards. One fault of
the cards' commit is mended on the way, the meshes, materials and target stood for a variant's
pictures held for the app's life, which are released once the pictures are read. No public listing
change, ABI 312.

Shared: a planted tree's octahedral impostor, in the commit carrying this line, item 70's phase
(c3). Past twenty of its heights a tree is one quad turned to the eye, painted from the three of
sixty-four pictures of the whole tree taken nearest the way it is seen, the ways an eight by eight
grid folded octahedrally over the upper half of the sphere, pictured once a variant beside its
cards, with the normals a leaf is lit by drawn on the CPU into two atlases so the blend is lit as
the leaf material lights a leaf. It covers the crown within a sixth of the cards and lights it
within an eighth, casts its shadow and takes none, and halves the opaque pass of a thousand far
trees against their cards.

Item 70's wind, (d1), in the commit carrying this line. The sway rides on two streams of the game's
own at locations 8 and 9, the limb's pivot and phase and the leaf's, which the bridge's stream
registry carries, so the mesh data is unchanged; the ABI moves to 313 for the leaf, bark and
impostor materials' stiffness and the wind's call. The bark takes a material extension of its own.
The limbs are the cards' sixteen, with each thinner stem off the trunk below them swinging as a limb
of its own, so a card swings with the leaves it pictures. Bevy's prepass view group holds the
globals at binding 1, where the main pass holds them at 11, which the trees' and the impostor's
prepass stages declare. The public listing gains Trees.Wind, the wind set by a game or taken from
the first weather. No line removed or reshaped.

Shared: a planted tree swaying in the wind, in the commit carrying this line, item 70's phase (d1).
Each vertex of the stems and leaves carries the pivot and phase of the limb it swings with and of
the stalk its leaf flutters about, the trunk bending by the square of the height, a limb by the
square of the reach from its pivot and a leaf across its reach, each tree in a phase from where it
stands, moved in the vertex stages of the bark's and the leaves' materials and their prepasses so
the shadows sway, the far cards swinging with their limbs and the impostor leaning with its trunk.
The wind is the weather's or one a game sets. It costs nothing measurable in a forest's passes at
1080p.

Item 70's leaves in the scene field, (d2), in the commit carrying this line.
MaterialSettings.LightThrough is the share of global illumination's light a meter of a surface lets
through, kept by the bridge beside the material and written in the fourth channel of each drawn
mesh's color, so the field's draw record is unchanged; the ABI moves to 314 for the calls that set
and read it and the trees' field tint. The probes' rays cross such surfaces as 3DEngine's do, a
separate image holding the share kept before a ray's interval since the shader images have no
two-channel half format, the trace built twice so a frame where nothing lets light through runs the
build that stops at the first surface, the screen probes crossing under a flag and the merge seeing
past the leaves. Two faults found on the way. The field read only the standard material, so the
leaves, since item 76, and the bark, since the wind, stood in it white and closed; it reads them
through their standard base and leaves the impostor out. And it took every level of a planted tree
at once, a visibility range hiding a mesh from each view and not from what it inherits; it takes a
mesh with a range where the field's eye stands within it. A lead, not chased, is the floor five and
a half meters up, which takes 0.24 of the held-back sky light where the geometry gives 0.41 and
3DEngine reads 0.37 and 0.32, and a slab letting all through holds back a tenth at Low where
3DEngine's holds back a hundredth. The public listing gains MaterialSettings.LightThrough. No line
removed or reshaped.

Shared: a surface letting a share of global illumination's light through, in the commit carrying
this line, item 70's phase (d2), BevyCSharp's side of SHARED.md's light-through row.
MaterialSettings.LightThrough, a share a meter, painted into the field's colors' alpha as 3DEngine's
lightThroughPaint does, the cascades' and the screen probes' rays crossing up to four such surfaces
and the merge seeing past them, the trace built twice so a frame of none runs the build that stops
at the first surface. A floor under a slab twenty across letting half through a meter takes 0.44 of
the sky light a solid slab holds back at Low and 0.32 at High two and a half meters up, and 0.24 at
both five and a half up, against 3DEngine's 0.36, 0.40, 0.37 and 0.32. A planted tree's leaves let
half through a meter, and nine tupelos' floor takes 0.21 of what their leaves held solid hold back.

Item 77's trees' memory, in the commit carrying this line, the reading in
2026-10-26-item-77-memory.md. A species' eight variants are drawn from 11 to 271 MB of their meshes'
data and plans, 1,027 MB for the forty-eight the suite plants, which the process kept for as long as
it ran, and their skeletons and capsules come to 0.06 to 0.89 MB. The process keeps those alone, and
an app that draws a variant shapes the rest from the skeleton on a worker and lets go of it, the
plans' per-card stems and leaves and the atlases' bytes among it, once the meshes are made and the
far levels pictured. The trees' meshes are made without the copy Render.CreateMesh keeps of data for
a scene, 477 MB of a forest's, since a scene writes a tree as its species and seed. And an app
shapes four variants at a time, since shaping one allocates three to five times what it keeps in
arrays the collector takes back in its full collections alone, so a thousand trees of three species
peak at 2.2 GB of the machine's memory against 5.0 GB shaped at once, their test taking 56 seconds
against 30, and once dressed hold 21 MB of managed memory against 1,317 MB, beside the bridge's 846
MB of their meshes and atlases. The suite ran whole in one host at a cap of 3 GB in a 20 GB scope,
1,976 passed and 3 skipped, the host's anonymous memory read at 2,222 MB at most, where it passed
the 3.75 GB cap after 469 tests before. Three leads, not chased. The bridge keeps each atlas in
Bevy's main world after its upload, 7 MB a variant nothing reads again, which
RenderAssetUsages::RENDER_WORLD would let go for an ABI call. A scene saved with planted trees
writes their dressed bodies, parts and capsules beside the Tree component, since the save's default
takes every entity carrying a component of the project's own, so a load dresses each tree again
beside its old parts. And the engine keeps the last app it ran through statics such as SceneField's
until the next app replaces them. No public line changed.

Item 77's guard's stop, in the commit carrying this line. MemoryGuard writes its line to the file
BCS_MEMORY_CAP_SAID names as well as to the console and the crash file, and build/test.py names one
for each suite process it runs, so a host the cap stopped reads as stopped whatever window of its
output vstest quotes. The process's line on the page says the guard's reading and its cap before the
most the script read it holding, and the guard's own line stands under its loss. The stand-in gained
a host whose line vstest cut off, and a real host stopped at a fifth of a gigabyte read as stopped
with its line, as did the part it ran again in. No public line changed.

Shared: build/test.py reading a memory guard's stop from a file it names for each process, in the
commit carrying this line, since dotnet test quotes only a window of a crashed host's output and cut
the guard's line off twice on 2026-10-26. 3DEngine's build/test.py reads no cap, so it is offered
for a guard of its own.
