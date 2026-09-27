# Scenes, data assets and saves

How a scene is written to a file and read back, with what it is made of: glTF content, components
from both sides of the bridge, scenes placed inside other scenes, data kept in files of its own
(Unity's ScriptableObject, Godot's Resource), and a player's save. None of this is built yet. This
file is the design, in the order it can be built, and [PLAY.md](PLAY.md) plans how a game made this
way is played and shipped.

## What exists

The editor's `assets/world.json` (`BevyCSharp.Editor/Framework/EditorWorld.cs`, format
`bevycsharp.world.1`) is a set of edits over a scene that code builds, and not a scene.

- **What it keeps.** Every named entity, every component on it that has a `ComponentSchema`, and
  the asset paths its mesh and material were loaded from. A vector is written as `"x, y, z"` and
  everything else that is not a number or a flag through `ToString()`.
- **How it loads.** Each entry is matched to an entity that already exists by name, and its fields
  are written over. Nothing is spawned and nothing is despawned.
- **What it loses.**
  - Unnamed entities, and the hierarchy.
  - Engine components with no schema, such as a camera's projection and a light's settings.
  - Meshes and materials built in memory, which have no path.
  - `Asset` and `Entity` fields, which are written as `"Asset(3)"`, a key valid for one run.
  - A component the file names and the live entity lacks, which is skipped without a word.
  - Two entities with one name, which collapse into the last.

Around it:

- `ComponentSchema` (`BevyCSharp/Ecs/ComponentSchema.cs`) describes every C# component field by
  field without reflection, because the generator emits it (`BevyCSharp.Generator/SchemaEmitter.cs`).
  Of Bevy's components only `Transform` and `Visibility` have one.
- An asset is identified by its path under the asset root and a `#label`
  (`AssetServer.PathOf`). An `AssetHandle` is a key into a table on the native side and means
  nothing in another run. There are no ids that survive a rename.
- `LoadGltfScene` and `EcsWorld.SpawnScene` spawn a glTF scene, and nothing addresses a node
  inside it afterward.

### Why not Bevy's formats

Bevy writes a scene as `.scn.ron` through reflection, which reads a Rust type's registered
description, and a C# component has no Rust type to describe. Bevy Scene Notation (`bsn!`) is a
macro compiled into Rust source, with no file format or loader to read one at runtime (see
[TODO.md](TODO.md)). So the scene format is the managed side's, written from the schemas, and it
hands Bevy only what Bevy loads anyway, which is glTF files, images and audio by path.

## 1. Two roots

Everything is read from one of two places, named in a path by its prefix.

- **`assets://`** is the game's content, and read-only once shipped. While developing it is the
  asset folder. A shipped game has it as a folder beside the executable, embedded in the binary,
  or in a pack file ([PLAY.md](PLAY.md), embedding).
- **`user://`** is writable and belongs to the player: settings, saves, anything the game writes.
  It is the platform's data directory under the game's name (`$XDG_DATA_HOME` or
  `~/.local/share` on Linux, `%APPDATA%` on Windows, `~/Library/Application Support` on macOS),
  the same one `Persistent<T>` writes to.

Both are registered with Bevy as named asset sources, which 0.19 supports, so a texture or a glTF
file under `user://` loads as one under `assets://` does. The managed files (scenes, data assets,
saves) go through one managed `AssetSource` with three backends (a folder, embedded resources, a
pack), which is the reader [PLAY.md](PLAY.md) plans for embedding, so both sides read the same bytes
from the same place.

A path with no prefix is under `assets://`, so every path written today keeps working.

## 2. Ids that survive a rename

A scene refers to a model by path, and renaming the model breaks the scene. Unity gives every file
a GUID in a `.meta` file beside it, and Godot 4.4 a `uid://` in a `.uid` file beside it.

- **A `.uid` file beside every asset**, holding a random 64-bit id. Not `.meta`, because Bevy reads
  a `.meta` beside an asset as its loader's settings, and a file of ours there would be read as a
  malformed one.
- **A reference holds the id and the path**, as `{ "uid": "…", "path": "models/ship.gltf",
  "label": "Scene0" }`. It is resolved by the id first, and by the path when the id is unknown,
  which keeps a file readable by eye and working when its sidecars were lost.
- **The editor keeps them.** The asset browser writes a sidecar for a file that has none, and moves
  and deletes the sidecar with its file. A rename made outside the editor keeps working through the
  id once the index is rebuilt, which happens when the project opens and when the asset watcher
  sees a sidecar appear.
- **A shipped game carries an index** (`uids.json`, id to path) instead of the sidecars, written by
  the export.

## 3. The scene file

`*.scene.json`, format `bevycsharp.scene.2`, one per scene.

```json
{
  "format": "bevycsharp.scene.2",
  "entities": [
    { "id": 1, "name": "Player",
      "components": {
        "Bevy.Transform": { "Translation": [0, 1, 0], "Rotation": [0, 0, 0, 1], "Scale": [1, 1, 1] },
        "Game.Health":    { "Current": 80, "Max": 100, "Target": { "entity": 4 } },
        "Game.Weapon":    { "Stats": { "uid": "8f3c…", "path": "data/sword.data.json" } } } },
    { "id": 2, "parent": 1, "name": "Camera",
      "components": { "Bevy.Camera3d": { "Fov": 60 } } }
  ]
}
```

- **Every entity has an id local to the file**, as Unity's fileID. It is kept across saves, so a
  diff of a scene in version control shows what changed rather than everything renumbered.
- **The hierarchy is a parent id,** and order among siblings is the order in the file.
- **Components are keyed by `QualifiedName`**, the key `ComponentSchemas.For(string)` looks up.
- **An `Entity` field is a local id**, and loading maps it to the entity that id spawned. An entity
  outside the scene cannot be referred to, and saving such a field writes nothing and warns.
- **Loading spawns everything.** Unnamed entities are kept, and names need not be unique.
- **What the editor spawns for itself is left out** by a marker component (`EditorOnly`) rather than
  by the names it skips today, so a game's entity called Ground is saved.
- **A mesh or material built in memory** is saved as the shape or settings it was built from where
  the bridge can say (a primitive's shape and size, a `MaterialSettings`), and otherwise as an
  asset written beside the scene on save (`<scene>.assets/`), so nothing on screen is lost.

Loading is `ctx.Ecs.LoadScene("assets://levels/one.scene.json")`, returning the root it spawned
under, and the editor's `world.save` and `world.load` take a scene path.

## 4. Writing values without reflection

The schemas carry each field's `FieldKind`, which decides how it is written.

- **More kinds.** `FieldKind` gains `String`, `Vec2`, `Vec4`, `Color`, `List` (of any other kind)
  and `Struct` for a nested struct, which is written as an object rather than flattened into the
  parent's fields as the inspector draws it.
- **One writer and one reader per kind** (`SceneValue`), over `Utf8JsonWriter` and
  `Utf8JsonReader`. A vector is an array of numbers, a color is linear RGBA, an enum is its name,
  flags are a list of names, and an asset or entity is the object above. So nothing goes through
  `ToString()` and back, and nothing reflects, which keeps it working under trimming and AOT.
- **Bevy's components get hand-written schemas,** as `Transform` has, for those worth saving: the
  camera and its projection, the lights, the mesh and material an entity is drawn with, visibility,
  and the render layers. Each is a byte-compatible mirror, which has to match field offsets and not
  only size.
- **A field the schema marks `Derived`** (computed from others) is not written.

## 5. Instances and overrides

One mechanism covers a glTF model placed in a scene, a scene placed in another (a subscene), and a
prefab, which is a scene made to be placed many times.

```json
{ "id": 7, "name": "Ship",
  "instance": { "uid": "a1b2…", "path": "models/ship.gltf", "label": "Scene0" },
  "overrides": [
    { "at": "Hull/Turret", "set": { "Bevy.Transform": { "Rotation": [0, 0.38, 0, 0.92] } } },
    { "at": "Hull/Turret", "add": { "Game.Aim": { "Speed": 2 } } },
    { "at": "Hull/Antenna", "delete": true },
    { "at": "Hull", "child": { "id": 8, "name": "Light", "components": { "…": {} } } }
  ] }
```

- **The instance is a reference**, to a glTF scene or to another `.scene.json`, and the file holds
  only what differs from it. A change to the model or the subscene reaches every place it is used.
- **A node is addressed by its path of names** from the instance's root, which is how both a glTF
  file and a scene name their nodes, with `#2` appended where siblings share a name. An address
  that no longer finds its node is kept in the file and reported, rather than dropped, so a model
  re-exported with a node renamed can be repaired rather than silently losing its edits.
- **An override** sets fields of a component, adds a component, removes one, adds a child, or
  deletes a node. Setting a field writes only that field, so a later change to the model's other
  fields still arrives.
- **Instances nest,** and an override can reach into a nested instance by a longer path. Saving
  refuses a scene that would contain itself.
- **The editor shows what is overridden** by marking the field, and offers reverting it to the
  model's value, as Unity's prefab overrides do. Godot's "editable children" is the same idea.

Bevy spawns a glTF scene's nodes a frame or more after `SpawnScene` returns, when the file has
loaded. Overrides are applied when Bevy reports the instance ready (`WorldInstanceReady`, since 0.19 calls a spawned scene a world), which the
bridge does not pass on yet, so this step adds that message to the bus and bumps the ABI version.
A `.scene.json` instance is spawned on the managed side and applied in the same frame.

## 6. Data assets

Data kept in a file of its own and shared by whatever refers to it, such as an enemy's stats, a
weapon, a dialogue or a loot table. Unity calls it a ScriptableObject and Godot a Resource.

```csharp
[DataAsset]
public partial struct WeaponStats
{
    [Range(0, 200)] public float Damage;
    public float Cooldown;
    public Asset Model;
}

[Behavior]
public partial struct Weapon
{
    public DataRef<WeaponStats> Stats;
}

var stats = DataAssets.Get(weapon.Stats);   // cached, and reloaded when its file changes
```

- **The generator emits the same schema** a component gets, so a data asset is written by the
  writers of §4, drawn by the inspector with its attributes, and needs no code of its own.
- **Stored as `*.data.json`**, with the type's `QualifiedName` in the file, so an asset is opened
  as the right type without its extension saying which.
- **`DataRef<T>` is a reference** (the object of §2), so a component field of that type is saved as
  one and the inspector offers the files of that type to pick from.
- **Loaded once and shared.** `DataAssets.Get` returns the cached value, which is read-only through
  the reference, because a value shared between every enemy is not one enemy's to change. A change
  made in the editor writes the file.
- **Reloaded when the file changes,** through the editor profile's asset watcher,
  with a message on the bus so a system can react.
- **The editor makes and edits them** from the asset browser, with the details panel drawing the
  asset as it draws a component.

## 7. Changing a type without breaking its files

A field renamed, a component renamed or moved to another namespace, or a value that changed its
meaning would otherwise make every saved scene and save game read wrong.

- **`[FormerName("Hp")]`** on a field or a type, recorded in the schema, so a file written under
  the old name is read under the new one.
- **A version on the type** (`[DataVersion(2)]`) with a static migration method the generator finds
  (`Migrate(int from, JsonObject value)`), for what a rename cannot say.
- **What cannot be read is kept.** A component or field the running build does not know is carried
  through a load and written back unchanged, so opening a scene in a build that lacks a type does
  not delete that type's data from it.

## 8. Saves

A save records what changed while playing, over the scenes the game started from.

- **A diff, using the override records of §5,** over the scenes it names. A level's static content
  is never written into a save, so a save is small and a level fixed in an update reaches players
  with saves from before it.
- **What is written is opted into.** An entity carries a `SaveId` (a stable id, set in the editor or
  when spawned) to be saved, and a component is marked `[Persist]` for its fields to be. Entities
  spawned during play with a `SaveId` are written whole, and ones from the scene that were
  despawned are written as deleted.
- **Global state** is `Persistent<T>` ([PLAY.md](PLAY.md)), named in the save rather than copied into
  it, or copied when the game asks for it to belong to the save slot.
- **Under `user://saves/`**, one file a slot, written to a temporary file and renamed over the old
  one, so a crash while saving leaves the last save whole.
- **Loading** loads the scenes the save names and applies it. A save names each scene by id, so a
  scene file renamed in an update still matches.
- **JSON first.** A binary encoding over the same writers is added only when a real save is too
  large, since a readable save is the easiest to debug.

## 9. The editor and Play

- **`EditorWorld` becomes the scene writer and reader,** and the project has a current scene rather
  than a world file. `world.save` and `world.load` take a path. An old `world.json` is read by the
  old code once and written in the new format, and the old reader is then deleted.
- **Play** ([PLAY.md](PLAY.md), the player) writes the scene being edited to `build/play/` and
  passes it to the player, which loads it through the same `LoadScene`.
- **The asset browser** shows scenes, data assets and models with their sidecars hidden, and
  dragging a model or a scene into the world makes an instance of it (§5).

## Order

Each step is usable on its own and is tested before the next.

1. **Values** (§4, the writers and the new kinds). Tested by writing and reading every kind.
2. **The scene file** (§3) with the hierarchy, spawning and entity references, replacing
   `world.json`. Tested by a scene saved from an `EngineHarness` world, loaded into a fresh one,
   and compared field by field.
3. **Bevy's components** (§4, the camera, lights, mesh and material mirrors). Tested by the same
   round trip, drawing both worlds and comparing the pictures.
4. **Ids** (§2), with the index and the asset browser keeping the sidecars.
5. **Data assets** (§6). Tested by a component referring to one, saved, reloaded and edited.
6. **glTF instances with overrides** (§5, with `WorldInstanceReady` bridged). Tested by an
   override on a node of a glTF test file, applied after the scene spawns.
7. **Subscenes** (§5, a `.scene.json` instance), with nesting and the cycle check.
8. **Changing types** (§7).
9. **Saves** (§8), over `Persistent<T>` and the `user://` root.
10. **Embedded and packed `assets://`** (§1), with the export in [PLAY.md](PLAY.md).
