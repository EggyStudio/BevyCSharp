# Scenes and saves

Data kept in files of its own, levels kept as scene files, models placed in them as instances, a
game saved over the scenes it started from, and a type changed without breaking the files written
with it.

## Data assets

Values many entities share, edited in one place and changed without recompiling, such as a
weapon's stats or a loot table. Unity calls this a ScriptableObject and Godot a Resource:

```csharp
[DataAsset]
public sealed class WeaponStats
{
    [Range(0, 200)] public float Damage = 10f;
    public string Title = "Sword";
    public List<string> Tags = [];
}

[Behavior]
public partial struct Armed
{
    public DataRef<WeaponStats> Weapon;

    [OnUpdate]
    public void Tick(BehaviorContext ctx)
    {
        if (Weapon.IsSet) Console.WriteLine(Weapon.Value.Damage);
    }
}

var sword = DataAssets.Create<WeaponStats>("weapons/sword.data.json");
```

A data asset lives on the managed side, so it holds strings, lists and dictionaries, which a
component cannot. Its file is JSON naming its type, and a `.uid` sidecar beside it holds an id. A
`DataRef` holds that id, so renaming or moving the file keeps every reference to it. It is
loaded once and shared, and the editor makes one (`Project/New data asset`), edits one when its
file is selected in the asset browser, and offers the files of the right type to a `DataRef` field.

## Scene files

```csharp
SceneFile.Save(ctx.Ecs, "assets://levels/one.scene.json");
var loaded = SceneFile.Load(ctx.Ecs, "assets://levels/one.scene.json");
```

A scene file holds every entity it is given, named or not, with its parent, its components and the
files its mesh and material came from, and loading spawns all of it. A field referring to another
entity in the scene is written as that entity's id in the file and read back as whichever entity
the id spawned as, and each entity keeps its id across saves, so a scene under version control
diffs as what changed. Bevy's own components, a camera's projection or a light's settings, are
written as the JSON Bevy's serializer makes for them, less what the engine works out every frame.
A primitive mesh or a standard material made in memory is written once as how to make it again,
and a mesh built vertex by vertex is written as the geometry it was built from (`Render.DataOf`). A file
the scene refers to is written as its id and its path, and the editor gives every such file an id
in a `.uid` sidecar as it saves, so renaming a model in the asset browser, which carries the
sidecar along, leaves the scene pointing at it.
`scene.save` and `scene.load` do the same from the console and from `./bcs`.

## Placing a model as an instance

```csharp
var ship = SceneInstances.Spawn(ctx.Ecs, "models/ship.gltf");

// Once WorldInstanceReady has been read for it:
var turret = SceneInstances.Find(ctx.Ecs, ship, "Main/Hull/Turret");
SceneInstances.Set(ctx.Ecs, turret, "Bevy.Transform", "Translation", new Vec3(0f, 2f, 0f));
SceneInstances.Delete(ctx.Ecs, SceneInstances.Find(ctx.Ecs, ship, "Main/Hull/Antenna"));
```

In the editor, a model or scene tile's "Place in the scene" does the first line, and an edit in
the details panel or with the gizmo to a node of the instance is kept as an override, with the
field's name in the accent color and a right-click to put the model's value back.

An instance is a glTF scene, or another scene file, placed with the changes made over it kept as
overrides, each naming a node by its path of names from the instance (Bevy puts a glTF scene's
nodes under an entity named after the scene, so the path starts there). A scene file placed this
way is read at once, its overrides applied in the same frame, and instances nest, with a scene
that would contain itself refused on load and on save. A scene file writes the reference and the overrides
and leaves the model's own entities out, so a model exported again reaches every place it is used
with the edits still on it. Loading spawns the model and applies the overrides once Bevy reports
it ready, before `WorldInstanceReady` is read, and an override whose node has gone is kept and
reported by `SceneInstances.Missed`.

With the asset root watched, as the editor watches it, a placed scene file written while the level
runs is spawned again under the entity that placed it. The old copy's entities go, and what they
held with them, the overrides are applied again, what the level added under the old nodes goes back
under the new ones, and `WorldInstanceReady` is posted again for code waiting on the new copy.

## Saving a game

```csharp
[Behavior, Persist]
public partial struct Wallet { public int Coins; }

SaveGame.Start(ctx.Ecs, "levels/one.scene.json");    // the scenes the game begins from
SaveGame.Save(ctx.Ecs);                              // user://saves/slot.save.json
SaveGame.Load(ctx.Ecs);                              // the scenes again, with the save laid over
```

A save holds what play changed and nothing a scene already says. An entity is saved when it
carries a `SaveId`, which the editor gives it with Add Component and code with `SaveId.New()`: its
components marked `[Persist]` (or named in `SaveGame.Persisted`, for one such as `Bevy.Transform`)
are written, an entity spawned during play is written whole, and one of the scene's that was
despawned is written as deleted. `user://` is the platform's data directory under
`Config.GameName`, which Bevy loads from as well (`AssetServer.Load(AssetKind.Image,
"user://shots/one.png")`), and a save is written through a temporary file renamed over the old
one.

A load ends the game in progress first, despawning what its scenes spawned and every entity with a
`SaveId`, so a pause menu loads as a title screen does. What the game spawned for itself without
an id, its camera and its interface, is left for it, since only the game knows whether that still
belongs. A load enters no state, so what a game builds on entering one is not built for what came
back, and `SaveLoaded` is sent the frame after for the game to build it there, once:

<!-- compiled with:
public enum Mode { Menu, Playing }
private static void MakeBall(BehaviorContext ctx) { }
-->
```csharp
[OnUpdate, InState(Mode.Playing)]
public void Loaded(BehaviorContext ctx)
{
    foreach (var loaded in ctx.Read<SaveLoaded>()) MakeBall(ctx);
}
```

The bodies a level holds as components need none of this, since the physics plugin makes them for
whatever the load spawned. A host that loads
its scenes itself, as the editor's player does, names them with `SaveGame.Begin` so a save made
there lays itself over the same scenes a game's would.

Settings that outlive a run go in a `Persistent<T>`, read from `user://` when made and written
when asked, through a `System.Text.Json` source-generated context so nothing reflects:

<!-- compiled with:
using System.Text.Json.Serialization;
public sealed record Settings { public float Volume { get; init; } = 1f; }
[JsonSerializable(typeof(Settings))] public sealed partial class GameJson : JsonSerializerContext;
-->
```csharp
var settings = new Persistent<Settings>("settings", GameJson.Default.Settings, () => new Settings());
settings.Update(value => value with { Volume = 0.5f });
settings.Persist();
```

A `Persistent<T>` given to `SaveGame.Carry` belongs to the save slot instead, copied into each save
and put back on a load, for a playthrough's own state such as its quests.

## Changing a type without breaking its files

<!-- compiled with:
using System.Text.Json.Nodes;
-->
```csharp
[Behavior, FormerName("Hitpoints"), DataVersion(2)]
public partial struct Health
{
    [FormerName("Max")] public float Most;
    public float Current;

    public static JsonObject Migrate(int from, JsonObject value)
    {
        if (from < 2) value["Current"] = (float?)value["Current"] * 10; // 2 counts in tenths
        return value;
    }
}
```

A scene or a data asset written before a rename still loads, because `[FormerName]` keeps the old
name of a type or a field on its schema, and the next save writes the new one. A name given alone
is in the type's own namespace. A change a rename cannot say goes in `Migrate`, which a load calls
with the version the file was written at, and a type at a later version with no such method is
warned about (BCS008).

What a build cannot read is kept rather than dropped: a component whose type it lacks, one Bevy
refuses, and a value no field reads all ride along on the entity as the JSON the file had and are
written back where they were. A branch that deletes a type, or an older build opening a newer
scene, then leaves that type's data in the file for the build that knows it.

---

Before this, [Components](components.md).
Next, [Assets and models](assets-and-models.md).
Bevy's own examples of this, and which are written in C# here, are in
[EXAMPLES.md](../.github/EXAMPLES.md#scene). Its calls are each a line in the [cheatsheet](../CHEATSHEET.md#scenes-and-saves). The [guide's contents](../README.md#guide) list every page.
