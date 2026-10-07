# Making a game

[`games/Courtyard`](../games/Courtyard) is a small game made the way one outside this repository is
made, as a project of its own on the package, with a level built in the editor, behaviors written
as scripts, and an export that runs from its own folder. The runner is walked around a courtyard
with WASD to pick up three coins and bring them to the goal, with Escape to pause, F5 to save and
F9 to load. Continuous integration plays it from its menu to its win with no display.

## The project

```
games/Courtyard/
  Courtyard.csproj            the package, and nothing of this repository
  nuget.config                build/package ahead of nuget.org
  Program.cs                  a window, physics, and the level loaded at startup
  build-level.sh              the level, built through bcs in a running editor
  play.sh                     the game, played through bcs from its menu to its win
  assets/
    project.json              the startup scene
    levels/courtyard.scene.json
    scripts/Courtyard.cs      every behavior, compiled into the game and loaded by the editor
    models/  sounds/
```

The package compiles `assets/scripts` into the game and copies `assets` beside it, so what a player
runs is one assembly and its files, and the editor reads the same scripts as the project's. The
program is a window, physics, and a behavior that starts the game from the level `project.json`
names:

<!-- not compiled: the game's own Program.cs, which the pack workflow builds with the game -->
```csharp
var config = Config.Windowed("Courtyard", 1280, 720);
config.GameName = "Courtyard";
return BevyApp.Run(app => app.AddPlugin(new PhysicsPlugin()), config);

[Behavior]
public partial struct Boot
{
    [OnStartup]
    public static void Start(BehaviorContext ctx) =>
        SaveGame.Start(ctx.Ecs, ctx.Res<ProjectSettings>().StartupScene!);
}
```

The level is loaded by the program rather than by a script, since the editor runs a project's
scripts while the level is edited and a level loading itself there would be a second one on top of
the first. The game's camera is the level's, which the editor holds off while the level is edited
and the game sees through from its first frame.

## The level

```bash
./bcs open --editor --offscreen -- --project games/Courtyard
games/Courtyard/build-level.sh
```

`build-level.sh` builds the level as a person would, one command at a time, so it can be built
again. Cubes are renamed and colored as walls and spheres as coins, the runner's model is placed as
an instance, a camera is placed behind it, the game's components are put on each, and the scene is
saved and named the startup scene.

```bash
c() { ./bcs command "$@" >/dev/null; }

c do Spawn/Cube
c entity.rename Cube "North wall"
c material.set "North wall" color "#8a7f72"
c entity.add "North wall" RigidBody
c entity.set "North wall" RigidBody.Kind Static
c entity.add "North wall" Collider
c scene.place models/runner.gltf
c world.save levels/courtyard.scene.json
c setting "Project/Startup scene" levels/courtyard.scene.json
```

Every body is the level's own, a `RigidBody` and a `Collider` fitted to what each entity is drawn
with. The walls are static boxes and the ground a static mesh of its own triangles, and each coin
and the goal is a sensor, which reports a touch and stops nothing. `Coin` and `Goal` are markers
the runner tells them apart by.

## The behaviors

Everything the game does is in [`Courtyard.cs`](../games/Courtyard/assets/scripts/Courtyard.cs). The
menu, play and the win are a state declared on its enum, and the pause a sub-state inside play.
The runner is made a character as play starts, a capsule its `Walk` steers by setting the
`CharacterController`'s `Move` while it turns the model to face the way it walks, which the walls
stop and the ground holds. It plays its walk while it moves, and reads `ContactStarted` for the
coins it touches, each
taken away with a sound and counted in a `[Persist]` wallet, and for the goal once none is left.
The HUD, the menu, the pause menu and the win are `Ui` nodes despawned as their state is left, and
F5 and F9 save and load through `SaveGame`.

A level a load brings back has its bodies made by the plugin as the first one had, the runner is
made a character again where it reads `SaveLoaded`, and the pause holds the simulation with
`PhysicsWorld.Paused`. The HUD's count is found by a `CoinsText` component rather
than kept in a static field, so it is found again after the script is reloaded.

## Playing it

```bash
dotnet build games/Courtyard && games/Courtyard/bin/Debug/net10.0/Courtyard
games/Courtyard/play.sh                      # from the menu to the win, with no display
```

In the editor, Play (or `play.scene`) plays the level through `BevyCSharp.Player`, and a script
saved while it plays is compiled again and swapped in with the runner, its coins and the HUD where
they were. Exporting with the assets in a pack (`project.export linux-x64 pack`) writes a folder
holding the game and `assets.pack`, and `play.sh` takes that folder to play the export instead.

---

Before this, [Running a game](running-a-game.md).
Next, [The tools](tools.md).
The [guide's contents](../README.md#guide) list every page.
