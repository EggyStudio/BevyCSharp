# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it already keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read.

Reviewed up to `661682e`. The examples project, the table and the first 22 examples (`661682e`)
were read: the table holds 421 rows, each written example has its capture, and `3d_shapes` was
read beside Bevy's source. The fixes the examples turned up (`5bc8e5b`) raised nothing. Counting
Bevy's components reached through reflection as bridged is right, since an example can be
written with them. The first batch raised the verdict below.

## Now

Item 1 is the owner's request of 2026-10-04 and goes on a group at a time, the verdict below
taken with its next batch. Items
4, 5 and 6 are taken from [SHARED.md](SHARED.md). Items 2 and 3 are the owner's decisions.

1. **Bevy's examples, one by one, as this engine's examples and as its measure.** Bevy ships
   416 examples in 31 groups (70 in 3D rendering, 60 in UI, 36 in ECS, 28 in 2D, and so on),
   listed with a sentence each in `examples/README.md` of the Bevy this bridge builds against
   (0.19.1, in the cargo registry, and at
   `https://github.com/bevyengine/bevy/blob/main/examples/README.md` for the version after) and
   as `[package.metadata.example.<name>]` entries in its `Cargo.toml`, each with a name, a
   description and a category. They are the widest statement there is of what Bevy does, so
   which of them can be written in C# is the most honest count of what the bridge covers.
   - **The examples project.** `BevyCSharp.Examples`, each example a program of its own under a
     folder for its group, picked by name as an argument and opened by `./bcs` by that name,
     windowed or offscreen, in the way `3DEngine.Examples/Program.cs` and
     `build/capture-example.sh` do it in 3DEngine's checkout beside this one. An example keeps
     Bevy's name (`3d_scene`, `sprite_sheet`), so the two can be read side by side. Each has a
     capture in `.github/assets/examples/<name>.png` taken offscreen, the README shows them, and
     the package workflow captures every one and fails on one that does not start. What
     `BevyCSharp.Sample` holds moves into it where it is an example.
   - **The table.** `.github/EXAMPLES.md`, kept by the working session, a row for every one of
     the 416, made by a script from the metadata so that none is left out, grouped as Bevy
     groups them. A row's state is one of four: `written`, with the example's path; `can be
     written`, when everything it uses is bridged and nobody has yet; `missing`, with what the
     bridge lacks named exactly; or `does not apply`, with the reason, for what is about Rust
     itself (`no_std`, the reflection of Rust types, custom render nodes written in Rust, wasm).
     Above the table, each group's counts and the whole's, which is the engine's completeness in
     one line and is repeated in the README's status section.
   - **The pass.** A group at a time, in the order a game needs them: 3D rendering, 2D, UI,
     ECS, animation, audio, input, camera, state, transforms, window, gizmos, picking, assets,
     glTF, scene, time, movement, math, shaders, then the games and showcases, with stress
     tests, diagnostics, dev tools, the application and async groups, reflection, the remote
     protocol and the advanced shaders after. For each example the Rust source is read, its row
     is set, and it is written when it can be. Something missing that is small is bridged in the
     same batch and the example written. Something large becomes an entry in TODO.md that names
     the examples waiting on it, so TODO.md's order comes to be set by how many examples an
     entry unlocks.
   - **How it lands.** The project, the script, the whole table with every row triaged by
     reading the example's description, and the first group written are the first batch. Each
     later batch is a group, or part of a large one, and ends with the counts moved. The other
     items below are taken between groups, not after all of them.
   - **What each is compared with.** `https://bevy.org/examples/` shows the examples that run in
     a browser, each with a picture and its source. An example written here is looked at beside
     Bevy's picture of it, and a difference that is not a matter of the window's size is either
     fixed or said in its row, since a capture that differs is a feature that is partly there.
   - Verified by the table holding 416 rows, by every `written` row opening by name and
     capturing a picture that is not blank, and by the counts in EXAMPLES.md and the README
     agreeing.
2. **The README is split into a guide under `docs/`**, which the owner decided for on 2026-10-04
   and asked to come soon, since every commit that edits a README of 3,810 lines makes the
   split larger. It comes after the examples batch in progress.
   The shape is the same in both engines and is recorded in [SHARED.md](SHARED.md). Who a
   document is for decides where it lives. `README.md` is for somebody deciding whether to use
   the engine, about 200 lines. `docs/` at the repository's root is for somebody using it, one
   page an area and `CHEATSHEET.md`. `.github/` is for somebody working on it.
   - **The move, one batch.** The README's reference leaves it with nothing dropped, each major
     heading becoming a page: `docs/behaviors.md` (systems and components, stages, the fixed
     timestep, filters, conditions, threading), `docs/states.md`,
     `docs/messages-and-hierarchy.md`, `docs/components.md` (Bevy's own, every other, lists and
     maps, visibility), `docs/scenes-and-saves.md` (data assets, scene files, instances, saving,
     changing a type), `docs/assets-and-models.md`, Drawing's 1,326 lines along its own
     sub-headings into `docs/drawing.md`, `docs/materials.md`, `docs/shaders.md`,
     `docs/cameras-and-light.md`, `docs/ray-tracing.md` and `docs/window.md`, then `docs/2d.md`,
     `docs/gizmos.md`, `docs/ui.md`, `docs/audio.md`, `docs/physics.md`, `docs/input.md`,
     `docs/running-a-game.md`, `docs/making-a-game.md`, `docs/tools.md` and
     `docs/how-it-works.md`. README.md is rewritten in the order 3DEngine's has, which is the
     model: what it is and a first behavior at the top, the examples with their pictures and
     the count from EXAMPLES.md, the install, then a Guide section that is the table of
     contents, a line a page saying what it covers, then status, building, contributing and the
     license.
   - **The cheatsheet, the batch after.** `docs/CHEATSHEET.md`, every public call of `App`,
     `EcsWorld`, `Render`, `Ui`, `Audio`, `Physics` and the rest on a line of its own with what
     it does, grouped as the guide is, and a test that holds it to the public surface as
     3DEngine's `CheatsheetTests` does, so a call added without its line fails.
   A page covers one area in 100 to 300 lines: what the area is for in two or three sentences,
   then step by step with a snippet each, then links to the example that shows it, the
   cheatsheet's section and the next page. A page past 400 lines is split. Where an example
   exists the snippet is the example's own code, so the two cannot drift apart. Links from the
   README are full GitHub URLs, since the README is also the package's page on nuget.org, where
   a relative link goes nowhere, and a check in the workflow follows every link in the README
   and `docs/`.
   - Verified by a script that lists the headings the README had before and finds each in a
     page under `docs/`, by the README walk still passing, by the package's readme being
     checked if the project packs one, and by no relative link left in the README.
3. **Gamepads** (decision 2), which the owner decided for. `bevy_gilrs` goes into the render and
   editor profiles, with buttons, sticks, triggers, connection and rumble reaching C# as the
   keyboard does, a command that presses a pad's button for `bcs`, and Courtyard's runner
   steered by a stick beside the keys.
4. **A build with no warnings, and a warning failing the workflow.** 3DEngine's first runs on
   GitHub carried dozens of annotations nobody had seen, a `stackalloc` in a loop among them. The
   managed build passes `-warnaserror` in the workflow once it is clean, with a warning that is
   right to keep turned off where it arises and its reason beside it, and `cargo` builds deny
   warnings the same way.
5. **A contact that says how hard its pair hit, and joints with limits**, from 3DEngine's
   `c5227118`: the speed a pair closed at on `ContactStarted`, which a game turns into damage or
   the loudness of a sound, a ball joint kept within a cone, and a distance joint whose range
   changes after it is made.
6. **Two more of a character and a collider**, from 3DEngine's `52579d98` and `454e9276`: a
   character that crouches and stands from its component's height, which TODO.md holds here,
   and a collider that is the shape of the meshes an entity and those under it show, made once
   the model has loaded, so a level's floors and walls are the model it places.

One thing about item 1, from the table as it stands. Nine of the 78 missing rows wait on 2D
meshes (`Mesh2d` with `ColorMaterial`) and 24 are in UI, so the entries TODO.md gains from the
table say how many examples each unlocks, and the largest of them are taken between groups ahead
of smaller ones. A row that says `can be written` is a claim until the example is, so the count
the README quotes is the written one, with the others beside it.

## Verdicts

1. **An example written in part counts as written** (`661682e`). Five of the 22 rows marked
   `written` say what they leave out: `3d_shapes` has the solids and none of Bevy's segment,
   polyline or seven extrusions, `bloom_3d` three of its six settings, `pbr` its turned label,
   `transparency_3d` its alpha to coverage, and `wireframe` its width and topology. The rows are
   honest and the count above them is not, since 22 of 421 says those five are done. The table
   gains a fifth state, `written in part`, with what is left named as it is in those rows and
   counted in a column of its own, the headline gives the two numbers apart, and each thing left
   out is an entry in TODO.md naming the example waiting on it, as a missing example's is. A
   difference that is not a feature, such as cubes scattered by another random generator, stays
   `written`.

## Decisions

1. **Commits stay local.** The owner pushes `main` from their own tools, and the working session
   commits and does not push, as COMMITS.md and CLAUDE.md say. An earlier entry here that allowed
   pushing is withdrawn by the owner.
2. **Gamepads go into the render profile.** The owner decided on 2026-10-04. `bevy_gilrs` needs
   libudev's headers when the bridge is built on Linux, and the render profile already takes
   ALSA for audio on the same terms, so `build-native.sh` installs them in the container and
   names the package for a local build as it does for ALSA, and the headless profile still
   builds with a C compiler alone. TODO.md's entry that excludes gamepads is rewritten around
   what is left, and BUILDING.md names the package.
3. **CLAUDE.md's bullet on SHARED.md is the owner's.** They approved it on 2026-10-04, and it is
   committed like any other change.

## Replies

- Shared: gamepads are taken in the commit carrying this line, as `Input.Gamepads` with buttons,
  sticks, triggers, `GamepadConnected` and `GamepadDisconnected` messages and rumble, gilrs in the
  render and editor profiles, and a pad pretended by `SyntheticInput.ConnectGamepad` in every
  profile, which `input.button`, `input.axis` and `input.pads` drive as 3DEngine's console pad is
  driven. Courtyard's runner takes the first pad's left stick beside the keys.

