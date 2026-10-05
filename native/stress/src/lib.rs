//! Bevy's own stress tests run as the bridge runs an app offscreen, so each is measured beside the
//! same test written in C#.
//!
//! `build/bevy-stress.sh` fetches a test's source at the Bevy version the bridge builds, puts it in
//! `examples/` with its `DefaultPlugins` swapped for [`default_plugins`], and runs it. Nothing else
//! in the test changes, so what is measured is Bevy's program as Bevy wrote it, drawn into the image
//! the bridge draws into rather than a window, and timed by the same clock as `frame.profile`.

use bevy::app::{PluginGroup, PluginGroupBuilder};
use bevy::prelude::*;
use bevy::render::{Render, RenderApp, RenderSystems};
use std::sync::atomic::{AtomicBool, AtomicU64, Ordering};
use std::time::{Duration, Instant};

/// Frames let run before the measuring starts, for pipelines to compile and a scene to fill, as
/// `games/Stress/measure.sh` lets the C# side settle.
const SETTLE: u32 = 300;

/// Frames measured, as many as `frame.profile` is asked for.
const MEASURED: u32 = 240;

/// Bevy's default plugins with no window, the bridge's offscreen image in its place, and a clock.
///
/// Winit is left out, as an offscreen run of the bridge leaves it out, so nothing opens on the
/// desktop and nothing waits for one. The test's own `Window` stays an entity, so a test that lays
/// itself out by the window's size reads the size it asked for, and each camera pointed at it is
/// pointed at the image instead by the bridge's own code.
pub fn default_plugins() -> PluginGroupBuilder {
    DefaultPlugins.build().disable::<bevy::winit::WinitPlugin>().add(Offscreen)
}

/// The image, the loop that runs frames unpaced as a window with no vertical sync would, and the
/// clock.
struct Offscreen;

impl Plugin for Offscreen {
    fn build(&self, app: &mut App) {
        let (width, height) = size();
        app.add_plugins(bevy::app::ScheduleRunnerPlugin::run_loop(Duration::ZERO));
        bevy_csharp::offscreen::install_offscreen_target(app, width, height);
        app.init_resource::<Clock>().add_systems(First, clock);
        with_the_bridge(app);

        // The render schedule's phases, timed where frame.profile times them through the bridge.
        if let Some(render_app) = app.get_sub_app_mut(RenderApp) {
            use RenderSystems as Set;
            render_app.init_resource::<RenderClock>();
            render_app.add_systems(Render, mark::<0>.before(Set::ExtractCommands));
            render_app.add_systems(Render, mark::<1>.after(Set::ExtractCommands).before(Set::PrepareMeshes));
            render_app.add_systems(Render, mark::<2>.after(Set::PrepareMeshes).before(Set::CreateViews));
            render_app.add_systems(Render, mark::<3>.after(Set::CreateViews).before(Set::Specialize));
            render_app.add_systems(Render, mark::<4>.after(Set::Specialize).before(Set::PrepareViews));
            render_app.add_systems(Render, mark::<5>.after(Set::PrepareViews).before(Set::Queue));
            render_app.add_systems(Render, mark::<6>.after(Set::Queue).before(Set::PhaseSort));
            render_app.add_systems(Render, mark::<7>.after(Set::PhaseSort).before(Set::Prepare));
            render_app.add_systems(Render, mark::<8>.after(Set::Prepare).before(Set::Render));
            render_app.add_systems(Render, mark::<9>.after(Set::Render).before(Set::Cleanup));
            render_app.add_systems(Render, mark::<10>.after(Set::Cleanup).before(Set::PostCleanup));
            render_app.add_systems(Render, mark::<11>.after(Set::PostCleanup));
        }
    }
}

/// What the bridge adds to every app beyond Bevy's defaults, by name, from `BEVY_STRESS_WITH` as
/// `wireframe2d,material`, to find what a difference between a test and its C# version is.
fn with_the_bridge(app: &mut App) {
    use bevy_csharp::render;

    let with = std::env::var("BEVY_STRESS_WITH").unwrap_or_default();
    for part in with.split(',').filter(|part| !part.is_empty()) {
        match part {
            "wireframe" => {
                app.add_plugins(bevy::pbr::wireframe::WireframePlugin::default());
            }
            "wireframe2d" => {
                app.add_plugins(bevy::sprite_render::Wireframe2dPlugin::default());
            }
            "autoexposure" => {
                app.add_plugins(bevy::post_process::auto_exposure::AutoExposurePlugin);
            }
            "material" => render::material::install(app, std::env::temp_dir()),
            "post" => render::post::install(app),
            "views" => render::views::install(app),
            "probes" => render::probes::install(app),
            "watch" => render::watch::install(app),
            "corners" => render::corners::install(app),
            "passes" => render::passes::install(app),
            "compute" => render::compute::install(app),
            "rays" => render::rays::install(app),
            "layers" => render::layers::install(app),
            other => eprintln!("BEVY_STRESS_WITH names {other}, which is nothing the bridge adds"),
        }
    }
}

/// The image's size, from `BEVY_STRESS_SIZE` as `1920x1080`, the window most of the tests ask for
/// where it is not given.
fn size() -> (u32, u32) {
    std::env::var("BEVY_STRESS_SIZE")
        .ok()
        .and_then(|size| {
            let (width, height) = size.split_once('x')?;
            Some((width.parse().ok()?, height.parse().ok()?))
        })
        .unwrap_or((1920, 1080))
}

#[derive(Resource, Default)]
struct Clock {
    seen: u32,
    began: Option<Instant>,
    total: f64,
}

/// Times a frame from the top of one to the top of the next, as `frame.profile` does, and once the
/// measured frames have run prints their average and ends the run.
fn clock(mut clock: ResMut<Clock>, mut exit: MessageWriter<AppExit>) {
    let now = Instant::now();
    clock.seen += 1;

    if clock.seen > SETTLE
        && let Some(began) = clock.began
    {
        clock.total += now.duration_since(began).as_secs_f64();
    }

    if clock.seen == SETTLE {
        MEASURING.store(true, Ordering::Relaxed);
    }

    if clock.seen == SETTLE + MEASURED {
        MEASURING.store(false, Ordering::Relaxed);
        let renders = RENDERS.load(Ordering::Relaxed).max(1) as f64;
        for (phase, name) in PHASE_NAMES.iter().enumerate() {
            let ms = PHASE_NANOS[phase].load(Ordering::Relaxed) as f64 / 1e6 / renders;
            if ms >= 0.05 {
                println!("  {ms:8.3} ms  render: {name}");
            }
        }
        println!("render_ms={:.3}", RENDER_NANOS.load(Ordering::Relaxed) as f64 / 1e6 / renders);
        println!("frame_ms={:.3}", clock.total * 1000.0 / f64::from(MEASURED));
        exit.write(AppExit::Success);
    }

    clock.began = Some(now);
}

static MEASURING: AtomicBool = AtomicBool::new(false);
static RENDER_NANOS: AtomicU64 = AtomicU64::new(0);
static RENDERS: AtomicU64 = AtomicU64::new(0);
static PHASE_NANOS: [AtomicU64; 11] = [const { AtomicU64::new(0) }; 11];

/// The render schedule's phases, in the order the bridge's frame.profile names them.
const PHASE_NAMES: [&str; 11] = [
    "extract commands", "prepare meshes", "create views", "specialize", "prepare views", "queue",
    "sort", "prepare", "render", "cleanup", "post cleanup",
];

#[derive(Resource, Default)]
struct RenderClock {
    marks: [Option<Instant>; 12],
}

/// Notes when phase `I` began, or past the last, when the schedule ended, as the bridge does.
fn mark<const I: usize>(mut clock: ResMut<RenderClock>) {
    let now = Instant::now();
    if I == 0 {
        clock.marks = [None; 12];
    }
    clock.marks[I] = Some(now);

    if I != 11 || !MEASURING.load(Ordering::Relaxed) {
        return;
    }

    if let Some(began) = clock.marks[0] {
        RENDER_NANOS.fetch_add(now.duration_since(began).as_nanos() as u64, Ordering::Relaxed);
        RENDERS.fetch_add(1, Ordering::Relaxed);
    }

    for phase in 0..11 {
        if let (Some(from), Some(to)) = (clock.marks[phase], clock.marks[phase + 1]) {
            PHASE_NANOS[phase].fetch_add(to.duration_since(from).as_nanos() as u64, Ordering::Relaxed);
        }
    }
}
