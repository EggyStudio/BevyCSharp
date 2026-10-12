//! Errors the renderer reports, and what each does to the app.

#![cfg(feature = "render")]

use std::sync::Mutex;
use std::sync::atomic::{AtomicBool, AtomicI32, AtomicU32, Ordering};

use bevy::ecs::world::World;
use bevy::render::render_resource::{CachedPipelineState, PipelineCache};
use bevy::render::settings::{Backends, WgpuSettings};

/// Whether a validation error leaves the app running rather than closing it.
static KEEP_RENDERING: AtomicBool = AtomicBool::new(false);

/// What any other error does, as [`set_policy`] takes it.
static POLICY: AtomicI32 = AtomicI32::new(QUIT);

/// How many errors the renderer has reported since the library was loaded.
static COUNT: AtomicU32 = AtomicU32::new(0);

/// Whether drawing was stopped by the last error, which Bevy then hands back every frame.
static STOPPED: AtomicBool = AtomicBool::new(false);

/// The graphics backend the app's renderer was made with, as the config names it, so a renderer
/// made again is made the same way.
static BACKEND: AtomicU32 = AtomicU32::new(0);

/// The app is closed with an error, Bevy's own answer.
pub const QUIT: i32 = 0;
/// The error is logged and drawing carries on as if nothing happened.
pub const IGNORE: i32 = 1;
/// The app keeps running and drawing stops until it is set otherwise.
pub const STOP_RENDERING: i32 = 2;
/// The renderer is made again, a device and all, and drawing carries on with it.
pub const RECOVER: i32 = 3;

/// Says what an error the renderer reports does, one of [`QUIT`], [`IGNORE`],
/// [`STOP_RENDERING`] and [`RECOVER`].
pub fn set_policy(policy: i32) {
    POLICY.store(policy, Ordering::Relaxed);
}

/// Forgets that drawing was stopped, as an app is built, so an error the last app quit over is not
/// taken for the same one handed back when the next app reports its own.
pub fn begin_app() {
    STOPPED.store(false, Ordering::Relaxed);
}

/// What an error the renderer reports does now.
pub fn policy() -> i32 {
    POLICY.load(Ordering::Relaxed)
}

/// How many errors the renderer has reported.
pub fn count() -> u32 {
    COUNT.load(Ordering::Relaxed)
}

/// The backend the config names, or `None` for `0`, wgpu's own choice.
///
/// `None` leaves wgpu's own preference order alone, which already puts Vulkan first on Linux and
/// Windows. Naming a backend pins the choice instead, and startup then fails loudly if the machine
/// cannot provide it, which is the point of asking.
pub fn backends_named(backend: u32) -> Option<Backends> {
    match backend {
        1 => Some(Backends::VULKAN),
        2 => Some(Backends::DX12),
        3 => Some(Backends::METAL),
        4 => Some(Backends::GL),
        _ => None,
    }
}

/// The settings a renderer is made with for the backend the config names, remembered so a
/// renderer made again after an error is made the same way.
pub fn wgpu_settings(backend: u32) -> WgpuSettings {
    BACKEND.store(backend, Ordering::Relaxed);

    match backends_named(backend) {
        Some(backends) => WgpuSettings { backends: Some(backends), ..Default::default() },
        None => WgpuSettings::default(),
    }
}

/// The last error the renderer reported, for whoever asks.
static LAST_ERROR: Mutex<String> = Mutex::new(String::new());

/// Says whether a validation error closes the app, which is Bevy's answer, or is logged and
/// survived.
pub fn keep_rendering_after_errors(keep: bool) {
    KEEP_RENDERING.store(keep, Ordering::Relaxed);
}

/// The last error the renderer reported, or an empty string.
pub fn last_error() -> String {
    LAST_ERROR.lock().map(|text| text.clone()).unwrap_or_default()
}

/// Decides what a render error does to the app.
///
/// A shader that compiles can still disagree with the pipeline it is put in, by reading an input
/// its vertex shader never wrote for instance. That is a validation error, and Bevy's answer to any
/// of those is to close the app, which is right for a shipped game and wrong for one somebody is
/// editing a shader in. Surviving means the frames that use the broken pipeline are not drawn until
/// the shader is fixed and reloads, so a validation error is ignored while [`KEEP_RENDERING`] is
/// set, whatever the policy.
///
/// Any other error does what the game's policy says, Bevy's quit unless it set another. Bevy calls
/// this again every frame while drawing is stopped, so a policy set later takes over from there,
/// and a renderer made again is made with the backend the app started with.
pub(super) fn on_render_error(
    error: &bevy::render::error_handler::RenderError,
    main_world: &mut World,
    render_world: &mut World,
) -> bevy::render::error_handler::RenderErrorPolicy {
    use bevy::render::error_handler::{ErrorType, RenderErrorPolicy};
    use bevy::render::settings::RenderCreation;

    let same = LAST_ERROR.lock().map(|mut last| {
        let same = *last == error.description;
        last.clone_from(&error.description);
        same
    });
    // Counted once, since a stopped renderer hands the same error back every frame, where one
    // that recurs while drawing carries on is a new error each time.
    if !(STOPPED.load(Ordering::Relaxed) && same.unwrap_or(false)) {
        COUNT.fetch_add(1, Ordering::Relaxed);
    }

    let answer = if KEEP_RENDERING.load(Ordering::Relaxed) && matches!(error.ty, ErrorType::Validation) {
        RenderErrorPolicy::Ignore
    } else {
        match policy() {
            IGNORE => RenderErrorPolicy::Ignore,
            STOP_RENDERING => RenderErrorPolicy::StopRendering,
            RECOVER => {
                let settings = wgpu_settings(BACKEND.load(Ordering::Relaxed));
                RenderErrorPolicy::Recover(RenderCreation::Automatic(Box::new(settings)))
            }
            _ => {
                bevy::log::error!("Quitting the application due to {:?} RenderError", error.ty);
                main_world.write_message(bevy::app::AppExit::error());
                if let Some(mut cache) = render_world.get_resource_mut::<PipelineCache>() {
                    finish_pipelines(&mut cache);
                }
                RenderErrorPolicy::StopRendering
            }
        }
    };

    STOPPED.store(matches!(answer, RenderErrorPolicy::StopRendering), Ordering::Relaxed);
    answer
}

/// Waits for every pipeline still compiling to finish, ten seconds at most, so none is left on a
/// pool thread as the app goes.
///
/// A pipeline compiles in a task on Bevy's pool, and the task holds the device. The app's worlds
/// are dropped as it exits, which cancels a task still compiling, and the pool thread finishing it
/// then lets the device go. With NVIDIA's Linux driver, that can run while the process's exit is
/// taking the driver down, which corrupts its heap and aborts the process. An exit early in a run,
/// while the first pipelines compile, meets it, and a render error that closes the app most often.
/// Starting the pipelines still queued is part of the wait, since asking is how a finished one is
/// collected.
pub(crate) fn finish_pipelines(cache: &mut PipelineCache) {
    let deadline = std::time::Instant::now() + std::time::Duration::from_secs(10);
    loop {
        cache.process_queue();

        let compiling = cache
            .pipelines()
            .any(|pipeline| matches!(pipeline.state, CachedPipelineState::Creating(_)));
        if !compiling || std::time::Instant::now() >= deadline {
            return;
        }
        std::thread::sleep(std::time::Duration::from_millis(1));
    }
}

/// Waits for the pipelines still compiling in the frame the app is asked to exit, for the reason
/// [`finish_pipelines`] gives.
pub(super) fn finish_pipelines_at_exit(
    exits: bevy::render::Extract<Option<bevy::ecs::system::Res<bevy::ecs::message::Messages<bevy::app::AppExit>>>>,
    cache: Option<bevy::ecs::system::ResMut<PipelineCache>>,
) {
    let exiting = exits.as_deref().is_some_and(|exits| !exits.is_empty());
    if let (true, Some(mut cache)) = (exiting, cache) {
        finish_pipelines(&mut cache);
    }
}
