#![doc = include_str!("../README.md")]

pub mod atmosphere;
pub mod celestial;
pub mod cloud_field;
pub mod cloud_shadows;
pub mod clouds;
pub mod config;
pub mod fog;
pub mod ground;
pub mod math;
pub mod precipitation;
pub mod presets;
pub mod procedural;
pub mod sky;
pub mod state;
pub mod thunder;
pub mod time;
pub mod wind;

use bevy::app::{App, Plugin, PluginGroup, PluginGroupBuilder};
use bevy::ecs::schedule::{IntoScheduleConfigs, SystemSet};
use bevy::prelude::Update;

/// Everything you normally need, in one `use`.
pub mod prelude {
    pub use crate::atmosphere::AtmosphereConfig;
    pub use crate::celestial::{
        CelestialBodies, DayPhase, DayPhaseChanged, MoonLight, MoonLightConfig, SunConfig, SunLight,
    };
    pub use crate::cloud_shadows::CloudShadowConfig;
    pub use crate::clouds::CloudConfig;
    pub use crate::config::{Quality, WeatherCamera, WeatherConfig};
    pub use crate::fog::FogConfig;
    pub use crate::ground::{GroundConditions, GroundConfig};
    pub use crate::precipitation::PrecipitationConfig;
    pub use crate::presets::WeatherPreset;
    pub use crate::procedural::{Climate, ProceduralWeather};
    pub use crate::sky::{GalaxyConfig, MeteorConfig, MoonConfig, StarConfig};
    pub use crate::state::{Weather, WeatherConditions};
    pub use crate::thunder::{LightningStrike, ThunderConfig};
    pub use crate::time::WeatherTime;
    pub use crate::wind::Wind;
    pub use crate::{WeatherPlugin, WeatherPlugins, WeatherSystems};
}

/// Ordered stages the plugin runs in every frame, all inside [`Update`].
///
/// Order your own systems against these when you want to read or override the
/// weather. For example, to hand-drive the sun, run
/// `.after(WeatherSystems::Simulate).before(WeatherSystems::Apply)`.
#[derive(SystemSet, Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub enum WeatherSystems {
    /// Advances [`WeatherTime`](time::WeatherTime).
    Tick,
    /// Runs the procedural driver and blends
    /// [`Weather::current`](state::Weather::current) toward the target.
    Simulate,
    /// Pushes the simulated state into lights, materials, fog and particles.
    Apply,
}

/// The one plugin that wires up the whole weather system.
///
/// ```no_run
/// # use bevy::prelude::*;
/// # use bevy_weather::prelude::*;
/// App::new()
///     .add_plugins((DefaultPlugins, WeatherPlugin::default()))
///     .run();
/// ```
///
/// Add [`WeatherCamera`](config::WeatherCamera) to the camera that should see
/// the weather. Everything else is driven by resources you can mutate at any
/// time: [`WeatherConfig`](config::WeatherConfig),
/// [`WeatherTime`](time::WeatherTime), [`Weather`](state::Weather) and
/// [`ProceduralWeather`](procedural::ProceduralWeather).
#[derive(Debug, Clone, Default)]
pub struct WeatherPlugin {
    /// Initial value for the [`WeatherConfig`](config::WeatherConfig) resource.
    pub config: config::WeatherConfig,
    /// Initial value for the [`WeatherTime`](time::WeatherTime) resource.
    pub time: time::WeatherTime,
    /// Initial value for the [`ProceduralWeather`](procedural::ProceduralWeather) resource.
    pub procedural: procedural::ProceduralWeather,
    /// Weather to start from. Ignored while `procedural.enabled` is true, since
    /// the driver overwrites the target on the first frame.
    pub initial: presets::WeatherPreset,
}

impl Plugin for WeatherPlugin {
    fn build(&self, app: &mut App) {
        // Inserted before `CorePlugin` runs, so its `init_resource` calls see
        // these and leave them alone.
        app.insert_resource(self.config.clone())
            .insert_resource(self.time.clone())
            .insert_resource(self.procedural.clone())
            .insert_resource(state::Weather::new(self.initial.conditions()));

        app.add_plugins((
            CorePlugin,
            time::WeatherTimePlugin,
            state::WeatherStatePlugin,
            procedural::ProceduralWeatherPlugin,
            wind::WindPlugin,
            ground::GroundPlugin,
            celestial::CelestialPlugin,
            atmosphere::WeatherAtmospherePlugin,
            sky::SkyPlugin,
            cloud_shadows::CloudShadowPlugin,
            fog::WeatherFogPlugin,
            precipitation::PrecipitationPlugin,
            thunder::ThunderPlugin,
        ));
    }
}

/// [`WeatherPlugin`] as a [`PluginGroup`], for symmetry with `DefaultPlugins`.
///
/// Lets you disable a subsystem without touching
/// [`WeatherConfig`](config::WeatherConfig):
///
/// ```no_run
/// # use bevy::prelude::*;
/// # use bevy_weather::prelude::*;
/// # use bevy_weather::precipitation::PrecipitationPlugin;
/// # let mut app = App::new();
/// app.add_plugins(WeatherPlugins.build().disable::<PrecipitationPlugin>());
/// ```
#[derive(Debug, Clone, Copy, Default)]
pub struct WeatherPlugins;

impl PluginGroup for WeatherPlugins {
    fn build(self) -> PluginGroupBuilder {
        PluginGroupBuilder::start::<Self>()
            .add(CorePlugin)
            .add(time::WeatherTimePlugin)
            .add(state::WeatherStatePlugin)
            .add(procedural::ProceduralWeatherPlugin)
            .add(wind::WindPlugin)
            .add(ground::GroundPlugin)
            .add(celestial::CelestialPlugin)
            .add(atmosphere::WeatherAtmospherePlugin)
            .add(sky::SkyPlugin)
            .add(cloud_shadows::CloudShadowPlugin)
            .add(fog::WeatherFogPlugin)
            .add(precipitation::PrecipitationPlugin)
            .add(thunder::ThunderPlugin)
    }
}

/// Resources and system-set ordering shared by every other weather plugin.
#[derive(Debug, Clone, Copy, Default)]
pub struct CorePlugin;

impl Plugin for CorePlugin {
    fn build(&self, app: &mut App) {
        app.init_resource::<config::WeatherConfig>()
            // Plain configuration, even though the sky is what draws from it.
            // The celestial and cloud-shadow plugins both read it too, and a
            // resource owned by the rendering plugin would make either of them
            // panic in a build that had disabled the sky.
            .init_resource::<clouds::CloudConfig>()
            .init_resource::<time::WeatherTime>()
            .init_resource::<procedural::ProceduralWeather>()
            .init_resource::<state::Weather>()
            .init_resource::<celestial::CelestialBodies>()
            .init_resource::<wind::Wind>()
            .configure_sets(
                Update,
                (
                    WeatherSystems::Tick,
                    WeatherSystems::Simulate,
                    WeatherSystems::Apply,
                )
                    .chain(),
            );
    }
}
