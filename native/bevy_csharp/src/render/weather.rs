//! Weather and a sky around Bevy's atmosphere, through the `bevy_weather` crate: a day and night
//! from real solar geometry, stars, a galaxy and a moon with its phase, clouds raymarched through a
//! shell over the planet with their shadows on the ground, fog, rain, snow and thunder, driven by a
//! procedural forecast or set by hand.
//!
//! An app asks for it with `Config.Weather`, since the plugin brings a sun and a moon of its own
//! where the app has marked none, a planet with an atmosphere, and the exposure it is calibrated
//! for on every camera marked as a weather camera. It is kept out where meshlets run, whose
//! pipelines Bevy builds without an atmosphere's bindings, so a camera under the weather's
//! atmosphere would end the app.
//!
//! The crate's resources and components reflect, so the managed side reaches them through the
//! wrappers generated from Bevy's registry (`WeatherConfigRef`, `WeatherTimeRef`, `WeatherRef`,
//! `ProceduralWeatherRef`, `WeatherCameraRef`, `SunLightRef` and the rest). What does not is here:
//! whether the plugin runs, and a preset of conditions to ease into or to snap to.

#[cfg(feature = "render")]
use crate::state::with_world;

/// Whether the weather plugin is running in this app, which the managed side asks before marking
/// a camera or reading the weather's resources.
#[cfg(feature = "render")]
static ACTIVE: std::sync::atomic::AtomicBool = std::sync::atomic::AtomicBool::new(false);

/// Says the weather is not running, before an app decides whether to add it.
///
/// Called for every app a process builds, since a flag left on by an earlier app with it would
/// otherwise answer for a later one without it.
#[cfg(feature = "render")]
pub fn forget() {
    ACTIVE.store(false, std::sync::atomic::Ordering::Relaxed);
}

/// Adds the weather plugin, unless meshlets run.
///
/// The camera's tonemapper and bloom are left to the app, which sets them with the rest of its
/// post processing, rather than the ACES and the bloom the crate puts on a weather camera, so a
/// settings screen and the weather do not take turns at them. Its exposure is kept, since the sky,
/// the sun and the night are calibrated to it, and it opens up through twilight as an eye does.
#[cfg(feature = "render")]
pub fn install(app: &mut bevy::app::App) {
    if super::meshlets::bcs_render_meshlets_active() != 0 {
        bevy::log::warn!(
            "The weather was asked for, but meshlets run, and a camera under the weather's \
             atmosphere would end the app, so this run has no weather."
        );
        return;
    }

    app.insert_resource(bevy_weather::atmosphere::AtmosphereConfig {
        tonemapping: None,
        bloom: None,
        ..Default::default()
    });
    app.add_plugins(bevy_weather::WeatherPlugin::default());
    ACTIVE.store(true, std::sync::atomic::Ordering::Relaxed);
}

/// Answers 1 when the weather plugin is running in this app, 0 when it is not, as when the app did
/// not ask for it, meshlets run, or the bridge has no renderer.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_weather_active() -> i32 {
    crate::interop::guard(|| {
        #[cfg(feature = "render")]
        {
            ACTIVE.load(std::sync::atomic::Ordering::Relaxed) as i32
        }

        #[cfg(not(feature = "render"))]
        {
            0
        }
    })
}

/// Sets the weather to one of the crate's presets, by its place in `WeatherPreset`, eased into over
/// the weather's transition half-life, or at once where `immediately` is non-zero.
///
/// The procedural forecast overwrites the target each frame while it is on, so a preset holds only
/// with it off, which the managed side says.
///
/// Answers `NOT_PRESENT` for a number that is no preset and `INVALID_STATE` where the weather is
/// not running.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_weather_set_preset(preset: i32, immediately: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (preset, immediately);
            crate::interop::status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use crate::interop::status;
            let Some(preset) = preset_at(preset) else {
                return status::NOT_PRESENT;
            };

            with_world(|world| {
                let Some(mut weather) = world.get_resource_mut::<bevy_weather::state::Weather>() else {
                    return status::INVALID_STATE;
                };

                let conditions = preset.conditions();
                if immediately != 0 {
                    weather.set_immediate(conditions);
                } else {
                    weather.set(conditions);
                }

                status::OK
            })
        }
    })
}

/// The preset at a place in `WeatherPreset::ALL`, the order the crate declares them in, which the
/// managed enum keeps.
#[cfg(feature = "render")]
fn preset_at(place: i32) -> Option<bevy_weather::presets::WeatherPreset> {
    use bevy_weather::presets::WeatherPreset;

    usize::try_from(place).ok().and_then(|place| WeatherPreset::ALL.get(place).copied())
}
