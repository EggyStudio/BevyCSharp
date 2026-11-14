# Running a game

Three ways to run the same behaviors, and one way to change them without stopping.

## In a window, headless, or offscreen

The feature test has a switch at the top of `Program.cs`:

```csharp
const bool RunInWindow = false;
const GraphicsBackend Backend = GraphicsBackend.Vulkan;
```

or from the command line, which wins over the constants:

```bash
build/build-native.sh --render                  # once: build a bridge with the renderer

dotnet run --project games/FeatureTest          # every feature on one map
dotnet run --project games/FeatureTest -- --backend vulkan
dotnet run --project games/FeatureTest -- --headless --frames 120
dotnet run --project games/FeatureTest -- --offscreen --frames 120
```

The feature test opens a window by default on a hub with a signpost to each zone of its map, and a
lit cube turning in the middle. F4 opens its admin panel, a list steered by the arrows or a pad,
with pages for the graphics, the audio, the weather, the controls, the debug draws, teleports to the
zones, things to spawn and the time, each setting kept in the player's folder for the next run. A
day passes over the map, an hour a minute unless the Time page sets another speed, the hour or the
latitude, the sun and a full moon carried across Bevy's atmosphere as two directional lights whose
strength and color follow the hour, the scene lit from the sky as it changes and the stars out at
night, and `./bcs command day.hour 22` sets the hour from a terminal. The engine's weather runs
over it where meshlets do not, clouds, fog, rain, snow and thunder from the kind of weather its page
sets, or from a forecast by the day's clock, as finely as the page's quality asks or not at all,
and `./bcs command sky.weather Storm now` sets it from
a terminal. F3
shows an overlay of the frame time, the view's place and facing, the entities and what the program
holds, and the key under Escape opens the console. The player is a capsule on a
`CharacterController`, walked with WASD or a pad's stick, jumping with Space, sprinting with Shift
and crouching with C, its view turned by the mouse while Tab locks the cursor. F3 held with F4 steps
it through walking, creative mode, where a double tap of jump flies, and spectator mode, a free
camera through everything, and F5 steps the view from its eyes, from behind and from in front.
`./bcs command player` says where it is and how it moves, and `./bcs command mode creative` sets the
mode. East of the hub the course tries it on ramps and stairs, a beam, a tunnel to crawl through,
ice, a bounce pad, a pit, a moving platform, an elevator, a turning disc, a conveyor and gaps to
jump, beside a playground of crates, a door on a hinge, a lift, a rope and a pressure plate, and a
mound of terraces. West of the hub the render gallery holds spheres from a dielectric to a metal and
from polished to rough, with a row under a clearcoat and a brushed row, on a wall that parts its
court in two, and through an arch in that wall a Cornell box lit by a point light's shadow map, or
by Solari's rays from the next start once the graphics page's ray-traced lighting is on, where the
bridge and the GPU have them. North of the hub the light hall
has a bay each for point lights in red, green and blue, a spot light through a cookie, a light the
size of a panel with soft shadows, a light through slats into a fog volume, a reflection probe
captured once, an irradiance volume made in code, clustered decals and emission for the bloom. South
of the hub a meadow of the engine's grass, crossed cards on the leaves' material, and a ring of
trees sway in one wind. In the scenes zone north-east of the hub the
panel's Scenes page fetches a scene pack and loads it, Intel Sponza first, standing at the level's
`info_scene` point with its main door open for the player to walk in, its heaviest meshes drawn as meshlets where the graphics page's
meshlets run from the next start, which its page switches off and on saying the frame time of each,
the light hall's lamps then casting no shadows, since with meshlets each shadow view holds a few
hundred megabytes of the graphics driver's memory, and `./bcs scenes fetch intel-sponza` fetches one
from a terminal. The panel's effects page switches
the tonemapper and turns on ambient occlusion, screen-space reflections, a depth of field focused on
whatever the view rests on, motion blur, chromatic aberration, auto exposure, sharpening and a dusk
sky drawn in code, and `./bcs command setting AmbientOcclusion true` changes a setting as the panel
does. In spectator mode `./bcs command look 6 3 8 0 0 0` puts the camera at a point looking at
another. `build/drive-feature-test.sh` walks the player through each station through `bcs` and
captures each zone, as the pack workflow does, without the weather there (`WEATHER_QUALITY=Off`),
which its software device spent nearly a third of each frame on.

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
be captured where there is no screen to draw it on. An offscreen run keeps a frame behind the GPU,
as a window does, and one whose GPU has not finished a frame after a minute says which frame and
exits with code 87, rather than waiting forever on a device that has stopped.

A run with no window, headless or offscreen, makes no sound, since it is a test, a soak, a capture
or a tool and whoever sits beside the machine did not ask to hear it. Its sounds still run their
course on the game's clock, so one played to despawn at its end goes on time and a game waiting on
a sound works the same. `Config.AudioWithoutWindow` plays such a run to the machine's device
instead, and `Audio.IsSilent` says which a run does.

A game that opens a window draws offscreen instead when `BCS_OFFSCREEN` is set in its environment,
as `BCS_SERVE` makes any app answer `bcs`, so a tool can run a game it did not write with nothing
on the screen. The editor's Play sets it when the editor itself has no window, and
`games/Courtyard/play.sh`, `games/Swarm/play.sh` and `games/Blockout/play.sh` play their games
that way.

A run that has to be the same on every machine, a test of something that moves or a capture of it,
sets `Config.FrameSeconds`, and every frame is then that many seconds to the game, to
`Time.DeltaSeconds` and to the fixed steps, however long the machine took over it. It is Bevy's
`TimeUpdateStrategy::ManualDuration`. `BCS_FRAME_TIME` sets it from outside where the config left it
at zero, `bcs open --frame-time` puts that in the environment of the app it starts, and
`Time.FrameSeconds` or the `app.frametime` command changes it while the game runs, zero letting the
machine's clock run again from where the set one had reached.

A set clock can be held still as well, so a frame counted on it is the same moment of the game on
a device that spent more frames loading or waiting. `BCS_HOLD_CLOCK` names what holds it, `loads`
while an asset the game asked for, or one that asset depends on, is loading, and `commands` between
the commands of a session served to `bcs`, so it moves only while one waits, both named with a
comma between. A held frame runs its systems, draws and reads its input as any other, and passes no
time to the game or its fixed steps. A fox whose run starts once its model has loaded then starts
it at the same moment of the clock on a device that loads in fewer frames, and a script's next
command finds the game where its last wait left it. `bcs open --hold` puts that in the environment
of the app it starts, and the `app.hold` command changes it while the game runs. The machine's
clock is never held, since it reads the time since the last frame it counted and would hand the
frame after a hold the whole of it.

The camera is steered the way an editor's scene view is, so the scene can be looked at from
anywhere while trying something out:

| held | does |
|---|---|
| right button | look around, with W, A, S, D to fly, Q and E for down and up, Shift for faster and Control for slower, and the wheel to set the speed |
| middle button | slide the view sideways and up |
| wheel | move along the view direction |
| Alt and left button | swing around a point in front of the camera |
| F | frame the origin from wherever the camera is looking |

`games/FeatureTest/Behaviors/FlyCamera.cs` is the whole of it, and it is an ordinary behavior. It
keeps its own yaw, pitch and speed as component fields, reads `ctx.Input`, and writes Bevy's
`Transform`.

Both modes run the identical behavior scripts. Nothing branches on whether a renderer exists;
the engine decides that, from `Config`.

`Config.Backend` pins the graphics API. `Automatic` already prefers Vulkan on Linux and Windows,
so naming it is about making the choice explicit and failing loudly rather than falling back
silently. `App.DescribeAdapter()` reports what you actually got, which is how you check:

```
[Renderer] adapter: Vulkan | <the GPU's name> | DiscreteGpu | <its vendor>
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
Bevy's `AppExit::Error` is answered. That is Bevy's app with its error handler set to warn, as
Bevy's error_handling sets it, each kind of exception a system throws logged the first time and
again at each power of ten, and `Config.FailFastOnSystemException` ends the run at the first, as
Bevy's default handler panics. A system that deals with its own error catches it.

An error the GPU reports ends the run too, as Bevy's does, unless the game says otherwise:

```csharp
Render.ErrorPolicy = RenderErrorPolicy.Recover;    // a lost device is made again
Render.CauseError(RenderErrorCause.DeviceLost);    // and here is one, to see that it is
```

`Ignore` draws on as if nothing happened, `StopRendering` keeps the game's systems running with
drawing stopped, and `Recover` makes the renderer again, a device and all, which is how a game
outlives a driver update or a GPU reset. Bevy asks again every frame while drawing is stopped, so a
policy set then takes over, and `Render.ErrorCount` and `Shaders.LastRenderError` say what
arrived. An error left where it was may come back every frame under `Ignore` or `Recover`, which can
make the picture flash, so a game choosing either takes away what caused it. `Render.CauseError`
causes each of Bevy's four kinds for a game to try its policy on, and two of them, running out of
memory and a shader that never ends, reach the machine's own graphics, which may need a restart, as
Bevy warns of its own.

Bevy 0.20 makes the renderer again without the lighting it had, so a mesh Bevy lights draws black
after a recovery, where an unlit one draws as before. A device lost while Bevy's GPU clustering waits
on a readback ends the run in Bevy's own code, which a test met in four runs of six, so a lost device
is survived only some of the time. `Recover` is the way to keep a game running long enough to save
and say so, rather than to carry on as before.

A game on a player's machine has no console anybody reads, so the run writes what it says to
`logs/latest.log` beside the executable, C#'s lines and Bevy's, with the last five runs' logs kept
beside it. An exception nothing caught, a task's exception nobody looked at, or a panic inside
Bevy on one of its own threads is written to `logs/crash-<time>.txt` there, with the last
200 lines of the log, the system, .NET, the bridge's version, the graphics adapter and the backend,
the things a report from somebody else's machine is read for. The next run says where the last
crash's file is as it starts, and `CrashLog.LastCrash` holds it for a game that offers to open it.
`Config.Logs` names another folder, or an empty string for none.

A panic inside Bevy's side is written with its stack twice. The bridge a package carries holds no
names for its functions, so the stack std writes names few of its frames, and after it, under
`frames:`, the panic writes the bridge's build id and each frame as an offset into the bridge, which
name the frames from the full library kept for that build. To name them:

1. Read the package's version on the crash file's `Bridge` line, the version the game ran on.
2. Download the symbols of that version. Its release on the repository's Releases page, from
   v0.6.114 on, carries them as a zip for each system, `bridge-symbols-<version>-<rid>.zip`, to be
   unpacked into a folder, and the pack run that made it carries the six as the artifact
   `bridge-symbols-<version>` on the run's page under the repository's Actions, which GitHub keeps
   ninety days from the run.
3. From a checkout, run `build/name-crash.py <crash file> <the folder>`. It finds the library whose
   build id the crash file names, by the `.build-id` file beside each, and writes the crash file
   again with the function each frame lies in.

A Windows bridge's frames keep their offsets, the build id naming the PDB beside the library in the
zip or the artifact, which the script does not read.

A run that might take the machine's memory with it, a test, a scene being tried or a tool
controlling a game, is given a cap in gigabytes by `Config.MemoryCap` or `BCS_MEMORY_CAP_GB`. A
thread of its own reads how much memory the process holds four times a second, and past the cap the
run says so on the console and in a crash file and exits with code 86, where a machine that ran out
would have ended whatever process the system chose, the terminal or the editor among them. A game
has no cap unless it asks for one. `bcs`, `build/drive-feature-test.sh` and the test suite give
every app they start eight gigabytes, or a quarter of the machine's memory where that is less.

## The live path

A running game is edited and seen without a restart where it watches its asset directory. A file
saved on disk is read again in the running app and said in the log, and the `live.status` command
lists the latest. A game's compiled C# is changed as it runs where `dotnet watch` runs it, as the
last part of this page says. The bridge's own code, the Rust under `native/`, is a rebuild and a
restart.

### What decides

`Config.WatchAssets` decides. A program that sets it decides either way. Where it leaves it alone,
the environment's `BCS_WATCH_ASSETS` decides, `1` watching and `0` not in any letter case, which
`./bcs open` sets to `1` and to `0` with `--no-watch`, as a capture or a timing read asks. Failing
both, a game built in Debug watches and one built in Release does not, so a shipped game starts no
watcher. An app the environment or the Debug default turned watching on for says so in the log as it
starts, naming the folder it watches, and `live.status` says what decided. The app decides as it is
made, so `Config.WatchAssets` is set on the config handed to it, and set after, it throws.

The Debug default reads the project's folder. A game built in Debug whose config names no
`AssetRoot` reads and watches its project's `assets` folder, where a file is saved, rather than the
copy its build puts beside it, and caches its compiled shaders under the project's `obj` folder, so
the assets folder holds only what was put there. Its first lines name both folders. A Debug build is
not what is shipped, and a published game reads the copy beside it. A program that reaches the
package through a library of its own is given no Debug default, and sets `WatchAssets` or
`BCS_WATCH_ASSETS` to watch. A folder the system will not watch, its limit on watchers met, is said
in the log as a warning and read without watching, the app starting all the same.

### What is read again

Bevy's assets, a texture, a model, a sound or a font, are read again through Bevy's watcher, which a
bridge with the renderer carries, and each is posted as an `AssetReloaded` message naming it. A
handle keeps pointing at the same asset, so whatever draws with it shows the new version, and the
message is for what worked something out from the old one, a size read from an image or a collider
fitted to a mesh. A glTF's scene is spawned again under the same root and posted as
`WorldInstanceReady` with `Reloaded` set. An asset whose dependency failed to load, a glTF naming a
missing texture, is read again without being posted until its dependencies load. Bevy's watcher
waits for a file to sit still a third of a second, so a file written over longer than that is read
before it is whole and again after, and a file replaced by renaming can be read and posted twice for
one save.

Shaders reload in every profile, watcher or not, including a Slang shader when a file it imports
changes, each said in the log as the bridge compiles it again, and since the bridge compiles them
they are not posted or listed. What happens when one fails is under [Drawing](drawing.md). Material
files, data assets and scene files are read by the engine itself and reload on any bridge, each
posted as `AssetReloaded` of the kinds `MaterialFile`, `DataAsset` and `SceneFile`. A scene file
placed in a level is spawned again under each entity placing it, with the level's overrides applied
again ([Scenes and saves](scenes-and-saves.md)).

The scene a game loads at the top level, `SceneFile.Load` with no parent, is spawned again in place
when its file's bytes change. What that load spawned is despawned and the file's entities are read
again, the world's resources left as the game has them. What the program spawned itself stays, and
an entity it put under one of the scene's goes back under the entity holding the same id in the
file. The app's own save is no change, nor is another hand writing the same bytes again, so a scene
saved from a running game is not spawned over itself. A saved game laid over the scene is not laid
again, since loading the save is the game's to do, and `scene.load` loads a file by hand. The editor
keeps the scene it has open as it is, since it is the document being edited, and says in the log
that its file changed.

### Behavior scripts

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
are not kept in the process's lists for apps made after. A script is compiled against every assembly
the app has loaded and every one the library's members name, so it calls whatever a game's compiled
code calls, `Render.SetWeather` with the sky's `WeatherState` among them. Each assembly a script is
compiled against is read once for the process and shared by every compilation, so a script saved
again and again costs a compilation and not another copy of the runtime. A `[Command]` in a script
is not registered, since commands are written into a game's assembly as it is built.

A script that does not compile changes nothing. The errors are said in the log as a warning and the
running generation stays, as it does for a script that cannot be read or a generation whose
registration throws. Deleting a script stops what it ran, the last one too. The compiler lives in
`BevyCSharp.Scripting`, which the package leaves out, because a game should not carry one in order
to run. The editor and the Player add its `ScriptsPlugin`, and a game that references the project
adds it the same way, with `app.AddPlugin(new ScriptsPlugin())`. It compiles the asset folder's
`scripts` as the app is built and again when a script is saved where the app watches its assets,
says each build in the log, posts each after the first as `AssetReloaded` of the kind `Scripts`, and
answers `scripts.reload`, which builds again on request, and `scripts.status`, which says what runs
and how the last build went.

### A game's code under dotnet watch

A game run with `dotnet watch run` takes an edit to its own C# as it runs, through .NET's hot
reload, and the app has the edit reach its schedule. A method's new body runs from its next call, a
behavior's among them. A method a behavior gains is a system the generator writes into the
assembly's registration, and the app runs the registration again at the start of the next frame, so
an `[OnUpdate]` method added runs from that frame, one taken out stops, and one moved to another
stage runs there in its place. The log says what each change added and took out.

A system the registration names again keeps its place in the schedule, with the order and the
condition it was first given, so an edit to a method's `[After]`, `[Before]`, `[RunIf]`, `[InState]`
or `[ToggleKey]` is seen at the next start. So is an `[OnStartup]` method added, since the app
started before it, which the log names, and a file added while the game runs, which `dotnet watch`
compiles at the next start. A field added to a behavior is an edit hot reload cannot make to a
running program, a struct's layout being fixed, and `dotnet watch` says a restart is needed, which
run with `--non-interactive` it makes at once.

An app follows changes only where its process can take one, as a program `dotnet watch` runs can,
and an app run on its own, a shipped game among them, has nothing of it in its frames.

---

Before this, [Input](input.md).
Next, [Making a game](making-a-game.md).
Bevy's own examples of this, and which are written in C# here, are in
[EXAMPLES.md](../.github/EXAMPLES.md#application). Its calls are each a line in the [cheatsheet](../CHEATSHEET.md#running-an-app). The [guide's contents](../README.md#guide) list every page.
