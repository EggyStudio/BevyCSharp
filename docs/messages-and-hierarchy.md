# Messages and the hierarchy

How one system tells another that something happened, how code runs the moment it does, and how
entities are parented so a child moves with what it belongs to.

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

## Observers

A message waits for a system to read it the next frame. An observer runs the moment its event is
triggered, which suits what has to happen before anything else looks at the world, an index kept in
step with a component, or an attack that armor softens before the body behind it takes it.

```csharp
public readonly record struct Explode(Entity Entity) : IEntityEvent;

ctx.Ecs.Observe<Explode>(on => Console.WriteLine($"{on.Entity} exploded"));
ctx.Ecs.Observe<Explode>(mine, on => on.Ecs.Despawn(on.Entity));

ctx.Ecs.Trigger(new Explode(mine));
```

An event is any type a game declares. One that names an entity, an `IEntityEvent`, reaches the
observers watching every such event and then the ones of that entity. An `IPropagatingEvent` goes on
from there up the entity's parents, one at a time, until an observer calls `on.Propagate(false)` or
it reaches the root. `on.Event` is a reference, so an observer that changes the event passes the
changed one on, which is how armor lessens the damage the body hears of. `on.Entity` is where the
event has reached, and the event's own `Entity` is where it started. `App.AddObserver` adds one
before the app runs. The handle `Observe` returns stops the observer when disposed.

Bevy reports what happens to a component, to C# observers as to its own, as five events over it.

```csharp
ctx.Ecs.Observe<Add<Mine>>(on => index.Add(on.Event.Entity, on.Event.Value));
ctx.Ecs.Observe<Remove<Mine>>(on => index.Forget(on.Event.Entity, on.Event.Value));
```

`Add<T>` when an entity gains one it did not have, `Insert<T>` each time one is put on it, added
or replacing, `Discard<T>` for each value given up, by being replaced, removed or despawned,
`Remove<T>` when it leaves, and `Despawn<T>` when its entity is despawned with it. Each carries the
entity and the component's value, which for `Discard` and `Remove` is the one that went. They run
once the change has been made and before the call that made it returns, with the whole world to
work in, so a `Remove<T>` observer finds the component already gone and is handed what it was. A
change queued on `ctx.Cmd` runs them when the commands are applied. An exception in one is written
to the console and goes no further, since it would otherwise have to cross back through Bevy.

A game's event likewise runs its observers inside `Trigger`, before it returns.

---

Before this, [States](states.md).
Next, [Components](components.md).
Bevy's own examples of this, and which are written in C# here, are in
[EXAMPLES.md](../.github/EXAMPLES.md#ecs-entity-component-system). Its calls are each a line in the [cheatsheet](../CHEATSHEET.md#messages). The [guide's contents](../README.md#guide) list every page.
