# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it already keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read. A stash of every changed file
takes what was written here since the last commit out of the tree until it is popped, so a stash
names its own paths.

Reviewed up to `4e15bea`. Verdict 1 is settled (`5264257`): every text file checks out with LF
ends, the norm's tests read files through one helper that ends lines with `\n`, the page's test
reads the script in UTF-8 on either ending and the script writes `\n` alone, and a picture asked
for with no interface running loads nothing on any bridge, the expected error never having been
possible on the headless bridge where the suite runs. Three more groups of examples keep on
their entities what Bevy's keep on theirs (`8637e7c`, `2724064`, `4e15bea`). No verdict is open.

Before them, nine commits brought the examples' groups to B 4, what Bevy's keep on an
entity kept in a behavior, with `GameTimer` as Bevy's `Timer` and `EaseFunction` as the package's
own (`3944016` to `e805d3a`). The run of `b263f6d` is the first with this engine's page, read from
GitHub: Linux passed 722 and failed 1, Windows passed 638 and failed 3, with 351 skipped on each
where there is no device, and the digest job joined the two.

Before them, `b263f6d` brings the first three groups of examples to B 4, what Bevy's keep
on an entity kept in a behavior, and the owner pushed up to it on 2026-10-06. The run of `b091fe3`
was canceled by that push before its tests ended, and its build printed warnings, a possibly null
dereference and XML comments that do not match their members among them, which item 6's N 6.1
clears.

The norm has 43 rules, and this engine stands at 26 checked, 4 with places listed, 4 to take
and 9 by review.

## Now

The owner asked that the work does not stop. A batch that ends is followed by the next item
here with no wait for a reply, and the list is long so that it does not run out. Items 4, 7 to 9
and 11 to 15 are taken from [SHARED.md](SHARED.md).

1. **What the next page says.** The run after `5264257` is pushed shows whether the three failures
   are gone, which the reviewing session reads and says here. The list goes on meanwhile.
2. **The norm's lists are paid down.** A listed file is mended when a batch next touches it, in a
   commit of its own that moves code alone, the largest first where there is a choice, and a
   batch reads the lists for the files it will touch before it starts. The rules still to take
   each have their item: N 2.2 and N 6.1 are the settings of item 6, N 2.1 the listing of item
   13, N 2.6 the table of bad files in item 12, and N 2.7 item 3.
3. **An example is a program somebody could write on the package.** A picture in the README
   opens an example as the way to do a thing, and what opens is written in words the package
   does not have. `BevyCSharp.Examples/Example.cs` holds helpers, in its own words for what
   Bevy's examples say in one word and the bridge in several, and 208 of the 231 examples call
   `app.Startup`, 171 `app.Update`, 56 `Scene.Srgb8` and 25 `SpawnGltf`, beside a mesh spawned
   with its material and its place in one call, a point light, a camera, `Slerp` and `Hsl`. None
   of it compiles outside the examples project. Each of those helpers is something a game says
   in several calls too, so they become the package's own, named and documented as the rest is
   and in the cheatsheet, and `Example.cs` keeps only what drives an example for its capture.
   A check builds a handful of examples in a project of their own on the packed package, as the
   README's walk does, so one that leans on the examples project fails it.
   `stress_tests/StressTest.cs` is such a helper as well. The window's three settings are said
   by each test, as Bevy's says them, and the log of frame times is Bevy's own
   `FrameTimeDiagnosticsPlugin` and `LogDiagnosticsPlugin`, added by the bridge where an app
   asks, so the two programs measured log by the same code and the C# one runs no system of
   its own for it.

   The README's first program is `[Behavior]` on a struct, which 8 examples use while 136 keep
   their state in static fields. Where Bevy's example keeps state on an entity, in a component
   with a system over it, as `cooldown` does with a timer on each button, the example here keeps
   it on the entity in a behavior, and a static field is for what Bevy keeps in a resource or a
   `Local`. The examples written are brought over a group at a time, EXAMPLES.md saying how many
   are, the helpers first since every example reads shorter for them.
4. **What a kinematic body carries, and a clock stepped by hand**, both from a red run of
   3DEngine's on Windows, mended there in `966c2c88`, `15fa305a` and `ee3b47dd`, and measured here
   before anything is changed. There the engine measured where the model put it, 1.75 against
   1.74 at 144 frames a second, and holds 2.00 at every rate since.
   `PhysicsWorld.Step` puts a kinematic body at its entity's place and gives it the distance
   moved since the last step over one step's time. An entity moved once a frame then has a body
   whose speed in a step is the frame's distance over a step's time, and zero in every further
   step of the frame. Friction is too weak to follow those jumps, so what rests on the body
   settles at the speed it has in most steps and not at its average. A model of one dimension
   gives, for a platform at 2.00 and Bevy's 64 steps a second, a crate at 2.12 at 60 frames a
   second, 1.82 at 144, 2.52 at 50 and 0.07 at 30. `Step` is public and takes its seconds, so a
   test moves a platform's entity once a frame, steps as many times as a frame of that length
   holds, and reads the crate's pace, as a table over 144, 75, 60, 50 and 30 frames a second and
   over frames of uneven length.
   If the engine agrees with the model, the body moves at its entity's speed, which is the
   distance the entity moved between two frames over the time between them, through every step
   of the next frame, and each step aims at the entity's last place moved on by that speed for
   the time simulated since. That holds 2.00 at every rate in the model. A turn gives the body no
   spin today, so nothing is carried round on a turning platform, and it is given the same way.
   An entity moved in fixed steps has to stay exact.
   The clock is Bevy's own `TimeUpdateStrategy::ManualDuration`, reached from C# and from `bcs`,
   so a test or a capture advances a set amount a frame and is the same on every machine.
   `Time.Step` runs frames at the delta they would have had, which is the machine's.
5. **The gaps, by how many rows each holds**, each bridged from Bevy
   with the examples it unlocks written in its batch: more of Bevy's WGSL reached as its
   lighting is (the deferred buffers, a decal's tag and a volume's voxels), the widgets' events
   as observers, keys observed as they reach a field, and what the table then names most. When
   the captures have settled, they are compared whole with checked-in references by the
   workflow, a small share of pixels allowed to differ between devices, as 3DEngine does for its
   scenes.
6. **A build with no warnings, and a warning failing the workflow.** The managed build passes
   `-warnaserror` in the workflow once it is clean, with a warning that is right to keep turned
   off where it arises and its reason beside it, and `cargo` builds deny warnings the same way.
7. **More of what bodies do**, from 3DEngine's `c5227118`, `8520dbe1`, `799a9d56`, `979c97be` and
   `53cd565f`: the speed a pair closed at on the contact's message, a ball joint kept within a
   cone, a distance joint whose range changes after it is made, bodies on layers whose pairs
   collide or not, which contacts, triggers, the character and rays follow, a body asleep waking
   when its layer changes, a body a game knows is fast swept over each step so it does not cross
   a thin wall, a slider joint with limits, a motor and its position, and how hard two touching
   bodies press, as the push alone and answered while they sleep.
8. **Two more of a character and a collider**, from 3DEngine's `52579d98` and `454e9276`: a
   character that crouches and stands from its component's height, and a collider that is the
   shape of the meshes an entity and those under it show, made once the model has loaded.
9. **A joint in a scene file, and a pad's sensors**, from 3DEngine's `e46058fc` and `73ce6326`:
   a joint as an entity naming its two bodies, so a level hangs a door where it stands, and a
   gamepad's gyro, accelerometer, touchpad and light where gilrs offers them. Two small things
   of the command line are checked in the same batch and taken if they are missing: `entity.set`
   writing a field that holds a list from items split by semicolons, a command that pretends
   files dropped on the window, a command's parameter with a default being left off, and a
   placed scene file spawned again when it is written while the level runs, under the entity
   that placed it and giving back what the old copy held (`5b2234d2`). A third: a command takes
   an enum member by its name alone, where `Enum.TryParse` takes any number as well, as
   `ConsoleWorldCommands.cs` reads gamepad buttons, axes and keys, and as 3DEngine's
   `InputCommands.TryName` does since `ef042886`, where a button of 100 stopped the program.
10. **The three shapes no wrapper types**, when a batch next touches the generator: a list inside
   a component (box shadows, gradients), an enum inside a variant (a sprite's slicer, an
   orthographic projection), and a range of numbers (`VisibilityRange`), which are the 13
   string paths the examples still hold.
11. **Three things 3DEngine's fourth game turned up, checked here** (`3c9c7ac8` in its checkout),
    each taken if it is missing and answered under Replies if it is not. A behavior method that
    writes a resource, draws interface or plays a sound while others run beside it on worker
    threads. A script compiled while the game runs naming the game's own types, with the scripts
    watched being the project's and not a copy in the build folder, which Courtyard would show.
    And a game written in behaviors alone with hundreds of entities, played by the workflow and
    profiled, which `games/Stress` measures and no game here plays.
12. **Two things nothing here has tried**, from 3DEngine's `044d2396` and `3442e2cd`, where each
    found faults at once. Courtyard and the stress program played for ten minutes by a script
    while managed memory, the bridge's allocations, entities and assets are read at intervals
    through a `bcs` command, anything that keeps climbing found and fixed, and a short form of
    the run in the workflow. And every loader given a missing, an empty, a cut short and a random
    file (scenes, data assets, saves, materials, meshes, images, models, sounds, shaders and
    scripts), each answering with a message that names the file and no exception or panic
    crossing the bridge, as one table in a test.
13. **The public surface written down, and release notes from the commits**, from 3DEngine's
    `fc5aef49`: a listing of every public type and member a tool writes from the built assembly,
    checked in, with a test that fails when the two differ, so a change to what a game calls is
    read as one, and the pack workflow writing the package's release notes from the commits
    since `build/version.txt` last changed. In the same batch it is checked whether a ray here
    stops at a sensor, which there threw a car's wheel and a character's ground check.
14. **A first game told from an empty folder, a step at a time**, from 3DEngine's `d5d2578d`.
    `docs/making-a-game.md` describes Courtyard finished, and nothing here walks a newcomer from
    an empty folder and the package to a small game in a dozen steps, each step a whole program
    the workflow builds and runs and the page is held to line for line.
15. **What a script host reads at each compilation**, from 3DEngine's `c06ec659`, where it took
    the Linux test job to the runner's 16 GB. `ScriptHost.References()` reads every loaded
    assembly with `MetadataReference.CreateFromFile` at each compilation, at line 190 of
    `ScriptHost.cs`, and each reference holds its file's whole image in native memory that only
    its finalizer gives back, while the GC's heap stays small and a full collection comes late.
    A host that compiles on each save gathers them. They are read once for the process and
    shared, as `EditorEval` keeps its own, and a test compiles a hundred times and finds the
    process holding within a few megabytes of what it held after ten, read before any
    collection. The row on an app's whole life in SHARED.md is checked in the same batch. So is
    whether a script's generation unloads when it is compiled again, as 3DEngine's
    `ScriptGenerationTests` holds since `d7e370ed` there, where a registration kept by the
    process held every generation and ran a stale script in every later app. `BehaviorsPlugin`
    passes over a collectible assembly's behaviors here, and whether a script's assembly adds
    schemas, commands or states to the lists of the process, as the module initializers the
    generator writes do for a game's, is read with it.

## Verdicts

None open.

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

## Replies

`GameTimer` and `TimerMode` are the package's own, Bevy's `Timer` as a struct a behavior keeps on
its entity and ticks in place, ticked as Bevy ticks its own, a repeating one counting each time a
long tick runs it out. It is not called `Timer`, since `System.Threading.Timer` is among the names
every C# program imports and the two would be ambiguous in every game. The examples that keep
timers on entities, cooldown and the sprite animations among them, are written on it as their
groups come.

B 4 for Time, Gizmos, glTF, Window and Application, 31 of 119 with these. Each marker of Bevy's
is a behavior on its entity, found by its query where Bevy finds it, and each component with data
keeps it, the tracking of axes' cubes, a helmet's tint, a text's last size and count. The work Bevy
does over a component is the behavior's own method where it is about that entity alone. Every
example's behaviors register in every example app, so an `[After]` names only a behavior's system,
since one naming an example's own system stopped every other example when tried. headless_renderer
is not counted and has nothing to bring, its two components being the render world's copying of
the picture, which the capture does in its place.

B 4 for Camera and Assets, 37 of 119. The shaking camera carries its configuration and its state
as Bevy's does, the state putting the camera back before each frame and the configuration shaking
it after, with the state taken beside it. The first person player turns with its sensitivity
beside it, and the world camera keeps the field of view Bevy keeps in its projection. Bevy's
`Bird` and `Shape` are enums on their entities, held here as a field of the behavior, and their
`Left` marks the one changed in place, `LeftShape` for alter_mesh where both share a namespace,
as `ViewModelPlayer` is for the second of two `Player`s.

B 4 for Usage, 40 of 119, cooldown among them as item 2 has it. Each food's button keeps its
`GameTimer` in a `Cooldown` behavior, eaten on a changed press and animated while a sparse
`ActiveCooldown` marks it, which filters the method and is never iterated, a sparse component
being one a query cannot iterate. The context menu's items each keep their color and react to
their own interaction changing, armed once the press that opened the menu is let go, as Bevy's
press events are, and a close is queued once a frame since an item's press and the background's
both ask. debug_frustum_culling's ring, shapes, wall and camera are each a behavior, the frustum
read once a frame for every shape. A press could not be checked live, a windowless run having no
pointer to hold an interaction pressed past Bevy's own focus system, which the old code met too.

B 4 for Shaders and Audio, 46 of 119. Each turning thing of the shader examples turns by its own
behavior. Each emitter keeps its stopwatch, its time run and whether Space stopped it, the 3D one
as `Emitter3d` beside the 2D one's `Emitter` in the namespace they share. The soundtrack's tracks
carry `FadeIn` and `FadeOut` and fade by the state's `GameTimer`. Bevy takes `FadeIn` off once a
track is in and finds it again by its sink, where here it stays on with a flag, the mark the next
change finds the playing track by.

B 4 for Transforms, 51 of 119. Each cube carries its own `Rotatable`, `Movable`, `Scaling` or
`CubeState`, and the transform example's sphere its `Center`, which sizes itself after the cubes
have moved and turned, as Bevy chains the three. align's ship carries its target and whether it
turns, the two directions sit on an entity of their own as `RandomAxes`, and H hides the
instructions by their `Visibility` as Bevy's does, where the text was emptied before. Its own
`FromAxes` gave way to `Quat.FromBasis`, which the library already had.

B 4 for Animation, 55 of 119, with Bevy's `EaseFunction` made the package's own first, a struct
a behavior keeps as a field, its kinds in `EaseKind` and its staircases' `JumpAt`, held to
bevy_math's own tests of it, and the examples' `Ease.cs` gone. Each easing plot carries its
function and its hue, its dots and label its children as Bevy's are. The fox and the morphing
model are spawned at once with `AnimationToPlay` on their roots, which plays the clip once the
model has come, `MorphAnimationToPlay` in morph_targets where the two share a namespace. Bevy's
generic `Curve<T>` and `Mixed<T>` are one `Curve` and one `Mixed` holding their space beside four
points in an `InlineList4`.

B 4 for the examples in shader_advanced, 58 of 119, all but one of them under Shaders in Bevy's
list. Each camera's `PostProcessSettings` and `FullscreenEffect` carry their intensity, set by
their own method and copied into the pass's values after Update, as Bevy extracts the component
each frame, and T takes `FullscreenEffect` off the camera and puts it back as Bevy's does. The cube
turns by `PostProcessRotates`, shader_prepass's `Rotates` sharing the namespace, and the red cube
carries `GenerateMesh`, which runs the compute shader for it once. custom_shader_instancing is left
as headless_renderer is, its two components being the list of instances Bevy's render world draws
from and the buffer it makes of them, with no system over either in the main world, which the
camera's draw holds here in their place.

B 4 for Games, 63 of 119. breakout's paddle, ball, bricks, colliders and scoreboard each carry
Bevy's component, the three chained systems ordered by `[After]` and a brick despawned by a
command as Bevy's is. contributors' birds carry `Contributor` and `ContributorVelocity`, breakout's
`Velocity` sharing the namespace, and the display brings the next forward by a repeating
`GameTimer`. desk_toy's logo, pupils and instructions are behaviors, the pointer's place in the
world read by an app system ordered `Before` the logo's first, which a behavior's system can be
named by from an example since every app has it. loading_screen marks its level and its screen,
the example's class `LoadingScreenExample` beside the behavior. game_menu's buttons carry
`MenuButtonAction`, `QualitySetting` or `VolumeSetting` and `SelectedOption`, which moves by
command, with a `MenuButton` in place of Bevy's `Button`, and each screen carries its marker. Their
methods ask the state themselves rather than by `[InState]`, which would warn in every other
example that its state was never added.

A windowless run's interface had no camera on its first frame. The bridge points the cameras at
the run's image and chooses the interface's camera among those pointed there, two systems in
First with no order between them, and the choice came first, found the camera still pointed at
the window, and left the interface to be laid out that frame against no camera at no size.
text_debug's columns, a margin inside a node that stretches, came out smaller than nothing there
and Bevy's border radius asserted on them, so the example stopped on its first frame, offscreen
and so in every capture, whatever its code. The choice is chained after the pointing, and a test
holds the camera to be the interface's on the first frame, which it fails on the bridge before.

B 4 for UI, 89 of 119. Each of Bevy's components is a behavior on its node, its system the
behavior's method where it is about that node, among them the output text's submission, the
changing text's spans written by their places, the letter spacing toggled and widened on the texts
that carry it, the transform panel slid by its `TargetNode`, a gradient turned by its
`AnimateMarker`, the bar's sizes set by each button's `Constraint` and `ButtonValue`, a slider's
thumb placed by its `DemoSlider`, the frames clipped and sized by their `Container`, a box shown or
hidden by `DisplayTarget` and `VisibilityTarget` in place of Bevy's generic `Target<T>`, and the
shadow's buttons acting by their `SettingsButton`. A `TransformButton` stands in for Bevy's
`Button` in ui_transform, whose button system is over every button, where the panel's own buttons
now light under the pointer as Bevy's do. A value that is no component's field here, a gradient's
stops and a `Val`, is kept beside its node or as the two values its buttons hold, and
vertical_slider's class is `VerticalSliderExample` beside Bevy's `VerticalSlider` marker.
