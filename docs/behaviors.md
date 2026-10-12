# Behaviors

A `[Behavior]` struct is both a component and the systems that act on it. This chapter covers what a
game writes: what a method runs as, when it runs, which entities it sees, and what it may touch
while it does.

The generator checks a behavior as it is compiled and says what is wrong as an error with a number,
such as a struct that is not `partial`, a stage method that does not take a `BehaviorContext` first,
or a method given two stages. An editor offers to fix those three, and to make a `[Command]` static
or reachable, from the code fixes the package carries beside the generator. Where what was meant is
the game's to say, as for a `[RunIf]` naming nothing, the error says what is wrong and offers no fix.

## Systems and components

Which one a method is depends on whether it is static.

**Static methods are plain systems.** They run once per frame. Use them for global logic that queries other components.

<!-- compiled with:
[Behavior] public partial struct Velocity { public float Y; }
-->
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

<!-- compiled with:
[Behavior] public partial struct Velocity { public Vec3 Value; }
-->
```csharp
[OnUpdate]
public void Tick(BehaviorContext ctx, ref Transform transform, in Velocity velocity) =>
    transform.Translation += velocity.Value * ctx.Time.Delta;
```

They come from the same storage as the behavior's own component, so a system of these reaches its
entities' transforms in a few calls into the engine whatever their number, where reaching each
through `ctx.Ecs` is a call an entity. One taken `in` is not marked changed, so what watches it for
changes sees only real ones.

One taken `ref` is marked changed every frame the system runs, whether it was written or not, where
Bevy marks one only when it is written through. What watches for changes then sees one every
frame: a `[Changed]` filter passes, a transform is propagated again, and Bevy's path tracer starts
its picture over for a camera it reads as moved. A behavior that reads every frame and writes
seldom takes the component `in` and writes through the world when it changes:

```csharp
[OnUpdate]
[MainThread]
public void Follow(BehaviorContext ctx, in Transform transform)
{
    var moved = transform;
    moved.Translation += new Vec3(0f, 0f, 1f) * ctx.Time.Delta;
    if (moved.Translation != transform.Translation) ctx.Ecs.Set(ctx.Entity, moved);
}
```

`[MainThread]` keeps the method on the thread that holds the world, which `ctx.Ecs` needs.

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

<!-- compiled with:
public struct Colliding : ISparseComponent;
-->
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

<!-- compiled with:
public sealed class Score { public void Tick(float seconds) { } }
private static void Setup(BehaviorContext ctx) { }
private static void Step(BehaviorContext ctx) { }
var score = new Score();
-->
```csharp
app.Startup(Setup, "Setup");
app.Update(ctx => score.Tick(ctx.Time.Delta), "Score");
app.On(Stage.FixedUpdate, Step, "Step", runIf: world => !world.Resource<Time>().Paused);
```

`Stage.PreStartup` runs once ahead of the startup stage, for what every startup system leans on,
and has no attribute, being a stage a plugin sets things up in rather than a behavior. A schedule of
the game's own, a `ScheduleLabel` taking systems as a stage does, runs in a place of its own in the
frame once `App.AddSchedule` puts it right after one of Bevy's, and runs its systems one after
another on one thread where it is asked to, as Bevy's custom_schedule shows:

<!-- compiled with:
private static void Settle(BehaviorContext ctx) { }
-->
```csharp
var settle = new ScheduleLabel("SingleThreadedUpdate");
app.AddSchedule(settle, after: Stage.Update, singleThreaded: true);
app.AddSystem(settle, new SystemDescriptor(world => Settle(new BehaviorContext(world)), "Settle"));
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

<!-- compiled with:
private static void CheckForWinner(World world) { }
SystemDescriptor newRound = null!, score = null!, gameOver = null!;
-->
```csharp
app.AddSystem(Stage.Update, new SystemDescriptor(CheckForWinner, "CheckForWinner").After("Score"));
app.Chain(Stage.Update, newRound, score, gameOver);
```

Names are looked up as the app starts, so the other system may be added later, and one that names
nothing in its stage, or a system in another stage, stops the app with a message saying which. A
system added while the app runs is ordered among the others added that way to its stage.

Two systems where one writes what the other reads give different results by whichever runs first,
and an order nobody stated may change between builds. `App.DetectAmbiguities` lists each such pair
in the log as the app starts, as Bevy's `ambiguity_detection` does, from what the systems declare
with `Read<T>` and `Write<T>`. A pair whose order does not matter says so with `AmbiguousWith`, and
`AmbiguityReport.Error` refuses to start instead:

<!-- compiled with:
public sealed class Score { public int Value; }
private static void Count(World world) { }
private static void Show(World world) { }
-->
```csharp
app.DetectAmbiguities(Stage.Update);
app.AddSystem(Stage.Update, new SystemDescriptor(Count, "Count").Write<Score>());
app.AddSystem(Stage.Update, new SystemDescriptor(Show, "Show").Read<Score>());   // reported
```

A system that declares nothing is left out, since nothing is known of what it touches, and
`among` narrows the report to a game's own systems where its stage holds what plugins added too.

## The fixed timestep

Every stage above except one runs exactly once a frame, so anything integrated in them advances
by however long the frame happened to take. That ties the result to the machine, because the same
inputs give a different fall on a slow frame, and a long enough one steps straight through the
floor.

`[OnFixedUpdate]` runs on Bevy's fixed timestep instead, as many times per frame as the elapsed
time allows: twice after a slow frame, not at all after a fast one. Each run covers the same
slice of time, so the simulation is reproducible.

<!-- compiled with:
public Vec3 Velocity;
-->
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

<!-- compiled with:
[Behavior] public partial struct Alive { }
[Behavior] public partial struct Frozen { }
-->
```csharp
[OnUpdate]
[With(typeof(Alive))]
[Without(typeof(Frozen))]
public void Tick(BehaviorContext ctx) { }
```

`[Changed]` skips entities whose listed components did not change this frame. It is a per-entity
test against Bevy's change ticks, so a method carrying it runs sequentially. The entities it skips
are left unmarked and those it runs on are marked, so a method watching its own component runs on
an entity after something changed it rather than on every entity every frame, unless another method
of the behavior runs on every entity without a filter and marks them all.

A system asks the same of one entity with `ctx.Ecs.Changed<T>(entity)`, and whether the component
was put on with `ctx.Ecs.Added<T>(entity)`, each since the running system last ran, as Bevy's `Ref`
answers them. `ctx.Ecs.SetIfNeq` writes only a value that differs from the one held, as Bevy's
`set_if_neq` does, so what watches sees a change only when there is one. Reading a component with
`TryGet` or `GetOrDefault` marks nothing, where `GetRef` and `Mutate` mark it changed.

A resource is a C# object the world cannot watch a field of, so it keeps when it was inserted and
last marked changed, and `ctx.World.IsChanged<T>()` and `IsAdded<T>()` weigh those against the
running system's last run in the same way. `InsertResource`, `SetResource` and `SetResourceIfNeq`
mark it, and `MarkChanged<T>()` does after a field of it is written:

<!-- compiled with:
public sealed record Score(int Value);
-->
```csharp
[OnUpdate]
public static void Report(BehaviorContext ctx)
{
    ctx.World.SetResourceIfNeq(new Score(3));
    if (ctx.World.IsChanged<Score>()) Console.WriteLine($"changed by {ctx.World.ChangedBy<Score>()}");
}
```

`ChangedBy` names the file and line of the call that last changed it, which
`Config.TrackChangeLocations` keeps for resources and for components put on with `Add`, `Set` or
`SetIfNeq`, as Bevy keeps it under its `track_location` feature. It is off by default, and a change
made with no call to name, a behavior's `ref` among them, names none.

What lost a component is no longer there to filter, so a system asks for it. `ctx.Ecs.Removed<T>()`
lists the entities that lost `T`, by a removal or a despawn, since the running system last asked,
as Bevy's `RemovedComponents<T>` gives a Rust system. Each system keeps its own place, so each
removal reaches each system that asks once. Bevy holds a removal for two frames, so a system that
asks less often misses the older ones, and an entity listed may already be gone:

<!-- compiled with:
public struct Health { public int Value; }
-->
```csharp
[OnUpdate]
public static void Forget(BehaviorContext ctx)
{
    foreach (var entity in ctx.Ecs.Removed<Health>())
        Console.WriteLine($"{entity} lost its health");
}
```

## Conditions

`[RunIf]` gates a system on a static `bool` member of the same struct, a field, a property, or a
method taking a `World`. The generator checks the member exists at compile time, so a rename
cannot silently disable your system.

<!-- compiled with:
public sealed class GameState { public bool Playing; }
-->
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
[OnRender, ToggleKey(Key.F3, KeyModifier.Ctrl)]                     // Ctrl + F3
public static void DrawHud(BehaviorContext ctx) { }

[OnRender, ToggleKey(Key.F3, KeyModifier.Ctrl | KeyModifier.Shift)] // Ctrl + Shift + F3
public static void DrawStats(BehaviorContext ctx) { }
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

## Stepping through a stage

`Stepping` holds back the systems of the stages added to it and runs them one at a time on request,
as Bevy's `Stepping` resource does, to watch a frame's work happen system by system. While it is
on, the systems of those stages wait, frame after frame, and every other stage runs as before, so
the app keeps drawing and reading keys. It is driven from a system in a stage that is not stepped:

```csharp
[OnPostUpdate]
public static void Step(BehaviorContext ctx)
{
    if (ctx.Input.KeyPressed(Key.Backquote))
    {
        Stepping.Add(Stage.Update);
        if (Stepping.IsEnabled) Stepping.Disable();
        else Stepping.Enable();
    }

    if (ctx.Input.KeyPressed(Key.S)) Stepping.StepFrame();
    if (ctx.Input.KeyPressed(Key.Space)) Stepping.ContinueFrame();
}
```

`StepFrame` runs the next waiting system and `ContinueFrame` the rest of them, and each call takes
effect at the start of the next frame, as Bevy's does. `AlwaysRun`, `NeverRun` and `SetBreakpoint`
name a system of a stage by its name, `Behavior.Method` for a behavior's, and `ClearBreakpoint`
clears whichever of the three was set. `Stepping.Systems` lists the stepped systems in the order a
stepping frame runs them, from the frame after a stage is added, and `Stepping.Cursor` is the place
in it of the one that runs next. Bevy's own systems in an added stage are stepped with the game's,
so a game that steps only its own sets the rest to always run, as Bevy's helper does in `breakout`
and `BevyCSharp.Examples` does in its copy.

## Copying an entity

<!-- compiled with:
Entity crate = default;
-->
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

A method that reaches what one thread at a time may touch, a resource's fields, the interface being
drawn, a sound or `ctx.Ecs`, is marked `[MainThread]` and runs on the main thread for every entity
however many there are, slower for thousands and safe. A static method runs there already:

<!-- compiled with:
public float Health;
private static AssetHandle cheer;
-->
```csharp
[OnUpdate, MainThread]
public void Cheer(BehaviorContext ctx)
{
    if (Health <= 0f) Audio.Play(cheer, new AudioSettings());
}
```

Queue structural changes rather than applying them mid-loop. Spawning, despawning, adding and
removing all move entities between archetypes, which invalidates every reference the loop holds:

<!-- compiled with:
public float Fuse;
-->
```csharp
[OnUpdate]
public void Tick(BehaviorContext ctx)
{
    Fuse -= ctx.Time.Delta;
    if (Fuse <= 0f) ctx.Cmd.Despawn(ctx.Entity);   // not ctx.Ecs.Despawn
}
```

A change can wait, too. `ctx.Cmd.Delayed(seconds)` is a queue of its own whose commands land once
that long has passed after the frame's queue is applied, Bevy's `commands.delayed()`, so something
happens a while after its cause with no timer to keep:

<!-- compiled with:
Entity spark = default;
-->
```csharp
ctx.Cmd.Delayed(0.5f).Despawn(spark);              // gone half a second from now
```

The delay counts the app's own time, so a game paused by its clock pauses its delays as well, and
a delay of nothing lands the next frame.

---

Before this, [A first game](first-game.md).
Next, [States](states.md).
Bevy's own examples of this, and which are written in C# here, are in
[EXAMPLES.md](../.github/EXAMPLES.md#ecs-entity-component-system). Its calls are each a line in the [cheatsheet](../CHEATSHEET.md#behaviors-and-systems). The [guide's contents](../README.md#guide) list every page.
