//! End-to-end tests for the simulation half of the plugin.
//!
//! The unit tests cover the maths as pure functions; these run the actual
//! systems in an actual `App`, which is what catches missing resources, bad
//! system ordering and plugins that panic on startup. The rendering plugins
//! need a GPU and so are not included here -- they are covered by the
//! `capture` example instead.

use bevy::MinimalPlugins;
use bevy::app::App;
use bevy::time::{Time, TimePlugin, TimeUpdateStrategy};
use std::time::Duration;

use bevy::light::{CascadeShadowConfig, DirectionalLight, SunDisk};
use bevy_weather::prelude::*;
use bevy_weather::{
    CorePlugin, celestial::CelestialBodies, celestial::CelestialPlugin,
    procedural::ProceduralWeatherPlugin, state::WeatherStatePlugin, time::WeatherTimePlugin,
    wind::WindPlugin,
};

/// An app with everything that does not need a renderer.
fn simulation_app() -> App {
    let mut app = App::new();
    app.add_plugins(MinimalPlugins)
        .add_plugins((
            CorePlugin,
            WeatherTimePlugin,
            WeatherStatePlugin,
            ProceduralWeatherPlugin,
            WindPlugin,
        ))
        // Fixed steps, so the tests are not at the mercy of how fast the
        // machine running them happens to be.
        .insert_resource(TimeUpdateStrategy::ManualDuration(Duration::from_millis(
            16,
        )));
    app
}

#[test]
fn the_plugin_stack_builds_and_runs() {
    let mut app = simulation_app();
    for _ in 0..120 {
        app.update();
    }
    // Having got here without a panic, the resources should all be present.
    assert!(app.world().get_resource::<WeatherTime>().is_some());
    assert!(app.world().get_resource::<Weather>().is_some());
    assert!(app.world().get_resource::<Wind>().is_some());
    assert!(app.world().get_resource::<ProceduralWeather>().is_some());
}

#[test]
fn the_clock_advances() {
    let mut app = simulation_app();
    app.world_mut()
        .resource_mut::<WeatherTime>()
        .day_length_secs = 10.0;
    let start = app.world().resource::<WeatherTime>().elapsed_days();
    for _ in 0..60 {
        app.update();
    }
    let end = app.world().resource::<WeatherTime>().elapsed_days();
    assert!(end > start, "{start} -> {end}");
}

#[test]
fn pausing_stops_the_clock() {
    let mut app = simulation_app();
    {
        let mut time = app.world_mut().resource_mut::<WeatherTime>();
        time.paused = true;
        time.time_of_day = 0.42;
    }
    for _ in 0..60 {
        app.update();
    }
    let time = app.world().resource::<WeatherTime>();
    assert_eq!(time.time_of_day, 0.42);
    assert_eq!(time.day, 0);
}

#[test]
fn the_procedural_driver_keeps_the_weather_valid() {
    let mut app = simulation_app();
    app.world_mut()
        .resource_mut::<WeatherTime>()
        .day_length_secs = 2.0;
    for _ in 0..600 {
        app.update();
        let weather = app.world().resource::<Weather>();
        assert_eq!(weather.current, weather.current.sanitized());
        assert_eq!(weather.target, weather.target.sanitized());
    }
}

#[test]
fn disabling_the_driver_hands_control_back() {
    let mut app = simulation_app();
    app.world_mut().resource_mut::<ProceduralWeather>().enabled = false;
    {
        let mut weather = app.world_mut().resource_mut::<Weather>();
        weather.set(WeatherPreset::Blizzard);
    }
    for _ in 0..30 {
        app.update();
    }
    assert_eq!(
        app.world().resource::<Weather>().target,
        WeatherPreset::Blizzard.conditions(),
        "the driver overwrote a manually set target"
    );
}

#[test]
fn weather_eases_toward_its_target_rather_than_snapping() {
    let mut app = simulation_app();
    {
        let mut procedural = app.world_mut().resource_mut::<ProceduralWeather>();
        procedural.enabled = false;
    }
    {
        let mut weather = app.world_mut().resource_mut::<Weather>();
        *weather = Weather::new(WeatherPreset::Clear.conditions());
        weather.transition_half_life = 1.0;
        weather.set(WeatherPreset::Storm);
    }

    // A few frames in, the transition should have started but be nowhere near
    // finished. (Bevy's very first frame reports a zero delta, so this cannot
    // be a single update.)
    for _ in 0..5 {
        app.update();
    }
    let target = WeatherPreset::Storm.conditions().rain;
    let early = app.world().resource::<Weather>().current.rain;
    assert!(
        early > 0.0 && early < target * 0.25,
        "should have barely started, got {early} of {target}"
    );

    // Ten half-lives is comfortably converged.
    for _ in 0..700 {
        app.update();
    }
    assert!(
        app.world().resource::<Weather>().is_settled(),
        "never converged"
    );
}

#[test]
fn wind_follows_the_weather() {
    let mut app = simulation_app();
    app.world_mut().resource_mut::<ProceduralWeather>().enabled = false;
    {
        let mut weather = app.world_mut().resource_mut::<Weather>();
        weather.set_immediate(WeatherPreset::Blizzard);
    }
    for _ in 0..30 {
        app.update();
    }
    let wind = app.world().resource::<Wind>();
    assert!(
        wind.speed() > 5.0,
        "a blizzard should be windy: {}",
        wind.speed()
    );
    assert!(wind.offset.length() > 0.0, "wind should have displaced");
}

#[test]
fn the_wind_offset_stays_bounded_over_a_long_session() {
    // The offset feeds shader-side `f32` maths, so it must wrap rather than
    // grow without limit.
    let mut app = simulation_app();
    app.world_mut().resource_mut::<ProceduralWeather>().enabled = false;
    {
        let mut weather = app.world_mut().resource_mut::<Weather>();
        weather.set_immediate(WeatherPreset::Blizzard);
    }
    app.insert_resource(TimeUpdateStrategy::ManualDuration(Duration::from_secs(60)));
    for _ in 0..2_000 {
        app.update();
        let wind = app.world().resource::<Wind>();
        let wrap = wind.offset_wrap;
        assert!(
            wind.offset.x.abs() <= wrap && wind.offset.y.abs() <= wrap,
            "offset escaped its wrap: {:?}",
            wind.offset
        );
    }
}

#[test]
fn celestial_bodies_default_before_any_render_systems_run() {
    // `CelestialPlugin` is a render-side plugin, but the resource it drives is
    // registered by `CorePlugin` so that headless code can still read it.
    let app = simulation_app();
    let bodies = app.world().resource::<CelestialBodies>();
    assert!((bodies.sun_direction.length() - 1.0).abs() < 1e-5);
}

#[test]
fn scrubbing_the_clock_backwards_and_forwards_is_stable() {
    let mut app = simulation_app();
    app.world_mut().resource_mut::<WeatherTime>().paused = true;

    let sample_at = |app: &mut App, hour: f32| {
        app.world_mut().resource_mut::<WeatherTime>().set_hour(hour);
        app.update();
        app.world().resource::<Weather>().target
    };

    let forwards = sample_at(&mut app, 14.0);
    let _ = sample_at(&mut app, 3.0);
    let backwards = sample_at(&mut app, 14.0);
    assert_eq!(
        forwards, backwards,
        "the same clock reading gave different weather"
    );
}

#[test]
fn time_and_weather_survive_a_zero_length_day() {
    // A `day_length_secs` of zero means "do not advance", not "divide by zero".
    let mut app = simulation_app();
    app.world_mut()
        .resource_mut::<WeatherTime>()
        .day_length_secs = 0.0;
    for _ in 0..30 {
        app.update();
    }
    let time = app.world().resource::<WeatherTime>();
    assert!(time.time_of_day.is_finite());
    assert_eq!(time.day, 0);
}

#[test]
fn a_zero_delta_frame_does_not_break_the_blend() {
    let mut app = App::new();
    app.add_plugins(TimePlugin)
        .add_plugins((CorePlugin, WeatherStatePlugin))
        .insert_resource(TimeUpdateStrategy::ManualDuration(Duration::ZERO));
    app.world_mut()
        .resource_mut::<Weather>()
        .set(WeatherPreset::Storm);
    for _ in 0..10 {
        app.update();
    }
    let weather = app.world().resource::<Weather>();
    assert!(weather.current.rain.is_finite());
    let _ = app.world().resource::<Time>();
}

#[test]
fn a_full_year_of_simulation_never_produces_invalid_state() {
    let mut app = simulation_app();
    app.world_mut()
        .resource_mut::<WeatherTime>()
        .day_length_secs = 1.0;
    app.insert_resource(TimeUpdateStrategy::ManualDuration(Duration::from_millis(
        500,
    )));
    // 730 half-second steps at one second per day is a year.
    for _ in 0..730 {
        app.update();
        let weather = app.world().resource::<Weather>();
        assert_eq!(weather.current, weather.current.sanitized());
        let time = app.world().resource::<WeatherTime>();
        assert!((0.0..1.0).contains(&time.time_of_day));
    }
}

#[test]
fn the_moon_light_suppresses_the_atmosphere_sun_disc() {
    // Bevy draws a sun disc for every directional light, defaulting to
    // `SunDisk::EARTH` when the component is absent. Without an explicit
    // `SunDisk::OFF` the moon light paints a blazing sun at the moon's
    // position, which sits on top of the real moon and hides its phase.
    let mut app = simulation_app();
    app.add_plugins(CelestialPlugin);
    app.update();
    app.update();

    let mut moons = app.world_mut().query_filtered::<Option<&SunDisk>, (
        bevy::ecs::query::With<MoonLight>,
        bevy::ecs::query::With<DirectionalLight>,
    )>();

    let discs: Vec<_> = moons.iter(app.world()).collect();
    assert_eq!(discs.len(), 1, "expected exactly one moon light");
    let disc = discs[0].expect("the moon light needs an explicit SunDisk");
    assert_eq!(
        disc.intensity, 0.0,
        "the moon light must not draw an atmospheric sun disc"
    );
}

#[test]
fn the_sun_light_does_draw_a_disc() {
    let mut app = simulation_app();
    app.add_plugins(CelestialPlugin);
    app.update();
    app.update();

    let mut suns = app.world_mut().query_filtered::<Option<&SunDisk>, (
        bevy::ecs::query::With<SunLight>,
        bevy::ecs::query::With<DirectionalLight>,
    )>();
    let discs: Vec<_> = suns.iter(app.world()).collect();
    assert_eq!(discs.len(), 1, "expected exactly one sun light");
    let disc = discs[0].expect("the sun light should carry a SunDisk");
    assert!(disc.intensity > 0.0);
    assert!(disc.angular_size > 0.0);
}

#[test]
fn the_sun_casts_shadows_well_past_the_default_distance() {
    // Bevy's default cascade layout stops shadows at 150 units and puts its
    // first cascade boundary ten units from the camera. That boundary is
    // visible: the volumetric fog pass picks one cascade per sample with no
    // blending between them, so a god ray changes character across a line ruled
    // at exactly that distance -- a line that follows the player around.
    let mut app = simulation_app();
    app.add_plugins(CelestialPlugin);
    app.update();
    app.update();

    let mut suns = app
        .world_mut()
        .query_filtered::<&CascadeShadowConfig, bevy::ecs::query::With<SunLight>>();
    let configs: Vec<_> = suns.iter(app.world()).collect();
    assert_eq!(configs.len(), 1, "expected exactly one sun light");
    let cascades = configs[0];

    let first = cascades.bounds.first().copied().unwrap_or(0.0);
    let last = cascades.bounds.last().copied().unwrap_or(0.0);
    assert!(
        first >= 25.0,
        "the first cascade boundary is still close enough to be stared at: {first}"
    );
    assert!(
        last >= 500.0,
        "shadows stop {last} units out, which is inside anything with a horizon"
    );
}

#[test]
fn the_atmosphere_is_handed_unfiltered_sunlight() {
    // The sky's colour is Bevy's atmosphere to compute: it integrates the
    // transmittance from the sun to every point along the view ray, and that
    // integral is what reddens a sunset. Handing it a light that has *already*
    // been reddened applies the extinction twice, and because Rayleigh
    // scattering is six times stronger in blue than in red, scattering an
    // orange sun leaves a muddy olive sky rather than an orange one.
    //
    // So the light stays much closer to white than the ground-level sun colour
    // this plugin's own shaders use.
    let mut app = simulation_app();
    app.add_plugins(CelestialPlugin);
    // Dusk, where the two differ most.
    app.world_mut().resource_mut::<WeatherTime>().set_hour(19.6);
    app.update();
    app.update();

    let ground = app.world().resource::<CelestialBodies>().sun_color;

    let mut suns = app
        .world_mut()
        .query_filtered::<&DirectionalLight, bevy::ecs::query::With<SunLight>>();
    let lights: Vec<_> = suns.iter(app.world()).collect();
    let light = lights[0].color.to_linear();

    let warmth = |c: bevy::color::LinearRgba| c.red / c.blue.max(1e-6);
    assert!(
        warmth(ground) > 1.5,
        "the ground-level sun should be strongly reddened at dusk: {ground:?}"
    );
    assert!(
        warmth(light) < warmth(ground) * 0.75,
        "the light is nearly as reddened as the ground colour, so the \
         atmosphere will redden it a second time: {light:?} against {ground:?}"
    );
}

#[test]
fn twilight_gets_an_exposure_lift_and_daylight_does_not() {
    // Twilight is orders of magnitude dimmer than noon and an eye sitting in it
    // opens up to match. The night sky is separately exaggerated so that stars
    // read at a daylight exposure, so the lift is only wanted in the gap
    // between the two.
    let atmosphere = AtmosphereConfig::default();
    assert_eq!(atmosphere.exposure_lift(1.0), 0.0, "noon needs no lift");
    assert_eq!(
        atmosphere.exposure_lift(-0.6),
        0.0,
        "a properly dark sky needs no lift"
    );
    let dusk = atmosphere.exposure_lift(-0.03);
    assert!(dusk > 0.5, "twilight was not lifted at all: {dusk}");
    assert!(dusk <= atmosphere.twilight_exposure_lift + 1e-4);
}

#[test]
fn cloud_shadows_follow_the_clouds_rather_than_the_ground() {
    // The shadow map is generated with no wind baked in; the deck's
    // displacement is carried by translating the light, which is free and
    // continuous between rebuilds. Bake it in as well and the shadows move at
    // twice the speed of the clouds casting them.
    use bevy::math::Vec2;
    use bevy_weather::cloud_field::CloudField;
    use bevy_weather::clouds::CloudConfig;

    let conditions = WeatherPreset::PartlyCloudy.conditions();
    let clouds = CloudConfig::default();
    let still = CloudField::new(&conditions, &clouds, Vec2::ZERO, 0.0);
    let blown = CloudField::new(&conditions, &clouds, Vec2::new(400.0, 0.0), 0.0);
    let shift = 400.0 * clouds.wind_multiplier;

    let a: f32 = (0..40)
        .map(|i| still.column_density(i as f32 * 130.0, 0.0))
        .sum();
    let b: f32 = (0..40)
        .map(|i| blown.column_density(i as f32 * 130.0 - shift, 0.0))
        .sum();
    assert!((a - b).abs() < 1e-2, "{a} vs {b}");
}

#[test]
fn twilight_is_not_switched_off_at_the_horizon() {
    // Bevy's atmosphere computes its inscattering as the sun light's colour
    // times a scattering factor, so cutting the light's illuminance at sunset
    // does not end the day -- it deletes twilight, and the sky drops from a
    // sunset to black in a few minutes with nothing in between.
    let mut app = simulation_app();
    app.add_plugins(CelestialPlugin);

    let illuminance_at = |app: &mut App, hour: f32| {
        app.world_mut().resource_mut::<WeatherTime>().set_hour(hour);
        app.update();
        app.update();
        let mut suns = app
            .world_mut()
            .query_filtered::<&DirectionalLight, bevy::ecs::query::With<SunLight>>();
        suns.iter(app.world()).next().unwrap().illuminance
    };

    // Northern summer at the default latitude: the sun sets a little before
    // eight, so these bracket it.
    let daylight = illuminance_at(&mut app, 15.0);
    let just_after_sunset = illuminance_at(&mut app, 20.0);
    let deep_twilight = illuminance_at(&mut app, 20.6);
    let night = illuminance_at(&mut app, 23.0);

    assert!(daylight > 1000.0);
    assert!(
        just_after_sunset > daylight * 0.05,
        "the sky went out at the horizon: {just_after_sunset} against {daylight}"
    );
    assert!(
        deep_twilight < just_after_sunset,
        "twilight should be draining, not steady"
    );
    assert_eq!(night, 0.0, "the sun is still lighting the air at 23:00");
}

#[test]
fn the_sun_stops_casting_shadows_once_it_has_set() {
    // The light stays on through twilight for the sky's sake, but the shadow
    // maps would be drawn for a light the atmosphere has already extinguished.
    let mut app = simulation_app();
    app.add_plugins(CelestialPlugin);

    let shadows_at = |app: &mut App, hour: f32| {
        app.world_mut().resource_mut::<WeatherTime>().set_hour(hour);
        app.update();
        app.update();
        let mut suns = app
            .world_mut()
            .query_filtered::<&DirectionalLight, bevy::ecs::query::With<SunLight>>();
        suns.iter(app.world()).next().unwrap().shadow_maps_enabled
    };
    assert!(shadows_at(&mut app, 13.0));
    assert!(!shadows_at(&mut app, 22.0));
}

#[test]
fn the_moon_casts_shadows_only_when_it_is_contributing() {
    // A second set of cascades is not worth rendering for a light that reaches
    // nothing, which is most of the time.
    let mut app = simulation_app();
    app.add_plugins(CelestialPlugin);
    app.world_mut()
        .resource_mut::<WeatherTime>()
        .moon_phase_offset = 0.5;

    let moon_shadows_at = |app: &mut App, hour: f32| {
        app.world_mut().resource_mut::<WeatherTime>().set_hour(hour);
        app.update();
        app.update();
        let mut moons = app
            .world_mut()
            .query_filtered::<&DirectionalLight, bevy::ecs::query::With<MoonLight>>();
        moons.iter(app.world()).next().unwrap().shadow_maps_enabled
    };
    // A full moon at midnight is high and the sun is nowhere.
    assert!(
        moon_shadows_at(&mut app, 0.5),
        "a full moon casts no shadow"
    );
    // Midday: the moon is down and the sun would drown it anyway.
    assert!(!moon_shadows_at(&mut app, 12.0));
}

#[test]
fn moonlight_is_well_clear_of_the_night_ambient() {
    // What makes a moonlit night read as *lit* is the ratio between the one
    // hard source and the skyglow around it. Set them close together and the
    // night is uniformly grey with no shadows in it, however many shadow maps
    // are being rendered.
    let mut app = simulation_app();
    app.add_plugins(CelestialPlugin);
    app.world_mut()
        .resource_mut::<WeatherTime>()
        .moon_phase_offset = 0.5;
    app.world_mut().resource_mut::<WeatherTime>().set_hour(0.5);
    app.update();
    app.update();

    let mut moons = app
        .world_mut()
        .query_filtered::<&DirectionalLight, bevy::ecs::query::With<MoonLight>>();
    let direct = moons.iter(app.world()).next().unwrap().illuminance;

    let mut ambients = app.world_mut().query::<&bevy::light::AmbientLight>();
    let ambient = ambients
        .iter(app.world())
        .map(|light| light.brightness)
        .fold(0.0f32, f32::max);

    assert!(
        direct > ambient * 2.5,
        "moonlight {direct} lux against {ambient} lux of ambient leaves no shadow to see"
    );
}

#[test]
fn cloud_shadows_still_rebuild_while_their_settings_are_being_written() {
    // The shadow texture is rebuilt a slice of rows at a time across many
    // frames. Abandoning that rebuild whenever the config changes looks tidy
    // and is a trap: an immediate-mode settings slider writes its resource
    // every frame whether or not anyone touched it, so the rebuild would
    // restart every frame, never reach the last row, and never upload
    // anything. The shadows would simply stop.
    use bevy::asset::Assets;
    use bevy::image::Image;
    use bevy_weather::celestial::CelestialPlugin;
    use bevy_weather::cloud_shadows::{CloudShadowConfig, CloudShadowMap, CloudShadowPlugin};

    let mut app = simulation_app();
    app.add_plugins(bevy::asset::AssetPlugin::default());
    bevy::asset::AssetApp::init_asset::<Image>(&mut app);
    app.add_plugins((CelestialPlugin, CloudShadowPlugin));
    app.world_mut().resource_mut::<ProceduralWeather>().enabled = false;
    app.world_mut()
        .resource_mut::<Weather>()
        .set_immediate(WeatherPreset::Overcast);
    // Noon, so the sun is high enough to project anything at all.
    app.world_mut().resource_mut::<WeatherTime>().set_hour(12.0);
    app.world_mut().resource_mut::<WeatherTime>().paused = true;

    // Stand in for that settings screen.
    app.add_systems(
        bevy::app::Update,
        |mut config: bevy::ecs::system::ResMut<CloudShadowConfig>| {
            config.strength = 0.8;
        },
    );

    for _ in 0..240 {
        app.update();
    }

    let handle = app.world().resource::<CloudShadowMap>().image.clone();
    let images = app.world().resource::<Assets<Image>>();
    let data = images
        .get(&handle)
        .and_then(|image| image.data.as_ref())
        .expect("the shadow map image should exist");
    let shaded = data
        .as_chunks::<4>()
        .0
        .iter()
        .filter(|texel| texel[0] < 250)
        .count();
    assert!(
        shaded > 0,
        "an overcast sky left the shadow map completely blank, so no rebuild \
         ever finished"
    );
}

#[test]
fn a_settled_sky_stops_announcing_itself() {
    // `Res<Weather>::is_changed()` is the obvious hook for reacting to the
    // weather turning, and it is worthless if the resource is rewritten every
    // frame whether or not anything moved. Which is what an exponential blend
    // does on its own: it approaches its target and stalls a float's width
    // short, so `current` is never equal to `target` and the write never stops.
    let mut app = simulation_app();
    app.world_mut().resource_mut::<ProceduralWeather>().enabled = false;
    app.world_mut()
        .resource_mut::<Weather>()
        .set_immediate(WeatherPreset::Clear);
    app.world_mut()
        .resource_mut::<Weather>()
        .set(WeatherPreset::Storm);

    // Long enough for an eight-second half-life to be thoroughly finished.
    for _ in 0..6_000 {
        app.update();
    }
    assert!(
        app.world().resource::<Weather>().is_settled(),
        "the storm never arrived"
    );

    // Now count the frames that claim a change, the way a game would.
    #[derive(bevy::ecs::resource::Resource, Default)]
    struct Announcements(u32);
    app.init_resource::<Announcements>();
    app.add_systems(
        bevy::app::Update,
        |weather: bevy::ecs::system::Res<Weather>,
         mut count: bevy::ecs::system::ResMut<Announcements>| {
            use bevy::ecs::change_detection::DetectChanges;
            if weather.is_changed() {
                count.0 += 1;
            }
        },
    );
    // A system sees everything as changed on its very first run, so let it
    // have one frame before counting.
    app.update();
    app.world_mut().resource_mut::<Announcements>().0 = 0;
    for _ in 0..30 {
        app.update();
    }
    let announcements = app.world().resource::<Announcements>().0;
    assert_eq!(
        announcements, 0,
        "a settled sky marked itself changed on {announcements} of 30 frames"
    );
}

#[test]
fn a_transition_that_only_moves_the_cloud_base_still_counts() {
    // `is_settled` used to look at six of the thirteen fields, so a sky that
    // was still visibly raising its cloud deck reported itself finished.
    let mut base = WeatherPreset::Overcast.conditions();
    base.cloud_altitude = 800.0;
    let mut raised = base;
    raised.cloud_altitude = 3_000.0;

    let mut weather = Weather::new(base);
    weather.set(raised);
    assert!(!weather.is_settled());
}
