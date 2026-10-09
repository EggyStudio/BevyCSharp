# bevy_weather

A complete, reusable weather and sky plugin for [Bevy](https://bevy.org) 0.19.

Day/night cycle, procedural stars, a Milky Way, moon phases, volumetric clouds,
volumetric fog, rain, snow, thunder — and a procedural driver that generates all
of it from nothing but the clock.

```toml
[dependencies]
bevy_weather = "0.1"
```

```rust,no_run
use bevy::prelude::*;
use bevy_weather::prelude::*;

fn main() {
    App::new()
        .add_plugins((DefaultPlugins, WeatherPlugin::default()))
        .add_systems(Startup, setup)
        .run();
}

fn setup(mut commands: Commands) {
    commands.spawn((
        Camera3d::default(),
        // Weather is only rendered for cameras you mark.
        WeatherCamera,
        Transform::from_xyz(0.0, 2.0, 0.0).looking_at(Vec3::new(0.0, 2.0, -10.0), Vec3::Y),
    ));
}
```

That is the whole setup. The sun rises, clouds build, it rains, night falls and
the stars come out.

## What it does

| | |
|---|---|
| **Day/night cycle** | Real solar geometry from latitude, axial tilt and date. Seasons and day length fall out of it. |
| **Atmosphere** | Bevy's built-in Bruneton scattering, wired up and quality-scaled. |
| **Stars** | Procedural, with a realistic magnitude distribution, colour temperature and horizon-weighted scintillation. Fixed to the celestial sphere, so they rotate about the pole and drift by sidereal time. |
| **Galaxy** | Procedural Milky Way with dust lanes and a central bulge, at the correct angle to the celestial equator. |
| **Moon** | Phase-correct, because the terminator is computed from real geometry rather than masked. Lunar-Lambert shading, so a full moon reads as a flat disc rather than a shaded ball. Procedural maria, craters and earthshine. |
| **Meteors** | Shooting stars on great-circle paths, a pure function of the clock, so every client sees the same one in the same place. |
| **Volumetric clouds** | Raymarched through a curved planetary shell, with light marching, Henyey–Greenstein phase, powder and wind advection. |
| **Cloud shadows** | The deck's own shape, projected onto the ground along the sun as a light cookie, drifting with the same wind. |
| **Volumetric fog** | Bevy's fog volumes and god rays, driven by weather and time of day. |
| **Rain and snow** | GPU-resident particle fields. One draw call each, tens of thousands of particles, world-anchored. |
| **Thunder** | Poisson-scheduled strikes with multi-stroke flash envelopes, scene lighting, in-cloud glow and speed-of-sound thunder delay. |
| **Procedural weather** | Climate-driven, and a pure function of the clock. |

## The procedural driver

Weather is generated from the clock, not accumulated frame to frame:

```rust,no_run
# use bevy::prelude::*;
# use bevy_weather::prelude::*;
# let mut app = App::new();
app.add_plugins(WeatherPlugin {
    procedural: ProceduralWeather {
        climate: Climate::OCEANIC,
        seed: 1234,
        // Roughly how many weather systems pass per in-game day.
        systems_per_day: 1.5,
        ..default()
    },
    time: WeatherTime {
        latitude: 57.0,
        day_length_secs: 600.0,
        ..default()
    },
    ..default()
});
```

Because it is a pure function, the same clock and seed always give the same sky:
on every machine, after a save/load, and on every client in a multiplayer game.
It also means you can ask what the weather *will* be:

```rust
# use bevy_weather::prelude::*;
# let driver = ProceduralWeather::default();
# let now = WeatherTime::default();
let this_evening = driver.forecast(&now, 8.0);
if this_evening.rain > 0.5 {
    // tell the player to pack a coat
}
```

Climates ship for temperate, oceanic, arid, tropical, Mediterranean, polar and
desert regions, and `Climate` is a plain struct if none of them fit.

## Driving it yourself

### From your own timeline

Set `paused` and write `time_of_day` — the "time progression ratio" — from
whatever drives your game:

```rust,no_run
# use bevy::prelude::*;
# use bevy_weather::prelude::*;
fn scrub(mut time: ResMut<WeatherTime>, cutscene_progress: f32) {
    time.paused = true;
    time.time_of_day = cutscene_progress; // 0.0 midnight, 0.5 noon
}
```

### Manual weather

Turn the driver off and set conditions directly. Presets ease in over
`Weather::transition_half_life`:

```rust,no_run
# use bevy::prelude::*;
# use bevy_weather::prelude::*;
fn make_it_rain(mut procedural: ResMut<ProceduralWeather>, mut weather: ResMut<Weather>) {
    procedural.enabled = false;
    weather.set(WeatherPreset::Thunderstorm);
}
```

Or build conditions field by field — nothing forces you into a preset:

```rust
# use bevy_weather::prelude::*;
let mut thundersnow = WeatherPreset::Thunderstorm.conditions();
thundersnow.rain = 0.0;
thundersnow.snow = 1.0;
thundersnow.temperature = -8.0;
```

### Thunder

The plugin owns the timing; you supply the sound. Strikes are scheduled as a
Poisson process, and each clap is delayed by the time sound actually takes to
reach the camera, then attenuated for distance:

```rust,no_run
# use bevy::prelude::*;
# use bevy_weather::prelude::*;
# #[cfg(feature = "audio")]
fn setup_thunder(mut thunder: ResMut<ThunderConfig>, assets: Res<AssetServer>) {
    thunder.sound = Some(assets.load("sounds/thunder.ogg"));
}
```

Requires the `audio` feature (on by default). Without a sound set, nothing is
played and you can drive audio yourself from the message below.

### Reacting to lightning

```rust,no_run
# use bevy::prelude::*;
# use bevy_weather::prelude::*;
fn on_strike(mut strikes: MessageReader<LightningStrike>) {
    for strike in strikes.read() {
        // `thunder_delay` is already computed from the speed of sound.
        println!("strike {:.0}m away, thunder in {:.1}s", strike.distance, strike.thunder_delay);
    }
}
```

### Reacting to nightfall

```rust,no_run
# use bevy::prelude::*;
# use bevy_weather::prelude::*;
fn street_lights(mut changes: MessageReader<DayPhaseChanged>) {
    for change in changes.read() {
        if change.to.is_dark() && !change.from.is_dark() {
            // Dusk. Light the lamps, close the shops, wake whatever hunts.
        }
    }
}
```

`DayPhase` is also a resource if you would rather poll than listen, and
`WeatherTime` will tell you when to expect the change:

```rust,no_run
# use bevy_weather::prelude::*;
# let time = WeatherTime::default();
// `None` inside the polar circles, where the sun may not cross the horizon.
let sunset = time.sunset_hour();
let hours = time.daylight_hours();
```

### What the weather left on the ground

`GroundConditions` is the running total the sky does not give you: ground is wet
because it rained ten minutes ago, and stays wet for a while after it stops.
Nothing here renders from it — it is for your materials, audio and physics.

```rust,no_run
# use bevy::prelude::*;
# use bevy_weather::prelude::*;
fn wet_roads(ground: Res<GroundConditions>, mut materials: ResMut<Assets<StandardMaterial>>) {
    for (_, material) in materials.iter_mut() {
        material.perceptual_roughness = 0.9 - 0.55 * ground.wetness;
    }
}
```

Snow only lies near or below freezing, and melts into `wetness` rather than
vanishing — so a snowfall into a warm afternoon leaves the ground soaked instead
of white.

## Configuration

Every subsystem has its own resource, all mutable at runtime:

| Resource | Controls |
|---|---|
| `WeatherConfig` | Master on/off switches, quality, unit scale |
| `WeatherTime` | Clock, latitude, axial tilt, moon phase offset |
| `Weather` | Current and target conditions |
| `ProceduralWeather` | The driver, climate and seed |
| `AtmosphereConfig` | Scattering density, exposure, tonemapping, bloom |
| `SunConfig` | Sun disc size and brightness |
| `StarConfig`, `GalaxyConfig`, `MoonConfig` | The night sky |
| `MeteorConfig` | Shooting star rate, length and brightness |
| `CloudConfig` | Cloud look and raymarch cost |
| `CloudShadowConfig` | Ground shadows cast by the cloud deck |
| `FogConfig` | Fog colour, visibility range and volume size |
| `GroundConfig` | How fast the ground wets, dries, gathers snow and thaws |
| `PrecipitationConfig` | Particle counts, sizes, speeds |
| `MoonLightConfig` | Moonlight brightness, colour and shadows |
| `ThunderConfig` | Strike rate, distance, flash |

`WeatherConfig::quality` sets sensible sample counts for everything at once;
each subsystem can still override its own.

## Ordering your systems

```rust,no_run
# use bevy::prelude::*;
# use bevy_weather::prelude::*;
# let mut app = App::new();
# fn my_system() {}
app.add_systems(
    Update,
    my_system
        .after(WeatherSystems::Simulate)
        .before(WeatherSystems::Apply),
);
```

`Tick` advances the clock, `Simulate` produces the weather, `Apply` pushes it
into lights, materials, fog and particles.

## How the sky is put together

Worth knowing if you plan to extend it, because the ordering is not arbitrary.

Bevy 0.19 renders its atmosphere as a full-screen pass between the opaque and
transparent passes, compositing as `destination * transmittance + inscattering`.
This plugin draws on both sides of it:

* **Stars, galaxy and moon** are drawn in the **opaque** pass, at the far plane,
  with depth writes off. The atmosphere then composites over them, so they are
  extinguished by exactly the right amount of air with no code on our side.
* **Clouds** are drawn in the **transparent** pass, *after* the atmosphere, with
  their own aerial perspective. They have to be: the atmosphere adds the
  inscattering of the whole column up to space as though the cloud were not
  there, so a deck drawn underneath it can never be darker than the clear sky —
  an overcast day would come out brighter than a sunny one.

Both are one full-screen triangle with no model transform, so a single entity
serves every camera and there is nothing to keep in sync with camera movement.

Everything the shaders emit is in physical radiance and is multiplied by
`view.exposure`, matching the atmosphere.

## Notes and limitations

* **Exposure.** The atmosphere is calibrated in physical units, so
  `AtmosphereConfig` sets `Exposure { ev100: 13.0 }` and ACES tonemapping on
  weather cameras by default. Set `exposure_ev100: None` to manage exposure
  yourself. Around sunrise and sunset it opens up by
  `AtmosphereConfig::twilight_exposure_lift` stops and closes again once it is
  properly dark — a stand-in for the adaptation that makes an afterglow look
  like something rather than like the near-black a fixed daylight exposure
  records.
* **Haze is a dial.** `AtmosphereConfig::aerosol_density` scales the Mie layer
  on its own. Aerosols sit in the lowest kilometre or two and scatter strongly
  forward without much colour preference, so at a low sun they take the reddened
  beam and spread it across a wide arc of sky. It is the difference between a
  thin orange line on the horizon and half the sky going gold; the default is a
  little hazier than clean air for exactly that reason.
* **The sun and moon are drawn larger than life.** Both are half a degree
  across in reality — about twenty pixels at a typical field of view, too few to
  show a lunar phase at all. `SunConfig::angular_radius` and
  `MoonConfig::angular_radius` default to a little over twice that, and both
  accept `DEFAULT_ANGULAR_RADIUS` if you want the true size.
* **Glare comes from bloom, not from the sky shader.** `AtmosphereConfig::bloom`
  is on by default and is doing most of the work of making the sun look like a
  sun. A halo baked into the sky instead cannot be occluded by anything, so it
  would shine straight through cloud that should have hidden it.
* **The night sky is deliberately exaggerated.** A point star has no meaningful
  radiance at raster resolution — the value depends entirely on the solid angle
  of a pixel — and a real moonlit night is a fraction of a lux, which at a fixed
  daylight exposure is indistinguishable from black. So stars, the galaxy and
  the night ambient are all scaled well past life, and faded out explicitly
  across twilight rather than being drowned by daylight on their own.
* **Clouds are a sky-layer effect.** They are raymarched through a curved
  planetary shell above the camera and read correctly from the ground or a hill.
  Flying *through* the layer works but is not the case it is tuned for.
* **Cloud shadows go through a light cookie.** The deck's shape is evaluated on
  the CPU from the same field the sky shader marches and projected along the
  sun with `DirectionalLightTexture`, so no extra render pass is involved. That
  machinery is Bevy's clustered decals, which need the `pbr_light_textures`
  feature (this crate enables it) and a GPU with texture binding arrays. Where
  either is missing the texture is simply never sampled and lighting is normal,
  just unshadowed. The pattern repeats every `CloudShadowConfig::tile_size`.
* **Sunset colour comes from the atmosphere, not from the light.** Bevy's
  atmosphere integrates the transmittance from the sun itself, so it expects the
  raw solar spectrum — the same way `illuminance` is the raw 128 klx rather than
  what reaches the ground. Hand it a pre-reddened light and the extinction is
  applied twice, and since Rayleigh scattering is six times stronger in blue
  than in red, scattering an already-orange sun gives a muddy olive sky rather
  than an orange one. `SunConfig::light_tint` splits the difference: enough
  warmth on the key light for a golden hour, little enough that the sky stays
  the atmosphere's to colour.
* **Fog is two effects.** `DistanceFog` and a matching fade in the sky shader do
  the visibility work, because they blend toward a colour — which is what makes
  heavy fog a white-out rather than a black-out. The volumetric pass adds god
  rays on top at a much lower density. Bevy's fog volumes have hard faces, so
  keep `FogConfig::volume_size` comfortably larger than your visibility distance
  or you will see the box.
* **Volumetric fog needs WebGPU** on the web; the `WebGL2` backend cannot run it.
  Turn `volumetric_fog` off there; `distance_fog` alone still looks right.
* **The sky renders for every 3D camera.** If you have a minimap or
  render-to-texture camera that must not see it, put the sky entities on their
  own render layer.

## Releasing

Publishing is driven from a tag. Set the version, commit, tag and push:

```bash
cargo set-version 0.2.0        # or edit Cargo.toml by hand
cargo update -p bevy_weather   # keep Cargo.lock in step
git commit -am "release 0.2.0"
git tag v0.2.0
git push && git push --tags
```

`.github/workflows/release.yml` then checks that the tag and the manifest agree,
runs the tests once more, and publishes. It needs a repository secret called
`CARGO_REGISTRY_TOKEN`, from [crates.io/settings/tokens]; scope it to this crate
and to publish only. Run the workflow by hand first with `dry_run` left on if
you want to rehearse it.

Every push also runs `cargo publish --dry-run`, because packaging fails for
reasons an ordinary build never surfaces — missing metadata, a file the
`exclude` list quietly dropped — and a tag is the worst moment to find out.

[crates.io/settings/tokens]: https://crates.io/settings/tokens

## Feature flags

* `audio` *(default)* — pulls in `bevy_audio` so the plugin can play a thunder
  clap you supply, delayed and attenuated for distance. Without it,
  `LightningStrike` still carries `thunder_delay` so you can do it yourself.

## Examples

```bash
cargo run --example showcase --release
```

An interactive tour: fly around with `WASD` and the mouse, scrub the clock,
cycle presets and climates, and toggle each subsystem. The on-screen panel lists
the keys and shows the live weather state. `8` turns the meteor rate up to a
shower, since at the honest rate you could watch for a long time without seeing
one.

### Building for Windows from macOS or Linux

```bash
rustup target add x86_64-pc-windows-gnu
brew install mingw-w64          # or your distribution's mingw-w64 package
cargo build --release --target x86_64-pc-windows-gnu --example showcase
```

`.cargo/config.toml` points the GNU target at the mingw linker. The MSVC target
needs the Microsoft linker and Windows SDK, so it cannot be cross-compiled.

The resulting `.exe` is self-contained — every shader is embedded in the binary,
the examples load nothing from disk, and it links only against system DLLs.

## Performance

Frame time is always on screen in the showcase, and `B` runs a sweep that turns
each subsystem on in turn and reports what it costs:

```bash
cargo run --release --example showcase -- --bench
```

Run it in release and **leave the window focused**. An unfocused window is
pinned to the display refresh by the compositor, which flattens every
measurement below 16.7 ms onto the same number — including measurements of
configurations that are genuinely cheaper, which is how the problem gives itself
away.

Almost all the cost is the cloud raymarch, and it is per-pixel: it scales with
resolution and with how much of the screen is sky, so looking straight up is the
worst case.

Cloud cost is not a single number. A clear sky skips the raymarch entirely, and
a solid overcast deck extinguishes each ray within a few samples — the expensive
case is the **broken sky in between**, where rays neither miss the cloud nor
terminate early inside it. That is also exactly what you pass through whenever
the weather changes, so a transition from clear to overcast costs more than
either end of it. The benchmark measures the whole curve.

### Quality tiers

`Quality` is meant to be bound straight to a graphics menu. `Quality::ALL` gives
you the entries in order and `Quality::name()` labels them:

| Tier | Cloud steps | Light march | Erosion | Particles |
|---|---|---|---|---|
| `Potato` | 12 | — | off | 1 200 |
| `Low` | 20 | — | on | 4 000 |
| `Medium` | 36 | 3 | on | 12 000 |
| `High` | 64 | 5 | on | 30 000 |
| `Ultra` | 128 | 8 | on | 60 000 |

Settings resolve in three levels, most specific first:

1. **An explicit count** — `CloudConfig::steps = Some(48)`. Always wins.
2. **A subsystem tier** — `CloudConfig::quality = Some(Quality::Low)`, for a menu
   with separate sliders for clouds, precipitation, fog and sky.
3. **The global tier** — `WeatherConfig::quality`.

So the one-line version is `config.quality = Quality::Low`, and nothing you set
by hand is ever overwritten.

```rust,no_run
# use bevy::prelude::*;
# use bevy_weather::prelude::*;
# use bevy_weather::clouds::CloudConfig;
fn apply_graphics_settings(
    mut config: ResMut<WeatherConfig>,
    mut clouds: ResMut<CloudConfig>,
    chosen: Quality,
) {
    config.quality = chosen;
    // Or pin one subsystem, leaving the rest on the global dial:
    clouds.quality = Some(Quality::High);
}
```

In rough order of leverage:

| Lever | |
|---|---|
| `WeatherConfig::quality` | One dial for everything; see the table above. |
| `CloudConfig::steps` | The single biggest number in the plugin. |
| `CloudConfig::light_steps` | `Some(0)` swaps the per-sample light march for an analytic approximation. Clouds lose some of their internal shadowing and get noticeably cheaper. |
| `CloudConfig::detail_distance` | How far out erosion detail is still worth computing. Lower it to buy back time on a broken sky, where much of the screen is distant cloud near the horizon. |
| `WeatherConfig::clouds` | Off is free. A clear sky already costs nothing — the raymarch is skipped when coverage is zero. |
| `AtmosphereConfig::environment_map_size` | Regenerated every frame; 512 buys nothing over 128 for ambient light. |
| `CloudShadowConfig::resolution` | The shadow texture is rebuilt on the CPU: about 1.5 ms at 64, 6 ms at 128, 20 ms at 256, spread across the frames of `update_seconds` rather than landing on one. |
| `CloudShadowConfig::enabled` | Off is free. |
| `MoonLightConfig::shadows` | A second set of cascaded shadow maps, rendered only while the moon is actually contributing. |
| `PrecipitationConfig::particle_count` | Fill-rate bound, so it also scales with `box_size`. |

`CloudConfig::adaptive_marching` exists only so the fast path can be measured
against the slow one — leave it on.

## License

MIT OR Apache-2.0, at your option.
