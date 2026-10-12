//! What a game does about an error the renderer reports, and the errors it can cause on purpose to
//! see that it copes, as Bevy's render_recovery example causes them.
//!
//! The handling itself is in `render_errors`, which Bevy calls with each error. This is the part a
//! game reaches, in every profile, a headless one answering that it has no renderer.

use crate::interop::status;

/// The error a game asked to cause, as [`bcs_render_cause_error`] numbers it, which the render
/// world causes as it next runs, or `0` for none.
#[cfg(feature = "render")]
static PENDING: std::sync::atomic::AtomicI32 = std::sync::atomic::AtomicI32::new(0);

/// Says what an error the renderer reports does: `0` closes the app with an error, Bevy's own
/// answer, `1` logs it and draws on as if nothing happened, `2` keeps the app running with drawing
/// stopped, and `3` makes the renderer again, a device and all, with the backend the app started
/// with, and draws on with that. Bevy asks again every frame while drawing is stopped, so a policy
/// set then takes over from that frame.
///
/// A validation error is ignored whatever this says while `bcs_shader_keep_rendering_after_errors`
/// is set, for a shader being edited. Answers [`status::NULL_ARG`] for any other value.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_error_policy(policy: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = policy;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use super::render_errors::{IGNORE, QUIT, RECOVER, STOP_RENDERING};

            if ![QUIT, IGNORE, STOP_RENDERING, RECOVER].contains(&policy) {
                return status::NULL_ARG;
            }
            super::render_errors::set_policy(policy);
            status::OK
        }
    })
}

/// Answers what an error the renderer reports does now, as [`bcs_render_set_error_policy`] takes it.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_error_policy() -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            super::render_errors::policy()
        }
    })
}

/// Answers how many errors the renderer has reported since the library was loaded, one counted
/// once however many frames Bevy hands it back while drawing is stopped.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_error_count() -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            i32::try_from(super::render_errors::count()).unwrap_or(i32::MAX)
        }
    })
}

/// Causes an error on the GPU, for a game to see what its policy does with one, as Bevy's example
/// causes them: `1` running out of memory, by asking for far more textures than a device holds,
/// `2` a validation error, from a buffer too large to make, `3` a lost device, by destroying it,
/// and `4` a lost device, by a compute shader that never ends, which the system resets the GPU
/// over.
///
/// The error is caused in the render world at the end of the next frame with no pipeline compiling,
/// where Bevy's example causes it (`cause_pending` says why it waits), and arrives as the renderer
/// next reports, a frame or so later.
///
/// The first and the last reach the machine's own graphics, which may need a restart to recover
/// from either, as Bevy warns of its own. Answers [`status::NULL_ARG`] for any other value and
/// [`status::UNSUPPORTED`] where the app draws nothing.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_cause_error(cause: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = cause;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if !(1..=4).contains(&cause) {
                return status::NULL_ARG;
            }

            crate::state::with_world(|world| {
                if !world.contains_resource::<bevy::render::renderer::RenderDevice>() {
                    return status::UNSUPPORTED;
                }

                PENDING.store(cause, std::sync::atomic::Ordering::Relaxed);
                status::OK
            })
        }
    })
}

/// Causes the error a game asked for, in the render world, in a frame with no pipeline compiling.
///
/// wgpu's error scopes are one stack for every thread in this build, its `std` feature being off,
/// and Bevy's pipeline cache opens one around each shader it compiles on a pool thread. A
/// validation error raised while one compiles is taken by that scope and reported as a shader that
/// failed, so the game's policy never hears of it, which a test met one run in three. So the cause
/// waits for a frame with nothing compiling, and the system is exclusive, so no compile starts
/// beside it.
#[cfg(feature = "render")]
fn cause_pending(world: &mut bevy::ecs::world::World) {
    use bevy::render::render_resource::{CachedPipelineState, PipelineCache};
    use bevy::render::renderer::{RenderDevice, RenderQueue};

    let compiling = world.get_resource::<PipelineCache>().is_some_and(|cache| {
        cache
            .pipelines()
            .any(|pipeline| matches!(pipeline.state, CachedPipelineState::Creating(_)))
    });
    if compiling {
        return;
    }

    let cause = PENDING.swap(0, std::sync::atomic::Ordering::Relaxed);
    let (Some(device), Some(queue)) = (world.get_resource::<RenderDevice>(), world.get_resource::<RenderQueue>()) else {
        return;
    };

    match cause {
        1 => run_out_of_memory(device),
        2 => fail_validation(device),
        3 => destroy_device(device),
        4 => loop_forever(device, queue),
        _ => {}
    }
}

/// Adds what causes an error a game asked for, and what waits for the pipelines still compiling
/// as the app exits. Called from [`super::material::install`].
#[cfg(feature = "render")]
pub(super) fn install(app: &mut bevy::app::App) {
    // A cause the last app asked for and ended before it was caused is the last app's, which would
    // otherwise go off in this one, in a frame of its own with nothing compiling.
    PENDING.store(0, std::sync::atomic::Ordering::Relaxed);
    super::render_errors::begin_app();

    if let Some(render_app) = app.get_sub_app_mut(bevy::render::RenderApp) {
        // At the end of the frame, since a device destroyed sooner would leave the frame's later
        // systems making buffers on it, which wgpu answers with a panic, and the error is read as
        // the next frame begins.
        render_app
            .add_systems(
                bevy::render::Render,
                bevy::ecs::schedule::IntoScheduleConfigs::in_set(cause_pending, bevy::render::RenderSystems::Cleanup),
            )
            .add_systems(bevy::render::ExtractSchedule, super::render_errors::finish_pipelines_at_exit);
    }
}

/// Asks for sixty-four pictures of half a gigabyte each, which no device holds, and lets them go.
#[cfg(feature = "render")]
fn run_out_of_memory(device: &bevy::render::renderer::RenderDevice) {
    use bevy::render::render_resource::{
        Extent3d, TextureDescriptor, TextureDimension, TextureFormat, TextureUsages,
    };

    let textures: Vec<_> = (0..64)
        .map(|_| {
            device.create_texture(&TextureDescriptor {
                label: None,
                size: Extent3d { width: 8192, height: 8192, depth_or_array_layers: 1 },
                mip_level_count: 1,
                sample_count: 1,
                dimension: TextureDimension::D2,
                format: TextureFormat::Rgba16Float,
                usage: TextureUsages::RENDER_ATTACHMENT,
                view_formats: &[],
            })
        })
        .collect();
    drop(textures);
}

/// Asks for a buffer of half the addressable bytes, which validation refuses.
#[cfg(feature = "render")]
fn fail_validation(device: &bevy::render::renderer::RenderDevice) {
    use bevy::render::render_resource::{BufferDescriptor, BufferUsages};

    device.create_buffer(&BufferDescriptor {
        label: None,
        size: 1 << 63,
        usage: BufferUsages::COPY_SRC,
        mapped_at_creation: false,
    });
}

/// Destroys the device and waits for it to be gone, which loses it.
#[cfg(feature = "render")]
fn destroy_device(device: &bevy::render::renderer::RenderDevice) {
    device.wgpu_device().destroy();
    let _ = device.poll(bevy::render::render_resource::PollType::wait_indefinitely());
}

/// Runs a compute shader that loops forever, which the system resets the GPU over, losing the
/// device.
#[cfg(feature = "render")]
fn loop_forever(
    device: &bevy::render::renderer::RenderDevice,
    queue: &bevy::render::renderer::RenderQueue,
) {
    use bevy::render::render_resource::{
        CommandEncoderDescriptor, ComputePassDescriptor, PipelineLayoutDescriptor, PollType,
        RawComputePipelineDescriptor, ShaderModuleDescriptor, ShaderSource,
    };

    let module = device.create_and_validate_shader_module(ShaderModuleDescriptor {
        label: Some("bcs_endless_loop"),
        source: ShaderSource::Wgsl(
            "@compute @workgroup_size(1, 1, 1) fn main() { loop { workgroupBarrier(); } }".into(),
        ),
    });

    let layout = device.create_pipeline_layout(&PipelineLayoutDescriptor {
        label: Some("bcs_endless_loop"),
        bind_group_layouts: &[],
        immediate_size: 0,
    });

    let pipeline = device.create_compute_pipeline(&RawComputePipelineDescriptor {
        label: Some("bcs_endless_loop"),
        layout: Some(&layout),
        module: &module,
        entry_point: Some("main"),
        compilation_options: Default::default(),
        cache: None,
    });

    let mut encoder = device.create_command_encoder(&CommandEncoderDescriptor::default());
    {
        let mut pass = encoder.begin_compute_pass(&ComputePassDescriptor::default());
        pass.set_pipeline(&pipeline);
        pass.dispatch_workgroups(1, 1, 1);
    }
    let _ = device.poll(PollType::wait_indefinitely());
    queue.submit([encoder.finish()]);
}
