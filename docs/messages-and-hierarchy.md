# Messages and the hierarchy

How one system tells another that something happened, and how entities are parented so a child moves
with what it belongs to.

## Messages

Components say what an entity is and resources what the world has. Neither says what just
happened, which is why a collision or a button press otherwise becomes a component invented to
carry it. A message is sent by one system and read by any number of others, none of which need
know about each other.

```csharp
public readonly record struct Collided(Entity A, Entity B);

ctx.Send(new Collided(a, b));

foreach (var hit in ctx.Read<Collided>())
    Console.WriteLine($"{hit.A} hit {hit.B}");
```

A reader sees the previous frame's messages. The queue is swapped once at the top of each frame,
so every reader sees the same complete set, exactly once, whatever stage it runs in and whatever
order the systems happen to run in. The cost is a frame of latency, since a message is not readable in
the frame it was sent, including by the sender.

Bevy's own messages instead give each reader a cursor, which lets it catch up within the frame. A
cursor needs a stable identity per reader, and a C# system has none the engine can see, so the swap
makes "exactly once" true here. `ctx.Send` is safe from a parallel behavior method, like `ctx.Cmd`;
reading is main-thread only.

What the window reports arrives on the same bus, so an engine message is read exactly like one
another system sent:

```csharp
foreach (var resized in ctx.Read<WindowResized>())
    Layout(resized.Width, resized.Height);

foreach (var focus in ctx.Read<WindowFocusChanged>())
    if (!focus.Focused) Pause();
```

`WindowResized`, `WindowFocusChanged`, `WindowCloseRequested`, `WindowScaleFactorChanged`,
`CursorEntered` and `CursorLeft`. `WindowCloseRequested` is a request rather than a fact. The window
is still open, which is the chance to save or to ask whether the player meant it, and
`App.RequestExit` actually closes it.

Files dragged onto the window arrive the same way:

```csharp
foreach (var hovered in ctx.Read<FileHovered>())
    ShowDropTarget(hovered.Path);

foreach (var _ in ctx.Read<FileHoverCanceled>())
    HideDropTarget();

foreach (var dropped in ctx.Read<FileDropped>())
    LoadLevel(dropped.Path);
```

One message per file, so dropping three sends three. The path is absolute and outside the asset
directory, so it is read with ordinary file APIs rather than through the asset server. Every
hover ends in either a drop or a cancellation.

An asset that will not load says why the same way:

```csharp
foreach (var failed in ctx.Read<AssetLoadFailed>())
    Console.Error.WriteLine($"{failed.Kind} {failed.Path} failed, because {failed.Reason}");
```

A handle reports that a load failed and nothing more, so this message tells a misspelled path apart
from a file that is there and unreadable. `Kind` names the asset type the way `AssetServer.Load`
names one, and is empty for a type the engine loaded for itself as part of something else. It
arrives in every profile, because an asset that will not load is exactly as wrong in a headless run
and harder to notice there.

## The hierarchy

```csharp
ctx.Ecs.SetParent(moon, planet);

var parent = ctx.Ecs.ParentOf(moon);       // planet
var children = ctx.Ecs.ChildrenOf(planet); // [moon]
ctx.Ecs.ClearParent(moon);
```

A child's `Transform` is relative to its parent, and Bevy combines them during propagation, so a
parented entity only has to describe its own motion. Parenting goes through Bevy's relationship API
rather than a raw component write, which keeps the reverse child list correct.

`GlobalTransform` is the result of that propagation: where the entity sits in world space.

```csharp
ref var world = ref ctx.Ecs.GetRef<GlobalTransform>(moon);

world.Translation;                           // world-space position
world.Forward;                               // the direction it faces
world.TransformPoint(new Vec3(0f, 0f, -1f)); // a local point, in world space
world.ToTransform();                         // position, rotation and scale
```

Read it and write `Transform`. Propagation overwrites `GlobalTransform` every frame, and it is a
frame behind a `Transform` written during `PostUpdate` or later, which is when propagation has
already run. It stores an affine matrix rather than a position/rotation/scale triple, because a
chain of arbitrary transforms cannot always be expressed as one, so `Scale`, `Rotation` and
`ToTransform()` decompose it the way Bevy's own accessors do.

Parenting is a structural change, so queue it on `ctx.Cmd` when calling from inside a loop.

---

Before this, [States](states.md).
Next, [Components](components.md).
Bevy's own examples of this, and which are written in C# here, are in
[EXAMPLES.md](../.github/EXAMPLES.md#ecs-entity-component-system). The [guide's contents](../README.md#guide) list every page.
