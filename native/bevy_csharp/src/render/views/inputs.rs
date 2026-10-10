//! The inputs every pass and every dispatch on a camera reads, and what stands in for what a
//! camera does not draw.

use bevy::camera::Camera;
use bevy::core_pipeline::prepass::{
    PreviousViewData, PreviousViewUniformOffset, PreviousViewUniforms, ViewPrepassTextures,
};
use bevy::ecs::component::Component;
use bevy::ecs::entity::Entity;
use bevy::ecs::query::With;
use bevy::ecs::resource::Resource;
use bevy::ecs::system::{Commands, Query, Res, ResMut, SystemParam};
use bevy::pbr::{
    GlobalClusterableObjectMeta, GpuClusteredLight, GpuLights, LightMeta, ShadowSamplers,
    ViewLightsUniformOffset, ViewShadowBindings,
};
use bevy::render::globals::GlobalsUniform;
use bevy::render::render_asset::RenderAssets;
use bevy::render::render_resource::{
    BindGroup, BindGroupEntry, BindGroupLayoutDescriptor, BindGroupLayoutEntry, BindingResource,
    BindingType, Buffer, BufferBinding, BufferBindingType, BufferDescriptor, BufferUsages, Extent3d,
    FilterMode, PipelineCache, Sampler, SamplerBindingType, SamplerDescriptor, ShaderStages,
    ShaderType, Texture, TextureDescriptor, TextureDimension, TextureFormat, TextureSampleType,
    TextureUsages, TextureView, TextureViewDescriptor, TextureViewDimension,
};
use bevy::render::renderer::RenderDevice;
use bevy::render::texture::{FallbackImage, GpuImage};
use bevy::render::view::ViewUniform;

/// The group every shader running on a camera reads as its second, and what stands in for what a
/// camera does not draw.
#[derive(Resource)]
pub struct ViewInputs {
    pub layout: BindGroupLayoutDescriptor,
    sampler: Sampler,
    empty_depth: TextureView,
    pub(super) empty_motion: TextureView,
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
    /// Black in every direction, for a camera lit by no environment map.
    empty_environment: TextureView,
    /// Bevy's blue noise as layers of one array, once it is on the GPU, and a single gray layer
    /// until then.
    blue_noise: Option<TextureView>,
    empty_blue_noise: TextureView,
}

/// Finds Bevy's blue noise on the GPU once, for every shader on a camera to read.
pub(super) fn prepare_blue_noise(
    mut inputs: ResMut<ViewInputs>,
    noise: Option<Res<bevy::pbr::Bluenoise>>,
    images: Res<RenderAssets<GpuImage>>,
) {
    if inputs.blue_noise.is_some() {
        return;
    }

    let Some(image) = noise.and_then(|noise| images.get(&noise.texture)) else {
        return;
    };

    inputs.blue_noise = Some(image.texture.create_view(&TextureViewDescriptor {
        dimension: Some(TextureViewDimension::D2Array),
        ..Default::default()
    }));
}

/// A camera's environment map as the main world gave it, extracted for the view.
#[derive(Component, Clone)]
pub struct ViewEnvironment {
    diffuse: bevy::asset::AssetId<bevy::image::Image>,
    specular: bevy::asset::AssetId<bevy::image::Image>,
    intensity: f32,
    rotation: bevy::math::Quat,
}

/// A view's environment map, once both cubes are on the GPU.
#[derive(Component, Clone)]
pub struct ViewEnvironmentTextures {
    diffuse: TextureView,
    specular: TextureView,
    /// How many mip levels the specular cube has, which roughness picks between.
    mips: u32,
    intensity: f32,
    /// The rotation undone, which turns a world direction into one to sample by.
    inverse_rotation: bevy::math::Quat,
}

/// Copies each camera's environment map, which lights a scene alongside every other kind of light a
/// shader can read, so a ray that leaves the scene can pick up the sky.
pub(super) fn extract_view_environments(
    mut commands: Commands,
    cameras: bevy::render::Extract<
        Query<(
            bevy::render::sync_world::RenderEntity,
            Option<&bevy::light::EnvironmentMapLight>,
        ), With<Camera>>,
    >,
) {
    for (render, environment) in &cameras {
        let Ok(mut view) = commands.get_entity(render) else { continue };

        match environment {
            Some(environment) => {
                view.insert(ViewEnvironment {
                    diffuse: environment.diffuse_map.id(),
                    specular: environment.specular_map.id(),
                    intensity: environment.intensity,
                    rotation: environment.rotation,
                });
            }
            None => {
                view.remove::<(ViewEnvironment, ViewEnvironmentTextures)>();
            }
        }
    }
}

/// Finds each view's environment cubes on the GPU, leaving a view whose cubes are still loading
/// with none.
pub(super) fn prepare_view_environments(
    mut commands: Commands,
    images: Res<RenderAssets<GpuImage>>,
    views: Query<(Entity, &ViewEnvironment)>,
) {
    for (entity, environment) in &views {
        let (Some(diffuse), Some(specular)) = (images.get(environment.diffuse), images.get(environment.specular))
        else {
            commands.entity(entity).remove::<ViewEnvironmentTextures>();
            continue;
        };

        commands.entity(entity).insert(ViewEnvironmentTextures {
            diffuse: diffuse.texture_view.clone(),
            specular: specular.texture_view.clone(),
            mips: specular.texture.mip_level_count(),
            intensity: environment.intensity,
            inverse_rotation: environment.rotation.inverse(),
        });
    }
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

pub(super) fn init_inputs(
    mut commands: Commands,
    render_device: Res<RenderDevice>,
    queue: Res<bevy::render::renderer::RenderQueue>,
) {
    let entry = |binding: u32, ty: BindingType| BindGroupLayoutEntry {
        binding,
        visibility: ShaderStages::VERTEX | ShaderStages::FRAGMENT | ShaderStages::COMPUTE,
        ty,
        count: None,
    };

    let float = |filterable: bool| BindingType::Texture {
        sample_type: TextureSampleType::Float { filterable },
        view_dimension: TextureViewDimension::D2,
        multisampled: false,
    };

    let cube = || BindingType::Texture {
        sample_type: TextureSampleType::Float { filterable: true },
        view_dimension: TextureViewDimension::Cube,
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
                min_binding_size: std::num::NonZeroU64::new(SCENE_INFO_BYTES),
            },
        ),
        entry(14, cube()),
        entry(15, cube()),
        entry(
            16,
            BindingType::Texture {
                sample_type: TextureSampleType::Float { filterable: true },
                view_dimension: TextureViewDimension::D2Array,
                multisampled: false,
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
        // Linear between mip levels as well, since the environment's specular cube is read at a
        // level roughness picks, which is rarely a whole one.
        sampler: render_device.create_sampler(&SamplerDescriptor {
            label: Some("bcs_view_sampler"),
            mag_filter: FilterMode::Linear,
            min_filter: FilterMode::Linear,
            mipmap_filter: bevy::render::render_resource::MipmapFilterMode::Linear,
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
        empty_environment: render_device
            .create_texture(&TextureDescriptor {
                label: Some("bcs_view_empty_environment"),
                size: Extent3d {
                    width: 1,
                    height: 1,
                    depth_or_array_layers: 6,
                },
                mip_level_count: 1,
                sample_count: 1,
                dimension: TextureDimension::D2,
                format: TextureFormat::Rgba16Float,
                usage: TextureUsages::TEXTURE_BINDING,
                view_formats: &[],
            })
            .create_view(&TextureViewDescriptor {
                dimension: Some(TextureViewDimension::Cube),
                ..Default::default()
            }),
        blue_noise: None,
        // Gray, a half everywhere, the average of noise, so a shader running before the real one
        // arrives gets no pattern rather than a wrong one.
        empty_blue_noise: {
            let texture = render_device.create_texture_with_data(
                &bevy::render::renderer::RenderQueue::clone(&queue),
                &TextureDescriptor {
                    label: Some("bcs_view_empty_blue_noise"),
                    size: Extent3d { width: 1, height: 1, depth_or_array_layers: 1 },
                    mip_level_count: 1,
                    sample_count: 1,
                    dimension: TextureDimension::D2,
                    format: TextureFormat::Rgba8Unorm,
                    usage: TextureUsages::TEXTURE_BINDING,
                    view_formats: &[],
                },
                bevy::render::render_resource::TextureDataOrder::default(),
                &[128, 128, 128, 128],
            );

            texture.create_view(&TextureViewDescriptor {
                dimension: Some(TextureViewDimension::D2Array),
                ..Default::default()
            })
        },
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
    pub environment: Option<&'a ViewEnvironmentTextures>,
}

/// The size of what binding thirteen holds: counts of the scene's lights, and the environment's
/// intensity, rotation and mip count, which `bcs_pass::SceneInfo` mirrors.
const SCENE_INFO_BYTES: u64 = 32;

impl<'a> ViewInputSources<'a> {
    /// Collects a view's prepass textures, with a stand-in for each it does not have.
    pub fn gather(
        inputs: &ViewInputs,
        fallback: &FallbackImage,
        prepass: Option<&ViewPrepassTextures>,
        previous: Option<(&'a PreviousViewUniforms, &PreviousViewUniformOffset)>,
        environment: Option<&'a ViewEnvironmentTextures>,
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
            environment,
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

        // No mip levels is how a shader tells there is no environment, since a cube always has one.
        let (mips, intensity, rotation) = self.environment.map_or((0, 0.0, bevy::math::Quat::IDENTITY), |environment| {
            (environment.mips, environment.intensity, environment.inverse_rotation)
        });

        let mut info = [0u32; 8];
        info[0] = point_lights;
        info[1] = mips;
        info[2] = intensity.to_bits();
        info[4..8].copy_from_slice(&rotation.to_array().map(f32::to_bits));

        let counts = device.create_buffer_with_data(&bevy::render::render_resource::BufferInitDescriptor {
            label: Some("bcs_view_scene_info"),
            contents: bytemuck::cast_slice(&info),
            usage: BufferUsages::UNIFORM,
        });

        let (environment_diffuse, environment_specular) = match self.environment {
            Some(environment) => (&environment.diffuse, &environment.specular),
            None => (&inputs.empty_environment, &inputs.empty_environment),
        };

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
                BindGroupEntry {
                    binding: 14,
                    resource: BindingResource::TextureView(environment_diffuse),
                },
                BindGroupEntry {
                    binding: 15,
                    resource: BindingResource::TextureView(environment_specular),
                },
                BindGroupEntry {
                    binding: 16,
                    resource: BindingResource::TextureView(
                        inputs.blue_noise.as_ref().unwrap_or(&inputs.empty_blue_noise),
                    ),
                },
            ],
        );

        Some((group, [view_offset, previous_offset, light_offset]))
    }

    /// The start of a stand-in buffer, as long as one element of what it stands in for, as a
    /// binding with a dynamic offset takes.
    fn whole(buffer: &Buffer, size: std::num::NonZeroU64) -> BindingResource<'_> {
        BindingResource::Buffer(BufferBinding {
            buffer,
            offset: 0,
            size: Some(size),
        })
    }
}
