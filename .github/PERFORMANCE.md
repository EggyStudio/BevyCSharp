# What a frame holds

What the engine spends a frame on, measured rather than guessed, and where that cost lies. A C#
engine over Bevy has a cost Bevy alone does not, the crossing between managed code and the bridge,
so a frame is split here into the managed systems, the crossings they make, Bevy's own schedule
and the render.

## How it is measured

`frame.profile [frames]` measures a span of frames in a running app and answers once they have
run, from the console, the editor's console or `./bcs command`. It reports a frame's average in
four parts.

- **the frame**, from the top of one to the top of the next;
- **the schedule**, Bevy's main schedule from the top of `First` to the end of `Last`, split into
  the managed systems (C# systems, by name) and Bevy's own work around them;
- **the crossings**, every call a managed system makes into the bridge, counted, each costed as a
  call that does nothing (the way in and out, timed from C# when the measurement starts) plus its
  own time inside the bridge (one call in 32 timed and scaled up);
- **the render**, Bevy's render schedule, which runs beside the main one on a thread of its own,
  split into its phases (prepare, queue, render and the rest), with the passes the GPU timings name
  where the app asked for them (`Config.GpuTimings`).

`FrameProfile.Start`, `Take` and `Stop` are the same from code. Counting costs something on every
crossing, so a measured frame is slower than the same frame unmeasured: 7.0 ms against 6.2 ms at a
hundred thousand crossings a frame, about a tenth.

`games/Stress` is the load, built on the packed package as a game is, in two modes.

- `Stress movers <count>` moves entities each frame through their `Transform` from a behavior, with
  no renderer, so a frame is the managed systems, their crossings and Bevy's schedule;
- `Stress drawn <count>` draws cubes in four materials under a directional light and two point
  lights, one cube in ten a physics body fallen onto a floor, offscreen at 1280 by 720.

`games/Stress/measure.sh <mode> <counts…>` runs one at each count, lets it settle for 300 frames,
measures 240, and stops two counts after a frame passes a sixtieth of a second. `STRESS_SHADOWS=0`
turns the lights' shadows off.

## The machine

An Intel Core i9-14900HX (32 threads), an NVIDIA GeForce RTX 4070 Laptop GPU on Vulkan with
driver 615.71.09, 62 GB of memory, Linux 7.2 (Fedora 44), .NET 10.0.112, the render profile of
the bridge in release, unpaced (`HeadlessFps` zero), October 2026.

## The numbers

Milliseconds a frame. *Managed* is the managed systems less their crossings, *crossings* their
calls into the bridge, *Bevy* the schedule less the managed systems, and *render* the render
schedule, which runs beside the schedule, so a frame is about the longer of the two.

### Movers

| count | frame | managed | crossings (a frame) | Bevy | render |
|---:|---:|---:|---:|---:|---:|
| 1,000 | 0.70 | 0.09 | 0.02 (1,008) | 0.55 | none |
| 10,000 | 2.72 | 0.66 | 0.22 (10,008) | 1.70 | none |
| 50,000 | 4.28 | 1.07 | 1.03 (50,008) | 2.07 | none |
| 100,000 | 7.03 | 1.93 | 2.08 (100,008) | 2.89 | none |
| 200,000 | 12.66 | 3.62 | 4.44 (200,009) | 4.46 | none |
| 250,000 | 16.37 | 5.63 | 5.15 (250,009) | 5.45 | none |
| 300,000 | 18.40 | 7.15 | 5.14 (300,009) | 5.97 | none |
| 400,000 | 24.94 | 8.33 | 8.83 (400,010) | 7.63 | none |

A frame leaves 60 a second at about **250,000 movers**. A crossing costs 16 to 28 ns with the way
in and out, about as much as the C# work for the entity it reaches.

### Drawn, three lights casting shadows

| count | frame | managed | crossings (a frame) | Bevy | render |
|---:|---:|---:|---:|---:|---:|
| 1,000 | 13.09 | 0.40 | 0.05 (516) | 2.97 | 12.34 |
| 5,000 | 14.22 | 1.23 | 0.07 (2,753) | 3.57 | 13.42 |
| 10,000 | 15.86 | 1.98 | 0.24 (6,128) | 4.24 | 14.97 |
| 20,000 | 15.53 | 2.88 | 0.31 (11,965) | 4.89 | 14.56 |
| 50,000 | 20.68 | 8.24 | 0.85 (39,642) | 7.32 | 18.14 |

A frame leaves 60 a second between **20,000 and 50,000 drawn cubes**, and costs 13 ms already at a
thousand.

### Drawn, no shadows

| count | frame | managed | crossings (a frame) | Bevy | render |
|---:|---:|---:|---:|---:|---:|
| 1,000 | 4.43 | 0.21 | 0.02 (181) | 2.17 | 3.77 |
| 5,000 | 4.91 | 0.87 | 0.04 (1,029) | 2.26 | 3.85 |
| 10,000 | 5.96 | 1.24 | 0.05 (2,432) | 2.77 | 4.57 |
| 20,000 | 7.86 | 2.16 | 0.16 (6,179) | 3.68 | 5.71 |
| 50,000 | 14.43 | 6.10 | 0.58 (27,764) | 6.17 | 8.47 |
| 100,000 | 53.64 | 36.74 | 4.28 (206,032) | 10.86 | 15.21 |

A frame leaves 60 a second between **50,000 and 100,000 drawn cubes**.

## The three largest costs

1. **Shadows, in Bevy's render.** Three shadow-casting lights, two of them points with a cube of six
   faces each, cost 8 to 10 ms of the render schedule at any count, almost all of it in its
   `render` phase (10.6 ms of 13.8 at a thousand cubes), where Bevy encodes and submits a pass for
   every shadow view. The GPU timings do not name those passes, and the GPU itself spends under
   2 ms. It is Bevy's work, and none of it crosses into C#. A game holds it down by which lights
   cast shadows.
2. **The physics plugin's sync of the bodies a level holds.** `PhysicsWorld.Sync` reads every body's
   `RigidBody`, `Collider` and `Transform` each fixed step to see whether anything changed, which at
   5,000 bodies is 6.6 ms of the 9.4 the physics system takes, and grows with the bodies whether or
   not any changed. Most of the drawn runs' managed time is this, and at 100,000 cubes without
   shadows (10,000 bodies) it is most of 37 ms. It is the largest cost this repository adds, and
   the one to take next.
3. **A behavior reaching a second component per entity.** Moving an entity's `Transform` from a
   behavior is a crossing per entity, as the world is reached through the bridge, and each costs
   about what the C# work for the entity does. At 250,000 movers that is 5.2 ms of crossings beside
   5.6 ms of C#. Bevy's own schedule around them, which propagates the transforms that moved, is
   another 5.5 ms.

## What measuring turned up

- **A behavior method over many entities could not reach the world, and said the wrong thing.**
  Past 4,096 entities a method per entity is spread across worker threads, where `ctx.Ecs` is not
  on loan. `GetRef` there reported that the entity did not carry the component. It says that the
  world is not on loan, and how to write it instead. The README's hot reload example did exactly
  this, and moves its transform from a static method over a query.
- **The GPU timings leave the shadow passes out**, so the render's phases are timed as well, which
  is where a cost the passes do not show is found.
