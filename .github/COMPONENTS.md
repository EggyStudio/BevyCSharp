# Components: Bevy's own, collections, and data assets

How every component Bevy has becomes usable from C# without a mirror written by hand, how a
component holds a list or a dictionary, and how data kept in a file of its own (Unity's
ScriptableObject) is declared, referred to and edited. None of this is built yet. This file is the
design, in the order it can be built. [SCENES.md](SCENES.md) covers how all of it is written to a
file, and [ASSETS.md](ASSETS.md) how meshes, materials and textures are seen and picked.

## What exists

- **Five of Bevy's components are mirrored by hand.** `Transform`, `GlobalTransform`, `Visibility`,
  `InheritedVisibility` and `ViewVisibility` are C# structs with the same bytes as the Rust types
  (`INativeComponent`, `BevyCSharp/Ecs/NativeComponents.cs`). A system reads them straight out of
  Bevy's storage through `bcs_ecs_get_ptr` and `bcs_ecs_chunks`, with nothing copied.
- **Each is found by a hand-written match.** `bcs_component_id_of` (`native/bevy_csharp/src/app.rs`)
  turns a name into a component id with one arm per type, and a `bcs_*_layout` export per type
  reports field offsets, which the managed side checks against its mirror before trusting it.
- **Some are named and never read.** `ChildOf`, `Children` and a few others can be filtered on and
  counted, and their contents are out of reach.
- **Everything else goes through an export of its own.** A camera, a light, a mesh and a material
  are set through calls such as `bcs_render_spawn_light` and `bcs_ecs_insert_asset`, and most of
  them cannot be read back.
- **Nothing uses Bevy's reflection.** The bridge never reads `AppTypeRegistry`, although
  `bevy_reflect` is compiled into every profile (`bevy_world_serialization` needs it) and the
  render profile turns on `reflect_auto_register`, which registers every reflected type Bevy has.
- **C# components are real Bevy components,** registered from their size and alignment
  (`bcs_component_register`) with no drop hook. `ComponentType<T>` requires `unmanaged`, and the
  generator refuses a stored behavior with a reference in it (BCS006), so a component cannot hold
  a list.
- **`FieldKind` has no kind for a collection.** The generator maps anything it does not know to
  `Opaque`, which the inspector shows as a name and a type with nothing to edit.

Adding a Bevy component today means a mirror, a match arm, a layout export, a schema and a test,
and most of Bevy is not worth that, so most of Bevy is invisible to C# and to the editor.

## 1. Every Bevy component, in three tiers

Bevy describes its own types at runtime through reflection: a type's path, its fields and their
types, its enum variants, and how to insert, read and remove it as a component
(`ReflectComponent`). A type added by a later Bevy, or by a plugin, is described the same way with
nothing written here. So the answer is to read that description rather than to write mirrors, and
to keep mirrors only where their speed matters.

### Tier 1: reflected access to everything

- **The registry, dumped.** A new export (`bcs_reflect_types`) walks `AppTypeRegistry` and returns
  each type that has `ReflectComponent`, with its type path (`bevy_light::point_light::PointLight`),
  its short name, and its fields: name, type path, and kind (struct, tuple, enum with its variants,
  list, map, value). Defaults come from `ReflectDefault` where Bevy has one, and documentation
  where the `reflect_documentation` feature is on.
- **Schemas from the dump.** `ComponentSchemas` builds a `ComponentSchema` for each, so the editor,
  the scene format and `./bcs entity.get` and `entity.set` cover every reflected component with no
  code per type. A field's `Read` and `Write` go through the next two exports.
- **Values as JSON.** `bcs_reflect_get(entity, type, path)` serializes a component or one field of
  it through `TypedReflectSerializer`, and `bcs_reflect_set(entity, type, path, json)` applies a
  value through `TypedReflectDeserializer`. A path is Bevy's reflect path (`intensity`, `color.0`,
  `transform.translation.x`), so one field is changed without writing the rest.
- **Insert and remove** go through `ReflectComponent::insert` and `remove`, from a JSON value or
  from the type's default.
- **The registry in every profile.** `reflect_auto_register` moves from the render profile to the
  headless one, so a headless run and the test suite see the same types a window does.

It costs a serialization a call, which suits the inspector, saving, loading and a script setting a
light once. A system reading a thousand entities a frame uses tier 3.

### Tier 2: typed C# over tier 1

Reading a string path at runtime is easy to get subtly wrong, and a rename in Bevy turns into a
silent failure rather than a compile error.

- **A checked-in schema.** `./bcs schema dump` writes the registry to
  `BevyCSharp/Generated/bevy-schema.json`, and the file is committed. It changes when Bevy is
  upgraded, and its diff is the list of what Bevy changed in its components.
- **Generated wrappers.** The source generator reads the file and emits a typed handle per
  component, such as `PointLightRef` with `Intensity`, `Range` and `Color` properties over tier 1,
  and an `ctx.Ecs.Get<PointLightRef>(entity)` that finds it. A field Bevy renamed is then a compile
  error in the code that used it.
- **No reflection on this side.** The wrappers are generated code, so trimming and AOT are
  unaffected, as for every other schema the generator emits.

### Tier 3: byte mirrors, generated

The hand mirrors stay the model for what a system iterates every frame, and the generator writes
them where it can.

- **Only plain data.** A type qualifies when the dump shows only fixed-size fields: numbers,
  vectors, quaternions, colors, fieldless enums, and structs of those. A handle, a `Vec`, a
  `String` or an `Option` of a non-plain type rules it out, and it stays on tier 2.
- **Layout from the compiler.** A mirror needs the offsets Rust chose, which Rust does not
  promise between versions. So for each qualifying type the build generates a Rust export that
  reports `size_of`, `align_of` and `offset_of!` for every field, and the managed side checks the
  mirror against it at startup, as it checks the hand mirrors today. The hand `bcs_*_layout`
  functions become generated ones.
- **Lookup by type path.** `bcs_component_id_of` looks the path up in the registry
  (`TypeRegistry::get_with_type_path`, then the component id of its `TypeId`) rather than matching
  names, so a new mirror needs no arm.

The camera, light and mesh exports stay as helpers that set several components at once, and read
back through tier 1.

### Traps

- **A type Bevy does not reflect is invisible.** A few render-world and internal components are not
  registered. The dump says which components exist without `ReflectComponent`, so the editor can
  list them by name, as it lists `ChildOf` today.
- **A handle in a component** (`Mesh3d`, `MeshMaterial3d`) reflects as an opaque value, so it is
  mapped to `AssetHandle` by type rather than serialized.
- **`#[reflect(ignore)]` fields** are neither read nor written.
- **An enum with data** is a variant name plus that variant's fields, and the inspector draws it as
  a choice with the chosen variant's rows under it.
- **Order of registration.** A type registered by a plugin exists only after that plugin is added,
  so the dump is taken after startup.

## 2. Lists and dictionaries in components

A component lives in Bevy's storage as bytes, so it cannot hold a managed `List<T>`, whose memory
the garbage collector moves and frees. Two kinds fill the gap.

- **`InlineList<T, N>`** holds up to `N` unmanaged items inline, with a count, over C#'s
  `[InlineArray]`. It is the component's own bytes, so it iterates in chunks with everything else
  and costs nothing to keep. Suited to small bounded lists: waypoints, slots, the last few hits.
- **`EcsList<T>` and `EcsMap<K, V>`** are unmanaged handles (a slot and a generation) into a store
  on the managed side, so they grow without bound and hold `string` as well as unmanaged values.
  - **Freed with the entity.** The generator marks a component that holds one, and the bridge
    registers an `on_remove` hook for it through `World::register_component_hooks_by_id`, which
    calls back into the store to free the slot. C# components have no hook today, so this is the
    first.
  - **Cloned deeply.** Cloning an entity copies the list, rather than sharing a slot two entities
    would both free.
  - **A stale handle is caught.** The generation makes a handle to a freed and reused slot fail
    loudly instead of reading someone else's list.
- **The generator** adds `FieldKind.List` and `FieldKind.Map` with an element schema, maps these
  types to them, and accepts them past BCS006.
- **The inspector** draws a list as a fold card of rows with a drag handle to reorder, a remove
  button per row and an add button under them, and a map as a table of key and value. Both record
  their edits in `EditorHistory`.
- **Scene files** write a list as a JSON array and a map as an object ([SCENES.md](SCENES.md), §4).

## 3. Data assets

Data kept in a file of its own and shared by whatever refers to it: an enemy's stats, a weapon, a
dialogue, a loot table. Unity calls it a ScriptableObject and Godot a Resource. [SCENES.md](SCENES.md)
(§6) covers the file and the reference, and this section the type and how it is edited.

```csharp
[DataAsset]
public sealed partial class LootTable
{
    public string Title = "Chest";
    public List<LootEntry> Entries = [];
    public Dictionary<string, float> Weights = [];
}

[Behavior]
public partial struct Chest
{
    public DataRef<LootTable> Loot;
}
```

- **A class or a struct, with collections.** A data asset lives on the managed side, not in Bevy's
  storage, so it may hold lists, dictionaries, strings and nested classes, which a component
  cannot.
- **The generator emits its schema,** including those kinds, and a `Create/Data/Loot Table` entry
  in the asset browser's menu, which writes a new `*.data.json` with the type's defaults.
- **`DataRef<T>` is a reference** (the asset's id, [SCENES.md](SCENES.md) §2), unmanaged, so it
  sits in a component. `DataAssets.Get(chest.Loot)` returns the loaded value, cached and shared, and
  read-only through the reference, because a value shared by every chest is not one chest's to
  change.
- **Reloaded when its file changes,** through the editor profile's asset watcher, with a message on
  the bus for a system that caches something derived from it.

### The drawer

A `DataRef<T>` field in the inspector is a row with the asset's name, a fold arrow and a picker.

- **Picking** opens the asset grid ([ASSETS.md](ASSETS.md), §4) filtered to files of type `T`, with
  a "New" entry that creates one beside the scene.
- **Opened,** the fold shows the asset's own fields under the row, one step in, on a card a step
  darker, as a `[Foldout]` does (`FoldStack` in `DetailsPanel`). A nested `DataRef` folds open the
  same way, a step further in.
- **Editing writes the asset,** not the entity, so every entity referring to it changes, and the
  row says how many do. Each edit is one step in `EditorHistory`.
- **Duplicate** copies the asset to a new file and points this field at the copy, for the one chest
  that differs.
- **Selecting a data asset** in the asset browser shows the same fields in the details panel, so it
  is edited without an entity that refers to it.

## Order

Each step is usable on its own and tested before the next.

1. **Tier 1**: the registry dump, get and set, insert and remove, schemas from the dump, and the
   inspector drawing every reflected component. Tested by setting a `PointLight` field through
   tier 1 and reading it back through Bevy.
2. **Tier 2**: `./bcs schema dump`, the checked-in file, and the generated wrappers. Tested by a
   wrapper and the hand mirror agreeing on a `Transform`.
3. **Collections**: `InlineList`, `EcsList` and `EcsMap`, the remove hook, the kinds and the
   drawers. Tested by a list freed on despawn, with the store's count back where it started.
4. **Data assets** with the drawer. Tested by a round trip through the file, an edit through the
   drawer undone, and a reference that survives the file being renamed.
5. **Tier 3**: generated mirrors and layout probes replacing the hand ones, with the existing
   mirror tests passing unchanged.
