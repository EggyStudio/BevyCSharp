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
   2 ms. Measured beside the same scene in Bevy alone (below), about 8.7 ms of it is Bevy's and
   about 2 ms the bridge's. A game holds it down by which lights cast shadows.
2. **The physics plugin's sync of the bodies a level holds**, since taken (below). `PhysicsWorld.Sync` reads every body's
   `RigidBody`, `Collider` and `Transform` each fixed step to see whether anything changed, which at
   5,000 bodies is 6.6 ms of the 9.4 the physics system takes, and grows with the bodies whether or
   not any changed. Most of the drawn runs' managed time is this, and at 100,000 cubes without
   shadows (10,000 bodies) it is most of 37 ms. It is the largest cost this repository adds, and
   the one to take next.
3. **A behavior reaching a second component per entity**, since taken (below). Moving an entity's `Transform` from a
   behavior is a crossing per entity, as the world is reached through the bridge, and each costs
   about what the C# work for the entity does. At 250,000 movers that is 5.2 ms of crossings beside
   5.6 ms of C#. Bevy's own schedule around them, which propagates the transforms that moved, is
   another 5.5 ms.

## What was done about them

### The physics sync reads what changed

`PhysicsWorld.Sync` asked after every body's `RigidBody`, `Collider` and `Transform` each fixed
step, a few calls into the bridge a body. It asks the world instead for the bodies and colliders
added or changed since it last ran (`bcs_ecs_changed_since`), and asks after the bodies it has
made all at once, whether each still has both components and whether its transform was written
(`bcs_ecs_has_many`), so a step in which nothing changed costs a handful of calls. The step asks
after every body's entity the same way, and writes back only the bodies the simulation moved, as
it did before, so a body at rest costs nothing.

At 50,000 drawn cubes with shadows (5,000 bodies) the sync went from 6.6 ms to 0.36 ms and the
physics system from 9.4 ms to 2.8 ms, which is now Bepu's step and the write back of what moved.

| count | frame before | frame after | managed before | managed after | crossings before | crossings after |
|---:|---:|---:|---:|---:|---:|---:|
| 1,000, shadows | 13.09 | 14.61 | 0.40 | 0.39 | 516 | 14 |
| 10,000, shadows | 15.86 | 14.45 | 1.98 | 1.09 | 6,128 | 31 |
| 50,000, shadows | 20.68 | 16.15 | 8.24 | 2.08 | 39,642 | 27 |
| 100,000, shadows | none | 24.36 | none | 6.52 | none | 17 |
| 10,000 | 5.96 | 5.55 | 1.24 | 0.84 | 2,432 | 119 |
| 50,000 | 14.43 | 9.25 | 6.10 | 1.98 | 27,764 | 160 |
| 100,000 | 53.64 | 14.21 | 36.74 | 3.08 | 206,032 | 19 |
| 200,000 | none | 35.50 | none | 13.40 | none | 22 |

Without shadows a frame leaves 60 a second between 100,000 and 200,000 drawn cubes, where it was
between 50,000 and 100,000. With them the render, which runs beside the schedule, is still what
holds a frame at 13 to 15 ms.

### The shadows, beside Bevy alone

`native/stress` is the drawn scene written against Bevy with no bridge and no C#, on the same Bevy
version and the bridge's render features (it depends on the bridge crate, which pins them), drawn
into an image of the same size and format, unpaced, with the plugins the bridge adds beyond Bevy's
defaults (wireframes, auto exposure, render timings) and the same clocks around the render
schedule and its phases:

```bash
CARGO_TARGET_DIR=native/target cargo build --release --manifest-path native/stress/Cargo.toml
native/target/release/stress 1000 [noshadows] wireframe autoexposure timings
```

The run-to-run spread on this laptop is about 1.5 ms either way, as the processor's clock moves,
so each figure is the median of six runs, the plain scene and the bridge's interleaved. At 1,000
cubes, the render schedule in milliseconds:

| | Bevy alone | through the bridge |
|---|---:|---:|
| three lights casting shadows | 12.2 | 14.6 |
| no shadows | 3.5 | 4.0 |

So the shadows are Bevy's cost, about 8.7 ms of its own render, nearly all in its `render` phase
(8.5 ms of a plain frame against 10.0 through the bridge), where a pass is encoded for each of the
sixteen shadow views (four cascades, and six cube faces for each point light) over every caster.
The places a bridge could make it worse were checked and are as Bevy has them. The shadow maps are
Bevy's default sizes, since the bridge sets them only when a game asks. The cubes share one mesh
and four materials and batch as they do in plain Bevy. No debug or validation setting is on, as
the renderer is made from `WgpuSettings::default()` in a release build. The bridge's own shadow
drawing returns before encoding anything where no shader material casts a shadow.

What is left is about 2 ms the bridge adds to a shadowed render and 0.5 ms to an unshadowed one,
growing with the shadow views. Adding the bridge's render installers to the plain scene one at a
time (`material`, `views`, `probes`, `watch`, `corners`, `compute`, `rays`, `layers` as arguments)
moved it by less than the spread, so it was not placed on this machine. With `timings` the plain
program also lists each pass Bevy times, as the bridge's `render.timings` does, and the two lists
agree to a hundredth of a millisecond. Bevy does not time its shadow passes, so the difference is
in the part of the `render` phase no timed pass covers, and over three interleaved runs it was 0
to 1.6 ms, inside the spread again. A quieter machine, or Bevy's tracing spans in a profiler,
would place it.

### A behavior takes its entity's transform

A behavior method can take its entity's other components after its context, `ref` to write one or
`in` to read it, handed from the same storage as its own. The runner lists the behavior's
storage runs and each other component's under the same filters, checks they are the same rows,
and walks them together. Moving a transform was a call into the bridge an entity, and is a few
calls a system whatever the count. `Stress movers` moves its transforms that way.

| count | frame before | frame after | managed before | managed after | crossings before | crossings after |
|---:|---:|---:|---:|---:|---:|---:|
| 1,000 | 0.70 | 0.82 | 0.09 | 0.11 | 1,008 | 9 |
| 10,000 | 2.72 | 1.35 | 0.66 | 0.27 | 10,008 | 9 |
| 100,000 | 7.03 | 3.07 | 1.93 | 0.52 | 100,008 | 9 |
| 200,000 | 12.66 | 5.39 | 3.62 | 0.79 | 200,009 | 10 |
| 300,000 | 18.40 | 6.91 | 7.15 | 1.08 | 300,009 | 10 |
| 400,000 | 24.94 | 8.97 | 8.33 | 1.45 | 400,010 | 10 |
| 500,000 | none | 20.03 | none | 3.60 | none | 12 |

A frame leaves 60 a second between 400,000 and 500,000 movers, where it was about 250,000. What is
left is Bevy's schedule, mostly propagating the transforms that moved, 7 ms at 400,000. Past a
few thousand entities the method runs across the thread pool, which a method reaching the world
through `ctx.Ecs` could not.

### Wireframes only where an app asks

The bridge added Bevy's wireframe plugins, for 3D meshes and 2D ones, to every app with a renderer,
and each looks at every mesh of its kind every frame. They are added where an app's
`Config.Wireframes` asks, as Bevy's own wireframe examples add them, and `Render.SetWireframe`
refuses to turn a wireframe on where it did not, rather than drawing nothing. The editor asks.

| test | Bevy alone, before | through the bridge, before | Bevy alone, after | through the bridge, after |
|---|---:|---:|---:|---:|
| many_sprite_meshes | 4.87 | 13.96 | 5.23 | 5.52 |
| many_materials | 6.89 | 7.10 | 7.01 | 7.64 |
| many_cameras_lights | 42.16 | 46.19 | 43.27 | 44.98 |
| many_lights | 6.98 | 6.66 | 6.60 | 7.30 |

The 2D test lost what the plugin cost it. The 3D ones moved within the spread between runs, as
the earlier measurement of the drawn scene found the 3D plugin costing less than the spread at a
thousand cubes.

### A run of lines in an array kept between calls

`Gizmos.Lines` built a run longer than 64 lines in an array made for the call. It builds it in
one kept between calls, grown when a run is longer than any before.

| test | Bevy alone, before | through the bridge, before | Bevy alone, after | through the bridge, after |
|---|---:|---:|---:|---:|
| many_gizmos | 6.54 | 12.39 | 6.97 | 10.68 |

The ten systems drawing the lines take 3.9 ms of the frame between them, where they took 4.9, 0.7
of it in their calls into the bridge either way.

## Bevy's stress tests, beside Bevy

Bevy's stress tests are written in C# in `BevyCSharp.Examples/stress_tests`, and each is measured
beside Bevy's own program, built from its source at the version the bridge builds, on the
bridge's render profile, by `build/bevy-stress.sh`. Both are drawn as an offscreen run of the
bridge draws, unpaced, into an image of the size the test asks for, 1920 by 1080. Bevy's program
has its window left out and its cameras pointed at the image by the bridge's own code, so what
differs between the two is the bridge and the C#. `build/measure-stress.sh <test> [arguments]`
runs the two in turn three times, lets each settle for 300 frames and measures the next 240, and
gives the median of each, in milliseconds a frame. Bevy's program also prints its render
schedule's phases, as frame.profile prints the bridge's, and `BEVY_STRESS_WITH` adds what the
bridge adds to every app to Bevy's program by name, which is how a difference is placed.

| test, with its own defaults | Bevy alone | through the bridge |
|---|---:|---:|
| many_animated_sprite_meshes | 14.57 | 213.24 |
| many_animated_sprites | 6.64 | 15.95 |
| many_cameras_lights | 43.27 | 44.98 |
| many_gizmos | 6.97 | 10.68 |
| many_glyphs | 18.60 | 20.86 |
| many_gradients | 4.83 | 5.74 |
| many_lights | 6.60 | 7.30 |
| many_materials | 7.01 | 7.64 |
| many_sprite_meshes | 5.23 | 5.52 |
| many_sprites | 3.89 | 4.69 |
| many_text | 16.83 | 20.90 |
| many_text2d | 8.68 | 9.55 |
| text_pipeline | 3.76 | 4.34 |

The rows are as they stand since the mends below. Most cost the bridge under a millisecond more,
or a few against a frame of tens. Four cost far more when first measured, each for a reason
measuring found, and one of them has since been mended.

- **many_sprite_meshes cost 9 ms more for Bevy's 2D wireframe plugin**, which the bridge added to
  every app so a 2D mesh could be outlined, and which looks at every 2D mesh in its prepare and
  queue phases whether or not any is outlined. Bevy's program with it added
  (`BEVY_STRESS_WITH=wireframe2d`) went from 5.12 ms to 12.98, its prepare meshes from 0.18 to 3.59
  and its queue from 0.10 to 4.33, which was the bridge's frame. A sprite mesh is a 2D mesh to
  Bevy, and a sprite is not, which is why many_sprites did not pay it. The plugins are now added
  only where an app asks (above).
- **many_gizmos cost 6 ms more for a copy of every line.** `Gizmos.Lines` takes a run of lines in
  one call, and built a new array of the bridge's layout for every call, ten of 5,000 lines a
  frame here, each on .NET's large object heap. The array is now kept between calls (above), which
  took 1.7 ms off. What is left is the copy itself, each line written into the bridge's general
  description of a shape, about 200 bytes, which a call taking lines as they are would not need.
- **many_animated_sprites, 9 ms more, is a call a frame turned.** Bevy moves a sprite's atlas index
  where it is. The bridge has no call that does only that, so each sprite whose timer finished is
  set again through `Render2d.SetSprite`, about 18,000 a frame, which is 11.3 ms of the frame.
- **many_animated_sprite_meshes, 198 ms more, is the same done through reflection, and it feeds
  itself.** The frame is written through the sprite mesh's reflected atlas, two calls a sprite,
  and once frames are slow every timer of a tenth of a second finishes every frame, so all
  102,400 sprites are written each frame, 614,000 calls, where Bevy writes the 15 in 100 whose
  timers finished.

## What measuring turned up

- **A behavior method over many entities could not reach the world, and said the wrong thing.**
  Past 4,096 entities a method per entity is spread across worker threads, where `ctx.Ecs` is not
  on loan. `GetRef` there reported that the entity did not carry the component. It says that the
  world is not on loan, and how to write it instead. The README's hot reload example did exactly
  this, and moves its transform from a static method over a query.
- **The GPU timings leave the shadow passes out**, so the render's phases are timed as well, which
  is where a cost the passes do not show is found.
- **A request to a running app gave up after half a minute whatever its caller said.** A command
  over many frames of an app that draws slowly, frame.profile over a stress test, was answered
  with a timeout at 30 seconds although `--timeout` asked for longer. The request carries how long
  its caller will wait, and the app waits that long, up to an hour.
- **Setting a reflected `Option` that was already set reset what the wrapper does not hold.**
  Writing a sprite's texture atlas through its wrapper switched the field to `Some` again, which
  makes the variant anew with its defaults and loses the layout. A field already `Some` is written
  in place.
