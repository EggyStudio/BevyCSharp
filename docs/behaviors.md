# Behaviors

A `[Behavior]` struct is both a component and the systems that act on it. This chapter covers what a
game writes: what a method runs as, when it runs, which entities it sees, and what it may touch
while it does.

## Systems and components

Which one a method is depends on whether it is static.

**Static methods are plain systems.** They run once per frame. Use them for global logic that queries other components.

```csharp
[Behavior]
public partial struct Gravity
{
    [OnUpdate]
    public static void Apply(BehaviorContext ctx)
    {
        foreach (var row in ctx.Ecs.Query<Velocity>())
            row.Component.Y -= 9.81f * ctx.Time.Delta;
    }
}
```

`Query` yields *references* into Bevy's table storage, so assigning to `row.Component` writes the
real component. There is no copy and no write-back step.

**Instance methods run per entity.** `this` is bound by reference to that entity's component. The struct's fields are per-entity state
living in Bevy's tables.

```csharp
[Behavior]
public partial struct Spinner
{
    public float Angle;
    public float Speed;

    [OnUpdate]
    public void Tick(BehaviorContext ctx) => 
        Angle += Speed * ctx.Time.Delta;
}
```

An instance method can also take up to two of the entity's other components after its context, as
`ref` to write one or `in` to read it, and runs only for entities carrying them:

```csharp
[OnUpdate]
public void Tick(BehaviorContext ctx, ref Transform transform, in Velocity velocity) =>
    transform.Translation += velocity.Value * ctx.Time.Delta;
```

They come from the same storage as the behavior's own component, so a system of these reaches its
entities' transforms in a few calls into the engine whatever their number, where reaching each
through `ctx.Ecs` is a call an entity. One taken `in` is not marked changed, so what watches it for
changes sees only real ones.

Above ~4096 entities the per-entity loop is automatically split across the thread pool, where the
method can read and write its own component and the ones it takes, and not the world.

**Any blittable struct is a component.** Any blittable struct is a component. It needs no attribute and no interface, the first time a
behavior touches it, its layout is registered with Bevy and it becomes a real Bevy component
with a real `ComponentId`.

```csharp
public struct Position { public float X, Y; }
public struct Falls;   // a zero-field tag costs nothing to store
```

Components sit in contiguous columns, so iteration is fast. The cost is paid on insertion and
removal, because both move the entity to another archetype and copy its other components along with
it. A tag that is added and removed far more often than it is read can opt out of that trade by
implementing `ISparseComponent`:

```csharp
public struct Colliding : ISparseComponent;   // toggled every frame, only ever filtered on
```

Adding or removing one costs an index write and moves nothing else. In exchange it cannot be the
component a query iterates, because Bevy exposes no way to reach a sparse set's storage in bulk, so
`Query<Colliding>()` is refused rather than quietly returning nothing. Everything else works,
including the thing it is for:

```csharp
[OnUpdate]
[Without(typeof(Colliding))]
public void Fall(BehaviorContext ctx) { }
```

A sparse filter cannot be answered once per table, because two entities in one may differ, so it
is answered per entity. The same rows come back, split into the contiguous runs that satisfy it.

## Stages

| Attribute        | When                                               |
|------------------|----------------------------------------------------|
| `[OnStartup]`    | Once, before the first frame                       |
| `[OnFirst]`      | Top of every frame                                 |
| `[OnPreUpdate]`  | Before `Update`                                    |
| `[OnFixedUpdate]`| Fixed timestep: zero or more times a frame         |
| `[OnUpdate]`     | Main gameplay stage                                |
| `[OnPostUpdate]` | After `Update`, before queued commands are applied |
| `[OnRender]`     | Drawing and overlays, ordered before `Last`        |
| `[OnLast]`       | End of every frame                                 |
| `[OnCleanup]`    | Once, on the way out                               |

A system with no component of its own, which sets a scene up or reads what every frame brings,
is said in one call, as Bevy's `add_systems` says it, each handed the frame's context:

```csharp
app.Startup(Setup, "Setup");
app.Update(ctx => score.Tick(ctx.Time.Delta), "Score");
app.On(Stage.FixedUpdate, Step, "Step", runIf: world => !world.Resource<Time>().Paused);
```

## Order within a stage

Systems in one stage run in an order Bevy picks as it builds the schedule, which is not the order
they were written or added in and may differ between runs. A system that reads what another wrote
this frame names it with `[After]` or `[Before]`, by its system's name, `Behavior.Method`:

```csharp
[OnUpdate]
[After("Gravity.Apply")]
public void Land(BehaviorContext ctx, ref Transform transform) { }
```

A system added by hand says the same on its descriptor, and `App.Chain` adds several, each after
the one before:

```csharp
app.AddSystem(Stage.Update, new SystemDescriptor(CheckForWinner, "CheckForWinner").After("Score"));
app.Chain(Stage.Update, newRound, score, gameOver);
```

Names are looked up as the app starts, so the other system may be added later, and one that names
nothing in its stage, or a system in another stage, stops the app with a message saying which. A
system added while the app runs is ordered among the others added that way to its stage.

## The fixed timestep

Every stage above except one runs exactly once a frame, so anything integrated in them advances
by however long the frame happened to take. That ties the result to the machine, because the same
inputs give a different fall on a slow frame, and a long enough one steps straight through the
floor.

`[OnFixedUpdate]` runs on Bevy's fixed timestep instead, as many times per frame as the elapsed
time allows: twice after a slow frame, not at all after a fast one. Each run covers the same
slice of time, so the simulation is reproducible.

```csharp
[OnFixedUpdate]
public void Step(BehaviorContext ctx)
{
    Velocity.Y -= 9.81f * ctx.Time.FixedDelta;
}
```

Integrate with `ctx.Time.FixedDelta`, not `ctx.Time.Delta`. It is the constant each step covers
rather than a per-frame reading, so it is correct from the first frame and identical in every
step. The rate is `Config.FixedHz` and defaults to Bevy's 64.

Keep a simulation on one clock or the other. Accelerating on the fixed step while integrating
position per frame is half a simulation, and inherits the frame-rate dependence you moved the
other half away from.

What moves in fixed steps is drawn every frame, so it is drawn between its place before the last
step and its place after it, `ctx.Time.FixedOverstep` of the way, the share of a step the fixed
clock has run past the last one. That keeps it smooth when the frame rate and the step rate
differ, and `physics_in_fixed_timestep` in the examples does it.

## Filters

`[With]` and `[Without]` restrict an instance method to a subset of entities. They are resolved
per archetype, not per entity, so they cost nothing in the loop.

```csharp
[OnUpdate]
[With(typeof(Alive))]
[Without(typeof(Frozen))]
public void Tick(BehaviorContext ctx) { }
```

`[Changed]` skips entities whose listed components did not change this frame. It is a per-entity
test against Bevy's change ticks, so a method carrying it runs sequentially.

## Conditions

`[RunIf]` gates a system on a static `bool` member of the same struct, a field, a property, or a
method taking a `World`. The generator checks the member exists at compile time, so a rename
cannot silently disable your system.

```csharp
[OnUpdate]
[RunIf(nameof(IsPlaying))]
public static void Tick(BehaviorContext ctx) { }

public static bool IsPlaying(World world) => 
    world.TryGetResource<GameState>(out var s) && s.Playing;
```

`[ToggleKey]` is the entire implementation of "press F3 to show the overlay":

```csharp
[OnRender]
[ToggleKey(Key.F3, DefaultEnabled = false)]
public static void DrawHud(BehaviorContext ctx) { }
```

`KeyModifier` is a flags enum, so a shortcut can require any number of modifiers at once:

```csharp
[ToggleKey(Key.F3, KeyModifier.Ctrl)]                     // Ctrl + F3
[ToggleKey(Key.F3, KeyModifier.Ctrl | KeyModifier.Shift)] // Ctrl + Shift + F3
```

Each flag is side-agnostic, so `Ctrl` is satisfied by either Ctrl key, as a shortcut normally means,
and matches winit's `ModifiersState`, the layer Bevy's own windowing sits on. Bevy itself has no
modifier type; it exposes only the individual `KeyCode`s. To pin one side, or to build a chord out
of an ordinary key, write the check yourself:

```csharp
[OnRender]
[RunIf(nameof(ChordHeld))]
public static void DrawHud(BehaviorContext ctx) { }

public static bool ChordHeld(World world) => 
    world.Resource<Input>().AllKeysDown([Key.ControlLeft, Key.F3]);
```

`Input` mirrors Bevy's `ButtonInput` here: `AnyKeyDown`, `AllKeysDown`, `AnyKeyPressed` and
`AnyKeyReleased` take a span of keys, like `any_pressed` / `all_pressed` / `any_just_pressed`.

## Copying an entity

```csharp
var copy = ctx.Ecs.Clone(crate);
```

The copy carries every component the original does, Bevy's and C#'s alike, so it draws with the
same mesh and material and sits under the same parent, and the original's children stay where they
are. A C# component is copied byte for byte, except that a stored list or map is copied rather
than shared, so the two entities never free each other's. A component Bevy can neither clone nor
reflect is left off the copy, as Bevy leaves it. It is a structural change, like spawning.

## Threading

A system runs on Bevy's main thread with the world loaned to it. When the generator fans a
per-entity loop out across worker threads, those threads can safely write through the component
reference they were handed, the partitions are disjoint, but they cannot touch the world.

- `ctx.Ecs` immediate, main thread only. From a worker it throws with a message telling you so,
  rather than corrupting the world.
- `ctx.Cmd` a thread-safe queue, applied at the end of `PostUpdate`.
- `ctx.Time`, `ctx.Input` plain snapshots, safe to read anywhere.

Queue structural changes rather than applying them mid-loop. Spawning, despawning, adding and
removing all move entities between archetypes, which invalidates every reference the loop holds:

```csharp
[OnUpdate]
public void Tick(BehaviorContext ctx)
{
    Fuse -= ctx.Time.Delta;
    if (Fuse <= 0f) ctx.Cmd.Despawn(ctx.Entity);   // not ctx.Ecs.Despawn
}
```

---

Next, [States](states.md).
Bevy's own examples of this, and which are written in C# here, are in
[EXAMPLES.md](../.github/EXAMPLES.md#ecs-entity-component-system). Its calls are each a line in the [cheatsheet](../CHEATSHEET.md#behaviors-and-systems). The [guide's contents](../README.md#guide) list every page.
