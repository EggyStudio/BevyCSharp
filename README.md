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

Each flag is side-agnostic, so `Ctrl` is satisfied by either Ctrl key, which is what a shortcut
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

## Running in a window

The sample has a switch at the top of `Program.cs`:

```csharp
const bool RunInWindow = false;
const GraphicsBackend Backend = GraphicsBackend.Vulkan;
```

or from the command line, which wins over the constants:

```bash
build/build-native.sh --render                  # once: build a bridge with the renderer

dotnet run --project BevyCSharp.Sample -- --window
dotnet run --project BevyCSharp.Sample -- --window --backend vulkan
dotnet run --project BevyCSharp.Sample -- --headless --frames 120
```

Both modes run the identical behavior scripts. Nothing branches on whether a renderer exists -
the engine decides that, from `Config`.

`Config.Backend` pins the graphics API. `Automatic` already prefers Vulkan on Linux and Windows,
so naming it is about making the choice explicit and failing loudly rather than falling back
silently. `App.DescribeAdapter()` reports what you actually got, which is how you check:

```
[Renderer] adapter: Vulkan | NVIDIA GeForce RTX 4070 Laptop GPU | DiscreteGpu | NVIDIA
[Renderer]  245.7 fps   frame    840   spinners 3
```

Ask for a backend the machine has no driver for and startup fails with a message saying so,
rather than quietly picking something else.

**What you will not see yet:** geometry. Bevy's own render components (`Camera`, `Mesh3d`,
`MeshMaterial3d`, …) are not bridged to C# (only components C# declares are), so managed code
cannot spawn anything drawable. `App.SpawnRenderCamera()` is a stopgap that puts a 2D camera in
the world so the window shows a cleared frame instead of undefined contents. The window opens,
the renderer initialises on the GPU, input flows, and the behaviors tick; the drawing half is
still to come.

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
build/                 the native build scripts, and everything they generate
.github/workflows/     CI: builds every runtime identifier, then packs them together
```

### Native profiles

The bridge builds in two profiles:

| Profile    | What it includes                                                  |
|------------|-------------------------------------------------------------------|
| `headless` | App, ECS, time, input, transform. No window, no GPU. The default. |
| `render`   | Bevy's `DefaultPlugins`: windowing, renderer, input backend.      |

```bash
build/build-native.sh --render          # bash
build/build-native.ps1 -Render          # PowerShell, same output
```

The `render` profile is assembled feature by feature rather than taking Bevy's
`default_platform`, which drags in gamepad support and links Wayland at build time. As
assembled here it needs nothing but a C compiler on any platform: X11 comes through `x11-dl`,
Wayland through `wayland-dlopen`, and Vulkan through the loader, all resolved at runtime. It
does take several minutes to compile and produces a much larger library.

`Config.Headless` forces the windowless path even on a render build, which is how the tests and
a dedicated server run the same behavior code without a display.

### Platforms

The managed assembly is portable. The bridge is a cdylib, so it has to be compiled once per
runtime identifier, and a package can only contain the platforms someone actually built.

| Runtime identifier                   | Windowing      | Graphics          |
|--------------------------------------|----------------|-------------------|
| `linux-x64`, `linux-arm64`           | X11 or Wayland | Vulkan            |
| `linux-musl-x64`, `linux-musl-arm64` | X11 or Wayland | Vulkan            |
| `win-x64`, `win-arm64`               | Win32          | Vulkan or DX12    |
| `osx-x64`, `osx-arm64`               | Cocoa          | Metal             |

Each is built by the same command, on a machine that can target it:

```bash
build/build-native.sh --render --target aarch64-apple-darwin
```

`.github/workflows/build.yml` does this across a runner matrix and packs every RID into one
package. Locally you get whichever platform you built; `dotnet pack` skips the slots you have
no binary for rather than failing.

Two platform notes worth knowing:

- **macOS requires the window event loop to own the main thread.** `App.Run` checks this and
  throws a clear error rather than letting it crash inside AppKit. The check applies only when
  a window is actually going to be opened (`App.WillOpenWindow`), because the constraint belongs
  to windowing rather than to the engine: a headless run has no event loop and works from any
  thread, which is what lets a test runner drive it from its own worker threads.
- **The one Linux binary serves both X11 and Wayland.** Which is used is decided at runtime, so
  there is no separate build for each.

Beyond the desktop, Bevy also targets Android, iOS and the web. Those need a different .NET
story entirely (a different app model and, for the web, a different runtime), so they are out of
scope here rather than merely unbuilt.

### Packing

```bash
build/build-native.sh          # stage the native bridge first
dotnet pack BevyCSharp/BevyCSharp.csproj -c Release
```

Packing fails with `BCS101` if the staged bridge is older than the Rust sources, because shipping
a stale one produces an `EntryPointNotFoundException` far from its cause.

To ship more than one platform, run `build-native.sh --target <triple>` for each; every staged RID
slot is picked up at pack time and missing ones are skipped.

### Publishing

An ordinary push is cheap: Linux only, tested, nothing published. Two things change that.

**Changed the readme, the icon or the project metadata.** None of that affects the binaries, so
pushing to the default branch republishes on its own. It reuses the native binaries from the last
full build and only repacks around them, which takes a couple of minutes instead of an hour.

**Changed the code.** Put `[publish]` anywhere in a commit message. That builds all six platforms,
tests on all three operating systems, and publishes:

```bash
git commit -m "add the thing [publish]"
```

The marker is a plain substring, so it works alongside any other text and in any commit of the
push, not only the last one. Either route can also be started by hand from the Actions tab.

Versions are `MAJOR.MINOR.<commit count>`: the first two from `VersionPrefix` in
`Directory.Build.props`, the last from `git rev-list --count HEAD`. One counter that only grows,
shared by both routes so they can never disagree, and nothing stored anywhere. To move to
`0.2.x`, change `VersionPrefix` and push.

Republishing needs artifacts from a full build to still exist, and they expire after two weeks.
If none survive, the run fails and says to push a `[publish]` commit first. An ordinary push
uploads artifacts too, but they cover `linux-x64` alone, so the reuse step checks that a
candidate run really did build every platform before taking anything from it.

---

## Status and limitations

Early. The behavior system, the ECS bridge and the schedule work and are covered by tests that
run against a real Bevy app. Known gaps:

- A locally built package contains only the platform you built it on. Use the CI workflow, or
  run `build-native.sh` on each target platform, to produce a package covering all of them.
- A `render` build opens a real window and initialises the GPU (verified on Vulkan), but Bevy's
  rendering, asset and UI components are not yet surfaced to C#, so nothing can be drawn from a
  behavior script.
- `BehaviorsPlugin.ScriptsDirectory` is reserved for hot-reloading behavior scripts and does
  nothing yet.
- Component filters must be table-stored components, which is everything C# registers. A filter
  naming a Bevy-side sparse-set component is rejected rather than silently wrong.

## License

Mozilla Public License 2.0. The full text is in [LICENSE](LICENSE), and it ships inside the
package.

MPL-2.0 is file-level copyleft: changes to files that are part of this project have to stay under
it and be made available in source form, while anything you build *around* it, including a game
that references the package, is yours under whatever terms you like. Bevy itself is MIT and
Apache-2.0, which this can incorporate freely.
