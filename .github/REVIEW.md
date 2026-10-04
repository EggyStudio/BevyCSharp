# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it already keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read.

Reviewed up to `f2ac0cd`. The character controller (`f2ac0cd`) is settled, on Courtyard's play
script still reaching its win, and is in the ledger. The plain stress program's passes agreeing
with the bridge's (`4c5a7c8`) was taken on its description.

## Now

Item 1 is the owner's request of 2026-10-04 and comes first, after the batch in progress. Items
3, 4, 5 and 7 are taken from [SHARED.md](SHARED.md). Items 2 and 6 are the owner's decisions.

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
3. **A system run on a move from one state value to a particular other**, 3DEngine's
   `OnTransition`, beside `[OnEnter]` and `[OnExit]`.
4. **A pointer dragged a step a frame by one command**, 3DEngine's `input.drag`, so a script can
   swipe, drag a transform handle or move a panel, which `input.move`, `input.press` and
   `input.release` in separate calls cannot time.
5. **A build with no warnings, and a warning failing the workflow.** 3DEngine's first runs on
   GitHub carried dozens of annotations nobody had seen, a `stackalloc` in a loop among them. The
   managed build passes `-warnaserror` in the workflow once it is clean, with a warning that is
   right to keep turned off where it arises and its reason beside it, and `cargo` builds deny
   warnings the same way.
6. **The README is split**, which the owner decided for. It is about 3,400 lines and changes in
   most commits. README.md keeps what the project is, the install, a first behavior, running and
   driving a game, the status, building and the license, and links to the rest. The section
   called The engine, some 2,300 lines, moves into one document an area under `.github/` or
   `docs/`, each linked from the README's contents, with nothing dropped. The README walk still
   takes its program from where the walk script looks, the package's readme is checked if it
   embeds README.md, and SHARED.md's rows that name a README section are told to the reviewing
   session with a `Shared:` line so they follow.
7. **A contact that says how hard its pair hit, and joints with limits**, from 3DEngine's
   `c5227118`: the speed a pair closed at on `ContactStarted`, which a game turns into damage or
   the loudness of a sound, a ball joint kept within a cone, and a distance joint whose range
   changes after it is made.

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


- Shared: a system run on a move from one state value to a particular other is taken in the commit carrying this line, as `[OnTransition(from, to)]` over Bevy's `OnTransition` schedule, run between the exit and the entry, also for a script reloaded while running. Naming two enums is BCS009 when compiled.

- Shared: a pointer dragged a step a frame by one command is taken in the commit carrying this line, as `input.drag <x> <y> <dx> <dy> <frames> [Left|Right|Middle]`, which presses where it starts, moves an equal step each frame and releases the frame after, answering then.
