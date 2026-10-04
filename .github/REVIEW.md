# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it already keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read.

Reviewed up to `4a7331d`. `OnTransition` (`66a5b4d`) and `input.drag` (`ce27e73`) are settled and in
the ledger, and the hinge test counted in fixed steps (`4a7331d`) raised nothing. EXAMPLES.md was
read as it stands uncommitted: 421 rows, 14 written, 276 that can be, 78 missing and 53 that do not
apply, with each missing row naming what it lacks, which is what was asked for.

## Now

Item 1 is the owner's request of 2026-10-04 and comes first, after the batch in progress. Items
3, 5 and 6 are taken from [SHARED.md](SHARED.md). Items 2 and 4 are the owner's decisions.

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
2. **Gamepads** (decision 2), which the owner decided for. `bevy_gilrs` goes into the render and
   editor profiles, with buttons, sticks, triggers, connection and rumble reaching C# as the
   keyboard does, a command that presses a pad's button for `bcs`, and Courtyard's runner
   steered by a stick beside the keys.
3. **A build with no warnings, and a warning failing the workflow.** 3DEngine's first runs on
   GitHub carried dozens of annotations nobody had seen, a `stackalloc` in a loop among them. The
   managed build passes `-warnaserror` in the workflow once it is clean, with a warning that is
   right to keep turned off where it arises and its reason beside it, and `cargo` builds deny
   warnings the same way.
4. **The README is split**, which the owner decided for. It is about 3,400 lines and changes in
   most commits. README.md keeps what the project is, the install, a first behavior, running and
   driving a game, the status, building and the license, and links to the rest. The section
   called The engine, some 2,300 lines, moves into one document an area under `.github/` or
   `docs/`, each linked from the README's contents, with nothing dropped. The README walk still
   takes its program from where the walk script looks, the package's readme is checked if it
   embeds README.md, and SHARED.md's rows that name a README section are told to the reviewing
   session with a `Shared:` line so they follow.
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

None open.

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


- Item 1, the count. Bevy 0.19.1's metadata has 421 examples, 408 of them in the list its
  `examples/README.md` keeps (411 rows there, `hello_world` and the two tests among them) and 13
  kept out of it, the testbeds and the hidden tests, so the table holds 421 rows in 35 groups, the
  last of them the ones kept out. The script reads the metadata, so a row is never left out for
  being hidden.
- Item 1, the first batch. 3D Rendering holds 67 rows, so it is taken over more than one batch:
  this one writes 22 of them, and the next ones go on with the group before 2D. Bevy's
  components reached through reflection count as bridged, which moved 33 rows from `missing` to
  `can be written` (fog, volumetric fog, rect lights, visibility ranges, decals, lightmaps, the
  interface's gradients, shadows and z-index, morph weights and others), and each such row names
  the component.
- Shared: Bevy's examples written in C# under Bevy's names, picked by name, each captured, with a
  row for every one of Bevy's in `.github/EXAMPLES.md`, are taken in the commit carrying this line,
  after `3DEngine.Examples` and its capture script.
