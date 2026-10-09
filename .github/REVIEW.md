# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read. A stash of every changed file
takes what was written here since the last commit out of the tree until it is popped, so a stash
names its own paths.

Reviewed up to `e4c122e3`. Decision 21 is carried out, prose alone: the five places name no one, the
version's commit a setting made by hand, the test script run on a contributor's machine, the scene
pack's file the one that is published, the audio check's crate one that would add to N 2.8's list,
and BUILDING.md's dependency decided apart from the work that would use it; `N_4_7` reads every
Markdown file but the sessions' five, the comments of every C#, Rust, Slang and WGSL file and the
whole of every script, manifest and workflow, for the owner named within a sentence of a decision
word or a reviewing or working session named at all, a thing's owner followed by what it owns left
alone, the vendored weather and Bevy's assets left out as N 4.1 leaves them, and it finds nothing
else today. Right, and the matcher is the rule's text made exact. The norm's tests passed with the
bump set aside, 29 with the script's, and the tree holds the bump in progress, headless compiling on
0.20 and the render profile half done, whose reply gives the whole suite's count. The owner's order
is in both repositories, 3DEngine's seven places next on its side.

Before it, the embedding came to be the bridge's own (Decision 17): `bevy_embedded_assets` 0.16 is
no longer a dependency, its build script the bridge's `build.rs`, which does nothing without the
`embed` feature and with it lists the folder `BEVY_ASSET_PATH` names, the crate's search beside the
target and `cargo-emit` left out, and its reader `src/embedded.rs` in the one mode the bridge used,
each file read through Bevy's own `SliceReader`, the crate's reader tests brought along with one
reading every embedded file back, a bridge built in the container with `--embed`, the lock lighter
by the crate and `cargo-emit`, and the notices keeping the crate's MIT or Apache-2.0 under a new
section for code taken into the bridge, BUILDING.md and PLAY.md saying so. Right, and done on 0.19
as the weather was, so the bump that follows changes one thing. The suite was last run whole at
`601c6264`; this commit reaches no test without the feature and the norm's pass, and the bump's
reply gives the whole count. Before the bump comes the owner's order of 2026-10-09 in item 1, five
places and N 4.7's check in one commit of prose.

Before it, the weather came to be vendored (Decision 18): `bevy_weather` 0.2.0 as published is a
member of the workspace at `native/bevy_weather` with its license and README, the bridge depending
on it by path so the lock changes only in where the crate comes from, upstream's examples, their
dev-dependency on Bevy's default plugins and dev tools, and its profiles left out, its 204 tests
building on its own dependencies; `default-members` keeps a plain cargo command on the bridge, and
`build/third-party-notices.py` leaves out the bridge alone, so the vendored crate stays in the
notices under its authors' license, which is right. Right too that it comes before the bump, as the
embedding will, so each stands alone on 0.19 and the port goes with the bump. The `Rule:` line is
taken: N 4.1's text names another's code kept whole in the tree beside the followed engine's words,
Annex B names `native/bevy_weather` as that code, which the layout rules leave out as well since
they read the bridge's own sources, and the test's leaving the folder out stands as it stands for
Bevy's assets, in both repositories' NORM.md. The spike's counts are answered: the ten glued units
composed through WESL and were then refused by naga 30 for the flat rule, which the report counted
among the unglued alone, 31 and 10 flat, 3 enable and 19 parentheses, and the irradiance unit fails
on the 0.19 condition's missing declaration when no volume is in view, which the port's reply says.
The suite was last run whole at `601c6264`, 1,276 passed and 2 skipped, and this commit runs none of
it but the norm's 18, which pass; the embedding on 0.19 is next, then the bump.

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

   After the batch in flight, the owner's order of 2026-10-10 (Decision 23): a run that shows no
   window, headless or offscreen, as `./bcs open --offscreen`, the suite, the soak and the drive
   script run one, makes no sound. The bridge gives Bevy an `AudioOutput` with no device before the
   audio plugin would open one (`AudioPlugin` initializes the resource and keeps one already there),
   so no device is opened and nothing reaches the speakers; a sound still plays its course and ends
   as it would, a sink that advances by the clock where Bevy's own path would leave it unstarted, so
   a game that waits on a sound's end works there; a config field lets a windowless run have real
   audio where one is wanted; a test holds a headless and an offscreen app to no device and a sound
   ending on time. Small, a commit of its own.

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

**Item 3 b, Bevy 0.20.** The bridge, the weather and the managed side are on Bevy 0.20.0 with wgpu
and naga 30, every profile compiling with warnings denied and the bridge's 116 tests passing with
meshlets and Solari. The lock gains what Bevy's own tree now holds, WESL and its parsers among it,
and loses `naga_oil`; the bridge names one feature more, `bevy/bevy_curve`, which only makes
reachable as `bevy::curve` the crate animation already compiles. ABI 232.

The moves are the ones item 3 listed, which take in observers on the lifecycle events themselves
(`On<AddEvent>`), the flat pointer events, `bevy::curve` and `bevy::shape` for what left
`bevy_math`, the typed `ShaderBuffer` filled through its own calls, `constants` on every pipeline
stage, `#[extract_app(RenderApp)]` on each extracted component, `ViewDepthStencilTexture`, the
prepass depth's own attachment type, extracted windows as render entities with their handle beside
them, a corner radius made circular from one length, Bevy's SSAO reach kept, a summary tick refused
for a C# component, and a Slang material left out of order-independent transparency, since its
fragment shader writes no buffers of that pass. `SpriteMesh` is gone, so the sprite frames move
sprites alone, bevymark takes 0.20's alpha mode on its sprites, and `many_sprite_meshes` and
`many_animated_sprite_meshes`, which Bevy dropped, are dropped here.

The interaction keeps its surface over `Hovered` and `Pressed`, as item 3 asked. An interactive node
carries Bevy's headless `Button` and `Hovered`, and the managed `Interaction` handle names a small
component of the bridge's, `PointerOnNode`, kept from the two in `PreUpdate` after picking, since
thirteen examples react through `[Changed(typeof(Interaction))]` and `Hovered` does not change on a
press. A hover now counts the nodes inside the node, as Bevy's does.

The glue is WESL with 0.20's module paths, each prelude's imports put once at the head, a directive
slangc wrote put after them, and the irradiance volumes' module imported only under
`IRRADIANCE_VOLUME && IRRADIANCE_VOLUMES_ARE_USABLE`; a glued unit is made with `from_wesl` under a
module path of its own, and every other stays `from_wgsl`. The fallbacks are WESL too and decompress
the vertex as Bevy's own shaders do. The spike's three mends live in `reflect/mend.rs` with tests of
their own, applied to every unit slangc writes. On the spike's count, the ten that make 63 are the
ten glued units, which composed and were then refused by naga 30 for the integer rule, which the
report counted only among the unglued. The weather's two shaders are WESL, their import, their one
condition and naga_oil's `#{MATERIAL_BIND_GROUP}` moved, and its Rust compiled unchanged.

Four of Bevy 0.20's own faults are worked around in the bridge, each said where it is done.
Passthrough SPIR-V is handed to wgpu 30 without the entry point it now asks for, so every SPIR-V
compute pipeline was refused; the bridge builds those itself (`render/spirv_compute.rs`), the module
naming its one entry point, and keeps the stage's words for it. A clip's targets are found by its
curves alone, so an event placed on a target with no curve never fired; such a target is given an
empty list of curves. Auto exposure's pass stays on a camera's view after the effect is removed, as
in 0.19, and 0.19's workaround, a new render entity for the camera, now bins its meshes twice and
panics; the pass's private component is taken off every view without the effect
(`render/exposure.rs`). A focused field takes keys only with `bevy_ui_widgets::TextInput`, which the
field now carries. `bcs_ray` follows Solari 0.20's scene group, the previous frame's acceleration
structure at binding 6 and the transforms as three rows of an affine matrix, and Solari 0.20 binds
its scene only once it holds a light, which the guide now says and the two tests tracing it now give
it.

The schema was dumped again from an editor build; its diff reads as the list of what Bevy changed.
The generator stopped on `FontSource::List(Vec<FontSource>)`, a type holding a list of itself, and
now leaves such a variant out of its union rather than recurse. Moved components get former paths
from `BevyCSharp/Generated/bevy-former-paths.tsv`, which `FormerPathsGenerator` turns into the paths
a reflected schema is also found by, so `Tonemapping` and `DebandDither` read from a file written on
0.19, Courtyard's left as it is, with a test placing one by its old path. The examples' table is
written from 0.20's list, the five lines Bevy no longer has taken out and the 22 new ones triaged.
`material.rs` and `reflected.rs`, which the port took past 800 lines, move their render errors and
their resource finder into modules of their own in this commit; the six listed files it touches,
`views.rs`, `post.rs`, `compute.rs`, `window.rs`, `slang.rs` and `ecs.rs`, are mended in commits of
their own that move code alone, next.

The suite passed 1,276, skipped 2 and failed 1, N 6.5, which reads the newest local package, one
packed on 0.19 whose notices name 0.19's crates; against a package packed from this tree it and the
rest of the norm's tests pass. Every example's head names 0.20.0, as the table's script writes it
from the lock.
