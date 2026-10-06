# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read. A stash of every changed file
takes what was written here since the last commit out of the tree until it is popped, so a stash
names its own paths.

Reviewed up to `db01753`. The run of `ba5f72c` passed on Linux and Windows, read from its page, the
three failures of `b263f6d` gone, which settles item 1 as it stood. The pretend wheel is Bevy's
`MouseWheel` as a real wheel's report begins (ABI 208), so a game, the interface and picking all see
it, and `scroll` is written with its own event carried up from picking's `Pointer<Scroll>`, 250, the
one grid that does not scroll being Taffy 0.10.1's, upstream's to mend (`a683c3e`); the widget tests
moved to `Assets` by a move alone, N 1.4's list at 93 to mend (`db01753`). The reply on the widgets'
events (ABI 209) is written, its commit to come. No verdict is open.

Before them, a game came to spawn pointers of its own and puts them on an image an interface is
drawn into, as Bevy's `PointerId::Custom`, and a ray cast answers the texture coordinate it met (ABI
207), so `render_ui_to_texture` is written, 249, and TODO.md's example gaps say what each of the
three left waits on (`d59365f`); the input tests moved to `Input` by a move alone, N 1.4's list at
94 to mend (`ba5f72c`). The reply on `scroll` is written, the pretend wheel going through Bevy's
`MouseWheel` and picking (ABI 208), its commit to come. The owner pushed.

Before them, a game came to observe what a pointer does to an entity as Bevy's `Pointer<E>`, the
seventeen kinds, taken up the parents and stopped with `on.Propagate(false)` (ABI 206); picking is
in the render profile as decided, measured as one clean release build each at 3 seconds and 0.6
percent more, mesh picking compiled in and added where `Config.MeshPicking` asks; an offscreen run
is pointed at through its image, the rays Bevy casts for no primary window added by `offscreen.rs`,
so an offscreen editor takes clicks on its scene; six more examples are written on the pointer's
events, 248, and `mesh_picking` and `simple_picking` run on observers in the render profile
(`b548987`, `4034f91`). Two things seen there are not mended: a move and a press at a new place in
one frame miss, as a window's mouse does in Bevy, and `dragdrop_picking`'s preview draws over the
words Bevy sorts it under, untraced, which item 4 holds with the captures. The pick ray's tests
moved to `Assets` by a move alone (`96a61e6`).

The norm has 43 rules, and this engine stands at 26 checked, 4 with places listed, 4 to take
and 9 by review.


## Now

The owner asked that the work does not stop. A batch that ends is followed by the next item
here with no wait for a reply, and the list is long so that it does not run out. Items 5 to 7
and 9 to 13 are taken from [SHARED.md](SHARED.md).

1. **What the next page says.** The run of `ba5f72c` passed on both systems, the first green run
   with the page. Each push's run is read by the reviewing session, and a failure it names comes
   first here.
2. **The norm's lists are paid down.** A listed file is mended when a batch next touches it, in a
   commit of its own that moves code alone, the largest first where there is a choice, and a
   batch reads the lists for the files it will touch before it starts. The rules still to take
   each have their item: N 2.2 and N 6.1 are the settings of item 4, N 2.1 the listing of item
   11 and N 2.6 the table of bad files in item 10.
3. **The gaps, by how many rows each holds**, each bridged from Bevy
   with the examples it unlocks written in its batch: more of Bevy's WGSL reached as its
   lighting is (the deferred buffers, a decal's tag and a volume's voxels), the widgets' events
   as observers, keys observed as they reach a field, and what the table then names most. When
   the captures have settled, they are compared whole with checked-in references by the
   workflow, a small share of pixels allowed to differ between devices, as 3DEngine does for its
   scenes. Transmission's glass spheres are missing from about one capture in four with TAA
   on, before `6a84286` as after it, so the cause is found before that job is red for them, or
   the example is compared with its spheres left out and the reason beside it.
   `dragdrop_picking`'s pale preview draws over the words Bevy sorts it under (`b548987`'s reply),
   untraced, and is traced before those captures are compared.
4. **A build with no warnings, and a warning failing the workflow.** The managed build passes
   `-warnaserror` in the workflow once it is clean, with a warning that is right to keep turned
   off where it arises and its reason beside it, and `cargo` builds deny warnings the same way.
   The build before a commit runs with `--no-incremental` when it checks for warnings, since
   3DEngine's incremental build passed over a test project an earlier build without the flag
   had left up to date, and a warning reached `main` (its `2b39ddd2`).
5. **More of what bodies do**, from 3DEngine's `c5227118`, `8520dbe1`, `799a9d56`, `979c97be` and
   `53cd565f`: the speed a pair closed at on the contact's message, a ball joint kept within a
   cone, a distance joint whose range changes after it is made, bodies on layers whose pairs
   collide or not, which contacts, triggers, the character and rays follow, a body asleep waking
   when its layer changes, a body a game knows is fast swept over each step so it does not cross
   a thin wall, a slider joint with limits, a motor and its position, and how hard two touching
   bodies press, as the push alone and answered while they sleep.
6. **Two more of a character and a collider**, from 3DEngine's `52579d98` and `454e9276`: a
   character that crouches and stands from its component's height, and a collider that is the
   shape of the meshes an entity and those under it show, made once the model has loaded.
7. **A joint in a scene file, and a pad's sensors**, from 3DEngine's `e46058fc` and `73ce6326`:
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
8. **The three shapes no wrapper types**, when a batch next touches the generator: a list inside
   a component (box shadows, gradients), an enum inside a variant (a sprite's slicer, an
   orthographic projection), and a range of numbers (`VisibilityRange`), which are the 13
   string paths the examples still hold.
9. **Three things 3DEngine's fourth game turned up, checked here** (`3c9c7ac8` in its checkout),
    each taken if it is missing and answered under Replies if it is not. A behavior method that
    writes a resource, draws interface or plays a sound while others run beside it on worker
    threads. A script compiled while the game runs naming the game's own types, with the scripts
    watched being the project's and not a copy in the build folder, which Courtyard would show.
    And a game written in behaviors alone with hundreds of entities, played by the workflow and
    profiled, which `games/Stress` measures and no game here plays.
10. **Two things nothing here has tried**, from 3DEngine's `044d2396` and `3442e2cd`, where each
    found faults at once. Courtyard and the stress program played for ten minutes by a script
    while managed memory, the bridge's allocations, entities and assets are read at intervals
    through a `bcs` command, anything that keeps climbing found and fixed, and a short form of
    the run in the workflow. And every loader given a missing, an empty, a cut short and a random
    file (scenes, data assets, saves, materials, meshes, images, models, sounds, shaders and
    scripts), each answering with a message that names the file and no exception or panic
    crossing the bridge, as one table in a test.
11. **The public surface written down, and release notes from the commits**, from 3DEngine's
    `fc5aef49`: a listing of every public type and member a tool writes from the built assembly,
    checked in, with a test that fails when the two differ, so a change to what a game calls is
    read as one, and the pack workflow writing the package's release notes from the commits
    since `build/version.txt` last changed. In the same batch it is checked whether a ray here
    stops at a sensor, which there threw a car's wheel and a character's ground check.
12. **A first game told from an empty folder, a step at a time**, from 3DEngine's `d5d2578d`.
    `docs/making-a-game.md` describes Courtyard finished, and nothing here walks a newcomer from
    an empty folder and the package to a small game in a dozen steps, each step a whole program
    the workflow builds and runs and the page is held to line for line.
13. **What a script host reads at each compilation**, from 3DEngine's `c06ec659`, where it took
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

**Now 3, the widgets' events.** What Bevy's widgets report reaches C# as Bevy's own events, an
observer in the bridge for each kind copying it into one shape (`widget.rs`, ABI 209), as the
pointer's do. `Activate` from a button or a menu item, `ValueChange<T>` from a slider (a float), a
checkbox or a radio button (a bool) and a radio group (the button chosen), and `MenuEvent` from a
menu, which C# takes up the parents to the menu's owner, `MenuAction` and `NavAction` beside it.
`standard_widgets` is written on them, 251, its slider and radio group kept in the example's own
record and set from it, its menu's popup spawned and despawned as asked, with Bevy's `Popover` and
`BoxShadow` written as JSON. Driven offscreen by `bcs`, the button logs its click, the checkbox
checks, the slider snaps to where its track was clicked, a radio button changes the track click,
the menu opens below its button and closes on an item, and D draws every widget disabled. Its
widgets are styled each frame from what they hold, written only when that changes, where Bevy's
systems run for a widget whose state was added, changed or removed. `standard_widgets_observers` is
left, since it observes Bevy's own components coming and going, `Pressed`, `Hovered` and the rest,
and C# observes its own components' alone, which is the next batch. The widget tests moved to
`Assets` first, by a move alone (`db01753`), and `WidgetTests` holds the report's layout and a
button, a checkbox, a slider, a radio group and a menu button clicked offscreen, each event heard
where it should be. Its remarks said an offscreen run cannot point at a widget, and `docs/ui.md`
said so too, which `b548987` changed.

**Now 3, Bevy's components observed.** `standard_widgets_observers` is written, 252, which ends the
widgets' rows. A game observes one of Bevy's own components coming and going through its wrapper,
`ecs.Observe<Add<PressedRef>>` as Bevy's `On<Add, Pressed>`, with no change to the bridge, whose
`bcs_component_id_of` already resolves a reflected component by its type path. Each generated
wrapper also implements an internal `IReflectedWrapper<T>` saying its path and making one over an
entity, which the observer asks of a default value, since it takes a C# component or a wrapper and
so is constrained to neither. The five lifecycle events and `ComponentType<T>` are constrained to
`struct` rather than `unmanaged` for it. Every public call that reads or writes a component's bytes
keeps `unmanaged`, and `ComponentType<T>` refuses a type holding references as it registers one. A
wrapper's event hands on a wrapper over the component, read as it is when read. Two types naming one
component, the `Transform` mirror and `TransformRef`, were heard only as the first that asked, the
reports being kept by component, and are now each heard in turn from the one observer the bridge
keeps. Driven offscreen by `bcs`, the button shows Hover and Press, the box checks, a drag moves the
thumb, and D disables and enables all three, each through an observer. The observer tests moved to
`Ecs` first, by a move alone (`47a2497`), and `ObserverTests` holds `TransformRef` and its mirror
heard alike, a removal's wrapper finding the component gone.
