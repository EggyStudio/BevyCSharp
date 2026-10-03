# Scenes, data assets and saves

How a scene is written to a file and read back, with what it is made of: glTF content, components
from both sides of the bridge, scenes placed inside other scenes, data kept in files of its own
(Unity's ScriptableObject, Godot's Resource), and a player's save. The values of §4, the ids of §2
for every file,
the scene file of §3, the instances and overrides of §5, the data assets of §6, the type changes
of §7 and the saves of §8 are built, and the rest of
this file is the design, in the order it can be built. [PLAY.md](PLAY.md) plans how a
game made this way is played and shipped.

## What exists

The editor's document is a scene file, `assets/world.scene.json`, opened at start and written and
read by `SceneFile` (§3) through `EditorScene` (`BevyCSharp.Editor/Framework/EditorScene.cs`),
which leaves the editor's own entities out (each marked `EditorOnly`) and replaces the scene when
one is loaded.

A project saved before that has an `assets/world.json` instead (`EditorWorld`, format
`bevycsharp.world.1`), a set of edits by name over a scene code built. Project/Load applies it the
old way when there is no scene file, and the next save writes the scene file, which is read from
then on.

Around it:

- `ComponentSchema` (`BevyCSharp/Ecs/ComponentSchema.cs`) describes every C# component field by
  field without reflection, because the generator emits it (`BevyCSharp.Generator/SchemaEmitter.cs`).
  `Transform` and `Visibility` have one written by hand over their mirrors, and every other
  component Bevy reflects has one built from Bevy's reflection ([COMPONENTS.md](COMPONENTS.md),
  §1), whose fields read and write JSON by reflect path.
- An asset is identified by its path under the asset root and a `#label`
  (`AssetServer.PathOf`). An `AssetHandle` is a key into a table on the native side and means
  nothing in another run. A file also has an id that survives a rename, kept in a sidecar beside it
  (§2).
- `LoadGltfScene` and `EcsWorld.SpawnScene` spawn a glTF scene, and `SceneInstances` places one
  as an instance whose nodes are addressed by path and whose changes are kept as overrides (§5).

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

The managed side resolves both (`SceneFile.Resolve`, `UserData`), and `user://` is registered with
Bevy as an asset source named `user` (ABI 156), with the directory made as an app starts, so a
texture or a glTF file under `user://` loads as one under `assets://` does. The managed files under
`assets://` (scenes, data assets, material and mesh files, the ids beside them) are read through
`AssetFiles`, which takes a file in the asset folder first and the game's own assembly after,
where an export that embeds compiles them (`BevyCSharp.Embed.targets`). A pack file, the third
place a shipped game's assets could be, is not built, and neither is one copy of the assets read
by both sides, which [PLAY.md](PLAY.md) leaves to a shared bridge.

A path with no prefix is under `assets://`, so every path written today keeps working.

## 2. Ids that survive a rename

A scene refers to a model by path, and renaming the model breaks the scene. Unity gives every file
a GUID in a `.meta` file beside it, and Godot 4.4 a `uid://` in a `.uid` file beside it.

Built in `BevyCSharp/Assets/AssetIds.cs`, and used by data assets (§6).

- **A `.uid` file beside an asset**, holding a random 64-bit id as sixteen hex digits. Not `.meta`,
  because Bevy reads a `.meta` beside an asset as its loader's settings, and a file of ours there
  would be read as a malformed one.
- **A reference holds the id and the path**, as `{ "uid": "…", "path": "data/sword.data.json" }`.
  It is resolved by the id first, and by the path when no sidecar holds the id, which keeps a file
  readable by eye and working when its sidecar was lost.
- **The index is read lazily.** `AssetIds.PathOf` reads every sidecar under the asset root the first
  time it is asked, checks a remembered path is still there before trusting it, and reads the root
  again when it is not, so a file renamed outside the editor, with its sidecar beside it, is found
  by its id.
- **Only what edits the project writes one.** The editor gives a file an id as a field is pointed
  at it and gives every file a scene refers to one as it saves the scene (`SceneFile.Save` with
  `giveIds`), and `DataAssets.Create` gives a new data asset one. A game saving a scene at runtime
  writes a reference by path to a file with no id rather than writing beside its assets.
- **Every file is referred to this way.** A model, an image or a sound in an asset field, and an
  entity's mesh and material file, are written as `{ "uid", "path" }` (`SceneReferences.WriteFile`).
  The id is the file's, so a `#label` naming a part of it stays in the path and is put back on
  whichever path the id is found at. A bare string, as scenes wrote before ids, still reads.
- **The asset browser carries sidecars.** It hides them, and its rename, move and delete
  (`EditorAssets.Move`, `EditorAssets.Delete`, `assets.move`, `assets.delete`) take each file's
  sidecar along or away with it.
- **A shipped game carries an index**, `uids.json` at the asset root (`AssetIds.WriteIndex`, format
  `bevycsharp.uids.1`), instead of the sidecars. It is read before them, a sidecar wins over it,
  and an entry naming a file that is not there is passed over. The Play tab's export writes it
  over its own copy of the assets and takes the sidecars out (`AssetIds.IndexForShipping`), and
  writes none for a project that never gave a file an id.

Not built:

- **A file renamed outside the editor without its sidecar** loses its id, and a reference to it
  falls back to the old path, which is gone. Watching for the rename and moving the sidecar after
  it would cover a rename made in a file manager.

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

Built in `BevyCSharp/Scenes/SceneFile.cs`: `SceneFile.Save(world, "assets://levels/one.scene.json")`
writes entities and `SceneFile.Load` spawns them, returning what it spawned and the component types
it kept without knowing them (§7). `scene.save` and `scene.load` do the same from the console and the command line.

- **Every entity has an id local to the file**, as Unity's fileID. Loading puts it on the entity as
  a `SceneId`, and a save gives an entity the id it was loaded with, so a diff of a scene in version
  control shows what changed rather than everything renumbered. An entity new since the last save
  takes the next free number.
- **The hierarchy is a parent id,** written parent before child, and order among siblings is the
  order in the file. A child whose parent is not written is written at the top level.
- **Components are keyed by `QualifiedName`**, the key `ComponentSchemas.For(string)` looks up.
  A C# or mirrored component is written by `SceneValue` (§4), and one of Bevy's reflected ones as
  the JSON Bevy's own serializer makes for it, keyed by its Rust path and inserted through Bevy's
  reflection on load. One Bevy refuses to take back is reported in `SceneLoad.Refused`.
- **What the engine works out for itself is left out:** a global transform, a view's visibility, a
  camera's frustum and what it sees, a light's shadow cascades and the rest of
  `SceneFile.Computed`, the same list the inspector hides.
- **An `Entity` field is a local id**, and loading maps it to the entity that id spawned, whichever
  order the two are listed in. An entity outside the scene cannot be referred to, and such a field
  is written as `null`.
- **Loading spawns everything,** into the top level or under an entity given. Unnamed entities are
  kept, and names need not be unique.
- **What is written** is every entity carrying a component of the project's own or a mirrored one
  of Bevy's, which leaves out the engine's bookkeeping, or whatever a caller's filter chooses, less
  `SceneFile.Excluded`, which the editor sets to its own cameras and previews.
- **A mesh and a material** are written by the file each came from, as its id and path (§2), or,
  made in memory, as a resource of the scene saying how to make it again (a mesh built vertex by
  vertex as its geometry, see below): a primitive as its shape and measures
  (`Render.RecipeOf`) and a standard material as its settings (`Render.TryReadMaterial`). Two
  entities sharing one share one resource, and loading makes it once, so they still share it.

A mesh built vertex by vertex is written as its geometry, the `MeshData` it was built from
(`Render.DataOf`), positions, normals, UVs, colors and indices as flat arrays, and a material from a
material file by that file's id and path ([ASSETS.md](ASSETS.md), §5).

## 4. Writing values without reflection

The schemas carry each field's `FieldKind`, which decides how it is written.

- **The kinds.** `FieldKind` has `String`, `Vec2`, `Vec4` and `Color` beside the others, with
  `Bevy.Vec2`, `Bevy.Vec4` and `Bevy.Color` (linear RGBA, with sRGB and hex conversions) for a C#
  component to hold, and a drawer for each in the inspector. A reflected `Color`, `LinearRgba` or
  `Srgba` of Bevy's is one `Color` field, converted by Bevy whatever space it holds. `List` and
  `Map` are the collections of [COMPONENTS.md](COMPONENTS.md) §2, written as an array and as an
  object (or an array of pairs where the keys cannot be property names), each item through the
  writer of its own kind.
- **A nested struct is an object** in the file, rebuilt from the dotted names the generator gives
  its fields (`Front.Held.At` is written inside `Front` and `Held`), so the file has the shape of
  the type while the inspector keeps its flattened rows. That needs no kind of its own.
- **One writer and one reader per kind** (`SceneValue`, `BevyCSharp/Scenes/SceneValue.cs`), over
  `Utf8JsonWriter` and the reader `JsonDocument` is built on. A vector is an array of numbers, a
  color is linear RGBA, an enum is its name, flags are a list of names, and an entity or an asset is
  an object written by `SceneReferences`: `{ "entity": 7 }` for an entity the writer numbered, and
  `{ "path": … }` for an asset loaded from a file. A reference that cannot be followed after a
  reload (an entity the scene did not number, an asset built in memory) is written as `null`. So
  nothing goes through `ToString()` and back, and nothing reflects, which keeps it working under
  trimming and AOT. `SceneValue.WriteComponent` and `ReadComponent` do a whole component, and a
  field the file names that the type no longer has, or whose JSON is the wrong kind, is left as it
  is rather than ending the load.
- **Bevy's components are saved as Bevy writes them.** Their schemas come from Bevy's reflection
  ([COMPONENTS.md](COMPONENTS.md), §1), and a scene writes each as the JSON Bevy's serializer
  produces, so a camera and its projection, a light, tonemapping and every other reflected
  component are saved with nothing written by hand (§3).
- **A field the schema marks `Derived`** (computed from others) is not written.

## 5. Instances and overrides

One mechanism covers a glTF model placed in a scene, a scene placed in another (a subscene), and a
prefab, which is a scene made to be placed many times.

```json
{ "id": 7, "name": "Ship",
  "instance": { "uid": "a1b2…", "path": "models/ship.gltf#Scene0" },
  "overrides": [
    { "at": "Main/Hull/Turret", "set": { "Bevy.Transform": { "Rotation": [0, 0.38, 0, 0.92] } } },
    { "at": "Main/Hull/Turret", "add": { "Game.Aim": { "Speed": 2 } } },
    { "at": "Main/Hull/Antenna", "remove": "Game.Blink" },
    { "at": "Main/Hull/Antenna", "delete": true },
    { "at": "Main/Hull", "child": 8 }
  ],
  "components": { "Bevy.Transform": { "Translation": [5, 0, 0] } } }
```

Built in `BevyCSharp/Scenes/SceneInstances.cs`, for a glTF scene and for a `.scene.json`:
`SceneInstances.Spawn` places one, `Set`, `Add`, `Remove` and `Delete` change a node and record the change, `SceneFile` writes an
instance as above and loads it again, and the bridge passes Bevy's `WorldInstanceReady` on as a
message of the same name (ABI 155), at the top of the frame after Bevy announces it, with the
instance's overrides already applied.

- **The instance is a reference**, to a glTF scene or to another `.scene.json`, and the file holds
  only what differs from it. A change to the model or the subscene reaches every place it is used.
- **A node is addressed by its path of names** from the instance's root, which is how both a glTF
  file and a scene name their nodes, with `#2` appended where siblings share a name and `#3` for a
  node with no name at all, by its place. Bevy puts a glTF scene's nodes under an entity named
  after the scene (`Main`), and each mesh primitive under its node as `Mesh.Material`, so paths
  begin with the scene's name. A path names each node as the model does, so a node renamed with
  `SceneInstances.Rename` (written `{ "at": "Main/Hull", "rename": "Body" }`, and done by the
  editor's rename) keeps its path, and overrides made before and after the rename all find it.
  An address that no longer finds its node is kept in the file and reported
  (`SceneInstances.Missed`, and a line on stderr), rather than dropped, so a model re-exported
  with a node renamed can be repaired rather than silently losing its edits.
- **An override** sets fields of a component, adds a component, removes one, adds a child, or
  deletes a node. Setting a field writes only that field, so a later change to the model's other
  fields still arrives. One of Bevy's reflected components is set by laying the named fields over
  what the node holds and inserting the result. Recording folds a change into the one before it
  for the same node and component, so the list says where the instance ended up.
- **Instances nest,** and an override can reach into a nested instance by a longer path. Saving
  refuses a scene that would contain itself. Built: overrides are recorded on the outermost
  instance, the one a scene file writes, and one reaching into a nested glTF instance that has
  not spawned yet is applied again when it has. Loading refuses a scene file that places itself,
  directly or through another, and saving looks through the scene files an instance places for
  the file being written.
- **The editor shows what is overridden** by marking the field, and offers reverting it to the
  model's value, as Unity's prefab overrides do. Godot's "editable children" is the same idea.
  Built: an inspector edit, a gizmo drag and `entity.set` on a node of an instance are recorded
  through `SceneInstances.Mark`, a component added or removed there and a node deleted through
  `MarkAdded`, `MarkRemoved` and `Delete`, the field's name is drawn in the accent, and a right-click on it
  offers the model's value back (`SceneInstances.Revert`). The model's value is remembered when a
  field is first overridden and when a load applies an override, and a field set back to it, by
  hand or by an undo, stops being an override. A model or scene tile's "Place in the scene", and
  `scene.place`, put it in the scene as an instance.

Bevy spawns a glTF scene's nodes a frame or more after `SpawnScene` returns, when the file has
loaded, and overrides are applied when Bevy reports the instance ready. A `.scene.json` instance
is spawned on the managed side and applied in the same frame.

Not built:

- **A child added under a node** is built. An instance takes note of what its scene spawned when
  its overrides are first applied (`SceneInstances.IsFromModel`), and an entity under one of its
  nodes that is not among them is the placing scene's own: written there as an ordinary entity,
  under the instance's root, with `{ "at": "Main/Hull", "child": 8 }` naming the node it goes
  under once the instance has spawned. It can be edited, renamed and deleted as any entity of
  the scene, and records no override.
- **One of Bevy's reflected components** is recorded whole and is neither marked field by field
  nor reverted, and removing a model's component and undoing it leaves an add holding its values
  rather than no override.
- **Dropping a model or a scene on the viewport** places it where the pointer meets the ground
  (`AssetsTab.SceneDrop`), with nothing under the pointer taken into account, so a model dropped
  on a table lands at the table's feet.

## 6. Data assets

Data kept in a file of its own and shared by whatever refers to it, such as an enemy's stats, a
weapon, a dialogue or a loot table. Unity calls it a ScriptableObject and Godot a Resource. Built,
and described with the type and the drawer in [COMPONENTS.md](COMPONENTS.md) §3.

- **Stored as `*.data.json`**, holding the format, the type's full name and its fields as
  `SceneValue` writes a component's, so an asset is opened as the right type without its
  extension saying which:

  ```json
  { "format": "bevycsharp.data.1", "type": "Game.WeaponStats",
    "fields": { "Damage": 12, "Title": "Sword", "Tags": ["sharp"], "Bonuses": { "fire": 3 } } }
  ```

- **Written through a temporary file** renamed over the old one, so a crash while saving leaves
  the last version whole.
- **`DataRef<T>` is a reference by id** (§2), so a component field of that type is written to a
  scene as the object of §2, and the inspector offers the files of that type.
- **Loaded once and shared.** `DataAssets.Get` returns the cached value, which is not any one
  reader's to change. `DataAssets.Reload` drops it, and every write and reload posts
  `DataAssetChanged`. Nothing watches the files, so one changed outside the editor is read again
  only on a reload.

## 7. Changing a type without breaking its files

A field renamed, a component renamed or moved to another namespace, or a value that changed its
meaning would otherwise make every saved scene and save game read wrong. All three are built, for
scenes and data assets alike (`BevyCSharp/Scenes/SceneAttributes.cs`, `SceneValue`).

- **`[FormerName("Hp")]`** on a field or a type, recorded in the schema (`ComponentSchema.FormerNames`,
  `FieldHints.FormerNames`), so a file written under the old name is read under the new one and
  saved under the new one. A type's name given alone is placed in its namespace, and one with a dot
  is a full name, for a type that moved. A struct field renamed takes every path under it along,
  because the generator gives each part every whole path it may have had.
- **A version on the type** (`[DataVersion(2)]`) with a static migration method the generator
  finds (`public static JsonObject Migrate(int from, JsonObject value)`), for what a rename cannot
  say. A file records `"$version"` beside the fields once a type is past version 1, a file with
  none is version 1, and the method is called once with the file's version, so it is written as a
  run of checks on `from`. A version past 1 with no method is warning BCS008.
- **What cannot be read is kept.** A component the build has no type for, one Bevy refuses, and a
  value no field reads are carried on the entity (`SceneKept`, a handle into the managed store,
  freed with the entity and copied with a clone) and written back where they were. A data asset
  keeps its unread values with its loaded value the same way.

Not built:

- **A newer file in an older build** is read as far as the fields match and saved at the older
  build's version, with what it did not read kept, so the newer build migrates it again. That is
  safe for a migration that leaves a value already in the new shape alone, which the attribute's
  documentation says, and nothing checks it.
- **A reflected component of Bevy's** is written as Bevy's JSON and has neither former names nor a
  version, since Bevy's own types change with Bevy. One Bevy refuses after an upgrade is kept and
  reported in `SceneLoad.Refused` rather than migrated.

## 8. Saves

A save records what changed while playing, over the scenes the game started from. Built in
`BevyCSharp/Scenes/SaveGame.cs`, with `Persistent<T>` and the `user://` root in
`BevyCSharp/Scenes/Persistent.cs`: `SaveGame.Start` loads the scenes a game begins from and
remembers which saved entities they hold, `SaveGame.Save` writes the difference (format
`bevycsharp.save.1`), and `SaveGame.Load` reads the scenes again and lays the save over them.

- **A diff over the scenes it names.** A level's static content is never written into a save, so a
  save is small and a level fixed in an update reaches players with saves from before it. Each
  saved entity's marked components are written whole rather than as overrides of single fields,
  which keeps the save readable and costs a few bytes a component.
- **What is written is opted into.** An entity carries a `SaveId` (a stable id, set in the editor or
  when spawned) to be saved, and a component is marked `[Persist]` for its fields to be, or named
  in `SaveGame.Persisted` when it is declared elsewhere, as Bevy's transform is. Entities spawned
  during play with a `SaveId` are written whole, with their parent's save id, and ones from the
  scene that were despawned are written as deleted. A field referring to an entity is written as
  that entity's save id.
- **Global state** is `Persistent<T>` ([PLAY.md](PLAY.md)), named in the save rather than copied into
  it, or copied when the game asks for it to belong to the save slot. Built: `SaveGame.Carry`
  copies a `Persistent<T>` into every save under `values` and puts it back on a load, and one not
  carried stays in its own file. Naming one in the save, to check on a load that it is the same
  file, is not built.
- **Under `user://saves/`**, one file a slot, written to a temporary file and renamed over the old
  one, so a crash while saving leaves the last save whole.
- **Loading** loads the scenes the save names and applies it. A save names each scene by id and
  path, so a scene file renamed in an update still matches once it has an id.
- **JSON first.** A binary encoding over the same writers is added only when a real save is too
  large, since a readable save is the easiest to debug.

## 9. The editor and Play

- **The project's document is a scene file** (built). The editor opens it at start, Project/Save
  and `world.save` write it, Project/Load and `world.load` replace the scene with it, and a project
  with no scene file starts from a scene the editor's code builds. An old `world.json` is applied
  the old way when there is no scene file, and deleting that reader once no project needs it is
  not built.
- **Play** ([PLAY.md](PLAY.md), the player) writes the scene being edited to `build/play/` and
  passes it to the player, which loads it through the same `LoadScene`.
- **The asset browser** shows scenes, data assets and models with their sidecars hidden, and
  dragging a model or a scene into the world makes an instance of it (§5). Built, with a tile's
  "Place in the scene" doing the same in front of the camera.

## Order

Each step is usable on its own and is tested before the next.

1. **A pack file** (§1), as a third place `AssetFiles` and the bridge read from, for a game whose
   assets are too large to compile into its binaries.
