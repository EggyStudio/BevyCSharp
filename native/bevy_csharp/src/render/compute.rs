//! Compute: programs run on the GPU outside of any picture, over buffers the game made.
//!
//! A buffer is a `ShaderBuffer` asset, which Bevy keeps on the GPU between frames, so what one
//! dispatch writes the next reads, and a material or a pass bound to the same buffer draws from
//! it without anything crossing back to the CPU. That is what a particle system, a cloth, a boids
//! flock or a fluid wants, because the state then lives where it is simulated and where it is
//! drawn.
//!
//! **Dispatching.** A dispatch is asked for from a system and runs once, that frame, before any
//! camera draws. A simulation asks every frame, which leaves how often it steps to the game rather
//! than to the bridge. Dispatches run in the order they were asked for, each seeing what the one
//! before wrote.
//!
//! **What a compute shader reads.** Group zero:
//!
//! | binding | holds |
//! |---|---|
//! | 0 | sixty-four floats, as sixteen `vec4` in a uniform |
//! | 1 to 4 | four storage buffers, read and written |
//! | 5 | Bevy's globals, which is where time is |
//! | 6 | an image written to, eight bits a channel (`rgba8unorm`) |
//! | 7 | an image written to, a half float a channel (`rgba16float`) |
//! | 8, 9 | two images read, sampled like a material's textures |
//! | 10 | a linear sampler for them |
//!
//! An image written to is one made by [`create_image`], because a storage texture's format is part
//! of the binding and the image has to be made with storage usage. A material or a pass samples
//! the same image afterwards, which is how a compute shader draws into a picture.
//!
//! **Reading a buffer back.** Asking copies it off the GPU after the frame's work, and the bytes
//! arrive a frame or two later, which is the latency of any readback. A test or a tool wants it;
//! a game that wants the answer every frame wants to keep the work on the GPU instead.
//!
//! **A buffer's size is fixed** when it is made. Writing replaces its contents in place, and a
//! shorter write is padded with zeros. Growing it would mean a new GPU buffer, and a material
//! bound to the old one would go on drawing from it, since Bevy does not prepare a material again
//! when a buffer it reads is replaced.

#![cfg(feature = "render")]

use std::collections::HashMap;
use std::num::NonZeroU64;

use bevy::app::App;
use bevy::asset::{Assets, Handle, RenderAssetUsages};
use bevy::core_pipeline::schedule::camera_driver;
use bevy::ecs::entity::Entity;
use bevy::ecs::observer::On;
use bevy::ecs::resource::Resource;
use bevy::ecs::schedule::IntoScheduleConfigs;
use bevy::ecs::system::{Commands, Res, ResMut};
use bevy::ecs::world::World;
use bevy::render::globals::{GlobalsBuffer, GlobalsUniform};
use bevy::render::gpu_readback::{Readback, ReadbackComplete};
use bevy::render::render_asset::RenderAssets;
use bevy::image::Image;
use bevy::render::render_resource::{
    BindGroup, BindGroupEntry, BindGroupLayoutDescriptor, BindGroupLayoutEntry, BindingResource,
    BindingType, Buffer, BufferBindingType, BufferDescriptor, BufferInitDescriptor, BufferUsages,
    CachedComputePipelineId, ComputePassDescriptor, ComputePipelineDescriptor, Extent3d,
    FilterMode, PipelineCache, Sampler, SamplerBindingType, SamplerDescriptor, ShaderStages,
    ShaderType, StorageTextureAccess, TextureDescriptor, TextureDimension, TextureFormat,
    TextureSampleType, TextureUsages, TextureView, TextureViewDescriptor, TextureViewDimension,
};
use bevy::render::texture::{FallbackImage, GpuImage};
use bevy::render::renderer::{RenderContext, RenderDevice, RenderGraph, RenderGraphSystems};
use bevy::render::storage::{GpuShaderBuffer, ShaderBuffer};
use bevy::render::{ExtractSchedule, MainWorld, Render, RenderApp, RenderStartup, RenderSystems};

use super::material::PARAMETER_COUNT;
use super::programs::{self, Role};
use crate::interop::status;

/// How many buffers a dispatch is handed.
pub const COMPUTE_BUFFER_COUNT: usize = 4;

/// How many images a dispatch writes, and how many it reads.
pub const COMPUTE_IMAGE_COUNT: usize = 2;

/// The format of each image a dispatch writes, in binding order.
pub const STORAGE_FORMATS: [TextureFormat; COMPUTE_IMAGE_COUNT] =
    [TextureFormat::Rgba8Unorm, TextureFormat::Rgba16Float];

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
    pub parameters: [f32; PARAMETER_COUNT],
    pub buffers: [Option<Handle<ShaderBuffer>>; COMPUTE_BUFFER_COUNT],
    /// The images written, at bindings six and seven.
    pub images: [Option<Handle<Image>>; COMPUTE_IMAGE_COUNT],
    /// The images read, at bindings eight and nine.
    pub textures: [Option<Handle<Image>>; COMPUTE_IMAGE_COUNT],
    pub workgroups: [u32; 3],
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
    layout: BindGroupLayoutDescriptor,
    pipelines: HashMap<u32, CachedComputePipelineId>,
    /// How many programs have been looked at for a compute stage. A program's stages never change,
    /// so each is looked at once.
    looked_at: u32,
    /// Bound where a dispatch was handed fewer than four buffers.
    empty: Buffer,
    /// Bound where a dispatch writes no image, one of each format.
    empty_images: [TextureView; COMPUTE_IMAGE_COUNT],
    /// What the images read are sampled with.
    sampler: Sampler,
}

/// A dispatch ready to run.
struct PreparedDispatch {
    pipeline: CachedComputePipelineId,
    bind_group: BindGroup,
    workgroups: [u32; 3],
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

fn init_pipelines(mut commands: Commands, render_device: Res<RenderDevice>) {
    let entry = |binding: u32, ty: BindingType| BindGroupLayoutEntry {
        binding,
        visibility: ShaderStages::COMPUTE,
        ty,
        count: None,
    };

    let mut entries = vec![entry(
        0,
        BindingType::Buffer {
            ty: BufferBindingType::Uniform,
            has_dynamic_offset: false,
            min_binding_size: NonZeroU64::new((PARAMETER_COUNT * 4) as u64),
        },
    )];

    for index in 0..COMPUTE_BUFFER_COUNT as u32 {
        entries.push(entry(
            1 + index,
            BindingType::Buffer {
                ty: BufferBindingType::Storage { read_only: false },
                has_dynamic_offset: false,
                min_binding_size: None,
            },
        ));
    }

    entries.push(entry(
        5,
        BindingType::Buffer {
            ty: BufferBindingType::Uniform,
            has_dynamic_offset: false,
            min_binding_size: Some(GlobalsUniform::min_size()),
        },
    ));

    for (index, format) in STORAGE_FORMATS.iter().enumerate() {
        entries.push(entry(
            6 + index as u32,
            BindingType::StorageTexture {
                access: StorageTextureAccess::WriteOnly,
                format: *format,
                view_dimension: TextureViewDimension::D2,
            },
        ));
    }

    for index in 0..COMPUTE_IMAGE_COUNT as u32 {
        entries.push(entry(
            8 + index,
            BindingType::Texture {
                sample_type: TextureSampleType::Float { filterable: true },
                view_dimension: TextureViewDimension::D2,
                multisampled: false,
            },
        ));
    }

    entries.push(entry(10, BindingType::Sampler(SamplerBindingType::Filtering)));

    // One texel of each format, written to by a dispatch that names no image and read by nobody.
    let empty_image = |format: TextureFormat| {
        render_device
            .create_texture(&TextureDescriptor {
                label: Some("bcs_compute_empty_image"),
                size: Extent3d {
                    width: 1,
                    height: 1,
                    depth_or_array_layers: 1,
                },
                mip_level_count: 1,
                sample_count: 1,
                dimension: TextureDimension::D2,
                format,
                usage: TextureUsages::STORAGE_BINDING,
                view_formats: &[],
            })
            .create_view(&TextureViewDescriptor::default())
    };

    commands.insert_resource(ComputePipelines {
        layout: BindGroupLayoutDescriptor::new("bcs_compute_layout", &entries),
        pipelines: HashMap::new(),
        looked_at: 0,
        empty: render_device.create_buffer(&BufferDescriptor {
            label: Some("bcs_compute_empty"),
            size: SMALLEST_BUFFER,
            usage: BufferUsages::STORAGE,
            mapped_at_creation: false,
        }),
        empty_images: STORAGE_FORMATS.map(empty_image),
        sampler: render_device.create_sampler(&SamplerDescriptor {
            label: Some("bcs_compute_sampler"),
            mag_filter: FilterMode::Linear,
            min_filter: FilterMode::Linear,
            ..Default::default()
        }),
    });
}

/// Takes the frame's dispatches over from the main world, leaving its queue empty for the next.
fn extract_dispatches(mut main_world: ResMut<MainWorld>, mut extracted: ResMut<ExtractedDispatches>) {
    extracted.0 = main_world
        .get_resource_mut::<DispatchQueue>()
        .map(|mut queue| std::mem::take(&mut queue.0))
        .unwrap_or_default();
}

fn prepare_dispatches(
    mut pipelines: ResMut<ComputePipelines>,
    mut prepared: ResMut<PreparedDispatches>,
    extracted: Res<ExtractedDispatches>,
    cache: Res<PipelineCache>,
    render_device: Res<RenderDevice>,
    buffers: Res<RenderAssets<GpuShaderBuffer>>,
    images: Res<RenderAssets<GpuImage>>,
    fallback: Res<FallbackImage>,
    globals: Res<GlobalsBuffer>,
) {
    prepared.0.clear();

    // Every program with a compute stage has its pipeline built as soon as it exists, rather than
    // when it is first dispatched, so a dispatch made once the program reports ready runs rather
    // than being dropped while its pipeline compiles.
    let known = programs::table_len();

    for id in pipelines.looked_at..known {
        queue_pipeline(&mut pipelines, &cache, id);
    }

    pipelines.looked_at = known;

    for (id, pipeline) in &pipelines.pipelines {
        if cache.get_compute_pipeline(*pipeline).is_some() {
            programs::mark_compute_ready(*id);
        }
    }

    let Some(globals) = globals.buffer.binding() else {
        return;
    };

    let layout = cache.get_bind_group_layout(&pipelines.layout);

    for dispatch in &extracted.0 {
        let Some(pipeline) = pipelines.pipelines.get(&dispatch.program).copied() else {
            bevy::log::warn_once!(
                "A dispatch named a shader program with no compute stage, so it does nothing."
            );
            continue;
        };

        // A buffer that has not reached the GPU yet, which is a buffer made this frame, holds the
        // dispatch back rather than running it against the empty one in its place.
        let mut bound = Vec::with_capacity(COMPUTE_BUFFER_COUNT);
        let mut waiting = false;

        for handle in &dispatch.buffers {
            match handle {
                Some(handle) => match buffers.get(handle) {
                    Some(gpu) => bound.push(gpu.buffer.clone()),
                    None => waiting = true,
                },
                None => bound.push(pipelines.empty.clone()),
            }
        }

        // The images written have to be the format their binding names and made to be written
        // to, and an image that is not is a dispatch that would fail, so it is skipped with a
        // warning rather than run.
        let mut written = Vec::with_capacity(COMPUTE_IMAGE_COUNT);

        for (index, handle) in dispatch.images.iter().enumerate() {
            match handle {
                None => written.push(pipelines.empty_images[index].clone()),
                Some(handle) => match images.get(handle) {
                    None => waiting = true,
                    Some(image)
                        if image.texture_descriptor.format == STORAGE_FORMATS[index]
                            && image
                                .texture_descriptor
                                .usage
                                .contains(TextureUsages::STORAGE_BINDING) =>
                    {
                        written.push(image.texture_view.clone());
                    }
                    Some(image) => {
                        bevy::log::warn_once!(
                            "A dispatch was handed a {:?} image to write at binding {}, which \
                             takes a {:?} image made by Shaders.CreateImage, so it does not run.",
                            image.texture_descriptor.format,
                            6 + index,
                            STORAGE_FORMATS[index]
                        );
                        waiting = true;
                    }
                },
            }
        }

        let mut read = Vec::with_capacity(COMPUTE_IMAGE_COUNT);

        for handle in &dispatch.textures {
            match handle {
                None => read.push(fallback.d2.texture_view.clone()),
                Some(handle) => match images.get(handle) {
                    Some(image) => read.push(image.texture_view.clone()),
                    None => waiting = true,
                },
            }
        }

        if waiting {
            continue;
        }

        let parameters = render_device.create_buffer_with_data(&BufferInitDescriptor {
            label: Some("bcs_compute_parameters"),
            contents: bytemuck::cast_slice(&dispatch.parameters),
            usage: BufferUsages::UNIFORM,
        });

        let mut entries = vec![BindGroupEntry {
            binding: 0,
            resource: parameters.as_entire_binding(),
        }];

        for (index, buffer) in bound.iter().enumerate() {
            entries.push(BindGroupEntry {
                binding: 1 + index as u32,
                resource: buffer.as_entire_binding(),
            });
        }

        entries.push(BindGroupEntry {
            binding: 5,
            resource: globals.clone(),
        });

        for (index, view) in written.iter().enumerate() {
            entries.push(BindGroupEntry {
                binding: 6 + index as u32,
                resource: BindingResource::TextureView(view),
            });
        }

        for (index, view) in read.iter().enumerate() {
            entries.push(BindGroupEntry {
                binding: 8 + index as u32,
                resource: BindingResource::TextureView(view),
            });
        }

        entries.push(BindGroupEntry {
            binding: 10,
            resource: BindingResource::Sampler(&pipelines.sampler),
        });

        let bind_group = render_device.create_bind_group("bcs_compute", &layout, &entries);

        prepared.0.push(PreparedDispatch {
            pipeline,
            bind_group,
            workgroups: dispatch.workgroups,
        });
    }
}

/// Queues the compute pipeline of a program that has a compute stage.
fn queue_pipeline(pipelines: &mut ComputePipelines, cache: &PipelineCache, id: u32) {
    let Some(program) = programs::lookup(id) else {
        return;
    };

    let Some(stage) = program.stages[Role::Compute as usize].clone() else {
        return;
    };

    let pipeline = cache.queue_compute_pipeline(ComputePipelineDescriptor {
        label: Some("bcs_compute".into()),
        layout: vec![pipelines.layout.clone()],
        immediate_size: 0,
        shader: stage.shader,
        shader_defs: program.defs.clone(),
        entry_point: Some(stage.entry),
        zero_initialize_workgroup_memory: true,
    });

    pipelines.pipelines.insert(id, pipeline);
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
        pass.set_bind_group(0, &dispatch.bind_group, &[]);
        pass.dispatch_workgroups(x, y, z);
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

/// Resolves a key the way an image key is resolved, where zero or less is no buffer and anything
/// else has to name one.
pub fn optional_buffer(world: &World, key: i32) -> Result<Option<Handle<ShaderBuffer>>, i32> {
    if key <= 0 {
        return Ok(None);
    }

    buffer_handle(world, key)
        .map(Some)
        .ok_or(status::NO_COMPONENT)
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

/// Makes an image a compute shader can write and anything can sample, and answers its asset key.
///
/// `format` is `0` for eight bits a channel, which binding six writes, and `1` for a half float a
/// channel, which binding seven writes and which can hold light brighter than white. It starts
/// transparent black.
pub fn create_image(world: &mut World, width: u32, height: u32, format: i32) -> i32 {
    let (format, texel_bytes) = match format {
        0 => (TextureFormat::Rgba8Unorm, 4),
        1 => (TextureFormat::Rgba16Float, 8),
        _ => return status::NULL_ARG,
    };

    if width == 0 || height == 0 {
        return status::NULL_ARG;
    }

    let texel = vec![0u8; texel_bytes];

    let mut image = Image::new_fill(
        Extent3d {
            width,
            height,
            depth_or_array_layers: 1,
        },
        TextureDimension::D2,
        &texel,
        format,
        RenderAssetUsages::default(),
    );

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
