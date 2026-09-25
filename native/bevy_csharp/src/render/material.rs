//! The material a shader the game wrote draws with.
//!
//! One Rust type for every such material, whichever shader draws it. Bevy asks a material's type
//! for its shaders through functions with no `self`, which would make one type one shader, but it
//! also hands `Material::specialize` a key taken from the instance, and a pipeline descriptor it
//! may rewrite. The key carries the number of a program (see [`super::programs`]), and
//! `specialize` puts that program's shaders into the descriptor. Every material with the same
//! program and the same face culling shares a pipeline, and there is no limit on how many
//! programs there are.
//!
//! **What a material carries.** The bind group is the same for every program, because Bevy lays
//! it out per type. It is sized to cover what shaders ask for rather than a common case:
//!
//! | binding | holds |
//! |---|---|
//! | 0 | sixty-four floats, as sixteen `vec4`, in a uniform |
//! | 1, 2 | texture zero and its sampler |
//! | 3 | a read-only storage buffer of any size: whatever bytes the caller gave, or a shared buffer |
//! | 4 to 17 | textures one to seven, each followed by its sampler |
//! | 18, 19 | two cubemaps |
//! | 20, 21 | two 2D array textures |
//! | 22, 23 | two 3D textures |
//!
//! The first three bindings are where a slot's sixteen floats and one picture have always been, so
//! a shader written for that layout reads the same values from this one.
//!
//! The cube, array and 3D textures have no samplers of their own. A sampler is the scarcest kind of
//! binding on some backends (Metal allows sixteen per stage, and Bevy's view already uses up to
//! eight), and any of the eight samplers samples them.
//!
//! **The prepass.** Bevy draws depth for shadows, and normals and motion for the effects that read
//! them, in a pass of its own with shaders of its own. A program may name a prepass vertex shader,
//! so a material that moves its own geometry casts the shadow of the shape it drew, and a prepass
//! fragment shader, so one that discards pixels casts a shadow with the same holes.

#![cfg(feature = "render")]

use std::num::NonZeroU64;
use std::sync::Arc;
use std::sync::atomic::{AtomicBool, Ordering};

use bevy::asset::{Asset, AssetPath, Assets, Handle};
use bevy::ecs::system::SystemParamItem;
use bevy::ecs::system::lifetimeless::SRes;
use bevy::ecs::world::World;
use bevy::image::Image;
use bevy::material::AlphaMode;
use bevy::mesh::{Mesh, MeshVertexBufferLayoutRef};
use bevy::pbr::{Material, MaterialPipeline, MaterialPipelineKey, MaterialPlugin};
use bevy::reflect::TypePath;
use bevy::render::render_asset::RenderAssets;
use bevy::render::render_resource::{
    AsBindGroup, AsBindGroupError, BindGroupLayout, BindGroupLayoutEntry, BindingResources,
    BindingType, BufferBindingType, BufferInitDescriptor, BufferUsages, Face, FragmentState,
    OwnedBindingResource, RenderPipelineDescriptor, Sampler, SamplerBindingType, ShaderStages,
    SpecializedMeshPipelineError, TextureSampleType, TextureView, TextureViewDimension,
    UnpreparedBindGroup,
};
use bevy::render::renderer::RenderDevice;
use bevy::render::texture::{FallbackImage, GpuImage};
use bevy::shader::{Shader, ShaderDefVal, ShaderRef};

use super::programs::{self, Role};

/// How many floats a material carries.
pub const PARAMETER_COUNT: usize = 64;

/// How many 2D textures, each with a sampler, a material carries.
pub const TEXTURE_COUNT: usize = 8;

/// How many of each of the other kinds of texture a material carries.
pub const EXTRA_COUNT: usize = 2;

/// Where the data buffer is bound.
const DATA_BINDING: u32 = 3;

/// The size of the data buffer when the material was given nothing, because a buffer has to have
/// a size to be bound.
const EMPTY_DATA: usize = 16;

/// Where 2D texture `index` and its sampler are bound.
const fn texture_bindings(index: usize) -> (u32, u32) {
    if index == 0 {
        (1, 2)
    } else {
        let texture = 4 + (index as u32 - 1) * 2;
        (texture, texture + 1)
    }
}

const CUBE_BINDING: u32 = 18;
const ARRAY_BINDING: u32 = 20;
const VOLUME_BINDING: u32 = 22;

/// A material drawn by a program the game named.
#[derive(Asset, TypePath, Clone)]
pub struct BcsShaderMaterial {
    /// Which program draws it, as a number from [`programs`].
    pub program: u32,
    pub parameters: [f32; PARAMETER_COUNT],
    /// Shared rather than owned, because Bevy clones a material to hand it to the render world,
    /// and the data may be large.
    pub data: Arc<[u8]>,
    /// A buffer bound at the data's binding in place of the data, which is how a material draws
    /// what a compute shader wrote.
    pub buffer: Option<Handle<bevy::render::storage::ShaderBuffer>>,
    pub textures: [Option<Handle<Image>>; TEXTURE_COUNT],
    pub cubes: [Option<Handle<Image>>; EXTRA_COUNT],
    pub arrays: [Option<Handle<Image>>; EXTRA_COUNT],
    pub volumes: [Option<Handle<Image>>; EXTRA_COUNT],
    pub alpha: AlphaMode,
    pub cull: Option<Face>,
    pub depth_bias: f32,
}

impl BcsShaderMaterial {
    /// A material for `program` with nothing set.
    pub fn new(program: u32) -> Self {
        Self {
            program,
            parameters: [0.0; PARAMETER_COUNT],
            data: Arc::from(Vec::new()),
            buffer: None,
            textures: Default::default(),
            cubes: Default::default(),
            arrays: Default::default(),
            volumes: Default::default(),
            alpha: AlphaMode::Opaque,
            cull: Some(Face::Back),
            depth_bias: 0.0,
        }
    }
}

/// What decides a material's pipeline: the program, and which faces it culls.
///
/// Everything else about a material is in its bind group, which changes without a new pipeline.
#[derive(Clone, Copy, PartialEq, Eq, Hash, Debug)]
pub struct BcsShaderKey {
    pub program: u32,
    /// `0` back, `1` front, `2` neither.
    pub cull: u8,
}

/// What view dimension a GPU image is sampled with, which is what a binding has to match.
fn view_dimension(image: &GpuImage) -> TextureViewDimension {
    if let Some(dimension) = image
        .texture_view_descriptor
        .as_ref()
        .and_then(|descriptor| descriptor.dimension)
    {
        return dimension;
    }

    // What wgpu infers when the view does not say, which is what Bevy leaves it to.
    match image.texture_descriptor.dimension {
        bevy::render::render_resource::TextureDimension::D1 => TextureViewDimension::D1,
        bevy::render::render_resource::TextureDimension::D2 => {
            if image.texture_descriptor.size.depth_or_array_layers > 1 {
                TextureViewDimension::D2Array
            } else {
                TextureViewDimension::D2
            }
        }
        bevy::render::render_resource::TextureDimension::D3 => TextureViewDimension::D3,
    }
}

/// The texture and sampler to bind for one slot.
///
/// An image of the wrong shape or a format that cannot be filtered is replaced by the fallback
/// rather than bound, because binding it would be a validation error, and wgpu's answer to one of
/// those is to stop drawing altogether.
fn texture_for(
    images: &RenderAssets<GpuImage>,
    fallback: &FallbackImage,
    handle: &Option<Handle<Image>>,
    dimension: TextureViewDimension,
) -> Result<(TextureView, Sampler), AsBindGroupError> {
    let stand_in = match dimension {
        TextureViewDimension::Cube => &fallback.cube,
        TextureViewDimension::D2Array => &fallback.d2_array,
        TextureViewDimension::D3 => &fallback.d3,
        _ => &fallback.d2,
    };

    let Some(handle) = handle else {
        return Ok((stand_in.texture_view.clone(), stand_in.sampler.clone()));
    };

    // Not uploaded yet. Asking again next frame is what Bevy's own materials do, and it means a
    // material never draws for a frame with a white square where its picture belongs.
    let Some(image) = images.get(handle) else {
        return Err(AsBindGroupError::RetryNextUpdate);
    };

    if view_dimension(image) != dimension {
        bevy::log::warn_once!(
            "A shader material was given a {:?} texture where it binds a {:?} one, so the slot \
             holds the fallback instead.",
            view_dimension(image),
            dimension
        );
        return Ok((stand_in.texture_view.clone(), stand_in.sampler.clone()));
    }

    let filterable = image.texture_descriptor.format.sample_type(None, None)
        == Some(TextureSampleType::Float { filterable: true });

    if !filterable {
        bevy::log::warn_once!(
            "A shader material was given a {:?} texture, which cannot be filtered, so the slot \
             holds the fallback instead.",
            image.texture_descriptor.format
        );
        return Ok((stand_in.texture_view.clone(), stand_in.sampler.clone()));
    }

    Ok((image.texture_view.clone(), image.sampler.clone()))
}

impl AsBindGroup for BcsShaderMaterial {
    type Data = BcsShaderKey;
    type Param = (
        SRes<RenderAssets<GpuImage>>,
        SRes<FallbackImage>,
        SRes<RenderAssets<bevy::render::storage::GpuShaderBuffer>>,
    );

    fn label() -> &'static str {
        "bcs_shader_material"
    }

    fn bind_group_data(&self) -> Self::Data {
        BcsShaderKey {
            program: self.program,
            cull: match self.cull {
                Some(Face::Back) => 0,
                Some(Face::Front) => 1,
                None => 2,
            },
        }
    }

    fn unprepared_bind_group(
        &self,
        _layout: &BindGroupLayout,
        render_device: &RenderDevice,
        (images, fallback, buffers): &mut SystemParamItem<'_, '_, Self::Param>,
        _force_no_bindless: bool,
    ) -> Result<UnpreparedBindGroup, AsBindGroupError> {
        let mut bindings = Vec::with_capacity(24);

        // A shared buffer that has not reached the GPU yet is waited for, like a picture.
        let shared = match &self.buffer {
            Some(handle) => match buffers.get(handle) {
                Some(gpu) => Some(gpu.buffer.clone()),
                None => return Err(AsBindGroupError::RetryNextUpdate),
            },
            None => None,
        };

        // Textures before buffers, so a picture that has not arrived gives up before anything has
        // been allocated.
        for (index, handle) in self.textures.iter().enumerate() {
            let (view, sampler) = texture_for(images, fallback, handle, TextureViewDimension::D2)?;
            let (texture_binding, sampler_binding) = texture_bindings(index);

            bindings.push((
                texture_binding,
                OwnedBindingResource::TextureView(TextureViewDimension::D2, view),
            ));
            bindings.push((
                sampler_binding,
                OwnedBindingResource::Sampler(SamplerBindingType::Filtering, sampler),
            ));
        }

        for (first, dimension, handles) in [
            (CUBE_BINDING, TextureViewDimension::Cube, &self.cubes),
            (ARRAY_BINDING, TextureViewDimension::D2Array, &self.arrays),
            (VOLUME_BINDING, TextureViewDimension::D3, &self.volumes),
        ] {
            for (index, handle) in handles.iter().enumerate() {
                let (view, _) = texture_for(images, fallback, handle, dimension)?;
                bindings.push((
                    first + index as u32,
                    OwnedBindingResource::TextureView(dimension, view),
                ));
            }
        }

        let parameters = render_device.create_buffer_with_data(&BufferInitDescriptor {
            label: Some("bcs_shader_material_parameters"),
            contents: bytemuck::cast_slice(&self.parameters),
            usage: BufferUsages::UNIFORM | BufferUsages::COPY_DST,
        });

        bindings.push((0, OwnedBindingResource::Buffer(parameters)));

        let storage = match shared {
            Some(buffer) => buffer,
            None => {
                // Rounded up to a whole number of words, because a shader reads it as an array of
                // them, and never empty.
                let mut data = self.data.to_vec();
                data.resize(data.len().max(EMPTY_DATA).next_multiple_of(4), 0);

                render_device.create_buffer_with_data(&BufferInitDescriptor {
                    label: Some("bcs_shader_material_data"),
                    contents: &data,
                    usage: BufferUsages::STORAGE | BufferUsages::COPY_DST,
                })
            }
        };

        bindings.push((DATA_BINDING, OwnedBindingResource::Buffer(storage)));

        Ok(UnpreparedBindGroup {
            bindings: BindingResources(bindings),
        })
    }

    fn bind_group_layout_entries(
        _render_device: &RenderDevice,
        _force_no_bindless: bool,
    ) -> Vec<BindGroupLayoutEntry>
    where
        Self: Sized,
    {
        // Both stages, because a vertex shader that displaces a mesh reads the same numbers and
        // samples the same heightmap the fragment shader colors it with.
        let visibility = ShaderStages::VERTEX_FRAGMENT;

        let entry = |binding: u32, ty: BindingType| BindGroupLayoutEntry {
            binding,
            visibility,
            ty,
            count: None,
        };

        let texture = |dimension: TextureViewDimension| BindingType::Texture {
            sample_type: TextureSampleType::Float { filterable: true },
            view_dimension: dimension,
            multisampled: false,
        };

        let mut entries = vec![
            entry(
                0,
                BindingType::Buffer {
                    ty: BufferBindingType::Uniform,
                    has_dynamic_offset: false,
                    min_binding_size: NonZeroU64::new((PARAMETER_COUNT * 4) as u64),
                },
            ),
            entry(
                DATA_BINDING,
                BindingType::Buffer {
                    ty: BufferBindingType::Storage { read_only: true },
                    has_dynamic_offset: false,
                    min_binding_size: None,
                },
            ),
        ];

        for index in 0..TEXTURE_COUNT {
            let (texture_binding, sampler_binding) = texture_bindings(index);
            entries.push(entry(texture_binding, texture(TextureViewDimension::D2)));
            entries.push(entry(
                sampler_binding,
                BindingType::Sampler(SamplerBindingType::Filtering),
            ));
        }

        for (first, dimension) in [
            (CUBE_BINDING, TextureViewDimension::Cube),
            (ARRAY_BINDING, TextureViewDimension::D2Array),
            (VOLUME_BINDING, TextureViewDimension::D3),
        ] {
            for index in 0..EXTRA_COUNT as u32 {
                entries.push(entry(first + index, texture(dimension)));
            }
        }

        entries.sort_by_key(|entry| entry.binding);
        entries
    }
}

/// Stands in for a program that does not exist, which a material cannot normally reach, since
/// every call that names a program checks it first.
pub const MISSING_PROGRAM: Handle<Shader> =
    bevy::asset::uuid_handle!("6f0e8a52-3b1d-4c2e-9a47-5d8f1b0c7e31");

impl Material for BcsShaderMaterial {
    // Named so that the prepass binds the material at all. Bevy leaves the bind group out of a
    // prepass whose shaders it knows do not read it, and it only knows which shaders those are
    // from what the type answers here, which is the same for every material of the type. A
    // program with a prepass shader of its own reads the bind group, so the type has to say that
    // its prepass might. This is Bevy's own prepass shader, which is what draws when a program
    // names none.
    fn prepass_vertex_shader() -> ShaderRef {
        ShaderRef::Path(AssetPath::from("embedded://bevy_pbr/prepass/prepass.wgsl"))
    }

    fn alpha_mode(&self) -> AlphaMode {
        self.alpha
    }

    fn depth_bias(&self) -> f32 {
        self.depth_bias
    }

    fn specialize(
        _pipeline: &MaterialPipeline,
        descriptor: &mut RenderPipelineDescriptor,
        layout: &MeshVertexBufferLayoutRef,
        key: MaterialPipelineKey<Self>,
    ) -> Result<(), SpecializedMeshPipelineError> {
        descriptor.primitive.cull_mode = match key.bind_group_data.cull {
            0 => Some(Face::Back),
            1 => Some(Face::Front),
            _ => None,
        };

        let Some(program) = programs::lookup(key.bind_group_data.program) else {
            // The fragment shader Bevy put there is the standard material's, which reads a bind
            // group laid out differently from this one and would fail to build.
            if let Some(fragment) = descriptor.fragment.as_mut() {
                fragment.shader = MISSING_PROGRAM;
                fragment.entry_point = Some("fragment".into());
            }
            return Ok(());
        };

        descriptor.vertex.shader_defs.extend(program.defs.iter().cloned());
        if let Some(fragment) = descriptor.fragment.as_mut() {
            fragment.shader_defs.extend(program.defs.iter().cloned());
        }

        let prepass = descriptor
            .vertex
            .shader_defs
            .iter()
            .any(|def| matches!(def, ShaderDefVal::Bool(name, true) if name == "PREPASS_PIPELINE"));

        if prepass {
            if let Some(stage) = &program.stages[Role::PrepassVertex as usize] {
                descriptor.vertex.shader = stage.shader.clone();
                descriptor.vertex.entry_point = Some(stage.entry.clone());

                // Every attribute the mesh has, at the locations Bevy's prepass uses. Bevy hands a
                // shadow pass the position alone, which is all its own shader reads, and a shader
                // that moves the mesh along its normal needs the normal there too.
                descriptor.vertex.buffers = vec![layout.0.get_layout(&prepass_attributes(layout))?];
            }

            if let Some(stage) = &program.stages[Role::PrepassFragment as usize] {
                match descriptor.fragment.as_mut() {
                    Some(fragment) => {
                        fragment.shader = stage.shader.clone();
                        fragment.entry_point = Some(stage.entry.clone());
                    }

                    // A depth-only pass has no fragment stage, and one that discards needs one,
                    // so it gets one that writes to no target.
                    None => {
                        descriptor.fragment = Some(FragmentState {
                            shader: stage.shader.clone(),
                            shader_defs: descriptor.vertex.shader_defs.clone(),
                            entry_point: Some(stage.entry.clone()),
                            targets: Vec::new(),
                        });
                    }
                }
            }

            return Ok(());
        }

        if let Some(stage) = &program.stages[Role::Vertex as usize] {
            descriptor.vertex.shader = stage.shader.clone();
            descriptor.vertex.entry_point = Some(stage.entry.clone());
        }

        if let (Some(stage), Some(fragment)) = (
            &program.stages[Role::Fragment as usize],
            descriptor.fragment.as_mut(),
        ) {
            fragment.shader = stage.shader.clone();
            fragment.entry_point = Some(stage.entry.clone());
        }

        Ok(())
    }
}

/// The attributes a prepass vertex shader of a program's own is handed, where the mesh has them.
fn prepass_attributes(
    layout: &MeshVertexBufferLayoutRef,
) -> Vec<bevy::mesh::VertexAttributeDescriptor> {
    let mut attributes = vec![Mesh::ATTRIBUTE_POSITION.at_shader_location(0)];

    for (attribute, location) in [
        (Mesh::ATTRIBUTE_UV_0, 1),
        (Mesh::ATTRIBUTE_UV_1, 2),
        (Mesh::ATTRIBUTE_NORMAL, 3),
        (Mesh::ATTRIBUTE_TANGENT, 4),
        (Mesh::ATTRIBUTE_JOINT_INDEX, 5),
        (Mesh::ATTRIBUTE_JOINT_WEIGHT, 6),
        (Mesh::ATTRIBUTE_COLOR, 7),
    ] {
        if layout.0.contains(attribute.id) {
            attributes.push(attribute.at_shader_location(location));
        }
    }

    attributes
}

// -- Errors the renderer reports

/// Whether a validation error leaves the app running rather than closing it.
static KEEP_RENDERING: AtomicBool = AtomicBool::new(false);

/// The last error the renderer reported, for whoever asks.
static LAST_ERROR: std::sync::Mutex<String> = std::sync::Mutex::new(String::new());

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
/// A shader that compiles can still disagree with the pipeline it is put in, by reading a
/// binding as a different type or an input the vertex shader never wrote. That is a validation
/// error, and Bevy's answer to any of those is to close the app, which is right for a shipped game
/// and wrong for one somebody is editing a shader in. Surviving means the frames that use the
/// broken pipeline are not drawn until the shader is fixed and reloads, and every other error
/// still closes the app.
fn on_render_error(
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

/// Adds what draws shader materials and shader passes, what runs compute shaders, and what
/// compiles and reloads the programs all three are made of.
///
/// Every app that draws gets it, because a program is made while the app runs and there is no
/// saying beforehand whether one will be.
pub fn install(app: &mut bevy::app::App, root: std::path::PathBuf) {
    use bevy::app::First;

    programs::forget_all();

    app.add_plugins(MaterialPlugin::<BcsShaderMaterial>::default());
    super::passes::install(app);
    super::compute::install(app);
    app.insert_resource(programs::ShaderPrograms::new(root));
    app.add_systems(First, programs::update);
    app.insert_resource(bevy::render::error_handler::RenderErrorHandler(on_render_error));

    if let Some(mut shaders) = app.world_mut().get_resource_mut::<Assets<Shader>>() {
        let _ = shaders.insert(
            MISSING_PROGRAM.id(),
            Shader::from_wgsl(
                "@fragment\nfn fragment(@builtin(position) position: vec4<f32>) -> @location(0) vec4<f32> {\n    \
                 return vec4<f32>(1.0, 0.0, 1.0, 1.0);\n}\n",
                "bevy_csharp/missing_program.wgsl",
            ),
        );
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn the_first_texture_stays_where_a_slot_had_it() {
        assert_eq!(texture_bindings(0), (1, 2));
        assert_eq!(texture_bindings(1), (4, 5));
        assert_eq!(texture_bindings(7), (16, 17));
    }
}
