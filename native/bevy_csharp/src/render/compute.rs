//! Compute: Slang programs run on the GPU outside of any picture, over buffers and images the game
//! made.
//!
//! A buffer is a `ShaderBuffer` asset, which Bevy keeps on the GPU between frames, so what one
//! dispatch writes the next reads, and a material or a pass handed the same buffer draws from it
//! without anything crossing back to the CPU. That is what a particle system, a cloth, a boids
//! flock or a fluid wants, because the state then lives where it is simulated and where it is
//! drawn.
//!
//! **Dispatching.** A dispatch is asked for from a system and runs once, that frame, before any
//! camera draws. A simulation asks every frame, which leaves how often it steps to the game rather
//! than to the bridge. Dispatches run in the order they were asked for, each seeing what the one
//! before wrote.
//!
//! **What a compute shader reads.** Group zero is whatever it declares: numbers, buffers read or
//! written, images read or written in any format, samplers, arrays of any of them. It is laid out
//! from the shader's reflection and filled by name. Group one is time, at binding two, which
//! `bcs_compute` declares. A dispatch that runs on a camera instead reads the camera's inputs there
//! (see [`super::views`]), which hold time at the same binding.
//!
//! **Reading a buffer back.** Asking copies it off the GPU after the frame's work, and the bytes
//! arrive a frame or two later, which is the latency of any readback.
//!
//! **A buffer's size is fixed** when it is made. Writing replaces its contents in place, and a
//! shorter write is padded with zeros. Growing it would mean a new GPU buffer, and a material
//! handed the old one would go on drawing from it, since Bevy does not prepare a material again
//! when a buffer it reads is replaced.

#![cfg(feature = "render")]

use std::collections::HashMap;

use bevy::app::App;
use bevy::asset::{Assets, Handle, RenderAssetUsages};
use bevy::core_pipeline::schedule::camera_driver;
use bevy::ecs::entity::Entity;
use bevy::ecs::observer::On;
use bevy::ecs::resource::Resource;
use bevy::ecs::schedule::IntoScheduleConfigs;
use bevy::ecs::system::{Commands, Res, ResMut};
use bevy::ecs::world::World;
use bevy::image::Image;
use bevy::render::globals::{GlobalsBuffer, GlobalsUniform};
use bevy::render::gpu_readback::{Readback, ReadbackComplete};
use bevy::render::render_asset::RenderAssets;
use bevy::render::render_resource::{
    BindGroup, BindGroupEntry, BindGroupLayoutDescriptor, BindGroupLayoutEntry, BindingType,
    BufferBindingType, BufferUsages, CachedComputePipelineId, ComputePassDescriptor,
    ComputePipelineDescriptor, Extent3d, PipelineCache, ShaderStages, ShaderType,
    TextureDimension, TextureFormat, TextureUsages,
};
use bevy::render::renderer::{RenderContext, RenderDevice, RenderGraph, RenderGraphSystems};
use bevy::render::storage::{GpuShaderBuffer, ShaderBuffer};
use bevy::render::texture::{FallbackImage, GpuImage};
use bevy::render::{ExtractSchedule, MainWorld, Render, RenderApp, RenderStartup, RenderSystems};

use super::material::say_once;
use super::programs::{self, Role};
use super::values::{PackContext, PackError, Stand, Values, pack};
use crate::interop::status;

/// The smallest a buffer is made, because a buffer has to have a size to be bound.
const SMALLEST_BUFFER: u64 = 16;

/// What a buffer the game made can be used as: read and written by a shader, copied to and from,
/// and read as vertices or as the arguments of an indirect draw.
pub fn buffer_usage() -> BufferUsages {
    BufferUsages::STORAGE
        | BufferUsages::COPY_DST
        | BufferUsages::COPY_SRC
        | BufferUsages::VERTEX
        | BufferUsages::INDIRECT
}

/// One dispatch as it was asked for.
#[derive(Clone)]
pub struct Dispatch {
    pub program: u32,
    pub values: Values,
    pub workgroups: [u32; 3],
    /// A buffer and an offset in it holding the workgroup counts, written on the GPU, in place of
    /// `workgroups`.
    pub indirect: Option<(Handle<ShaderBuffer>, u64)>,
}

/// The dispatches asked for this frame, in order.
#[derive(Resource, Default)]
pub struct DispatchQueue(pub Vec<Dispatch>);

/// The same, taken over by the render world at extraction.
#[derive(Resource, Default)]
struct ExtractedDispatches(Vec<Dispatch>);

/// What every dispatch is built from.
#[derive(Resource)]
struct ComputePipelines {
    /// Group one, which is time and nothing else.
    inputs: BindGroupLayoutDescriptor,
    /// One pipeline per program and version of it.
    pipelines: HashMap<(u32, u32), CachedComputePipelineId>,
}

/// A dispatch ready to run.
struct PreparedDispatch {
    pipeline: CachedComputePipelineId,
    own: BindGroup,
    inputs: BindGroup,
    workgroups: [u32; 3],
    indirect: Option<(bevy::render::render_resource::Buffer, u64)>,
}

#[derive(Resource, Default)]
struct PreparedDispatches(Vec<PreparedDispatch>);

/// Buffers being read back, and what has arrived.
#[derive(Resource, Default)]
pub struct BufferReads {
    next: i32,
    pending: HashMap<Entity, i32>,
    done: HashMap<i32, Vec<u8>>,
}

/// Adds what runs dispatches and reads buffers back.
pub fn install(app: &mut App) {
    app.init_resource::<DispatchQueue>();
    app.init_resource::<BufferReads>();
    app.add_observer(on_readback);

    let Some(render_app) = app.get_sub_app_mut(RenderApp) else {
        return;
    };

    render_app
        .init_resource::<ExtractedDispatches>()
        .init_resource::<PreparedDispatches>()
        .add_systems(RenderStartup, init_pipelines)
        .add_systems(ExtractSchedule, extract_dispatches)
        .add_systems(
            Render,
            prepare_dispatches.in_set(RenderSystems::PrepareBindGroups),
        )
        .add_systems(
            RenderGraph,
            run_dispatches
                .in_set(RenderGraphSystems::Render)
                .before(camera_driver),
        );
}

fn init_pipelines(mut commands: Commands) {
    // At binding two, where a dispatch on a camera reads time as well, so a shader importing
    // `bcs_compute` runs the same either way.
    let globals = BindGroupLayoutEntry {
        binding: 2,
        visibility: ShaderStages::COMPUTE,
        ty: BindingType::Buffer {
            ty: BufferBindingType::Uniform,
            has_dynamic_offset: false,
            min_binding_size: Some(GlobalsUniform::min_size()),
        },
        count: None,
    };

    commands.insert_resource(ComputePipelines {
        inputs: BindGroupLayoutDescriptor::new("bcs_compute_inputs", &[globals]),
        pipelines: HashMap::new(),
    });
}

/// Takes the frame's dispatches over from the main world, leaving its queue empty for the next.
fn extract_dispatches(
    mut main_world: ResMut<MainWorld>,
    mut extracted: ResMut<ExtractedDispatches>,
) {
    extracted.0 = main_world
        .get_resource_mut::<DispatchQueue>()
        .map(|mut queue| std::mem::take(&mut queue.0))
        .unwrap_or_default();
}

/// The layout of a program's own group for a dispatch.
fn own_layout(layout: &super::reflect::Layout) -> BindGroupLayoutDescriptor {
    BindGroupLayoutDescriptor::new("bcs_compute_own", &layout.entries(ShaderStages::COMPUTE))
}

/// The compute pipeline for a program's current version, queued the first time it is asked for.
fn pipeline_for(
    pipelines: &mut ComputePipelines,
    cache: &PipelineCache,
    id: u32,
) -> Option<(CachedComputePipelineId, programs::PipelineProgram)> {
    let program = programs::lookup(id)?;
    let layout = program.compute.clone()?;
    let stage = program.stages[Role::Compute as usize].clone()?;

    // One reading a camera's inputs has nothing to read here, and its pipeline is built against
    // the camera's inputs instead, by `super::views`.
    if layout.reads_view {
        return None;
    }

    let key = (id, program.generation);

    if let Some(pipeline) = pipelines.pipelines.get(&key) {
        return Some((*pipeline, program));
    }

    let pipeline = cache.queue_compute_pipeline(ComputePipelineDescriptor {
        label: Some("bcs_compute".into()),
        layout: vec![own_layout(&layout), pipelines.inputs.clone()],
        immediate_size: 0,
        shader: stage.shader,
        shader_defs: Vec::new(),
        entry_point: None,
        zero_initialize_workgroup_memory: true,
    });

    pipelines.pipelines.insert(key, pipeline);
    Some((pipeline, program))
}

#[allow(clippy::too_many_arguments)]
fn prepare_dispatches(
    mut pipelines: ResMut<ComputePipelines>,
    mut prepared: ResMut<PreparedDispatches>,
    extracted: Res<ExtractedDispatches>,
    cache: Res<PipelineCache>,
    render_device: Res<RenderDevice>,
    images: Res<RenderAssets<GpuImage>>,
    buffers: Res<RenderAssets<GpuShaderBuffer>>,
    fallback: Res<FallbackImage>,
    stand: Option<Res<Stand>>,
    globals: Res<GlobalsBuffer>,
) {
    prepared.0.clear();

    // Every program with a compute stage has the pipeline of its current version built as soon as
    // it exists, rather than when it is first dispatched, so a dispatch made once the program
    // reports ready runs rather than being dropped while its pipeline compiles.
    for id in 0..programs::table_len() {
        if let Some((pipeline, program)) = pipeline_for(&mut pipelines, &cache, id)
            && cache.get_compute_pipeline(pipeline).is_some()
        {
            programs::mark_compute_ready(id, program.generation);
        }
    }

    let (Some(stand), Some(globals)) = (stand, globals.buffer.binding()) else {
        return;
    };

    let inputs = render_device.create_bind_group(
        "bcs_compute_inputs",
        &cache.get_bind_group_layout(&pipelines.inputs),
        &[BindGroupEntry {
            binding: 2,
            resource: globals,
        }],
    );

    for dispatch in &extracted.0 {
        let Some((pipeline, program)) = pipeline_for(&mut pipelines, &cache, dispatch.program)
        else {
            // Silent while the program is still compiling, which is every program's first few
            // frames and says nothing wrong about the dispatch.
            let found = programs::lookup(dispatch.program).filter(|program| program.generation > 0);

            if let Some(program) = found {
                if program.stages[Role::Compute as usize].is_none() {
                    say_once(format!(
                        "A dispatch named shader program {}, which has no compute stage, so it \
                         does nothing.",
                        dispatch.program
                    ));
                } else if program.compute.as_ref().is_some_and(|layout| layout.reads_view) {
                    say_once(format!(
                        "Shader program {} reads a camera's inputs through bcs_pass, so it runs \
                         only on a camera. Give it to Shaders.SetViewDispatches rather than \
                         Shaders.Dispatch.",
                        dispatch.program
                    ));
                }
            }

            continue;
        };

        let layout = program.compute.clone().expect("a compute stage has a layout");

        let context = PackContext {
            device: &render_device,
            images: &images,
            buffers: &buffers,
            fallback: &fallback,
            stand: &stand,
            view: None,
        };

        // A buffer or an image that has not reached the GPU yet holds the dispatch back rather
        // than running it against a stand-in.
        let packed = match pack(&layout, &dispatch.values, &context) {
            Ok(packed) => packed,
            Err(PackError::NotReady) => continue,
            Err(PackError::Missing(message)) => {
                say_once(format!(
                    "A dispatch of shader program {}: {message}",
                    dispatch.program
                ));
                continue;
            }
        };

        for problem in &packed.problems {
            say_once(format!(
                "A dispatch of shader program {}: {problem}",
                dispatch.program
            ));
        }

        let own = packed.bind_group(
            &render_device,
            "bcs_compute_own",
            &cache.get_bind_group_layout(&own_layout(&layout)),
        );

        // A buffer of counts that has not reached the GPU holds the dispatch back, as any other
        // buffer does.
        let indirect = match &dispatch.indirect {
            Some((handle, offset)) => match buffers.get(handle) {
                Some(gpu) => Some((gpu.buffer.clone(), *offset)),
                None => continue,
            },
            None => None,
        };

        prepared.0.push(PreparedDispatch {
            pipeline,
            own,
            inputs: inputs.clone(),
            workgroups: dispatch.workgroups,
            indirect,
        });
    }
}

/// Runs the frame's dispatches, before any camera draws, so what they write is what the frame
/// shows.
fn run_dispatches(
    prepared: Res<PreparedDispatches>,
    cache: Res<PipelineCache>,
    mut ctx: RenderContext,
) {
    if prepared.0.is_empty() {
        return;
    }

    for dispatch in &prepared.0 {
        // Still compiling. Dropped rather than kept for later, because a dispatch is about the
        // frame it was asked for in.
        let Some(pipeline) = cache.get_compute_pipeline(dispatch.pipeline) else {
            continue;
        };

        let [x, y, z] = dispatch.workgroups;

        let mut pass = ctx
            .command_encoder()
            .begin_compute_pass(&ComputePassDescriptor {
                label: Some("bcs_compute"),
                timestamp_writes: None,
            });

        pass.set_pipeline(pipeline);
        pass.set_bind_group(0, &dispatch.own, &[]);
        pass.set_bind_group(1, &dispatch.inputs, &[]);

        match &dispatch.indirect {
            Some((buffer, offset)) => pass.dispatch_workgroups_indirect(buffer, *offset),
            None => pass.dispatch_workgroups(x, y, z),
        }
    }
}

/// Keeps what a readback brought back, if it was one of ours.
fn on_readback(event: On<ReadbackComplete>, mut reads: ResMut<BufferReads>, mut commands: Commands) {
    let entity = event.entity;

    let Some(ticket) = reads.pending.remove(&entity) else {
        return;
    };

    reads.done.insert(ticket, event.data.clone());

    // A readback component reads again every frame it is there, and one answer is what was asked
    // for.
    if let Ok(mut entity) = commands.get_entity(entity) {
        entity.despawn();
    }
}

// -- What the entry points do

/// A buffer of `size` bytes holding `bytes` at its start, with the rest zero.
fn sized(bytes: &[u8], size: u64) -> Vec<u8> {
    let mut data = bytes.to_vec();
    data.resize(size as usize, 0);
    data
}

/// The size a buffer asked to hold `wanted` bytes is made, which is a whole number of words and
/// never less than sixteen.
pub fn buffer_size(wanted: u64) -> u64 {
    wanted.max(SMALLEST_BUFFER).next_multiple_of(4)
}

/// Makes a buffer of `size` bytes starting with `bytes`, and answers its asset key.
pub fn create_buffer(world: &mut World, bytes: &[u8], size: u64) -> i32 {
    let size = buffer_size(size.max(bytes.len() as u64));

    let Some(mut assets) = world.get_resource_mut::<Assets<ShaderBuffer>>() else {
        return status::UNSUPPORTED;
    };

    let mut buffer = ShaderBuffer::new(&sized(bytes, size), RenderAssetUsages::default());
    buffer.buffer_description.label = Some("bcs_shader_buffer");
    buffer.buffer_description.size = size;
    buffer.buffer_description.usage = buffer_usage();

    let handle = assets.add(buffer);
    crate::assets::insert_handle(world, handle.untyped())
}

/// The buffer behind an asset key, if it is one.
pub fn buffer_handle(world: &World, key: i32) -> Option<Handle<ShaderBuffer>> {
    crate::assets::clone_handle(world, key)?
        .try_typed::<ShaderBuffer>()
        .ok()
}

/// Replaces a buffer's contents with `bytes`, padded with zeros to its size.
pub fn write_buffer(world: &mut World, key: i32, bytes: &[u8]) -> i32 {
    let Some(handle) = buffer_handle(world, key) else {
        return status::NO_COMPONENT;
    };

    let Some(mut assets) = world.get_resource_mut::<Assets<ShaderBuffer>>() else {
        return status::UNSUPPORTED;
    };

    let Some(mut buffer) = assets.get_mut(&handle) else {
        return status::NO_COMPONENT;
    };

    let size = buffer.buffer_description.size;

    if bytes.len() as u64 > size {
        return status::BUFFER_TOO_SMALL;
    }

    buffer.data = Some(sized(bytes, size));
    status::OK
}

/// A buffer's size in bytes.
pub fn size_of_buffer(world: &World, key: i32) -> i32 {
    let Some(handle) = buffer_handle(world, key) else {
        return status::NO_COMPONENT;
    };

    world
        .get_resource::<Assets<ShaderBuffer>>()
        .and_then(|assets| assets.get(&handle))
        .map(|buffer| buffer.buffer_description.size.min(i32::MAX as u64) as i32)
        .unwrap_or(status::NO_COMPONENT)
}

/// Starts copying a buffer back, and answers the ticket its bytes will arrive under.
pub fn read_buffer(world: &mut World, key: i32) -> i32 {
    let Some(handle) = buffer_handle(world, key) else {
        return status::NO_COMPONENT;
    };

    let entity = world.spawn(Readback::buffer(handle)).id();

    let Some(mut reads) = world.get_resource_mut::<BufferReads>() else {
        return status::UNSUPPORTED;
    };

    reads.next += 1;
    let ticket = reads.next;
    reads.pending.insert(entity, ticket);
    ticket
}

/// Takes what a read brought back, if it has arrived and fits.
///
/// The text convention with one addition: [`status::NOT_PRESENT`] while the bytes are still on
/// their way. Bytes that do not fit are left for a second call with a buffer of the size returned.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null.
pub unsafe fn take_read(world: &mut World, ticket: i32, out: *mut u8, capacity: i32) -> i32 {
    let Some(mut reads) = world.get_resource_mut::<BufferReads>() else {
        return status::UNSUPPORTED;
    };

    let Some(bytes) = reads.done.get(&ticket) else {
        return if reads.pending.values().any(|pending| *pending == ticket) {
            status::NOT_PRESENT
        } else {
            status::NO_COMPONENT
        };
    };

    let needed = bytes.len() as i32;

    if out.is_null() || capacity < needed {
        return needed;
    }

    // SAFETY: the caller promised `capacity` writable bytes at `out`, and `needed` fits in them.
    unsafe { core::ptr::copy_nonoverlapping(bytes.as_ptr(), out, bytes.len()) };
    reads.done.remove(&ticket);
    needed
}

/// The formats an image a shader writes can be made in, by the number the entry points take.
///
/// In the order the managed side's enum lists them, which is the order of how often they are
/// wanted rather than of anything the GPU cares about.
pub const IMAGE_FORMATS: [(TextureFormat, usize); 10] = [
    (TextureFormat::Rgba8Unorm, 4),
    (TextureFormat::Rgba16Float, 8),
    (TextureFormat::Rgba32Float, 16),
    (TextureFormat::R32Float, 4),
    (TextureFormat::R32Uint, 4),
    (TextureFormat::R32Sint, 4),
    (TextureFormat::Rg32Float, 8),
    (TextureFormat::Rgba32Uint, 16),
    (TextureFormat::Rgba8Uint, 4),
    (TextureFormat::R16Float, 2),
];

/// Makes an image a compute shader can write and anything can sample, and answers its asset key.
///
/// `depth` above one makes a 3D image that many deep, which is what a shader writing a
/// `RWTexture3D` wants. It starts as zeros.
pub fn create_image(world: &mut World, width: u32, height: u32, depth: u32, format: i32) -> i32 {
    create_image_from(world, width, height, depth, format, None)
}

/// Makes an image as [`create_image`] does, starting with `texels` rather than zeros.
///
/// `texels` is every texel in the format's own layout, row after row and slice after slice, so a
/// heightmap of floats is `width * height` floats as they are. Anything but exactly that many bytes
/// is refused, since a picture read with the wrong stride is noise rather than an error.
pub fn create_image_from(
    world: &mut World,
    width: u32,
    height: u32,
    depth: u32,
    format: i32,
    texels: Option<&[u8]>,
) -> i32 {
    let Some(&(format, texel_bytes)) = usize::try_from(format)
        .ok()
        .and_then(|index| IMAGE_FORMATS.get(index))
    else {
        return status::NULL_ARG;
    };

    if width == 0 || height == 0 || depth == 0 {
        return status::NULL_ARG;
    }

    let size = Extent3d {
        width,
        height,
        depth_or_array_layers: depth,
    };

    let dimension = if depth > 1 {
        TextureDimension::D3
    } else {
        TextureDimension::D2
    };

    let mut image = match texels {
        Some(texels) => {
            let wanted = width as usize * height as usize * depth as usize * texel_bytes;

            if texels.len() != wanted {
                return status::BUFFER_TOO_SMALL;
            }

            Image::new(size, dimension, texels.to_vec(), format, RenderAssetUsages::default())
        }
        None => Image::new_fill(
            size,
            dimension,
            &vec![0u8; texel_bytes],
            format,
            RenderAssetUsages::default(),
        ),
    };

    image.texture_descriptor.usage = TextureUsages::TEXTURE_BINDING
        | TextureUsages::STORAGE_BINDING
        | TextureUsages::COPY_SRC
        | TextureUsages::COPY_DST;

    let Some(mut assets) = world.get_resource_mut::<Assets<Image>>() else {
        return status::UNSUPPORTED;
    };

    let handle = assets.add(image);
    crate::assets::insert_handle(world, handle.untyped())
}

/// Queues a dispatch for this frame.
pub fn queue(world: &mut World, dispatch: Dispatch) -> i32 {
    match world.get_resource_mut::<DispatchQueue>() {
        Some(mut queue) => {
            queue.0.push(dispatch);
            status::OK
        }
        None => status::UNSUPPORTED,
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn a_buffer_is_whole_words_and_never_empty() {
        assert_eq!(buffer_size(0), 16);
        assert_eq!(buffer_size(17), 20);
        assert_eq!(buffer_size(64), 64);
    }

    #[test]
    fn a_short_write_is_padded_with_zeros() {
        assert_eq!(sized(&[1, 2], 4), vec![1, 2, 0, 0]);
    }
}
