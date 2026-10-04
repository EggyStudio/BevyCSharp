//! games/Stress's drawn scene written against Bevy alone, with no bridge and no C#, to measure
//! beside the stress program and tell Bevy's cost from what the bridge adds.
//!
//!   cargo run --release --manifest-path native/stress/Cargo.toml -- <count> [noshadows] [wireframe]
//!       [autoexposure] [timings] [ui] [material] [views] [probes] [watch] [corners] [compute] [rays] [layers]
//!
//! Each word after the count adds what the bridge adds, Bevy's plugins and the bridge's own render
//! installers, `timings` also prints each pass Bevy times as the bridge's render.timings lists them,
//! and `ui` makes the camera the interface's default, as an offscreen bridge run does.
//!
//! The same scene as `Stress drawn`, cubes in four materials on a floor under a directional light
//! and two point lights, drawn into a 1280 by 720 image with no window, unpaced. It lets 300 frames
//! settle, then prints the average frame and render schedule over the next 240 and exits. Physics
//! is left out, since the bridge's is C# and the frames compared here are the render's.

use std::time::Instant;

use bevy::asset::RenderAssetUsages;
use bevy::camera::RenderTarget;
use bevy::image::Image;
use bevy::prelude::*;
use bevy::render::render_resource::{Extent3d, TextureDimension, TextureFormat, TextureUsages};
use bevy::render::{Render, RenderApp, RenderSystems};
use bevy::window::ExitCondition;

const SETTLE: u32 = 300;
const MEASURED: u32 = 240;

#[derive(Resource)]
struct Load {
    count: usize,
    shadows: bool,
    ui: bool,
}

#[derive(Resource, Default)]
struct Frames {
    seen: u32,
    began: Option<Instant>,
    total: f64,
}

#[derive(Resource, Default)]
struct RenderClock {
    marks: [Option<Instant>; 12],
}

static RENDER_NANOS: std::sync::atomic::AtomicU64 = std::sync::atomic::AtomicU64::new(0);
static RENDERS: std::sync::atomic::AtomicU64 = std::sync::atomic::AtomicU64::new(0);
static MEASURING: std::sync::atomic::AtomicBool = std::sync::atomic::AtomicBool::new(false);

fn main() {
    let args: Vec<String> = std::env::args().skip(1).collect();
    let count = args.first().and_then(|n| n.parse().ok()).unwrap_or(1000);
    let shadows = !args.iter().any(|arg| arg == "noshadows");

    let mut app = App::new();
    app.add_plugins(
        DefaultPlugins
            .set(WindowPlugin {
                primary_window: None,
                exit_condition: ExitCondition::DontExit,
                ..default()
            })
            .disable::<bevy::winit::WinitPlugin>(),
    )
    .add_plugins(bevy::app::ScheduleRunnerPlugin::run_loop(std::time::Duration::ZERO))
    .insert_resource(Load { count, shadows, ui: args.iter().any(|arg| arg == "ui") })
    .init_resource::<Frames>()
    .add_systems(Startup, scene)
    .add_systems(First, frame);

    // The plugins the bridge adds beyond Bevy's defaults, by name, to find what a difference is.
    if args.iter().any(|arg| arg == "wireframe") {
        app.add_plugins(bevy::pbr::wireframe::WireframePlugin::default());
    }
    if args.iter().any(|arg| arg == "autoexposure") {
        app.add_plugins(bevy::post_process::auto_exposure::AutoExposurePlugin);
    }
    if args.iter().any(|arg| arg == "timings") {
        app.add_plugins(bevy::render::diagnostic::RenderDiagnosticsPlugin);
    }

    // The bridge's own rendering, whole or a part at a time, from the bridge crate itself.
    use bevy_csharp::render;
    for arg in &args {
        match arg.as_str() {
            "material" => render::material::install(&mut app, std::env::temp_dir()),
            "views" => render::views::install(&mut app),
            "probes" => render::probes::install(&mut app),
            "watch" => render::watch::install(&mut app),
            "corners" => render::corners::install(&mut app),
            "passes" => render::passes::install(&mut app),
            "compute" => render::compute::install(&mut app),
            "rays" => render::rays::install(&mut app),
            "layers" => render::layers::install(&mut app),
            _ => {}
        }
    }

    let render_app = app.sub_app_mut(RenderApp);
    render_app.init_resource::<RenderClock>();
    {
        use RenderSystems as Set;
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

    app.run();
}

fn scene(
    mut commands: Commands,
    load: Res<Load>,
    mut meshes: ResMut<Assets<Mesh>>,
    mut materials: ResMut<Assets<StandardMaterial>>,
    mut images: ResMut<Assets<Image>>,
) {
    let mesh = meshes.add(Cuboid::new(1.0, 1.0, 1.0));
    let colors = [(0.8, 0.3, 0.3), (0.3, 0.8, 0.3), (0.3, 0.3, 0.8), (0.8, 0.8, 0.3)];
    let looks: Vec<_> = colors
        .iter()
        .map(|&(r, g, b)| materials.add(StandardMaterial::from(Color::linear_rgb(r, g, b))))
        .collect();

    let side = (load.count as f32).sqrt().ceil() as usize;
    let across = side as f32 * 1.5;

    commands.spawn((
        Mesh3d(mesh.clone()),
        MeshMaterial3d(looks[0].clone()),
        Transform::from_xyz(0.0, -0.5, 0.0).with_scale(Vec3::new(across + 4.0, 1.0, across + 4.0)),
    ));

    for i in 0..load.count {
        let x = ((i % side) as f32 - side as f32 / 2.0) * 1.5;
        let z = ((i / side) as f32 - side as f32 / 2.0) * 1.5;
        commands.spawn((
            Mesh3d(mesh.clone()),
            MeshMaterial3d(looks[i % looks.len()].clone()),
            Transform::from_xyz(x, 0.5, z),
        ));
    }

    commands.spawn((
        DirectionalLight {
            illuminance: 8000.0,
            shadow_maps_enabled: load.shadows,
            ..default()
        },
        Transform::from_xyz(1.0, 2.0, 1.0).looking_at(Vec3::ZERO, Vec3::Y),
    ));

    for x in [-across / 4.0, across / 4.0] {
        commands.spawn((
            PointLight {
                intensity: 200_000.0,
                range: 20.0,
                shadow_maps_enabled: load.shadows,
                ..default()
            },
            Transform::from_xyz(x, 6.0, 0.0),
        ));
    }

    // An image to draw into, as the bridge's offscreen run does, in the format its target has.
    let size = Extent3d { width: 1280, height: 720, depth_or_array_layers: 1 };
    let mut target = Image::new_fill(
        size,
        TextureDimension::D2,
        &[0, 0, 0, 255],
        TextureFormat::Rgba8UnormSrgb,
        RenderAssetUsages::default(),
    );
    target.texture_descriptor.usage =
        TextureUsages::TEXTURE_BINDING | TextureUsages::COPY_SRC | TextureUsages::RENDER_ATTACHMENT;

    let camera = commands
        .spawn((
        Camera3d::default(),
        RenderTarget::Image(images.add(target).into()),
        Projection::Perspective(PerspectiveProjection { fov: 60f32.to_radians(), ..default() }),
        Transform::from_xyz(0.0, across * 0.9 + 10.0, across * 0.4).looking_at(Vec3::ZERO, Vec3::Y),
    ))
        .id();

    // The interface's default camera, as the bridge makes an offscreen run's camera.
    if load.ui {
        commands.entity(camera).insert(bevy::ui::IsDefaultUiCamera);
    }
}

fn frame(
    mut frames: ResMut<Frames>,
    load: Res<Load>,
    store: Option<Res<bevy::diagnostic::DiagnosticsStore>>,
    mut exit: MessageWriter<AppExit>,
) {
    use std::sync::atomic::Ordering;

    let now = Instant::now();
    frames.seen += 1;

    if frames.seen == SETTLE {
        MEASURING.store(true, Ordering::Relaxed);
    } else if frames.seen > SETTLE {
        if let Some(began) = frames.began {
            frames.total += now.duration_since(began).as_secs_f64();
        }
    }

    if frames.seen == SETTLE + MEASURED {
        MEASURING.store(false, Ordering::Relaxed);
        let renders = RENDERS.load(Ordering::Relaxed).max(1);
        println!(
            "count={} shadows={} frame_ms={:.3} render_ms={:.3}",
            load.count,
            load.shadows,
            frames.total * 1000.0 / f64::from(MEASURED - 1),
            RENDER_NANOS.load(Ordering::Relaxed) as f64 / 1e6 / renders as f64,
        );
        for (phase, name) in PHASE_NAMES.iter().enumerate() {
            let ms = PHASE_NANOS[phase].load(Ordering::Relaxed) as f64 / 1e6 / renders as f64;
            if ms >= 0.05 {
                println!("  {ms:8.3} ms  render: {name}");
            }
        }
        // Each pass's own time, as the bridge's render.timings lists it, where `timings` asked
        // for Bevy's render diagnostics, so a difference in the render phase can be read pass by
        // pass beside the bridge's.
        if let Some(store) = store {
            let mut passes: Vec<(String, f64)> = store
                .iter()
                .filter_map(|diagnostic| {
                    let path = diagnostic.path().as_str();
                    let name = path.strip_prefix("render/")?.strip_suffix("/elapsed_cpu")?;
                    Some((name.to_string(), diagnostic.average()?))
                })
                .collect();
            passes.sort_by(|a, b| a.0.cmp(&b.0));
            for (name, ms) in passes {
                println!("  {ms:8.3} ms  pass: {name}");
            }
        }

        exit.write(AppExit::Success);
    }

    frames.began = Some(now);
}

/// The render schedule's phases, in the order the bridge's frame.profile names them.
const PHASE_NAMES: [&str; 11] = [
    "extract commands", "prepare meshes", "create views", "specialize", "prepare views", "queue",
    "sort", "prepare", "render", "cleanup", "post cleanup",
];

static PHASE_NANOS: [std::sync::atomic::AtomicU64; 11] = [const { std::sync::atomic::AtomicU64::new(0) }; 11];

/// Notes when phase `I` began, or past the last, when the schedule ended, as the bridge does.
fn mark<const I: usize>(mut clock: ResMut<RenderClock>) {
    use std::sync::atomic::Ordering;

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
