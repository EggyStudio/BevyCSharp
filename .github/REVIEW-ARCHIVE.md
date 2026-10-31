# Review archive

What REVIEW.md no longer carries, moved here at 19:30 on 2026-10-23 and after: the items of its Now
list that are done or dropped and the verdicts that are settled, each under its number as it stood,
for BevyCSharp. REVIEW.md holds the current work, this file the record; nothing here is read before
a batch.

## Items done

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
   interface material next, the last four of the list read against the lock first. Open again in
   full on 2026-10-18 at 22:25 (Decision 38). Every gap closes, in the table's order by how many
   rows each holds, with the examples each unlocks written in its batch and the comparison kept
   green; the gaps that need a crate, Feathers' widgets and theming, the three camera controllers,
   the dev tools' three and the system information, wait each on the owner's word typed in the
   working session (Decisions 11, 12 and 20, AGENTS.md), which the working session asks for by name
   as it reaches them, taking the gaps that need no word first; the remote protocol's three stay as
   they are. One batch a commit, as before, and the bridge is complete when the table names no gap
   but those the owner declines. Read against `triage.tsv` on 2026-10-18 at 22:31, no gap is left
   that needs no word. The twenty rows not written whole are the camera controllers, five (free, pan
   and the three pan-orbit); Feathers, four (counter, gallery, virtual keyboard, number input); the
   dev tools, four (the FPS overlay, the infinite grid, the scene viewer, debug picking); the system
   information, one (`log_diagnostics` in part); `web_asset`, one; `compressed_image_saver`, one;
   the system fonts, three, which Decision 13 declined; and `monitor_info` in part, which waits on
   the owner's screens; the remote protocol's rows stay as they are. None of the crates needed is in
   `native/Cargo.lock`, and each was measured in a scratch copy of `native/` by adding the Bevy
   feature to the editor profile and comparing the lock's package names: the camera controllers add
   one package, `bevy_camera_controller`; Feathers one; the dev tools one; the system information
   five, `sysinfo` with `ntapi`, `winapi` and its two shims; `web_asset` over https twenty-one,
   `ureq` with `rustls`, `ring`, `webpki` and the platform verifiers, where plain http adds five and
   the example loads an https address; and `compressed_image_saver` seventeen, `ctt` with its five
   compressors, some prebuilt, `ispc-build-utils`, `zstd` and `fast_image_resize`. The working
   session asks the owner for each word by name in the table's order, the camera controllers first,
   then Feathers and the dev tools, then the system information, `web_asset` and the image saver,
   and bridges each as its word is typed; item 29's list, which is reading and no mend, may be
   written meanwhile. On 2026-10-19 the owner typed the words (Decision 39): the camera controllers'
   crate, Feathers', the dev tools' and the system information's come in, the https asset loading
   and the image saver are declined and their rows stay missing as declined, and `monitor_info` is
   captured on virtual monitors in a headless display and never on the owner's screens. The batches
   go in that order, one a commit. The camera controllers came at `670ecf2c` on 2026-10-19, five
   rows written, the table at 363 written and 13 missing, the lock gaining the one crate, the ABI
   286, the five examples captured and the examples' own free camera gone for Bevy's. Feathers'
   first step came at `edee9686` the same day, the core plugin, the dark theme, ten controls and the
   theme tokens, `feathers_counter` written, the table at 364 written and 12 missing, the lock
   gaining `bevy_feathers`, the ABI 287; its steps to come were the number input and the virtual
   keyboard, the gallery's controls, and the fifteen examples moved to Feathers' radio, each a
   commit with its examples. The second step came at `5452a595`, the number input and the virtual
   keyboard with their two examples, the table at 366 written and 10 missing, the ABI 289, the lock
   unchanged. The third came at `1e8c4750`, the gallery's thirty-four controls with the gallery
   written, the color conversions across Bevy's spaces, the table at 367 written and 9 missing,
   Feathers' four rows all in, the ABI 291, the lock unchanged. The last came at `319624bf`, the
   fourteen examples with option rows at Bevy's v0.20.0 on Feathers' radio, a theme call in the
   package, the port's own helper gone for one file mirroring Bevy's three shared helper files, the
   ABI 292, the lock unchanged, Feathers whole in four steps. The dev tools came at `64c1b25d` the
   same day, the overlay, the grid, the picking debug and the scene viewer with their four examples,
   the table at 371 written and 5 missing, the lock gaining `bevy_dev_tools`, the ABI 293. The
   system information came at `8ae312a4` the same day, `log_diagnostics` whole, the table at 372
   written, 1 in part and 5 missing, the lock gaining `sysinfo` and its four, the ABI 294. The two
   declined rows are marked at `b7cd83db` the same day with Decision 39's reasons, the table at 5
   missing with 2 declined. `monitor_info` came at `142dc131` the same day, its picture taken on a
   headless compositor's four virtual outputs in a container by a script under `build/`, the row
   written. Done at `142dc131`, the table at 373 written and 5 missing, 2 declined by Decision 39
   and 3 by Decision 13, no gap left that a word could open.

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
   list only getting shorter. Done at `5d955562`, the two helpers listed. The free camera helper
   left at `670ecf2c` for Bevy's controller and the radio buttons helper at `319624bf` for Feathers'
   radio, and one file mirroring the three helper files Bevy's own examples take in by path stands
   on the list with that reason, by item 29's part (k).

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
    TODO.md's open line under the components through reflection, item 4's remainder, done at
    `c3bd5349`.** Chosen on 2026-10-18 as the next work, since every other row of the Now list and
    of TODO.md waits on a crate the lock lacks, on a word the owner types in the working session
    (Decisions 11, 12 and 20; Feathers, the camera controllers, the dev tools, `sysinfo`, `ctt`,
    `ureq`, the template's publishing) or on a run on the owner's screens (`monitor_info`), and the
    font examples stay off under Decision 13. The generated wrappers type an enum inside a variant,
    a list, a duration and an integer that holds no zero, and leave an `Option` of a tuple (a text
    layout's cursor, a window's pan gesture, the focus and its cause), of Rust's `Instant` (the real
    clock's first and last update) or of a node index (animation transitions' main animation) with
    its variant names alone, the value on a string path checked when it runs; each gets a typed
    property, with a test a kind, and TODO.md's line and COMPONENTS.md follow. One commit, the eight
    register rewordings riding along. Done at `c3bd5349` as asked, a test a kind and the lists of
    tuples left named in COMPONENTS.md.

27. **Byte mirrors for the components that are plain data, COMPONENTS.md's third tier, TODO.md's
    next open line, in steps, done at `1a6c5d22`.** Chosen on 2026-10-18 at 03:10 over the smaller
    atmosphere line, by the list's order, the working session having measured that 141 of the 352
    described components hold only fixed-size fields or fieldless enums. The build generates a Rust
    export a type reporting its size, alignment and each field's offset, which replace the hand
    layout functions, the generator writes a byte mirror for each type that qualifies, and
    `bcs_component_id_of` resolves any reflected path, its short-name arms leaving with the hand
    mirrors. The first step is a commit of its own and bounded, since `offset_of!` from the bridge's
    crate wants each field public and a Bevy type with private fields does not qualify, two of the
    five hand mirrors perhaps among them: it counts the qualifying types with public fields, emits
    their layout exports and the checks that hold them, and reports the count and the types left out
    before the mirrors are written; the mirrors and the lookup by path follow in commits of their
    own, the hand mirrors staying for the types that do not qualify. The atmosphere's custom medium,
    a bridge call, a settings record, a test and the guide's line, is the line after. Changed at
    03:10 on the working session's finding, before the first step. `offset_of!` compiles only where
    every field on the path is public from the bridge's crate and reaches no field inside a variant,
    so choosing the types would mean parsing Bevy's sources; instead the layout is read through
    reflection on a value made at its default, a reflected field being a reference into the value
    whose address less the value's is the offset the compiler chose, private fields included, which
    a spike on `Transform` gave as 0, 16 and 28, `offset_of!`'s answers, with the size and alignment
    from the component's registered layout. One export, `bcs_reflect_layout` of a type path, an ABI
    bump; the mirrors are generated from layouts dumped into the description as the wrappers are,
    and a test of the suite and a check at a mirror's first use compare the dumped layout with the
    running bridge's measurement, refusing on a difference, so a Bevy upgrade that moves a field
    fails a test and not a game's bytes. The first step holds the export against the hand layouts,
    `Transform`'s three offsets, `GlobalTransform`'s four, `Visibility`'s size with its
    discriminants staying on their own function, and the two visibility types whose field is
    private, and counts of the 141 how many have a default and every byte in a reflected field or
    padding, naming the types left out and why, no default, an ignored field or a variant's field.
    The hand mirrors stay for a type reflection cannot measure, and COMPONENTS.md's bullet says the
    layout is the compiler's, read from a value rather than written at build time. The first step is
    done at `5f8e4e07`, the export holding every hand layout and 115 of 140 candidates mirrorable,
    24 left out for a variant's value and 1 for an ignored field; step two, the dumped layouts, the
    generated mirrors and the two checks, follows. The second step is done at `9b539deb`, the 115
    layouts dumped, 100 mirrors generated with 15 kept on their wrappers for an entity, a duration,
    a range or a color, the suite's test and the first-use check in place; step three, the lookup by
    path retiring the hand mirrors, follows. Step three's shape, chosen at 04:45 on the working
    session's reading. The five hand types stay, being the library's own surface, `Transform` with
    its constructors and methods in 353 files, `GlobalTransform` in 25 and the visibility types in a
    score, and what retires is the hand machinery under them, the three hand layout exports, the
    three verify functions with the size check through the component layout, and the five short-name
    arms of the id lookup, an ABI bump; each hand type names its component by its reflected path and
    carries the description's layout, so the first-use check and the suite's test hold it as they
    hold a generated mirror, a test holds each hand type's declared offsets to the description's
    signature, and the generator writes no mirror for a component a hand type covers, the five
    generated at `9b539deb` leaving the surface. The arms for the name-only handles, the child and
    children relations, the world instance and the atmosphere, and for the bridge's own interaction
    type stay. The third step is done at `1a6c5d22` as chosen, the hand types kept on the
    description's layouts and the hand machinery gone, which ends the item, 95 generated mirrors
    beside the five of the library's.

28. **An atmosphere takes a medium of the game's own, TODO.md's rendering line, one commit, done at
    `c5f19669`.** Chosen on 2026-10-18 as the line after item 27. Bevy's `ScatteringMedium::new`
    takes a game's own scattering and absorption terms where the bridge offers earth's and Mars's
    alone; a bridge call, a C# settings record, a test that a custom medium draws otherwise than
    earth's, and the guide's line. Done at `c5f19669` as asked, with a curve or a picture for a
    falloff or a phase left named in TODO.md.

29. **What was mended for the time being is mended for good (Decision 38), after item 4.** The owner
    said on 2026-10-18 at 22:25 that once everything is bridged, the places where a mend of the
    moment stands are to be mended fundamentally. First the list, in a commit of its own: every
    place mended for the time being, in the code's comments and TODO.md's lines, and in the
    verdicts' mends that bounded, listed or said a figure in place of removing a cause, among them
    the captures' differ lines whose cause is the machine's clock, the pictures taken on a bridge
    without Solari for the `dfg_lut` it enables, the measure's rooms and the figures said and not
    judged, each with what the lasting mend is and what it costs, written here as parts of this
    item, and the reviewer orders them with the owner. Then each part is mended on its own, one
    commit a part, measured where a figure moves (N 3.5). Listed at `2b355276` in TODO.md's section
    "Mended for the time being", thirteen parts, taken here in the reviewer's order, the cheap and
    self-contained first and the ones that are the owner's to decide last: (a) `many_sprites` and
    `many_animated_sprites` seeded under the workflow, two lines off the differ list, done at
    `024c1113` on 2026-10-19, the pictures matching in every pixel across two bridges; (b) Solari's
    panel hiding the device's timings under the workflow, its room measured again for the noise
    alone, done at `ddce5717` on 2026-10-19, the noise at 0.17 percent of pixels across devices
    where the line allowed 3, and Mesa 26.2.2's lavapipe drawing Solari whole, evidence for (h); (c)
    the soak's heap judged after a compacting collection in `memory.collect`, the managed total
    dropped from the answer, done at `30b87d1f` on 2026-10-19 with a second cause mended, the CLI
    server's accept buffer pinned while it waited, the heap within 8,672 bytes of live over
    ten-minute soaks of three games; (d) the soak's other rooms measured from soaks of the three
    programs, the process figure read as private memory without the driver's mappings, done at
    `b9069e8c` on 2026-10-19, each room twice the most its figure moved over the soaks named in the
    check's comment, the process figure the anonymous pages on Linux; (e) a `TimeProvider` on the
    app's clock for the two async examples, their generators seeded under the workflow, two lines
    off, done at `3a2e403a` on 2026-10-19, a time provider on the game's clock as `Time.Provider`,
    the two pictures matching in every pixel across three devices; (f) the bridge's clock held while
    an asset loads and between the drive's commands, one hold serving the two animation examples and
    the drive's swing, every capture taken again, done at `714451b0` on 2026-10-19, the clock held
    while a load runs and between a served session's commands, the drive's readings identical on two
    devices and two differ lines off; (g) `dfg_lut` on in the render profile, the owner's decision,
    since games on the package are then lit a little otherwise and every 3D picture is taken again
    (Decision 25), done at `93600e85` on 2026-10-19, the lookup table's feature on in the render
    profile with no crate added and the lock unchanged, eleven pictures retaken on a bridge with it,
    the three that stood apart between bridges within a pixel in the runner's image; (h) a newer
    Mesa on the runner with a `--meshlet --solari` bridge for the six uncompared examples, the
    owner's decision on the jobs' image; (i) lavapipe's dropped bunnies traced and reported to Mesa,
    outside the repository, and found at `714451b0` on the GPU as well, a different subset of the
    2,500 bunnies drawn at each take, so the differ line's reason is incomplete; (j) the anisotropy
    fault reported to Bevy, the owner's decision, a patched shader declined; (k) the two helpers go
    with item 4's gaps; (l) runners with a graphics card, the owner's purchase; (m) the norm's lists
    are item 2's. The mends follow item 4 as Decision 38 orders, unless the owner says the parts
    that need no decision may start while the words for item 4 are awaited. On 2026-10-19 the owner
    decided (Decision 39): item 4 comes first and then this item in the order above; (g) `dfg_lut`
    goes on in the render profile with the lock byte for byte the same; (h) and (l) are one, the
    capture jobs in a container of a distribution image with a newer Mesa and a Linux bridge built
    with meshlets and Solari, no runner with a graphics card and no Mesa from an archive outside the
    distribution; (j) the anisotropy fault is mended by a patched shader of Bevy's in the bridge,
    which overrides the earlier decline, with a test that fails when Bevy's original changes. Part
    (k) is whole at `319624bf`, half done at `670ecf2c` with the free camera helper gone for Bevy's
    controller, the radio buttons helper gone in Feathers' last step, where Bevy 0.20's own fourteen
    examples with option rows share three files by path, a radio row of Feathers' radios under a
    group, a theme of some sixty token colors replacing the whole and, in two of the fourteen, a
    labeled number input holding a float with its precision and limit, ten of the fourteen written
    from 0.19.1 differing from their sources by 100 to 560 lines in that swap, so the reviewer
    decided on 2026-10-19 that the theme's setting from token colors is the package's own call, the
    port's helper of its own goes, and one file mirroring Bevy's helper files the examples use at
    the pinned version, the three, stays on the package script's list with the reason naming them,
    carrying what they carry and nothing of the port's own, since the list's rule is against helpers
    of the port's own and a mirror of what Bevy's examples share is the port's fidelity, not its
    shortcut; the radio buttons go with Feathers. (h) read by 03:11 on 2026-10-20 against three
    distribution images, each capturing the six from a fresh checkout on lavapipe held to four cores
    with a bridge built with meshlets and Solari, cold in 18 minutes 51 seconds, inside the native
    job's 45. Fedora 44, Mesa 26.2.4 and the .NET SDK from its own archive, draws all six,
    `clustered_decals` 2 pixels off the GPU's picture, `meshlet` 0, `test_meshlet` 467 and
    `solari_reflections` 1,407, all within 2 percent, and `many_meshlet_materials` 48,354, 5.2
    percent, inside its 10 percent line, which the GPU's own takes differing by 6.9 percent explain,
    so the line stays with that reason; `solari` needs some 13 minutes for its 400 frames, past a
    shot's 600 seconds, so its wait rises to 1,500. Ubuntu 26.04's Mesa 26.0.8 crashes on
    `clustered_decals` as the runner's 25.2 does, and Debian trixie's 25.0.7 is older than the
    runner's, both out. But Fedora's lavapipe runs some scenes two to seven times slower than the
    runner's Mesa, `many_foxes` 316 seconds where 152, `multi_asset_sync` 331 where 57,
    `pan_orbit_camera_minimal` 332 where 75 and `error_handling` past 600 where 141, about 6.7
    seconds a frame alone, the heavy scenes alike at 200 to 290 on both, so the four shards run
    there in parallel on four cores with 40 minutes reached 65, 60, 38 and 41 of their 93 or 94
    examples and none finished, everything reached matching its picture with no allowance. The
    reviewer takes the coder's proposal under Decision 39's constraints, a container of a
    distribution image, the distribution's own Mesa and no runner with a graphics card, so the four
    shards stay on the runner's image and bridge, where they pass in 22 to 36 minutes, and a fifth
    capture job runs the six in Fedora 44 through docker from the runner with the meshlet and Solari
    bridge, some 25 to 30 minutes on the development machine, Solari's 13 of it, and compares them,
    the shards still capturing the six as words compared with nothing; the native job's matrix gains
    the Linux entry with meshlets and Solari, uploaded under a name that is not `native-*`, so the
    pack never packs it; the Mesa version is named in the workflow and asserted when the job starts,
    so a change of the distribution's Mesa is a commit that names it with the six retaken, not a
    silent shift of the reference; the job's limit carries its reason (N 6.8), the Solari wait its
    reason in the script's remark, and BUILDING.md says which image draws which pictures. One
    commit, with the SHARED.md row for the reviewer to write. The alternative, every capture in
    Fedora on eight shards or a raised budget, costs two to five times the capture minutes on a Mesa
    slower for some scenes, and is not taken; the owner is told that the shards stay on the runner's
    Mesa, a narrowing of Decision 39's words by the measured cost. (i)'s cause found by 03:14 on
    2026-10-20, and it is not Mesa's. `many_meshlet_materials` drops bunnies on every device because
    its cluster budget is too small for its crowd, Bevy's own example setting 8,192 cluster buffer
    slots for its grid of 2,500, which the port's configuration mirrors, where Bevy's own
    documentation of the field says that too low a number shows missing or blinking meshes; raised
    to 2 to the 20th in a local experiment not kept, two GPU takes on the editor bridge match in
    every pixel and the host's lavapipe comes within 1.2 pixels of them, where at 8,192 the GPU's
    takes differ by 6.9 percent and lavapipe's by 5 to 7. So no Mesa report is written. The reviewer
    decides, on the owner's precedent in Decision 39 of mending Bevy's faults in the port with the
    difference said, that the port's example takes the smallest power of two that draws the crowd
    whole on both devices, measured, the difference from Bevy's 8,192 said in its triage row with
    the documentation's sentence as the reason, so the picture is whole and compared as the others
    are, the differ line comes off and TODO.md's paragraph goes; a report of the budget to Bevy is
    the owner's call, as upstream reports are. Order: (h)'s commit first with the line's reason as
    the budget's, then (i)'s commit taking the line off, two things, two commits. (h) done at
    `17b004c0` on 2026-10-20, as the head says. (i) done at `5195e95a` on 2026-10-20, as the head
    says. (j) done at `0f8e89b4` on 2026-10-20, as the head says, and no part of the item is open.

30. **The templates package is published beside the engine, and the README's install line follows
    (Decision 39).** The owner decided on 2026-10-19 that a pack run that publishes pushes
    `BevyCSharp.Templates` beside the engine's package, which `pack.yml`'s glob over the packed
    files already takes, and that the README's `dotnet new install` line stands once nuget.org lists
    the package. Checked by the publishing pack run's page naming both packages and the README's
    line tried from nuget.org. Done on 2026-10-19 at 22:01 with no commit: the owner uploaded
    `BevyCSharp.Templates` `0.5.120` beside the engine's `0.5.120`, and the README's two program
    blocks ran as written in a clean container of the SDK's image with nothing of the repository's,
    the install from the feed, the project made from the template with the README's first program to
    the letter, a clean build, 120 frames offscreen on lavapipe with exit 0, and the guide's empty
    template built and run the same; no document said the templates waited, so nothing changes.

31. **The README's centered parts are markdown inside one centering block (Decision 40), and
    nuget.org draws them.** The heading, the description and the banners at the README's head stand
    as markdown inside one `<div align="center">` with blank lines inside it, no `<h1>`, `<p>` or
    `<img>` of their own, and every other HTML line of the README is read for whether markdown says
    it, the gallery's tables staying HTML unless thumbnails at the shown width make a markdown table
    render alike on GitHub and nuget.org, measured first; checked on GitHub's rendering and on
    nuget.org's page of `0.5.105` for what it draws today and of the next version uploaded,
    `build/examples-table.py` writing the same. Done at `b68da75c` on 2026-10-19, the head one
    centering block of markdown and the gallery markdown tables of 260 by 146 thumbnails with padded
    names, measured on both sites before it was taken, nuget.org's page of the next upload the
    check. Open again on 2026-10-19 at 22:34 for the banners: the owner asked that the README's
    centered block carry the five banners 3DEngine's carries, .NET, Graphics, Windowing, Shaders and
    Status, as markdown images inside the block, each with this project's own value, .NET 10.0, the
    graphics by what Bevy draws with, the windowing by its window crate, the shaders by what the
    bridge compiles, and the status as the project stands, the labels' colors as 3DEngine's for the
    labels the two share. Done at `8fe38f60` on 2026-10-19, the five in one row with this project's
    values and 3DEngine's colors.

32. **A package's release notes are the commits since the package last published (Decision 41).**
    The pack run's notes are the commits since `build/version.txt` last changed
    (`build/release-notes.sh`), the whole minor each time, 119 lines at `0.5.120`. The script takes
    the latest version of the package the feed lists, from nuget.org's index of the package and that
    version's nuspec, whose repository commit names where that package was built, and writes the
    commits from that commit to the head, newest first, each commit's sentence a line as before;
    where the feed answers nothing, lists no version of the minor, or names a commit the checkout
    does not have, the notes are the minor's as before and the summary says why; the pack job's step
    says which commit the notes start from. Held by a test of the script on a scratch history with a
    feed answer given to it, and read on the next package's page. One commit, after part (g). Done
    at `d482caa3` on 2026-10-19, 14 lines where 134 against the feed's `0.5.120`; the next package's
    page is its check. Widened on 2026-10-19 at 23:25 with 3DEngine's item 37: a commit whose every
    file lies under `.github/` is left out, since the package carries nothing from there, which
    takes the review's and the workflows' commits by what they touch, the README, the guides, the
    license and the notices staying in; a small commit after (g)'s. The rule is in at `a7895887`,
    the notes at 9 lines of the engine's alone. Proven on `0.5.149`'s page on 2026-10-20, 12 lines
    of the engine's alone.

33. **The pack run publishes through nuget.org's trusted publishing and no stored key (Decision
    42).** The pack job gains the identity token's write permission beside its read of the contents,
    a step with the feed's login action, `NuGet/login@v1`, given the nuget.org user name the policy
    belongs to, `EggyStudio` unless the owner says another, which answers a short-lived key, and the
    push step takes that key where it took the secret, the input's description and the head comment
    saying the run publishes through the trusted publisher policy and needs no secret, BUILDING.md's
    publishing section saying the same with the policy's values, workflow `pack.yml`, no environment
    and the package pattern; the step with the secret goes. Checked by a pack run with publish on,
    whose push the feed accepts, the first the owner starts after the policy stands. One commit, the
    pattern `BevyCSharp*`; at 22:54 the owner asked whether the pack could upload by itself, so this
    comes first, right after the step of (g) in hand, and item 32 with it. Done at `e1f516fa` on
    2026-10-19; the first pack run with publish on is its check. Proven on the pack run of
    `3eaa0a44` on 2026-10-20 at 02:00, the login and the push passing and `0.5.149` on the feed.

34. **A behavior's write after a call that moves its entity's table row lands in another entity's
    row.** Found under Verdict 38 on 2026-10-20: a behavior holds its own table row by reference
    while it runs, and a call it makes that inserts a table component on its entity, or takes one
    off, moves the entity to another table and the old table's last row into its place, so what the
    behavior writes after the call lands in that other entity; in `many_foxes` half the foxes'
    started flags landed in other foxes' rows and 499 played twice. Verdict 38's mend makes the
    animator's two components sparse, so a play keeps the row, and names what stays: the graph play
    and the clips insert Bevy's own animation player and graph handle on the entity the first time,
    Bevy's types, whose storage the bridge cannot choose, and a behavior calling them in its update
    would move its row the same way. The item is the general mend, read first for its form and cost:
    every call a behavior can make that changes its own entity's archetype found by reading the
    bridge's entry points, and either the change deferred to the end of the behavior's run, as
    Bevy's own commands are, or the runner resolving the row again after each such call, with a test
    that a behavior playing a graph on its own entity keeps what it writes; the reading says which,
    with the cost. One commit, after Verdict 38's. Read by 07:06 on 2026-10-20. Every bridge entry
    point reaches the world through one module's two functions and no other path, and behind them
    stand about 300 insert and remove sites, the explicit structural calls, the setters that insert
    where missing, meshes, materials, lights and cameras, the animation calls, which insert Bevy's
    player and graph handle and the targets' ids, and the hooks and observers a call fires;
    deferring them would change what each answers within the same body, as an insert followed by a
    read does in the animator, so the form is the runner resolving the row. The bridge keeps a
    per-thread row guard, the entity, the address of its component as the runner handed it, the
    component's id and a moved flag, which the runner gets a pointer to once a run through a new
    native call; at each exit from the world while the guard holds an entity the bridge looks the
    component's address up again and sets moved where it differs, the entity moved to another table,
    the column grown and moved, or the entity or component gone. The sequential loops, one component
    and with others, hand the method a copy of each component it takes by reference and write the
    copy back to the row when moved is clear, and by entity where it is set and the entity still has
    the component, then list the runs again and go on from the row after the entity visited before,
    each moved entity in a set the rest of the run skips, so none is visited twice and the entity
    that took its row is visited; the parallel path changes nothing, since the world is thread-local
    and a method on a worker gets no world from every call. The cost falls on the sequential path
    alone, under 4,096 entities a chunk, on the main thread or under a changed filter, a copy of
    each reference component in and out, three stores to the guard and one load an entity, every
    native call a thread-local load and a branch and one entity lookup more while a guard is set,
    and a move a listing again and a search; the stress movers run parallel from a few thousand on,
    so PERFORMANCE.md's table does not move. Promised is a call that changes the running entity
    itself, its writes landing, each entity visited once and memory valid when a column grows; a
    body that changes other entities directly is still told to queue them, as the behaviors guide
    says. The reviewer takes the form, with the cost measured in nanoseconds an entity before and
    after on a sequential run with a small component and a large one, recorded in the remark; the
    tests as proposed, a behavior playing a graph on its own entity keeping what it writes, one
    adding a component to its own entity through the context keeping its write and visited once with
    the rest, the moved entity not visited again in the frame, and one more where the insert grows
    the column so the row the runner handed out moves, the write landing and nothing read through
    the old address; and the behaviors guide saying what is promised and what is not. One commit,
    carrying Decision 44's sentence. Done at `20f3d5c0` on 2026-10-20, as the head says; the
    sequential walk's cost about three times an entity for the promise, the parallel walk's none.

35. **The build workflow's jobs are named as 3DEngine's are, by their ids, with no expression in a
    name.** The owner wrote on 2026-10-20 that a push run's page shows the pack-only jobs with their
    name templates unexpanded, `capture the examples (${{ matrix.shard }} of 4)` and `native (${{
    matrix.rid }}...)`, since GitHub skips a matrix job before it expands the matrix and prints the
    name as written, where 3DEngine's skipped jobs show plain names because its jobs carry no `name`
    and no matrix. At 07:10 the owner left the shape to the reviewer, for a graph that reads as
    3DEngine's. So: the suite job keeps its matrix, which is never skipped, and names its three
    entries `linux`, `windows` and `macos` through an include and the entry's system in its `name`,
    as 3DEngine's suite jobs are named, since a name's expression expands wherever the job runs;
    every pack-only job drops its `name` and shows its id, `native`, `captures` for the four shards,
    `captures-fedora`, `games`, `drive`, `storm` and `readme`, GitHub appending a matrix entry's
    values where the matrix runs and a skipped job showing its id alone; `digest` shows its id too.
    The matrices stay where they save copies, the native job's seven targets and the four shards,
    since a matrix entry is one node of the graph as a job of its own would be and the steps are one
    text; the suite's steps, which branch on the system five times, stay one text for the same
    reason. The graph then reads as 3DEngine's, the three suites, the digest and the examples on a
    push with the pack-only jobs skipped by id, and the whole set on a pack run. The workflow tests
    follow the ids, and BUILDING.md's job names with them. One commit, after item 34. Done at
    `41ebc700` on 2026-10-20, as the head says. Proven on the build run of `41ebc700` at 11:40 on
    2026-10-20, the page as the item says.

36. **A program reads the force a joint held its bodies with over the last step, from 3DEngine's
    `GetPhysicsJointForce`.** SHARED.md's row, taken on 2026-10-20 since this engine simulates its
    bodies over the same library on the managed side and the flat API reads a contact's impulse and
    no joint's: a bridge game in 3DEngine could not make a weak bridge break where it is weak, since
    the joints' give under load reads alike for a truss and a bare road, where the solver's
    accumulated impulse for the joint's own constraint over a substep is the force in newtons. So
    the physics API gains the read, the impulse magnitude of the joint's constraint over the last
    step, its limit and motor apart, costing nothing until called, with its cheat sheet line, the
    surface listing, the upgrading page's entry and the physics guide's paragraph on joints, and a
    test of a box hanging on a distance joint reading its weight within a percent at the substep
    counts the engine runs. One commit, carrying the review's files. Done at `d56e7cb7` on
    2026-10-20, as the head says.

37. **The feature test moves into the games folder, as 3DEngine's lives (Decision 45).** The owner
    wrote on 2026-10-20 at 23:21 that 3DEngine's feature test under its games folder looks good and
    BevyCSharp is to do the same: `BevyCSharp.FeatureTest` moves to `games/FeatureTest` with its
    history kept by a move git follows, and every place that names the old path follows it, 58 files
    today, the solution, the workflow's drive and storm jobs, the drive, soak, scenes and publish
    scripts, the answer script, the editor's project and its play, the CLI's launch and tools, the
    tests that name it, BUILDING.md, TODO.md, the guides and AGENTS.md's table row. Read first what
    the games' build properties apply to a project under that folder, since the games build on the
    packed package where the feature test tests the tree through a project reference, and say
    whether it keeps the tree's reference by opting out of those properties in its own project file,
    said in its README, or builds on the package as 3DEngine's does with the drive job packing
    first; the first keeps the drive's time and what the test is for. The feature test stays out of
    the README's gallery as before. One commit, the move alone with the references, carrying the
    review's files; the tests that name the path pass after it. Done at `cf70c650` on 2026-10-21, as
    the head says.

38. **An edit mesh a program shapes by operations, with a recipe that rebuilds it, built into what
    the engine draws and collides with (Decision 46).** The owner asked for modeling in both
    engines, Blender's kind, from the command line as well as in a game, grid prototyping first;
    this item is the kernel the rest stands on. An edit mesh is a polygon mesh of vertices, faces as
    loops of vertex indices of any count, a texture coordinate at each face corner, a material slot
    and a smooth flag a face, and edges read from the faces; a selection of vertices, edges or faces
    as sets; and its history as the list of operations that made it, the recipe, so undo drops the
    last line and rebuilds, and a saved model is its recipe, a text file `*.model` of one operation
    a line in the words the commands of item 40 use, read back and rebuilt at load, so a level's
    models are small files a reader understands and a diff reads. The operations of this item: new
    and free; the primitives box, plane, cylinder, cone, wedge, stairs with a count of steps, a
    rise, a run and a width and closed underneath, and arch, each placed by its lowest corner or its
    center on the grid; select all, none, invert, faces by index, faces by the direction their
    normal faces nearest (`+y`, `-x`), vertices and faces inside a box, and the faces sharing a
    material slot; move, rotate about an axis through the selection's center or a given pivot, and
    scale, of the selection, snapped to a grid step the mesh holds (one meter by default, any power
    of two of it down to a sixteenth); extrude faces along their normals by a distance or by a
    vector, the sides filled; inset faces by a distance; delete the selection, the faces left open
    or the hole filled; merge vertices within a distance; flip the selection's normals; triangulate;
    duplicate the selection in place; and join another edit mesh in. The build: faces triangulated
    with ear clipping, so a concave face triangulates right, normals per face or averaged where the
    smooth flag is set and the edge's angle under a threshold, tangents from the texture
    coordinates, one mesh a material slot, through `Modeling.Build`, a mesh handle a material slot
    through `CreateMesh` from a `MeshData`, written again in place through `WriteMesh` after a
    change, so an entity drawn with it follows the edit, and a collider through a
    `ColliderShape.Mesh` over the built mesh. The surface is a static class `Modeling` beside
    `Meshes` and `MeshFiles`, over a sealed class `EditMesh`, each call with its cheat sheet line
    through the script and the surface listing, and a guide page `docs/modeling.md` with a section
    an operation showing the recipe and what it makes. Tests: a test an operation on exact counts of
    vertices, edges and faces, the signed volume of a closed mesh (a two by three by four box 24, a
    stairs the sum of its steps), and watertightness, every edge shared by two faces with opposite
    winding, and a recipe written and read back making the same counts; a capture of each primitive
    in the gallery row. Two commits, the mesh and the primitives with the selection and the
    transforms, then the topology operations, the recipe, the build and the collider. Done at
    `08d03b4d` and `a1babb72` on 2026-10-21, the mesh, the primitives, the selection and the
    transforms with the recipe's words in the Reply, then the topology, the recipe, the build, the
    collider and the feature test's row, as the heads say.

39. **Grid prototyping: materials that show a meter, texture coordinates aligned to the world, and
    openings cut by booleans (Decision 46).** The owner's first use: blocking out a level with
    textures of different colors showing a grid of about a meter, stairs, and windows cut by boolean
    operations. The engine makes the grid image, `Images.Grid(int pixelsPerMeter, Color face, Color
    line, Color fine)`, an image of one meter's cell with its edge lines and lines every tenth of a
    meter in a finer tone, 1024 pixels a meter by default with mipmaps, and a prototype material
    from a color, the grid image tinted, in a small set of named colors for floors, walls, ceilings,
    stairs, doors, windows, and a hazard, so a level reads by color as the dev textures of the
    engines of the 2000s read; a face's texture coordinates by world-aligned box mapping, each face
    projected along the axis its normal faces nearest, in meters, so the grid shows one meter
    everywhere and continues across faces, which is the default of every primitive and of every face
    a boolean makes. Booleans: union, difference and intersection of two edit meshes, the second
    placed by a transform, written in the engine as the binary space partition algorithm of csg.js,
    each mesh split by the other's planes, the parts kept by the operation, the triangles then
    merged back into faces where coplanar and sharing edges and the vertices merged, so a window cut
    from a wall is a few faces and not a hundred triangles; coplanar faces of the two meshes handled
    by the algorithm's classification with an epsilon the mesh holds, and the limit said in the
    guide that two faces in one plane are cut as the algorithm cuts them, so an opening is cut with
    a box a little deeper than the wall. The new primitives on it: a room, a box with its inside
    hollowed by a wall thickness, a door and a window, each a cut of a box through a wall selected
    or named, a ramp. Tests: a box cut by a box through its wall has the faces counted, is
    watertight and has the volume of the two less their overlap; a union of two boxes sharing a
    face; the grid image's lines at the right pixels; the box mapping giving a two meter face
    coordinates spanning two; a capture of a room with a window and a door and stairs in it. Two
    commits, the grid image, the materials and the mapping, then the booleans and the openings. Done
    at `bfe1fc63` and `76f784af` on 2026-10-21, the grid image, the prototype materials and the box
    mapping, then the booleans, the merge and the openings, as the heads say.

40. **The modeling commands, so a level is blocked out from the terminal, and a window in the game
    to shape by hand (Decision 46).** The owner asked that all of it be possible from the command
    line, simple to use. The engine's catalog gains `model.*` commands, one an operation, each
    taking the model's name first and then the operation's measures in the recipe's words, so a line
    typed is a line of the recipe: `model.box room 8 3 6`, `model.select room +y`, `model.extrude
    room 1`, `model.cut room window 1 1 2 0.5`, `model.material room wall`, `model.undo room`,
    `model.save room resources/models/room.model`, `model.load room <path>`, `model.show room 0 0 0`
    spawning or moving an entity that draws the model and follows its edits, `model.info room`
    answering the counts, the bounds, the volume and the open edges, and `model.list`; a command
    answers the counts after it, so a script reads what it did, and `./bcs command model.<op>`
    reaches each as the catalog does. The window: an ImGui window a program draws with one call,
    `Modeling.DrawWindow(EditMesh)`, and the same window as a panel of the editor, which picks
    entities already, opened on the edit mesh an entity's mesh was built from; it lists the recipe
    with the last lines undone and redone by buttons, shows the selection's counts, offers each
    operation with its measures as fields and a button, and takes the mouse in the 3D view: a click
    selects the face under the pointer by a ray tested against the edit mesh's faces one by one,
    with shift adding and control removing, a drag along the face's normal extrudes or moves the
    selection by the grid step with the distance shown, and the hovered face and the selection drawn
    as outlines over the model, the way the voxel game's aim outlines a block. A guide section on
    blocking out a level from the terminal, as a script of `./bcs command` lines making a room with
    a window. Tests: the commands through the console host making the room of the guide and reading
    its counts, a load after a save giving the same, and the window's picking tested through the ray
    against a known face. Two commits, the commands, then the window. Done at `bee54e98` and
    `3fb5643e` on 2026-10-21, the commands, then the window with its picking and its drag and the
    editor's Model tab, as the heads say.

41. **Texture coordinates shaped and textures given to faces (Decision 46).** The owner asked for UV
    processing and adding textures to surfaces. The selection's texture coordinates by projection,
    planar along an axis or the view, box as item 39's default, cylindrical about an axis, and
    spherical; moved, scaled and rotated as a whole or about their center; set to a texel density,
    so many pixels a meter, read from the material's texture; and reset to the world-aligned
    default. A material slot takes a material of the engine's, a color with a texture and the maps
    the engine's material has, so a face is given a texture by giving its slot one, the built
    model's meshes drawn with each slot's; slots added, renamed and removed, and the faces of a
    removed slot given the first. The texture coordinates of the recipe's operations are recorded as
    operations too, so a saved recipe rebuilds them. A section of the window shows the selection's
    coordinates over the slot's texture. Tests: each projection's coordinates on a box's known face,
    the density on a two meter face with a 512 pixel texture, and a capture of a room with a brick
    wall and a wooden floor from the examples' resources. One commit. Done at `fc63b7ba` on
    2026-10-21, as the head says.

42. **A game that is a level blocked out with the tools, Blockout unless the owner names it
    otherwise (Decision 46).** A game under `games/` built on the package as the others are: a level
    of three rooms, a corridor, stairs between two floors, a window and two doors, made from one
    recipe file of some forty lines in the game's resources and rebuilt at load, in the prototype
    materials; a character walking it through the character controller as Courtyard has, the level
    solid through the built collider; Tab opening and closing the modeling window, Blender's key for
    edit mode, F1 to F3 being the engine's on the level's edit mesh so the level is reshaped while
    walking in it, the collider rebuilt on a change, and F5 saving the recipe; a sun and the global
    illumination on, so the grid walls show the light. Commands `blockout.status` and
    `blockout.autopilot` for the drive and the soak, the drive walking the stairs to the upper floor
    and reading the character's height, cutting a window by a command and reading the counts, and a
    capture; the game joins the build-all, drive, soak and storm lists and the README's gallery with
    its capture, and the guide's page names it. One commit. Read at 07:05 on 2026-10-21 for this
    engine: the global illumination is Solari, asked for when the app is made with the level's
    meshes marked ray traced, and where ray tracing is not active, the package and CI among them,
    the sun with shadows, an ambient fill and SSAO light the level, the status command saying which,
    the README's capture from whichever runs on the machine taking it and named in the commit; the
    feature test's screen-space reference stays out of the game as the guide says. Blockout joins
    the games job's build, play and soak lists, the defaults script taking it by its glob. With the
    radiance cascades coming as items 50 to 52, Blockout keeps the lighting read at 07:05 and
    switches to the cascades once item 51 exists, a line in its item then. Item 53 first, ImGui's
    pass in the render profile, so the window opens on the package. Done at `09fc95dd` on
    2026-10-21, as the head says, the booleans' line and the window's focus with it.

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
    and command test passing; the local build run of `135e43b5` is the proof. The run of `a2c8348e`,
    carrying it, green on the three systems; item 45 done.

46. **An animated capture of a run, frames taken at an interval and written as one file, and one
    made from captures on disk (Decision 48).** The owner asked at about 00:29 on 2026-10-21 for
    captures made into animations from several pictures with a time between them, a second or 250
    ms, as tooling to show things in the documentation and in devlogs. The engine gains a recorder:
    `record <path> <length> [every <step>] [scale <s>]` beside `shot` and `window.shot` in the
    catalog, taking frames from the frame being drawn at a step given in milliseconds or frames
    (`250ms`, `15f`), for a length in seconds or frames or a count of pictures (`5s`, `8`), scaled
    down by a factor where asked, each frame kept in memory as it is taken and the file written when
    the length is reached or `record.stop` is sent, with the frames' own times as the delays, so a
    step of 1 s plays at 1 s and `record ... play <ms>` plays faster or slower; the formats by the
    path's extension, `.gif` by an encoder of the engine's own in managed code, a palette of 256
    colors by median cut over all the frames with ordered dithering, the LZW of the format and a
    loop forever, and `.png` as an animated PNG, the frames as the format's frame chunks over the
    engine's PNG writer, lossless, which GitHub's pages play as they play a GIF; no library is
    taken, the dependency table allowing none for it and, for a program,
    `Capture.StartRecording(path, intervalMs)` and `Capture.StopRecording()` with their cheat sheet
    lines through the script and the surface listing; and a step of its own in the bcs client,
    `./bcs record <path> <length> [--every <step>]`, which sends the command and waits for the file.
    For captures on disk, a script `build/animate.py <out> <pictures...> --delay <ms> [--loop n]
    [--scale s]` assembles PNG or WebP captures into a GIF, an animated PNG or an animated WebP by
    their extension, with Pillow where it is present and the engine's own writers where it is not,
    so a devlog's animation is made from the captures the pack run already takes; the guide's
    section on captures shows both, with one animation of a game in the README's gallery as its
    first use. Tests: a recording of eight frames at 250 ms on the null device whose file parses as
    a GIF with eight frames and the delays right, the same as an animated PNG read back by the
    engine's reader frame by frame, a palette of a known picture within a distance of its colors,
    and the script run over three captures in the suite. Two commits, the recorder with its formats
    and the command, then the client's step and the script. One or two days, before item 38's second
    commit. Done at `585497f1` and `27b3f8eb` on 2026-10-21, the recorder, the two writers, the
    calls and the command, then the client's step, the script and the README's animation, as the
    heads say.

47. **F2 takes a screenshot in every program, to a screenshots folder beside it (Decision 49).** The
    owner wrote shortly before 03:42 on 2026-10-21 that, as in Minecraft, a screenshot is to be
    taken in the game with F2, the same for BevyCSharp, the captures put in the root or a subfolder.
    The engine gains a built-in key: F2 writes the frame being drawn as a PNG into a `screenshots`
    folder beside the program, made on first use, the file named by the time as the source names its
    own, `2026-10-21_03.45.12.png` and a count where two fall in one second, the second file `_2`
    and the third `_3` as the source's own code counts, read by the game's coder, through the
    capture the `shot` command uses; a line in the log says the path; `Capture.ScreenshotKey` moves
    it or turns it off with none, and a program that reads F2 itself still sees the press. The cheat
    sheet line through the script, the surface listing and the tools guide's paragraph beside the
    captures. Tests: a press through the input writing a file of the frame's size into the folder in
    a headless run, two in one second given two names, the key moved and turned off. One commit,
    after item 40's first commit. Where the engine has a default key of its own on F2, it moves to
    F3 as 3DEngine's performance window does, with a setting to move it to match, the move said in
    the tools guide. Done at `3d045110` on 2026-10-21, as the head says.

48. **The render recovery test reads its error count until it is one or a bound of frames.** Found
    by item 40's suite run on 2026-10-21: the test that a stopped drawing leaves the systems running
    and counts the error once read 0 errors where 1 was expected, eleven minutes into a loaded run,
    and passed alone and in the run before; it causes a validation error at frame 10 and reads the
    count at frame 39, a bound in frames on a count that arrives when the device reports, which a
    loaded machine stretches. The mend: the test reads the count each frame until it is one or a
    bound of frames well past the device's reporting, failing then with the count, under N 3.5. One
    commit, small, after item 41's first commit. Done at `5f051299` on 2026-10-21, as the head says,
    the device's reporting measured idle and under load.

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
    item. Second commit done at `d2e6ef81` on 2026-10-23, pushed at 22:20, the departures in the
    head; item 50 done.

53. **ImGui's pass moves from the editor profile to the render profile, so a game on the package
    draws ImGui.** Found by item 42 on 2026-10-21 at 07:16: the bridge's ImGui pass, the render pass
    rasterizing ImGui's draw lists, is built under the editor feature alone, the package's bridges
    are built with render for the six targets, and `Config.Gui` says it needs the editor profile, so
    a game built on the package, Blockout, Courtyard and Swarm among them, has no ImGui and the
    modeling window opens nothing on Tab; the pass needs no crate of its own, using what render
    already names, so the move leaves the lock file as it is, the editor profile keeping the file
    watcher and the reflection it is for, and ImGui staying opt-in at run time through `Config.Gui`
    so a game that does not ask pays nothing but the code. The mend: the pass and its entry points
    under render, `Config.Gui`'s words and BUILDING.md's profile table changed, the three profiles'
    cargo checks and the cross-target checks run. One commit, small, before item 42's commit, so
    Blockout on the package opens its window on Tab. Done at `45526e4b` on 2026-10-21, as the head
    says.

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
    cherry-pick, after item 50. Its first commit, `fc749b2e` on `modeling-operations` at 00:30 on
    2026-10-24, is the node with its stages, the shader and the Cargo tests of the weights, the
    reprojection and the shader, 6 files and 1,171 lines under the owner's identity, the branch's
    build run green; the setting, the guide and the readings follow as the second commit with the C
    entry points, ABI 300 to 301, and the local cherry-pick after item 50. Reported at 01:2x; its
    departures the node's place after the main opaque pass and before the transparent one, Solari's
    system and the transmissive pass being private to their crates, sky and forward-material texels
    neither filtered nor taps, non-finite light taken as none and a zero-weight tap unread, and the
    à-trous depth gradient read from surface neighbors alone after sky taps spread a NaN to a black
    floor; 8 Cargo tests, the check clean across the profiles on three systems, the six ray tracing
    tests passing on lavapipe with the node on. Its second, `62136e08` at 01:47 on 2026-10-24 under
    the owner's identity, 15 files, 376 lines, is Render.SetRayTracedDenoiser with DenoiserSettings,
    a sealed class of the history's clamp and the three widths at SVGF's defaults,
    Render.RayTracedDenoiserActive, the two C entry points at ABI 303 since main holds 302, every
    app clearing the ray tracing flags as it begins, a fault found there, the guide's passage
    rewritten with a compiled block; 1,132 passed headless, the 9 ray tracing tests on lavapipe, 11
    Cargo tests, the checks clean on three systems. The item is whole on the branch; the local coder
    cherry-picks both, the ABI line the one conflict, and takes the still means, the strafe and the
    node's time at 1080p with its budget. On main as `97440a1d` and `16614918`, pushed at 01:57 on
    2026-10-24; the readings to come. The readings at `c770351f` on 2026-10-24, pushed at 03:47, the
    demodulation mended there and the cost taken as item 59; item 54 done.

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
    `bufferElement`, `Instance`, `passUv`, where 3DEngine's modules declare their names at the top
    level and name each helper for what it is, `fieldDistance` rather than `distance`; nothing in
    Slang or the WGSL it writes asks for the namespaces, and no document records a reason beyond
    keeping generic names from colliding with a program's own. The mend: the namespaces go, and each
    helper is named for its module and its work so no plain name collides, `bufferCount`,
    `bufferElement` and `bufferStore` for the raw buffer's three, `passUv` and its kin for the
    pass's, the types plain, `Instance`, `Material`, `PoolVertex`, `PoolMesh`, `FieldInfo`, read
    against 3DEngine's names where the two engines hold the same thing so a shader reads the same in
    both; the kernels of item 50, the feature test's shaders and every example's shader change with
    it, the compute and ray tracing guides and docs/shaders.md follow; no upgrading entry, the owner
    saying at 21:32 that none is needed since nobody uses the engine yet, and this engine having no
    upgrading page. Tests: the existing shader and compute tests passing with the plain names, and a
    test that no module declares a namespace. One commit, after item 50's second commit and before
    item 54's readings, by the local coder since the kernels of item 50 are in its hands. Done at
    `fda2f62e` on 2026-10-23, the scheme and the second test in the head.

58. **The cheat sheet's headers are 3DEngine's, plain group headings and no sub-heading a type
    (Decision 58).** The owner wrote at 21:39 on 2026-10-23 that the cheat sheets are to be
    coherent: 3DEngine's lines one-liners as BevyCSharp's are, and BevyCSharp's section headers as
    3DEngine's, plain words without the code styling. Here each group opens with a `##` heading and
    a guide link, then a `###` sub-heading in code styling for every type, `BevyApp`, `App`,
    `Config` and the rest, each with its own block, where 3DEngine's page has the group heading and
    one block. The mend: the sub-headings go, each group one fenced block with its members in the
    order the types came, a type's members set apart by a blank line inside the block where that
    helps a reader, the heading words plain; `CheatsheetTests` holds the form as it holds the lines.
    One commit, small, with the local coder's next batch. Done at `66e2b067` on 2026-10-23, pushed
    at 23:09 under `fda2f62e`, each type's methods under a comment naming it inside the block.

59. **The denoiser's node comes down from 3.6 ms at 1080p, read stage by stage first.** Item 54's
    readings put the node at 3.6 ms in Bevy's Solari scene and 5.7 with every pixel filtered on the
    development machine's GPU, about what Solari's lighting takes, where an SVGF of five à-trous
    passes is usually a third of that; so the stages are timed one by one through render.timings,
    the demodulation, the reprojection, the variance, each à-trous pass and the remodulation, and
    the costly ones mended by the usual means where the readings keep: the variance estimate folded
    into the first à-trous pass, the history and the moments in 16-bit storage, the taps read
    through group-shared memory, and an à-trous pass skipped where a tile's variance lies under a
    threshold; item 54's seven still means and the strafe read again, none further from its mean by
    more than a level, the budget test tightened to the result with a margin, the guide's figures
    changed. One or two commits, by stage. After item 54, while items 51 and 52 wait. Done at
    `750c9cbc` on 2026-10-24, 3.6 to 1.87 ms, the departures in the head.

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
    reference widens taken from 3DEngine as the mends' rule. The tests are the red wall tinting a
    white block's side, a lit room's floor within the bound of its reference, and the quality
    levels' costs read on the Cornell box. Three commits, the probes and their tracing, the pass and
    the reference, then the glossy term and the emitters. It waited while the method was still
    moving in 3DEngine, its items 71, 72 and 74 open, and the owner said shortly before 07:28 on
    2026-10-21 that if it is not yet ready BevyCSharp waits, so this item starts only once those are
    mended and the voxel game's captures say the light holds, the port carrying the mends in.
    3DEngine's items 71, 72 and 74 are mended and on its main on 2026-10-24 and the voxel game's
    pictures retaken that day, so the wait ends by the reviewer's reading of the owner's condition,
    the owner's to overrule; the item starts at 07:2x with its first commit, the probes and their
    tracing. The first commit at `0f0be8f0` on 2026-10-24, pushed at 08:19, the departures in the
    head. The pass at `07863068`, pushed at 09:24, the sky's share departure in the head; the
    reference in a commit of its own next. The reference at `8cb6cbaa`, pushed at 10:22; the third
    commit takes 3DEngine's present glossy term, the reflection traced through the depth and the
    field under a roughness of 0.5 and the sky's specular scaled by the share of sky the probes see
    along the mirror direction, and the emitters. The emitters at `ccc0f000`, pushed at 12:41, the
    departures in the head, the glowing blocks' and the strip's views narrowed; the environment
    map's sky at `6e69cfa1`, pushed at 13:21; the glossy term at `c230300b`, pushed at 14:21. Done
    at `c230300b` on 2026-10-24 in five commits, the probes, the pass, the reference, the emitters,
    the sky and the glossy term, the departures in the heads of their records and in RENDERING.md.

61. **No running sentence holds a colon, across the documents, the comments and the scripts.** The
    owner wrote at about 10:34 on 2026-10-24 that some documents join sentences with a colon and
    that STYLE.md forbids it. STYLE.md's own check lists the hits, the awk over the Markdown files
    outside their code and the grep over the comments and scripts, and each is read and recast as
    STYLE.md says, a sentence of its own or the same sentence recast, a label at a line's end or in
    a table left as it is, over the guides under docs, RENDERING.md, BUILDING.md, AGENTS.md's line,
    CHEATSHEET.md's comment lines, ASKS.md and the comments of the code and the scripts; the
    review's files are the reviewing session's own and are not touched. One commit, docs and
    comments only, after item 51's third commit and before item 60. Done at `eca6e651` on
    2026-10-24, 36 joints recast, the labels and the shaders' component labels kept.

60. **The deferred G-buffer keeps the emissive's exposure.** Found by item 51's reference on
    2026-10-24: Bevy's deferred G-buffer drops the emissive color's alpha, which carries whether the
    emissive is exposed, so on a camera that the field's shadows or the global illumination make
    deferred an exposed emissive is shown unexposed, the feature test's lamp panel reading 40,053
    against a reference of 49, since item 50's deferred switch. The mend is the exposure carried
    through the deferred path, in the G-buffer's spare bits or a flag the lighting pass reads, so a
    deferred camera shows the emissive as the forward one does; a test reading the lamp panel on a
    deferred camera within the bound of its reference, failing without the mend. One commit, after
    item 51. The coder's reading at 15:13 on 2026-10-24 found the premise wrong, an exposed panel of
    40,000 nits reading 38,124 forward and 38,165 deferred, since Bevy 0.20's material uniform puts
    emissive_exposure_weight, 0 by default, in emissive.w and ignores the color's alpha, and the
    bridge never sets that weight, so the documented alpha meant nothing and only the gather of
    `ccc0f000` and the reference read it, exposing an emitter Bevy draws unexposed. The choice is
    the first of the coder's two, the bridge setting the weight from the alpha, the default alpha 0
    as Bevy's default so nothing changes unasked, the callers passing 1 for today's unexposed look
    moved to 0, the deferred mend kept for weights above 0, and the gather and the reference reading
    the weight; the item's test holds the deferred path for an exposed emitter. Done at `36d783bf`
    on 2026-10-24 on the first choice, the alpha the exposure's weight with 0 the default.

62. **The open floor test reads the same light every run.** From item 60's suite on 2026-10-24, read
    at 16:42: the open floor test under an ambient light read black once with global illumination
    off and passed in three reruns and the full rerun, so a frame was read before the floor was lit,
    the pipelines or the ambient not yet in place, as the room test's read was before it waited
    three frames of ready pipelines. The cause read, the test waiting for what it reads to be ready
    as the room test does, the wait shared by the global illumination tests where they read a first
    frame; the test run twenty times in a row green. One commit, small, before item 52. Done at
    `ce186327` on 2026-10-24, the tests waiting for the field's draws to be on the camera.

52. **The screen's probes, the glow lights and the counted meshes, 3DEngine's third phase and its
    mends of 2026-10-21 (Decision 51).** The screen's probes standing on the window's surfaces every
    few pixels beside the cascades with the filter and the history as 3DEngine's,
    `Render.SetGlobalIlluminationScreenProbes` leaving them out with the glow-only pass of
    3DEngine's item 71 so a world of small emitters pays no per-pixel march, the glow lights of
    small emitters, the field's read of a mesh a compute shader wrote with its count read back,
    3DEngine's item 69, and the field's budget in triangles a frame, its item 74; the blotches, the
    edge leak and the stepped edges read against 3DEngine's findings so this engine takes the mends
    it makes. Tests as 3DEngine's for each, and a capture of the feature test's room with the probes
    on and off. Two or three commits by part. After items 43 to 45. Waits with item 51. The first
    part at `8911be4c` on 2026-10-24, the triangle budget, its two departures in the head; the
    screen probes with the glow-only pass next, and the field's read of compute-written buffers
    waiting until the engine draws such a mesh. The second part's choice at 19:04 is the coder's
    second of three, the screen probes on by default as in 3DEngine, the bounce rooms measured with
    them off as the method's own guard so no view's bound moves, and a separate test with them on
    holding each view within 3DEngine's reading at the same size plus 15 percent, the blotches at
    160 by 90 being the tile's size and not the method's; the edge-leak mend's port with its Walled
    test in the same part. The coder's correction, the screen probes left out stand for the glow
    lights alone wherever one is lit, so the rooms' guard measures the probes traced and left
    unread, the old path exactly, and no bound moves. Done at `f0ae14ec` on 2026-10-24 in two parts,
    the triangle budget, then the screen probes with the glow-only pass and the edge leak's mend;
    3DEngine's item 69, the field reading a mesh a compute shader wrote, waits until this engine
    draws such a mesh, and its two departures stand in the head.

52. **The screen's probes, the glow lights and the counted meshes, 3DEngine's third phase and its
    mends of 2026-10-21 (Decision 51).** The screen's probes standing on the window's surfaces every
    few pixels beside the cascades with the filter and the history as 3DEngine's,
    `Render.SetGlobalIlluminationScreenProbes` leaving them out with the glow-only pass of
    3DEngine's item 71 so a world of small emitters pays no per-pixel march, the glow lights of
    small emitters, the field's read of a mesh a compute shader wrote with its count read back,
    3DEngine's item 69, and the field's budget in triangles a frame, its item 74; the blotches, the
    edge leak and the stepped edges read against 3DEngine's findings so this engine takes the mends
    it makes. Tests as 3DEngine's for each, and a capture of the feature test's room with the probes
    on and off. Two or three commits by part. After items 43 to 45. Waits with item 51. The first
    part at `8911be4c` on 2026-10-24, the triangle budget, its two departures in the head; the
    screen probes with the glow-only pass next, and the field's read of compute-written buffers
    waiting until the engine draws such a mesh. The second part's choice at 19:04 is the coder's
    second of three, the screen probes on by default as in 3DEngine, the bounce rooms measured with
    them off as the method's own guard so no view's bound moves, and a separate test with them on
    holding each view within 3DEngine's reading at the same size plus 15 percent, the blotches at
    160 by 90 being the tile's size and not the method's; the edge-leak mend's port with its Walled
    test in the same part. The coder's correction, the screen probes left out stand for the glow
    lights alone wherever one is lit, so the rooms' guard measures the probes traced and left
    unread, the old path exactly, and no bound moves. Done at `f0ae14ec` on 2026-10-24 in two parts,
    the triangle budget, then the screen probes with the glow-only pass and the edge leak's mend;
    3DEngine's item 69, the field reading a mesh a compute shader wrote, waits until this engine
    draws such a mesh, and its two departures stand in the head.

63. **The feature test's HUD is clean, a green frame rate in a corner and nothing else over the
    view.** The owner wrote to this session at about 19:30 on 2026-10-24, Decision 61, to remove the
    "frame N fps" text at the bottom left and the stray pixels in the screen's middle and to add a
    green FPS number in a top corner. The text goes, the number stands in a top corner in green, and
    the stray pixels are read first for what draws them, the cursor, a debug draw or a pass's seam,
    and mended at the cause; a capture before and after. One commit, small, after item 52. Done at
    `30362cf2` on 2026-10-24, the orb a throbber sprite and the stray marks its camera's gizmos,
    both gone, the frame rate green at the top left.

71. **A frame point after tonemapping is ordered against FXAA, SMAA and CAS.** Found by item 64's
    second part on 2026-10-24, read at 23:20: FramePoint.AfterTonemapping and the passes after
    tonemapping run unordered against Bevy's FXAA, SMAA and CAS, whose systems sit in
    bevy_anti_alias's private node modules the bridge cannot name, so an effect drawn there can be
    dropped by a fresh texture as the fog was by the vignette. The bridge orders the point by the
    graph's labels where the nodes are public, or by a node of its own placed after the
    anti-aliasing nodes in the camera's graph, or an upstream change asked of Bevy with the bridge's
    patch meanwhile, whichever the reading finds; a test drawing a mark after tonemapping under
    FXAA, SMAA and CAS each and reading it kept. One commit, small, after item 64. The coder's
    correction at 23:30, the systems are nameable, bevy_anti_alias re-exporting fxaa and cas and
    smaa a public function, so the point and the passes order after the three directly in Core3d and
    Core2d, no label, node or upstream ask; taken inside item 64's third commit with its test under
    each. Done at `d8ecd271` on 2026-10-25 inside item 64's third part, the point and the passes
    after the anti-aliasing with the test under each.

64. **The sky is photoreal after Hillaire's 2020 atmosphere, keeping 3DEngine's vibrance.** From
    Decision 61. The sky and the aerial perspective follow Production Ready Atmosphere Rendering
    (Hillaire 2020), the transmittance, the multiple scattering and the sky-view LUTs and the aerial
    perspective volume, with Cody Bennett's post as the reference for the sky-view LUT and the
    aerial perspective against Bruneton's coefficients; the sun a disc with no blurred dot, the
    lower hemisphere taking the fog's and the horizon's color as the feature test's does, 3DEngine's
    vibrance kept with its tone curve and grading, and the volumetric clouds removed; readings of
    the sky's luminance and color against Bruneton's at four sun heights, and captures at dawn,
    noon, dusk and night. The coder proposes the phases under this item when item 52 is in; the
    first lands before item 65. The coder's phases, each a commit with the suite green, are the
    medium exposed with Earth, Bruneton and ARC presets, Bevy 0.20's atmosphere being Hillaire's
    with a replaceable medium, the sun a crisp disc, the volumetric clouds out, and a per-pixel
    reference raymarch the LUT sky is read against at four sun heights with Bruneton and ARC
    pictures; then the lower hemisphere and the distance fog taking the horizon's color from the
    atmosphere so the far ground meets the sky with no gray band; then 3DEngine's tone curve and
    grading as engine settings read against 3DEngine's numbers; then, on the owner's word, the view
    from space with per-pixel transmittance as Bennett's. The third phase may take 3DEngine's
    grading keyed by the sun's height, a small timecycle as GTA V's artists key theirs, the coder's
    suggestion the owner heard. The first phase at `0ce0069d` on 2026-10-24, the two airs with their
    readings; the second next, the distant geometry and the band below the horizon fading to the
    sky's horizon color by view direction, read from the camera's sky-lit environment map in a pass
    of the engine's own. The second phase at `53583307` on 2026-10-24, the fog and the band with the
    frame points ordered; the third next, the grading keyed by the sun's height. The third phase's
    plan at 23:30, since 3DEngine's look, the voxel game's BSL curve at exposure 1.6, saturation 1.4
    in linear light after the curve, contrast 1.12 about the middle once encoded and the engine's
    knee as the default curve, cannot be said in Bevy's tonemappers, is an engine pass in the
    tonemapper's place, the view's Tonemapping set to None in the render world while a look is on,
    the look pass after tonemapping and before the anti-aliasing and the passes after tonemapping,
    Render.SetLook(camera, LookSettings) with a Vivid preset of 3DEngine's numbers and its knee,
    tests against 3DEngine's TonemapCurveTests figures, the feature test on Vivid; then, as a second
    commit, the timecycle this engine's own, LookSettings.Lerp with keys by the sun's height,
    3DEngine keying no grading by the hour. The third phase at `d8ecd271` on 2026-10-25, the look
    pass with the Vivid preset, item 71 inside it; its second commit next, the look keyed by the
    sun's height; the fourth, the view from space, on the owner's word. Done at `5041dda3` on
    2026-10-25 in four commits, the media, the fog and the band, the look pass and the look cycle;
    the view from space on the owner's word as a new item.

72. **The feature test's sample behaviors go from the source.** From the word of 2026-10-25 at
    08:18, with the console of the feature test run from games/FeatureTest on the desktop and closed
    by its window, that its final state and transforms lists are residue not to be in the builds,
    and at 08:22 that the sample behaviors themselves are to be deleted, the tests and the docs
    holding what they showed. Diagnostics, which counts spinners, lifetimes and grounded entities
    and reports them at shutdown, Movement, which spawns five moving entities and integrates them,
    Orbit, which moves entities by Bevy's Transform and parents one to another, and Spinner, a
    behavior that is its own component, are demonstrations from the program's first days and not
    zones, pages or checks of the feature test; they go with the entities they spawned and their
    console reports, and any other behavior that demonstrates alone goes with them, the coder naming
    each. The drive script, the tests and the docs are read first for a line, a count or a reference
    that relied on them and moved to what remains. One commit, with or before item 69's sixth phase.
    Done at `6795478f` on 2026-10-25, the four sample behaviors deleted with their components and
    the verbose flag, nothing found relying on them.

69. **The weather is the engine's own, lit by the atmosphere, and the vendored weather crate
    leaves.** From Decision 62, the owner's word at about 21:3x on 2026-10-24. Volumetric fog and
    volumetric clouds are written in the engine and lit by Bevy's Hillaire atmosphere as RDR2 lights
    its froxel fog and raymarched clouds together under its sky; rain, snow, thunder, wind, and the
    stars and the moon are written in the engine as well; the vendored native/bevy_weather, 0.2.0 at
    about 9,800 lines, leaves the workspace once those stand, nothing of it kept. Phases proposed by
    the coder, each a commit with the suite green, the fog and the clouds first, read against the
    crate's look and the feature test's captures; after item 64 and before 65. The coder's phases at
    01:18 on 2026-10-25, each a commit with the suite green and pictures, taken as proposed. The
    fog, froxel-based as RDR2's, a per-camera volume of about 160 by 90 by 64 at High built after
    the prepass, the density the fog's height layer with box volumes and wind-scrolled noise, lit by
    the sun through Bevy's shadow cascades and transmittance and the sky-lit map's ambient with
    temporal reprojection, integrated front to back and applied in the fog's draw slot before
    tonemapping, SetSkyFog the cheap tier and the far color, read against the crate's fog and Bevy's
    volumetric fog with the cost. The clouds, a raymarched shell at quarter resolution with temporal
    reconstruction, Perlin-Worley shape and Worley detail volumes made by compute once, a weather
    map of coverage and type moved by the wind, lit from Bevy's transmittance LUT and the sky-lit
    map, composited with the aerial perspective, their shadow a GPU-built cookie on the sun dimming
    its disc, read against the crate's captures at its presets taken before the fog and against
    Hillaire and Nubis. The clock and the sky behind the atmosphere, the time of day, latitude and
    season, the sun's and the moon's directions, the moon's disc with its phase and earthshine, the
    stars and the galaxy, the feature test's DayNight on it. The weather state, presets, blending,
    the climate and forecast, wind with gusts moving the clouds, the fog, the rain and a wind vector
    foliage reads, and ground wetness and snow cover. Rain and snow as GPU particles with an
    indirect draw, occluded by a top-down depth map so roofs keep rain out, with splashes. Thunder,
    the strikes, the flash light, the in-cloud glow, a branching bolt drawn, and the delay and the
    sound. Then the crate leaves, the workspace member and lock, the render feature,
    render/weather.rs and BcsConfig.weather with its ABI, the Weather types onto the engine's own,
    the TSV, PublicApi, CHEATSHEET, the guide, the comparisons, BUILDING, the norm's and STYLE's
    lines, NormTests' exclusions, the notices, the drive and soak scripts and the workflow's
    comment. The first phase at `7fe46ce3` on 2026-10-25, the froxel fog with its departure over the
    sky; the clouds next. The second phase's first commit at `f93cb76b` on 2026-10-25, the clouds in
    the sky with their departure; its second next, the clouds' shadows on the ground, in the froxel
    fog and on the sun's light, and the stratus deck with the fourth phase's presets. The second
    phase's second commit at `dc2ea49d` on 2026-10-25, the clouds' shadow as a cookie on the sun and
    in the fog; the third phase next, the clock and the sky behind the atmosphere. The third phase
    at `6649ab49` on 2026-10-25, the sky's clock and the night sky, the two departures in the head;
    the fourth next, the weather state with its presets, the stratus deck among them, the blending,
    the forecast, the wind and the gusts moving the clouds, the fog, the rain and a wind vector for
    foliage, and the ground's wetness and snow cover. The fourth phase at `4ac3871a` on 2026-10-25,
    the weather state with the presets, the forecast, the wind, the wetness and the snow cover,
    three departures and three faults in the head and the environment map's blue under an overcast
    kept as a gap; the fifth next, the rain and the snow. The fifth phase at `a2646e71` on
    2026-10-25, the rain and the snow with a frame point after the temporal antialiasing as its
    departure and two faults in the head; the sixth next, the thunder, then a seventh taken as
    proposed, the sky and the clouds rendered into the environment map's cube so an overcast grays
    the ground and the falling drops rather than darkening their blue, then the crate's removal. The
    sixth phase at `4cc89851` on 2026-10-25, the thunder, the bolt's program made with the storm as
    its departure; the seventh next, the sky and the clouds into the environment map's cube. The
    seventh phase at `5a8c6251` on 2026-10-25, the sky and the clouds in the environment map's cube
    so an overcast grays the ground, two departures in the head; the eighth next, the crate's
    removal. Done at `ff5fa5e9` on 2026-10-25 with the eighth phase, the crate's removal, after the
    fog and the sky through the atmosphere, the clouds and their shadow, the sky's clock and the
    night sky, the weather state, the rain and the snow, the thunder and the sky in the environment
    map, ABI 303 to 310.

73. **A capture shard's container reaps its children, and the four idle shards go.** From the
    question of 2026-10-25 at about 10:24 whether headless instances pile up over days. They do not;
    no game or engine runs headless on the machine beyond the suite running at the moment. But four
    podman containers named shard1 to shard4, started at about 00:00 on 2026-10-20 for the examples'
    captures, stand with sleep infinity as their first process, which reaps nothing, so about six
    hundred finished children, 408 shells, 183 BevyCSharp.Examples and 4 dotnet, all from 01:08 to
    01:39 that night, stand as defunct entries in the process table to this day, holding no memory
    and no GPU. The four containers are removed once nothing uses them, and the script that starts a
    shard gives it an init, podman's --init or a shell that waits on its children, so a shard's
    children are reaped; a line in the script says why. One commit, with or before item 69's eighth
    phase. Read at 10:34, the shards are not in use; they were started by hand on 2026-10-20 at
    01:07 with podman run and sleep infinity, a work script run in each by podman exec, and no
    script in the repository starts a shard, capture-in-fedora.sh running podman with --rm and
    ending with its work, so no repository line takes an init. Each shard holds an idle dotnet and
    VBCSCompiler from that night beside the defunct entries. The coder's removal was refused by its
    session's permission check and the owner is asked to remove them or allow it; the rule, a work
    container started with --init and removed as its work ends, goes into BUILDING.md's local
    recipes in one sentence, with the crate's removal. Removed at 10:37 on 2026-10-25 by the owner's
    leave, podman killing sleep after ten seconds since it ignores the term signal, the defunct
    entries gone with them and three unrelated ones left; the sentence stands in BUILDING.md's
    caches section and goes with the crate's removal, which closes this item. Done at `ff5fa5e9` on
    2026-10-25, the sentence under BUILDING.md's caches, the shards removed at 10:37 by the owner's
    leave.

74. **A texture collection for the vegetation, imported from free sources, small on disk and on the
    GPU.** From Decision 66, the owner's word of 2026-10-25 at about 13:00. The collection is
    assets/textures in the library, built by build/textures.py from a manifest naming each source
    under .ref and the recipe applied to it, the crop, the resize, the channel packing and any color
    change, so the import is repeatable and the sources stay out of the repository; an entry is
    three KTX2 files with mipmaps and Basis Universal supercompression, albedo.ktx2 with the color
    in RGB and the opacity in A, ETC1S or UASTC where the alpha has edges, normal.ktx2 with the
    tangent-space normal in RG as UASTC, and surface.ktx2 with roughness in R, translucency in G and
    occlusion in B as ETC1S, Bevy's loader transcoding them to the GPU's block format on load, the
    bridge gaining the basis-universal feature beside its ktx2 and zstd ones if the transcoder's
    size and load time allow, KTX2 with BC7 and BC5 under zstd otherwise, the coder reading both; a
    bark set tileable and at most 1024 square, a strip 512 by 1024, a leaf set one atlas of up to
    eight leaves with alpha at most 1024 square, its translucency from the source's subsurface color
    or derived from the albedo; a species at most 3 MB on disk and the collection at most 32 MB, a
    test holding both; a manifest textures.json with each entry's name, kind, size, source folder
    and recipe; a lookup in the library by name that a TreeSpecies names for its bark and its
    leaves, the generated bark and leaf masks of item 70's dressing standing where no entry is
    named. The first entries are the sorted folder's, bark for birch, grey willow, hornbeam, silver
    maple, Scots pine, Monterey cypress, acacia, linden and red maple, leaves for birch, hornbeam,
    silver maple, the three maples in fall, pine and cypress needles, and atlases cut from the
    alder, sycamore and fall leaf scans, the aspen preset taking the birch, the tupelo the alder
    leaves, the oak the sycamore leaves until an oak comes, the bush the hornbeam; the models stay
    reference and are not imported. ASSETS.md is the review's request list for what the presets
    still lack. An AGENTS.md row for ASSETS.md. The tests read the import of one source reproducible
    byte for byte, the bounds, a loaded entry's size and format, and a tree built with a named bark
    and leaves drawn in a picture. After item 70's second phase's growth and before its dressing, so
    the dressing takes the collection where it has an entry. Two commits, the pipeline and the
    format, then the entries. The owner added at 13:08 that branch cards or leaf clusters batched
    from the leaves are the review's call and that questions are welcome; so the import may also
    bake branch cards, a twig with several leaves of an atlas drawn into one card with its own
    normal and translucency, for the middle level of detail where item 70's third phase goes from
    clusters to cards, the coder choosing per species whether a card is baked or built from the
    atlas at load, said in the record. The coder's reading at 13:10, taken. Basis Universal is out,
    its crate among those the owner declined with ureq, ctt and wgpu's std, so the entries are KTX2
    holding BC7 and BC5 under zstd, which the bridge's ktx2 and zstd features read with nothing
    added; build/textures.py encodes BC7 mode 6 for the albedo and the surface and BC5 for the
    normals in plain Python with PIL reading, cropping, resizing and packing and Python's own zstd,
    no numpy, the import taking minutes for the collection, the mips keeping the alpha's coverage so
    leaves do not thin with distance. BC7 being a byte a texel, a bark's albedo is 512 by 1024 or
    512 square and a leaf atlas's 1024 square, the normal and the surface at half the albedo's size,
    about 1 MB a bark and 2 a leaf set before zstd, the bounds held after it. Branch cards are built
    at load from the atlas in item 70's third phase and not baked at import, so the collection stays
    small. Changed at 13:40 under Decision 66 as amended. The entries come from the hidden folder
    .vegetation, where the review sorted the usable sources by kind, bark, leaves, branches and
    scans, a set a folder with its files named albedo, opacity, normal, gloss or specular,
    subsurface, ao and height as the source had them and a README saying what each gives and what is
    needed; the folder stays out of the repository, the import reads it by path, and the manifest
    names each entry's folder as its source. The entries ship in the second commit; nothing waits.
    The coder's three points, taken. The surface file follows Bevy's channels, occlusion in R,
    roughness in G, metallic in B left at zero and the diffuse transmission in A, so one file binds
    as the occlusion, the metallic-roughness and the transmission textures with no shader of the
    library's, and Bevy reads BC5 as a two-channel normal by itself; the collection lives in
    BevyCSharp/Vegetation/Textures, since a folder named assets would collide with BevyCSharp/Assets
    on a case-insensitive file system, the KTX2 files and textures.json embedded in the library and
    mounted under bevycsharp/vegetation in the asset root so Bevy's own loader reads them by path,
    waiting entries imported to Textures/waiting under .gitignore and not embedded, TreeTextures.Add
    mounting a folder for a local reading or a game's own; the AGENTS.md row for ASSETS.md is the
    owner's to say, .github being hidden from N 1.5, and the coder asks. At 14:22 the owner told the
    coder to ship the sorted folder as it is and to say nothing more of it in any document, so the
    pipeline commit ships no set and adds nothing to the notices, a set ships unless the manifest
    marks it waiting, the tests run on a 64-square sample kept under the tests' references, and the
    entries' commit imports the sorted folder by its README's preset map. The pipeline at `70f92473`
    on 2026-10-25, the encoder, the KTX2 writer, the mount and the lookup, no set shipped; the
    entries next, the six presets' sets alone. Done at `d151e0cc` on 2026-10-25 with the entries,
    ten sets for the six presets at 3.3 MB, the rest of the sorted folder waiting for an item that
    asks.

75. **Bark has depth by parallax occlusion, its height map taken from the source or derived from the
    normal.** From the owner's question of 2026-10-25 at about 15:00 whether the tree textures could
    take parallax occlusion mapping for depth, and the word yes at {now}. About a third of the
    sorted bark sets carry a height or a displacement map, the hornbeam and silver maple trunks, the
    Italian cypress and both pines among them; for the rest the import derives the height from the
    normal map by integrating its slopes, truer than a guess from the color and needing no other
    tool. An entry gains a fourth file, depth.ktx2, one channel in BC4 at half a byte a texel, the
    height inverted into the depth Bevy's standard material reads, the coder checking which way Bevy
    takes it; the tree material sets the parallax depth scale and the occlusion method with a layer
    count of 8 to 16 on bark alone, the leaf cards taking none. The cost is read in the feature
    test's forest at 1080p and written into the record and the guide, and a picture pair shows a
    trunk flat and with depth at reading distance. After item 70's dressing, whose bark material
    carries the depth, placed so at 15:21. One commit. Done at `8aa5bc94` on 2026-10-25, the depth
    from the source's height or integrated from the normal, Bevy's occlusion at 12 layers on bark
    alone, about 0.2 ms on 24 trunks at 1080p, the departure in the head.

76. **A crown shades as a volume, each leaf's depth and outward direction taken from the shadow
    grid.** From the owner's question of 2026-10-25 at about 19:45 on the leaves' normals, and the
    word yes at 19:55. A leaf card's own normal is right for its specular and its transmission, but
    a crown shades mostly as a volume, leaves deep inside and on the far side getting less sun and
    sky than those on the lit rim, so per-leaf normals alone read as a heap of flat cards; bending
    the normals outward is the stand-in artists use, toward a sphere by the shortcut and toward the
    crown's real shape by the physical form. The growth's shadow grid already holds the leaf density
    of each variant, so at each leaf the dressing reads two numbers from it and writes them per
    vertex. The depth is the density above and around the leaf by Beer and Lambert's law, which
    scales the ambient and the indirect light the leaf takes and gives the transmission its
    thickness; the outward direction is the grid's gradient, toward which the leaf's normal is bent
    about half way for the diffuse term alone, the leaf's own normal kept for specular and
    transmission. No proxy mesh and no distance field is built, the grid being finer than a hull and
    already computed. The leaf material carries the two in its vertex stream, through the standard
    material where it can and a Slang material of the library's where it cannot; direct shadows
    between leaves stay the shadow map's and the indirect light under a canopy the field's share of
    the fourth phase. Tests of a leaf deep in a crown darker than one on the rim under the same sky
    and of the rim's leaves facing out, and a picture pair of a crown flat and shaded in grazing
    sun. After item 70's dither levels and before its branch cards and impostors, which draw
    pictures of the leaves and would miss the shading if it came after, placed so at 20:20. One
    commit. The coder's reading at 21:04 and the choice. The bend baked into the vertex normals
    reads as the sheen feared, an aspen seen from below under a high sun with Bevy's turned card
    normals reading a mean of 102, 119 and 94, the depth's gray alone 94, 111 and 91, about 8%
    darker with nothing else changed, the bent normal alone 116, 133 and 114 and both 109, 125 and
    111, the blue up by twenty with white patches on the sun's side, the leaves mirroring the sky
    since a normal bent out of the crown points up at it from under it where the card's own turned
    normal points at the ground, the crowns in the feature test turning gray from below and from
    inside the forest; side-lit in grazing sun the bend does what it should, the aspen's sun side
    20.0 brighter than its far side against 11.7 flat. Getting there needed
    MaterialSettings.SameNormalBothSides, since Bevy's double-sided turns a back face's normal to
    the eye and a baked bend lit half the cards inward, and a correction of DoubleSided's doc, which
    claimed the opposite of Bevy's turn. Chosen, the depth commits alone, the vertex gray with
    Bevy's turned normals and no bend, the doc correction kept and SameNormalBothSides dropped; then
    the bend for the diffuse term alone as its own commit through a leaf Slang material lighting
    twice, its cost measured against the leaves' lighting, with SameNormalBothSides if it needs it.
    The owner, shown the grazing-sun pair at about 21:34, the aspen and the oak flat on the left and
    with the normal bent half way out of the crown on the right, wrote that it looks great, so the
    right-hand look is the target the bend's commit gives without the sheen from below, its side-lit
    picture held against this one. The first part at `6b2503ff` on 2026-10-25, the depth as a vertex
    gray from the sky each leaf sees, the deepest at half; the bend next as its own commit through
    the leaf material, the owner's liked pair its target. Done at `14c2df0f` on 2026-10-25 with the
    second part, the leaf material lighting the diffuse term on the bent normal and the rest on the
    leaf's own, the sun's side showing and no sky mirrored from below, 2.3 ms on 24 trunks' crowns
    at 1080p.

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
    (d3). Done at `bc5559d2` and `8a85790a` on 2026-10-26, the process keeping a variant's skeleton
    and capsules alone, four variants shaped at a time and the scene copy left out, a thousand trees
    at 21 MB of managed memory against 1,317 and the process at 2.19 GB against 5.6, the guard's
    line read by test.py from a file; three leads in the head.

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
    head; item 77 next, the trees' memory, then d3. The meshlet clusters (d3) read on 2026-10-26 and
    left waiting on Bevy, whose 0.20 meshlet pass prepares opaque materials alone so masked leaf
    cards draw nothing, takes a mesh of position, normal and one texture coordinate alone so the
    crown shade, the way out and the sway streams are lost, has no vertex stage to sway, and whose
    meshlet mesh the scene field cannot read as a mesh, the conversion itself no obstacle at 1.6 s
    for a tupelo's 60,000 leaf triangles on a worker; it would take Bevy's pass accepting masked
    materials, extra attributes and a vertex displacement, or a large patched copy of its passes in
    the bridge, so the clusters wait for Bevy, read under Replies. Done with the wind at `fc02fec5`
    and the leaves in the field at `929a1cf7` on 2026-10-26, the trees grown, dressed, planted,
    leveled, swayed and shading their floor; item 77 took their memory.

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
    test's hub from a file, the other rooms under item 68. The format is Valve 220 map text as
    TrenchBroom reads and writes it, Decision 67 of 2026-10-26. The loader's fixture from the tool
    itself waits on a small level saved in TrenchBroom, asked in ASSETS.md on 2026-10-26; the phase
    ships with its own round-trip tests and the fixture's test is added when the file comes. The
    first phase at `f703b3b1` on 2026-10-26, the format and the loader, the room pictured. The
    second phase reads each material's own texel size for the mapping, the picture's pixels a meter,
    with the units a meter as the fallback for a material without a picture, since the first phase
    maps one repeat a meter at scale 1 and 512 pixel prototype pictures with a game config's face
    scale of a sixteenth would otherwise repeat wrong. The second phase in two commits as the coder
    proposed on 2026-10-26: the grid's steps follow the editor's snap at 32 units a meter, the meter
    edge line kept with a half-meter line and quarter-meter fine lines in place of the tenths, a
    small 1 m measure mark in each cell's corner from a 5 by 7 bitmap font in code as the dev
    textures print their sizes, and Unreal's palette by the names a level uses, the floor a dark
    gray, the wall a light gray, the ceiling lighter, stairs, ramps and moving parts the prototyping
    orange, door, window and hazard keeping green, blue and red, proto_dark, proto_light and
    proto_orange naming the tones, the palette's change going to 3DEngine by a Shared line since
    SHARED.md keeps the look alike; then level.textures writing each prototype as a 512 pixel
    picture under a folder TrenchBroom reads, a TrenchBroom game config in the repository with the
    Generic layout, the Valve format, a face scale of a sixteenth and an fgd of the engine's
    classes, the engine reading each material's own texel size in place of the units a meter;
    rounded doorways are the third phase's brushes, not a material. The second phase at `ce142f8a`
    and `1092b434` on 2026-10-26, Unreal's tones and the editor's grid steps, the pictures and the
    TrenchBroom config shipped under the levels folder, the palette to 3DEngine as its item 130. The
    third phase in two commits as the coder proposed, public helpers on Level making brushes as
    Hammer's block and arch tools do, a box, a wedge, an arch of convex segments and a doorway with
    a rounded opening, each face with world-aligned axes so a file built in code is what TrenchBroom
    draws, item 67's block tool reusing them; then the hub's file under the feature test's assets, a
    raised plaza floor, a ring of low walls with five rounded doorways in orange framing toward the
    zones' roads, steps down in orange and an info_player_start at the hub's start, written once by
    a generator and checked in as text, Scene.cs loading it in place of the turning cube with the
    ground and the signposts kept outside the ring and the player's start read from the file, a test
    reading the file back with its five doorways and the start, the drive's and capture jobs' new
    picture, the FeatureTest README and docs/levels.md pointing at the file. The helpers at
    `d53a3108` on 2026-10-26, boxes, wedges, arches and doorways as brushes; the hub's file next,
    and the level then grows into item 68's rooms by the owner's word of that day, Decision 68. Done
    at `c5cb4888` on 2026-10-26 with the third phase's hub, the plaza from a level file in place of
    the open ground's cube alone, the level then growing under item 68 by Decision 68.

79. **The feature test's window shows what the devlog's pictures show.** The owner wrote on
    2026-10-30 that the feature test run by hand does not look like the devlog's pictures, the
    clouds and the lighting looking far worse than the latest pictures of the clouds, which come
    from the drive's offscreen runs. A reading first, in the owner's own way of running with the
    same build, window size and mode. The weather preset, hour and quality of the pictured frames
    are read against what a run by hand starts with and what keys change them; a window screenshot
    at the pictures' views is read against offscreen captures of the same frames, pixel by pixel,
    the swapchain's format and tone mapping against the capture's; and the clouds and the global
    illumination under movement and in the first seconds are read against a still that settled,
    since temporal passes converge over frames. Then the mend for what the reading shows, the
    feature test starting as pictured or its keys said on screen, a window path matching the
    capture's, or the README saying what settles and how long. First, before item 68's next zone,
    with a note and the pair of pictures. The owner added that the feature test is run with dotnet
    run, the bin folder deleted at times, so the C# build is current, the clouds the most visible
    difference, and that the sessions may open a window on this machine and capture it; the reading
    runs windowed the owner's way and asks first which native bridge dotnet run loads, since the
    clouds' shaders live in the bridge and a bridge not rebuilt since a pull is older than the C#
    that calls it, the freshness check read for why it did not say so. Read first on 2026-10-30, the
    bridge is not stale for the clouds on this machine, a plain dotnet run copying the library built
    on 2026-10-26 at 04:45, which holds the cloud code of 2025-10-25 and lacks only scene_field.rs's
    light-through of `929a1cf7`; the owner's saved settings start at hour 11 with the day stopped
    and PartlyCloudy where the pictures stand at 15:00 and 8:00, the window ugly standing still, so
    size and settling are out; the windowed Debug run against offscreen at one pose and hour, pixel
    by pixel, is next. Read at the owner's settings on 2026-10-30, the window equal to offscreen
    within 0.3 to 1.2% on still views, so not the swapchain; the fault is the clouds lit from the
    eye's side, the start view north at 11:00 with the sun behind, coming out gray and no brighter
    than the sky, the phase of two Henyey-Greenstein lobes over four octaves returning about 0.08 of
    the light toward the sun where a white cloud returns about 0.25, the devlog's 15:00 shots facing
    the sun's side where the forward lobe whitens them; a smaller lavender tint from the clear sky's
    ambient of `5a8c6251`; and near clouds as soft smears with the walls hiding the horizon's. The
    mend is the clouds' many-times-scattered light so a front-lit thick cloud reaches a white
    albedo, then the tint, each with a pair at the owner's settings. Done at `e3ddec48` on
    2026-10-30, the many-times-scattered share in the clouds' march, the other glitches the owner
    sees to items 81 and 82.

81. **The meadow's grass holds its light from one frame to the next.** The owner wrote on 2026-10-30
    at 20:4x that the feature test looks good but the grass in the vegetation areas glitches, dark
    patches for one frame that change the next, and asked that two pictures be taken one after the
    other to see it. A reading first, two consecutive offscreen captures at the meadow and their
    difference picture, then the cause among the frame-to-frame terms, the global illumination's
    screen probes and their history, the leaves' share in the scene field with the wind moving the
    grass, the leaf material's twofold lighting under a swaying normal, the shadow cascades' jitter,
    or the smoothing's history; then the mend so a still view of the grass changes by no more than
    the smoothing's own grain between frames, read by the difference picture's share of pixels past
    a threshold before and after, with a pair or a short animation at the owner's settings. First
    after item 79's cloud mend. The cause read on 2026-10-30, the grass shader's main vertex stage
    adding the wind's push in world space while the prepass took it into mesh space and back through
    the transform, the two depths differing by a rounding so that where the main pass landed behind
    the prepass depth nothing drew, a dark blade-shaped hole moving as the wind bent the blade, the
    frames identical with time frozen; the mend places the main pass's point by the prepass's own
    sums, pixels changing over a quarter between consecutive frames from 4.96% to 0.20% and blades
    darker than 0.6 of their surroundings from 0.58% to none, docs/shaders.md saying the same way
    means the same sums in the same order. Done at `de14e311` on 2026-10-30, the two vertex stages
    summing the wind the same way.

82. **The light hall keeps its brightness as the camera turns.** The owner wrote on 2026-10-30 at
    21:0x that in the lighting room the room gets brighter when the camera looks left and darker
    when it looks right. A reading first, offscreen in the hall at the owner's settings, the same
    point turned left and right with the mean of the frame and of a fixed wall patch, then the cause
    among the view-dependent terms, the camera's exposure adapting to what is in view, the global
    illumination's screen probes or the cascades' merge changing with the view, the clustered lights
    cut by the view's bounds, or bloom; then the mend so a wall's patch reads the same within a few
    percent from either side, with a pair at the owner's settings. After item 81. Read at 08:02 on
    2026-10-31, the cause Bevy 0.20's. The floor two metres ahead, swept every 15 degrees, reads
    0.24 to 0.27 and 0.14 to 0.17 from 165 to 315 degrees, the same at 15:00 and gone at night;
    without the bay's irradiance volume no jump, with the probe alone none, with the volume alone
    the jump, the dark arc where the volume is in the view's frustum. pbr_functions.wesl at 702 to
    711 under IRRADIANCE_VOLUME, set a view after culling, adds the volume's light and marks diffuse
    indirect found for every point in view, inside a volume or not, so the environment map's
    diffuse, the sky's ambient, is lost for the whole view; the lines read by the reviewer. The mend
    through render/mended.rs as one shader, taken or refused whole, marking diffuse indirect found
    only where a volume holds the point, with a mend test, the shader-mends bit, the three profiles
    and the sweep before and after; a point inside the volume reads as before. Taken. Done at
    `785a1136`, read at 09:12 on 2026-10-31. The mend goes through render/mended.rs as one shader,
    bits 4 and 5 and Render.IrradianceFallbackMended, diffuse indirect marked found only where a
    volume holds the point, the comment naming pbr_functions.wesl 702 to 711 so the mend is dropped
    when a Bevy release has it; the floor two metres ahead reads 0.24 to 0.27 at every 15 degrees of
    the turn where it read 0.14 to 0.17 from 165 to 315; LightProbeTests holds the outside floor
    within 3%, the floor inside the volume dark and the mend made; the sheet
    2026-10-31-item-82-hall.webp with four labeled frames, box A alone, the plot of the floor
    against the turn and a white ground; docs/sky-and-reflections.md says so. Headless here, render
    and editor in the container, the bridge's tests passing.

## Verdicts settled

Verdicts 3 and 14 to 32 are settled: 15, the native job's four targets, on the pack run of
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
`--render` bridge; and 29, the lost-suite test's Windows figure, on the push of `3d147d49` on
2026-10-18, the third green Windows suite in a row after `b50890cb` held the hang case to a
terabyte, said the reading beside the limit and took a process as a child only where it was not
created before its parent; and 30, the visibility test on a headless build, on the same push, the
three suites passing with the test asking the three ids on every build and the remarks saying every
build has the component; and 31, the capture of `async_compute` on the machine's clock, on the pack
run of `6bac018b` on 2026-10-18, the four shards passing with the example listed beside its sibling
at a room measured on the runner and in a container of its image; and 32, the system information
test's process measures on macOS, held at zero where Bevy's App Store build of sysinfo reads no
process, on the macOS suites of the push run and the pack run of `35ca43b1` and the push run of
`3a2e403a` on 2026-10-19, three green in a row.

33. **The command line server's held-clock test fails on the Linux runner with a frame it never
    read.** On the push run of `97ea3055`, 2026-10-19, the Linux suite failed
    `CliServerTests.AClockHeldBetweenCommandsMovesOnlyWhileOneWaits` at its line 297 with a key of
    15 not present in its dictionary, 1,246 passed and 556 skipped in 4 minutes 30 seconds, the
    macOS and Windows suites of the same commit green; the test came with part (f) at `714451b0`,
    whose push run passed it on the three systems, so the fault shows on one runner of two. The test
    reads a frame by its number from what the server answered and the frame it asks for was not
    among them, which a wait that ended on another frame, a reading taken before the server's answer
    or the clock's frames counted apart from the frame count would each give. The mend is read
    before it is chosen: the test's dictionary and what fills it, which frame 15 is and why it was
    absent, with the run's output if the artifact holds it; then the test made to hold what the hold
    promises without reading a frame that may not come, or the server mended where it answers a
    frame it did not run. Mended at `8e9c20b7` the same day, the test reading its times after the
    app has stopped, the server unchanged, as the head says. Settled on the Linux suites of the push
    run of `e7ea1a06` and the push run and the pack run of `3eaa0a44`, 2026-10-19 and 2026-10-20,
    three green in a row after the mend.

34. **The release notes tests cannot delete their scratch repositories on Windows, since git writes
    its objects read-only.** On the pack run of `987676b5` and the push run of `e7ea1a06`,
    2026-10-19, the Windows suite failed both release notes tests in their disposal, an access
    denied on an object folder of the scratch repository with 17 files left in it and no process
    holding one (`TestFolder.cs:73`, `ReleaseNotesTests.cs:23`), 1,067 passed and 560 skipped, the
    Linux and macOS suites passing them; the tests came with item 32 at `d482caa3`, run on Linux
    alone before the push. Git writes its loose objects read-only, and the folder's deletion on
    Windows refuses a read-only file where Linux and macOS do not, so a test folder that holds a
    repository cannot be swept there. The mend is one thing: the test folder's deletion clears the
    read-only attribute of what it sweeps before deleting, with the reason in its remark, or the
    release notes tests sweep their repositories through git, whichever the other tests that make
    repositories, if any, already do. Mended at `7a0f07ea` the same day, the test folder's sweep
    clearing the read-only attribute on a refusal. Settled at three green Windows suites in a row
    after the mend. The push run and the pack run of `3eaa0a44` and the push run of `9b6dceaa`
    passed the Windows suite on 2026-10-20, the third at 03:55, three green in a row. Settled on the
    runs of `3eaa0a44` and `9b6dceaa` on 2026-10-20.

35. **The load hold test's image never loaded on the macOS runner.** On the pack run of `987676b5`,
    2026-10-19, the macOS suite failed `ClockHoldTests.AClockHeldForLoadsStandsStillUntilTheyEnd` at
    its line 42, the image never loaded, 1,061 passed and 567 skipped in 5 minutes 44 seconds, the
    Linux and Windows suites passing it; the test came with part (f) at `714451b0`, whose push run
    passed it on the three systems, so it shows on one runner of two as Verdict 33's did. The mend
    is read before it is chosen: how long the test waits for the load and in what, frames or the
    machine's time, and whether a held clock can hold what the load itself waits on, since a load
    that waits on a frame's step while the step is held would never end; then the test given the
    patience a slow runner needs in the machine's time, or the hold mended where it holds the load,
    whichever the reading supports, with the reason in the remark. Read and mended at `17487d27` the
    same day: the held clock holds nothing the load waits on, the test had given the load a few
    milliseconds unpaced, and it paces its frames at sixty a second up to six hundred. Settled at
    three green macOS suites in a row after the mend. The push run and the pack run of `3eaa0a44`
    and the push run of `9b6dceaa` passed the macOS suite on 2026-10-20, the third at 03:55, three
    green in a row. Settled on the runs of `3eaa0a44` and `9b6dceaa` on 2026-10-20.

36. **The pack job waits 20 minutes for nuget.org to list the package it pushed, and the feed took
    longer.** On the pack run of `3eaa0a44`, 2026-10-20, the pack job logged in and pushed `0.5.149`
    at 02:00, and its last step, which builds each game on its own default from nuget.org, waited 20
    minutes for the feed to list the package and failed at 02:20 saying so, while the feed listed
    the package by 02:47, the templates package of the same push listed within the twenty; so the
    run ends red with the package published and the games unbuilt on it, and the run's page says a
    step failed where the feed was slow. The mend is one thing, under N 3.5: the wait set from what
    the feed takes, read from this run and the next, with the job's own limit raised to hold it, or
    the games' build on their defaults moved out of the pack job into a step that runs on the next
    build run, where the package is listed by then, said in the workflow's comment; and the step
    says, when it gives up, that the package was pushed and stands, so a red run is not read as an
    unpublished package. An aside kept here: the pack step warns that a stable package should not
    take a prerelease dependency, the physics library's, which the owner decides on. Settled when
    the pack job passes whole on the next pack run with publish on. The coder's reading on
    2026-10-20 is the first form, the wait set from what the feed took, the templates listed within
    20 minutes and the engine by 47, so a wait of 45 minutes with the job's limit raised to hold it,
    and the summary moved before the games' step, so a run says the push stood even where the wait
    gives up; the reviewer takes it. Mended at `d0e58c56` on 2026-10-20 with a wait of ninety
    minutes, near twice the 47 measured, the coder's figure where the reviewer's 45 would have given
    up two minutes before the one listing read, and the job's limit at 120; the pack run with
    publish on started on it is the second reading of the feed's time, and its pack job passing
    whole settles the verdict. A reading of the feed's time from the other engine: its `5.2.155`,
    pushed at 08:38 on 2026-10-20, was listed at 08:48, ten minutes. On the pack run of `3b0e54d7`
    on 2026-10-20 the package was pushed at 11:48 and the feed listed `0.5.174` and the templates
    within 24 minutes, inside the ninety, the summary standing before the games' step on the page;
    the games' step then failed on a game's own default, Verdict 40, not on the wait. Settled on the
    pack run of `3b0e54d7` on 2026-10-20.

37. **The Fedora capture job compares its six pictures on the runner, which has no decoder for
    them.** On the pack run of `d0e58c56`, 2026-10-20 at 05:07, the job's compare step failed at its
    first picture, `clustered_decals.webp`, which the compare script said could not be decoded; the
    script decodes through ImageMagick or libwebp's tool, and the shards' job installs both on the
    runner where this job installs them in the container alone, so on the runner the script found
    neither and read nothing. The six pictures the container wrote are whole, lossy WebP of the same
    encoding as the checked-in ones, 1280 by 720 and 1920 by 1080, read from the uploaded artifact.
    The mend is one thing, under N 3.5: the job installs libwebp's tools and ImageMagick on the
    runner before the compare, as the shards' job does, with the reason in its comment, or compares
    inside the container where they are; and the compare script, where it finds no decoder, says so
    by name in place of saying the picture could not be decoded, so the next such page names the
    fault. Settled when the job passes on the next pack run. Mended at `6fcecb1a` on 2026-10-20, as
    the head says. The job passed on the pack run of `3b0e54d7` on 2026-10-20 at 11:17, its six
    compared on the runner. Settled on the pack run of `3b0e54d7` on 2026-10-20.

38. **`many_foxes` differed in 6.7 percent of its pixels on a capture shard, the foxes a step along
    in their run.** On the pack run of `d0e58c56`, 2026-10-20 at 05:00, the first shard's compare
    failed on `many_foxes` alone, 6.7 percent of its pixels past the 2 allowed within x 24 to 1,894
    and y 132 to 899, the whole field of foxes; the uploaded capture against the checked-in picture
    shows the same scene, the same colors and the same shadows, each fox a little further along its
    gallop, so the frame's animation time was not the picture's, where the shards passed the example
    on every run before and the three other shards passed theirs on this one. The capture's frame is
    counted on the set clock with the clock held while a load runs (`714451b0`), so the reading
    comes first: whether the foxes' animation players start on the frame their asset arrives and
    count from a time the hold does not cover, or whether the run had a load the hold did not catch;
    then the mend where it points, the players' start tied to the held clock or the example's
    animation time set at the capture's frame, with the reason in the remark, one commit. Read by
    05:48 on 2026-10-20 on the development machine with logging in a scratch copy of the example,
    two faults in the bridge's animator and neither in the example. The first play on a scene loads
    the file again to read the clips' names, since Bevy freed the file's root asset once the scene
    had loaded, and that load is not among the handles the load hold watches, so the frames it takes
    pass on the clock; on the GPU all 1,000 plays failed in the first frame and succeeded in the
    second, and on a device whose load lands a frame sooner or later every fox starts a frame sooner
    or later. And a play inserts the animator's source and then its animator on the scene root,
    every call after taking the animator off and putting it back, table components all, so each call
    moves the root to another table and the old table's last row into its place while the behavior
    that called it holds its own row by reference, and its started flag lands in another fox's row:
    in the second frame 1,000 plays returned true and 501 foxes still read not started in the third
    and played again, 499 twice and one three times, each replay resetting the clip a frame later,
    the half following the table's order and so moving with the frame the file arrives in. The mend,
    one commit: the two animator components sparse, so the root changes archetype but keeps its row
    and the behavior's write lands on its own; the clock hold keeping the ids of loads the bridge
    starts for the app while the hold is on, holding the clock while any of them loads and dropping
    each once it stops; two engine tests failing before the mend, several scene roots whose behavior
    plays a clip each playing once and keeping what they wrote, and no time passing under a held
    clock between a scene's first play and the one that succeeds; and the pictures of the examples
    that play clips taken again, `many_foxes`, `animated_mesh`, `animated_mesh_control`,
    `morph_targets`, `many_morph_targets`, `mirror` and `scene_viewer`, since their start frames can
    move. The row's move for Bevy's own animation types is item 34. Mended at `05ed21e4` on
    2026-10-20 as the head says, the first shard of the pack run started on it the first of three.
    Settled at three green first shards in a row after the mend. The first shard of the pack run of
    `05ed21e4` passed at 07:40 on 2026-10-20 with the example matching, the first of three. The
    first shard of the pack run of `3b0e54d7` passed at 11:19 on 2026-10-20, the second of three.
    Settled on the pack run of `cf70c650` on 2026-10-21, its four capture shards green, the third in
    a row.

39. **`meshlet`'s colored bunnies differ in their patches between the Fedora job's capture and the
    picture.** On the pack run of `05ed21e4`, 2026-10-20 at 07:39, the Fedora job's compare, running
    for the first time with its decoders in place, found `meshlet` differing in 10.9 percent of its
    pixels past the 2 allowed, within x 126 to 616 and y 192 to 607, which is the row of bunnies
    drawn with a color a meshlet, while the plain bunnies on the right, the floor and the shadows
    match; the uploaded capture against the picture shows the same bunnies in the same places with
    their patches cut otherwise, larger and elsewhere, so the meshlets the bridge built from the
    mesh, or the level of detail it chose, differ between the bridge the runner built and the one
    the picture was taken with on the development machine, where the coder's own container run
    matched the picture to no pixel. The reading comes first: the meshlet count and the level drawn
    on the runner against the machine, whether the builder's partition is deterministic across
    builds of the bridge and its dependencies, and whether the runner's four cores or its Mesa pick
    another level; then the mend where it points, a deterministic partition if the builder allows
    one, the example's debug colors keyed to something stable, or, if the partition is not the
    bridge's to fix, the colored bunnies' region left out of the compare with the reason in the
    differ file, which would be its one line. One commit. Settled when the Fedora job passes on the
    next pack run. Read so far by 08:27 on 2026-10-20: the partition is not the bridge's, since the
    example loads Bevy's prebuilt meshlet bunny, pinned by the assets file to one commit, so both
    machines read the same clusters, and the colors are seeded from the instance and meshlet ids,
    both stable; the runner's picture shows the near cluster-colored bunny a whole level coarser,
    larger clusters, other colors, no holes. Two things can choose the level, the
    imperceptible-error formula with its one-pixel threshold, and the cull pass's append into the
    raster clusters' list, which has no bound check, software clusters filling from the left and
    hardware ones from the right, so with the 16,384 budget overrun they overwrite each other in an
    order the atomics set, which the thread count changes; the coder captures the example in Fedora
    44 with the runner's own bridge, once pinned to four cpus and once on all 32, to tell them
    apart. Read further by 08:56: it is neither the thread count nor the runner's build. In Fedora
    44 on Mesa 26.2.4 compared with no allowance, the runner's own bridge from the pack run on all
    32 cores gives the runner's picture to no pixel, as does the coder's bridge at `05ed21e4`; the
    bridge of `5195e95a`, before the anisotropy mend, gives the checked-in picture to no pixel; and
    the head's bridge with the mend gated off by a scratch variable gives the checked-in picture and
    with it on the runner's, each to no pixel. So the engine draws the example the same on both
    machines and both builds, and the picture went stale at `0f8e89b4`, which retook its anisotropy
    pictures on the GPU and never ran the six in Fedora, where the compare could not see it until
    Verdict 37's decoders. What moved is the color of nearly every cluster with the outlines
    coinciding to 89 to 92 percent within a pixel, the same clusters under another seed of the
    instance and meshlet ids, not a coarser level as it first looked. The coder splits the mend's
    two effects next, its resource and system present with the shader not rewritten against the full
    rewrite, to say whether the seed moved with the extra resource entity Bevy spawns or with the
    shader's recompiled pipelines. The reviewer's direction: where the seed moved with an entity's
    id, the example keys its colors to the meshlet's id and the instance's place in the example's
    own list, so the picture holds when an entity is spawned before it, with the reason in the
    example's comment, and the picture is taken again in Fedora once; where it moved with the
    pipelines alone, the picture is taken again with the reason in the commit; `test_meshlet` and
    `many_meshlet_materials` checked against theirs the same way, which matched on the runner. One
    commit. Mended at `3b0e54d7` on 2026-10-20, as the head says, the seed the example's own; the
    pack run started on it is the check. The Fedora job passed on the pack run of `3b0e54d7` on
    2026-10-20 at 11:17 with the seeded picture. Settled on the pack run of `3b0e54d7` on
    2026-10-20.

40. **Swarm's project names `0.5.0` as its default engine version, which the feed never had, so the
    games built on their defaults fail on it after every publish.** On the pack run of `3b0e54d7`,
    2026-10-20 at 12:12, the pack job's last step found `0.5.174` listed and built each game on the
    default its project names, and Swarm's, `0.5.0` since `5e750133`, does not build, nuget.org
    holding no such version, where the other three games name `0.4.129`; the step's own message says
    to raise each to the version the run pushed. So the four games' defaults go to `0.5.174` in one
    commit, with the step's script run on the machine against the feed as the check, as its message
    asks, and item 22's check of Swarm read again against the feed and not a local pack, so a
    default names a published version from here; the step stays as it is, since it found the fault.
    Settled when the step passes whole on the next pack run with publish on. Mended at `2949041f` on
    2026-10-20, as the head says, the script's check passing on the machine against the feed.
    Settled on the pack run of `cf70c650` on 2026-10-21, the step passing whole with `0.5.178`
    listed after 45 minutes and the four games built on `0.5.174`.

41. **The Linux suite of the push of `27b3f8eb` is lost to the animate script's test, which hung for
    five minutes and took the test host down.** On the build run 39516364351 on 2026-10-21, the
    Windows and macOS suites passed and the Linux suite's test step ended with the suite lost: 295
    passed, 0 failed, 5 skipped, then
    `AnimateScriptTests.ThreeCapturesMakeAnAnimationOfThreeFramesWithTheDelayGiven` hung after 5
    minutes 22 seconds and the run was aborted with the test host process crashed, the suite's own
    output file not carried to an artifact, so the job's log holds no more. The test runs
    `build/animate.py` over three captures, which takes Pillow where it is installed and otherwise
    `./bcs animate`, which builds and runs the client; a test that starts a build or an interpreter
    with no bound of its own waits for whatever that process waits on, here for five minutes on the
    Linux runner where the two other systems returned. Read first what the process waited on,
    running the script on a Linux machine as the runner has it, with and without Pillow, under the
    test's conditions; then the mend: the test holds its process to a bound of seconds and fails
    with the process's output where it is passed, the script's engine path runs the client built by
    the suite's own build and never builds inside a test, and the Pillow path is tested only where
    Pillow is found. One commit, before item 38's second half's commit, since the push is red on
    Linux. Mended at `32c8467e` on 2026-10-21, the cause an MSBuild node of the client's build
    inside the test holding the script's pipe, as the head says. Settles on three green Linux suites
    after it. The first, the build run of `32c8467e` on 2026-10-21, green in 14 minutes. The second,
    the build run of `a1babb72`, green in 13 minutes. Settled on the build run of `bfe1fc63` on
    2026-10-21, the third green Linux suite in a row after the mend, in 12 minutes.

42. **The macOS suite of the push of `5f051299` fails the app leak test, the heap's floor rising 6
    MB from the twentieth app to the hundredth.** On the build run 39547498414 on 2026-10-21, the
    Linux and Windows suites passed and the macOS suite, 1,166 passed in 4 minutes 46 seconds,
    failed the test that an app made and closed a hundred times leaves nothing behind: the heap
    after every ten apps read 91.89, 101.10, 103.34, 90.84, 92.82, 94.81, 96.85, 98.88, 100.96 and
    102.94 MB, a collection at the fortieth and two MB a ten apps after it, and what grew from the
    twentieth to the hundredth was 8,548 strings of about 199 KB, 684 runtime parameter infos of
    about 59 KB, 255 runtime method infos of about 26 KB, twelve int arrays and an event source's
    metadata table; the same test passed on the macOS suites of `fc63b7ba` and `3fb5643e` and on the
    other two systems of this run. The parameter and method infos say reflection over methods is
    kept across apps, about nine parameters and three methods an app, which a registration by
    reflection each app would leave in a static, and the strings with them; or the floor's rule
    reads a sawtooth as a rise where the collections fell differently on the runner. Read first
    which static holds the infos across apps, from a count of them after twenty and after a hundred
    apps on the machine, and whether `5f051299` or the two before it could have added it; then the
    mend, the static emptied or the cache keyed once rather than an app, or, where the infos stand
    still and the floor's rule is the fault, the base taken as the greatest of the readings after
    the collections as 3DEngine's Verdict 75 took it, under N 3.5. One commit, before the next push.
    Mended at `722cb062`, the floor's rule, the infos read as the census's own; settled on the build
    run 39554837971 of `45526e4b`, read at 08:42 on 2026-10-21, the macOS suite green with 1,167
    passed in 5 minutes and the two other systems green.

44. **The Linux suite of the push of `fda2f62e` fails the examples table's check, EXAMPLES.md out of
    date.** On the build run 40024690045 on 2026-10-23 the Linux job stopped at
    `build/examples-table.py --check` after six minutes, saying `.github/EXAMPLES.md` is out of date
    and to run the script, while Windows and macOS passed their suites; the cause, read by the
    coder, is the triage, BevyCSharp.Examples/triage.tsv, which item 56's rename passed over as a
    .tsv file, so it kept bcs::lit, bcs::deferred and three more and the script wrote the table back
    to the old names. Mended at `a2c8348e` on 2026-10-23, the triage and one comment of the bridge's
    manifest renamed, EXAMPLES.md standing as committed, pushed at 23:31; settles on the Linux suite
    of that push. Settled on the Linux suite of the build run 40027448850 of `a2c8348e`, green on
    the three systems, read at 23:50 on 2026-10-23.

45. **The run of `ddf51f5a` is red on the runners' dotnet install, not on the library.** Read at
    20:25 on 2026-10-25 from the run's jobs, run 40225592505. The three suites failed in a minute at
    setup-dotnet, asked for 10.0.x and resolving 10.0.403, whose SDK archive the install script
    could not download, the Windows runner reading a 409 from the blob while the release feed's
    index named 10.0.402 as the latest, so Microsoft was publishing a release as the runners asked
    for it; the same minute took 3DEngine's two runs of `23b4793b`, its Verdict 92. The commit
    changes nothing of the SDK or the workflow. Nothing to mend in the tree; the failed jobs run
    again after an hour and the verdict settles when they go green. At 20:38 the feed served the
    10.0.403 archives for the three systems, so the failed jobs were run again then, ahead of the
    hour. That attempt failed the same way at 20:39, the install script still not finding the SDK
    though the archives answer; the next attempt waits for the hour, at about 21:25. Settled at
    21:46 on 2026-10-25 by the third attempt, green on the three systems once Microsoft's feed
    served the release, the second attempt at 20:38 having failed the same way.

46. **The run of `f703b3b1` is red on the three suites, the level tests building the prototype
    picture where the push's bridge has no renderer.** Read at 11:24 on 2026-10-26. On each system
    two of LevelTests' cases fail, the headless build with its collision and entities by class and
    the console command's build, both through Levels.Spawn drawing a slot as the prototype of its
    name, Modeling's GridOf calling Images.Grid and Render.CreateImage for a 1024 by 1024 picture
    with its mipmaps, which the bridge answers with its renderer-missing error, since a push's run
    tests the library on a bridge built without the renderer while the coder's own bridge has it;
    the console case then reads the failure's text where it expected the built line, and an
    IndexOutOfRangeException rides inside the AggregateException beside the bridge's error, a second
    thing to read. Linux 1,556 passed and 2 failed in under five minutes, Windows 1,353 and 2, macOS
    alike. The mend is Levels.Spawn, or the prototype's lookup under it, asking App.HasRenderer
    before making a picture and giving a slot its material without one where the bridge has no
    renderer, so a headless level builds its collision and entities anywhere, as the trees' pictures
    already wait on the renderer; the two tests then pass on the push's bridge, and the out-of-range
    index is read and mended or named. Small, before item 65's second phase, run on a bridge built
    without the renderer before committing. Mended at `25e9f601` on 2026-10-26, the parts drawn
    where the renderer is and the bodies given in code, the index the test's own on an empty array
    after the spawn's throw; settled when its run goes green. Settled on 2026-10-26 by the run of
    `25e9f601`, green on the three suites at 11:58.

47. **The run of `1092b434` is red on the three suites, the shipped prototype pictures compared byte
    for byte with what the engine draws on another machine.** Read at 12:49 on 2026-10-26. On Linux,
    Windows and macOS alike one test fails, LevelTests' case that holds the shipped pictures to the
    engine's output, at floor.png, the first it reads, with everything else green, 1,559 passed on
    Linux and 1,356 on Windows; the pictures were written on the coder's machine and the test
    compares the PNG bytes, so an encoder's compression, metadata or ordering that differs by system
    or library version fails it where the pixels may well agree, and if the pixels differ too the
    CPU drawing or the tint is not the same on every machine, which the test must say. The mend
    decodes both pictures and compares the pixels, exact for a drawing meant to be the same
    everywhere, with the first differing pixel and its two values in the failure's text; if the
    pixels differ on the runners, the drawing is made the same everywhere, integer steps in place of
    what rounds apart, and the shipped pictures written again. Small, before item 65's third phase
    goes on; the run's artifacts hold the runners' pictures if the test writes them on failure,
    which it should. Mended at `c6b95ea9` on 2026-10-26, the pictures compared pixel for pixel and
    the drawn one kept with the run's page; settled when its run goes green. Settled on 2026-10-26
    by the run of `c6b95ea9`, green on the three suites at 13:13, so the runners' failure was the
    PNG bytes alone.

48. **The run of `7e35f94b` is red on macOS, the heap's floor rising across a hundred apps.** Read
    at 18:39 on 2026-10-30. AppLeakTests' hundred apps read the heap's floor 5.9 MB higher at the
    hundredth than at the twentieth on macOS, 108.3 to 119.1 MB in even steps of about 1.5 MB every
    ten apps, the growers strings by 8,791, RuntimeParameterInfo by 684, RuntimeMethodInfo by 255
    and an EventSource metadata entry, Linux and Windows passing the same test; the steady step says
    something each app makes is kept in a static, reflection over methods and their parameters among
    it, new since `7e082be1`, so the module initializer of `7e35f94b`, the level's registry or the
    hall's load are the first places to read. The mend finds what each app leaves behind and lets it
    go or makes it once for the process, the test reading flat on macOS; if the growth is the
    runtime's own on that system, the reading says so with the types and the bound follows. Small,
    first, the commit going with `169d5afe`. Read on 2026-10-30, no app code changed since
    `7e082be1` and a census finds about 0.4 MB alive against GetTotalMemory's 10.7 MB more, so the
    measure is the mend, the test reading the promoted bytes of the last full blocking collection as
    3DEngine's does, 19.82 to 19.83 MB over a hundred apps. Mended at `1226ff50` on 2026-10-30;
    settled when its run goes green. Settled on 2026-10-30 by the run of `1226ff50`, green on the
    three suites at 20:14.

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

## Replies archived

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

Item 70's meshlet leaf clusters, (d3), read before any cut from Bevy 0.20's meshlet module in
bevy_pbr. Its pass cannot draw the leaf material's two lightings, so the clusters wait, as the order
says. Four things stand in the way. The meshlet material pipeline leaves out any material whose
alpha mode is not opaque, so a leaf card cut by its picture's mask draws nothing, and the leaves
would have to be cut as their outlines, some eight triangles a leaf against two.
MeshletMesh::from_mesh takes positions, normals and the first texture coordinates and refuses a mesh
with anything else, so the crown's shade in the vertex colors and the way out of the crown in the
second coordinates, which the diffuse lighting bends the normal toward, would have to be read in the
fragment shader from a texture of the crown's volume by the fragment's place in the tree's space. A
meshlet material has no vertex stage, so neither the leaves nor the bark could sway, and a tree
drawn as clusters near the camera would stand still in the wind its far levels bend in. And the
scene field reads each drawn mesh's triangles from Bevy's meshes, which a meshlet mesh is not, so
the clusters would leave the canopy out of the field. The conversion is no obstacle, a tupelo's
60,000 leaf triangles cut into clusters in 1.6 s and an oak's 44,130 stem triangles in 0.7 s on a
worker, as a scratch test timed Render.CreateMeshletMesh's baked files, and the GPU the pass asks
for, 64-bit atomic textures on Vulkan or Metal and no MSAA, the bridge handles where meshlets are
built. What it would take is Bevy's meshlet pass taking alpha-masked materials, extra vertex
attributes and a vertex displacement, or a patched copy of its raster and material passes in the
bridge, which is large; until one of them, the near levels stay the cards they are.

Item 65's first phase, the format and the loader, in the commit carrying this line, as Decision 67
has it. Level reads and writes Valve 220 map text, entities in braces with their keyvalues kept in
order and duplicates kept, as two outputs of one name are, and brushes of faces each three points of
a plane, a material, two axes with offsets, a rotation and two scales, each point in double
precision as LevelPoint, since a corner found where planes meet moves by as much as the digits a
float drops. The canonical writer lays a file out as TrenchBroom does, its game and format comments
and a comment numbering each entity and brush, so the engine's own file reads and writes back byte
for byte, and a file laid out otherwise, braces sharing lines, tabs, numbers with points and signs,
reads to the same values and writes the canonical form. A face in Quake's older format, with no
axes, is refused at its line with TrenchBroom's menu named for converting it. A brush's corners are
where three of its planes meet within the rest, each found once and shared, and planes leaving the
solid open are refused, every edge having to run back round the face beside it. Levels.Spawn builds
it under a root, the worldspawn's brushes and each brush entity's through an EditMesh, a slot a
material and every face's coordinates its own, drawn as the slot of its material's name and
colliding as their triangles on a static body; a point entity stands at its origin turned by its
angle or angles so its minus Z looks along the file's X turned, and a brush entity stands unturned
at its origin or its brushes' middle, which the first try did not, its collision turned a quarter
off its drawing. Every entity carries LevelKeys, its class, its targetname and its keys as the file
writes them, Levels.Register says what a class makes, and the engine registers tree from a species
and a seed. The worldspawn's _units_per_meter key, 32 by default, is named in docs/levels.md with
TrenchBroom as the tool, and a material's picture spans the units a meter, so it repeats once a
meter at a scale of one, as the prototype grid does; phase two's prototypes settle what TrenchBroom
shows for them. level.load builds a file from the console and ./bcs. The test on a file TrenchBroom
itself wrote waits for the owner's, asked in ASSETS.md. The picture is the room in
2026-10-26-item-65-room.webp. The suite ran whole in one host at the 3 GB cap, 1,988 passed and 3
skipped, the host's anonymous memory 2,107 MB at most. The public listing gains Level, LevelEntity,
LevelBrush, LevelFace, LevelPoint, LevelKeys and Levels, no line removed or reshaped, and AGENTS.md
gains BevyCSharp/Levels's row under N 1.5.

Item 65's second phase, its first commit, the grid and the palette, in the commit carrying this
line. Images.Grid's cell keeps its meter line and steps at the half and the quarters, a level
editor's grid steps, in place of the tenths, and letters 1m in its corner as Source's measuring
textures print their sizes, where the cell has 128 pixels or more. Modeling.PrototypeColors keeps
its seven names in Unreal's level prototyping tones, the floor a dark gray, the wall a light gray,
the ceiling a lighter one and stairs the prototyping orange, with door, window and hazard as they
were. docs/modeling.md's grid prototyping says both. The picture is 2026-10-26-item-65-palette.webp,
the room again and a corner of it up close. The whole suite ran in one host at the 3 GB cap with the
second commit's work beside it, 1,990 passed and 3 skipped, and the level and images tests on a
headless bridge. No public line changed, and the cheat sheet's line for Images.Grid follows its
summary.

Shared: the prototyping look in Unreal's level prototyping palette and a level editor's grid steps,
in the commit carrying this line, item 65's second phase, for the row keeping the look alike. The
tones as sRGB are floor (0.31, 0.31, 0.32), wall (0.71, 0.71, 0.70), ceiling (0.82, 0.82, 0.83) and
stairs (0.96, 0.46, 0.13), with door (0.38, 0.72, 0.38), window (0.36, 0.62, 0.92) and hazard (0.90,
0.22, 0.16) as they were. The grid's cell is a meter on a face of sRGB 0.9, its edge line of 0.35 a
128th of the cell wide and half on each edge, a line of the same 0.35 a 256th wide across its middle
and fine lines of 0.68 a 512th wide at its quarters in place of the tenths, each a pixel at least,
and 1m lettered in the line's color in its top left corner, letters of five by seven squares each a
128th of the cell, two squares in from the edge line, where the cell has 128 pixels or more. The
quarters and the half are TrenchBroom's 8 and 16 unit steps at 32 units a meter.

Item 65's third phase, its first commit, the brushes a level is blocked out with, in the commit
carrying this line. LevelBrush.Box, LevelBrush.Wedge, a ramp rising along X, and LevelBrush.Arch, a
ring or a part of one cut into straight segments as Hammer's arch tool cuts one, are each a convex
outline across Y stretched along it, every face wound as the file winds one and mapped along the
world's axes as a new face is, at LevelBrush.DefaultScale, a sixteenth, so a prototype's cell spans
a meter. Level.Doorway is a wall along X with a rounded opening, a brush each side and the wall
above cut into segments along rays round the opening's half circle and through the wall's top
corners, so each segment's outer side lies along one edge and stays convex. LevelBrush.Turned turns
a brush about the vertical and maps its faces again from the world. LevelShapesTests holds each to
its faces, corners and volume, the doorway's open where its opening is and solid beside and over it,
within the eight chords' cut of the circle, and a turned box to its corners and its new mapping.
docs/levels.md shows a room built in code. The public listing gains the six, no line removed or
reshaped.

Item 65's third phase, its second commit, the feature test's hub from a level file, in the commit
carrying this line. games/FeatureTest/assets/levels/hub.map is an octagonal plaza nine meters from
its middle to its walls' insides, its floor an octagon four units up made of a square's planes and
the same square's turned an eighth, eight walls half a meter thick and five meters high, a rounded
doorway three meters wide and four high with its arch in the prototyping orange in the five toward
the zones, and the player's start six meters south facing north. HubLevelTests holds the file to its
generator by value within a millionth of a unit, since a sine may differ in its last digit between
systems, writes it again with BCS_WRITE_LEVELS=1, and reads a doorway open toward each zone and a
wall in the three ways between. The shapes keep their points to a millionth of a unit and take a
quarter turn's sine and cosine whole, so the file holds no dust of a sine. Levels.Spawn and
Levels.Load take where the level's root stands, since its bodies are put where it stands as they are
made, and the feature test stands the hub on its ground with it, the plaza's floor Scene.PlazaHeight
over it and Zones' hub start on the floor. The turning cube stays and turns in the plaza's middle,
since the crates and the ray-traced occlusion page build on it. The feature test's drive runs whole
on it, its peak 3.44 GB and its 1-hub capture the plaza, and 8a287c2f before this commit points the
drive's zone check at the meadow's quaking-aspen 70, the meadow naming its trees by species and seed
since 1bd18a6c, so the drive had stopped there on every run since. The picture is
2026-10-26-item-65-hub.webp. The whole suite ran with this and the helpers' commit, 1,997 passed and
3 skipped in one host at the 3 GB cap, and the level, shapes and hub tests pass on the headless
bridge. The public listing reshapes Levels.Load and Levels.Spawn, each given a place after its other
parameters, which a call giving the earlier ones keeps.
