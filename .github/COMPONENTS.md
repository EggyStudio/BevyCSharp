# Components: Bevy's own, collections, and data assets

How every component Bevy has becomes usable from C# without a mirror written by hand, how a
component holds a list or a dictionary, and how data kept in a file of its own (Unity's
ScriptableObject) is declared, referred to and edited. Tier 1 of §1 is built, and the rest of this
file is the design, in the order it can be built. [SCENES.md](SCENES.md) covers how all of it is written to a
file, and [ASSETS.md](ASSETS.md) how meshes, materials and textures are seen and picked.

## What exists

- **Five of Bevy's components are mirrored by hand.** `Transform`, `GlobalTransform`, `Visibility`,
  `InheritedVisibility` and `ViewVisibility` are C# structs with the same bytes as the Rust types
  (`INativeComponent`, `BevyCSharp/Ecs/NativeComponents.cs`). A system reads them straight out of
  Bevy's storage through `bcs_ecs_get_ptr` and `bcs_ecs_chunks`, with nothing copied.
- **Each mirror is found by a hand-written match.** `bcs_component_id_of`
  (`native/bevy_csharp/src/app.rs`) turns a short name into a component id with one arm per
  mirrored type, and a `bcs_*_layout` export per type reports field offsets, which the managed side
  checks against its mirror before trusting it. Any other name is looked up as a full type path in
  Bevy's registry.
- **Every reflected component is reachable through tier 1** (`native/bevy_csharp/src/reflected.rs`),
  by JSON and by schema, which covers cameras, lights, the hierarchy and nearly everything else.
  The camera, light, mesh and material exports remain as helpers that set several components at
  once.
- **C# components are real Bevy components,** registered from their size and alignment
  (`bcs_component_register`) with no drop hook. `ComponentType<T>` requires `unmanaged`, and the
  generator refuses a stored behavior with a reference in it (BCS006), so a component cannot hold
  a list.
- **`FieldKind` has no kind for a collection.** The generator maps anything it does not know to
  `Opaque`, which the inspector shows as a name and a type with nothing to edit.

A mirror is still a match arm, a layout export, a schema and a test, so it is written only where a
system needs to read the bytes in place every frame. Everything else goes through tier 1.

## 1. Every Bevy component, in three tiers

Bevy describes its own types at runtime through reflection: a type's path, its fields and their
types, its enum variants, and how to insert, read and remove it as a component
(`ReflectComponent`). A type added by a later Bevy, or by a plugin, is described the same way with
nothing written here. So the answer is to read that description rather than to write mirrors, and
to keep mirrors only where their speed matters.

### Tier 1: reflected access to everything

- **The registry, described.** `bcs_reflect_types` walks `AppTypeRegistry` and returns each type
  that has `ReflectComponent`, with its type path (`bevy_light::point_light::PointLight`), its
  short name, its component id, whether it has a default (`ReflectDefault` or `ReflectFromWorld`)
  and whether it is mutable. Every type a field reaches is described once beside them: struct,
  tuple struct, enum with its variants and their fields, or a list, map, set, tuple or opaque
  value. Every component is registered with the world as it is described, so its id is fixed for
  the run.
- **Values as JSON.** `bcs_reflect_get(entity, type, path)` serializes a component or one field of
  it through `TypedReflectSerializer`, and `bcs_reflect_set(entity, type, path, json)` reads a value
  against the field's own type through `TypedReflectDeserializer` and applies it through Bevy's
  `Mut`, so change detection sees it. A path is Bevy's reflect path (`.intensity`, `.color.0.red`),
  so one field is changed without writing the rest. An immutable component is refused rather than
  written, because `reflect_mut` panics on one.
- **Variants by name.** `bcs_reflect_variant` reads which variant an enum holds, since Bevy writes
  an `Option` as `null` or the bare value and its JSON is no record of the variant.
  `bcs_reflect_set_variant` switches to another, with that variant's fields at their defaults.
- **Insert and remove** go through `ReflectComponent::insert` and `remove`, from a JSON value or
  from the type's default.
- **Reasons.** Every failure leaves a sentence for `bcs_reflect_error`, so a path that leads nowhere
  says where it stopped.
- **The C# surface** is `EcsWorld.GetReflected`, `SetReflected`, `GetVariant`, `SetVariant`,
  `InsertReflected` and `RemoveReflected`, naming a component by its full type path.
- **Schemas from the description** (`BevyCSharp/Ecs/ReflectedSchemas.cs`). `ComponentSchemas`
  builds a `ComponentSchema` for each reflected component the first time it is asked inside a
  system, and drops them when a new app starts, since ids belong to a world. A mirrored component
  keeps its hand-written schema. A nested struct is taken apart into rows in a fold, as the
  generator does it, and an enum that carries data is a row choosing the variant, with each
  variant's fields as rows shown only while it is chosen, through the same `FieldCondition` a
  `[ShowIf]` produces. A field with no editor (a string, a list, a handle) is a read-only row
  showing its JSON. So the editor and `./bcs entity.get` and `entity.set` cover every reflected
  component with no code per type, and `ComponentSchema.Origin` tells them apart from a project's
  own.
- **The registry in every profile.** `reflect_auto_register` is in the headless profile, so a
  headless run and the test suite see the same types a window does.

It costs a serialization a call, which suits the inspector, the CLI and a script setting a light
once. A system reading a thousand entities a frame uses tier 3. What tier 1 does not do yet:

- **A handle reads as JSON or as its type name.** `Mesh3d` and `MeshMaterial3d` hold a typed
  handle, which has no JSON form, so the inspector leaves both to "Drawn with". Mapping a handle to
  the bridge's `AssetHandle` by its type would make it a field like any other.
- **No documentation.** `reflect_documentation` is not turned on, so a reflected field has no
  tooltip. Turning it on carries every doc comment Bevy has into the binary.
- **A color is four numbers.** `FieldKind` has no color, so a `Color` is a variant choice over rows
  of floats rather than a swatch, until [SCENES.md](SCENES.md) §4 adds the kind.

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
- **Lookup by type path.** `bcs_component_id_of` resolves any reflected type path through the
  registry, and keeps its arms only for the short names the hand mirrors use. A generated
  mirror names its type by path, so it needs no arm, and the arms go with the hand mirrors.

The camera, light and mesh exports stay as helpers that set several components at once, and read
back through tier 1.

### Traps

- **A type Bevy does not reflect has no schema.** A few render-world and internal components are not
  registered with `ReflectComponent`. `./bcs entity.get` names one as having no schema, and the
  inspector leaves it out, as it leaves out the engine's other bookkeeping.
- **A short name is not unique.** Bevy's crates reuse names, so a schema takes the full type path
  as its name wherever two components share a short one.
- **`#[reflect(ignore)]` fields** are neither read nor written.
- **Order of registration.** A type registered by a plugin exists only after that plugin is added,
  so the description is taken from inside a system, once the app is running.

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

1. **The rest of tier 1**: handles mapped to `AssetHandle`, and documentation as tooltips. Tested
   by reading an entity's mesh through its `Mesh3d` field and getting the handle `Render.SetMesh`
   was given.
2. **Tier 2**: `./bcs schema dump`, the checked-in file, and the generated wrappers. Tested by a
   wrapper and the hand mirror agreeing on a `Transform`.
3. **Collections**: `InlineList`, `EcsList` and `EcsMap`, the remove hook, the kinds and the
   drawers. Tested by a list freed on despawn, with the store's count back where it started.
4. **Data assets** with the drawer. Tested by a round trip through the file, an edit through the
   drawer undone, and a reference that survives the file being renamed.
5. **Tier 3**: generated mirrors and layout probes replacing the hand ones, with the existing
   mirror tests passing unchanged.
