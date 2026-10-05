# Components

Bevy's own components read and written from C#, the rest of Bevy's reached through its reflection, a
component holding a list or a map, and what is drawn.

## Bevy's own components

A struct you declare is registered with Bevy from its layout. Bevy's own components are the
opposite problem, because they are Rust types C# has no handle on, so they are asked for by name. That
name is the only difference, and the type carries it, so they are used exactly like any other
component:

```csharp
var entity = ctx.Ecs.Spawn();
ctx.Ecs.Add(entity, Transform.At(0f, 5f, 0f));

ref var transform = ref ctx.Ecs.GetRef<Transform>(entity);
transform.Translation.Y -= 9.81f * ctx.Time.Delta;

foreach (var row in ctx.Ecs.Query<Transform>())
    row.Component.Translation.X += 1f;
```

That is Bevy's real `Transform`, not a copy kept in sync, so propagation and rendering see the
write. A struct becomes one of these by implementing `INativeComponent`, which names the engine
type. `ComponentType<T>` then resolves that name instead of registering a fresh component, and
everything downstream already works in ids, so queries, `[With]` filters, change detection and
`ctx.Cmd` reach it with no separate API: there is one `Add`, and it does not care which kind of
component it was handed. `NativeComponents` exposes the raw ids for the handful of entry points
that take one rather than a type (`HasById`, `CountById`, `RemoveById`, `ChangedById`, and the
`Chunks` overload that iterates a named component).

```csharp
public struct Transform : INativeComponent
{
    readonly string INativeComponent.NativeName => "Transform";
    // ... fields laid out exactly as Bevy's
}
```

Only a component C# can mirror byte for byte can be read or written, and the mirrors are checked
against the engine the first time an id is resolved. They are easy to get subtly wrong in a way
nothing else catches. `Transform` uses Rust's default representation, so the compiler reorders its
fields to save padding. `Quat` is sixteen-byte aligned and moves ahead of the two vectors, giving
offsets of 0, 16 and 28 rather than the source order. Both layouts are 48 bytes, so a size check
passes either way and the mistake shows up as stretched geometry. The check compares every offset.

`ChildOf` and `Children` hold a relationship and a `Vec`, neither of which raw bytes can
represent, so they are name-only handles: `Has<ChildOf>()`, `Count<Children>()` and `[With]`
filters work, while reading or writing one is refused rather than corrupting the world. Use
`SetParent`, `ParentOf` and `ChildrenOf` for the hierarchy itself.

The list is curated rather than general, because each entry needs a mirror written by hand as
well as a name the bridge resolves. It holds `Transform`, `GlobalTransform`, `ChildOf`,
`Children`, `Visibility`, `InheritedVisibility`, `ViewVisibility`, `WorldInstance`, `Interaction`
and `Atmosphere`. Any other component Bevy reflects resolves to an id by its full type path, so
`HasById` and the rest reach it as well.

## Every other Bevy component

A component with no mirror is reached through Bevy's reflection, which describes Bevy's types at
runtime, and each one has a typed wrapper in `Bevy.Reflected`, generated from a description of
Bevy's components checked in beside the library, with a property per field:

```csharp
using Bevy.Reflected;

var light = ctx.Ecs.Insert<PointLightRef>(lamp);          // at Bevy's default
light.Intensity = 5000f;
light.ShadowMapsEnabled = true;
light.Color = Color.FromHex("#ffcc88");

if (ctx.Ecs.Get<PointLightRef>(lamp) is { } found)        // or null when the lamp has none
    Console.WriteLine($"range {found.Range}");

ctx.Ecs.Wrap<TextColorRef>(label).Value = Color.White;    // one the program put there itself
```

A number crosses as a number and a color as a linear `Color` whatever space Bevy holds it in, and a
handle inside a component, such as the image a sprite draws, is an `AssetHandle`. An enum whose
variants hold nothing is a C# enum named after its property, `NodeRef.DisplayVariant` for a node's
`Display`. One whose variants hold values is a record, with a record for each variant, so writing
one sets the variant and its values together and reading one is a `switch` on its type:

```csharp
var fog = ctx.Ecs.Insert<DistanceFogRef>(camera);
fog.Falloff = new FogFalloff.Linear(5f, 20f);

var node = ctx.Ecs.Wrap<NodeRef>(panel);
node.Left = new Val.Px(12f);
node.Width = new Val.Percent(50f);

var described = fog.Falloff switch
{
    FogFalloff.Linear linear => $"from {linear.Start} to {linear.End}",
    FogFalloff.Exponential exponential => $"density {exponential.Density}",
    _ => "other",
};
```

Rust's `Option` is a nullable, `float?` for a text box's width or a record such as `SubCameraView`
for a camera's sub view, and null writes `None`. A component that is itself an enum, such as
`Visibility`, has the one property `Value`.

Bevy's resources are reached the same way, since in this Bevy a resource is a component on an
entity of its own. `Resource<T>` finds that entity and gives the wrapper over it, or null when the
world has none, and `InsertResource<T>` puts one in or replaces the one there:

```csharp
if (ctx.Ecs.Resource<UiScaleRef>() is { } scale)
    scale.Value = 2f;                                     // the interface at twice its size

var config = ctx.Ecs.Resource<Wireframe2dConfigRef>() ?? ctx.Ecs.InsertResource<Wireframe2dConfigRef>();
config.Global = true;                                     // every 2D mesh drawn as its edges
```

`ResourceEntity` gives the entity itself, for the string calls below. A resource is found afresh
each time rather than kept, since a plugin may take one away and put it back on another entity.

What a wrapper adds over a string path is the compiler, because a field Bevy renames stops
compiling once the description is regenerated after an upgrade, rather than failing on the day the
line runs. It goes through the same reflection, so it costs what a string path costs.

Under the wrappers is the string API they are written over, which reaches what no wrapper types: a
list, such as a node's box shadows, or an enum inside a variant, such as an orthographic
projection's scaling mode. A component is named by its full Rust type path, a field by Bevy's
reflect path, and a value is JSON:

```csharp
const string Shadow = "bevy_ui::ui_node::BoxShadow";

ctx.Ecs.InsertReflected(panel, Shadow, "[]");
ctx.Ecs.SetReflected(panel, Shadow, string.Empty, shadows.ToJsonString());
string? json = ctx.Ecs.GetReflected(panel, Shadow);    // or null if absent
```

`SetVariant`, `SetReflectedColor` and `SetReflectedAsset` are the same calls for a variant, a color
and a handle. A range of numbers, such as the margins of a `VisibilityRange`, is JSON of its two
ends, `{"start":3,"end":4}`, and is written whole, since a path stops at the range rather than going
into one of its ends.

That reaches nearly everything Bevy has (cameras, lights, projections, the hierarchy), and a
component a later Bevy or a plugin adds is reachable the day it exists, with nothing written on
this side. Inserting goes through Bevy's own insert, so a light arrives with the transform and
visibility it requires.

It costs a serialization per call. That suits setting a light once, an inspector or a save, and a
system reading many entities a frame needs a mirror instead. A path is checked when it is used
rather than when it is compiled, so a refusal throws `BevyNativeException` with Bevy's account of
where the path stopped or why the value did not fit.

The same description gives every reflected component a `ComponentSchema`, built the first time
one is asked for inside a system, so the editor's inspector and `./bcs entity.get` and `entity.set`
show and change them like any C# component. A nested struct becomes rows in a fold, and an enum
that carries data, such as a light's color, is a row choosing the variant with that variant's
fields under it. `ComponentSchema.Origin` says whether a schema is the project's own, a mirror, or
reflected.

## Lists and maps in a component

A component is bytes in Bevy's storage, so it cannot hold a `List<T>`. It holds a list one of two
ways instead:

```csharp
[Behavior]
public partial struct Patrol
{
    public InlineList8<Vec3> Waypoints;   // up to eight, inside the component's own bytes
    public EcsList<string> Visited;       // as many as there are, in a store on the managed side
    public EcsMap<string, int> Counts;    // a dictionary, in the same store

    [OnUpdate]
    public void Tick(BehaviorContext ctx)
    {
        if (Waypoints.Count == 0) Waypoints.Add(new Vec3(1f, 0f, 2f));
        if (!Visited.Contains("gate")) Visited.Add("gate");
    }
}
```

An inline list costs nothing to keep and iterates with everything else, and its capacity is part
of its type, from `InlineList4<T>` to `InlineList64<T>`. An `EcsList<T>` or `EcsMap<K, V>` grows
without bound and holds any type, and is made on its first write. The component holds a handle to it, which a hook
frees when the component leaves its entity, by removal or despawn, and a handle used after that
throws rather than reading a list that has since gone to another entity. Cloning the entity
(`ctx.Ecs.Clone(entity)`) gives the copy lists of its own. Both are drawn by the
inspector as rows, a list with a grip to reorder each item and a map with its keys beside its
values, and written to a scene as an array or an object.

An item can be a struct or a class with fields of its own, such as an `InlineList8<Waypoint>`, or a
`List<LootEntry>` in a data asset. Each is drawn as a fold of its fields, named by its first text,
and written as an object.

## Visibility

Whether an entity is drawn. Render builds only, since a headless bridge has no such component and
says so when the id is resolved.

```csharp
ctx.Ecs.Add(entity, Visibility.Hidden);                 // and everything below it
ctx.Ecs.GetRef<Visibility>(entity).Mode = VisibilityMode.Inherited;

ctx.Ecs.GetRef<InheritedVisibility>(entity).IsVisible;  // after the hierarchy is walked
ctx.Ecs.GetRef<ViewVisibility>(entity).IsVisible;       // after culling: did a camera see it
```

`Visibility` is the request and the other two are Bevy's answers, computed during `PostUpdate`
and overwritten every frame. `InheritedVisibility` reports whether an ancestor hides the entity;
`ViewVisibility` reports whether a camera actually rendered it, which is the one to check before
doing work that only matters on screen.

Set `Visibility` on an entity that is already drawable. Adding it writes the component but does
not pull in the two Bevy computes from it, which arrive with the mesh.

---

Before this, [Messages and the hierarchy](messages-and-hierarchy.md).
Next, [Scenes and saves](scenes-and-saves.md).
Bevy's own examples of this, and which are written in C# here, are in
[EXAMPLES.md](../.github/EXAMPLES.md#ecs-entity-component-system). Its calls are each a line in the [cheatsheet](../CHEATSHEET.md#components). The [guide's contents](../README.md#guide) list every page.
