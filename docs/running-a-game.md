# Running a game

Three ways to run the same behaviors, and one way to change them without stopping.

## In a window, headless, or offscreen

The sample has a switch at the top of `Program.cs`:

```csharp
const bool RunInWindow = false;
const GraphicsBackend Backend = GraphicsBackend.Vulkan;
```

or from the command line, which wins over the constants:

```bash
build/build-native.sh --render                  # once: build a bridge with the renderer

dotnet run --project BevyCSharp.Sample          # a rotating cube
dotnet run --project BevyCSharp.Sample -- --backend vulkan
dotnet run --project BevyCSharp.Sample -- --headless --frames 120
dotnet run --project BevyCSharp.Sample -- --offscreen --frames 120
```

The sample opens a window by default and draws a lit cube turning in place. Escape closes it.

There are three ways to run the same behaviors, and `Config` chooses between them. A window is the
usual one. `Headless` installs no renderer, for a test or a dedicated server. `Offscreen` installs
the renderer and draws into an image instead of onto a screen, and is the only one of the three that
produces a picture on a machine with no display server:

```csharp
var config = Config.OffscreenFor(1280, 720, frames: 120);
```

`Width` and `Height` size the image the way they would size the window, `Render.Screenshot` writes
it to a PNG, and `HeadlessFps` and `HeadlessFrames` pace and bound the run, because a run with no
window has no window to close. The editor takes `--offscreen` as well, so the interface itself can
be captured where there is no screen to draw it on.

A game that opens a window draws offscreen instead when `BCS_OFFSCREEN` is set in its environment,
as `BCS_SERVE` makes any app answer `bcs`, so a tool can run a game it did not write with nothing
on the screen. The editor's Play sets it when the editor itself has no window, and
`games/Courtyard/play.sh` and `games/Swarm/play.sh` play their games that way.

A run that has to be the same on every machine, a test of something that moves or a capture of it,
sets `Config.FrameSeconds`, and every frame is then that many seconds to the game, to
`Time.DeltaSeconds` and to the fixed steps, however long the machine took over it. It is Bevy's
`TimeUpdateStrategy::ManualDuration`. `BCS_FRAME_TIME` sets it from outside where the config left it
at zero, `bcs open --frame-time` puts that in the environment of the app it starts, and
`Time.FrameSeconds` or the `app.frametime` command changes it while the game runs, zero letting the
machine's clock run again from where the set one had reached.

The camera is steered the way an editor's scene view is, so the scene can be looked at from
anywhere while trying something out:

| held | does |
|---|---|
| right button | look around, with W, A, S, D to fly, Q and E for down and up, Shift for faster and Control for slower, and the wheel to set the speed |
| middle button | slide the view sideways and up |
| wheel | move along the view direction |
| Alt and left button | swing around a point in front of the camera |
| F | frame the origin from wherever the camera is looking |

`BevyCSharp.Sample/Behaviors/FlyCamera.cs` is the whole of it, and it is an ordinary behavior. It
keeps its own yaw, pitch and speed as component fields, reads `ctx.Input`, and writes Bevy's
`Transform`.

Both modes run the identical behavior scripts. Nothing branches on whether a renderer exists;
the engine decides that, from `Config`.

`Config.Backend` pins the graphics API. `Automatic` already prefers Vulkan on Linux and Windows,
so naming it is about making the choice explicit and failing loudly rather than falling back
silently. `App.DescribeAdapter()` reports what you actually got, which is how you check:

```
[Renderer] adapter: Vulkan | NVIDIA GeForce RTX 4070 Laptop GPU | DiscreteGpu | NVIDIA
[Renderer]  245.7 fps   frame    840   spinners 3
```

Ask for a backend the machine has no driver for and startup fails with a message saying so,
rather than quietly picking something else.

Cameras, lights, meshes and materials are reachable from C# through `Render`, which draws the scene
in the screenshot above. See [Drawing](drawing.md) for the calls.

## Bevy's log, and a run that fails

`Log` writes into Bevy's log at its five levels, beside the lines Bevy's own systems write, under
the target `csharp`. Bevy's filter shows the info level and louder by default, and `RUST_LOG` chooses
otherwise, `RUST_LOG=csharp=debug` showing a game's debug lines without Bevy's:

<!-- compiled with:
private static object Build() => new();
object? table = null;
-->
```csharp
Log.Info("helpful information that is worth printing by default");
Log.Debug("helpful for debugging");                 // left out by default
Log.WarnOnce("some warning we wish to call out only once");
Log.Once(() => table = Build());                    // once in the process, from a system run each frame
```

A `Once` form writes the first time its line of code runs and never again, as Bevy's `info_once!`
does, so a loop writes its first pass alone. A line at the error level is one of the engine's
errors, as Bevy's own are.

A system that throws is logged and the app runs on, where a Rust system's panic ends Bevy's app. A
game that cannot go on ends the run with a code instead, `ctx.Exit(1)`, which `App.Run` answers, as
Bevy's `AppExit::Error` is answered.

A game on a player's machine has no console anybody reads, so the run writes what it says to
`logs/latest.log` beside the executable, C#'s lines and Bevy's, with the last five runs' logs kept
beside it. An exception nothing caught, a task's exception nobody looked at, or a panic inside
Bevy on one of its own threads is written to `logs/crash-<time>.txt` there, with the last
200 lines of the log, the system, .NET, the bridge's version, the graphics adapter and the backend,
the things a report from somebody else's machine is read for. The next run says where the last
crash's file is as it starts, and `CrashLog.LastCrash` holds it for a game that offers to open it.
`Config.Logs` names another folder, or an empty string for none.

## Hot reload

The editor profile watches the asset directory, so a running app picks up what changed on disk.
`Config.WatchAssets` turns it on.

Assets reload, so a texture, a mesh or a font changed on disk is picked up by the running app.
Shaders reload in every profile, watcher or not, including a Slang shader when a file it imports
changes. What happens when one fails is under [Drawing](drawing.md).

Behavior scripts reload too. A script is an ordinary `[Behavior]` struct in a `.cs` file that is
compiled while the app runs, with the same source generator the compiled projects use, so what it
gets is the same runner and the same scheduling:

```csharp
[Behavior]
public partial struct Spin
{
    public float Speed;
    public float Angle;

    [OnUpdate]
    public void Tick(BehaviorContext ctx, ref Transform transform)
    {
        Angle += Speed * ctx.Time.Delta;
        transform.Rotation = Quat.FromRotationY(Angle);
    }
}
```

Two pieces in this library make that possible, and neither involves a compiler.
`App.EnableDynamicSystems` puts a dispatcher in each stage before the loop starts, because a
schedule cannot be added to once Bevy owns it, and that is where a system compiled later goes.
`App.RemoveSystemsBySource` retires the generation being replaced, which is why each one
registers under a tag of its own. A generation's `[OnStartup]` runs when it arrives rather than
when the app began, so a reloaded script spawns what it needs and clears out what the last one
left.

A script compiled again is a new assembly, so its types are new types with the old names. The
entities carrying the last generation's components are given the new one's, each written the way a
scene writes it and read back into the new type, so a field kept keeps its value, a field added
starts at its default, and an entity or an asset a field refers to is carried as itself. A state is
known by its enum's name, so it stays the one the game was in. A static field starts over, being
the new type's, so what a script has to find again after a reload it finds by a component rather
than keeps in a field. A scene loaded before its scripts were compiled keeps their components as
the file had them, and they are put on their entities once the scripts are, which is how the editor
opens a level.

The generation replaced is unloaded, since nothing of the app or the process keeps it. Its systems
let go of what they ran, its messages and its resources are dropped, and its behaviors and states
are not kept in the process's lists for apps made after. Each assembly a script is compiled against
is read once for the process and shared by every compilation, so a script saved again and again
costs a compilation and not another copy of the runtime. A `[Command]` in a script is not
registered, since commands are written into a game's assembly as it is built.

A script that does not compile changes nothing. The errors are reported and the running
generation stays. The compiler itself lives in `BevyCSharp.Editor`, because a game should not
carry one in order to run.

---

Before this, [Input](input.md).
Next, [Making a game](making-a-game.md).
Bevy's own examples of this, and which are written in C# here, are in
[EXAMPLES.md](../.github/EXAMPLES.md#application). Its calls are each a line in the [cheatsheet](../CHEATSHEET.md#running-an-app). The [guide's contents](../README.md#guide) list every page.
