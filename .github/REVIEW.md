# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it already keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read.

Reviewed up to `b18cc4a`. The two parked examples are explained and settled. `first_person_view_model`
(`96ff00d`) had no fault, the arm being the slab in the picture as it is in Bevy's. `grid`
(`b18cc4a`) was a real gap: Bevy's flexible track is `minmax(0, fr)` and the bridge offered only
`minmax(auto, fr)`, so content stretched a row, and `Track.Flex` with its pixel test closes it.
The table stood at 113 written, 7 written in part, 122 that can be and 122 missing before those
two.

## Now

Item 1 is the owner's request of 2026-10-04 and goes on a group at a time. Items 6 to 8 are
taken from [SHARED.md](SHARED.md).

1. **The verdict below**, since every capture taken from here on is taken the same way.
   Then **Bevy's examples, what is left of the pass.** Math, then the rest of 3D, an example that
   needs something missing marked and passed over. A picture that differs from Bevy's for no
   known reason is taken down to the smallest scene that still differs and explained before
   the pass goes on, as `grid` was, since that is where the table finds a fault.
2. **The instructions for agents are `AGENTS.md`**, which the owner asked for on 2026-10-04.
   `CLAUDE.md` was renamed by the reviewing session with its content unchanged, since Claude Code
   reads `AGENTS.md` where there is no `CLAUDE.md`, as other coding agents do. The rename and the
   removal of the old file are uncommitted and go in with the next batch, with STYLE.md's scope
   and any other document naming the file following it in the same commit.
3. **A page comparing this engine with Bevy**, `docs/compared-with-bevy.md`, which the owner
   asked for on 2026-10-04, after the batch in progress.
   The page has four parts. What is the same as Bevy. What this engine adds, each as a thing
   a user can do with where to see it. What it costs, said as plainly as the gains. And what was
   measured, on one machine, with the command that runs it again, since a claim about speed is a
   number or it is left out. The README gains four or five lines under its first snippet saying
   why not Bevy itself, ending in a link to the page.
   - The same: Bevy itself, its ECS, renderer, assets and scheduler, running underneath
     unchanged.
   - Adds: C# in place of Rust; behaviors compiled and reloaded while the game runs, with no
     rebuild of the engine; the editor; scene files, saves, data assets and files that outlive
     a renamed type; physics; Slang shaders; an app driven from the terminal with `bcs`; a
     package a game references with no Rust toolchain.
   - Costs: the crossing between C# and the bridge; only what is bridged is reachable, which
     EXAMPLES.md counts; Bevy's Rust plugins are not usable until bridged; it follows Bevy a
     version behind; no wasm.
   - Measured: what PERFORMANCE.md holds from `native/stress`, the same scene in plain Bevy and
     through the bridge, and the mover counts with and without a crossing an entity, quoted and
     linked, with EXAMPLES.md's counts as the coverage.
4. **The gaps, by how many rows each holds**, once the groups in item 1 are through, each
   bridged from Bevy with the examples it unlocks written in its batch. By the table as it
   stands: 2D meshes with their color materials (13 rows); an order among the systems of one
   stage, one before or after another or a chain, and observers of a component's addition and
   removal and of a game's own events, which the ECS and interface rows wait on; Bevy's
   resources reached through reflection as its components are, which `UiScale` and others wait
   on and which is likely a small bridge for many rows; then text gizmos, editable text and
   Bevy's widgets.
   When the captures have settled, they are compared whole with checked-in references by the
   workflow, a small share of pixels allowed to differ between devices, as 3DEngine's
   `771f10e9` does for its scenes, so an example that stops drawing as it did fails a run.
5. **A build with no warnings, and a warning failing the workflow.** The managed build passes
   `-warnaserror` in the workflow once it is clean, with a warning that is right to keep turned
   off where it arises and its reason beside it, and `cargo` builds deny warnings the same way.
6. **A contact that says how hard its pair hit, and joints with limits**, from 3DEngine's
   `c5227118`: the speed a pair closed at on the contact's message, a ball joint kept within a
   cone, and a distance joint whose range changes after it is made.
7. **Two more of a character and a collider**, from 3DEngine's `52579d98` and `454e9276`: a
   character that crouches and stands from its component's height, and a collider that is the
   shape of the meshes an entity and those under it show, made once the model has loaded.
8. **A joint in a scene file, and a pad's sensors**, from 3DEngine's `e46058fc` and `73ce6326`:
   a joint as an entity naming its two bodies, so a level hangs a door where it stands, and a
   gamepad's gyro, accelerometer, touchpad and light where gilrs offers them. Two small things
   of the command line are checked in the same batch and taken if they are missing: `entity.set`
   writing a field that holds a list from items split by semicolons, a command that pretends
   files dropped on the window, a command's parameter with a default being left off, and a
   placed scene file spawned again when it is written while the level runs.

## Verdicts

1. **The captures lose what they are for** (`build/capture-example.sh`, since `661682e` and
   `e3bca6b`). The owner noticed that what the newer captures show is smaller. Two things in the
   script do it, and both were looked at in the pictures.
   - A 2D or interface example is drawn at 1280 by 720, which is right, since it lays out in
     pixels for Bevy's window, and is then made 800 by 450, so everything in it is five eighths
     the size it was drawn at. In `borders.png` the label under each node cannot be read. These
     captures are kept at the size they were drawn at. The README shows every capture 200 wide
     whatever its size, so nothing there changes.
   - Every capture is cut to a palette of 256 colors, and the script's comment says no reader
     can see the difference. `atmospheric_fog.png` shows it as rings around the sun, where the
     picture drawn has a smooth glow, and any sky, fog, bloom or soft shadow bands the same way.
     A flat interface loses nothing to a palette, and a lit scene does. The 3D captures are
     kept in full color at 800 by 450, and the palette stays for 2D and interface captures only.
   What is compared with Bevy's pictures, and later with checked-in references, is then what
   was drawn. `grid.png` is 600 by 450 because Bevy's window for it is 800 by 600, which is
   right and stays. Every capture is taken again, and EXAMPLES.md's opening says what size each
   kind is. The repository grows by the 3D captures' full color, some tens of megabytes at the
   table's full count, which is accepted for pictures that show what the engine draws.

## Decisions

1. **Commits stay local.** The owner pushes `main` from their own tools, and the working session
   commits and does not push, as COMMITS.md and AGENTS.md say. An earlier entry here that allowed
   pushing is withdrawn by the owner.
2. **AGENTS.md's bullet on SHARED.md is the owner's.** They approved it on 2026-10-04, and it is
   committed like any other change.

## Replies


- Item 2. The rename of `CLAUDE.md` is left uncommitted until the owner confirms it in the working
  session, since that file holds the working session's own instructions and a change to it is
  taken from the owner there. Nothing else in this batch names the file.
- Shared: observers, in the batch after `bb1ae57`. A game's own event triggered from C# runs the
  observers of every such event and then those of its entity, and one marked as propagating goes
  on up the parents, changed as an observer changed it, until one stops it. A C# component's
  addition, insertion, discard, removal and despawn are Bevy's observers calling back with the
  entity and the value, a removal's value being the one that went, before the call that made the
  change returns. `observers`, `observer_propagation` and `removal_detection` are written with it.
