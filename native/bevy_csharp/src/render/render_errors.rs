//! Errors the renderer reports, and what each does to the app.

#![cfg(feature = "render")]

use std::sync::Mutex;
use std::sync::atomic::{AtomicBool, Ordering};

use bevy::ecs::world::World;

/// Whether a validation error leaves the app running rather than closing it.
static KEEP_RENDERING: AtomicBool = AtomicBool::new(false);

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
/// the shader is fixed and reloads, and every other error still closes the app.
pub(super) fn on_render_error(
    error: &bevy::render::error_handler::RenderError,
    main_world: &mut World,
    _render_world: &mut World,
) -> bevy::render::error_handler::RenderErrorPolicy {
    use bevy::render::error_handler::{ErrorType, RenderErrorPolicy};

    if let Ok(mut last) = LAST_ERROR.lock() {
        *last = error.description.clone();
    }

    if KEEP_RENDERING.load(Ordering::Relaxed) && matches!(error.ty, ErrorType::Validation) {
        return RenderErrorPolicy::Ignore;
    }

    bevy::log::error!("Quitting the application due to {:?} RenderError", error.ty);
    main_world.write_message(bevy::app::AppExit::error());
    RenderErrorPolicy::StopRendering
}
