# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it already keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read.

Reviewed up to `29ebd78`. The captures at Bevy's size as WebP are settled: 110 pictures in 2.2 MB,
where the full color ones were 8.7 MB. The pictures opening Bevy's live demos (`29ebd78`) are
settled too, 276 addresses that answered kept in `BevyCSharp.Examples/bevy-live.txt`, checked in
the README and the table. `visibility_range`, `mesh_ray_cast` and `shadow_biases` (`d951f50`,
`f54a284`) were taken on their descriptions. The table stands at 122 written, 7 written in part,
117 that can be, 118 missing and 57 that do not apply.

## Now

Item 1 is the owner's request of 2026-10-04 and goes on a group at a time. Items 4 to 6 are
taken from [SHARED.md](SHARED.md).

1. **Bevy's examples, what is left of the pass.** Math, then the rest of 3D, an example that
   needs something missing marked and passed over. A picture that differs from Bevy's for no
   known reason is taken down to the smallest scene that still differs and explained before
   the pass goes on, as `grid` was, since that is where the table finds a fault.
2. **The gaps, by how many rows each holds**, once the groups in item 1 are through, each
   bridged from Bevy with the examples it unlocks written in its batch. By the table as it
   stands: 2D meshes with their color materials (13 rows); an order among the systems of one
   Bevy's
   resources reached through reflection as its components are, which `UiScale` and others wait
   on and which is likely a small bridge for many rows; then text gizmos, editable text and
   Bevy's widgets.
   When the captures have settled, they are compared whole with checked-in references by the
   workflow, a small share of pixels allowed to differ between devices, as 3DEngine's
   `771f10e9` does for its scenes, so an example that stops drawing as it did fails a run.
3. **A build with no warnings, and a warning failing the workflow.** The managed build passes
   `-warnaserror` in the workflow once it is clean, with a warning that is right to keep turned
   off where it arises and its reason beside it, and `cargo` builds deny warnings the same way.
4. **A contact that says how hard its pair hit, and joints with limits**, from 3DEngine's
   `c5227118`: the speed a pair closed at on the contact's message, a ball joint kept within a
   cone, and a distance joint whose range changes after it is made.
5. **Two more of a character and a collider**, from 3DEngine's `52579d98` and `454e9276`: a
   character that crouches and stands from its component's height, and a collider that is the
   shape of the meshes an entity and those under it show, made once the model has loaded.
6. **A joint in a scene file, and a pad's sensors**, from 3DEngine's `e46058fc` and `73ce6326`:
   a joint as an entity naming its two bodies, so a level hangs a door where it stands, and a
   gamepad's gyro, accelerometer, touchpad and light where gilrs offers them. Two small things
   of the command line are checked in the same batch and taken if they are missing: `entity.set`
   writing a field that holds a list from items split by semicolons, a command that pretends
   files dropped on the window, a command's parameter with a default being left off, and a
   placed scene file spawned again when it is written while the level runs.

## Verdicts

None open.

## Decisions

1. **Commits stay local.** The owner pushes `main` from their own tools, and the working session
   commits and does not push, as COMMITS.md and AGENTS.md say. An earlier entry here that allowed
   pushing is withdrawn by the owner.
2. **AGENTS.md's bullet on SHARED.md is the owner's.** They approved it on 2026-10-04, and it is
   committed like any other change.

## Replies


- Verdict 1. Taken. Every capture is drawn at 1280 by 720, or at the example's own size as
  `grid`'s 800 by 600, and written as WebP, lossless for a flat one and quality 85 for a 3D one.
  They are taken again from the drawing rather than converted, since the flat captures taken
  before had been cut to a dithered palette, which scattered specks over `borders.png`'s plain
  ground. The script writes WebP with ImageMagick, or with libwebp's `cwebp` where ImageMagick
  was built without it, and the workflow installs libwebp's tools for that. The capture run's test
  for a blank picture counts colors through the same tools. `audio` and `soundtrack` draw
  nothing by design, with no camera, and join the examples the run does not hold to showing
  something.
- Item 2. Done. `build/examples-table.py --live` asks bevy.org for each example Bevy's metadata
  marks for the web and writes those that answer to `BevyCSharp.Examples/bevy-live.txt`, 276 of
  278, the other two being the widgets helper and the no_std library, which have no page. A row
  of EXAMPLES.md gains "live in Bevy" and a README picture links its live page, and both say it is
  Bevy's Rust original. The link check passes over those addresses.
