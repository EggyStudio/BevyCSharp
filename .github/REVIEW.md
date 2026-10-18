# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read. A stash of every changed file
takes what was written here since the last commit out of the tree until it is popped, so a stash
names its own paths.

Reviewed up to `5e750133`, item 22 done and nothing else, 8 files, the first item taken since
Decision 33 lifted. Courtyard, the stress program and the first game name 0.4.129, the newest
published version, and each built on it from nuget.org; Swarm names 0.5.0, which NuGet reads as the
lowest published version at or past it, so the first publish of 0.5 restores it with no commit, and
until then its restore fails naming 0.4.129 as the nearest, its project's comment saying why and how
it is built meanwhile. `build/games-on-defaults.sh <version>` waits for nuget.org's flat container
to list the version, twenty minutes at most, then builds each game strict on its own default from
nuget.org alone into a folder of its own, so the fresh pack under `build/package` cannot stand in,
prints each game's errors and names a game whose default does not build in an error with the version
to raise it to, and a version not listed in time in one of its own; run against 0.4.129 it built the
three and named Swarm. The pack job runs it after the push to nuget.org, when publishing alone, its
limit 45 minutes where 20 stood, the comment counting the wait and a minute or two a game.
BUILDING.md's publishing section says what a game's default is, that a raise is one line a game, how
Swarm is built until 0.5 is published, a pack under a version of its own and new each time since
NuGet keeps the first package under a version, and what the publish check does. Checked on the
working machine was the whole suite, 1,475 passed and 3 skipped of 1,478. Right. The pack run of
`b50890cb` ended whole green at 02:35 with its package, the second in a row, its Windows suite the
second of three toward Verdict 29 and the feature test's course taking 47 minutes. Next the owner's
push of this commit, and item 26, the reflected variants, chosen at 02:05 since every other row
waits on a crate, a word the owner types in the working session or the owner's screens.

Before it, `b50890cb` came, Verdict 29's addition done and nothing else, 3 files. The test script's
table of processes carries each one's creation time beside its parent and resident bytes, read on
Windows through `GetProcessTimes` as a FILETIME and none on Linux and macOS, which hand an orphan to
another parent; one walk, `tree_under`, takes a process as a child only where it was not created
before the parent its recorded id names, with the reason in its docstring, and both the memory
reading, the suite's own cap with a test's, and the kill of a tree go through it; a test feeds a
table in which a 900 MB process made before the watched one names it as parent and finds the tree
without it and its largest process at 40 MB, an entry with no creation time taken by id alone.
Checked on the working machine were the whole suite, 1,475 passed and 3 skipped of 1,478, and the
grow case through the real table reading 210 MB past the 200 allowed; the Windows path runs on the
next Windows suite. Right. The owner pushed `b50890cb` at 00:37 and started a pack run on it, which
waits on the pack run of `b4d45938`, started 00:05, whose natives, suites, digest, storm, README
walk, play job and two shards are green at 00:50, the play job's soak of three games passing in 18
minutes, which settles Verdict 28, its two other shards and the feature test's course still running.
At 01:07 the push run of `b50890cb` has ended whole green, its Windows suite the first of three
toward Verdict 29, and the pack run of `b4d45938` has its four shards green, which settles Verdict
26, with the feature test's course still running and the pack job after it; the second pack run
waits behind. At 01:29 the pack run of `b4d45938` ended whole green, the feature test's course in 65
minutes and the pack job with its package in two, which with the whole green push run of the same
commit is Decision 35's gate met, so Decision 33 is lifted here and development resumes, item 22
first and then the owner's items; item 23 is proven. The second pack run, on `b50890cb`, started at
01:29 and is read when it ends, its Windows suite the second toward Verdict 29.

Before it, `c9d2bf3d` came, Verdict 29 mended and nothing else, 4 files. The lost-suite theory's
hang case runs under a memory limit of a terabyte, which no process of a runner holds, so its time
limit alone can end it, the other three cases keeping 200 MB, each case's limit an argument of the
theory with the reason in a comment, the sleeping stand-in measured at 10 MB on Linux; a loss to the
memory limit says the reading against the limit in the part's summary line and in its heading, "its
largest process read at 210 MB past the 200 MB allowed", which the grow case asserts, the digest's
JSON carrying it and BUILDING.md saying so. Checked on the working machine was the whole suite,
1,474 passed and 3 skipped of 1,477. Right. The working session read a possible cause and left it,
which Verdict 29 takes up. Next the owner's push and a pack run, Decision 35's gate, whose push half
the run of `b4d45938` met.

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
   Slang shaders of the bridge and the examples as it counts the C# and the Rust, as 3DEngine's does
   since its `09419080`, none of them over 800 today, which it does not yet (SHARED.md's row, taken
   next). `60141490` takes `NativeTypes.Render.cs` off N 1.3's list, split into ten files named for
   their structs, and those ten types off N 1.2's. `render/assets.rs`, `NormTests.cs` and
   `UiTests.cs`, which stood at 799, 794 and 794, are split at `88a75e54`, `f68bd192` and `09e3b339`
   to 557, 588 and 535, each a commit that moves code alone; `app.rs` is at 709 since `c5b6d1d3`. N
   7.2's list takes `ff467a5f`, the subject's marks run together and pushed before the suite read
   them, beside the owner's `5a7f2c07`, and the list only gets shorter from there; `a0c8b1b0`, the
   owner's own one-line commit of the showcase's caption, pushed, joins it as the owner's with
   custom_schedule's commit, three left out.
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
    TODO.md's open line under the components through reflection, item 4's remainder.** Chosen on
    2026-10-18 as the next work, since every other row of the Now list and of TODO.md waits on a
    crate the lock lacks, on a word the owner types in the working session (Decisions 11, 12 and 20;
    Feathers, the camera controllers, the dev tools, `sysinfo`, `ctt`, `ureq`, the template's
    publishing) or on a run on the owner's screens (`monitor_info`), and the font examples stay off
    under Decision 13. The generated wrappers type an enum inside a variant, a list, a duration and
    an integer that holds no zero, and leave an `Option` of a tuple (a text layout's cursor, a
    window's pan gesture, the focus and its cause), of Rust's `Instant` (the real clock's first and
    last update) or of a node index (animation transitions' main animation) with its variant names
    alone, the value on a string path checked when it runs; each gets a typed property, with a test
    a kind, and TODO.md's line and COMPONENTS.md follow. One commit, the eight register rewordings
    riding along.

## Verdicts

Verdicts 3 and 14 to 28 are settled: 15, the native job's four targets, on the pack run of
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
`--render` bridge.

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

29. **The script's lost-suite test asked for the time limit and the script named the memory limit,
    on Windows, once.** On the push of `dd2149ee`, 2026-10-17, the Windows suite failed one of the
    1,041 tests it ran, `TestScriptTests`' theory that a lost suite is said first and run again in
    parts, in its hang case, where the stand-in sleeps ten minutes under a limit of 0.1 minutes and
    200 MB, and the page read "ended at its memory limit" where "ended at its time limit" was asked;
    Linux and macOS passed it, and so had every Windows suite before, the push of `cd2d542d` among
    them. `build/test.py:164-166` reads the clock first and the memory second on each poll, so the
    memory limit was passed before the six seconds were, the stand-in's process tree holding more
    than 200 MB on that runner for a moment, python and whatever the watch counts under it. One
    thing. The hang case's memory limit is set where nothing the stand-in holds can reach it,
    measured, so the time limit alone can end it, and the script's lost line says the figure it read
    against the limit, as the cap case's does, so a reading that passes a limit is read on the page
    rather than guessed. Settled at three green Windows suites in a row after the mend. Mended at
    `c9d2bf3d`, the hang case held to a terabyte so its time limit alone ends it, and the page
    saying the reading that passed a memory limit beside the limit. The working session read a cause
    it did not act on, and it is asked here. On Windows `build/test.py` builds the process tree from
    the parent ids Toolhelp records (line 318), and Windows neither reparents an orphan nor keeps an
    id from reuse, so a process left from an earlier run whose recorded parent id is one the
    stand-in's tree reuses is counted under it, a test host among them, which is how a sleeping
    stand-in's tree read past 200 MB and how the suite's own cap could be passed by a stranger. One
    thing more. The tree takes a process as a child only when it was created after its recorded
    parent (`GetProcessTimes`), as psutil's walk does, so a reused id pulls no stranger in. Settled
    at three green Windows suites in a row after that. Done at `b50890cb`, the tree taking a process
    as a child only where it was not created before the parent its id names, on Windows by
    `GetProcessTimes`, for the memory reading and the kill alike, with a test of a stranger left
    out; settled at three green Windows suites in a row after it.

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

## Replies