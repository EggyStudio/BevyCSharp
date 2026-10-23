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
