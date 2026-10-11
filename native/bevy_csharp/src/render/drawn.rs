//! How many meshes the GPU drew in the opaque pass, read back from Bevy's indirect draw buffers, so
//! the meshes a camera's occlusion culling leaves out can be counted.
//!
//! With GPU preprocessing, Bevy's culling runs on the GPU and writes how many instances each batch
//! draws into the indirect parameters the draws read, so the CPU never learns what was culled.
//! Bevy's occlusion_culling example copies those buffers after the main pass into buffers it can
//! map, and reads them back a frame or two later. The bridge does the same while a game counts,
//! the copies allowed by the debug flag the app's render plugins are built with, and keeps the last
//! count where the managed side reads it.

use crate::interop::status;

#[cfg(feature = "render")]
use std::sync::{Arc, Mutex};

#[cfg(feature = "render")]
use bevy::ecs::resource::Resource;
#[cfg(feature = "render")]
use bevy::ecs::system::{Res, ResMut};
#[cfg(feature = "render")]
use bevy::render::render_resource::{Buffer, BufferDescriptor, BufferUsages, MapMode};

/// What was read back last, shared between the two worlds.
#[cfg(feature = "render")]
#[derive(Default)]
struct Read {
    /// Each indirect draw's instance count, in the buffer's order.
    instances: Vec<u32>,
    /// How many of those draws the opaque pass's first batch set holds.
    draws: u32,
    /// Whether the GPU culls at all, which occlusion culling needs.
    culling: bool,
    /// Whether the counts can be read, which needs the GPU to say how many draws there are.
    countable: bool,
    /// Whether a count has arrived since counting began.
    arrived: bool,
}

/// The counts read back, held by both worlds.
#[cfg(feature = "render")]
#[derive(Resource, Clone, Default)]
struct Counts(Arc<Mutex<Read>>);

/// Whether the game asked for the counts, which costs two copies and a readback a frame.
#[cfg(feature = "render")]
#[derive(Resource, Clone, Copy, Default)]
struct Counting(bool);

/// The buffers the indirect parameters are copied into this frame, to be mapped once extracted.
#[cfg(feature = "render")]
#[derive(Resource, Default)]
struct Staging {
    data: Option<Buffer>,
    batch_sets: Option<Buffer>,
}

/// Starts or stops counting the meshes drawn, `on` non-zero to start.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_count_drawn(on: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = on;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        crate::state::with_world(|world| {
            if !world.contains_resource::<Counts>() {
                return status::UNSUPPORTED;
            }
            world.insert_resource(Counting(on != 0));
            status::OK
        })
    })
}

/// Writes how many meshes the opaque pass drew in the last frame read back, and answers `1` where
/// it is known, `0` where no count has arrived yet, [`status::INVALID_STATE`] where the GPU does
/// not cull, and [`status::NOT_PRESENT`] where it culls but the counts cannot be read.
///
/// # Safety
/// `drawn` must be writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_drawn_meshes(drawn: *mut u32) -> i32 {
    crate::interop::guard(|| {
        if drawn.is_null() {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        crate::state::with_world(|world| {
            let Some(counts) = world.get_resource::<Counts>() else {
                return status::UNSUPPORTED;
            };
            let read = counts.0.lock().unwrap_or_else(|poisoned| poisoned.into_inner());

            if !read.culling {
                return status::INVALID_STATE;
            }
            if !read.countable {
                return status::NOT_PRESENT;
            }

            unsafe { drawn.write(total(&read.instances, read.draws)) };
            i32::from(read.arrived)
        })
    })
}

/// Each indexed draw's instance count, out of the bytes of Bevy's `IndirectParametersIndexed`, five
/// uints a draw with the instance count the second of them.
#[cfg(feature = "render")]
fn instance_counts(bytes: &[u8]) -> Vec<u32> {
    bytes
        .chunks_exact(20)
        .map(|draw| u32::from_ne_bytes([draw[4], draw[5], draw[6], draw[7]]))
        .collect()
}

/// The instances the first `draws` draws drew, which is the meshes drawn, since a draw draws each
/// mesh it batches as an instance.
#[cfg(feature = "render")]
fn total(instances: &[u32], draws: u32) -> u32 {
    instances.iter().take(draws as usize).sum()
}

/// Records what the GPU can do, once its device is there.
#[cfg(feature = "render")]
fn note_support(
    counts: Res<Counts>,
    device: Res<bevy::render::renderer::RenderDevice>,
    support: Res<bevy::render::batching::gpu_preprocessing::GpuPreprocessingSupport>,
) {
    let mut read = counts.0.lock().unwrap_or_else(|poisoned| poisoned.into_inner());
    read.culling = support.is_culling_supported();
    read.countable = device
        .features()
        .contains(bevy::render::settings::WgpuFeatures::MULTI_DRAW_INDIRECT_COUNT);
}

/// Makes buffers the opaque pass's indirect parameters can be copied into and mapped, of their
/// size this frame.
#[cfg(feature = "render")]
fn make_staging(
    counting: Res<Counting>,
    mut staging: ResMut<Staging>,
    buffers: Res<bevy::render::batching::gpu_preprocessing::IndirectParametersBuffers>,
    device: Res<bevy::render::renderer::RenderDevice>,
) {
    if !counting.0 {
        return;
    }
    let Some(opaque) = buffers.get(&std::any::TypeId::of::<bevy::core_pipeline::core_3d::Opaque3d>()) else {
        return;
    };
    let (Some(data), Some(batch_sets)) = (opaque.indexed.data_buffer(), opaque.indexed.batch_sets_buffer()) else {
        return;
    };

    let mappable = |label: &'static str, size: u64| {
        device.create_buffer(&BufferDescriptor {
            label: Some(label),
            size,
            usage: BufferUsages::MAP_READ | BufferUsages::COPY_DST,
            mapped_at_creation: false,
        })
    };
    staging.data = Some(mappable("bcs_drawn_data", data.size()));
    staging.batch_sets = Some(mappable("bcs_drawn_batch_sets", batch_sets.size()));
}

/// Copies the opaque pass's indirect parameters once the main pass has drawn with them.
#[cfg(feature = "render")]
fn copy_parameters(
    mut ctx: bevy::render::renderer::RenderContext,
    buffers: Res<bevy::render::batching::gpu_preprocessing::IndirectParametersBuffers>,
    staging: Res<Staging>,
) {
    let Some(opaque) = buffers.get(&std::any::TypeId::of::<bevy::core_pipeline::core_3d::Opaque3d>()) else {
        return;
    };
    let (Some(data), Some(batch_sets), Some(data_copy), Some(batch_sets_copy)) = (
        opaque.indexed.data_buffer(),
        opaque.indexed.batch_sets_buffer(),
        staging.data.as_ref(),
        staging.batch_sets.as_ref(),
    ) else {
        return;
    };

    let encoder = ctx.command_encoder();
    encoder.copy_buffer_to_buffer(data, 0, data_copy, 0, data.size().min(data_copy.size()));
    encoder.copy_buffer_to_buffer(batch_sets, 0, batch_sets_copy, 0, batch_sets.size().min(batch_sets_copy.size()));
}

/// Maps last frame's copies and keeps what they hold once the GPU has written them, and takes over
/// whether the game counts this frame.
#[cfg(feature = "render")]
fn read_back(
    asked: bevy::render::Extract<Option<Res<Counting>>>,
    mut counting: ResMut<Counting>,
    mut staging: ResMut<Staging>,
    counts: Res<Counts>,
) {
    *counting = asked.as_deref().copied().unwrap_or_default();

    let (Some(data), Some(batch_sets)) = (staging.data.take(), staging.batch_sets.take()) else {
        return;
    };

    let instances = counts.clone();
    map(data, move |bytes| {
        let mut read = instances.0.lock().unwrap_or_else(|poisoned| poisoned.into_inner());
        read.instances = instance_counts(bytes);
        read.arrived = true;
    });

    let draws = counts.clone();
    map(batch_sets, move |bytes| {
        if let Some(first) = bytes.get(..4) {
            let mut read = draws.0.lock().unwrap_or_else(|poisoned| poisoned.into_inner());
            read.draws = u32::from_ne_bytes([first[0], first[1], first[2], first[3]]);
        }
    });
}

/// Maps `buffer` and hands its bytes to `then` once they are there.
#[cfg(feature = "render")]
fn map(buffer: Buffer, then: impl FnOnce(&[u8]) + Send + 'static) {
    let mapped = buffer.clone();
    buffer.slice(..).map_async(MapMode::Read, move |result| {
        if result.is_err() {
            return;
        }
        if let Ok(view) = mapped.slice(..).get_mapped_range() {
            then(&view);
        }
        mapped.unmap();
    });
}

/// Adds what counts the meshes drawn. Called from [`super::material::install`].
#[cfg(feature = "render")]
pub(super) fn install(app: &mut bevy::app::App) {
    use bevy::core_pipeline::{Core3d, Core3dSystems};
    use bevy::ecs::schedule::IntoScheduleConfigs;
    use bevy::render::{Render, RenderApp, RenderStartup, RenderSystems};

    let counts = Counts::default();
    app.insert_resource(counts.clone());

    if let Some(render_app) = app.get_sub_app_mut(RenderApp) {
        render_app
            .insert_resource(counts)
            .init_resource::<Counting>()
            .init_resource::<Staging>()
            .add_systems(RenderStartup, note_support)
            .add_systems(bevy::render::ExtractSchedule, read_back)
            .add_systems(Render, make_staging.in_set(RenderSystems::PrepareResourcesFlush))
            .add_systems(
                Core3d,
                copy_parameters
                    .after(Core3dSystems::MainPass)
                    .before(Core3dSystems::PostProcess),
            );
    }
}

#[cfg(all(test, feature = "render"))]
mod tests {
    /// Two draws' parameters, of three instances and of one, a third draw past the count left out.
    #[test]
    fn the_meshes_drawn_are_the_instances_of_the_counted_draws() {
        let bytes: Vec<u8> = [[36u32, 3, 0, 0, 0], [6, 1, 36, 0, 3], [6, 9, 0, 0, 0]]
            .iter()
            .flatten()
            .flat_map(|value| value.to_ne_bytes())
            .collect();

        let instances = super::instance_counts(&bytes);
        assert_eq!(instances, [3, 1, 9]);
        assert_eq!(super::total(&instances, 2), 4);
        assert_eq!(super::total(&instances, 0), 0);
    }
}
