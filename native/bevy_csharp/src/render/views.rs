//! What a camera owns for the shaders that run on it: images kept from frame to frame, compute run
//! at a point in its frame, and the inputs every pass and every such dispatch reads.
//!
//! Screen-space techniques (ambient occlusion, global illumination, reflections, anything temporal)
//! are a chain of compute and full-screen work over one camera's picture, reading what the camera
//! drew and what the chain itself left behind last frame. This is the camera's side of that chain,
//! so a technique is written as shaders and a list of images rather than as a system that finds
//! cameras, keeps textures per camera and works out when to run.
//!
//! **Images a camera owns.** A camera is given images by name, format and scale of its picture. The
//! engine makes them, makes them again when the picture changes size (which starts them over from
//! zeros), and binds them wherever a shader running on that camera declares the name. One marked
//! as history is two images that trade places every frame, so `name` is this frame's and
//! `name_previous` is what `name` held last frame. One made with more than one mip level is also
//! reachable a level at a time as `name_mip0`, `name_mip1` and so on, which is how a depth pyramid
//! is built one level from the last, reading one level while writing the next.
//!
//! **Compute on a camera.** A dispatch attached to a camera runs every frame at one of four points:
//! after the prepass, after opaque geometry and before transparent, before tonemapping, or after
//! it. Its workgroups are either counted from the picture's size, a fixed number, or read from a
//! buffer another dispatch wrote, which is what lets a shader decide on the GPU how much work
//! follows.
//!
//! **The inputs.** Every pass and every dispatch on a camera reads the same second group, which
//! `bcs_pass` declares:
//!
//! | binding | holds |
//! |---|---|
//! | 0, 1 | the picture so far, and a linear sampler clamped at its edges |
//! | 2 | Bevy's globals, which is where time is |
//! | 3 | the view uniform, with the camera's matrices and viewport |
//! | 4 | the camera's depth, as a depth texture |
//! | 5 | the camera's normals |
//! | 6 | the camera's motion vectors, in UV units per frame |
//! | 7 | the previous frame's view matrices |
//! | 8 | the camera's lights: every directional light with its shadow cascades, and the ambient light |
//! | 9 | every point and spot light |
//! | 10 | the directional lights' shadow maps, a layer per cascade |
//! | 11 | the point lights' shadow maps, a cube per light |
//! | 12 | the comparison sampler shadow maps are read with |
//! | 13 | how many point and spot lights binding nine holds |
//!
//! The lights are Bevy's own, laid out as Bevy lays them out, so a shader running on a camera
//! lights and shadows what it finds the way Bevy's materials do, which is what a global
//! illumination shading a ray's hit needs, and what it could otherwise only get by drawing the
//! scene's lights again itself.
//!
//! Depth, normals and motion come from the prepass, which a camera draws only when asked. What a
//! camera does not draw, or draws multisampled (which a plain texture binding cannot take), is a
//! stand-in: depth zero, which is the far plane in Bevy's reversed depth, white normals and zero
//! motion. A camera with no previous view data, which is every 2D camera, reads this frame's.

#![cfg(feature = "render")]

use std::collections::HashMap;

use bevy::app::App;
use bevy::asset::Handle;
use bevy::camera::Camera;
use bevy::core_pipeline::core_3d::{main_opaque_pass_3d, main_transparent_pass_3d};
use bevy::core_pipeline::prepass::{
    PreviousViewData, PreviousViewUniformOffset, PreviousViewUniforms, ViewPrepassTextures,
};
use bevy::core_pipeline::tonemapping::tonemapping;
use bevy::core_pipeline::{Core2d, Core2dSystems, Core3d, Core3dSystems};
use bevy::ecs::system::SystemParam;
use bevy::pbr::{
    GlobalClusterableObjectMeta, GpuClusteredLight, GpuLights, LightMeta,
    ScreenSpaceAmbientOcclusionResources, ShadowSamplers, ViewLightsUniformOffset,
    ViewShadowBindings,
};
use bevy::pbr::deferred::deferred_lighting;
use bevy::ecs::component::Component;
use bevy::ecs::entity::Entity;
use bevy::ecs::query::{With, Without};
use bevy::ecs::resource::Resource;
use bevy::ecs::schedule::IntoScheduleConfigs;
use bevy::ecs::system::{Commands, Query, Res, ResMut};
use bevy::render::extract_component::{ExtractComponent, ExtractComponentPlugin};
use bevy::render::globals::{GlobalsBuffer, GlobalsUniform};
use bevy::render::render_asset::RenderAssets;
use bevy::render::render_resource::{
    BindGroup, BindGroupEntry, BindGroupLayoutDescriptor, BindGroupLayoutEntry, BindingResource,
    BindingType, Buffer, BufferBinding, BufferBindingType, BufferDescriptor, BufferUsages,
    CachedComputePipelineId, ComputePassDescriptor, ComputePipelineDescriptor, Extent3d,
    FilterMode, PipelineCache, Sampler, SamplerBindingType, SamplerDescriptor, ShaderStages,
    ShaderType, Texture, TextureDescriptor, TextureDimension, TextureFormat, TextureSampleType,
    TextureUsages, TextureView, TextureViewDescriptor, TextureViewDimension,
};
use bevy::render::renderer::{RenderContext, RenderDevice, ViewQuery};
use bevy::render::storage::{GpuShaderBuffer, ShaderBuffer};
use bevy::render::texture::{FallbackImage, GpuImage};
use bevy::render::view::{ViewTarget, ViewUniform, ViewUniformOffset, ViewUniforms};
use bevy::render::{Render, RenderApp, RenderStartup, RenderSystems};

use super::material::say_once;
use super::programs::{self, Role};
use super::values::{PackContext, PackError, Stand, Values, ViewTexture, pack};

// -- The inputs every pass and dispatch on a camera reads

/// The group every shader running on a camera reads as its second, and what stands in for what a
/// camera does not draw.
#[derive(Resource)]
pub struct ViewInputs {
    pub layout: BindGroupLayoutDescriptor,
    sampler: Sampler,
    empty_depth: TextureView,
    empty_motion: TextureView,
    /// Bound where a camera has no previous view data, with this frame's view in it would be
    /// better, but a 2D camera has neither, so it is zeros, which a 2D shader has no reason to
    /// read.
    empty_previous: Buffer,
    /// What stands in for the lights of a camera that has none, which is every 2D camera: no
    /// directional lights, no point lights, and shadow maps that shadow nothing.
    empty_lights: Buffer,
    empty_clustered: Buffer,
    empty_directional_shadows: TextureView,
    empty_point_shadows: TextureView,
    comparison: Sampler,
}

/// The scene's lights, as the render world holds them for Bevy's own materials.
#[derive(SystemParam)]
pub struct SceneLights<'w> {
    meta: Option<Res<'w, LightMeta>>,
    clustered: Option<Res<'w, GlobalClusterableObjectMeta>>,
    samplers: Option<Res<'w, ShadowSamplers>>,
}

/// One view's part of the scene's lights.
pub struct ViewLights<'a> {
    pub offset: Option<&'a ViewLightsUniformOffset>,
    pub shadows: Option<&'a ViewShadowBindings>,
}

fn init_inputs(mut commands: Commands, render_device: Res<RenderDevice>) {
    let entry = |binding: u32, ty: BindingType| BindGroupLayoutEntry {
        binding,
        visibility: ShaderStages::FRAGMENT | ShaderStages::COMPUTE,
        ty,
        count: None,
    };

    let float = |filterable: bool| BindingType::Texture {
        sample_type: TextureSampleType::Float { filterable },
        view_dimension: TextureViewDimension::D2,
        multisampled: false,
    };

    let entries = [
        entry(0, float(true)),
        entry(1, BindingType::Sampler(SamplerBindingType::Filtering)),
        entry(
            2,
            BindingType::Buffer {
                ty: BufferBindingType::Uniform,
                has_dynamic_offset: false,
                min_binding_size: Some(GlobalsUniform::min_size()),
            },
        ),
        entry(
            3,
            BindingType::Buffer {
                ty: BufferBindingType::Uniform,
                has_dynamic_offset: true,
                min_binding_size: Some(ViewUniform::min_size()),
            },
        ),
        entry(
            4,
            BindingType::Texture {
                sample_type: TextureSampleType::Depth,
                view_dimension: TextureViewDimension::D2,
                multisampled: false,
            },
        ),
        entry(5, float(true)),
        entry(6, float(true)),
        entry(
            7,
            BindingType::Buffer {
                ty: BufferBindingType::Uniform,
                has_dynamic_offset: true,
                min_binding_size: Some(PreviousViewData::min_size()),
            },
        ),
        entry(
            8,
            BindingType::Buffer {
                ty: BufferBindingType::Uniform,
                has_dynamic_offset: true,
                min_binding_size: Some(GpuLights::min_size()),
            },
        ),
        entry(
            9,
            BindingType::Buffer {
                ty: BufferBindingType::Storage { read_only: true },
                has_dynamic_offset: false,
                min_binding_size: Some(GpuClusteredLight::min_size()),
            },
        ),
        entry(
            10,
            BindingType::Texture {
                sample_type: TextureSampleType::Depth,
                view_dimension: TextureViewDimension::D2Array,
                multisampled: false,
            },
        ),
        entry(
            11,
            BindingType::Texture {
                sample_type: TextureSampleType::Depth,
                view_dimension: TextureViewDimension::CubeArray,
                multisampled: false,
            },
        ),
        entry(12, BindingType::Sampler(SamplerBindingType::Comparison)),
        entry(
            13,
            BindingType::Buffer {
                ty: BufferBindingType::Uniform,
                has_dynamic_offset: false,
                min_binding_size: std::num::NonZeroU64::new(16),
            },
        ),
    ];

    let texture = |label: &'static str, format: TextureFormat| {
        render_device
            .create_texture(&TextureDescriptor {
                label: Some(label),
                size: Extent3d {
                    width: 1,
                    height: 1,
                    depth_or_array_layers: 1,
                },
                mip_level_count: 1,
                sample_count: 1,
                dimension: TextureDimension::D2,
                format,
                usage: TextureUsages::TEXTURE_BINDING,
                view_formats: &[],
            })
            .create_view(&TextureViewDescriptor::default())
    };

    let depth_layers = |label: &'static str, layers: u32, dimension: TextureViewDimension| {
        render_device
            .create_texture(&TextureDescriptor {
                label: Some(label),
                size: Extent3d {
                    width: 1,
                    height: 1,
                    depth_or_array_layers: layers,
                },
                mip_level_count: 1,
                sample_count: 1,
                dimension: TextureDimension::D2,
                format: TextureFormat::Depth32Float,
                usage: TextureUsages::TEXTURE_BINDING,
                view_formats: &[],
            })
            .create_view(&TextureViewDescriptor {
                dimension: Some(dimension),
                ..Default::default()
            })
    };

    let zeros = |label: &'static str, size: u64, usage: BufferUsages| {
        render_device.create_buffer(&BufferDescriptor {
            label: Some(label),
            size,
            usage,
            mapped_at_creation: false,
        })
    };

    commands.insert_resource(ViewInputs {
        layout: BindGroupLayoutDescriptor::new("bcs_view_inputs", &entries),
        sampler: render_device.create_sampler(&SamplerDescriptor {
            label: Some("bcs_view_sampler"),
            mag_filter: FilterMode::Linear,
            min_filter: FilterMode::Linear,
            ..Default::default()
        }),
        empty_depth: texture("bcs_view_empty_depth", TextureFormat::Depth32Float),
        // Zeros, which wgpu guarantees a texture starts as, is no motion at all.
        empty_motion: texture("bcs_view_empty_motion", TextureFormat::Rg16Float),
        empty_previous: zeros(
            "bcs_view_empty_previous",
            PreviousViewData::min_size().get(),
            BufferUsages::UNIFORM,
        ),
        empty_lights: zeros("bcs_view_empty_lights", GpuLights::min_size().get(), BufferUsages::UNIFORM),
        empty_clustered: zeros(
            "bcs_view_empty_clustered",
            GpuClusteredLight::min_size().get(),
            BufferUsages::STORAGE,
        ),
        // Depth zero, which is the far plane in Bevy's reversed depth, so everything is in front of
        // it and nothing is in shadow.
        empty_directional_shadows: depth_layers(
            "bcs_view_empty_directional_shadows",
            1,
            TextureViewDimension::D2Array,
        ),
        empty_point_shadows: depth_layers(
            "bcs_view_empty_point_shadows",
            6,
            TextureViewDimension::CubeArray,
        ),
        comparison: render_device.create_sampler(&SamplerDescriptor {
            label: Some("bcs_view_comparison"),
            mag_filter: FilterMode::Linear,
            min_filter: FilterMode::Linear,
            compare: Some(bevy::render::render_resource::CompareFunction::GreaterEqual),
            ..Default::default()
        }),
    });
}

/// What one view hands its shaders, gathered once and bound as many times as it has passes and
/// dispatches.
pub struct ViewInputSources<'a> {
    pub depth: TextureView,
    pub normals: TextureView,
    pub motion: TextureView,
    pub previous: Option<(&'a PreviousViewUniforms, u32)>,
}

impl<'a> ViewInputSources<'a> {
    /// Collects a view's prepass textures, with a stand-in for each it does not have.
    pub fn gather(
        inputs: &ViewInputs,
        fallback: &FallbackImage,
        prepass: Option<&ViewPrepassTextures>,
        previous: Option<(&'a PreviousViewUniforms, &PreviousViewUniformOffset)>,
    ) -> Self {
        // Only a texture drawn once a pixel can be bound as a plain texture. A multisampled
        // camera's prepass draws several samples a pixel, and it is left out rather than failing
        // the pipeline.
        let single = |texture: &Texture| texture.sample_count() == 1;

        let depth = prepass
            .and_then(|prepass| prepass.depth.as_ref())
            .filter(|depth| single(&depth.texture.texture))
            .map(|depth| depth.texture.default_view.clone())
            .unwrap_or_else(|| inputs.empty_depth.clone());

        let normals = prepass
            .and_then(|prepass| prepass.normal.as_ref())
            .filter(|normal| single(&normal.texture.texture))
            .map(|normal| normal.texture.default_view.clone())
            .unwrap_or_else(|| fallback.d2.texture_view.clone());

        let motion = prepass
            .and_then(|prepass| prepass.motion_vectors.as_ref())
            .filter(|motion| single(&motion.texture.texture))
            .map(|motion| motion.texture.default_view.clone())
            .unwrap_or_else(|| inputs.empty_motion.clone());

        Self {
            depth,
            normals,
            motion,
            previous: previous.map(|(uniforms, offset)| (uniforms, offset.offset)),
        }
    }

    /// The group, and the dynamic offsets it is bound with, for a shader reading `picture` as the
    /// picture so far.
    #[allow(clippy::too_many_arguments)]
    pub fn bind(
        &self,
        device: &RenderDevice,
        cache: &PipelineCache,
        inputs: &ViewInputs,
        picture: &TextureView,
        globals: BindingResource,
        view: BindingResource,
        view_offset: u32,
        scene: &SceneLights,
        lights: &ViewLights,
    ) -> Option<(BindGroup, [u32; 3])> {
        let (previous, previous_offset) = match self.previous {
            Some((uniforms, offset)) => (uniforms.uniforms.binding()?, offset),
            None => (
                BindingResource::Buffer(BufferBinding {
                    buffer: &inputs.empty_previous,
                    offset: 0,
                    size: Some(PreviousViewData::min_size()),
                }),
                0,
            ),
        };

        let (light_binding, light_offset) = match (scene.meta.as_deref(), lights.offset) {
            (Some(meta), Some(offset)) => match meta.view_gpu_lights.binding() {
                Some(binding) => (binding, offset.offset),
                None => (Self::whole(&inputs.empty_lights, GpuLights::min_size()), 0),
            },
            _ => (Self::whole(&inputs.empty_lights, GpuLights::min_size()), 0),
        };

        let clustered = scene
            .clustered
            .as_deref()
            .and_then(|clustered| clustered.gpu_clustered_lights.binding())
            .unwrap_or_else(|| inputs.empty_clustered.as_entire_binding());

        let point_lights = scene
            .clustered
            .as_deref()
            .map_or(0, |clustered| clustered.entity_to_index.len() as u32);

        let counts = device.create_buffer_with_data(&bevy::render::render_resource::BufferInitDescriptor {
            label: Some("bcs_view_light_counts"),
            contents: bytemuck::cast_slice(&[point_lights, 0u32, 0u32, 0u32]),
            usage: BufferUsages::UNIFORM,
        });

        let (directional_shadows, point_shadows) = match lights.shadows {
            Some(shadows) => (
                &shadows.directional_light_depth_texture_view,
                &shadows.point_light_depth_texture_view,
            ),
            None => (&inputs.empty_directional_shadows, &inputs.empty_point_shadows),
        };

        let comparison = scene
            .samplers
            .as_deref()
            .map_or(&inputs.comparison, |samplers| &samplers.directional_light_comparison_sampler);

        let group = device.create_bind_group(
            "bcs_view_inputs",
            &cache.get_bind_group_layout(&inputs.layout),
            &[
                BindGroupEntry {
                    binding: 0,
                    resource: BindingResource::TextureView(picture),
                },
                BindGroupEntry {
                    binding: 1,
                    resource: BindingResource::Sampler(&inputs.sampler),
                },
                BindGroupEntry {
                    binding: 2,
                    resource: globals,
                },
                BindGroupEntry {
                    binding: 3,
                    resource: view,
                },
                BindGroupEntry {
                    binding: 4,
                    resource: BindingResource::TextureView(&self.depth),
                },
                BindGroupEntry {
                    binding: 5,
                    resource: BindingResource::TextureView(&self.normals),
                },
                BindGroupEntry {
                    binding: 6,
                    resource: BindingResource::TextureView(&self.motion),
                },
                BindGroupEntry {
                    binding: 7,
                    resource: previous,
                },
                BindGroupEntry {
                    binding: 8,
                    resource: light_binding,
                },
                BindGroupEntry {
                    binding: 9,
                    resource: clustered,
                },
                BindGroupEntry {
                    binding: 10,
                    resource: BindingResource::TextureView(directional_shadows),
                },
                BindGroupEntry {
                    binding: 11,
                    resource: BindingResource::TextureView(point_shadows),
                },
                BindGroupEntry {
                    binding: 12,
                    resource: BindingResource::Sampler(comparison),
                },
                BindGroupEntry {
                    binding: 13,
                    resource: counts.as_entire_binding(),
                },
            ],
        );

        Some((group, [view_offset, previous_offset, light_offset]))
    }

    /// The start of a stand-in buffer, as long as one element of what it stands in for, which is
    /// what a binding with a dynamic offset takes.
    fn whole(buffer: &Buffer, size: std::num::NonZeroU64) -> BindingResource<'_> {
        BindingResource::Buffer(BufferBinding {
            buffer,
            offset: 0,
            size: Some(size),
        })
    }
}

// -- Images a camera owns

/// One image a camera was asked to own.
#[derive(Clone, PartialEq, Debug)]
pub struct ViewImageSpec {
    pub name: String,
    pub format: TextureFormat,
    /// A fraction of the picture's size, one for the same size and a half for half of it.
    pub scale: f32,
    /// Two images that trade places every frame, so last frame's is readable as `name_previous`.
    pub history: bool,
    pub mips: u32,
}

/// The images a camera owns.
#[derive(Component, Clone, ExtractComponent)]
#[extract_component_filter(With<Camera>)]
pub struct BcsViewImages(pub Vec<ViewImageSpec>);

/// One of a view's images, as the render world keeps it.
struct ViewImageSlot {
    spec: ViewImageSpec,
    size: (u32, u32),
    /// One texture, or two for history.
    textures: Vec<Texture>,
    /// Which of the two is this frame's.
    current: usize,
}

/// A view's images, kept from frame to frame.
#[derive(Component, Default)]
pub struct ViewImageTextures {
    slots: Vec<ViewImageSlot>,
    /// Every name a shader on this view can read an image by, this frame.
    pub names: HashMap<String, ViewTexture>,
}

/// The size of an image `scale` of a picture `width` by `height`, which is never nothing.
pub fn scaled(width: u32, height: u32, scale: f32) -> (u32, u32) {
    let at = |length: u32| ((length as f32 * scale).ceil() as u32).max(1);
    (at(width), at(height))
}

/// How many mip levels an image of `size` can have, at most `asked`.
fn mip_count(size: (u32, u32), asked: u32) -> u32 {
    let full = 32 - size.0.max(size.1).leading_zeros();
    asked.clamp(1, full)
}

/// Makes each view's images match what its camera asked for, and trades history images round.
fn prepare_view_images(
    mut commands: Commands,
    render_device: Res<RenderDevice>,
    mut views: Query<(
        Entity,
        &ViewTarget,
        &BcsViewImages,
        Option<&mut ViewImageTextures>,
    )>,
) {
    for (entity, target, asked, kept) in &mut views {
        let picture = target.main_texture();
        let mut slots: Vec<ViewImageSlot> = kept
            .map(|mut kept| std::mem::take(&mut kept.slots))
            .unwrap_or_default();

        let mut next = Vec::with_capacity(asked.0.len());

        for spec in &asked.0 {
            let size = scaled(picture.width(), picture.height(), spec.scale);

            let reusable = slots
                .iter()
                .position(|slot| slot.spec == *spec && slot.size == size);

            let slot = match reusable {
                Some(index) => {
                    let mut slot = slots.swap_remove(index);
                    if slot.spec.history {
                        slot.current = 1 - slot.current;
                    }
                    slot
                }
                None => ViewImageSlot {
                    spec: spec.clone(),
                    size,
                    textures: (0..if spec.history { 2 } else { 1 })
                        .map(|_| {
                            render_device.create_texture(&TextureDescriptor {
                                label: Some("bcs_view_image"),
                                size: Extent3d {
                                    width: size.0,
                                    height: size.1,
                                    depth_or_array_layers: 1,
                                },
                                mip_level_count: mip_count(size, spec.mips),
                                sample_count: 1,
                                dimension: TextureDimension::D2,
                                format: spec.format,
                                usage: TextureUsages::TEXTURE_BINDING
                                    | TextureUsages::STORAGE_BINDING
                                    | TextureUsages::COPY_SRC
                                    | TextureUsages::COPY_DST,
                                view_formats: &[],
                            })
                        })
                        .collect(),
                    current: 0,
                },
            };

            next.push(slot);
        }

        let mut names = HashMap::new();

        for slot in &next {
            let format = slot.spec.format;
            let levels = slot.textures[0].mip_level_count();
            let current = &slot.textures[slot.current];

            names.insert(
                slot.spec.name.clone(),
                ViewTexture {
                    view: current.create_view(&TextureViewDescriptor::default()),
                    level: level_view(current, 0),
                    format,
                },
            );

            for mip in (0..levels).filter(|_| levels > 1) {
                let single = level_view(current, mip);
                names.insert(
                    format!("{}_mip{mip}", slot.spec.name),
                    ViewTexture {
                        view: single.clone(),
                        level: single,
                        format,
                    },
                );
            }

            if slot.spec.history {
                let previous = &slot.textures[1 - slot.current];
                names.insert(
                    format!("{}_previous", slot.spec.name),
                    ViewTexture {
                        view: previous.create_view(&TextureViewDescriptor::default()),
                        level: level_view(previous, 0),
                        format,
                    },
                );
            }
        }

        commands.entity(entity).insert(ViewImageTextures { slots: next, names });
    }
}

/// A view of one mip level of `texture`, which is what a storage binding takes.
fn level_view(texture: &Texture, mip: u32) -> TextureView {
    texture.create_view(&TextureViewDescriptor {
        base_mip_level: mip,
        mip_level_count: Some(1),
        ..Default::default()
    })
}

/// Every name a shader on a view can read an image by: the camera's own images, and the engine's
/// images a shader may replace, which is Bevy's ambient occlusion where the camera has it on.
///
/// The engine's go under names of their own, so a camera's image can never shadow one.
pub fn view_names<'a>(
    owned: Option<&'a ViewImageTextures>,
    occlusion: Option<&ScreenSpaceAmbientOcclusionResources>,
) -> Option<std::borrow::Cow<'a, HashMap<String, ViewTexture>>> {
    match (owned, occlusion) {
        (owned, Some(occlusion)) => {
            let mut names = owned.map(|owned| owned.names.clone()).unwrap_or_default();
            let texture = &occlusion.screen_space_ambient_occlusion_texture;

            names.insert(
                "ambient_occlusion".into(),
                ViewTexture {
                    view: texture.default_view.clone(),
                    level: texture.default_view.clone(),
                    format: texture.texture.format(),
                },
            );

            Some(std::borrow::Cow::Owned(names))
        }
        (Some(owned), None) => Some(std::borrow::Cow::Borrowed(&owned.names)),
        (None, None) => None,
    }
}

/// Drops the images of a view whose camera no longer asks for any.
fn forget_view_images(
    mut commands: Commands,
    views: Query<Entity, (With<ViewImageTextures>, Without<BcsViewImages>)>,
) {
    for entity in &views {
        commands.entity(entity).remove::<ViewImageTextures>();
    }
}

// -- Compute on a camera

/// Where in a camera's frame a dispatch runs.
#[derive(Clone, Copy, PartialEq, Eq, Debug)]
pub enum FramePoint {
    /// Once depth, normals, motion, shadows and Bevy's own ambient occlusion are drawn, before
    /// anything is lit.
    AfterPrepass = 0,
    /// Once opaque geometry is drawn, before transparent geometry.
    AfterOpaque = 1,
    /// On the linear picture, before the passes that run before tonemapping.
    BeforeTonemapping = 2,
    /// On the picture as the screen will show it, before the passes that run after tonemapping.
    AfterTonemapping = 3,
}

impl FramePoint {
    pub fn from_number(number: i32) -> Option<Self> {
        Some(match number {
            0 => Self::AfterPrepass,
            1 => Self::AfterOpaque,
            2 => Self::BeforeTonemapping,
            3 => Self::AfterTonemapping,
            _ => return None,
        })
    }
}

/// How many workgroups a dispatch on a camera runs.
#[derive(Clone, Debug)]
pub enum Workgroups {
    /// Enough of `size` to cover `scale` of the picture, which is what a shader working a pixel at
    /// a time wants.
    PerPixel { size: [u32; 2], scale: f32 },
    /// Exactly these.
    Fixed([u32; 3]),
    /// Three numbers at `offset` in a buffer, written on the GPU.
    Indirect {
        buffer: Handle<ShaderBuffer>,
        offset: u64,
    },
}

/// One dispatch a camera runs every frame.
#[derive(Clone, Debug)]
pub struct ViewDispatch {
    pub program: u32,
    pub values: Values,
    pub point: FramePoint,
    pub workgroups: Workgroups,
}

/// The dispatches a camera runs every frame, in order.
#[derive(Component, Clone, ExtractComponent)]
#[extract_component_filter(With<Camera>)]
pub struct BcsViewDispatches(pub Vec<ViewDispatch>);

/// A dispatch ready to run on a view.
struct PreparedViewDispatch {
    point: FramePoint,
    pipeline: CachedComputePipelineId,
    own: BindGroup,
    workgroups: PreparedWorkgroups,
}

enum PreparedWorkgroups {
    Direct([u32; 3]),
    Indirect(Buffer, u64),
}

/// A view's dispatches, ready to run.
#[derive(Component)]
pub struct PreparedViewDispatches(Vec<PreparedViewDispatch>);

/// One compute pipeline per program and version of it, for dispatches on a camera.
#[derive(Resource, Default)]
struct ViewComputePipelines(HashMap<(u32, u32), CachedComputePipelineId>);

/// The layout of a program's own group for a dispatch on a camera.
fn own_layout(layout: &super::reflect::Layout) -> BindGroupLayoutDescriptor {
    BindGroupLayoutDescriptor::new("bcs_view_compute_own", &layout.entries(ShaderStages::COMPUTE))
}

/// The pipeline for a program's current version dispatched on a camera, queued the first time it
/// is asked for.
fn view_pipeline_for(
    pipelines: &mut ViewComputePipelines,
    inputs: &ViewInputs,
    cache: &PipelineCache,
    id: u32,
) -> Option<(CachedComputePipelineId, programs::PipelineProgram)> {
    let program = programs::lookup(id)?;
    let layout = program.compute.clone()?;
    let stage = program.stages[Role::Compute as usize].clone()?;

    let pipeline = *pipelines
        .0
        .entry((id, program.generation))
        .or_insert_with(|| {
            cache.queue_compute_pipeline(ComputePipelineDescriptor {
                label: Some("bcs_view_compute".into()),
                layout: vec![own_layout(&layout), inputs.layout.clone()],
                immediate_size: 0,
                shader: stage.shader,
                shader_defs: Vec::new(),
                entry_point: None,
                zero_initialize_workgroup_memory: true,
            })
        });

    Some((pipeline, program))
}

#[allow(clippy::too_many_arguments)]
fn prepare_view_dispatches(
    mut commands: Commands,
    mut pipelines: ResMut<ViewComputePipelines>,
    inputs: Res<ViewInputs>,
    cache: Res<PipelineCache>,
    render_device: Res<RenderDevice>,
    images: Res<RenderAssets<GpuImage>>,
    buffers: Res<RenderAssets<GpuShaderBuffer>>,
    fallback: Res<FallbackImage>,
    stand: Option<Res<Stand>>,
    views: Query<(
        Entity,
        &ViewTarget,
        &BcsViewDispatches,
        Option<&ViewImageTextures>,
        Option<&ScreenSpaceAmbientOcclusionResources>,
    )>,
) {
    let Some(stand) = stand else {
        return;
    };

    // A program that reads a camera's inputs runs only on a camera, so its pipeline is built here
    // as soon as it exists, which is also what says it is ready. One that does not is built by
    // `super::compute` against time alone, and built here too only once a camera runs it.
    for id in 0..programs::table_len() {
        let reads_view = programs::lookup(id)
            .and_then(|program| program.compute)
            .is_some_and(|layout| layout.reads_view);

        if reads_view
            && let Some((pipeline, program)) = view_pipeline_for(&mut pipelines, &inputs, &cache, id)
            && cache.get_compute_pipeline(pipeline).is_some()
        {
            programs::mark_compute_ready(id, program.generation);
        }
    }

    for (entity, target, asked, owned, occlusion) in &views {
        let names = view_names(owned, occlusion);
        let mut prepared = Vec::with_capacity(asked.0.len());

        for dispatch in &asked.0 {
            let Some((pipeline, program)) =
                view_pipeline_for(&mut pipelines, &inputs, &cache, dispatch.program)
            else {
                if programs::lookup(dispatch.program).is_some_and(|program| {
                    program.generation > 0 && program.stages[Role::Compute as usize].is_none()
                }) {
                    say_once(format!(
                        "A camera runs shader program {}, which has no compute stage, so it does \
                         nothing.",
                        dispatch.program
                    ));
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
                view: names.as_deref(),
            };

            let packed = match pack(&layout, &dispatch.values, &context) {
                Ok(packed) => packed,
                Err(PackError::NotReady) => continue,
                Err(PackError::Missing(message)) => {
                    say_once(format!(
                        "A dispatch on a camera of shader program {}: {message}",
                        dispatch.program
                    ));
                    continue;
                }
            };

            for problem in &packed.problems {
                say_once(format!(
                    "A dispatch on a camera of shader program {}: {problem}",
                    dispatch.program
                ));
            }

            let workgroups = match &dispatch.workgroups {
                Workgroups::PerPixel { size, scale } => {
                    let picture = target.main_texture();
                    let (width, height) = scaled(picture.width(), picture.height(), *scale);
                    PreparedWorkgroups::Direct([
                        width.div_ceil(size[0].max(1)),
                        height.div_ceil(size[1].max(1)),
                        1,
                    ])
                }
                Workgroups::Fixed(groups) => PreparedWorkgroups::Direct(*groups),
                Workgroups::Indirect { buffer, offset } => match buffers.get(buffer) {
                    Some(gpu) => PreparedWorkgroups::Indirect(gpu.buffer.clone(), *offset),
                    None => continue,
                },
            };

            prepared.push(PreparedViewDispatch {
                point: dispatch.point,
                pipeline,
                own: packed.bind_group(
                    &render_device,
                    "bcs_view_compute_own",
                    &cache.get_bind_group_layout(&own_layout(&layout)),
                ),
                workgroups,
            });
        }

        commands.entity(entity).insert(PreparedViewDispatches(prepared));
    }
}

/// Drops what a view kept for dispatches its camera no longer has.
fn forget_view_dispatches(
    mut commands: Commands,
    views: Query<Entity, (With<PreparedViewDispatches>, Without<BcsViewDispatches>)>,
) {
    for entity in &views {
        commands.entity(entity).remove::<PreparedViewDispatches>();
    }
}

/// Runs a view's dispatches for one point of its frame.
#[allow(clippy::too_many_arguments)]
fn run_view_dispatches<const POINT: u8>(
    view: ViewQuery<(
        &ViewTarget,
        &ViewUniformOffset,
        &PreparedViewDispatches,
        Option<&ViewPrepassTextures>,
        Option<&PreviousViewUniformOffset>,
        Option<&ViewLightsUniformOffset>,
        Option<&ViewShadowBindings>,
    )>,
    inputs: Res<ViewInputs>,
    fallback: Res<FallbackImage>,
    cache: Res<PipelineCache>,
    globals: Res<GlobalsBuffer>,
    view_uniforms: Res<ViewUniforms>,
    previous_uniforms: Option<Res<PreviousViewUniforms>>,
    scene: SceneLights,
    mut ctx: RenderContext,
) {
    let (target, offset, prepared, prepass, previous, light_offset, shadows) = view.into_inner();

    if !prepared.0.iter().any(|dispatch| dispatch.point as u8 == POINT) {
        return;
    }

    let (Some(globals), Some(view_binding)) =
        (globals.buffer.binding(), view_uniforms.uniforms.binding())
    else {
        return;
    };

    let sources = ViewInputSources::gather(
        &inputs,
        &fallback,
        prepass,
        previous_uniforms.as_deref().zip(previous),
    );

    let Some((group, offsets)) = sources.bind(
        ctx.render_device(),
        &cache,
        &inputs,
        target.main_texture_view(),
        globals,
        view_binding,
        offset.offset,
        &scene,
        &ViewLights {
            offset: light_offset,
            shadows,
        },
    ) else {
        return;
    };

    for dispatch in &prepared.0 {
        if dispatch.point as u8 != POINT {
            continue;
        }

        // Still compiling. Left out of this frame, as a pass still compiling is.
        let Some(pipeline) = cache.get_compute_pipeline(dispatch.pipeline) else {
            continue;
        };

        let mut pass = ctx
            .command_encoder()
            .begin_compute_pass(&ComputePassDescriptor {
                label: Some("bcs_view_compute"),
                timestamp_writes: None,
            });

        pass.set_pipeline(pipeline);
        pass.set_bind_group(0, &dispatch.own, &[]);
        pass.set_bind_group(1, &group, &offsets);

        match &dispatch.workgroups {
            PreparedWorkgroups::Direct([x, y, z]) => pass.dispatch_workgroups(*x, *y, *z),
            PreparedWorkgroups::Indirect(buffer, offset) => {
                pass.dispatch_workgroups_indirect(buffer, *offset)
            }
        }
    }
}

/// Adds what gives cameras their images, their dispatches and the inputs their shaders read.
pub fn install(app: &mut App) {
    app.add_plugins((
        ExtractComponentPlugin::<BcsViewImages>::default(),
        ExtractComponentPlugin::<BcsViewDispatches>::default(),
    ));

    let Some(render_app) = app.get_sub_app_mut(RenderApp) else {
        return;
    };

    render_app
        .init_resource::<ViewComputePipelines>()
        .add_systems(RenderStartup, init_inputs)
        .add_systems(
            Render,
            (
                (prepare_view_images, forget_view_images).in_set(RenderSystems::PrepareResources),
                (prepare_view_dispatches, forget_view_dispatches)
                    .in_set(RenderSystems::PrepareBindGroups),
            ),
        )
        .add_systems(
            Core3d,
            (
                // At the start of the main pass rather than between it and the prepass, which is
                // where Bevy's own ambient occlusion and shadows run, so a shader here sees them
                // done and can replace what Bevy's lighting is about to read.
                run_view_dispatches::<0>
                    .in_set(Core3dSystems::MainPass)
                    .before(deferred_lighting)
                    .before(main_opaque_pass_3d),
                run_view_dispatches::<1>
                    .after(main_opaque_pass_3d)
                    .before(main_transparent_pass_3d)
                    .in_set(Core3dSystems::MainPass),
                run_view_dispatches::<2>
                    .before(tonemapping)
                    .before(super::passes::BeforeTonemappingPasses)
                    .in_set(Core3dSystems::PostProcess),
                run_view_dispatches::<3>
                    .after(tonemapping)
                    .before(super::passes::AfterTonemappingPasses)
                    .in_set(Core3dSystems::PostProcess),
            ),
        )
        .add_systems(
            Core2d,
            (
                run_view_dispatches::<0>
                    .after(Core2dSystems::Prepass)
                    .before(Core2dSystems::MainPass),
                run_view_dispatches::<1>
                    .after(Core2dSystems::MainPass)
                    .before(Core2dSystems::EarlyPostProcess),
                run_view_dispatches::<2>
                    .before(tonemapping)
                    .before(super::passes::BeforeTonemappingPasses)
                    .in_set(Core2dSystems::PostProcess),
                run_view_dispatches::<3>
                    .after(tonemapping)
                    .before(super::passes::AfterTonemappingPasses)
                    .in_set(Core2dSystems::PostProcess),
            ),
        );
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn a_scaled_image_is_rounded_up_and_never_empty() {
        assert_eq!(scaled(1280, 720, 0.5), (640, 360));
        assert_eq!(scaled(1281, 721, 0.5), (641, 361));
        assert_eq!(scaled(3, 3, 0.01), (1, 1));
    }

    #[test]
    fn mips_stop_at_one_pixel() {
        assert_eq!(mip_count((1024, 512), 64), 11);
        assert_eq!(mip_count((1024, 512), 4), 4);
        assert_eq!(mip_count((1, 1), 8), 1);
        assert_eq!(mip_count((16, 16), 0), 1);
    }
}
