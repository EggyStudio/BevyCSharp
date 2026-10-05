//! Whether the renderer is still compiling pipelines.
//!
//! Bevy compiles a pipeline the first time something is drawn with it, over several frames and off
//! the main thread, and draws nothing with it until it is done. A loading screen that waits for its
//! assets alone comes down a frame or more before the level can be seen, so it also waits for the
//! pipeline cache to empty. The cache lives in the render world, which the main world cannot reach,
//! so its count is copied across while the render world extracts, the one time the two meet.

#[cfg(not(feature = "render"))]
use crate::interop::status;

/// How many pipelines the cache was still waiting on at the last extract, or nothing before the
/// first, which says nothing is ready yet rather than that nothing waits.
#[cfg(feature = "render")]
#[derive(bevy::ecs::resource::Resource, Default)]
struct PipelinesWaiting(Option<u32>);

/// Adds the copy of the count into the main world.
#[cfg(feature = "render")]
pub fn install(app: &mut bevy::app::App) {
    use bevy::render::{ExtractSchedule, RenderApp};

    app.init_resource::<PipelinesWaiting>();
    if let Some(render_app) = app.get_sub_app_mut(RenderApp) {
        render_app.add_systems(ExtractSchedule, copy_count);
    }
}

#[cfg(feature = "render")]
fn copy_count(
    mut main_world: bevy::ecs::system::ResMut<bevy::render::MainWorld>,
    cache: bevy::ecs::system::Res<bevy::render::render_resource::PipelineCache>,
) {
    if let Some(mut waiting) = main_world.get_resource_mut::<PipelinesWaiting>() {
        waiting.0 = Some(cache.waiting_pipelines().count() as u32);
    }
}

/// Writes how many pipelines are still being compiled. Answers `NOT_PRESENT` before the render
/// world has extracted once, when the count is not known yet.
///
/// # Safety
/// `out` must be writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_pipelines_waiting(out: *mut u32) -> i32 {
    crate::interop::guard(|| {
        if out.is_null() {
            return crate::interop::status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use crate::interop::status;

            crate::state::with_world(|world| match world.get_resource::<PipelinesWaiting>() {
                Some(PipelinesWaiting(Some(count))) => {
                    unsafe { out.write(*count) };
                    status::OK
                }
                Some(PipelinesWaiting(None)) => status::NOT_PRESENT,
                None => status::INVALID_STATE,
            })
        }
    })
}
