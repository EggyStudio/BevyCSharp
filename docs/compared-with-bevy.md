# Compared with Bevy

What a game written here shares with one written against Bevy in Rust, what it gains, what it
gives up, and what was measured of the difference.

## What is the same

Bevy itself runs underneath, unchanged. Its ECS holds the world, its scheduler runs the stages, and
its renderer, assets, audio, input, interface, text, gizmos and picking do what they do in a Rust
game, at the version `native/Cargo.lock` pins, Bevy 0.20.0. Nothing of it is written again in C#.
A component a C# struct declares is a Bevy component on a Bevy entity, a C# system is a system in
Bevy's schedule, a `Transform` written from C# is the transform Bevy propagates and draws, and one
of Bevy's own components is reached by its type path through Bevy's reflection. A Rust game and a
C# one drawing the same scene draw the same picture, and [EXAMPLES.md](../.github/EXAMPLES.md)
shows each written example's capture beside Bevy's to compare.

## What it adds

- **C# in place of Rust.** A game is a .NET project, `[Behavior]` structs with methods given stage
  attributes, which a source generator turns into Bevy components and systems
  ([Behaviors](behaviors.md)). Nothing reflects at runtime, so a game trims and compiles ahead of
  time.
- **Behaviors compiled and reloaded while the game runs.** A behavior script changed on disk is
  compiled into the running app, the entities that carried the old type are given the new one with
  their values kept, and the engine is not rebuilt ([Hot reload](running-a-game.md#hot-reload)).
- **An editor.** A hierarchy, details, an asset browser, play in the editor and undo, itself an
  ordinary app of this engine ([The editor](tools.md#the-editor)).
- **Scene files, saves and data assets**, with entity references and Bevy's components kept, and
  files that outlive a renamed type or field by its former name and a migration
  ([Scenes and saves](scenes-and-saves.md)).
- **Physics**, bodies, colliders, joints, contacts and characters over BepuPhysics, with the
  library and nothing else, where Bevy leaves physics to crates of its community
  ([Physics](physics.md)).
- **Shaders in Slang**, compiled and reloaded as the game runs, beside Bevy's WGSL
  ([Shaders](shaders.md)).
- **An app driven from the terminal.** `bcs` asks a running app what is in its world, sets a
  value, presses a key and takes a picture, the same commands its console and editor have
  ([Driving a running app](tools.md#driving-a-running-app)).
- **A package with no Rust in it.** `dotnet add package BevyCSharp` brings the library, the
  generator and a built bridge for Linux, Windows and macOS on x64 and Arm, so a game needs the .NET
  SDK and no Rust toolchain ([Install](https://github.com/EggyStudio/BevyCSharp#install)).

## What it costs

- **A crossing between C# and the bridge.** Each call from C# into Bevy goes through the bridge's C
  ABI, 16 to 28 ns with the way in and out on the machine below, about what the C# work for one
  entity costs. A behavior that takes its entity's components as parameters is handed them in runs
  and makes a few calls a system, and one that asks the world for each entity makes a call an
  entity.
- **Only what is bridged is reachable.** A part of Bevy the bridge has not opened cannot be called
  from C#. EXAMPLES.md counts it against Bevy's own examples, and of the 364 that are about a game
  rather than Rust, 126 are written here, 7 of them in part, 120 more can be with what is bridged,
  and 118 wait on something the bridge lacks.
- **Bevy's Rust plugins wait on the bridge.** A crate of Bevy's community is added to a Rust game
  in a line. Here it is added to the bridge, which is then built again, and reached from C# once
  the bridge opens it.
- **A Bevy version at a time.** The bridge is built against one release of Bevy, and a new release
  reaches a game when the bridge is moved onto it and the package that carries it is published.
- **Desktop only.** The package carries the bridge for Linux, Windows and macOS. A browser, a phone
  and a console are not built for.

## What was measured

On one machine, an Intel Core i9-14900HX with an NVIDIA GeForce RTX 4070 Laptop GPU on Vulkan,
under Linux. [PERFORMANCE.md](../.github/PERFORMANCE.md) holds the full tables, how each run is
made and what was changed because of them.

The same drawn scene of a thousand cubes under three lights, written once against Bevy alone and
once as a C# game, with the render schedule in milliseconds a frame, each the median of six runs:

| | Bevy alone | through the bridge |
|---|---:|---:|
| three lights casting shadows | 12.2 | 14.6 |
| no shadows | 3.5 | 4.0 |

```bash
CARGO_TARGET_DIR=native/target cargo build --release --manifest-path native/stress/Cargo.toml
native/target/release/stress 1000 [noshadows] wireframe autoexposure timings
games/Stress/measure.sh drawn 1000
```

How far a C# game goes in a sixtieth of a second, measured with `games/Stress/measure.sh`:

| | at 60 frames a second |
|---|---|
| entities moved each frame by a behavior | between 400,000 and 500,000 |
| cubes drawn, no shadows, one in ten a physics body | between 100,000 and 200,000 |
| cubes drawn, three lights casting shadows | between 50,000 and 100,000, held by the render |

```bash
games/Stress/measure.sh movers 100000 200000 400000 500000
games/Stress/measure.sh drawn 10000 50000 100000 200000
```

What the weather adds to a frame at each tier, at 1280 by 720 over the feature test's hub at noon
under a partly cloudy sky, the broken sky the weather's clouds cost most in, measured as the frame
time against the same view without it. The feature test holds its frame near 16 ms with its own
work, so the two lowest tiers vanish into it:

| tier | added to a frame |
|---|---:|
| Potato and Low | under a millisecond |
| Medium | about 2 ms |
| High | 5 to 9 ms |
| Ultra | 10 to 13 ms |

```bash
cd BevyCSharp.FeatureTest/bin/Debug/net10.0 && ./BevyCSharp.FeatureTest --offscreen --frames 0 --serve --timings
./bcs command setting "WeatherTier High"    # and the frame rate from ./bcs command app.status
```

The workflow's drive of the feature test writes the hub's frame time under the weather's lowest
tier beside its captures, which on its software renderer measures the weather's cost there.

How much of Bevy is reached is [EXAMPLES.md](../.github/EXAMPLES.md)'s count, kept by
`build/examples-table.py` from the table of every one of Bevy's examples.

---

Before this, [How it works](how-it-works.md).
The [guide's contents](../README.md#guide) list every page.
