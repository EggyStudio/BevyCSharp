# BevyCSharp

Write [Bevy](https://bevy.org) games in C#.

Mark a struct `[Behavior]`, give it methods with stage attributes, and a Roslyn source generator
wires it into Bevy's schedule, as a component and a system at the same time. Bevy is the engine
underneath: its ECS, its scheduler, its timing, its input, and its renderer.

```csharp
using Bevy;

[Behavior]
public partial struct Bouncer
{
    public float Height;
    public float Velocity;

    [OnStartup]
    public static void Spawn(BehaviorContext ctx)
        => ctx.Ecs.Add(ctx.Ecs.Spawn(), new Bouncer { Height = 5f });

    [OnUpdate]
    public void Tick(BehaviorContext ctx)
    {
        Velocity -= 9.81f * ctx.Time.Delta;
        Height += Velocity * ctx.Time.Delta;

        if (Height > 0f) return;
        Height = 0f;
        Velocity = -Velocity * 0.8f;
    }
}
```

In Program.cs
```csharp
BevyApp.Run();
```

No registration call, no partial-class list, no startup boilerplate. Add the package, write
behavior scripts, compile, run.

---

## Install

```
dotnet add package BevyCSharp
```

The package carries three things: the managed library, the source generator (in the analyzer
slot), and a prebuilt native bridge per runtime identifier under `runtimes/`.

---

## The behavior model

A `[Behavior]` struct is both a component and the systems that act on it. Which one a method is
depends on whether it is static.

### Static methods are plain systems

They run once per frame. Use them for global logic that queries other components.

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

### Instance methods run per entity

`this` is bound by reference to that entity's component. The struct's fields are per-entity state
living in Bevy's tables.

```csharp
[Behavior]
public partial struct Spinner
{
    public float Angle;
    public float Speed;

    [OnUpdate]
    public void Tick(BehaviorContext ctx) => Angle += Speed * ctx.Time.Delta;
}
```

Above ~4096 entities the per-entity loop is automatically split across the thread pool.

### Plain components

Any blittable struct is a component. It needs no attribute and no interface, the first time a
behavior touches it, its layout is registered with Bevy and it becomes a real Bevy component
with a real `ComponentId`.

```csharp
public struct Position { public float X, Y; }
public struct Falls;   // a zero-field tag costs nothing to store
```

---

## Attributes

### Stages

| Attribute       | When                                              |
|-----------------|---------------------------------------------------|
| `[OnStartup]`   | Once, before the first frame                      |
| `[OnFirst]`     | Top of every frame                                |
| `[OnPreUpdate]` | Before `Update`                                   |
| `[OnUpdate]`    | Main gameplay stage                               |
| `[OnPostUpdate]`| After `Update`, before queued commands are applied |
| `[OnRender]`    | Drawing and overlays, ordered before `Last`       |
| `[OnLast]`      | End of every frame                                |
| `[OnCleanup]`   | Once, on the way out                              |

### Filters

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

### Conditions

`[RunIf]` gates a system on a static `bool` member of the same struct, a field, a property, or a
method taking a `World`. The generator checks the member exists at compile time, so a rename
cannot silently disable your system.

```csharp
[OnUpdate]
[RunIf(nameof(IsPlaying))]
public static void Tick(BehaviorContext ctx) { }

public static bool IsPlaying(World world)
    => world.TryGetResource<GameState>(out var s) && s.Playing;
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

Each flag is side-agnostic — `Ctrl` is satisfied by either Ctrl key — which is what a shortcut
normally means, and matches winit's `ModifiersState`, the layer Bevy's own windowing sits on.
Bevy itself has no modifier type; it exposes only the individual `KeyCode`s. To pin one side, or
to build a chord out of an ordinary key, write the check yourself:

```csharp
[OnRender]
[RunIf(nameof(ChordHeld))]
public static void DrawHud(BehaviorContext ctx) { }

public static bool ChordHeld(World world)
    => world.Resource<Input>().AllKeysDown([Key.ControlLeft, Key.F3]);
```

`Input` mirrors Bevy's `ButtonInput` here: `AnyKeyDown`, `AllKeysDown`, `AnyKeyPressed` and
`AnyKeyReleased` take a span of keys, like `any_pressed` / `all_pressed` / `any_just_pressed`.

---

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

## How it works

```
your game (C#)
      │  [Behavior] structs
      ▼
BevyCSharp.Generator          Roslyn generator: emits one runner per behavior plus a
      │                       module initializer that announces them
      ▼
BevyCSharp (managed)          App, World, EcsWorld, EcsCommands, BehaviorContext,
      │                       Time, Input, the behavior runners
      ▼  C ABI
bevy_csharp (Rust cdylib)     dynamic component registration, exclusive systems,
      │                       chunked table access, frame-state mirroring
      ▼
Bevy 0.19                     ECS, scheduler, time, input, windowing, renderer
```

A few decisions worth knowing about:

**Components are registered at runtime.** Bevy normally learns component layouts from Rust types
at compile time. C# types are not available to it, so each blittable struct is registered with its
size and alignment through Bevy's dynamic `ComponentDescriptor` support. From that point it is an
ordinary Bevy component: it lives in tables, participates in archetypes, and Bevy's own change
detection sees it.

**Iteration is zero-copy.** A query hands C# raw pointers into Bevy's table storage. The
per-entity loop writes straight into the component column, no marshalling, no staging buffer.

**C# systems are exclusive systems.** While managed code can spawn and despawn at any moment,
that is the only sound option, so Bevy serialises C# systems against each other. The parallelism
that matters is still there: it is inside the per-entity loop, which is where the entity counts
are.

**Discovery is generated, not scanned.** The generator emits a module initializer per assembly, so
registrations have announced themselves before the app is built. A reflection scan remains as a
fallback for assemblies that are loaded but untouched.

---

## Building from source

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download) and
[Rust](https://rustup.rs).

```bash
build/build-native.sh          # build the native bridge (headless profile)
dotnet build                   # build the managed side
dotnet test                    # run the suite
dotnet run --project BevyCSharp.Sample -- --frames 120 --verbose
```

Everything generated lands in `build/`, cargo's target directory, the staged per-RID artifacts,
and the packed `.nupkg`. The repository root stays clean.

```
BevyCSharp/            managed runtime library
BevyCSharp.Generator/  Roslyn source generator
BevyCSharp.Sample/     runnable example behaviors
BevyCSharp.Tests/      test suite, run against a real Bevy app
native/                Rust sources for the bridge
build/                 build-native.sh, and everything it generates
```

### Native profiles

The bridge builds in two profiles:

| Profile    | What it includes                                                  |
|------------|-------------------------------------------------------------------|
| `headless` | App, ECS, time, input, transform. No window, no GPU. The default. |
| `render`   | Bevy's `DefaultPlugins`: windowing, renderer, input backend.      |

```bash
build/build-native.sh --render
```

The `render` profile needs the platform development packages, on Linux that means X11 or
Wayland, alsa, udev and a Vulkan loader. `Config.Headless` forces the windowless path even on a
render build, which is how the tests and a dedicated server run the same behavior code without a
display.

### Packing

```bash
build/build-native.sh          # stage the native bridge first
dotnet pack BevyCSharp/BevyCSharp.csproj -c Release
```

Packing fails with `BCS101` if the staged bridge is older than the Rust sources, because shipping
a stale one produces an `EntryPointNotFoundException` far from its cause.

To ship more than one platform, run `build-native.sh --target <triple>` for each; every staged RID
slot is picked up at pack time and missing ones are skipped.

---

## Status and limitations

Early. The behavior system, the ECS bridge and the schedule work and are covered by tests that
run against a real Bevy app. Known gaps:

- Only the `linux-x64` native bridge is built here. Other RIDs need a run of `build-native.sh` on
  (or cross-compiled for) that platform.
- Bevy's rendering, assets and UI are reachable from Rust but are not yet surfaced to C#. A
  `render` build opens a window; drawing into it still needs bridge work.
- `BehaviorsPlugin.ScriptsDirectory` is reserved for hot-reloading behavior scripts and does
  nothing yet.
- Component filters must be table-stored components, which is everything C# registers. A filter
  naming a Bevy-side sparse-set component is rejected rather than silently wrong.

## License

MIT.
