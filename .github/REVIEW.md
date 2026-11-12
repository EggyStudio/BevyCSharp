# Review

Reviewed up to `8784bdd1`, InternalsVisibleTo's pairs measured by the compiler and narrowed,
AnimationReader moved to Rendering beside the writers it reads back and FieldCurve and FieldValue to
Animation, 67 pairs and 217 internal names reached where 68 and 221, the pairs listed with a why
each in build/norm/1.6.txt and held by N 1.6, build/norm/internals.cs finding them and writing the
project files' comments, pushed at 22:37 on 2026-11-11, green at 22:54 with Windows and Linux in 12
minutes each and macOS in 17; the whole suite passed, 2,048 passed, none failed and 3 skipped,
NormTests 19; it keeps the form and carries the review's files. Before it `064cdae8`, the tests'
Assets folder moved to the areas, 146 files renamed whole by git to the area each tests and the
asset server's 12 staying, no file's text changed, pushed at 21:16 on 2026-11-11 once the run with
publish had started on the commit before it, its run red on the three systems at the step that keeps
an artifact, their tests passed, Verdict 58, and green at 22:37 on its second attempt, Linux in 14
minutes, Windows in 19 and macOS in 21; the whole suite passed, 2,048 passed, none failed and 3
skipped, NormTests 19; it keeps the form and carries the review's files. Before it `77941f42`, the
split packed as one package by Decision 88, every area's assembly and the three modules' in the one
package's lib folder with no dependency on an area, the bridges' pack items in the flat project, a
size step in the pack job failing a package over 262,139,904 bytes before anything is kept or
pushed, and the script host walking from every loaded assembly of the engine, pushed at 19:47 on
2026-11-11, its run green at 20:05 with Linux and Windows in 15 minutes each and macOS in 18, its
pack run without publish, 43081159142, green at 21:11 with the drive in 59 minutes. Its package read
from the artifact, BevyCSharp.0.6.106.nupkg, 65 entries, 46 in lib for the 20 assemblies of the
engine and the modules' Levels, Sky and Vegetation, each a dll with its xml, six bridges, the two
musl slots empty as before, its only dependencies BepuPhysics 2.5.0-beta.29 and
Twizzle.ImGui-Bundle.NET 1.91.5.2, the templates naming BevyCSharp alone, 250,121,275 bytes with
12,018,629 of room, 4.58 percent, and both ids on nuget.org before it, so no new id goes out; the
run with publish, 43094830090, started at 21:15 on the same head, red by 21:45 in its native jobs by
an outage of GitHub's Actions, Verdict 58, and green at 00:36 on 2026-11-12 on its second attempt,
0.6.106 published as the one package with its templates and tagged v0.6.106; the whole suite passed,
2,048 passed, none failed and 3 skipped, NormTests 19; a local package with the Linux bridge alone
is 52,739,654 bytes in 60 entries, its only dependencies BepuPhysics and Twizzle.ImGui-Bundle.NET;
it keeps the form and carries the review's files. Before it `7428798d`, the split's second commit,
the library's areas as nineteen projects with the flat one, each packed at one version and depending
on those below it at exactly that version, pushed at 15:30 on 2026-11-11, its run green on Windows
and macOS at once and on Linux at 18:18 on a third attempt, the runners' package mirror having
failed from 15:34 to 17:54, which settles Verdict 57; its pack run without publish, 43039130555, red
by the same fault and not run again, the packages' layout having changed by Decision 88; the whole
suite passed, 2,045 passed, none failed and 3 skipped, NormTests 19; it keeps the form and carries
the review's files. The pushes before these stand in REVIEW-ARCHIVE.md's head records.

Next 136, the glow history's quarter texel mended apart and first, then the depth's points and ways
far from the origin; then 135, the bridge's size, stripped with offsets in the crash file; then 137,
a near wall's light moving with where the grids fall; then 120, the live path, each a commit with
the whole suite, and after them 66 and 67, the connections and the level editor; 93's course drawn
smaller after 119, its drive at 100 minutes in the pack run of 16:03 on 2026-11-10. The package
stands at 0.6.106 from `77941f42`, one package with its templates, published at 00:36 on 2026-11-12;
the next pack run when 136 and 135 are in. The engine's illumination read goes first on the GPU (N
7.9). No budget by Decision 80; the meadow at 8:00 the one target by Decision 81; a fault seen in
motion is read in motion by the engine's Decision 107.

REVIEW-ARCHIVE.md beside this file holds, their numbers kept, the items done, the items closed or
parked, the verdicts settled, Decisions 1 to 79, the Replies before those here and the head records,
moved there on 2026-10-23 and again on 2026-11-09 so that this file holds the current work alone and
stays under 100 KB, read at each watch tick; a batch reads this file's head, its items and the
Replies (N 7.7), and the archive when an item, a verdict or a decision named here is wanted in full.

## Now

The owner asked that the work does not stop. A batch that ends is followed by the next item in the
order under the head with no wait for a reply; global illumination is fixed first (Decision 74), the
meadow at 8:00 the one target (Decision 81).

1. **What the next page says.** Each push's run and each pack run is read by the reviewing session
    from GitHub's API, and a failure it names is a verdict here and comes first. The runs' record
    since the bump stands in REVIEW-ARCHIVE.md under Items closed, parked or shortened, with this
    item as it stood.

2. **The norm's lists are paid down.** A listed file is mended when a batch next touches it, in a
    commit of its own that moves code alone, the largest first where there is a choice, and a batch
    reads the lists for the files it will touch before it starts. Every ask the item held has
    landed, N 1.3's Slang count among them in NormTests.cs; the record stands in REVIEW-ARCHIVE.md
    under Items closed, parked or shortened.

Closed without a mend, recorded or parked, each in full in REVIEW-ARCHIVE.md under Items closed,
parked or shortened, and taken up again only as a new item: 95, global illumination on by default in
the feature test, on at Medium with the fill off and its faults taken as items 97 to 130; 104, the
deep bounces' shortfall, closed with its sampling part open, the engine's 200 the same question;
127, a probe carried from nothing, read and left out. Item 96's meshes wait on Modeling's item 3.

66. **Entities connect through outputs and inputs as Source's do, in the engine.** From Decision 61.
    An entity's output names a target entity, an input on it, a parameter, a delay and whether it
    fires once, the engine firing them at run time in order with their delays, the level file
    carrying the connections, and the classes of the feature test's level, a door, a button, a
    trigger, a light and a sound, holding their inputs and outputs; a test of a button opening a
    door after a delay and once. After item 65.

67. **The level editor edits the file in Hammer's four panes or Unreal's single view, switchable.**
    From Decision 61, level editing for artists who do not program. The editor opens a level file,
    shows it in four panes, the view and the top, front and side, or in one view, switched by a key,
    draws brushes with a block tool, moves their faces and vertices, applies textures with the
    world-aligned mapping, places entities and edits their keyvalues, and holds the connections of
    item 66 in a panel of outputs, targets, inputs, parameters, delays and fire once; it saves the
    file the engine loads. Phases proposed by the coder; after item 66. The coder's phases are four
    panes or one view, switchable, with a power-of-two grid; the block tool, select, move and resize
    with snapping, undo and saving; the clip tool, vertex editing kept convex and face texturing;
    entities, their properties and item 66's connections panel.

93. **The drive's course is cheaper on lavapipe.** From Verdict 51's reading on 2026-11-02. The
    course's stations judge only where the player stands, yet each of its frames costs 1.1 to 1.7 s
    drawn at full size on the runner's llvmpipe, and the course is 60 of the drive's minutes; the
    drive's course drawn smaller, or with fewer frames between stations where nothing is judged, or
    both, the stations' judgments and the views' pictures unchanged, read by the drive's time in the
    runner's setting on each processor the runner has and by the GPU drive unchanged. One or two
    commits, after 92. A reading from Verdict 51's runs at 08:02, each station's frames.wait of its
    frames plus five after an input.hold counts the hold's frames again, since the hold answers only
    once they have passed, so the course's walking frames run twice; the first thing this item takes
    out. The figures from the pack run of `b8cc8316`, read at 14:33, an EPYC 7763 with llvmpipe on
    four threads, the hub's frame 2,187 ms, the step ending at 123:15 and the job 10 s inside its
    125; to the zones' line 72:22 (the hub's shot 11:38, the ramps 10:48, the tunnel 14:14, the
    stairs 5:42, the beam 4:24, the rest under 4 each) and the views 50:53 (the gallery 8:14, the
    models 3:02, the hall's eight 3:51, the meadow 9:59, the effects 12:46, the night 7:32, rain and
    snow 2:25), against the container's 71:55 and 38:55; every command inside its bound, the
    gallery's frames 394 s of 900, the effects' pipelines 165 s. The course's stations hold keys for
    1,179 frames and each hold's wait counts them again, about 20 of the 72 minutes at a second a
    frame, the first thing out. The first step is `54f985a4`, read at 17:15; each wait after a hold
    is the settling alone, 5 frames, 10 after the hub's walk and 1 on the ice, about 1,180 frames
    fewer, the steep ramp keeping 50 since a character pushed up it slides back over them, the
    moving platform and the elevator judged by how far the player ranged over 180 and 240 frames
    read every 15, since two readings 90 apart depended on where in the swing the run met them; the
    GPU course in 4:14 with its pictures within the bar but the meadow's sway; the runner's setting
    cold at 97 minutes for the step where 111, the zones' line at 57:53 where 71:55, the views 38:56
    unchanged, the 7763 reckoned at about 108 of its 125; the suite 1,997 with one failure that
    passes alone 3 of 3, item 94. The course drawn smaller waits on the next pack run's reading. The
    reading of the pack run of `9ae92147` on 2026-11-10, the drive green in 100 minutes where 125 at
    its limit's edge on 2026-11-02, the first step's saving and the start at Off in it; the course
    drawn smaller is taken up after 119 where the drive's room is wanted.

120. **The live path.** Decision 85 on 2026-11-08. What Bevy's asset watching gives the bridge, a
    changed texture, model, scene or shader reloaded in the running program, is exposed and read,
    and a changed behavior script is recompiled into the running world through the bridge's runtime
    compiler, each change said in the log and reachable through the CLI, so a running program is
    edited and seen without a restart; the bridge's own code stays a restart. After 119.

135. **The bridge's size.** Decision 88 on 2026-11-11. The one package stands at 249,913,716 bytes
    of nuget.org's 250 MB, each system's bridge about 41 MB compressed, so a publish fails on the
    day a bridge grows by a tenth of a percent. One read first, on one system, of what a bridge's
    bytes are, by section and by crate, the symbols and the debug information, the features built in
    and the optimization's settings; then the cut that takes the most with nothing a game draws
    changed, held by the captures and the ABI's check, the package's size said in every pack run's
    log beside the limit. After the split is published. The limit read by the coder at 16:21 on
    2026-11-11. nuget.org's documents say up to 250 MB and no bytes; its gallery's configuration
    allows a request of 262,144,000 bytes, 250 times 1024 times 1024, answering 413 past it, so the
    pack's check fails a package above 262,139,904 and 0.6.91 has 12,226,188 bytes of room, 4.66
    percent, where the review had counted 86,284 under 250,000,000. So the item is less pressing
    than it was written and stays, the room being four or five percent of a bridge's growth. The
    read, by the coder at 22:27 on 2026-11-11 from 0.6.91's six bridges. The Linux and macOS bridges
    carry their whole symbol tables, .symtab and .strtab 58.9 MB of linux-x64's 179.7 MB unpacked,
    and stripping them saves 5.20, 6.42, 5.59 and 5.56 MB compressed on linux-x64, linux-arm64,
    osx-arm64 and osx-x64, about 22.8 MB of the package, the room going from 12.0 MB to about 34.8.
    The Windows bridges carry none. The rest is the code, .text 26.5 of linux-x64's 43.95 MB
    compressed, 38 percent of the named code bevy_ecs generics made for the bridge's systems. The
    cost is the crash file's stack, whose frames' names come of that table, the panic's message and
    its file and line staying. Decided by the review, the owner having chosen to shrink the bridge.
    Strip in the release profile; the crash file writes each frame as the bridge's build id and an
    offset, on every system; each pack run keeps the unstripped bridges or their symbols as an
    artifact named by the version, and a script under build names a crash file's frames from them,
    with a test that a forced crash on a stripped bridge is named again; the size step says the room
    before and after. Not the packed relocations, which need glibc 2.36 at run time. Whether the
    symbols are also kept for good as release assets for each tag, a run's artifacts being gone
    after 90 days, is the owner's, asked the same evening. After 136.

136. **A point and a ray's way taken from the depth far from the origin.** From the sibling's item
    257 on 2026-11-11 and a read of the bridge's own shaders by the review the same evening, nothing
    built or run, every figure reasoned from a float's step, 0.0005 at 4,450 units and 0.0078 at
    71,000. The sibling's noise in the owner's game was normals made from differences of world
    points taken from the depth, several percent off 4,450 units out, where every test scene stands
    at the origin. The bridge has the same. Every depth point comes through passWorldPosition in
    bcs_pass.slang, world_from_clip in floats, and the kernels make ways and normals from
    differences of such points, gi_screen.slang's and gi_screen_glow.slang's pointAt and wayAlong
    and gi_light.slang's, which the probes' likeness weights, the filter's and the history's tests
    and each probe's hemisphere then rest on; reasoned, a one-pixel step is off by a third of its
    length 2 units from the eye and by 6 percent at 10, visible within about 5 units where probes
    beside each other differ. Second, passRayDirection takes the near plane's point in the world
    less the eye, a direction in steps of about 6 pixels at 4,450, which the night sky's stars, the
    clouds, the fog and the reference tracer take. Third, the rain's and the snow's streaks are
    built at world points a few float steps wide. Fourth, the histories look up through
    clip_from_world of a world point. The one far test there is, the screen probes at 10,000 units,
    counts black pixels alone. First the read, one room of the bridge's at the origin and at 4,450
    units, the wall's steps at each quality, and the night sky at both, to say what shows; then the
    mend, an eye-relative point and way in bcs_pass.slang, the view's point turned by
    world_from_view's rotation, the kernels taking differences and distances on offsets and adding
    the eye only where the field or the probes are read; with a test that the room reads far out as
    at the origin. One thing to check before the histories are judged, at the origin too.
    gi_screen_glow.slang's pixelOf takes pixel 2t+1 for texel t, whose middle looks up a quarter
    texel off the texel's own, so a still view may never read as still; read what still and steady
    come to for a still view. Left as they are, the field's and the probes' lookups at world points,
    0.2 percent of a cell at 4,450, the cascades' snap, the glow lights' keys, and what is Bevy's
    own, the meshes, its shadows and its picking. After the pairs' commit and before 135. The read,
    by the coder at 23:47 on 2026-11-11, an 8-unit closed room with a shadowed lamp at the origin
    and 4,450 out on x and on -z. The night sky is visibly wrong far out, stars stretched a few
    pixels and moved off their places, 6,870 of 76,800 pixels apart by 3 levels or more,
    passRayDirection as reasoned. A wall 7.4 units from the eye reads the same far out at every
    quality. A wall 2 units from the eye reads 6 levels darker far out, most of it no precision, the
    same wall 174.1 against 182.1 at 34 and at 354 units out, which sit against every power-of-two
    grid as 4,450 does; with the grids alike, 354 against 4,450, precision costs 1.8 levels in broad
    bands and steps 6 percent rougher, measurable and not visible. The rain's streaks come a little
    wider and more broken, 3.50 levels of added light against 3.34. And pixelOf is as the review
    counted, texel t taking pixel 2t+1 whose middle looks up at t+0.25, so a still view reads still
    0.5625 and steady 0.125 and the frames settle near 1.14 where 32 is meant, at the origin too. By
    the review, pixelOf is mended apart and first, its own commit read by what still, steady and
    frames come to and by a scene whose glow light changes; then the precision mend, passEyeOffset
    in bcs_pass.slang with passWorldPosition, passRayDirection and the previous frame's lookup on
    it, the kernels' points as offsets, the streaks' corners, and two tests, the night sky far
    against near and the wall 2 units off at 354 against 4,450. The grids' share, 4.4 percent of a
    near wall's indirect light by where the grids fall, is item 137.

137. **A near wall's indirect light moves with where the grids fall against the room.** From 136's
    read on 2026-11-11. The same closed room reads its wall 2 units from the eye at 182.1 at the
    origin and 174.1 at 34 and at 354 units out, 4.4 percent of the indirect light, the same at
    every quality and no precision, the two places differing only in how the field's cells and the
    probes' spacing fall against the walls. One read, the room stepped through a cell and through a
    probe's spacing along each axis, which term moves and by how much, the field's distance at the
    wall, the probes' light or the screen probes' pick, and whether the alley's probe that varies
    between starts is the same thing. Then what holds it, as an item of its own. After 135.

## Verdicts

Verdicts 2 and 33 to 58 are settled, said under each in REVIEW-ARCHIVE.md; none is open.

## Decisions

Decisions 1 Commits stay local; 2 AGENTS.md's bullet on SHARED.md is the owner's; 3 A picture opens
the example's own source, and not Bevy's live demo of it; 4 AGENTS.md's bullet on NORM.md is the
owner's, with the exception N 7.4 makes; 5 The examples written are brought to B 4 a group at a
time; 6 A run that fails says what failed in a page, and the reviewing session is given no log; 7
The page's repeated lines are warnings and errors; 8 The next package is 0.4; 9 A document a game's
author reads names no one who decided; 10 The tests run on macOS as well; 11 Feathers' crate may be
enabled, after the other gaps; 12 The free and pan camera controllers may be enabled, after the
other gaps; 13 System font discovery stays off; 14 The sample becomes a feature test; 15 Graphics
test scenes come as packs fetched on demand, never checked in; 16 Every tonemapper here comes to
3DEngine; 17 The embedding is the bridge's own; 18 The weather is vendored and ported; 19 Bevy 0.20
comes after the deferred batch in flight and before the other gaps; 20 The crates' word for the
upgrade; 21 No document or comment a reader sees names the owner or a session; 22 Content streamed
on the go stays in the ledger, and the browser stays out; 23 A run that shows no window makes no
sound; 24 A game's session writes what the engine lacks in `.github/ASKS.md`, here as in 3DEngine;
25 A feature of a crate already in the tree that pulls no crate in is the working session's to turn
on; a feature that pulls a crate in waits on the owner's word as Decision 11 has it; 26 An insert
from nothing over a resource a plugin made, of a type that reflects no default, is refused and names
`Resource<T>`; 27 The README's gallery is one scrolling row of pictures a group, in Bevy's order of
its groups; 28 The bcs skill leaves `.claude` for `.github/BCS.md`, and the gallery's pictures are
one size; 29 The development machine's GPU is named nowhere a user reads; 30 The examples' counts
stand under the Examples heading and on each group's heading; 31 A vertex stream of the game's own,
in both engines; 32 A position is every mesh's, and the packed data a stream beside it; 33 No
development on either engine until every job is green on every push and pack; 34 Two workflows an
engine, build and pack, the same shape in both; the tester zips and the scene packs are runs on the
owner's machine; 35 Decision 33's gate is the new workflows; 36 The docs job goes; the repository's
links are the suite's to check; 37 Prose is technical and generic; 38 Development resumes, the
bridge completed first and the mends of the moment made lasting after; 39 The owner's words of
2026-10-19 on the crates, the order and the mends of the moment; 40 The README's centered parts are
markdown inside a centering block, since nuget.org draws a README without its HTML; 41 A package's
release notes are the commits since the package last published on nuget.org; 42 The pack workflow
publishes through nuget.org's trusted publishing; 43 The reviewer starts the pack runs when a
package is due; 44 The review's files are committed by the batch that comes next, and by the
reviewer at most once a day; 45 The feature test lives in the games folder, as 3DEngine's does; 46
Modeling in both engines, from the kernel to the modifiers, grid prototyping first; 47 The Actions
bill is not weighed; 48 Animated captures as tooling for documentation and devlogs; 49 A screenshot
on F2 in every program; 50 F1 hides the interface and F3 shows the debug overlay, in both engines;
51 3DEngine's radiance cascades come to this engine; 52 A denoiser for the ray-traced tier of global
illumination, beside the cascades, in both engines; 53 The owner's cloud sessions take GPU-free
items on branches of their own; 54 Branches are named for their work, never claude/ or a random
string, and work stays on main where one session writes; 55 Every commit carries the owner's
identity, and the cloud session's branch was deleted for not carrying it; 56 Slang reads the same in
both engines, plain names and no module prefix; 57 Until a full release, both engines are used by
nobody; 58 The two cheat sheets have one form; 59 A devlog under `.github/devlog`, with AGENTS.md's
row for it; 60 A session saves the pictures of its work in the repository, N 4.8; 61 The feature
test's level is redone, the look, the sky, the file, the entities' connections and an editor; 62 The
fog, the clouds and the rest of the weather are written in the engine, and the weather crate goes;
63 Trees and foliage are grown, not loaded; 64 The devlog written as the work goes; 65 The devlog
carries notes beside the pictures; 66 Free textures ship as a bounded collection, the models stay
reference; 67 The level file is Valve 220 map text; 68 The feature test's open map is legacy and its
features move into one generated level; 69 A reading's picture explains itself, and a plain capture
or pair stays where it shows the thing; 70 Every picture a session makes goes to the repository's
dump folder; 71 Six ideas for later, thought about and not scheduled; 72 The version's major and
minor move by the reviewer's judgment; 73 What a scriptable render pipeline has, written down for
later; 74 Global illumination is fixed first and used in the feature test after the radiance
cascades' fix; 75 The sky's model is a module, the drawing stays in each engine; 76 No one is named
in a picture; 77 Anisotropic filtering at 16 everywhere; 78 Crowns read thick and filled; 79 Each
README cross-references the other engine, each in full under Decisions in REVIEW-ARCHIVE.md.

80. **No millisecond budget.** The owner wrote on 2026-11-08 at 08:50, "There is no ms limit budget.
   We first make it work and performant and focus on crunching the numbers after or along the way as
   much as possible." So a mend that brings a view to its reference lands by its figures, its cost
   measured and recorded beside them and cut after or along the way where a cut holds the readings;
   item 114 and the rest land or stay by their figures, the cost a figure and not a gate, and the
   forest's and the meadow's milliseconds stay in each record.

81. **A stop rule and one target.** The owner wrote on 2026-11-08 at 08:5x, asked why the light
   leaking had moved little in days, that the review should do what it thinks right and tell them
   for approval. The owner approved at 08:49 on 2026-11-08 ("Do it"), so a view inside its band
   lands and a figure that remains is recorded and not chased unless it lies in a scene the owner
   looks at, the meadow here and Sponza's named views in the engine; one bounded read places a cause
   and a second is the last before a mend is tried or the figure recorded. The meadow at eight in
   the morning is the bridge's one target scene, its crowns and its grass against the one reference,
   the rooms the guard.

82. **The difference beside a pair.** The owner wrote to the BevyCSharp session at about 13:50 on
    2026-11-08, on item 116's meadow pair, that they saw no visible changes in the comparison, the
    crossing among them, as in the times before, and asked that where there is a difference a third
    image to the right show it. From here a reading's pair carries a third panel, the after's
    luminance less the before's at a gain the caption names, brighter in orange and darker in blue
    on black, in both engines (N 4.9); the earlier readings stand as they are. The change 116 makes
    is small, about one point in the leaves and a little more of the crown's scattered light on the
    wall and the ground behind it, which the panel shows at a gain of 40, and the owner hears it so.

83. **Version tags.** The owner wrote at about 14:00 on 2026-11-08, having tagged every commit of
    the Unity Essentials repositories as `v2.0.30` and the like, that the three projects take tags
    too, and answered the review's questions at 14:1x. The form is `v6.0.91`, the letter v before
    `build/version.sh`'s number. Tags go on each major or minor change and on the commits packages
    were built from, since "doing every commit clutters it and is unnecessary as we want to focus
    the version tags to releases time apart with meaningful changes and not commits apart". The
    major and the minor are the sessions' to raise, "each session working on the project can decide
    when the time is right to rise that number in minor or major as each commit counts as patch",
    with "full liberty" when "a step is done and meaningful changes were done". N 6.3 holds the rule
    and the reviewer sets the tags as it pushes. `v0.6.0` marks the commit that set 0.6 and
    `v0.6.18` the commit `b8cc8316` the package of 0.6.18 was published from on 2026-11-02, both set
    at 14:12.

84. **No middle dot.** The owner wrote at 17:57 and 18:00 on 2026-11-08 that the middle dot is not
    used, the README's top links separated by a space alone and the dot nowhere else; the reviewer
    changed the README's first line in the tree on their word, for the next commit to carry, and
    every other middle dot in the repository is item 118's. The rule goes into STYLE.md's
    Punctuation as the shared text has it everywhere.

85. **One repository, many projects.** The owner wrote at about 18:20 on 2026-11-08, thinking with
    the review about the engines' future after Unity Essentials, where every feature is a repository
    of its own, that the engines feel "a bit cluttered" and should be "modulated extendable and
    stable and easy to use", that splitting everything into repositories "clutters" and makes
    commits "weird", that the shared modules' repositories are right, that the engine's own content
    could be modulated the same way inside its repository, and that a "one process approach", one
    running process edited and seen as it changes, is worth thinking about; at 18:30 they approved
    the review's recommendation. The direction is that each engine stays one repository and becomes
    one solution of many projects, each an area with its own public surface and its own plugin,
    packed as its own package at one version for all, as Bevy's crates are in Bevy's one repository;
    the dependency direction is held by project references and a norm test over them; the flat API
    stays unchanged on top and a game names the packages it needs; the names are dotted .NET names
    and not com.; there is no editor, and the live path is a watcher that recompiles a changed
    shader, reloads a changed asset or scene and recompiles a changed behavior into the running
    process, the engine's own code by restart. It is scheduled after the current items and done as
    one mechanical batch per engine with the public API held identical before and after and the
    documents' tables redrawn. Items 119 and 120, 3DEngine Decision 102.

86. **Comments on the code's own terms.** The owner read a comment of scenes_sponza at about 19:00
    on 2026-11-08, "The frame graded as that scene's HDRP profile grades it, by the look of its AgX
    - Powerful.exr, a lookup build/unity-lut.py writes from that strip, a gamma of w 0.225, and a
    bloom of intensity 0.25, threshold 0.333 and scatter 0.7, worked out as HDRP works them out",
    and wrote that it "sounds just bad", reads as an em dash, and "is taking too much liberty of
    outside knowledge and in-process session knowledge", in scenes_sponza explicitly and probably
    everywhere else. The rule from it, in STYLE.md's Register and as N 4.11, is that a comment
    stands on the code's own terms. It says what the code does and why in the engine's words; a
    number taken from elsewhere stands with the reason it is right here, what it makes the frame or
    the program do, and the source is named once in the documents' references and not in the
    comment; the file names of outside assets, another product's profile or process as the
    explanation, "as X works it out", and the facts of the sessions' work are not written in a
    comment. Item 121, 3DEngine Decision 103.

87. **No readings or costs in comments.** The owner wrote at about 05:20 on 2026-11-09, reading
    scenes_sponza's comment on the far cascade of 100 that carried the lion's end wall's reading
    from 34.6 to 15.1 times its reference and the mend's 0.6 ms, "I also dont think we need to name
    the progress and ms count as these are just comments and not protocols". N 4.11 widens so that a
    comment carries no reading of before and after, no cost figure and no account of the trials,
    which are the review's records and RENDERING.md's; a number stands in a comment only where the
    code's choice depends on it, with its reason. Item 123.

88. **One package on nuget.org.** The owner chose on 2026-11-11 at about 16:20, asked by the review
    with the twenty ids of item 119's split before them, to keep one package, and wrote, "I want
    bevy csharp nuget to be one pacakge because habing so many different pacakges is a hassle and
    not great. It is also not necessary", and then of both engines, "I prefer one nuger only or two
    with the second being heavy binaries". So the library's areas stay projects of their own inside
    the repository, their layers held by the project references and N 1.6, and BevyCSharp's one
    package carries every area's assembly, the modules', the six systems' bridges, the analyzers and
    the build files; no BevyCSharp.<Area> id is published. Asked about the bridge's size, 249.9 MB
    against nuget.org's limit of 250, the owner chose to keep one package there too. The pack job
    says the package's size and fails before the push where it is over the limit. Decision 85's
    packages at one version give way to this for the bridge. Asked again at about 16:40 between one
    package and two, the second for the bridges, as the owner had written "Like BevyCSharp.Binaries
    or something", the owner chose one package with the bridge shrunk, a second package of the
    bridges alone standing at about 247 MB of the limit by itself; the shrinking is item 135.

## Replies

119's split planned on 2026-11-11, before the work. The projects are N 1.6's layers, each in its
area's folder under BevyCSharp/, named BevyCSharp.<Area>, each referencing what its code names and
nothing else, the rest coming through those. BevyCSharp.Native, the Interop folder, references
nothing. BevyCSharp.Core, holding Core, Ecs, Math, Time, Input and Behaviors as its folders,
references Native. BevyCSharp.Assets, Audio, Gizmos and ImGui (the Ui folder as it stands) reference
Core and Native, ImGui with the ImGui package, and BevyCSharp.CliServer, the Cli folder, references
Core, its name the sibling's since BevyCSharp.Cli is the command line's. BevyCSharp.Rendering
references Assets. Render2d, Animation, GlobalIllumination and Scenes reference Rendering, with
Assets for the last three and ImGui for global illumination. BevyCSharp.Ui, Bevy's interface from
the Assets folder, references Render2d and ImGui, Physics references Scenes and Gizmos with
BepuPhysics, and Sky references GlobalIllumination and Audio with the Sky module. Modeling
references Physics and ImGui, Diagnostics references Ui, Scenes and GlobalIllumination, Vegetation
references Sky and Modeling with its module, and Levels references Vegetation with its module.
build/norm/1.6.tsv is the move list, each file going to its line's project.

The flat project, BevyCSharp, keeps BevyApp, EnginePlugin and DefaultPlugins, which compose the
rest, references every project, and is the package a game names, carrying the generator and its code
fixes in its analyzer slot and the props and targets of BevyCSharp/build. A Directory.Build.props
under BevyCSharp/ runs the generators in every project, so each area's schemas, behaviors and
commands register from its own assembly, gives every project InternalsVisibleTo the engine's
projects, the tests, the editor and the script host, and packs the icon, the readme, the license and
the notices into each package. Each module's assembly is carried in the package of the project that
references it, as the sibling's Rendering carries Sky. The reflected wrappers' list goes with Core,
and the library's own kernels go with the area that runs them, embedded in its assembly, the look's
in Rendering, the field's and the probes' in GlobalIllumination and the weather's in Sky, the kernel
helpers reading the assembly they are handed.

The bridge's native libraries, every platform's under runtimes/<rid>/native, go in
BevyCSharp.Native's package alone, with the loader, the ABI check and the copy into bin for a run
from a clone with its freshness check, and every other package depends on it. Since
NativeLibrary.SetDllImportResolver is set once for each assembly, NativeLoader.Initialize takes the
assembly of the area class calling it from its static constructor and sets the resolver for that
assembly too, so every area's entry points find the one library.

The pack workflow packs every project at the one version from build/version.sh into build/package,
the push taking every .nupkg there as it does, and a run with publish pushes all of them, so the
NuGet key's pattern has to cover BevyCSharp.*, as it covers BevyCSharp.Templates. N 6.4 and N 6.5
read the natives in BevyCSharp.Native's package, the analyzers and build files in BevyCSharp's, and
every package's dependencies at the one version. The games, the templates and the examples on the
package keep their one PackageReference to BevyCSharp, a local feed of build/package serving them
all, and the feature test and the tests keep their reference to the flat project. The script host's
walk starts from the flat assembly, as item 134 said. N 1.6 then reads the project files' references
against its layers, as the sibling's does, and build/norm/1.6.tsv goes.

The first run of a game on the split packages shows the game restoring BevyCSharp and the 19 area
packages at the one version and building with the generator, the bridge found in BevyCSharp.Native's
runtimes folder on each system with the ABI check passing, ./bcs list naming the same commands as
before the split, Courtyard and the feature test's scripted run playing their courses, a script
compiling against the areas, the examples on the package building, and PublicApi.txt and
CHEATSHEET.md reading the same lines. The split's cut gets a pack run with publish.

The cut is two commits. The first moves the files into their projects' folders inside the one
project, git moves alone with N 1.6's list read from the folders, and the second makes the projects,
the packing and the workflow. BUILDING.md's table of packages, the guide's lines, RENDERING.md's
names and AGENTS.md's table of where things are follow the folders. That table's rows are the
owner's word, which I ask for in my session before the first commit.

No file of the engine changed. NormTests passed, 19. The public listing is unchanged.

Item 136's first commit, the glow history's quarter texel, apart and before the precision mend by
the review's word, in the commit carrying this line. A texel of the glow lights' half-sized images
takes its point at its pixel's middle, pixel 2t+1 for texel t where the picture is twice the images'
size, a quarter of a texel off the texel's own middle, and the frame before's texels hold their
light at the same part off theirs. A still view looked its place up a quarter texel along each axis,
shared it with three other texels, read as still for 0.5625 and steady for 0.125, and kept frames at
a seventh over one where it is meant to keep 32, so the review's count holds. The history's place is
looked up less the texel's own part, so a still view finds its texel whole.

Read before and after on a wall seen at a slant and lit alone by a glow light on the floor beside
it, on Medium, with a scratch probe never committed that wrote frames over 32, steady and still into
the glow's normal image and showed them in place of its light. Before, still read 0.58, steady 0.14
to 0.16 and frames 1.2 to 1.26, and after, each read whole, frames about 31 to 32. In a still view
the wall's pixels moved by 0.289 of a level between frames before and by 0.023 after, as the
screen's probes the glow's light is read from step through their sixteen places. Moving sideways two
centimeters a frame, they moved by 0.665 before and 0.529 after. Switched from dim to lit, the wall
read 180.9, 187.7 and 189.2 after one, two and four frames before and 183.0, 187.6 and 189.0 after,
settling at 189.8 and 189.6, so the history follows a change as fast as it did and holds still where
it did not.

No example draws global illumination, so no capture of the examples moves, and the feature test's
views, read in the next pack run, show the rest.
GlobalIlluminationTests.AStillViewOfAWallLitByAGlowLightHoldsItsLightFromFrameToFrame holds a still
view's wall within a tenth of a level between frames, and without the change fails at 0.289.

The whole suite ran in two parts, the engine's collection under the GPU's lock in 77 minutes and
every other class with it let go in 21 seconds, 2,048 passed, 1 failed and 3 skipped, 2,052 in all
where the listing is 2,041, with no abort. The one failure is
AFloorUnderASlabThatLetsLightThroughTakesTheSkyLightItsShareLetsThrough on High at 5.5, its share
0.180 where 0.2 to 0.5 is held, a scene of sky light through a slab with no glow light in it, so the
glow lights' pass this commit changes does not run there. Run alone after the suite, its four cases
passed with the kernel as it was and as it is. The engine part took 77 minutes where the whole suite
took 53 the evening before. NormTests passed, 19. No native file changed, the solution builds with
warnings as errors, and the public listing and the cheat sheet are unchanged.

Shared: a texel of a half-sized image whose point is its pixel's middle and not its own looks its
history up less that part, or a still view reads as moving, for the sibling's glow texels, whose
points stand at the depth's texel middles.
