# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it already keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read.

Reviewed up to `e98b3b0`. An order among systems (`bb1ae57`), observers with three examples
(`26c0a16`), the rename committed (`e807089`) and the page comparing this engine with Bevy
(`e98b3b0`) are settled, on the descriptions and the replies, which were read. The captures
taken again in the working tree were looked at, and the verdict below is what is left of them.

## Now

Item 1 is the owner's request of 2026-10-04 and goes on a group at a time. Items 5 to 7 are
taken from [SHARED.md](SHARED.md).

1. **The verdict below**, since every capture taken from here on is taken the same way.
   Then **Bevy's examples, what is left of the pass.** Math, then the rest of 3D, an example that
   needs something missing marked and passed over. A picture that differs from Bevy's for no
   known reason is taken down to the smallest scene that still differs and explained before
   the pass goes on, as `grid` was, since that is where the table finds a fault.
2. **A click on an example opens Bevy's live demo of it**, which the owner asked for on
   2026-10-04. Bevy hosts its examples running in the browser, and each example here is the
   same scene under the same name, so its picture can open the original. It adds no file to
   host and nothing to history, which is why animated captures were turned down.
   - Bevy's site addresses an example as `https://bevy.org/examples/<category>/<example>/`, both
     parts in lower case joined by hyphens (`3d-rendering/3d-scene`, `2d-rendering/sprite`,
     `ui-user-interface/borders`, `games/breakout`), from the `category` and the name that
     `build/examples-table.py` already reads, for the examples whose metadata says
     `wasm = true`. The site shows Bevy's newest release and the table is built from 0.19.1, so
     the pattern is not trusted. The script, run by hand with a flag such as `--check-live`,
     asks the site for each address and writes those that answer to
     `BevyCSharp.Examples/bevy-live.txt`. A normal run reads that file and touches no network.
   - EXAMPLES.md: a row whose example has a page gains a link reading `live in Bevy` beside
     the link to Bevy's source. The opening says in a sentence that it runs Bevy's Rust original
     in the browser, which this engine's example draws the same scene as.
   - The README's gallery: a picture is a link to the live page where there is one and to the
     C# source where there is none, the caption keeping the link to the C# source, with a line
     above the gallery saying what a picture opens.
   - The link check in the workflow passes over `bevy.org` addresses, since they were checked
     when the list was made and a site that is down is not this repository's fault. The list is
     made again when the bridge moves to a new Bevy.
   - Verified by `3d_scene`'s picture opening `https://bevy.org/examples/3d-rendering/3d-scene/`,
     by an example with no browser build having no live link, and by EXAMPLES.md building the
     same with the network off.
3. **The gaps, by how many rows each holds**, once the groups in item 1 are through, each
   bridged from Bevy with the examples it unlocks written in its batch. By the table as it
   stands: 2D meshes with their color materials (13 rows); an order among the systems of one
   Bevy's
   resources reached through reflection as its components are, which `UiScale` and others wait
   on and which is likely a small bridge for many rows; then text gizmos, editable text and
   Bevy's widgets.
   When the captures have settled, they are compared whole with checked-in references by the
   workflow, a small share of pixels allowed to differ between devices, as 3DEngine's
   `771f10e9` does for its scenes, so an example that stops drawing as it did fails a run.
4. **A build with no warnings, and a warning failing the workflow.** The managed build passes
   `-warnaserror` in the workflow once it is clean, with a warning that is right to keep turned
   off where it arises and its reason beside it, and `cargo` builds deny warnings the same way.
5. **A contact that says how hard its pair hit, and joints with limits**, from 3DEngine's
   `c5227118`: the speed a pair closed at on the contact's message, a ball joint kept within a
   cone, and a distance joint whose range changes after it is made.
6. **Two more of a character and a collider**, from 3DEngine's `52579d98` and `454e9276`: a
   character that crouches and stands from its component's height, and a collider that is the
   shape of the meshes an entity and those under it show, made once the model has loaded.
7. **A joint in a scene file, and a pad's sensors**, from 3DEngine's `e46058fc` and `73ce6326`:
   a joint as an entity naming its two bodies, so a level hangs a door where it stands, and a
   gamepad's gyro, accelerometer, touchpad and light where gilrs offers them. Two small things
   of the command line are checked in the same batch and taken if they are missing: `entity.set`
   writing a field that holds a list from items split by semicolons, a command that pretends
   files dropped on the window, a command's parameter with a default being left off, and a
   placed scene file spawned again when it is written while the level runs.

## Verdicts

1. **Every capture is Bevy's size, and stored as WebP.** The captures taken again after the last
   verdict are right in what they did and leave two things the owner raised on 2026-10-04: some
   are 800 by 450 and some 1280 by 720, which looks uneven going through them, and the 3D ones
   in full color are 138 KB each where the owner asked for small. Measured on those captures:
   - A palette with dithering is larger than full color, 296 KB against 246 KB for
     `atmospheric_fog`, because the grain defeats PNG's compression. A palette without it is
     55 KB and bands.
   - WebP at quality 85 is 16 to 38 KB for the same 3D captures with no rings to be seen.
   - For an interface capture, lossless WebP is smaller than the palette PNG, 10 KB against
     17 KB for `borders`, and lossy WebP is larger and softens text.
   So one rule for all of them. Every example is drawn at Bevy's window, 1280 by 720, or the
   size the example itself asks for, as `grid` does, and stored at the size drawn. A 3D capture
   is WebP at quality 85, and a 2D or interface capture is lossless WebP. The files are
   `<name>.webp`, and EXAMPLES.md, the README's gallery and the capture script follow the name.
   The 2D and interface captures need no drawing again, only converting. The 3D ones are drawn
   again at 1280 by 720. The script's test for which kind an example is stays as it is.
   Verified by every capture being 1280 by 720 or its example's own size, `atmospheric_fog`
   showing no rings, and the folder being smaller than the 8.7 MB it is.

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
