# How it works

What happens inside, between C# and Bevy.

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
ordinary Bevy component. It lives in tables, participates in archetypes, and Bevy's own change
detection sees it.

**Iteration is zero-copy.** A query hands C# raw pointers into Bevy's table storage. The
per-entity loop writes straight into the component column, no marshaling, no staging buffer.

**C# systems are exclusive systems.** While managed code can spawn and despawn at any moment,
that is the only sound option, so Bevy serializes C# systems against each other. The parallelism
that matters is still there, inside the per-entity loop, which is where the entity counts
are.

**Registration happens at assembly load.** The generator emits a module initializer per assembly,
so every registration has announced itself before the app is built. A reflection scan covers
assemblies that are loaded but untouched.

---

Before this, [The tools](tools.md).
Next, [Compared with Bevy](compared-with-bevy.md).
The [guide's contents](../README.md#guide) list every page.
