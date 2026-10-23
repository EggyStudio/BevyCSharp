# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read. A stash of every changed file
takes what was written here since the last commit out of the tree until it is popped, so a stash
names its own paths.

Reviewed up to `cc4245da`, item 50's first commit, the scene's distance field, after `cd05d9a2`, the
captures of items 43 and 44 read from the device's pictures as tests, and nothing else, carrying the
review's files, COMMITS.md's pushes, author and branch rules and the Shared line for 3DEngine: the
field built on the GPU around the camera in up to eight cascades from the meshes that cast shadows,
by 3DEngine's plan and kernels over a pool of triangles the bridge keeps, set by
`Render.SetSceneField` and `field.set` and said by gi.state; departures taken, the kernels holding
distances alone with the paint, faces, emission and lent light coming with item 51, 3DEngine's parts
not taken so a moving or skinned mesh is stamped as its whole box, a box following its mesh a frame
behind, a mesh drawn twice gathered once, a stage from source named by its path in the timings; on
the RTX 4070 in the feature test 260 meshes, 7.1 MB, a cascade's build 0.55 ms, the tick 0.78 ms
against 0.17 without in Debug; 1,720 passed and 3 skipped, Cargo's 107 and 186, the three profiles
clean here and on Windows and macOS, the lock unchanged, NormTests clean. Pushed at 21:23 on
2026-10-23, the build run of `135e43b5` having ended green; its own build run green on the three
systems within seventeen minutes, read at 21:43. At 21:31 the owner asked why Slang here needs the
`bcs_*::` prefix and said yes to dropping it, item 56 and Decision 56. Next item 50's second commit,
the field's view, the slices window and the contact shadows, then 56, then 54's readings. At 19:35
on 2026-10-23 the owner's cloud session on this engine took items 43, 44 and 45 on its branch,
Decision 53, so the local coder goes from item 49 to 50 and 54, cherry-picking the branch as the
first part of a batch when the reviewer says so. At 20:00 the cloud session's first commit of item
43, `01b89b59` on its branch, was read and accepted, and at 20:10 its second, `c0e6c111`, both to be
cherry-picked in order by the local coder as the first part of item 50's batch with their authors
reset and the capture taken. At 20:43 the cloud session's item 45, `4659b331` on its branch
`modeling-operations` under the owner's identity, was read and accepted, to be cherry-picked with
item 50's batch, its first commit `ba8a5324` skipped as item 44's twin; the cloud session takes item
54's node next.

Before it, `135e43b5` came, item 55 after the cherry-picks of items 44 and 45, and nothing else.
Pushed at 20:46 on 2026-10-23, their build run green.

Before it, `4f80c464` came, item 43's two commits cherry-picked from the cloud session's branch, and
nothing else. Pushed at 20:14 on 2026-10-23, their build run green.

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
    commits, the bevel, the loop cut and the subdivision, then the rest. Taken at 19:35 on
    2026-10-23 by the owner's cloud session on its branch (Decision 53), after item 49 of the local
    coder; the captures and the device tests fall to the local coder when it cherry-picks the
    branch. First commit `01b89b59` on the branch, read at 20:00 on 2026-10-23: the bevel by a width
    in one to 64 segments across a round profile, the loop cut round a ring of four-sided faces, the
    subdivision, and `select edge a b` since no edge could be selected alone, each a recipe line, a
    model command and a button of the window's new Edges group; departures taken, a bevel skipping
    open edges and a hole's rim and refused when none is left, a loop cut refusing edges on two
    rings, a face of more than four corners subdividing into a fan; a cube with two segments 56
    vertices and 54 faces closed, a loop cut 12 and 10, a subdivision 26 and 24 then 98 and 96; the
    strict build clean, the headless suite 1,095 passed and 574 skipped. The message is COMMITS.md's
    form; the author is the cloud environment's identity, reset at the cherry-pick; the capture
    falls to the local coder. The second commit, the rest of the operations, follows. The session
    notes Bevy 0.20 needs rustc 1.97.1, 1.97.0 refusing to build the bridge. Second commit
    `c0e6c111` on the branch, read at 20:10: bridge, fill, dissolve, knife, mirror, spin and smooth
    in the recipe, the commands and the window's Edges group; departures taken, a bridge of two
    regions each with one rim of the same count and refused otherwise, dissolved edges taking the
    straight corners with them so a dissolved loop gives the box back, the knife cutting a face
    crossed twice and only marking a concave face crossed more, a mirror merging within a millimeter
    and dropping faces in the plane, a spin's faces taking the profile's facing; two boxes bridged
    16 vertices and 14 faces closed, a half box mirrored 12 and 10, a square spun a whole turn in 16
    steps 34 and 48 closed; the headless suite 1,108 passed and 574 skipped, the strict build clean.
    The item's code is whole on the branch; the capture falls to the local coder at the cherry-pick.
    On main at `ae596af8` and `4f80c464`, pushed at 20:14; done but for the capture, which the local
    coder's next commit carries. Capture done at `cd05d9a2`; item 43 done.

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
    capture of a subdivided and beveled shape. One commit. Taken at 19:35 on 2026-10-23 by the
    owner's cloud session on its branch (Decision 53), after item 43; the captures and the device
    tests fall to the local coder when it cherry-picks the branch. Commit `1f79ec4a` on the cloud
    session's branch, read at 20:22: the stack of mirror, array, bevel, Catmull-Clark subdivision to
    level 6, solidify, boolean and triangulate, each added, moved, removed and applied by a recipe
    line, the model.modifier command and the window's Modifiers group, the build, materials,
    collider and drawn parts using the built mesh; departures taken, a mirrored half box tested as a
    whole box with a middle loop, a boolean placed by at alone, an array along points translating
    without turning, a modifier built once as it is added so a missing file is refused then, apply
    running on the base as Blender does; the stack of mirror, array of 4, bevel and subdivision 2
    making 5,760 faces in 13 ms warm against a budget of 200; the budget test's Stopwatch on the N
    3.3 list as a cost rather than motion, accepted; the headless suite 1,119 passed and 574
    skipped. The capture of a subdivided and beveled shape falls to the local coder at the
    cherry-pick. On main as `33edc8a3` at 20:28 on 2026-10-23, cherry-picked plain with the owner's
    identity before the branch's deletion, the capture with it in the local coder's batch of item
    50. Pushed at 20:46; the capture follows in the local coder's batch. Capture done at `cd05d9a2`;
    item 44 done.

45. **What is modeled is exported for Blender and imported from it (Decision 46).** The built mesh
    and its materials written as a glTF 2.0 file, a JSON with its buffer beside or embedded,
    positions, normals, texture coordinates, indices, one primitive a material slot with the slot's
    color and its texture referenced by path, by a writer of the engine's own in managed code, so a
    level blocked out in the game is opened and finished in Blender; and a model file read into an
    edit mesh through the engine's glTF reader, `Read`, triangles merged back into faces where
    coplanar and sharing edges, materials into slots, so a model from Blender is cut and shaped in
    the game. `model.export` and `model.import` beside the save and the load. Tests: a box exported
    and read back with the engine's own reader having the same counts, a file validated against the
    glTF schema's required fields, and a known glTF's import counted. One commit. Taken at 19:35 on
    2026-10-23 by the owner's cloud session on its branch (Decision 53), after item 44; the captures
    and the device tests fall to the local coder when it cherry-picks the branch. Commit `4659b331`
    on the cloud session's branch `modeling-operations`, read at 20:43 on 2026-10-23, the owner's
    identity as author and committer: `EditMesh.Export(path, embedded)` writing glTF 2.0 with a
    writer of the engine's own, a primitive and a material a slot with linear factors, emissive
    strength past 1 by its extension and textures by relative URI, and `EditMesh.Import(path)`
    reading .gltf and .glb through a new managed `GltfReader` along the scene's node transforms,
    welding corners within 0.01 mm and joining coplanar triangles of one slot with matching texture
    coordinates into faces, materials as slots and smooth normals as smoothed faces, the recipe
    line, the two commands and the window's import; departures taken, faces that are not flat coming
    back as triangles, a prototype slot written as its color, embedded images not taken into a slot,
    the import throwing as Load does with no BadFileTests row; a box out and back 8 vertices, 12
    edges and 6 faces closed at volume 8, the schema's required fields checked, Bevy's own loader
    reading the exported box as 24 corners and 36 indices on a render bridge; the headless suite
    1,130 passed and 579 skipped, the render suite reported when it ends. The branch's first commit
    `ba8a5324` repeats item 44 under the owner's identity and is skipped at the cherry-pick,
    `33edc8a3` standing on main. On main as `ba8111fb`, pushed at 20:46. The cloud's render suite on
    lavapipe gave 1,542 passed, 163 skipped and 4 failed, the four the container's own, three Slang
    shaders uncompiled without slangc and a readback of red as 0 on llvmpipe, every modeling, glTF
    and command test passing; the local build run of `135e43b5` is the proof.

49. **F1 hides the interface, F3 shows a debug overlay like the newest Minecraft's with sections a
    program adds, and Shift+F3 the profiler with it (Decision 50).** The owner asked shortly before
    07:05 on 2026-10-21 for F1 to hide the interface as Minecraft does, as an engine feature, F2 the
    screenshot, and F3 a debug overlay like the newest Minecraft's, sorted in parts logically with
    modules a game adds, world generation among them, showing the system, the camera and the
    renderer, memory, fps and frametime with a graph like the RivaTuner overlay's, both for
    BevyCSharp and 3DEngine, F1 hiding the interface and ImGui but not the F3 overlay so everything
    hides but the overlay over the renderer, and Shift+F3 showing the frame debugger too; and
    answered the reviewer's questions shortly before 07:06: the interface hidden by a scope, F3 the
    overlay's on its release alone with any chord left to the program, the look Minecraft's with no
    header, window styling or border, text on a transparent backing, and the order as the reviewer
    sees fit. The keys, engine-wide by default: F1 hides what the program draws inside the scope,
    its HUD and any ImGui window submitted inside it, and nothing outside it, so a pause screen, a
    menu or an inventory drawn outside the scope stays whatever F1 says, as Minecraft hides its HUD
    and never its screens, the engine's overlay staying, the world drawing on; F3 on its release
    with no other key pressed during its hold toggles the overlay, so a program's own F3 chords
    stand and the overlay never opens on one; Shift+F3 toggles the overlay with the profiler's pass
    timings under it. The overlay is the engine's own text and lines, never an ImGui window, in
    Minecraft's look, two columns of lines in the engine's font on a dark half-clear backing a line,
    no header, no border, no window: the left column the engine's version and build, the fps and the
    frametime with a strip of the last 240 frames' times under it as the RivaTuner overlay draws it,
    the current, the average and the one percent low beside it and the GPU time as a second line,
    the camera's position, facing and field of view, the entities and the world's counts, then the
    program's sections; the right column the system, OS, CPU and its cores, memory in use and the
    managed heap, GPU, driver and its memory in use, the display mode, then the renderer, passes,
    draw calls, triangles, lights, shadows, global illumination's state and the frame's GPU time,
    the frame's times and the passes' GPU times from Bevy's diagnostics. The calls: `Ui.BeginHidden`
    and `Ui.EndHidden` around a program's own interface nodes and ImGui drawing of its interface,
    hidden while asked, `Ui.Hidden`, `Ui.HiddenKey` and `Debug.OverlayKey` moving the keys or
    turning them off, and `Debug.AddOverlaySection(string name, Func<IEnumerable<string>> lines)`
    with a remove, the sections drawn in the left column under the engine's own in the order added;
    the diagnostics overlay folded into it where one overlaps. The same keys, layout, words and
    registration in both engines. The guides' section on the debug keys, the cheat sheet lines
    through the script, the surface listing and the upgrading entry; the feature test adding a
    section and the voxel game its own when the package comes. Tests: F1 dropping the scope's draws
    and the ImGui windows and keeping the overlay, read from the frame's pixels on the device; F3's
    release alone toggling and a chord not; a section's lines in the overlay's text; the graph's
    strip holding the last 240 times. Two commits, F1 and the overlay's columns, then the graph, the
    profiler and the sections' registration. After item 42, before 43. The scope's shape, settled
    with BevyCSharp's coder at 10:05 for both engines: ImGui's public API cannot leave out windows
    begun between two calls, alpha zero leaves windows that take the pointer, and a second context
    doubles the runtime, so the opening call answers whether the scope draws this frame, false while
    hidden, and a program writes its interface inside an `if` on it as it writes `if
    (ImGui.Begin(...))`; what the engine itself draws or holds inside the scope is dropped or hidden
    by the engine whatever the program does, its 2D draws here and its interface nodes in
    BevyCSharp, and a program's ImGui windows are left to the `if`, so a hidden interface costs
    nothing. So here `bool Ui.BeginHidden()` and `Ui.EndHidden()`, the interface nodes spawned
    inside recorded and their visibility set hidden and given back while `Ui.Hidden` holds,
    `Ui.Hidden` settable and `Ui.HiddenKey` F1 or null; the tests reading the frame's pixels with a
    node and an ImGui window inside the scope and a second window outside, F1 pressed and pressed
    again. The feature test: the engine's overlay replaces its Overlay.cs in the first commit and
    its memory lines become its section in the second; its admin panel, a screen outside the scope,
    moves from F1 to F4 with its help line, its README's key list and the drive, since a drive
    opening it with F1 would hide the HUD instead; its HUD lines, the help text and the clock, go
    inside the scope. First half done at `42abf357` on 2026-10-23, the scope and the overlay's
    columns, as the head says; the strip, the profiler and the sections follow. Second half done at
    `e1b08124` on 2026-10-23, the departures in the head; item 49 done.

50. **The scene's distance field, 3DEngine's first phase of its global illumination, through the
    bridge (Decision 51).** The owner wrote to the coder shortly before 07:15 on 2026-10-21 that
    3DEngine's radiance cascades were always meant for this engine too, built there first, and can
    be done here. The three phases come as 3DEngine's did, each read from its code and RENDERING.md
    as the model and its Slang kernels taken through the bridge's compute chain, plain Slang the
    bridge compiles, changed only where the bridge's bindings ask. First the scene's distance field:
    the meshes the renderer draws, pooled by their triangles as 3DEngine pools them, splatted into a
    cascaded distance field around the camera, cascades from one to eight with the finest's cell a
    given size, rebuilt under a budget of cascades a frame, moving meshes stamped as their boxes,
    with `Render.SetSceneField(cascades, cellSize, budget)` and the field's state in the `gi.state`
    command and the debug overlay's renderer section; a window drawing the field's slices as
    3DEngine's bounce window does, through ImGui; the contact shadows of the sun through the field
    where the shadow map does not reach, as a setting. Tests on the device: a box's distance read at
    known points, a tunnel one cell wide clear in its middle, a moving mesh's stamp following it,
    and the budget keeping a frame's builds to the cascades asked, with the feature test's gallery
    holding the field's view. Two commits, the field and its builds, then the contact shadows and
    the window. Placed after items 43 to 45, since the field has stood stable in 3DEngine for days.
    First commit done at `cc4245da` on 2026-10-23, the field and its builds, pushed at 21:23; the
    view, the slices window and the contact shadows follow. Settled at 21:24 for the second commit:
    the contact shadows as a draw after the opaque pass blended Add into the HDR picture, taking
    away the sun's direct light times the field's occlusion past the shadow map's reach by default
    and everywhere as a setting, Bevy's directional BRDF ported to Slang from the G-buffer and
    tested against the map's own darkening on the same pixels within its reach, a bound in levels in
    the commit, rather than a patch to Bevy's shader or a wait for item 51; the gallery holds the
    slices window drawn through ImGui, no engine-owned draw list for a view over the camera in this
    item.

51. **Radiance cascades over the field, 3DEngine's second phase, lighting Bevy's surfaces through an
    additive pass (Decision 51).** The world's probes in cascades over the field as 3DEngine traces
    them, each cascade's rays marching the field, merged upward, the last cascade's rays reaching
    the field's edge and the sky, the sun's and the lamps' light gathered at the hits with the
    materials' colors from the meshes' vertex colors and base colors as 3DEngine's are; the light
    read per pixel in the bridge's pass after the opaque geometry, added as indirect diffuse times
    the surface's base color and occlusion from the deferred prepass, since Bevy's lighting takes no
    indirect input of its own, with `Render.SetGlobalIllumination(quality)` at off, low, medium and
    high as 3DEngine's levels; the glossy term and the emissive blocks' light as 3DEngine's phase
    two has them; the path-traced reference of a view, `gi.reference` and `gi.compare`, giving each
    region's distance, which the ledger's open question on this engine's reference answers; the
    bounce rooms as a test scene with their bounds, the rule that no view's distance to its
    reference widens taken from 3DEngine as the mends' rule. Tests: the red wall tinting a white
    block's side, a lit room's floor within the bound of its reference, and the quality levels'
    costs read on the Cornell box. Three commits, the probes and their tracing, the pass and the
    reference, then the glossy term and the emitters. Waits: the method is still moving in 3DEngine,
    its items 71, 72 and 74 open, and the owner said shortly before 07:28 on 2026-10-21 that if it
    is not yet ready BevyCSharp waits, so this item starts only once those are mended and the voxel
    game's captures say the light holds, the port carrying the mends in.

52. **The screen's probes, the glow lights and the counted meshes, 3DEngine's third phase and its
    mends of 2026-10-21 (Decision 51).** The screen's probes standing on the window's surfaces every
    few pixels beside the cascades with the filter and the history as 3DEngine's,
    `Render.SetGlobalIlluminationScreenProbes` leaving them out with the glow-only pass of
    3DEngine's item 71 so a world of small emitters pays no per-pixel march, the glow lights of
    small emitters, the field's read of a mesh a compute shader wrote with its count read back,
    3DEngine's item 69, and the field's budget in triangles a frame, its item 74; the blotches, the
    edge leak and the stepped edges read against 3DEngine's findings so this engine takes the mends
    it makes. Tests as 3DEngine's for each, and a capture of the feature test's room with the probes
    on and off. Two or three commits by part. After items 43 to 45. Waits with item 51.

54. **A denoiser over Solari's lit result, so the ray-traced tier's picture is clean (Decision
    52).** Solari writes the direct and indirect light it resampled into the view target as one
    color, the surface's color already in it, and leaves the noise to DLSS Ray Reconstruction, which
    the bridge does not build, so the picture is grainy wherever it runs. A node of the bridge's own
    in the `solari` profile, after Solari's node and before Bevy's post-processing, in five stages.
    First the color is demodulated, the deferred prepass's emissive taken off and the rest divided
    by its base color above a floor. Then it is accumulated in time, reprojected by the motion
    vectors onto the previous frame's accumulation and kept where the previous depth, which Solari
    double-buffers, and the normal agree within a plane distance and a normal threshold, the history
    clamped at 32 frames, the first and second moments of luminance accumulated beside it. Then the
    variance, from the moments where the history holds four frames or more, else from a 7 by 7
    neighborhood. Then an à-trous filter of five passes at steps 1, 2, 4, 8 and 16, each tap
    weighted by its depth, normal and luminance differences, the luminance's against the root of the
    blurred variance, the variance filtered along, and the first pass's result fed back as the next
    frame's history, as SVGF does. Last the color is remodulated, times the base color plus the
    emissive, into the view target. `Render.SetRayTracedDenoiser(on)` turns it off for a capture
    beside, on wherever ray tracing is active, and the history's clamp and the three weights' widths
    are a settings struct with SVGF's defaults, the guide naming them. The reading, on a machine
    with ray queries since the runners have none, the local command named under N 3.6: the mean of
    256 frames of Solari standing still as the reference of each of the feature test's reference
    views, the denoised single frame nearer it than the noisy one at every view, the camera strafing
    past a pillar for thirty frames leaving no trail, read as the disoccluded pixels no further from
    the reference than the noisy frame's, and the node's time at 1080p from Bevy's diagnostics, the
    budget set from the first reading. Cargo tests of the weights and the reprojection as pure
    functions, a headless test that a bridge without `solari` has no node, the ray tracing guide's
    grainy sentence changed and RENDERING.md's node added. H-Trace's denoiser is read for its stage
    order and constants, nothing of its code taken (Decision 52). Two commits, the node with its
    stages, then the setting, the guide and the readings. After item 50 and before items 51 and 52.
    Given at 20:43 on 2026-10-23 to the cloud session on `modeling-operations` for the node, its
    stages and the Cargo tests of the math, compiled and checked across the profiles there; the
    readings on a machine with ray queries and the guide's figures fall to the local coder at the
    cherry-pick, after item 50.

55. **The README's animated capture of Swarm goes.** The owner wrote at 20:22 on 2026-10-23 that the
    small section at the top of both engines' READMEs showing that they capture GIFs, BevyCSharp's
    of a game and 3DEngine's of Ore, is not necessary information and is to be removed. The mend:
    the Swarm picture and the paragraph under the Examples heading saying it was recorded by `./bcs
    record` go from the README, and `.github/assets/swarm.gif` is deleted since nothing else
    references it; the showcase picture at the very top and Blockout's picture stay, the tools guide
    keeps its own words. One commit, small, with the local coder's next batch. Done at `135e43b5` on
    2026-10-23.

56. **The bridge's Slang modules give their names plainly, as 3DEngine's do, with no `bcs_*::`
    before them (Decision 56).** The owner asked at 21:31 on 2026-10-23 why a shader here cannot use
    Slang as in 3DEngine, without the double colon, and said yes to the change. The cause:
    `bcs_compute`, `bcs_scene`, `bcs_pass`, `bcs_field` and the bridge's other modules wrap their
    contents in a `public namespace` of the module's name, so every use is qualified,
    `bcs_compute::element`, `bcs_scene::Instance`, `bcs_pass::uv_of`, where 3DEngine's modules
    declare their names at the top level and name each helper for what it is, `fieldDistance` rather
    than `distance`; nothing in Slang or the WGSL it writes asks for the namespaces, and no document
    records a reason beyond keeping generic names from colliding with a program's own. The mend: the
    namespaces go, and each helper is named for its module and its work so no plain name collides,
    `bufferCount`, `bufferElement` and `bufferStore` for the raw buffer's three, `passUv` and its
    kin for the pass's, the types plain, `Instance`, `Material`, `PoolVertex`, `PoolMesh`,
    `FieldInfo`, read against 3DEngine's names where the two engines hold the same thing so a shader
    reads the same in both; the kernels of item 50, the feature test's shaders and every example's
    shader change with it, the compute and ray tracing guides and docs/shaders.md follow; no
    upgrading entry, the owner saying at 21:32 that none is needed since nobody uses the engine yet,
    and this engine having no upgrading page. Tests: the existing shader and compute tests passing
    with the plain names, and a test that no module declares a namespace. One commit, after item
    50's second commit and before item 54's readings, by the local coder since the kernels of item
    50 are in its hands.

58. **The cheat sheet's headers are 3DEngine's, plain group headings and no sub-heading a type
    (Decision 58).** The owner wrote at 21:39 on 2026-10-23 that the cheat sheets are to be
    coherent: 3DEngine's lines one-liners as BevyCSharp's are, and BevyCSharp's section headers as
    3DEngine's, plain words without the code styling. Here each group opens with a `##` heading and
    a guide link, then a `###` sub-heading in code styling for every type, `BevyApp`, `App`,
    `Config` and the rest, each with its own block, where 3DEngine's page has the group heading and
    one block. The mend: the sub-headings go, each group one fenced block with its members in the
    order the types came, a type's members set apart by a blank line inside the block where that
    helps a reader, the heading words plain; `CheatsheetTests` holds the form as it holds the lines.
    One commit, small, with the local coder's next batch.

## Verdicts

Verdicts 33 to 42 are settled, said under each in REVIEW-ARCHIVE.md; 2 and 43 are open.

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
and 3DEngine's reads of it in this bridge's names, `bcs_field::distance`, `normal`, `hold` and the
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
