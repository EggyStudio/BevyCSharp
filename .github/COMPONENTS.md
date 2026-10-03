# Components: Bevy's own, collections, and data assets

How every component Bevy has becomes usable from C# without a mirror written by hand, how a
component holds a list or a dictionary, and how data kept in a file of its own (Unity's
ScriptableObject) is declared, referred to and edited. Tiers 1 and 2 of §1, the collections of §2
and the data assets of §3 are built, and the rest of this file is the design, in the order it can
be built. [SCENES.md](SCENES.md) covers how all of it is written to a
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
  (`bcs_component_register`). `ComponentType<T>` requires `unmanaged`, and the generator refuses a
  stored behavior with a reference in it (BCS006), so a component holds a list through §2 rather
  than as a `List<T>`.

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
- **Colors as one value.** Bevy's `Color` holds a color in any of ten spaces, so its JSON is
  whichever space it is in. `bcs_reflect_get_color` and `bcs_reflect_set_color` read any `Color`,
  `LinearRgba` or `Srgba` as linear RGBA through Bevy's own conversions, and write one back in the
  space it was held in, so the field is a single `Bevy.Color` swatch rather than a choice of space
  over rows of numbers that mean something different in each.
- **Handles as asset keys.** A handle has no JSON form, so `bcs_reflect_get_asset` turns one into
  the key the bridge's asset table knows it by, through the `ReflectHandle` Bevy registers on every
  `Handle<A>`, and `bcs_reflect_set_asset` retypes a key back into the field's own handle type,
  refusing an asset of another kind. A read finds the slot already holding the asset, so a field
  read every frame takes no slot of its own. The description names each handle's asset kind with
  the same list `AssetServer.Load` takes.
- **Documentation in the editor.** The editor profile turns on `reflect_documentation`, so the
  description carries Bevy's doc comment for every field and the inspector shows its first
  paragraph as the row's tooltip. A game's profile leaves it off, since nobody reads a tooltip in a
  shipped game and the comments would be carried in its library.
- **The C# surface** is `EcsWorld.GetReflected`, `SetReflected`, `GetVariant`, `SetVariant`,
  `GetReflectedAsset`, `SetReflectedAsset`, `InsertReflected` and `RemoveReflected`, naming a
  component by its full type path.
- **Schemas from the description** (`BevyCSharp/Ecs/ReflectedSchemas.cs`). `ComponentSchemas`
  builds a `ComponentSchema` for each reflected component the first time it is asked inside a
  system, and drops them when a new app starts, since ids belong to a world. A mirrored component
  keeps its hand-written schema. A nested struct is taken apart into rows in a fold, as the
  generator does it, and an enum that carries data is a row choosing the variant, with each
  variant's fields as rows shown only while it is chosen, through the same `FieldCondition` a
  `[ShowIf]` produces. A handle is an asset field picked from the files of its kind, as a C#
  `AssetHandle` field is. A field with no editor (a string, a list) is a read-only row showing its
  JSON. So the editor and `./bcs entity.get` and `entity.set` cover every reflected
  component with no code per type, and `ComponentSchema.Origin` tells them apart from a project's
  own.
- **The registry in every profile.** `reflect_auto_register` is in the headless profile, so a
  headless run and the test suite see the same types a window does.

It costs a serialization a call, which suits the inspector, the CLI and a script setting a light
once. A system reading a thousand entities a frame uses tier 3. What tier 1 does not do:

- **A handle of a kind the bridge does not load is only shown.** Its kind has no name in the list
  `AssetServer.Load` takes, so there are no files to offer for it.

### Tier 2: typed C# over tier 1

Reading a string path at runtime is easy to get subtly wrong, and a rename in Bevy turns into a
silent failure rather than a compile error.

- **A checked-in description.** `./bcs command schema.dump <path>`
  (`BevyCSharp/Diagnostics/ConsoleSchemaCommands.cs`) writes every reflected component and the
  fields a wrapper can type to `BevyCSharp/Generated/bevy-components.tsv`, and the file is
  committed. It is written from an editor build, the profile that reflects the most, and again
  whenever Bevy is upgraded ([BUILDING.md](BUILDING.md)), and its diff is the list of what Bevy
  changed in its components. It is sorted tab-separated lines rather than JSON, because the
  generator runs inside the compiler on netstandard2.0, where a JSON reader would have to ship with
  the analyzer, and because a line per field diffs as exactly the fields that changed. The fields
  come from the same schemas the inspector draws, so a wrapper and a row name everything alike.
- **Generated wrappers.** `BevyCSharp.Generator/ReflectedGenerator.cs` reads the file, which only
  the library names as an additional file, and emits `Bevy.Reflected.PointLightRef` and the rest:
  a readonly struct over an entity with a typed property per field (`float Intensity`,
  `bool ShadowMapsEnabled`, a nested enum for an enum's variants, `AssetHandle` for a handle) and a
  `Remove()`. `ctx.Ecs.Get<PointLightRef>(entity)` returns one, or null when the entity has no
  light, and `ctx.Ecs.Insert<PointLightRef>(entity)` adds the component at its default. A field
  Bevy renamed is then a compile error in the code that used it.
- **No reflection on this side.** The wrappers are generated code, so trimming and AOT are
  unaffected, as for every other schema the generator emits.
- **What a wrapper leaves out.** A field shown only as JSON, and the fields inside an enum's
  variants, which are there only while that variant is held, have no property. Those stay on the
  string paths of tier 1.

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
the garbage collector moves and frees. Two kinds of list fill the gap, and both are built.

- **Inline lists** (`BevyCSharp/Ecs/InlineList.cs`) hold a count and a fixed run of unmanaged
  items over C#'s `[InlineArray]`. They are the component's own bytes, so they iterate in chunks
  with everything else and cost nothing to keep, which suits small bounded lists: waypoints, slots,
  the last few hits. The capacity is part of the type, `InlineList4<T>` through `InlineList64<T>`,
  because C# fixes an inline array's length at compile time and cannot take it as a type parameter.
  A freed slot is cleared, so two lists of the same items are the same bytes.
- **`EcsList<T>` and `EcsMap<K, V>`** (`BevyCSharp/Ecs/EcsList.cs`, `EcsMap.cs`) are unmanaged
  handles (a slot and a generation) into a store on the managed side, so they grow without bound
  and hold `string` as well as unmanaged values. Each is made on its first write.
  - **Freed with the entity.** The generator registers a remove hook for every component holding
    either, from the module initializer that registers its schema. The bridge attaches it with
    `World::register_component_hooks_by_id` (`bcs_component_on_remove`,
    `native/bevy_csharp/src/lifecycle.rs`) when the component is first registered, since Bevy
    takes a hook only before any entity carries the component, and Bevy calls it on removal and on
    despawn with the component's bytes, from which the hook frees each list. `on_remove` alone,
    because the read, modify, write of a component writes back the handle it read, and freeing on
    a replace would free the list the new value still holds.
  - **A stale handle is caught.** The generation makes a handle to a freed and reused slot throw
    `ObjectDisposedException` instead of reading someone else's list.
- **The generator** gives every list `FieldKind.List`, with `ComponentField.ElementKind` naming the
  items' kind, and a map `FieldKind.Map`, with `KeyKind` beside it. It reads one as a `ListValue`
  or a `MapValue` (equal by their entries, so a tool comparing two reads sees no change) and writes
  one whole, refusing a map with two entries under one key. All of them pass BCS006, being
  unmanaged.
- **The inspector** draws a list (`ListRows`) as a row per item, through the same switch as any
  field, with a grip to drag it to another place, a button to take it out and one under them to add
  one, and a map (`MapRows`) as a row per entry with its key and its value side by side. Every
  change is a write of the whole collection, recorded as one step in `EditorHistory`.
- **Scene files** write a list as a JSON array, and a map as an object, or as an array of pairs
  where its keys cannot be property names ([SCENES.md](SCENES.md), §4).
- **Bevy's lists are lists too.** A reflected `Vec` or array of an editable kind is a list field,
  read and written as one JSON array.
- **Cloned deeply.** Bevy clones only components that implement `Clone` or `Reflect`, which a C#
  component's bytes do not, so every C# component is registered with a clone behavior of the
  bridge's own (`lifecycle::cloned`) that copies its bytes. For a component holding a stored list
  or map, the generator registers a clone hook beside the remove hook, which the copy passes
  through first and which gives it copies of its own (`EcsList.Copy`), so the first of the two to
  go frees only its own. `EcsWorld.Clone` copies an entity, and the editor's Duplicate is built on
  it.

What is not built:

- **A collection replaced in a component is not freed.** Writing a component over one holding a
  different list replaces the handle and leaves the old one to `Free`.

## 3. Data assets

Data kept in a file of its own and shared by whatever refers to it: an enemy's stats, a weapon, a
dialogue, a loot table. Unity calls it a ScriptableObject and Godot a Resource. [SCENES.md](SCENES.md)
(§6) covers the file and the reference, and this section the type and how it is edited. Built in
`BevyCSharp/Assets/DataAssets.cs` and `BevyCSharp.Editor/Framework/EditorDataAssets.cs`.

```csharp
[DataAsset]
public sealed class LootTable
{
    public string Title = "Chest";
    [Range(0, 1)] public float Chance = 0.25f;
    public List<string> Items = [];
    public Dictionary<string, float> Weights = [];
}

[Behavior]
public partial struct Chest
{
    public DataRef<LootTable> Loot;
}

var table = chest.Loot.Value;   // or DataAssets.Get(chest.Loot)
```

- **A class or a struct, with collections.** A data asset lives on the managed side, not in Bevy's
  storage, so it may hold strings, `List<T>` and `Dictionary<K, V>`, which a component cannot. A
  class needs a constructor with no parameters, which gives a new file its defaults.
- **The generator emits its fields,** with the attributes a component's carry, counting a string,
  a list and a dictionary as fields since the type is managed, and registers the type from a
  module initializer. The fields read and write a box holding the asset's value, so a struct asset
  is written back as a class one is, and they are the same `ComponentField` rows a component has,
  which the inspector draws and `SceneValue` writes with no code of their own.
- **`DataRef<T>` is a reference** by the file's id ([SCENES.md](SCENES.md) §2), eight unmanaged
  bytes, so it sits in a component. `DataAssets.Get(chest.Loot)` returns the loaded value, cached
  and shared, which is not one chest's to change. `DataAssets.Save` and the editor write the file.
  `DataAssets.Reload` drops the cached value, and every write or reload posts `DataAssetChanged` on
  the bus for a system that caches something derived from it.

### The drawer

- **Picking.** A `DataRef<T>` field in the inspector is a row with the file's path, offering the
  data files whose type is `T`, and "Nothing". A file is given an id as it is picked, so the
  reference keeps working when the file is renamed.
- **Selecting a data asset** in the asset browser shows its fields in the details panel, as a
  component's are, and an edit writes the file at the end of the frame, so a slider dragged across
  a field writes once a frame. Each edit is one step in `EditorHistory`.
- **Making one.** `Project/New data asset` offers every registered type and writes a new file at
  the type's defaults in the directory the asset browser is looking at.

- **Items with fields of their own.** A list or a map whose items are a struct or a class (a
  `List<LootEntry>`, an `InlineList8<Waypoint>`) has `FieldKind.Struct` items, and the generator
  describes the item type in an `ItemFields` on the field, as it describes a data asset: rows over
  a box holding one item, a class item copied first so an edit never changes the one the list
  holds. A scene or a data file writes each item as an object of its fields, and the editor draws
  a list's items each as a fold named by its first text, with the grip and the remove button in
  front, and a map's values as their fields under their key. Items go
  three types deep, since a class can hold a list of itself.
- **Opened in place.** A set `DataRef` row has a fold under it named for the asset's type and how
  much shares it ("shared with 2 others"), holding the asset's own rows, and "Make unique" in it
  copies the asset to a file of its own beside it (`DataAssets.Copy`) and points the field at the
  copy, for the one chest whose loot differs. The count takes in the entities and the other data
  assets that refer to it, which its tip tells apart, and a reference a data asset holds folds
  open as a component's does, so a weapon's ammunition is edited under the weapon.
- **Reloaded on its own.** With `Config.WatchAssets` on, as the editor has it, a data file changed
  on disk is dropped from the cache at the top of the next frame and `DataAssetChanged` posted,
  while a write this side made is told apart by its time and passed over (`DataAssets.Watching`).

What is not built:

- **References inside items.** A `DataRef` held in a list's item is picked like any other but has
  no fold under it, since an item's rows are drawn by `ListRows.Fields` rather than as a field's
  row, and the count of what shares an asset reads only the fields of an entity's components and
  of a data asset, so it leaves such a reference out.

## Order

Each step is usable on its own and tested before the next.

1. **Tier 3**: generated mirrors and layout probes replacing the hand ones, with the existing
   mirror tests passing unchanged.
