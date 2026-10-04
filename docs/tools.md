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
(`ProjectSettings`), beside the startup scene, the fixed step and the export's choices, which an
app reads as `app.Project` and takes its fixed step from. [.github/EDITOR.md](../.github/EDITOR.md) has the design language in full.

The Play tab runs the game in a window of its own and stops it again, builds it without running it,
exports it for a player and shows what each wrote. `F5` plays the scene being edited instead,
through `BevyCSharp.Player`, so a change made in the editor is seen without being saved or written
into code. [.github/PLAY.md](../.github/PLAY.md) has the plan past that.

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
internal static string Select(string name) { … }
```

A generator finds them at compile time and a module initializer registers them, so nothing reflects
at runtime and a command survives trimming. Parameters are read from the words after the name and
may be strings, numbers or flags; a single string parameter takes the whole of what was typed after
it. Returning a string writes that line back, and anything a person can get wrong is answered with
a sentence rather than an exception.

`ConsoleCommands.Run(line)` is the whole of the runtime surface, so a game gets a console by
drawing one. The editor's is the tab along the bottom, which the key under Escape raises and puts
away, and everything it knows about the log and the commands it asks the library for.

## Driving a running app

The same catalog is reachable from a terminal. `Config.Serve`, or `--serve`, or `BCS_SERVE` in the
environment, opens a socket on the loopback interface and writes a session file, and `bcs` finds it
and asks it things:

```bash
./bcs open --editor                    # start one, detached, and wait until it answers
./bcs open --editor --offscreen        # the same, on a machine with no display
./bcs list                             # every command that app offers, with its parameters
./bcs command entity.set Cube Transform.Translation 0,2.5,0
./bcs command entity.set Cube Transform.Rotation -30,45,0    # degrees, as the inspector shows them
./bcs command input.click 1450 700
./bcs command input.hold W,D 12        # held for twelve frames exactly, for a game walking while they are
./bcs command input.drag 400 300 120 0 10   # pressed, moved 12 pixels a frame for ten frames, released
./bcs command frames.wait 5
./bcs command frame.profile 240        # what a frame spends, split as .github/PERFORMANCE.md describes
./bcs shot /tmp/after.png              # captures the window, and waits for the file
```

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

`bcs` also wraps the cold paths, in the order this repository needs them: `bcs build` builds the
bridge and then the managed side, `bcs test` runs the suite and exits 8 when tests fail and 6 when
the run never reached a verdict, and `bcs doctor` answers why nothing is starting. `bcs help` lists
the rest.

Nothing about this is privileged. The plugin ships in the library and is off unless asked for, so a
game built on BevyCSharp is drivable exactly the way the editor is. The editor additionally
registers `eval`, which compiles a fragment of C# and runs it against the live world through the
same script host that reloads behavior scripts.

---

Before this, [Making a game](making-a-game.md).
Next, [How it works](how-it-works.md).
Its calls are each a line in the [cheatsheet](../CHEATSHEET.md#the-tools). The [guide's contents](../README.md#guide) list every page.
