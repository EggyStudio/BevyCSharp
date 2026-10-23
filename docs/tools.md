# The tools

Both are ordinary BevyCSharp apps rather than privileged ones, so anything learned building them
applies to building a game.

## The editor

`BevyCSharp.Editor` runs the same way the sample does and is built on the same library, with no
privileged path into the engine, because the editor is a BevyCSharp app whose behaviors happen to
draw an editor.

```bash
build/build-native.sh --editor
dotnet run --project BevyCSharp.Editor
```

The scene fills the window and the panels float over it. The panel on the right holds the world
beside the details of whatever is selected, the tools float in the scene's corners, and the tabs
along the bottom open the console, the asset browser, the shaders, the settings and the style. The
Shaders tab lists every program with whether it compiled and what the compiler said, and the details
of an entity drawn by a shader material show its program and its numbers as rows of four, which can
be dragged while it draws. Docking the panel
gives the camera the rectangle that is left rather than drawing it behind. The arrangement is a
handful of numbers that `EditorShell` owns and every part reads, saved with the settings, so the
editor opens the way it was left. The look is one theme file, `assets/theme.txt`, which the Style
tab writes, and Ship with project writes into the project's own `assets/project.json`
(`ProjectSettings`), beside the startup scene, the window's title and icon, the fixed step and the
export's choices, which an app reads as `app.Project` and takes its fixed step from, its window's
title where its config leaves the default, and its window's icon, which Windows and X11 show. [.github/EDITOR.md](../.github/EDITOR.md) has the design language in full.

The Play tab runs the game in a window of its own and stops it again, builds it without running it,
exports it for a player and shows what each wrote. `F5` plays the scene being edited instead,
through `BevyCSharp.Player`, so a change made in the editor is seen without being saved or written
into code. [.github/PLAY.md](../.github/PLAY.md) has the plan past that. The Model tab shapes the
model an entity selected is drawn with, its faces picked, extruded and moved in the scene, as
[Modeling](modeling.md#the-window) describes.

Opened on a project, the editor edits that project's assets rather than its own:

```bash
dotnet run --project BevyCSharp.Editor -- --project games/Courtyard
```

Its fonts, icons and theme stay beside its build and are read as `editor://`, so a project's files
load by the paths the game uses for them. The document is the project's startup scene when
`project.json` names one and `world.scene.json` when it does not, and `world.load` and `world.save`
open another and save under another name. The project's scripts are compiled once the level is up,
and their components are put on the level's entities then and moved onto the new types each time a
script is saved. Every row of the Settings tab, the project's own among them, is read and changed
by `setting`, as in `setting "Project/Startup scene" levels/one.scene.json`, so a script can set
up a project the way a person would.

A level carries its cameras as it carries its lights. Spawn/Camera puts one where the editor is
looking from, and each camera the level has is held off while it is edited, so it does not draw
over the editor's view, and drawn as the shape of what it sees. Entity/Look through camera moves
the editor's view to it and Entity/Move camera to the view moves it to the editor's, which is how
one is placed. A save writes the cameras the editor held off as on, so Play and the game start
from the level's camera, and one the level holds off of its own accord is written off.

Two things it is built on belong to the library rather than to the editor, and any tool can use
them.

**Showing a component needs no reflection.** The generator emits a `ComponentSchema` for every
`[Behavior]` struct, holding each field's name, its kind, and a pair of closures that read and write
it, and `ComponentSchemas` maps a live component id to it. So an entity's components can be listed
and edited without naming a single type:

<!-- compiled with:
Entity entity = default;
-->
```csharp
foreach (var id in ctx.Ecs.ComponentsOf(entity))
{
    if (ComponentSchemas.For(id) is not { } schema) continue;

    foreach (var field in schema.Fields)
        Console.WriteLine($"{schema.Name}.{field.Name} = {field.Read(ctx.Ecs, entity)}");
}
```

A field whose type is a struct with fields of its own is taken apart, so `Front.Held.At` is a row
called `At`, in a fold called `Held`, in one called `Front`. Writing one reads the component,
changes that part and writes it back, so a part written does not wipe its neighbors. Bevy's own
components have schemas too, described from Bevy's reflection once the app is running, so the
same loop lists a light's fields beside a behavior's, and `ComponentSchema.Origin` says which is
which.

**A field says how it is drawn**, in attributes the generator reads at compile time, so nothing
reflects at runtime:

<!-- compiled with:
public enum Mode { Idle, Running }
public Mode Mode;
private void Rebuild() { }
-->
```csharp
[Range(0, 1, Readout = SliderReadout.Number)] public float Weight;   // a bar, and the number
[Separator]                                                          // under a line
[Info("Changing this rebuilds the shape.", Kind = NoteKind.Warning)] // said in the panel
[OnValueChanged(nameof(Rebuild))] public float Radius;               // and something to call

[ShowIf(nameof(Mode), Mode.Running)] public float WhileRunning;      // only while it is
[Inline] public Vec3 Corner;                                         // three boxes, one row
[Wide] public int Seed;                                              // no name column at all

[Foldout("Advanced")] public float Bias;                             // folded away
[Foldout("Advanced/Debug", Open = false)] public bool Noisy;         // and a fold inside it

[Button("Save", Line = ButtonLine.Start, Weight = 2)] public void Save() { }
[Button("Load", Line = ButtonLine.End)]               public void Load() { }
```

A component with twenty fields is unreadable however well it is ordered, so `[Foldout]` puts the
rest away under a name. Consecutive fields naming the same fold share it, folds nest as deep as the
slashes go, and whether one is open is remembered per component rather than per entity, because
somebody who shut one meant it about the component. What a fold holds is set in one step, and a
fold inside it another, so a field's depth says which fold it is in.

Several things selected are edited together. The panel shows the last one picked, and a change
made in it reaches everything else in the selection carrying the same component. A field they
disagree about has its name dimmed, since the box beside it can only show one of their values.

A model picked in the assets panel is drawn beside the tiles by a camera of its own, framed by its
bounds, on a render layer nothing else is on. It is put away when no panel asks for it, since a
camera pointed at an image costs a pass a frame whether or not anybody is looking.

The mesh and the material an entity is drawn with are Bevy's own components holding typed handles,
so they have no schema and are drawn as a section of their own. It shows where each came from, says
"made here" for anything built in memory, and offers the files under the asset root that suit.

A schema also carries how to add the component, how to remove it, and any method the struct has that
takes nothing, so a panel offers those as buttons without naming a type. What the editor changes can
be taken back. `EditorHistory` records an operation only when it can be reversed exactly. A delete
writes what goes as a scene first, in memory, so undoing it puts the entities back under the
parents they had and points every field that named one of them at what came back. A node of a
placed model, and an entity drawn with a mesh or a material a scene cannot describe, are deleted
without a way back, and the console says so.

## The console

Everything written to the output and error streams is teed into `ConsoleLog`, a ring of leveled
lines that collapses repeats, so a console can show it without anything that writes a line knowing
a console exists. What can be typed into one is a static method with `[Command]` on it:

```csharp
[Command("select", "Selects the first entity with a name: select <name>")]
internal static string Select(string name) => $"selected {name}";
```

A generator finds them at compile time and a module initializer registers them, so nothing reflects
at runtime and a command survives trimming. Parameters are read from the words after the name and
may be strings, numbers or flags, and one the method gives a default can be left off, the last ones
first. A single string parameter takes the whole of what was typed after it. Returning a string
writes that line back, and anything a person can get wrong is answered with a sentence rather than
an exception. An enum's member is read by its name alone, so a number that names no member is
refused rather than handed to code that was never written for it.

`ConsoleCommands.Run(line)` is the whole of the runtime surface, so a game gets a console by
drawing one. The editor's is the tab along the bottom, which the key under Escape raises and puts
away, and everything it knows about the log and the commands it asks the library for.

## Diagnostics

Bevy keeps a store of named measures, each a history of numbers with a value smoothed over time and
an average over what it keeps. Bevy's own plugins measure into it where `Config.DiagnosticPlugins`
asks for them, `FrameTime` for the frame time, frames a second and the frame count, `EntityCount`
for the entities, `SystemInformation` for how much of the CPU and memory the machine and the
process use, and `Render` for each render pass's CPU and GPU time. `Log` prints every diagnostic
once a second to Bevy's log. A game's own measures go in the same store and are printed
beside Bevy's, a suffix after each number:

<!-- compiled with:
struct Enemy { }
-->
```csharp
app.Startup(ctx => Diagnostics.Register("game/enemies", " enemies"));
app.Update(ctx => Diagnostics.Measure("game/enemies", ctx.Ecs.EntitiesWith<Enemy>().Length));
```

A diagnostic is registered before it is measured, since the store drops a measurement of a path it
does not have. A path is names apart by slashes. `Diagnostics.TryRead` gives one's last, smoothed
and average values, and `Diagnostics.All` gives every one, which an overlay of the frame rate reads.
`Diagnostics.SetEnabled` stops one being measured and logged and keeps its history, and
`Diagnostics.SetLogFilter` has the log print only the paths it is given, or none for an empty list,
or all of them again for null. The system's measures are read on a task of Bevy's own no oftener
than the system can measure the CPU, so their first values come some frames after the app starts.
On macOS Bevy builds them for the App Store's sandbox, which reads no process, so the process's two
are zero there.
The `diagnostics` command lists the store as the log prints it.

## Bevy's dev tools

Bevy's dev tools are plugins its defaults leave out, which an app asks for with `Config.DevTools`.
The FPS overlay shows the frame rate at the window's top left over a graph of the frame times, and
is set through Bevy's reflected `FpsOverlayConfigRef` resource. The infinite grid is a ground plane
of lines fading with distance, drawn on an entity given `InfiniteGridRef` and set through its
`InfiniteGridSettingsRef`. Picking's debug overlay names what picking finds under each pointer
while `Picking.DebugMode` is normal or noisy, and logs picking's events at Bevy's debug and trace
levels, which `RUST_LOG=bevy_dev_tools=trace` lets through:

<!-- compiled with:
Config config = new();
-->
```csharp
config.DevTools = DevTools.FpsOverlay | DevTools.InfiniteGrid | DevTools.PickingDebug;

app.Startup(ctx =>
{
    if (ctx.Ecs.Resource<FpsOverlayConfigRef>() is { } overlay) overlay.TextColor = Color.FromSrgb(0f, 1f, 0f);
    ctx.Ecs.Insert<InfiniteGridRef>(ctx.Ecs.Spawn());
    Picking.DebugMode = PickingDebugMode.Normal;
});
```

The overlay adds Bevy's frame time diagnostics where the app has not, and how often its text
changes is set as the app is built, so the resource's interval changes nothing after.

## Controlling a running app

The same catalog is reachable from a terminal. `Config.Serve`, or `--serve`, or `BCS_SERVE` in the
environment, opens a socket on the loopback interface and writes a session file, and `bcs` finds it
and asks it things:

```bash
./bcs open --editor                    # start one, detached, and wait until it answers
./bcs open --editor --offscreen        # the same, on a machine with no display
./bcs open --example 3d_scene --frame-time 0.016666   # each frame a sixtieth to the game, however long it takes
./bcs open --example many_foxes --frame-time 0.016666 --hold loads   # that clock held while the foxes load
./bcs list                             # every command that app offers, with its parameters
./bcs command entity.set Cube Transform.Translation 0,2.5,0
./bcs command entity.set Cube Transform.Rotation -30,45,0    # degrees, as the inspector shows them
./bcs command entity.set Path Route.Speeds '1;2.5;4'         # a list, its items split by semicolons
./bcs command input.click 1450 700
./bcs command input.hold W,D 12        # held for twelve frames exactly, for a game walking while they are
./bcs command input.drag 400 300 120 0 10   # pressed, moved 12 pixels a frame for ten frames, released
./bcs command input.drop 'levels/yard.scene.json;notes.txt'   # files dropped on the window, for a game to read
./bcs command frames.wait 5            # five frames of the clock, those a hold keeps still left out
./bcs command app.hold commands        # the set clock moving only while a command waits
./bcs command pipelines.wait           # until every pipeline asked for has compiled
./bcs command frame.profile 240        # what a frame spends, split as .github/PERFORMANCE.md describes
./bcs command diagnostics              # Bevy's diagnostics store, as its log prints it
./bcs command state.set Mode Over      # a state moved by its enum's name, at its next transition
./bcs command memory.collect           # what it holds, after a full compacting collection
./bcs shot /tmp/after.png              # captures the window, and waits for the file
./bcs shot /tmp/settled.png 300        # in the 300th frame of the app's clock, the same moment on any device on a set clock
./bcs command window.list              # the windows past the first a game spawned, by #index
./bcs command window.shot '#402 /tmp/second.png'   # one of them, offscreen as well
```

`memory` answers with what the program holds as name and number pairs, the managed heap's bytes its
last full collection kept alive as `live` and its size after that collection as `heap`, which after
`memory.collect`, a compacting collection, is what is alive and the space pinned objects hold apart,
the memory the process holds of its own, which leaves out the files it maps and the memory it shares
with a graphics driver, and the most it has held, the bytes the bridge has allocated, the entities
with the indices handed out, the asset handles held and the assets of each kind Bevy holds, and
where a renderer runs, the GPU memory wgpu has allocated and how many buffers, textures, pipelines
and other GPU resources are alive, as `gpu.` pairs. Read at intervals while a game is played, as
`build/soak.sh` does, a value that keeps climbing is a leak, and a process figure that climbs while
every `gpu.` count stands still is memory the graphics driver keeps for itself.

Each of those is answered inside the next frame of the app that is already running, which is the
point, because a fresh process per question costs a second of startup, a new world, and a guess
about which frame to look at. What arrives over the socket is queued and run by a system at the top
of the frame, because everything ECS-touching is ambient on the world Bevy lends the running
system. The socket thread never touches an entity.

A command whose answer is not ready on the frame it runs, such as `shader.buffer`, which reads a
buffer back from the GPU, calls `ConsoleHost.Later` with a question to ask each frame. The command
line holds the call until the question answers, and a console prints the answer as a line of its
own once it arrives.

Every verb writes one envelope to the standard output stream under `--json`, whether it worked or
not, with a stable token in `errors[0].code` and an exit code that separates *it failed* from
*nothing was there to ask*:

```json
{ "success": true, "command": "command", "data": { "result": "…", "frame": 962 },
  "errors": [], "warnings": [] }
```

`bcs` also wraps the cold paths, in the order this repository needs them. `bcs build` builds the
bridge and then the managed side, `bcs test` runs the suite through `build/test.py`, as CI does,
and exits 8 when tests fail and 6 when the run never reached a verdict, a test host lost to a crash
or to its memory cap among them, and `bcs doctor` answers why nothing is starting. `bcs help` lists
the rest.

Nothing about this is privileged. The plugin ships in the library and is off unless asked for, so a
game built on BevyCSharp is drivable exactly the way the editor is. The editor additionally
registers `eval`, which compiles a fragment of C# and runs it against the live world through the
same script host that reloads behavior scripts.

## Debug keys

F1 hides the program's interface as Minecraft hides its HUD, what is drawn inside the hidden scope
and nothing outside it, so a pause menu or an inventory drawn outside the scope stays whatever F1
says, and the debug overlay stays too. `Ui.BeginHidden()` opens the scope and answers whether it
draws this frame, false while hidden, so a program's own ImGui windows go inside an `if` on it as
they go inside one on `ImGui.Begin`, and `Ui.EndHidden()` closes it. The interface nodes made
inside the scope with `Ui.SpawnNode` and `Ui.SpawnText`, and what is made under them, are hidden by
the engine and given back the visibility they had:

<!-- compiled with:
using ImGuiNET;
-->
```csharp
Ui.BeginHidden();                                     // a HUD made as the game starts
var health = Ui.SpawnText("health 100", new UiSettings { Absolute = true, Left = Length.Px(16f), Top = Length.Px(16f) });
Ui.EndHidden();

if (Ui.BeginHidden())                                 // an ImGui window each frame
{
    ImGui.Begin("hud");
    ImGui.TextUnformatted("health 100");
    ImGui.End();
}
Ui.EndHidden();
```

`Ui.Hidden` says whether the scope is hidden and sets it, and `Ui.HiddenKey` moves the key, or with
`null` turns it off.

F3 shows the engine's debug overlay as the newest Minecraft's shows, the engine's own text in two
columns, a line on a dark half-clear backing, with no window, over the program's interface and
outside the hidden scope. The left column says the engine's version, its bridge's profile and ABI,
the frames a second and a frame's time, the camera's place, facing and field of view, and the
entities, meshes, materials, images and sounds. The right says .NET and the architecture, the OS
and its kernel, the CPU and its cores, the memory in use of what is installed and the managed heap,
the GPU, its driver and its memory, the display, the meshes in view and their triangles, the
lights by kind and those casting shadow maps, and the lighting. It shows and hides as F3 is let go
where no other key was pressed while it was held, so F3 held with another key, as the feature
test's F3 and F4, is the program's. `Debug.OverlayShown` shows it from code and `Debug.OverlayKey`
moves the key, or with `null` turns it off.

## Screenshots

F2 saves a picture of the frame being drawn into a folder named `screenshots` beside the program,
made on first use, named by the local time as Minecraft names its own, `2026-10-21_03.45.12.png`,
the second taken in one second `2026-10-21_03.45.12_2.png` and the third `_3`, and the log says
where it went. The press is left for the program to read as well. `Capture.ScreenshotKey` moves it to
another key, or with `null` turns it off, for a game that uses F2 for something of its own,
`Capture.ScreenshotFolder` puts the pictures elsewhere, and `Capture.Screenshot()` takes one from
code. A run that draws nothing, headless, takes none. The engine holds no other key of its own on
F2.

## Animated captures

`record` takes pictures of what the window shows at a step of the app's clock and writes them as
one animated file, a GIF or an animated PNG by the path's extension, for a page of the guide or a
devlog:

```bash
./bcs command record /tmp/walk.gif 5s every 250ms             # twenty pictures a quarter second apart
./bcs command record /tmp/turn.png 8 every 1s scale 0.5       # eight a second apart, half as wide and high
./bcs command record /tmp/run.gif 120f every 15f play 100ms   # every fifteenth of 120 frames, played faster
./bcs command record.stop                                     # ends one sooner, with what it took
./bcs record /tmp/walk.gif 5s --every 250ms                   # the first, waiting until the file is written
```

The length is seconds, frames of the clock or a count of pictures, and the step after `every` is
milliseconds, seconds or frames, a tenth of a second unless given. Each picture is of the frame
being drawn when it falls due and is kept in memory, and the file is written once the length is
reached and the last picture is back off the GPU, under its own name only when it is whole. Each
frame is shown for the time between it and the next by the clock, so a step of a second plays at a
second, and `play` shows every frame for one time in its place. On a set clock
(`bcs open --frame-time`) the pictures are the same moments of the game on any device.

A GIF holds 256 colors, one palette cut for the whole recording by median cut, and a pixel between
two of them is dithered by a pattern that holds still where the picture does, so a part that does
not move costs nothing after the first frame. It counts time in hundredths of a second, and browsers
play a delay under two hundredths slower. An animated PNG keeps every color as drawn and every
millisecond, in a larger file. The engine writes both itself, and browsers and GitHub's pages play
both.

`./bcs record` sends `record` and asks `record.status` until the app says the file is written,
however long the recording takes by the machine's clock, and answers with the path or with why there
is none.

Captures on disk, those the pack run takes or `./bcs shot` wrote, are made into one
animation by `build/animate.py`, a GIF, an animated PNG or an animated WebP by its extension, each
picture shown for the delay and fitted to the first's size:

```bash
build/animate.py /tmp/turn.gif /tmp/turn-1.png /tmp/turn-2.png /tmp/turn-3.png --delay 1000
build/animate.py /tmp/turn.webp /tmp/turn-*.png --delay 250 --scale 0.5 --loop 3
```

Pillow writes it where it is installed. Where it is not, the script runs `./bcs animate`, which
writes a GIF or an animated PNG from PNG pictures with the writers a recording uses, and a WebP,
read or written, needs Pillow.

A program records the same way, until it stops, and the task each call returns finishes with the
file's path once it is written:

```csharp
var recording = Capture.StartRecording("captures/jump.gif", intervalMs: 250, scale: 0.5f);   // as the jump starts

// As it lands, a frame or more later, which writes the file once the last pictures are back.
_ = Capture.StopRecording();
```

---

Before this, [Making a game](making-a-game.md).
Next, [How it works](how-it-works.md).
Its calls are each a line in the [cheatsheet](../CHEATSHEET.md#the-tools). The [guide's contents](../README.md#guide) list every page.
