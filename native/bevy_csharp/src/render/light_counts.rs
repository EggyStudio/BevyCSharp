//! How many point lights the render world took from the game's world and how many it drew, the
//! two numbers Bevy's many_lights stress test logs to show how many of its lights the view's
//! clusters kept.
//!
//! Counted in the render world, where the lights are once extracted, as Bevy's example counts
//! them, and kept for the managed side to read from its own frame, a frame behind.

use std::sync::atomic::{AtomicU32, Ordering};

use crate::interop::status;

/// Point lights the render world held as it prepared the last frame it drew.
static EXTRACTED: AtomicU32 = AtomicU32::new(0);

/// Of those, the ones the view's clusters held, which the frame drew.
static CLUSTERED: AtomicU32 = AtomicU32::new(0);

/// Counts the lights each frame the render world prepares, from zero for a new app.
#[cfg(feature = "render")]
pub(crate) fn install(app: &mut bevy::app::App) {
    use bevy::ecs::schedule::IntoScheduleConfigs;
    use bevy::render::{Render, RenderApp, RenderSystems};

    EXTRACTED.store(0, Ordering::Relaxed);
    CLUSTERED.store(0, Ordering::Relaxed);

    if let Some(render) = app.get_sub_app_mut(RenderApp) {
        render.add_systems(Render, count.in_set(RenderSystems::Prepare));
    }
}

/// Bevy's example's own count, the clustered lights being the clusterable objects the frame
/// holds, which in a scene with no other clusterable object are its lights.
#[cfg(feature = "render")]
fn count(
    extracted: bevy::ecs::system::Query<&bevy::pbr::ExtractedPointLight>,
    meta: Option<bevy::ecs::system::Res<bevy::pbr::GlobalClusterableObjectMeta>>,
) {
    EXTRACTED.store(extracted.iter().len() as u32, Ordering::Relaxed);
    CLUSTERED.store(meta.map_or(0, |meta| meta.entity_to_index.len() as u32), Ordering::Relaxed);
}

/// Writes the point lights the render world held as it prepared its last frame, then those its
/// clusters held, to `out`, both zero before a frame has been drawn or with no renderer.
///
/// # Safety
/// `out` must be writable for two `u32`s.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_point_lights_drawn(out: *mut u32) -> i32 {
    crate::interop::guard(|| {
        if out.is_null() {
            return status::NULL_ARG;
        }

        unsafe {
            out.write(EXTRACTED.load(Ordering::Relaxed));
            out.add(1).write(CLUSTERED.load(Ordering::Relaxed));
        }
        status::OK
    })
}
